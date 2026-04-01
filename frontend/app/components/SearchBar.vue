<template>
  <div ref="searchContainer" class="relative w-full">
    <div
      class="flex items-center border border-gray-300 dark:border-gray-700 bg-white dark:bg-gray-800 rounded-2xl px-3 py-2 w-full"
    >
      <button
        @click="onSearch"
        class="text-gray-400 hover:text-gray-600 dark:text-gray-500 dark:hover:text-gray-300 transition mr-3"
      >
        <UIcon name="i-lucide-search" class="size-5" />
      </button>
      <input
        v-model="internalSearchWord"
        type="text"
        maxlength="50"
        placeholder="日本語, にほんご, nihongo..."
        class="flex-grow bg-transparent outline-none text-base text-gray-900 dark:text-white"
        @keyup.enter="onSearch"
        @focus="onInputFocus"
        @keydown="handleKeydown"
      />

      <button
        ref="penButtonRef"
        @click="onPenClick"
        class="text-gray-400 hover:text-gray-600 dark:text-gray-500 dark:hover:text-gray-300 transition ml-3"
        :class="{ 'text-blue-500 dark:text-blue-400': showDrawingPad }"
      >
        <UIcon name="i-lucide-pen" class="size-5" />
      </button>

      <button
        @click="onImageClick"
        class="text-gray-400 hover:text-gray-600 dark:text-gray-500 dark:hover:text-gray-300 transition ml-2"
        :class="{ 'text-blue-500 dark:text-blue-400': showOcrPad }"
      >
        <UIcon name="i-lucide-image" class="size-5" />
      </button>
    </div>

    <div
      v-if="showSuggestions && suggestions.length > 0"
      class="absolute z-1000 w-full mt-1 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg max-h-80 overflow-y-auto suggestions-list"
    >
      <ul ref="suggestionsListEl">
        <li
          v-for="(suggestion, index) in suggestions"
          :key="suggestion.word + suggestion.reading"
          class="px-4 py-3 border-b border-gray-200 dark:border-gray-700 last:border-b-0 cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-700"
          :class="{ 'bg-gray-100 dark:bg-gray-700': index === selectedIndex }"
          @click="onSelectSuggestion(suggestion)"
        >
          <div class="flex items-baseline gap-x-2">
            <span class="font-medium text-gray-900 dark:text-white">{{
              suggestion.word
            }}</span>
            <span class="text-sm text-blue-500 dark:text-blue-400">{{
              suggestion.reading
            }}</span>
          </div>
          <p class="text-sm text-gray-600 dark:text-gray-300 mt-1">
            {{ suggestion.meaning }}
          </p>
        </li>
      </ul>
    </div>

    <div
      v-if="showDrawingPad"
      class="absolute z-10 w-full mt-1 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg p-4 z-1000"
    >
      <div class="min-h-[48px] mb-3 flex items-center justify-center">
        <div
          v-if="isPredicting"
          class="flex items-center justify-center h-full"
        >
          <div
            class="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-500 dark:border-blue-400"
          ></div>
        </div>

        <div
          v-else-if="predictionLabels.length > 0"
          class="flex flex-wrap justify-center gap-2"
        >
          <button
            v-for="label in predictionLabels"
            :key="label"
            class="px-3 py-1 bg-gray-100 text-gray-800 hover:bg-gray-200 dark:bg-gray-700 dark:text-white dark:hover:bg-gray-600 rounded-md text-center font-medium transition"
            @click="onSelectPrediction(label)"
          >
            {{ label }}
          </button>
        </div>

        <p
          v-else
          class="text-sm text-gray-500 dark:text-gray-400 text-center py-2"
        >
          Vẽ chữ Kanji hoặc Kana vào khung bên dưới và chọn từ gợi ý để thêm vào
          ô tìm kiếm.
        </p>
      </div>

      <canvas
        ref="canvasRef"
        width="256"
        height="256"
        class="handwriting-canvas"
        style="width: 256px; height: 256px; margin: 0 auto"
        :style="{ opacity: isPredicting ? 0.7 : 1 }"
      ></canvas>
      <div class="flex justify-center items-center gap-x-4 mt-4">
        <button
          @click.prevent="undo"
          :disabled="historyIndex <= 0 || isPredicting"
          class="text-gray-500 hover:text-gray-800 disabled:text-gray-300 dark:text-gray-400 dark:hover:text-white dark:disabled:text-gray-600 disabled:cursor-not-allowed"
        >
          <UIcon name="i-lucide-undo-2" class="size-5" />
        </button>
        <button
          @click.prevent="redo"
          :disabled="historyIndex >= history.length - 1 || isPredicting"
          class="text-gray-500 hover:text-gray-800 disabled:text-gray-300 dark:text-gray-400 dark:hover:text-white dark:disabled:text-gray-600 disabled:cursor-not-allowed"
        >
          <UIcon name="i-lucide-redo-2" class="size-5" />
        </button>

        <div class="border-l border-gray-300 dark:border-gray-600 h-6"></div>

        <button
          @click.prevent="clearCanvas"
          :disabled="isPredicting"
          class="text-gray-500 hover:text-gray-800 disabled:text-gray-300 dark:text-gray-400 dark:hover:text-white dark:disabled:text-gray-600"
        >
          Xóa
        </button>
      </div>
    </div>

    <div
      v-if="showOcrPad"
      class="absolute z-10 w-full mt-1 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-lg p-4 z-1000"
    >
      <div class="flex" style="min-height: 250px">
        <div
          class="w-1/2 pr-3 border-r border-gray-300 dark:border-gray-600 flex flex-col"
        >
          <h4
            class="text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase mb-2"
          >
            Văn bản được nhận dạng
          </h4>

          <div
            ref="ocrResultsRef"
            class="flex-grow max-h-[210px] overflow-y-auto suggestions-list pr-2"
          >
            <p
              v-if="ocrResults.length === 0 && !isOcrLoading && !ocrStatus"
              class="text-sm text-gray-500 dark:text-gray-400"
            >
              Chưa có kết quả nào. Vui lòng tải lên hình ảnh chứa văn bản tiếng
              Nhật.
            </p>

            <div class="flex flex-col gap-1">
              <p
                v-for="(result, index) in ocrResults"
                :key="index"
                class="w-full text-left p-2 bg-gray-100 text-gray-800 hover:bg-gray-200 dark:bg-gray-700 dark:text-white dark:hover:bg-gray-600 rounded-md transition"
              >
                {{ result.text }}
              </p>
            </div>
          </div>

          <div
            class="pt-2 mt-auto border-t border-gray-300 dark:border-gray-700 min-h-[2.5rem]"
          >
            <div v-if="isOcrLoading" class="flex items-center gap-2">
              <div
                class="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-500 dark:border-blue-400"
              ></div>
              <span class="text-sm text-gray-500 dark:text-gray-400">{{
                ocrStatus
              }}</span>
            </div>
            <p
              v-else-if="ocrStatus"
              class="text-sm text-gray-500 dark:text-gray-400"
            >
              {{ ocrStatus }}
            </p>
          </div>
        </div>

        <div class="w-1/2 pl-3 flex flex-col items-center justify-center">
          <input
            type="file"
            ref="ocrFileInputRef"
            @change="handleFileChange"
            accept="image/*"
            class="hidden"
            :disabled="isOcrLoading"
          />
          <div
            v-if="!isOcrLoading"
            @click="onUploadAreaClick"
            @dragover.prevent="handleDragOver"
            @dragleave.prevent="handleDragLeave"
            @drop.prevent="handleDrop"
            class="w-full h-full flex flex-col items-center justify-center border-2 border-dashed border-gray-300 dark:border-gray-600 rounded-lg cursor-pointer hover:border-gray-400 dark:hover:border-gray-500 transition"
            :class="{
              'border-blue-500 bg-blue-50 dark:bg-gray-700': isDragging,
            }"
          >
            <UIcon
              name="i-lucide-upload-cloud"
              class="size-10 text-gray-400 dark:text-gray-500 mb-3"
            />
            <p class="text-sm text-gray-500 dark:text-gray-400">
              <span class="font-semibold text-blue-500 dark:text-blue-400"
                >Click vào để upload</span
              >
              hoặc kéo thả ảnh vào đây
            </p>
            <p class="text-xs text-gray-400 dark:text-gray-500">
              Định dạng (PNG, JPG, etc.)
            </p>
          </div>

          <div
            v-else
            class="w-full h-full flex flex-col items-center justify-center border-2 border-dashed border-gray-300 dark:border-gray-700 rounded-lg"
          >
            <p class="text-sm text-gray-500 dark:text-gray-400">
              Processing...
            </p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* ✅ SỬA LỖI Ở ĐÂY: CSS cho canvas và scrollbar */

/* --- Canvas Styling --- */
.handwriting-canvas {
  /* Light Mode (Mặc định) */
  border: 2px solid #d1d5db; /* gray-300 */
  border-radius: 8px;
  cursor: crosshair;
  background-color: #ffffff; /* NỀN TRẮNG (cho light mode) */
}
.dark .handwriting-canvas {
  /* Dark Mode */
  border: 2px solid #4b5563; /* gray-600 */
  background-color: #1f2937; /* NỀN TỐI (cho dark mode) */
}
/* (Logic MÀU BÚT VẼ nằm trong <script setup>) */

/* --- Scrollbar Styling (Chung cho cả 2 list) --- */
.suggestions-list::-webkit-scrollbar,
.ocr-results-ref::-webkit-scrollbar {
  /* Sửa tên class cho đúng */
  width: 8px;
}
/* Light Mode */
.suggestions-list::-webkit-scrollbar-track,
.ocr-results-ref::-webkit-scrollbar-track {
  background: #f9fafb; /* gray-50 */
  border-radius: 10px;
}
.suggestions-list::-webkit-scrollbar-thumb,
.ocr-results-ref::-webkit-scrollbar-thumb {
  background-color: #d1d5db; /* gray-300 */
  border-radius: 10px;
  border: 2px solid #f9fafb; /* gray-50 */
  background-clip: padding-box;
}
.suggestions-list::-webkit-scrollbar-thumb:hover,
.ocr-results-ref::-webkit-scrollbar-thumb:hover {
  background-color: #9ca3af; /* gray-400 */
}
/* Dark Mode */
.dark .suggestions-list::-webkit-scrollbar-track,
.dark .ocr-results-ref::-webkit-scrollbar-track {
  background: #1f2937; /* gray-800 */
}
.dark .suggestions-list::-webkit-scrollbar-thumb,
.dark .ocr-results-ref::-webkit-scrollbar-thumb {
  background-color: #4b5563; /* gray-600 */
  border: 2px solid #1f2937; /* gray-800 */
}
.dark .suggestions-list::-webkit-scrollbar-thumb:hover,
.dark .ocr-results-ref::-webkit-scrollbar-thumb:hover {
  background-color: #6b7280; /* gray-500 */
}
</style>

<script setup lang="ts">
// ✅ SỬA LỖI Ở ĐÂY: Thêm useColorMode
import { ref, watch, onMounted, onBeforeUnmount, nextTick } from "vue";
import { toKana } from "wanakana";
import { useColorMode } from "#imports"; // <-- THÊM DÒNG NÀY

// --- Props & Emits ---
const props = defineProps({
  modelValue: {
    type: String,
    default: "",
  },
  searchResultRef: {
    type: Object, // Đây là một cái 'ref'
    default: null,
  },
});
const emit = defineEmits(["update:modelValue", "search"]);
const autocompleteCache = new Map<string, any[]>();
const config = useRuntimeConfig();
const colorMode = useColorMode(); // <-- THÊM DÒNG NÀY

// --- Internal State ---
const internalSearchWord = ref(props.modelValue);
const suggestions = ref<any[]>([]);
const showSuggestions = ref(false);
const searchContainer = ref<HTMLDivElement | null>(null);
let debounceTimer: any = null;
const selectedIndex = ref(-1);
const suggestionsListEl = ref<HTMLUListElement | null>(null);
const isProgrammaticUpdate = ref(false);
const abortController = ref<AbortController | null>(null);

// --- Drawing Pad State ---
const showDrawingPad = ref(false);
const penButtonRef = ref<HTMLButtonElement | null>(null);
const canvasRef = ref<HTMLCanvasElement | null>(null);
const ctx = ref<CanvasRenderingContext2D | null>(null);
const isDrawing = ref(false);
const didDraw = ref(false);
const history = ref<ImageData[]>([]);
const historyIndex = ref(-1);
const isPredicting = ref(false);
const predictionLabels = ref<string[]>([]);
// const isHandlingPredictionClick = ref(false);

const predictionAbortController = ref<AbortController | null>(null);

// --- NEW: OCR Pad State ---
const showOcrPad = ref(false);
const ocrResultsRef = ref<HTMLDivElement | null>(null);
const ocrFileInputRef = ref<HTMLInputElement | null>(null);
const ocrResults = ref<any[]>([]); // Sửa thành any[] để nhận { text: "..." }
const isDragging = ref(false);
const isOcrLoading = ref(false);
const ocrStatus = ref("");

// --- Watcher to sync prop to internal state ---
watch(
  () => props.modelValue,
  (newValue) => {
    if (newValue !== internalSearchWord.value) {
      isProgrammaticUpdate.value = true;
      internalSearchWord.value = newValue;
    }
  },
);

// --- Watcher for Autocomplete ---
watch(internalSearchWord, (newValue) => {
  emit("update:modelValue", newValue);

  if (isProgrammaticUpdate.value) {
    isProgrammaticUpdate.value = false;
    return;
  }

  if (debounceTimer) {
    clearTimeout(debounceTimer);
  }
  selectedIndex.value = -1;

  const trimmed = newValue.trim();
  if (!trimmed) {
    suggestions.value = [];
    showSuggestions.value = false;
    return;
  }

  // Tiền xử lý từ khóa bằng wanakana
  const convertedWord = toKana(trimmed);

  // --- BƯỚC 2: CHECK CACHE TRƯỚC KHI DEBOUNCE VÀ FETCH ---
  if (autocompleteCache.has(convertedWord)) {
    suggestions.value = autocompleteCache.get(convertedWord) || [];
    if (!showDrawingPad.value) {
      showSuggestions.value = suggestions.value.length > 0;
    }
    return; // Có rồi thì nghỉ, KHÔNG CẦN FETCH, KHÔNG CẦN CHỜ DEBOUNCE!
  }

  // --- NẾU CHƯA CÓ, MỚI BẮT ĐẦU CHỜ DEBOUNCE ĐỂ GỌI API ---
  debounceTimer = setTimeout(async () => {
    if (abortController.value) {
      abortController.value.abort();
    }
    abortController.value = new AbortController();
    const signal = abortController.value.signal;

    try {
      const res = await fetch(
        `${
          config.public.apiBaseUrl
        }/api/Search/autocomplete/${encodeURIComponent(convertedWord)}`,
        { signal },
      );
      if (!res.ok) throw new Error("Autocomplete fetch failed");

      const data = await res.json();

      // --- BƯỚC 3: LƯU KẾT QUẢ VÀO CACHE CHO LẦN SAU ---
      autocompleteCache.set(convertedWord, data || []);

      suggestions.value = data || [];

      if (!showDrawingPad.value) {
        showSuggestions.value = suggestions.value.length > 0;
      }
    } catch (e: any) {
      if (e.name === "AbortError") {
        return;
      }
      console.error("Autocomplete error:", e);
      suggestions.value = [];
      showSuggestions.value = false;
    }
  }, 200);
});

// --- Watcher to manage canvas listeners ---
watch(showDrawingPad, (isShowing) => {
  if (isShowing) {
    // Wait for canvas to be in the DOM, then initialize
    nextTick(() => {
      initializeCanvas();
    });
  } else {
    // Remove listeners when pad is hidden to prevent errors
    destroyCanvasListeners();
  }
});

// --- Methods ---

const onInputFocus = () => {
  // Khi focus, chỉ cần hiện gợi ý (nếu có) và tắt OCR pad
  showSuggestions.value = suggestions.value.length > 0;
  showOcrPad.value = false;
  // KHÔNG tắt drawing pad ở đây nữa
};

const onPenClick = () => {
  // Toggle the drawing pad
  showDrawingPad.value = !showDrawingPad.value;

  showSuggestions.value = false;
  showOcrPad.value = false; // <-- ADD THIS
};

const onImageClick = () => {
  showOcrPad.value = !showOcrPad.value;
  // Hide other panels
  showSuggestions.value = false;
  showDrawingPad.value = false;
};

const onSearch = () => {
  showSuggestions.value = false;
  showDrawingPad.value = false;
  showOcrPad.value = false;
  if (debounceTimer) {
    clearTimeout(debounceTimer);
  }
  emit("search", internalSearchWord.value.trim());
};

const onSelectSuggestion = (suggestion: any) => {
  if (!suggestion) return;

  isProgrammaticUpdate.value = true;
  internalSearchWord.value = suggestion.word;

  showSuggestions.value = false;
  selectedIndex.value = -1;

  emit("search", suggestion.word);
};

const handleClickOutside = (event: MouseEvent) => {
  const target = event.target as Node;

  // Kiểm tra 1: Click có bên trong SearchBar không?
  if (searchContainer.value && searchContainer.value.contains(target)) {
    return; // Nếu có, không làm gì cả
  }

  // ✅ SỬA LỖI: (props.searchResultRef as any)
  // Kiểm tra 2: Click có bên trong SearchResult (dùng prop) không?
  if (
    props.searchResultRef &&
    (props.searchResultRef as any).contains(target)
  ) {
    return; // Nếu có, không làm gì cả
  }

  // Nếu click ra ngoài cả 2, thì TẮT
  showSuggestions.value = false;
  showDrawingPad.value = false;
  showOcrPad.value = false;
  selectedIndex.value = -1;
};

const scrollToSelected = async () => {
  if (selectedIndex.value < 0 || !suggestionsListEl.value) return;

  await nextTick();
  const selectedEl = suggestionsListEl.value.children[
    selectedIndex.value
  ] as HTMLLIElement;
  if (selectedEl) {
    selectedEl.scrollIntoView({
      block: "nearest",
      behavior: "smooth",
    });
  }
};

const handleKeydown = (event: KeyboardEvent) => {
  // Don't handle keys if drawing pad is open
  if (showDrawingPad.value) return;

  if (showSuggestions.value && suggestions.value.length > 0) {
    if (event.key === "ArrowDown") {
      event.preventDefault();
      if (selectedIndex.value < suggestions.value.length - 1) {
        selectedIndex.value++;
        scrollToSelected();
      }
    } else if (event.key === "ArrowUp") {
      event.preventDefault();
      if (selectedIndex.value > 0) {
        selectedIndex.value--;
        scrollToSelected();
      }
    } else if (event.key === "Enter") {
      if (selectedIndex.value >= 0) {
        event.preventDefault();
        onSelectSuggestion(suggestions.value[selectedIndex.value]);
      }
    } else if (event.key === "Escape") {
      showSuggestions.value = false;
      selectedIndex.value = -1;
    }
  }
};

const autoScrollOcrResults = () => {
  nextTick(() => {
    if (ocrResultsRef.value) {
      ocrResultsRef.value.scrollTop = ocrResultsRef.value.scrollHeight;
    }
  });
};

// --- Canvas Drawing Methods ---

// ✅ SỬA LỖI Ở ĐÂY: Cập nhật hàm này
const initializeCanvas = () => {
  if (!canvasRef.value) return;
  // Thêm { willReadFrequently: true } để tối ưu getImageData
  ctx.value = canvasRef.value.getContext("2d", { willReadFrequently: true });
  if (!ctx.value) return;

  // Đặt style bút vẽ dựa trên theme
  ctx.value.strokeStyle = colorMode.value === "dark" ? "white" : "black"; // <-- SỬA Ở ĐÂY
  ctx.value.lineWidth = 12;
  ctx.value.lineCap = "round";
  ctx.value.lineJoin = "round";

  clearCanvas(); // Đặt nền ban đầu

  // Add listeners
  canvasRef.value.addEventListener("mousedown", startDrawing);
  canvasRef.value.addEventListener("mousemove", draw);
  canvasRef.value.addEventListener("mouseup", stopDrawing);
  canvasRef.value.addEventListener("mouseleave", stopDrawing);
  canvasRef.value.addEventListener("touchstart", startDrawing, {
    passive: false,
  });
  canvasRef.value.addEventListener("touchmove", draw, { passive: false });
  canvasRef.value.addEventListener("touchend", stopDrawing);
  canvasRef.value.addEventListener("touchcancel", stopDrawing);
};

const destroyCanvasListeners = () => {
  if (!canvasRef.value) return;
  // Remove listeners
  canvasRef.value.removeEventListener("mousedown", startDrawing);
  canvasRef.value.removeEventListener("mousemove", draw);
  canvasRef.value.removeEventListener("mouseup", stopDrawing);
  canvasRef.value.removeEventListener("mouseleave", stopDrawing);
  canvasRef.value.removeEventListener("touchstart", startDrawing);
  canvasRef.value.removeEventListener("touchmove", draw);
  canvasRef.value.removeEventListener("touchend", stopDrawing);
  canvasRef.value.removeEventListener("touchcancel", stopDrawing);
};

const getCoordinates = (event: MouseEvent | TouchEvent) => {
  event.preventDefault(); // Prevents page scrolling while drawing
  if (!canvasRef.value) return { x: 0, y: 0 };

  const rect = canvasRef.value.getBoundingClientRect();
  let x, y;

  if (event instanceof TouchEvent) {
    x = event.touches[0].clientX - rect.left;
    y = event.touches[0].clientY - rect.top;
  } else {
    // MouseEvent
    x = event.clientX - rect.left;
    y = event.clientY - rect.top;
  }
  return { x, y };
};

const startDrawing = (event: MouseEvent | TouchEvent) => {
  // Truncate history if we draw after undoing
  if (historyIndex.value < history.value.length - 1) {
    history.value = history.value.slice(0, historyIndex.value + 1);
  }

  isDrawing.value = true;
  didDraw.value = false;
  const { x, y } = getCoordinates(event);
  ctx.value?.beginPath();
  ctx.value?.moveTo(x, y);
};

const draw = (event: MouseEvent | TouchEvent) => {
  if (!isDrawing.value) return;
  didDraw.value = true;
  const { x, y } = getCoordinates(event);
  ctx.value?.lineTo(x, y);
  ctx.value?.stroke();
};

const stopDrawing = () => {
  if (!isDrawing.value) return;
  isDrawing.value = false;
  ctx.value?.beginPath();

  if (didDraw.value) {
    saveHistory();
    runPrediction();
  }
};

// ✅ SỬA LỖI Ở ĐÂY: Cập nhật hàm này
const clearCanvas = () => {
  if (!ctx.value || !canvasRef.value) return;

  // 1. Đặt MÀU NỀN (Fill) dựa trên theme
  // (CSS trong <style> cũng đặt màu nền, nhưng hàm fillRect này
  // đảm bảo hình ảnh export ra (getMatrix) có nền đúng)
  ctx.value.fillStyle = colorMode.value === "dark" ? "#1f2937" : "#ffffff"; // gray-800 / white
  ctx.value.fillRect(0, 0, canvasRef.value.width, canvasRef.value.height);

  // 2. Đặt MÀU BÚT VẼ (Stroke) dựa trên theme
  ctx.value.strokeStyle = colorMode.value === "dark" ? "white" : "black"; // <-- SỬA Ở ĐÂY

  history.value = [];
  historyIndex.value = -1;
  saveHistory();

  predictionLabels.value = [];
  isPredicting.value = false;
  didDraw.value = false;
};

const saveHistory = () => {
  if (!ctx.value || !canvasRef.value) return;
  const data = ctx.value.getImageData(
    0,
    0,
    canvasRef.value.width,
    canvasRef.value.height,
  );
  history.value.push(data);
  historyIndex.value = history.value.length - 1;
};

const restoreHistory = () => {
  if (!ctx.value || !canvasRef.value || historyIndex.value < 0) return;
  const data = history.value[historyIndex.value];
  ctx.value.putImageData(data, 0, 0);
};

const undo = () => {
  if (historyIndex.value <= 0 || isPredicting.value) return;

  historyIndex.value--;
  restoreHistory();

  if (historyIndex.value === 0) {
    predictionLabels.value = [];
  } else {
    runPrediction();
  }
};

const redo = () => {
  if (historyIndex.value >= history.value.length - 1 || isPredicting.value)
    return;

  historyIndex.value++;
  restoreHistory();
  runPrediction();
};

const onSelectPrediction = (label: string) => {
  isProgrammaticUpdate.value = true;
  // Concatenate the label to the existing search word
  internalSearchWord.value = internalSearchWord.value + label;

  // Clear the canvas and predictions
  clearCanvas();

  penButtonRef.value?.focus();
};

// ✅ SỬA LỖI Ở ĐÂY: Cập nhật hàm này
const getMatrixFromCanvas = () => {
  const processingCanvas = document.createElement("canvas");
  processingCanvas.width = 64;
  processingCanvas.height = 64;
  const pCtx = processingCanvas.getContext("2d", { willReadFrequently: true }); // Thêm willReadFrequently
  if (!pCtx) return [];

  const isDark = colorMode.value === "dark";

  // 1. Vẽ nền (giống hệt clearCanvas)
  pCtx.fillStyle = isDark ? "#1f2937" : "#ffffff"; // Match background
  pCtx.fillRect(0, 0, 64, 64);

  // 2. Vẽ hình ảnh đã thu nhỏ
  if (canvasRef.value) {
    pCtx.drawImage(canvasRef.value, 0, 0, 256, 256, 0, 0, 64, 64);
  }

  const imageData = pCtx.getImageData(0, 0, 64, 64);
  const data = imageData.data;
  const matrix = Array(64)
    .fill(0)
    .map(() => Array(64).fill(0));

  // 3. Đặt ngưỡng (threshold)
  const threshold = isDark ? 50 : 200; // Ngưỡng cho màu trắng (dark) / màu đen (light)

  for (let i = 0; i < data.length; i += 4) {
    const r = data[i];
    const g = data[i + 1];
    const b = data[i + 2];
    const grayscale = (r + g + b) / 3;

    // 4. Kiểm tra pixel "nét vẽ"
    // Dark mode: nét vẽ là MÀU TRẮNG (grayscale > 50)
    // Light mode: nét vẽ là MÀU ĐEN (grayscale < 200)
    const binaryValue = isDark
      ? grayscale > threshold
        ? 1
        : 0
      : grayscale < threshold
      ? 1
      : 0;

    if (binaryValue === 1) {
      const pixelIndex = i / 4;
      const x = pixelIndex % 64;
      const y = Math.floor(pixelIndex / 64);
      matrix[y][x] = 1;
    }
  }
  return matrix;
};

// (Các hàm còn lại: runPrediction, OCR, Lifecycle... giữ nguyên)
// ...
const runPrediction = async () => {
  // --- NEW: Abort previous request ---
  if (predictionAbortController.value) {
    predictionAbortController.value.abort("New prediction started");
  }
  predictionAbortController.value = new AbortController();
  const signal = predictionAbortController.value.signal;
  // --- END NEW ---

  isPredicting.value = true;
  predictionLabels.value = [];

  const matrixData = getMatrixFromCanvas();
  const token = localStorage.getItem("jwt_token");

  // ✅ SỬA LỖI: Bỏ kiểm tra token (theo yêu cầu trước)
  /*
  if (!token) {
    console.error("Prediction error: Not logged in");
    isPredicting.value = false;
    return;
  }
  */

  try {
    const response = await fetch(
      `${config.public.apiBaseUrl}/api/infer/predict`,
      {
        method: "POST",
        headers: {
          // Authorization: `Bearer ${token}`, // <-- Đã bỏ
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ matrix: matrixData }),
        signal: signal,
      },
    );

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Server error: ${response.statusText} - ${errorText}`);
    }

    const prediction = await response.json();

    if (prediction && prediction.top_categories) {
      predictionLabels.value = prediction.top_categories;
    } else {
      predictionLabels.value = [];
    }
  } catch (error: any) {
    // --- NEW: Handle AbortError ---
    if (error.name === "AbortError") {
      console.log("Prediction aborted:", error.message);
      return; // Don't set error, this is intentional
    }
    // --- END NEW ---
    console.error("Error during handwriting prediction:", error);
    predictionLabels.value = [];
  } finally {
    isPredicting.value = false;
  }
};

const onUploadAreaClick = () => {
  ocrFileInputRef.value?.click();
};

const handleFileChange = (event: Event) => {
  const target = event.target as HTMLInputElement;
  const file = target.files?.[0];
  if (file) {
    processFile(file);
  }
  if (target) target.value = "";
};

const handleDragOver = () => {
  isDragging.value = true;
};

const handleDragLeave = () => {
  isDragging.value = false;
};

const handleDrop = (event: DragEvent) => {
  isDragging.value = false;
  const file = event.dataTransfer?.files?.[0];
  if (file) {
    processFile(file);
  }
};

/**
 * Placeholder for processing the uploaded file.
 */
const processFile = async (file: File) => {
  ocrResults.value = [];
  isOcrLoading.value = true;
  ocrStatus.value = "Uploading file...";

  const formData = new FormData();
  formData.append("file", file);
  const token = localStorage.getItem("jwt_token");

  if (!token) {
    ocrStatus.value = "Error: Not logged in!";
    isOcrLoading.value = false;
    return;
  }

  try {
    const response = await fetch(
      `${config.public.apiBaseUrl}/api/Infer/stream`,
      {
        method: "POST",
        body: formData,
        headers: { Authorization: `Bearer ${token}` },
      },
    );

    if (!response.ok) {
      throw new Error(`Server error: ${response.statusText}`);
    }

    const reader = response.body?.getReader();
    if (!reader) {
      throw new Error("Could not read response body.");
    }

    const decoder = new TextDecoder();
    let buffer = "";
    ocrStatus.value = "Processing image...";

    while (true) {
      const { done, value } = await reader.read();
      if (done) {
        console.log("Stream finished.");
        break;
      }

      buffer += decoder.decode(value, { stream: true });
      const lines = buffer.split("\n\n");
      buffer = lines.pop() || ""; // Keep the last incomplete part

      for (const line of lines) {
        if (line.startsWith("data:")) {
          try {
            const jsonString = line.substring(5).trim();
            const data = JSON.parse(jsonString);

            if (data.status === "result" && data.text) {
              ocrResults.value.push(data); // Push the whole { text: "..." } object
              autoScrollOcrResults();
            } else {
              ocrStatus.value = data.message || data.status;
            }
          } catch (e) {
            console.error("Error parsing JSON from stream:", line, e);
          }
        }
      }
    }
  } catch (error: any) {
    console.error("Error during OCR streaming:", error);
    ocrStatus.value = `Lỗi: ${error.message}`;
  } finally {
    isOcrLoading.value = false;
    if (ocrResults.value.length > 0) {
      ocrStatus.value = `${ocrResults.value.length} dòng.`;
    } else if (!ocrStatus.value.startsWith("Error")) {
      ocrStatus.value = "Không tìm thấy văn bản tiếng Nhật.";
    }
  }
};

// --- Lifecycle Hooks ---
onMounted(() => {
  document.addEventListener("click", handleClickOutside);
});

onBeforeUnmount(() => {
  document.removeEventListener("click", handleClickOutside);
  if (debounceTimer) {
    clearTimeout(debounceTimer);
  }
  destroyCanvasListeners();
});
</script>
