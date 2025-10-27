<template>
  <div class="ocr-page-container">
    <h2>Trang Tải Lên OCR</h2>
    <p>Chọn một file ảnh và bấm "Bắt đầu OCR" để xem kết quả stream.</p>

    <!-- 1. Ô chọn file và Nút bấm -->
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
    
    <!-- 2. Khu vực trạng thái -->
    <div v-if="statusMessage" class="status-box">
      <strong>Trạng thái:</strong> {{ statusMessage }}
    </div>
    
    <!-- 3. Khu vực kết quả -->
    <h3>Kết quả ({{ results.length }} dòng tìm thấy):</h3>
    <ul class="ocr-results">
      <!-- Hiển thị khi không có kết quả -->
      <li v-if="!isLoading && results.length === 0" class="ocr-item-empty">
        Chưa có kết quả...
      </li>

      <!-- Dùng v-for để lặp qua mảng results -->
      <li v-for="item in results" :key="item.line_number" class="ocr-item">
        <div class="item-text">
          <strong>Dòng {{ item.line_number }}:</strong> {{ item.text }}
        </div>
        
        <!-- Hiển thị ảnh crop và ảnh highlight -->
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

  </div>
</template>

<script setup>
import { ref } from 'vue';

// --- 1. Định nghĩa State (Trạng thái) ---
// ref() tạo ra một biến "phản ứng" (reactive)
const file = ref(null);
const results = ref([]);
const statusMessage = ref('Chưa chọn file');
const isLoading = ref(false);

// --- 2. Định nghĩa Methods (Hàm) ---

/**
 * Được gọi khi người dùng chọn một file.
 */
function handleFileChange(event) {
  file.value = event.target.files[0];
  statusMessage.value = 'Đã chọn file. Sẵn sàng bắt đầu.';
  results.value = []; // Xóa kết quả cũ
}

/**
 * Hàm chính: Gửi file lên backend và đọc stream kết quả.
 */
async function startOCR() {
  if (!file.value) {
    alert('Vui lòng chọn 1 file ảnh!');
    return;
  }

  // Reset state trước khi chạy
  isLoading.value = true;
  results.value = [];
  statusMessage.value = 'Đang tải file lên...';

  // Chuẩn bị form data để gửi file
  const formData = new FormData();
  formData.append('file', file.value);
  const token = localStorage.getItem('jwt_token'); 
    if (!token) {
        statusMessage.value = 'Lỗi: Bạn chưa đăng nhập!';
        isLoading.value = false;
        return;
    }
  try {
    // Gọi API fetch. Phải dùng fetch để đọc được response.body
    const response = await fetch('https://localhost:7084/api/Infer/stream', {
      method: 'POST',
      body: formData,
      // Quan trọng: Không set 'Content-Type', trình duyệt sẽ tự làm
      headers: {
                // CẬP NHẬT HEADER:
                // Thêm Authorization header
                'Authorization': `Bearer ${token}`
            }
    });

    if (!response.ok) {
      throw new Error(`Lỗi server: ${response.statusText}`);
    }

    // Lấy "reader" (trình đọc) từ response body
    const reader = response.body.getReader();
    const decoder = new TextDecoder(); // Để giải mã text từ Uint8Array
    let buffer = ''; // Buffer để xử lý các message bị ngắt quãng

    // Vòng lặp liên tục đọc stream
    while (true) {
      const { done, value } = await reader.read();

      if (done) {
        // Stream đã kết thúc
        console.log('Stream finished.');
        break; 
      }

      // Nối dữ liệu vừa nhận (dạng byte) vào buffer (dạng text)
      buffer += decoder.decode(value, { stream: true });

      // Tách buffer dựa trên \n\n (chuẩn SSE mà backend đang gửi)
      const lines = buffer.split('\n\n');

      // Giữ lại phần tử cuối (có thể chưa hoàn chỉnh) trong buffer
      buffer = lines.pop(); 

      for (const line of lines) {
        // Chỉ xử lý các dòng bắt đầu bằng "data: "
        if (line.startsWith('data:')) {
          try {
            // Bỏ 5 ký tự "data: " và parse JSON
            const jsonString = line.substring(5).trim();
            const data = JSON.parse(jsonString);

            // CẬP NHẬT STATE CỦA VUE
            if (data.status === 'result') {
              // Nếu là kết quả 1 dòng, đẩy vào mảng results
              results.value.push(data);
            } else {
              // Cập nhật các thông báo trạng thái
              // (processing, detecting, recognizing, done)
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
    // Cập nhật trạng thái cuối cùng nếu chưa phải là "done"
    if (statusMessage.value !== 'Hoàn thành.') {
      statusMessage.value = `Xử lý xong (có thể có lỗi). Tìm thấy ${results.value.length} dòng.`;
    }
  }
}
</script>

<style scoped>
/* Thêm "scoped" để CSS không ảnh hưởng tới các page khác */
.ocr-page-container {
  max-width: 800px;
  margin: 0 auto;
  padding: 20px;
  background-color: #fff;
  border-radius: 8px;
  box-shadow: 0 4px 12px rgba(0,0,0,0.05);
}

h2 {
  border-bottom: 2px solid #42b983; /* Màu xanh của Vue */
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
  white-space: nowrap; /* Chống vỡ dòng chữ */
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
  max-height: 500px; /* Tăng chiều cao tối đa */
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
</style>
