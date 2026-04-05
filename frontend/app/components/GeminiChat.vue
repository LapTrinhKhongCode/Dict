<template>
  <div class="flex flex-col h-full overflow-hidden relative bg-white dark:bg-gray-900 transition-colors">
    <div class="flex items-center gap-3 p-4 border-b border-gray-200 dark:border-gray-700 shrink-0 transition-colors">
      <div class="w-8 h-8 rounded-lg flex items-center justify-center font-bold text-white bg-gradient-to-br from-blue-500 to-purple-600 shadow-sm">
        G
      </div>
      <div>
        <p class="font-semibold text-sm text-gray-900 dark:text-white">Trợ lý Gemini</p>
        <p class="text-xs text-gray-500 dark:text-gray-400">
          <span v-if="accessDenied" class="text-red-500 font-medium">Bị chặn truy cập</span>
          <span v-else>{{ apiKey ? 'Đã kết nối API' : 'Chưa có API Key' }}</span>
        </p>
      </div>
      <button @click="clearChat" :disabled="accessDenied" class="ml-auto p-2 text-gray-500 dark:text-gray-400 hover:text-red-500 dark:hover:text-red-400 rounded-md hover:bg-gray-100 dark:hover:bg-gray-800 disabled:opacity-50 transition-colors">
        ↺ Mới
      </button>
    </div>

    <div v-if="accessDenied" class="absolute inset-0 top-[65px] z-50 flex flex-col items-center justify-center bg-gray-50/95 dark:bg-gray-900/95 backdrop-blur-sm p-6 text-center">
      <div class="w-16 h-16 bg-red-100 dark:bg-red-900/30 text-red-500 rounded-full flex items-center justify-center mb-4">
        <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"/>
        </svg>
      </div>
      <h3 class="text-lg font-bold text-gray-900 dark:text-white mb-2">Chức năng bị vô hiệu hóa</h3>
      <p class="text-sm text-gray-500 dark:text-gray-400">
        Bạn không có quyền truy cập vào nội dung của tài liệu này, do đó AI không thể trả lời câu hỏi của bạn.
      </p>
    </div>

    <div v-else class="flex-1 overflow-y-auto p-4 space-y-5 custom-scrollbar" ref="messagesEl">
      <div v-if="messages.length === 0" class="text-center mt-10">
        <div class="w-16 h-16 mx-auto bg-blue-50 dark:bg-blue-900/20 text-blue-500 rounded-full flex items-center justify-center mb-4">
          <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 10h.01M12 10h.01M16 10h.01M9 16H5a2 2 0 01-2-2V6a2 2 0 012-2h14a2 2 0 012 2v8a2 2 0 01-2 2h-5l-5 5v-5z"/>
          </svg>
        </div>
        <p class="font-bold text-lg text-gray-900 dark:text-white mb-2">Xin chào!</p>
        <p class="text-sm text-gray-500 dark:text-gray-400 mb-6">Hỏi tôi bất cứ điều gì về tài liệu:<br/><b class="text-gray-700 dark:text-gray-300">"{{ pdfName }}"</b></p>
        <div class="flex flex-col gap-2 max-w-xs mx-auto">
          <button v-for="q in quickQuestions" :key="q" @click="sendQuick(q)" 
            class="p-2.5 text-sm bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-blue-50 dark:hover:bg-blue-900/30 hover:border-blue-300 dark:hover:border-blue-700 text-left rounded-xl transition-all border border-gray-200 dark:border-gray-700 shadow-sm">
            {{ q }}
          </button>
        </div>
      </div>

      <div v-for="(msg, i) in messages" :key="i" :class="['flex gap-3', msg.role === 'user' ? 'flex-row-reverse' : '']">
        <div class="w-8 h-8 rounded-lg flex items-center justify-center text-xs font-bold text-white shrink-0 shadow-sm" 
             :class="msg.role === 'user' ? 'bg-blue-600' : 'bg-gradient-to-br from-blue-500 to-purple-600'">
          {{ msg.role === 'user' ? 'U' : 'G' }}
        </div>
        <div class="max-w-[85%] p-3.5 rounded-2xl text-[14px] leading-relaxed shadow-sm" 
             :class="msg.role === 'user' 
               ? 'bg-blue-600 text-white rounded-tr-sm' 
               : 'bg-gray-100 dark:bg-gray-800 text-gray-800 dark:text-gray-200 border border-gray-200 dark:border-gray-700 rounded-tl-sm'">
          <div class="prose-sm dark:prose-invert" v-html="formatMessage(msg.content)"></div>
        </div>
      </div>
      
      <div v-if="isLoading" class="flex gap-3">
        <div class="w-8 h-8 rounded-lg bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-xs font-bold text-white shadow-sm shrink-0">G</div>
        <div class="p-4 bg-gray-100 dark:bg-gray-800 rounded-2xl rounded-tl-sm border border-gray-200 dark:border-gray-700 flex items-center gap-1.5 shadow-sm">
          <span class="w-2 h-2 bg-gray-400 dark:bg-gray-500 rounded-full animate-bounce"></span>
          <span class="w-2 h-2 bg-gray-400 dark:bg-gray-500 rounded-full animate-bounce" style="animation-delay: 0.2s"></span>
          <span class="w-2 h-2 bg-gray-400 dark:bg-gray-500 rounded-full animate-bounce" style="animation-delay: 0.4s"></span>
        </div>
      </div>
    </div>

    <div class="p-4 border-t border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-900 shrink-0 transition-colors">
      <div class="flex items-end gap-2 bg-gray-50 dark:bg-gray-800 border border-gray-300 dark:border-gray-600 p-2 rounded-2xl focus-within:border-blue-500 dark:focus-within:border-blue-400 focus-within:ring-2 focus-within:ring-blue-500/20 transition-all shadow-sm">
        <textarea 
          v-model="userInput" 
          ref="inputEl"
          class="flex-1 max-h-32 bg-transparent outline-none resize-none text-[14px] text-gray-900 dark:text-white p-2 placeholder-gray-400 dark:placeholder-gray-500 custom-scrollbar" 
          rows="1" 
          :placeholder="accessDenied ? 'Trợ lý đã bị khóa...' : 'Hỏi AI...'" 
          :disabled="accessDenied"
          @input="autoResize" 
          @keydown.enter.exact.prevent="sendMessage">
        </textarea>
        <button @click="sendMessage" 
                :disabled="!userInput.trim() || isLoading || !apiKey || accessDenied" 
                class="w-10 h-10 flex items-center justify-center bg-blue-600 text-white rounded-xl disabled:opacity-50 disabled:bg-gray-400 dark:disabled:bg-gray-600 hover:bg-blue-700 transition-all shrink-0">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="18" height="18">
            <line x1="22" y1="2" x2="11" y2="13"></line>
            <polygon points="22 2 15 22 11 13 2 9 22 2"></polygon>
          </svg>
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
  ragIndex: { type: Array, default: () => [] },
  // Bổ sung Props mới để mẹ truyền xuống
  accessDenied: { type: Boolean, default: false } 
})

const messages = ref([])
const userInput = ref('')
const isLoading = ref(false)
const messagesEl = ref(null)
const inputEl = ref(null)

const quickQuestions = ['Tóm tắt tài liệu này', 'Các điểm chính là gì?', 'Giải thích thuật ngữ khó']
// List models mới nhất của Google
const GEMINI_MODELS = ['gemini-2.5-flash', 'gemini-2.0-flash-lite']

function ragRetrieve(query, topK = 5) {
  if (!props.ragIndex.length) return []
  const q = query.toLowerCase()
  return props.ragIndex.filter(c => c.text.toLowerCase().includes(q)).slice(0, topK)
}

async function callGeminiStream(model, systemContext, history, text, imageBase64 = null, onChunk) {
  const userParts = []
  if (imageBase64 && typeof imageBase64 === 'string') {
    const base64Data = imageBase64.replace(/^data:image\/\w+;base64,/, '')
    userParts.push({ inline_data: { mime_type: 'image/jpeg', data: base64Data } })
  }
  userParts.push({ text })
  
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
  if (props.accessDenied) return; // Chặn thêm 1 lớp bảo vệ cứng bằng code
  
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
    const ragCtx = chunks.length 
      ? '\n\n---\nĐOẠN THAM KHẢO TỪ TÀI LIỆU:\n' + chunks.map(c => `[Trang ${c.page}]: ${c.text}`).join('\n\n') + '\n---' 
      : ''
    
    const systemContext = `Bạn là trợ lý AI đọc tài liệu "${props.pdfName}".${ragCtx}\nTrả lời bằng tiếng Việt.`
    
    messages.value.push({ role: 'model', content: '', streaming: true })
    const streamingIdx = messages.value.length - 1
    
    // Gọi API stream
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

function sendQuick(q) { 
  if (props.accessDenied) return;
  userInput.value = q; 
  sendMessage() 
}

function clearChat() { messages.value = [] }

async function scrollBottom() { 
  await nextTick(); 
  if (messagesEl.value) messagesEl.value.scrollTop = messagesEl.value.scrollHeight 
}

function autoResize(e) { 
  e.target.style.height = 'auto'; 
  e.target.style.height = Math.min(e.target.scrollHeight, 120) + 'px' 
}

function formatMessage(raw) { 
  return raw
    .replace(/\n/g, '<br/>')
    .replace(/\*\*(.*?)\*\*/g, '<b>$1</b>')
    .replace(/\*(.*?)\*/g, '<i>$1</i>')
    .replace(/`(.*?)`/g, '<code class="bg-gray-200 dark:bg-gray-700 px-1 py-0.5 rounded font-mono text-[12px]">$1</code>')
}
</script>

<style scoped>
/* Tuỳ chỉnh thanh cuộn cho khu vực tin nhắn */
.custom-scrollbar::-webkit-scrollbar {
  width: 5px;
}
.custom-scrollbar::-webkit-scrollbar-track {
  background: transparent;
}
.custom-scrollbar::-webkit-scrollbar-thumb {
  background-color: #d1d5db;
  border-radius: 10px;
}
:global(.dark) .custom-scrollbar::-webkit-scrollbar-thumb {
  background-color: #4b5563;
}
</style>