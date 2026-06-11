<template>
  <div class="app-shell flex flex-col h-screen overflow-hidden bg-[#0d1117] text-[#c9d1d9] font-sans">
    
    <header class="h-14 flex items-center justify-between px-4 border-b border-[#30363d] bg-[#161b22] shrink-0 z-10">
      <button @click="goBack" class="flex items-center gap-2 px-3 py-1.5 bg-[#21262d] hover:bg-[#30363d] rounded-lg border border-[#30363d] text-sm transition">
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="16" height="16"><polyline points="15 18 9 12 15 6"/></svg>
        Quay lại
      </button>
      <!-- Tên tài liệu — click để đổi tên như Google Docs -->
      <div class="flex items-center gap-1 min-w-0">
        <span
          v-if="!isEditingName"
          @click="startRename"
          class="font-bold cursor-text hover:bg-[#21262d] px-2 py-0.5 rounded transition truncate max-w-[200px]"
          :title="editableName"
        >{{ editableName || 'Tài liệu' }}</span>
        <input
          v-else
          ref="nameInputRef"
          v-model="editableName"
          @blur="saveRename"
          @keydown.enter.prevent="saveRename"
          @keydown.esc.prevent="cancelRename"
          class="font-bold bg-[#0d1117] border border-[#5b8dee] rounded px-2 py-0.5 text-white outline-none text-sm w-[200px]"
        />
        <span v-if="renameSaving" class="w-3 h-3 border-2 border-gray-400 border-t-white rounded-full animate-spin shrink-0"></span>
      </div>
      <!-- Avatars người đang xem realtime -->
      <div class="flex items-center gap-2">
        <TransitionGroup name="viewer-pop" tag="div" class="flex -space-x-2">
          <div
            v-for="v in viewers"
            :key="v.userId"
            class="relative w-9 h-9 rounded-full border-2 border-red-500 shadow-lg overflow-visible cursor-default shrink-0"
            :title="v.userName"
          >
            <div class="w-full h-full rounded-full overflow-hidden">
              <img v-if="v.avatarUrl" :src="v.avatarUrl" :alt="v.userName" class="w-full h-full object-cover" />
              <div v-else class="w-full h-full flex items-center justify-center text-[13px] font-bold text-white uppercase" :style="{ backgroundColor: v.color }">
                {{ v.userName.charAt(0) }}
              </div>
            </div>
          </div>
        </TransitionGroup>
        <span v-if="viewers.length > 0" class="text-[11px] text-red-400 font-semibold ml-1 whitespace-nowrap">
          {{ viewers.length }} đang xem
        </span>
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
          :project-id="projectId"
          @page-changed="handlePageChanged"
          @media-id-loaded="(id) => fileId = id"
          @access-denied="handleAccessDenied"
        />
        <div v-else class="flex flex-col items-center justify-center h-full text-gray-400">
          <div class="w-8 h-8 border-4 border-gray-600 border-t-[#f0c040] rounded-full animate-spin mb-4"></div>
          <p>Đang chờ nạp dữ liệu OCR/PDF...</p>
        </div>
        <!-- Overlay cursor cộng tác — absolute fill, pointer-events none -->
        <CollabCursorOverlay
          v-if="fileId"
          :file-id="Number(fileId)"
          :current-page="pdfCurrentPage"
          @viewers-updated="(v) => viewers = v"
        />
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

        <div class="flex border-b border-[#30363d] bg-[#0d1117] shrink-0">
          <div class="flex-1 p-3 text-sm font-semibold text-[#f0c040] border-b-2 border-[#f0c040] bg-[#21262d] flex items-center justify-center gap-2">
            📝 Thảo luận
          </div>
        </div>

        <div class="flex-1 overflow-hidden">
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
definePageMeta({ layout: 'reader', ssr: false, middleware: 'auth-client' })

import { ref, computed, watch, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useRuntimeConfig } from '#app'
import { useJwt } from '~/composables/useJwt'

import PdfViewer from '~/components/PdfViewer.vue'
import FileCommentTab from '~/components/FileCommentTab.vue'
import CollabCursorOverlay from '~/components/CollabCursorOverlay.vue'

const route = useRoute()
const router = useRouter()
const config = useRuntimeConfig()
const { jwt } = useJwt()

const fileUrl= computed(() => route.query.url || '')
const jobId = computed(() => route.query.jobId ? Number(route.query.jobId) : null)
const projectId = computed(() => route.query.projectId ? Number(route.query.projectId) : null)

const pdfName = computed(() => {
  if (!route.query.name) return 'Tài liệu'
  try { return decodeURIComponent(route.query.name) }
  catch (e) { return route.query.name }
})

// Inline rename
const editableName = ref('')
const isEditingName = ref(false)
const renameSaving = ref(false)
const nameInputRef = ref(null)
let originalName = ''

watch(pdfName, (v) => { if (!isEditingName.value) editableName.value = v }, { immediate: true })

function startRename() {
  if (!jobId.value || !projectId.value) return
  originalName = editableName.value
  isEditingName.value = true
  nextTick(() => nameInputRef.value?.select())
}

function cancelRename() {
  editableName.value = originalName
  isEditingName.value = false
}

async function saveRename() {
  isEditingName.value = false
  const name = editableName.value.trim()
  if (!name || name === originalName || !jobId.value || !projectId.value) return
  renameSaving.value = true
  try {
    await fetch(`${config.public.apiBaseUrl}/api/projects/${projectId.value}/files/${jobId.value}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${jwt.value}` },
      body: JSON.stringify({ fileName: name })
    })
    originalName = name
  } catch {
    editableName.value = originalName
  } finally {
    renameSaving.value = false
  }
}

const pdfData = ref(null) 
const fileId = ref(null) 
const leftPanelWidth = ref(60)
const isAccessDenied = ref(false)
const pdfViewerRef = ref(null)
const pdfCurrentPage = ref(1)
const viewers = ref([])

watch(() => route.query.id, (newId) => {
  if (newId && !fileId.value) {
    fileId.value = Number(newId)
    const raw = sessionStorage.getItem(`pdf_${newId}`)
    if (raw) pdfData.value = new Uint8Array(JSON.parse(raw)) 
  }
}, { immediate: true })

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

function startResize() {
  const move = (e) => { leftPanelWidth.value = Math.max(25, Math.min(75, (e.clientX / window.innerWidth) * 100)) }
  const up = () => { document.removeEventListener('mousemove', move); document.removeEventListener('mouseup', up) }
  document.addEventListener('mousemove', move); document.addEventListener('mouseup', up)
}

const goBack = () => router.back()
</script>

<style scoped>
.app-shell {
  display: flex;
  flex-direction: column;
  height: 100vh;
  overflow: hidden;
  background-color: #0d1117;
  color: #c9d1d9;
}
.viewer-pop-enter-active { transition: all 0.2s ease; }
.viewer-pop-leave-active { transition: all 0.15s ease; }
.viewer-pop-enter-from { opacity: 0; transform: scale(0.5); }
.viewer-pop-leave-to { opacity: 0; transform: scale(0.5); }
</style>