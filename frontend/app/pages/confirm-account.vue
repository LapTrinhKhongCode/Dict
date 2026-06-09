<template>
  <div class="flex items-center justify-center min-h-screen bg-[#0d1117]">
    <div class="bg-[#161b22] border border-[#30363d] shadow-2xl rounded-2xl p-8 w-full max-w-md text-center space-y-5">

      <!-- Loading -->
      <div v-if="loading" class="flex flex-col items-center gap-4 py-4">
        <div class="w-12 h-12 border-4 border-[#f0c040] border-t-transparent rounded-full animate-spin"></div>
        <p class="text-gray-300 font-medium">Đang xác nhận tài khoản...</p>
      </div>

      <!-- Kết quả -->
      <div v-else>
        <!-- Thành công -->
        <div v-if="success" class="flex flex-col items-center gap-3">
          <div class="w-16 h-16 rounded-full bg-green-500/10 flex items-center justify-center">
            <svg class="w-8 h-8 text-green-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"/>
            </svg>
          </div>
          <h2 class="text-xl font-bold text-green-400">Xác nhận thành công!</h2>
          <p class="text-gray-400 text-sm">Bạn đã được đăng nhập. Đang chuyển hướng...</p>
        </div>

        <!-- Lỗi -->
        <div v-if="error" class="flex flex-col items-center gap-4">
          <div class="w-16 h-16 rounded-full bg-red-500/10 flex items-center justify-center">
            <svg class="w-8 h-8 text-red-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/>
            </svg>
          </div>
          <h2 class="text-lg font-bold text-red-400">{{ error }}</h2>

          <!-- Resend section — chỉ hiện khi có email trong URL -->
          <div v-if="emailFromQuery" class="w-full pt-2 border-t border-[#30363d]">
            <p class="text-gray-500 text-xs mb-3">Link đã hết hạn hoặc đã dùng? Gửi lại email xác nhận.</p>

            <!-- Thông báo sau resend -->
            <p v-if="resendSuccess" class="text-green-400 text-sm font-medium mb-3">✅ {{ resendSuccess }}</p>
            <p v-if="resendError" class="text-red-400 text-sm mb-3">{{ resendError }}</p>

            <button
              @click="resendEmail"
              :disabled="resendCooldown > 0 || resending"
              class="w-full py-2.5 px-4 rounded-xl text-sm font-bold uppercase tracking-wider transition-all flex items-center justify-center gap-2"
              :class="resendCooldown > 0 || resending
                ? 'bg-[#21262d] border border-[#30363d] text-gray-500 cursor-not-allowed'
                : 'bg-[#f0c040] hover:bg-[#e3b330] text-black shadow-lg active:scale-95'"
            >
              <span v-if="resending" class="w-4 h-4 border-2 border-current border-t-transparent rounded-full animate-spin"></span>
              <span v-else-if="resendCooldown > 0">Gửi lại sau {{ resendCooldown }}s</span>
              <span v-else>Gửi lại email xác nhận</span>
            </button>
          </div>

          <a href="/register" class="text-[#58a6ff] text-xs hover:underline mt-1">← Quay lại đăng ký</a>
        </div>
      </div>

    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import { useJwt } from '~/composables/useJwt'
import { useRuntimeConfig, useRoute, useRouter } from '#imports'

definePageMeta({ layout: false })

const config = useRuntimeConfig()
const route = useRoute()
const router = useRouter()
const { logout, login } = useJwt()

const loading = ref(true)
const error = ref(null)
const success = ref(null)

const emailFromQuery = ref('')
const resending = ref(false)
const resendSuccess = ref('')
const resendError = ref('')
const resendCooldown = ref(0)
let cooldownTimer = null

function startCooldown(seconds = 30) {
  resendCooldown.value = seconds
  cooldownTimer = setInterval(() => {
    resendCooldown.value--
    if (resendCooldown.value <= 0) clearInterval(cooldownTimer)
  }, 1000)
}

async function resendEmail() {
  if (!emailFromQuery.value || resending.value || resendCooldown.value > 0) return
  resending.value = true
  resendSuccess.value = ''
  resendError.value = ''
  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/Auth/resend-confirmation`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: emailFromQuery.value }),
    })
    const data = await res.json()
    if (!res.ok || !data.isSuccess) throw new Error(data.message || 'Gửi thất bại.')
    resendSuccess.value = data.message
    startCooldown(30)
  } catch (err) {
    resendError.value = err.message || 'Không thể gửi email. Vui lòng thử lại.'
  } finally {
    resending.value = false
  }
}

onMounted(async () => {
  logout()

  const confirmationToken = route.query.token
  const userEmail = route.query.email
  emailFromQuery.value = userEmail || ''

  if (!confirmationToken || !userEmail) {
    loading.value = false
    error.value = 'Link xác nhận không hợp lệ hoặc thiếu thông tin.'
    return
  }

  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/Auth/confirm-email`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: userEmail, token: confirmationToken }),
    })
    const data = await res.json()

    if (!res.ok || !data.isSuccess) throw new Error(data.message || 'Xác nhận thất bại.')

    const result = data.result
    if (!result?.token) throw new Error('Không nhận được token đăng nhập từ máy chủ.')

    login(result.token, result.username, result.avatarUrl, result.email, result.role, result.userId)
    success.value = true
    loading.value = false
    setTimeout(() => router.push('/'), 2000)
  } catch (err) {
    loading.value = false
    error.value = err.message || 'Đã xảy ra lỗi. Vui lòng thử lại.'
  }
})

onUnmounted(() => clearInterval(cooldownTimer))
</script>