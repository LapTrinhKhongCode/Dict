import uvicorn
import cv2
import numpy as np
import json
from fastapi import FastAPI, UploadFile, File, HTTPException
from fastapi.responses import StreamingResponse
from contextlib import asynccontextmanager
import asyncio
import sys 
import traceback
import pickle
from typing import List, Optional
from pydantic import BaseModel
import torch
import torch.nn as nn

# Import các hàm từ file của bạn
from text_detection import init_models, process_image_stream
from fastapi.middleware.cors import CORSMiddleware
from starlette.concurrency import iterate_in_threadpool 

# === IMPORT MỚI TỪ TESTTRANS.PY ===
import re
import pykakasi
import unicodedata
from transformers import MBartForConditionalGeneration, MBart50Tokenizer
from peft import PeftModel
import time # Đã có, nhưng để đây cho rõ
# === KẾT THÚC IMPORT MỚI ===


# =====================================================================
# (1) DEFINITION MODEL VIẾT TAY (Giữ nguyên)
# =====================================================================
class ConvBlock(nn.Module):
# ... (Nội dung lớp ConvBlock giữ nguyên) ...
# ... (Giả sử nội dung bên trong không đổi) ...
    def __init__(self, in_channels, out_channels, use_residual=False):
        super(ConvBlock, self).__init__()
        self.use_residual = use_residual and (in_channels == out_channels)
        self.conv = nn.Sequential(
            nn.Conv2d(in_channels, out_channels, kernel_size=3, padding=1, bias=False),
            nn.BatchNorm2d(out_channels),
            nn.ReLU(inplace=True),

            nn.Conv2d(out_channels, out_channels, kernel_size=3, padding=1, bias=False),
            nn.BatchNorm2d(out_channels),
        )
        self.relu = nn.ReLU(inplace=True)

    def forward(self, x):
        out = self.conv(x)
        if self.use_residual:
            out += x
        return self.relu(out)

class ComplexCNN(nn.Module):
# ... (Nội dung lớp ComplexCNN giữ nguyên) ...
# ... (Giả sử nội dung bên trong không đổi) ...
    def __init__(self, num_classes):
        super(ComplexCNN, self).__init__()
        self.stem = nn.Sequential(
            nn.Conv2d(1, 32, kernel_size=3, stride=1, padding=1),
            nn.BatchNorm2d(32),
            nn.ReLU(inplace=True)
        )
        self.layer1 = nn.Sequential(
            ConvBlock(32, 64, use_residual=True),
            nn.MaxPool2d(2)
        )
        self.layer2 = nn.Sequential(
            ConvBlock(64, 128, use_residual=True),
            nn.MaxPool2d(2)
        )
        self.layer3 = nn.Sequential(
            ConvBlock(128, 256, use_residual=True),
            nn.MaxPool2d(2)
        )
        self.classifier = nn.Sequential(
            nn.AdaptiveAvgPool2d((1, 1)),
            nn.Flatten(),
            nn.Dropout(0.4),
            nn.Linear(256, num_classes)
        )
    def forward(self, x):
        x = self.stem(x)
        x = self.layer1(x)
        x = self.layer2(x)
        x = self.layer3(x)
        x = self.classifier(x)
        return x

# =====================================================================
# (2) LỚP TRANSLATOR MỚI TỪ TESTTRANS.PY
# =====================================================================

class FastJapaneseVietnameseTranslator:
    # --- Toàn bộ nội dung lớp FastJapaneseVietnameseTranslator từ testtrans_fixed.py ---
    # --- được sao chép vào đây ---
    def __init__(self, model_path):
        """
        model_path: đường dẫn đến thư mục chứa adapter LoRA (ví dụ: G:\\GPT\\OCR\\OCR\\best_model)
        """
        BASE_MODEL = "facebook/mbart-large-50-many-to-many-mmt"
        print("⏳ [Translator] Đang tải mô hình gốc:", BASE_MODEL)
        base_model = MBartForConditionalGeneration.from_pretrained(
            BASE_MODEL,
            torch_dtype=torch.float16 if torch.cuda.is_available() else torch.float32,
            device_map="auto" if torch.cuda.is_available() else None,
            low_cpu_mem_usage=True
        )
        print("⏳ [Translator] Đang tải adapter LoRA từ:", model_path)
        self.model = PeftModel.from_pretrained(base_model, model_path)
        print(f"⏳ [Translator] Đang tải tokenizer từ đường dẫn cục bộ: {model_path}")
        self.tokenizer = MBart50Tokenizer.from_pretrained(model_path)
        self.device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        if torch.cuda.is_available():
            print(f"✅ [Translator] Đang sử dụng GPU: {torch.cuda.get_device_name()}")
        else:
            print("⚠️ [Translator] Đang sử dụng CPU - tốc độ sẽ chậm hơn")
        self.lang_codes = {"ja": "ja_XX", "vi": "vi_VN"}
        self.kks = pykakasi.kakasi()
        self.kks.setMode("K", "a")
        self.kks.setMode("H", "a")
        self.kks.setMode("J", "a")
        self.conv = self.kks.getConverter()
        self.katakana_pattern = re.compile(r'[\u30A0-\u30FF]+')
        self.hiragana_pattern = re.compile(r'[\u3040-\u309F]+')
        self.kanji_pattern = re.compile(r'[\u4E00-\u9FFF]+')
        self.vietnamese_pattern = re.compile(
            r'[áàảãạăắằẳẵặâấầẩẫậéèẻẽẹêếềểễệíìỉĩịóòỏõọôốồổỗộơớờởỡợúùủũụưứừửữựýỳỷỹỵđ]'
        )
        self._warmup_model()

    def detect_language(self, text: str) -> str:
        if self.vietnamese_pattern.search(text):
            return "vi"
        elif (self.hiragana_pattern.search(text) or
              self.katakana_pattern.search(text) or
              self.kanji_pattern.search(text)):
            return "ja"
        else:
            return "ja"

    def contains_katakana(self, text):
        return bool(self.katakana_pattern.search(text))

    def is_mostly_katakana(self, text):
        if not text:
            return False
        katakana_count = sum(1 for char in text if '\u30A0' <= char <= '\u30FF')
        if len(text) == 0:
            return False
        return (katakana_count / len(text)) > 0.7

    def convert_katakana_to_romaji(self, text):
        try:
            result = self.conv.do(text)
            return result
        except Exception as e:
            print(f"❌ [Translator] Lỗi chuyển đổi katakana: {e}")
            return text

    def _warmup_model(self):
        print("🔥 [Translator] Đang khởi động (warmup) model...")
        try:
            test_texts = ["こんにちは", "xin chào"]
            for text in test_texts:
                lang = self.detect_language(text)
                print(f"  [Warmup] Test: '{text}' → Ngôn ngữ: {lang}")
                if lang == "ja":
                    result = self.translate_ja_to_vi(text)
                    print(f"    Dịch: '{result}'")
                else:
                    result = self.translate_text(text, "vi", "ja")
                    print(f"    Dịch: '{result}'")
            print("✅ [Translator] Model đã sẵn sàng!")
        except Exception as e:
            print(f"❌ [Translator] Lỗi khi warmup: {e}")
            import traceback
            traceback.print_exc()

    def translate_text(self, text: str, src_lang: str, tgt_lang: str) -> str:
        src_lang_code = self.lang_codes.get(src_lang, "ja_XX")
        tgt_lang_code = self.lang_codes.get(tgt_lang, "vi_VN")
        self.tokenizer.src_lang = src_lang_code
        inputs = self.tokenizer(
            text,
            return_tensors="pt",
            truncation=True,
            padding=True,
            max_length=256
        ).to(self.device)
        with torch.no_grad():
            generated_tokens = self.model.generate(
                **inputs,
                forced_bos_token_id=self.tokenizer.lang_code_to_id[tgt_lang_code],
                max_length=256,
                num_beams=5,
                early_stopping=True,
                repetition_penalty=1.2
            )
        result = self.tokenizer.decode(generated_tokens[0], skip_special_tokens=True)
        return result

    def smart_translate(self, text: str, tgt_lang: str = None) -> str:
        detected_lang = self.detect_language(text)
        if detected_lang == "ja":
            return self.translate_ja_to_vi(text)
        else:
            return self.translate_text(text, "vi", "ja")

    def translate_ja_to_vi(self, text: str) -> str:
        if len(text.split()) == 1 and self.is_mostly_katakana(text):
            direct_translation = self.translate_text(text, "ja", "vi")
            if (direct_translation.isascii() and
                    not any(c in direct_translation for c in
                            'áàảãạăắằẳẵặâấầẩẫậéèẻẽẹêếềểễệíìỉĩịóòỏõọôốồổỗộơớờởỡợúùủũụưứừửữựýỳỷỹỵđ')):
                katakana_dict = {
                    "ポイント": "điểm",
                    "コンピュータ": "máy tính",
                    "インターネット": "internet",
                    "カメラ": "máy ảnh",
                    "テレビ": "TV",
                    "スマートフォン": "điện thoại thông minh",
                }
                if text in katakana_dict:
                    return katakana_dict[text]
            return direct_translation
        else:
            return self.translate_text(text, "ja", "vi")

# =====================================================================
# (3) PYDANTIC MODELS (GỘP CẢ HAI)
# =====================================================================

# --- Model cho Chữ Viết Tay ---
class PredictionRequest(BaseModel):
    matrix: List[List[int]]

class PredictionResponse(BaseModel):
    top_categories: List[str]
    confidence_scores: Optional[List[float]] = None

# --- Model MỚI cho Dịch Thuật ---
class TranslationRequest(BaseModel):
    """Dữ liệu đầu vào mà API dịch mong đợi"""
    text: str
    tgt_lang: Optional[str] = None


class TranslationResponse(BaseModel):
    """Dữ liệu đầu ra mà API dịch sẽ trả về"""
    original_text: str
    translated_text: str
    detected_lang: str
    processing_time_ms: float

# =====================================================================
# (4) BIẾN TOÀN CỤC VÀ LIFESPAN (ĐÃ GỘP)
# =====================================================================

# --- Biến toàn cục để giữ TẤT CẢ model ---
models = {}

# --- HỢP NHẤT LOGIC STARTUP VÀO LIFESPAN ---
@asynccontextmanager
async def lifespan(app: FastAPI):
    print("Loading AI models...")
    
    # --- Tải Model OCR (Cũ) ---
    try:
        craft_model, rec_model, device, charset, blank_idx = init_models()
        models["craft_model"] = craft_model
        models["rec_model"] = rec_model
        models["device"] = device
        models["charset"] = charset
        models["blank_idx"] = blank_idx
        print("✅ OCR Models (CRAFT, CRNN) loaded.")
    except Exception as e:
        print(f"❌ LỖI khi tải model OCR: {e}", file=sys.stderr)
        traceback.print_exc(file=sys.stderr)

    # --- Tải Model Chữ Viết Tay (Cũ) ---
    try:
        # Tận dụng device đã tải từ OCR nếu có
        hw_device = models.get("device", torch.device("cuda" if torch.cuda.is_available() else "cpu"))
        print(f"Handwriting model will use device: {hw_device}")
        hw_model = ComplexCNN(3001) 
        checkpoint = torch.load('best_weight.pth', map_location=hw_device) 
        checkpoint = {k.replace('module.', ''): v for k, v in checkpoint.items()}
        hw_model.load_state_dict(checkpoint)
        hw_model.to(hw_device) 
        hw_model.eval()
        
        with open('categories.pkl', 'rb') as f:
            categories = pickle.load(f)
        
        models["hw_model"] = hw_model
        models["hw_categories"] = categories
        models["hw_device"] = hw_device 
        print("✅ Handwriting CNN model loaded.")
    
    except Exception as e:
        print(f"❌ LỖI khi tải model chữ viết tay: {e}", file=sys.stderr)
        traceback.print_exc(file=sys.stderr)

    # --- Tải Model Dịch Thuật (MỚI) ---
    try:
        translator_model_path = r"G:\GPT\OCR\OCR\best_model"
        print("=" * 50)
        print(f"⏳ [API Server] Đang tải mô hình DỊCH THUẬT từ: {translator_model_path}")
        
        translator_instance = FastJapaneseVietnameseTranslator(translator_model_path)
        
        # Lưu vào biến models toàn cục
        models["translator"] = translator_instance
        print("✅ [API Server] Mô hình DỊCH THUẬT đã tải xong!")
        print("=" * 50)
    except Exception as e:
        print(f"❌ LỖI NGHIÊM TRỌNG KHI TẢI MÔ HÌNH DỊCH THUẬT: {e}")
        import traceback
        traceback.print_exc()
        models["translator"] = None # Đánh dấu là tải lỗi

    yield
    
    models.clear()
    print("Models cleared.")

# =====================================================================
# (5) KHỞI TẠO APP VÀ CORS (Giữ nguyên)
# =====================================================================

# --- Khởi tạo FastAPI app với lifespan ---
app = FastAPI(lifespan=lifespan)

# --- CORS (Giữ nguyên từ main.py) ---
origins = [
    "http://localhost:3000",
    "http://localhost",
    "http://localhost:5123", 
    "https://localhost:7084", 
]
app.add_middleware(
    CORSMiddleware,
    allow_origins=origins,
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# =====================================================================
# (6) ENDPOINTS (GỘP CẢ HAI)
# =====================================================================

# --- Endpoint Gốc (MỚI) ---
@app.get("/", summary="Endpoint gốc")
async def root():
    return {
        "message": "Chào mừng bạn đến với API gộp (OCR, Handwriting, Translator)!",
        "docs_url": "/docs",
        "health_check": "/health"
    }

# --- Endpoint Health Check (CẬP NHẬT) ---
@app.get("/health")
async def health_check():
    # Kiểm tra xem các model đã được tải thành công chưa
    loaded_models = list(models.keys())
    status_report = {
        "ocr_status": "Sẵn sàng" if "craft_model" in loaded_models else "Lỗi",
        "handwriting_status": "Sẵn sàng" if "hw_model" in loaded_models else "Lỗi",
        "translator_status": "Sẵn sàng" if models.get("translator") is not None else "Lỗi",
        "models_loaded": loaded_models
    }
    return status_report

# --- Endpoint OCR STREAMING (Giữ nguyên) ---
async def stream_generator(image_bgr):
    # (Tạm xóa bớt [DEBUG] print cho gọn)
    try:
        craft = models["craft_model"]
        rec = models["rec_model"]
        dev = models["device"]
        charset = models["charset"]
        blank_idx = models["blank_idx"]

        def sync_gen_callable():
            try:
                yield from process_image_stream(
                    image_bgr, craft, rec, dev, charset, blank_idx
                )
            except Exception as e_thread:
                print(f"[DEBUG] !!! LỖI BÊN TRONG THREAD: {e_thread}", file=sys.stderr)
                traceback.print_exc(file=sys.stderr)
                yield {"status": "error", "message": f"Lỗi thread: {e_thread}"}
        
        async for result_dict in iterate_in_threadpool(sync_gen_callable()):
            json_data = json.dumps(result_dict, ensure_ascii=False)
            yield f"data: {json_data}\n\n"
            # (Đã xóa sleep)

    except Exception as e_main:
        print(f"[DEBUG] !!! LỖI BÊN NGOÀI THREAD: {e_main}", file=sys.stderr)
        traceback.print_exc(file=sys.stderr)
        error_msg = {"status": "error", "message": f"Lỗi main: {e_main}"}
        yield f"data: {json.dumps(error_msg)}\n\n"

@app.post("/ocr-stream")
async def ocr_stream_endpoint(file: UploadFile = File(...)):
    contents = await file.read()
    nparr = np.frombuffer(contents, np.uint8)
    image_bgr = cv2.imdecode(nparr, cv2.IMREAD_COLOR)
    if image_bgr is None:
        return {"error": "Invalid image file"}
    return StreamingResponse(
        stream_generator(image_bgr), 
        media_type="text/event-stream" 
    )

# --- Endpoint MỚI CHO CHỮ VIẾT TAY (Giữ nguyên) ---
@app.post("/predict", response_model=PredictionResponse)
async def predict(request: PredictionRequest):
    print("[DEBUG] predict: Đã nhận request.")
    try:
        model = models.get("hw_model")
        categories = models.get("hw_categories")
        device = models.get("hw_device")

        if not model or not categories or not device:
            raise HTTPException(status_code=503, detail="Model chữ viết tay chưa sẵn sàng.")
        
        if len(request.matrix) != 64 or any(len(row) != 64 for row in request.matrix):
            raise HTTPException(status_code=400, detail="Input matrix must be 64x64")
        
        if any(any(pixel not in [0, 1] for pixel in row) for row in request.matrix):
            raise HTTPException(status_code=400, detail="Matrix must contain only 0s and 1s")
        
        # Xử lý tensor
        input_data = np.array(request.matrix)
        input_data = np.expand_dims(input_data, axis=(0, -1)) # Shape: (1, 64, 64, 1)
        
        input_tensor = torch.tensor(input_data, dtype=torch.float32)
        input_tensor = input_tensor.to(device) 
        
        # --- SỬA LỖI Ở ĐÂY ---
        # PyTorch cần (Batch, Channels, Height, Width)
        # Chuyển (1, 64, 64, 1) -> (1, 1, 64, 64)
        input_tensor = input_tensor.permute(0, 3, 1, 2) 
        # --- HẾT SỬA LỖI ---
        
        with torch.no_grad():
            predicted_output = model(input_tensor) # Bây giờ sẽ khớp
        
        # Lấy kết quả
        predictions = predicted_output.cpu().numpy()[0]
        top_10_indices = np.argsort(predictions)[-10:][::-1]
        top_10_categories = [categories[i] for i in top_10_indices]
        
        softmax = torch.nn.Softmax(dim=1)
        probabilities = softmax(predicted_output).cpu().numpy()[0]
        confidence_scores = [float(probabilities[i]) for i in top_10_indices]
        
        print("[DEBUG] predict: Trả về kết quả thành công.")
        return {
            "top_categories": top_10_categories,
            "confidence_scores": confidence_scores
        }
    
    except Exception as e:
        print(f"[DEBUG] !!! LỖI /predict: {e}", file=sys.stderr)
        traceback.print_exc(file=sys.stderr)
        # Trả về lỗi chi tiết cho Vue (như bạn thấy)
        raise HTTPException(status_code=500, detail=f"Prediction error: {str(e)}")

# --- ENDPOINT MỚI CHO DỊCH THUẬT (ĐÃ CẬP NHẬT) ---

@app.post("/translate", response_model=TranslationResponse)
async def translate_text_endpoint(request: TranslationRequest):
    """
    Endpoint chính để dịch văn bản.
    """
    # Lấy translator từ biến models toàn cục
    translator_instance = models.get("translator")

    if translator_instance is None:
        raise HTTPException(status_code=503, detail="Lỗi: Mô hình Dịch Thuật chưa được tải.")

    if not request.text.strip():
        raise HTTPException(status_code=400, detail="Lỗi: 'text' không được để trống.")

    print(f"\n[Request] Nhận yêu cầu dịch cho: '{request.text[:50]}...'")
    start_time = time.time()

    try:
        detected_lang = translator_instance.detect_language(request.text)
        # Hàm smart_translate giờ sẽ tự động đảo chiều
        translated_text = translator_instance.smart_translate(request.text, request.tgt_lang)
        end_time = time.time()
        processing_time_ms = (end_time - start_time) * 1000

        print(f"[Response] Dịch xong trong {processing_time_ms:.2f}ms: '{translated_text[:50]}...'")

        return TranslationResponse(
            original_text=request.text,
            translated_text=translated_text,
            detected_lang=detected_lang,
            processing_time_ms=processing_time_ms
        )
    except Exception as e:
        print(f"❌ [API Server] Lỗi khi đang dịch: {e}")
        traceback.print_exc()
        raise HTTPException(status_code=500, detail=f"Lỗi máy chủ nội bộ: {str(e)}")


# =====================================================================
# (7) CHẠY SERVER (Giữ nguyên)
# =====================================================================

if __name__ == "__main__":
    print("Bắt đầu chạy server Uvicorn gộp trên port 8000...")
    uvicorn.run("main:app", host="0.0.0.0", port=8000, reload=True)

    