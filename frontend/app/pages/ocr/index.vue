<template>
  <div class="p-6 space-y-6 bg-gray-800 rounded-xl shadow-lg text-gray-300">
    <h2 class="text-xl font-bold mb-4 border-b border-gray-700 pb-2 text-white">
      OCR (Tải lên Ảnh hoặc PDF)
    </h2>

    <div class="flex items-center gap-4">
      <label class="flex-grow">
        <input
          type="file"
          @change="handleFileChange"
          accept="image/*,application/pdf"
          :disabled="isLoading"
          class="block w-full text-sm text-gray-400 file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-sm file:font-semibold file:bg-gray-700 file:text-gray-300 hover:file:bg-gray-600"
        />
      </label>
      <button
        @click="startOCR"
        :disabled="isLoading || !file"
        class="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors disabled:bg-gray-500 disabled:cursor-not-allowed"
      >
        {{ isLoading ? "Đang xử lý..." : "Bắt đầu OCR" }}
      </button>
    </div>

    <hr class="border-t border-gray-700" />

    <div
      v-if="statusMessage"
      class="p-4 bg-gray-900 border border-blue-800 rounded-lg text-blue-300"
    >
      <strong>Trạng thái OCR:</strong> {{ statusMessage }}
    </div>

    <div class="result-container">
      <div class="image-wrapper" ref="imageWrapperRef">
        <div v-if="!originalImageUrl && !isPdf" class="image-placeholder">
          Vui lòng tải ảnh lên để xem preview...
        </div>
        <div v-if="isPdf" class="image-placeholder">
          Đang xử lý PDF (Không có preview)...
        </div>

        <div v-else-if="originalImageUrl" class="image-overlay-container">
          <img
            :src="originalImageUrl"
            alt="Ảnh gốc"
            ref="originalImageRef"
            @load="updateImageDimensions"
          />

          <div
            v-for="(item, index) in results.filter((r) => r.bbox)"
            :key="index"
            class="ocr-bbox"
            :class="{ 'is-hovered': item.line_number === hoveredLine }"
            :style="calculateBoxStyle(item.bbox)"
            @mouseover="hoveredLine = item.line_number"
            @mouseleave="hoveredLine = -1"
            :title="item.text"
          ></div>
        </div>
      </div>

      <div class="text-list-wrapper">
        <h3 class="text-lg font-semibold text-white mb-2">Kết quả OCR:</h3>
        <ul class="ocr-results">
          <li
            v-if="isLoading && results.length === 0"
            class="p-4 text-gray-500"
          >
            Đang xử lý...
          </li>
          <li
            v-if="!isLoading && results.length === 0"
            class="p-4 text-gray-500"
          >
            Chưa có kết quả...
          </li>
          <li
            v-for="item in results"
            :key="item.line_number"
            class="ocr-item"
            :class="{ 'is-hovered': item.line_number === hoveredLine }"
            @mouseover="hoveredLine = item.line_number"
            @mouseleave="hoveredLine = -1"
          >
            <strong class="text-green-400">
              <span v-if="item.page_number">P{{ item.page_number }} - </span>
              L{{ item.line_number }}:
            </strong>
            {{ item.text }}
          </li>
        </ul>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from "vue";

const file = ref(null);
const results = ref([]);
const statusMessage = ref("Chưa chọn file");
const isLoading = ref(false);
const config = useRuntimeConfig();

// --- STATE MỚI ---
const originalImageUrl = ref(null);
const originalImageRef = ref(null);
const imageWrapperRef = ref(null); // Ref cho khung CHA
const hoveredLine = ref(-1);
const isPdf = ref(false);
// Kích thước thật của ảnh
const imageDimensions = ref({
  nativeWidth: 0,
  nativeHeight: 0,
});

function handleFileChange(event) {
  file.value = event.target.files[0];
  if (!file.value) return;

  statusMessage.value = "Đã chọn file. Sẵn sàng bắt đầu.";
  results.value = [];
  imageDimensions.value = { nativeWidth: 0, nativeHeight: 0 }; // Reset

  if (file.value.type.startsWith("image/")) {
    isPdf.value = false;
    originalImageUrl.value = URL.createObjectURL(file.value);
  } else if (file.value.type === "application/pdf") {
    isPdf.value = true;
    originalImageUrl.value = null;
  }
}

// Hàm này chạy khi ảnh gốc @load xong
function updateImageDimensions() {
  if (originalImageRef.value) {
    // Lưu kích thước thật (native) của ảnh
    imageDimensions.value = {
      nativeWidth: originalImageRef.value.naturalWidth,
      nativeHeight: originalImageRef.value.naturalHeight,
    };
  }
}

// --- HÀM TÍNH TOÁN ĐÃ SỬA LỖI ---
function calculateBoxStyle(bbox) {
  const { nativeWidth, nativeHeight } = imageDimensions.value;
  if (
    !bbox ||
    bbox.length < 4 ||
    !originalImageRef.value ||
    !imageWrapperRef.value ||
    nativeWidth === 0 ||
    nativeHeight === 0
  ) {
    return { display: "none" };
  }

  const containerWidth = imageWrapperRef.value.clientWidth;
  const containerHeight = imageWrapperRef.value.clientHeight;
  const ratioX = containerWidth / nativeWidth;
  const ratioY = containerHeight / nativeHeight;
  const ratio = Math.min(ratioX, ratioY);
  const displayedWidth = nativeWidth * ratio;
  const displayedHeight = nativeHeight * ratio;
  const offsetX = (containerWidth - displayedWidth) / 2;
  const offsetY = (containerHeight - displayedHeight) / 2;

  const [x_min, y_min, x_max, y_max] = bbox;

  // <-- thêm hàng này: số px muốn dịch xuống (chỉnh ở đây)
  const extraOffsetY = 10; // dịch xuống 6px

  const top = y_min * ratio + offsetY + extraOffsetY + "px";
  const left = x_min * ratio + offsetX + "px";
  const width = (x_max - x_min) * ratio + "px";
  const height = (y_max - y_min) * ratio + "px";

  return { top, left, width, height };
}

async function startOCR() {
  if (!file.value) {
    alert("Vui lòng chọn 1 file!");
    return;
  }
  isLoading.value = true;
  results.value = [];
  statusMessage.value = "Đang tải file lên...";
  const formData = new FormData();
  formData.append("file", file.value);
  const token = localStorage.getItem("jwt_token");
  if (!token) {
    statusMessage.value = "Lỗi: Bạn chưa đăng nhập!";
    isLoading.value = false;
    return;
  }

  try {
    let apiUrl = "";
    if (file.value.type === "application/pdf") {
      apiUrl = `${config.public.apiBaseUrl}/api/Infer/pdf-stream`;
      statusMessage.value = "Đang xử lý PDF (có thể mất vài phút)...";
    } else {
      apiUrl = `${config.public.apiBaseUrl}/api/Infer/stream`;
    }

    const response = await fetch(apiUrl, {
      method: "POST",
      body: formData,
      headers: { Authorization: `Bearer ${token}` },
    });

    if (!response.ok) {
      throw new Error(`Lỗi server: ${response.statusText}`);
    }

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = "";
    while (true) {
      const { done, value } = await reader.read();
      if (done) {
        console.log("Stream finished.");
        break;
      }
      buffer += decoder.decode(value, { stream: true });
      const lines = buffer.split("\n\n");
      buffer = lines.pop();
      for (const line of lines) {
        if (line.startsWith("data:")) {
          try {
            const jsonString = line.substring(5).trim();
            const data = JSON.parse(jsonString);

            if (data.status === "result") {
              results.value.push(data);
            } else {
              statusMessage.value = data.message || data.status;
            }
          } catch (e) {
            console.error("Lỗi parse JSON từ stream:", line, e);
          }
        }
      }
    }
  } catch (error) {
    console.error("Lỗi khi gọi OCR:", error);
    statusMessage.value = `Lỗi nghiêm trọng: ${error.message}`;
  } finally {
    isLoading.value = false;
    if (statusMessage.value.startsWith("Đang")) {
      statusMessage.value = `Xử lý xong. Tìm thấy ${results.value.length} dòng.`;
    }
  }
}
</script>

<style scoped>
/* CSS cho layout mới (2 cột) */
.result-container {
  display: flex;
  flex-direction: row;
  gap: 20px;
  max-height: 70vh;
  min-height: 500px;
}

/* CỘT BÊN TRÁI (ẢNH) */
.image-wrapper {
  flex: 3;
  background: #111;
  border-radius: 8px;
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1px solid #4b5563;
  position: relative; /* Thêm relative để làm cha cho box */
}
.image-placeholder {
  color: #6b7280;
  padding: 20px;
  text-align: center;
}

/* CỘT BÊN PHẢI (TEXT) */
.text-list-wrapper {
  flex: 2;
  display: flex;
  flex-direction: column;
  min-width: 300px;
}
.ocr-results {
  flex-grow: 1;
  list-style: none;
  padding: 0;
  margin: 0;
  overflow-y: auto;
  border: 1px solid #4b5563;
  border-radius: 5px;
  background: #1f2937;
}
.ocr-item {
  padding: 10px 12px;
  border-bottom: 1px solid #374151;
  transition: background-color 0.1s;
  cursor: default;
}
.ocr-item.is-hovered {
  background-color: #3b82f6; /* Highlight màu xanh */
  color: white;
}
.ocr-item.is-hovered strong {
  color: #c7d2fe;
}

/* CSS cho ảnh và overlay boxes */
.image-overlay-container {
  position: relative; /* Dùng relative để box con định vị */
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}
.image-overlay-container img {
  width: auto; /* Sửa: width auto */
  height: auto;
  max-width: 100%;
  max-height: 100%;
  object-fit: contain;
}
.ocr-bbox {
  position: absolute;
  border: 2px solid rgba(239, 68, 68, 0.7); /* Đỏ */
  background-color: rgba(239, 68, 68, 0.2);
  transition: background-color 0.1s, border-color 0.1s;
  /* pointer-events: none; */
}
/* Khi hover vào list text, highlight box tương ứng */
/* (Chúng ta dùng .image-overlay-container thay vì .ocr-item) */
.text-list-wrapper .ocr-item.is-hovered ~ .image-wrapper .ocr-bbox {
  /* Logic này sai, sửa lại bên dưới */
}
/* Sửa lại: Dùng JS để hover */
.ocr-bbox.is-hovered {
  background-color: rgba(59, 130, 246, 0.5);
  border-color: rgba(59, 130, 246, 1);
  z-index: 10;
}
/* Khi hover vào list, box tương ứng sẽ được thêm class 'is-hovered' (logic đã có) */
.ocr-item.is-hovered {
  background-color: #3b82f6;
}
/* Style scrollbar giống SearchBar của bạn */
.ocr-results::-webkit-scrollbar {
  width: 8px;
}
.ocr-results::-webkit-scrollbar-track {
  background: #1f2937;
}
.ocr-results::-webkit-scrollbar-thumb {
  background-color: #4b5563;
  border-radius: 10px;
  border: 2px solid #1f2937;
  background-clip: padding-box;
}
.ocr-results::-webkit-scrollbar-thumb:hover {
  background-color: #6b7280;
}
</style>
