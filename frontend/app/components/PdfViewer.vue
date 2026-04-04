<template>
  <div class="flex flex-col h-full bg-[#1c2128] w-full relative">
    
    <div v-if="accessDenied" class="absolute inset-0 z-50 flex flex-col items-center justify-center bg-[#161b22] text-[#c9d1d9] p-6 text-center">
      <div class="w-20 h-20 bg-red-900/30 text-red-500 rounded-full flex items-center justify-center mb-6">
        <svg class="w-10 h-10" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" /></svg>
      </div>
      <h2 class="text-2xl font-bold text-white mb-2">Không khả dụng</h2>
      <p class="text-gray-400 mb-6 max-w-md">Bạn chưa đăng nhập hoặc tài liệu này thuộc về một Workspace mà bạn không có quyền truy cập.</p>
      <button @click="$router.push('/workspaces')" class="px-6 py-2.5 bg-[#f0c040] text-black font-bold rounded-lg hover:bg-[#e3b330] transition-colors">
        Quay lại trang chủ
      </button>
    </div>

    <div v-if="!accessDenied" class="flex items-center gap-2 p-2 bg-[#161b22] border-b border-[#30363d] shrink-0 text-[#c9d1d9] flex-wrap z-10">
      <div class="flex bg-[#21262d] border border-[#30363d] rounded overflow-hidden">
        <button
          :class="[
            'px-3 py-1 hover:bg-[#30363d]',
            viewMode === 'single' ? 'bg-[#f0c040] text-black font-semibold' : '',
          ]"
          @click="setViewMode('single')"
        >
          Trang đơn
        </button>
        <button
          :class="[
            'px-3 py-1 hover:bg-[#30363d]',
            viewMode === 'scroll' ? 'bg-[#f0c040] text-black font-semibold' : '',
          ]"
          @click="setViewMode('scroll')"
        >
          Cuộn
        </button>
      </div>

      <div class="w-px h-5 bg-[#30363d] mx-1"></div>

      <template v-if="viewMode === 'single'">
        <button class="px-2 py-1 bg-[#21262d] rounded border border-[#30363d]" @click="prevPage" :disabled="currentPage <= 1">‹</button>
        <span class="text-sm px-2">
          <input v-model.number="gotoPage" type="number" class="w-10 text-center bg-[#161b22] border border-[#30363d] rounded outline-none" @change="jumpToPage" />
          / {{ totalPages }}
        </span>
        <button class="px-2 py-1 bg-[#21262d] rounded border border-[#30363d]" @click="nextPage" :disabled="currentPage >= totalPages">›</button>
      </template>
      <template v-else>
        <span class="text-sm px-2">Trang {{ currentPage }} / {{ totalPages }}</span>
      </template>

      <div class="w-px h-5 bg-[#30363d] mx-1"></div>

      <button class="px-2 py-1 bg-[#21262d] rounded border border-[#30363d]" @click="zoomOut">−</button>
      <span class="text-sm w-12 text-center">{{ Math.round(scale * 100) }}%</span>
      <button class="px-2 py-1 bg-[#21262d] rounded border border-[#30363d]" @click="zoomIn">+</button>
      <button class="px-2 py-1 bg-[#21262d] rounded border border-[#30363d] ml-1" @click="fitWidth" title="Vừa chiều rộng">⟺</button>
    </div>

    <div v-if="!accessDenied" class="flex-1 flex flex-col items-center relative pdf-scroll-area overflow-auto p-4" ref="pdfScrollAllEl" @mouseup="handleTextSelection">
      
      <div v-if="!pdfDoc && !ocrMode" class="flex flex-col items-center justify-center h-full text-gray-400 w-full mt-20">
        <div class="w-8 h-8 border-4 border-gray-600 border-t-[#f0c040] rounded-full animate-spin mb-4"></div>
        <p>Đang tải tài liệu PDF...</p>
      </div>

      <div v-else-if="ocrMode" class="w-full flex justify-center">
        <div v-if="ocrLoading" class="flex flex-col items-center justify-center h-full text-gray-400 mt-20">
          <div class="w-8 h-8 border-4 border-gray-600 border-t-[#f0c040] rounded-full animate-spin mb-4"></div>
          <p>AI đang đọc tài liệu OCR...</p>
        </div>
        <div
          v-else-if="ocrImageUrl"
          class="relative no-drag inline-block leading-none"
          :style="{ transform: `scale(${scale})`, transformOrigin: 'top center' }"
          @mousedown.prevent="startBboxSelect($event)"
          @mousemove="updateBboxSelect($event)"
          @mouseup="endBboxSelect($event)"
        >
          <img :src="ocrImageUrl" ref="ocrImgEl" @load="onOcrImageLoad" draggable="false" class="block shadow-2xl max-w-full" />
          <div v-if="dragSelect.active" class="absolute bg-blue-500/15 border border-blue-500/70 pointer-events-none z-10" :style="getDragRectStyle()"></div>
          <div v-if="ocrResults" class="absolute inset-0 pointer-events-none">
            <div
              v-for="(r, i) in ocrResults"
              :key="i"
              class="absolute bg-transparent pointer-events-auto cursor-crosshair rounded-[2px] border border-blue-500/40 transition hover:bg-blue-500/10 hover:border-blue-500/80"
              :class="{ 'bg-blue-500/25 border-blue-500/90': selectedOcrWords.includes(r) }"
              :style="getOcrBoxStyle(r)"
              :title="r.wordText"
              @click.stop="onBboxClick(r, $event)"
            ></div>
          </div>
        </div>
      </div>

      <div v-else class="flex flex-col gap-6 w-full items-center">
        <template v-for="n in totalPages" :key="n">
          <div
            v-if="viewMode === 'scroll' ? true : n === currentPage"
            :data-page="n"
            :ref="(el) => { if (el) pageRefs[n] = el; }"
            class="w-full flex justify-center"
            :style="{ minHeight: defaultPageHeight + 'px' }"
          >
            <div v-if="pageRendered[n] || viewMode === 'single'" class="relative shadow-2xl bg-white leading-none">
              <canvas :ref="(el) => { if (el) pageCanvases[n] = el; else delete pageCanvases[n]; }" class="block"></canvas>
              <div :ref="(el) => { if (el) textLayerRefs[n] = el; }" class="textLayer absolute inset-0 pointer-events-auto text-transparent selection:bg-blue-500/30" :class="{ 'pointer-events-none select-none': visionEnabled }"></div>
              
              <div v-if="visionEnabled" class="absolute inset-0 z-10 select-none cursor-crosshair" @mousedown.prevent="startDrag($event, n)" @mousemove="updateDrag($event, n)" @mouseup="endDrag($event, n)">
                <div v-if="pageVisionData[n]?.status === 'processing' || pageVisionData[n]?.status === 'scanning'" class="absolute bottom-2 left-1/2 -translate-x-1/2 bg-black/80 text-blue-400 px-3 py-1 rounded-full text-xs flex items-center gap-2">
                  <span class="w-3 h-3 border-2 border-blue-400 border-t-transparent rounded-full animate-spin"></span>
                  Quét OCR...
                </div>
                <div v-else-if="pageVisionData[n]?.status === 'error'" class="absolute bottom-2 left-1/2 -translate-x-1/2 bg-red-900/80 text-white px-3 py-1 rounded-full text-xs">
                  Lỗi API
                  <button @click.stop="retryVision(n)" class="underline ml-1">Thử lại</button>
                </div>
                <template v-if="pageVisionData[n]?.status === 'done'">
                  <div
                    v-for="(item, i) in pageVisionData[n]?.items"
                    :key="i"
                    class="absolute border border-blue-500/30 hover:bg-blue-500/10 transition-colors"
                    :class="{ 'bg-blue-500/25 !border-blue-500/90': isVisionItemSelected(item) }"
                    :style="getVisionItemStyle(item)"
                    :title="item.text"
                    @click.stop="onVisionItemClick(item, $event)"
                  ></div>
                </template>
                <div v-if="dragState.active && dragState.page === n" class="absolute bg-blue-500/15 border border-blue-500/70 pointer-events-none" :style="getVisionDragRectStyle()"></div>
              </div>
            </div>
            <div v-else-if="viewMode === 'scroll'" class="w-full max-w-4xl bg-white/5 border border-dashed border-gray-700 flex items-center justify-center rounded" :style="{ height: defaultPageHeight + 'px' }">
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
import { useJwt } from '~/composables/useJwt';

import * as pdfjsLib from "pdfjs-dist/legacy/build/pdf";
import workerUrl from "pdfjs-dist/build/pdf.worker.min.mjs?url";
pdfjsLib.GlobalWorkerOptions.workerSrc = workerUrl;

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
  "access-denied"
]);

const config = useRuntimeConfig();
const route = useRoute(); // <-- Lấy thông tin query URL
const router = useRouter();
const { isAuthenticated, userId: currentUserId } = useJwt();
const getToken = () => localStorage.getItem("jwt_token") || "";

// Cờ chặn truy cập
const accessDenied = ref(false);

// --- STATES PDF ---
const pdfDoc = shallowRef(null);
const totalPages = ref(0);
const currentPage = ref(1);
const gotoPage = ref(1);
const scale = ref(1.2);
const viewMode = ref("scroll");
const defaultPageHeight = ref(800);

// --- STATES OCR ---
const ocrMode = ref(false);
const ocrImageUrl = ref("");
const ocrResults = shallowRef(null);
const ocrFullText = ref("");
const ocrLoading = ref(false);
const ocrImgEl = ref(null);
const dragSelect = ref({ active: false, startX: 0, startY: 0, endX: 0, endY: 0 });
const selectedOcrWords = ref([]);
const ocrNaturalW = ref(0);
const ocrNaturalH = ref(0);
const ocrDisplayW = ref(0);
const ocrDisplayH = ref(0);
const dbOcrResults = ref([]);

// --- CACHE & QUEUE ---
const pageRendered = ref({});
const renderQueue = [];
const visiblePagesSet = new Set();
let isRendering = false;
let isDisplayRendering = false;
let scrollObserver = null;
const pageRefs = ref({});
const pageCanvases = ref({});
const textLayerRefs = ref({});
const pdfScrollAllEl = ref(null);

// --- VISION OCR ---
const visionEnabled = ref(true);
const pageVisionData = ref({});
const visionQueue = ref([]);
let isProcessingVision = false;
const dragState = ref({ active: false, page: null, startX: 0, startY: 0, endX: 0, endY: 0 });
const visionSelectedItems = ref([]);

const visionStatusText = computed(() => {
  const s = pageVisionData.value[currentPage.value]?.status;
  if (s === "processing" || s === "scanning") return "Đang xử lý...";
  if (s === "done") return "Đã nhận diện";
  if (s === "error") return "Lỗi";
  return "Chờ xử lý...";
});

// ==========================================
// 1. NẠP DỮ LIỆU BẢO MẬT
// ==========================================
watch(() => props.jobId, (newVal) => { if (newVal) startLoadJob(newVal); });
watch(() => props.fileUrl, (newVal) => { if (newVal) loadPdf(); });
watch(() => props.fileData, (newVal) => { if (newVal) loadPdf(); });


// HÀM MỚI: Check quyền nhanh gọn lẹ nếu có projectId trên URL
async function checkAccessByProjectId(token, projectId) {
  try {
    const projRes = await fetch(`${config.public.apiBaseUrl}/api/projects/${projectId}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    
    if (!projRes.ok) return false;
    
    const projData = await projRes.json();
    const wsId = projData.result?.workspaceId || projData.workspaceId;
    
    if (!wsId) return false;

    const membersRes = await fetch(`${config.public.apiBaseUrl}/api/workspaces/${wsId}/members`, {
      headers: { Authorization: `Bearer ${token}` }
    });

    if (!membersRes.ok) return false;

    const membersData = await membersRes.json();
    const members = membersData.result || membersData;
    
    return members.some(m => m.userId === currentUserId.value);
  } catch (error) {
    console.error("Lỗi check quyền:", error);
    return false;
  }
}

// Bắt đầu luồng kiểm tra quyền và tải File
async function startLoadJob(jobId) {
  const token = getToken();

  // 1. Check Login
  if (!token || !isAuthenticated.value) {
    accessDenied.value = true;
    emit("access-denied"); 
    return;
  }

  ocrLoading.value = true;
  ocrMode.value = true;

  // 2. CHECK QUYỀN
  let hasAccess = false;
  const urlProjectId = route.query.projectId;

  // Nếu có projectId trên URL -> Check thẳng luôn
  if (urlProjectId) {
    hasAccess = await checkAccessByProjectId(token, urlProjectId);
  } else {
    // Nếu ko có trên URL (th user mở file cũ/bị ẩn) -> Gọi hỏi Job trước rồi bóc projectId
    try {
      const jobRes = await fetch(`${config.public.apiBaseUrl}/api/Infer/job/${jobId}`, {
        method: "GET",
        headers: { Authorization: `Bearer ${token}` },
      });
      if (jobRes.ok) {
        const jobData = await jobRes.json();
        const pId = jobData.projectId || jobData.result?.projectId;
        if (pId) {
          hasAccess = await checkAccessByProjectId(token, pId);
        }
      }
    } catch (e) { }
  }

  // 3. TỪ CHỐI
  if (!hasAccess) {
    accessDenied.value = true;
    emit("access-denied"); 
    ocrLoading.value = false;
    return;
  }

  // 4. CHO PHÉP - Tiến hành tải dữ liệu Job
  accessDenied.value = false;
  try {
    const apiUrl = `${config.public.apiBaseUrl}/api/Infer/job/${jobId}`;
    const res = await fetch(apiUrl, {
      method: "GET",
      headers: { Authorization: `Bearer ${token}` },
    });

    if (!res.ok) throw new Error("Load Job thất bại sau khi đã check quyền.");
    
    const data = await res.json();

    if (data.mediaId) emit("media-id-loaded", data.mediaId);
    if (data.results && data.results.length > 0) dbOcrResults.value = data.results;

    if (data.imageUrl && data.imageUrl.toLowerCase().includes(".pdf")) {
      ocrMode.value = false;
      ocrImageUrl.value = data.imageUrl;
      const loadingTask = pdfjsLib.getDocument(data.imageUrl);
      pdfDoc.value = await loadingTask.promise;
      totalPages.value = pdfDoc.value.numPages;

      await nextTick();
      if (viewMode.value === "scroll") setupScrollObserver();
      else renderSinglePage(1);

      preloadOcr(1, 3);
    } else {
      ocrImageUrl.value = data.imageUrl;
      ocrResults.value = data.results;
      totalPages.value = 1;
    }
  } catch (e) {
    console.error("Lỗi nạp dữ liệu OCR:", e);
    // accessDenied.value = true; // Có thể giữ nguyên lỗi màn hình trắng nếu BE lỗi đột ngột
  } finally {
    ocrLoading.value = false;
  }
}

async function loadPdf() {
  if (!props.fileUrl && !props.fileData) return;
  try {
    const source = props.fileData ? { data: props.fileData } : { url: props.fileUrl };
    pdfDoc.value = await pdfjsLib.getDocument(source).promise;
    totalPages.value = pdfDoc.value.numPages;

    const p1 = await pdfDoc.value.getPage(1);
    const vp = p1.getViewport({ scale: scale.value });
    defaultPageHeight.value = vp.height;

    await nextTick();
    if (viewMode.value === "scroll") setupScrollObserver();
    else renderSinglePage(1);

    const ragTemp = [];
    for (let i = 1; i <= pdfDoc.value.numPages; i++) {
      const page = await pdfDoc.value.getPage(i);
      const tc = await page.getTextContent();
      const text = tc.items.map((it) => it.str).join(" ").trim();
      if (text) {
        for (let s = 0; s < text.length; s += 300)
          ragTemp.push({ page: i, text: text.slice(s, s + 400) });
      }
      if (i % 3 === 0) await new Promise((r) => setTimeout(r, 10));
    }
    emit("rag-updated", ragTemp);
  } catch (e) {
    console.error("Lỗi nạp PDF", e);
  }
}

// ==========================================
// 2. RENDER ENGINE 
// ==========================================
async function renderSinglePage(pageNum) {
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

  while (isDisplayRendering) { await new Promise((r) => setTimeout(r, 30)); }
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

    await page.render({ canvasContext: ctx, viewport }).promise;

    if (visionEnabled.value) preloadOcr(pageNum, 1);
  } catch (e) {
    console.error(`renderSinglePage lỗi trang ${pageNum}:`, e);
  } finally {
    isDisplayRendering = false;
  }
}

async function executeRenderPage(pageNum) {
  if (!pdfDoc.value) return;
  await nextTick();
  let canvas = pageCanvases.value[pageNum];
  if (!canvas) {
    await new Promise((r) => setTimeout(r, 50));
    canvas = pageCanvases.value[pageNum];
  }
  if (!canvas) return;
  while (isDisplayRendering) { await new Promise((r) => setTimeout(r, 30)); }
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

    await page.render({ canvasContext: ctx, viewport }).promise;

    if (visionEnabled.value) preloadOcr(pageNum, 1);
  } catch (e) {
    console.error(`executeRenderPage lỗi trang ${pageNum}:`, e);
  } finally {
    isDisplayRendering = false;
  }
}

function processQueue() {
  if (isRendering || renderQueue.length === 0) return;
  isRendering = true;
  renderQueue.sort((a, b) => Math.abs(a - currentPage.value) - Math.abs(b - currentPage.value));
  const p = renderQueue.shift();
  executeRenderPage(p).then(() => {
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
          if (Math.abs(p - currentPage.value) > 15) pageRendered.value[p] = false;
        }
      });

      const rootRect = rootEl.getBoundingClientRect();
      const viewportCenter = rootRect.top + rootRect.height / 2;
      let closestPage = currentPage.value;
      let minDistance = Infinity;

      visiblePagesSet.forEach((p) => {
        const el = pageRefs.value[p];
        if (el) {
          const rect = el.getBoundingClientRect();
          const pageCenter = rect.top + rect.height / 2;
          const distance = Math.abs(pageCenter - viewportCenter);
          if (distance < minDistance) {
            minDistance = distance;
            closestPage = p;
          }
        }
      });

      if (closestPage !== currentPage.value) {
        currentPage.value = closestPage;
        gotoPage.value = closestPage;
        emit("page-changed", closestPage);
        if (visionEnabled.value) preloadOcr(closestPage, 3);
      }
    },
    { root: rootEl, rootMargin: "800px 0px", threshold: [0, 0.1, 0.5, 1] },
  );

  nextTick(() => Object.values(pageRefs.value).forEach((el) => el && scrollObserver.observe(el)));
}

// ==========================================
// 3. BACKGROUND VISION OCR & EVENTS
// ==========================================
function toggleVisionMode() {
  visionEnabled.value = !visionEnabled.value;
  if (visionEnabled.value) preloadOcr(currentPage.value, 3);
}

function preloadOcr(start, count) {
  if (!pdfDoc.value || !props.apiKey) return;
  const end = Math.min(start + count - 1, totalPages.value);
  for (let i = start; i <= end; i++) {
    const s = pageVisionData.value[i]?.status;
    if (s === "done" || s === "processing" || s === "scanning") continue;
    pageVisionData.value = { ...pageVisionData.value, [i]: { status: "scanning", items: [] } };
    visionQueue.value.push(i);
  }
  processVisionQueue();
}

async function processVisionQueue() {
  if (isProcessingVision || visionQueue.value.length === 0) return;
  isProcessingVision = true;
  visionQueue.value.sort((a, b) => Math.abs(a - currentPage.value) - Math.abs(b - currentPage.value));
  const p = visionQueue.value.shift();
  await performOcr(p);
  isProcessingVision = false;
  setTimeout(processVisionQueue, 200);
}

async function performOcr(pageNum) {
  if (pageVisionData.value[pageNum]?.status === "done") return;
  pageVisionData.value = { ...pageVisionData.value, [pageNum]: { status: "processing", items: [] } };

  try {
    const page = await pdfDoc.value.getPage(pageNum);
    const viewport = page.getViewport({ scale: 2.0 });

    const resultsForPage = dbOcrResults.value.filter((r) => r.pageNumber === pageNum);

    if (resultsForPage.length > 0) {
      const items = resultsForPage.map((r) => {
        let b = r.boundingBox;
        if (typeof b === "string") b = JSON.parse(b);
        const xs = b.map((pt) => pt[0] ?? pt.x ?? 0);
        const ys = b.map((pt) => pt[1] ?? pt.y ?? 0);
        return {
          text: r.wordText,
          box_2d: [
            Math.round((Math.min(...ys) / viewport.height) * 1000),
            Math.round((Math.min(...xs) / viewport.width) * 1000),
            Math.round((Math.max(...ys) / viewport.height) * 1000),
            Math.round((Math.max(...xs) / viewport.width) * 1000),
          ],
        };
      });
      pageVisionData.value = { ...pageVisionData.value, [pageNum]: { status: "done", items, fullText: items.map((i) => i.text).join(" ") } };
      return;
    }

    const canvas = document.createElement("canvas");
    const ctx = canvas.getContext("2d");
    canvas.width = Math.floor(viewport.width);
    canvas.height = Math.floor(viewport.height);
    ctx.fillStyle = "#ffffff";
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    await page.render({ canvasContext: ctx, viewport }).promise;

    const blob = await new Promise((resolve) => canvas.toBlob(resolve, "image/jpeg", 0.9));
    const formData = new FormData();
    formData.append("image", blob, `page_${pageNum}.jpg`);

    const response = await fetch(`${config.public.apiBaseUrl}/api/Infer/scan-page-ocr`, {
      method: "POST",
      headers: { Authorization: `Bearer ${getToken()}` },
      body: formData,
    });

    if (!response.ok) throw new Error("API scan-page-ocr lỗi.");
    const data = await response.json();

    const items = (data.results || []).map((r) => {
      let b = r.boundingBox;
      if (typeof b === "string") b = JSON.parse(b);
      const xs = b.map((pt) => pt[0] ?? pt.x ?? 0);
      const ys = b.map((pt) => pt[1] ?? pt.y ?? 0);
      return {
        text: r.wordText,
        box_2d: [
          Math.round((Math.min(...ys) / viewport.height) * 1000),
          Math.round((Math.min(...xs) / viewport.width) * 1000),
          Math.round((Math.max(...ys) / viewport.height) * 1000),
          Math.round((Math.max(...xs) / viewport.width) * 1000),
        ],
      };
    });

    pageVisionData.value = { ...pageVisionData.value, [pageNum]: { status: "done", items, fullText: items.map((i) => i.text).join(" ") } };
  } catch (e) {
    pageVisionData.value = { ...pageVisionData.value, [pageNum]: { status: "error", items: [] } };
  }
}

function retryVision(pageNum) {
  pageVisionData.value = { ...pageVisionData.value, [pageNum]: undefined };
  preloadOcr(pageNum, 1);
}

const getVisionItemStyle = (it) => ({
  position: "absolute",
  left: `${it.box_2d[1] / 10}%`,
  top: `${it.box_2d[0] / 10}%`,
  width: `${(it.box_2d[3] - it.box_2d[1]) / 10}%`,
  height: `${(it.box_2d[2] - it.box_2d[0]) / 10}%`,
});

const isVisionItemSelected = (it) => visionSelectedItems.value.includes(it);

const onVisionItemClick = (it, e) => {
  if (!dragState.value.active) {
    visionSelectedItems.value = [it];
    navigator.clipboard?.writeText(it.text).catch(() => {});
    emit("text-selected", { text: it.text, x: e.clientX, y: e.clientY });
  }
};

function startDrag(e, n) {
  if (e.button !== 0) return;
  const rect = e.currentTarget.getBoundingClientRect();
  const x = e.clientX - rect.left;
  const y = e.clientY - rect.top;
  dragState.value = { active: true, page: n, startX: x, startY: y, endX: x, endY: y, w: rect.width, h: rect.height };
  visionSelectedItems.value = [];
}

function updateDrag(e, n) {
  if (dragState.value.active && dragState.value.page === n) {
    const rect = e.currentTarget.getBoundingClientRect();
    dragState.value.endX = e.clientX - rect.left;
    dragState.value.endY = e.clientY - rect.top;
    _updateSelection(n);
  }
}

function endDrag(e, n) {
  if (!dragState.value.active) return;
  dragState.value.active = false;
  const text = visionSelectedItems.value.map((i) => i.text).join(" ").trim();
  if (text) {
    navigator.clipboard?.writeText(text).catch(() => {});
    emit("text-selected", { text, x: e.clientX, y: e.clientY });
  }
}

function _updateSelection(n) {
  const d = dragState.value;
  const l = (Math.min(d.startX, d.endX) / d.w) * 1000;
  const r = (Math.max(d.startX, d.endX) / d.w) * 1000;
  const t = (Math.min(d.startY, d.endY) / d.h) * 1000;
  const b = (Math.max(d.startY, d.endY) / d.h) * 1000;
  visionSelectedItems.value = (pageVisionData.value[n]?.items || []).filter(
    (i) => i.box_2d[1] < r && i.box_2d[3] > l && i.box_2d[0] < b && i.box_2d[2] > t
  );
}

function getVisionDragRectStyle() {
  return {
    position: "absolute",
    left: `${Math.min(dragState.value.startX, dragState.value.endX)}px`,
    top: `${Math.min(dragState.value.startY, dragState.value.endY)}px`,
    width: `${Math.abs(dragState.value.endX - dragState.value.startX)}px`,
    height: `${Math.abs(dragState.value.endY - dragState.value.startY)}px`,
    background: "rgba(59,130,246,0.15)",
    border: "1px solid rgba(59,130,246,0.7)",
    pointerEvents: "none",
    zIndex: 10,
  };
}

function handleTextSelection(e) {
  if (visionEnabled.value) return;
  const text = window.getSelection()?.toString().trim();
  if (text && text.length > 0 && text.length < 200) {
    navigator.clipboard?.writeText(text).catch(() => {});
    emit("text-selected", { text, x: e.clientX, y: e.clientY });
  }
}

const setViewMode = (m) => {
  if (viewMode.value === m) return;
  viewMode.value = m;
  pageRendered.value = {};
  if (scrollObserver) scrollObserver.disconnect();
  nextTick(() => {
    if (m === "scroll") setupScrollObserver();
    else renderSinglePage(currentPage.value);
  });
};

const zoomIn = () => { scale.value = Math.min(scale.value + 0.2, 4); rerender(); };
const zoomOut = () => { scale.value = Math.max(scale.value - 0.2, 0.4); rerender(); };
const fitWidth = () => rerender();

function rerender() {
  pageRendered.value = {};
  nextTick(() => {
    if (viewMode.value === "scroll") setupScrollObserver();
    else renderSinglePage(currentPage.value);
  });
}

const prevPage = async () => {
  if (currentPage.value > 1) {
    currentPage.value--; gotoPage.value = currentPage.value; emit("page-changed", currentPage.value);
    await renderSinglePage(currentPage.value);
    if (visionEnabled.value) preloadOcr(currentPage.value, 2);
  }
};

const nextPage = async () => {
  if (currentPage.value < totalPages.value) {
    currentPage.value++; gotoPage.value = currentPage.value; emit("page-changed", currentPage.value);
    await renderSinglePage(currentPage.value);
    if (visionEnabled.value) preloadOcr(currentPage.value, 2);
  }
};

const jumpToPage = async () => {
  currentPage.value = Math.max(1, Math.min(gotoPage.value, totalPages.value)); gotoPage.value = currentPage.value; emit("page-changed", currentPage.value);
  await renderSinglePage(currentPage.value);
  if (visionEnabled.value) preloadOcr(currentPage.value, 2);
};

// ==========================================
// 5. OCR MÔI TRƯỜNG ẢNH ĐƠN
// ==========================================
function onOcrImageLoad() {
  if (!ocrImgEl.value) return;
  ocrNaturalW.value = ocrImgEl.value.naturalWidth;
  ocrNaturalH.value = ocrImgEl.value.naturalHeight;
  ocrDisplayW.value = ocrImgEl.value.offsetWidth;
  ocrDisplayH.value = ocrImgEl.value.offsetHeight;
}

function startBboxSelect(e) {
  if (e.button !== 0) return;
  const rect = e.currentTarget.getBoundingClientRect();
  const x = (e.clientX - rect.left) / scale.value;
  const y = (e.clientY - rect.top) / scale.value;
  dragSelect.value = { active: true, startX: x, startY: y, endX: x, endY: y };
  selectedOcrWords.value = [];
}

function updateBboxSelect(e) {
  if (!dragSelect.value.active) return;
  const rect = e.currentTarget.getBoundingClientRect();
  dragSelect.value.endX = (e.clientX - rect.left) / scale.value;
  dragSelect.value.endY = (e.clientY - rect.top) / scale.value;

  const { startX, startY, endX, endY } = dragSelect.value;
  const selLeft = Math.min(startX, endX), selTop = Math.min(startY, endY);
  const selRight = Math.max(startX, endX), selBottom = Math.max(startY, endY);

  if (ocrResults.value) {
    selectedOcrWords.value = ocrResults.value.filter((r) => {
      const b = typeof r.boundingBox === "string" ? JSON.parse(r.boundingBox) : r.boundingBox;
      if (!b) return false;
      const xs = b.map((p) => (Array.isArray(p) ? p[0] : p.x || 0));
      const ys = b.map((p) => (Array.isArray(p) ? p[1] : p.y || 0));
      const scX = ocrNaturalW.value > 0 ? ocrDisplayW.value / ocrNaturalW.value : 1;
      const scY = ocrNaturalH.value > 0 ? ocrDisplayH.value / ocrNaturalH.value : 1;
      return (Math.min(...xs) * scX < selRight && Math.max(...xs) * scX > selLeft && Math.min(...ys) * scY < selBottom && Math.max(...ys) * scY > selTop);
    });
  }
}

function endBboxSelect(e) {
  dragSelect.value.active = false;
  const words = selectedOcrWords.value.map((r) => r.wordText).join(" ").trim();
  if (words) {
    navigator.clipboard?.writeText(words).catch(() => {});
    emit("text-selected", { text: words, x: e.clientX, y: e.clientY });
  }
}

function onBboxClick(r, e) {
  selectedOcrWords.value = [r];
  navigator.clipboard?.writeText(r.wordText).catch(() => {});
  emit("text-selected", { text: r.wordText, x: e.clientX, y: e.clientY });
}

function getDragRectStyle() {
  const { startX, startY, endX, endY } = dragSelect.value;
  return {
    position: "absolute",
    left: `${Math.min(startX, endX)}px`,
    top: `${Math.min(startY, endY)}px`,
    width: `${Math.abs(endX - startX)}px`,
    height: `${Math.abs(endY - startY)}px`,
    background: "rgba(59,130,246,0.15)",
    border: "1px solid rgba(59,130,246,0.7)",
    pointerEvents: "none",
    zIndex: 10,
  };
}

function getOcrBoxStyle(r) {
  const b = typeof r.boundingBox === "string" ? JSON.parse(r.boundingBox) : r.boundingBox;
  if (!b) return { display: "none" };
  const xs = b.map((p) => (Array.isArray(p) ? p[0] : p.x || 0));
  const ys = b.map((p) => (Array.isArray(p) ? p[1] : p.y || 0));
  let x = Math.min(...xs), y = Math.min(...ys), w = Math.max(...xs) - x, h = Math.max(...ys) - y;
  if (ocrNaturalW.value > 0 && ocrDisplayW.value > 0) {
    const scX = ocrDisplayW.value / ocrNaturalW.value;
    const scY = ocrDisplayH.value > 0 ? ocrDisplayH.value / ocrNaturalH.value : scX;
    x *= scX; y *= scY; w *= scX; h *= scY;
  }
  return { position: "absolute", left: `${x}px`, top: `${y}px`, width: `${Math.max(w, 8)}px`, height: `${Math.max(h, 8)}px` };
}

onMounted(() => {
  if (props.jobId) startLoadJob(props.jobId);
  else if (props.fileUrl || props.fileData) loadPdf();
});

onBeforeUnmount(() => {
  if (scrollObserver) scrollObserver.disconnect();
});

defineExpose({ gotoPage, jumpToPage });
</script>
<style scoped>
.textLayer {
  position: absolute;
  inset: 0;
  overflow: hidden;
  pointer-events: auto;
  color: transparent !important;
}
.textLayer--hidden { display: none; }
.textLayer :deep(span) { position: absolute; white-space: pre; cursor: text; user-select: text; }
.textLayer :deep(span::selection) { background: rgba(91, 141, 238, 0.3); color: transparent; }
</style>