<template>
  <div class="relative h-full w-14">

    <!-- Activity Bar: 56px collapsed, expands to 208px on hover ONLY when workspace panel is closed -->
    <div
      :class="[
        'group/bar absolute left-0 top-0 h-full z-50 flex flex-col overflow-hidden',
        'bg-gray-200 dark:bg-neutral-900 border-r border-gray-300 dark:border-neutral-700',
        'transition-[width] duration-200 ease-in-out w-14',
        !activeWs ? 'hover:w-52 hover:shadow-xl' : ''
      ]"
    >
      <!-- Japanese nav items -->
      <div class="flex flex-col flex-1 gap-0.5 px-2 pt-3">
        <NuxtLink
          v-for="item in navItems" :key="item.to"
          :to="item.to"
          :title="item.label"
          :class="[
            'relative flex items-center gap-3 px-2 py-2 rounded-xl transition-all duration-150 min-w-0',
            route.path.startsWith(item.to)
              ? 'bg-primary-100 dark:bg-primary-900/40 text-primary-600 dark:text-primary-400'
              : 'text-gray-500 dark:text-neutral-400 hover:bg-gray-300 dark:hover:bg-neutral-700 hover:text-gray-900 dark:hover:text-white'
          ]"
        >
          <span v-if="route.path.startsWith(item.to)"
            class="absolute -left-2 top-1/2 -translate-y-1/2 w-1 h-5 bg-primary-500 rounded-r-full" />
          <UIcon :name="item.icon" class="w-5 h-5 flex-shrink-0" />
          <span class="text-sm font-medium whitespace-nowrap overflow-hidden
                       opacity-0 group-hover/bar:opacity-100 transition-opacity duration-150">
            {{ item.label }}
          </span>
        </NuxtLink>
      </div>

      <!-- Divider + Workspace section -->
      <template v-if="isAuthenticated">
        <div class="mx-3 my-2 h-px bg-gray-300 dark:bg-neutral-700" />

        <div class="flex flex-col px-2 pb-2 gap-0.5">
          <!-- Section label -->
          <div class="flex items-center gap-3 px-2 py-1 mb-0.5 overflow-hidden">
            <span class="w-5 h-5 flex-shrink-0" />
            <span class="text-xs font-semibold uppercase tracking-wider text-gray-400 dark:text-neutral-500 whitespace-nowrap
                         opacity-0 group-hover/bar:opacity-100 transition-opacity duration-150">
              Workspace
            </span>
          </div>

          <!-- Loading -->
          <template v-if="loading">
            <div v-for="i in 3" :key="i" class="h-9 rounded-xl bg-gray-300 dark:bg-neutral-700 animate-pulse mx-1" />
          </template>

          <template v-else>
            <button
              v-for="ws in workspaces" :key="ws.id"
              @click="selectWorkspace(ws)"
              :title="ws.name"
              :class="[
                'relative flex items-center gap-3 w-full px-2 py-1.5 rounded-xl transition-all duration-150 text-left overflow-hidden',
                activeWs?.id === ws.id
                  ? 'bg-blue-500 text-white'
                  : 'text-gray-600 dark:text-neutral-300 hover:bg-gray-300 dark:hover:bg-neutral-700'
              ]"
            >
              <span :class="[
                'w-5 h-5 rounded-lg flex items-center justify-center font-bold text-xs flex-shrink-0',
                activeWs?.id === ws.id
                  ? 'bg-blue-400 text-white'
                  : 'bg-gray-400 dark:bg-neutral-600 text-white'
              ]">{{ ws.name[0].toUpperCase() }}</span>
              <span class="text-sm truncate whitespace-nowrap
                           opacity-0 group-hover/bar:opacity-100 transition-opacity duration-150">
                {{ ws.name }}
              </span>
              <span v-if="activeWs?.id === ws.id"
                class="absolute -left-2 top-1/2 -translate-y-1/2 w-1 h-5 bg-blue-300 rounded-r-full" />
            </button>

            <div class="mx-1 my-1 h-px bg-gray-300 dark:bg-neutral-700" />

            <button @click="showCreate = true" title="Workspace mới"
              class="flex items-center gap-3 px-2 py-1.5 rounded-xl overflow-hidden
                     text-gray-500 dark:text-neutral-400
                     hover:bg-green-100 dark:hover:bg-green-900/30
                     hover:text-green-600 dark:hover:text-green-400 transition-all"
            >
              <UIcon name="i-lucide-plus" class="w-5 h-5 flex-shrink-0" />
              <span class="text-sm whitespace-nowrap
                           opacity-0 group-hover/bar:opacity-100 transition-opacity duration-150">
                Workspace mới
              </span>
            </button>
          </template>
        </div>
      </template>

      <!-- Admin -->
      <NuxtLink v-if="isAuthenticated && role === 'ADMIN'" to="/admin" title="Trang Admin"
        :class="[
          'flex items-center gap-3 mx-2 px-2 py-2 rounded-xl mb-3 transition-all duration-150 overflow-hidden',
          route.path.startsWith('/admin')
            ? 'bg-primary-100 dark:bg-primary-900/40 text-primary-600 dark:text-primary-400'
            : 'text-gray-500 dark:text-neutral-400 hover:bg-gray-300 dark:hover:bg-neutral-700 hover:text-gray-900 dark:hover:text-white'
        ]"
      >
        <UIcon name="i-lucide-settings" class="w-5 h-5 flex-shrink-0" />
        <span class="text-sm whitespace-nowrap
                     opacity-0 group-hover/bar:opacity-100 transition-opacity duration-150">
          Trang Admin
        </span>
      </NuxtLink>
    </div>

    <!-- Workspace panel: absolute at left-14, appears when workspace active -->
    <Transition name="slide">
      <div v-if="activeWs"
        class="absolute left-14 top-0 h-full flex flex-col w-50
               bg-gray-100 dark:bg-neutral-800 border-r border-gray-200 dark:border-neutral-700 overflow-hidden z-[45] shadow-lg"
      >
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
            <button @click="goToWorkspace" title="Cài đặt workspace"
              class="w-6 h-6 flex items-center justify-center rounded text-gray-400 hover:text-gray-700 dark:hover:text-white hover:bg-gray-200 dark:hover:bg-neutral-700 transition-colors flex-shrink-0"
            >
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="14" height="14">
                <circle cx="12" cy="12" r="3"/>
                <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/>
              </svg>
            </button>
          </div>
        </div>

        <div class="flex-1 overflow-y-auto py-2 scrollbar-thin">
          <div class="px-3 mb-1 flex items-center justify-between">
            <span class="text-xs font-semibold text-gray-400 dark:text-neutral-500 uppercase tracking-wider">Dự án</span>
            <button @click="showCreateProject = true" title="Tạo dự án mới"
              class="w-4 h-4 flex items-center justify-center text-gray-400 hover:text-gray-700 dark:hover:text-white transition-colors text-lg leading-none"
            >+</button>
          </div>

          <div v-if="loadingProjects" class="px-3 py-2 space-y-1">
            <div v-for="i in 4" :key="i" class="h-7 rounded bg-gray-200 dark:bg-neutral-700 animate-pulse"></div>
          </div>

          <div v-else-if="projects.length === 0"
            class="px-3 py-3 text-xs text-gray-400 dark:text-neutral-500 italic">
            Chưa có dự án nào
          </div>

          <div
            v-else
            v-for="p in projects" :key="p.id"
            :class="[
              'group w-full flex items-center gap-1 px-3 py-1.5 text-sm rounded-lg mx-0 transition-colors',
              activeProjectId === p.id
                ? 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 font-medium'
                : 'text-gray-600 dark:text-neutral-300 hover:bg-gray-200 dark:hover:bg-neutral-700'
            ]"
          >
            <button @click="goToProject(p.id)" class="flex items-center gap-1.5 flex-1 min-w-0 text-left">
              <span class="text-xs opacity-60">#</span>
              <span class="truncate">{{ p.name }}</span>
              <span class="ml-auto text-xs text-gray-400 dark:text-neutral-500 flex-shrink-0">{{ p.mediaCount }}</span>
            </button>

            <button
              v-if="activeWs?.myRole === 'Admin'"
              @click.stop="confirmDeleteProject(p)"
              title="Xóa dự án"
              class="opacity-0 group-hover:opacity-100 flex-shrink-0 w-5 h-5 flex items-center justify-center rounded text-gray-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/30 transition-all"
            >
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="12" height="12">
                <path stroke-linecap="round" stroke-linejoin="round" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/>
              </svg>
            </button>
          </div>
        </div>

        <button @click="activeWs = null; emit('panel-change', false)"
          class="mx-3 mb-3 py-1.5 text-xs text-gray-400 dark:text-neutral-500 hover:text-gray-600 dark:hover:text-neutral-300 border border-gray-200 dark:border-neutral-700 rounded-lg transition-colors"
        >Thu gọn</button>
      </div>
    </Transition>

    <Teleport to="body">
      <Transition name="modal">
        <div v-if="deleteTarget"
          class="fixed inset-0 bg-black/80 backdrop-blur-md flex items-center justify-center p-4"
          style="z-index: 99999;"
          @click.self="deleteTarget = null"
        >
          <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl p-6 w-full max-w-sm shadow-2xl" @click.stop>
            <div class="flex items-center gap-3 mb-4">
              <div class="w-10 h-10 rounded-full bg-red-100 dark:bg-red-900/30 flex items-center justify-center flex-shrink-0">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="20" height="20" class="text-red-600 dark:text-red-400">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"/>
                </svg>
              </div>
              <div>
                <h3 class="font-bold text-gray-900 dark:text-white text-sm">Xóa dự án</h3>
                <p class="text-xs text-gray-500 dark:text-gray-400 mt-0.5">Hành động này không thể hoàn tác</p>
              </div>
            </div>
            <p class="text-sm text-gray-700 dark:text-gray-300 mb-5">
              Bạn có chắc muốn xóa dự án <span class="font-semibold text-red-600 dark:text-red-400">{{ deleteTarget?.name }}</span>?
            </p>
            <div class="flex justify-end gap-2">
              <button @click="deleteTarget = null"
                class="px-4 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors">
                Hủy
              </button>
              <button @click="handleDeleteProject" :disabled="deletingProject"
                class="px-4 py-2 text-sm rounded-lg bg-red-600 hover:bg-red-700 disabled:opacity-50 text-white font-semibold transition-colors flex items-center gap-2">
                <svg v-if="deletingProject" class="w-3.5 h-3.5 animate-spin" fill="none" viewBox="0 0 24 24">
                  <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/>
                  <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"/>
                </svg>
                {{ deletingProject ? 'Đang xóa...' : 'Xóa' }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <Teleport to="body">
      <Transition name="modal">
        <div v-if="showCreate"
          class="fixed inset-0 bg-black/80 backdrop-blur-md flex items-center justify-center p-4"
          style="z-index: 99999;"
          @click.self="showCreate = false"
        >
          <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl p-8 w-full max-w-md shadow-2xl" @click.stop>
            <h2 class="text-xl font-bold text-gray-900 dark:text-white mb-6">Tạo Workspace mới</h2>
            <div class="space-y-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">
                  Tên workspace <span class="text-red-500">*</span>
                </label>
                <input v-model="wsForm.name" @keyup.enter="handleCreateWs"
                  class="w-full bg-gray-50 dark:bg-gray-900 border border-gray-300 dark:border-gray-600 text-gray-900 dark:text-white rounded-lg px-3.5 py-2.5 text-sm outline-none focus:border-blue-500 transition-colors placeholder-gray-400 dark:placeholder-gray-500"
                  placeholder="VD: FPT Software" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Mô tả</label>
                <textarea v-model="wsForm.description" rows="2"
                  class="w-full bg-gray-50 dark:bg-gray-900 border border-gray-300 dark:border-gray-600 text-gray-900 dark:text-white rounded-lg px-3.5 py-2.5 text-sm outline-none focus:border-blue-500 transition-colors placeholder-gray-400 dark:placeholder-gray-500 resize-none"
                  placeholder="Mô tả ngắn..."></textarea>
              </div>
            </div>
            <div class="flex justify-end gap-3 mt-6">
              <button @click="showCreate = false"
                class="px-4 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors">Hủy</button>
              <button @click="handleCreateWs" :disabled="!wsForm.name.trim() || creatingWs"
                class="px-5 py-2 text-sm rounded-lg bg-yellow-400 hover:bg-yellow-500 disabled:opacity-50 text-gray-900 font-semibold transition-colors">
                {{ creatingWs ? 'Đang tạo...' : 'Tạo' }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

    <Teleport to="body">
      <Transition name="modal">
        <div v-if="showCreateProject"
          class="fixed inset-0 bg-black/80 backdrop-blur-md flex items-center justify-center p-4"
          style="z-index: 99999;"
          @click.self="showCreateProject = false"
        >
          <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl p-8 w-full max-w-md shadow-2xl" @click.stop>
            <h2 class="text-xl font-bold text-gray-900 dark:text-white mb-6">Tạo dự án mới</h2>
            <div class="space-y-4">
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">
                  Tên dự án <span class="text-red-500">*</span>
                </label>
                <input v-model="projectForm.name" @keyup.enter="handleCreateProject"
                  class="w-full bg-gray-50 dark:bg-gray-900 border border-gray-300 dark:border-gray-600 text-gray-900 dark:text-white rounded-lg px-3.5 py-2.5 text-sm outline-none focus:border-blue-500 transition-colors placeholder-gray-400 dark:placeholder-gray-500"
                  placeholder="VD: Dự án ngân hàng ABC" />
              </div>
              <div>
                <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Mô tả</label>
                <textarea v-model="projectForm.description" rows="2"
                  class="w-full bg-gray-50 dark:bg-gray-900 border border-gray-300 dark:border-gray-600 text-gray-900 dark:text-white rounded-lg px-3.5 py-2.5 text-sm outline-none focus:border-blue-500 transition-colors placeholder-gray-400 dark:placeholder-gray-500 resize-none"
                  placeholder="Mô tả ngắn..."></textarea>
              </div>
            </div>
            <div class="flex justify-end gap-3 mt-6">
              <button @click="showCreateProject = false"
                class="px-4 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors">Hủy</button>
              <button @click="handleCreateProject" :disabled="!projectForm.name.trim() || creatingProject"
                class="px-5 py-2 text-sm rounded-lg bg-yellow-400 hover:bg-yellow-500 disabled:opacity-50 text-gray-900 font-semibold transition-colors">
                {{ creatingProject ? 'Đang tạo...' : 'Tạo' }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

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
const { jwt, isAuthenticated, role } = useJwt()

const navItems = [
  { to: '/search',   icon: 'i-lucide-search',         label: 'Tra cứu' },
  { to: '/alphabet', icon: 'i-custom-alphabet',        label: 'Bảng chữ cái' },
  { to: '/kanji',    icon: 'i-custom-kanji',           label: 'Hán tự' },
  { to: '/explore',  icon: 'i-lucide-layers',          label: 'Flashcard' },
  { to: '/notebook', icon: 'i-lucide-notebook-pen',    label: 'Sổ tay Dự án' },
  // { to: '/sensei', icon: 'i-lucide-message-circle', label: 'Hội thoại' }, // chưa phát triển
  { to: '/reading',  icon: 'i-lucide-book-open',       label: 'Luyện đọc' },
]
const config = useRuntimeConfig()

const emit = defineEmits<{ 'panel-change': [isOpen: boolean] }>()

watch(jwt, (newVal) => {
  if (newVal) {
    load() // Reload workspaces sau khi đăng nhập
  } else {
    workspaces.value = []
    activeWs.value = null
    projects.value = []
    emit('panel-change', false)
  }
}, { flush: 'post' })

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

const deleteTarget = ref<any>(null)
const deletingProject = ref(false)

// 🪄 HÀM LOAD THÔNG MINH: Có tham số 'silent' để không bị chớp giật loading khi tải ngầm
async function load(silent = false) {
  const token = jwt.value || (process.client ? localStorage.getItem('jwt_token') : null)
  if (!token) return
  
  try {
    if (!silent) loading.value = true
    const freshWorkspaces = await getMyWorkspaces()
    workspaces.value = freshWorkspaces

    // Đồng bộ trạng thái của Workspace đang mở (phòng khi bạn vừa sửa/xóa nó ở trang chi tiết)
    if (activeWs.value) {
      const stillExists = freshWorkspaces.find((w: any) => w.id === activeWs.value.id)
      if (stillExists) {
        activeWs.value = stillExists // Cập nhật tên mới
      } else {
        // Workspace đã bị xóa mất tiêu -> dọn dẹp panel
        activeWs.value = null
        emit('panel-change', false)
      }
    }
  } catch (e) {
    console.error(e)
  } finally {
    if (!silent) loading.value = false
  }
}

// 🪄 WATCHER "THẦN THÁNH": Theo dõi mỗi khi bạn chuyển URL
watch(() => route.path, (newPath) => {
  // Khi bạn từ trang xóa/sửa quay về, hàm này tự động gọi API lấy lại ds Workspace mới nhất
  load(true) 

  // Kéo theo update trạng thái bôi xanh Project đang chọn
  const match = newPath.match(/\/project\/(\d+)/)
  activeProjectId.value = match ? parseInt(match[1]) : null
}, { immediate: true })


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

function goToWorkspace() {
  if (activeWs.value) router.push(`/workspaces/${activeWs.value.id}`)
}

function goToProject(projectId: number) {
  activeProjectId.value = projectId
  router.push(`/workspaces/project/${projectId}`)
}

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

function confirmDeleteProject(p: any) {
  deleteTarget.value = p
}

async function handleDeleteProject() {
  if (!deleteTarget.value || deletingProject.value) return
  const token = localStorage.getItem('jwt_token') || jwt.value
  deletingProject.value = true
  try {
    const res = await fetch(
      `${config.public.apiBaseUrl}/api/projects/${deleteTarget.value.id}`,
      { method: 'DELETE', headers: { Authorization: `Bearer ${token}` } }
    )
    if (res.ok || res.status === 204) {
      projects.value = projects.value.filter(p => p.id !== deleteTarget.value.id)
      if (activeProjectId.value === deleteTarget.value.id) {
        activeProjectId.value = null
        router.push(`/workspaces/${activeWs.value?.id}`)
      }
      deleteTarget.value = null
    }
  } catch (e) {
    console.error(e)
  } finally {
    deletingProject.value = false
  }
}

const { $bus } = useNuxtApp();

onMounted(() => {
  load(); // Lần đầu load trang
  
  // 🔥 Lắng nghe sự kiện từ AppNavBar
  $bus.on('workspace-updated', () => {
    load(true);
  });
});

// Nhớ dọn dẹp khi unmount
onUnmounted(() => {
  $bus.off('workspace-updated');
});
</script>

<style scoped>
.slide-enter-active, .slide-leave-active {
  transition: max-width 0.15s ease, opacity 0.15s ease;
  overflow: hidden;
}
.slide-enter-from, .slide-leave-to { max-width: 0; opacity: 0; }
.slide-enter-to, .slide-leave-from { max-width: 200px; }

.modal-enter-active, .modal-leave-active { transition: opacity 0.2s ease; }
.modal-enter-from, .modal-leave-to { opacity: 0; }

.scrollbar-thin { scrollbar-width: thin; scrollbar-color: rgba(0,0,0,0.15) transparent; }
.scrollbar-thin::-webkit-scrollbar { width: 3px; }
.scrollbar-thin::-webkit-scrollbar-thumb { background: rgba(0,0,0,0.15); border-radius: 3px; }
</style>