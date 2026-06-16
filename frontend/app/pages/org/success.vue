<template>
  <div class="min-h-screen flex flex-col items-center justify-center bg-gray-50 dark:bg-gray-900 text-center p-8">
    <div v-if="!confirmed">
      <div class="text-5xl mb-6 animate-bounce">⏳</div>
      <h1 class="text-2xl font-bold text-gray-900 dark:text-white mb-2">Đang xác nhận thanh toán...</h1>
      <p class="text-gray-400 text-sm">Vui lòng chờ trong giây lát</p>
      <div class="w-48 h-1.5 bg-gray-200 dark:bg-gray-700 rounded-full mt-6 mx-auto overflow-hidden">
        <div class="h-full bg-indigo-500 rounded-full transition-all duration-500" :style="{ width: progress + '%' }"/>
      </div>
    </div>
    <div v-else>
      <div class="text-7xl mb-6">🎉</div>
      <h1 class="text-4xl font-extrabold text-gray-900 dark:text-white mb-2">Tổ chức đã được nâng cấp!</h1>
      <p class="text-gray-500 dark:text-gray-400 mb-8 max-w-md">
        Plan <span class="font-bold text-indigo-600">{{ newPlan }}</span> đã được kích hoạt. Toàn bộ thành viên tổ chức đã được hưởng quyền lợi mới.
      </p>
      <div class="flex gap-3 justify-center">
        <button @click="navigateTo(`/org/${orgId}`)"
          class="px-8 py-3 bg-indigo-600 hover:bg-indigo-700 text-white font-bold rounded-xl text-sm transition-colors">
          Xem tổ chức →
        </button>
        <button @click="navigateTo('/workspaces')"
          class="px-8 py-3 border border-gray-300 dark:border-gray-600 rounded-xl text-sm font-medium transition-colors hover:bg-gray-100 dark:hover:bg-gray-800">
          Về Workspace
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
definePageMeta({ ssr: false })

const route = useRoute()
const { jwt } = useJwt()
const config = useRuntimeConfig()

const orgId = computed(() => Number(route.query.orgId))
const confirmed = ref(false)
const progress = ref(10)
const newPlan = ref('TEAM')

onMounted(async () => {
  if (!orgId.value || isNaN(orgId.value)) {
    // Không có orgId → về /org
    navigateTo('/org')
    return
  }

  const progressTimer = setInterval(() => {
    if (progress.value < 90) progress.value += 12
  }, 600)

  let attempts = 0
  const poll = setInterval(async () => {
    attempts++
    if (attempts > 15) {
      clearInterval(poll); clearInterval(progressTimer)
      progress.value = 100; confirmed.value = true
      return
    }
    if (!jwt.value) return
    try {
      const res = await $fetch<any>(`${config.public.apiBaseUrl}/api/organizations/my`, {
        headers: { Authorization: `Bearer ${jwt.value}` }
      })
      const org = res?.result?.find((o: any) => o.id === orgId.value)
      if (org && org.orgPlan !== 'FREE') {
        newPlan.value = org.orgPlan
        clearInterval(poll); clearInterval(progressTimer)
        progress.value = 100; confirmed.value = true
      }
    } catch {}
  }, 1500)
})
</script>
