<template>
  <div class="bg-gray-50 dark:bg-gray-900 min-h-screen transition-colors">
    <div class="max-w-5xl mx-auto px-6 py-10">

      <!-- Loading -->
      <div v-if="!project" class="flex justify-center py-20">
        <div class="w-6 h-6 border-2 border-yellow-400 border-t-transparent rounded-full animate-spin"></div>
      </div>

      <template v-else>
        <!-- Breadcrumb -->
        <div class="flex items-center gap-2 text-sm text-gray-400 dark:text-gray-500 mb-6">
          <!-- <button @click="router.push('/workspace')" class="hover:text-gray-700 dark:hover:text-gray-300 transition-colors">
            Workspaces
          </button>
          <span>/</span>
          <button @click="router.push(`/workspace/${project.workspaceId}`)" class="hover:text-gray-700 dark:hover:text-gray-300 transition-colors">
            Workspace
          </button>
          <span>/</span>
          <span class="text-gray-700 dark:text-gray-300 font-medium truncate max-w-[200px]">{{ project.name }}</span> -->
        </div>

        <!-- Header -->
        <div class="flex items-start justify-between gap-4 mb-8 flex-wrap">
          <div>
            <h1 class="text-2xl font-bold text-gray-900 dark:text-white mb-1">{{ project.name }}</h1>
            <p class="text-sm text-gray-500 dark:text-gray-400">{{ project.description || 'Chưa có mô tả' }}</p>
            <div class="flex items-center gap-4 mt-2 text-xs text-gray-400 dark:text-gray-500">
              <span>{{ project.mediaCount }} PDF</span>
              <span>{{ project.vocabularyCount }} từ vựng</span>
              <span>Tạo bởi {{ project.createdByUserName }}</span>
            </div>
          </div>

          <!-- Upload -->
          <label
            class="flex items-center gap-2 bg-yellow-400 hover:bg-yellow-500 text-gray-900 font-semibold text-sm px-4 py-2 rounded-lg cursor-pointer transition-colors flex-shrink-0"
            :class="{ 'opacity-50 cursor-not-allowed': uploading }"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
              <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
              <polyline points="17 8 12 3 7 8"/>
              <line x1="12" y1="3" x2="12" y2="15"/>
            </svg>
            {{ uploading ? 'Đang upload...' : 'Upload PDF' }}
            <input
              v-if="!uploading"
              type="file"
              accept=".pdf"
              hidden
              @change="handleUpload"
            />
          </label>
        </div>

        <!-- Upload progress -->
        <div v-if="uploading"
          class="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-xl p-4 mb-4 flex items-center gap-3">
          <div class="w-4 h-4 border-2 border-blue-500 border-t-transparent rounded-full animate-spin flex-shrink-0"></div>
          <p class="text-sm text-blue-700 dark:text-blue-300">Đang upload {{ uploadingName }}...</p>
        </div>

        <!-- Error -->
        <div v-if="uploadError"
          class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800 rounded-xl p-4 mb-4">
          <p class="text-sm text-red-600 dark:text-red-400">{{ uploadError }}</p>
        </div>

        <!-- PDF list -->
        <div v-if="mediaList.length > 0" class="space-y-2">
          <div
            v-for="m in mediaList" :key="m.id"
            class="group bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl px-5 py-4 flex items-center gap-4 hover:border-blue-400 dark:hover:border-blue-500 transition-colors cursor-pointer"
            @click="openReader(m)"
          >
            <!-- PDF icon -->
            <div class="w-10 h-10 bg-red-100 dark:bg-red-900/30 rounded-lg flex items-center justify-center flex-shrink-0">
              <svg class="w-5 h-5 text-red-600 dark:text-red-400" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
                <polyline points="14 2 14 8 20 8"/>
              </svg>
            </div>

            <!-- Info -->
            <div class="flex-1 min-w-0">
              <p class="font-medium text-gray-900 dark:text-white text-sm truncate">{{ m.fileName }}</p>
              <p class="text-xs text-gray-400 dark:text-gray-500 mt-0.5">
                {{ formatSize(m.sizeBytes) }} · Upload bởi {{ m.ownerName }} · {{ formatDate(m.createdAt) }}
              </p>
            </div>

            <!-- Actions -->
            <div class="flex items-center gap-2 opacity-0 group-hover:opacity-100 transition-opacity">
              <button
                @click.stop="openReader(m)"
                class="flex items-center gap-1.5 text-xs bg-blue-500 hover:bg-blue-600 text-white px-3 py-1.5 rounded-lg transition-colors"
              >
                <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
                  <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                  <circle cx="12" cy="12" r="3"/>
                </svg>
                Đọc
              </button>
              <button
                @click.stop="handleDeleteMedia(m.id)"
                class="w-8 h-8 flex items-center justify-center rounded-lg text-gray-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors"
              >✕</button>
            </div>
          </div>
        </div>

        <!-- Empty -->
        <div v-else class="flex flex-col items-center justify-center py-20 text-gray-400 dark:text-gray-600">
          <svg class="w-12 h-12 mb-4 opacity-40" fill="none" stroke="currentColor" stroke-width="1" viewBox="0 0 24 24">
            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
            <polyline points="14 2 14 8 20 8"/>
            <line x1="12" y1="18" x2="12" y2="12"/>
            <line x1="9" y1="15" x2="15" y2="15"/>
          </svg>
          <p class="font-medium text-base">Chưa có file PDF nào</p>
          <p class="text-sm mt-1">Upload PDF để bắt đầu đọc và lưu từ vựng</p>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useProject, type ProjectDto, type MediaDto } from '~/composables/useProject'
import { useRuntimeConfig } from '#app'

definePageMeta({ middleware: 'auth-client' })

const route = useRoute()
const router = useRouter()
const projectId = parseInt(route.params.projectid as string)

const { getProject, getMedia, uploadMedia, deleteMedia } = useProject()
const config = useRuntimeConfig()

const project = ref<ProjectDto | null>(null)
const mediaList = ref<MediaDto[]>([])
const uploading = ref(false)
const uploadingName = ref('')
const uploadError = ref('')

async function load() {
  const [p, m] = await Promise.all([getProject(projectId), getMedia(projectId)])
  project.value = p
  mediaList.value = m
}

async function handleUpload(e: Event) {
  const file = (e.target as HTMLInputElement).files?.[0]
  if (!file) return

  uploadError.value = ''
  uploading.value = true
  uploadingName.value = file.name

  try {
    const media = await uploadMedia(projectId, file)
    mediaList.value.unshift(media)
    if (project.value) project.value.mediaCount++
  } catch (err: any) {
    uploadError.value = err?.data?.message || 'Upload thất bại. Vui lòng thử lại.'
  } finally {
    uploading.value = false
    uploadingName.value = ''
    ;(e.target as HTMLInputElement).value = ''
  }
}

async function handleDeleteMedia(id: number) {
  if (!confirm('Xóa file PDF này?')) return
  await deleteMedia(id)
  mediaList.value = mediaList.value.filter(m => m.id !== id)
  if (project.value) project.value.mediaCount--
}

function openReader(m: MediaDto) {
  router.push({
    path: '/reader',
    query: {
      mediaId: String(m.id),
      name: m.fileName,
      projectId: String(projectId),
      url: m.storageUrl
    }
  })
}

function formatSize(bytes: number | null) {
  if (!bytes) return '—'
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`
  return `${(bytes / 1024 / 1024).toFixed(1)} MB`
}

function formatDate(d: string | null) {
  if (!d) return '—'
  return new Date(d).toLocaleDateString('vi-VN')
}

onMounted(load)
</script>