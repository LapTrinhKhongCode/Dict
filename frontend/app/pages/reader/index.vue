<template>
  <div class="app-shell flex flex-col h-screen overflow-hidden bg-[#0d1117] text-[#c9d1d9] font-sans">
    
    <header class="h-14 flex items-center justify-between px-4 border-b border-[#30363d] bg-[#161b22] shrink-0 z-10">
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
          ref="pdfViewerRef"
          :file-url="fileUrl" 
          :file-data="pdfData" 
          :job-id="jobId"
          :api-key="apiKey" 
          @rag-updated="(data) => ragIndex = data"
          @text-selected="handleTextSelection"
          @page-changed="handlePageChanged"
          @media-id-loaded="(id) => fileId = id"
          @access-denied="handleAccessDenied" 
        /> 
        <div v-else class="flex flex-col items-center justify-center h-full text-gray-400">
          <div class="w-8 h-8 border-4 border-gray-600 border-t-[#f0c040] rounded-full animate-spin mb-4"></div>
          <p>Đang chờ nạp dữ liệu OCR/PDF...</p>
        </div>
      </section>

      <div class="w-1.5 bg-[#161b22] border-x border-[#30363d] cursor-col-resize hover:bg-[#5b8dee] transition-colors z-20" @mousedown="startResize"></div>

      <section class="flex-1 flex flex-col bg-[#161b22] relative">
        
        <div v-if="isAccessDenied" class="absolute inset-0 z-50 bg-[#161b22]/90 backdrop-blur-sm flex flex-col items-center justify-center p-6 text-center border-l border-[#30363d]">
          <div class="w-16 h-16 bg-red-900/30 text-red-500 rounded-full flex items-center justify-center mb-4">
            <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z"/></svg>
          </div>
          <h3 class="text-xl font-bold text-white mb-2">Chức năng bị khóa</h3>
          <p class="text-sm text-gray-400">Bạn không có quyền truy cập vào công cụ hỗ trợ của dự án này.</p>
        </div>

        <div class="flex border-b border-[#30363d] bg-[#0d1117] shrink-0 overflow-x-auto custom-scrollbar">
          <button @click="activeTab='chat'" :class="['flex-1 p-3 text-sm font-semibold transition whitespace-nowrap', activeTab==='chat' ? 'text-[#f0c040] border-b-2 border-[#f0c040] bg-[#21262d]' : 'text-gray-500 hover:text-gray-300']">💬 Trợ lý AI</button>
          <button @click="activeTab='vocab'" :class="['flex-1 p-3 text-sm font-semibold transition whitespace-nowrap', activeTab==='vocab' ? 'text-[#f0c040] border-b-2 border-[#f0c040] bg-[#21262d]' : 'text-gray-500 hover:text-gray-300']">📚 Từ vựng</button>
          <button @click="activeTab='comment'" :class="['flex-1 p-3 text-sm font-semibold transition whitespace-nowrap', activeTab==='comment' ? 'text-[#f0c040] border-b-2 border-[#f0c040] bg-[#21262d]' : 'text-gray-500 hover:text-gray-300']">📝 Thảo luận</button>
        </div>

        <div v-show="activeTab === 'chat'" class="flex-1 overflow-hidden">
          <GeminiChat 
            :api-key="apiKey" 
            :pdf-name="pdfName" 
            :rag-index="ragIndex" 
            :accessDenied="isAccessDenied"  
          /> 
        </div>

        <div v-show="activeTab === 'vocab'" class="flex-1 overflow-hidden">
          <VocabManager ref="vocabMgrRef" :project-id="projectId" :pdf-name="pdfName" />
        </div>

        <div v-show="activeTab === 'comment'" class="flex-1 overflow-hidden">
          <FileCommentTab 
            v-if="fileId"
            :file-id="Number(fileId)" 
            :current-page="pdfCurrentPage"
            @jump-to-page="triggerPdfJump"
          />
        </div>
      </section>
    </main>
  </div>
</template>

<script setup>
definePageMeta({ layout: 'reader', ssr: false })

import { ref, computed, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useRuntimeConfig } from '#app'

import PdfViewer from '~/components/PdfViewer.vue'
import GeminiChat from '~/components/GeminiChat.vue'
import VocabManager from '~/components/VocabManager.vue'
import FileCommentTab from '~/components/FileCommentTab.vue'

const route = useRoute()
const router = useRouter()
const config = useRuntimeConfig()

// --- FIX LỖI DÁN LINK: SỬ DỤNG COMPUTED ĐỂ BIẾN LUÔN BÁM SÁT URL ---
const fileUrl = computed(() => route.query.url || '')
const jobId = computed(() => route.query.jobId ? Number(route.query.jobId) : null)
const projectId = computed(() => route.query.projectId ? Number(route.query.projectId) : null)

const pdfName = computed(() => {
  if (!route.query.name) return 'Tài liệu'
  try {
    return decodeURIComponent(route.query.name)
  } catch (e) {
    return route.query.name // Fallback nếu decode lỗi
  }
})

// Các state khác giữ nguyên
const pdfData = ref(null) 
const fileId = ref(null) 
const apiKey = ref('')
const leftPanelWidth = ref(60)

const isAccessDenied = ref(false)

const activeTab = ref('chat')
const ragIndex = ref([])
const vocabMgrRef = ref(null)
const vocabPopup = ref({ visible: false, word: '', meaning: '', x: 0, y: 0, loading: false })

const pdfViewerRef = ref(null)
const pdfCurrentPage = ref(1)

// --- Lắng nghe ID từ URL để khôi phục fileData (nếu có id) ---
watch(() => route.query.id, (newId) => {
  if (newId && !fileId.value) {
    fileId.value = Number(newId)
    // Load local sessionStorage (nếu user đi từ trang project vào)
    const raw = sessionStorage.getItem(`pdf_${newId}`)
    if (raw) pdfData.value = new Uint8Array(JSON.parse(raw)) 
  }
}, { immediate: true })

// --- HÀM BẢO MẬT ---
function handleAccessDenied() {
  isAccessDenied.value = true
}

function handlePageChanged(page) {
  pdfCurrentPage.value = page
}

function triggerPdfJump(pageNum) {
  if (pdfViewerRef.value) {
    pdfViewerRef.value.gotoPage = pageNum
    pdfViewerRef.value.jumpToPage()
  }
}

// --- LOGIC XỬ LÝ SỰ KIỆN TỪ PDF VIEWER ---
function handleTextSelection(data) {
  if (isAccessDenied.value) return; 

  const popupHeight = 220; 
  const popupWidth = 270;  

  let posX = data.x + 10;
  let posY = data.y + 15; 

  if (posY + popupHeight > window.innerHeight) {
    posY = data.y - popupHeight - 15;
  }
  posY = Math.max(10, posY);
  posX = Math.min(posX, window.innerWidth - popupWidth - 10);

  vocabPopup.value = { 
    visible: true, 
    word: data.text, 
    meaning: '', 
    x: posX, 
    y: posY, 
    loading: true 
  }
  
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
})
</script>

<style scoped>
.fade-enter-active, .fade-leave-active { transition: opacity 0.2s ease, transform 0.2s ease; }
.fade-enter-from, .fade-leave-to { opacity: 0; transform: translateY(-5px); }

.app-shell {
  display: flex;
  flex-direction: column;
  height: 100vh;
  overflow: hidden;
  background-color: #0d1117;
  color: #c9d1d9;
}

/* Scrollbar */
.custom-scrollbar::-webkit-scrollbar { height: 4px; }
.custom-scrollbar::-webkit-scrollbar-track { background: transparent; }
.custom-scrollbar::-webkit-scrollbar-thumb { background: #30363d; border-radius: 4px; }
.custom-scrollbar::-webkit-scrollbar-thumb:hover { background: #484f58; }
</style>