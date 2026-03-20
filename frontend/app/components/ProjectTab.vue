<template>
  <div class="space-y-4">

    <!-- Header -->
    <div class="flex items-center justify-between">
      <p class="text-sm text-gray-500 dark:text-gray-400">{{ projects.length }} dự án</p>
      <button
        @click="showCreate = true"
        class="flex items-center gap-1.5 bg-yellow-400 hover:bg-yellow-500 text-gray-900 font-semibold text-sm px-3.5 py-2 rounded-lg transition-colors"
      >
        + Tạo dự án
      </button>
    </div>

    <!-- Loading -->
    <div v-if="loading" class="flex justify-center py-10">
      <div class="w-5 h-5 border-2 border-yellow-400 border-t-transparent rounded-full animate-spin"></div>
    </div>

    <!-- Project list -->
    <div v-else class="grid grid-cols-1 sm:grid-cols-2 gap-3">
      <div
        v-for="p in projects" :key="p.id"
        class="group bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-4 cursor-pointer hover:border-blue-400 dark:hover:border-blue-500 hover:-translate-y-0.5 hover:shadow-md transition-all duration-200"
        @click="router.push(`/workspaces/project/${p.id}`)"
      >
        <div class="flex items-start justify-between gap-2 mb-3">
          <div class="w-9 h-9 rounded-lg bg-gradient-to-br from-blue-500 to-indigo-600 flex items-center justify-center text-white font-bold text-sm flex-shrink-0">
            {{ p.name[0].toUpperCase() }}
          </div>
          <!-- Actions (chỉ hiện khi hover) -->
          <div class="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
            <button
              @click.stop="openEdit(p)"
              class="w-7 h-7 flex items-center justify-center rounded-lg text-gray-400 hover:text-blue-500 hover:bg-blue-50 dark:hover:bg-blue-900/20 transition-colors text-xs"
            >✎</button>
            <button
              @click.stop="handleDelete(p.id)"
              class="w-7 h-7 flex items-center justify-center rounded-lg text-gray-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors text-xs"
            >✕</button>
          </div>
        </div>

        <p class="font-semibold text-gray-900 dark:text-white text-sm mb-1 truncate">{{ p.name }}</p>
        <p class="text-xs text-gray-400 dark:text-gray-500 mb-3 line-clamp-2 min-h-[2rem]">
          {{ p.description || 'Chưa có mô tả' }}
        </p>

        <div class="flex items-center gap-3 text-xs text-gray-400 dark:text-gray-500">
          <span class="flex items-center gap-1">
            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
              <polyline points="14 2 14 8 20 8"/>
            </svg>
            {{ p.mediaCount }} PDF
          </span>
          <span class="flex items-center gap-1">
            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
              <path d="M2 3h6a4 4 0 0 1 4 4v14a3 3 0 0 0-3-3H2z"/>
              <path d="M22 3h-6a4 4 0 0 0-4 4v14a3 3 0 0 1 3-3h7z"/>
            </svg>
            {{ p.vocabularyCount }} từ vựng
          </span>
        </div>
      </div>

      <!-- Empty -->
      <div v-if="projects.length === 0"
        class="col-span-full flex flex-col items-center justify-center py-16 text-gray-400 dark:text-gray-600">
        <svg class="w-10 h-10 mb-3 opacity-40" fill="none" stroke="currentColor" stroke-width="1" viewBox="0 0 24 24">
          <path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"/>
        </svg>
        <p class="font-medium">Chưa có dự án nào</p>
        <p class="text-sm mt-1">Nhấn "+ Tạo dự án" để bắt đầu</p>
      </div>
    </div>

    <!-- Modal tạo project -->
    <Transition name="modal">
      <div v-if="showCreate"
        class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4"
        @click.self="showCreate = false"
      >
        <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl p-8 w-full max-w-md shadow-2xl">
          <h2 class="text-xl font-bold text-gray-900 dark:text-white mb-6">Tạo dự án mới</h2>
          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">
                Tên dự án <span class="text-red-500">*</span>
              </label>
              <input
                v-model="form.name"
                class="w-full bg-gray-50 dark:bg-gray-900 border border-gray-300 dark:border-gray-600 text-gray-900 dark:text-white rounded-lg px-3.5 py-2.5 text-sm outline-none focus:border-blue-500 dark:focus:border-blue-400 transition-colors placeholder-gray-400 dark:placeholder-gray-500"
                placeholder="VD: Dự án ngân hàng ABC"
                @keyup.enter="handleCreate"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Mô tả</label>
              <textarea
                v-model="form.description"
                rows="3"
                class="w-full bg-gray-50 dark:bg-gray-900 border border-gray-300 dark:border-gray-600 text-gray-900 dark:text-white rounded-lg px-3.5 py-2.5 text-sm outline-none focus:border-blue-500 dark:focus:border-blue-400 transition-colors placeholder-gray-400 dark:placeholder-gray-500 resize-none"
                placeholder="Mô tả ngắn về dự án..."
              ></textarea>
            </div>
          </div>
          <div class="flex justify-end gap-3 mt-6">
            <button @click="showCreate = false; resetForm()"
              class="px-4 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors">
              Hủy
            </button>
            <button @click="handleCreate" :disabled="!form.name.trim() || saving"
              class="px-5 py-2 text-sm rounded-lg bg-yellow-400 hover:bg-yellow-500 disabled:opacity-50 disabled:cursor-not-allowed text-gray-900 font-semibold transition-colors">
              {{ saving ? 'Đang tạo...' : 'Tạo dự án' }}
            </button>
          </div>
        </div>
      </div>
    </Transition>

    <!-- Modal sửa project -->
    <Transition name="modal">
      <div v-if="showEdit"
        class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4"
        @click.self="showEdit = false"
      >
        <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl p-8 w-full max-w-md shadow-2xl">
          <h2 class="text-xl font-bold text-gray-900 dark:text-white mb-6">Sửa dự án</h2>
          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Tên</label>
              <input v-model="editForm.name"
                class="w-full bg-gray-50 dark:bg-gray-900 border border-gray-300 dark:border-gray-600 text-gray-900 dark:text-white rounded-lg px-3.5 py-2.5 text-sm outline-none focus:border-blue-500 dark:focus:border-blue-400 transition-colors" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Mô tả</label>
              <textarea v-model="editForm.description" rows="3"
                class="w-full bg-gray-50 dark:bg-gray-900 border border-gray-300 dark:border-gray-600 text-gray-900 dark:text-white rounded-lg px-3.5 py-2.5 text-sm outline-none focus:border-blue-500 dark:focus:border-blue-400 transition-colors resize-none"></textarea>
            </div>
          </div>
          <div class="flex justify-end gap-3 mt-6">
            <button @click="showEdit = false"
              class="px-4 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors">
              Hủy
            </button>
            <button @click="handleUpdate" :disabled="saving"
              class="px-5 py-2 text-sm rounded-lg bg-yellow-400 hover:bg-yellow-500 disabled:opacity-50 text-gray-900 font-semibold transition-colors">
              {{ saving ? 'Đang lưu...' : 'Lưu' }}
            </button>
          </div>
        </div>
      </div>
    </Transition>

  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useProject, type ProjectDto } from '~/composables/useProject'

const props = defineProps<{ workspaceId: number }>()
const router = useRouter()
const { getProjects, createProject, updateProject, deleteProject } = useProject()

const projects = ref<ProjectDto[]>([])
const loading = ref(true)
const saving = ref(false)
const showCreate = ref(false)
const showEdit = ref(false)
const editingId = ref<number | null>(null)

const form = ref({ name: '', description: '' })
const editForm = ref({ name: '', description: '' })

function resetForm() { form.value = { name: '', description: '' } }

async function load() {
  try {
    loading.value = true
    projects.value = await getProjects(props.workspaceId)
  } catch (e) {
    console.error(e)
  } finally {
    loading.value = false
  }
}

async function handleCreate() {
  if (!form.value.name.trim() || saving.value) return
  try {
    saving.value = true
    const p = await createProject(props.workspaceId, form.value)
    projects.value.unshift(p)
    showCreate.value = false
    resetForm()
  } finally { saving.value = false }
}

function openEdit(p: ProjectDto) {
  editingId.value = p.id
  editForm.value = { name: p.name, description: p.description }
  showEdit.value = true
}

async function handleUpdate() {
  if (!editingId.value) return
  try {
    saving.value = true
    const updated = await updateProject(editingId.value, editForm.value)
    const idx = projects.value.findIndex(p => p.id === editingId.value)
    if (idx !== -1) projects.value[idx] = updated
    showEdit.value = false
  } finally { saving.value = false }
}

async function handleDelete(id: number) {
  if (!confirm('Xóa dự án này? Tất cả từ vựng sẽ bị xóa.')) return
  await deleteProject(id)
  projects.value = projects.value.filter(p => p.id !== id)
}

onMounted(load)
</script>

<style scoped>
.modal-enter-active, .modal-leave-active { transition: opacity 0.2s ease; }
.modal-enter-from, .modal-leave-to { opacity: 0; }
</style>