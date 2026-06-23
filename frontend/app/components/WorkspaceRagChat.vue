<template>
  <div class="flex flex-col h-full bg-white dark:bg-neutral-900 text-gray-900 dark:text-neutral-100">
    <!-- Header -->
    <div class="p-3 border-b border-gray-200 dark:border-neutral-700 bg-gray-50 dark:bg-neutral-800 shrink-0 space-y-2">
      <div class="flex items-center justify-between">
        <h3 class="font-semibold flex items-center gap-2 text-sm text-gray-700 dark:text-neutral-200">
          <span>🌐</span> {{ scopeLabel ? `Hỏi về ${scopeLabel}` : 'Toàn bộ workspace' }}
        </h3>
        <button
          v-if="messages.length > 0"
          @click="clearChat"
          class="text-[10px] text-gray-400 dark:text-neutral-500 hover:text-red-500 transition px-2 py-1 rounded border border-transparent hover:border-red-400/30"
        >
          Xóa
        </button>
      </div>
      <button
        @click="indexAll"
        :disabled="indexing || (!workspaceId && !projectId)"
        class="w-full px-3 py-1.5 rounded-lg bg-green-600 hover:bg-green-700 disabled:bg-gray-200 dark:disabled:bg-neutral-700 disabled:text-gray-400 text-white text-[11px] font-bold uppercase tracking-widest transition flex items-center justify-center gap-1.5"
      >
        <span v-if="indexing" class="w-2.5 h-2.5 border-2 border-white border-t-transparent rounded-full animate-spin"></span>
        {{ indexing ? indexStatus || 'Đang index...' : '⚡ Index tất cả tài liệu' }}
      </button>
      <div v-if="indexStatus && !indexing" class="text-[10px] text-gray-500 dark:text-neutral-400 bg-gray-100 dark:bg-neutral-800 rounded px-2 py-1">{{ indexStatus }}</div>
    </div>

    <!-- Messages -->
    <div ref="chatScrollEl" class="flex-1 overflow-y-auto p-4 space-y-4 custom-scrollbar">
      <div v-if="messages.length === 0 && !asking" class="flex flex-col items-center justify-center text-gray-400 dark:text-neutral-500 text-sm pt-10 gap-2">
        <span class="text-3xl">🌐</span>
        <p>Hỏi về bất kỳ tài liệu nào trong workspace.</p>
        <p class="text-xs text-gray-400 dark:text-neutral-600">Nhớ index từng tài liệu trước khi hỏi.</p>
      </div>

      <template v-for="(msg, idx) in messages" :key="idx">
        <!-- User bubble -->
        <div v-if="msg.role === 'user'" class="flex justify-end">
          <div class="max-w-[80%] bg-blue-600 text-white rounded-[22px] rounded-br-md px-3 py-2 text-sm shadow-sm animate-message-in">
            {{ msg.content }}
          </div>
        </div>

        <!-- Assistant bubble — chỉ hiện khi đã có content -->
        <div v-else-if="msg.answer" class="flex flex-col gap-2 animate-message-in">
          <div class="max-w-[86%]">
            <div
              v-if="msg.answer"
              class="assistant-bubble bg-gray-100/95 dark:bg-neutral-800/95 border border-gray-200 dark:border-neutral-700 rounded-[22px] rounded-tl-md px-3 py-2.5 text-sm prose prose-sm max-w-none dark:prose-invert shadow-sm"
              v-html="formatAnswer(cleanAnswer(msg.answer || ''))"
            ></div>

              <!-- Citations -->
              <div v-if="msg.citations?.length" class="mt-2 flex flex-wrap gap-1.5 pl-1 transition-opacity duration-300">
                <span
                  v-for="citation in msg.citations"
                  :key="`c-${idx}-${citation.sourceId}`"
                  class="px-2 py-0.5 text-[10px] rounded-full border border-gray-300 dark:border-neutral-600 bg-white/90 dark:bg-neutral-900/80 text-blue-600 dark:text-blue-400"
                >
                  {{ citation.label }}
                </span>
              </div>

              <!-- Sources -->
              <details v-if="msg.sources?.length" class="mt-2 pl-1 transition-opacity duration-300">
                <summary class="text-[10px] text-gray-400 dark:text-neutral-500 cursor-pointer hover:text-gray-600 dark:hover:text-neutral-300 transition select-none">
                  {{ msg.sources.length }} nguồn truy xuất
                </summary>
                <div class="mt-1.5 space-y-1.5">
                  <div
                    v-for="source in msg.sources"
                    :key="`s-${idx}-${source.sourceId}`"
                    @mouseenter="emit('highlight-doc', source.jobId)"
                    @mouseleave="emit('highlight-doc', null)"
                    class="bg-gray-50/90 dark:bg-neutral-800/85 border border-gray-200 dark:border-neutral-700 rounded-xl p-2 transition"
                  >
                    <div class="flex items-center justify-between gap-2 flex-wrap">
                      <button
                        @click="openInReader(source)"
                        class="text-[11px] font-bold text-blue-600 dark:text-blue-400 hover:text-yellow-500 dark:hover:text-yellow-400 transition text-left"
                        title="Mở trong reader"
                      >
                        [{{ source.sourceId }}] {{ source.documentName || 'Tài liệu' }} — Tr.{{ source.pageNumber }}<span v-if="source.occurrenceCount && source.occurrenceCount > 1"> · {{ source.occurrenceCount }} vị trí</span>
                      </button>
                      <span class="text-[9px] font-mono text-gray-400 dark:text-neutral-500 shrink-0">{{ formatScore(source.score) }}</span>
                    </div>
                    <p v-if="source.occurrenceSummary" class="mt-1 text-[10px] text-gray-500 dark:text-neutral-400 line-clamp-2">{{ source.occurrenceSummary }}</p>
                    <p class="mt-1 text-[10px] text-gray-500 dark:text-neutral-400 line-clamp-3">{{ source.text }}</p>
                  </div>
                </div>
              </details>
          </div>
        </div>
      </template>

      <!-- Typing indicator -->
      <div v-if="asking && !messages.at(-1)?.answer" class="max-w-[82%] animate-message-in">
        <div class="assistant-bubble bg-gray-100/95 dark:bg-neutral-800/95 border border-gray-200 dark:border-neutral-700 rounded-[22px] rounded-tl-md px-3 py-2.5 shadow-sm">
          <div class="flex items-center gap-2 text-[11px] text-gray-500 dark:text-neutral-400">
            <span class="thinking-pulse"></span>
            <span class="thinking-label" :data-text="getThinkingLabel()">{{ getThinkingLabel() }}</span>
          </div>
        </div>
      </div>

      <div v-if="error" class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 text-red-600 dark:text-red-400 rounded-xl p-2 text-xs">
        {{ error }}
      </div>
    </div>

    <!-- Input -->
    <div class="p-3 border-t border-gray-200 dark:border-neutral-700 bg-gray-50 dark:bg-neutral-800 shrink-0 space-y-2">
      <!-- Mode selector -->
      <div class="flex gap-1">
        <button
          v-for="m in modes" :key="m.value"
          @click="selectedMode = m.value"
          :class="[
            'flex-1 py-1 text-[9px] font-bold rounded-lg transition border',
            selectedMode === m.value
              ? 'bg-yellow-400 text-black border-yellow-400'
              : 'text-gray-400 dark:text-neutral-500 border-gray-200 dark:border-neutral-700 hover:border-gray-400 dark:hover:border-neutral-500'
          ]"
          :title="m.desc"
        >{{ m.label }}</button>
      </div>
      <div class="flex gap-2 items-end">
        <textarea
          ref="inputEl"
          v-model="question"
          rows="1"
          class="flex-1 bg-white dark:bg-neutral-900 border border-gray-300 dark:border-neutral-600 text-gray-900 dark:text-neutral-100 text-sm rounded-xl px-3 py-2 outline-none focus:border-blue-500 dark:focus:border-blue-400 resize-none max-h-28 min-h-[38px]"
          placeholder="Hỏi về toàn bộ tài liệu workspace..."
          @keydown.enter.exact.prevent="askWorkspace"
          @input="autoResize"
        />
        <button
          @click="askWorkspace"
          :disabled="(!workspaceId && !projectId) || !question.trim() || asking"
          class="w-9 h-9 rounded-xl bg-yellow-400 text-black flex items-center justify-center disabled:bg-gray-200 dark:disabled:bg-gray-700 disabled:text-gray-400 hover:bg-yellow-500 transition shrink-0"
        >
          <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M12 19V5m0 0l-7 7m7-7l7 7" />
          </svg>
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, nextTick, watch, onBeforeUnmount } from 'vue'

type WorkspaceSource = {
  sourceId: number
  jobId: number
  projectId: number
  pageNumber: number
  chunkIndex: number
  text: string
  score: number
  documentName?: string
  occurrenceCount?: number
  occurrenceSummary?: string
}

type WorkspaceCitation = {
  sourceId: number
  pageNumber: number
  chunkIndex: number
  label: string
  documentName?: string
}

type ChatMessage = {
  role: 'user' | 'assistant'
  content: string
  answer?: string
  attributedAnswer?: string
  sources?: WorkspaceSource[]
  citations?: WorkspaceCitation[]
}

type ConversationTurn = { role: string; content: string }

const props = defineProps<{
  workspaceId: number | null
  projectId?: number | null
  scopeLabel?: string
  loadSessionId?: number | null
}>()

const emit = defineEmits<{
  (e: 'highlight-doc', jobId: number | null): void
  (e: 'session-saved'): void
}>()

const config = useRuntimeConfig()
const question = ref('')
const messages = ref<ChatMessage[]>([])
const asking = ref(false)
const indexing = ref(false)
const indexStatus = ref('')
const error = ref('')
const chatScrollEl = ref<HTMLElement | null>(null)
const inputEl = ref<HTMLTextAreaElement | null>(null)
const _sessionId = ref('')
const currentSessionId = ref<number | null>(null)
const selectedMode = ref('high')
const streamPhase = ref<'idle' | 'retrieving' | 'thinking' | 'answering'>('idle')
const modes = [
  { value: 'fast', label: '⚡ Nhanh', desc: 'Không mở rộng query, không rerank' },
  { value: 'balance', label: '⚖ Cân bằng', desc: 'MultiQuery, không rerank' },
  { value: 'high', label: '🎯 Chính xác', desc: 'Đầy đủ pipeline' },
]

let pendingChunk = ''
let pendingChunkAssistantIdx: number | null = null
let pendingChunkTimer: ReturnType<typeof setTimeout> | null = null

watch([() => props.workspaceId, () => props.projectId], () => {
  const key = props.projectId ? `proj-${props.projectId}` : `ws-${props.workspaceId ?? 'na'}`
  _sessionId.value = `${key}-${Date.now().toString(36).slice(-6)}`
  messages.value = []
  currentSessionId.value = null
}, { immediate: true })

// Load session history when a session is selected from sidebar
watch(() => props.loadSessionId, async (id) => {
  if (id === null) return
  if (id === -1) {
    // New session: clear chat
    messages.value = []
    currentSessionId.value = null
    _sessionId.value = `${props.projectId ? `proj-${props.projectId}` : `ws-${props.workspaceId}`}-${Date.now().toString(36).slice(-6)}`
    return
  }
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
    }))
    scrollToBottom()
  } catch { /* silent */ }
})

function getScopeType() { return props.projectId ? 'project' : 'workspace' }
function getScopeId() { return props.projectId ?? props.workspaceId ?? 0 }

async function ensureSession() {
  if (currentSessionId.value) return currentSessionId.value
  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/chat/sessions`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify({ scopeType: getScopeType(), scopeId: getScopeId(), title: 'Hội thoại mới' })
    })
    if (res.ok) {
      const d = await res.json()
      currentSessionId.value = d.id
      _sessionId.value = `db-${d.id}`
    }
  } catch { /* silent */ }
  return currentSessionId.value
}

async function saveTurnToSession(userText: string, assistantAnswer: string, sourcesJson?: string, citationsJson?: string) {
  const sessionId = await ensureSession()
  if (!sessionId) return
  try {
    await fetch(`${config.public.apiBaseUrl}/api/chat/sessions/${sessionId}/turn`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify({ userMessage: userText, assistantMessage: assistantAnswer, sourcesJson, citationsJson, cacheHit: false })
    })
    emit('session-saved')
  } catch { /* silent */ }
}

function getApiEndpoint() {
  if (props.projectId) return `${config.public.apiBaseUrl}/api/rag/project/${props.projectId}/ask/stream`
  return `${config.public.apiBaseUrl}/api/rag/workspace/${props.workspaceId}/ask/stream`
}

function getIndexAllEndpoint() {
  if (props.projectId) return `${config.public.apiBaseUrl}/api/rag/project/${props.projectId}/index-all`
  return `${config.public.apiBaseUrl}/api/rag/workspace/${props.workspaceId}/index-all`
}

function canAsk() {
  return !!(props.projectId || props.workspaceId)
}

async function indexAll() {
  if (indexing.value || !canAsk()) return
  indexing.value = true
  indexStatus.value = ''
  try {
    const res = await fetch(getIndexAllEndpoint(), { method: 'POST', headers: getAuthHeaders() })
    if (!res.ok) throw new Error('Index lỗi')
    const data = await res.json()
    indexStatus.value = `Đã index ${data.indexedJobs}/${data.totalJobs} tài liệu (${data.totalChunks} đoạn)`
  } catch (err: any) {
    indexStatus.value = err?.message || 'Không thể index.'
  } finally {
    indexing.value = false
  }
}

function getAuthHeaders() {
  const token = localStorage.getItem('jwt_token') || ''
  return { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` }
}

async function readError(res: Response, fallback: string) {
  const data = await res.json().catch(() => null)
  return data?.message || data?.Message || fallback
}

function clearChat() { messages.value = []; error.value = '' }

function autoResize(e: Event) {
  const el = e.target as HTMLTextAreaElement
  el.style.height = 'auto'
  el.style.height = Math.min(el.scrollHeight, 112) + 'px'
}

function scrollToBottom(smooth = false) {
  nextTick(() => {
    if (!chatScrollEl.value) return
    chatScrollEl.value.scrollTo({
      top: chatScrollEl.value.scrollHeight,
      behavior: smooth ? 'smooth' : 'auto',
    })
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

function getThinkingLabel() {
  if (streamPhase.value === 'retrieving') return 'Đang tìm đoạn liên quan'
  if (streamPhase.value === 'thinking') return 'Đang phân tích tài liệu'
  if (streamPhase.value === 'answering') return 'Đang viết câu trả lời'
  return 'Đang xử lý'
}

onBeforeUnmount(() => {
  resetPendingChunk()
})

function buildHistory(): ConversationTurn[] {
  return messages.value
    .filter(m => m.role === 'user' || (m.role === 'assistant' && m.answer))
    .slice(-8)
    .map(m => ({ role: m.role, content: m.role === 'user' ? m.content : cleanAnswer(m.answer || '') }))
}

function cleanAnswer(text: string): string {
  if (!text) return ''
  const strongClose = '<' + '/strong>'
  return text
    .replace(/\[Nguồn\s*\d+(?:[,、]\s*\d+)*(?:[,、]\s*Tr\.?\s*\d+)?\]/g, '')
    .replace(/\(Nguồn\s*\d+[^)]*\)/g, '')
    .replace(/\[\d+(?:[,、\s]*\d+)*\]/g, '')
    .replace(/\s*,\s*\./g, '.').replace(/\s*,\s*,/g, ',')
    .replace(/\s+\./g, '.').replace(/\s+,/g, ',')
    .replace(/\s{2,}/g, ' ').trim()
}

function formatAnswer(text: string): string {
  if (!text) return ''
  const strongOpen = '<strong class="font-semibold">'
  const strongClose = '<' + '/strong>'
  const brClose = '<' + '/b>'
  if (/<(ul|ol|li|b|strong|em|br|p|h[1-6])\b/i.test(text)) {
    return text.replace(/<b>/gi, strongOpen).replace(new RegExp(brClose, 'gi'), strongClose)
  }
  return text
    .replace(/\*\*(.+?)\*\*/g, (_m, p1) => `${strongOpen}${p1}${strongClose}`)
    .replace(/\n/g, '<' + 'br>')
}

function formatScore(score: number) {
  return typeof score === 'number' ? score.toFixed(3) : '0.000'
}

function openInReader(source: WorkspaceSource) {
  const url = `/reader?jobId=${source.jobId}&page=${source.pageNumber}`
  window.open(url, '_blank')
}

function handleStreamEvent(type: string, data: string, assistantIdx: number) {
  const msg = messages.value[assistantIdx]
  if (!msg) return
  if (type === 'sources') {
    streamPhase.value = 'thinking'
    try { const p = JSON.parse(data); msg.sources = p.sources ?? [] } catch { }
  } else if (type === 'chunk') {
    streamPhase.value = 'answering'
    queueChunkAppend(assistantIdx, data)
  } else if (type === 'done') {
    flushPendingChunk()
    try {
      const p = JSON.parse(data)
      msg.answer = p.answer ?? msg.answer
      msg.attributedAnswer = p.attributedAnswer ?? msg.answer
      msg.citations = p.citations ?? []
      msg.content = msg.answer
      // Auto-save turn to DB
      const userMsg = messages.value.slice(0, assistantIdx).findLast((m: any) => m.role === 'user')
      if (userMsg) {
        saveTurnToSession(
          userMsg.content,
          msg.answer ?? '',
          msg.sources ? JSON.stringify(msg.sources) : undefined,
          msg.citations ? JSON.stringify(msg.citations) : undefined
        )
      }
    } catch { }
    streamPhase.value = 'idle'
    scrollToBottom()
  } else if (type === 'error') {
    flushPendingChunk()
    msg.answer = data; msg.content = data
    streamPhase.value = 'idle'
  }
}

async function askWorkspace() {
  if (!canAsk() || !question.value.trim() || asking.value) return

  const userText = question.value.trim()
  question.value = ''
  if (inputEl.value) inputEl.value.style.height = 'auto'

  messages.value.push({ role: 'user', content: userText })
  scrollToBottom(true)

  const assistantIdx = messages.value.length
  messages.value.push({ role: 'assistant', content: '', answer: '', sources: [], citations: [] })

  asking.value = true
  error.value = ''
  streamPhase.value = 'retrieving'
  resetPendingChunk()

  try {
    const history = buildHistory().slice(0, -1)
    const res = await fetch(getApiEndpoint(), {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify({ question: userText, topK: 5, conversationHistory: history, sessionId: _sessionId.value, mode: selectedMode.value }),
    })

    if (!res.ok || !res.body) throw new Error(await readError(res, 'API lỗi'))

    const reader = res.body.getReader()
    const decoder = new TextDecoder()
    let buffer = ''

    while (true) {
      const { done, value } = await reader.read()
      if (done) break
      buffer += decoder.decode(value, { stream: true })
      const lines = buffer.split('\n')
      buffer = lines.pop() ?? ''
      let eventType = ''
      for (const line of lines) {
        if (line.startsWith('event:')) { eventType = line.slice('event:'.length).trim() }
        else if (line.startsWith('data:')) {
          handleStreamEvent(eventType, line.slice('data:'.length).trim(), assistantIdx)
          eventType = ''
        }
      }
    }
    scrollToBottom()
  } catch (err: any) {
    resetPendingChunk()
    error.value = err?.message || 'Không thể hỏi workspace.'
    messages.value.splice(assistantIdx, 1)
    messages.value.pop()
  } finally {
    asking.value = false
    streamPhase.value = 'idle'
  }
}
</script>

<style scoped>
.custom-scrollbar::-webkit-scrollbar { width: 4px; }
.custom-scrollbar::-webkit-scrollbar-track { background: transparent; }
.custom-scrollbar::-webkit-scrollbar-thumb { background-color: #cbd5e1; border-radius: 10px; }

.assistant-bubble {
  backdrop-filter: blur(10px);
}

.animate-message-in {
  animation: message-in 220ms ease-out;
}

.typing-dot {
  width: 6px;
  height: 6px;
  background-color: #9ca3af;
  border-radius: 50%;
  display: inline-block;
  animation: typing-bounce 1.05s ease-in-out infinite !important;
}
.dark .typing-dot { background-color: #6b7280; }

.thinking-pulse {
  width: 7px;
  height: 7px;
  border-radius: 9999px;
  background: #f59e0b;
  box-shadow: 0 0 0 0 rgba(245, 158, 11, 0.35);
  animation: thinking-pulse 1.8s ease-out infinite;
}

@keyframes typing-bounce {
  0%, 80%, 100% { transform: translateY(0); opacity: 0.5; }
  40% { transform: translateY(-6px); opacity: 1; }
}

@keyframes message-in {
  from { opacity: 0; transform: translateY(6px); }
  to { opacity: 1; transform: translateY(0); }
}

@keyframes thinking-pulse {
  0% { box-shadow: 0 0 0 0 rgba(245, 158, 11, 0.35); opacity: 0.8; }
  70% { box-shadow: 0 0 0 9px rgba(245, 158, 11, 0); opacity: 1; }
  100% { box-shadow: 0 0 0 0 rgba(245, 158, 11, 0); opacity: 0.8; }
}
</style>
