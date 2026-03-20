<template>
  <div class="flex items-center justify-center min-h-screen bg-gray-50">
    <div
      class="bg-white shadow-xl rounded-2xl p-8 w-full max-w-md text-center space-y-4 border border-gray-100"
    >
      <!-- Loading -->
      <div v-if="loading" class="animate-pulse">
        <div class="flex flex-col items-center space-y-3">
          <svg
            class="w-10 h-10 text-blue-500 animate-spin"
            fill="none"
            viewBox="0 0 24 24"
          >
            <circle
              class="opacity-25"
              cx="12"
              cy="12"
              r="10"
              stroke="currentColor"
              stroke-width="4"
            />
            <path
              class="opacity-75"
              fill="currentColor"
              d="M4 12a8 8 0 018-8v8H4z"
            />
          </svg>
          <h2 class="text-lg font-semibold text-gray-700">
            Đang xác nhận tài khoản...
          </h2>
        </div>
      </div>

      <!-- Kết quả -->
      <div v-else>
        <h2
          class="text-xl font-semibold transition-all duration-300"
          :class="success ? 'text-green-600' : 'text-red-600'"
        >
          {{ success || error }}
        </h2>

        <p v-if="success" class="text-gray-600 mt-2">
          Bạn sẽ được chuyển hướng về trang chủ trong giây lát...
        </p>

        <p v-if="error" class="text-gray-500 mt-2">
          Nếu lỗi vẫn tiếp tục, vui lòng đăng ký lại hoặc liên hệ hỗ trợ.
        </p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useJwt } from '~/composables/useJwt'
import { useRuntimeConfig, useRoute, useRouter } from '#imports'

const config = useRuntimeConfig()
const route = useRoute()
const router = useRouter()
const { logout, login } = useJwt()

const loading = ref(true)
const error = ref(null)
const success = ref(null)

onMounted(async () => {
  // 🧹 1. Logout mọi user hiện tại
  logout()

  const confirmationToken = route.query.token
  if (!confirmationToken) {
    loading.value = false
    error.value = 'Link xác nhận không hợp lệ.'
    return
  }

  try {
    // 📨 2. Gọi API xác nhận đăng ký
    const url = `${config.public.apiBaseUrl}/api/Auth/confirm-registration`
    const res = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ token: confirmationToken }),
    })
    const data = await res.json()

    if (!res.ok || !data.isSuccess) {
      throw new Error(data.message || 'Xác nhận thất bại.')
    }

    // ✅ 3. Nhận token và thông tin user
    const result = data.result
    const loginToken = result?.token
    if (!loginToken) throw new Error('Không nhận được token đăng nhập từ máy chủ.')

    // 🌟 4. Tự động login lại bằng useJwt()
    login(
      loginToken,
      result?.username,
      result?.avatarUrl,
      result?.email,
      result?.role,
      result?.userId
    )

    success.value = '✅ Xác nhận thành công! Bạn đã được đăng nhập.'
    loading.value = false

    // ⏳ 5. Chuyển hướng về trang chủ
    setTimeout(() => router.push('/'), 2000)
  } catch (err) {
    loading.value = false
    error.value = err.message || 'Đã xảy ra lỗi. Vui lòng thử lại.'
  }
})
</script>

<style scoped>
h2 {
  transition: all 0.3s ease-in-out;
}
</style>
