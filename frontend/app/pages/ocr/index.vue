<template>
  <div class="ocr-page-container">
    <!-- ============================================= -->
    <!-- PHẦN CŨ: OCR (GIỮ NGUYÊN) -->
    <!-- ============================================= -->
    <h2>OCR (Stream từ ảnh)</h2>
    <div class="controls">
      <input 
        type="file" 
        @change="handleFileChange" 
        accept="image/*" 
        :disabled="isLoading"
      >
      <button @click="startOCR" :disabled="isLoading || !file">
        {{ isLoading ? 'Đang xử lý...' : 'Bắt đầu OCR' }}
      </button>
    </div>
    <hr>
    <div v-if="statusMessage" class="status-box">
      <strong>Trạng thái OCR:</strong> {{ statusMessage }}
    </div>
    <h3>Kết quả OCR ({{ results.length }} dòng tìm thấy):</h3>
    <ul class="ocr-results">
      <li v-if="!isLoading && results.length === 0" class="ocr-item-empty">
        Chưa có kết quả...
      </li>
      <li v-for="item in results" :key="item.line_number" class="ocr-item">
        <div class="item-text">
          <strong>Dòng {{ item.line_number }}:</strong> {{ item.text }}
        </div>
        <div class="previews">
          <figure>
            <figcaption>Ảnh Crop:</figcaption>
            <img :src="item.crop_dataurl" alt="Cropped text">
          </figure>
          <figure>
            <figcaption>Ảnh Highlight:</figcaption>
            <img :src="item.vis_dataurl" alt="Highlighted text">
          </figure>
        </div> 
      </li>
    </ul>

    <!-- ============================================= -->
    <!-- PHẦN MỚI: DỰ ĐOÁN CHỮ VIẾT TAY (CANVAS) -->
    <!-- ============================================= -->
    <hr style="margin-top: 40px; border-color: blue;">
    
    <h2>Vẽ Chữ Viết Tay (Test /predict)</h2>
    <p>
      Vẽ một ký tự vào ô 256x256 bên dưới. Hệ thống sẽ tự động thu nhỏ về 64x64 và gửi đi.
    </p>
    
    <!-- (1) Bảng vẽ Canvas -->
    <canvas 
      ref="canvasRef"
      width="256"
      height="256"
      class="handwriting-canvas"
    ></canvas>
    
    <!-- (2) Nút điều khiển cho Canvas -->
    <div class="controls">
      <button @click="clearCanvas" :disabled="isPredicting">
        Xóa Bảng Vẽ
      </button>
      <button @click="handleHandwritingPrediction" :disabled="isPredicting">
        {{ isPredicting ? 'Đang dự đoán...' : 'Dự đoán Ký Tự' }}
      </button>
    </div>

    <!-- (3) Kết quả dự đoán (Giữ nguyên) -->
    <div v-if="predictionStatus" class="status-box">
      <strong>Trạng thái Predict:</strong> {{ predictionStatus }}
    </div>
    <div v-if="predictionResult">
      <strong>Kết quả dự đoán (Top 10):</strong> 
      <pre>{{ predictionResult }}</pre>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue';

// --- (BIẾN CŨ CHO OCR) ---
const file = ref(null);
const results = ref([]);
const statusMessage = ref('Chưa chọn file');
const isLoading = ref(false);

// --- (BIẾN MỚI CHO PREDICT) ---
const isPredicting = ref(false);
const predictionStatus = ref('');
const predictionResult = ref(null);

// --- (BIẾN MỚI CHO CANVAS) ---
const canvasRef = ref(null); // Ref tới element <canvas>
const ctx = ref(null);       // Context 2D của canvas
const isDrawing = ref(false);

// --- (HÀM CŨ: XỬ LÝ CHỌN FILE) ---
function handleFileChange(event) {
  file.value = event.target.files[0];
  statusMessage.value = 'Đã chọn file. Sẵn sàng bắt đầu.';
  results.value = []; 
}

// --- (HÀM CŨ: GỌI OCR STREAM) ---
async function startOCR() {
  // (Giữ nguyên toàn bộ logic hàm startOCR của bạn)
  if (!file.value) {
    alert('Vui lòng chọn 1 file ảnh!');
    return;
  }
  isLoading.value = true;
  results.value = [];
  statusMessage.value = 'Đang tải file lên...';
  const formData = new FormData();
  formData.append('file', file.value);
  const token = localStorage.getItem('jwt_token'); 
  if (!token) {
    statusMessage.value = 'Lỗi: Bạn chưa đăng nhập!';
    isLoading.value = false;
    return;
  }
  try {
    const response = await fetch('https://localhost:7084/api/Infer/stream', {
      method: 'POST',
      body: formData,
      headers: { 'Authorization': `Bearer ${token}` }
    });
    if (!response.ok) {
      throw new Error(`Lỗi server: ${response.statusText}`);
    }
    const reader = response.body.getReader();
    const decoder = new TextDecoder(); 
    let buffer = ''; 
    while (true) {
      const { done, value } = await reader.read();
      if (done) {
        console.log('Stream finished.');
        break; 
      }
      buffer += decoder.decode(value, { stream: true });
      const lines = buffer.split('\n\n');
      buffer = lines.pop(); 
      for (const line of lines) {
        if (line.startsWith('data:')) {
          try {
            const jsonString = line.substring(5).trim();
            const data = JSON.parse(jsonString);
            if (data.status === 'result') {
              results.value.push(data);
            } else {
              statusMessage.value = data.message || data.status;
            }
          } catch (e) {
            console.error('Lỗi parse JSON từ stream:', line, e);
          }
        }
      }
    }
  } catch (error) {
    console.error('Lỗi khi gọi OCR:', error);
    statusMessage.value = `Lỗi nghiêm trọng: ${error.message}`;
  } finally {
    isLoading.value = false;
    if (statusMessage.value !== 'Hoàn thành.') {
      statusMessage.value = `Xử lý xong. Tìm thấy ${results.value.length} dòng.`;
    }
  }
}

// --- (HÀM MỚI: LẤY TỌA ĐỘ CHUỘT/TOUCH) ---
function getCoordinates(event) {
  event.preventDefault(); // Ngăn cuộn trang khi vẽ trên mobile
  const rect = canvasRef.value.getBoundingClientRect();
  let x, y;
  if (event.touches) { // Touch event
    x = event.touches[0].clientX - rect.left;
    y = event.touches[0].clientY - rect.top;
  } else { // Mouse event
    x = event.clientX - rect.left;
    y = event.clientY - rect.top;
  }
  return { x, y };
}

// --- (HÀM MỚI: BẮT ĐẦU VẼ) ---
function startDrawing(event) {
  isDrawing.value = true;
  const { x, y } = getCoordinates(event);
  ctx.value.beginPath();
  ctx.value.moveTo(x, y);
}

// --- (HÀM MỚI: VẼ) ---
function draw(event) {
  if (!isDrawing.value) return;
  const { x, y } = getCoordinates(event);
  ctx.value.lineTo(x, y);
  ctx.value.stroke();
}

// --- (HÀM MỚI: DỪNG VẼ) ---
function stopDrawing() {
  isDrawing.value = false;
  ctx.value.beginPath(); // Bắt đầu path mới cho lần vẽ sau
}

// --- (HÀM MỚI: XÓA BẢNG VẼ) ---
function clearCanvas() {
  ctx.value.fillStyle = 'white'; // Nền trắng
  ctx.value.fillRect(0, 0, canvasRef.value.width, canvasRef.value.height);
  predictionResult.value = null;
  predictionStatus.value = '';
}

// --- (HÀM MỚI: CHUYỂN CANVAS THÀNH MATRIX 64x64) ---
function getMatrixFromCanvas() {
  // 1. Tạo 1 canvas 64x64 ẩn
  const processingCanvas = document.createElement('canvas');
  processingCanvas.width = 64;
  processingCanvas.height = 64;
  const pCtx = processingCanvas.getContext('2d');

  // 2. Vẽ canvas 256x256 vào canvas 64x64 (trình duyệt tự động downscale)
  // Dùng nền trắng để đảm bảo pixel trống là 0
  pCtx.fillStyle = 'white';
  pCtx.fillRect(0, 0, 64, 64);
  pCtx.drawImage(canvasRef.value, 0, 0, 256, 256, 0, 0, 64, 64);

  // 3. Lấy dữ liệu pixel 64x64
  const imageData = pCtx.getImageData(0, 0, 64, 64);
  const data = imageData.data; // Mảng 1D [R,G,B,A, R,G,B,A, ...]

  // 4. Tạo ma trận 64x64 rỗng (toàn số 0)
  const matrix = Array(64).fill().map(() => Array(64).fill(0));

  // 5. Chuyển pixel (R,G,B,A) thành 0 hoặc 1
  for (let i = 0; i < data.length; i += 4) {
    // Lấy 1 pixel (chỉ cần kênh R là đủ vì ta vẽ màu đen trên nền trắng)
    const r = data[i];
    const g = data[i + 1];
    const b = data[i + 2];
    
    // Tính giá trị xám (trung bình)
    const grayscale = (r + g + b) / 3;

    // Nếu pixel "tối" (không phải màu trắng) thì là 1, ngược lại là 0
    // Đặt ngưỡng < 250 để bắt nét vẽ
    const binaryValue = (grayscale < 250) ? 1 : 0; 
    
    if (binaryValue === 1) {
      // Tính toán vị trí (x, y) trong ma trận 64x64
      const pixelIndex = i / 4;
      const x = pixelIndex % 64;
      const y = Math.floor(pixelIndex / 64);
      
      matrix[y][x] = 1;
    }
  }
  
  // (Debug: log ma trận ra console)
  // console.log(matrix);
  return matrix;
}

// --- (HÀM CẬP NHẬT: GỌI PREDICT JSON) ---
async function handleHandwritingPrediction() {
  isPredicting.value = true;
  predictionStatus.value = 'Đang xử lý ảnh và gửi đi...';
  predictionResult.value = null;

  // 1. LẤY MATRIX TỪ CANVAS
  const matrixData = getMatrixFromCanvas();

  const token = localStorage.getItem('jwt_token'); 
  if (!token) {
    predictionStatus.value = 'Lỗi: Bạn chưa đăng nhập!';
    isPredicting.value = false;
    return;
  }

  try {
    // 2. Gọi C# endpoint MỚI
    const response = await fetch('https://localhost:7084/api/infer/predict', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json' 
      },
      // 3. Gửi matrixData (thật)
      body: JSON.stringify({ matrix: matrixData }) 
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Lỗi server: ${response.statusText} - ${errorText}`);
    }

    const prediction = await response.json();
    
    console.log('Kết quả dự đoán:', prediction);
    predictionStatus.value = 'Dự đoán thành công!';
    predictionResult.value = prediction;

  } catch (error) {
    console.error('Lỗi khi dự đoán chữ viết tay:', error);
    predictionStatus.value = `Lỗi: ${error.message}`;
  } finally {
    isPredicting.value = false;
  }
}

// --- (LIFECYCLE HOOKS CHO CANVAS) ---

// onMounted: Chạy khi component được tải lên
onMounted(() => {
  if (canvasRef.value) {
    // Lấy context
    ctx.value = canvasRef.value.getContext('2d');
    
    // Cài đặt bút vẽ
    ctx.value.strokeStyle = 'black'; // Màu đen
    ctx.value.lineWidth = 12;      // Nét vẽ dày (cho dễ nhận diện)
    ctx.value.lineCap = 'round'; // Bo tròn nét
    ctx.value.lineJoin = 'round';

    // Xóa canvas (vẽ nền trắng)
    clearCanvas();

    // Thêm Event Listeners cho Chuột
    canvasRef.value.addEventListener('mousedown', startDrawing);
    canvasRef.value.addEventListener('mousemove', draw);
    canvasRef.value.addEventListener('mouseup', stopDrawing);
    canvasRef.value.addEventListener('mouseleave', stopDrawing);

    // Thêm Event Listeners cho Cảm ứng (Mobile)
    canvasRef.value.addEventListener('touchstart', startDrawing);
    canvasRef.value.addEventListener('touchmove', draw);
    canvasRef.value.addEventListener('touchend', stopDrawing);
    canvasRef.value.addEventListener('touchcancel', stopDrawing);
  }
});

// onUnmounted: Chạy khi component bị hủy (để dọn dẹp)
onUnmounted(() => {
  if (canvasRef.value) {
    // Xóa Event Listeners
    canvasRef.value.removeEventListener('mousedown', startDrawing);
    canvasRef.value.removeEventListener('mousemove', draw);
    canvasRef.value.removeEventListener('mouseup', stopDrawing);
    canvasRef.value.removeEventListener('mouseleave', stopDrawing);

    canvasRef.value.removeEventListener('touchstart', startDrawing);
    canvasRef.value.removeEventListener('touchmove', draw);
    canvasRef.value.removeEventListener('touchend', stopDrawing);
    canvasRef.value.removeEventListener('touchcancel', stopDrawing);
  }
});

</script>

<style scoped>
/* (Style cũ của bạn) */
.ocr-page-container {
  max-width: 800px;
  margin: 0 auto;
  padding: 20px;
  background-color: #fff;
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0,0,0,0.05);
}
h2 {
  border-bottom: 2px solid #42b983;
  padding-bottom: 10px;
}
.controls {
  display: flex;
  align-items: center;
  gap: 15px;
  margin: 20px 0;
}
input[type="file"] {
  flex-grow: 1;
}
button {
  padding: 10px 15px;
  font-size: 16px;
  background-color: #42b983;
  color: white;
  border: none;
  border-radius: 5px;
  cursor: pointer;
  transition: background-color 0.2s;
  white-space: nowrap;
}
button:hover:not(:disabled) {
  background-color: #3aa875;
}
button:disabled {
  background-color: #ccc;
  cursor: not-allowed;
}
hr {
  margin: 20px 0;
  border: 0;
  border-top: 1px solid #eee;
}
.status-box {
  padding: 12px;
  background-color: #eef7ff;
  border: 1px solid #bde0ff;
  border-radius: 5px;
  margin-bottom: 20px;
  font-weight: 500;
  color: #0056b3;
}
.ocr-results {
  list-style: none;
  padding: 0;
  max-height: 500px; 
  overflow-y: auto;
  border: 1px solid #ccc;
  border-radius: 5px;
}
.ocr-item {
  padding: 10px 14px;
  border-bottom: 1px solid #eee;
}
.ocr-item-empty {
  padding: 10px 14px;
  color: #888;
}
.ocr-item:last-child {
  border-bottom: none;
}
.ocr-item strong {
  color: #2a7f5e;
}
.previews {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  margin-top: 10px;
}
.previews figure {
  margin: 0;
  padding: 0;
}
.previews figcaption {
  font-size: 0.8em;
  color: #555;
  margin-bottom: 4px;
}
.previews img {
  max-width: 200px;
  height: auto;
  max-height: 50px;
  border: 1px solid #ddd;
  border-radius: 4px;
}
pre {
  background-color: #f4f4f4;
  border: 1px solid #ddd;
  padding: 10px;
  border-radius: 5px;
  white-space: pre-wrap;
  word-wrap: break-word;
}

/* --- (STYLE MỚI CHO CANVAS) --- */
.handwriting-canvas {
  border: 2px solid #333;
  border-radius: 8px;
  cursor: crosshair;
  background-color: white; /* Đảm bảo nền trắng */
  width: 256px;
  height: 256px;
}
</style>

