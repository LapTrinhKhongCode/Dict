<template>
  <div class="app-shell flex flex-col h-screen overflow-hidden bg-[#0d1117] text-[#c9d1d9] font-sans">
    
    <header class="h-14 flex items-center justify-between px-4 border-b border-[#30363d] bg-[#161b22] shrink-0">
      <button @click="goBack" class="flex items-center gap-2 px-3 py-1.5 bg-[#21262d] hover:bg-[#30363d] rounded-lg border border-[#30363d] text-sm transition">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="16" height="16"><polyline points="15 18 9 12 15 6"/></svg>
        Quay lại
      </button>
      <div class="font-bold">{{ pdfName || 'Tài liệu' }}</div>
      <div class="text-xs px-3 py-1 bg-[#1e3a5f] text-[#5b8dee] rounded-full border border-[#5b8dee]/30 flex items-center gap-2">
        <div class="w-2 h-2 rounded-full bg-[#5b8dee]"></div> AI Ready
      </div>
    </header>

    <main class="flex flex-1 overflow-hidden relative">
      
      <section class="flex flex-col relative h-full" :style="{ width: leftPanelWidth + '%' }">
        <PdfViewer 
          v-if="fileUrl || pdfData || jobId"
          :file-url="fileUrl" 
          :file-data="pdfData" 
          :job-id="jobId"
          :api-key="apiKey" 
          @rag-updated="(data) => ragIndex = data"
          @text-selected="handleTextSelection"
        />
        <div v-else class="flex flex-col items-center justify-center h-full text-gray-400">
          <div class="w-8 h-8 border-4 border-gray-600 border-t-[#f0c040] rounded-full animate-spin mb-4"></div>
          <p>Đang chờ nạp dữ liệu OCR/PDF...</p>
        </div>
      </section>

      <div class="w-1.5 bg-[#161b22] border-x border-[#30363d] cursor-col-resize hover:bg-[#5b8dee] transition-colors z-20" @mousedown="startResize"></div>

      <section class="flex-1 flex flex-col bg-[#161b22]">
        <div class="flex border-b border-[#30363d] bg-[#0d1117]">
          <button @click="activeTab='chat'" :class="['flex-1 p-3 text-sm font-semibold transition', activeTab==='chat' ? 'text-[#f0c040] border-b-2 border-[#f0c040] bg-[#21262d]' : 'text-gray-500 hover:text-gray-300']">💬 Trợ lý Chat</button>
          <button @click="activeTab='vocab'" :class="['flex-1 p-3 text-sm font-semibold transition', activeTab==='vocab' ? 'text-[#f0c040] border-b-2 border-[#f0c040] bg-[#21262d]' : 'text-gray-500 hover:text-gray-300']">📚 Sổ từ vựng</button>
        </div>

        <div v-show="activeTab === 'chat'" class="flex-1 overflow-hidden">
          <GeminiChat :api-key="apiKey" :pdf-name="pdfName" :rag-index="ragIndex" />
        </div>

        <div v-show="activeTab === 'vocab'" class="flex-1 overflow-hidden">
          <VocabManager ref="vocabMgrRef" :project-id="projectId" :pdf-name="pdfName" />
        </div>
      </section>

     <Transition name="fade">
  <div 
    v-if="vocabPopup.visible" 
    class="fixed z-[9999] top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 bg-[#1c2128] border border-[#f0c040] p-5 rounded-xl shadow-2xl w-[400px] max-w-[90vw]"
  >
    <h4 class="text-lg font-bold text-[#f0c040] mb-3 line-clamp-4 overflow-hidden break-words leading-tight">
      {{ vocabPopup.word }}
    </h4>

    <div v-if="vocabPopup.loading" class="text-sm text-gray-400 flex items-center gap-2 py-2">
      <span class="w-4 h-4 border-2 border-gray-400 border-t-[#f0c040] rounded-full animate-spin"></span> 
      Đang dịch...
    </div>

    <div v-else>
      <input 
        v-model="vocabPopup.meaning" 
        @keyup.enter="saveVocab" 
        class="w-full bg-[#0d1117] border border-[#30363d] p-2.5 rounded text-sm mb-4 outline-none text-white focus:border-[#f0c040] transition-colors" 
        placeholder="Nhập nghĩa..." 
        ref="vocabInput"
      />
      
      <div class="flex justify-end gap-2">
        <button 
          @click="vocabPopup.visible = false" 
          class="px-4 py-1.5 bg-[#30363d] text-[#c9d1d9] rounded-lg text-sm hover:bg-[#444c56] transition"
        >
          Đóng
        </button>
        <button 
          @click="saveVocab" 
          class="px-4 py-1.5 bg-[#f0c040] text-black font-bold rounded-lg text-sm hover:bg-[#d4a017] transition shadow-lg"
        >
          Lưu từ
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

import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useRuntimeConfig } from '#app'

import PdfViewer from '~/components/PdfViewer.vue'
import GeminiChat from '~/components/GeminiChat.vue'
import VocabManager from '~/components/VocabManager.vue'

const route = useRoute()
const router = useRouter()
const config = useRuntimeConfig()

// --- STATES TRUYỀN DATA ---
const fileUrl = ref('')
const pdfData = ref(null) 
const jobId = ref(null) // <-- BỔ SUNG BIẾN JOB_ID
const pdfName = ref('')
const projectId = ref(null)
const apiKey = ref('')
const leftPanelWidth = ref(60)

const activeTab = ref('chat')
const ragIndex = ref([])
const vocabMgrRef = ref(null)
const vocabPopup = ref({ visible: false, word: '', meaning: '', x: 0, y: 0, loading: false })

// --- LOGIC XỬ LÝ SỰ KIỆN TỪ PDF VIEWER ---
// --- LOGIC XỬ LÝ SỰ KIỆN TỪ PDF VIEWER ---
function handleTextSelection(data) {
  const popupHeight = 220; // Ước lượng chiều cao của popup tra từ
  const popupWidth = 270;  // Ước lượng chiều rộng

  let posX = data.x + 10;
  let posY = data.y + 15; // Mặc định hiện hơi lệch xuống dưới con trỏ chuột

  // CHỐNG LẸM ĐÁY: Nếu thả popup xuống dưới mà vượt quá màn hình -> Lật ngược lên trên
  if (posY + popupHeight > window.innerHeight) {
    posY = data.y - popupHeight - 15;
  }

  // BẢO VỆ: Không cho lẹm lên nóc màn hình
  posY = Math.max(10, posY);

  // BẢO VỆ: Không cho lẹm sát mép phải
  posX = Math.min(posX, window.innerWidth - popupWidth - 10);

  vocabPopup.value = { 
    visible: true, 
    word: data.text, 
    meaning: '', 
    x: posX, 
    y: posY, 
    loading: true 
  }
  
  // Gọi API Dictionary thực tế của bạn
  fetchWordMeaning(data.text)
}
async function fetchWordMeaning(word) {
  try {
    const url = `${config.public.apiBaseUrl}/api/Word/GetWordJson/${encodeURIComponent(word)}`
    const res = await fetch(url)
    if (res.ok) {
      const data = await res.json()
      if (data.status === 200 && data.data) {
        const words = data.data?.words || []
        const firstMeaning = words[0]?.means?.[0]?.mean || ''
        const fallback = data.data?.suggestWords?.[0]?.means?.[0]?.mean || ''
        vocabPopup.value.meaning = firstMeaning || fallback
      }
    }
  } catch (e) {
  } finally {
    vocabPopup.value.loading = false
  }
}

function saveVocab() {
  if (!vocabPopup.value.meaning) return
  if (vocabMgrRef.value) {
    vocabMgrRef.value.addNewVocab({ wordText: vocabPopup.value.word, contextMeaning: vocabPopup.value.meaning })
  }
  vocabPopup.value.visible = false
  activeTab.value = 'vocab' 
}

function startResize() {
  const move = (e) => { leftPanelWidth.value = Math.max(25, Math.min(75, (e.clientX / window.innerWidth) * 100)) }
  const up = () => { document.removeEventListener('mousemove', move); document.removeEventListener('mouseup', up) }
  document.addEventListener('mousemove', move); document.addEventListener('mouseup', up)
}

const goBack = () => router.back()

onMounted(() => {
  apiKey.value = config.public.geminiApiKey || ''
  const { url, id, jobId: qJobId, projectId: pid, name } = route.query
  
  fileUrl.value = url || ''
  pdfName.value = name ? decodeURIComponent(name) : 'Tài liệu'
  projectId.value = pid
  jobId.value = qJobId // LẤY JOB ID TỪ URL
  
  if (id) {
    const raw = sessionStorage.getItem(`pdf_${id}`)
    if (raw) pdfData.value = new Uint8Array(JSON.parse(raw)) 
  }
})
</script>

<style scoped>
/* Hiệu ứng mượt mà cho Popup */
.fade-enter-active, .fade-leave-active { transition: opacity 0.2s ease, transform 0.2s ease; }
.fade-enter-from, .fade-leave-to { opacity: 0; transform: translateY(-5px); }

/* Ghi đè CSS thuần để cứu Layout bị đen xì do thiếu Tailwind */
.app-shell {
  display: flex;
  flex-direction: column;
  height: 100vh;
  overflow: hidden;
  background-color: #0d1117;
  color: #c9d1d9;
}
header {
  height: 56px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 16px;
  background-color: #161b22;
  border-bottom: 1px solid #30363d;
  flex-shrink: 0;
}
main {
  display: flex;
  flex: 1;
  overflow: hidden;
  position: relative;
}
.pdf-panel {
  display: flex;
  flex-direction: column;
  position: relative;
  height: 100%;
}
.chat-panel {
  flex: 1;
  display: flex;
  flex-direction: column;
  background-color: #161b22;
  border-left: 1px solid #30363d;
  min-width: 300px;
}
.resizer {
  width: 6px;
  background-color: #000;
  cursor: col-resize;
  z-index: 20;
  transition: background-color 0.2s;
}
.resizer:hover {
  background-color: #5b8dee;
}
</style>