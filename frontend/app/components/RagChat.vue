<template>
  <div class="flex h-full bg-[#0d1117] border-l border-[#30363d] text-[#c9d1d9]">

    <!-- Sessions sidebar -->
    <div :class="['shrink-0 border-r border-[#30363d] bg-[#010409] flex flex-col transition-all duration-200', showSessions ? 'w-44' : 'w-0 overflow-hidden']">
      <div class="p-2 border-b border-[#30363d] flex items-center justify-between">
        <span class="text-[10px] font-bold text-gray-400 uppercase tracking-widest">Hội thoại</span>
        <button @click="newSession" class="text-[10px] text-[#58a6ff] hover:text-white transition">+ Mới</button>
      </div>
      <div class="flex-1 overflow-y-auto custom-scrollbar">
        <button
          v-for="s in sessions"
          :key="s.id"
          @click="loadSession(s.id)"
          :class="['w-full text-left px-2 py-1.5 text-[10px] border-b border-[#21262d] hover:bg-[#161b22] transition group relative', currentSessionId === s.id ? 'bg-[#161b22] text-white' : 'text-gray-400']"
        >
          <div class="truncate pr-4">{{ s.title }}</div>
          <div class="text-[9px] text-gray-600 mt-0.5">{{ s.messageCount }} tin</div>
          <button
            @click.stop="deleteSession(s.id)"
            class="absolute right-1 top-1.5 opacity-0 group-hover:opacity-100 text-red-400 hover:text-red-300 text-[10px] transition"
          >✕</button>
        </button>
        <div v-if="sessions.length === 0" class="text-[10px] text-gray-600 p-2">Chưa có hội thoại</div>
      </div>
    </div>

    <!-- Main chat area -->
    <div class="flex flex-col flex-1 min-w-0">
    <!-- Header + setup -->
    <div class="p-3 border-b border-[#30363d] bg-[#161b22] shrink-0 space-y-2">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-2">
          <button @click="showSessions = !showSessions" class="text-gray-500 hover:text-gray-300 transition" title="Danh sách hội thoại">
            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"/></svg>
          </button>
          <h3 class="font-bold text-white flex items-center gap-2 text-sm">
            <span class="text-[#f0c040]">🤖</span> AI RAG tài liệu
          </h3>
        </div>
        <button
          v-if="messages.length > 0"
          @click="newSession"
          class="text-[10px] text-gray-500 hover:text-[#f0c040] transition px-2 py-1 rounded border border-transparent hover:border-[#f0c040]/30"
        >
          + Mới
        </button>
      </div>
      <div class="grid grid-cols-2 gap-2">
        <button
          @click="scanAllOcrForRag"
          :disabled="!jobId || !props.onScanAllOcr || scanningAllOcr || indexing"
          class="px-2 py-1.5 rounded-lg bg-[#1f6feb] text-white text-[10px] font-bold uppercase tracking-widest disabled:bg-[#30363d] disabled:text-gray-500 hover:bg-[#388bfd] transition flex items-center justify-center gap-1"
        >
          <span v-if="scanningAllOcr" class="w-2.5 h-2.5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
          {{ scanningAllOcr ? 'Đang quét...' : 'Quét OCR' }}
        </button>
        <button
          @click="indexDocument"
          :disabled="!jobId || indexing || scanningAllOcr"
          class="px-2 py-1.5 rounded-lg bg-[#238636] text-white text-[10px] font-bold uppercase tracking-widest disabled:bg-[#30363d] disabled:text-gray-500 hover:bg-[#2ea043] transition flex items-center justify-center gap-1"
        >
          <span v-if="indexing" class="w-2.5 h-2.5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
          {{ indexing ? 'Đang index...' : 'Index tài liệu' }}
        </button>
      </div>
      <div v-if="indexStatus" class="text-[10px] text-gray-400 bg-[#0d1117] border border-[#30363d] rounded-lg px-2 py-1">
        {{ indexStatus }}
      </div>
    </div>

    <!-- Chat messages -->
    <div ref="chatScrollEl" class="flex-1 overflow-y-auto p-3 space-y-4 custom-scrollbar">
      <div v-if="!jobId" class="bg-yellow-500/10 border border-yellow-500/30 text-yellow-200 rounded-xl p-3 text-xs">
        Chưa có jobId OCR. Hãy mở tài liệu đã qua OCR.
      </div>

      <div v-if="messages.length === 0 && !asking" class="flex flex-col items-center justify-center text-gray-500 text-sm pt-8 gap-2">
        <span class="text-3xl">💬</span>
        <p>Hỏi bất cứ điều gì về tài liệu này.</p>
        <p class="text-xs text-gray-600">Index trước rồi hỏi sau.</p>
      </div>

      <template v-for="(msg, idx) in messages" :key="idx">
        <!-- User bubble -->
        <div v-if="msg.role === 'user'" class="flex justify-end">
          <div class="max-w-[80%] bg-[#1f6feb] text-white rounded-[22px] rounded-br-md px-3 py-2 text-sm shadow-sm animate-message-in">
            {{ msg.content }}
          </div>
        </div>

        <!-- Clarify bubble -->
        <div v-else-if="msg.clarify" class="flex flex-col gap-2 animate-message-in">
          <div class="max-w-[86%] bg-amber-950/30 border border-amber-700/50 rounded-[22px] rounded-tl-md px-3 py-2.5 shadow-sm">
            <p class="text-xs text-amber-300 font-medium mb-2">💬 {{ msg.clarify.question }}</p>
            <div v-if="msg.clarify.options?.length" class="flex flex-col gap-1">
              <button
                v-for="opt in msg.clarify.options"
                :key="opt"
                @click="respondToClarify(messages[idx - 1]?.content ?? '', opt)"
                class="text-left text-[11px] px-2.5 py-1.5 rounded-lg bg-neutral-800 border border-amber-700/50 hover:bg-amber-900/40 text-amber-200 transition truncate"
              >{{ opt }}</button>
              <button
                @click="respondToClarify(messages[idx - 1]?.content ?? '', '')"
                class="text-left text-[11px] px-2.5 py-1.5 rounded-lg bg-neutral-800 border border-neutral-600 hover:bg-neutral-700 text-neutral-400 transition"
              >Tất cả tài liệu</button>
            </div>
            <div v-else class="flex gap-1 mt-1">
              <input
                type="text"
                placeholder="Nhập lại câu hỏi..."
                class="flex-1 text-xs px-2 py-1 rounded-lg border border-amber-700/50 bg-neutral-900 text-gray-200 outline-none focus:border-amber-500"
                @keydown.enter.prevent="e => { respondToClarify((e.target as HTMLInputElement).value, ''); (e.target as HTMLInputElement).value = '' }"
              />
            </div>
          </div>
        </div>

        <!-- Assistant bubble — chỉ hiện khi stream xong -->
        <div v-else-if="msg.answer" class="flex flex-col gap-2 animate-message-in">
          <div class="max-w-[86%]">
            <div
              class="assistant-bubble bg-[#161b22]/95 border border-[#30363d] rounded-[22px] rounded-tl-md px-3 py-2.5 text-sm text-gray-100 prose prose-invert prose-sm max-w-none shadow-sm"
              v-html="formatAnswer(cleanAnswer(msg.answer || ''))"
            ></div>

              <!-- Cache hit badge -->
              <div v-if="msg.cacheHit" class="mt-1.5 pl-1">
                <span class="text-[9px] px-1.5 py-0.5 rounded-full bg-yellow-400/10 text-yellow-400 border border-yellow-400/30" title="Phản hồi tức thì từ cache">⚡ Cache</span>
              </div>

              <!-- Citations inline -->
              <div v-if="msg.citations?.length" class="mt-2 flex flex-wrap gap-1.5 pl-1 transition-opacity duration-300">
                <button
                  v-for="citation in msg.citations"
                  :key="`c-${idx}-${citation.sourceId}`"
                  @click="emit('highlight-source', { pageNumber: citation.pageNumber, text: getHighlightKeywords(msg) })"
                  @mouseenter="emit('highlight-source', { pageNumber: citation.pageNumber, text: getHighlightKeywords(msg) })"
                  class="px-2 py-0.5 text-[10px] rounded-full border border-[#30363d] bg-[#0d1117]/90 text-[#58a6ff] hover:border-[#f0c040] hover:text-[#f0c040] transition"
                >
                  {{ citation.label }}
                </button>
              </div>

              <!-- Sources collapsible -->
              <details v-if="msg.sources?.length" class="mt-2 pl-1 transition-opacity duration-300">
                <summary class="text-[10px] text-gray-500 cursor-pointer hover:text-gray-300 transition select-none">
                  {{ msg.sources.length }} nguồn truy xuất
                </summary>
                <div class="mt-1.5 space-y-1.5">
                  <div
                    v-for="source in msg.sources"
                    :key="`s-${idx}-${source.sourceId}`"
                    class="bg-[#0d1117]/90 border border-[#30363d] rounded-xl p-2"
                  >
                    <div class="flex items-center justify-between gap-2">
                      <button
                        @click="emit('highlight-source', { pageNumber: source.pageNumber, text: getHighlightKeywords(msg) })"
                        @mouseenter="emit('highlight-source', { pageNumber: source.pageNumber, text: getHighlightKeywords(msg) })"
                        class="text-[11px] font-bold text-[#58a6ff] hover:text-[#f0c040] transition text-left"
                      >
                        [{{ source.sourceId }}] Tr.{{ source.pageNumber }}, đoạn {{ source.chunkIndex + 1 }}<span v-if="source.occurrenceCount && source.occurrenceCount > 1"> · {{ source.occurrenceCount }} vị trí</span>
                      </button>
                      <span class="text-[9px] font-mono text-gray-600 shrink-0">{{ formatScore(source.score) }}</span>
                    </div>
                    <p v-if="source.occurrenceSummary" class="mt-1 text-[10px] text-[#8b949e] line-clamp-2">{{ source.occurrenceSummary }}</p>
                    <p class="mt-1 text-[10px] text-gray-400 line-clamp-3">{{ source.text }}</p>
                  </div>
                </div>
              </details>
          </div>
        </div>
      </template>

      <!-- Typing indicator: hiện khi đang hỏi hoặc đang stream -->
      <div v-if="asking && !messages.at(-1)?.answer" class="max-w-[82%] animate-message-in">
        <div class="assistant-bubble bg-[#161b22]/95 border border-[#30363d] rounded-[22px] rounded-tl-md px-3 py-2.5 shadow-sm">
          <div class="flex items-center gap-2 text-[11px] text-gray-400">
            <span class="thinking-pulse"></span>
            <span class="thinking-label" :data-text="thinkingLabel">{{ thinkingLabel }}</span>
          </div>
        </div>
      </div>

      <div v-if="error" class="bg-red-500/10 border border-red-500/30 text-red-300 rounded-xl p-2 text-xs">
        {{ error }}
      </div>
    </div>

    <!-- Input area -->
    <div class="p-3 border-t border-[#30363d] bg-[#161b22] shrink-0 space-y-2">
      <!-- Mode selector -->
      <div class="flex gap-1">
        <button
          v-for="m in modes" :key="m.value"
          @click="selectedMode = m.value"
          :class="[
            'flex-1 py-1 text-[9px] font-bold rounded-lg transition border',
            selectedMode === m.value
              ? 'bg-[#f0c040] text-black border-[#f0c040]'
              : 'bg-transparent text-gray-500 border-[#30363d] hover:border-gray-500 hover:text-gray-300'
          ]"
          :title="m.desc"
        >{{ m.label }}</button>
      </div>
      <div class="flex gap-2 items-end">
        <textarea
          ref="inputEl"
          v-model="question"
          rows="1"
          class="flex-1 bg-[#0d1117] border border-[#30363d] text-[#c9d1d9] text-sm rounded-xl px-3 py-2 outline-none focus:border-[#58a6ff] resize-none max-h-28 min-h-[38px]"
          :placeholder="selectedMode === 'fast' ? 'Hỏi rõ ý — phản hồi nhanh...' : 'Hỏi về tài liệu...'"
          @keydown.enter.exact.prevent="askDocument"
          @input="autoResize"
        />
        <button
          @click="askDocument"
          :disabled="!jobId || !question.trim() || asking"
          class="w-9 h-9 rounded-xl bg-[#f0c040] text-black flex items-center justify-center disabled:bg-[#30363d] disabled:text-gray-500 hover:bg-[#e3b330] transition shrink-0"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M12 19V5m0 0l-7 7m7-7l7 7" />
          </svg>
        </button>
      </div>
    </div>
    </div> <!-- end main chat area -->
  </div>
</template>

<script setup lang="ts">
import { ref, nextTick, computed, watch, onMounted, onBeforeUnmount } from 'vue'

type DocumentRagSource = {
  sourceId: number
  jobId: number
  projectId: number
  pageNumber: number
  chunkIndex: number
  text: string
  score: number
  occurrenceCount?: number
  occurrenceSummary?: string
}

type DocumentRagCitation = {
  sourceId: number
  pageNumber: number
  chunkIndex: number
  label: string
}

type ChatMessage = {
  role: 'user' | 'assistant'
  content: string
  answer?: string
  attributedAnswer?: string
  sources?: DocumentRagSource[]
  citations?: DocumentRagCitation[]
  cacheHit?: boolean
  clarify?: { reason: string; question: string; options?: string[] }
  isStreaming?: boolean
}

type DocumentRagTurn = {
  role: string
  content: string
}

const props = defineProps<{
  jobId: number | null
  projectId?: number | null
  onScanAllOcr?: () => Promise<{ scannedPages?: number; skipped?: boolean; busy?: boolean } | void>
}>()

const emit = defineEmits<{
  (e: 'jump-to-page', pageNumber: number): void
  (e: 'highlight-source', payload: { pageNumber: number; text: string }): void
}>()

const config = useRuntimeConfig()
const question = ref('')
const messages = ref<ChatMessage[]>([])
const indexing = ref(false)
const scanningAllOcr = ref(false)
const asking = ref(false)
const error = ref('')
const indexStatus = ref('')
const chatScrollEl = ref<HTMLElement | null>(null)
const inputEl = ref<HTMLTextAreaElement | null>(null)
const selectedMode = ref('high')
const streamPhase = ref<'idle' | 'retrieving' | 'thinking' | 'answering'>('idle')
const modes = [
  { value: 'fast', label: '⚡ Nhanh', desc: 'Không mở rộng query, không rerank — trả lời ngay. Cần hỏi rõ ý.' },
  { value: 'balance', label: '⚖ Cân bằng', desc: 'Mở rộng query, không rerank — nhanh hơn High.' },
  { value: 'high', label: '🎯 Chính xác', desc: 'Đầy đủ pipeline: MultiQuery + HyDE + Rerank.' },
]
const thinkingMessages = [
  'Đang tìm đoạn liên quan...',
  'Đang phân tích tài liệu...',
  'Đang tổng hợp thông tin...',
  'Đang soạn câu trả lời...',
  'Đang kiểm tra độ chính xác...',
]
const thinkingIdx = ref(0)
let thinkingTimer: ReturnType<typeof setInterval> | null = null

const thinkingLabel = computed(() => thinkingMessages[thinkingIdx.value])

function startRotating() {
  thinkingIdx.value = 0
  if (thinkingTimer) clearInterval(thinkingTimer)
  thinkingTimer = setInterval(() => {
    thinkingIdx.value = (thinkingIdx.value + 1) % thinkingMessages.length
  }, 700)
}

function stopRotating() {
  if (thinkingTimer) { clearInterval(thinkingTimer); thinkingTimer = null }
}

let pendingChunk = ''
let pendingChunkAssistantIdx: number | null = null
let pendingChunkTimer: ReturnType<typeof setTimeout> | null = null

// Session management
type SessionSummary = { id: number; title: string; messageCount: number; updatedAt: string }
const sessions = ref<SessionSummary[]>([])
const currentSessionId = ref<number | null>(null)
const showSessions = ref(false)

const _sessionId = ref('')
watch(() => props.jobId, () => {
  _sessionId.value = `${props.jobId ?? 'na'}-${Date.now().toString(36).slice(-6)}`
  messages.value = []
  currentSessionId.value = null
  if (props.jobId) loadSessions()
}, { immediate: true })

onMounted(() => { if (props.jobId) loadSessions() })

async function loadSessions() {
  if (!props.jobId) return
  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/chat/sessions?scopeType=file&scopeId=${props.jobId}`, { headers: getAuthHeaders() })
    if (res.ok) sessions.value = await res.json()
  } catch { /* silent */ }
}

async function loadSession(id: number) {
  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/chat/sessions/${id}`, { headers: getAuthHeaders() })
    if (!res.ok) return
    const data = await res.json()
    currentSessionId.value = id
    _sessionId.value = `db-${id}`
    messages.value = data.messages.map((m: any) => ({
      role: m.role,
      content: m.content,
      answer: m.role === 'assistant' ? m.content : undefined,
      sources: m.sourcesJson ? JSON.parse(m.sourcesJson) : undefined,
      citations: m.citationsJson ? JSON.parse(m.citationsJson) : undefined,
      cacheHit: m.cacheHit,
    }))
    scrollToBottom()
  } catch { /* silent */ }
}

// Thêm tham số keepMessages
async function newSession(keepMessages?: boolean) {
  // Biến keepMessages === true đảm bảo an toàn, tránh nhận nhầm MouseEvent từ nút click
  const shouldKeep = keepMessages === true; 
  
  if (!props.jobId) return
  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/chat/sessions`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify({ scopeType: 'file', scopeId: props.jobId, title: 'Hội thoại mới' })
    })
    if (res.ok) {
      const data = await res.json()
      currentSessionId.value = data.id
      _sessionId.value = `db-${data.id}`
      
      // SỬA TẠI ĐÂY: Chỉ xóa mảng messages nếu không phải là auto-save
      if (!shouldKeep) {
        messages.value = []
      }
      
      sessions.value.unshift({ id: data.id, title: data.title, messageCount: 0, updatedAt: new Date().toISOString() })
    }
  } catch { /* silent */ }
}

async function deleteSession(id: number) {
  try {
    await fetch(`${config.public.apiBaseUrl}/api/chat/sessions/${id}`, { method: 'DELETE', headers: getAuthHeaders() })
    sessions.value = sessions.value.filter(s => s.id !== id)
    if (currentSessionId.value === id) { messages.value = []; currentSessionId.value = null }
  } catch { /* silent */ }
}

async function saveTurnToSession(userText: string, assistantAnswer: string, sourcesJson?: string, citationsJson?: string) {
  if (!currentSessionId.value) {
    // SỬA TẠI ĐÂY: Truyền `true` để không bị clear mất tin nhắn đang hiển thị
    await newSession(true)
  }
  
  if (!currentSessionId.value) return
  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/chat/sessions/${currentSessionId.value}/turn`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify({ userMessage: userText, assistantMessage: assistantAnswer, sourcesJson, citationsJson, cacheHit: false })
    })
    if (res.ok) {
      const data = await res.json()
      // update title in sidebar
      const s = sessions.value.find(x => x.id === currentSessionId.value)
      if (s) { s.title = data.title; s.messageCount += 2 }
    }
  } catch { /* silent */ }
}

function formatScore(score: number) {
  if (typeof score !== 'number') return '0.000'
  return score.toFixed(3)
}

function getAuthHeaders() {
  const token = localStorage.getItem('jwt_token') || ''
  return {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${token}`,
  }
}

async function readError(res: Response, fallback: string) {
  const data = await res.json().catch(() => null)
  return data?.message || data?.Message || fallback
}

function clearChat() {
  messages.value = []
  error.value = ''
}

function autoResize(e: Event) {
  const el = e.target as HTMLTextAreaElement
  el.style.height = 'auto'
  el.style.height = Math.min(el.scrollHeight, 112) + 'px'
}

function scrollToBottom(smooth = false) {
  nextTick(() => {
    if (chatScrollEl.value) {
      chatScrollEl.value.scrollTo({
        top: chatScrollEl.value.scrollHeight,
        behavior: smooth ? 'smooth' : 'auto',
      })
    }
  })
}

function resetPendingChunk() {
  pendingChunk = ''
  pendingChunkAssistantIdx = null
  if (pendingChunkTimer) {
    clearTimeout(pendingChunkTimer)
    pendingChunkTimer = null
  }
}

function flushPendingChunk() {
  if (pendingChunkAssistantIdx === null || !pendingChunk) {
    resetPendingChunk()
    return
  }

  const msg = messages.value[pendingChunkAssistantIdx]
  if (msg) {
    msg.answer = (msg.answer ?? '') + pendingChunk
    msg.content = msg.answer
    scrollToBottom()
  }

  pendingChunk = ''
  pendingChunkAssistantIdx = null
  if (pendingChunkTimer) {
    clearTimeout(pendingChunkTimer)
    pendingChunkTimer = null
  }
}

function queueChunkAppend(assistantIdx: number, chunk: string) {
  pendingChunkAssistantIdx = assistantIdx
  pendingChunk += chunk
  if (pendingChunkTimer) return
  pendingChunkTimer = setTimeout(flushPendingChunk, 45)
}

onBeforeUnmount(() => {
  resetPendingChunk()
  stopRotating()
})

function buildConversationHistory(): DocumentRagTurn[] {
  return messages.value
    .filter(m => m.role === 'user' || (m.role === 'assistant' && m.answer))
    .slice(-8)
    .map(m => ({
      role: m.role,
      content: m.role === 'user' ? m.content : cleanAnswer(m.attributedAnswer || m.answer || ''),
    }))
}

function getHighlightKeywords(msg: ChatMessage): string {
  const userMsg = messages.value.find((m, i) =>
    m.role === 'user' && messages.value[i + 1] === msg
  )
  return `${userMsg?.content || ''} ${msg.answer || ''}`.trim()
}

function cleanAnswer(text: string): string {
  if (!text) return ''
  return text
    // Remove [Nguồn X, Tr.Y] and all variants (with/without spaces, with/without Tr.)
    .replace(/\[Nguồn\s*\d+(?:[,、]\s*\d+)*(?:[,、]\s*Tr\.?\s*\d+)?\]/g, '')
    // Remove (Nguồn X...) round bracket variants
    .replace(/\(Nguồn\s*\d+[^)]*\)/g, '')
    // Remove [X], [X, Y], [X,Y,Z] pure numeric citations
    .replace(/\[\d+(?:[,、\s]*\d+)*\]/g, '')
    // Clean up trailing punctuation artifacts left after removing citations
    .replace(/\s*,\s*\./g, '.')
    .replace(/\s*,\s*,/g, ',')
    .replace(/\s+\./g, '.')
    .replace(/\s+,/g, ',')
    .replace(/\s{2,}/g, ' ')
    .trim()
}

function formatAnswer(text: string): string {
  if (!text) return ''
  const strongOpen = '<strong class="text-white font-semibold">'
  const strongClose = '<' + '/strong>'
  const brClose = '<' + '/b>'
  if (/<(ul|ol|li|b|strong|em|br|p|h[1-6])\b/i.test(text)) {
    return text
      .replace(/<b>/gi, strongOpen)
      .replace(new RegExp(brClose, 'gi'), strongClose)
  }
  return text
    .replace(/\*\*(.+?)\*\*/g, (_m, p1) => `${strongOpen}${p1}${strongClose}`)
    .replace(/\n/g, '<' + 'br>')
}



async function indexDocument() {
  if (!props.jobId || indexing.value) return

  indexing.value = true
  error.value = ''
  indexStatus.value = ''

  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/rag/documents/${props.jobId}/index`, {
      method: 'POST',
      headers: getAuthHeaders(),
    })

    if (!res.ok) {
      throw new Error(await readError(res, 'Index tài liệu lỗi'))
    }

    const data = await res.json()
    indexStatus.value = data.status === 'indexed'
      ? `Đã index ${data.pagesIndexed} trang / ${data.chunksIndexed} đoạn.`
      : 'Chưa có OCR text để index.'
  } catch (err: any) {
    error.value = err?.message || 'Không thể index tài liệu.'
  } finally {
    indexing.value = false
  }
}

async function scanAllOcrForRag() {
  if (!props.jobId || scanningAllOcr.value || !props.onScanAllOcr) return

  scanningAllOcr.value = true
  error.value = ''
  indexStatus.value = ''

  try {
    const result = await props.onScanAllOcr()
    if (result?.busy) {
      indexStatus.value = 'Tiến trình khác đang chạy, vui lòng thử lại sau.'
      return
    }

    const scanned = result?.scannedPages ?? 0
    indexStatus.value = scanned > 0
      ? `Đã quét OCR thêm ${scanned} trang còn thiếu.`
      : 'OCR đã đầy đủ cho toàn bộ tài liệu.'
  } catch (err: any) {
    error.value = err?.message || 'Không thể quét OCR toàn bộ tài liệu.'
  } finally {
    scanningAllOcr.value = false
  }
}

function respondToClarify(originalQuestion: string, clarification: string) {
  const combined = clarification
    ? `Về tài liệu "${clarification}": ${originalQuestion}`
    : originalQuestion
  question.value = combined
  const lastIdx = messages.value.length - 1
  if (messages.value[lastIdx]?.clarify) messages.value.splice(lastIdx, 1)
  const prevIdx = messages.value.length - 1
  if (messages.value[prevIdx]?.role === 'user') messages.value.splice(prevIdx, 1)
  nextTick(() => askDocument())
}

async function askDocument() {
  if (!props.jobId || !question.value.trim() || asking.value) return

  const userText = question.value.trim()
  question.value = ''

  if (inputEl.value) inputEl.value.style.height = 'auto'

  messages.value.push({ role: 'user', content: userText })
  scrollToBottom(true)

  asking.value = true
  error.value = ''
  streamPhase.value = 'retrieving'
  resetPendingChunk()
  startRotating()

  // Add a placeholder assistant message we'll fill via streaming
  const assistantIdx = messages.value.length
  messages.value.push({ role: 'assistant', content: '', answer: '', sources: [], citations: [] })

  try {
    const history = buildConversationHistory()
    const historyWithoutLastUser = history.slice(0, -1)

    const res = await fetch(`${config.public.apiBaseUrl}/api/rag/documents/${props.jobId}/ask/stream`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify({
        question: userText,
        topK: 5,
        conversationHistory: historyWithoutLastUser,
        sessionId: _sessionId.value,
        mode: selectedMode.value,
      }),
    })

    if (!res.ok || !res.body) {
      throw new Error(await readError(res, 'API hỏi tài liệu lỗi'))
    }

    const reader = res.body.getReader()
    const decoder = new TextDecoder()
    let eventType = '' 
    let buffer = ''

    while (true) {
      const { done, value } = await reader.read()
      
      if (value) {
        buffer += decoder.decode(value, { stream: true })
      }

      const lines = buffer.split('\n')
      buffer = lines.pop() ?? '' // Giữ lại phần chưa có \n

      for (const line of lines) {
        if (line.trim() === '') {
          eventType = '' 
          continue
        }

        if (line.startsWith('event:')) {
          eventType = line.slice(6).trim()
        } else if (line.startsWith('data:')) {
          const dataStr = line.slice(5)
          const cleanData = dataStr.startsWith(' ') ? dataStr.slice(1) : dataStr
          handleStreamEvent(eventType, cleanData, assistantIdx)
        }
      }

      if (done) {
        // QUAN TRỌNG: Xử lý nốt dòng cuối cùng nếu BE ngắt đột ngột 
        // mà không có dấu xuống dòng (\n)
        if (buffer.trim() !== '') {
          if (buffer.startsWith('data:')) {
            const dataStr = buffer.slice(5)
            const cleanData = dataStr.startsWith(' ') ? dataStr.slice(1) : dataStr
            handleStreamEvent(eventType || 'chunk', cleanData, assistantIdx)
          }
        }
        break // Xử lý xong buffer cuối mới được thoát
      }
    }

    scrollToBottom()
  } catch (err: any) {
    resetPendingChunk()
    error.value = err?.message || 'Không thể hỏi tài liệu.'
    messages.value.splice(assistantIdx, 1) // remove empty placeholder
    messages.value.pop() // remove user message
  } finally {
    asking.value = false
    streamPhase.value = 'idle'
    const lastMsg = messages.value.at(-1)
    if (lastMsg && lastMsg.role === 'assistant') {
      lastMsg.isStreaming = false
      flushPendingChunk()
    }
  }
}

function handleStreamEvent(type: string, data: string, assistantIdx: number) {
  const msg = messages.value[assistantIdx]
  if (!msg) return

  if (type === 'sources') {
    try {
      const parsed = JSON.parse(data)
      msg.sources = parsed.sources ?? []
    } catch { /* ignore */ }
  } else if (type === 'chunk') {
    msg.isStreaming = true
    queueChunkAppend(assistantIdx, data)
  } else if (type === 'done') {
    flushPendingChunk()
    stopRotating()
    msg.isStreaming = false
    try {
      const parsed = JSON.parse(data)
      msg.answer = parsed.answer ?? msg.answer
      msg.attributedAnswer = parsed.attributedAnswer ?? msg.answer
      msg.citations = parsed.citations ?? []
      msg.cacheHit = parsed.cacheHit === true
      msg.content = msg.answer
      
      // Auto-save to DB session
      const userMsg = messages.value.slice(0, assistantIdx).findLast((m: any) => m.role === 'user')
      if (userMsg) {
        saveTurnToSession(
          userMsg.content,
          msg.answer ?? '',
          msg.sources ? JSON.stringify(msg.sources) : undefined,
          msg.citations ? JSON.stringify(msg.citations) : undefined
        )
      }
    } catch { /* ignore */ }
    streamPhase.value = 'idle'
    scrollToBottom()
  } else if (type === 'clarify') {
    flushPendingChunk()
    stopRotating()
    try {
      const p = JSON.parse(data)
      msg.clarify = p
      msg.content = p.question
    } catch { }
    streamPhase.value = 'idle'
    scrollToBottom()
  } else if (type === 'error') {
    flushPendingChunk()
    stopRotating()
    msg.answer = data
    msg.content = data
    msg.isStreaming = false
    streamPhase.value = 'idle'
  }
}
</script>

<style scoped>
.custom-scrollbar::-webkit-scrollbar {
  width: 4px;
}

.custom-scrollbar::-webkit-scrollbar-track {
  background: transparent;
}

.custom-scrollbar::-webkit-scrollbar-thumb {
  background-color: #484f58;
  border-radius: 10px;
}

.assistant-bubble {
  backdrop-filter: blur(10px);
}

.animate-message-in {
  animation: message-in 220ms ease-out;
}

.thinking-label {
  position: relative;
  display: inline-block;
  color: inherit;
}

.thinking-label::after {
  content: attr(data-text);
  position: absolute;
  inset: 0;
  color: transparent;
  background-image: linear-gradient(
    90deg,
    transparent 0%,
    transparent 34%,
    rgba(250, 204, 21, 0.98) 47%,
    rgba(245, 158, 11, 0.95) 50%,
    rgba(250, 204, 21, 0.98) 53%,
    transparent 66%,
    transparent 100%
  );
  background-size: 220% 100%;
  background-repeat: no-repeat;
  -webkit-background-clip: text;
  background-clip: text;
  animation: thinking-shimmer 2.15s linear infinite;
}

.thinking-pulse {
  width: 7px;
  height: 7px;
  border-radius: 9999px;
  background: #f0c040;
  box-shadow: 0 0 0 0 rgba(240, 192, 64, 0.35);
  animation: thinking-pulse 1.8s ease-out infinite;
}

.line-clamp-3 {
  display: -webkit-box;
  -webkit-line-clamp: 3;
  -webkit-box-orient: vertical;
  overflow: hidden;
}

.prose :deep(ul) {
  list-style-type: disc;
  padding-left: 1.25rem;
  margin: 0.4rem 0;
}

.prose :deep(ol) {
  list-style-type: decimal;
  padding-left: 1.25rem;
  margin: 0.4rem 0;
}

.prose :deep(li) {
  margin: 0.2rem 0;
  color: #c9d1d9;
}

.prose :deep(strong) {
  color: #ffffff;
}

@keyframes thinking-shimmer {
  0% { background-position: 140% 0; }
  100% { background-position: -40% 0; }
}

@keyframes message-in {
  from { opacity: 0; transform: translateY(6px); }
  to { opacity: 1; transform: translateY(0); }
}

@keyframes thinking-pulse {
  0% { box-shadow: 0 0 0 0 rgba(240, 192, 64, 0.35); opacity: 0.8; }
  70% { box-shadow: 0 0 0 9px rgba(240, 192, 64, 0); opacity: 1; }
  100% { box-shadow: 0 0 0 0 rgba(240, 192, 64, 0); opacity: 0.8; }
}
</style>
