<template>
  <div class="flex flex-col h-full overflow-hidden relative bg-white dark:bg-gray-900">
    <div class="flex items-center gap-3 p-4 border-b border-gray-200 dark:border-gray-700 shrink-0">
      <div class="w-8 h-8 rounded-lg flex items-center justify-center font-bold text-white bg-gradient-to-br from-blue-500 to-purple-600">G</div>
      <div>
        <p class="font-semibold text-sm">Trợ lý Gemini</p>
        <p class="text-xs text-gray-500">{{ apiKey ? 'Đã kết nối' : 'Chưa có API Key' }}</p>
      </div>
      <button @click="clearChat" class="ml-auto p-2 text-gray-500 hover:text-red-500 rounded-md hover:bg-gray-100 transition">
        ↺ Mới
      </button>
    </div>

    <div class="flex-1 overflow-y-auto p-4 space-y-4" ref="messagesEl">
      <div v-if="messages.length === 0" class="text-center text-gray-500 mt-10">
        <p class="font-bold text-lg mb-2">Xin chào!</p>
        <p class="text-sm mb-4">Hỏi tôi bất cứ điều gì về tài liệu "{{ pdfName }}"</p>
        <div class="flex flex-col gap-2 max-w-xs mx-auto">
          <button v-for="q in quickQuestions" :key="q" @click="sendQuick(q)" class="p-2 text-sm bg-gray-100 hover:bg-blue-50 text-left rounded-lg transition border border-gray-200">
            {{ q }}
          </button>
        </div>
      </div>

      <div v-for="(msg, i) in messages" :key="i" :class="['flex gap-3', msg.role === 'user' ? 'flex-row-reverse' : '']">
        <div class="w-7 h-7 rounded flex items-center justify-center text-xs font-bold text-white shrink-0" :class="msg.role === 'user' ? 'bg-blue-600' : 'bg-gradient-to-br from-blue-500 to-purple-600'">
          {{ msg.role === 'user' ? 'U' : 'G' }}
        </div>
        <div class="max-w-[85%] p-3 rounded-xl text-sm leading-relaxed" :class="msg.role === 'user' ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-800 border border-gray-200'">
          <div v-html="formatMessage(msg.content)"></div>
        </div>
      </div>
      
      <div v-if="isLoading" class="flex gap-3">
        <div class="w-7 h-7 rounded bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-xs font-bold text-white">G</div>
        <div class="p-3 bg-gray-100 rounded-xl flex items-center gap-1">
          <span class="w-2 h-2 bg-gray-400 rounded-full animate-bounce"></span>
          <span class="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style="animation-delay: 0.2s"></span>
          <span class="w-2 h-2 bg-gray-400 rounded-full animate-bounce" style="animation-delay: 0.4s"></span>
        </div>
      </div>
    </div>

    <div class="p-3 border-t border-gray-200 bg-white shrink-0">
      <div class="flex items-end gap-2 bg-gray-50 border border-gray-300 p-2 rounded-xl focus-within:border-blue-500 transition">
<textarea 
  v-model="userInput" 
  ref="inputEl"
  class="flex-1 max-h-32 bg-transparent outline-none resize-none text-sm font-semibold text-gray-900 p-1" 
  rows="1" 
  placeholder="Hỏi AI..." 
  @input="autoResize" 
  @keydown.enter.exact.prevent="sendMessage">
</textarea>
        <button @click="sendMessage" :disabled="!userInput.trim() || isLoading || !apiKey" class="p-2 bg-blue-600 text-white rounded-lg disabled:opacity-50 hover:bg-blue-700 transition shrink-0">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="16" height="16"><line x1="22" y1="2" x2="11" y2="13"></line><polygon points="22 2 15 22 11 13 2 9 22 2"></polygon></svg>
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, nextTick } from 'vue'

const props = defineProps({
  apiKey: { type: String, required: true },
  pdfName: { type: String, default: 'Tài liệu' },
  ragIndex: { type: Array, default: () => [] } // Mảng RAG truyền từ cha vào
})

const messages = ref([])
const userInput = ref('')
const isLoading = ref(false)
const messagesEl = ref(null)
const inputEl = ref(null)

const quickQuestions = ['Tóm tắt tài liệu này', 'Các điểm chính là gì?', 'Giải thích thuật ngữ']
const GEMINI_MODELS = ['gemini-2.0-flash', 'gemini-2.5-flash']

function ragRetrieve(query, topK = 5) {
  if (!props.ragIndex.length) return []
  const q = query.toLowerCase()
  return props.ragIndex.filter(c => c.text.toLowerCase().includes(q)).slice(0, topK)
}

async function callGeminiStream(model, systemContext, history, text, imageBase64 = null, onChunk) {
  const userParts = []
  // FIX: Sửa lỗi kiểm tra imageBase64, đảm bảo chỉ replace khi nó là string hợp lệ
  if (imageBase64 && typeof imageBase64 === 'string') {
    const base64Data = imageBase64.replace(/^data:image\/\w+;base64,/, '')
    userParts.push({ inline_data: { mime_type: 'image/jpeg', data: base64Data } })
  }
  
  userParts.push({ text })
  
  // FIX: Dùng props.apiKey thay vì apiKey.value
  const res = await fetch(
    `https://generativelanguage.googleapis.com/v1beta/models/${model}:streamGenerateContent?alt=sse&key=${props.apiKey}`,
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
  let fullText = '', buffer = ''
  while (true) {
    const { done, value } = await reader.read()
    if (done) break
    buffer += decoder.decode(value, { stream: true })
    const lines = buffer.split('\n')
    buffer = lines.pop()
    for (const line of lines) {
      if (!line.startsWith('data: ')) continue
      const raw = line.slice(6).trim()
      if (raw === '[DONE]') continue
      try {
        const json = JSON.parse(raw)
        const chunk = json.candidates?.[0]?.content?.parts?.[0]?.text || ''
        if (chunk) { fullText += chunk; onChunk(fullText) }
        if (json.error) throw { message: json.error.message, status: json.error.status }
      } catch (e) { if (e.message && e.status) throw e }
    }
  }
  return fullText || 'Không có phản hồi.'
}

async function sendMessage() {
  const text = userInput.value.trim()
  if (!text || isLoading.value || !props.apiKey) return
  messages.value.push({ role: 'user', content: text })
  userInput.value = ''
  if (inputEl.value) inputEl.value.style.height = 'auto'
  isLoading.value = true
  await scrollBottom()

  try {
    const history = messages.value.slice(0, -1).map(m => ({ role: m.role, parts: [{ text: m.content }] }))
    const chunks = ragRetrieve(text)
    const ragCtx = chunks.length ? '\n\n---\nĐOẠN THAM KHẢO TỪ TÀI LIỆU:\n' + chunks.map(c => `[Trang ${c.page}]: ${c.text}`).join('\n\n') + '\n---' : ''
    
    const systemContext = `Bạn là trợ lý AI đọc tài liệu "${props.pdfName}".${ragCtx}\nTrả lời bằng tiếng Việt.`
    
    messages.value.push({ role: 'model', content: '', streaming: true })
    const streamingIdx = messages.value.length - 1
    
    // FIX: Truyền rõ tham số thứ 5 (imageBase64) là null
    await callGeminiStream(GEMINI_MODELS[0], systemContext, history, text, null, (partial) => {
      messages.value[streamingIdx].content = partial
      scrollBottom()
    })
    
    messages.value[streamingIdx].streaming = false
  } catch (err) {
    messages.value.push({ role: 'model', content: `❌ Lỗi: ${err.message}` })
  } finally {
    isLoading.value = false
    await scrollBottom()
  }
}

function sendQuick(q) { userInput.value = q; sendMessage() }
function clearChat() { messages.value = [] }
async function scrollBottom() { await nextTick(); if (messagesEl.value) messagesEl.value.scrollTop = messagesEl.value.scrollHeight }
function autoResize(e) { e.target.style.height = 'auto'; e.target.style.height = Math.min(e.target.scrollHeight, 120) + 'px' }
function formatMessage(raw) { return raw.replace(/\n/g, '<br/>').replace(/\*\*(.*?)\*\*/g, '<b>$1</b>') }
</script>