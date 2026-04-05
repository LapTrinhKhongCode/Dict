<template>
  <div class="bg-white dark:bg-[#161b22] border border-gray-200 dark:border-[#30363d] rounded-xl shadow-sm overflow-hidden">
    <div class="p-5 border-b border-gray-200 dark:border-[#30363d] flex justify-between items-center bg-gray-50 dark:bg-[#0d1117]">
      <div>
        <h2 class="text-lg font-bold text-gray-900 dark:text-white">Quản lý Workspace & Project</h2>
        <p class="text-sm text-gray-500 dark:text-[#8b949e] mt-1">Kiểm duyệt và xóa các không gian làm việc hoặc dự án vi phạm nội quy.</p>
      </div>
      <button @click="fetchWorkspaces" class="p-2 text-gray-500 hover:text-blue-600 dark:text-[#8b949e] dark:hover:text-[#58a6ff] transition-colors" title="Làm mới">
        <svg class="w-5 h-5" :class="{'animate-spin': loading}" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" /></svg>
      </button>
    </div>

    <div v-if="loading" class="p-8 text-center text-gray-500 dark:text-[#8b949e]">
      <svg class="w-8 h-8 animate-spin mx-auto mb-3 text-blue-500" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"/></svg>
      Đang tải dữ liệu...
    </div>

    <div v-else class="divide-y divide-gray-200 dark:divide-[#30363d]">
      <div v-if="workspaces.length === 0" class="p-8 text-center text-gray-500 dark:text-[#8b949e]">
        Không tìm thấy Workspace nào trên hệ thống.
      </div>

      <div v-for="ws in workspaces" :key="ws.id" class="flex flex-col">
        <div class="flex items-center justify-between p-4 hover:bg-gray-50 dark:hover:bg-[#21262d] transition-colors group cursor-pointer" @click="toggleWorkspace(ws.id)">
          <div class="flex items-center gap-3 flex-1 min-w-0">
            <button class="text-gray-400 dark:text-[#8b949e] focus:outline-none transition-transform" :class="{ 'rotate-90': expandedWs === ws.id }">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" /></svg>
            </button>
            <div class="w-10 h-10 rounded bg-indigo-100 dark:bg-indigo-900/30 text-indigo-600 dark:text-indigo-400 flex items-center justify-center flex-shrink-0">
              <svg class="w-5 h-5" fill="currentColor" viewBox="0 0 20 20"><path d="M4 4a2 2 0 00-2 2v4a2 2 0 002 2V6h10a2 2 0 00-2-2H4zm2 6a2 2 0 012-2h8a2 2 0 012 2v4a2 2 0 01-2 2H8a2 2 0 01-2-2v-4zm6 4a2 2 0 100-4 2 2 0 000 4z" /></svg>
            </div>
            <div class="flex flex-col min-w-0">
              <span class="font-bold text-gray-900 dark:text-white truncate">{{ ws.name }}</span>
              <span class="text-xs text-gray-500 dark:text-[#8b949e]">Quản lý bởi: {{ ws.ownerName }} • {{ ws.memberCount }} thành viên • {{ formatDate(ws.createdAt) }}</span>
            </div>
          </div>
          
          <div class="flex items-center gap-3 ml-4">
            <span class="text-xs font-medium px-2.5 py-1 rounded-full bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400 border border-blue-200 dark:border-blue-800">
              {{ ws.projectCount || 0 }} Projects
            </span>
            <button @click.stop="promptDeleteWorkspace(ws)" class="p-2 text-gray-400 hover:text-red-600 dark:text-[#8b949e] dark:hover:text-red-400 rounded-lg hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors" title="Xóa Workspace vi phạm">
              <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" /></svg>
            </button>
          </div>
        </div>

        <div v-show="expandedWs === ws.id" class="bg-gray-50 dark:bg-[#0d1117] border-t border-gray-100 dark:border-[#30363d] px-8 py-4">
          <div v-if="loadingProjects" class="text-sm text-gray-500 dark:text-[#8b949e] flex items-center gap-2 px-4">
            <svg class="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"/></svg>
            Đang lấy danh sách dự án...
          </div>
          
          <div v-else-if="!wsProjects[ws.id] || wsProjects[ws.id].length === 0" class="text-sm text-gray-500 dark:text-[#8b949e] py-2 px-4">
            Workspace này chưa có dự án nào.
          </div>

          <div v-else class="space-y-3">
            <div v-for="project in wsProjects[ws.id]" :key="project.id" class="bg-white dark:bg-[#161b22] border border-gray-200 dark:border-[#30363d] rounded-lg shadow-sm overflow-hidden">
              
              <div class="flex items-center justify-between p-3 hover:bg-gray-50 dark:hover:bg-[#21262d] cursor-pointer transition-colors" @click="toggleProject(project.id)">
                <div class="flex items-center gap-3 min-w-0 pr-4">
                  <button class="text-gray-400 dark:text-[#8b949e] transition-transform" :class="{ 'rotate-90': expandedProject === project.id }">
                    <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5l7 7-7 7" /></svg>
                  </button>
                  <div class="flex flex-col min-w-0">
                    <span class="text-sm font-bold text-gray-800 dark:text-[#c9d1d9] truncate">{{ project.name }}</span>
                    <span class="text-xs text-gray-500 dark:text-[#8b949e] truncate">{{ project.description || 'Không có mô tả' }}</span>
                  </div>
                </div>
                <div class="flex items-center gap-4 flex-shrink-0">
                  <span class="text-xs text-gray-500 dark:text-[#8b949e]">{{ formatDate(project.createdAt) }}</span>
                  <button @click.stop="promptDeleteProject(ws.id, project)" class="text-xs px-3 py-1.5 bg-white dark:bg-transparent border border-red-200 dark:border-red-900/50 text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 rounded transition-colors font-medium">
                    Xóa Project
                  </button>
                </div>
              </div>

              <div v-show="expandedProject === project.id" class="bg-gray-50 dark:bg-[#0d1117] border-t border-gray-100 dark:border-[#30363d] p-3 pl-10">
                <div v-if="loadingFiles" class="text-xs text-gray-500 dark:text-[#8b949e] flex items-center gap-2">
                  <svg class="w-3 h-3 animate-spin" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"/></svg>
                  Đang tải file...
                </div>
                
                <div v-else-if="!projectFiles[project.id] || projectFiles[project.id].length === 0" class="text-xs text-gray-500 dark:text-[#8b949e]">
                  Dự án này chưa có tài liệu nào.
                </div>

                <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-2">
                  <div v-for="file in projectFiles[project.id]" :key="file.id" 
                       @click="goToReader(file, project.id)"
                       class="flex items-center gap-2 p-2 bg-white dark:bg-[#161b22] border border-gray-200 dark:border-[#30363d] rounded cursor-pointer hover:border-blue-400 dark:hover:border-[#58a6ff] transition-colors group">
                    
                    <div class="w-6 h-6 rounded flex items-center justify-center flex-shrink-0" :class="file.type === 'pdf' ? 'bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400' : 'bg-gray-200 dark:bg-[#30363d] text-gray-700 dark:text-[#c9d1d9]'">
                      <svg v-if="file.type === 'pdf'" class="w-3 h-3" fill="currentColor" viewBox="0 0 24 24"><path d="M4 5a2 2 0 012-2h8.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V19a2 2 0 01-2 2H6a2 2 0 01-2-2V5zm10 1V4.5l3.5 3.5H16a2 2 0 01-2-2z"/></svg>
                      <span v-else class="text-[7px] font-black uppercase">{{ file.type || 'IMG' }}</span>
                    </div>
                    
                    <div class="flex flex-col min-w-0 flex-1">
                      <span class="text-xs font-medium text-gray-800 dark:text-[#c9d1d9] truncate group-hover:text-blue-600 dark:group-hover:text-[#58a6ff]">{{ file.name }}</span>
                    </div>
                    
                    <svg class="w-3 h-3 text-gray-400 opacity-0 group-hover:opacity-100 transition-opacity" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" /></svg>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <Teleport to="body">
      <Transition name="fade">
        <div v-if="confirmDialog.isOpen" class="fixed inset-0 z-[100] flex items-center justify-center bg-black/50 backdrop-blur-sm p-4" @click.self="closeConfirmDialog">
          <div class="bg-white dark:bg-[#161b22] border border-gray-200 dark:border-[#30363d] p-6 rounded-2xl w-full max-w-sm shadow-2xl transform transition-all scale-100">
            
            <div class="flex items-center justify-center w-12 h-12 mx-auto bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400 rounded-full mb-4">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" /></svg>
            </div>
            
            <h3 class="text-lg font-bold text-center text-gray-900 dark:text-white mb-2">{{ confirmDialog.title }}</h3>
            <p class="text-sm text-center text-gray-500 dark:text-[#8b949e] mb-6">
              {{ confirmDialog.message }}
            </p>
            
            <div class="flex justify-center gap-3">
              <button @click="closeConfirmDialog" class="px-4 py-2 rounded-lg text-sm font-medium text-gray-600 dark:text-gray-300 bg-gray-100 dark:bg-[#21262d] hover:bg-gray-200 dark:hover:bg-[#30363d] transition-colors w-full">Hủy</button>
              <button @click="executeConfirmAction" :disabled="isDeleting" class="px-4 py-2 rounded-lg bg-red-600 text-white text-sm font-bold hover:bg-red-700 shadow-sm disabled:opacity-50 transition-all w-full flex justify-center items-center gap-2">
                <svg v-if="isDeleting" class="w-4 h-4 animate-spin text-white" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"/></svg>
                {{ isDeleting ? 'Đang xóa...' : 'Xóa vĩnh viễn' }}
              </button>
            </div>
          </div>
        </div>
      </Transition>
    </Teleport>

  </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useRuntimeConfig } from '#imports'
import { useToast } from '~/composables/useToast'
import { useJwt } from '~/composables/useJwt'

const config = useRuntimeConfig()
const router = useRouter()
const { showToast } = useToast()
const { jwt } = useJwt()

const getToken = () => localStorage.getItem('jwt_token') || jwt.value || ''

// States Workspace
const loading = ref(false)
const workspaces = ref([])
const expandedWs = ref(null)

// States Project
const loadingProjects = ref(false)
const wsProjects = ref({}) 
const expandedProject = ref(null)

// States File
const loadingFiles = ref(false)
const projectFiles = ref({}) 

// State Modal Custom Confirm
const confirmDialog = reactive({
  isOpen: false,
  title: '',
  message: '',
  actionData: null 
})
const isDeleting = ref(false)

onMounted(() => {
  fetchWorkspaces()
})

// Lấy danh sách Workspace
async function fetchWorkspaces() {
  loading.value = true
  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/Admin/workspaces`, {
      headers: { Authorization: `Bearer ${getToken()}` }
    })
    if (!res.ok) throw new Error('Không thể tải danh sách Workspace')
    const data = await res.json()
    workspaces.value = Array.isArray(data) ? data : (data.result || data.data || [])
  } catch (error) {
    showToast(error.message, 'error')
  } finally {
    loading.value = false
  }
}

// Xổ/Đóng Workspace
async function toggleWorkspace(wsId) {
  if (expandedWs.value === wsId) {
    expandedWs.value = null
    return
  }
  expandedWs.value = wsId
  if (wsProjects.value[wsId]) return

  loadingProjects.value = true
  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/Admin/workspaces/${wsId}/projects`, {
      headers: { Authorization: `Bearer ${getToken()}` }
    })
    if (!res.ok) throw new Error('Không thể tải danh sách Projects')
    const data = await res.json()
    wsProjects.value[wsId] = Array.isArray(data) ? data : (data.result || data.data || [])
  } catch (error) {
    showToast("Lỗi tải Projects", 'error')
  } finally {
    loadingProjects.value = false
  }
}

// Xổ/Đóng Project để lấy Files
async function toggleProject(projectId) {
  if (expandedProject.value === projectId) {
    expandedProject.value = null
    return
  }
  expandedProject.value = projectId
  if (projectFiles.value[projectId]) return

  loadingFiles.value = true
  try {
    // API lấy file của project (Admin có thể dùng chung api này nếu Role phù hợp, hoặc đổi thành API riêng của Admin)
    const res = await fetch(`${config.public.apiBaseUrl}/api/projects/${projectId}/files`, {
      headers: { Authorization: `Bearer ${getToken()}` }
    })
    if (!res.ok) throw new Error('Không tải được tài liệu')
    const data = await res.json()
    projectFiles.value[projectId] = Array.isArray(data) ? data : (data.result || data.data || [])
  } catch (error) {
    showToast("Lỗi tải Files", 'error')
  } finally {
    loadingFiles.value = false
  }
}

// Điều hướng sang trang Reader
function goToReader(file, projectId) {
  const name = encodeURIComponent(file.name || '')
  router.push(`/reader?jobId=${file.id}&projectId=${projectId}&name=${name}`)
}

// --- LOGIC MODAL CONFIRM ---
function closeConfirmDialog() {
  if (isDeleting.value) return
  confirmDialog.isOpen = false
  confirmDialog.actionData = null
}

function promptDeleteWorkspace(ws) {
  confirmDialog.title = `Xóa Workspace`
  confirmDialog.message = `Bạn có chắc muốn xóa "${ws.name}" và TẤT CẢ dự án bên trong? Hành động này không thể hoàn tác.`
  confirmDialog.actionData = { type: 'workspace', target: ws }
  confirmDialog.isOpen = true
}

function promptDeleteProject(wsId, project) {
  confirmDialog.title = `Xóa Project`
  confirmDialog.message = `Bạn đang chuẩn bị xóa dự án "${project.name}". Hành động này không thể hoàn tác.`
  confirmDialog.actionData = { type: 'project', wsId, target: project }
  confirmDialog.isOpen = true
}

async function executeConfirmAction() {
  if (!confirmDialog.actionData) return
  isDeleting.value = true
  
  const { type, wsId, target } = confirmDialog.actionData
  
  try {
    if (type === 'workspace') {
      const res = await fetch(`${config.public.apiBaseUrl}/api/Admin/workspaces/${target.id}`, {
        method: 'DELETE',
        headers: { Authorization: `Bearer ${getToken()}` }
      })
      if (!res.ok) throw new Error('Xóa thất bại')
      
      workspaces.value = workspaces.value.filter(w => w.id !== target.id)
      if (expandedWs.value === target.id) expandedWs.value = null
      showToast(`Đã xóa Workspace: ${target.name}`, 'success')

    } else if (type === 'project') {
      const res = await fetch(`${config.public.apiBaseUrl}/api/Admin/projects/${target.id}`, {
        method: 'DELETE',
        headers: { Authorization: `Bearer ${getToken()}` }
      })
      if (!res.ok) throw new Error('Xóa thất bại')
      
      if (wsProjects.value[wsId]) {
        wsProjects.value[wsId] = wsProjects.value[wsId].filter(p => p.id !== target.id)
      }
      const wsIndex = workspaces.value.findIndex(w => w.id === wsId)
      if (wsIndex !== -1 && workspaces.value[wsIndex].projectCount > 0) {
        workspaces.value[wsIndex].projectCount -= 1
      }
      showToast(`Đã xóa Project: ${target.name}`, 'success')
    }
  } catch (error) {
    showToast(error.message, 'error')
  } finally {
    isDeleting.value = false
    closeConfirmDialog()
  }
}

function formatDate(d) {
  if (!d) return ''
  return new Date(d).toLocaleDateString('vi-VN', { year: 'numeric', month: 'short', day: 'numeric' })
}
</script>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>