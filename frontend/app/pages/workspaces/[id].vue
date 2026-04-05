<template>
  <div v-if="!isAuthenticated || accessDenied" class="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900 transition-colors">
    <div class="text-center bg-white dark:bg-gray-800 p-8 rounded-2xl shadow-xl border border-gray-200 dark:border-gray-700 max-w-md w-full mx-4">
      <div class="w-16 h-16 bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400 rounded-full flex items-center justify-center mx-auto mb-4">
        <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" /></svg>
      </div>
      <h1 class="text-2xl font-bold text-gray-900 dark:text-white mb-2">Không khả dụng</h1>
      <p class="text-gray-500 dark:text-gray-400 mb-6">
        Bạn Không có quyền truy cập vào Workspace này.
      </p>
      <div class="flex gap-3">
        <button @click="router.push('/workspaces')" class="flex-1 px-4 py-2.5 bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-lg font-bold hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors shadow-sm">
          Trang chủ
        </button>
        <button v-if="!isAuthenticated" @click="router.push('/login')" class="flex-1 px-4 py-2.5 bg-[#f0c040] text-black rounded-lg font-bold hover:bg-[#e3b330] transition-colors shadow-sm">
          Đăng nhập
        </button>
      </div>
    </div>
  </div>

  <div v-else class="bg-gray-50 dark:bg-gray-900 min-h-screen transition-colors">
    <div class="max-w-4xl mx-auto px-6 py-10">

      <div v-if="!workspace" class="flex justify-center items-center py-20">
        <div class="w-6 h-6 border-2 border-yellow-400 border-t-transparent rounded-full animate-spin"></div>
      </div>

      <template v-else>
        <div class="mb-8">
          <button
            @click="router.push('/workspaces')"
            class="flex items-center gap-1.5 text-sm text-gray-500 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white mb-4 transition-colors"
          >
            <svg class="w-4 h-4" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
              <polyline points="15 18 9 12 15 6"/>
            </svg>
            Workspaces
          </button>

          <div class="flex items-start justify-between gap-4 flex-wrap">
            <div class="flex items-center gap-4">
              <div class="w-12 h-12 rounded-xl bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-white font-bold text-xl flex-shrink-0">
                {{ workspace.name[0].toUpperCase() }}
              </div>
              <div>
                <div class="flex items-center gap-3">
                  <h1 class="text-2xl font-bold text-gray-900 dark:text-white">{{ workspace.name }}</h1>
                  <span :class="[
                    'text-xs font-semibold px-2.5 py-1 rounded-full',
                    isAdmin
                      ? 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-400'
                      : 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400'
                  ]">{{ workspace.myRole }}</span>
                </div>
                <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">{{ workspace.description || 'Chưa có mô tả' }}</p>
              </div>
            </div>

            <div v-if="isAdmin" class="flex gap-2">
              <button
                @click="openEdit"
                class="px-3.5 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
              >
                Sửa
              </button>
              <button
                @click="confirmDelete = true"
                class="px-3.5 py-2 text-sm rounded-lg border border-red-300 dark:border-red-800 text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors"
              >
                Xóa
              </button>
            </div>
          </div>
        </div>

        <div class="flex border-b border-gray-200 dark:border-gray-700 mb-6">
          <button
            v-for="t in tabs" :key="t.key"
            @click="tab = t.key"
            :class="[
              'px-5 py-3 text-sm font-medium border-b-2 transition-colors',
              tab === t.key
                ? 'border-yellow-400 text-yellow-600 dark:text-yellow-400'
                : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300'
            ]"
          >
            {{ t.label }}
            <span v-if="t.key === 'members'" class="ml-1.5 bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 text-xs px-1.5 py-0.5 rounded-full">
              {{ members.length }}
            </span>
          </button>
        </div>

        <div v-if="tab === 'projects'">
          <ProjectTab :workspace-id="id" />
        </div>

        <div v-if="tab === 'members'" class="space-y-4">

          <div v-if="isAdmin" class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-4">
            <p class="text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">Mời thành viên</p>
            <div class="flex gap-2">
              <input
                v-model="inviteEmail"
                type="email"
                placeholder="Email..."
                class="flex-1 bg-gray-50 dark:bg-gray-900 border border-gray-300 dark:border-gray-600 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm outline-none focus:border-blue-500 dark:focus:border-blue-400 transition-colors placeholder-gray-400 dark:placeholder-gray-500"
                @keyup.enter="handleInvite"
              />
              <select
                v-model="inviteRole"
                class="bg-gray-50 dark:bg-gray-900 border border-gray-300 dark:border-gray-600 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm outline-none focus:border-blue-500 dark:focus:border-blue-400 transition-colors cursor-pointer"
              >
                <option value="MEMBER">Member</option>
                <option value="ADMIN">Admin</option>
              </select>
              <button
                @click="handleInvite"
                :disabled="!inviteEmail.trim() || inviting"
                class="px-4 py-2 bg-yellow-400 hover:bg-yellow-500 disabled:opacity-50 disabled:cursor-not-allowed text-gray-900 font-semibold text-sm rounded-lg transition-colors whitespace-nowrap"
              >
                {{ inviting ? 'Đang mời...' : 'Mời' }}
              </button>
            </div>
            <p v-if="inviteError" class="text-red-500 dark:text-red-400 text-xs mt-2">{{ inviteError }}</p>
          </div>

          <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl overflow-hidden">
            <div
              v-for="(m, i) in members"
              :key="m.userId"
              :class="[
                'flex items-center gap-4 px-5 py-4 transition-colors hover:bg-gray-50 dark:hover:bg-gray-700/50',
                i !== members.length - 1 ? 'border-b border-gray-100 dark:border-gray-700' : ''
              ]"
            >
              <img
                :src="m.avatarUrl || `https://ui-avatars.com/api/?name=${m.userName}&background=6366f1&color=fff`"
                :alt="m.userName"
                class="w-9 h-9 rounded-full object-cover flex-shrink-0"
              />

              <div class="flex-1 min-w-0">
                <p class="text-sm font-medium text-gray-900 dark:text-white truncate">
                  {{ m.userName }}
                  <span v-if="m.userId === currentUserId" class="text-xs text-gray-400 dark:text-gray-500 font-normal ml-1">(bạn)</span>
                </p>
                <p class="text-xs text-gray-400 dark:text-gray-500 truncate">{{ m.email }}</p>
              </div>

              <div class="flex items-center gap-2">
                <select
                  v-if="isAdmin && m.userId !== currentUserId"
                  :value="m.role?.toUpperCase()"
                  @change="handleRoleChange(m.userId, ($event.target as HTMLSelectElement).value)"
                  class="bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-600 text-gray-700 dark:text-gray-300 text-xs rounded-lg px-2.5 py-1.5 outline-none focus:border-blue-500 cursor-pointer transition-colors"
                >
                  <option value="MEMBER">Member</option>
                  <option value="ADMIN">Admin</option>
                </select>

                <span v-else :class="[
                  'text-xs font-semibold px-2.5 py-1 rounded-full',
                  m.role?.toUpperCase() === 'ADMIN'
                    ? 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-400'
                    : 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400'
                ]">{{ m.role }}</span>

                <button
                  v-if="isAdmin && m.userId !== currentUserId"
                  @click="handleRemove(m.userId)"
                  class="w-7 h-7 flex items-center justify-center rounded-lg text-gray-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors text-sm"
                  title="Xóa thành viên"
                >✕</button>
              </div>
            </div>
          </div>

          <div class="flex justify-end pt-2">
            <button
              @click="openLeaveModal"
              class="text-sm text-red-500 dark:text-red-400 hover:text-red-700 dark:hover:text-red-300 border border-red-200 dark:border-red-800 hover:border-red-400 dark:hover:border-red-600 px-4 py-2 rounded-lg transition-colors"
            >
              Rời workspace
            </button>
          </div>
        </div>
      </template>
    </div>

    <Transition name="modal">
      <div v-if="showEdit"
        class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4"
        @click.self="showEdit = false"
      >
        <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl p-8 w-full max-w-md shadow-2xl">
          <h2 class="text-xl font-bold text-gray-900 dark:text-white mb-6">Sửa Workspace</h2>
          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Tên</label>
              <input
                v-model="editForm.name"
                class="w-full bg-gray-50 dark:bg-gray-900 border border-gray-300 dark:border-gray-600 text-gray-900 dark:text-white rounded-lg px-3.5 py-2.5 text-sm outline-none focus:border-blue-500 dark:focus:border-blue-400 transition-colors"
              />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Mô tả</label>
              <textarea
                v-model="editForm.description"
                rows="3"
                class="w-full bg-gray-50 dark:bg-gray-900 border border-gray-300 dark:border-gray-600 text-gray-900 dark:text-white rounded-lg px-3.5 py-2.5 text-sm outline-none focus:border-blue-500 dark:focus:border-blue-400 transition-colors resize-none"
              ></textarea>
            </div>
          </div>
          <div class="flex justify-end gap-3 mt-6">
            <button @click="showEdit = false"
              class="px-4 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors">
              Hủy
            </button>
            <button @click="handleUpdate" :disabled="saving"
              class="px-5 py-2 text-sm rounded-lg bg-yellow-400 hover:bg-yellow-500 disabled:opacity-50 text-gray-900 font-semibold transition-colors">
              {{ saving ? 'Đang lưu...' : 'Lưu thay đổi' }}
            </button>
          </div>
        </div>
      </div>
    </Transition>

    <Transition name="modal">
      <div v-if="confirmDelete"
        class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4"
        @click.self="confirmDelete = false"
      >
        <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl p-8 w-full max-w-sm shadow-2xl">
          <div class="w-12 h-12 bg-red-100 dark:bg-red-900/30 rounded-full flex items-center justify-center mb-4">
            <svg class="w-6 h-6 text-red-600 dark:text-red-400" fill="none" stroke="currentColor" stroke-width="2" viewBox="0 0 24 24">
              <polyline points="3 6 5 6 21 6"/><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/>
            </svg>
          </div>
          <h2 class="text-lg font-bold text-gray-900 dark:text-white mb-2">Xóa workspace?</h2>
          <p class="text-sm text-gray-500 dark:text-gray-400 mb-6">Hành động này không thể hoàn tác. Tất cả dự án, file và từ vựng sẽ bị xóa vĩnh viễn.</p>
          <div class="flex justify-end gap-3">
            <button @click="confirmDelete = false"
              class="px-4 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors">
              Hủy
            </button>
            <button @click="handleDelete" :disabled="deleting"
              class="flex items-center gap-2 px-4 py-2 text-sm rounded-lg bg-red-500 hover:bg-red-600 disabled:opacity-50 text-white font-semibold transition-colors">
              <svg v-if="deleting" class="w-4 h-4 animate-spin text-white" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"/>
              </svg>
              {{ deleting ? 'Đang xóa...' : 'Xóa workspace' }}
            </button>
          </div>
        </div>
      </div>
    </Transition>

    <Transition name="modal">
      <div v-if="confirmLeave"
        class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4"
        @click.self="confirmLeave = false"
      >
        <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl p-8 w-full max-w-md shadow-2xl">
          <h2 class="text-lg font-bold text-gray-900 dark:text-white mb-2">Rời workspace?</h2>
          
          <div v-if="members.length <= 1">
            <p class="text-sm text-red-500 dark:text-red-400 mb-6">Bạn là thành viên duy nhất. Vui lòng sử dụng tính năng Xóa Workspace thay vì rời đi.</p>
          </div>
          <div v-else>
            <div v-if="isSoleAdmin" class="mb-6">
              <p class="text-sm text-yellow-600 dark:text-yellow-500 font-medium mb-3">
                Bạn là Admin duy nhất. Vui lòng nhượng quyền Admin cho một thành viên khác trước khi rời đi:
              </p>
              <select
                v-model="newAdminId"
                class="w-full bg-gray-50 dark:bg-gray-900 border border-gray-300 dark:border-gray-600 text-gray-900 dark:text-white rounded-lg px-3 py-2 text-sm outline-none focus:border-blue-500 transition-colors cursor-pointer"
              >
                <option value="" disabled>-- Chọn người kế nhiệm --</option>
                <option v-for="m in eligibleSuccessors" :key="m.userId" :value="m.userId">
                  {{ m.userName }} ({{ m.email }})
                </option>
              </select>
            </div>
            <p v-else class="text-sm text-gray-500 dark:text-gray-400 mb-6">Bạn sẽ không còn truy cập được workspace và các tài nguyên trong đó.</p>
          </div>

          <div class="flex justify-end gap-3">
            <button @click="confirmLeave = false"
              class="px-4 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors">
              Hủy
            </button>
            <button v-if="members.length > 1" @click="handleLeave" :disabled="leaving"
              class="flex items-center gap-2 px-4 py-2 text-sm rounded-lg bg-red-500 hover:bg-red-600 disabled:opacity-50 text-white font-semibold transition-colors">
              <svg v-if="leaving" class="w-4 h-4 animate-spin text-white" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"/>
              </svg>
              {{ leaving ? 'Đang xử lý...' : 'Xác nhận rời' }}
            </button>
          </div>
        </div>
      </div>
    </Transition>

  </div>
</template>

<script setup lang="ts">
// Bỏ qua middleware để tự kiểm soát luồng
definePageMeta({
  // middleware: 'auth-client' // <-- XÓA DÒNG NÀY ĐỂ KHÔNG BỊ VĂNG
})

import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useWorkspace } from '~/composables/useWorkspace'
import { useJwt } from '~/composables/useJwt'
import { useToast } from '~/composables/useToast'
import ProjectTab from '~/components/ProjectTab.vue'

const { showToast } = useToast()
const route = useRoute()
const router = useRouter()
const id = parseInt(route.params.id as string)

const { getWorkspace, updateWorkspace, deleteWorkspace,
        getMembers, inviteMember, updateMemberRole, removeMember, leaveWorkspace } = useWorkspace()

const { userId: currentUserId, isAuthenticated } = useJwt()

// --- THÊM STATE TRUY CẬP ---
const accessDenied = ref(false)

const workspace = ref<any>(null)
const members = ref<any[]>([])
const tab = ref('members')
const tabs = [
  { key: 'projects', label: 'Dự án' },
  { key: 'members', label: 'Thành viên' },
]

const showEdit = ref(false)
const confirmDelete = ref(false)
const confirmLeave = ref(false)
const saving = ref(false)
const deleting = ref(false)
const leaving = ref(false) 
const inviting = ref(false)
const inviteEmail = ref('')
const inviteRole = ref('MEMBER')
const inviteError = ref('')
const editForm = ref({ name: '', description: '' })
const newAdminId = ref<number | ''>('') 

const isAdmin = computed(() => workspace.value?.myRole?.toUpperCase() === 'ADMIN')

const isSoleAdmin = computed(() => {
  if (!isAdmin.value) return false
  const adminCount = members.value.filter(m => m.role?.toUpperCase() === 'ADMIN').length
  return adminCount === 1
})

const eligibleSuccessors = computed(() => members.value.filter(m => m.userId !== currentUserId.value))

function openEdit() {
  editForm.value = { name: workspace.value.name, description: workspace.value.description }
  showEdit.value = true
}

function openLeaveModal() {
  newAdminId.value = ''
  confirmLeave.value = true
}

async function load() {
  // Nếu chưa đăng nhập => Bật ngay cờ từ chối
  if (!isAuthenticated.value) {
    accessDenied.value = true;
    return;
  }

  try {
     const [ws, mb] = await Promise.all([getWorkspace(id), getMembers(id)])
     workspace.value = ws
     members.value = mb
  } catch(error) {
     console.error(error)
     // Bất kỳ lỗi nào (như 403 Forbidden, 404 Not Found) đều kích hoạt cờ từ chối
     accessDenied.value = true;
  }
}

async function handleUpdate() {
  try {
    saving.value = true
    workspace.value = await updateWorkspace(id, editForm.value)
    showEdit.value = false
    showToast('Cập nhật thành công', 'success')
  } catch (error) {
    showToast('Lỗi cập nhật', 'error')
  } finally { saving.value = false }
}

async function handleDelete() {
  try {
    deleting.value = true
    await deleteWorkspace(id)
    showToast('Xóa Workspace thành công', 'success')
    router.push('/workspaces')
  } catch (error) {
    showToast('Bạn không thể xóa', 'error')
  } finally { deleting.value = false }
}

async function handleInvite() {
  if (!inviteEmail.value.trim()) return
  try {
    inviting.value = true
    inviteError.value = ''
    await inviteMember(id, inviteEmail.value, inviteRole.value)
    members.value = await getMembers(id)
    inviteEmail.value = ''
    showToast('Mời thành công', 'success')
  } catch (e: any) {
    inviteError.value = e?.data?.message || 'Không tìm thấy user hoặc đã là thành viên.'
  } finally { inviting.value = false }
}

async function handleRoleChange(userId: number, role: string) {
  try {
    await updateMemberRole(id, userId, role)
    const m = members.value.find(m => m.userId === userId)
    if (m) m.role = role
    showToast('Đã đổi quyền thành viên', 'success')
  } catch (error) {
    showToast('Lỗi cập nhật quyền', 'error')
  }
}

async function handleRemove(userId: number) {
  try {
    await removeMember(id, userId)
    members.value = members.value.filter(m => m.userId !== userId)
    showToast('Đã xóa thành viên', 'success')
  } catch (error) {
    showToast('Lỗi xóa thành viên', 'error')
  }
}

async function handleLeave() {
  if (members.value.length <= 1) return 

  try {
    leaving.value = true

    if (isSoleAdmin.value) {
      if (!newAdminId.value) {
        showToast('Vui lòng chọn người kế nhiệm trước khi rời', 'error')
        return
      }
      await updateMemberRole(id, Number(newAdminId.value), 'ADMIN')
    }

    await leaveWorkspace(id)
    showToast('Đã rời workspace.', 'success')
    confirmLeave.value = false
    router.push('/workspaces')

  } catch (e: any) {
    showToast(e?.data?.message || 'Không thể rời workspace.', 'error')
  } finally {
    leaving.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.modal-enter-active, .modal-leave-active { transition: opacity 0.2s ease; }
.modal-enter-from, .modal-leave-to { opacity: 0; }
</style>
