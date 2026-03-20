<template>
  <div class="bg-gray-50 dark:bg-gray-900 min-h-screen transition-colors">
    <div class="max-w-4xl mx-auto px-6 py-10">

      <!-- Loading -->
      <div v-if="!workspace" class="flex justify-center items-center py-20">
        <div class="w-6 h-6 border-2 border-yellow-400 border-t-transparent rounded-full animate-spin"></div>
      </div>

      <template v-else>
        <!-- Header -->
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
                    workspace.myRole === 'Admin'
                      ? 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-400'
                      : 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400'
                  ]">{{ workspace.myRole }}</span>
                </div>
                <p class="text-sm text-gray-500 dark:text-gray-400 mt-0.5">{{ workspace.description || 'Chưa có mô tả' }}</p>
              </div>
            </div>

            <!-- Admin actions -->
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

        <!-- Tabs -->
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

        <!-- ── Tab: Projects ── -->
      <div v-if="tab === 'projects'">
  <ProjectTab :workspace-id="id" />
</div>

        <!-- ── Tab: Members ── -->
        <div v-if="tab === 'members'" class="space-y-4">

          <!-- Invite bar (Admin only) -->
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
                <option value="Member">Member</option>
                <option value="Admin">Admin</option>
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

          <!-- Member list -->
          <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl overflow-hidden">
            <div
              v-for="(m, i) in members"
              :key="m.userId"
              :class="[
                'flex items-center gap-4 px-5 py-4 transition-colors hover:bg-gray-50 dark:hover:bg-gray-700/50',
                i !== members.length - 1 ? 'border-b border-gray-100 dark:border-gray-700' : ''
              ]"
            >
              <!-- Avatar -->
              <img
                :src="m.avatarUrl || `https://ui-avatars.com/api/?name=${m.userName}&background=6366f1&color=fff`"
                :alt="m.userName"
                class="w-9 h-9 rounded-full object-cover flex-shrink-0"
              />

              <!-- Info -->
              <div class="flex-1 min-w-0">
                <p class="text-sm font-medium text-gray-900 dark:text-white truncate">
                  {{ m.userName }}
                  <span v-if="m.userId === currentUserId" class="text-xs text-gray-400 dark:text-gray-500 font-normal ml-1">(bạn)</span>
                </p>
                <p class="text-xs text-gray-400 dark:text-gray-500 truncate">{{ m.email }}</p>
              </div>

              <!-- Role -->
              <div class="flex items-center gap-2">
                <!-- Admin có thể đổi role của người khác -->
                <select
                  v-if="isAdmin && m.userId !== currentUserId"
                  :value="m.role"
                  @change="handleRoleChange(m.userId, ($event.target as HTMLSelectElement).value)"
                  class="bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-600 text-gray-700 dark:text-gray-300 text-xs rounded-lg px-2.5 py-1.5 outline-none focus:border-blue-500 cursor-pointer transition-colors"
                >
                  <option value="Member">Member</option>
                  <option value="Admin">Admin</option>
                </select>

                <!-- Badge cho chính mình hoặc khi không phải admin -->
                <span v-else :class="[
                  'text-xs font-semibold px-2.5 py-1 rounded-full',
                  m.role === 'Admin'
                    ? 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-400'
                    : 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400'
                ]">{{ m.role }}</span>

                <!-- Xóa thành viên -->
                <button
                  v-if="isAdmin && m.userId !== currentUserId"
                  @click="handleRemove(m.userId)"
                  class="w-7 h-7 flex items-center justify-center rounded-lg text-gray-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors text-sm"
                  title="Xóa thành viên"
                >✕</button>
              </div>
            </div>
          </div>

          <!-- Leave workspace -->
          <div class="flex justify-end pt-2">
            <button
              @click="confirmLeave = true"
              class="text-sm text-red-500 dark:text-red-400 hover:text-red-700 dark:hover:text-red-300 border border-red-200 dark:border-red-800 hover:border-red-400 dark:hover:border-red-600 px-4 py-2 rounded-lg transition-colors"
            >
              Rời workspace
            </button>
          </div>
        </div>
      </template>
    </div>

    <!-- ── Modal sửa workspace ── -->
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

    <!-- ── Modal xác nhận xóa ── -->
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
            <button @click="handleDelete"
              class="px-4 py-2 text-sm rounded-lg bg-red-500 hover:bg-red-600 text-white font-semibold transition-colors">
              Xóa workspace
            </button>
          </div>
        </div>
      </div>
    </Transition>

    <!-- ── Modal xác nhận rời ── -->
    <Transition name="modal">
      <div v-if="confirmLeave"
        class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4"
        @click.self="confirmLeave = false"
      >
        <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl p-8 w-full max-w-sm shadow-2xl">
          <h2 class="text-lg font-bold text-gray-900 dark:text-white mb-2">Rời workspace?</h2>
          <p class="text-sm text-gray-500 dark:text-gray-400 mb-6">Bạn sẽ không còn truy cập được workspace và các tài nguyên trong đó.</p>
          <div class="flex justify-end gap-3">
            <button @click="confirmLeave = false"
              class="px-4 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors">
              Hủy
            </button>
            <button @click="handleLeave"
              class="px-4 py-2 text-sm rounded-lg bg-red-500 hover:bg-red-600 text-white font-semibold transition-colors">
              Rời workspace
            </button>
          </div>
        </div>
      </div>
    </Transition>

  </div>
</template>

<script setup lang="ts">
definePageMeta({
  middleware: 'auth-client'
})

import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useWorkspace } from '~/composables/useWorkspace'
import { useJwt } from '~/composables/useJwt'
import { useAuth } from '~/composables/useAuth'
import { useToast } from '~/composables/useToast'
import ProjectTab from '~/components/ProjectTab.vue'


const { showToast } = useToast()


const route = useRoute()
const router = useRouter()
const id = parseInt(route.params.id as string)

const { getWorkspace, updateWorkspace, deleteWorkspace,
        getMembers, inviteMember, updateMemberRole, removeMember, leaveWorkspace } = useWorkspace()

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
const inviting = ref(false)
const inviteEmail = ref('')
const inviteRole = ref('Member')
const inviteError = ref('')
const editForm = ref({ name: '', description: '' })

// Lấy userId trực tiếp từ useJwt — không cần decode JWT thủ công
const { userId: currentUserId } = useJwt()

const isAdmin = computed(() => workspace.value?.myRole === 'Admin')

function openEdit() {
  editForm.value = { name: workspace.value.name, description: workspace.value.description }
  showEdit.value = true
}

async function load() {
  const [ws, mb] = await Promise.all([getWorkspace(id), getMembers(id)])
  workspace.value = ws
  members.value = mb
}

async function handleUpdate() {
  try {
    saving.value = true
    workspace.value = await updateWorkspace(id, editForm.value)
    showEdit.value = false
  } finally { saving.value = false }
}

async function handleDelete() {
  await deleteWorkspace(id)
  router.push('/workspaces')
}

async function handleInvite() {
  if (!inviteEmail.value.trim()) return
  try {
    inviting.value = true
    inviteError.value = ''
    await inviteMember(id, inviteEmail.value, inviteRole.value)
    members.value = await getMembers(id)
    inviteEmail.value = ''
  } catch (e: any) {
    inviteError.value = e?.data?.message || 'Không tìm thấy user hoặc đã là thành viên.'
  } finally { inviting.value = false }
}

async function handleRoleChange(userId: number, role: string) {
  await updateMemberRole(id, userId, role)
  const m = members.value.find(m => m.userId === userId)
  if (m) m.role = role
}

async function handleRemove(userId: number) {
  await removeMember(id, userId)
  members.value = members.value.filter(m => m.userId !== userId)
}

async function handleLeave() {
  // Nếu chỉ còn 1 thành viên thì không cho rời
  if (members.value.length <= 1) {
    confirmLeave.value = false
    showToast('Bạn là thành viên duy nhất, không thể rời workspace. Hãy xóa workspace nếu muốn.', 'error')
    return
  }

  // Nếu là Admin duy nhất thì cũng không cho rời
  const adminCount = members.value.filter(m => m.role === 'Admin').length
  if (adminCount <= 1 && currentUserId.value === members.value.find(m => m.role === 'Admin')?.userId) {
    confirmLeave.value = false
    showToast('Bạn là Admin duy nhất. Hãy chỉ định Admin khác trước khi rời.', 'error')
    return
  }

  try {
    await leaveWorkspace(id)
    showToast('Đã rời workspace.', 'success')
    router.push('/workspaces')
  } catch (e: any) {
    confirmLeave.value = false
    showToast(e?.data?.message || 'Không thể rời workspace.', 'error')
  }
}
onMounted(load)
</script>

<style scoped>
.modal-enter-active, .modal-leave-active { transition: opacity 0.2s ease; }
.modal-enter-from, .modal-leave-to { opacity: 0; }
</style>