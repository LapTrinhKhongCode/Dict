<template>
  <div
    class="bg-gray-50 dark:bg-gray-900 transition-colors min-h-full"
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

    <main class="p-6 md:p-8">
      <div class="flex items-center justify-between mb-6">
        <div class="flex items-center gap-3">
          <button @click="$router.back()"
            class="p-2 rounded-full hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-500">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18"/>
            </svg>
          </button>
          <div>
            <h1 class="text-xl font-bold text-gray-900 dark:text-white">{{ projectName || 'Dự án' }}</h1>
            <p class="text-xs text-gray-400 mt-0.5">{{ files.length }} tài liệu</p>
          </div>
        </div>
        <div class="flex items-center gap-3">
          <!-- Upload button với inline loading -->
          <button @click="$refs.fileInput.click()" :disabled="uploading"
            class="px-5 py-2.5 bg-blue-600 text-white rounded-full font-semibold text-sm hover:bg-blue-700 disabled:opacity-60 transition flex items-center gap-2 shadow">
            <svg v-if="uploading" class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
              <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
              <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"/>
            </svg>
            <svg v-else class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M12 4v16m8-8H4"/>
            </svg>
            {{ uploading ? 'Đang tải lên...' : 'Tải lên tài liệu' }}
          </button>
          <input type="file" ref="fileInput" @change="handleFileChange"
            accept="image/*,application/pdf" class="hidden" />
        </div>
      </div>

      <!-- Upload progress banner — không che cả trang -->
      <div v-if="uploading"
        class="mb-4 flex items-center gap-3 px-4 py-3 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-xl text-sm text-blue-700 dark:text-blue-300">
        <div class="w-4 h-4 border-2 border-blue-600 border-t-transparent rounded-full animate-spin flex-shrink-0"></div>
        Đang tải file lên... Danh sách tài liệu sẽ cập nhật sau khi hoàn tất.
      </div>

      <!-- Upload error -->
      <div v-if="uploadError"
        class="mb-4 px-4 py-3 bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-xl text-sm text-red-700 dark:text-red-300">
        {{ uploadError }}
        <button @click="uploadError = ''" class="ml-2 underline">Đóng</button>
      </div>

      <!-- Initial loading -->
      <div v-if="isLoading" class="flex items-center gap-3 py-10 text-gray-500 text-sm">
        <div class="w-5 h-5 border-2 border-gray-300 border-t-blue-600 rounded-full animate-spin"></div>
        Đang tải danh sách...
      </div>

      <!-- Empty -->
      <div v-else-if="!files.length"
        class="text-center py-32 bg-white dark:bg-gray-800 rounded-3xl border border-dashed border-gray-200 dark:border-gray-700 shadow-xl">
        <svg class="mx-auto w-24 h-24 mb-6 text-gray-300 dark:text-gray-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5"
            d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/>
        </svg>
        <h3 class="text-2xl font-bold text-gray-800 dark:text-gray-100">Chưa có tài liệu nào!</h3>
        <p class="text-gray-500 mt-2">Upload file để AI nhận diện chữ.</p>
      </div>

      <!-- File table -->
      <div v-else class="bg-white dark:bg-gray-800 shadow-xl rounded-2xl overflow-hidden border border-gray-100 dark:border-gray-700">
        <table class="w-full text-sm text-left">
          <thead class="text-xs text-gray-500 uppercase bg-gray-50/50 dark:bg-gray-700/50 border-b dark:border-gray-700">
            <tr>
              <th class="px-6 py-4 w-5/12">Tên tài liệu</th>
              <th class="px-6 py-4">Loại</th>
              <th class="px-6 py-4">Trạng thái AI</th>
              <th class="px-6 py-4">Ngày tạo</th>
              <th class="px-6 py-4 w-[80px]">Mở</th>
            </tr>
          </thead>
          <tbody>
            <!-- File mới upload đang pending — hiện ngay không cần chờ refresh -->
            <tr v-if="pendingFile"
              class="border-b dark:border-gray-700 bg-blue-50/50 dark:bg-blue-900/10">
              <td class="px-6 py-4 font-medium text-gray-900 dark:text-gray-100 flex items-center gap-3">
                <div class="w-4 h-4 border-2 border-blue-500 border-t-transparent rounded-full animate-spin flex-shrink-0"></div>
                <span class="truncate text-blue-600 dark:text-blue-400">{{ pendingFile.name }}</span>
              </td>
              <td class="px-6 py-4 text-gray-400 font-mono uppercase text-xs">{{ pendingFile.type }}</td>
              <td class="px-6 py-4">
                <span class="text-xs font-semibold px-2.5 py-1 rounded-full bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300">
                  Đang tải lên...
                </span>
              </td>
              <td class="px-6 py-4 text-gray-400">Vừa xong</td>
              <td class="px-6 py-4"></td>
            </tr>

            <tr v-for="file in files" :key="file.id"
              class="border-b dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-700/50 cursor-pointer transition"
              @click="openFile(file)">
              <td class="px-6 py-4 font-medium text-gray-900 dark:text-gray-100 flex items-center gap-3">
                <svg v-if="file.type === 'pdf'" class="w-6 h-6 text-red-500 flex-shrink-0" fill="currentColor" viewBox="0 0 24 24">
                  <path fill-rule="evenodd" d="M4 5a2 2 0 012-2h8.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V19a2 2 0 01-2 2H6a2 2 0 01-2-2V5zm10 1V4.5l3.5 3.5H16a2 2 0 01-2-2z" clip-rule="evenodd"/>
                </svg>
                <svg v-else class="w-6 h-6 text-blue-500 flex-shrink-0" fill="currentColor" viewBox="0 0 24 24">
                  <path fill-rule="evenodd" d="M4 5a2 2 0 012-2h8.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V19a2 2 0 01-2 2H6a2 2 0 01-2-2V5zm10 1V4.5l3.5 3.5H16a2 2 0 01-2-2z" clip-rule="evenodd"/>
                </svg>
                <span class="truncate">{{ file.name }}</span>
              </td>
              <td class="px-6 py-4 text-gray-500 dark:text-gray-400 font-mono uppercase text-xs">{{ file.type }}</td>
              <td class="px-6 py-4">
                <span :class="statusClass(file.status)"
                  class="text-xs font-semibold px-2.5 py-1 rounded-full whitespace-nowrap">
                  {{ statusText(file.status) }}
                </span>
              </td>
              <td class="px-6 py-4 text-gray-500 dark:text-gray-400">{{ formatDate(file.createdAt) }}</td>
              <td class="px-6 py-4 text-center" @click.stop>
                <button @click="openFile(file)"
                  class="p-1.5 rounded hover:bg-gray-100 dark:hover:bg-gray-700 text-blue-600">
                  <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"/>
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"/>
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
definePageMeta({ layout: 'default', ssr: false })

import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'

const route = useRoute()
const router = useRouter()
const config = useRuntimeConfig()

const projectId = route.params.projectid
const projectName = ref('')
const files = ref([])
const isLoading = ref(true)
const uploading = ref(false)
const uploadError = ref('')
const isDragging = ref(false)
const pendingFile = ref(null) // file vừa upload, hiện ngay trong list

async function fetchFiles() {
  const token = localStorage.getItem('jwt_token')
  if (!token) return
  try {
    const res = await fetch(
      `${config.public.apiBaseUrl}/api/projects/${projectId}/files`,
      { headers: { Authorization: `Bearer ${token}` } }
    )
    if (res.ok) files.value = await res.json()
  } catch (e) {
    console.error(e)
  } finally {
    isLoading.value = false
  }
}

function openFile(file) {
  // Navigate sang reader với jobId để lazy OCR
  const name = encodeURIComponent(file.name || '')
  router.push(`/reader?jobId=${file.id}&projectId=${projectId}&name=${name}`)
}

async function handleFileUpload(pickedFile) {
  if (!pickedFile || uploading.value) return
  const token = localStorage.getItem('jwt_token')
  if (!token) { uploadError.value = 'Vui lòng đăng nhập.'; return }

  uploading.value = true
  uploadError.value = ''

  // Hiện file pending ngay trong list
  const ext = pickedFile.name.split('.').pop()?.toLowerCase() || ''
  pendingFile.value = { name: pickedFile.name, type: ext }

  const formData = new FormData()
  formData.append('image', pickedFile)
  formData.append('projectId', projectId)

  try {
    const res = await fetch(
      `${config.public.apiBaseUrl}/api/Infer/upload-and-infer?saveAnnotated=false`,
      { method: 'POST', body: formData, headers: { Authorization: `Bearer ${token}` } }
    )
    if (!res.ok) throw new Error(await res.text() || res.statusText)
    const jobData = await res.json()

    // Cache để reader hiện ảnh ngay
    sessionStorage.setItem(
      `ocr_view_meta_${jobData.jobId}`,
      JSON.stringify({ jobId: jobData.jobId, imageUrl: jobData.imageUrl })
    )

    // Refresh list sau khi upload xong
    await fetchFiles()
    pendingFile.value = null
  } catch (err) {
    uploadError.value = `Lỗi upload: ${err.message}`
    pendingFile.value = null
  } finally {
    uploading.value = false
  }
}

function handleFileChange(e) {
  handleFileUpload(e.target.files[0])
  // Reset input để có thể upload cùng file lần 2
  e.target.value = ''
}
function handleDrop(e) {
  isDragging.value = false
  const f = e.dataTransfer.files[0]
  if (!f || !['application/pdf', 'image/jpeg', 'image/png'].includes(f.type)) {
    uploadError.value = 'Chỉ hỗ trợ JPG, PNG hoặc PDF.'
    return
  }
  handleFileUpload(f)
}

function statusClass(s) {
  const map = {
    completed: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300',
    processing: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/50 dark:text-yellow-300',
    failed: 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300',
  }
  return map[s?.toLowerCase()] || 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-300'
}
function statusText(s) {
  return { completed: 'Hoàn thành', processing: 'Đang quét AI', failed: 'Lỗi' }[s?.toLowerCase()] || 'Chờ xử lý'
}
function formatDate(d) {
  if (!d) return ''
  const date = new Date(d)
  return date.toLocaleDateString('vi-VN') + ' ' +
    date.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
}

onMounted(fetchFiles)
</script>