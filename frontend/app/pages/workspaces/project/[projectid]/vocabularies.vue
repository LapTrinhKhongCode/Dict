<template>
  <div class="min-h-screen bg-transparent text-[#c9d1d9]">
    <main class="max-w-[1000px] mx-auto px-6 py-8">

      <!-- HEADER -->
      <div class="flex items-center gap-3 mb-6">
        <button
          @click="$router.back()"
          class="p-2 rounded-full bg-gray-200 dark:bg-[#161b22] hover:bg-gray-300 dark:hover:bg-[#30363d] text-gray-700 dark:text-gray-400 transition-all border border-gray-300 dark:border-[#30363d]"
        >
          <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18"/>
          </svg>
        </button>

        <div>
          <div class="flex items-center gap-2">
            <svg class="w-5 h-5 text-blue-500" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.75 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"/>
            </svg>
            <h1 class="text-xl font-bold text-gray-900 dark:text-white">Từ vựng dự án</h1>
            <span class="text-xs bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-300 px-2 py-0.5 rounded-full font-medium">{{ vocabs.length }}</span>
          </div>
          <p v-if="projectName" class="text-sm text-gray-500 mt-0.5">{{ projectName }}</p>
        </div>

        <div class="ml-auto flex gap-2">
          <!-- Search -->
          <div class="relative">
            <input
              v-model="search"
              type="text"
              placeholder="Tìm từ vựng..."
              class="text-sm bg-white dark:bg-[#161b22] border border-gray-300 dark:border-[#30363d] text-gray-900 dark:text-white rounded-xl pl-9 pr-3 py-2 focus:outline-none focus:border-blue-500 w-44 transition-all"
            />
            <svg class="absolute left-2.5 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
            </svg>
          </div>

          <button
            v-if="canManage"
            @click="showAddModal = true"
            class="flex items-center gap-1.5 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-xl transition-colors"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M12 4v16m8-8H4"/>
            </svg>
            Thêm từ
          </button>
        </div>
      </div>

      <!-- CARD -->
      <div class="bg-white dark:bg-[#161b22] border border-gray-200 dark:border-[#30363d] rounded-2xl">

        <!-- Loading -->
        <div v-if="loading" class="flex justify-center py-16">
          <div class="w-7 h-7 border-2 border-blue-400/30 border-t-blue-500 rounded-full animate-spin"></div>
        </div>

        <!-- Empty -->
        <div v-else-if="filtered.length === 0" class="text-center py-16 text-gray-400 dark:text-gray-500">
          <svg class="w-12 h-12 mx-auto mb-3 opacity-30" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.75 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"/>
          </svg>
          <p class="text-sm">{{ search ? 'Không tìm thấy từ vựng phù hợp.' : 'Chưa có từ vựng nào.' }}</p>
          <p v-if="!search && canManage" class="text-xs mt-1">Nhấn <b>Thêm từ</b> để bắt đầu!</p>
        </div>

        <!-- Table -->
        <div v-else class="overflow-x-auto">
          <!-- Bulk action bar -->
          <div v-if="selected.length > 0 && canManage" class="flex items-center gap-3 px-5 py-3 bg-red-50 dark:bg-red-900/10 border-b border-red-200 dark:border-red-800/30 rounded-t-2xl">
            <span class="text-sm text-red-600 dark:text-red-400 font-medium">Đã chọn {{ selected.length }} từ</span>
            <button @click="deleteMany" :disabled="deleting" class="px-3 py-1.5 bg-red-600 hover:bg-red-700 disabled:opacity-50 text-white text-sm font-medium rounded-lg transition-colors flex items-center gap-1.5">
              <span v-if="deleting" class="w-3.5 h-3.5 border-2 border-white/30 border-t-white rounded-full animate-spin"></span>
              Xóa đã chọn
            </button>
            <button @click="selected = []" class="text-sm text-gray-500 hover:text-gray-700 dark:hover:text-gray-300">Bỏ chọn</button>
          </div>

          <table class="w-full text-sm">
            <thead>
              <tr class="border-b border-gray-200 dark:border-[#30363d] bg-gray-50 dark:bg-[#0d1117]">
                <th v-if="canManage" class="py-3 px-4 w-10 rounded-tl-2xl">
                  <input type="checkbox" @change="e => selected = e.target.checked ? filtered.map(v => v.id) : []" :checked="selected.length === filtered.length && filtered.length > 0" class="rounded" />
                </th>
                <th class="text-left py-3 px-4 font-semibold text-gray-500 dark:text-gray-400">Từ vựng</th>
                <th class="text-left py-3 px-4 font-semibold text-gray-500 dark:text-gray-400">Nghĩa / Ghi chú</th>
                <th class="text-left py-3 px-4 font-semibold text-gray-500 dark:text-gray-400 hidden sm:table-cell">Người thêm</th>
                <th class="text-left py-3 px-4 font-semibold text-gray-500 dark:text-gray-400 hidden md:table-cell">Ngày thêm</th>
                <th v-if="canManage" class="py-3 px-4 w-20 rounded-tr-2xl"></th>
              </tr>
            </thead>
            <tbody>
              <tr
                v-for="v in filtered"
                :key="v.id"
                class="border-b border-gray-100 dark:border-[#21262d] hover:bg-gray-50 dark:hover:bg-[#0d1117]/50 transition-colors group"
              >
                <td v-if="canManage" class="py-3 px-4">
                  <input type="checkbox" :value="v.id" v-model="selected" class="rounded" />
                </td>
                <td class="py-3 px-4">
                  <span class="font-semibold text-lg text-gray-900 dark:text-white">{{ v.wordText }}</span>
                </td>
                <td class="py-3 px-4 text-gray-600 dark:text-gray-300">
                  <template v-if="editingId === v.id">
                    <div class="flex gap-1.5 items-center">
                      <input
                        v-model="editingVal"
                        @keyup.enter="saveEdit(v)"
                        @keyup.escape="editingId = null"
                        class="flex-1 text-sm bg-white dark:bg-[#0d1117] border border-blue-500 rounded-lg px-3 py-1.5 focus:outline-none text-gray-900 dark:text-white"
                        autofocus
                      />
                      <button @click="saveEdit(v)" class="text-green-500 hover:text-green-600 p-1">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/></svg>
                      </button>
                      <button @click="editingId = null" class="text-gray-400 hover:text-gray-600 p-1">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M6 18L18 6M6 6l12 12"/></svg>
                      </button>
                    </div>
                  </template>
                  <template v-else>
                    <span :class="v.contextMeaning ? '' : 'text-gray-400 dark:text-gray-600 italic'">{{ v.contextMeaning || 'Chưa có nghĩa' }}</span>
                  </template>
                </td>
                <td class="py-3 px-4 text-gray-400 text-xs hidden sm:table-cell">{{ v.addedByName || '—' }}</td>
                <td class="py-3 px-4 text-gray-400 text-xs hidden md:table-cell">{{ formatDate(v.createdAt) }}</td>
                <td v-if="canManage" class="py-3 px-4">
                  <div class="flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity justify-end">
                    <button @click="startEdit(v)" title="Sửa nghĩa" class="p-1.5 text-amber-500 hover:bg-amber-50 dark:hover:bg-amber-900/20 rounded-lg transition-colors">
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"/></svg>
                    </button>
                    <button @click="deleteOne(v.id)" title="Xóa" class="p-1.5 text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 rounded-lg transition-colors">
                      <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/></svg>
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </main>

    <!-- MODAL THÊM TỪ VỰNG -->
    <Transition name="fade">
      <div v-if="showAddModal" class="fixed inset-0 z-[9999] flex items-center justify-center bg-black/70 backdrop-blur-md px-4" @click.self="showAddModal = false">
        <div class="bg-[#161b22] border border-[#30363d] p-6 rounded-2xl shadow-2xl w-full max-w-md">
          <h3 class="text-lg font-bold text-white mb-4">Thêm từ vựng</h3>
          <div class="space-y-3 mb-5">
            <div>
              <label class="block text-xs text-gray-400 mb-1.5">Từ vựng (tiếng Nhật)</label>
              <input
                v-model="newWord"
                type="text"
                placeholder="e.g. 食べる"
                @keyup.enter="$refs.meaningInput.focus()"
                class="w-full bg-[#0d1117] border border-[#30363d] text-white rounded-xl px-4 py-2.5 focus:outline-none focus:border-blue-500 transition-all"
                autofocus
              />
            </div>
            <div>
              <label class="block text-xs text-gray-400 mb-1.5">Nghĩa / Ghi chú</label>
              <input
                ref="meaningInput"
                v-model="newMeaning"
                type="text"
                placeholder="e.g. ăn (động từ nhóm 2)"
                @keyup.enter="addVocab"
                class="w-full bg-[#0d1117] border border-[#30363d] text-white rounded-xl px-4 py-2.5 focus:outline-none focus:border-blue-500 transition-all"
              />
            </div>
          </div>
          <div class="flex gap-3">
            <button @click="showAddModal = false; newWord = ''; newMeaning = ''" class="flex-1 px-5 py-2.5 bg-[#21262d] text-white rounded-xl hover:bg-[#30363d] transition-all">Hủy</button>
            <button @click="addVocab" :disabled="!newWord.trim() || saving"
              class="flex-1 px-5 py-2.5 bg-blue-600 text-white font-bold rounded-xl hover:bg-blue-700 disabled:opacity-50 flex items-center justify-center gap-2">
              <span v-if="saving" class="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></span>
              Lưu từ vựng
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

import { ref, reactive, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useJwt } from '~/composables/useJwt'

const route = useRoute()
const router = useRouter()
const config = useRuntimeConfig()
const { userId: currentUserId, isAuthenticated, role: systemRole } = useJwt()

const projectId = route.params.projectid
const projectName = ref('')
const vocabs = ref([])
const loading = ref(false)
const search = ref('')
const selected = ref([])
const deleting = ref(false)
const editingId = ref(null)
const editingVal = ref('')
const showAddModal = ref(false)
const newWord = ref('')
const newMeaning = ref('')
const saving = ref(false)
const isAdmin = ref(false)
const toast = reactive({ visible: false, message: '', type: 'success' })

const getToken = () => localStorage.getItem('jwt_token') || ''

// Chỉ workspace ADMIN hoặc system ADMIN/MODERATOR mới được CRUD
const canManage = computed(() =>
  isAdmin.value ||
  systemRole.value === 'ADMIN' ||
  systemRole.value === 'MODERATOR'
)

const filtered = computed(() => {
  if (!search.value.trim()) return vocabs.value
  const q = search.value.toLowerCase()
  return vocabs.value.filter(v =>
    v.wordText.toLowerCase().includes(q) ||
    (v.contextMeaning || '').toLowerCase().includes(q)
  )
})

function showToast(message, type = 'success') {
  toast.message = message
  toast.type = type
  toast.visible = true
  setTimeout(() => { toast.visible = false }, 3000)
}

function formatDate(iso) {
  return new Date(iso).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

async function init() {
  const token = getToken()
  if (!token || !isAuthenticated.value) return router.push('/login')

  try {
    // Lấy tên project + kiểm tra quyền
    const projRes = await fetch(`${config.public.apiBaseUrl}/api/projects/${projectId}`, {
      headers: { Authorization: `Bearer ${token}` }
    })
    if (!projRes.ok) return router.back()
    const projData = await projRes.json()
    const proj = projData.result || projData
    projectName.value = proj.name

    const membersRes = await fetch(`${config.public.apiBaseUrl}/api/workspaces/${proj.workspaceId}/members`, {
      headers: { Authorization: `Bearer ${token}` }
    })
    const membersData = await membersRes.json()
    const members = membersData.result || membersData
    const me = members.find(m => m.userId === currentUserId.value)
    isAdmin.value = me?.role === 'Admin'
  } catch {}

  await fetchVocabs()
}

async function fetchVocabs() {
  const token = getToken()
  loading.value = true
  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/projects/${projectId}/vocabularies`, {
      headers: { Authorization: `Bearer ${token}` }
    })
    if (res.ok) {
      const data = await res.json()
      vocabs.value = data.result || data
    }
  } catch (e) {
    showToast('Lỗi tải dữ liệu', 'error')
  } finally {
    loading.value = false
  }
}

async function addVocab() {
  if (!newWord.value.trim() || saving.value) return
  const token = getToken()
  saving.value = true
  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/projects/${projectId}/vocabularies`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      body: JSON.stringify({ wordText: newWord.value.trim(), contextMeaning: newMeaning.value.trim() })
    })
    if (res.ok) {
      const saved = (await res.json()).result
      const idx = vocabs.value.findIndex(v => v.id === saved.id)
      if (idx >= 0) vocabs.value[idx] = saved
      else vocabs.value.unshift(saved)
      showToast('Đã lưu từ vựng!')
      showAddModal.value = false
      newWord.value = ''
      newMeaning.value = ''
    } else {
      const err = await res.json()
      showToast(err.message || 'Lỗi lưu từ vựng', 'error')
    }
  } catch {
    showToast('Lỗi kết nối', 'error')
  } finally {
    saving.value = false
  }
}

function startEdit(v) {
  editingId.value = v.id
  editingVal.value = v.contextMeaning || ''
}

async function saveEdit(v) {
  const token = getToken()
  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/projects/${projectId}/vocabularies/${v.id}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
      body: JSON.stringify(editingVal.value)
    })
    if (res.ok) {
      v.contextMeaning = editingVal.value
      editingId.value = null
      showToast('Đã cập nhật nghĩa!')
    } else {
      showToast('Lỗi cập nhật', 'error')
    }
  } catch {
    showToast('Lỗi kết nối', 'error')
  }
}

async function deleteOne(id) {
  const token = getToken()
  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/projects/${projectId}/vocabularies?vocabIds=${id}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` }
    })
    if (res.ok || res.status === 204) {
      vocabs.value = vocabs.value.filter(v => v.id !== id)
      showToast('Đã xóa từ vựng!')
    } else {
      showToast('Lỗi xóa', 'error')
    }
  } catch {
    showToast('Lỗi kết nối', 'error')
  }
}

async function deleteMany() {
  if (!selected.value.length) return
  const token = getToken()
  deleting.value = true
  try {
    const params = selected.value.map(id => `vocabIds=${id}`).join('&')
    const res = await fetch(`${config.public.apiBaseUrl}/api/projects/${projectId}/vocabularies?${params}`, {
      method: 'DELETE',
      headers: { Authorization: `Bearer ${token}` }
    })
    if (res.ok || res.status === 204) {
      const ids = new Set(selected.value)
      vocabs.value = vocabs.value.filter(v => !ids.has(v.id))
      showToast(`Đã xóa ${selected.value.length} từ vựng!`)
      selected.value = []
    } else {
      showToast('Lỗi xóa', 'error')
    }
  } catch {
    showToast('Lỗi kết nối', 'error')
  } finally {
    deleting.value = false
  }
}

onMounted(init)
</script>

<style scoped>
.fade-enter-active, .fade-leave-active { transition: opacity 0.2s ease; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
.toast-fade-enter-active, .toast-fade-leave-active { transition: all 0.3s ease; }
.toast-fade-enter-from, .toast-fade-leave-to { opacity: 0; transform: translateX(-50%) translateY(10px); }
</style>
