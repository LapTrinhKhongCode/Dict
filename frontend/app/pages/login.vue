<script setup lang="ts">
definePageMeta({ middleware: 'guest-only-client', layout: false })

import { ref, watch, onUnmounted } from "vue";
import { useRouter } from "vue-router";
import { useJwt } from "@/composables/useJwt";
import { useToast } from "@/composables/useToast";
import { useRuntimeConfig } from "#imports";

const mode = ref<"login" | "register" | "forgot" | "reset" | "pending">("login");
const username = ref("");
const password = ref("");
const email = ref("");
const otp = ref("");
const newPassword = ref("");
const confirmPassword = ref("");

// Trạng thái ẩn/hiện mật khẩu
const showPassword = ref(false);
const showNewPassword = ref(false);
const showConfirmPassword = ref(false);

const error = ref("");
const success = ref("");
const loading = ref(false);
const touched = ref(false);
const router = useRouter();
const { login } = useJwt();
const { showToast } = useToast();
const config = useRuntimeConfig();

// Field error state
const fieldErrors = ref<{ [k: string]: string }>({});

const isEmailValid = () => /.+@.+\..+/.test(email.value);

const isFormValid = () => {
  if (mode.value === "login") {
    return username.value.trim() && password.value.trim();
  } else if (mode.value === "register") {
    return (
      username.value.trim() &&
      password.value.trim() &&
      email.value.trim() &&
      isEmailValid()
    );
  } else if (mode.value === "forgot") {
    return email.value.trim() && isEmailValid();
  } else if (mode.value === "reset") {
    return (
      otp.value.trim() &&
      newPassword.value.trim() &&
      confirmPassword.value.trim() &&
      newPassword.value === confirmPassword.value
    );
  }
  return false;
};

// Clear error khi người dùng gõ lại (UX tốt hơn)
watch([username, password, email], () => {
  error.value = "";
  fieldErrors.value = {};
});

async function handleAuth() {
  touched.value = true;
  error.value = "";
  success.value = "";
  fieldErrors.value = {};

  if (!isFormValid()) {
    if (mode.value === "reset" && newPassword.value !== confirmPassword.value) {
      error.value = "Mật khẩu xác nhận không khớp.";
    }
    return;
  }

  loading.value = true;
  
  try {
    let url = "";
    let body = {};

    if (mode.value === "login") {
      url = `${config.public.apiBaseUrl}/api/Auth/login`;
      body = { username: username.value, password: password.value };
    } else if (mode.value === "register") {
      url = `${config.public.apiBaseUrl}/api/Auth/register`;
      body = { username: username.value, email: email.value, password: password.value };
    } else if (mode.value === "forgot") {
      url = `${config.public.apiBaseUrl}/api/Auth/forgot-password`;
      body = { email: email.value };
    } else if (mode.value === "reset") {
      url = `${config.public.apiBaseUrl}/api/Auth/reset-password`;
      body = {
        email: email.value,
        otp: otp.value,
        newPassword: newPassword.value,
        confirmPassword: confirmPassword.value,
      };
    }

    const response = await fetch(url, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    
    const data = await response.json();

    // 1. Xử lý lỗi validation từ server (.NET Data Annotations)
    if (data.errors) {
      Object.entries(data.errors).forEach(([key, msgs]) => {
        if (Array.isArray(msgs)) fieldErrors.value[key.toLowerCase()] = msgs[0];
      });
      error.value = data.title || data.message || "Lỗi dữ liệu đầu vào.";
      loading.value = false; // Phải tắt loading trước khi return
      return;
    }

    // 2. Xử lý lỗi logic (Email không tồn tại, user đã tồn tại, v.v...)
    if (!data.isSuccess && response.status !== 200) {
      error.value = data.message || "Có lỗi xảy ra từ máy chủ.";
      loading.value = false; 
      return;
    }

    // 3. XỬ LÝ THÀNH CÔNG THEO TỪNG LUỒNG
    
    // Đăng ký
    if (mode.value === "register") {
      mode.value = "pending"
      resendCooldown.value = 0
      resendSuccess.value = ''
      resendError.value = ''
      // Tự động bắt đầu cooldown 30s sau khi gửi mail lần đầu
      startCooldown(30)
    } 
    // Quên mật khẩu
    else if (mode.value === "forgot") {
      success.value = data.message || "Mã OTP đã được gửi đến email của bạn.";
      mode.value = "reset";
      touched.value = false;
    } 
    // Reset mật khẩu
    else if (mode.value === "reset") {
      success.value = "Đổi mật khẩu thành công! Vui lòng đăng nhập.";
      showToast("Đổi mật khẩu thành công!", "success");
      mode.value = "login";
      touched.value = false;
      password.value = ""; 
      otp.value = "";
      newPassword.value = "";
      confirmPassword.value = "";
      
      showPassword.value = false;
      showNewPassword.value = false;
      showConfirmPassword.value = false;
    } 
    // Đăng nhập
    else if (mode.value === "login" && data.result?.token) {
      login(
        data.result.token,
        data.result.username,
        data.result.avatarUrl,
        data.result.email,
        data.result.role,
        data.result.userId
      );
      showToast("Đăng nhập thành công!", "success");
      await router.push("/");
    } else {
      error.value = "Không nhận được token từ máy chủ.";
    }

  } catch (e) {
    error.value = "Lỗi kết nối mạng hoặc máy chủ không phản hồi.";
  } finally {
    // Đảm bảo loading LUÔN được tắt dù có lỗi hay không
    loading.value = false;
  }
}

function switchMode(newMode: "login" | "register" | "forgot") {
  mode.value = newMode;
  touched.value = false;
  fieldErrors.value = {};
  error.value = "";
  success.value = "";
  
  showPassword.value = false;
  showNewPassword.value = false;
  showConfirmPassword.value = false;
}

// ===== RESEND EMAIL =====
const resendCooldown = ref(0)
const resending = ref(false)
const resendSuccess = ref('')
const resendError = ref('')
let cooldownTimer: ReturnType<typeof setInterval> | null = null

function startCooldown(seconds = 30) {
  resendCooldown.value = seconds
  cooldownTimer = setInterval(() => {
    resendCooldown.value--
    if (resendCooldown.value <= 0 && cooldownTimer) clearInterval(cooldownTimer)
  }, 1000)
}

async function resendEmail() {
  if (!email.value || resending.value || resendCooldown.value > 0) return
  resending.value = true
  resendSuccess.value = ''
  resendError.value = ''
  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/Auth/resend-confirmation`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: email.value }),
    })
    const data = await res.json()
    if (!res.ok || !data.isSuccess) throw new Error(data.message || 'Gửi thất bại.')
    resendSuccess.value = 'Email xác nhận đã được gửi lại!'
    startCooldown(30)
  } catch (err: any) {
    resendError.value = err.message || 'Không thể gửi. Vui lòng thử lại.'
  } finally {
    resending.value = false
  }
}

onUnmounted(() => { if (cooldownTimer) clearInterval(cooldownTimer) })
</script>

<template>
  <div class="flex items-center justify-center min-h-screen bg-gray-100 dark:bg-neutral-900 transition-colors">
    <div class="bg-white dark:bg-neutral-800 rounded-lg shadow-lg p-8 w-full max-w-md border border-gray-200 dark:border-transparent">
      
      <div class="flex items-center justify-between mb-6">
        <h2 class="text-2xl font-bold text-gray-900 dark:text-white uppercase">
          {{ mode === "login" ? "Đăng nhập" : mode === "register" ? "Đăng ký" : mode === "pending" ? "Xác nhận Email" : mode === "forgot" ? "Quên mật khẩu" : "Đặt lại mật khẩu" }}
        </h2>
        
        <button
          v-if="mode === 'login' || mode === 'register'"
          @click="switchMode(mode === 'login' ? 'register' : 'login')"
          class="text-primary-600 dark:text-primary-400 hover:underline text-sm"
        >
          {{ mode === "login" ? "Chưa có tài khoản? ĐĂNG KÝ" : "Đã có tài khoản? ĐĂNG NHẬP" }}
        </button>
        <button
          v-if="mode === 'forgot' || mode === 'reset'"
          @click="switchMode('login')"
          class="text-primary-600 dark:text-primary-400 hover:underline text-sm"
        >
          Quay lại Đăng nhập
        </button>
        <button
          v-if="mode === 'pending'"
          @click="switchMode('login')"
          class="text-primary-600 dark:text-primary-400 hover:underline text-sm"
        >
          Quay lại Đăng nhập
        </button>
      </div>

      <div v-if="success" class="mb-4 text-green-600 dark:text-green-400 text-sm font-medium">
        {{ success }}
      </div>

      <div v-if="mode === 'login' || mode === 'register'" class="mb-4">
        <label for="username" class="block text-gray-700 dark:text-gray-200 mb-1">Username</label>
        <input
          id="username"
          v-model="username"
          class="w-full px-3 py-2 rounded bg-gray-100 dark:bg-neutral-700 text-gray-900 dark:text-white border border-gray-300 dark:border-transparent focus:outline-none focus:ring-2 focus:ring-primary-500"
        />
        <div v-if="touched && !username" class="text-xs text-red-600 dark:text-red-400 mt-1">
          Không được để trống username
        </div>
        <div v-if="fieldErrors.username" class="text-xs text-red-600 dark:text-red-400 mt-1">
          {{ fieldErrors.username }}
        </div>
      </div>

      <div v-if="mode === 'register' || mode === 'forgot'" class="mb-4">
        <label class="block text-gray-700 dark:text-gray-200 mb-1">Email</label>
        <input
          v-model="email"
          type="email"
          placeholder="Nhập email của bạn..."
          class="w-full px-3 py-2 rounded bg-gray-100 dark:bg-neutral-700 text-gray-900 dark:text-white border border-gray-300 dark:border-transparent focus:outline-none focus:ring-2 focus:ring-primary-500"
        />
        <div v-if="touched && !email" class="text-xs text-red-600 dark:text-red-400 mt-1">
          Email không được để trống
        </div>
        <div v-if="touched && email && !isEmailValid()" class="text-xs text-red-600 dark:text-red-400 mt-1">
          Email không hợp lệ
        </div>
        <div v-if="fieldErrors.email" class="text-xs text-red-600 dark:text-red-400 mt-1">
          {{ fieldErrors.email }}
        </div>
      </div>

      <div v-if="mode === 'login' || mode === 'register'" class="mb-4">
        <label for="password" class="block text-gray-700 dark:text-gray-200 mb-1">Password</label>
        <div class="relative">
          <input
            id="password"
            :type="showPassword ? 'text' : 'password'"
            v-model="password"
            class="w-full px-3 py-2 pr-10 rounded bg-gray-100 dark:bg-neutral-700 text-gray-900 dark:text-white border border-gray-300 dark:border-transparent focus:outline-none focus:ring-2 focus:ring-primary-500"
          />
          <button 
            type="button" 
            @click="showPassword = !showPassword"
            class="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200"
          >
            <svg v-if="!showPassword" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
              <path stroke-linecap="round" stroke-linejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z" />
              <path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
            </svg>
            <svg v-else xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5">
              <path stroke-linecap="round" stroke-linejoin="round" d="M3.98 8.223A10.477 10.477 0 001.934 12C3.226 16.338 7.244 19.5 12 19.5c.993 0 1.953-.138 2.863-.395M6.228 6.228A10.45 10.45 0 0112 4.5c4.756 0 8.773 3.162 10.065 7.498a10.523 10.523 0 01-4.293 5.774M6.228 6.228L3 3m3.228 3.228l3.65 3.65m7.894 7.894L21 21m-3.228-3.228l-3.65-3.65m0 0a3 3 0 10-4.243-4.243m4.242 4.242L9.88 9.88" />
            </svg>
          </button>
        </div>
        <div v-if="touched && !password" class="text-xs text-red-600 dark:text-red-400 mt-1">
          Không được để trống password
        </div>
        <div v-if="fieldErrors.password" class="text-xs text-red-600 dark:text-red-400 mt-1">
          {{ fieldErrors.password }}
        </div>
      </div>

      <div v-if="mode === 'login'" class="flex justify-end mb-4">
        <button @click="switchMode('forgot')" class="text-sm text-primary-600 dark:text-primary-400 hover:underline">
          Quên mật khẩu?
        </button>
      </div>

      <template v-if="mode === 'reset'">
        <div class="mb-4">
          <label class="block text-gray-700 dark:text-gray-200 mb-1">Email đang đặt lại</label>
          <input
            v-model="email"
            type="email"
            disabled
            class="w-full px-3 py-2 rounded bg-gray-200 dark:bg-neutral-600 text-gray-500 dark:text-gray-400 border border-gray-300 dark:border-transparent cursor-not-allowed"
          />
        </div>

        <div class="mb-4">
          <label class="block text-gray-700 dark:text-gray-200 mb-1">Mã xác nhận (OTP)</label>
          <input
            v-model="otp"
            type="text"
            placeholder="Nhập 6 số OTP từ email..."
            class="w-full px-3 py-2 rounded bg-gray-100 dark:bg-neutral-700 text-gray-900 dark:text-white border border-gray-300 dark:border-transparent focus:outline-none focus:ring-2 focus:ring-primary-500"
          />
          <div v-if="touched && !otp" class="text-xs text-red-600 dark:text-red-400 mt-1">
            Vui lòng nhập mã OTP
          </div>
        </div>

        <div class="mb-4">
          <label class="block text-gray-700 dark:text-gray-200 mb-1">Mật khẩu mới</label>
          <div class="relative">
            <input
              :type="showNewPassword ? 'text' : 'password'"
              v-model="newPassword"
              class="w-full px-3 py-2 pr-10 rounded bg-gray-100 dark:bg-neutral-700 text-gray-900 dark:text-white border border-gray-300 dark:border-transparent focus:outline-none focus:ring-2 focus:ring-primary-500"
            />
            <button 
              type="button" 
              @click="showNewPassword = !showNewPassword"
              class="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200"
            >
              <svg v-if="!showNewPassword" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5"><path stroke-linecap="round" stroke-linejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z" /><path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" /></svg>
              <svg v-else xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5"><path stroke-linecap="round" stroke-linejoin="round" d="M3.98 8.223A10.477 10.477 0 001.934 12C3.226 16.338 7.244 19.5 12 19.5c.993 0 1.953-.138 2.863-.395M6.228 6.228A10.45 10.45 0 0112 4.5c4.756 0 8.773 3.162 10.065 7.498a10.523 10.523 0 01-4.293 5.774M6.228 6.228L3 3m3.228 3.228l3.65 3.65m7.894 7.894L21 21m-3.228-3.228l-3.65-3.65m0 0a3 3 0 10-4.243-4.243m4.242 4.242L9.88 9.88" /></svg>
            </button>
          </div>
          <div v-if="touched && !newPassword" class="text-xs text-red-600 dark:text-red-400 mt-1">
            Vui lòng nhập mật khẩu mới
          </div>
        </div>

        <div class="mb-4">
          <label class="block text-gray-700 dark:text-gray-200 mb-1">Xác nhận mật khẩu mới</label>
          <div class="relative">
            <input
              :type="showConfirmPassword ? 'text' : 'password'"
              v-model="confirmPassword"
              class="w-full px-3 py-2 pr-10 rounded bg-gray-100 dark:bg-neutral-700 text-gray-900 dark:text-white border border-gray-300 dark:border-transparent focus:outline-none focus:ring-2 focus:ring-primary-500"
            />
            <button 
              type="button" 
              @click="showConfirmPassword = !showConfirmPassword"
              class="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200"
            >
              <svg v-if="!showConfirmPassword" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5"><path stroke-linecap="round" stroke-linejoin="round" d="M2.036 12.322a1.012 1.012 0 010-.639C3.423 7.51 7.36 4.5 12 4.5c4.638 0 8.573 3.007 9.963 7.178.07.207.07.431 0 .639C20.577 16.49 16.64 19.5 12 19.5c-4.638 0-8.573-3.007-9.963-7.178z" /><path stroke-linecap="round" stroke-linejoin="round" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" /></svg>
              <svg v-else xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="w-5 h-5"><path stroke-linecap="round" stroke-linejoin="round" d="M3.98 8.223A10.477 10.477 0 001.934 12C3.226 16.338 7.244 19.5 12 19.5c.993 0 1.953-.138 2.863-.395M6.228 6.228A10.45 10.45 0 0112 4.5c4.756 0 8.773 3.162 10.065 7.498a10.523 10.523 0 01-4.293 5.774M6.228 6.228L3 3m3.228 3.228l3.65 3.65m7.894 7.894L21 21m-3.228-3.228l-3.65-3.65m0 0a3 3 0 10-4.243-4.243m4.242 4.242L9.88 9.88" /></svg>
            </button>
          </div>
          <div v-if="touched && !confirmPassword" class="text-xs text-red-600 dark:text-red-400 mt-1">
            Vui lòng xác nhận mật khẩu
          </div>
          <div v-if="touched && confirmPassword && newPassword !== confirmPassword" class="text-xs text-red-600 dark:text-red-400 mt-1">
            Mật khẩu không khớp
          </div>
        </div>
      </template>

      <div v-if="error" class="text-red-600 dark:text-red-400 text-sm mb-4">
        {{ error }}
      </div>

      <!-- Màn chờ xác nhận email -->
      <div v-if="mode === 'pending'" class="text-center py-4 space-y-5">
        <div class="w-16 h-16 mx-auto rounded-full bg-blue-500/10 flex items-center justify-center">
          <svg class="w-8 h-8 text-blue-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"/>
          </svg>
        </div>
        <div>
          <p class="font-semibold text-gray-800 dark:text-white">Kiểm tra hộp thư của bạn!</p>
          <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
            Chúng tôi đã gửi link xác nhận tới<br/>
            <span class="font-medium text-blue-400">{{ email }}</span>
          </p>
        </div>

        <div class="border-t border-gray-200 dark:border-neutral-700 pt-4 space-y-2">
          <p class="text-xs text-gray-400">Không nhận được email?</p>
          <p v-if="resendSuccess" class="text-green-500 text-xs font-medium">✅ {{ resendSuccess }}</p>
          <p v-if="resendError" class="text-red-400 text-xs">{{ resendError }}</p>
          <button
            @click="resendEmail"
            :disabled="resendCooldown > 0 || resending"
            class="w-full py-2 rounded font-medium text-sm transition-all"
            :class="resendCooldown > 0 || resending
              ? 'bg-gray-100 dark:bg-neutral-700 text-gray-400 cursor-not-allowed'
              : 'bg-primary-600 hover:bg-primary-700 text-white'"
          >
            <span v-if="resending">Đang gửi...</span>
            <span v-else-if="resendCooldown > 0">Gửi lại sau {{ resendCooldown }}s</span>
            <span v-else>Gửi lại email xác nhận</span>
          </button>
        </div>
      </div>

      <button
        v-if="mode !== 'pending'"
        @click="handleAuth"
        :disabled="loading"
        class="w-full bg-primary-600 hover:bg-primary-700 text-white py-2 rounded font-medium disabled:opacity-60 transition-colors"
      >
        <span v-if="loading">Đang xử lý...</span>
        <span v-else>
          {{ 
            mode === "login" ? "Đăng nhập" : 
            mode === "register" ? "Đăng ký" : 
            mode === "forgot" ? "Nhận mã OTP" : "Xác nhận đổi mật khẩu" 
          }}
        </span>
      </button>
      
    </div>
  </div>
</template>