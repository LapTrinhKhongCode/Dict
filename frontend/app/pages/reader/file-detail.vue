<template>
  <div class="app-shell">
    <header class="header">
      <button class="btn-back" @click="goBack">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="16" height="16">
          <polyline points="15 18 9 12 15 6"/>
        </svg>
        Thư viện
      </button>

      <div class="header-center">
        <span v-if="pdfName" class="pdf-badge">
          <span class="pdf-icon">⬛</span> {{ pdfName }}
        </span>
      </div>

      <div class="header-right">
        <div class="api-badge" :class="apiKey ? 'ok' : 'warn'">
          <span class="api-dot"></span>
          {{ apiKey ? 'Gemini' : 'Không có API Key' }}
        </div>
        <label class="btn-upload">
          <input type="file" accept=".pdf" @change="handleNewFile" hidden />
          + PDF khác
        </label>
      </div>
    </header>

    <main class="main-content">
      <!-- LEFT: PDF Viewer -->
      <section ref="pdfPanelEl" class="panel pdf-panel">
        <div v-if="!pdfDoc" class="pdf-empty">
          <p class="loading-text">{{ loadingMsg }}</p>
        </div>

        <div v-else class="pdf-viewer-wrap">
          <!-- Toolbar -->
          <div class="pdf-toolbar">
            <div class="view-mode-toggle">
              <button :class="['mode-btn', viewMode === 'single' ? 'active' : '']"
                @click="setViewMode('single')" title="Từng trang">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="14" height="14">
                  <rect x="3" y="3" width="18" height="18" rx="2"/>
                </svg>
              </button>
              <button :class="['mode-btn', viewMode === 'scroll' ? 'active' : '']"
                @click="setViewMode('scroll')" title="Cuộn toàn bộ">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="14" height="14">
                  <rect x="3" y="2" width="18" height="7" rx="1"/>
                  <rect x="3" y="10" width="18" height="7" rx="1"/>
                  <rect x="3" y="18" width="18" height="4" rx="1"/>
                </svg>
              </button>
            </div>

            <div class="tool-sep"></div>

            <template v-if="viewMode === 'single'">
              <button class="tool-btn" @click="prevPage" :disabled="currentPage <= 1">‹</button>
              <span class="page-info">
                <input v-model.number="gotoPage" type="number" class="page-input"
                  :min="1" :max="totalPages" @change="jumpToPage" />
                / {{ totalPages }}
              </span>
              <button class="tool-btn" @click="nextPage" :disabled="currentPage >= totalPages">›</button>
              <div class="tool-sep"></div>
            </template>

            <template v-else>
              <span class="page-info-scroll">Trang {{ currentPage }} / {{ totalPages }}</span>
              <div class="tool-sep"></div>
            </template>

            <button class="tool-btn" @click="zoomOut">−</button>
            <span class="zoom-label">{{ Math.round(scale * 100) }}%</span>
            <button class="tool-btn" @click="zoomIn">+</button>
            <div class="tool-sep"></div>
            <button class="tool-btn" @click="fitWidth" title="Vừa khung">⟺</button>
          </div>

          <!-- Single page mode -->
          <div v-if="viewMode === 'single'" class="pdf-scroll" ref="pdfScrollEl">
            <div class="pdf-page-container">
              <canvas ref="pdfCanvas" class="pdf-canvas"></canvas>
              <!-- Text layer để bôi đen và copy -->
              <div ref="textLayerEl" class="text-layer"></div>
            </div>
          </div>

          <!-- Scroll all mode -->
          <div v-else class="pdf-scroll pdf-scroll--all" ref="pdfScrollAllEl">
            <div class="pdf-pages-wrap" ref="pdfPagesWrapEl">
              <div
                v-for="n in totalPages"
                :key="n"
                class="pdf-page-item"
                :data-page="n"
                :ref="el => { if (el) pageRefs[n] = el }"
              >
                <div class="pdf-page-container">
                  <canvas :ref="el => { if (el) pageCanvases[n] = el }" class="pdf-canvas"></canvas>
                  <div :ref="el => { if (el) textLayerRefs[n] = el }" class="text-layer"></div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <div class="resizer" @mousedown="startResize"></div>

      <!-- RIGHT: Chat -->
      <section ref="chatPanelEl" class="panel chat-panel">
        <div class="chat-header">
          <div class="chat-avatar">G</div>
          <div>
            <p class="chat-title">Trợ lý Gemini</p>
            <p class="chat-sub">{{ apiKey ? 'Đã kết nối' : 'Chưa có API Key' }}</p>
          </div>
          <button class="btn-clear" @click="clearChat">↺</button>
        </div>

        <div class="messages" ref="messagesEl">
          <div v-if="messages.length === 0" class="welcome-wrap">
            <div class="welcome-orb"></div>
            <p class="welcome-title">Xin chào!</p>
            <p class="welcome-sub">Hỏi bất cứ điều gì về tài liệu.</p>
            <div class="quick-btns">
              <button v-for="q in quickQuestions" :key="q" class="quick-btn" @click="sendQuick(q)">
                {{ q }}
              </button>
            </div>
          </div>

          <div v-for="(msg, i) in messages" :key="i" :class="['msg-row', msg.role]">
            <div class="msg-avatar">{{ msg.role === 'user' ? 'U' : 'G' }}</div>
            <div class="msg-bubble" v-html="formatMessage(msg.content)"></div>
          </div>

          <div v-if="isLoading" class="msg-row model">
            <div class="msg-avatar">G</div>
            <div class="msg-bubble typing"><span></span><span></span><span></span></div>
          </div>
        </div>

        <div class="chat-input-wrap">
          <textarea v-model="userInput" class="chat-input" placeholder="Hỏi về tài liệu..."
            rows="1" @keydown.enter.exact.prevent="sendMessage" @input="autoResize" ref="inputEl">
          </textarea>
          <button class="btn-send" @click="sendMessage"
            :disabled="!userInput.trim() || isLoading || !apiKey">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="22" y1="2" x2="11" y2="13"></line>
              <polygon points="22 2 15 22 11 13 2 9 22 2"></polygon>
            </svg>
          </button>
        </div>
        <p v-if="!apiKey" class="no-key-hint">
          ⚠ Chưa có API Key — <a href="/" @click.prevent="goBack">quay lại</a> để nhập
        </p>
      </section>
    </main>
  </div>
</template>

<script setup>
import { ref, shallowRef, nextTick, onMounted, onBeforeUnmount } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useRuntimeConfig } from '#app'
// pdfjs chỉ chạy client-side — không import tĩnh vì gây lỗi SSR DOMMatrix
let pdfjsLib = null

const route = useRoute()
const router = useRouter()

// ── State ────────────────────────────────────────────────────────
const apiKey = ref('')
const pdfName = ref('')
const loadingMsg = ref('Đang tải tài liệu...')
const pdfDoc = shallowRef(null)

// Single page
const pdfCanvas = ref(null)
const pdfScrollEl = ref(null)
const textLayerEl = ref(null)

// Scroll all
const pdfScrollAllEl = ref(null)
const pdfPagesWrapEl = ref(null)
const pageRefs = {}
const pageCanvases = {}
const textLayerRefs = {}
let scrollObserver = null

const pdfPanelEl = ref(null)
const chatPanelEl = ref(null)
const currentPage = ref(1)
const totalPages = ref(0)
const scale = ref(1.2)
const gotoPage = ref(1)
const viewMode = ref('single')

let renderTask = null
let isResizing = false

const messages = ref([])
const userInput = ref('')
const isLoading = ref(false)
const messagesEl = ref(null)
const inputEl = ref(null)

const quickQuestions = ['Tóm tắt tài liệu này', 'Các điểm chính là gì?', 'Giải thích trang hiện tại']

// ── Navigation ────────────────────────────────────────────────────
function goBack() { router.push('/') }

// ── Load PDF từ sessionStorage ────────────────────────────────────
async function loadFromSession(id) {
  const raw = sessionStorage.getItem(`pdf_${id}`)
  if (!raw) {
    loadingMsg.value = 'Không tìm thấy file. Vui lòng quay lại thư viện.'
    return
  }
  const arr = new Uint8Array(JSON.parse(raw))
  await loadPdf(arr)
}

// Dynamic import — chỉ chạy ở browser, tránh lỗi SSR
async function initPdfJs() {
  if (pdfjsLib) return
  pdfjsLib = await import('pdfjs-dist')
  const workerUrl = new URL('pdfjs-dist/build/pdf.worker.min.mjs', import.meta.url).href
  pdfjsLib.GlobalWorkerOptions.workerSrc = workerUrl
}

async function loadPdf(data) {
  await initPdfJs()
  pdfDoc.value = await pdfjsLib.getDocument({ data }).promise
  totalPages.value = pdfDoc.value.numPages
  currentPage.value = 1
  gotoPage.value = 1
  await nextTick()
  await fitWidth()
}

// ── Upload file mới thẳng từ reader ──────────────────────────────
async function handleNewFile(e) {
  const file = e.target.files?.[0]
  if (!file) return
  pdfName.value = file.name
  const ab = await file.arrayBuffer()
  const id = `${file.name}_${file.size}_${Date.now()}`
  sessionStorage.setItem(`pdf_${id}`, JSON.stringify(Array.from(new Uint8Array(ab))))
  router.replace({ path: '/reader', query: { id, name: file.name } })
  await loadPdf(new Uint8Array(ab))
  e.target.value = ''
}

// ── Text Layer (cho phép bôi đen, copy) ──────────────────────────
async function renderTextLayer(page, viewport, containerEl) {
  if (!containerEl) return

  // Xóa text layer cũ
  containerEl.innerHTML = ''
  containerEl.style.width = `${Math.floor(viewport.width)}px`
  containerEl.style.height = `${Math.floor(viewport.height)}px`

  const textContent = await page.getTextContent()

  await pdfjsLib.renderTextLayer({
    textContentSource: textContent,
    container: containerEl,
    viewport,
    textDivs: [],
  }).promise
}

// ── Single Page Render ────────────────────────────────────────────
async function renderPage(pageNum = currentPage.value) {
  if (!pdfDoc.value || !pdfCanvas.value) return

  if (renderTask) {
    try { renderTask.cancel() } catch {}
    renderTask = null
  }

  const page = await pdfDoc.value.getPage(pageNum)
  const viewport = page.getViewport({ scale: scale.value })

  const canvas = pdfCanvas.value
  const ctx = canvas.getContext('2d')
  if (!ctx) return

  const dpr = window.devicePixelRatio || 1
  canvas.width = Math.floor(viewport.width * dpr)
  canvas.height = Math.floor(viewport.height * dpr)
  canvas.style.width = `${Math.floor(viewport.width)}px`
  canvas.style.height = `${Math.floor(viewport.height)}px`
  ctx.setTransform(dpr, 0, 0, dpr, 0, 0)

  try {
    renderTask = page.render({ canvasContext: ctx, viewport, intent: 'display' })
    await renderTask.promise
  } catch (err) {
    if (err?.name !== 'RenderingCancelledException') console.error(err)
  } finally {
    renderTask = null
  }

  // Render text layer sau khi canvas xong
  await renderTextLayer(page, viewport, textLayerEl.value)
}

async function prevPage() {
  if (currentPage.value <= 1) return
  currentPage.value--; gotoPage.value = currentPage.value
  await renderPage()
}
async function nextPage() {
  if (currentPage.value >= totalPages.value) return
  currentPage.value++; gotoPage.value = currentPage.value
  await renderPage()
}
async function jumpToPage() {
  const p = Math.max(1, Math.min(Number(gotoPage.value) || 1, totalPages.value))
  currentPage.value = p; gotoPage.value = p
  if (viewMode.value === 'single') await renderPage()
  else scrollToPage(p)
}
async function fitWidth() {
  if (!pdfDoc.value) return
  const containerEl = viewMode.value === 'single' ? pdfScrollEl.value : pdfScrollAllEl.value
  if (!containerEl) return
  const page = await pdfDoc.value.getPage(currentPage.value)
  const vp = page.getViewport({ scale: 1 })
  scale.value = +((containerEl.clientWidth - 48) / vp.width).toFixed(2)
  if (viewMode.value === 'single') await renderPage()
  else await renderAllPages()
}
async function zoomIn() {
  scale.value = Math.min(3, +(scale.value + 0.2).toFixed(1))
  if (viewMode.value === 'single') await renderPage()
  else await renderAllPages()
}
async function zoomOut() {
  scale.value = Math.max(0.3, +(scale.value - 0.2).toFixed(1))
  if (viewMode.value === 'single') await renderPage()
  else await renderAllPages()
}

// ── Scroll All Pages ──────────────────────────────────────────────
async function initScrollMode() {
  await nextTick()
  await nextTick()
  await renderAllPages()
  setupScrollObserver()
}

async function renderOnePageScroll(pageNum) {
  if (!pdfDoc.value) return
  const canvas = pageCanvases[pageNum]
  if (!canvas) return

  const page = await pdfDoc.value.getPage(pageNum)
  const viewport = page.getViewport({ scale: scale.value })
  const ctx = canvas.getContext('2d')
  if (!ctx) return

  const dpr = window.devicePixelRatio || 1
  canvas.width = Math.floor(viewport.width * dpr)
  canvas.height = Math.floor(viewport.height * dpr)
  canvas.style.width = `${Math.floor(viewport.width)}px`
  canvas.style.height = `${Math.floor(viewport.height)}px`
  ctx.setTransform(dpr, 0, 0, dpr, 0, 0)

  try {
    const task = page.render({ canvasContext: ctx, viewport, intent: 'display' })
    await task.promise
  } catch (err) {
    if (err?.name !== 'RenderingCancelledException') console.error(err)
  }

  // Text layer cho scroll mode
  await renderTextLayer(page, viewport, textLayerRefs[pageNum])
}

async function renderAllPages() {
  if (!pdfDoc.value) return
  await nextTick()
  for (let i = 1; i <= totalPages.value; i++) {
    await renderOnePageScroll(i)
  }
}

function setupScrollObserver() {
  if (scrollObserver) scrollObserver.disconnect()
  scrollObserver = new IntersectionObserver(
    (entries) => {
      let maxRatio = 0, visiblePage = currentPage.value
      entries.forEach(entry => {
        const p = parseInt(entry.target.dataset.page)
        if (entry.intersectionRatio > maxRatio) { maxRatio = entry.intersectionRatio; visiblePage = p }
      })
      if (maxRatio > 0) { currentPage.value = visiblePage; gotoPage.value = visiblePage }
    },
    { root: pdfScrollAllEl.value, threshold: [0, 0.25, 0.5, 0.75, 1] }
  )
  nextTick(() => Object.values(pageRefs).forEach(el => el && scrollObserver.observe(el)))
}

function scrollToPage(pageNum) {
  const el = pageRefs[pageNum]
  if (el && pdfScrollAllEl.value) {
    pdfScrollAllEl.value.scrollTo({ top: el.offsetTop - 16, behavior: 'smooth' })
  }
}

async function setViewMode(mode) {
  if (viewMode.value === mode) return
  viewMode.value = mode
  if (scrollObserver) { scrollObserver.disconnect(); scrollObserver = null }
  await nextTick()
  if (mode === 'single') await renderPage(currentPage.value)
  else await initScrollMode()
}

// ── Resizer ──────────────────────────────────────────────────────
function startResize() {
  isResizing = true
  document.addEventListener('mousemove', doResize)
  document.addEventListener('mouseup', stopResize)
}
function doResize(e) {
  if (!isResizing || !pdfPanelEl.value || !chatPanelEl.value) return
  const pct = Math.max(30, Math.min(70, (e.clientX / window.innerWidth) * 100))
  pdfPanelEl.value.style.width = `${pct}%`
  chatPanelEl.value.style.width = `${100 - pct - 0.4}%`
}
function stopResize() {
  isResizing = false
  document.removeEventListener('mousemove', doResize)
  document.removeEventListener('mouseup', stopResize)
}

// ── Gemini Chat ───────────────────────────────────────────────────
const GEMINI_MODELS = ['gemini-2.5-flash', 'gemini-2.0-flash-lite']

async function callGemini(model, systemContext, history, text) {
  const res = await fetch(
    `https://generativelanguage.googleapis.com/v1beta/models/${model}:generateContent?key=${apiKey.value}`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        system_instruction: { parts: [{ text: systemContext }] },
        contents: [...history, { role: 'user', parts: [{ text }] }],
        generationConfig: { temperature: 0.7, maxOutputTokens: 2048 }
      })
    }
  )
  const data = await res.json()
  if (data.error) throw { message: data.error.message, status: data.error.status }
  return data.candidates?.[0]?.content?.parts?.[0]?.text || 'Không có phản hồi.'
}

async function sendMessage() {
  const text = userInput.value.trim()
  if (!text || isLoading.value || !apiKey.value) return

  messages.value.push({ role: 'user', content: text })
  userInput.value = ''
  if (inputEl.value) inputEl.value.style.height = 'auto'
  isLoading.value = true
  await scrollBottom()

  try {
    const history = messages.value.slice(0, -1).map(m => ({
      role: m.role === 'user' ? 'user' : 'model',
      parts: [{ text: m.content }]
    }))
    const systemContext = pdfName.value
      ? `Bạn là trợ lý AI hỗ trợ đọc PDF "${pdfName.value}". Trang hiện tại: ${currentPage.value}/${totalPages.value}. Trả lời bằng tiếng Việt, rõ ràng và hữu ích.`
      : 'Bạn là trợ lý AI hữu ích. Trả lời bằng tiếng Việt.'

    let reply = null, lastError = null
    for (const model of GEMINI_MODELS) {
      try { reply = await callGemini(model, systemContext, history, text); break }
      catch (err) {
        lastError = err
        if (err.status === 'RESOURCE_EXHAUSTED') continue
        throw err
      }
    }
    if (!reply) {
      const m = lastError?.message?.match(/retry in (\d+)s/i)
      throw new Error(`Hết quota.${m ? ` Thử lại sau ${Math.ceil(m[1]/60)} phút.` : ''}`)
    }
    messages.value.push({ role: 'model', content: reply })
  } catch (err) {
    const isQuota = err.message?.includes('quota') || err.message?.includes('RESOURCE_EXHAUSTED')
    messages.value.push({
      role: 'model',
      content: isQuota
        ? `⚠️ **Hết quota API**\n\n1. Chờ quota reset\n2. Lấy API Key mới tại aistudio.google.com`
        : `❌ Lỗi: ${err.message}`
    })
  } finally {
    isLoading.value = false
    await scrollBottom()
  }
}

async function sendQuick(q) { userInput.value = q; await sendMessage() }
function clearChat() { messages.value = [] }
async function scrollBottom() {
  await nextTick()
  if (messagesEl.value) messagesEl.value.scrollTop = messagesEl.value.scrollHeight
}
function autoResize(e) {
  e.target.style.height = 'auto'
  e.target.style.height = Math.min(e.target.scrollHeight, 140) + 'px'
}
function formatMessage(text) {
  return text
    .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
    .replace(/\*(.*?)\*/g, '<em>$1</em>')
    .replace(/`([^`]+)`/g, '<code>$1</code>')
    .replace(/\n/g, '<br />')
}

function handleWindowResize() { if (pdfDoc.value) fitWidth() }

onMounted(async () => {
  // Lấy API key từ .env qua runtimeConfig
  const config = useRuntimeConfig()
  apiKey.value = config.public.geminiApiKey || ''

  // Lấy thông tin file từ query
  const id = route.query.id
  pdfName.value = route.query.name || ''

  if (id) await loadFromSession(id)
  else loadingMsg.value = 'Không có file được chọn.'

  window.addEventListener('resize', handleWindowResize)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', handleWindowResize)
  stopResize()
  if (scrollObserver) scrollObserver.disconnect()
})
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=DM+Sans:wght@300;400;500;600&family=Playfair+Display:wght@600&display=swap');

* { box-sizing: border-box; margin: 0; padding: 0; }

.app-shell {
  --bg: #0e0f11;
  --surface: #16181c;
  --surface2: #1e2026;
  --border: #2a2d35;
  --accent: #f0c040;
  --accent2: #5b8dee;
  --text: #e8eaf0;
  --text-muted: #6b7280;
  font-family: 'DM Sans', sans-serif;
  background: var(--bg);
  color: var(--text);
  height: 100vh;
  display: flex; flex-direction: column;
  overflow: hidden;
}

/* Header */
.header {
  display: flex; align-items: center; justify-content: space-between;
  padding: 0 20px; height: 52px;
  background: var(--surface); border-bottom: 1px solid var(--border);
  flex-shrink: 0; gap: 12px;
}
.btn-back {
  display: flex; align-items: center; gap: 6px;
  background: transparent; border: 1px solid var(--border);
  color: var(--text-muted); border-radius: 8px; padding: 5px 12px;
  font-size: 13px; cursor: pointer; transition: all 0.2s; font-family: inherit;
  white-space: nowrap; flex-shrink: 0;
}
.btn-back:hover { border-color: var(--accent); color: var(--accent); }
.header-center { flex: 1; display: flex; justify-content: center; }
.pdf-badge {
  display: flex; align-items: center; gap: 8px;
  background: var(--surface2); border: 1px solid var(--border);
  border-radius: 20px; padding: 4px 14px; font-size: 13px;
  color: var(--text-muted); max-width: 340px;
  overflow: hidden; white-space: nowrap; text-overflow: ellipsis;
}
.pdf-icon { font-size: 10px; }
.header-right { display: flex; align-items: center; gap: 10px; flex-shrink: 0; }
.api-badge {
  display: flex; align-items: center; gap: 7px;
  border-radius: 20px; padding: 5px 12px; font-size: 12px;
  border: 1px solid var(--border); color: var(--text-muted);
}
.api-badge.ok { border-color: #166534; color: #4ade80; }
.api-badge.warn { border-color: #92400e; color: #f59e0b; }
.api-dot { width: 6px; height: 6px; border-radius: 50%; background: currentColor; }
.btn-upload {
  display: flex; align-items: center; gap: 6px;
  background: var(--accent); color: #111; border-radius: 8px;
  padding: 5px 14px; font-size: 13px; font-weight: 600;
  cursor: pointer; transition: opacity 0.2s; font-family: inherit; border: none;
}
.btn-upload:hover { opacity: 0.85; }

/* Layout */
.main-content { display: flex; flex: 1; overflow: hidden; }
.panel { display: flex; flex-direction: column; overflow: hidden; }
.pdf-panel { width: 55%; background: var(--bg); min-width: 320px; }
.chat-panel { flex: 1; background: var(--surface); border-left: 1px solid var(--border); min-width: 280px; }
.resizer { width: 5px; background: var(--border); cursor: col-resize; flex-shrink: 0; transition: background 0.2s; }
.resizer:hover { background: var(--accent); }

/* Loading */
.pdf-empty {
  flex: 1; display: flex; align-items: center; justify-content: center;
}
.loading-text { font-size: 15px; color: var(--text-muted); }

/* Toolbar */
.pdf-viewer-wrap { flex: 1; display: flex; flex-direction: column; overflow: hidden; }
.pdf-toolbar {
  display: flex; align-items: center; gap: 4px; padding: 7px 12px;
  background: var(--surface); border-bottom: 1px solid var(--border); flex-shrink: 0;
}
.view-mode-toggle {
  display: flex; background: var(--surface2);
  border: 1px solid var(--border); border-radius: 6px; overflow: hidden;
}
.mode-btn {
  display: flex; align-items: center; justify-content: center;
  width: 30px; height: 28px;
  background: transparent; border: none;
  color: var(--text-muted); cursor: pointer; transition: all 0.15s;
}
.mode-btn:hover { color: var(--text); }
.mode-btn.active { background: var(--accent); color: #111; }

.tool-btn {
  width: 30px; height: 28px; background: var(--surface2);
  border: 1px solid var(--border); color: var(--text); border-radius: 6px;
  cursor: pointer; font-size: 15px; display: flex; align-items: center; justify-content: center;
  transition: all 0.15s; font-family: inherit; flex-shrink: 0;
}
.tool-btn:hover:not(:disabled) { background: var(--accent); color: #111; border-color: var(--accent); }
.tool-btn:disabled { opacity: 0.4; cursor: not-allowed; }
.page-info { display: flex; align-items: center; gap: 5px; font-size: 13px; color: var(--text-muted); padding: 0 2px; }
.page-info-scroll { font-size: 12px; color: var(--text-muted); padding: 0 4px; white-space: nowrap; }
.page-input {
  width: 40px; background: var(--surface2); border: 1px solid var(--border);
  color: var(--text); border-radius: 5px; padding: 2px 4px; font-size: 13px;
  text-align: center; font-family: inherit; outline: none;
}
.page-input:focus { border-color: var(--accent); }
.page-input::-webkit-inner-spin-button { display: none; }
.zoom-label { font-size: 12px; color: var(--text-muted); min-width: 36px; text-align: center; }
.tool-sep { width: 1px; height: 20px; background: var(--border); margin: 0 3px; flex-shrink: 0; }

/* PDF scroll areas */
.pdf-scroll {
  flex: 1; overflow: auto;
  display: flex; justify-content: center; align-items: flex-start;
  padding: 24px; background: #cacaca;
}
.pdf-scroll--all { display: block; padding: 16px; }
.pdf-pages-wrap {
  display: flex; flex-direction: column;
  gap: 12px; align-items: center;
  width: 100%; padding-top: 8px;
}
.pdf-page-item { display: flex; justify-content: center; flex-shrink: 0; width: 100%; }

/* Page container — position relative để text layer overlay đúng vị trí */
.pdf-page-container {
  position: relative;
  display: inline-block;
  line-height: 0;
}

.pdf-canvas {
  display: block;
  background: #fff;
  box-shadow: 0 2px 16px rgba(0,0,0,0.35);
}

/* ── Text Layer — cho phép bôi đen và copy ── */
.text-layer {
  position: absolute;
  top: 0; left: 0;
  width: 100%; height: 100%;
  overflow: hidden;
  opacity: 0.25;
  line-height: 1;
  /* Quan trọng: để text layer ở trên canvas */
  z-index: 2;
}

/* Khi hover/select thì opacity tăng lên để dễ thấy selection */
.text-layer:has(::selection) { opacity: 1; }

/* Style các span text của pdfjs */
.text-layer :deep(span) {
  color: transparent;
  position: absolute;
  white-space: pre;
  cursor: text;
  transform-origin: 0% 0%;
}

.text-layer :deep(span::selection) {
  background: rgba(91, 141, 238, 0.3);
  color: transparent;
}

/* Chat */
.chat-header {
  display: flex; align-items: center; gap: 12px; padding: 14px 20px;
  border-bottom: 1px solid var(--border); flex-shrink: 0;
}
.chat-avatar {
  width: 34px; height: 34px;
  background: linear-gradient(135deg, var(--accent2), #8b5cf6);
  border-radius: 10px; display: flex; align-items: center; justify-content: center;
  font-weight: 700; font-size: 14px; flex-shrink: 0;
}
.chat-title { font-size: 15px; font-weight: 600; }
.chat-sub { font-size: 12px; color: var(--text-muted); }
.btn-clear {
  margin-left: auto; background: transparent; border: 1px solid var(--border);
  color: var(--text-muted); width: 30px; height: 30px; border-radius: 8px;
  cursor: pointer; font-size: 15px; display: flex; align-items: center; justify-content: center; transition: all 0.2s;
}
.btn-clear:hover { border-color: #ef4444; color: #ef4444; }

.messages {
  flex: 1; overflow-y: auto; padding: 20px;
  display: flex; flex-direction: column; gap: 16px;
  scrollbar-width: thin; scrollbar-color: var(--border) transparent;
}
.messages::-webkit-scrollbar { width: 4px; }
.messages::-webkit-scrollbar-thumb { background: var(--border); border-radius: 4px; }

.welcome-wrap { flex: 1; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 12px; text-align: center; padding: 20px; }
.welcome-orb { width: 56px; height: 56px; background: radial-gradient(circle, var(--accent2), #8b5cf6 60%, transparent); border-radius: 50%; margin-bottom: 8px; animation: pulse 3s ease-in-out infinite; }
@keyframes pulse { 0%, 100% { transform: scale(1); opacity: 0.9; } 50% { transform: scale(1.08); opacity: 1; } }
.welcome-title { font-size: 18px; font-weight: 600; }
.welcome-sub { font-size: 13px; color: var(--text-muted); }
.quick-btns { display: flex; flex-direction: column; gap: 8px; margin-top: 10px; width: 100%; max-width: 260px; }
.quick-btn {
  background: var(--surface2); border: 1px solid var(--border); color: var(--text-muted);
  border-radius: 8px; padding: 8px 12px; font-size: 13px; cursor: pointer;
  text-align: left; transition: all 0.2s; font-family: inherit;
}
.quick-btn:hover { border-color: var(--accent2); color: var(--text); }

.msg-row { display: flex; gap: 10px; align-items: flex-start; animation: slideIn 0.2s ease; }
@keyframes slideIn { from { opacity: 0; transform: translateY(8px); } to { opacity: 1; transform: translateY(0); } }
.msg-row.user { flex-direction: row-reverse; }
.msg-avatar { width: 28px; height: 28px; border-radius: 8px; display: flex; align-items: center; justify-content: center; font-size: 11px; font-weight: 700; flex-shrink: 0; }
.user .msg-avatar { background: var(--accent); color: #111; }
.model .msg-avatar { background: linear-gradient(135deg, var(--accent2), #8b5cf6); }
.msg-bubble { max-width: 82%; padding: 10px 14px; border-radius: 12px; font-size: 14px; line-height: 1.6; }
.user .msg-bubble { background: var(--accent); color: #111; border-bottom-right-radius: 4px; }
.model .msg-bubble { background: var(--surface2); border: 1px solid var(--border); color: var(--text); border-bottom-left-radius: 4px; }
.msg-bubble code { background: rgba(0,0,0,0.3); padding: 1px 5px; border-radius: 4px; font-family: monospace; font-size: 12px; }

.typing { display: flex; gap: 5px; align-items: center; padding: 12px 16px; }
.typing span { width: 7px; height: 7px; background: var(--text-muted); border-radius: 50%; animation: bounce 1.2s infinite; }
.typing span:nth-child(2) { animation-delay: 0.2s; }
.typing span:nth-child(3) { animation-delay: 0.4s; }
@keyframes bounce { 0%, 80%, 100% { transform: translateY(0); } 40% { transform: translateY(-6px); } }

.chat-input-wrap {
  display: flex; align-items: flex-end; gap: 10px; padding: 12px 14px;
  border-top: 1px solid var(--border); flex-shrink: 0; background: var(--surface);
}
.chat-input {
  flex: 1; background: var(--surface2); border: 1px solid var(--border);
  color: var(--text); border-radius: 10px; padding: 10px 14px; font-size: 14px;
  resize: none; max-height: 140px; overflow-y: auto; font-family: inherit;
  line-height: 1.5; outline: none; transition: border-color 0.2s;
}
.chat-input:focus { border-color: var(--accent2); }
.chat-input::placeholder { color: var(--text-muted); }
.btn-send {
  width: 38px; height: 38px; background: var(--accent2); border: none;
  border-radius: 10px; cursor: pointer; display: flex; align-items: center; justify-content: center;
  flex-shrink: 0; transition: all 0.2s; color: white;
}
.btn-send:hover:not(:disabled) { background: #4a7de0; transform: translateY(-1px); }
.btn-send:disabled { opacity: 0.4; cursor: not-allowed; }
.btn-send svg { width: 15px; height: 15px; }

.no-key-hint { text-align: center; font-size: 12px; color: #f59e0b; padding: 0 16px 10px; }
.no-key-hint a { color: var(--accent); text-decoration: underline; cursor: pointer; }
</style>