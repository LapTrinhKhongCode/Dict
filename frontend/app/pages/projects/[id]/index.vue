<template>
  <div
    class="bg-gray-50 dark:bg-gray-900 transition-colors min-h-full"
    @dragover.prevent="isDragging = true"
    @dragleave.prevent="isDragging = false"
    @drop.prevent="handleDrop"
  >
    <div
      v-if="isDragging"
      class="fixed inset-0 bg-blue-600/90 z-50 flex flex-col items-center justify-center text-white backdrop-blur-sm transition-opacity duration-300"
    >
      <svg class="w-24 h-24 mb-6 animate-bounce" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path>
      </svg>
      <p class="text-3xl font-bold">Thả file PDF hoặc Ảnh vào đây</p>
      <p class="text-xl mt-2 opacity-80">để Trợ lý AI bắt đầu nhận diện ngay lập tức!</p>
    </div>

    <main class="p-6 md:p-8">
      <!-- Header đơn giản không sticky -->
      <div class="flex items-center justify-between mb-6">
        <div class="flex items-center gap-3">
          <button
             @click="$router.push('/projects')"
            class="p-2 rounded-full hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-500"
          >
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18"></path>
            </svg>
          </button>
          <h1 class="text-xl font-bold text-gray-900 dark:text-white">
            {{ projectName || 'Dự án' }}
          </h1>
        </div>
        <div class="flex items-center gap-3">
          <div class="relative">
            <input
              type="text"
              placeholder="Tìm kiếm tài liệu..."
              class="w-64 p-2.5 pl-10 border rounded-full bg-white dark:bg-gray-700 dark:border-gray-600 dark:text-gray-100 outline-none focus:ring-2 focus:ring-blue-300 transition-all text-sm border-gray-200"
            />
            <svg class="absolute left-3.5 top-3 w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"></path>
            </svg>
          </div>
          <button
            @click="$refs.fileInput.click()"
            class="px-5 py-2.5 bg-blue-600 text-white rounded-full font-semibold text-sm hover:bg-blue-700 transition flex items-center gap-2 shadow"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M12 4v16m8-8H4"></path>
            </svg>
            Tải lên tài liệu mới
          </button>
          <input type="file" ref="fileInput" @change="handleFileChange" accept="image/*,application/pdf" class="hidden" />
        </div>
      </div>
      <div v-if="isLoading" class="text-center p-20 text-gray-500 flex flex-col items-center gap-4">
        <div class="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
        <p>Đang tải danh sách tài liệu...</p>
      </div>

      <div v-if="error" class="bg-red-50 border border-red-200 text-red-700 p-4 rounded-lg text-center">
        {{ error }}
      </div>

      <div
        v-if="!isLoading && !files.length"
        class="text-center p-20 py-32 bg-white dark:bg-gray-800 rounded-3xl border border-dashed border-gray-200 dark:border-gray-700 shadow-xl"
      >
        <svg class="mx-auto w-24 h-24 mb-6 text-gray-300 dark:text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path>
        </svg>
        <h3 class="text-2xl font-bold text-gray-800 dark:text-gray-100">Project này chưa có tài liệu nào!</h3>
        <p class="text-gray-500 mt-2">Bấm nút Upload hoặc kéo thả file PDF vào đây để trợ lý AI xử lý.</p>
      </div>

      <div v-if="!isLoading && files.length" class="bg-white dark:bg-gray-800 shadow-xl rounded-2xl overflow-hidden border border-gray-100 dark:border-gray-700">
        <table class="w-full text-sm text-left">
          <thead class="text-xs text-gray-500 uppercase bg-gray-50/50 dark:bg-gray-700/50 border-b dark:border-gray-700">
            <tr>
              <th scope="col" class="px-6 py-4 w-5/12">Tên tài liệu</th>
              <th scope="col" class="px-6 py-4">Loại</th>
              <th scope="col" class="px-6 py-4">Trạng thái AI</th>
              <th scope="col" class="px-6 py-4">Ngày tạo</th>
              <th scope="col" class="px-6 py-4 w-[120px]">Thao tác</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="file in files" :key="file.id"
              class="bg-white dark:bg-gray-800 border-b dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-700/50 cursor-pointer transition"
              @click="$router.push(`/projects/${projectId}/file/${file.id}`)"
            >
              <td class="px-6 py-4 font-medium text-gray-900 dark:text-gray-100 flex items-center gap-3">
                <svg v-if="file.type === 'pdf'" class="w-6 h-6 text-red-500 flex-shrink-0" fill="currentColor" viewBox="0 0 24 24">
                  <path fill-rule="evenodd" d="M4 5a2 2 0 012-2h8.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V19a2 2 0 01-2 2H6a2 2 0 01-2-2V5zm10 1V4.5l3.5 3.5H16a2 2 0 01-2-2z" clip-rule="evenodd"></path>
                </svg>
                <svg v-else class="w-6 h-6 text-blue-500 flex-shrink-0" fill="currentColor" viewBox="0 0 24 24">
                  <path fill-rule="evenodd" d="M4 5a2 2 0 012-2h8.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V19a2 2 0 01-2 2H6a2 2 0 01-2-2V5zm10 1V4.5l3.5 3.5H16a2 2 0 01-2-2z" clip-rule="evenodd"></path>
                </svg>
                <span class="truncate">{{ file.name }}</span>
              </td>
              <td class="px-6 py-4 text-gray-500 dark:text-gray-400 font-mono uppercase text-xs">{{ file.type }}</td>
              <td class="px-6 py-4">
                <span :class="getStatusBadgeClass(file.status)" class="text-xs font-semibold px-2.5 py-1 rounded-full whitespace-nowrap">
                  {{ formatStatus(file.status) }}
                </span>
              </td>
              <td class="px-6 py-4 text-gray-500 dark:text-gray-400">{{ formatDate(file.createdAt) }}</td>
              <td class="px-6 py-4 flex gap-2 justify-center" @click.stop>
                <button class="p-1.5 rounded hover:bg-gray-100 dark:hover:bg-gray-700 text-blue-600" title="Xem kết quả">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"></path>
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"></path>
                  </svg>
                </button>
                <button class="p-1.5 rounded hover:bg-gray-100 dark:hover:bg-gray-700 text-red-600" title="Xóa tài liệu">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path>
                  </svg>
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </main>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'

definePageMeta({ layout: 'default', middleware: 'auth-client' })

const route = useRoute()
const router = useRouter()
const config = useRuntimeConfig()

const projectId = route.params.id
const projectName = ref('')
const files = ref([])
const isLoading = ref(true)
const error = ref(null)
const isDragging = ref(false)

async function fetchFiles() {
  isLoading.value = true
  error.value = null
  const token = localStorage.getItem('jwt_token')
  if (!token) { error.value = 'Vui lòng đăng nhập.'; isLoading.value = false; return }
  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/Projects/${projectId}/files`, {
      headers: { Authorization: `Bearer ${token}` }
    })
    if (!res.ok) throw new Error('Không thể tải danh sách tài liệu.')
    files.value = await res.json()
  } catch (err) {
    error.value = err.message
  } finally {
    isLoading.value = false
  }
}

async function handleFileUpload(pickedFile) {
  if (!pickedFile) return
  const token = localStorage.getItem('jwt_token')
  if (!token) { alert('Vui lòng đăng nhập.'); return }
  const formData = new FormData()
  formData.append('image', pickedFile)
  formData.append('projectId', projectId)
  isLoading.value = true
  error.value = null
  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/Infer/upload-and-infer?saveAnnotated=false`, {
      method: 'POST', body: formData,
      headers: { Authorization: `Bearer ${token}` }
    })
    if (!res.ok) throw new Error(await res.text() || res.statusText)
    const jobData = await res.json()
    sessionStorage.setItem(`ocr_view_meta_${jobData.jobId}`, JSON.stringify({ jobId: jobData.jobId, imageUrl: jobData.imageUrl }))
    router.push(`/projects/${projectId}/file/${jobData.jobId}`)
  } catch (err) {
    error.value = `Lỗi: ${err.message}`
  } finally {
    isLoading.value = false
  }
}

function handleFileChange(e) { handleFileUpload(e.target.files[0]) }
function handleDrop(e) {
  isDragging.value = false
  const f = e.dataTransfer.files[0]
  if (!['application/pdf','image/jpeg','image/png'].includes(f.type)) { alert('Chỉ hỗ trợ JPG, PNG hoặc PDF.'); return }
  handleFileUpload(f)
}

function getStatusBadgeClass(status) {
  const map = {
    completed: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300',
    processing: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/50 dark:text-yellow-300',
    failed: 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300',
  }
  return map[status] || 'bg-gray-100 text-gray-800'
}
function formatStatus(s) {
  return { completed:'Xong', processing:'Đang quét AI', failed:'Lỗi' }[s] || 'Chờ'
}
function formatDate(d) {
  if (!d) return ''
  const date = new Date(d)
  return date.toLocaleDateString('vi-VN') + ' - ' + date.toLocaleTimeString('vi-VN', { hour:'2-digit', minute:'2-digit' })
}

onMounted(fetchFiles)
</script>

<style>
.fixed.inset-0.bg-blue-600\/90 * { pointer-events: none; user-select: none; }
</style>