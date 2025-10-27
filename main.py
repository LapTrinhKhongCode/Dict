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

# --- (1) DEFINITION MODEL VIẾT TAY ---
class ConvBlock(nn.Module):
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

# --- (2) PYDANTIC MODELS MỚI ---
class PredictionRequest(BaseModel):
    matrix: List[List[int]]

class PredictionResponse(BaseModel):
    top_categories: List[str]
    confidence_scores: Optional[List[float]] = None

# --- Biến toàn cục để giữ TẤT CẢ model ---
models = {}

# --- (3) HỢP NHẤT LOGIC STARTUP VÀO LIFESPAN ---
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
        print("OCR Models (CRAFT, CRNN) loaded.")
    except Exception as e:
        print(f"!!! LỖI khi tải model OCR: {e}", file=sys.stderr)
        traceback.print_exc(file=sys.stderr)

    # --- Tải Model Chữ Viết Tay (Mới) ---
    try:
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
        print("Handwriting CNN model loaded.")
    
    except Exception as e:
        print(f"!!! LỖI khi tải model chữ viết tay: {e}", file=sys.stderr)
        traceback.print_exc(file=sys.stderr)

    yield
    
    models.clear()
    print("Models cleared.")

# --- Khởi tạo FastAPI app với lifespan ---
app = FastAPI(lifespan=lifespan)

# --- CORS (Giữ nguyên) ---
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

# --- (4) ENDPOINT OCR STREAMING (Giữ nguyên) ---
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

# --- (5) ENDPOINT MỚI CHO CHỮ VIẾT TAY (ĐÃ SỬA LỖI) ---

@app.get("/health")
async def health_check():
    return {"status": "healthy", "models_loaded": list(models.keys())}

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

# --- (6) CHẠY SERVER (Giữ nguyên) ---
if __name__ == "__main__":
    print("Bắt đầu chạy server Uvicorn trên port 8000...")
    uvicorn.run("main:app", host="0.0.0.0", port=8000, reload=True)

