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
    </div>

    <!-- MAIN CONTENT -->
    <div
      v-if="!accessDenied"
      class="flex-1 flex flex-col items-center relative pdf-scroll-area overflow-auto p-4"
      ref="pdfScrollAllEl"
      @mouseup="handleTextSelection"
    >
      <!-- LOADING -->
      <div
        v-if="!pdfDoc && !ocrMode"
        class="flex flex-col items-center justify-center h-full text-gray-400 w-full mt-20"
      >
        <div
          class="w-8 h-8 border-4 border-gray-600 border-t-[#f0c040] rounded-full animate-spin mb-4"
        ></div>
        <p>Đang tải tài liệu PDF...</p>
      </div>

      <!-- OCR IMAGE MODE -->
      <div v-else-if="ocrMode" class="w-full flex justify-center">
        <div
          v-if="ocrLoading"
          class="flex flex-col items-center justify-center h-full text-gray-400 mt-20"
        >
          <div
            class="w-8 h-8 border-4 border-gray-600 border-t-[#f0c040] rounded-full animate-spin mb-4"
          ></div>
          <p>AI đang đọc tài liệu OCR...</p>
        </div>

        <div
          v-else-if="ocrImageUrl"
          class="relative no-drag inline-block leading-none"
          :style="{
            transform: `scale(${scale})`,
            transformOrigin: 'top center',
          }"
        >
          <img
            :src="ocrImageUrl"
            ref="ocrImgEl"
            @load="onOcrImageLoad"
            draggable="false"
            class="block shadow-2xl max-w-full"
          />

          <!-- Text layer trong suốt — bôi đen như text thật -->
          <div
            v-if="ocrResults && ocrDisplayW > 0"
            class="absolute inset-0 overflow-hidden ocr-text-layer"
          >
            <span
              v-for="(r, i) in ocrResults"
              :key="i"
              class="absolute cursor-text pointer-events-auto select-text ocr-word"
              :style="getOcrTextStyle(r)"
              >{{ r.wordText }}</span
            >
          </div>
        </div>
      </div>

      <!-- PDF PAGES -->
      <div v-else class="flex flex-col gap-6 w-full items-center">
        <template v-for="n in totalPages" :key="n">
          <div
            v-if="viewMode === 'scroll' || n === currentPage"
            :data-page="n"
            :ref="
              (el) => {
                if (el) pageRefs[n] = el;
              }
            "
            class="w-full flex justify-center"
            :style="{ minHeight: defaultPageHeight + 'px' }"
          >
            <div
              v-if="pageRendered[n] || viewMode === 'single'"
              class="relative shadow-2xl bg-white leading-none"
            >
              <canvas
                :ref="
                  (el) => {
                    if (el) pageCanvases[n] = el;
                    else delete pageCanvases[n];
                  }
                "
                class="block"
              ></canvas>

              <!-- Text layer PDF — span trong suốt giống OCR image -->
              <div class="absolute inset-0 overflow-hidden ocr-text-layer">
                <span
                  v-for="(word, wi) in pageTextWords[n] || []"
                  :key="wi"
                  class="absolute cursor-text pointer-events-auto select-text ocr-word"
                  :style="word.style"
                  >{{ word.text }}</span
                >
              </div>
            </div>

            <!-- PLACEHOLDER khi chưa render -->
            <div
              v-else-if="viewMode === 'scroll'"
              class="w-full max-w-4xl bg-white/5 border border-dashed border-gray-700 flex items-center justify-center rounded"
              :style="{ height: defaultPageHeight + 'px' }"
            >
              <span class="text-gray-500 text-sm">Trang {{ n }}</span>
            </div>
          </div>
        </template>
      </div>
    </div>
  </div>
</template>

<script setup>
import {
  ref,
  shallowRef,
  computed,
  nextTick,
  onMounted,
  onBeforeUnmount,
  watch,
} from "vue";
import { useRuntimeConfig, useRoute, useRouter } from "#imports";
import { useJwt } from "~/composables/useJwt";
import { useOcrResultsState } from "~/composables/useLookupState";

import * as pdfjsLib from "pdfjs-dist/legacy/build/pdf";
import workerUrl from "pdfjs-dist/build/pdf.worker.min.mjs?url";
pdfjsLib.GlobalWorkerOptions.workerSrc = workerUrl;

const ocrResultsState = useOcrResultsState();

// ─── PROPS & EMITS ────────────────────────────────────────────────────────────
const props = defineProps({
  fileUrl: { type: String, required: false },
  fileData: { type: Uint8Array, required: false },
  jobId: { type: [String, Number], required: false },
  apiKey: { type: String, required: true },
});

const emit = defineEmits([
  "text-selected",
  "rag-updated",
  "page-changed",
  "media-id-loaded",
  "access-denied",
]);

// ─── COMPOSABLES ──────────────────────────────────────────────────────────────
const config = useRuntimeConfig();
const route = useRoute();
const router = useRouter();
const { isAuthenticated, userId: currentUserId } = useJwt();
const getToken = () => localStorage.getItem("jwt_token") || "";

// ─── ACCESS CONTROL ───────────────────────────────────────────────────────────
const accessDenied = ref(false);

// ─── PDF STATE ────────────────────────────────────────────────────────────────
const pdfDoc = shallowRef(null);
const totalPages = ref(0);
const currentPage = ref(1);
const gotoPage = ref(1);
const scale = ref(1.2);
const viewMode = ref("scroll");
const defaultPageHeight = ref(800);

// ─── PDF TEXT WORDS (span layer) ──────────────────────────────────────────────
// Mỗi trang lưu mảng { text, style } để render span trong suốt
const pageTextWords = ref({});

// ─── OCR IMAGE STATE ──────────────────────────────────────────────────────────
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

// ─── RENDER CACHE & QUEUE ─────────────────────────────────────────────────────
const pageRendered = ref({});
const renderQueue = [];
const visiblePagesSet = new Set();
let isRendering = false;
let isDisplayRendering = false;
let scrollObserver = null;
let activeRenderTask = null;

const pageRefs = ref({});
const pageCanvases = ref({});
const pdfScrollAllEl = ref(null);

// ─── WATCHERS ─────────────────────────────────────────────────────────────────
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

// ─── PDF UPLOAD STATE ─────────────────────────────────────────────────────────
const pdfJobId = ref(null); // jobId nhận từ BE sau create-pdf-job
const pageUploadStatus = ref({}); // { 1: 'uploading'|'done'|'cached', 2: ... }
const uploadQueue = ref([]); // Hàng đợi các trang cần upload
let isUploadRunning = false;

// ─── BƯỚC 1: Tạo Job khi user mở PDF ─────────────────────────────────────────
async function initPdfJob(file) {
  const token = localStorage.getItem("jwt_token") || "";
  const formData = new FormData();
  formData.append("fileName", file.name);
  formData.append("totalPages", totalPages.value);
  if (props.projectId) formData.append("projectId", props.projectId);

  const res = await fetch(
    `${useRuntimeConfig().public.apiBaseUrl}/api/Infer/create-pdf-job`,
    {
      method: "POST",
      headers: { Authorization: `Bearer ${token}` },
      body: formData,
    },
  );
  const data = await res.json();
  pdfJobId.value = data.jobId;
  console.log(`[PDF Job] Tạo jobId=${data.jobId}`);

  // Upload ngay trang 1, 2, 3 (trang hiện tại + 2 ahead)
  enqueuePagesAhead(1);
}

// ─── ENQUEUE: Thêm trang cần upload vào hàng đợi ─────────────────────────────
function enqueuePagesAhead(fromPage) {
  const AHEAD = 2;
  for (
    let p = fromPage;
    p <= Math.min(fromPage + AHEAD, totalPages.value);
    p++
  ) {
    if (!pageUploadStatus.value[p]) {
      // Chưa upload và chưa trong queue
      pageUploadStatus.value[p] = "queued";
      uploadQueue.value.push(p);
    }
  }
  runUploadQueue();
}

// ─── QUEUE RUNNER: Chạy tuần tự, không spam request ─────────────────────────
async function runUploadQueue() {
  if (isUploadRunning || uploadQueue.value.length === 0) return;
  isUploadRunning = true;

  while (uploadQueue.value.length > 0) {
    const pageNum = uploadQueue.value.shift();
    if (
      pageUploadStatus.value[pageNum] === "done" ||
      pageUploadStatus.value[pageNum] === "cached"
    )
      continue;

    await uploadOnePage(pageNum);
  }

  isUploadRunning = false;
}

// ─── UPLOAD 1 TRANG: Render canvas → PNG blob → POST lên BE ──────────────────
async function uploadOnePage(pageNum) {
  if (!pdfDoc.value || !pdfJobId.value) return;
  pageUploadStatus.value[pageNum] = "uploading";

  try {
    // Render trang thành canvas (scale 2x cho rõ nét)
    const page = await pdfDoc.value.getPage(pageNum);
    const viewport = page.getViewport({ scale: 2.0 });
    const canvas = document.createElement("canvas");
    canvas.width = viewport.width;
    canvas.height = viewport.height;
    await page.render({ canvasContext: canvas.getContext("2d"), viewport })
      .promise;

    // Canvas → PNG Blob
    const blob = await new Promise((resolve) =>
      canvas.toBlob(resolve, "image/png"),
    );

    // Gửi lên BE
    const token = localStorage.getItem("jwt_token") || "";
    const formData = new FormData();
    formData.append("image", blob, `page_${pageNum}.png`);
    formData.append("jobId", pdfJobId.value);
    formData.append("pageNumber", pageNum);

    const res = await fetch(
      `${useRuntimeConfig().public.apiBaseUrl}/api/Infer/upload-pdf-page`,
      {
        method: "POST",
        headers: { Authorization: `Bearer ${token}` },
        body: formData,
      },
    );
    const data = await res.json();

    pageUploadStatus.value[pageNum] = data.status; // 'completed' hoặc 'cached'
    console.log(`[Upload] Trang ${pageNum} → ${data.status}`);
  } catch (err) {
    console.error(`[Upload] Lỗi trang ${pageNum}:`, err);
    pageUploadStatus.value[pageNum] = "error";
  }
}

// ─── WATCH: Khi user cuộn sang trang mới → enqueue ahead ─────────────────────
watch(currentPage, (newPage) => {
  enqueuePagesAhead(newPage);
});
async function checkAccessByProjectId(token, projectId) {
  try {
    const projRes = await fetch(
      `${config.public.apiBaseUrl}/api/projects/${projectId}`,
      {
        headers: { Authorization: `Bearer ${token}` },
      },
    );
    if (!projRes.ok) return false;

    const projData = await projRes.json();
    const wsId = projData.result?.workspaceId ?? projData.workspaceId;
    if (!wsId) return false;

    const membersRes = await fetch(
      `${config.public.apiBaseUrl}/api/workspaces/${wsId}/members`,
      {
        headers: { Authorization: `Bearer ${token}` },
      },
    );
    if (!membersRes.ok) return false;

    const membersData = await membersRes.json();
    const members = membersData.result ?? membersData;
    return members.some((m) => m.userId === currentUserId.value);
  } catch (err) {
    console.error("Lỗi check quyền:", err);
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

  ocrLoading.value = true;
  ocrMode.value = true;

  let hasAccess = false;
  const urlProjectId = route.query.projectId;

  if (urlProjectId) {
    hasAccess = await checkAccessByProjectId(token, urlProjectId);
  } else {
    try {
      const jobRes = await fetch(
        `${config.public.apiBaseUrl}/api/Infer/job/${jobId}`,
        {
          headers: { Authorization: `Bearer ${token}` },
        },
      );
      if (jobRes.ok) {
        const jobData = await jobRes.json();
        const pId = jobData.projectId ?? jobData.result?.projectId;
        if (pId) hasAccess = await checkAccessByProjectId(token, pId);
      }
    } catch {
      /* bỏ qua */
    }
  }

  if (!hasAccess) {
    accessDenied.value = true;
    emit("access-denied");
    ocrLoading.value = false;
    return;
  }

  accessDenied.value = false;
  try {
    const res = await fetch(
      `${config.public.apiBaseUrl}/api/Infer/job/${jobId}`,
      {
        headers: { Authorization: `Bearer ${token}` },
      },
    );
    if (!res.ok) throw new Error("Load Job thất bại sau khi đã check quyền.");

    const data = await res.json();

    if (data.mediaId) emit("media-id-loaded", data.mediaId);
    if (data.results?.length > 0) dbOcrResults.value = data.results;

    if (data.imageUrl?.toLowerCase().includes(".pdf")) {
      ocrMode.value = false;
      ocrImageUrl.value = data.imageUrl;

      pdfDoc.value = await pdfjsLib.getDocument(data.imageUrl).promise;
      totalPages.value = pdfDoc.value.numPages;

      await nextTick();
      viewMode.value === "scroll" ? setupScrollObserver() : renderPage(1);
    } else {
      ocrImageUrl.value = data.imageUrl;
      ocrResults.value = data.results;
      ocrResultsState.value = data.results;
      totalPages.value = 1;
    }
  } catch (err) {
    console.error("Lỗi nạp dữ liệu OCR:", err);
  } finally {
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

    // 🆕 Tạo PDF Job và bắt đầu prefetch
    if (props.jobId) {
      // Mở lại file cũ → dùng jobId có sẵn
      pdfJobId.value = props.jobId;
      enqueuePagesAhead(1);
    } else {
      // File mới → tạo job mới
      // (cần truyền file object vào loadPdf nếu muốn dùng tên file)
      await initPdfJob({ name: "document.pdf" });
    }
  } catch (err) {
    console.error("Lỗi nạp PDF:", err);
  }
}

// ==========================================
// 2. RENDER ENGINE
// ==========================================

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
    const dpr = window.devicePixelRatio || 1;

    canvas.width = Math.floor(viewport.width * dpr);
    canvas.height = Math.floor(viewport.height * dpr);
    canvas.style.width = `${Math.floor(viewport.width)}px`;
    canvas.style.height = `${Math.floor(viewport.height)}px`;

    const ctx = canvas.getContext("2d");
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    ctx.fillStyle = "#ffffff";
    ctx.fillRect(0, 0, viewport.width, viewport.height);

    activeRenderTask = page.render({ canvasContext: ctx, viewport });
    await activeRenderTask.promise;
    activeRenderTask = null;

    // ── Build text word spans giống OCR image mode ──────────────────────
    const textContent = await page.getTextContent();
    const words = [];

    for (const item of textContent.items) {
      if (!item.str?.trim()) continue;

      const [a, b, c, d, tx, ty] = item.transform;
      const fontH = Math.sqrt(a * a + b * b); // Chiều cao font gốc

      // DÙNG HÀM CHUẨN CỦA PDF.JS ĐỂ CONVERT TỌA ĐỘ
      // Hàm này tự xử lý việc scale, offset và lật trục Y cho bạn
      const [baseX, baseY] = viewport.convertToViewportPoint(tx, ty);

      const h = fontH * scale.value;
      const w = (item.width ?? 0) * scale.value;

      // tx, ty trong PDF thường chỉ vào ĐÁY (baseline) của chữ.
      // Khi convert sang baseY, nó là tọa độ Y của mép dưới chữ trên màn hình.
      // => Để set thuộc tính CSS 'top', bạn phải lấy đáy trừ đi chiều cao chữ (h).
      const x = baseX;
      const y = baseY - h;

      words.push({
        text: item.str,
        style: {
          position: "absolute",
          left: `${x}px`,
          top: `${y}px`,
          width: `${Math.max(w, 4)}px`,
          height: `${Math.max(h, 4)}px`,
          fontSize: `${Math.max(h * 0.92, 6)}px`, // Ép nhỏ font lại một chút để không bị tràn
          lineHeight: "1",
          whiteSpace: "nowrap",
          overflow: "hidden",
          color: "transparent",
          userSelect: "text",
          transformOrigin: "left bottom", // Giúp font scale chuẩn xác hơn
        },
      });
    }

    pageTextWords.value = { ...pageTextWords.value, [pageNum]: words };
  } catch (err) {
    if (err?.name !== "RenderingCancelledException") {
      console.error(`renderPage lỗi trang ${pageNum}:`, err);
    }
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
  const p = renderQueue.shift();
  renderPage(p).then(() => {
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

// ─── TEXT SELECTION ───────────────────────────────────────────────────────────
function handleTextSelection(e) {
  const text = window.getSelection()?.toString().trim();
  if (text && text.length > 0 && text.length < 500) {
    navigator.clipboard?.writeText(text).catch(() => {});
    emit("text-selected", { text, x: e.clientX, y: e.clientY });
  }
}

// ==========================================
// 3. CONTROLS
// ==========================================

const setViewMode = (m) => {
  if (viewMode.value === m) return;
  viewMode.value = m;
  pageRendered.value = {};
  pageTextWords.value = {};
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
  pageTextWords.value = {};
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

// ==========================================
// 4. OCR IMAGE MODE helpers
// ==========================================

function onOcrImageLoad() {
  if (!ocrImgEl.value) return;
  ocrNaturalW.value = ocrImgEl.value.naturalWidth;
  ocrNaturalH.value = ocrImgEl.value.naturalHeight;
  ocrDisplayW.value = ocrImgEl.value.offsetWidth;
  ocrDisplayH.value = ocrImgEl.value.offsetHeight;
}

function getOcrTextStyle(r) {
  const b =
    typeof r.boundingBox === "string"
      ? JSON.parse(r.boundingBox)
      : r.boundingBox;
  if (!b) return { display: "none" };

  const xs = b.map((p) => (Array.isArray(p) ? p[0] : p.x ?? 0));
  const ys = b.map((p) => (Array.isArray(p) ? p[1] : p.y ?? 0));

  let x = Math.min(...xs);
  let y = Math.min(...ys);
  let w = Math.max(...xs) - x;
  let h = Math.max(...ys) - y;

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

// ==========================================
// 5. LIFECYCLE
// ==========================================

onMounted(() => {
  if (props.jobId) startLoadJob(props.jobId);
  else if (props.fileUrl || props.fileData) loadPdf();
});

onBeforeUnmount(() => {
  if (scrollObserver) scrollObserver.disconnect();
  if (activeRenderTask) {
    try {
      activeRenderTask.cancel();
    } catch {
      /* ignore */
    }
    activeRenderTask = null;
  }
});

defineExpose({ gotoPage, jumpToPage });
</script>

<style scoped>
/* Dùng chung cho cả PDF và OCR image */
.ocr-text-layer {
  user-select: text;
}
.ocr-word {
  color: transparent;
  user-select: text;
}
.ocr-word::selection {
  background: rgba(91, 141, 238, 0.4);
  color: transparent;
}
</style>
