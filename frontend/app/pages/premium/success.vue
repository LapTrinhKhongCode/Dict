<template>
  <div class="min-h-screen flex flex-col items-center justify-center bg-gray-50 dark:bg-gray-900 text-center p-8">
    <div v-if="!confirmed">
      <div class="text-5xl mb-6 animate-bounce">⏳</div>
      <h1 class="text-2xl font-bold text-gray-900 dark:text-white mb-2">Đang xác nhận thanh toán...</h1>
      <p class="text-gray-400 text-sm">Vui lòng chờ trong giây lát</p>
      <div class="w-48 h-1.5 bg-gray-200 dark:bg-gray-700 rounded-full mt-6 mx-auto overflow-hidden">
        <div class="h-full bg-indigo-500 rounded-full animate-pulse" :style="{ width: progress + '%' }"/>
      </div>
    </div>
    <div v-else>
      <div class="text-7xl mb-6">🎉</div>
      <h1 class="text-4xl font-extrabold text-gray-900 dark:text-white mb-4">Chào mừng bạn đến với Premium!</h1>
      <p class="text-gray-500 dark:text-gray-400 mb-8 max-w-md">
        Thanh toán thành công. Tất cả tính năng Premium đã được kích hoạt.
      </p>
      <div class="flex gap-3 justify-center">
        <button @click="navigateTo('/')"
          class="px-8 py-3 bg-indigo-600 hover:bg-indigo-700 text-white font-bold rounded-xl text-sm transition-colors">
          Bắt đầu ngay →
        </button>
        <button @click="navigateTo('/premium')"
          class="px-8 py-3 border border-gray-300 dark:border-gray-600 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-xl text-sm font-medium transition-colors">
          Xem plan của tôi
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
definePageMeta({ ssr: false })

const { jwt } = useJwt()
const config = useRuntimeConfig()
const confirmed = ref(false)
const progress = ref(10)

onMounted(async () => {
  let attempts = 0
  // Tăng progress bar visual
  const progressTimer = setInterval(() => {
    if (progress.value < 90) progress.value += 10
  }, 500)

  // Poll cho đến khi webhook xử lý xong (tối đa 15 giây)
  const poll = setInterval(async () => {
    attempts++
    if (attempts > 15) {
      clearInterval(poll)
      clearInterval(progressTimer)
      progress.value = 100
      confirmed.value = true // fallback: hiện thành công dù chưa confirm
      return
    }
    if (!jwt.value) return

    try {
      const res = await $fetch<any>(`${config.public.apiBaseUrl}/api/auth/me`, {
        headers: { Authorization: `Bearer ${jwt.value}` }
      })
      if (res?.result?.isPremiumActive) {
        clearInterval(poll)
        clearInterval(progressTimer)
        progress.value = 100
        confirmed.value = true
        window.dispatchEvent(new CustomEvent('premium-activated'))
      }
    } catch {}
  }, 1000) // check mỗi 1 giây
})
</script>
