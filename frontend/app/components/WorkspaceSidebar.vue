<template>
  <!-- Wrapper bọc cả sidebar workspace + panel project -->
  <div class="relative flex h-full">

    <!-- ── Cột 1: Workspace icons (kiểu Slack) ── -->
    <div class="flex flex-col items-center gap-2 w-14 py-3 bg-gray-200 dark:bg-neutral-900 border-r border-gray-300 dark:border-neutral-700 flex-shrink-0">

      <!-- Loading -->
      <div v-if="loading" class="flex flex-col gap-2 mt-2">
        <div v-for="i in 3" :key="i"
          class="w-9 h-9 rounded-xl bg-gray-300 dark:bg-neutral-700 animate-pulse">
        </div>
      </div>

      <!-- Workspace list -->
      <template v-else>
        <button
          v-for="ws in workspaces" :key="ws.id"
          @click="selectWorkspace(ws)"
          :title="ws.name"
          :class="[
            'relative w-9 h-9 rounded-xl flex items-center justify-center font-bold text-sm transition-all duration-150 flex-shrink-0',
            activeWs?.id === ws.id
              ? 'bg-yellow-400 text-gray-900 rounded-2xl shadow-md scale-105'
              : 'bg-gray-400 dark:bg-neutral-600 text-white hover:bg-blue-500 dark:hover:bg-blue-600 hover:rounded-2xl'
          ]"
        >
          {{ ws.name[0].toUpperCase() }}
          <!-- Indicator active -->
          <span v-if="activeWs?.id === ws.id"
            class="absolute -left-1 top-1/2 -translate-y-1/2 w-1 h-5 bg-gray-900 dark:bg-white rounded-r-full">
          </span>
        </button>

        <div class="w-7 h-px bg-gray-300 dark:bg-neutral-700 my-1"></div>

        <!-- Nút thêm workspace -->
        <button
          @click="showCreate = true"
          title="Tạo workspace mới"
          class="w-9 h-9 rounded-xl flex items-center justify-center text-xl font-light text-gray-500 dark:text-neutral-400 bg-gray-300 dark:bg-neutral-700 hover:bg-green-500 hover:text-white hover:rounded-2xl transition-all duration-150"
        >+</button>
      </template>
    </div>

    <!-- ── Cột 2: Project panel — absolute để không đẩy content ── -->
    <Transition name="slide">
      <div v-if="activeWs"
        class="absolute left-14 top-0 h-full flex flex-col w-56 bg-gray-100 dark:bg-neutral-800 border-r border-gray-200 dark:border-neutral-700 z-40 shadow-lg overflow-hidden"
      >
        <!-- Workspace header -->
        <div class="px-3 py-3 border-b border-gray-200 dark:border-neutral-700">
          <div class="flex items-center justify-between">
            <div class="min-w-0">
              <p class="font-semibold text-gray-900 dark:text-white text-sm truncate">{{ activeWs.name }}</p>
              <span :class="[
                'text-xs px-1.5 py-0.5 rounded font-medium',
                activeWs.myRole === 'Admin'
                  ? 'text-yellow-700 dark:text-yellow-400'
                  : 'text-blue-600 dark:text-blue-400'
              ]">{{ activeWs.myRole }}</span>
            </div>
            <!-- Nút settings workspace -->
            <button
              @click="goToWorkspace"
              title="Cài đặt workspace"
              class="w-6 h-6 flex items-center justify-center rounded text-gray-400 hover:text-gray-700 dark:hover:text-white hover:bg-gray-200 dark:hover:bg-neutral-700 transition-colors flex-shrink-0"
            >
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="14" height="14">
                <circle cx="12" cy="12" r="3"/>
                <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/>
              </svg>
            </button>
          </div>
        </div>

        <!-- Project list -->
        <div class="flex-1 overflow-y-auto py-2 scrollbar-thin">
          <div class="px-3 mb-1 flex items-center justify-between">
            <span class="text-xs font-semibold text-gray-400 dark:text-neutral-500 uppercase tracking-wider">Dự án</span>
            <button
              @click="showCreateProject = true"
              title="Tạo dự án mới"
              class="w-4 h-4 flex items-center justify-center text-gray-400 hover:text-gray-700 dark:hover:text-white transition-colors text-lg leading-none"
            >+</button>
          </div>

          <div v-if="loadingProjects" class="px-3 py-2 space-y-1">
            <div v-for="i in 4" :key="i"
              class="h-7 rounded bg-gray-200 dark:bg-neutral-700 animate-pulse">
            </div>
          </div>

          <div v-else-if="projects.length === 0"
            class="px-3 py-3 text-xs text-gray-400 dark:text-neutral-500 italic">
            Chưa có dự án nào
          </div>

          <button
            v-else
            v-for="p in projects" :key="p.id"
            @click="goToProject(p.id)"
            :class="[
              'w-full flex items-center gap-2 px-3 py-1.5 text-sm rounded-lg mx-0 transition-colors text-left',
              activeProjectId === p.id
                ? 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 font-medium'
                : 'text-gray-600 dark:text-neutral-300 hover:bg-gray-200 dark:hover:bg-neutral-700'
            ]"
          >
            <span class="text-xs opacity-60">#</span>
            <span class="truncate">{{ p.name }}</span>
            <span class="ml-auto text-xs text-gray-400 dark:text-neutral-500 flex-shrink-0">
              {{ p.mediaCount }}
            </span>
          </button>
        </div>

        <!-- Đóng panel -->
        <button
          @click="activeWs = null; emit('panel-change', false)"
          class="mx-3 mb-3 py-1.5 text-xs text-gray-400 dark:text-neutral-500 hover:text-gray-600 dark:hover:text-neutral-300 border border-gray-200 dark:border-neutral-700 rounded-lg transition-colors"
        >
          Thu gọn
        </button>
      </div>
    </Transition>

    <!-- ── Modal tạo Workspace ── -->
    <Transition name="modal">
      <div v-if="showCreate"
        class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4"
        @click.self="showCreate = false"
      >
        <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl p-8 w-full max-w-md shadow-2xl">
          <h2 class="text-xl font-bold text-gray-900 dark:text-white mb-6">Tạo Workspace mới</h2>
          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">
                Tên workspace <span class="text-red-500">*</span>
              </label>
              <input v-model="wsForm.name" @keyup.enter="handleCreateWs"
                class="w-full bg-gray-50 dark:bg-gray-900 border border-gray-300 dark:border-gray-600 text-gray-900 dark:text-white rounded-lg px-3.5 py-2.5 text-sm outline-none focus:border-blue-500 dark:focus:border-blue-400 transition-colors placeholder-gray-400 dark:placeholder-gray-500"
                placeholder="VD: FPT Software" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Mô tả</label>
              <textarea v-model="wsForm.description" rows="2"
                class="w-full bg-gray-50 dark:bg-gray-900 border border-gray-300 dark:border-gray-600 text-gray-900 dark:text-white rounded-lg px-3.5 py-2.5 text-sm outline-none focus:border-blue-500 dark:focus:border-blue-400 transition-colors placeholder-gray-400 dark:placeholder-gray-500 resize-none"
                placeholder="Mô tả ngắn..."></textarea>
            </div>
          </div>
          <div class="flex justify-end gap-3 mt-6">
            <button @click="showCreate = false"
              class="px-4 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors">
              Hủy
            </button>
            <button @click="handleCreateWs" :disabled="!wsForm.name.trim() || creatingWs"
              class="px-5 py-2 text-sm rounded-lg bg-yellow-400 hover:bg-yellow-500 disabled:opacity-50 text-gray-900 font-semibold transition-colors">
              {{ creatingWs ? 'Đang tạo...' : 'Tạo' }}
            </button>
          </div>
        </div>
      </div>
    </Transition>

    <!-- ── Modal tạo Project ── -->
    <Transition name="modal">
      <div v-if="showCreateProject"
        class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4"
        @click.self="showCreateProject = false"
      >
        <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl p-8 w-full max-w-md shadow-2xl">
          <h2 class="text-xl font-bold text-gray-900 dark:text-white mb-6">Tạo dự án mới</h2>
          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">
                Tên dự án <span class="text-red-500">*</span>
              </label>
              <input v-model="projectForm.name" @keyup.enter="handleCreateProject"
                class="w-full bg-gray-50 dark:bg-gray-900 border border-gray-300 dark:border-gray-600 text-gray-900 dark:text-white rounded-lg px-3.5 py-2.5 text-sm outline-none focus:border-blue-500 dark:focus:border-blue-400 transition-colors placeholder-gray-400 dark:placeholder-gray-500"
                placeholder="VD: Dự án ngân hàng ABC" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Mô tả</label>
              <textarea v-model="projectForm.description" rows="2"
                class="w-full bg-gray-50 dark:bg-gray-900 border border-gray-300 dark:border-gray-600 text-gray-900 dark:text-white rounded-lg px-3.5 py-2.5 text-sm outline-none focus:border-blue-500 dark:focus:border-blue-400 transition-colors placeholder-gray-400 dark:placeholder-gray-500 resize-none"
                placeholder="Mô tả ngắn..."></textarea>
            </div>
          </div>
          <div class="flex justify-end gap-3 mt-6">
            <button @click="showCreateProject = false"
              class="px-4 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors">
              Hủy
            </button>
            <button @click="handleCreateProject" :disabled="!projectForm.name.trim() || creatingProject"
              class="px-5 py-2 text-sm rounded-lg bg-yellow-400 hover:bg-yellow-500 disabled:opacity-50 text-gray-900 font-semibold transition-colors">
              {{ creatingProject ? 'Đang tạo...' : 'Tạo' }}
            </button>
          </div>
        </div>
      </div>
    </Transition>

  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useWorkspace } from '~/composables/useWorkspace'
import { useProject } from '~/composables/useProject'
import { useJwt } from '~/composables/useJwt'

const router = useRouter()
const route = useRoute()
const { getMyWorkspaces, createWorkspace } = useWorkspace()
const { getProjects, createProject } = useProject()
const { jwt } = useJwt()

const emit = defineEmits<{ 'panel-change': [isOpen: boolean] }>()

// Khi logout (jwt bị xóa) → clear toàn bộ state sidebar
watch(jwt, (newVal) => {
  if (!newVal) {
    workspaces.value = []
    activeWs.value = null
    projects.value = []
    emit('panel-change', false)
  }
})

// ── State ────────────────────────────────────────────────────────
const workspaces = ref<any[]>([])
const loading = ref(true)
const activeWs = ref<any>(null)

const projects = ref<any[]>([])
const loadingProjects = ref(false)
const activeProjectId = ref<number | null>(null)

const showCreate = ref(false)
const creatingWs = ref(false)
const wsForm = ref({ name: '', description: '' })

const showCreateProject = ref(false)
const creatingProject = ref(false)
const projectForm = ref({ name: '', description: '' })

// ── Load workspaces ───────────────────────────────────────────────
async function load() {
  // Không load nếu chưa đăng nhập
  const token = jwt.value || (process.client ? localStorage.getItem('jwt_token') : null)
  if (!token) return

  try {
    loading.value = true
    workspaces.value = await getMyWorkspaces()
  } catch (e) {
    console.error(e)
  } finally {
    loading.value = false
  }
}

// ── Select workspace → load projects ─────────────────────────────
async function selectWorkspace(ws: any) {
  if (activeWs.value?.id === ws.id) {
    activeWs.value = null
    emit('panel-change', false)
    return
  }
  activeWs.value = ws
  emit('panel-change', true)
  await loadProjects(ws.id)
}

async function loadProjects(workspaceId: number) {
  try {
    loadingProjects.value = true
    projects.value = await getProjects(workspaceId)
  } catch (e) {
    console.error(e)
  } finally {
    loadingProjects.value = false
  }
}

// ── Navigation ────────────────────────────────────────────────────
function goToWorkspace() {
  if (activeWs.value) router.push(`/workspaces/${activeWs.value.id}`)
}

function goToProject(projectId: number) {
  activeProjectId.value = projectId
  router.push(`/workspaces/project/${projectId}`)
}

// ── Create workspace ──────────────────────────────────────────────
async function handleCreateWs() {
  if (!wsForm.value.name.trim() || creatingWs.value) return
  try {
    creatingWs.value = true
    const ws = await createWorkspace(wsForm.value)
    workspaces.value.push(ws)
    showCreate.value = false
    wsForm.value = { name: '', description: '' }
    await selectWorkspace(ws)
  } finally { creatingWs.value = false }
}

// ── Create project ────────────────────────────────────────────────
async function handleCreateProject() {
  if (!projectForm.value.name.trim() || creatingProject.value || !activeWs.value) return
  try {
    creatingProject.value = true
    const p = await createProject(activeWs.value.id, projectForm.value)
    projects.value.unshift(p)
    showCreateProject.value = false
    projectForm.value = { name: '', description: '' }
    goToProject(p.id)
  } finally { creatingProject.value = false }
}

// ── Sync active project từ route ──────────────────────────────────
watch(() => route.path, (path) => {
  const match = path.match(/\/project\/(\d+)/)
  activeProjectId.value = match ? parseInt(match[1]) : null
}, { immediate: true })

onMounted(load)
</script>

<style scoped>
/* Slide transition cho project panel */
.slide-enter-active, .slide-leave-active {
  transition: all 0.2s ease;
}
.slide-enter-from, .slide-leave-to {
  width: 0;
  opacity: 0;
  overflow: hidden;
}
.slide-enter-to, .slide-leave-from {
  width: 13rem; /* w-52 */
}

.modal-enter-active, .modal-leave-active { transition: opacity 0.2s ease; }
.modal-enter-from, .modal-leave-to { opacity: 0; }

/* Scrollbar mỏng cho danh sách project */
.scrollbar-thin {
  scrollbar-width: thin;
  scrollbar-color: rgba(0,0,0,0.15) transparent;
}
.scrollbar-thin::-webkit-scrollbar { width: 3px; }
.scrollbar-thin::-webkit-scrollbar-thumb { background: rgba(0,0,0,0.15); border-radius: 3px; }
</style>