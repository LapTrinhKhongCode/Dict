<template>
  <div class="flex h-screen bg-gray-50 overflow-hidden font-sans">
    <div
      class="w-[60%] h-full flex flex-col border-r border-gray-200 bg-gray-100 relative"
    >
      <div
        class="h-14 bg-white border-b border-gray-200 flex items-center px-4 justify-between shadow-sm z-10 shrink-0"
      >
        <div class="font-semibold text-gray-700 truncate">
          Chi tiết tài liệu
        </div>
        <div class="flex gap-2 items-center">
          <!-- Annotation toolbar -->
          <div class="flex gap-1 items-center border border-gray-200 rounded-lg px-2 py-1 bg-gray-50">
            <button
              v-for="t in tools"
              :key="t.id"
              @click="selectTool(t.id)"
              :title="t.label"
              :class="['w-7 h-7 rounded flex items-center justify-center text-base transition', activeTool === t.id ? 'bg-blue-100 text-blue-600' : 'hover:bg-gray-200 text-gray-600']"
            >{{ t.icon }}</button>
            <div class="w-px h-5 bg-gray-300 mx-1"></div>
            <input type="color" v-model="penColor" title="Màu" class="w-6 h-6 rounded cursor-pointer border-0 p-0" :disabled="activeTool === 'eraser'" />
            <select v-model="penWidth" class="text-xs border border-gray-200 rounded px-1 py-0.5 bg-white ml-1">
              <option value="2">Mảnh</option>
              <option value="5">Vừa</option>
              <option value="10">Dày</option>
            </select>
          </div>
          <button
            class="px-3 py-1 bg-blue-50 text-blue-600 hover:bg-blue-100 rounded text-sm font-medium transition"
          >
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
        <span class="text-sm font-semibold text-gray-700"
          >Trang {{ currentPage }} / {{ totalPages }}</span
        >
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

          <!-- Annotation overlay canvas -->
          <canvas
            ref="annotationCanvas"
            class="absolute inset-0 w-full h-full"
            :style="{ cursor: cursorStyle, pointerEvents: activeTool ? 'auto' : 'none' }"
            @mousedown.stop="onDrawStart"
            @mousemove.stop="onDrawMove"
            @mouseup.stop="onDrawEnd"
            @mouseleave.stop="onDrawEnd"
          ></canvas>

          <div
            class="absolute inset-0 pointer-events-auto"
            :style="{ pointerEvents: activeTool ? 'none' : 'auto' }"
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

    <div class="w-[40%] h-full flex flex-col bg-white">
      <div class="flex border-b border-gray-200 bg-gray-50 shrink-0">
        <button
          @click="activeTab = 'dict'"
          :class="[
            'flex-1 py-4 text-sm font-semibold border-b-2 transition-colors',
            activeTab === 'dict'
              ? 'border-blue-600 text-blue-600 bg-white'
              : 'border-transparent text-gray-500 hover:text-gray-700',
          ]"
        >
          📖 Tra Từ Điển
        </button>
        <button
          @click="activeTab = 'ai'"
          :class="[
            'flex-1 py-4 text-sm font-semibold border-b-2 transition-colors',
            activeTab === 'ai'
              ? 'border-blue-600 text-blue-600 bg-white'
              : 'border-transparent text-gray-500 hover:text-gray-700',
          ]"
        >
          ✨ Trợ lý AI
        </button>
      </div>

      <div
        v-if="activeTab === 'dict'"
        class="flex-1 flex flex-col overflow-hidden bg-white"
      >
        <div class="p-4 border-b border-gray-100">
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Bôi đen chữ bên trái hoặc gõ vào đây..."
            class="w-full p-3 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 outline-none text-base"
          />
        </div>
        <div class="flex-1 p-6 overflow-y-auto">
          <div v-if="!searchQuery" class="text-center text-gray-400 mt-10">
            Hãy bôi đen một từ trong tài liệu để tra cứu ngay lập tức.
          </div>
          <div v-else class="space-y-4">
            <h1 class="text-3xl font-bold text-gray-900">{{ searchQuery }}</h1>
            <p class="text-gray-600">
              Đang tìm kiếm ý nghĩa trong ngữ cảnh dự án...
            </p>
          </div>
        </div>
      </div>

      <div
        v-if="activeTab === 'ai'"
        class="flex-1 flex flex-col overflow-hidden"
      >
        <div class="flex-1 p-4 overflow-y-auto space-y-4 bg-gray-50">
          <div
            class="bg-blue-50 p-3 rounded-lg text-sm text-gray-800 shadow-sm border border-blue-100 w-5/6"
          >
            Xin chào! Bạn cần tôi tóm tắt tài liệu này hay giải thích một đoạn
            cụ thể nào đó? (Hãy bôi đen đoạn cần hỏi nhé).
          </div>
          <div
            v-if="userMessage"
            class="bg-white p-3 rounded-lg text-sm text-gray-800 shadow-sm border border-gray-200 w-5/6 ml-auto self-end"
          >
            {{ userMessage }}
          </div>
        </div>
        <div class="p-4 border-t border-gray-200 bg-white shrink-0">
          <div class="flex items-center bg-gray-100 rounded-full px-4 py-2">
            <input
              v-model="chatInput"
              type="text"
              placeholder="Hỏi trợ lý AI..."
              class="flex-1 bg-transparent border-none focus:ring-0 text-sm outline-none"
              @keyup.enter="sendAiMessage"
            />
            <button @click="sendAiMessage" class="text-blue-600 font-bold ml-2">
              Gửi
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, shallowRef, onMounted, watch, nextTick, computed } from "vue";
import * as pdfjsLib from "pdfjs-dist/legacy/build/pdf";
import workerUrl from "pdfjs-dist/build/pdf.worker.min.mjs?url";
pdfjsLib.GlobalWorkerOptions.workerSrc = workerUrl;

// --- Props ---
const props = defineProps({
  imageUrl: { type: String, required: true },
  results: { type: Array, default: () => [] },
  projectId: { type: [String, Number], default: null },
  ocrJobId: { type: [String, Number], default: null },
});

// --- States UI ---
const activeTab = ref("dict");
const searchQuery = ref("");
const chatInput = ref("");
const userMessage = ref("");

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

// --- Annotation states ---
const annotationCanvas = ref(null);
const activeTool = ref("pen"); // pen | highlight | eraser | null
const penColor = ref("#e53e3e");
const penWidth = ref(5);
const isDrawing = ref(false);
const currentStroke = ref(null);
const allStrokes = ref({}); // key: pageNumber → array of strokes

const tools = [
  { id: "pen", icon: "✏️", label: "Bút vẽ" },
  { id: "highlight", icon: "🖍️", label: "Highlight" },
  { id: "eraser", icon: "🧹", label: "Tẩy" },
];

const cursorStyle = computed(() => {
  if (activeTool.value === "eraser") return "cell";
  if (activeTool.value) return "crosshair";
  return "default";
});

function selectTool(tool) {
  activeTool.value = activeTool.value === tool ? null : tool;
}

// --- WATCHERS & MOUNTED ---
onMounted(() => {
  checkAndRenderMedia();
});

watch(
  () => props.imageUrl,
  () => {
    currentPage.value = 1;
    scannedPagesCache.value = {};
    allStrokes.value = {};
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

// --- LOGIC PDF ---
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

  // Sync annotation canvas size
  await nextTick();
  resizeAnnotationCanvas();
  await loadAnnotations(pageNum);

  if (scannedPagesCache.value[pageNum] && Array.isArray(scannedPagesCache.value[pageNum])) {
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

// --- ANNOTATION CANVAS ---
function resizeAnnotationCanvas() {
  const ac = annotationCanvas.value;
  const pc = pdfCanvas.value;
  if (!ac || !pc) return;
  ac.width = pc.width;
  ac.height = pc.height;
  redrawAnnotations(currentPage.value);
}

function getCanvasPos(e) {
  const ac = annotationCanvas.value;
  const rect = ac.getBoundingClientRect();
  const scaleX = ac.width / rect.width;
  const scaleY = ac.height / rect.height;
  return {
    x: (e.clientX - rect.left) * scaleX,
    y: (e.clientY - rect.top) * scaleY,
  };
}

function onDrawStart(e) {
  if (!activeTool.value) return;
  isDrawing.value = true;
  const pos = getCanvasPos(e);

  if (activeTool.value === "eraser") {
    eraseAt(pos);
    return;
  }

  currentStroke.value = {
    tool: activeTool.value,
    color: activeTool.value === "highlight" ? penColor.value + "66" : penColor.value,
    width: activeTool.value === "highlight" ? Number(penWidth.value) * 3 : Number(penWidth.value),
    points: [pos],
  };
}

function onDrawMove(e) {
  if (!isDrawing.value || !activeTool.value) return;
  const pos = getCanvasPos(e);

  if (activeTool.value === "eraser") {
    eraseAt(pos);
    return;
  }

  if (currentStroke.value) {
    currentStroke.value.points.push(pos);
    redrawAnnotations(currentPage.value);
    drawStroke(currentStroke.value);
  }
}

function onDrawEnd() {
  if (!isDrawing.value) return;
  isDrawing.value = false;

  if (activeTool.value === "eraser") {
    saveAnnotations();
    return;
  }

  if (currentStroke.value && currentStroke.value.points.length > 1) {
    if (!allStrokes.value[currentPage.value]) allStrokes.value[currentPage.value] = [];
    allStrokes.value[currentPage.value].push(currentStroke.value);
    saveAnnotations();
  }
  currentStroke.value = null;
}

function eraseAt(pos) {
  const strokes = allStrokes.value[currentPage.value];
  if (!strokes) return;
  const eraseRadius = 15;
  allStrokes.value[currentPage.value] = strokes.filter(stroke => {
    return !stroke.points.some(pt =>
      Math.hypot(pt.x - pos.x, pt.y - pos.y) < eraseRadius
    );
  });
  redrawAnnotations(currentPage.value);
}

function drawStroke(stroke) {
  const ac = annotationCanvas.value;
  if (!ac) return;
  const ctx = ac.getContext("2d");
  const pts = stroke.points;
  if (pts.length < 2) return;

  ctx.save();
  ctx.strokeStyle = stroke.color;
  ctx.lineWidth = stroke.width;
  ctx.lineCap = "round";
  ctx.lineJoin = "round";
  if (stroke.tool === "highlight") ctx.globalAlpha = 0.4;

  ctx.beginPath();
  ctx.moveTo(pts[0].x, pts[0].y);
  for (let i = 1; i < pts.length; i++) {
    ctx.lineTo(pts[i].x, pts[i].y);
  }
  ctx.stroke();
  ctx.restore();
}

function redrawAnnotations(pageNum) {
  const ac = annotationCanvas.value;
  if (!ac) return;
  const ctx = ac.getContext("2d");
  ctx.clearRect(0, 0, ac.width, ac.height);
  const strokes = allStrokes.value[pageNum] || [];
  for (const stroke of strokes) drawStroke(stroke);
}

// --- API: Load & Save annotations ---
async function loadAnnotations(pageNum) {
  if (!props.projectId) return;
  try {
    const config = useRuntimeConfig();
    const token = localStorage.getItem("jwt_token") || "";
    const base = config.public.apiBaseUrl || "https://localhost:7084";
    const jobParam = props.ocrJobId ? `&ocrJobId=${props.ocrJobId}` : "";
    const res = await fetch(`${base}/api/projects/${props.projectId}/annotations?pageNumber=${pageNum}${jobParam}`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    if (!res.ok) return;
    const data = await res.json();
    // Merge all users' strokes
    const merged = [];
    for (const ann of data) {
      try {
        const strokes = JSON.parse(ann.data);
        if (Array.isArray(strokes)) merged.push(...strokes);
      } catch {}
    }
    allStrokes.value[pageNum] = merged;
    redrawAnnotations(pageNum);
  } catch (e) {
    console.warn("Không load được annotations:", e);
  }
}

let saveTimer = null;
function saveAnnotations() {
  if (!props.projectId) return;
  clearTimeout(saveTimer);
  saveTimer = setTimeout(async () => {
    try {
      const config = useRuntimeConfig();
      const token = localStorage.getItem("jwt_token") || "";
      const base = config.public.apiBaseUrl || "https://localhost:7084";
      await fetch(`${base}/api/projects/${props.projectId}/annotations`, {
        method: "POST",
        headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
        body: JSON.stringify({
          ocrJobId: props.ocrJobId ? Number(props.ocrJobId) : null,
          pageNumber: currentPage.value,
          data: JSON.stringify(allStrokes.value[currentPage.value] || []),
        }),
      });
    } catch (e) {
      console.warn("Lỗi lưu annotation:", e);
    }
  }, 800);
}

// --- OCR PRELOAD ---
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
      await page.render({ canvasContext: offContext, viewport: viewport }).promise;
      offScreenCanvas.toBlob(
        async (blob) => {
          const formData = new FormData();
          formData.append("image", blob, `page_${i}.jpg`);
          const config = useRuntimeConfig();
          const token = localStorage.getItem("jwt_token");
          const response = await fetch(
            `${config.public.apiBaseUrl || "https://localhost:7084"}/api/Infer/scan-page-ocr`,
            { method: "POST", headers: { Authorization: `Bearer ${token}` }, body: formData },
          );
          if (response.ok) {
            const data = await response.json();
            scannedPagesCache.value[i] = data.results;
            if (currentPage.value === i) localResults.value = data.results;
          } else {
            scannedPagesCache.value[i] = null;
          }
        },
        "image/jpeg",
        0.9,
      );
    } catch (err) {
      scannedPagesCache.value[i] = null;
    }
  }
}

// --- IMAGE ---
function onImageLoad(e) {
  if (e.target.naturalWidth) {
    nativeWidth.value = e.target.naturalWidth;
    nativeHeight.value = e.target.naturalHeight;
  }
}

function calculateTextStyle(item) {
  if (!item.boundingBox || nativeWidth.value === 0 || nativeHeight.value === 0)
    return { display: "none" };
  let bbox = [];
  try {
    const arr = typeof item.boundingBox === "string" ? JSON.parse(item.boundingBox) : item.boundingBox;
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
    backgroundColor: "rgba(255, 0, 0, 0.2)",
    color: "rgba(255, 0, 0, 1)",
  };
}

function handleTextSelection() {
  if (activeTool.value) return; // Đừng bôi đen khi đang vẽ
  const selectedText = window.getSelection().toString().trim();
  if (selectedText) {
    searchQuery.value = selectedText;
    activeTab.value = "dict";
  }
}

function sendAiMessage() {
  if (!chatInput.value.trim()) return;
  userMessage.value = chatInput.value;
  chatInput.value = "";
}
</script>

<style scoped>
::selection {
  background: rgba(59, 130, 246, 0.4);
  color: transparent;
}
</style>
