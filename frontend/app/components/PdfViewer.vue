<template>
  <div class="flex flex-col h-full bg-[#1c2128] w-full relative">
    <!-- ACCESS DENIED OVERLAY -->
    <div
      v-if="accessDenied"
      class="absolute inset-0 z-50 flex flex-col items-center justify-center bg-[#161b22] text-[#c9d1d9] p-6 text-center"
    >
      <div
        class="w-20 h-20 bg-red-900/30 text-red-500 rounded-full flex items-center justify-center mb-6"
      >
        <svg
          class="w-10 h-10"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
          />
        </svg>
      </div>
      <h2 class="text-2xl font-bold text-white mb-2">Không khả dụng</h2>
      <p class="text-gray-400 mb-6 max-w-md">
        Bạn chưa đăng nhập hoặc tài liệu này thuộc về một Workspace mà bạn không
        có quyền truy cập.
      </p>
      <button
        @click="$router.push('/workspaces')"
        class="px-6 py-2.5 bg-[#f0c040] text-black font-bold rounded-lg hover:bg-[#e3b330] transition-colors"
      >
        Quay lại trang chủ
      </button>
    </div>

    <!-- TOOLBAR -->
    <div
      v-if="!accessDenied"
      class="flex items-center gap-2 p-2 bg-[#161b22] border-b border-[#30363d] shrink-0 text-[#c9d1d9] flex-wrap z-10"
    >
      <div
        class="flex bg-[#21262d] border border-[#30363d] rounded overflow-hidden"
      >
        <button
          :class="[
            'px-3 py-1 hover:bg-[#30363d]',
            viewMode === 'single'
              ? 'bg-[#f0c040] text-black font-semibold'
              : '',
          ]"
          @click="setViewMode('single')"
        >
          Trang đơn
        </button>
        <button
          :class="[
            'px-3 py-1 hover:bg-[#30363d]',
            viewMode === 'scroll'
              ? 'bg-[#f0c040] text-black font-semibold'
              : '',
          ]"
          @click="setViewMode('scroll')"
        >
          Cuộn
        </button>
      </div>
      <div class="w-px h-5 bg-[#30363d] mx-1"></div>
      <template v-if="viewMode === 'single'">
        <button
          class="px-2 py-1 bg-[#21262d] rounded border border-[#30363d]"
          @click="prevPage"
          :disabled="currentPage <= 1"
        >
          ‹
        </button>
        <span class="text-sm px-2">
          <input
            v-model.number="gotoPage"
            type="number"
            class="w-10 text-center bg-[#161b22] border border-[#30363d] rounded outline-none"
            @change="jumpToPage"
          />
          / {{ totalPages }}
        </span>
        <button
          class="px-2 py-1 bg-[#21262d] rounded border border-[#30363d]"
          @click="nextPage"
          :disabled="currentPage >= totalPages"
        >
          ›
        </button>
      </template>
      <template v-else>
        <span class="text-sm px-2"
          >Trang {{ currentPage }} / {{ totalPages }}</span
        >
      </template>
      <div class="w-px h-5 bg-[#30363d] mx-1"></div>
      <button
        class="px-2 py-1 bg-[#21262d] rounded border border-[#30363d]"
        @click="zoomOut"
      >
        −
      </button>
      <span class="text-sm w-12 text-center"
        >{{ Math.round(scale * 100) }}%</span
      >
      <button
        class="px-2 py-1 bg-[#21262d] rounded border border-[#30363d]"
        @click="zoomIn"
      >
        +
      </button>
      <button
        class="px-2 py-1 bg-[#21262d] rounded border border-[#30363d] ml-1"
        @click="fitWidth"
        title="Vừa chiều rộng"
      >
        ⟺
      </button>
      <!-- Annotation Toolbar -->
      <div class="w-px h-5 bg-[#30363d] mx-1"></div>
      <div class="flex items-center gap-1">
        <button
          v-for="t in annotTools"
          :key="t.id"
          @click="selectAnnotTool(t.id)"
          :title="t.label"
          :class="['px-2 py-1 rounded text-sm transition', activeTool === t.id ? 'bg-[#f0c040] text-black font-bold' : 'bg-[#21262d] border border-[#30363d] hover:bg-[#30363d]']"
        >{{ t.icon }}</button>
        <input
          v-if="activeTool && activeTool !== 'eraser'"
          type="color"
          v-model="penColor"
          title="Màu bút"
          class="w-7 h-7 cursor-pointer border-0 rounded bg-transparent"
        />
        <select
          v-if="activeTool"
          v-model.number="penWidth"
          class="text-xs bg-[#21262d] border border-[#30363d] rounded px-1 py-0.5 text-[#c9d1d9]"
        >
          <option :value="2">Mảnh</option>
          <option :value="5">Vừa</option>
          <option :value="10">Dày</option>
        </select>
        <!-- Save indicator -->
        <span
          v-if="annotSaveStatus"
          class="text-xs ml-1 transition-opacity"
          :class="annotSaveStatus === 'saving' ? 'text-yellow-400' : 'text-green-400'"
        >
          {{ annotSaveStatus === 'saving' ? '⏳ Đang lưu...' : '✓ Đã lưu' }}
        </span>
      </div>
      <button
        @click="exportToSearchablePdf"
        :disabled="isExporting || ocrLoading"
        :class="[
          'px-3 py-1.5 rounded flex items-center gap-1.5 text-sm font-semibold transition-colors',
          isExporting || ocrLoading
            ? 'bg-[#30363d] text-gray-500 cursor-wait'
            : 'bg-[#2ea043] hover:bg-[#2c974b] text-white',
        ]"
        title="Xuất Searchable PDF"
      >
        <svg
          v-if="isExporting"
          class="w-4 h-4 animate-spin text-white"
          fill="none"
          viewBox="0 0 24 24"
        >
          <circle
            class="opacity-25"
            cx="12"
            cy="12"
            r="10"
            stroke="currentColor"
            stroke-width="4"
          ></circle>
          <path
            class="opacity-75"
            fill="currentColor"
            d="M4 12a8 8 0 018-8v8H4z"
          ></path>
        </svg>
        <svg
          v-else
          class="w-4 h-4"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="2"
            d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"
          />
        </svg>
        {{ isExporting ? "Đang xử lý..." : "Xuất PDF" }}
      </button>
    </div>

    <!-- MAIN CONTENT -->
    <!-- Đã bỏ 'flex', 'flex-col', 'items-center' để tránh xung đột cắt mất góc màn hình -->
    <div
      v-if="!accessDenied"
      class="flex-1 overflow-auto relative pdf-scroll-area p-4"
      ref="pdfScrollAllEl"
      @mouseup="handleTextSelection"
    >
      <div class="min-w-full block pb-12">
        <div
          v-if="!pdfDoc && !ocrMode"
          class="flex flex-col items-center justify-center text-gray-400 mt-20"
        >
          <div
            class="w-8 h-8 border-4 border-gray-600 border-t-[#f0c040] rounded-full animate-spin mb-4"
          ></div>
          <p>Đang tải tài liệu PDF...</p>
        </div>

        <template v-else-if="ocrMode">
          <div
            v-if="ocrImageUrl"
            class="relative no-drag block leading-none mx-auto mb-6"
            :style="{ width: Math.round(scale * 100) + '%' }"
          >
            <img
              :src="ocrImageUrl"
              ref="ocrImgEl"
              @load="onOcrImageLoad"
              draggable="false"
              class="block shadow-2xl w-full"
              style="pointer-events: none; user-select: none"
            />

            <div
              v-if="ocrLoading"
              class="absolute inset-0 z-30 bg-[#161b22]/40 backdrop-blur-[2px] flex flex-col items-center justify-center text-white transition-opacity"
            >
              <div
                class="w-8 h-8 border-4 border-gray-400 border-t-[#f0c040] rounded-full animate-spin mb-3 shadow-lg"
              ></div>
              <p
                class="text-sm font-semibold drop-shadow-md bg-black/50 px-4 py-1.5 rounded-full"
              >
                AI đang phân tích ký tự ngầm...
              </p>
            </div>

            <div
              v-if="!ocrLoading && ocrResults && ocrDisplayW > 0"
              class="absolute inset-0 overflow-hidden ocr-text-layer z-20"
            >
              <span
                v-for="(r, i) in ocrResults"
                :key="i"
                class="absolute cursor-text pointer-events-auto select-text ocr-word"
                :style="getOcrTextStyle(r)"
              >
                {{ r.wordText }}
              </span>
            </div>
            <!-- Annotation canvas for OCR mode -->
            <canvas
              ref="ocrAnnotCanvas"
              class="absolute inset-0 z-30"
              :style="{ pointerEvents: activeTool ? 'auto' : 'none', cursor: activeTool === 'eraser' ? 'cell' : activeTool ? 'crosshair' : 'default' }"
              @pointerdown="e => onAnnotPointerDown(e, 1)"
              @pointermove="e => onAnnotPointerMove(e, 1)"
              @pointerup="e => onAnnotPointerUp(e, 1)"
              @pointerleave="e => onAnnotPointerUp(e, 1)"
            ></canvas>
          </div>
        </template>

        <template v-else>
          <template v-for="n in totalPages" :key="n">
            <div
              v-if="viewMode === 'scroll' || n === currentPage"
              :data-page="n"
              :ref="
                (el) => {
                  if (el) pageRefs[n] = el;
                }
              "
              class="relative block mx-auto mb-6 w-max max-w-none"
              :style="{ minHeight: defaultPageHeight + 'px' }"
            >
              <div
                v-if="pageRendered[n] || viewMode === 'single'"
                class="relative shadow-2xl bg-white leading-none text-left"
                style="overflow: hidden"
              >
                <canvas
                  :ref="
                    (el) => {
                      if (el) pageCanvases[n] = el;
                      else delete pageCanvases[n];
                    }
                  "
                  class="block relative z-0"
                ></canvas>

                <div
                  :ref="
                    (el) => {
                      if (el) textLayerRefs[n] = el;
                      else delete textLayerRefs[n];
                    }
                  "
                  class="absolute inset-0 z-10 textLayer"
                ></div>

                <div
                  v-if="pageOcrResults[n] && pageOcrResults[n].length > 0"
                  class="absolute inset-0 z-20 ocr-text-layer"
                >
                  <span
                    v-for="(r, i) in pageOcrResults[n]"
                    :key="'ocr-' + n + '-' + i"
                    class="absolute cursor-text pointer-events-auto select-text ocr-word"
                    :style="getOcrTextStyleForPdf(r, n)"
                  >
                    {{ r.wordText }}
                  </span>
                </div>
                <!-- Annotation canvas overlay -->
                <canvas
                  :ref="el => { if (el) annotCanvases[n] = el; else delete annotCanvases[n]; }"
                  class="absolute inset-0 z-30"
                  :style="{ pointerEvents: activeTool ? 'auto' : 'none', cursor: activeTool === 'eraser' ? 'cell' : activeTool ? 'crosshair' : 'default' }"
                  @pointerdown="e => onAnnotPointerDown(e, n)"
                  @pointermove="e => onAnnotPointerMove(e, n)"
                  @pointerup="e => onAnnotPointerUp(e, n)"
                  @pointerleave="e => onAnnotPointerUp(e, n)"
                ></canvas>
              </div>

              <div
                v-else-if="viewMode === 'scroll'"
                class="w-[800px] max-w-full bg-white/5 border border-dashed border-gray-700 flex items-center justify-center rounded mx-auto"
                :style="{ height: defaultPageHeight + 'px' }"
              >
                <span class="text-gray-500 text-sm">Trang {{ n }}</span>
              </div>
            </div>
          </template>
        </template>
      </div>
    </div>
  </div>
</template>

<script setup>
import {
  ref,
  shallowRef,
  nextTick,
  onMounted,
  onBeforeUnmount,
  watch,
} from "vue";
import { useRuntimeConfig, useRoute, useRouter } from "#imports";
import { useJwt } from "~/composables/useJwt";
import { useDocumentHub } from "~/composables/useDocumentHub";
import { useOcrResultsState } from "~/composables/useLookupState";

import * as pdfjsLib from "pdfjs-dist/legacy/build/pdf";
import { TextLayer } from "pdfjs-dist";
import "pdfjs-dist/web/pdf_viewer.css";

import workerUrl from "pdfjs-dist/build/pdf.worker.min.mjs?url";
pdfjsLib.GlobalWorkerOptions.workerSrc = workerUrl;

const ocrResultsState = useOcrResultsState();

const props = defineProps({
  fileUrl: { type: String, required: false },
  fileData: { type: Uint8Array, required: false },
  jobId: { type: [String, Number], required: false },
  apiKey: { type: String, required: true },
  projectId: { type: [String, Number], required: false }, // THÊM DÒNG NÀY
});

const emit = defineEmits([
  "text-selected",
  "rag-updated",
  "page-changed",
  "media-id-loaded",
  "access-denied",
]);
const isExporting = ref(false);
const config = useRuntimeConfig();
const route = useRoute();
const router = useRouter();
const { isAuthenticated, userId: currentUserId } = useJwt();
const getToken = () => localStorage.getItem("jwt_token") || "";
const { connect: hubConnect, broadcastStroke, broadcastErase, onStroke, offStroke, onErase, offErase } = useDocumentHub();

const accessDenied = ref(false);

const pdfDoc = shallowRef(null);
const totalPages = ref(0);
const currentPage = ref(1);
const gotoPage = ref(1);
const scale = ref(1.2);
const viewMode = ref("scroll");
const defaultPageHeight = ref(800);

// --- Annotation state ---
const activeTool = ref(null); // 'pen' | 'highlight' | 'eraser' | null
const penColor = ref('#e53e3e');
const penWidth = ref(3);
const annotCanvases = ref({});   // PDF mode: keyed by page number
const ocrAnnotCanvas = ref(null); // OCR mode
const annotStrokesMap = ref({});  // { pageNum: [...allStrokes] }
const annotTools = [
  { id: 'pen', icon: '✏️', label: 'Bút vẽ' },
  { id: 'highlight', icon: '🖍️', label: 'Tô màu' },
  { id: 'eraser', icon: '🧹', label: 'Tẩy' },
];
let annotDrawing = false;
let annotCurrentStroke = null;
const annotSaveTimers = {};
const annotLoadedPages = new Set();
const annotDirtyPages = new Set(); // pages with unsaved changes
const annotSaveStatus = ref(null); // null | 'saving' | 'saved'
let annotSaveStatusTimer = null;

const ocrMode = ref(false);
const ocrImageUrl = ref("");
const ocrResults = shallowRef(null);
const ocrLoading = ref(false);
const ocrImgEl = ref(null);
const ocrNaturalW = ref(0);
const ocrNaturalH = ref(0);
const ocrDisplayW = ref(0);
const ocrDisplayH = ref(0);
const dbOcrResults = ref([]);

const pageRendered = ref({});
const renderQueue = [];
const visiblePagesSet = new Set();
let isRendering = false;
let isDisplayRendering = false;
let scrollObserver = null;
let activeRenderTask = null;

const pageRefs = ref({});
const pageCanvases = ref({});
const textLayerRefs = ref({});
const pdfScrollAllEl = ref(null);

const pageOcrResults = ref({});
const pageUploadStatus = ref({});
const uploadQueue = ref([]);
const pdfJobId = ref(null);
let isUploadRunning = false;
const pageDimensions = ref({});

// SignalR connection cho OCR progress
let signalrConnection = null;

async function setupSignalR(jobId) {
  if (!process.client) return;
  try {
    const { HubConnectionBuilder, LogLevel } = await import("@microsoft/signalr");
    const token = getToken();
    signalrConnection = new HubConnectionBuilder()
      .withUrl(`${config.public.apiBaseUrl}/notificationHub`, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    // Lắng nghe khi backend push OcrCompleted (ảnh đơn)
    signalrConnection.on("OcrCompleted", (payload) => {
      if (payload.jobId !== jobId) return;
      if (payload.status === "completed") {
        ocrLoading.value = false;
        startLoadJob(jobId); // fetch 1 lần để lấy results
      } else if (payload.status === "failed") {
        ocrLoading.value = false;
        console.error("OCR thất bại, thử lại bằng cách bấm lại.");
      }
    });

    // Lắng nghe progress từng trang PDF
    signalrConnection.on("OcrPageCompleted", (payload) => {
      if (payload.jobId !== Number(jobId) && payload.jobId !== jobId) return;
      // Đã có data rồi → đánh dấu nếu cần
    });

    await signalrConnection.start();
    await signalrConnection.invoke("JoinOcrRoom", Number(jobId));
  } catch (err) {
    console.warn("SignalR OCR connect thất bại (fallback polling):", err);
  }
}

// Dành cho ảnh đơn: kết nối SignalR và lắng nghe OcrCompleted
// Trả về true nếu kết nối thành công (không cần polling)
async function trySetupSignalRForImage(jobId) {
  if (!process.client) return false;
  try {
    const { HubConnectionBuilder, LogLevel } = await import("@microsoft/signalr");
    const token = getToken();

    if (signalrConnection) {
      signalrConnection.stop().catch(() => {});
    }

    signalrConnection = new HubConnectionBuilder()
      .withUrl(`${config.public.apiBaseUrl}/notificationHub`, {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    signalrConnection.on("OcrCompleted", (payload) => {
      const id = Number(payload.jobId ?? payload.JobId);
      if (id !== Number(jobId)) return;
      ocrLoading.value = false;
      if (payload.status === "completed" || payload.Status === "completed") {
        startLoadJob(jobId);
      }
    });

    await signalrConnection.start();
    await signalrConnection.invoke("JoinOcrRoom", Number(jobId));
    return true;
  } catch (err) {
    console.warn("SignalR image OCR thất bại, dùng polling:", err);
    return false;
  }
}

watch(
  () => props.jobId,
  (v) => {
    if (v) startLoadJob(v);
  },
);
watch(
  () => props.fileUrl,
  (v) => {
    if (v) loadPdf();
  },
);
watch(
  () => props.fileData,
  (v) => {
    if (v) loadPdf();
  },
);

// FIX 4: Theo dõi ảnh cẩn thận để tránh lỗi cache trình duyệt làm ocrDisplayW bị kẹt số 0
watch(ocrImgEl, (el) => {
  if (el && el.complete) {
    onOcrImageLoad();
  }
});

async function initPdfJob(file) {
  const token = getToken();
  const formData = new FormData();
  formData.append("fileName", file.name);
  formData.append("totalPages", totalPages.value);
  if (props.projectId) formData.append("projectId", props.projectId);

  try {
    const res = await fetch(
      `${config.public.apiBaseUrl}/api/Infer/create-pdf-job`,
      {
        method: "POST",
        headers: { Authorization: `Bearer ${token}` },
        body: formData,
      },
    );
    const data = await res.json();
    pdfJobId.value = data.jobId;
    enqueuePagesAhead(1);
  } catch (err) {
    console.error("Lỗi khởi tạo PDF Job:", err);
  }
}

function enqueuePagesAhead(fromPage) {
  const AHEAD = 2;
  for (
    let p = fromPage;
    p <= Math.min(fromPage + AHEAD, totalPages.value);
    p++
  ) {
    if (!pageUploadStatus.value[p]) {
      pageUploadStatus.value[p] = "queued";
      uploadQueue.value.push(p);
    }
  }
  runUploadQueue();
}

async function runUploadQueue() {
  if (isUploadRunning || uploadQueue.value.length === 0) return;
  isUploadRunning = true;

  const CONCURRENCY = 3; // Upload tối đa 3 trang song song

  while (uploadQueue.value.length > 0) {
    const batch = uploadQueue.value.splice(0, CONCURRENCY);
    const pending = batch.filter(
      (p) =>
        pageUploadStatus.value[p] !== "done" &&
        pageUploadStatus.value[p] !== "cached",
    );
    if (pending.length > 0) {
      await Promise.all(pending.map((p) => uploadOnePage(p)));
    }
  }
  isUploadRunning = false;
}

async function uploadOnePage(pageNum, retryCount = 0) {
  if (!pdfDoc.value || !pdfJobId.value) return;
  pageUploadStatus.value[pageNum] = "uploading";

  try {
    const page = await pdfDoc.value.getPage(pageNum);
    // Scale 1.5 — đủ chất lượng cho Google Vision, nhẹ hơn 2.0 ~44%
    const viewport = page.getViewport({ scale: 1.5 });
    const canvas = document.createElement("canvas");
    canvas.width = viewport.width;
    canvas.height = viewport.height;
    await page.render({ canvasContext: canvas.getContext("2d"), viewport })
      .promise;

    const blob = await new Promise((resolve) =>
      canvas.toBlob(resolve, "image/jpeg", 0.85),
    );

    // Cleanup canvas để tránh memory leak
    canvas.width = 0;
    canvas.height = 0;

    const token = getToken();
    const formData = new FormData();
    formData.append("image", blob, `page_${pageNum}.jpg`);
    formData.append("jobId", pdfJobId.value);
    formData.append("pageNumber", pageNum);

    const res = await fetch(
      `${config.public.apiBaseUrl}/api/Infer/upload-pdf-page`,
      {
        method: "POST",
        headers: { Authorization: `Bearer ${token}` },
        body: formData,
      },
    );

    if (!res.ok) throw new Error(`HTTP ${res.status}`);

    const data = await res.json();
    pageUploadStatus.value[pageNum] = data.status || "done";

    if (data.results && data.results.length > 0) {
      pageOcrResults.value = {
        ...pageOcrResults.value,
        [pageNum]: data.results,
      };
      // pageDimensions dùng scale 1.5 làm hệ quy chiếu
      pageDimensions.value = {
        ...pageDimensions.value,
        [pageNum]: { w: viewport.width, h: viewport.height },
      };
    }
  } catch (err) {
    console.error(`[Upload] Lỗi trang ${pageNum}:`, err);
    if (retryCount < 1) {
      // Retry 1 lần sau 1.5s
      await new Promise((r) => setTimeout(r, 1500));
      return uploadOnePage(pageNum, retryCount + 1);
    }
    pageUploadStatus.value[pageNum] = "error";
  }
}

watch(currentPage, (newPage) => {
  enqueuePagesAhead(newPage);
});

async function checkAccessByProjectId(token, projectId) {
  try {
    const projRes = await fetch(
      `${config.public.apiBaseUrl}/api/projects/${projectId}`,
      { headers: { Authorization: `Bearer ${token}` } },
    );
    if (!projRes.ok) return false;
    const projData = await projRes.json();
    const wsId = projData.result?.workspaceId ?? projData.workspaceId;
    if (!wsId) return false;

    const membersRes = await fetch(
      `${config.public.apiBaseUrl}/api/workspaces/${wsId}/members`,
      { headers: { Authorization: `Bearer ${token}` } },
    );
    if (!membersRes.ok) return false;
    const membersData = await membersRes.json();
    const members = membersData.result ?? membersData;
    return members.some((m) => m.userId === currentUserId.value);
  } catch (err) {
    return false;
  }
}

async function startLoadJob(jobId) {
  const token = getToken();
  if (!token || !isAuthenticated.value) {
    accessDenied.value = true;
    emit("access-denied");
    return;
  }

  accessDenied.value = false;
  try {
    const res = await fetch(
      `${config.public.apiBaseUrl}/api/Infer/job/${jobId}`,
      { headers: { Authorization: `Bearer ${token}` } },
    );
    if (!res.ok) throw new Error("Load Job thất bại.");

    const data = await res.json();
    if (data.mediaId) emit("media-id-loaded", data.mediaId);

    const isPdf = data.imageUrl?.toLowerCase().includes(".pdf");

    // =========================================================
    // KỊCH BẢN 1: FILE PDF -> CHỈ LOAD 1 LẦN, KHÔNG LẶP LẠI
    // =========================================================
    if (isPdf) {
      ocrMode.value = false;
      ocrLoading.value = false; // Tắt vòng quay loading

      if (!pdfDoc.value) {
        ocrImageUrl.value = data.imageUrl; // Đánh dấu đã nạp URL
        pdfJobId.value = jobId;

        // 1. Phục hồi dữ liệu chữ đã OCR từ trước (nếu bạn F5 tải lại trang)
        if (data.results?.length > 0) {
          const grouped = {};
          data.results.forEach((r) => {
            const pNum = r.pageNumber || 1;
            if (!grouped[pNum]) grouped[pNum] = [];
            grouped[pNum].push(r);
          });
          pageOcrResults.value = grouped;

          // CHIÊU TỐI ƯU: Báo cho hệ thống biết trang này đã có data
          // Để hàm cuộn trang không gọi API Upload OCR lại nữa!
          Object.keys(grouped).forEach((pNum) => {
            pageUploadStatus.value[pNum] = "done";
          });
        }

        // 2. Tải cái vỏ PDF và kích hoạt logic Lazy Load (cuộn + 3 trang)
        pdfjsLib
          .getDocument(data.imageUrl)
          .promise.then((doc) => {
            pdfDoc.value = doc;
            totalPages.value = doc.numPages;
            nextTick(() =>
              viewMode.value === "scroll"
                ? setupScrollObserver()
                : renderPage(1),
            );

            // Logic 3 trang của bạn được gọi tại đây (chỉ chạy 1 lần lúc khởi tạo)
            enqueuePagesAhead(1);
          })
          .catch((err) => console.error("Lỗi vẽ PDF ban đầu:", err));
      }

      // LƯU Ý: Tuyệt đối không có đoạn setTimeout gọi lại hàm ở đây!
      return; // Chấm dứt vòng lặp!
    }
    // =========================================================
    // KỊCH BẢN 2: FILE ẢNH -> Dùng SignalR, fallback polling
    // =========================================================
    else {
      ocrMode.value = true;
      ocrImageUrl.value = data.imageUrl;
      totalPages.value = 1;

      if (data.status === "processing" || data.status === "pending") {
        ocrLoading.value = true;

        // Thử dùng SignalR — không poll nếu kết nối được
        const signalrOk = await trySetupSignalRForImage(jobId);
        if (!signalrOk) {
          // Fallback: polling 2s nếu SignalR không hoạt động
          setTimeout(() => startLoadJob(jobId), 2000);
        }
        return;
      }

      ocrLoading.value = false;
      if (data.results?.length > 0) {
        ocrResults.value = data.results;
        ocrResultsState.value = data.results;
      }
    }
  } catch (err) {
    console.error("Lỗi nạp dữ liệu OCR:", err);
    ocrLoading.value = false;
  }
}

async function loadPdf() {
  if (!props.fileUrl && !props.fileData) return;
  try {
    const source = props.fileData
      ? { data: props.fileData }
      : { url: props.fileUrl };
    pdfDoc.value = await pdfjsLib.getDocument(source).promise;
    totalPages.value = pdfDoc.value.numPages;

    const p1 = await pdfDoc.value.getPage(1);
    defaultPageHeight.value = p1.getViewport({ scale: scale.value }).height;

    await nextTick();
    viewMode.value === "scroll" ? setupScrollObserver() : renderPage(1);

    if (props.jobId) {
      pdfJobId.value = props.jobId;
      enqueuePagesAhead(1);
    } else {
      await initPdfJob({ name: "document.pdf" });
    }
  } catch (err) {
    console.error("Lỗi nạp PDF:", err);
  }
}

async function renderPage(pageNum) {
  if (!pdfDoc.value) return;

  if (!pageRendered.value[pageNum]) {
    pageRendered.value = { ...pageRendered.value, [pageNum]: true };
  }
  await nextTick();

  let canvas = pageCanvases.value[pageNum];
  if (!canvas) {
    for (let i = 0; i < 6; i++) {
      await new Promise((r) => setTimeout(r, 50));
      canvas = pageCanvases.value[pageNum];
      if (canvas) break;
    }
  }
  if (!canvas) return;

  while (isDisplayRendering) {
    await new Promise((r) => setTimeout(r, 30));
  }
  isDisplayRendering = true;

  try {
    const page = await pdfDoc.value.getPage(pageNum);
    const viewport = page.getViewport({ scale: scale.value });
    const textViewport = viewport.clone({ dontFlip: true });
    const dpr = window.devicePixelRatio || 1;

    canvas.width = Math.floor(viewport.width * dpr);
    canvas.height = Math.floor(viewport.height * dpr);
    canvas.style.width = `${Math.floor(viewport.width)}px`;
    canvas.style.height = `${Math.floor(viewport.height)}px`;

    // Hệ quy chiếu tọa độ Google Vision: phải khớp với scale upload (1.5)
    const ocrViewport = page.getViewport({ scale: 1.5 });
    pageDimensions.value = {
      ...pageDimensions.value,
      [pageNum]: { w: ocrViewport.width, h: ocrViewport.height },
    };

    const ctx = canvas.getContext("2d");
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    ctx.fillStyle = "#ffffff";
    ctx.fillRect(0, 0, viewport.width, viewport.height);

    activeRenderTask = page.render({ canvasContext: ctx, viewport });
    await activeRenderTask.promise;
    activeRenderTask = null;

    const textContent = await page.getTextContent();
    const textLayerDiv = textLayerRefs.value[pageNum];

    if (textLayerDiv) {
      textLayerDiv.innerHTML = "";
      textLayerDiv.style.width = `${viewport.width}px`;
      textLayerDiv.style.height = `${viewport.height}px`;
      textLayerDiv.style.setProperty("--total-scale-factor", dpr);

      const textLayer = new TextLayer({
        textContentSource: textContent,
        viewport: textViewport,
        container: textLayerDiv,
      });
      await textLayer.render();
    }

    // Annotation canvas: resize and load/redraw after page renders
    await nextTick();
    resizeAnnotCanvas(pageNum);
    if (annotLoadedPages.has(pageNum)) {
      redrawAnnotCanvas(pageNum);
    } else {
      annotLoadedPages.add(pageNum);
      loadAnnotations(pageNum);
    }
  } catch (err) {
    if (err?.name !== "RenderingCancelledException")
      console.error(`renderPage lỗi trang ${pageNum}:`, err);
  } finally {
    isDisplayRendering = false;
  }
}

function processQueue() {
  if (isRendering || renderQueue.length === 0) return;
  isRendering = true;
  renderQueue.sort(
    (a, b) => Math.abs(a - currentPage.value) - Math.abs(b - currentPage.value),
  );
  renderPage(renderQueue.shift()).then(() => {
    isRendering = false;
    requestAnimationFrame(processQueue);
  });
}

function setupScrollObserver() {
  if (scrollObserver) scrollObserver.disconnect();
  const rootEl = pdfScrollAllEl.value;
  if (!rootEl) return;

  scrollObserver = new IntersectionObserver(
    (entries) => {
      entries.forEach((entry) => {
        const p = parseInt(entry.target.dataset.page);
        if (entry.isIntersecting) {
          visiblePagesSet.add(p);
          if (!pageRendered.value[p]) {
            pageRendered.value[p] = true;
            renderQueue.push(p);
            processQueue();
          }
        } else {
          visiblePagesSet.delete(p);
          if (Math.abs(p - currentPage.value) > 15)
            pageRendered.value[p] = false;
        }
      });

      const rootRect = rootEl.getBoundingClientRect();
      const viewportCenter = rootRect.top + rootRect.height / 2;
      let closestPage = currentPage.value;
      let minDistance = Infinity;

      visiblePagesSet.forEach((p) => {
        const el = pageRefs.value[p];
        if (!el) return;
        const rect = el.getBoundingClientRect();
        const distance = Math.abs(rect.top + rect.height / 2 - viewportCenter);
        if (distance < minDistance) {
          minDistance = distance;
          closestPage = p;
        }
      });

      if (closestPage !== currentPage.value) {
        currentPage.value = closestPage;
        gotoPage.value = closestPage;
        emit("page-changed", closestPage);
      }
    },
    { root: rootEl, rootMargin: "800px 0px", threshold: [0, 0.1, 0.5, 1] },
  );

  nextTick(() =>
    Object.values(pageRefs.value).forEach(
      (el) => el && scrollObserver.observe(el),
    ),
  );
}

function handleTextSelection(e) {
  const selection = window.getSelection();
  const text = selection?.toString().trim();
  if (text && text.length > 0 && text.length < 500) {
    navigator.clipboard?.writeText(text).catch(() => {});
    // Try to get surrounding sentence from the text layer container
    let sourceSentence = text;
    try {
      const range = selection.getRangeAt(0);
      const container = range.startContainer.parentElement?.closest('[data-page-number], .textLayer, .ocr-text-layer') 
                     || range.startContainer.parentElement;
      if (container?.textContent) {
        const full = container.textContent.replace(/\s+/g, ' ').trim();
        const idx = full.indexOf(text);
        if (idx >= 0) {
          const start = Math.max(0, idx - 50);
          const end = Math.min(full.length, idx + text.length + 80);
          sourceSentence = full.substring(start, end).trim();
        }
      }
    } catch {}
    emit("text-selected", { text, x: e.clientX, y: e.clientY, sourceSentence });
  }
}

const setViewMode = (m) => {
  if (viewMode.value === m) return;
  viewMode.value = m;
  if (scrollObserver) scrollObserver.disconnect();
  nextTick(() =>
    m === "scroll" ? setupScrollObserver() : renderPage(currentPage.value),
  );
};

const zoomIn = () => {
  scale.value = Math.min(scale.value + 0.2, 4);
  rerender();
};
const zoomOut = () => {
  scale.value = Math.max(scale.value - 0.2, 0.4);
  rerender();
};
const fitWidth = () => rerender();

function rerender() {
  pageRendered.value = {};
  nextTick(() =>
    viewMode.value === "scroll"
      ? setupScrollObserver()
      : renderPage(currentPage.value),
  );
}

async function prevPage() {
  if (currentPage.value <= 1) return;
  currentPage.value--;
  gotoPage.value = currentPage.value;
  emit("page-changed", currentPage.value);
  await renderPage(currentPage.value);
}

async function nextPage() {
  if (currentPage.value >= totalPages.value) return;
  currentPage.value++;
  gotoPage.value = currentPage.value;
  emit("page-changed", currentPage.value);
  await renderPage(currentPage.value);
}

async function jumpToPage() {
  currentPage.value = Math.max(1, Math.min(gotoPage.value, totalPages.value));
  gotoPage.value = currentPage.value;
  emit("page-changed", currentPage.value);
  await renderPage(currentPage.value);
}

// FIX 5: Sử dụng setTimeout để đảm bảo DOM render kích thước chính xác
function onOcrImageLoad() {
  if (!ocrImgEl.value) return;
  setTimeout(() => {
    ocrNaturalW.value = ocrImgEl.value.naturalWidth || 0;
    ocrNaturalH.value = ocrImgEl.value.naturalHeight || 0;
    ocrDisplayW.value = ocrImgEl.value.offsetWidth || 0;
    ocrDisplayH.value = ocrImgEl.value.offsetHeight || 0;
    // Resize OCR annotation canvas and load annotations
    resizeOcrAnnotCanvas();
    if (!annotLoadedPages.has(1)) {
      annotLoadedPages.add(1);
      loadAnnotations(1);
    } else {
      redrawAnnotCanvas(1);
    }
  }, 50);
}

function getOcrTextStyle(r) {
  const b =
    typeof r.boundingBox === "string"
      ? JSON.parse(r.boundingBox)
      : r.boundingBox;
  if (!b) return { display: "none" };

  const xs = b.map((p) => (Array.isArray(p) ? p[0] : p.x ?? 0));
  const ys = b.map((p) => (Array.isArray(p) ? p[1] : p.y ?? 0));
  let x = Math.min(...xs),
    y = Math.min(...ys),
    w = Math.max(...xs) - x,
    h = Math.max(...ys) - y;

  if (ocrNaturalW.value > 0 && ocrDisplayW.value > 0) {
    const scX = ocrDisplayW.value / ocrNaturalW.value;
    const scY =
      ocrDisplayH.value > 0 ? ocrDisplayH.value / ocrNaturalH.value : scX;
    x *= scX;
    y *= scY;
    w *= scX;
    h *= scY;
  }

  return {
    position: "absolute",
    left: `${x}px`,
    top: `${y}px`,
    width: `${Math.max(w, 4)}px`,
    height: `${Math.max(h, 4)}px`,
    fontSize: `${Math.max(h * 0.85, 6)}px`,
    lineHeight: "1",
    whiteSpace: "nowrap",
    overflow: "hidden",
    color: "transparent",
    userSelect: "text",
  };
}

function getOcrTextStyleForPdf(r, pageNum) {
  const b =
    typeof r.boundingBox === "string"
      ? JSON.parse(r.boundingBox)
      : r.boundingBox;
  if (!b) return { display: "none" };

  const xs = b.map((p) => (Array.isArray(p) ? p[0] : p.x ?? 0));
  const ys = b.map((p) => (Array.isArray(p) ? p[1] : p.y ?? 0));
  let x = Math.min(...xs),
    y = Math.min(...ys),
    w = Math.max(...xs) - x,
    h = Math.max(...ys) - y;

  // pageDimensions được lưu ở scale 1.5, nên ratio tính theo đó
  const ratio = scale.value / 1.5;

  x *= ratio;
  y *= ratio;
  w *= ratio;
  h *= ratio;

  return {
    position: "absolute",
    left: `${x}px`,
    top: `${y}px`,
    width: `${Math.max(w, 2)}px`,
    height: `${Math.max(h, 2)}px`,
    fontSize: `${Math.max(h * 0.85, 4)}px`,
    lineHeight: "1",
    whiteSpace: "nowrap",
    overflow: "hidden",
    color: "transparent",
    userSelect: "text",
  };
}
// Hàm xuất Searchable PDF có ép chạy OCR toàn bộ
async function exportToSearchablePdf() {
  if (isExporting.value || ocrLoading.value) return;
  isExporting.value = true;

  try {
    // 1. KIỂM TRA & ÉP CHẠY QUÉT OCR CHO CÁC TRANG LAZY LOAD (Chỉ áp dụng với PDF)
    if (!ocrMode.value && pdfDoc.value) {
      const missingPages = [];
      for (let i = 1; i <= totalPages.value; i++) {
        const status = pageUploadStatus.value[i];
        // Nếu trang chưa hoàn thành hoặc chưa được cache từ trước -> Đưa vào danh sách cần quét
        if (status !== "done" && status !== "cached") {
          missingPages.push(i);
        }
      }

      if (missingPages.length > 0) {
        console.log(
          `Đang ép nhận diện ${missingPages.length} trang còn thiếu để xuất PDF...`,
        );
        // Chạy tuần tự (hạn chế chạy song song quá nhiều để tránh spam server AI)
        for (const p of missingPages) {
          await uploadOnePage(p);
        }
      }
    }

    // 2. LẤY PROJECT ID VÀ GỌI API CỦA BACKEND
    // Lấy projectId từ url (VD: /workspaces/project/1) hoặc thông qua prop
    const projectId = route.params.id || props.projectId;
    const fileId = props.jobId; // JobId được sử dụng tương đương FileId

    if (!projectId) {
      alert(
        "Không tìm thấy ID dự án (Project ID). Vui lòng kiểm tra lại component cha.",
      );
      isExporting.value = false;
      return;
    }

    const token = getToken();
    const url = `${config.public.apiBaseUrl}/api/Projects/${projectId}/files/${fileId}/export-pdf`;

    const response = await fetch(url, {
      method: "GET",
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({}));
      console.log(response);
      throw new Error(
        errorData.message || "Đã xảy ra lỗi khi tạo file PDF từ máy chủ.",
      );
    }

    // 3. NHẬN FILE BLOB TỪ BACKEND VÀ TẢI XUỐNG CỤC BỘ
    const blob = await response.blob();
    const downloadUrl = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = downloadUrl;

    // Ưu tiên lấy tên file chuẩn từ Header Backend gửi về, nếu không có thì tự tạo tên mặc định
    let fileName = `Searchable_Document_${new Date().getTime()}.pdf`;
    const disposition = response.headers.get("Content-Disposition");
    if (disposition && disposition.includes("filename=")) {
      const matches = /filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/.exec(
        disposition,
      );
      if (matches != null && matches[1])
        fileName = matches[1].replace(/['"]/g, "");
    }

    a.download = fileName;
    document.body.appendChild(a);
    a.click();

    // Dọn dẹp DOM
    document.body.removeChild(a);
    URL.revokeObjectURL(downloadUrl);
  } catch (error) {
    console.error("Lỗi khi xuất PDF:", error);
    alert(error.message || "Không thể xuất file PDF. Vui lòng thử lại sau.");
  } finally {
    isExporting.value = false;
  }
}

// ---- ANNOTATION FUNCTIONS ----
function selectAnnotTool(id) {
  activeTool.value = activeTool.value === id ? null : id;
}

function resizeAnnotCanvas(pageNum) {
  const pdfCvs = pageCanvases.value[pageNum];
  const annotCvs = annotCanvases.value[pageNum];
  if (!pdfCvs || !annotCvs) return;
  const w = parseInt(pdfCvs.style.width) || pdfCvs.offsetWidth;
  const h = parseInt(pdfCvs.style.height) || pdfCvs.offsetHeight;
  if (!w || !h) return;
  annotCvs.width = w;
  annotCvs.height = h;
}

function resizeOcrAnnotCanvas() {
  const cvs = ocrAnnotCanvas.value;
  const img = ocrImgEl.value;
  if (!cvs || !img) return;
  const w = img.offsetWidth;
  const h = img.offsetHeight;
  if (!w || !h) return;
  cvs.width = w;
  cvs.height = h;
}

async function loadAnnotations(pageNum) {
  if (!props.projectId || !props.jobId) { redrawAnnotCanvas(pageNum); return; }
  try {
    const token = getToken();
    const res = await fetch(
      `${config.public.apiBaseUrl}/api/projects/${props.projectId}/annotations?ocrJobId=${props.jobId}&pageNumber=${pageNum}`,
      { headers: { Authorization: `Bearer ${token}` } }
    );
    if (!res.ok) { redrawAnnotCanvas(pageNum); return; }
    const data = await res.json();
    const allStrokes = [];
    for (const entry of data) {
      try {
        const strokes = typeof entry.data === 'string' ? JSON.parse(entry.data) : (entry.data || []);
        if (Array.isArray(strokes)) allStrokes.push(...strokes);
      } catch {}
    }
    annotStrokesMap.value[pageNum] = allStrokes;
    redrawAnnotCanvas(pageNum);
  } catch (err) {
    console.warn('[Annotation] loadAnnotations failed:', err);
    redrawAnnotCanvas(pageNum);
  }
}

function redrawAnnotCanvas(pageNum) {
  const cvs = pageNum === 1 && ocrMode.value ? ocrAnnotCanvas.value : annotCanvases.value[pageNum];
  if (!cvs) return;
  const ctx = cvs.getContext('2d');
  ctx.clearRect(0, 0, cvs.width, cvs.height);
  const strokes = annotStrokesMap.value[pageNum] || [];
  for (const stroke of strokes) drawAnnotStroke(ctx, stroke);
}

function drawAnnotStroke(ctx, stroke) {
  if (!stroke.points || stroke.points.length < 2) return;
  const W = ctx.canvas.width;
  const H = ctx.canvas.height;
  ctx.save();
  if (stroke.tool === 'highlight') {
    ctx.globalAlpha = 0.35;
    ctx.lineWidth = (stroke.width || 0.003) * W * 3;
  } else {
    ctx.globalAlpha = 1;
    ctx.lineWidth = (stroke.width || 0.003) * W;
  }
  ctx.strokeStyle = stroke.color || '#e53e3e';
  ctx.lineCap = 'round';
  ctx.lineJoin = 'round';
  ctx.beginPath();
  ctx.moveTo(stroke.points[0].x * W, stroke.points[0].y * H);
  for (let i = 1; i < stroke.points.length; i++) ctx.lineTo(stroke.points[i].x * W, stroke.points[i].y * H);
  ctx.stroke();
  ctx.restore();
}

function getAnnotPos(e, pageNum) {
  const cvs = pageNum === 1 && ocrMode.value ? ocrAnnotCanvas.value : annotCanvases.value[pageNum];
  if (!cvs) return { x: 0, y: 0 };
  const rect = cvs.getBoundingClientRect();
  // Normalized 0..1 coordinates relative to the page — scale/DPR independent
  return {
    x: (e.clientX - rect.left) / rect.width,
    y: (e.clientY - rect.top) / rect.height,
  };
}

function onAnnotPointerDown(e, pageNum) {
  if (!activeTool.value) return;
  e.preventDefault();
  annotDrawing = true;
  const pos = getAnnotPos(e, pageNum);
  if (activeTool.value === 'eraser') { eraseAnnotAt(pos, pageNum); return; }
  const cvs = pageNum === 1 && ocrMode.value ? ocrAnnotCanvas.value : annotCanvases.value[pageNum];
  const cssW = cvs ? cvs.getBoundingClientRect().width : 1000;
  // Store width as fraction of CSS canvas width so it's scale-independent
  annotCurrentStroke = { tool: activeTool.value, color: penColor.value, width: penWidth.value / cssW, points: [pos], pageNum };
}

function onAnnotPointerMove(e, pageNum) {
  if (!annotDrawing || !activeTool.value) return;
  e.preventDefault();
  const pos = getAnnotPos(e, pageNum);
  if (activeTool.value === 'eraser') { eraseAnnotAt(pos, pageNum); return; }
  if (!annotCurrentStroke) return;
  annotCurrentStroke.points.push(pos);
  const cvs = pageNum === 1 && ocrMode.value ? ocrAnnotCanvas.value : annotCanvases.value[pageNum];
  if (!cvs) return;
  const ctx = cvs.getContext('2d');
  redrawAnnotCanvas(pageNum);
  drawAnnotStroke(ctx, annotCurrentStroke);
}

function onAnnotPointerUp(e, pageNum) {
  if (!annotDrawing) return;
  annotDrawing = false;
  if (activeTool.value === 'eraser') {
    // Broadcast the updated strokes (after erase) to collaborators
    if (props.jobId) {
      broadcastErase(Number(props.jobId), pageNum, annotStrokesMap.value[pageNum] || []);
    }
    scheduleAnnotSave(pageNum);
    return;
  }
  if (!annotCurrentStroke || annotCurrentStroke.points.length < 2) { annotCurrentStroke = null; return; }
  const finishedStroke = { ...annotCurrentStroke };
  if (!annotStrokesMap.value[pageNum]) annotStrokesMap.value[pageNum] = [];
  annotStrokesMap.value[pageNum].push(finishedStroke);
  annotCurrentStroke = null;
  redrawAnnotCanvas(pageNum);
  // Broadcast new stroke to collaborators
  if (props.jobId) {
    broadcastStroke(Number(props.jobId), pageNum, finishedStroke);
  }
  scheduleAnnotSave(pageNum);
}

function eraseAnnotAt(pos, pageNum) {
  const strokes = annotStrokesMap.value[pageNum];
  if (!strokes) return;
  const ERASE_RADIUS = 0.02; // 2% of page width — scale-independent
  annotStrokesMap.value[pageNum] = strokes.filter(s => !s.points.some(pt => Math.hypot(pt.x - pos.x, pt.y - pos.y) < ERASE_RADIUS));
  redrawAnnotCanvas(pageNum);
}

function scheduleAnnotSave(pageNum) {
  annotDirtyPages.add(pageNum);
  if (annotSaveTimers[pageNum]) clearTimeout(annotSaveTimers[pageNum]);
  annotSaveTimers[pageNum] = setTimeout(() => saveAnnotations(pageNum), 1500);
}

async function saveAnnotations(pageNum) {
  if (!props.projectId || !props.jobId) return;
  const strokes = annotStrokesMap.value[pageNum] || [];
  // Backup to localStorage
  const lsKey = `annot_${props.projectId}_${props.jobId}_${pageNum}`;
  try { localStorage.setItem(lsKey, JSON.stringify(strokes)); } catch {}
  annotSaveStatus.value = 'saving';
  try {
    const token = getToken();
    await fetch(`${config.public.apiBaseUrl}/api/projects/${props.projectId}/annotations`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      body: JSON.stringify({ ocrJobId: Number(props.jobId), pageNumber: pageNum, data: JSON.stringify(strokes) }),
    });
    annotDirtyPages.delete(pageNum);
    annotSaveStatus.value = 'saved';
    if (annotSaveStatusTimer) clearTimeout(annotSaveStatusTimer);
    annotSaveStatusTimer = setTimeout(() => { annotSaveStatus.value = null; }, 2000);
  } catch (err) {
    annotSaveStatus.value = null;
    console.warn('[Annotation] saveAnnotations failed:', err);
  }
}

async function saveAllDirtyPages() {
  if (annotDirtyPages.size === 0) return;
  const pages = [...annotDirtyPages];
  await Promise.all(pages.map(p => {
    if (annotSaveTimers[p]) { clearTimeout(annotSaveTimers[p]); delete annotSaveTimers[p]; }
    return saveAnnotations(p);
  }));
}
// ---- END ANNOTATION ----

onMounted(() => {
  if (props.jobId) startLoadJob(props.jobId);
  else if (props.fileUrl || props.fileData) loadPdf();

  // Connect to SignalR document room for collaborative drawing
  if (props.jobId) {
    hubConnect(Number(props.jobId)).catch(() => {});
  }

  // Receive strokes drawn by other collaborators
  const handleRemoteStroke = ({ pageNumber, strokeJson }) => {
    try {
      const stroke = JSON.parse(strokeJson);
      if (!annotStrokesMap.value[pageNumber]) annotStrokesMap.value[pageNumber] = [];
      annotStrokesMap.value[pageNumber].push(stroke);
      redrawAnnotCanvas(pageNumber);
    } catch {}
  };

  // Receive erase events from other collaborators
  const handleRemoteErase = ({ pageNumber, strokesJson }) => {
    try {
      const strokes = JSON.parse(strokesJson);
      annotStrokesMap.value[pageNumber] = strokes;
      redrawAnnotCanvas(pageNumber);
    } catch {}
  };

  onStroke(handleRemoteStroke);
  onErase(handleRemoteErase);
  onBeforeUnmount(() => {
    offStroke(handleRemoteStroke);
    offErase(handleRemoteErase);
  });

  // Ctrl+S to force-save all dirty annotation pages
  const handleCtrlS = (e) => {
    if ((e.ctrlKey || e.metaKey) && e.key === 's') {
      e.preventDefault();
      saveAllDirtyPages();
    }
  };
  window.addEventListener('keydown', handleCtrlS);
  onBeforeUnmount(() => window.removeEventListener('keydown', handleCtrlS));
});

onBeforeUnmount(() => {
  if (scrollObserver) scrollObserver.disconnect();
  if (activeRenderTask) {
    try {
      activeRenderTask.cancel();
    } catch {}
    activeRenderTask = null;
  }
  // Cleanup SignalR connection
  if (signalrConnection) {
    signalrConnection.stop().catch(() => {});
    signalrConnection = null;
  }
  // Save any remaining dirty pages before unmounting
  saveAllDirtyPages();
});
// Thêm watcher này để bắt sự kiện khi bấm Zoom (+ / -)
watch(scale, () => {
  if (ocrMode.value && ocrImgEl.value) {
    // Đợi DOM nở ra theo scale mới rồi mới lấy kích thước
    setTimeout(() => {
      ocrDisplayW.value = ocrImgEl.value.offsetWidth || 0;
      ocrDisplayH.value = ocrImgEl.value.offsetHeight || 0;
      resizeOcrAnnotCanvas();
      redrawAnnotCanvas(1);
    }, 50);
  }
});

defineExpose({ gotoPage, jumpToPage });
</script>

<style scoped>
:deep(.textLayer) {
  position: absolute;
  inset: 0;
  z-index: 10;
  pointer-events: auto;
  user-select: text;
  overflow: hidden !important;
}

:deep(.textLayer span) {
  color: transparent !important;
}

:deep(.textLayer ::selection),
.ocr-word::selection {
  background: rgba(59, 130, 246, 0.3) !important;
  color: transparent !important;
}

:deep(.textLayer br) {
  display: none;
}

.ocr-text-layer {
  user-select: text;
  overflow: hidden !important;
  position: absolute;
  inset: 0;
  z-index: 20;
  pointer-events: auto !important;
}

.ocr-word {
  position: absolute;
  transform-origin: 0 0;
  white-space: pre;
  color: transparent; /* Giữ trong suốt để bôi đen, nhưng có thể xóa để test nếu muốn */
  background: transparent;
  user-select: text;
  pointer-events: auto !important; /* Đảm bảo con trỏ chuột bôi đen được */
  cursor: text;
}
</style>
