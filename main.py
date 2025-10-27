import uvicorn
import cv2
import numpy as np
import json
from fastapi import FastAPI, UploadFile, File
from fastapi.responses import StreamingResponse
from contextlib import asynccontextmanager
import asyncio
import sys # <--- THÊM ĐỂ GỠ LỖI
import traceback # <--- THÊM ĐỂ GỠ LỖI

# Import các hàm từ file của bạn
from text_detection import init_models, process_image_stream
from fastapi.middleware.cors import CORSMiddleware
from starlette.concurrency import iterate_in_threadpool 

# --- Biến toàn cục để giữ model ---
models = {}

# --- Dùng @asynccontextmanager để tải model khi khởi động ---
@asynccontextmanager
async def lifespan(app: FastAPI):
    # Tải model 1 lần duy nhất
    print("Loading AI models...")
    craft_model, rec_model, device, charset, blank_idx = init_models()
    models["craft_model"] = craft_model
    models["rec_model"] = rec_model
    models["device"] = device
    models["charset"] = charset
    models["blank_idx"] = blank_idx
    print("Models loaded.")
    yield
    # (Dọn dẹp model nếu cần khi tắt app)
    models.clear()
    print("Models cleared.")

# --- Khởi tạo FastAPI app với lifespan ---
app = FastAPI(lifespan=lifespan)
origins = [
    "http://localhost:3000",  # Cho phép Vue/Nuxt dev server của bạn
    "http://localhost",       # Thêm cả localhost
    "http://localhost:5123", # Cho phép C# gọi
]

app.add_middleware(
    CORSMiddleware,
    allow_origins=origins,       # Cho phép các origin trong list
    allow_credentials=True,
    allow_methods=["*"],       # Cho phép tất cả các method (POST, GET, v.v.)
    allow_headers=["*"],       # Cho phép tất cả các header
)

# --- Generator function để làm cầu nối ---
# Hàm này sẽ gọi hàm 'process_image_stream' của bạn
async def stream_generator(image_bgr):
    print("[DEBUG] stream_generator: Đã bắt đầu.") # GỠ LỖI
    try:
        # Lấy model đã tải
        craft = models["craft_model"]
        rec = models["rec_model"]
        dev = models["device"]
        charset = models["charset"]
        blank_idx = models["blank_idx"]

        # 2. Tạo một hàm (callable) để threadpool gọi
        #    Hàm này sẽ chạy trong một thread riêng
        def sync_gen_callable():
            print("[DEBUG] sync_gen_callable: Thread BẮT ĐẦU CHẠY.") # GỠ LỖI
            try:
                # process_image_stream là một sync generator, 
                # nó sẽ chạy và yield bên trong thread đó
                yield from process_image_stream(
                    image_bgr, craft, rec, dev, charset, blank_idx
                )
                print("[DEBUG] sync_gen_callable: Thread KẾT THÚC (chạy xong generator).") # GỠ LỖI
            except Exception as e_thread:
                # BẮT LỖI BÊN TRONG THREAD
                print(f"[DEBUG] !!! LỖI BÊN TRONG THREAD: {e_thread}", file=sys.stderr)
                traceback.print_exc(file=sys.stderr)
                # Yield một thông báo lỗi để FE có thể nhận được
                yield {"status": "error", "message": f"Lỗi thread: {e_thread}"}


        print("[DEBUG] stream_generator: Chuẩn bị gọi iterate_in_threadpool.") # GỠ LỖI
        
        # 3. Dùng iterate_in_threadpool để biến sync generator...
        #    SỬA LỖI: Thêm dấu () để *gọi* hàm và lấy generator
        async for result_dict in iterate_in_threadpool(sync_gen_callable()):
            
            print(f"[DEBUG] stream_generator: Đã nhận 1 item từ thread: {result_dict.get('status') or result_dict.get('line_number')}") # GỠ LỖI
            
            # Định dạng lại thành chuỗi JSON chuẩn cho SSE
            json_data = json.dumps(result_dict, ensure_ascii=False)
            yield f"data: {json_data}\n\n"
            
            # (Để 0.5s thôi cho dễ test nhé)
            # await asyncio.sleep(2) 
        
        print("[DEBUG] stream_generator: Đã KẾT THÚC vòng lặp async for.") # GỠ LỖI

    except Exception as e_main:
        # BẮT LỖI BÊN NGOÀI (LUỒNG CHÍNH)
        print(f"[DEBUG] !!! LỖI BÊN NGOÀI THREAD: {e_main}", file=sys.stderr)
        traceback.print_exc(file=sys.stderr)
        error_msg = {"status": "error", "message": f"Lỗi main: {e_main}"}
        yield f"data: {json.dumps(error_msg)}\n\n"
    
    print("[DEBUG] stream_generator: Kết thúc hàm.") # GỠ LỖI

# --- Endpoint chính cho C# gọi vào ---
@app.post("/ocr-stream")
async def ocr_stream_endpoint(file: UploadFile = File(...)):
    
    print(f"[DEBUG] ocr_stream_endpoint: Đã nhận request cho file: {file.filename}") # GỠ LỖI
    
    # Đọc file ảnh từ request
    contents = await file.read()
    nparr = np.frombuffer(contents, np.uint8)
    image_bgr = cv2.imdecode(nparr, cv2.IMREAD_COLOR)

    if image_bgr is None:
        print("[DEBUG] ocr_stream_endpoint: Lỗi decode ảnh.") # GỠ LỖI
        return {"error": "Invalid image file"}

    # Trả về một StreamingResponse, 
    # nó sẽ gọi generator của chúng ta
    print("[DEBUG] ocr_stream_endpoint: Tạo StreamingResponse và trả về.") # GỠ LỖI
    return StreamingResponse(
        stream_generator(image_bgr), 
        media_type="text/event-stream" # Rất quan trọng!
    )

# --- Chạy server ---
if __name__ == "__main__":
    # Chạy ở cổng 8000 (khác cổng với C#)
    print("Bắt đầu chạy server Uvicorn trên port 8000...")
    uvicorn.run(app, host="0.0.0.0", port=8000)

