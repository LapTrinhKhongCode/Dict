<template>
  <div class="app-shell">
    <header class="header">
      <button class="btn-back" @click="goBack">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="16" height="16">
          <polyline points="15 18 9 12 15 6"/>
        </svg>
        Quay lại
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
      </div>
    </header>

    <main class="main-content">
      <section ref="pdfPanelEl" class="panel pdf-panel">
        <!-- OCR Mode -->
        <div v-if="ocrMode" class="pdf-viewer-wrap">
          <div class="pdf-toolbar">
            <span class="page-info-scroll" style="flex:1">
              {{ pdfName || 'Tài liệu OCR' }}
            </span>
            <div class="tool-sep"></div>
            <button class="tool-btn" @click="zoomOut">−</button>
            <span class="zoom-label">{{ Math.round(scale * 100) }}%</span>
            <button class="tool-btn" @click="zoomIn">+</button>
          </div>
          <div class="pdf-scroll" ref="pdfScrollEl">
            <div v-if="ocrLoading" class="loading-wrap" style="padding:40px">
              <div class="spinner"></div>
              <p class="loading-text">AI đang đọc tài liệu...</p>
            </div>
            <div v-else-if="ocrImageUrl" class="ocr-container" :style="`transform:scale(${scale});transform-origin:top center`">
              <!-- Ảnh gốc -->
              <img :src="ocrImageUrl" class="ocr-image" ref="ocrImgEl"
                @load="onOcrImageLoad" />
              <!-- Text overlay từ OCR results -->
              <div v-if="ocrResults" class="ocr-overlay">
                <div
                  v-for="(r, i) in ocrResults" :key="i"
                  class="ocr-text-box"
                  :style="getOcrBoxStyle(r)"
                  :title="r.wordText"
                  @click="copyWord(r.wordText)"
                >
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- PDF Mode -->
        <div v-else-if="!pdfDoc" class="pdf-empty">
          <div v-if="loadingPdf" class="loading-wrap">
            <div class="spinner"></div>
            <p class="loading-text">Đang tải tài liệu...</p>
          </div>
          <p v-else class="loading-text">{{ loadingMsg }}</p>
        </div>

        <div v-else class="pdf-viewer-wrap">
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

          <!-- Single page -->
          <div v-if="viewMode === 'single'" class="pdf-scroll" ref="pdfScrollEl">
            <div class="pdf-page-container">
              <canvas ref="pdfCanvas" class="pdf-canvas"></canvas>
              <div ref="textLayerEl" class="textLayer"></div>
            </div>
          </div>

          <!-- Scroll all -->
          <div v-else class="pdf-scroll pdf-scroll--all" ref="pdfScrollAllEl">
            <div class="pdf-pages-wrap">
              <div v-for="n in totalPages" :key="n"
                class="pdf-page-item" :data-page="n"
                :ref="el => { if (el) pageRefs[n] = el }">
                <div class="pdf-page-container">
                  <canvas :ref="el => { if (el) pageCanvases[n] = el }" class="pdf-canvas"></canvas>
                  <div :ref="el => { if (el) textLayerRefs[n] = el }" class="textLayer"></div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      <div class="resizer" @mousedown="startResize"></div>

      <!-- Vocab Popup Tooltip -->
      <Transition name="popup">
        <div v-if="vocabPopup.visible" class="vocab-popup"
          :style="{ top: vocabPopup.y + 'px', left: vocabPopup.x + 'px' }">
          <div class="vocab-popup-word">{{ vocabPopup.word }}</div>

          <!-- Loading tra từ -->
          <div v-if="vocabPopup.loading" class="vocab-popup-loading">
            <div class="mini-spinner"></div> Đang tra...
          </div>

          <!-- Nghĩa tra được (pre-fill) + cho phép sửa -->
          <input v-if="!vocabPopup.loading"
            v-model="vocabPopup.editMeaning"
            class="vocab-popup-input"
            placeholder="Nhập hoặc sửa nghĩa..." />

          <div class="vocab-popup-actions">
            <button class="vocab-btn-save" @click="saveVocab"
              :disabled="vocabPopup.saving">
              {{ vocabPopup.saving ? '...' : '💾 Lưu' }}
            </button>
            <button class="vocab-btn-close" @click="closeVocabPopup">✕</button>
          </div>
        </div>
      </Transition>

      <section ref="chatPanelEl" class="panel chat-panel">
        <!-- Tab switcher -->
        <div class="panel-tabs">
          <button :class="['panel-tab', activeTab === 'chat' ? 'active' : '']"
            @click="activeTab = 'chat'">
            💬 Chat
          </button>
          <button :class="['panel-tab', activeTab === 'vocab' ? 'active' : '']"
            @click="activeTab = 'vocab'">
            📚 Từ vựng
            <span v-if="vocabList.length" class="tab-badge">{{ vocabList.length }}</span>
          </button>
        </div>

        <!-- Chat tab -->
        <template v-if="activeTab === 'chat'">
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
              <button v-for="q in quickQuestions" :key="q" class="quick-btn" @click="sendQuick(q)">{{ q }}</button>
            </div>
          </div>

          <div v-for="(msg, i) in messages" :key="i" :class="['msg-row', msg.role]">
            <div class="msg-avatar">{{ msg.role === 'user' ? 'U' : 'G' }}</div>
            <div class="msg-bubble">
              <!-- Hiển thị ảnh đính kèm nếu có -->
              <img v-if="msg.image" :src="msg.image" class="msg-image" />
              <div v-html="formatMessage(msg.content)"></div>
            </div>
          </div>

          <div v-if="isLoading" class="msg-row model">
            <div class="msg-avatar">G</div>
            <div class="msg-bubble typing"><span></span><span></span><span></span></div>
          </div>
        </div>

        <!-- Preview ảnh đã chụp -->
        <div v-if="capturedImage" class="capture-preview">
          <img :src="capturedImage" class="capture-thumb" />
          <div class="capture-info">
            <span class="capture-label">📷 Trang {{ currentPage }}</span>
            <button class="capture-remove" @click="capturedImage = null" title="Bỏ ảnh">✕</button>
          </div>
        </div>

        <div class="chat-input-wrap">
          <textarea v-model="userInput" class="chat-input" placeholder="Hỏi về tài liệu... (Ctrl+V để dán ảnh)"
            rows="1"
            @keydown.enter.exact.prevent="sendMessage"
            @input="autoResize"
            @paste="handlePaste"
            ref="inputEl">
          </textarea>
          <button class="btn-send" @click="sendMessage"
            :disabled="!userInput.trim() || isLoading || !apiKey">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <line x1="22" y1="2" x2="11" y2="13"></line>
              <polygon points="22 2 15 22 11 13 2 9 22 2"></polygon>
            </svg>
          </button>
        </div>
        <p v-if="!apiKey" class="no-key-hint">⚠ Chưa có API Key</p>
        </template>

        <!-- Vocab tab -->
        <template v-if="activeTab === 'vocab'">
          <div class="vocab-header">
            <span class="vocab-header-title">Từ vựng đã lưu</span>
            <button v-if="vocabList.length" class="btn-create-deck" @click="showDeckModal = true">
              + Tạo Flashcard
            </button>
          </div>

          <div class="vocab-list">
            <div v-if="vocabList.length === 0" class="vocab-empty">
              Bôi đen từ trong PDF để lưu từ vựng
            </div>
            <div v-for="(v, i) in vocabList" :key="i" class="vocab-item">
              <div class="vocab-item-word">{{ v.wordText }}</div>
              <div class="vocab-item-meaning">{{ v.contextMeaning }}</div>
              <button class="vocab-item-del" @click="removeVocab(i)">✕</button>
            </div>
          </div>
        </template>
      </section>

      <!-- Modal tạo Deck từ vocab -->
      <Transition name="modal">
        <div v-if="showDeckModal"
          class="fixed inset-0 bg-black/70 backdrop-blur-sm flex items-center justify-center z-50 p-4"
          @click.self="showDeckModal = false">
          <div class="deck-modal">
            <h2 class="deck-modal-title">Tạo Flashcard từ từ vựng</h2>

            <div class="deck-modal-section">
              <label class="deck-label">Tên bộ thẻ</label>
              <input v-model="deckForm.name" class="deck-input"
                :placeholder="pdfName || 'Tên bộ thẻ...'" />
            </div>

            <div class="deck-modal-section">
              <label class="deck-label">Chọn từ để tạo thẻ</label>
              <div class="deck-select-all">
                <input type="checkbox" id="selectAll" :checked="selectedVocabIndices.length === vocabList.length"
                  @change="toggleSelectAll" />
                <label for="selectAll">Chọn tất cả ({{ vocabList.length }} từ)</label>
              </div>
              <div class="deck-vocab-picks">
                <label v-for="(v, i) in vocabList" :key="i" class="deck-vocab-pick">
                  <input type="checkbox" :value="i" v-model="selectedVocabIndices" />
                  <span class="pick-word">{{ v.wordText }}</span>
                  <span class="pick-meaning">{{ v.contextMeaning }}</span>
                </label>
              </div>
            </div>

            <div class="deck-modal-actions">
              <button class="btn-cancel" @click="showDeckModal = false">Hủy</button>
              <button class="btn-create"
                :disabled="!selectedVocabIndices.length || creatingDeck"
                @click="createDeckFromVocab">
                {{ creatingDeck ? 'Đang tạo...' : `Tạo ${selectedVocabIndices.length} thẻ` }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </main>
  </div>
</template>

<script setup>
definePageMeta({ layout: 'reader', ssr: false })

import { ref, shallowRef, nextTick, onMounted, onBeforeUnmount } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useRuntimeConfig } from '#app'
import { useJwt } from '~/composables/useJwt'
import { useToast } from '~/composables/useToast'

let pdfjsLib = null

const route = useRoute()
const router = useRouter()
const { jwt } = useJwt()
const { showToast } = useToast()

// Helper lấy token — ưu tiên localStorage (consistent với các page khác)
const getToken = () => localStorage.getItem('jwt_token') || jwt.value || ''

const apiKey = ref('')
const pdfName = ref('')
const loadingMsg = ref('Không có file được chọn.')
const loadingPdf = ref(false)
const pdfDoc = shallowRef(null)

// ── OCR Mode state ───────────────────────────────────────────────
const ocrMode = ref(false)       // true khi đọc từ OCR thay vì PDF
const ocrImageUrl = ref('')      // URL ảnh từ OCR
const ocrResults = ref(null)     // kết quả OCR [{wordText, boundingBox}]
const ocrFullText = ref('')      // toàn bộ text OCR để đưa vào Gemini
const ocrLoading = ref(false)
const ocrImgEl = ref(null)
const ocrNaturalW = ref(0)
const ocrNaturalH = ref(0)
const ocrDisplayW = ref(0)
const ocrDisplayH = ref(0)

async function onOcrImageLoad() {
  if (!ocrImgEl.value) return
  await nextTick()
  ocrNaturalW.value = ocrImgEl.value.naturalWidth
  ocrNaturalH.value = ocrImgEl.value.naturalHeight
  ocrDisplayW.value = ocrImgEl.value.offsetWidth
  ocrDisplayH.value = ocrImgEl.value.offsetHeight
  if (window._ocrResizeObs) window._ocrResizeObs.disconnect()
  window._ocrResizeObs = new ResizeObserver(() => {
    if (ocrImgEl.value) {
      ocrDisplayW.value = ocrImgEl.value.offsetWidth
      ocrDisplayH.value = ocrImgEl.value.offsetHeight
    }
  })
  window._ocrResizeObs.observe(ocrImgEl.value)
}

function refreshOcrSize() {
  if (!ocrImgEl.value) return
  ocrDisplayW.value = ocrImgEl.value.offsetWidth
  ocrDisplayH.value = ocrImgEl.value.offsetHeight
}

const pdfCanvas = ref(null)
const pdfScrollEl = ref(null)
const textLayerEl = ref(null)
const pdfScrollAllEl = ref(null)
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

const capturedImage = ref(null) // base64 ảnh chụp canvas PDF
const messages = ref([])
const userInput = ref('')
const isLoading = ref(false)
const messagesEl = ref(null)
const inputEl = ref(null)

const quickQuestions = ['Tóm tắt tài liệu này', 'Các điểm chính là gì?', 'Giải thích trang hiện tại']

// ── RAG: Extract → Chunk → Index → Retrieve → Augment ───────────
const ragIndex = ref([])
const ragReady = ref(false)

async function buildRagIndex() {
  if (!pdfDoc.value) return
  ragReady.value = false
  ragIndex.value = []
  for (let i = 1; i <= pdfDoc.value.numPages; i++) {
    const page = await pdfDoc.value.getPage(i)
    const tc = await page.getTextContent()
    const text = tc.items.map(it => it.str || '').join(' ').replace(/\s+/g, ' ').trim()
    if (!text) continue
    // Chunk ~300 ký tự với overlap 50 ký tự
    let start = 0
    let chunkIdx = 0
    while (start < text.length) {
      const end = Math.min(start + 300, text.length)
      ragIndex.value.push({ page: i, chunkIdx: chunkIdx++, text: text.slice(start, end) })
      start += 250 // overlap 50
    }
  }
  ragReady.value = true
}

function ragRetrieve(query, topK = 5) {
  if (!ragIndex.value.length) return []
  const qLower = query.toLowerCase()
  const qWords = qLower.split(/\s+/).filter(w => w.length > 1)

  const scored = ragIndex.value.map(chunk => {
    const cLower = chunk.text.toLowerCase()
    let score = 0
    // Exact phrase
    if (cLower.includes(qLower)) score += 30
    // Keyword match
    for (const w of qWords) {
      const n = (cLower.match(new RegExp(w, 'g')) || []).length
      score += n * 2
    }
    // Bigram
    for (let i = 0; i < qWords.length - 1; i++) {
      if (cLower.includes(qWords[i] + ' ' + qWords[i+1])) score += 8
    }
    return { ...chunk, score }
  })
  .filter(c => c.score > 0)
  .sort((a, b) => b.score - a.score)
  .slice(0, topK)

  return scored
}

function goBack() { router.back() }

// ── Init pdfjs ────────────────────────────────────────────────────
async function initPdfJs() {
  if (pdfjsLib) return
  pdfjsLib = await import('pdfjs-dist')
  const workerUrl = new URL('pdfjs-dist/build/pdf.worker.min.mjs', import.meta.url).href
  pdfjsLib.GlobalWorkerOptions.workerSrc = workerUrl
}

// ── Load PDF từ URL ───────────────────────────────────────────────
async function loadFromUrl(url) {
  try {
    loadingPdf.value = true
    await initPdfJs()
    const res = await fetch(url)
    if (!res.ok) { loadingMsg.value = `Không tải được file (${res.status}).`; return }
    const arrayBuffer = await res.arrayBuffer()
    await loadPdf(new Uint8Array(arrayBuffer))
  } catch (e) {
    loadingMsg.value = 'Lỗi khi tải file. Vui lòng thử lại.'
    console.error(e)
  } finally {
    loadingPdf.value = false
  }
}

// ── Load từ OCR API ──────────────────────────────────────────────
async function loadFromOcr(jobId) {
  ocrLoading.value = true
  ocrMode.value = true
  try {
    const config = useRuntimeConfig()
    const token = localStorage.getItem('jwt_token')
    if (!token) throw new Error('Vui lòng đăng nhập.')

    // Thử load từ sessionStorage cache trước
    const cached = sessionStorage.getItem(`ocr_view_meta_${jobId}`)
    if (cached) {
      const meta = JSON.parse(cached)
      if (meta.imageUrl) ocrImageUrl.value = meta.imageUrl
    }

    // Gọi endpoint có sẵn: GET /api/Infer/job/{jobId} — lazy OCR
    const apiUrl = `${config.public.apiBaseUrl}/api/Infer/job/${jobId}`
    const res = await fetch(apiUrl, {
      method: 'GET',
      headers: { Authorization: `Bearer ${token}` },
    })
    if (!res.ok) throw new Error('Không thể kết nối với máy chủ AI.')
    const data = await res.json()

    if (!ocrImageUrl.value) ocrImageUrl.value = data.imageUrl
    else ocrImageUrl.value = data.imageUrl || ocrImageUrl.value
    if (!pdfName.value) pdfName.value = decodeURIComponent(route.query.name || '') || 'Tài liệu OCR'
    ocrResults.value = data.results || null

    // Build full text + RAG
    const fullText = data.detectedText ||
      (data.results || []).map(r => r.wordText || '').join(' ').replace(/\s+/g, ' ').trim()

    // Set ocrFullText để đưa vào Gemini context
    ocrFullText.value = fullText

    if (fullText) {
      ragIndex.value = []
      let start = 0, chunkIdx = 0
      while (start < fullText.length) {
        const end = Math.min(start + 300, fullText.length)
        ragIndex.value.push({ page: 1, chunkIdx: chunkIdx++, text: fullText.slice(start, end) })
        start += 250
      }
      ragReady.value = true
    }
    totalPages.value = 1
    currentPage.value = 1
  } catch (e) {
    loadingMsg.value = `Lỗi OCR: ${e.message}`
    ocrMode.value = false
    console.error(e)
  } finally {
    ocrLoading.value = false
  }
}

async function loadPdf(data) {
  await initPdfJs()
  pdfDoc.value = await pdfjsLib.getDocument({ data }).promise
  totalPages.value = pdfDoc.value.numPages
  currentPage.value = 1
  gotoPage.value = 1
  await nextTick()
  await fitWidth()
  // Build RAG index background
  buildRagIndex().catch(e => console.warn('RAG index failed:', e))
}

// ── Text Layer — pdfjs v5 TextLayer class ────────────────────────
async function renderTextLayer(page, viewport, containerEl, cssWidth, cssHeight) {
  if (!containerEl) return

  if (containerEl._copyHandler) {
    containerEl.removeEventListener('copy', containerEl._copyHandler)
    containerEl._copyHandler = null
  }

  containerEl.innerHTML = ''
  containerEl.style.width = cssWidth || `${Math.floor(viewport.width)}px`
  containerEl.style.height = cssHeight || `${Math.floor(viewport.height)}px`

  // Build text layer thủ công với px tuyệt đối — không % không scaleX
  const { items } = await page.getTextContent({ disableNormalization: false })

  for (const item of items) {
    if (!item.str) continue
    const tx = pdfjsLib.Util.transform(viewport.transform, item.transform)
    const x = tx[4]
    const y = tx[5]
    const fontSize = Math.sqrt(tx[0] * tx[0] + tx[1] * tx[1])
    const spanWidth = item.width * viewport.scale

    if (fontSize < 1) continue

    const span = document.createElement('span')
    span.textContent = item.str
    span.style.cssText = [
      'position:absolute',
      `left:${x.toFixed(3)}px`,
      `top:${(y - fontSize).toFixed(3)}px`,
      `font-size:${fontSize.toFixed(3)}px`,
      `width:${Math.max(spanWidth, fontSize * 0.5).toFixed(3)}px`,
      'white-space:nowrap',
      'color:transparent',
      'cursor:text',
      'user-select:text',
      '-webkit-user-select:text',
      'pointer-events:auto',
      'transform-origin:0% 0%',
      'line-height:1',
      'overflow:visible',
    ].join(';')
    containerEl.appendChild(span)
  }

  // Copy handler
  const copyHandler = (e) => {
    const sel = window.getSelection()
    if (!sel || sel.isCollapsed) return
    const frag = sel.getRangeAt(0).cloneContents()
    const tmp = document.createElement('div')
    tmp.appendChild(frag)
    const text = tmp.innerText || tmp.textContent || ''
    if (text.trim()) {
      e.preventDefault()
      e.clipboardData.setData('text/plain', text)
    }
  }
  containerEl._copyHandler = copyHandler
  containerEl.addEventListener('copy', copyHandler)
}
// ── Single Page ───────────────────────────────────────────────────
async function renderPage(pageNum = currentPage.value) {
  if (!pdfDoc.value || !pdfCanvas.value) return
  if (renderTask) { try { renderTask.cancel() } catch {} renderTask = null }

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
  } finally { renderTask = null }

  await renderTextLayer(page, viewport, textLayerEl.value,
    canvas.style.width, canvas.style.height)
}

async function prevPage() {
  if (currentPage.value <= 1) return
  currentPage.value--; gotoPage.value = currentPage.value; await renderPage()
}
async function nextPage() {
  if (currentPage.value >= totalPages.value) return
  currentPage.value++; gotoPage.value = currentPage.value; await renderPage()
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
  scale.value = +((containerEl.clientWidth - 16) / vp.width).toFixed(2)  // 8px padding * 2
  if (viewMode.value === 'single') await renderPage()
  else await renderAllPages()
}
async function zoomIn() {
  scale.value = Math.min(3, +(scale.value + 0.2).toFixed(1))
  if (ocrMode.value) { await nextTick(); refreshOcrSize(); return }
  if (viewMode.value === 'single') await renderPage(); else await renderAllPages()
}
async function zoomOut() {
  scale.value = Math.max(0.3, +(scale.value - 0.2).toFixed(1))
  if (ocrMode.value) { await nextTick(); refreshOcrSize(); return }
  if (viewMode.value === 'single') await renderPage(); else await renderAllPages()
}

// ── OCR: copy từ khi click bbox ──────────────────────────────────
function copyWord(word) {
  if (!word) return
  navigator.clipboard?.writeText(word).catch(() => {})
  // Trigger vocab popup nếu từ đủ ngắn
  if (word.length <= 50) {
    const popup = document.querySelector('.vocab-popup')
    vocabPopup.value = {
      visible: true,
      word,
      meaning: '',
      editMeaning: '',
      x: 20, y: 60,
      loading: false,
      saving: false,
    }
    fetchWordMeaning(word)
  }
}

// ── OCR: tính vị trí text box từ Google Vision bbox ─────────────
// BoundingBox là JSON string: [[x1,y1],[x2,y2],[x3,y3],[x4,y4]]
function getOcrBoxStyle(result) {
  const bboxRaw = result.boundingBox || result.bounding_box || result.bbox
  if (!bboxRaw) return { display: 'none' }

  try {
    const pts = typeof bboxRaw === 'string' ? JSON.parse(bboxRaw) : bboxRaw
    if (!Array.isArray(pts) || pts.length < 2) return { display: 'none' }

    const xs = pts.map(p => Array.isArray(p) ? p[0] : p.x || 0)
    const ys = pts.map(p => Array.isArray(p) ? p[1] : p.y || 0)

    let x = Math.min(...xs)
    let y = Math.min(...ys)
    let w = Math.max(...xs) - x
    let h = Math.max(...ys) - y

    // Scale bbox từ tọa độ ảnh gốc → tọa độ ảnh đang hiển thị
    if (ocrNaturalW.value > 0 && ocrDisplayW.value > 0) {
      const scaleX = ocrDisplayW.value / ocrNaturalW.value
      const scaleY = ocrDisplayH.value > 0 ? ocrDisplayH.value / ocrNaturalH.value : scaleX
      x = x * scaleX
      y = y * scaleY
      w = w * scaleX
      h = h * scaleY
    }

    return {
      position: 'absolute',
      left: `${x}px`,
      top: `${y}px`,
      width: `${Math.max(w, 8)}px`,
      height: `${Math.max(h, 8)}px`,
    }
  } catch {
    return { display: 'none' }
  }
}

// ── Scroll All ────────────────────────────────────────────────────
async function initScrollMode() {
  await nextTick(); await nextTick()
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
  } catch (err) { if (err?.name !== 'RenderingCancelledException') console.error(err) }
  const cv = pageCanvases[pageNum]
  await renderTextLayer(page, viewport, textLayerRefs[pageNum],
    cv?.style.width, cv?.style.height)
}

async function renderAllPages() {
  if (!pdfDoc.value) return
  await nextTick()
  for (let i = 1; i <= totalPages.value; i++) await renderOnePageScroll(i)
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
  if (el && pdfScrollAllEl.value)
    pdfScrollAllEl.value.scrollTo({ top: el.offsetTop - 16, behavior: 'smooth' })
}

async function setViewMode(mode) {
  if (viewMode.value === mode) return
  viewMode.value = mode
  if (scrollObserver) { scrollObserver.disconnect(); scrollObserver = null }
  await nextTick()
  if (mode === 'single') await renderPage(currentPage.value)
  else await initScrollMode()
}

// ── Resizer ───────────────────────────────────────────────────────
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

// ── Lấy text trang hiện tại để làm context cho Gemini ───────────
async function getCurrentPageText() {
  // OCR mode: dùng text từ OCR results thay vì PDF
  if (ocrMode.value && ocrResults.value?.length) {
    return ocrResults.value
      .map(r => r.wordText || '')
      .join(' ')
      .slice(0, 3000)
  }
  if (!pdfDoc.value) return ''
  try {
    const page = await pdfDoc.value.getPage(currentPage.value)
    const content = await page.getTextContent({ disableNormalization: false })
    return content.items
      .filter(item => item.str?.trim())
      .map(item => item.str)
      .join(' ')
      .slice(0, 3000)
  } catch { return '' }
}

// ── Gemini ────────────────────────────────────────────────────────
const GEMINI_MODELS = ['gemini-2.5-flash', 'gemini-2.0-flash-lite']

async function callGeminiStream(model, systemContext, history, text, imageBase64 = null, onChunk) {
  const userParts = []
  if (imageBase64) {
    const base64Data = imageBase64.replace(/^data:image\/\w+;base64,/, '')
    userParts.push({ inline_data: { mime_type: 'image/jpeg', data: base64Data } })
  }
  userParts.push({ text })

  const res = await fetch(
    `https://generativelanguage.googleapis.com/v1beta/models/${model}:streamGenerateContent?alt=sse&key=${apiKey.value}`,
    {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        system_instruction: { parts: [{ text: systemContext }] },
        contents: [...history, { role: 'user', parts: userParts }],
        generationConfig: { temperature: 0.7, maxOutputTokens: 2048 }
      })
    }
  )

  if (!res.ok) {
    const err = await res.json()
    throw { message: err.error?.message || 'API error', status: err.error?.status }
  }

  const reader = res.body.getReader()
  const decoder = new TextDecoder()
  let fullText = ''
  let buffer = ''

  while (true) {
    const { done, value } = await reader.read()
    if (done) break

    buffer += decoder.decode(value, { stream: true })
    const lines = buffer.split('\n')
    buffer = lines.pop() // giữ lại dòng chưa hoàn chỉnh

    for (const line of lines) {
      if (!line.startsWith('data: ')) continue
      const raw = line.slice(6).trim()
      if (raw === '[DONE]') continue
      try {
        const json = JSON.parse(raw)
        const chunk = json.candidates?.[0]?.content?.parts?.[0]?.text || ''
        if (chunk) {
          fullText += chunk
          onChunk(fullText)
        }
        // Check for errors in stream
        if (json.error) throw { message: json.error.message, status: json.error.status }
      } catch (e) {
        if (e.message && e.status) throw e
        // ignore parse errors for non-data lines
      }
    }
  }

  return fullText || 'Không có phản hồi.'
}

async function sendMessage() {
  const text = userInput.value.trim()
  if (!text || isLoading.value || !apiKey.value) return
  messages.value.push({
    role: 'user',
    content: text,
    image: capturedImage.value // lưu để hiển thị trong chat
  })
  userInput.value = ''
  if (inputEl.value) inputEl.value.style.height = 'auto'
  isLoading.value = true
  await scrollBottom()
  try {
    const history = messages.value.slice(0, -1).map(m => ({
      role: m.role === 'user' ? 'user' : 'model',
      parts: [{ text: m.content }]
    }))
    // Lấy text trang hiện tại làm context (giống RAG đơn giản)
    const pageText = await getCurrentPageText()
    // RAG: Retrieve đoạn liên quan nhất
    const chunks = ragRetrieve(text)
    const ragCtx = chunks.length
      ? '\n\n---\nĐOẠN THAM KHẢO TỪ TÀI LIỆU:\n' +
        chunks.map(c => `[Trang ${c.page}]: ${c.text}`).join('\n\n') +
        '\n---'
      : ''

    // Detect xem câu hỏi có liên quan đến tiếng Nhật không
    const isJapaneseQuery = /[\u3000-\u9fff\u30a0-\u30ff\u3040-\u309f]/.test(text) ||
      /ngữ pháp|văn phạm|jlpt|kanji|hiragana|katakana|dịch nghĩa|giải thích câu|phân tích/i.test(text)

    const jsonInstruction = isJapaneseQuery ? `

Khi phân tích tiếng Nhật, LUÔN trả về JSON theo format sau (không markdown, chỉ JSON thuần):
{
  "type": "japanese_analysis",
  "sentence": "câu gốc",
  "translation": "dịch nghĩa tự nhiên",
  "explanation": "giải thích tổng quan bằng tiếng Việt",
  "grammar": [
    {
      "pattern": "tên ngữ pháp (vd:〜ている)",
      "jlpt": "N5/N4/N3/N2/N1",
      "meaning": "ý nghĩa",
      "example": "ví dụ khác"
    }
  ],
  "vocabulary": [
    {
      "word": "từ",
      "reading": "cách đọc",
      "meaning": "nghĩa",
      "jlpt": "N5/N4/N3/N2/N1"
    }
  ]
}

Nếu câu hỏi không phải phân tích ngữ pháp thì trả lời bình thường bằng text.` : ''

    // OCR mode: đưa toàn bộ text đã quét vào context
    const ocrCtx = ocrMode.value && ocrFullText.value
      ? `\n\n=== NỘI DUNG TÀI LIỆU (được quét bằng AI OCR) ===\n${ocrFullText.value.slice(0, 8000)}\n=== HẾT NỘI DUNG ===`
      : ''

    const systemContext = pdfName.value
      ? `Bạn là trợ lý AI đọc tài liệu "${pdfName.value}".${ocrCtx}${ragCtx}\n\nQuy tắc:\n- Ưu tiên dùng nội dung tài liệu trên để trả lời\n- Nếu không có thông tin phù hợp, hãy nói rõ\n- Trả lời bằng tiếng Việt${jsonInstruction}`
      : `Bạn là trợ lý AI hữu ích. Trả lời bằng tiếng Việt.${jsonInstruction}`
    // Thêm placeholder message để streaming update vào đúng chỗ
    messages.value.push({ role: 'model', content: '', streaming: true })
    const streamingIdx = messages.value.length - 1

    let reply = null, lastError = null
    for (const model of GEMINI_MODELS) {
      try {
        reply = await callGeminiStream(
          model, systemContext, history, text, capturedImage.value,
          (partial) => {
            // Update streaming message realtime
            messages.value[streamingIdx] = { role: 'model', content: partial, streaming: true }
            scrollBottom()
          }
        )
        break
      } catch (err) {
        lastError = err
        if (err.status === 'RESOURCE_EXHAUSTED') continue
        throw err
      }
    }
    if (!reply) {
      const m = lastError?.message?.match(/retry in (\d+)s/i)
      throw new Error(`Hết quota.${m ? ` Thử lại sau ${Math.ceil(m[1]/60)} phút.` : ''}`)
    }
    // Finalize message (bỏ streaming flag)
    messages.value[streamingIdx] = { role: 'model', content: reply, streaming: false }
    capturedImage.value = null
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

// ── Paste ảnh từ clipboard (Ctrl+V) ──────────────────────────────
function handlePaste(e) {
  const items = e.clipboardData?.items
  if (!items) return

  for (const item of items) {
    if (item.type.startsWith('image/')) {
      e.preventDefault() // không paste text rác vào textarea

      const file = item.getAsFile()
      if (!file) continue

      const reader = new FileReader()
      reader.onload = (ev) => {
        capturedImage.value = ev.target.result // base64
      }
      reader.readAsDataURL(file)
      break // chỉ lấy ảnh đầu tiên
    }
  }
}
function formatMessage(rawText) {
  // Thử parse JSON phân tích tiếng Nhật
  try {
    const jsonMatch = rawText.match(/\{[\s\S]*"type"\s*:\s*"japanese_analysis"[\s\S]*\}/)
    if (jsonMatch) {
      const data = JSON.parse(jsonMatch[0])
      return renderJapaneseAnalysis(data)
    }
  } catch {}

  // Fallback: render markdown thường
  return rawText
    .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
    .replace(/\*(.*?)\*/g, '<em>$1</em>')
    .replace(/`([^`]+)`/g, '<code>$1</code>')
    .replace(/\n/g, '<br />')
}

function renderJapaneseAnalysis(data) {
  const jlptColor = (level) => {
    const colors = { N5: '#4ade80', N4: '#60a5fa', N3: '#f59e0b', N2: '#f87171', N1: '#c084fc' }
    return colors[level] || '#9ca3af'
  }

  let html = `<div class="jp-card">`

  // Câu gốc + dịch
  html += `<div class="jp-sentence">${data.sentence || ''}</div>`
  if (data.translation) {
    html += `<div class="jp-translation">🇻🇳 ${data.translation}</div>`
  }
  if (data.explanation) {
    html += `<div class="jp-explanation">${data.explanation}</div>`
  }

  // Ngữ pháp
  if (data.grammar?.length) {
    html += `<div class="jp-section-title">📐 Ngữ pháp</div>`
    html += `<div class="jp-grammar-list">`
    for (const g of data.grammar) {
      html += `<div class="jp-grammar-item">
        <div class="jp-grammar-header">
          <span class="jp-pattern">${g.pattern}</span>
          <span class="jp-badge" style="background:${jlptColor(g.jlpt)}22;color:${jlptColor(g.jlpt)};border-color:${jlptColor(g.jlpt)}44">${g.jlpt}</span>
        </div>
        <div class="jp-grammar-meaning">${g.meaning}</div>
        ${g.example ? `<div class="jp-example">例: ${g.example}</div>` : ''}
      </div>`
    }
    html += `</div>`
  }

  // Từ vựng
  if (data.vocabulary?.length) {
    html += `<div class="jp-section-title">📚 Từ vựng</div>`
    html += `<div class="jp-vocab-list">`
    for (const v of data.vocabulary) {
      html += `<div class="jp-vocab-item">
        <span class="jp-vocab-word">${v.word}</span>
        ${v.reading ? `<span class="jp-vocab-reading">【${v.reading}】</span>` : ''}
        <span class="jp-vocab-meaning">${v.meaning}</span>
        <span class="jp-badge" style="background:${jlptColor(v.jlpt)}22;color:${jlptColor(v.jlpt)};border-color:${jlptColor(v.jlpt)}44">${v.jlpt}</span>
      </div>`
    }
    html += `</div>`
  }

  html += `</div>`
  return html
}

function handleWindowResize() { if (pdfDoc.value) fitWidth() }

// ── Vocab State ──────────────────────────────────────────────────
const activeTab = ref('chat')
const projectId = ref(null)
const vocabList = ref([]) // [{ id, wordText, contextMeaning }]

// Popup bôi đen
const vocabPopup = ref({
  visible: false,
  word: '',
  meaning: '',      // từ API
  editMeaning: '',  // người dùng nhập/sửa
  x: 0, y: 0,
  loading: false,
  saving: false,
})

// Deck modal
const showDeckModal = ref(false)
const selectedVocabIndices = ref([])
const creatingDeck = ref(false)
const deckForm = ref({ name: '' })

// ── Vocab: lắng nghe mouseup trên PDF để bắt bôi đen ─────────────
function setupTextSelection() {
  const pdfArea = document.querySelector('.main-content')
  if (!pdfArea) return
  pdfArea.addEventListener('mouseup', handleTextSelect)
}

function handleTextSelect(e) {
  // Bỏ qua nếu click trong vocab popup — tránh đóng popup khi nhập nghĩa
  const popup = document.querySelector('.vocab-popup')
  if (popup?.contains(e.target)) return

  const sel = window.getSelection()
  const text = sel?.toString().trim()
  if (!text || text.length < 1 || text.length > 50) {
    // Chỉ đóng popup nếu không click trong popup
    if (!popup?.contains(e.target)) closeVocabPopup()
    return
  }

  // Bắt selection trong pdf panel (cả PDF mode và OCR mode)
  const pdfPanel = document.querySelector('.pdf-panel')
  if (!pdfPanel?.contains(sel.anchorNode)) return

  // Lấy vị trí popup gần con trỏ
  const rect = sel.getRangeAt(0).getBoundingClientRect()
  const mainRect = document.querySelector('.main-content').getBoundingClientRect()

  vocabPopup.value = {
    visible: true,
    word: text,
    meaning: '',
    editMeaning: '',
    x: Math.min(rect.left - mainRect.left, mainRect.width - 260),
    y: rect.bottom - mainRect.top + 8,
    loading: false,
    saving: false,
  }

  // Tự động tra nghĩa
  fetchWordMeaning(text)
}

// ── Tra nghĩa từ API ──────────────────────────────────────────────
async function fetchWordMeaning(word) {
  const config = useRuntimeConfig()
  vocabPopup.value.loading = true
  vocabPopup.value.meaning = ''
  try {
    const url = `${config.public.apiBaseUrl}/api/Word/GetWordJson/${encodeURIComponent(word)}`
    const res = await fetch(url)
    if (!res.ok) throw new Error()
    const data = await res.json()
    if (data.status === 200 && data.data) {
      // Cấu trúc response: data.data.words[0].means[0].mean
      const words = data.data?.words || []
      const means = words[0]?.means || []
      const firstMeaning = means[0]?.mean || ''

      // Fallback: suggestWords nếu không có words
      const suggest = data.data?.suggestWords || []
      const fallback = suggest[0]?.means?.[0]?.mean || ''

      vocabPopup.value.meaning = firstMeaning || fallback
      vocabPopup.value.editMeaning = firstMeaning || fallback // pre-fill input
    }
  } catch (e) {
    // Không tìm thấy nghĩa, để người dùng tự nhập
  } finally {
    vocabPopup.value.loading = false
  }
}

function closeVocabPopup() {
  vocabPopup.value.visible = false
}

// ── Lưu từ vựng ──────────────────────────────────────────────────
async function saveVocab() {
  if (!vocabPopup.value.word) return
  // Lấy nghĩa — ưu tiên meaning từ API, fallback customMeaning người dùng nhập
  const finalMeaning = vocabPopup.value.editMeaning?.trim() || vocabPopup.value.meaning?.trim()
  if (!finalMeaning) return // Vẫn không có nghĩa thì không lưu

  vocabPopup.value.saving = true
  const config = useRuntimeConfig()

  try {
    // Lưu vào BE nếu có projectId
    if (projectId.value) {
      const res = await fetch(
        `${config.public.apiBaseUrl}/api/projects/${projectId.value}/vocabularies`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${getToken()}`
          },
          body: JSON.stringify({
            wordText: vocabPopup.value.word,
            contextMeaning: finalMeaning,
          })
        }
      )
      if (res.ok) {
        const saved = await res.json()
        // Cập nhật hoặc thêm vào danh sách local
        const idx = vocabList.value.findIndex(v => v.wordText === saved.wordText)
        if (idx >= 0) vocabList.value[idx] = saved
        else vocabList.value.unshift(saved)
      }
    } else {
      // Không có project: lưu local
      const idx = vocabList.value.findIndex(v => v.wordText === vocabPopup.value.word)
      const item = { id: Date.now(), wordText: vocabPopup.value.word, contextMeaning: finalMeaning }
      if (idx >= 0) vocabList.value[idx] = item
      else vocabList.value.unshift(item)
    }

    // Chuyển sang tab vocab để xem
    activeTab.value = 'vocab'
    closeVocabPopup()
    window.getSelection()?.removeAllRanges()
  } finally {
    vocabPopup.value.saving = false
  }
}

// ── Xóa vocab ─────────────────────────────────────────────────────
async function removeVocab(index) {
  const v = vocabList.value[index]
  const config = useRuntimeConfig()

  if (projectId.value && v.id) {
    await fetch(
      `${config.public.apiBaseUrl}/api/projects/${projectId.value}/vocabularies/${v.id}`,
      {
        method: 'DELETE',
        headers: { 'Authorization': `Bearer ${getToken()}` }
      }
    )
  }
  vocabList.value.splice(index, 1)
}

// ── Load vocab đã có sẵn ──────────────────────────────────────────
async function loadVocabs() {
  if (!projectId.value) return
  const config = useRuntimeConfig()
  try {
    const res = await fetch(
      `${config.public.apiBaseUrl}/api/projects/${projectId.value}/vocabularies`,
      { headers: { 'Authorization': `Bearer ${getToken()}` } }
    )
    if (res.ok) vocabList.value = await res.json()
  } catch (e) { console.error(e) }
}

// ── Tạo Deck từ vocab đã chọn ────────────────────────────────────
function toggleSelectAll(e) {
  if (e.target.checked) {
    selectedVocabIndices.value = vocabList.value.map((_, i) => i)
  } else {
    selectedVocabIndices.value = []
  }
}

async function createDeckFromVocab() {
  if (!selectedVocabIndices.value.length) return
  const config = useRuntimeConfig()
  creatingDeck.value = true

  try {
    const selectedVocabs = selectedVocabIndices.value.map(i => vocabList.value[i])

    // Format theo kiểu import của CreateDeckForm: "frontText,backText"
    const importText = selectedVocabs
      .map(v => `${v.wordText},${v.contextMeaning}`)
      .join('')

    // Tạo deck qua API
    const deckName = deckForm.value.name || pdfName.value || 'Từ vựng PDF'
    const res = await fetch(`${config.public.apiBaseUrl}/api/decks`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${getToken()}`
      },
      body: JSON.stringify({
        title: deckName,
        description: `Tạo từ tài liệu: ${pdfName.value}`,
        isPublic: false,
        cards: selectedVocabs.map(v => ({
          frontText: v.wordText,
          backText: v.contextMeaning,
          tags: null,
        }))
      })
    })

    if (res.ok) {
      showDeckModal.value = false
      selectedVocabIndices.value = []
      deckForm.value.name = ''
      showToast(`Đã tạo bộ thẻ "${deckName}" với ${selectedVocabs.length} thẻ!`, 'success')
    } else {
      showToast('Tạo deck thất bại. Vui lòng thử lại.', 'error')
    }
  } finally {
    creatingDeck.value = false
  }
}

onMounted(async () => {
  const config = useRuntimeConfig()
  apiKey.value = config.public.geminiApiKey || ''

  const url = route.query.url
  const name = route.query.name
  const sessionId = route.query.id
  const pid = route.query.projectId

  pdfName.value = name ? decodeURIComponent(name) : ''
  if (pid) {
    projectId.value = parseInt(pid)
    await loadVocabs()
  }

  const jobId = route.query.jobId

  if (jobId) {
    // OCR mode — load từ API
    await loadFromOcr(jobId)
  } else if (url) {
    await loadFromUrl(url)
  } else if (sessionId) {
    const raw = sessionStorage.getItem(`pdf_${sessionId}`)
    if (raw) {
      await initPdfJs()
      await loadPdf(new Uint8Array(JSON.parse(raw)))
    } else {
      loadingMsg.value = 'Không tìm thấy file.'
    }
  } else {
    loadingMsg.value = 'Không có file được chọn.'
  }

  window.addEventListener('resize', handleWindowResize)
  await nextTick()
  setupTextSelection()

  // Ẩn WordResultModal và TranslationModal của web gốc
  // Bằng cách override useLookupState composable
  try {
    const { useLookupModalVisible, useTranslateModalVisible } = await import('~/composables/useLookupState')
    const isLookup = useLookupModalVisible()
    const isTranslate = useTranslateModalVisible()
    // Force hide cả 2 modal khi mouseup trong reader
    document.addEventListener('mouseup', () => {
      if (isLookup.value) isLookup.value = false
      if (isTranslate.value) isTranslate.value = false
    }, true) // capture phase — chạy trước handler của web
  } catch (e) {
    // Nếu composable không tồn tại thì bỏ qua
  }
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', handleWindowResize)
  stopResize()
  if (scrollObserver) scrollObserver.disconnect()
})
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=DM+Sans:wght@300;400;500;600&family=Playfair+Display:wght@600&display=swap');

/* ── Text Layer CSS ── */
.textLayer {
  position: absolute;
  inset: 0;
  overflow: visible; /* Đổi hidden -> visible để không cắt chữ bên phải */
  z-index: 2;
  user-select: text !important;
  -webkit-user-select: text !important;
  cursor: text;
}

/* Override pdfjs css — v5 mặc định set pointer-events: none trên span */
.textLayer :deep(span) {
  color: transparent !important;
  cursor: text !important;
  user-select: text !important;
  -webkit-user-select: text !important;
  pointer-events: auto !important;  /* QUAN TRỌNG: enable click/select */
}

.textLayer :deep(span::selection),
.textLayer :deep(::selection) {
  background: rgba(91, 141, 238, 0.35) !important;
  color: transparent !important;
}

/* Fallback div khi TextLayer không khả dụng */
.textLayer :deep(div) {
  color: transparent;
  position: absolute;
  white-space: pre;
  cursor: text;
  transform-origin: 0% 0%;
  user-select: text;
  -webkit-user-select: text;
  pointer-events: auto;
  line-height: 1;
}
.textLayer :deep(div::selection) {
  background: rgba(91, 141, 238, 0.35);
  color: transparent;
}

/* ── App layout ── */
* { box-sizing: border-box; margin: 0; padding: 0; }

.app-shell {
  /* ── Dark mode (default) ── */
  --bg: #0e0f11;
  --surface: #16181c;
  --surface2: #1e2026;
  --border: #2a2d35;
  --accent: #f0c040;
  --accent2: #5b8dee;
  --text: #e8eaf0;
  --text-muted: #6b7280;
  --pdf-bg: #cacaca;

  font-family: 'DM Sans', sans-serif;
  background: var(--bg);
  color: var(--text);
  height: 100vh;
  display: flex; flex-direction: column;
  overflow: hidden;
  transition: background-color 0.3s, color 0.3s;
}

/* ── Light mode — khi <html> có class="light" ── */
:global(html.light) .app-shell {
  --bg: #f0f2f5;
  --surface: #ffffff;
  --surface2: #f5f5f5;
  --border: #e0e0e0;
  --accent: #d4a017;
  --accent2: #3b72d4;
  --text: #1a1a1a;
  --text-muted: #666666;
  --pdf-bg: #b0b0b0;
}

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

.main-content { display: flex; flex: 1; overflow: hidden; position: relative; }
.panel { display: flex; flex-direction: column; overflow: hidden; }
.pdf-panel { width: 55%; background: var(--bg); min-width: 320px; }
.chat-panel { flex: 1; background: var(--surface); border-left: 1px solid var(--border); min-width: 280px; }
.resizer { width: 5px; background: var(--border); cursor: col-resize; flex-shrink: 0; transition: background 0.2s; }
.resizer:hover { background: var(--accent); }

.pdf-empty { flex: 1; display: flex; align-items: center; justify-content: center; }
.loading-wrap { display: flex; flex-direction: column; align-items: center; gap: 12px; }
.spinner {
  width: 32px; height: 32px;
  border: 3px solid var(--border);
  border-top-color: var(--accent);
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }
.loading-text { font-size: 15px; color: var(--text-muted); }

.pdf-viewer-wrap { flex: 1; display: flex; flex-direction: column; overflow: hidden; }
.pdf-toolbar {
  display: flex; align-items: center; gap: 4px; padding: 7px 12px;
  background: var(--surface); border-bottom: 1px solid var(--border); flex-shrink: 0;
}
.view-mode-toggle { display: flex; background: var(--surface2); border: 1px solid var(--border); border-radius: 6px; overflow: hidden; }
.mode-btn { display: flex; align-items: center; justify-content: center; width: 30px; height: 28px; background: transparent; border: none; color: var(--text-muted); cursor: pointer; transition: all 0.15s; }
.mode-btn:hover { color: var(--text); }
.mode-btn.active { background: var(--accent); color: #111; }
.tool-btn { width: 30px; height: 28px; background: var(--surface2); border: 1px solid var(--border); color: var(--text); border-radius: 6px; cursor: pointer; font-size: 15px; display: flex; align-items: center; justify-content: center; transition: all 0.15s; font-family: inherit; flex-shrink: 0; }
.tool-btn:hover:not(:disabled) { background: var(--accent); color: #111; border-color: var(--accent); }
.tool-btn:disabled { opacity: 0.4; cursor: not-allowed; }
.page-info { display: flex; align-items: center; gap: 5px; font-size: 13px; color: var(--text-muted); padding: 0 2px; }
.page-info-scroll { font-size: 12px; color: var(--text-muted); padding: 0 4px; white-space: nowrap; }
.page-input { width: 40px; background: var(--surface2); border: 1px solid var(--border); color: var(--text); border-radius: 5px; padding: 2px 4px; font-size: 13px; text-align: center; font-family: inherit; outline: none; }
.page-input:focus { border-color: var(--accent); }
.page-input::-webkit-inner-spin-button { display: none; }
.zoom-label { font-size: 12px; color: var(--text-muted); min-width: 36px; text-align: center; }
.tool-sep { width: 1px; height: 20px; background: var(--border); margin: 0 3px; flex-shrink: 0; }

.pdf-scroll { flex: 1; overflow: auto; display: flex; justify-content: center; align-items: flex-start; padding: 8px; background: var(--pdf-bg); }
.pdf-scroll--all { display: block; padding: 16px; }
.pdf-pages-wrap { display: flex; flex-direction: column; gap: 12px; align-items: center; width: 100%; padding-top: 8px; }
.pdf-page-item { display: flex; justify-content: center; flex-shrink: 0; width: 100%; }

/* Container phải relative để text layer overlay đúng */
.pdf-page-container {
  position: relative;
  display: inline-block;
  line-height: 0;
  overflow: visible; /* Không cắt text layer */
}

.pdf-canvas {
  display: block;
  background: #fff;
  box-shadow: 0 2px 16px rgba(0,0,0,0.35);
}

/* Chat */
.chat-header { display: flex; align-items: center; gap: 12px; padding: 14px 20px; border-bottom: 1px solid var(--border); flex-shrink: 0; }
.chat-avatar { width: 34px; height: 34px; background: linear-gradient(135deg, var(--accent2), #8b5cf6); border-radius: 10px; display: flex; align-items: center; justify-content: center; font-weight: 700; font-size: 14px; flex-shrink: 0; }
.chat-title { font-size: 15px; font-weight: 600; }
.chat-sub { font-size: 12px; color: var(--text-muted); }
.btn-clear { margin-left: auto; background: transparent; border: 1px solid var(--border); color: var(--text-muted); width: 30px; height: 30px; border-radius: 8px; cursor: pointer; font-size: 15px; display: flex; align-items: center; justify-content: center; transition: all 0.2s; }
.btn-clear:hover { border-color: #ef4444; color: #ef4444; }
.messages { flex: 1; overflow-y: auto; padding: 20px; display: flex; flex-direction: column; gap: 16px; scrollbar-width: thin; scrollbar-color: var(--border) transparent; }
.messages::-webkit-scrollbar { width: 4px; }
.messages::-webkit-scrollbar-thumb { background: var(--border); border-radius: 4px; }
.welcome-wrap { flex: 1; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 12px; text-align: center; padding: 20px; }
.welcome-orb { width: 56px; height: 56px; background: radial-gradient(circle, var(--accent2), #8b5cf6 60%, transparent); border-radius: 50%; margin-bottom: 8px; animation: pulse 3s ease-in-out infinite; }
@keyframes pulse { 0%, 100% { transform: scale(1); opacity: 0.9; } 50% { transform: scale(1.08); opacity: 1; } }
.welcome-title { font-size: 18px; font-weight: 600; }
.welcome-sub { font-size: 13px; color: var(--text-muted); }
.quick-btns { display: flex; flex-direction: column; gap: 8px; margin-top: 10px; width: 100%; max-width: 260px; }
.quick-btn { background: var(--surface2); border: 1px solid var(--border); color: var(--text-muted); border-radius: 8px; padding: 8px 12px; font-size: 13px; cursor: pointer; text-align: left; transition: all 0.2s; font-family: inherit; }
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
.chat-input-wrap { display: flex; align-items: flex-end; gap: 10px; padding: 12px 14px; border-top: 1px solid var(--border); flex-shrink: 0; background: var(--surface); }
.chat-input { flex: 1; background: var(--surface2); border: 1px solid var(--border); color: var(--text); border-radius: 10px; padding: 10px 14px; font-size: 14px; resize: none; max-height: 140px; overflow-y: auto; font-family: inherit; line-height: 1.5; outline: none; transition: border-color 0.2s; }
.chat-input:focus { border-color: var(--accent2); }
.chat-input::placeholder { color: var(--text-muted); }
.btn-send { width: 38px; height: 38px; background: var(--accent2); border: none; border-radius: 10px; cursor: pointer; display: flex; align-items: center; justify-content: center; flex-shrink: 0; transition: all 0.2s; color: white; }
.btn-send:hover:not(:disabled) { background: #4a7de0; transform: translateY(-1px); }
.btn-send:disabled { opacity: 0.4; cursor: not-allowed; }
.btn-send svg { width: 15px; height: 15px; }
.no-key-hint { text-align: center; font-size: 12px; color: #f59e0b; padding: 0 16px 10px; }

.capture-preview {
  display: flex; align-items: center; gap: 10px;
  padding: 8px 14px;
  background: var(--surface2);
  border-top: 1px solid var(--border);
  flex-shrink: 0;
}
.capture-thumb {
  width: 56px; height: 40px;
  object-fit: cover;
  border-radius: 4px;
  border: 1px solid var(--border);
  flex-shrink: 0;
}
.capture-info { display: flex; align-items: center; justify-content: space-between; flex: 1; }
.capture-label { font-size: 12px; color: var(--text-muted); }
.capture-remove {
  background: transparent; border: none;
  color: var(--text-muted); cursor: pointer;
  font-size: 14px; padding: 2px 6px;
  border-radius: 4px; transition: color 0.2s;
}
.capture-remove:hover { color: #ef4444; }

/* Ảnh trong message */
.msg-image {
  display: block;
  width: 100%; max-width: 220px;
  border-radius: 6px;
  margin-bottom: 8px;
  border: 1px solid var(--border);
}

/* ── Panel Tabs ── */
.panel-tabs {
  display: flex;
  border-bottom: 1px solid var(--border);
  flex-shrink: 0;
  background: var(--surface);
}
.panel-tab {
  flex: 1; padding: 10px 4px; font-size: 13px; font-weight: 500;
  background: transparent; border: none; border-bottom: 2px solid transparent;
  color: var(--text-muted); cursor: pointer; transition: all 0.15s;
  display: flex; align-items: center; justify-content: center; gap: 4px;
  font-family: inherit;
}
.panel-tab:hover { color: var(--text); }
.panel-tab.active { border-bottom-color: var(--accent); color: var(--text); }
.tab-badge {
  background: var(--accent); color: #111;
  font-size: 10px; font-weight: 700;
  padding: 1px 5px; border-radius: 10px;
  min-width: 16px; text-align: center;
}

/* ── Vocab Popup ── */
.vocab-popup {
  position: absolute;
  z-index: 100;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 12px;
  padding: 12px;
  width: 240px;
  box-shadow: 0 8px 32px rgba(0,0,0,0.4);
  pointer-events: auto;
}
.vocab-popup-word {
  font-size: 15px; font-weight: 700;
  color: var(--accent); margin-bottom: 8px;
}
.vocab-popup-loading {
  display: flex; align-items: center; gap: 6px;
  font-size: 12px; color: var(--text-muted); margin-bottom: 8px;
}
.mini-spinner {
  width: 12px; height: 12px;
  border: 2px solid var(--border);
  border-top-color: var(--accent);
  border-radius: 50%;
  animation: spin 0.7s linear infinite;
  flex-shrink: 0;
}
.vocab-popup-meaning {
  font-size: 13px; color: var(--text);
  margin-bottom: 8px; line-height: 1.5;
  background: var(--surface2); padding: 6px 8px;
  border-radius: 6px;
}
.vocab-popup-input {
  width: 100%; background: var(--surface2);
  border: 1px solid var(--border);
  color: var(--text); border-radius: 6px;
  padding: 6px 8px; font-size: 13px; outline: none;
  margin-bottom: 8px; font-family: inherit;
  transition: border-color 0.2s;
}
.vocab-popup-input:focus { border-color: var(--accent); }
.vocab-popup-actions {
  display: flex; gap: 6px; justify-content: flex-end;
}
.vocab-btn-save {
  background: var(--accent); color: #111;
  border: none; border-radius: 6px;
  padding: 5px 12px; font-size: 12px; font-weight: 600;
  cursor: pointer; transition: opacity 0.2s; font-family: inherit;
}
.vocab-btn-save:hover:not(:disabled) { opacity: 0.85; }
.vocab-btn-save:disabled { opacity: 0.5; cursor: not-allowed; }
.vocab-btn-close {
  background: transparent; border: 1px solid var(--border);
  color: var(--text-muted); border-radius: 6px;
  padding: 5px 10px; font-size: 12px;
  cursor: pointer; transition: all 0.2s; font-family: inherit;
}
.vocab-btn-close:hover { border-color: #ef4444; color: #ef4444; }

.popup-enter-active, .popup-leave-active { transition: all 0.15s ease; }
.popup-enter-from, .popup-leave-to { opacity: 0; transform: translateY(-6px) scale(0.96); }

/* ── Vocab Tab ── */
.vocab-header {
  display: flex; align-items: center; justify-content: space-between;
  padding: 12px 16px;
  border-bottom: 1px solid var(--border);
  flex-shrink: 0;
}
.vocab-header-title { font-size: 14px; font-weight: 600; color: var(--text); }
.btn-create-deck {
  background: var(--accent); color: #111;
  border: none; border-radius: 7px;
  padding: 5px 12px; font-size: 12px; font-weight: 600;
  cursor: pointer; transition: opacity 0.2s; font-family: inherit;
}
.btn-create-deck:hover { opacity: 0.85; }

.vocab-list {
  flex: 1; overflow-y: auto; padding: 8px;
  display: flex; flex-direction: column; gap: 4px;
  scrollbar-width: thin; scrollbar-color: var(--border) transparent;
}
.vocab-empty {
  text-align: center; color: var(--text-muted);
  font-size: 13px; padding: 40px 20px;
  line-height: 1.6;
}
.vocab-item {
  display: flex; align-items: flex-start; gap: 8px;
  background: var(--surface2); border: 1px solid var(--border);
  border-radius: 8px; padding: 8px 10px;
  transition: border-color 0.15s;
}
.vocab-item:hover { border-color: var(--accent2); }
.vocab-item-word {
  font-size: 13px; font-weight: 600;
  color: var(--accent); min-width: 80px; flex-shrink: 0;
}
.vocab-item-meaning {
  font-size: 12px; color: var(--text-muted);
  flex: 1; line-height: 1.4;
}
.vocab-item-del {
  background: transparent; border: none;
  color: var(--text-muted); cursor: pointer;
  font-size: 12px; padding: 2px 4px;
  border-radius: 4px; transition: color 0.2s;
  flex-shrink: 0;
}
.vocab-item-del:hover { color: #ef4444; }

/* ── Deck Modal ── */
.deck-modal {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 16px;
  padding: 28px;
  width: 100%; max-width: 500px;
  max-height: 80vh;
  display: flex; flex-direction: column;
  gap: 16px;
  box-shadow: 0 20px 60px rgba(0,0,0,0.5);
  overflow-y: auto;
}
.deck-modal-title {
  font-size: 18px; font-weight: 700; color: var(--text);
  flex-shrink: 0;
}
.deck-modal-section { display: flex; flex-direction: column; gap: 6px; }
.deck-label { font-size: 13px; font-weight: 500; color: var(--text-muted); }
.deck-input {
  background: var(--surface2); border: 1px solid var(--border);
  color: var(--text); border-radius: 8px;
  padding: 8px 12px; font-size: 14px; outline: none;
  font-family: inherit; transition: border-color 0.2s;
}
.deck-input:focus { border-color: var(--accent2); }
.deck-select-all {
  display: flex; align-items: center; gap: 8px;
  font-size: 13px; color: var(--text-muted);
  padding: 6px 0;
}
.deck-vocab-picks {
  display: flex; flex-direction: column; gap: 4px;
  max-height: 260px; overflow-y: auto;
  scrollbar-width: thin; scrollbar-color: var(--border) transparent;
}
.deck-vocab-pick {
  display: flex; align-items: center; gap: 10px;
  padding: 7px 10px;
  background: var(--surface2); border: 1px solid var(--border);
  border-radius: 7px; cursor: pointer;
  transition: border-color 0.15s;
}
.deck-vocab-pick:hover { border-color: var(--accent2); }
.deck-vocab-pick input[type="checkbox"] { flex-shrink: 0; cursor: pointer; }
.pick-word { font-size: 13px; font-weight: 600; color: var(--accent); min-width: 80px; }
.pick-meaning { font-size: 12px; color: var(--text-muted); }
.deck-modal-actions {
  display: flex; justify-content: flex-end; gap: 10px;
  flex-shrink: 0;
}
.btn-cancel {
  background: transparent; border: 1px solid var(--border);
  color: var(--text-muted); border-radius: 8px;
  padding: 8px 16px; font-size: 13px;
  cursor: pointer; transition: all 0.2s; font-family: inherit;
}
.btn-cancel:hover { border-color: var(--text-muted); color: var(--text); }
.btn-create {
  background: var(--accent); color: #111;
  border: none; border-radius: 8px;
  padding: 8px 20px; font-size: 13px; font-weight: 600;
  cursor: pointer; transition: opacity 0.2s; font-family: inherit;
}
.btn-create:hover:not(:disabled) { opacity: 0.85; }
.btn-create:disabled { opacity: 0.4; cursor: not-allowed; }

.modal-enter-active, .modal-leave-active { transition: opacity 0.2s ease; }
.modal-enter-from, .modal-leave-to { opacity: 0; }

/* ── OCR Mode ── */
.ocr-container {
  position: relative;
  display: inline-block;
  line-height: 0;
}
.ocr-image {
  display: block;
  max-width: 100%;
  box-shadow: 0 2px 16px rgba(0,0,0,0.35);
}
.ocr-overlay {
  position: absolute;
  inset: 0;
  pointer-events: none;
}
.ocr-text-box {
  position: absolute;
  background: transparent;
  pointer-events: auto;
  cursor: pointer;
  border-radius: 2px;
  border: 2px solid rgba(59, 130, 246, 0.7);
  box-sizing: border-box;
  transition: background 0.12s, border-color 0.12s, box-shadow 0.12s;
}
.ocr-text-box:hover {
  background: rgba(59, 130, 246, 0.08);
  border-color: rgba(59, 130, 246, 1);
  box-shadow: 0 0 0 1px rgba(59, 130, 246, 0.3);
}
.ocr-text-box.selected {
  background: rgba(59, 130, 246, 0.15);
  border-color: rgba(59, 130, 246, 1);
}
.ocr-text-box.selected {
  background: rgba(59, 130, 246, 0.2);
  border-color: #3b82f6;
}


/* ── Japanese Analysis Card ── */
.jp-card {
  display: flex; flex-direction: column; gap: 10px;
  font-family: inherit;
}
.jp-sentence {
  font-size: 18px; font-weight: 700;
  color: var(--text);
  letter-spacing: 0.05em;
  line-height: 1.6;
  padding: 10px 12px;
  background: var(--surface);
  border-left: 3px solid var(--accent);
  border-radius: 4px;
}
.jp-translation {
  font-size: 14px; color: var(--text);
  padding: 6px 10px;
  background: rgba(91,141,238,0.08);
  border-radius: 6px;
  line-height: 1.5;
}
.jp-explanation {
  font-size: 13px; color: var(--text-muted);
  line-height: 1.6;
}
.jp-section-title {
  font-size: 12px; font-weight: 700;
  text-transform: uppercase; letter-spacing: 0.08em;
  color: var(--text-muted);
  margin-top: 4px;
}
.jp-grammar-list, .jp-vocab-list {
  display: flex; flex-direction: column; gap: 6px;
}
.jp-grammar-item {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 8px;
  padding: 8px 10px;
  display: flex; flex-direction: column; gap: 4px;
}
.jp-grammar-header {
  display: flex; align-items: center; gap: 8px;
}
.jp-pattern {
  font-size: 14px; font-weight: 700;
  color: var(--accent);
}
.jp-badge {
  font-size: 10px; font-weight: 700;
  padding: 2px 7px; border-radius: 20px;
  border: 1px solid;
  flex-shrink: 0;
}
.jp-grammar-meaning {
  font-size: 13px; color: var(--text);
}
.jp-example {
  font-size: 12px; color: var(--text-muted);
  font-style: italic;
}
.jp-vocab-item {
  display: flex; align-items: center; gap: 8px; flex-wrap: wrap;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 6px;
  padding: 6px 10px;
}
.jp-vocab-word {
  font-size: 15px; font-weight: 700; color: var(--text);
}
.jp-vocab-reading {
  font-size: 12px; color: var(--text-muted);
}
.jp-vocab-meaning {
  font-size: 13px; color: var(--text); flex: 1;
}
</style>

<style>
/* Ẩn popup tra từ của web gốc khi đang ở trang reader */
.word-result-modal,
.translation-modal,
[class*="lookup"],
[class*="word-popup"],
[class*="WordResult"],
[class*="TranslationModal"] {
  display: none !important;
}

/* Override pdfjs-dist/web/pdf_viewer.css — global để có priority cao hơn */
.textLayer span {
  color: transparent !important;
  cursor: text !important;
  user-select: text !important;
  -webkit-user-select: text !important;
  pointer-events: auto !important;
}

.textLayer {
  user-select: text !important;
  -webkit-user-select: text !important;
  cursor: text !important;
  overflow: visible !important;
}

.textLayer span::selection,
.textLayer ::selection {
  background: rgba(91, 141, 238, 0.35) !important;
  color: transparent !important;
}
</style>