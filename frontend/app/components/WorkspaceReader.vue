<template>
  <div class="flex h-screen bg-gray-50 overflow-hidden font-sans">
    <div class="w-full h-full flex flex-col border-r border-gray-200 bg-gray-100 relative">
      
      <div class="h-14 bg-white border-b border-gray-200 flex items-center px-4 justify-between shadow-sm z-10 shrink-0">
        <div class="font-semibold text-gray-700 truncate">
          Chi tiết tài liệu
        </div>
        <div class="flex gap-2">
          <button class="px-3 py-1 hover:bg-gray-100 rounded text-sm text-gray-600 font-medium">
            100%
          </button>
          <button class="px-3 py-1 bg-blue-50 text-blue-600 hover:bg-blue-100 rounded text-sm font-medium transition">
            Lưu kết quả
          </button>
        </div>
      </div>

      <div
        v-if="isPdf && totalPages > 0"
        class="absolute top-20 left-1/2 -translate-x-1/2 bg-white/90 backdrop-blur px-4 py-2 rounded-full shadow-lg border border-gray-200 flex gap-6 items-center z-50"
      >
        <button
          @click="changePage(-1)"
          :disabled="currentPage <= 1"
          class="font-bold text-blue-600 hover:text-blue-800 disabled:text-gray-300 transition-colors"
        >
          &lt; Trang trước
        </button>
        <span class="text-sm font-semibold text-gray-700">Trang {{ currentPage }} / {{ totalPages }}</span>
        <button
          @click="changePage(1)"
          :disabled="currentPage >= totalPages"
          class="font-bold text-blue-600 hover:text-blue-800 disabled:text-gray-300 transition-colors"
        >
          Trang sau &gt;
        </button>
      </div>

      <div
        class="flex-1 overflow-auto p-6 flex justify-center items-start bg-gray-200"
        @mouseup="handleTextSelection"
      >
        <div class="relative inline-block max-w-full shadow-xl bg-white">
          
          <img
            v-if="!isPdf"
            :src="imageUrl"
            ref="imageRef"
            @load="onImageLoad"
            class="max-w-full h-auto block"
            alt="Document"
          />

          <canvas
            v-show="isPdf"
            ref="pdfCanvas"
            class="max-w-full h-auto block"
          ></canvas>

          <div
            class="absolute inset-0 pointer-events-auto"
            style="container-type: inline-size"
          >
            <span
              v-for="(item, index) in localResults"
              :key="index"
              class="absolute select-text cursor-text origin-top-left flex items-center"
              :style="calculateTextStyle(item)"
              :title="item.wordText"
            >
              {{ item.wordText }}
            </span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, shallowRef, onMounted, watch, nextTick } from "vue";
import { useRuntimeConfig } from "#app";
import * as pdfjsLib from "pdfjs-dist/legacy/build/pdf";
import workerUrl from "pdfjs-dist/build/pdf.worker.min.mjs?url";
pdfjsLib.GlobalWorkerOptions.workerSrc = workerUrl;

// --- Định nghĩa Emits để gửi dữ liệu ra trang index.vue ---
const emit = defineEmits(['text-selected', 'rag-updated']);

// --- Props ---
const props = defineProps({
  imageUrl: { type: String, required: true },
  results: { type: Array, default: () => [] },
});

// --- States Media & PDF ---
const isPdf = ref(false);
const pdfCanvas = ref(null);
const imageRef = ref(null);
const nativeWidth = ref(0);
const nativeHeight = ref(0);

const pdfDoc = shallowRef(null);
const currentPage = ref(1);
const totalPages = ref(0);
const scannedPagesCache = ref({});
const localResults = ref([]);

// --- WATCHERS & MOUNTED ---
onMounted(() => {
  checkAndRenderMedia();
});

watch(
  () => props.imageUrl,
  () => {
    currentPage.value = 1;
    scannedPagesCache.value = {};
    checkAndRenderMedia();
  },
);

watch(
  () => props.results,
  (newVal) => {
    if (!isPdf.value) {
      localResults.value = newVal;
    }
  },
  { immediate: true },
);

// --- KIỂM TRA ĐỊNH DẠNG ---
async function checkAndRenderMedia() {
  if (!props.imageUrl) return;

  isPdf.value = props.imageUrl.toLowerCase().includes(".pdf");

  if (isPdf.value) {
    await nextTick();
    await renderPdf();
  } else if (imageRef.value && imageRef.value.complete) {
    onImageLoad({ target: imageRef.value });
    localResults.value = props.results;
  }
}

// --- LOGIC PDF & QUÉT NGẦM ---
async function renderPdf() {
  try {
    const loadingTask = pdfjsLib.getDocument(props.imageUrl);
    pdfDoc.value = await loadingTask.promise;
    totalPages.value = pdfDoc.value.numPages;

    await drawPageToScreen(currentPage.value);
    preloadOcr(currentPage.value, 3);
  } catch (error) {
    console.error("Lỗi khi render PDF:", error);
  }
}

async function drawPageToScreen(pageNum) {
  if (!pdfDoc.value) return;
  const page = await pdfDoc.value.getPage(pageNum);
  const viewport = page.getViewport({ scale: 1.5 });

  const canvas = pdfCanvas.value;
  const context = canvas.getContext("2d");
  canvas.height = viewport.height;
  canvas.width = viewport.width;
  nativeWidth.value = viewport.width;
  nativeHeight.value = viewport.height;

  await page.render({ canvasContext: context, viewport: viewport }).promise;

  if (
    scannedPagesCache.value[pageNum] &&
    Array.isArray(scannedPagesCache.value[pageNum])
  ) {
    localResults.value = scannedPagesCache.value[pageNum];
  } else {
    localResults.value = [];
  }
}

async function changePage(delta) {
  const newPage = currentPage.value + delta;
  if (newPage >= 1 && newPage <= totalPages.value) {
    currentPage.value = newPage;
    await drawPageToScreen(currentPage.value);
    preloadOcr(currentPage.value, 3);
  }
}

// TRÁI TIM: Quét ngầm OCR
async function preloadOcr(startPage, lookAheadCount) {
  if (!pdfDoc.value) return;
  const endPage = Math.min(startPage + lookAheadCount - 1, totalPages.value);

  for (let i = startPage; i <= endPage; i++) {
    if (scannedPagesCache.value[i]) continue;

    scannedPagesCache.value[i] = "scanning";

    try {
      const page = await pdfDoc.value.getPage(i);
      const viewport = page.getViewport({ scale: 1.5 });

      const offScreenCanvas = document.createElement("canvas");
      offScreenCanvas.width = viewport.width;
      offScreenCanvas.height = viewport.height;
      const offContext = offScreenCanvas.getContext("2d");

      await page.render({ canvasContext: offContext, viewport: viewport })
        .promise;

      offScreenCanvas.toBlob(
        async (blob) => {
          const formData = new FormData();
          formData.append("image", blob, `page_${i}.jpg`);

          const config = useRuntimeConfig();
          const token = localStorage.getItem("jwt_token");

          const response = await fetch(
            `${
              config.public.apiBaseUrl || "https://localhost:7084"
            }/api/Infer/scan-page-ocr`,
            {
              method: "POST",
              headers: { Authorization: `Bearer ${token}` },
              body: formData,
            },
          );

          if (response.ok) {
            const data = await response.json();
            scannedPagesCache.value[i] = data.results;

            if (currentPage.value === i) {
              localResults.value = data.results;
            }
          } else {
            scannedPagesCache.value[i] = null;
          }
        },
        "image/jpeg",
        0.9,
      );
    } catch (err) {
      console.error(`Lỗi quét ngầm trang ${i}:`, err);
      scannedPagesCache.value[i] = null;
    }
  }
}

// --- LOGIC XỬ LÝ ẢNH THƯỜNG ---
function onImageLoad(e) {
  if (e.target.naturalWidth) {
    nativeWidth.value = e.target.naturalWidth;
    nativeHeight.value = e.target.naturalHeight;
  }
}

// --- TÍNH TOÁN TỌA ĐỘ BÔI ĐEN (Đã sửa tàng hình) ---
function calculateTextStyle(item) {
  if (!item.boundingBox || nativeWidth.value === 0 || nativeHeight.value === 0)
    return { display: "none" };

  let bbox = [];
  try {
    const arr =
      typeof item.boundingBox === "string"
        ? JSON.parse(item.boundingBox)
        : item.boundingBox;
    const xs = arr.map((pt) => pt[0]);
    const ys = arr.map((pt) => pt[1]);
    bbox = [Math.min(...xs), Math.min(...ys), Math.max(...xs), Math.max(...ys)];
  } catch (e) {
    return { display: "none" };
  }

  const [x_min, y_min, x_max, y_max] = bbox;
  const leftPct = (x_min / nativeWidth.value) * 100;
  const topPct = (y_min / nativeHeight.value) * 100;
  const widthPct = ((x_max - x_min) / nativeWidth.value) * 100;
  const heightPct = ((y_max - y_min) / nativeHeight.value) * 100;
  const fontSizeCqw = ((y_max - y_min) / nativeWidth.value) * 100;

  return {
    left: `${leftPct}%`,
    top: `${topPct}%`,
    width: `${widthPct}%`,
    height: `${heightPct}%`,
    fontSize: `${fontSizeCqw * 0.8}cqw`,
    lineHeight: `${fontSizeCqw}cqw`,
    whiteSpace: "nowrap",
    color: "transparent", // Xóa màu đỏ, làm chữ trong suốt đè lên ảnh
    backgroundColor: "transparent",
  };
}

// --- TƯƠNG TÁC NGƯỜI DÙNG: PHÁT SỰ KIỆN RA TRANG CHÍNH ---
function handleTextSelection(e) {
  const selectedText = window.getSelection().toString().trim();
  if (selectedText && selectedText.length > 0 && selectedText.length < 200) {
    // Copy vào bộ nhớ tạm
    navigator.clipboard?.writeText(selectedText).catch(() => {});
    
    // Gửi đoạn text bị bôi đen cùng tọa độ con chuột ra index.vue
    emit('text-selected', { 
      text: selectedText, 
      x: e.clientX, 
      y: e.clientY 
    });
  }
}
</script>

<style scoped>
/* Chữ mờ tàng hình, chỉ hiện màu xanh lam khi user bôi đen */
::selection {
  background: rgba(59, 130, 246, 0.35);
  color: transparent;
}
</style>