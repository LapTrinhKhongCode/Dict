<template>
  <div ref="searchContainer" class="relative w-full">
    <div class="flex items-center border rounded-2xl px-3 py-2 w-full">
      <button
        @click="onSearch"
        class="text-gray-500 hover:text-gray-700 transition mr-3"
      >
        <UIcon name="i-lucide-search" class="size-5" />
      </button>
      <input
        v-model="internalSearchWord"
        type="text"
        placeholder="Enter word..."
        class="flex-grow bg-transparent outline-none text-base"
        @keyup.enter="onSearch"
        @focus="onInputFocus"
        @keydown="handleKeydown"
      />

      <button
        ref="penButtonRef" @click="onPenClick"
        class="text-gray-500 hover:text-gray-700 transition ml-3"
        :class="{ 'text-blue-500': showDrawingPad }"
      >
        <UIcon name="i-lucide-pen" class="size-5" />
      </button>


     <button
        @click="onImageClick"
        class="text-gray-500 hover:text-gray-700 transition ml-2"
        :class="{ 'text-blue-500': showOcrPad }"
      >
        <UIcon name="i-lucide-image" class="size-5" />
      </button>
    </div>

    <div
      v-if="showSuggestions && suggestions.length > 0"
      class="absolute z-10 w-full mt-1 bg-gray-800 border border-gray-700 rounded-lg shadow-lg max-h-80 overflow-y-auto suggestions-list"
    >
      <ul ref="suggestionsListEl">
        <li
          v-for="(suggestion, index) in suggestions"
          :key="suggestion.word + suggestion.reading"
          class="px-4 py-3 border-b border-gray-700 last:border-b-0 cursor-pointer hover:bg-gray-700"
          :class="{ 'bg-gray-700': index === selectedIndex }"
          @click="onSelectSuggestion(suggestion)"
        >
          <div class="flex items-baseline gap-x-2">
            <span class="font-medium text-white">{{ suggestion.word }}</span>
            <span class="text-sm text-blue-400">{{ suggestion.reading }}</span>
          </div>
          <p class="text-sm text-gray-300 mt-1">
            {{ suggestion.meaning }}
          </p>
        </li>
      </ul>
    </div>

    <div
      v-if="showDrawingPad"
      class="absolute z-10 w-full mt-1 bg-gray-800 border border-gray-700 rounded-lg shadow-lg p-4"
    >
      <div class="min-h-[48px] mb-3 flex items-center justify-center">
        <div
          v-if="isPredicting"
          class="flex items-center justify-center h-full"
        >
          <div
            class="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-400"
          ></div>
        </div>

        <div
          v-else-if="predictionLabels.length > 0"
          class="flex flex-wrap justify-center gap-2"
        >
          <button
            v-for="label in predictionLabels"
            :key="label"
            class="px-3 py-1 bg-gray-700 text-white rounded-md text-center font-medium hover:bg-gray-600 transition"
            @click="onSelectPrediction(label)"
            >
            {{ label }}
          </button>
        </div>

        <p
          v-else
          class="text-sm text-gray-500 text-center py-2"
        >
          Draw a character to see suggestions.
        </p>
      </div>

      <canvas
        ref="canvasRef"
        width="256"
        height="256"
        class="handwriting-canvas"
        style="width: 256px; height: 256px; margin: 0 auto;"
        :style="{ 'pointer-events': isPredicting ? 'none' : 'auto', opacity: isPredicting ? 0.7 : 1 }"
      ></canvas>

      <div class="flex justify-center items-center gap-x-4 mt-4">
        <button
          @click.prevent="undo"
          :disabled="historyIndex <= 0 || isPredicting"
          class="text-gray-300 hover:text-white disabled:text-gray-600 disabled:cursor-not-allowed"
        >
          <UIcon name="i-lucide-undo-2" class="size-5" />
        </button>
        <button
          @click.prevent="redo"
          :disabled="historyIndex >= history.length - 1 || isPredicting"
          class="text-gray-300 hover:text-white disabled:text-gray-600 disabled:cursor-not-allowed"
        >
          <UIcon name="i-lucide-redo-2" class="size-5" />
        </button>

        <div class="border-l border-gray-600 h-6"></div>

        <button
          @click.prevent="clearCanvas"
          :disabled="isPredicting"
          class="text-gray-300 hover:text-white disabled:text-gray-600"
        >
          Clear
        </button>
      </div>
    </div>
    <div
      v-if="showOcrPad"
      class="absolute z-10 w-full mt-1 bg-gray-800 border border-gray-700 rounded-lg shadow-lg p-4"
    >
      <div class="flex" style="min-height: 250px">
        <div class="w-1/2 pr-3 border-r border-gray-600 flex flex-col">
          <h4 class="text-xs font-semibold text-gray-400 uppercase mb-2">
            Recognized Text
          </h4>
          
          <div
            ref="ocrResultsRef"
            class="flex-grow max-h-[210px] overflow-y-auto suggestions-list pr-2"
          >
            <p
              v-if="ocrResults.length === 0 && !isOcrLoading && !ocrStatus"
              class="text-sm text-gray-500"
            >
              Upload an image to see results...
            </p>
            
            <div class="flex flex-col gap-1">
              <p
                v-for="(result, index) in ocrResults"
                :key="index"
                class="w-full text-left p-2 bg-gray-700 text-white rounded-md hover:bg-gray-600 transition"
              >
                {{ result.text }}
              </p>
            </div>
          </div>
          
          <div class="pt-2 mt-auto border-t border-gray-700 min-h-[2.5rem]">
            <div v-if="isOcrLoading" class="flex items-center gap-2">
              <div
                class="animate-spin rounded-full h-4 w-4 border-b-2 border-blue-400"
              ></div>
              <span class="text-sm text-gray-400">{{ ocrStatus }}</span>
            </div>
            <p v-else-if="ocrStatus" class="text-sm text-gray-400">
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
            class="w-full h-full flex flex-col items-center justify-center border-2 border-dashed border-gray-600 rounded-lg cursor-pointer hover:border-gray-500 transition"
            :class="{ 'border-blue-500 bg-gray-700': isDragging }"
          >
            <UIcon name="i-lucide-upload-cloud" class="size-10 text-gray-500 mb-3" />
            <p class="text-sm text-gray-400">
              <span class="font-semibold text-blue-400">Click to upload</span>
              or drag and drop
            </p>
            <p class="text-xs text-gray-500">Image file (PNG, JPG, etc.)</p>
          </div>
          
          <div
            v-else
            class="w-full h-full flex flex-col items-center justify-center border-2 border-dashed border-gray-700 rounded-lg"
          >
            <p class="text-sm text-gray-400">Processing...</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* NEW: Dark background for the canvas */
.handwriting-canvas {
  border: 2px solid #4b5563; /* gray-600 */
  border-radius: 8px;
  cursor: crosshair;
  background-color: #1f2937; /* gray-800 */
}

.suggestions-list::-webkit-scrollbar,
.ocr-results-panel::-webkit-scrollbar {
  width: 8px;
}
.suggestions-list::-webkit-scrollbar-track,
.ocr-results-panel::-webkit-scrollbar-track {
  background: #1f2937;
  border-radius: 10px;
}
.suggestions-list::-webkit-scrollbar-thumb,
.ocr-results-panel::-webkit-scrollbar-thumb {
  background-color: #4b5563;
  border-radius: 10px;
  border: 2px solid #1f2937;
  background-clip: padding-box;
}
.suggestions-list::-webkit-scrollbar-thumb:hover,
.ocr-results-panel::-webkit-scrollbar-thumb:hover {
  background-color: #6b7280;
}
</style>

<script setup lang="ts">
import { ref, watch, onMounted, onBeforeUnmount, nextTick } from "vue";
import { toKana } from "wanakana";

// --- Props & Emits ---
const props = defineProps({
  modelValue: {
    type: String,
    default: "",
  },
});
const emit = defineEmits(["update:modelValue", "search"]);

const config = useRuntimeConfig();

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

// --- NEW: OCR Pad State ---
const showOcrPad = ref(false);
const ocrResultsRef = ref<HTMLDivElement | null>(null);
const ocrFileInputRef = ref<HTMLInputElement | null>(null);
const ocrResults = ref<string[]>([]); // For now, an array of strings
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
  }
);

// --- Watcher for Autocomplete ---
watch(internalSearchWord, (newValue) => {
  emit("update:modelValue", newValue);

  // If update came from code (e.g., prediction click), stop.
  if (isProgrammaticUpdate.value) {
    isProgrammaticUpdate.value = false;
    return;
  }

  // If user types, hide the drawing pad
  // if (showDrawingPad.value) {
  //   showDrawingPad.value = false;
  // }

  if (showDrawingPad.value) {
    showDrawingPad.value = false;
  }
  if (showOcrPad.value) { // <-- ADD THIS
    showOcrPad.value = false;
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

  debounceTimer = setTimeout(async () => {
    if (abortController.value) {
      abortController.value.abort();
    }
    abortController.value = new AbortController();
    const signal = abortController.value.signal;

    try {
      const convertedWord = toKana(trimmed);
      const res = await fetch(
        `${
          config.public.apiBaseUrl
        }/api/Search/autocomplete/${encodeURIComponent(convertedWord)}`,
        { signal }
      );
      if (!res.ok) throw new Error("Autocomplete fetch failed");

      const data = await res.json();
      suggestions.value = data || [];

      // Check that drawing pad isn't open *before* showing suggestions
      if (!showDrawingPad.value) {
        showSuggestions.value = suggestions.value.length > 0;
      }
    } catch (e: any) {
      if (e.name === "AbortError") {
        console.log("Autocomplete fetch aborted.");
        return;
      }
      console.error("Autocomplete error:", e);
      suggestions.value = [];
      showSuggestions.value = false;
    }
  }, 350);
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
  showSuggestions.value = suggestions.value.length > 0;
  showDrawingPad.value = false;
  showOcrPad.value = false;
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
  if (
    searchContainer.value &&
    !searchContainer.value.contains(event.target as Node)
  ) {
    showSuggestions.value = false;
    showDrawingPad.value = false;
    showOcrPad.value = false;
    selectedIndex.value = -1;
  }
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

const initializeCanvas = () => {
  if (!canvasRef.value) return;
  ctx.value = canvasRef.value.getContext("2d");
  if (!ctx.value) return;

  // Set pen styles for a dark background
  ctx.value.strokeStyle = "white"; // Draw in white
  ctx.value.lineWidth = 12;
  ctx.value.lineCap = "round";
  ctx.value.lineJoin = "round";

  clearCanvas(); // Set initial background

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

const clearCanvas = () => {
  if (!ctx.value || !canvasRef.value) return;
  ctx.value.fillStyle = "#1f2937"; // bg-gray-800
  ctx.value.fillRect(
    0,
    0,
    canvasRef.value.width,
    canvasRef.value.height
  );

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
    canvasRef.value.height
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


  // Concatenate the label to the existing search word
  internalSearchWord.value = internalSearchWord.value + label;

  // Clear the canvas and predictions
  clearCanvas();

  penButtonRef.value?.focus();
};

const getMatrixFromCanvas = () => {
  const processingCanvas = document.createElement("canvas");
  processingCanvas.width = 64;
  processingCanvas.height = 64;
  const pCtx = processingCanvas.getContext("2d");
  if (!pCtx) return [];

  pCtx.fillStyle = "#1f2937";
  pCtx.fillRect(0, 0, 64, 64);
  if (canvasRef.value) {
    pCtx.drawImage(canvasRef.value, 0, 0, 256, 256, 0, 0, 64, 64);
  }

  const imageData = pCtx.getImageData(0, 0, 64, 64);
  const data = imageData.data;
  const matrix = Array(64)
    .fill(0)
    .map(() => Array(64).fill(0));

  for (let i = 0; i < data.length; i += 4) {
    const r = data[i];
    const g = data[i + 1];
    const b = data[i + 2];
    const grayscale = (r + g + b) / 3;
    const binaryValue = grayscale > 50 ? 1 : 0;

    if (binaryValue === 1) {
      const pixelIndex = i / 4;
      const x = pixelIndex % 64;
      const y = Math.floor(pixelIndex / 64);
      matrix[y][x] = 1;
    }
  }
  return matrix;
};

const runPrediction = async () => {
  isPredicting.value = true;
  predictionLabels.value = [];

  const matrixData = getMatrixFromCanvas();
  const token = localStorage.getItem("jwt_token");
  if (!token) {
    console.error("Prediction error: Not logged in");
    isPredicting.value = false;
    return;
  }

  try {
    const response = await fetch(
      `${config.public.apiBaseUrl}/api/infer/predict`,
      {
        method: "POST",
        headers: {
          Authorization: `Bearer ${token}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ matrix: matrixData }),
      }
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
  } catch (error) {
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
      }
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
    ocrStatus.value = `Error: ${error.message}`;
  } finally {
    isOcrLoading.value = false;
    if (ocrResults.value.length > 0) {
      ocrStatus.value = `Found ${ocrResults.value.length} lines.`;
    } else if (!ocrStatus.value.startsWith("Error")) {
      ocrStatus.value = "No text found.";
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
