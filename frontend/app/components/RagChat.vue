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
          <div class="max-w-[80%] bg-[#1f6feb] text-white rounded-2xl rounded-br-md px-3 py-2 text-sm">
            {{ msg.content }}
          </div>
        </div>

        <!-- Assistant bubble -->
        <div v-else class="flex flex-col gap-2">
          <div class="flex items-start gap-2">
            <div class="w-6 h-6 rounded-full bg-[#f0c040] flex items-center justify-center text-black text-xs font-bold shrink-0 mt-0.5">🤖</div>
            <div class="flex-1 min-w-0">
              <div
                class="bg-[#161b22] border border-[#30363d] rounded-2xl rounded-tl-md px-3 py-2 text-sm text-gray-100 prose prose-invert prose-sm max-w-none"
                v-html="formatAnswer(cleanAnswer(msg.answer || ''))"
              ></div>

              <!-- Cache hit badge -->
              <div v-if="msg.cacheHit" class="mt-1 pl-1">
                <span class="text-[9px] px-1.5 py-0.5 rounded-full bg-yellow-400/10 text-yellow-400 border border-yellow-400/30" title="Phản hồi tức thì từ cache">⚡ Cache</span>
              </div>

              <!-- Citations inline -->
              <div v-if="msg.citations?.length" class="mt-1.5 flex flex-wrap gap-1.5 pl-1">
                <button
                  v-for="citation in msg.citations"
                  :key="`c-${idx}-${citation.sourceId}`"
                  @click="emit('highlight-source', { pageNumber: citation.pageNumber, text: getHighlightKeywords(msg) })"
                  @mouseenter="emit('highlight-source', { pageNumber: citation.pageNumber, text: getHighlightKeywords(msg) })"
                  class="px-2 py-0.5 text-[10px] rounded-full border border-[#30363d] bg-[#0d1117] text-[#58a6ff] hover:border-[#f0c040] hover:text-[#f0c040] transition"
                >
                  {{ citation.label }}
                </button>
              </div>

              <!-- Sources collapsible -->
              <details v-if="msg.sources?.length" class="mt-1.5 pl-1">
                <summary class="text-[10px] text-gray-500 cursor-pointer hover:text-gray-300 transition select-none">
                  {{ msg.sources.length }} nguồn truy xuất
                </summary>
                <div class="mt-1.5 space-y-1.5">
                  <div
                    v-for="source in msg.sources"
                    :key="`s-${idx}-${source.sourceId}`"
                    class="bg-[#0d1117] border border-[#30363d] rounded-lg p-2"
                  >
                    <div class="flex items-center justify-between gap-2">
                      <button
                        @click="emit('highlight-source', { pageNumber: source.pageNumber, text: getHighlightKeywords(msg) })"
                        @mouseenter="emit('highlight-source', { pageNumber: source.pageNumber, text: getHighlightKeywords(msg) })"
                        class="text-[11px] font-bold text-[#58a6ff] hover:text-[#f0c040] transition text-left"
                      >
                        [{{ source.sourceId }}] Tr.{{ source.pageNumber }}, đoạn {{ source.chunkIndex + 1 }}
                      </button>
                      <span class="text-[9px] font-mono text-gray-600 shrink-0">{{ formatScore(source.score) }}</span>
                    </div>
                    <p class="mt-1 text-[10px] text-gray-400 line-clamp-3">{{ source.text }}</p>
                  </div>
                </div>
              </details>
            </div>
          </div>
        </div>
      </template>

      <!-- Typing indicator: show only while waiting for first chunk -->
      <div v-if="asking && !messages.at(-1)?.answer" class="flex items-center gap-2">
        <div class="w-6 h-6 rounded-full bg-[#f0c040] flex items-center justify-center text-black text-xs font-bold shrink-0">🤖</div>
        <div class="bg-[#161b22] border border-[#30363d] rounded-2xl px-3 py-2 flex gap-1.5">
          <span class="w-1.5 h-1.5 bg-gray-400 rounded-full animate-bounce" style="animation-delay: 0ms"></span>
          <span class="w-1.5 h-1.5 bg-gray-400 rounded-full animate-bounce" style="animation-delay: 150ms"></span>
          <span class="w-1.5 h-1.5 bg-gray-400 rounded-full animate-bounce" style="animation-delay: 300ms"></span>
        </div>
      </div>

      <div v-if="error" class="bg-red-500/10 border border-red-500/30 text-red-300 rounded-xl p-2 text-xs">
        {{ error }}
      </div>
    </div>

    <!-- Input area -->
    <div class="p-3 border-t border-[#30363d] bg-[#161b22] shrink-0">
      <div class="flex gap-2 items-end">
        <textarea
          ref="inputEl"
          v-model="question"
          rows="1"
          class="flex-1 bg-[#0d1117] border border-[#30363d] text-[#c9d1d9] text-sm rounded-xl px-3 py-2 outline-none focus:border-[#58a6ff] resize-none max-h-28 min-h-[38px]"
          placeholder="Hỏi về tài liệu..."
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
import { ref, nextTick, computed, watch, onMounted } from 'vue'

type DocumentRagSource = {
  sourceId: number
  jobId: number
  projectId: number
  pageNumber: number
  chunkIndex: number
  text: string
  score: number
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

async function newSession() {
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
      messages.value = []
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
    // auto-create session on first message
    await newSession()
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

function scrollToBottom() {
  nextTick(() => {
    if (chatScrollEl.value) {
      chatScrollEl.value.scrollTop = chatScrollEl.value.scrollHeight
    }
  })
}

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

async function askDocument() {
  if (!props.jobId || !question.value.trim() || asking.value) return

  const userText = question.value.trim()
  question.value = ''

  if (inputEl.value) inputEl.value.style.height = 'auto'

  messages.value.push({ role: 'user', content: userText })
  scrollToBottom()

  asking.value = true
  error.value = ''

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
      }),
    })

    if (!res.ok || !res.body) {
      throw new Error(await readError(res, 'API hỏi tài liệu lỗi'))
    }

    const reader = res.body.getReader()
    const decoder = new TextDecoder()
    let buffer = ''

    while (true) {
      const { done, value } = await reader.read()
      if (done) break
      buffer += decoder.decode(value, { stream: true })

      // Parse SSE events from buffer
      const lines = buffer.split('\n')
      buffer = lines.pop() ?? '' // last partial line stays in buffer

      let eventType = ''
      for (const line of lines) {
        if (line.startsWith('event:')) {
          eventType = line.slice('event:'.length).trim()
        } else if (line.startsWith('data:')) {
          const data = line.slice('data:'.length).trim()
          handleStreamEvent(eventType, data, assistantIdx)
          eventType = ''
        }
      }
    }

    scrollToBottom()
  } catch (err: any) {
    error.value = err?.message || 'Không thể hỏi tài liệu.'
    messages.value.splice(assistantIdx, 1) // remove empty placeholder
    messages.value.pop() // remove user message
  } finally {
    asking.value = false
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
    msg.answer = (msg.answer ?? '') + data
    msg.content = msg.answer
    scrollToBottom()
  } else if (type === 'done') {
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
  } else if (type === 'error') {
    msg.answer = data
    msg.content = data
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
</style>
