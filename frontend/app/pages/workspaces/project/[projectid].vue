<template>
  <div v-if="!isAuthenticated || accessDenied" class="min-h-screen flex items-center justify-center bg-[#0d1117]">
    <div class="text-center bg-[#161b22] p-8 rounded-2xl shadow-xl border border-[#30363d] max-w-md w-full mx-4">
      <div class="w-16 h-16 bg-red-900/30 text-red-400 rounded-full flex items-center justify-center mx-auto mb-4">
        <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" /></svg>
      </div>
      <h1 class="text-2xl font-bold text-white mb-2">Không khả dụng</h1>
      <p class="text-gray-400 mb-6">Bạn không có quyền truy cập vào Dự án này.</p>
      <div class="flex gap-3">
        <button @click="$router.push('/workspaces')" class="flex-1 px-4 py-2.5 bg-[#21262d] text-gray-300 rounded-lg font-bold hover:bg-[#30363d] transition-colors">Trang chủ</button>
        <button v-if="!isAuthenticated" @click="$router.push('/login')" class="flex-1 px-4 py-2.5 bg-blue-600 text-white rounded-lg font-bold hover:bg-blue-700 transition-colors">Đăng nhập</button>
      </div>
    </div>
  </div>

  <div v-else
    class="min-h-screen bg-transparent text-[#c9d1d9]"
    @dragover.prevent="isDragging = true"
    @dragleave.prevent="isDragging = false"
    @drop.prevent="handleDrop"
  >
    <!-- DROP OVERLAY -->
    <div v-if="isDragging" class="fixed inset-0 bg-blue-600/90 z-50 flex flex-col items-center justify-center text-white backdrop-blur-sm">
      <svg class="w-20 h-20 mb-4 animate-bounce" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
      </svg>
      <p class="text-3xl font-bold">Thả file vào đây</p>
      <p class="text-lg mt-2 opacity-80">Trợ lý AI sẽ nhận diện ngay!</p>
    </div>

<main class="max-w-[1400px] mx-auto px-6 py-6 bg-transparent">

  <!-- HEADER -->
  <div class="flex items-center justify-between mb-6">
    <div class="flex items-center gap-3">
      <button
        @click="$router.back()"
        class="p-2 rounded-full bg-gray-200 dark:bg-[#161b22]
               hover:bg-gray-300 dark:hover:bg-[#30363d]
               text-gray-700 dark:text-gray-400
               transition-all border border-gray-300 dark:border-[#30363d]"
      >
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2"
            d="M10 19l-7-7m0 0l7-7m-7 7h18"/>
        </svg>
      </button>

      <div>
        <div class="flex items-center gap-2">
          <h1 class="text-2xl font-bold text-black dark:text-white">
            {{ projectName || 'Chi tiết Dự án' }}
          </h1>

          <button
            v-if="isAdmin"
            @click="showDeleteProjectConfirm = true"
            class="p-1.5 text-gray-500 hover:text-red-400
                   hover:bg-red-900/20 rounded-lg transition-all"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round"
                stroke-width="2"
                d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862
                a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4
                a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
            </svg>
          </button>
        </div>

        <p class="text-sm text-gray-500">
          {{ files.length }} tài liệu
        </p>
      </div>
    </div>

    <!-- Link sang trang từ vựng -->
    <NuxtLink
      :to="`/workspaces/project/${projectId}/vocabularies`"
      class="flex items-center gap-2 px-4 py-2 bg-white dark:bg-[#161b22] border border-gray-200 dark:border-[#30363d] rounded-xl text-sm font-medium text-gray-700 dark:text-gray-300 hover:border-blue-500 hover:text-blue-600 dark:hover:text-blue-400 transition-all"
    >
      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.75 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"/>
      </svg>
      Từ vựng dự án
    </NuxtLink>

    <input
      type="file"
      ref="fileInput"
      @change="handleFileChange"
      accept="image/*,application/pdf"
      class="hidden"
    />
  </div>

  <!-- GRID -->
  <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-5">

    <!-- CARD ADD FILE -->
    <div
      @click="$refs.fileInput.click()"
      class="group cursor-pointer overflow-hidden
             border-2 border-dashed border-gray-300 dark:border-[#30363d]
             bg-white dark:bg-[#161b22]
             hover:border-blue-500 hover:shadow-lg
             transition-all duration-200"
    >
      <div class="aspect-[3/4] flex flex-col items-center justify-center
                  bg-gray-50 dark:bg-[#0d1117]">

        <div class="w-16 h-16
                    bg-blue-100 dark:bg-blue-500/10
                    flex items-center justify-center
                    group-hover:scale-110 transition-transform">
          <svg
            class="w-8 h-8 text-blue-600 dark:text-blue-400"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="2.5"
                  d="M12 4v16m8-8H4"/>
          </svg>
        </div>

        <p class="mt-5 text-base font-semibold text-gray-700 dark:text-gray-200">
          {{ uploading ? 'Đang tải...' : 'Tài liệu mới' }}
        </p>

        <!-- <p class="text-sm text-gray-500 mt-1">
          Nhấn để tải file lên
        </p> -->

        <div
          v-if="uploading"
          class="mt-4 w-6 h-6 border-2 border-blue-400/30
                 border-t-blue-500 rounded-full animate-spin"
        ></div>
      </div>
    </div>

    <!-- FILE CARD -->
    <div
      v-for="file in files"
      :key="file.id"
      @click="openFile(file)"
      class="group relative cursor-pointer flex flex-col
      
             bg-white dark:bg-[#161b22]
            overflow-hidden
             border border-gray-300 dark:border-[#30363d]
             hover:border-blue-500
             transition-all duration-200
             hover:shadow-xl"
    >

      <!-- THUMB -->
      <div class="relative aspect-[3/4] overflow-hidden bg-gray-100 dark:bg-[#0d1117]">

        <img
          v-if="thumbnails[file.id]"
          :src="thumbnails[file.id]"
          :alt="file.name"
          class="w-full h-full object-cover object-top
                 group-hover:scale-[1.02] transition-transform duration-300"
          draggable="false"
        />

        <!-- LOADING -->
        <div
          v-else-if="thumbnailLoading[file.id]"
          class="w-full h-full flex items-center justify-center"
        >
          <div class="w-7 h-7 border-2 border-gray-400/30
                      border-t-blue-500 rounded-full animate-spin">
          </div>
        </div>

        <!-- EMPTY -->
        <div
          v-else
          class="w-full h-full flex flex-col items-center justify-center"
        >
          <svg
            v-if="file.type === 'pdf'"
            class="w-14 h-14 text-red-400 opacity-70"
            fill="currentColor"
            viewBox="0 0 24 24"
          >
            <path d="M4 5a2 2 0 012-2h8.586a1 1 0 01.707.293
                     l4.414 4.414a1 1 0 01.293.707V19a2 2
                     0 01-2 2H6a2 2 0 01-2-2V5z"/>
          </svg>

          <svg
            v-else
            class="w-14 h-14 text-blue-400 opacity-70"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path stroke-linecap="round"
                  stroke-linejoin="round"
                  stroke-width="1.5"
                  d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16"/>
          </svg>

          <span class="mt-3 text-xs uppercase tracking-widest text-gray-400">
            {{ file.type }}
          </span>
        </div>

        <!-- TYPE -->
        <div class="absolute bottom-2 left-2">
          <span class="px-2 py-1 rounded-lg text-[10px]
                       font-bold uppercase
                       bg-black/60 text-white backdrop-blur-sm">
            {{ file.type }}
          </span>
        </div>
      </div>

      <!-- INFO -->
      <div class="p-4 border-t border-gray-200 dark:border-[#30363d]">

        <div class="flex items-center gap-2">
          <div
            class="w-2 h-2 rounded-full"
            :class="file.type === 'pdf'
              ? 'bg-red-400'
              : 'bg-blue-400'"
          ></div>

          <p
            class="text-sm font-semibold truncate
                   text-gray-800 dark:text-gray-200"
            :title="file.name"
          >
            {{ file.name }}
          </p>
        </div>

        <p class="text-xs text-gray-500 mt-2">
          {{ formatDate(file.createdAt) }}
        </p>
      </div>

      <!-- ACTIONS -->
      <div
        class="absolute top-2 right-2 flex flex-col gap-2
               opacity-0 group-hover:opacity-100
               transition-opacity"
      >
        <button
          v-if="isAdmin"
          @click.stop="promptRenameFile(file)"
          class="w-8 h-8 rounded-xl
                 bg-white/90 dark:bg-[#21262d]/90
                 border border-gray-300 dark:border-[#30363d]
                 flex items-center justify-center
                 text-amber-500 hover:bg-amber-500/10"
        >
          ✏️
        </button>

        <button
          v-if="isAdmin"
          @click.stop="promptDeleteFile(file)"
          class="w-8 h-8 rounded-xl
                 bg-white/90 dark:bg-[#21262d]/90
                 border border-gray-300 dark:border-[#30363d]
                 flex items-center justify-center
                 text-red-500 hover:bg-red-500/10"
        >
          🗑️
        </button>
      </div>

    </div>
  </div>
</main>

    <!-- MODAL THÊM TỪ VỰNG -->
    <Transition name="fade">
      <div v-if="showDeleteProjectConfirm" class="fixed inset-0 z-[9999] flex items-center justify-center bg-black/70 backdrop-blur-md px-4">
        <div class="bg-[#161b22] border border-red-500/30 p-8 rounded-2xl shadow-2xl w-full max-w-md text-center">
          <div class="w-16 h-16 bg-red-500/10 text-red-500 rounded-2xl flex items-center justify-center mx-auto mb-5">
            <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" /></svg>
          </div>
          <h3 class="text-xl font-black text-white mb-2">Xóa vĩnh viễn dự án?</h3>
          <p class="text-gray-400 text-sm leading-relaxed mb-6">Bạn đang thực hiện xóa <b class="text-white">{{ projectName }}</b>. Tất cả dữ liệu dự án sẽ biến mất vĩnh viễn.</p>
          <div class="flex gap-3">
            <button @click="showDeleteProjectConfirm = false" class="flex-1 px-5 py-2.5 bg-[#21262d] text-white rounded-xl hover:bg-[#30363d] transition-all">Hủy bỏ</button>
            <button @click="handleDeleteProject" :disabled="isDeleting" class="flex-1 px-5 py-2.5 bg-red-600 text-white font-bold rounded-xl hover:bg-red-700 flex items-center justify-center gap-2">
              <span v-if="isDeleting" class="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></span>
              Xác nhận xóa
            </button>
          </div>
        </div>
      </div>
    </Transition>

    <!-- MODAL XÁC NHẬN XÓA FILE -->
    <Transition name="fade">
      <div v-if="showDeleteFileConfirm" class="fixed inset-0 z-[9999] flex items-center justify-center bg-black/70 backdrop-blur-md px-4">
        <div class="bg-[#161b22] border border-red-500/30 p-8 rounded-2xl shadow-2xl w-full max-w-md text-center">
          <div class="w-16 h-16 bg-red-500/10 text-red-500 rounded-2xl flex items-center justify-center mx-auto mb-5">
            <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/></svg>
          </div>
          <h3 class="text-xl font-black text-white mb-2">Xóa tài liệu này?</h3>
          <p class="text-gray-400 text-sm leading-relaxed mb-6">Bạn có chắc muốn xóa<br/><b class="text-white">{{ fileToDelete?.name }}</b>?</p>
          <div class="flex gap-3">
            <button @click="showDeleteFileConfirm = false" class="flex-1 px-5 py-2.5 bg-[#21262d] text-white rounded-xl hover:bg-[#30363d] transition-all">Hủy</button>
            <button @click="handleDeleteFile" :disabled="isDeletingFile" class="flex-1 px-5 py-2.5 bg-red-600 text-white font-bold rounded-xl hover:bg-red-700 flex items-center justify-center gap-2">
              <span v-if="isDeletingFile" class="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></span>
              Xóa ngay
            </button>
          </div>
        </div>
      </div>
    </Transition>

    <!-- MODAL ĐỔI TÊN FILE -->
    <Transition name="fade">
      <div v-if="showRenameModal" class="fixed inset-0 z-[9999] flex items-center justify-center bg-black/70 backdrop-blur-md px-4">
        <div class="bg-[#161b22] border border-[#30363d] p-8 rounded-2xl shadow-2xl w-full max-w-md">
          <h3 class="text-lg font-bold text-white mb-4">Đổi tên tài liệu</h3>
          <div class="mb-6">
            <label class="block text-sm text-gray-400 mb-2">Tên file mới</label>
            <input v-model="newFileName" type="text"
              class="w-full bg-[#0d1117] border border-[#30363d] text-white rounded-xl px-4 py-3 focus:outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 transition-all"
              placeholder="Nhập tên tài liệu..."
              @keyup.enter="handleRenameFile">
          </div>
          <div class="flex gap-3">
            <button @click="showRenameModal = false" class="flex-1 px-5 py-2.5 bg-[#21262d] text-white rounded-xl hover:bg-[#30363d] transition-all">Hủy</button>
            <button @click="handleRenameFile" :disabled="!newFileName || isRenaming"
              class="flex-1 px-5 py-2.5 bg-blue-600 text-white font-bold rounded-xl hover:bg-blue-700 disabled:opacity-50 flex items-center justify-center gap-2">
              <span v-if="isRenaming" class="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></span>
              Lưu thay đổi
            </button>
          </div>
        </div>
      </div>
    </Transition>

    <!-- TOAST -->
    <Transition name="toast-fade">
      <div v-if="toast.visible" class="fixed bottom-6 left-1/2 -translate-x-1/2 z-[10000] pointer-events-none">
        <div class="bg-[#161b22] border px-5 py-3 rounded-xl shadow-2xl flex items-center gap-3"
          :class="toast.type === 'error' ? 'border-red-500/50' : 'border-green-500/50'">
          <div class="w-8 h-8 rounded-full flex items-center justify-center shrink-0"
            :class="toast.type === 'error' ? 'bg-red-500/10 text-red-400' : 'bg-green-500/10 text-green-400'">
            <svg v-if="toast.type === 'error'" class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>
            <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/></svg>
          </div>
          <p class="text-white text-sm font-medium">{{ toast.message }}</p>
        </div>
      </div>
    </Transition>
  </div>
</template>

<script setup>
definePageMeta({ layout: 'default', ssr: false })

import { ref, reactive, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useJwt } from '~/composables/useJwt'

// Import pdfjs-dist (đã có sẵn trong project)
import * as pdfjsLib from 'pdfjs-dist/legacy/build/pdf'
import workerUrl from 'pdfjs-dist/build/pdf.worker.min.mjs?url'
pdfjsLib.GlobalWorkerOptions.workerSrc = workerUrl

const route = useRoute()
const router = useRouter()
const config = useRuntimeConfig()
const { userId: currentUserId, isAuthenticated } = useJwt()

// --- STATES ---
const projectId = route.params.projectid
const projectName = ref('')
const files = ref([])
const isLoading = ref(true)
const uploading = ref(false)
const isDragging = ref(false)
const pendingFile = ref(null)
const accessDenied = ref(false)

// Thumbnail state: { [fileId]: dataURL | null }
const thumbnails = ref({})
// Thumbnail loading state: { [fileId]: true/false }
const thumbnailLoading = ref({})

// States cho Xóa Project
const showDeleteProjectConfirm = ref(false)
const isDeleting = ref(false)

// States cho Xóa File
const showDeleteFileConfirm = ref(false)
const fileToDelete = ref(null)
const isDeletingFile = ref(false)
const isAdmin = ref(false)

// States cho Đổi Tên File
const showRenameModal = ref(false)
const fileToRename = ref(null)
const newFileName = ref('')
const isRenaming = ref(false)

// State cho Toast UI
const toast = reactive({ visible: false, message: '', type: 'success' })

// --- THUMBNAIL GENERATION ---

/**
 * Render trang đầu của PDF thành base64 dataURL bằng pdfjs-dist
 * @param {string} pdfUrl - URL của file PDF
 * @returns {Promise<string|null>} dataURL hoặc null nếu lỗi
 */
async function generatePdfThumbnail(pdfUrl) {
  try {
    const pdf = await pdfjsLib.getDocument({ url: pdfUrl, withCredentials: false }).promise
    const page = await pdf.getPage(1)

    // Scale để thumbnail đủ nét, không cần quá lớn
    const THUMB_WIDTH = 400
    const viewport = page.getViewport({ scale: 1 })
    const scale = THUMB_WIDTH / viewport.width
    const scaledViewport = page.getViewport({ scale })

    const canvas = document.createElement('canvas')
    canvas.width = scaledViewport.width
    canvas.height = scaledViewport.height

    const ctx = canvas.getContext('2d')
    ctx.fillStyle = '#ffffff'
    ctx.fillRect(0, 0, canvas.width, canvas.height)

    await page.render({ canvasContext: ctx, viewport: scaledViewport }).promise

    // Giải phóng bộ nhớ
    pdf.destroy()

    return canvas.toDataURL('image/jpeg', 0.85)
  } catch (err) {
    console.warn('Không thể tạo thumbnail:', err)
    return null
  }
}

/**
 * Tạo thumbnail cho tất cả files sau khi load xong
 * Xử lý tuần tự để không overload browser
 */
async function generateThumbnailsForFiles(fileList) {
  for (const file of fileList) {
    // Bỏ qua nếu đã có thumbnail hoặc không có URL
    if (thumbnails.value[file.id]) continue

    const url = file.imageUrl || file.fileUrl || file.url
    console.log('file item:', file)
console.log('thumbnail url:', file.imageUrl || file.fileUrl || file.url)
    if (!url) continue

    const isPdf = file.type === 'pdf' || url.toLowerCase().includes('.pdf')
    const isImage = !isPdf && (
      file.type === 'image' ||
      /\.(jpg|jpeg|png|webp|gif|bmp)(\?|$)/i.test(url)
    )

    if (isPdf) {
      // Đánh dấu đang loading
      thumbnailLoading.value = { ...thumbnailLoading.value, [file.id]: true }
      const dataUrl = await generatePdfThumbnail(url)
      thumbnailLoading.value = { ...thumbnailLoading.value, [file.id]: false }
      if (dataUrl) {
        thumbnails.value = { ...thumbnails.value, [file.id]: dataUrl }
      }
    } else if (isImage) {
      // File ảnh thì dùng trực tiếp làm thumbnail
      thumbnails.value = { ...thumbnails.value, [file.id]: url }
    }
  }
}

// --- FUNCTIONS ---

function showToast(message, type = 'success') {
  toast.message = message
  toast.type = type
  toast.visible = true
  setTimeout(() => { toast.visible = false }, 3000)
}

const getToken = () => localStorage.getItem('jwt_token') || ''

async function checkAccessAndLoad() {
  const token = getToken()
  if (!token || !isAuthenticated.value) {
    accessDenied.value = true
    isLoading.value = false
    return
  }

  isLoading.value = true
  try {
    const projRes = await fetch(`${config.public.apiBaseUrl}/api/projects/${projectId}`, {
      headers: { Authorization: `Bearer ${token}` }
    })
    if (!projRes.ok) throw new Error('Dự án không tồn tại')
    const projData = await projRes.json()
    const project = projData.result || projData

    projectName.value = project.name
    const wsId = project.workspaceId

    const membersRes = await fetch(`${config.public.apiBaseUrl}/api/workspaces/${wsId}/members`, {
      headers: { Authorization: `Bearer ${token}` }
    })
    if (!membersRes.ok) throw new Error('Không có quyền truy cập WS')
    const membersData = await membersRes.json()
    const members = membersData.result || membersData

    const currentMember = members.find(m => m.userId === currentUserId.value)
    if (!currentMember) {
      accessDenied.value = true
      return
    }

    isAdmin.value = currentMember.role === 'Admin'

    await fetchFiles(token)

  } catch (e) {
    console.error(e)
    accessDenied.value = true
  } finally {
    isLoading.value = false
  }
}

async function fetchFiles(token) {
  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/projects/${projectId}/files`, {
      headers: { Authorization: `Bearer ${token}` }
    })
    if (res.ok) {
      const data = await res.json()
      files.value = data.result || data
      if (files.value.length > 0 && !projectName.value) {
        projectName.value = files.value[0].projectName
      }
      // Tạo thumbnail ngay sau khi có danh sách file
      generateThumbnailsForFiles(files.value)
    }
  } catch (e) {
    console.error("Lỗi lấy file:", e)
  }
}

// --- VOCABULARY FUNCTIONS ---

async function handleDeleteProject() {
  const token = getToken()
  if (!token) return
  isDeleting.value = true
  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/projects/${projectId}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` }
    })
    if (res.ok) {
      showToast("Đã xóa dự án thành công!")
      setTimeout(() => { router.push('/projects') }, 1000)
    } else {
      const err = await res.json()
      showToast(err.message || "Không có quyền xóa dự án này", "error")
    }
  } catch (e) {
    showToast("Lỗi kết nối máy chủ", "error")
  } finally {
    isDeleting.value = false
    showDeleteProjectConfirm.value = false
  }
}

function promptRenameFile(file) {
  fileToRename.value = file
  newFileName.value = file.name
  showRenameModal.value = true
}

async function handleRenameFile() {
  const token = getToken()
  if (!token || !newFileName.value.trim()) return
  isRenaming.value = true
  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/projects/${projectId}/files/${fileToRename.value.id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      body: JSON.stringify({ fileName: newFileName.value.trim() })
    })
    if (res.ok) {
      showToast("Đã đổi tên file thành công!")
      await fetchFiles(token)
      showRenameModal.value = false
    } else {
      const err = await res.json()
      showToast(err.message || "Bạn không có quyền sửa file này", "error")
    }
  } catch (e) {
    showToast("Lỗi kết nối máy chủ", "error")
  } finally {
    isRenaming.value = false
  }
}

function promptDeleteFile(file) {
  fileToDelete.value = file
  showDeleteFileConfirm.value = true
}

async function handleDeleteFile() {
  const token = getToken()
  if (!token) return
  isDeletingFile.value = true
  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/projects/${projectId}/files/${fileToDelete.value.id}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` }
    })
    if (res.ok) {
      showToast("Đã xóa file thành công!")
      await fetchFiles(token)
      showDeleteFileConfirm.value = false
    } else {
      const err = await res.json()
      showToast(err.message || "Bạn không có quyền xóa file này", "error")
    }
  } catch (e) {
    showToast("Lỗi kết nối máy chủ", "error")
  } finally {
    isDeletingFile.value = false
  }
}

async function handleFileUpload(pickedFile) {
  if (!pickedFile || uploading.value) return
  const token = getToken()
  if (!token) { showToast("Vui lòng đăng nhập", "error"); return }

  uploading.value = true
  const ext = pickedFile.name.split('.').pop()?.toLowerCase() || ''
  pendingFile.value = { name: pickedFile.name, type: ext }

  const formData = new FormData()
  formData.append('image', pickedFile)
  formData.append('projectId', projectId)

  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/Infer/upload-and-infer?saveAnnotated=false`, {
      method: 'POST', body: formData, headers: { Authorization: `Bearer ${token}` }
    })
    if (!res.ok) throw new Error(await res.text() || "Lỗi upload")
    showToast("Tải lên thành công!")
    await fetchFiles(token)
  } catch (err) {
    showToast(err.message, "error")
  } finally {
    uploading.value = false
    pendingFile.value = null
  }
}

function handleFileChange(e) { if (e.target.files[0]) handleFileUpload(e.target.files[0]); e.target.value = '' }

function handleDrop(e) {
  isDragging.value = false
  const f = e.dataTransfer.files[0]
  if (f) handleFileUpload(f)
}

function openFile(file) {
  const name = encodeURIComponent(file.name || '')
  router.push(`/reader?jobId=${file.id}&projectId=${projectId}&name=${name}`)
}

// UI HELPERS
const statusClass = (s) => {
  const map = {
    completed: 'bg-green-900/80 text-green-300',
    processing: 'bg-yellow-900/80 text-yellow-300',
    failed: 'bg-red-900/80 text-red-300'
  }
  return map[s?.toLowerCase()] || 'bg-gray-800/80 text-gray-400'
}

const statusText = (s) => {
  const map = { completed: 'Hoàn thành', processing: 'Đang quét', failed: 'Lỗi xử lý' }
  return map[s?.toLowerCase()] || 'Chờ xử lý'
}

const formatDate = (dateString) => {
  if (!dateString) return ''
  const utcString = dateString.endsWith('Z') ? dateString : `${dateString}Z`
  const date = new Date(utcString)
  return date.toLocaleString('vi-VN', {
    hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit', year: 'numeric'
  })
}

onMounted(() => {
  checkAccessAndLoad()
})
</script>

<style scoped>
.fade-enter-active, .fade-leave-active { transition: opacity 0.2s ease; }
.fade-enter-from, .fade-leave-to { opacity: 0; }

.toast-fade-enter-active { transition: all 0.35s cubic-bezier(0.175, 0.885, 0.32, 1.275); }
.toast-fade-leave-active { transition: all 0.25s ease; }
.toast-fade-enter-from { opacity: 0; transform: translateX(-50%) translateY(16px) scale(0.9); }
.toast-fade-leave-to { opacity: 0; transform: translateX(-50%) scale(0.95); }
</style>