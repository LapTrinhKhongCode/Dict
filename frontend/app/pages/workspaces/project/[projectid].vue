<template>
  <div v-if="!isAuthenticated || accessDenied" class="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900 transition-colors">
    <div class="text-center bg-white dark:bg-gray-800 p-8 rounded-2xl shadow-xl border border-gray-200 dark:border-gray-700 max-w-md w-full mx-4">
      <div class="w-16 h-16 bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400 rounded-full flex items-center justify-center mx-auto mb-4">
        <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" /></svg>
      </div>
      <h1 class="text-2xl font-bold text-gray-900 dark:text-white mb-2">Không khả dụng</h1>
      <p class="text-gray-500 dark:text-gray-400 mb-6">
        Bạn Không có quyền truy cập vào Dự án này.
      </p>
      <div class="flex gap-3">
        <button @click="$router.push('/workspaces')" class="flex-1 px-4 py-2.5 bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg font-bold hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors shadow-sm">
          Trang chủ
        </button>
        <button v-if="!isAuthenticated" @click="$router.push('/login')" class="flex-1 px-4 py-2.5 bg-blue-600 text-white rounded-lg font-bold hover:bg-blue-700 transition-colors shadow-sm">
          Đăng nhập
        </button>
      </div>
    </div>
  </div>

  <div v-else
    class="bg-gray-50 dark:bg-gray-900 transition-colors min-h-screen"
    @dragover.prevent="isDragging = true"
    @dragleave.prevent="isDragging = false"
    @drop.prevent="handleDrop"
  >
    <div v-if="isDragging"
      class="fixed inset-0 bg-blue-600/90 z-50 flex flex-col items-center justify-center text-white backdrop-blur-sm">
      <svg class="w-24 h-24 mb-6 animate-bounce" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
          d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
      </svg>
      <p class="text-3xl font-bold">Thả file vào đây</p>
      <p class="text-xl mt-2 opacity-80">Trợ lý AI sẽ nhận diện ngay!</p>
    </div>

  <main class="max-w-[1400px] mx-auto p-6 md:p-8">
  <div class="flex items-center justify-between mb-8">
    <div class="flex items-center gap-4">
      <button @click="$router.back()"
        class="p-2.5 rounded-full bg-white dark:bg-gray-800 shadow-sm hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-500 transition-all border border-gray-100 dark:border-gray-700">
        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18"/>
        </svg>
      </button>
      <div>
        <div class="flex items-center gap-3">
          <h1 class="text-2xl font-extrabold text-gray-900 dark:text-white tracking-tight">
            {{ projectName || 'Chi tiết Dự án' }}
          </h1>
          <button @click="showDeleteConfirm = true" 
            class="p-1.5 text-gray-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg transition-all"
            title="Xóa toàn bộ dự án">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
            </svg>
          </button>
        </div>
        <p class="text-sm text-gray-500 dark:text-gray-400 font-medium">{{ files.length }} tài liệu hiện có</p>
      </div>
    </div>

    <div class="flex items-center gap-3">
      <button @click="$refs.fileInput.click()" :disabled="uploading"
        class="px-6 py-2.5 bg-blue-600 text-white rounded-xl font-bold text-sm hover:bg-blue-700 disabled:opacity-60 transition-all flex items-center gap-2 shadow-lg shadow-blue-600/20">
        <svg v-if="uploading" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"/>
        </svg>
        <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M12 4v16m8-8H4"/>
        </svg>
        {{ uploading ? 'Đang xử lý...' : 'Tải lên tài liệu' }}
      </button>
      <input type="file" ref="fileInput" @change="handleFileChange" accept="image/*,application/pdf" class="hidden" />
    </div>
  </div>

  <div v-if="isLoading" class="space-y-4">
    <div v-for="i in 3" :key="i" class="h-20 bg-gray-100 dark:bg-gray-800 animate-pulse rounded-2xl"></div>
  </div>

  <div v-else class="bg-white dark:bg-gray-800 shadow-2xl rounded-3xl overflow-hidden border border-gray-100 dark:border-gray-700">
    <div class="overflow-x-auto">
      <table class="w-full text-sm text-left">
        <thead class="text-xs text-gray-400 uppercase bg-gray-50/50 dark:bg-gray-700/50 border-b">
          <tr>
            <th class="px-8 py-5 font-bold">Tài liệu</th>
            <th class="px-6 py-5 font-bold">Định dạng</th>
            <th class="px-6 py-5 font-bold">Trạng thái AI</th>
            <th class="px-6 py-5 font-bold">Ngày tải lên</th>
            <th class="px-8 py-5 font-bold text-center">Mở</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-gray-50 dark:divide-gray-700/50">
          <tr v-for="file in files" :key="file.id" @click="openFile(file)" class="hover:bg-gray-50 dark:hover:bg-gray-700/30 transition-all cursor-pointer group">
            <td class="px-8 py-5">
              <div class="flex items-center gap-4">
                <div class="w-10 h-10 rounded-xl flex items-center justify-center transition-transform group-hover:scale-110"
                  :class="file.type === 'pdf' ? 'bg-red-50 dark:bg-red-900/20 text-red-500' : 'bg-blue-50 dark:bg-blue-900/20 text-blue-500'">
                  <svg v-if="file.type === 'pdf'" class="w-6 h-6" fill="currentColor" viewBox="0 0 24 24">
                    <path d="M4 5a2 2 0 012-2h8.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V19a2 2 0 01-2 2H6a2 2 0 01-2-2V5zm10 1V4.5l3.5 3.5H16a2 2 0 01-2-2z"/>
                  </svg>
                  <svg v-else class="w-6 h-6" fill="currentColor" viewBox="0 0 24 24">
                     <path d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round"/>
                  </svg>
                </div>
                <span class="font-bold text-gray-700 dark:text-gray-200">{{ file.name }}</span>
              </div>
            </td>
            <td class="px-6 py-5 uppercase font-bold text-xs text-gray-400">{{ file.type }}</td>
            <td class="px-6 py-5">
              <span :class="statusClass(file.status)" class="px-3 py-1 rounded-full text-[8px] font-black uppercase">
                {{ statusText(file.status) }}
              </span>
            </td>
            <td class="px-6 py-5 text-gray-500">{{ formatDate(file.createdAt) }}</td>
            <td class="px-8 py-5 text-center">
               <button @click.stop="openFile(file)" class="p-2.5 rounded-xl hover:bg-blue-50 dark:hover:bg-blue-900/30 text-blue-600 transition-all">
                  <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/>
                  </svg>
               </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</main>

    <Transition name="fade">
      <div v-if="showDeleteConfirm" class="fixed inset-0 z-[9999] flex items-center justify-center bg-black/60 backdrop-blur-md px-4">
        <div class="bg-[#1c2128] border border-red-500/30 p-8 rounded-[2rem] shadow-2xl w-full max-w-md text-center">
          <div class="w-20 h-20 bg-red-500/10 text-red-500 rounded-3xl flex items-center justify-center mx-auto mb-6">
            <svg class="w-10 h-10" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" /></svg>
          </div>
          <h3 class="text-2xl font-black text-white mb-3">Xóa vĩnh viễn dự án?</h3>
          <p class="text-gray-400 text-sm leading-relaxed mb-8">Bạn đang thực hiện xóa <b>{{ projectName }}</b>. Tất cả dữ liệu dự án sẽ biến mất vĩnh viễn.</p>
          <div class="flex gap-4">
            <button @click="showDeleteConfirm = false" class="flex-1 px-6 py-3 bg-gray-800 text-white rounded-2xl hover:bg-gray-700 transition-all">Hủy bỏ</button>
            <button @click="handleDeleteProject" :disabled="isDeleting" class="flex-1 px-6 py-3 bg-red-600 text-white font-black rounded-2xl hover:bg-red-700 flex items-center justify-center gap-2">
              <span v-if="isDeleting" class="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></span>
              Xác nhận xóa
            </button>
          </div>
        </div>
      </div>
    </Transition>

    <Transition name="toast-fade">
      <div v-if="toast.visible" class="fixed inset-0 z-[10000] flex items-center justify-center pointer-events-none px-4">
        <div class="bg-[#1c2128] border p-5 rounded-2xl shadow-2xl flex flex-col items-center max-w-sm text-center transform transition-all pointer-events-auto"
          :class="toast.type === 'error' ? 'border-red-500/50' : 'border-blue-500/50'">
          <div class="w-12 h-12 rounded-full flex items-center justify-center mb-3"
            :class="toast.type === 'error' ? 'bg-red-500/10 text-red-500' : 'bg-blue-500/10 text-blue-500'">
            <svg v-if="toast.type === 'error'" class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>
            <svg v-else class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/></svg>
          </div>
          <p class="text-white font-bold line-clamp-5 break-words">{{ toast.message }}</p>
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

// States cho Xóa Project
const showDeleteConfirm = ref(false)
const isDeleting = ref(false)

// State cho Toast UI
const toast = reactive({
  visible: false,
  message: '',
  type: 'success'
})

// --- FUNCTIONS ---

function showToast(message, type = 'success') {
  toast.message = message
  toast.type = type
  toast.visible = true
  setTimeout(() => { toast.visible = false }, 3000)
}

const getToken = () => localStorage.getItem('jwt_token') || ''

// KIỂM TRA QUYỀN TRUY CẬP TRƯỚC KHI LOAD
async function checkAccessAndLoad() {
  const token = getToken()
  if (!token || !isAuthenticated.value) {
    accessDenied.value = true
    isLoading.value = false
    return
  }

  isLoading.value = true
  try {
    // 1. Lấy thông tin Project để biết nó thuộc Workspace nào
    const projRes = await fetch(`${config.public.apiBaseUrl}/api/projects/${projectId}`, {
      headers: { Authorization: `Bearer ${token}` }
    })
    
    if (!projRes.ok) throw new Error('Dự án không tồn tại')
    const projData = await projRes.json()
    const project = projData.result || projData
    
    projectName.value = project.name
    const wsId = project.workspaceId

    // 2. Lấy danh sách thành viên của Workspace
    const membersRes = await fetch(`${config.public.apiBaseUrl}/api/workspaces/${wsId}/members`, {
      headers: { Authorization: `Bearer ${token}` }
    })
    
    if (!membersRes.ok) throw new Error('Không có quyền truy cập WS')
    const membersData = await membersRes.json()
    const members = membersData.result || membersData
    
    // 3. Kiểm tra user hiện tại có trong mảng members không
    const isMember = members.some(m => m.userId === currentUserId.value)
    if (!isMember) {
      accessDenied.value = true
      return
    }

    // 4. Đã qua kiểm tra -> Tải danh sách file
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
    }
  } catch (e) {
    console.error("Lỗi lấy file:", e)
  }
}

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
      const err = await res.text()
      showToast(err || "Không có quyền xóa dự án này", "error")
    }
  } catch (e) {
    showToast("Lỗi kết nối máy chủ", "error")
  } finally {
    isDeleting.value = false
    showDeleteConfirm.value = false
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
    completed: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300',
    processing: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/50 dark:text-yellow-300',
    failed: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300'
  }
  return map[s?.toLowerCase()] || 'bg-gray-100 text-gray-800'
}

const statusText = (s) => {
  const map = { completed: 'Hoàn thành', processing: 'Đang quét', failed: 'Hoàn thành' }
  return map[s?.toLowerCase()] || 'Chờ xử lý'
}

const formatDate = (d) => d ? new Date(d).toLocaleDateString('vi-VN') + ' ' + new Date(d).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }) : ''

onMounted(() => {
  checkAccessAndLoad()
})
</script>

<style scoped>
.fade-enter-active, .fade-leave-active { transition: opacity 0.3s ease; }
.fade-enter-from, .fade-leave-to { opacity: 0; }

.toast-fade-enter-active { transition: all 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275); }
.toast-fade-leave-active { transition: all 0.3s ease; }
.toast-fade-enter-from { opacity: 0; transform: translateY(20px) scale(0.8); }
.toast-fade-leave-to { opacity: 0; transform: scale(0.9); }

.line-clamp-5 {
  display: -webkit-box;
  -webkit-line-clamp: 5;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
</style>