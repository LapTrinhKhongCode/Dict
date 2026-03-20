# text_detection_updated.py
import os
import importlib.util
import sys
import cv2
import numpy as np
import torch
import re
import base64
import json

# --- CẤU HÌNH (Sử dụng đường dẫn mới của bạn) ---
TRAIN_PY_PATH = r"G:\GPT\OCR\SourceCode.py"
CHECKPOINT_PATH = r"G:\GPT\OCR\checkpoints_seq\best_seq.pt"
CRAFT_WEIGHTS = r"G:\GPT\CRAFT-pytorch\craft_mlt_25k.pth"
CRAFT_PATH = r"G:\GPT\CRAFT-pytorch"
CHARSET_PATH = os.path.join(os.path.dirname(TRAIN_PY_PATH), "charsett.txt")
INFER_HEIGHT = 32

# Cấu hình từ file 'trimmed_infer.py'
TEXT_THRESHOLD = 0.5
LINK_THRESHOLD = 0.4
LOW_TEXT = 0.22
MSER_DELTA = 4
MSER_MIN_AREA = 7
MSER_MAX_AREA = 1000
DEBUG_INFER = os.environ.get("DEBUG_INFER", "0") == "1"
# ---------------------------------------------------------------

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

# --- checkpoint loader (PHIÊN BẢN CHUẨN TỪ trimmed_infer.py) ---
def load_ckpt_into_model(model, ckpt_path, device):
    if DEBUG_INFER: print("DEBUG: Loading checkpoint:", ckpt_path)
    ck = None
    try:
        ck = torch.load(ckpt_path, map_location=device, weights_only=True)
        if DEBUG_INFER: print("DEBUG: Loaded checkpoint with weights_only=True")
    except TypeError:
        try:
            ck = torch.load(ckpt_path, map_location=device)
            if DEBUG_INFER: print("DEBUG: Loaded checkpoint without weights_only arg")
        except Exception as e:
            if DEBUG_INFER: print("DEBUG: torch.load failed (no weights_only):", e)
            raise
    except Exception as e:
        if DEBUG_INFER: print("DEBUG: weights_only=True failed:", e)
        if DEBUG_INFER: print("DEBUG: Retrying with weights_only=False (UNSAFE - only if you trust file).")
        try:
            ck = torch.load(ckpt_path, map_location=device, weights_only=False)
            if DEBUG_INFER: print("DEBUG: Loaded checkpoint with weights_only=False")
        except Exception as e2:
            if DEBUG_INFER: print("DEBUG: Failed to load checkpoint even with weights_only=False:", e2)
            raise

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
    except Exception as e:
        if DEBUG_INFER: print("DEBUG: model.load_state_dict warning:", e)

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
    except Exception as e:
        if DEBUG_INFER: print("DEBUG: save_heatmap failed:", e)


def debug_print_detres(det_res):
    try:
        if DEBUG_INFER: print("DEBUG: getDetBoxes return type:", type(det_res))
        if isinstance(det_res, (list, tuple)):
            if DEBUG_INFER: print(" DEBUG: length:", len(det_res))
            for i, item in enumerate(det_res):
                t = type(item)
                try:
                    l = len(item)
                except Exception:
                    l = "NA"
                if DEBUG_INFER: print(f"  item[{i}] type={t} len={l}")
    except Exception as e:
        if DEBUG_INFER: print("DEBUG: debug_print_detres failed:", e)

# --- get_text_boxes (PHIÊN BẢN CHUẨN TỪ trimmed_infer.py) ---
def get_text_boxes(image, craft_model, device,
                   text_threshold=TEXT_THRESHOLD, link_threshold=LINK_THRESHOLD, low_text=LOW_TEXT,
                   save_debug_dir=None):
    if DEBUG_INFER and save_debug_dir:
        os.makedirs(save_debug_dir, exist_ok=True)

    image_resized, target_ratio, size_heatmap = imgproc.resize_aspect_ratio(
        image, 1280, interpolation=cv2.INTER_LINEAR, mag_ratio=1.5
    )
    ratio_h = ratio_w = 1 / target_ratio

    x = imgproc.normalizeMeanVariance(image_resized)
    x = torch.from_numpy(x).permute(2, 0, 1).unsqueeze(0)
    x = x.to(device)

    with torch.no_grad():
        y, _ = craft_model(x)

    score_text = y[0, :, :, 0].cpu().data.numpy()
    score_link = y[0, :, :, 1].cpu().data.numpy()

    if DEBUG_INFER and save_debug_dir:
        try:
            save_heatmap(image_resized, score_text, os.path.join(save_debug_dir, "debug_score_text.png"))
            save_heatmap(image_resized, score_link, os.path.join(save_debug_dir, "debug_score_link.png"))
            np.save(os.path.join(save_debug_dir, "debug_score_text.npy"), score_text)
            np.save(os.path.join(save_debug_dir, "debug_score_link.npy"), score_link)
            print(f"DEBUG: Saved debug heatmaps to {save_debug_dir}")
            print("DEBUG: score_text min/max:", float(score_text.min()), float(score_text.max()))
            print("DEBUG: score_link min/max:", float(score_link.min()), float(score_link.max()))
        except Exception as e:
            print("DEBUG: Failed to save heatmaps:", e)

    try:
        det_res = craft_utils.getDetBoxes(score_text, score_link, text_threshold, link_threshold, low_text)
    except Exception as e:
        if DEBUG_INFER: print("DEBUG: getDetBoxes threw exception:", e)
        det_res = None

    if DEBUG_INFER: debug_print_detres(det_res)

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
        if DEBUG_INFER: print("DEBUG: No valid polygons after cleaning. Trying MSER fallback.")
        gray_resized = cv2.cvtColor(image_resized, cv2.COLOR_RGB2GRAY)
        mser_polys = mser_fallback_regions(gray_resized)
        if DEBUG_INFER: print("DEBUG: MSER produced", len(mser_polys), "regions")
        scaled = []
        for p in mser_polys:
            arr = np.array(p, dtype=np.float32)
            scaled.append((arr * np.array([ratio_w, ratio_h], dtype=np.float32)).astype(np.int32))
        return scaled

    try:
        boxes_out = craft_utils.adjustResultCoordinates([p.tolist() for p in flat_polys], ratio_w, ratio_h)
    except Exception as e:
        if DEBUG_INFER: print("DEBUG: adjustResultCoordinates threw exception:", e)
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
        except Exception as e:
            if DEBUG_INFER: print("DEBUG: Failed converting poly to array:", e)

    if DEBUG_INFER: print(f"DEBUG: Detected {len(final_polys)} polygons after postprocessing.")
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
    x = max(0, x); y = max(0, y)
    x2 = min(w_img, x + w); y2 = min(h_img, y + h)
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

# --- (THÊM MỚI) CÁC HÀM HELPER TỪ FILE CHUẨN ---

KEEP_CHARS = r"[。、,.!?]"

def clean_text(s: str, remove_chars=r"[^ぁ-んァ-ン一-龯0-9A-Za-z" + KEEP_CHARS + "]", collapse_spaces=True):
    out = re.sub(remove_chars, "", s)
    if collapse_spaces:
        out = re.sub(r"\s+", " ", out).strip()
    return out

def is_kana_string(s, min_kana_ratio=0.7):
    if not s:
        return False
    kana_count = 0
    total = 0
    for ch in s:
        total += 1
        code = ord(ch)
        if (0x3040 <= code <= 0x309F) or (0x30A0 <= code <= 0x30FF) or (0x31F0 <= code <= 0x31FF):
            kana_count += 1
    return (kana_count / total) >= min_kana_ratio

def recognize_small_region(crop_gray, rec_model, device, charset, blank_idx, height_for_small=20):
    if crop_gray is None or crop_gray.size == 0:
        return ''
    if crop_gray.ndim == 3:
        crop_gray = cv2.cvtColor(crop_gray, cv2.COLOR_BGR2GRAY)
    try:
        txt = infer_image_array(crop_gray, rec_model, device, charset, blank_idx, height=height_for_small)
        return clean_text(txt)
    except Exception:
        return ''

def enhanced_furigana_detection(polys, image_gray, image_rgb=None,
                                rec_model=None, charset=None, blank_idx=None, device=None,
                                debug=False):
    if not polys:
        return [], []
    items = []
    for p in polys:
        p = np.array(p, dtype=np.int32)
        x, y, w, h = cv2.boundingRect(p)
        items.append({
            "poly": p, "x": x, "y": y, "w": w, "h": h,
            "center_y": y + h / 2,
            "area": w * h,
            "aspect_ratio": w / max(h, 1)
        })
    heights = np.array([it["h"] for it in items])
    areas = np.array([it["area"] for it in items])
    med_h = np.median(heights)
    furigana_candidates = []
    normal_text = []
    for i, it in enumerate(items):
        furigana_score = 0
        size_ratio = it["h"] / med_h if med_h > 0 else 1.0
        if size_ratio < 0.6:
            furigana_score += 3
        elif size_ratio < 0.8:
            furigana_score += 0
        relative_position = it["y"] / image_gray.shape[0]
        if relative_position < 0.3:
            furigana_score += 2
        if it["aspect_ratio"] > 2.0:
            furigana_score += 1
        area_ratio = it["area"] / np.median(areas) if areas.size else 1.0
        if area_ratio < 0.4:
            furigana_score += 2
        if rec_model is not None and furigana_score >= 3:
            x0, y0 = max(0, it["x"] - 2), max(0, it["y"] - 2)
            x1, y1 = it["x"] + it["w"] + 2, it["y"] + it["h"] + 2
            crop = image_gray[y0:y1, x0:x1]
            if crop.size > 0:
                txt = recognize_small_region(crop, rec_model, device, charset, blank_idx, height_for_small=max(18, it["h"]))
                if txt and is_kana_string(txt, min_kana_ratio=0.8):
                    furigana_score += 2
        if furigana_score >= 6:
            furigana_candidates.append(it)
        else:
            normal_text.append(it)
    if debug or DEBUG_INFER:
        print(f"DEBUG: Furigana detection - Total: {len(items)}, Furigana: {len(furigana_candidates)}, Normal: {len(normal_text)}")
    return [it["poly"] for it in normal_text], [it["poly"] for it in furigana_candidates]

def merge_lines_by_adjacency(polys, y_tolerance_factor=0.5, x_gap_tolerance_factor=4, debug=False):
    if len(polys) < 2:
        return polys
    items = []
    for i, p in enumerate(polys):
        p_arr = np.array(p, dtype=np.int32)
        x, y, w, h = cv2.boundingRect(p_arr)
        items.append({
            'id': i, 'poly': p_arr, 'x': x, 'y': y, 'w': w, 'h': h,
            'cx': x + w / 2, 'cy': y + h / 2
        })
    heights = [it['h'] for it in items if it['h'] > 0]
    med_h = np.median(heights) if heights else 10
    adj = [[] for _ in range(len(items))]
    for i in range(len(items)):
        for j in range(i + 1, len(items)):
            a = items[i]; b = items[j]
            is_vertically_aligned = abs(a['cy'] - b['cy']) < med_h * y_tolerance_factor
            if is_vertically_aligned:
                gap = max(0, b['x'] - (a['x'] + a['w']), a['x'] - (b['x'] + b['w']))
                max_gap = med_h * x_gap_tolerance_factor
                if gap < max_gap:
                    adj[i].append(j); adj[j].append(i)
    visited = [False] * len(items)
    groups = []
    for i in range(len(items)):
        if not visited[i]:
            component = []
            q = [i]; visited[i] = True
            while q:
                u = q.pop(0)
                component.append(items[u])
                for v in adj[u]:
                    if not visited[v]:
                        visited[v] = True; q.append(v)
            groups.append(component)
    merged_polys = []
    for group in groups:
        if not group: continue
        all_points = np.vstack([it['poly'] for it in group])
        x_min, y_min = np.min(all_points, axis=0)
        x_max, y_max = np.max(all_points, axis=0)
        merged_poly = np.array([[x_min, y_min], [x_max, y_min], [x_max, y_max], [x_min, y_max]], dtype=np.int32)
        merged_polys.append(merged_poly)
    if debug or DEBUG_INFER:
        print(f"DEBUG: Merged {len(polys)} boxes into {len(merged_polys)} final lines using adjacency.")
    return merged_polys

# --- (CẬP NHẬT) HÀM PROCESS_IMAGE VỚI LOGIC CHUẨN ---
def process_image(image_bgr, craft_model, rec_model, device, charset, blank_idx, infer_height=INFER_HEIGHT):
    image_rgb = cv2.cvtColor(image_bgr, cv2.COLOR_BGR2RGB)
    image_gray = cv2.cvtColor(image_bgr, cv2.COLOR_BGR2GRAY)
    
    # 1. Phát hiện text boxes
    text_boxes = get_text_boxes(image_rgb, craft_model, device, 
                                text_threshold=TEXT_THRESHOLD, 
                                link_threshold=LINK_THRESHOLD, 
                                low_text=LOW_TEXT)
    
    # 2. Fallback hoặc Gộp dòng (LOGIC CHUẨN)
    if text_boxes is None or len(text_boxes) == 0:
        if DEBUG_INFER: print("DEBUG: No text boxes detected by CRAFT. Using direct line detection.")
        text_boxes = detect_lines_directly(image_gray)
    else:
        if DEBUG_INFER: print("DEBUG: Using adjacency-based line merging...")
        text_boxes = merge_lines_by_adjacency(text_boxes, x_gap_tolerance_factor=0.5, debug=DEBUG_INFER)

    # 3. Phát hiện Furigana (LOGIC CHUẨN)
    normal_polys, furigana_polys = enhanced_furigana_detection(
        text_boxes, image_gray, image_rgb,
        rec_model=rec_model, charset=charset, blank_idx=blank_idx, device=device,
        debug=DEBUG_INFER
    )
    
    # 4. Mask furigana khỏi ảnh xám (LOGIC CHUẨN)
    masked_gray = image_gray.copy()
    for poly in furigana_polys:
        x, y, w, h = cv2.boundingRect(np.array(poly, dtype=np.int32))
        cv2.rectangle(masked_gray, (x, y), (x + w, y + h), 255, -1) # Tô trắng
        
    # 5. Nhận dạng văn bản chính (từ ảnh đã mask)
    results = []
    sorted_boxes = sorted(normal_polys, key=lambda p: cv2.boundingRect(p)[1])
    for idx, poly in enumerate(sorted_boxes, start=1):
        cropped_region, bbox = crop_text_region(masked_gray, poly)
        if cropped_region.size == 0:
            continue
        pred_text = infer_image_array(cropped_region, rec_model, device, charset, blank_idx, height=infer_height)
        pred_text = clean_text(re.sub(r'\s+', ' ', pred_text).strip())
        box_list = poly.tolist() if isinstance(poly, np.ndarray) else list(map(list, poly))
        results.append({"bbox": bbox, "text": pred_text, "box_points": box_list, "type": "main_text"})
        
    # 6. Nhận dạng Furigana (từ ảnh gốc)
    furigana_results = []
    for poly in furigana_polys:
        x, y, w, h = cv2.boundingRect(np.array(poly, dtype=np.int32))
        x0, y0 = max(0, x - 1), max(0, y - 1)
        x1, y1 = min(image_gray.shape[1], x + w + 1), min(image_gray.shape[0], y + h + 1)
        crop = image_gray[y0:y1, x0:x1]
        
        fur_text = recognize_small_region(crop, rec_model, device, charset, blank_idx, height_for_small=max(18, h))
        if fur_text and is_kana_string(fur_text, min_kana_ratio=0.7):
            furigana_results.append({"text": fur_text, "position": (x, y, w, h), "type": "furigana"})
            
    return {"main_text": results, "furigana": furigana_results, "image_info": {"width": image_gray.shape[1], "height": image_gray.shape[0]}}


# --- (CẬP NHẬT) HÀM PROCESS_IMAGE_STREAM VỚI LOGIC CHUẨN + GIỮ TRẠNG THÁI YIELD ---

def process_image_stream(image_bgr, craft_model, rec_model, device, charset, blank_idx, infer_height=INFER_HEIGHT):
    
    # --- BƯỚC 1: YIELD TRẠNG THÁI "BẮT ĐẦU" ---
    yield {"status": "processing", "message": "Bắt đầu xử lý ảnh..."}

    image_rgb = cv2.cvtColor(image_bgr, cv2.COLOR_BGR2RGB)
    image_gray = cv2.cvtColor(image_bgr, cv2.COLOR_BGR2GRAY)

    # --- BƯỚC 2: YIELD TRẠNG THÁI "ĐANG PHÁT HIỆN" ---
    yield {"status": "detecting", "message": "Đang phát hiện văn bản (CRAFT)..."}

    # Đây là hàm nặng (Blocking call) - SỬ DỤNG LOGIC CHUẨN
    text_boxes = get_text_boxes(image_rgb, craft_model, device, text_threshold=TEXT_THRESHOLD, link_threshold=LINK_THRESHOLD, low_text=LOW_TEXT)
    
    # SỬ DỤNG LOGIC GỘP DÒNG CHUẨN
    if text_boxes is None or len(text_boxes) == 0:
        if DEBUG_INFER: print("DEBUG: No text boxes detected by CRAFT. Using direct line detection.")
        text_boxes = detect_lines_directly(image_gray)
    else:
        if DEBUG_INFER: print("DEBUG: Using adjacency-based line merging...")
        text_boxes = merge_lines_by_adjacency(text_boxes, x_gap_tolerance_factor=0.5, debug=DEBUG_INFER)

    # SỬ DỤNG LOGIC PHÁT HIỆN FURIGANA CHUẨN
    normal_polys, furigana_polys = enhanced_furigana_detection(
        text_boxes, image_gray, image_rgb,
        rec_model=rec_model, charset=charset, blank_idx=blank_idx, device=device,
        debug=DEBUG_INFER
    )

    sorted_boxes = sorted(normal_polys, key=lambda p: cv2.boundingRect(p)[1])
    base_vis = cv2.cvtColor(image_rgb.copy(), cv2.COLOR_RGB2BGR)

    # --- BƯỚC 3: YIELD TRẠNG THÁI "BẮT ĐẦU NHẬN DIỆN" ---
    total_lines = len(sorted_boxes)
    furigana_count = len(furigana_polys)
    yield {"status": "recognizing", 
           "message": f"Phát hiện xong. Bắt đầu nhận diện {total_lines} dòng chính và {furigana_count} furigana...", 
           "total_lines": total_lines,
           "furigana_count": furigana_count}

    # SỬ DỤNG LOGIC MASKING CHUẨN
    masked_gray = image_gray.copy()
    for poly in furigana_polys:
        x, y, w, h = cv2.boundingRect(np.array(poly, dtype=np.int32))
        cv2.rectangle(masked_gray, (x, y), (x + w, y + h), 255, -1) # Tô trắng

    # --- BƯỚC 4A: STREAM KẾT QUẢ VĂN BẢN CHÍNH ---
    for idx, poly in enumerate(sorted_boxes, start=1):
        # Nhận dạng từ ảnh đã mask
        cropped_region_gray, bbox = crop_text_region(masked_gray, poly)
        if cropped_region_gray.size == 0:
            continue
        
        pred_text = infer_image_array(cropped_region_gray, rec_model, device, charset, blank_idx, height=infer_height)
        pred_text = clean_text(re.sub(r'\s+', ' ', pred_text).strip())
        
        # ... (Tất cả code để lấy bbox_py, box_points_py, dataurl...) ...
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
            box_list = poly.tolist() if isinstance(poly, np.ndarray) else list(map(list, poly))
            for p in box_list:
                if isinstance(p, (list, tuple)):
                    box_points_py.append([int(x) for x in p])
                elif isinstance(p, np.ndarray):
                    box_points_py.append([int(x) for x in p.tolist()])
                else:
                    box_points_py.append(p)
        except Exception:
            box_points_py = []

        # Đây là YIELD KẾT QUẢ (main_text)
        yield {
            "status": "result", 
            "type": "main_text", # Thêm type
            "line_number": int(idx),
            "text": str(pred_text),
            "bbox": bbox_py,
            "box_points": box_points_py,
            "crop_dataurl": crop_dataurl,
            "vis_dataurl": vis_dataurl
        }

    # --- BƯỚC 4B: STREAM KẾT QUẢ FURIGANA ---
    for idx, poly in enumerate(furigana_polys, start=1):
        x, y, w, h = cv2.boundingRect(np.array(poly, dtype=np.int32))
        
        # Nhận dạng từ ảnh gray gốc
        x0, y0 = max(0, x - 1), max(0, y - 1)
        x1, y1 = min(image_gray.shape[1], x + w + 1), min(image_gray.shape[0], y + h + 1)
        crop = image_gray[y0:y1, x0:x1]
        
        fur_text = recognize_small_region(crop, rec_model, device, charset, blank_idx, height_for_small=max(18, h))
        
        if fur_text and is_kana_string(fur_text, min_kana_ratio=0.7):
            try:
                crop_color = image_bgr[y:y + h, x:x + w] if (h > 0 and w > 0) else np.zeros((10, 10, 3), dtype=np.uint8)
                vis_copy = base_vis.copy()
                cv2.rectangle(vis_copy, (x, y), (x + w, y + h), (0, 0, 255), 2)
                crop_dataurl = imencode_to_dataurl(crop_color)
                vis_dataurl = imencode_to_dataurl(vis_copy)
                
                # Đây là YIELD KẾT QUẢ (furigana)
                yield {
                    "status": "result", 
                    "type": "furigana", # Thêm type
                    "line_number": int(idx),
                    "text": str(fur_text),
                    "bbox": (int(x), int(y), int(w), int(h)),
                    "box_points": [[int(x), int(y)], [int(x + w), int(y)], [int(x + w), int(y + h)], [int(x), int(y + h)]],
                    "crop_dataurl": crop_dataurl,
                    "vis_dataurl": vis_dataurl
                }
            except Exception:
                continue

    # --- BƯỚC 5: YIELD TRẠNG THÁI "HOÀN THÀNH" ---
    yield {"status": "done", "message": "Hoàn thành.", "total_lines": total_lines, "furigana_count": furigana_count}


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


# --- (ĐÃ XÓA) các hàm split_by_newline_projection và merge_polygons_into_lines ---


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