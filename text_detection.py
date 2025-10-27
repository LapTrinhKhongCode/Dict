import os
import importlib.util
import sys
import cv2
import numpy as np
import torch
import re
import base64
import json

TRAIN_PY_PATH = r"G:\GPT\OCR\SourceCode.py"
CHECKPOINT_PATH = r"G:\GPT\OCR\checkpoints_seq\best_seq.pt"
CRAFT_WEIGHTS = r"G:\GPT\CRAFT-pytorch\craft_mlt_25k.pth"
OUTPUT_JSON = r"G:\GPT\OCR\checkpoints_seq\infer_results.json"
CRAFT_PATH = r"G:\GPT\CRAFT-pytorch"
CHARSET_PATH = os.path.join(os.path.dirname(TRAIN_PY_PATH), "charsett.txt")
INFER_HEIGHT = 32
TEXT_THRESHOLD = 0.5
LINK_THRESHOLD = 0.4
LOW_TEXT = 0.22
MSER_DELTA = 4
MSER_MIN_AREA = 7
MSER_MAX_AREA = 1000

sys.path.insert(0, CRAFT_PATH)


def _safe_import(path, modname):
    spec = importlib.util.spec_from_file_location(modname, path)
    mod = importlib.util.module_from_spec(spec)
    sys.modules[modname] = mod
    spec.loader.exec_module(mod)
    return mod


craft = _safe_import(os.path.join(CRAFT_PATH, "craft.py"), "craft")
craft_utils = _safe_import(os.path.join(CRAFT_PATH, "craft_utils.py"), "craft_utils")
imgproc = _safe_import(os.path.join(CRAFT_PATH, "imgproc.py"), "imgproc")
CRAFT = craft.CRAFT


def import_train_module(path):
    spec = importlib.util.spec_from_file_location("train_module_for_infer", path)
    mod = importlib.util.module_from_spec(spec)
    sys.modules["train_module_for_infer"] = mod
    spec.loader.exec_module(mod)
    return mod


train_mod = None


def load_charset(path):
    with open(path, encoding='utf-8') as f:
        return [line.strip() for line in f if line.strip()]


def load_ckpt_into_model(model, ckpt_path, device):
    ck = None
    try:
        ck = torch.load(ckpt_path, map_location=device, weights_only=False)
    except Exception:
        try:
            ck = torch.load(ckpt_path, map_location=device)
        except Exception:
            try:
                ck = torch.load(ckpt_path, map_location=device, weights_only=True)
            except Exception as e:
                raise RuntimeError(f"Failed to load checkpoint {ckpt_path}: {e}")
    if isinstance(ck, dict):
        model_state = ck.get('model_state') or ck.get('state_dict') or ck
    else:
        model_state = ck
    new_state_dict = {}
    if isinstance(model_state, dict):
        for k, v in model_state.items():
            if k.startswith("module."):
                new_state_dict[k[7:]] = v
            else:
                new_state_dict[k] = v
    else:
        new_state_dict = model_state
    try:
        model.load_state_dict(new_state_dict, strict=False)
    except Exception:
        pass
    model.to(device)
    model.eval()
    return model


def greedy_decode_fallback(logits, charset, blank_idx):
    probs = logits.argmax(dim=2).cpu().numpy()
    batch = []
    for n in range(probs.shape[1]):
        seq = probs[:, n].tolist()
        last = None
        out_chars = []
        for s in seq:
            if s == last:
                last = s
                continue
            if s != blank_idx:
                if s < len(charset):
                    out_chars.append(charset[s])
            last = s
        batch.append(''.join(out_chars))
    return batch


def infer_image_array(img_gray, model, device, charset, blank_idx, height=INFER_HEIGHT):
    if img_gray is None or img_gray.size == 0:
        return ''
    if img_gray.ndim == 3:
        img_gray = cv2.cvtColor(img_gray, cv2.COLOR_BGR2GRAY)
    h, w = img_gray.shape
    if h <= 0 or w <= 0:
        return ''
    new_h = height
    scale = new_h / float(h)
    new_w = max(1, int(w * scale))
    img = cv2.resize(img_gray, (new_w, new_h), interpolation=cv2.INTER_CUBIC)
    img_t = (img.astype(np.float32) / 255.0 - 0.5) / 0.5
    tensor = torch.from_numpy(img_t).unsqueeze(0).unsqueeze(0).float().to(device)
    with torch.no_grad():
        logits = model(tensor)
    greedy_decode = getattr(train_mod, 'greedy_decode', None) or greedy_decode_fallback
    preds = greedy_decode(logits, charset, blank_idx)
    return preds[0] if preds else ''


def mser_fallback_regions(image_gray):
    try:
        mser = cv2.MSER_create(MSER_DELTA, MSER_MIN_AREA, MSER_MAX_AREA, 0.25, 0.2)
    except Exception:
        mser = cv2.MSER_create()
    regions, _ = mser.detectRegions(image_gray)
    boxes = []
    for p in regions:
        arr = np.array(p).reshape(-1, 2)
        x, y, w, h = cv2.boundingRect(arr)
        if w < 4 or h < 4:
            continue
        ar = w / float(h)
        if ar < 0.1 or ar > 10:
            continue
        boxes.append(np.array([[x, y], [x + w, y], [x + w, y + h], [x, y + h]], dtype=np.int32))
    final = []
    for b in boxes:
        bx = cv2.boundingRect(b)
        x, y, w, h = bx
        merged = False
        for i, fb in enumerate(final):
            fx, fy, fw, fh = cv2.boundingRect(fb)
            ix = max(x, fx); iy = max(y, fy)
            ix2 = min(x + w, fx + fw); iy2 = min(y + h, fy + fh)
            iw = max(0, ix2 - ix); ih = max(0, iy2 - iy)
            if iw * ih > 0.3 * (w * h):
                nx = min(x, fx); ny = min(y, fy)
                nx2 = max(x + w, fx + fw); ny2 = max(y + h, fy + fh)
                final[i] = np.array([[nx, ny], [nx2, ny], [nx2, ny2], [nx, ny2]], dtype=np.int32)
                merged = True
                break
        if not merged:
            final.append(b)
    return final


def flatten_polys(polys):
    out = []
    if polys is None:
        return out
    if isinstance(polys, np.ndarray):
        if polys.ndim == 2 and polys.shape[1] == 2:
            out.append(polys)
        return out
    if isinstance(polys, (list, tuple)):
        for p in polys:
            if p is None:
                continue
            if isinstance(p, (list, tuple, np.ndarray)):
                arr = np.array(p, dtype=np.float32)
                if arr.ndim == 2 and arr.shape[1] == 2:
                    out.append(arr)
                else:
                    out.extend(flatten_polys(p))
    return out


def save_heatmap(img_rgb, mat, path, normalize=True):
    try:
        mat = np.asarray(mat)
        if normalize:
            mn, mx = float(mat.min()), float(mat.max())
            if mx - mn > 1e-6:
                mat_norm = (mat - mn) / (mx - mn)
            else:
                mat_norm = np.zeros_like(mat)
        else:
            mat_norm = mat
        heat = (mat_norm * 255).astype(np.uint8)
        h_img, w_img = img_rgb.shape[:2]
        heat_resized = cv2.resize(heat, (w_img, h_img), interpolation=cv2.INTER_LINEAR)
        heat_color = cv2.applyColorMap(heat_resized, cv2.COLORMAP_JET)
        overlay = cv2.addWeighted(cv2.cvtColor(img_rgb, cv2.COLOR_RGB2BGR), 0.6, heat_color, 0.4, 0)
        cv2.imwrite(path, overlay)
    except Exception:
        pass


def debug_print_detres(det_res):
    try:
        if isinstance(det_res, (list, tuple)):
            for i, item in enumerate(det_res):
                pass
    except Exception:
        pass


def get_text_boxes(image, craft_model, device, text_threshold=TEXT_THRESHOLD, link_threshold=LINK_THRESHOLD, low_text=LOW_TEXT, save_debug_dir=None):
    if save_debug_dir is None:
        save_debug_dir = None
    if save_debug_dir:
        os.makedirs(save_debug_dir, exist_ok=True)

    image_resized, target_ratio, size_heatmap = imgproc.resize_aspect_ratio(image, 1280, interpolation=cv2.INTER_LINEAR, mag_ratio=1.5)
    ratio_h = ratio_w = 1 / target_ratio

    x = imgproc.normalizeMeanVariance(image_resized)
    x = torch.from_numpy(x).permute(2, 0, 1).unsqueeze(0)
    x = x.to(device)

    with torch.no_grad():
        y, _ = craft_model(x)

    score_text = y[0, :, :, 0].cpu().data.numpy()
    score_link = y[0, :, :, 1].cpu().data.numpy()

    try:
        det_res = craft_utils.getDetBoxes(score_text, score_link, text_threshold, link_threshold, low_text)
    except Exception:
        det_res = None

    debug_print_detres(det_res)

    boxes = None
    polys = None
    if isinstance(det_res, (list, tuple)) and len(det_res) == 2:
        boxes, polys = det_res
    else:
        polys = det_res

    flat_polys = flatten_polys(polys)
    if len(flat_polys) == 0 and boxes is not None:
        flat_polys = flatten_polys(boxes)

    if len(flat_polys) == 0:
        gray_resized = cv2.cvtColor(image_resized, cv2.COLOR_RGB2GRAY)
        mser_polys = mser_fallback_regions(gray_resized)
        scaled = []
        for p in mser_polys:
            arr = np.array(p, dtype=np.float32)
            scaled.append((arr * np.array([ratio_w, ratio_h], dtype=np.float32)).astype(np.int32))
        return scaled

    try:
        boxes_out = craft_utils.adjustResultCoordinates([p.tolist() for p in flat_polys], ratio_w, ratio_h)
    except Exception:
        boxes_out = []
        for arr in flat_polys:
            scaled = arr * np.array([ratio_w, ratio_h], dtype=np.float32)
            boxes_out.append(scaled.tolist())

    final_polys = []
    for p in boxes_out:
        try:
            p_arr = np.array(p, dtype=np.int32)
            if p_arr.ndim == 1 and p_arr.size % 2 == 0:
                p_arr = p_arr.reshape(-1, 2)
            final_polys.append(p_arr)
        except Exception:
            pass
    return final_polys


def crop_text_region(image, poly):
    if poly is None:
        return np.array([], dtype=image.dtype), (0, 0, 0, 0)
    poly = np.array(poly, dtype=np.int32)
    if poly.size == 0:
        return np.array([], dtype=image.dtype), (0, 0, 0, 0)
    try:
        x, y, w, h = cv2.boundingRect(poly)
    except Exception:
        return np.array([], dtype=image.dtype), (0, 0, 0, 0)
    h_img, w_img = image.shape[:2]
    x = max(0, x)
    y = max(0, y)
    x2 = min(w_img, x + w)
    y2 = min(h_img, y + h)
    if x2 <= x or y2 <= y:
        return np.array([], dtype=image.dtype), (x, y, 0, 0)
    cropped = image[y:y2, x:x2]
    return cropped, (x, y, x2 - x, y2 - y)


def imencode_to_dataurl(img_bgr):
    success, buf = cv2.imencode('.png', img_bgr)
    if not success:
        return ''
    b64 = base64.b64encode(buf.tobytes()).decode('ascii')
    return f"data:image/png;base64,{b64}"


def process_image(image_bgr, craft_model, rec_model, device, charset, blank_idx, infer_height=INFER_HEIGHT):
    image_rgb = cv2.cvtColor(image_bgr, cv2.COLOR_BGR2RGB)
    image_gray = cv2.cvtColor(image_bgr, cv2.COLOR_BGR2GRAY)
    text_boxes = get_text_boxes(image_rgb, craft_model, device, text_threshold=TEXT_THRESHOLD, link_threshold=LINK_THRESHOLD, low_text=LOW_TEXT)
    if text_boxes is None or len(text_boxes) == 0:
        text_boxes = detect_lines_directly(image_gray)
    else:
        text_boxes = merge_polygons_into_lines(text_boxes, image_gray, max_line_gap=20, min_x_overlap_ratio=0.02, split_min_line_height=12, split_min_blank_height=6, debug=False)
    results = []
    sorted_boxes = sorted(text_boxes, key=lambda p: cv2.boundingRect(p)[1])
    for idx, poly in enumerate(sorted_boxes, start=1):
        cropped_region, bbox = crop_text_region(image_gray, poly)
        if cropped_region.size == 0:
            continue
        pred_text = infer_image_array(cropped_region, rec_model, device, charset, blank_idx, height=infer_height)
        pred_text = re.sub(r'\s+', ' ', pred_text).strip()
        box_list = poly.tolist() if isinstance(poly, np.ndarray) else list(map(list, poly))
        results.append({"line_number": idx, "text": pred_text, "bbox": bbox, "box_points": box_list})
    return results


# Sửa lại hàm process_image_stream

def process_image_stream(image_bgr, craft_model, rec_model, device, charset, blank_idx, infer_height=INFER_HEIGHT):
    
    # --- BƯỚC 1: YIELD TRẠNG THÁI "BẮT ĐẦU" NGAY LẬP TỨC ---
    yield {"status": "processing", "message": "Bắt đầu xử lý ảnh..."}

    image_rgb = cv2.cvtColor(image_bgr, cv2.COLOR_BGR2RGB)
    image_gray = cv2.cvtColor(image_bgr, cv2.COLOR_BGR2GRAY)

    # --- BƯỚC 2: YIELD TRẠNG THÁI "ĐANG PHÁT HIỆN" ---
    # Dòng này sẽ được gửi đi ngay, trước khi chạy hàm 5 giây
    yield {"status": "detecting", "message": "Đang phát hiện văn bản (CRAFT)..."}

    # Đây là hàm nặng 5 giây (Blocking call)
    text_boxes = get_text_boxes(image_rgb, craft_model, device, text_threshold=TEXT_THRESHOLD, link_threshold=LINK_THRESHOLD, low_text=LOW_TEXT)
    
    if text_boxes is None or len(text_boxes) == 0:
        text_boxes = detect_lines_directly(image_gray)
    else:
        text_boxes = merge_polygons_into_lines(text_boxes, image_gray, max_line_gap=20, min_x_overlap_ratio=0.02, split_min_line_height=12, split_min_blank_height=6, debug=False)

    sorted_boxes = sorted(text_boxes, key=lambda p: cv2.boundingRect(p)[1])
    base_vis = cv2.cvtColor(image_rgb.copy(), cv2.COLOR_RGB2BGR)

    # --- BƯỚC 3: YIELD TRẠNG THÁI "BẮT ĐẦU NHẬN DIỆN" ---
    # Báo cho client biết đã phát hiện xong, chuẩn bị stream kết quả
    total_lines = len(sorted_boxes)
    yield {"status": "recognizing", "message": f"Phát hiện xong. Bắt đầu nhận diện {total_lines} dòng...", "total_lines": total_lines}

    # --- BƯỚC 4: STREAM KẾT QUẢ (Như code cũ) ---
    for idx, poly in enumerate(sorted_boxes, start=1):
        cropped_region_gray, bbox = crop_text_region(image_gray, poly)
        if cropped_region_gray.size == 0:
            continue
        
        # Hàm này cũng là 1 blocking call nhỏ (nhưng nhanh vì crop nhỏ)
        pred_text = infer_image_array(cropped_region_gray, rec_model, device, charset, blank_idx, height=infer_height)
        pred_text = re.sub(r'\s+', ' ', pred_text).strip()
        
        # ... (Tất cả code để lấy bbox_py, box_points_py, dataurl...) ...
        # (Tôi giữ nguyên logic của bạn)
        x, y, w, h = bbox
        crop_color = image_bgr[y:y + h, x:x + w] if (h > 0 and w > 0) else np.zeros((10, 10, 3), dtype=np.uint8)
        vis_copy = base_vis.copy()
        try:
            pts = np.array(poly, dtype=np.int32)
            cv2.polylines(vis_copy, [pts], True, (0, 255, 0), 2)
        except Exception:
            pass
        crop_dataurl = imencode_to_dataurl(crop_color)
        vis_dataurl = imencode_to_dataurl(vis_copy)
        try:
            bbox_py = tuple(int(x) for x in bbox)
        except Exception:
            bbox_py = (0, 0, 0, 0)
        try:
            box_points_py = []
            for p in (poly.tolist() if isinstance(poly, np.ndarray) else list(map(list, poly))):
                if isinstance(p, (list, tuple)):
                    box_points_py.append([int(x) for x in p])
                elif isinstance(p, np.ndarray):
                    box_points_py.append([int(x) for x in p.tolist()])
                else:
                    box_points_py.append(p)
        except Exception:
            box_points_py = []


        # Đây là YIELD KẾT QUẢ
        yield {
            "status": "result",  # Thêm trạng thái "result"
            "line_number": int(idx),
            "text": str(pred_text),
            "bbox": bbox_py,
            "box_points": box_points_py,
            "crop_dataurl": crop_dataurl,
            "vis_dataurl": vis_dataurl
        }

    # --- BƯỚC 5: YIELD TRẠNG THÁI "HOÀN THÀNH" ---
    yield {"status": "done", "message": "Hoàn thành.", "total_lines": total_lines}


def detect_lines_directly(image_gray):
    _, binary = cv2.threshold(image_gray, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)
    projection = np.sum(binary, axis=1)
    lines = []
    in_line = False
    start = 0
    min_height = 10
    for i, val in enumerate(projection):
        if val > 0 and not in_line:
            in_line = True
            start = i
        elif val == 0 and in_line:
            in_line = False
            end = i
            if end - start >= min_height:
                line_poly = np.array([[0, start], [image_gray.shape[1] - 1, start], [image_gray.shape[1] - 1, end], [0, end]], dtype=np.int32)
                lines.append(line_poly)
    if in_line:
        end = len(projection)
        if end - start >= min_height:
            line_poly = np.array([[0, start], [image_gray.shape[1] - 1, start], [image_gray.shape[1] - 1, end], [0, end]], dtype=np.int32)
            lines.append(line_poly)
    return lines


def split_by_newline_projection(cropped_region, min_line_height=15, min_blank_height=8, blank_threshold_ratio=0.08, debug=False):
    if len(cropped_region.shape) == 3:
        gray = cv2.cvtColor(cropped_region, cv2.COLOR_BGR2GRAY)
    else:
        gray = cropped_region
    h, w = gray.shape
    binary = cv2.adaptiveThreshold(gray, 255, cv2.ADAPTIVE_THRESH_GAUSSIAN_C, cv2.THRESH_BINARY_INV, 11, 4)
    kernel_x = cv2.getStructuringElement(cv2.MORPH_RECT, (max(1, w // 15), 1))
    binary = cv2.morphologyEx(binary, cv2.MORPH_CLOSE, kernel_x)
    kernel_y = cv2.getStructuringElement(cv2.MORPH_RECT, (1, 3))
    binary = cv2.morphologyEx(binary, cv2.MORPH_OPEN, kernel_y)
    projection = np.sum(binary, axis=1)
    if np.max(projection) > 0:
        projection_norm = projection / np.max(projection)
    else:
        projection_norm = projection
    strong_blank_threshold = blank_threshold_ratio
    weak_blank_threshold = blank_threshold_ratio * 0.5
    text_regions = []
    in_text = False
    start_idx = 0
    for i, val in enumerate(projection_norm):
        if val > strong_blank_threshold and not in_text:
            in_text = True
            start_idx = i
        elif val <= strong_blank_threshold and in_text:
            gap_size = 0
            j = i
            while j < len(projection_norm) and projection_norm[j] <= weak_blank_threshold:
                gap_size += 1
                j += 1
                if gap_size >= min_blank_height:
                    break
            if gap_size >= min_blank_height:
                in_text = False
                end_idx = i
                height = end_idx - start_idx
                if height >= min_line_height:
                    text_regions.append((start_idx, end_idx))
    if in_text and (h - start_idx) >= min_line_height:
        text_regions.append((start_idx, h))
    if not text_regions:
        return []
    merged_regions = [text_regions[0]]
    for i in range(1, len(text_regions)):
        prev_start, prev_end = merged_regions[-1]
        curr_start, curr_end = text_regions[i]
        gap = curr_start - prev_end
        if gap < min_blank_height // 2:
            merged_regions[-1] = (prev_start, curr_end)
        else:
            merged_regions.append((curr_start, curr_end))
    lines = []
    for start, end in merged_regions:
        padding = max(1, int((end - start) * 0.02))
        y_start = max(0, start - padding)
        y_end = min(h, end + padding)
        line_img = cropped_region[y_start:y_end, :]
        lines.append((line_img, y_start, y_end))
    return lines


def merge_polygons_into_lines(polygons, image_gray, max_line_gap=15, min_x_overlap_ratio=0.01, split_min_line_height=12, split_min_blank_height=6, debug=False):
    if len(polygons) <= 1:
        return [np.array(p, dtype=np.int32) for p in polygons]
    items = []
    for p in polygons:
        x, y, w, h = cv2.boundingRect(p)
        cx, cy = x + w / 2, y + h / 2
        items.append({"poly": p, "x": x, "y": y, "w": w, "h": h, "cx": cx, "cy": cy})
    items = sorted(items, key=lambda it: it["cy"])
    lines, current_group = [], [items[0]]
    for it in items[1:]:
        last = current_group[-1]
        vertical_gap = it["y"] - (last["y"] + last["h"])
        x1, x1r = last["x"], last["x"] + last["w"]
        x2, x2r = it["x"], it["x"] + it["w"]
        overlap = max(0, min(x1r, x2r) - max(x1, x2))
        min_w = min(last["w"], it["w"])
        x_overlap_ratio = overlap / (min_w + 1e-6)
        horizontal_gap = max(0, x2 - x1r, x1 - x2r)
        if (vertical_gap <= max_line_gap and (x_overlap_ratio >= min_x_overlap_ratio or horizontal_gap < last["w"] * 0.5)):
            current_group.append(it)
        else:
            lines.append(current_group)
            current_group = [it]
    if current_group:
        lines.append(current_group)
    merged_lines = []
    for group in lines:
        if not merged_lines:
            merged_lines.append(group)
            continue
        last_group = merged_lines[-1]
        last_bottom = max(g["y"] + g["h"] for g in last_group)
        current_top = min(g["y"] for g in group)
        vertical_gap = current_top - last_bottom
        avg_h = np.mean([g["h"] for g in last_group])
        if vertical_gap < avg_h * 0.3 and vertical_gap < max_line_gap:
            merged_lines[-1].extend(group)
        else:
            merged_lines.append(group)
    merged_polys = []
    for group in merged_lines:
        group = sorted(group, key=lambda g: g["x"])
        all_points = np.vstack([g["poly"] for g in group])
        x_min, y_min = int(np.min(all_points[:, 0])), int(np.min(all_points[:, 1]))
        x_max, y_max = int(np.max(all_points[:, 0])), int(np.max(all_points[:, 1]))
        merged_poly = np.array([[x_min, y_min], [x_max, y_min], [x_max, y_max], [x_min, y_max]], dtype=np.int32)
        merged_polys.append(merged_poly)
    final_polys = []
    H, W = image_gray.shape[:2]
    for mpoly in merged_polys:
        x_min, y_min = max(0, int(np.min(mpoly[:, 0]))), max(0, int(np.min(mpoly[:, 1])))
        x_max, y_max = min(W, int(np.max(mpoly[:, 0]))), min(H, int(np.max(mpoly[:, 1])))
        if y_max <= y_min or x_max <= x_min:
            continue
        crop = image_gray[y_min:y_max, x_min:x_max]
        sub_lines = split_by_newline_projection(crop, min_line_height=split_min_line_height, min_blank_height=split_min_blank_height, blank_threshold_ratio=0.05, debug=debug)
        if len(sub_lines) <= 1:
            final_polys.append(mpoly)
        else:
            for (_, sy, ey) in sub_lines:
                abs_y1 = y_min + max(0, int(sy))
                abs_y2 = y_min + min(crop.shape[0], int(ey))
                new_poly = np.array([[x_min, abs_y1], [x_max, abs_y1], [x_max, abs_y2], [x_min, abs_y2]], dtype=np.int32)
                final_polys.append(new_poly)
    return final_polys

def init_models(train_py_path=TRAIN_PY_PATH, ckpt_path=CHECKPOINT_PATH, craft_weights_path=CRAFT_WEIGHTS):
    """
    Initialize and return: craft_model, rec_model, device, charset, blank_idx
    This is safe to call once at app startup.
    """
    global train_mod
    train_mod = import_train_module(train_py_path)
    CRNN = getattr(train_mod, 'CRNN', None)
    charset_path = getattr(train_mod, 'CHARSET_PATH', None) or CHARSET_PATH
    if not os.path.exists(charset_path):
        charset_path = CHARSET_PATH
    charset = load_charset(charset_path)
    blank_idx = len(charset)
    num_classes = len(charset) + 1
    device = torch.device('cuda' if torch.cuda.is_available() else 'cpu')

    if CRNN is None:
        raise RuntimeError("CRNN not found in train file")

    rec_model = CRNN(num_classes)
    rec_model = load_ckpt_into_model(rec_model, ckpt_path, device)

    craft_model = CRAFT()
    craft_model = load_ckpt_into_model(craft_model, craft_weights_path, device)

    return craft_model, rec_model, device, charset, blank_idx

