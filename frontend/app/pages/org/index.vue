<template>
  <div class="min-h-screen bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-white p-4 sm:p-8">
    <div class="max-w-5xl mx-auto">

      <div class="flex items-center justify-between mb-8">
        <div>
          <h1 class="text-3xl font-bold">Tổ chức của tôi</h1>
          <p class="text-gray-500 dark:text-gray-400 mt-1 text-sm">Quản lý tổ chức và thành viên</p>
        </div>
        <button @click="showCreate = true"
          class="flex items-center gap-2 px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white font-semibold rounded-xl transition-colors text-sm">
          + Tạo tổ chức
        </button>
      </div>

      <!-- Loading -->
      <div v-if="loading" class="flex justify-center py-20">
        <div class="w-8 h-8 border-4 border-gray-300 border-t-indigo-500 rounded-full animate-spin"/>
      </div>

      <!-- Empty -->
      <div v-else-if="orgs.length === 0" class="text-center py-20 text-gray-400">
        <div class="text-6xl mb-4">🏢</div>
        <p class="text-xl font-semibold mb-2">Chưa có tổ chức nào</p>
        <p class="text-sm">Tạo tổ chức để mời đồng nghiệp và chia sẻ workspace.</p>
      </div>

      <!-- Org list -->
      <div v-else class="grid gap-4 sm:grid-cols-2">
        <NuxtLink v-for="org in orgs" :key="org.id" :to="`/org/${org.id}`"
          class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl p-6 hover:shadow-md transition-shadow group">
          <div class="flex items-start justify-between mb-3">
            <div class="w-12 h-12 rounded-xl bg-indigo-100 dark:bg-indigo-900/30 flex items-center justify-center text-2xl">
              🏢
            </div>
            <span class="text-xs font-bold px-2 py-1 rounded-full"
              :class="{
                'bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400': org.myRole === 'OWNER',
                'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400': org.myRole === 'ADMIN',
                'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-400': org.myRole === 'MEMBER',
              }">
              {{ org.myRole }}
            </span>
          </div>
          <h3 class="font-bold text-lg text-gray-900 dark:text-white group-hover:text-indigo-600 dark:group-hover:text-indigo-400 transition-colors">
            {{ org.name }}
          </h3>
          <div class="flex items-center gap-4 mt-3 text-xs text-gray-500 dark:text-gray-400">
            <span>👥 {{ org.memberCount }} thành viên</span>
            <span class="px-2 py-0.5 rounded-full font-semibold"
              :class="{
                'bg-gray-100 dark:bg-gray-700': org.orgPlan === 'FREE',
                'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400': org.orgPlan === 'TEAM',
                'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400': org.orgPlan === 'ENTERPRISE',
              }">
              {{ org.orgPlan }}
            </span>
          </div>
        </NuxtLink>
      </div>

      <!-- Create org modal -->
      <Transition name="modal">
        <div v-if="showCreate" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
          @click.self="showCreate = false">
          <div class="bg-white dark:bg-gray-800 rounded-2xl p-6 w-full max-w-md shadow-2xl">
            <h2 class="text-xl font-bold mb-4">Tạo tổ chức mới</h2>
            <div class="space-y-4">
              <div>
                <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Tên tổ chức *</label>
                <input v-model="newOrg.name" type="text" placeholder="VD: Công ty ABC"
                  class="mt-1 w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white focus:outline-none focus:border-indigo-500 text-sm" />
              </div>
              <div>
                <label class="text-sm font-medium text-gray-700 dark:text-gray-300">Mô tả</label>
                <textarea v-model="newOrg.description" placeholder="Mô tả ngắn về tổ chức..."
                  class="mt-1 w-full px-3 py-2 rounded-lg border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-900 text-gray-900 dark:text-white focus:outline-none focus:border-indigo-500 text-sm" rows="3"/>
              </div>
            </div>
            <div class="flex gap-3 mt-6">
              <button @click="showCreate = false"
                class="flex-1 px-4 py-2 border border-gray-300 dark:border-gray-600 rounded-xl text-sm font-medium hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors">
                Hủy
              </button>
              <button @click="createOrg" :disabled="!newOrg.name.trim() || creating"
                class="flex-1 px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl text-sm font-bold disabled:opacity-50 transition-colors">
                {{ creating ? 'Đang tạo...' : 'Tạo tổ chức' }}
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

const { jwt } = useJwt()
const config = useRuntimeConfig()
const { showToast } = useToast()

const orgs = ref<any[]>([])
const loading = ref(true)
const showCreate = ref(false)
const creating = ref(false)
const newOrg = reactive({ name: '', description: '' })

async function fetchOrgs() {
  loading.value = true
  try {
    const res = await $fetch<any>(`${config.public.apiBaseUrl}/api/organizations/my`, {
      headers: { Authorization: `Bearer ${jwt.value}` }
    })
    orgs.value = res?.result ?? []
  } catch { orgs.value = [] }
  finally { loading.value = false }
}

async function createOrg() {
  if (!newOrg.name.trim()) return
  creating.value = true
  try {
    const res = await $fetch<any>(`${config.public.apiBaseUrl}/api/organizations`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${jwt.value}`, 'Content-Type': 'application/json' },
      body: { name: newOrg.name, description: newOrg.description }
    })
    showToast(`Tổ chức "${newOrg.name}" đã được tạo!`, 'success')
    showCreate.value = false
    await nextTick()
    newOrg.name = ''
    newOrg.description = ''
    // Thêm org mới trực tiếp vào list thay vì fetch lại để UI update ngay
    if (res?.result) {
      orgs.value.push({ ...res.result, myRole: 'OWNER', memberCount: 1 })
    } else {
      await fetchOrgs()
    }
  } catch (e: any) {
    const msg = e?.data?.message || e?.message || 'Không thể tạo tổ chức. Thử lại sau.'
    showToast(msg, 'error')
  } finally {
    creating.value = false
  }
}

onMounted(() => fetchOrgs())
</script>

<style scoped>
.modal-enter-active, .modal-leave-active { transition: opacity 0.2s; }
.modal-enter-from, .modal-leave-to { opacity: 0; }
</style>
