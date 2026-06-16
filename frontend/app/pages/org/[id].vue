<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-white p-4 sm:p-8">
    <div class="max-w-4xl mx-auto">

      <!-- Back -->
      <button @click="navigateTo('/org')" class="flex items-center gap-2 text-gray-400 hover:text-gray-600 dark:hover:text-white text-sm mb-6 transition-colors">
        ← Danh sách tổ chức
      </button>

      <div v-if="loading" class="flex justify-center py-20">
        <div class="w-8 h-8 border-4 border-gray-300 border-t-indigo-500 rounded-full animate-spin"/>
      </div>

      <div v-else-if="!org" class="text-center py-20 text-gray-400">Không tìm thấy tổ chức.</div>

      <div v-else>
        <!-- Header -->
        <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl p-6 mb-6">
          <div class="flex items-start justify-between">
            <div class="flex items-center gap-4">
              <div class="w-14 h-14 rounded-xl bg-indigo-100 dark:bg-indigo-900/30 flex items-center justify-center text-3xl">🏢</div>
              <div>
                <div class="flex items-center gap-3">
                  <h1 class="text-2xl font-bold">{{ org.name }}</h1>
                  <span class="text-xs font-bold px-2.5 py-1 rounded-full"
                    :class="{
                      'bg-gray-100 text-gray-500 dark:bg-gray-700': org.orgPlan === 'FREE',
                      'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400': org.orgPlan === 'TEAM',
                      'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400': org.orgPlan === 'ENTERPRISE',
                    }">{{ org.orgPlan }}</span>
                </div>
                <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">{{ org.description || 'Chưa có mô tả' }}</p>
              </div>
            </div>
            <!-- Actions for OWNER/ADMIN -->
            <div v-if="myOrgRole === 'OWNER' || myOrgRole === 'ADMIN'" class="flex gap-2">
              <button @click="showEdit = true" class="px-3 py-1.5 text-sm border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors">
                ✏️ Sửa
              </button>
              <button v-if="myOrgRole === 'OWNER'" @click="showDelete = true"
                class="px-3 py-1.5 text-sm border border-red-300 dark:border-red-700 text-red-600 dark:text-red-400 rounded-lg hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors">
                🗑 Xoá
              </button>
            </div>
          </div>

          <!-- Plan upgrade CTA (FREE) -->
          <div v-if="org.orgPlan === 'FREE' && (myOrgRole === 'OWNER' || myOrgRole === 'ADMIN')"
            class="mt-4 pt-4 border-t border-gray-100 dark:border-gray-700 flex items-center justify-between">
            <p class="text-sm text-gray-500">Nâng cấp để toàn bộ thành viên dùng OCR không giới hạn</p>
            <NuxtLink to="/premium" class="px-4 py-1.5 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-semibold rounded-lg transition-colors">
              ⚡ Nâng cấp Plan
            </NuxtLink>
          </div>

          <!-- Current plan info + manage (TEAM/ENTERPRISE) -->
          <div v-if="org.orgPlan !== 'FREE' && (myOrgRole === 'OWNER' || myOrgRole === 'ADMIN')"
            class="mt-4 pt-4 border-t border-gray-100 dark:border-gray-700">
            <div class="flex items-center justify-between">
              <div>
                <p class="text-sm font-semibold text-gray-700 dark:text-gray-300">
                  Plan hiện tại:
                  <span class="font-bold"
                    :class="org.orgPlan === 'ENTERPRISE' ? 'text-purple-600 dark:text-purple-400' : 'text-green-600 dark:text-green-400'">
                    {{ org.orgPlan }}
                  </span>
                </p>
                <p class="text-xs text-gray-400 mt-0.5">
                  {{ org.orgPlan === 'TEAM' ? 'OCR không giới hạn · Upload 500MB cho cả team' : 'Unlimited everything · Audit log · SLA' }}
                </p>
              </div>
              <button @click="openBillingPortal" :disabled="portalLoading"
                class="px-4 py-1.5 border border-gray-300 dark:border-gray-600 hover:bg-gray-100 dark:hover:bg-gray-700 text-sm font-medium rounded-lg transition-colors">
                {{ portalLoading ? 'Đang mở...' : '💳 Quản lý / Huỷ subscription' }}
              </button>
            </div>
          </div>
        </div>

        <!-- Members section -->
        <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl p-6">
          <div class="flex items-center justify-between mb-4">
            <h2 class="font-bold text-lg">Thành viên <span class="text-gray-400 font-normal text-sm">({{ members.length }})</span></h2>
            <button v-if="myOrgRole === 'OWNER' || myOrgRole === 'ADMIN'" @click="showInvite = true"
              class="px-3 py-1.5 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-semibold rounded-lg transition-colors">
              + Mời thành viên
            </button>
          </div>

          <div class="divide-y divide-gray-100 dark:divide-gray-700">
            <div v-for="m in members" :key="m.userId" class="flex items-center justify-between py-3">
              <div class="flex items-center gap-3">
                <img :src="m.avatarUrl || '/images/default_ava.jpg'" class="w-9 h-9 rounded-full object-cover border border-gray-200 dark:border-gray-600" />
                <div>
                  <p class="font-medium text-sm">{{ m.userName }}</p>
                  <p class="text-xs text-gray-400">{{ m.email }}</p>
                </div>
              </div>
              <div class="flex items-center gap-2">
                <span class="text-xs font-semibold px-2 py-0.5 rounded-full"
                  :class="{
                    'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400': m.orgRole === 'OWNER',
                    'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400': m.orgRole === 'ADMIN',
                    'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400': m.orgRole === 'MEMBER',
                    'bg-gray-100 text-gray-500 dark:bg-gray-700': m.orgRole === 'BILLING_MANAGER',
                  }">{{ m.orgRole }}</span>
                <!-- Actions: only OWNER can change roles, only OWNER/ADMIN can remove -->
                <div v-if="(myOrgRole === 'OWNER' || myOrgRole === 'ADMIN') && m.orgRole !== 'OWNER'"
                  class="flex gap-1">
                  <select v-if="myOrgRole === 'OWNER'" :value="m.orgRole"
                    @change="changeRole(m.userId, ($event.target as HTMLSelectElement).value)"
                    class="text-xs bg-gray-50 dark:bg-gray-900 border border-gray-200 dark:border-gray-600 rounded px-1.5 py-1">
                    <option value="ADMIN">Admin</option>
                    <option value="MEMBER">Member</option>
                    <option value="VIEWER">Viewer</option>
                  </select>
                  <button @click="removeMember(m.userId)"
                    class="text-xs text-red-400 hover:text-red-600 px-2 py-1 rounded hover:bg-red-50 dark:hover:bg-red-900/20 transition-colors">
                    Xoá
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Edit modal -->
      <Transition name="modal">
        <div v-if="showEdit" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4" @click.self="showEdit = false">
          <div class="bg-white dark:bg-gray-800 rounded-2xl p-6 w-full max-w-md shadow-2xl">
            <h2 class="text-lg font-bold mb-4">Sửa thông tin tổ chức</h2>
            <div class="space-y-3">
              <input v-model="editForm.name" type="text" placeholder="Tên tổ chức"
                class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-sm focus:outline-none focus:border-indigo-500"/>
              <textarea v-model="editForm.description" placeholder="Mô tả" rows="3"
                class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-sm focus:outline-none focus:border-indigo-500"/>
            </div>
            <div class="flex gap-3 mt-4">
              <button @click="showEdit = false" class="flex-1 px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-xl text-sm hover:bg-gray-100 dark:hover:bg-gray-700">Hủy</button>
              <button @click="updateOrg" :disabled="saving" class="flex-1 px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl text-sm font-bold disabled:opacity-50">
                {{ saving ? 'Đang lưu...' : 'Lưu' }}
              </button>
            </div>
          </div>
        </div>
      </Transition>

      <!-- Invite modal -->
      <Transition name="modal">
        <div v-if="showInvite" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4" @click.self="showInvite = false">
          <div class="bg-white dark:bg-gray-800 rounded-2xl p-6 w-full max-w-md shadow-2xl">
            <h2 class="text-lg font-bold mb-4">Mời thành viên</h2>
            <div class="space-y-3">
              <input v-model="inviteForm.email" type="text" placeholder="Email hoặc username"
                class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-sm focus:outline-none focus:border-indigo-500"/>
              <select v-model="inviteForm.role" class="w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-sm">
                <option value="ADMIN">Admin</option>
                <option value="MEMBER">Member</option>
                <option value="VIEWER">Viewer</option>
              </select>
            </div>
            <p v-if="inviteError" class="text-red-500 text-sm mt-2">{{ inviteError }}</p>
            <div class="flex gap-3 mt-4">
              <button @click="showInvite = false" class="flex-1 px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-xl text-sm hover:bg-gray-100 dark:hover:bg-gray-700">Hủy</button>
              <button @click="inviteMember" :disabled="!inviteForm.email.trim() || inviting"
                class="flex-1 px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl text-sm font-bold disabled:opacity-50">
                {{ inviting ? 'Đang mời...' : 'Mời' }}
              </button>
            </div>
          </div>
        </div>
      </Transition>

      <!-- Delete confirm -->
      <Transition name="modal">
        <div v-if="showDelete" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4" @click.self="showDelete = false">
          <div class="bg-white dark:bg-gray-800 rounded-2xl p-6 w-full max-w-sm shadow-2xl text-center">
            <div class="text-4xl mb-3">⚠️</div>
            <h2 class="text-lg font-bold mb-2">Xoá tổ chức?</h2>
            <p class="text-sm text-gray-500 mb-6">Hành động này không thể hoàn tác. Tất cả dữ liệu tổ chức sẽ bị xoá.</p>
            <div class="flex gap-3">
              <button @click="showDelete = false" class="flex-1 px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-xl text-sm">Hủy</button>
              <button @click="deleteOrg" :disabled="deleting" class="flex-1 px-4 py-2 bg-red-600 hover:bg-red-700 text-white rounded-xl text-sm font-bold disabled:opacity-50">
                {{ deleting ? 'Đang xoá...' : 'Xoá' }}
              </button>
            </div>
          </div>
        </div>
      </Transition>

    </div>
  </div>
</template>

<script setup lang="ts">
definePageMeta({ middleware: 'auth-client', ssr: false })
import { useToast } from '~/composables/useToast'

const route = useRoute()
const { jwt } = useJwt()
const config = useRuntimeConfig()
const { showToast } = useToast()
const orgId = computed(() => Number(route.params.id))

const loading = ref(true)
const org = ref<any>(null)
const members = ref<any[]>([])
const myOrgRole = computed(() => members.value.find(m => m.userId === myUserId.value)?.orgRole ?? '')
const myUserId = ref(0)

// Forms
const showEdit = ref(false)
const showInvite = ref(false)
const showDelete = ref(false)
const saving = ref(false)
const inviting = ref(false)
const deleting = ref(false)
const portalLoading = ref(false)
const inviteError = ref('')
const editForm = reactive({ name: '', description: '' })
const inviteForm = reactive({ email: '', role: 'MEMBER' })

const headers = computed(() => ({ Authorization: `Bearer ${jwt.value}`, 'Content-Type': 'application/json' }))

async function load() {
  if (!orgId.value || isNaN(orgId.value)) return
  loading.value = true
  try {
    // Get my userId from me endpoint
    const me = await $fetch<any>(`${config.public.apiBaseUrl}/api/auth/me`, { headers: headers.value })
    myUserId.value = me?.result?.userId ?? 0

    // Get orgs to find this one
    const orgsRes = await $fetch<any>(`${config.public.apiBaseUrl}/api/organizations/my`, { headers: headers.value })
    org.value = orgsRes?.result?.find((o: any) => o.id === orgId.value) ?? null
    if (org.value) {
      editForm.name = org.value.name
      editForm.description = org.value.description || ''
    }

    // Get members
    const membersRes = await $fetch<any>(`${config.public.apiBaseUrl}/api/organizations/${orgId.value}/members`, { headers: headers.value })
    members.value = membersRes?.result ?? []
  } catch { org.value = null }
  finally { loading.value = false }
}

async function updateOrg() {
  saving.value = true
  try {
    await $fetch(`${config.public.apiBaseUrl}/api/organizations/${orgId.value}`, {
      method: 'PUT', headers: headers.value,
      body: { name: editForm.name, description: editForm.description }
    })
    if (org.value) { org.value.name = editForm.name; org.value.description = editForm.description }
    showEdit.value = false
    showToast('Đã cập nhật tổ chức', 'success')
  } catch { showToast('Không thể cập nhật', 'error') }
  finally { saving.value = false }
}

async function inviteMember() {
  inviteError.value = ''
  inviting.value = true
  try {
    await $fetch(`${config.public.apiBaseUrl}/api/organizations/${orgId.value}/members`, {
      method: 'POST', headers: headers.value,
      body: { email: inviteForm.email, role: inviteForm.role }
    })
    showInvite.value = false
    inviteForm.email = ''
    showToast('Đã mời thành viên', 'success')
    await load()
  } catch (e: any) {
    inviteError.value = e?.data?.message || 'Không tìm thấy user hoặc đã là thành viên'
  } finally { inviting.value = false }
}

async function changeRole(userId: number, newRole: string) {
  try {
    await $fetch(`${config.public.apiBaseUrl}/api/organizations/${orgId.value}/members/${userId}/role`, {
      method: 'PUT', headers: headers.value,
      body: { orgRole: newRole }
    })
    const m = members.value.find(m => m.userId === userId)
    if (m) m.orgRole = newRole
    showToast('Đã đổi quyền', 'success')
  } catch { showToast('Không thể đổi quyền', 'error') }
}

async function removeMember(userId: number) {
  try {
    await $fetch(`${config.public.apiBaseUrl}/api/organizations/${orgId.value}/members/${userId}`, {
      method: 'DELETE', headers: headers.value
    })
    members.value = members.value.filter(m => m.userId !== userId)
    showToast('Đã xoá thành viên', 'success')
  } catch { showToast('Không thể xoá thành viên', 'error') }
}

async function deleteOrg() {
  deleting.value = true
  try {
    await $fetch(`${config.public.apiBaseUrl}/api/organizations/${orgId.value}`, {
      method: 'DELETE', headers: headers.value
    })
    showToast('Đã xoá tổ chức', 'success')
    navigateTo('/org')
  } catch { showToast('Không thể xoá tổ chức', 'error') }
  finally { deleting.value = false }
}

async function openBillingPortal() {
  portalLoading.value = true
  try {
    const res = await $fetch<any>(`${config.public.apiBaseUrl}/api/stripe/org-portal/${orgId.value}`, {
      method: 'POST', headers: headers.value
    })
    window.location.href = res.result.url
  } catch (e: any) {
    showToast(e?.data?.message || 'Không thể mở trang billing', 'error')
  } finally { portalLoading.value = false }
}

onMounted(() => {
  // Watch orgId trong case SSR/hydration delay
  if (orgId.value && !isNaN(orgId.value)) {
    load()
  } else {
    watch(orgId, (val) => { if (val && !isNaN(val)) load() }, { once: true })
  }
})
</script>

<style scoped>
.modal-enter-active, .modal-leave-active { transition: opacity 0.2s; }
.modal-enter-from, .modal-leave-to { opacity: 0; }
</style>
