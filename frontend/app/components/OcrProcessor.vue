<template>
  <div
    class="p-6 space-y-6 bg-white dark:bg-gray-800 rounded-xl shadow-lg text-gray-900 dark:text-gray-300 transition-colors"
  >
    <h2
      class="text-xl font-bold mb-4 border-b border-gray-200 dark:border-gray-700 pb-2 text-gray-900 dark:text-white"
    >
      OCR (Tải lên Ảnh hoặc PDF)
    </h2>

    <div class="flex items-center gap-4">
      <label class="flex-grow">
        <input
          type="file"
          @change="handleFileChange"
          accept="image/*,application/pdf"
          :disabled="isLoading"
          class="block w-full text-sm text-gray-500 dark:text-gray-400 file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-sm file:font-semibold file:bg-gray-100 file:text-gray-700 hover:file:bg-gray-200 dark:file:bg-gray-700 dark:file:text-gray-300 dark:hover:file:bg-gray-600"
        />
      </label>
      <button
        @click="startOCR"
        :disabled="isLoading || !file"
        class="px-4 py-2 bg-primary-600 text-white rounded-lg hover:bg-primary-700 transition-colors disabled:bg-gray-300 dark:disabled:bg-gray-500 disabled:cursor-not-allowed"
      >
        {{ isLoading ? "Đang xử lý..." : "Bắt đầu OCR" }}
      </button>
    </div>

    <hr class="border-t border-gray-200 dark:border-gray-700" />

    <div
      v-if="statusMessage"
      class="p-4 bg-blue-50 border border-blue-200 text-blue-700 dark:bg-gray-900 dark:border-blue-800 dark:text-blue-300 rounded-lg"
    >
      <strong>Trạng thái OCR:</strong> {{ statusMessage }}
    </div>

    <div v-if="!isPdf" class="result-container single-image-layout">
      <div class="image-wrapper" ref="imageWrapperRef">
        <div v-if="!originalImageUrl" class="image-placeholder">
          Vui lòng tải ảnh lên để xem preview...
        </div>

        <div v-else class="image-overlay-container">
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
        <h3
          class="text-lg font-semibold text-gray-900 dark:text-white mb-2"
        >
          Kết quả OCR:
        </h3>
        <ul class="ocr-results">
          <li
            v-if="isLoading && results.length === 0"
            class="p-4 text-gray-400 dark:text-gray-500"
          >
            Đang xử lý...
          </li>
          <li
            v-if="!isLoading && results.length === 0"
            class="p-4 text-gray-400 dark:text-gray-500"
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
            <strong class="text-green-600 dark:text-green-400">
              <span v-if="item.page_number">P{{ item.page_number }} - </span>
              L{{ item.line_number }}:
            </strong>
            {{ item.text }}
          </li>
        </ul>
      </div>
    </div>

    <div v-else-if="isPdf" class="pdf-results-list">
      <div
        v-if="pdfPagePreviews.length === 0"
        class="image-placeholder"
        style="width: 100%; text-align: center; padding: 40px 0"
      >
        {{ statusMessage }}
      </div>

      <div
        v-for="page in pdfPagePreviews"
        :key="page.id"
        class="result-container pdf-page-row"
      >
        <div class="image-wrapper">
          <div class="pdf-page-item">
            <h4 class="page-number-title">Trang {{ page.id }}</h4>
            <div class="image-overlay-container pdf-page-image-container">
              <img :src="page.imageUrl" :alt="'Trang ' + page.id" />

              <div
                v-for="item in results.filter(
                  (r) => r.page_number === page.id && r.bbox
                )"
                :key="item.line_number"
                class="ocr-bbox"
                :class="{ 'is-hovered': item.line_number === hoveredLine }"
                :style="calculateBoxStyleForPdf(item.bbox, page)"
                @mouseover="hoveredLine = item.line_number"
                @mouseleave="hoveredLine = -1"
                :title="item.text"
              ></div>
            </div>
          </div>
        </div>

        <div class="text-list-wrapper">
          <h3
            class="text-lg font-semibold text-gray-900 dark:text-white mb-2"
          >
            Kết quả Trang {{ page.id }}:
          </h3>
          <ul class="ocr-results">
            <li
              v-for="item in results.filter((r) => r.page_number === page.id)"
              :key="item.line_number"
              class="ocr-item"
              :class="{ 'is-hovered': item.line_number === hoveredLine }"
              @mouseover="hoveredLine = item.line_number"
              @mouseleave="hoveredLine = -1"
            >
              <strong class="text-green-600 dark:text-green-400">
                L{{ item.line_number }}:
              </strong>
              {{ item.text }}
            </li>

            <li
              v-if="
                !isLoading &&
                results.filter((r) => r.page_number === page.id).length === 0
              "
              class="p-4 text-gray-400 dark:text-gray-500"
            >
              (Không có kết quả cho trang này)
            </li>
          </ul>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from "vue";
const file = ref(null);
const results = ref([]);
const statusMessage = ref("Chưa chọn file");
const isLoading = ref(false);
const config = useRuntimeConfig();

const originalImageUrl = ref(null);
const originalImageRef = ref(null);
const hoveredLine = ref(-1);
const isPdf = ref(false);
const pdfPagePreviews = ref([]); // [{id, imageUrl, originalWidth, originalHeight, scaledWidth, scaledHeight}]
const imageDimensions = ref({ nativeWidth: 0, nativeHeight: 0 });

function handleFileChange(event) {
  file.value = event.target.files[0];
  if (!file.value) return;

  statusMessage.value = "Đã chọn file. Sẵn sàng bắt đầu.";
  results.value = [];
  imageDimensions.value = { nativeWidth: 0, nativeHeight: 0 };
  pdfPagePreviews.value = [];
  originalImageUrl.value = null;

  if (file.value.type.startsWith("image/")) {
    isPdf.value = false;
    originalImageUrl.value = URL.createObjectURL(file.value);
  } else if (file.value.type === "application/pdf") {
    isPdf.value = true;
    originalImageUrl.value = null;
    generatePdfPreviews(file.value);
  }
}

/**
 * Nuxt 4 + Vite friendly:
 * - dynamic import pdfjs only on client
 * - import worker as URL using '?url' so Vite emits asset and returns URL
 * - set GlobalWorkerOptions.workerSrc BEFORE getDocument()
 */
async function generatePdfPreviews(pdfFile) {
  if (typeof window === "undefined") {
    statusMessage.value = "Không thể render PDF trên server.";
    return;
  }

  try {
    statusMessage.value = "Đang tải thư viện pdf.js...";
    // dynamic import module (legacy build recommended for browser)
    const pdfjsModule = await import("pdfjs-dist/legacy/build/pdf"); // returns module
    // import worker file as URL (Vite will return an URL string under default)
    const workerModule = await import(
      "pdfjs-dist/build/pdf.worker.min.mjs?url"
    );
    const workerSrc = workerModule?.default ?? workerModule;
    // set workerSrc BEFORE any pdfjsModule.getDocument call
    pdfjsModule.GlobalWorkerOptions.workerSrc = workerSrc;

    // read file as arrayBuffer
    const arrayBuffer = await pdfFile.arrayBuffer();
    const loadingTask = pdfjsModule.getDocument({
      data: new Uint8Array(arrayBuffer),
    });
    const pdfDoc = await loadingTask.promise;
    const numPages = pdfDoc.numPages;
    statusMessage.value = `Đang render PDF... (Tổng ${numPages} trang)`;

    const previews = [];
    const scale = 1.5;
    for (let i = 1; i <= numPages; i++) {
      const page = await pdfDoc.getPage(i);
      const originalViewport = page.getViewport({ scale: 1.0 });
      const scaledViewport = page.getViewport({ scale });

      const canvas = document.createElement("canvas");
      const ctx = canvas.getContext("2d");
      canvas.width = Math.round(scaledViewport.width);
      canvas.height = Math.round(scaledViewport.height);

      await page.render({ canvasContext: ctx, viewport: scaledViewport })
        .promise;
      previews.push({
        id: i,
        imageUrl: canvas.toDataURL("image/jpeg"),
        originalWidth: originalViewport.width,
        originalHeight: originalViewport.height,
        scaledWidth: scaledViewport.width,
        scaledHeight: scaledViewport.height,
      });
      // update progressively
      pdfPagePreviews.value = [...previews];
      // tiny yield for UI
      await new Promise((r) => setTimeout(r, 5));
    }
    statusMessage.value = `Đã tạo preview cho ${numPages} trang.`;
  } catch (err) {
    console.error("Lỗi khi render PDF:", err);
    // nếu worker fetch fail, log gốc có thông tin
    statusMessage.value = `Lỗi render PDF: ${err?.message || err}`;
  }
}

function updateImageDimensions() {
  if (originalImageRef.value) {
    imageDimensions.value = {
      nativeWidth: originalImageRef.value.naturalWidth,
      nativeHeight: originalImageRef.value.naturalHeight,
    };
  }
}

function calculateBoxStyle(bbox) {
  const { nativeWidth, nativeHeight } = imageDimensions.value;
  if (
    !bbox ||
    bbox.length < 4 ||
    !originalImageRef.value ||
    nativeWidth === 0 ||
    nativeHeight === 0
  )
    return { display: "none" };

  const container = originalImageRef.value.parentElement;
  if (!container) return { display: "none" };

  const containerWidth = container.clientWidth;
  const containerHeight = container.clientHeight;
  const ratioX = containerWidth / nativeWidth;
  const ratioY = containerHeight / nativeHeight;
  const ratio = Math.min(ratioX, ratioY);
  const displayedWidth = nativeWidth * ratio;
  const displayedHeight = nativeHeight * ratio;
  const offsetX = (containerWidth - displayedWidth) / 2;
  const offsetY = (containerHeight - displayedHeight) / 2;

  const [x_min, y_min, x_max, y_max] = bbox;
  return {
    top: y_min * ratio + offsetY + "px",
    left: x_min * ratio + offsetX + "px",
    width: (x_max - x_min) * ratio + "px",
    height: (y_max - y_min) * ratio + "px",
  };
}

function calculateBoxStyleForPdf(bbox, page) {
  if (
    !bbox ||
    bbox.length < 4 ||
    !page ||
    !page.originalWidth ||
    !page.originalHeight
  )
    return { display: "none" };
  // We use percent positioning relative to originalWidth/originalHeight (scale=1)
  const [x_min, y_min, x_max, y_max] = bbox;
  return {
    top: (y_min / page.originalHeight) * 100 + "%",
    left: (x_min / page.originalWidth) * 100 + "%",
    width: ((x_max - x_min) / page.originalWidth) * 100 + "%",
    height: ((y_max - y_min) / page.originalHeight) * 100 + "%",
  };
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

    if (!response.ok) throw new Error(`Lỗi server: ${response.statusText}`);

    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = "";
    while (true) {
      const { done, value } = await reader.read();
      if (done) break;
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
    if (statusMessage.value.startsWith("Đang"))
      statusMessage.value = `Xử lý xong. Tìm thấy ${results.value.length} dòng.`;
  }
}
</script>

<style scoped>
/* THAY ĐỔI:
  - Tái cấu trúc lại toàn bộ khối style
  - Thêm style cho light mode (mặc định)
  - Bọc style cho dark mode (cũ) trong class .dark
*/

/* === CẤU TRÚC CHUNG === */
.result-container {
  display: flex;
  flex-direction: row;
  gap: 20px;
  align-items: stretch;
}

/* === 1️⃣ ẢNH ĐƠN === */
.single-image-layout {
  display: flex;
  flex-direction: row;
  gap: 20px;
  height: 70vh; /* 🔥 cố định chiều cao */
  overflow: hidden;
  align-items: stretch; /* hai cột cùng chiều cao */
}

/* --- CỘT TRÁI (ẢNH + BOX ĐỎ) --- */
.image-wrapper {
  flex: 3;
  border-radius: 8px;
  overflow: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
  position: relative;
  padding: 12px;
  box-sizing: border-box;
  height: 100%; /* cao bằng layout */
  /* Light Mode */
  background: #f3f4f6; /* bg-gray-100 */
  border: 1px solid #e5e7eb; /* border-gray-200 */
  color: #6b7280; /* text-gray-500 */
}
.dark .image-wrapper {
  /* Dark Mode */
  background: #111;
  border: 1px solid #4b5563;
  color: #9ca3af; /* text-gray-400 */
}

.image-overlay-container {
  position: relative;
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
}
.image-overlay-container img {
  max-width: 100%;
  max-height: 100%;
  object-fit: contain;
  display: block;
}

/* Box đỏ OCR (Giữ nguyên, hoạt động tốt trên cả 2 nền) */
.ocr-bbox {
  position: absolute;
  border: 2px solid rgba(239, 68, 68, 0.7);
  background-color: rgba(239, 68, 68, 0.2);
  transition: background-color 0.1s, border-color 0.1s;
}
.ocr-bbox.is-hovered {
  background-color: rgba(59, 130, 246, 0.5);
  border-color: rgba(59, 130, 246, 1);
  z-index: 10;
}

/* --- CỘT PHẢI (KẾT QUẢ TEXT) --- */
.text-list-wrapper {
  flex: 2;
  display: flex;
  flex-direction: column;
  height: 100%; /* cao bằng khung trái */
  min-height: 0; /* fix flex + overflow */
  overflow: hidden;
  background: transparent;
}
.ocr-results {
  flex: 1;
  overflow-y: auto; /* 🔥 cuộn riêng trong cột phải */
  min-height: 0;
  list-style: none;
  margin: 0;
  padding: 0;
  border: 1px solid #e5e7eb; /* border-gray-200 */
  border-radius: 5px;
  background: #ffffff; /* bg-white */
}
.dark .ocr-results {
  border: 1px solid #4b5563;
  background: #1f2937;
}

.ocr-item {
  padding: 10px 12px;
  border-bottom: 1px solid #f3f4f6; /* border-gray-100 */
  transition: background-color 0.1s;
  cursor: default;
}
.dark .ocr-item {
  border-bottom: 1px solid #374151;
}

.ocr-item.is-hovered {
  background-color: #dbeafe; /* blue-100 */
  color: #1f2937; /* text-gray-800 */
}
.ocr-item.is-hovered strong {
  color: #1d4ed8; /* blue-700 */
}
.dark .ocr-item.is-hovered {
  background-color: #3b82f6; /* blue-500 */
  color: white;
}
.dark .ocr-item.is-hovered strong {
  color: #c7d2fe; /* blue-200 */
}

/* === 2️⃣ PDF (DANH SÁCH NHIỀU TRANG) === */
.pdf-results-list {
  display: flex;
  flex-direction: column;
  gap: 30px;
}
.pdf-page-row {
  display: flex;
  flex-direction: row;
  gap: 20px;
  border: 1px solid #e5e7eb; /* border-gray-200 */
  border-radius: 8px;
  padding: 16px;
  background: #ffffff; /* bg-white */
}
.dark .pdf-page-row {
  border: 1px solid #374151;
  background: #1f2937;
}

.pdf-page-row .image-wrapper {
  flex: 3;
  padding: 0;
  align-items: flex-start;
  border: none;
  background: none;
}
.pdf-page-row .text-list-wrapper {
  flex: 2;
  height: auto; /* Reset chiều cao cố định */
}
.pdf-page-row .ocr-results {
  height: 400px; /* Thêm chiều cao + cuộn cho list text PDF */
}

/* === ẢNH TRONG PDF === */
.pdf-preview-container {
  width: 100%;
  height: 100%;
  overflow-y: visible;
  padding: 0;
  box-sizing: border-box;
  display: flex;
  flex-direction: column;
  align-items: center;
}
.pdf-page-item {
  margin-bottom: 0;
  border: 1px solid #e5e7eb; /* border-gray-200 */
  border-radius: 8px;
  overflow: hidden;
  background: #f9fafb; /* bg-gray-50 */
  width: 100%;
  max-width: 100%;
  box-sizing: border-box;
}
.dark .pdf-page-item {
  border: 1px solid #4b5563;
  background: #1f2937;
}

.page-number-title {
  text-align: center;
  padding: 8px;
  background: #f3f4f6; /* bg-gray-100 */
  color: #111827; /* text-gray-900 */
  font-weight: bold;
}
.dark .page-number-title {
  background: #374151;
  color: white;
}

.pdf-page-image-container {
  position: relative;
  width: 100%;
  height: auto;
}
.pdf-page-image-container img {
  width: 100%;
  display: block;
}

/* === SCROLLBAR CHUNG === */
.ocr-results::-webkit-scrollbar,
.pdf-preview-container::-webkit-scrollbar {
  width: 8px;
}
/* Light Mode */
.ocr-results::-webkit-scrollbar-thumb,
.pdf-preview-container::-webkit-scrollbar-thumb {
  background-color: #d1d5db; /* gray-300 */
  border-radius: 10px;
  border: 2px solid #ffffff; /* bg-white */
}
.ocr-results::-webkit-scrollbar-thumb:hover,
.pdf-preview-container::-webkit-scrollbar-thumb:hover {
  background-color: #9ca3af; /* gray-400 */
}
/* Dark Mode */
.dark .ocr-results::-webkit-scrollbar-thumb,
.dark .pdf-preview-container::-webkit-scrollbar-thumb {
  background-color: #4b5563; /* gray-600 */
  border: 2px solid #1f2937; /* bg-gray-800 */
}
.dark .ocr-results::-webkit-scrollbar-thumb:hover,
.dark .pdf-preview-container::-webkit-scrollbar-thumb:hover {
  background-color: #6b7280; /* gray-500 */
}
</style>