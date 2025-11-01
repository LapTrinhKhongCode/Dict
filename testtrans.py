from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import Optional, List
import time
import uvicorn
import torch
import re
import pykakasi
import unicodedata
from transformers import MBartForConditionalGeneration, MBart50TokenizerFast

# THÊM DÒNG NÀY để import CORSMiddleware
from fastapi.middleware.cors import CORSMiddleware


# =====================================================================
# PHẦN 1: LỚP TRANSLATOR (Không thay đổi)
# =====================================================================

class FastJapaneseVietnameseTranslator:
    def __init__(self, model_path):
        # Tải model
        print("⏳ [Translator] Đang tải mô hình từ đường dẫn:", model_path)
        self.model = MBartForConditionalGeneration.from_pretrained(
            model_path,
            torch_dtype=torch.float16 if torch.cuda.is_available() else torch.float32,
            device_map="auto" if torch.cuda.is_available() else None,
            low_cpu_mem_usage=True
        )

        self.tokenizer = MBart50TokenizerFast.from_pretrained(model_path)

        # Thiết lập device
        self.device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
        if torch.cuda.is_available():
            print(f"✅ [Translator] Đang sử dụng GPU: {torch.cuda.get_device_name()}")
        else:
            print("⚠️  [Translator] Đang sử dụng CPU - tốc độ sẽ chậm hơn")

        # Cache các thiết lập
        self.lang_codes = {"ja": "ja_XX", "vi": "vi_VN"}

        # Khởi tạo Katakana converter
        self.kks = pykakasi.kakasi()
        self.kks.setMode("K", "a")  # Katakana to romaji
        self.kks.setMode("H", "a")  # Hiragana to romaji
        self.kks.setMode("J", "a")  # Kanji to romaji
        self.conv = self.kks.getConverter()

        # Regex patterns để nhận diện
        self.katakana_pattern = re.compile(r'[\u30A0-\u30FF]+')  # Katakana
        self.hiragana_pattern = re.compile(r'[\u3040-\u309F]+')  # Hiragana
        self.kanji_pattern = re.compile(r'[\u4E00-\u9FFF]+')  # Kanji
        self.vietnamese_pattern = re.compile(
            r'[áàảãạăắằẳẵặâấầẩẫậéèẻẽẹêếềểễệíìỉĩịóòỏõọôốồổỗộơớờởỡợúùủũụưứừửữựýỳỷỹỵđ]')  # Vietnamese

        # Warmup model
        self._warmup_model()

    def detect_language(self, text: str) -> str:
        """Tự động phát hiện ngôn ngữ"""
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
        return (katakana_count / len(text)) > 0.7

    def convert_katakana_to_romaji(self, text):
        try:
            result = self.conv.do(text)
            return result
        except Exception as e:
            print(f"❌ [Translator] Lỗi chuyển đổi katakana: {e}")
            return text

    def _warmup_model(self):
        """Khởi động model"""
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

    def translate_text(self, text: str, src_lang: str, tgt_lang: str) -> str:
        """Dịch một đoạn văn bản"""
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
        """
        Dịch thông minh.
        TỰ ĐỘNG ĐẢO CHIỀU: Nhật -> Việt và Việt -> Nhật.
        Tham số 'tgt_lang' từ request sẽ bị bỏ qua để ưu tiên tự động đảo chiều.
        """
        detected_lang = self.detect_language(text)

        # === LOGIC MỚI: TỰ ĐỘNG ĐẢO CHIỀU ===
        if detected_lang == "ja":
            # Nếu phát hiện là Tiếng Nhật, dịch sang Tiếng Việt
            return self.translate_ja_to_vi(text)
        else:
            # Nếu phát hiện là Tiếng Việt (hoặc mặc định), dịch sang Tiếng Nhật
            return self.translate_text(text, "vi", "ja")
        # === KẾT THÚC LOGIC MỚI ===

    def translate_ja_to_vi(self, text: str) -> str:
        """Dịch Nhật → Việt với xử lý katakana thông minh"""
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
# PHẦN 2: API SERVER (ĐÃ CẬP NHẬT)
# =====================================================================

class TranslationRequest(BaseModel):
    """Dữ liệu đầu vào mà API mong đợi"""
    text: str
    tgt_lang: Optional[str] = None


class TranslationResponse(BaseModel):
    """Dữ liệu đầu ra mà API sẽ trả về"""
    original_text: str
    translated_text: str
    detected_lang: str
    processing_time_ms: float


# --- Khởi tạo ứng dụng FastAPI ---
app = FastAPI(
    title="API Dịch Nhật-Việt Thông Minh (1 Tệp)",
    description="Một API sử dụng mô hình MBart để dịch giữa tiếng Nhật và tiếng Việt.",
    version="1.1.0"
)

# === SỬA LỖI: THÊM CẤU HÌNH CORS TẠI ĐÂY ===
# Đây là phần quan trọng để vá lỗi "405 Method Not Allowed"
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],  # Cho phép tất cả các nguồn. Trong production, bạn nên thay bằng URL của frontend
    allow_credentials=True,
    allow_methods=["*"],  # Cho phép tất cả các phương thức (POST, GET, OPTIONS, v.v.)
    allow_headers=["*"],  # Cho phép tất cả các tiêu đề
)
# === KẾT THÚC SỬA LỖI CORS ===


# --- TẢI MÔ HÌNH (CHỈ 1 LẦN) ---
# SỬA ĐƯỜNG DẪN NÀY
model_path = r"D:\DUT\DichNhat\trainfile\mbart\best_model"
print("=" * 50)
print(f"⏳ [API Server] Đang tải mô hình từ: {model_path}")
print("Đây là quá trình MỘT LẦN. Vui lòng chờ...")

try:
    # Khởi tạo lớp translator (đã định nghĩa ở trên)
    translator = FastJapaneseVietnameseTranslator(model_path)
    print("✅ [API Server] Mô hình đã tải xong và API sẵn sàng!")
    print("=" * 50)
except Exception as e:
    print(f"❌ [API Server] LỖI NGHIÊM TRỌNG KHI TẢI MÔ HÌNH: {e}")
    translator = None


# --- Định nghĩa API Endpoint ---

@app.post("/translate", response_model=TranslationResponse)
async def translate_text_endpoint(request: TranslationRequest):
    """
    Endpoint chính để dịch văn bản.
    """
    if translator is None:
        raise HTTPException(status_code=503, detail="Lỗi: Mô hình chưa được tải.")

    if not request.text.strip():
        raise HTTPException(status_code=400, detail="Lỗi: 'text' không được để trống.")

    print(f"\n[Request] Nhận yêu cầu dịch cho: '{request.text[:50]}...'")
    start_time = time.time()

    try:
        detected_lang = translator.detect_language(request.text)
        # Hàm smart_translate giờ sẽ tự động đảo chiều
        translated_text = translator.smart_translate(request.text, request.tgt_lang)
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
        raise HTTPException(status_code=500, detail=f"Lỗi máy chủ nội bộ: {str(e)}")


@app.get("/", summary="Kiểm tra trạng thái API")
async def root():
    """Endpoint gốc để kiểm tra API."""
    status = "Sẵn sàng" if translator is not None else "Lỗi (Mô hình chưa được tải)"
    return {
        "message": "Chào mừng bạn đến với API Dịch Nhật-Việt (1 Tệp)!",
        "model_status": status,
        "docs_url": "/docs"
    }


# --- Lệnh để chạy server ---
if __name__ == "__main__":
    print("🚀 Khởi chạy Uvicorn server tại http://127.0.0.1:8000")
    print("Truy cập http://127.0.0.1:8000/docs để xem tài liệu API.")
    uvicorn.run(app, host="0.0.0.0", port=8000)

