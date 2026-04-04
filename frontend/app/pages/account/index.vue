<template>
  <div class="min-h-screen p-4 sm:p-8 bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-white transition-colors">
    <div class="max-w-6xl mx-auto flex flex-col md:flex-row gap-8">
      
      <div class="w-full md:w-1/4 shrink-0">
        <div class="bg-white dark:bg-gray-800 rounded-lg p-6 sticky top-24 shadow-sm border border-gray-200 dark:border-gray-700">
          <div class="flex flex-col items-center mb-6">
            <div
              :class="[
                'w-32 h-32 rounded-full mb-4',
                role === 'Admin'
                  ? 'p-1 admin-avatar-border'
                  : role === 'Premium'
                  ? 'p-1 premium-avatar-border'
                  : 'border-4 border-sky-500',
              ]"
            >
              <img
                :src="profile.avatarPreview || profile.avatarUrl"
                alt="Avatar"
                class="w-full h-full rounded-full object-cover bg-gray-700"
              />
            </div>
            <h2 class="text-2xl font-bold text-black dark:text-white text-center">
              {{ username || "Người dùng" }}
            </h2>
            <p class="text-sm text-gray-600 dark:text-gray-300">
              {{
                role === "Admin"
                  ? "Admin Member"
                  : role === "Premium"
                  ? "Premium Member"
                  : "Thành viên"
              }}
            </p>
          </div>

          <nav class="space-y-2">
            <button @click="selectTab('general')" :class="['nav-button', { active: currentTab === 'general' }]">Giới thiệu chung</button>
            <button @click="selectTab('security')" :class="['nav-button', { active: currentTab === 'security' }]">Bảo mật</button>
            <button @click="selectTab('theme')" :class="['nav-button', { active: currentTab === 'theme' }]">Đổi nền</button>
          </nav>
        </div>
      </div>

      <div class="w-full md:w-3/4 space-y-6">
        
        <section v-if="currentTab === 'general'" class="bg-white dark:bg-gray-800 rounded-lg p-6 sm:p-8 shadow-sm border border-gray-200 dark:border-gray-700">
          <h2 class="text-2xl font-bold text-black dark:text-white mb-6">Thông tin cá nhân</h2>

          <div class="space-y-6">
            <div>
              <label for="email" class="form-label">Email</label>
              <input
                type="email"
                id="email"
                v-model="profile.email"
                class="form-input"
                placeholder="Nhập email mới..."
              />
            </div>

            <div>
              <label for="name" class="form-label">Username</label>
              <input
                type="text"
                id="name"
                v-model="profile.name"
                class="form-input opacity-60 bg-gray-100 cursor-not-allowed"
                disabled
              />
              <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">Bạn không thể thay đổi username.</p>
            </div>

            <div>
              <label class="form-label">Ảnh đại diện</label>
              <div class="flex items-center gap-4">
                <img
                  :src="profile.avatarPreview || profile.avatarUrl"
                  alt="Current Avatar"
                  class="w-16 h-16 rounded-full object-cover bg-gray-700 border border-gray-300 dark:border-gray-600"
                />
                <input
                  type="file"
                  @change="handleFileSelect"
                  accept="image/*"
                  class="text-sm text-gray-500 file:mr-4 file:py-2.5 file:px-5 file:rounded-lg file:border-0 file:text-sm file:font-semibold file:bg-sky-50 file:text-sky-600 hover:file:bg-sky-100 dark:file:bg-sky-900/30 dark:file:text-sky-400 dark:hover:file:bg-sky-900/50 cursor-pointer transition-colors"
                />
              </div>
            </div>

            <div class="text-right pt-4">
              <button @click="handleProfileUpdate" class="form-button-primary" :disabled="isSaving || isSendingOtp">
                <span v-if="isSendingOtp">Đang gửi mã...</span>
                <span v-else-if="isSaving">Đang cập nhật...</span>
                <span v-else>Cập nhật thông tin</span>
              </button>
            </div>
          </div>
        </section>

        <section v-if="currentTab === 'security'" class="bg-white dark:bg-gray-800 rounded-lg p-6 sm:p-8 shadow-sm border border-gray-200 dark:border-gray-700">
          <h2 class="text-2xl font-bold text-black dark:text-white mb-6">Bảo mật</h2>
          <div class="space-y-6 max-w-lg">
            <div>
              <label for="current-pass" class="form-label">Mật khẩu hiện tại</label>
              <div class="input-with-toggle relative">
                <input :type="showPass.current ? 'text' : 'password'" id="current-pass" v-model="passwords.current" class="form-input pr-10" placeholder="••••••••" autocomplete="current-password" />
                <button type="button" @click="showPass.current = !showPass.current" class="password-toggle">
                  <svg v-if="!showPass.current" xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor"><path d="M10 12a2 2 0 100-4 2 2 0 000 4z" /><path fill-rule="evenodd" d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.022 7-9.542 7S1.732 14.057.458 10zM14 10a4 4 0 11-8 0 4 4 0 018 0z" clip-rule="evenodd" /></svg>
                  <svg v-else xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M3.707 2.293a1 1 0 00-1.414 1.414l14 14a1 1 0 001.414-1.414l-1.473-1.473A10.014 10.014 0 0019.542 10C18.268 5.943 14.478 3 10 3a9.958 9.958 0 00-4.512 1.074L3.707 2.293zM10 12a2 2 0 100-4 2 2 0 000 4z" clip-rule="evenodd" /><path d="M10 17a9.953 9.953 0 01-4.512-1.074l-1.473 1.473a1 1 0 101.414 1.414l14-14a1 1 0 10-1.414-1.414L10 17z" /></svg>
                </button>
              </div>
            </div>

            <div>
              <label for="new-pass" class="form-label">Mật khẩu mới</label>
              <div class="input-with-toggle relative">
                <input :type="showPass.new ? 'text' : 'password'" id="new-pass" v-model="passwords.new" class="form-input pr-10" placeholder="••••••••" autocomplete="new-password" />
                <button type="button" @click="showPass.new = !showPass.new" class="password-toggle">
                  <svg v-if="!showPass.new" xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor"><path d="M10 12a2 2 0 100-4 2 2 0 000 4z" /><path fill-rule="evenodd" d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.022 7-9.542 7S1.732 14.057.458 10zM14 10a4 4 0 11-8 0 4 4 0 018 0z" clip-rule="evenodd" /></svg>
                  <svg v-else xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M3.707 2.293a1 1 0 00-1.414 1.414l14 14a1 1 0 001.414-1.414l-1.473-1.473A10.014 10.014 0 0019.542 10C18.268 5.943 14.478 3 10 3a9.958 9.958 0 00-4.512 1.074L3.707 2.293zM10 12a2 2 0 100-4 2 2 0 000 4z" clip-rule="evenodd" /><path d="M10 17a9.953 9.953 0 01-4.512-1.074l-1.473 1.473a1 1 0 101.414 1.414l14-14a1 1 0 10-1.414-1.414L10 17z" /></svg>
                </button>
              </div>
            </div>

            <div>
              <label for="confirm-pass" class="form-label">Nhập lại mật khẩu mới</label>
              <div class="input-with-toggle relative">
                <input :type="showPass.confirm ? 'text' : 'password'" id="confirm-pass" v-model="passwords.confirm" class="form-input pr-10" placeholder="••••••••" autocomplete="new-password" />
                <button type="button" @click="showPass.confirm = !showPass.confirm" class="password-toggle">
                  <svg v-if="!showPass.confirm" xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor"><path d="M10 12a2 2 0 100-4 2 2 0 000 4z" /><path fill-rule="evenodd" d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.022 7-9.542 7S1.732 14.057.458 10zM14 10a4 4 0 11-8 0 4 4 0 018 0z" clip-rule="evenodd" /></svg>
                  <svg v-else xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M3.707 2.293a1 1 0 00-1.414 1.414l14 14a1 1 0 001.414-1.414l-1.473-1.473A10.014 10.014 0 0019.542 10C18.268 5.943 14.478 3 10 3a9.958 9.958 0 00-4.512 1.074L3.707 2.293zM10 12a2 2 0 100-4 2 2 0 000 4z" clip-rule="evenodd" /><path d="M10 17a9.953 9.953 0 01-4.512-1.074l-1.473 1.473a1 1 0 101.414 1.414l14-14a1 1 0 10-1.414-1.414L10 17z" /></svg>
                </button>
              </div>
            </div>

            <div class="text-right pt-4">
              <button @click="handleChangePassword" class="form-button-primary" :disabled="isChangingPassword">
                <span v-if="isChangingPassword">Đang đổi...</span>
                <span v-else>Đổi mật khẩu</span>
              </button>
            </div>
          </div>
        </section>

        <section v-if="currentTab === 'theme'" class="bg-white dark:bg-gray-800 rounded-lg p-6 sm:p-8 shadow-sm border border-gray-200 dark:border-gray-700">
          <h2 class="text-2xl font-bold text-black dark:text-white mb-6">Đổi nền</h2>
          <div class="space-y-6">
            <div>
              <label class="form-label text-black dark:text-white">Chọn màu nền</label>
              <div class="flex items-center gap-3 mt-2">
                <span class="form-label mb-0">Chế độ ban đêm</span>
                <label class="relative inline-block w-12 h-6 cursor-pointer">
                  <input type="checkbox" v-model="isDarkMode" @change="toggleTheme" class="sr-only peer" />
                  <span class="absolute inset-0 rounded-full bg-gray-300 dark:bg-gray-600 transition-colors peer-checked:bg-sky-500"></span>
                  <span class="absolute left-0.5 top-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform duration-300 peer-checked:translate-x-6"></span>
                </label>
                <span class="text-sm text-gray-500 dark:text-gray-300">{{ isDarkMode ? "Bật" : "Tắt" }}</span>
              </div>
            </div>
          </div>
        </section>

      </div>
    </div>

    <Transition name="fade">
      <div v-if="showOtpModal" class="fixed inset-0 z-[100] flex items-center justify-center bg-black/60 backdrop-blur-sm px-4">
        <div class="bg-white dark:bg-gray-800 p-8 rounded-2xl shadow-2xl max-w-sm w-full border border-gray-200 dark:border-gray-700 text-center">
          <div class="w-16 h-16 bg-sky-100 dark:bg-sky-900/30 text-sky-500 rounded-full flex items-center justify-center mx-auto mb-4">
            <svg class="w-8 h-8" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z"/></svg>
          </div>
          <h3 class="text-xl font-bold text-gray-900 dark:text-white mb-2">Xác nhận Email mới</h3>
          <p class="text-sm text-gray-500 dark:text-gray-400 mb-6">
            Mã OTP 6 số đã được gửi đến <br/><b class="text-gray-900 dark:text-gray-200">{{ profile.email }}</b>. Vui lòng kiểm tra hộp thư.
          </p>
          
          <input 
            v-model="otpCode" 
            type="text" 
            placeholder="000000" 
            class="form-input mb-6 text-center tracking-[1em] font-bold text-xl uppercase placeholder-gray-300" 
            maxlength="6" 
          />
          
          <div class="flex gap-3">
            <button @click="showOtpModal = false" class="flex-1 px-4 py-2.5 bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-300 rounded-xl font-bold hover:bg-gray-200 dark:hover:bg-gray-600 transition-colors">
              Hủy
            </button>
            <button @click="verifyOtpAndUpdate" :disabled="isVerifyingOtp || otpCode.length < 6" class="flex-1 form-button-primary rounded-xl flex items-center justify-center">
              <svg v-if="isVerifyingOtp" class="w-5 h-5 animate-spin mr-2" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"/><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z"/></svg>
              {{ isVerifyingOtp ? 'Đang thử...' : 'Xác nhận' }}
            </button>
          </div>
        </div>
      </div>
    </Transition>

  </div>
</template>

<script setup lang="ts">
import { ref, reactive, inject, onMounted } from "vue";
import { useJwt } from "~/composables/useJwt";
import { useToast } from "~/composables/useToast";

definePageMeta({
  middleware: "auth-client",
});

const { username, email, avatarUrl, role, jwt } = useJwt();
const config = useRuntimeConfig();
const BASE_URL = config.public.apiBaseUrl || "https://localhost:7084";

type Tab = "general" | "security" | "theme";
const currentTab = ref<Tab>("general");

const isSaving = ref(false);
const isChangingPassword = ref(false);
const { showToast } = useToast();

// OTP States
const isSendingOtp = ref(false);
const showOtpModal = ref(false);
const isVerifyingOtp = ref(false);
const otpCode = ref("");

// profile reactive
const profile = reactive({
  name: username.value || "",
  email: email.value || "",
  avatarUrl: avatarUrl.value || "https://storage.googleapis.com/maker-studio-project-media-prod/11072a6a-d42a-41e9-8902-613d0a6a0e69/images/220b33b9-a99f-4318-9126-d66a6a96f131.jpg",
  avatarFile: null as File | null,
  avatarPreview: null as string | null,
});

const passwords = reactive({ current: "", new: "", confirm: "" });
const showPass = reactive({ current: false, new: false, confirm: false });

const theme = inject("theme") as { value: "dark" | "light" } | undefined;
const setTheme = inject("setTheme") as ((mode: "dark" | "light") => void) | undefined;
const isDarkMode = ref<boolean>(true);

onMounted(() => {
  isDarkMode.value = !!(theme?.value === "dark");
});

function toggleTheme() {
  const mode = isDarkMode.value ? "dark" : "light";
  setTheme?.(mode);
  showToast(`Đã chuyển sang chế độ ${mode === "dark" ? "ban đêm 🌙" : "ban ngày ☀️"}`, "success");
}

function selectTab(tab: Tab) {
  currentTab.value = tab;
}

function handleFileSelect(event: Event) {
  const target = event.target as HTMLInputElement;
  if (target.files && target.files[0]) {
    const file = target.files[0];
    profile.avatarFile = file;
    const reader = new FileReader();
    reader.onload = (e) => (profile.avatarPreview = e.target?.result as string);
    reader.readAsDataURL(file);
  }
}

// Validate định dạng email
const isEmailValid = (emailStr: string) =>
  /^[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}$/.test(emailStr.trim());

// Đọc message lỗi từ response backend
async function parseErrorMessage(res: Response): Promise<string> {
  try {
    const data = await res.json();
    return data?.message || data?.title || Object.values(data?.errors || {}).flat().join(" ") || "";
  } catch {
    return "";
  }
}

// 1. Phân luồng cập nhật profile
async function handleProfileUpdate() {
  if (!username.value) {
    showToast("Không tìm thấy tên người dùng để cập nhật.", "error");
    return;
  }

  const currentSavedEmail = (email.value || "").trim();
  const newEmail = profile.email.trim();

  if (newEmail !== currentSavedEmail) {
    // Có đổi email → validate trước
    if (!isEmailValid(newEmail)) {
      showToast("Định dạng email không hợp lệ. Vui lòng kiểm tra lại.", "error");
      return;
    }
    await sendOtpForEmailChange();
  } else {
    // Không đổi email → chỉ cập nhật avatar
    await executeUpdateApi();
  }
}

// 2. Gửi OTP đến email mới
async function sendOtpForEmailChange() {
  isSendingOtp.value = true;
  try {
    const res = await fetch(`${BASE_URL}/api/users/send-email-otp`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${jwt.value}`,
      },
      body: JSON.stringify({ newEmail: profile.email.trim() }),
    });

    if (!res.ok) {
      const errMsg = await parseErrorMessage(res);
      const lower = errMsg.toLowerCase();

      if (res.status === 400) {
        if (
          lower.includes("exist") ||
          lower.includes("tồn tại") ||
          lower.includes("already") ||
          lower.includes("taken") ||
          lower.includes("used")
        ) {
          showToast("Email này đã được sử dụng bởi tài khoản khác. Vui lòng chọn email khác.", "error");
        } else if (
          lower.includes("invalid") ||
          lower.includes("không hợp lệ") ||
          lower.includes("not found") ||
          lower.includes("không tồn tại") ||
          lower.includes("format")
        ) {
          showToast("Email không hợp lệ hoặc không tồn tại. Vui lòng kiểm tra lại.", "error");
        } else {
          showToast(errMsg || "Yêu cầu không hợp lệ. Vui lòng thử lại.", "error");
        }
      } else if (res.status === 500) {
        showToast("Không thể gửi email xác nhận. Vui lòng thử lại sau.", "error");
      } else {
        showToast(errMsg || `Gửi OTP thất bại (${res.status}). Vui lòng thử lại.`, "error");
      }

      profile.email = email.value || ""; // Phục hồi email cũ
      return;
    }

    // Thành công → mở modal OTP
    otpCode.value = "";
    showOtpModal.value = true;
    showToast("Đã gửi mã xác nhận đến email mới. Vui lòng kiểm tra hộp thư!", "success");
  } catch {
    showToast("Lỗi kết nối. Vui lòng kiểm tra mạng và thử lại.", "error");
    profile.email = email.value || "";
  } finally {
    isSendingOtp.value = false;
  }
}

// 3. Xác thực OTP và tiến hành update
async function verifyOtpAndUpdate() {
  isVerifyingOtp.value = true;
  try {
    const res = await fetch(`${BASE_URL}/api/users/verify-email-otp`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${jwt.value}`,
      },
      body: JSON.stringify({ newEmail: profile.email.trim(), otp: otpCode.value.trim() }),
    });

    if (!res.ok) {
      const errMsg = await parseErrorMessage(res);
      if (res.status === 400) {
        showToast(errMsg || "Mã OTP không chính xác hoặc đã hết hạn. Vui lòng thử lại.", "error");
      } else {
        showToast("Xác minh OTP thất bại. Vui lòng thử lại.", "error");
      }
      return;
    }

    showOtpModal.value = false;
    await executeUpdateApi();
  } catch {
    showToast("Lỗi kết nối khi xác minh OTP.", "error");
  } finally {
    isVerifyingOtp.value = false;
  }
}

// 4. Gọi API cập nhật thực sự
async function executeUpdateApi() {
  isSaving.value = true;
  const formData = new FormData();
  formData.append("Email", profile.email.trim());
  if (profile.avatarFile) formData.append("AvatarUrl", profile.avatarFile);

  try {
    const res = await fetch(`${BASE_URL}/api/users/by-username/${username.value}`, {
      method: "PUT",
      headers: { Authorization: `Bearer ${jwt.value}` },
      body: formData,
    });

    if (!res.ok) {
      const errMsg = await parseErrorMessage(res);
      throw new Error(errMsg || `Cập nhật thất bại (${res.status})`);
    }

    const json = await res.json().catch(() => ({}));
    const updatedUser = json?.result ?? json;

    showToast("Cập nhật thông tin thành công!", "success");

    if (updatedUser?.email) {
      profile.email = updatedUser.email;
    }
    if (updatedUser?.avatarUrl) {
      const newAvatarUrl = `${updatedUser.avatarUrl}?t=${Date.now()}`;
      (avatarUrl as any).value = newAvatarUrl;
      profile.avatarUrl = newAvatarUrl;
    }
    profile.avatarFile = null;
    profile.avatarPreview = null;
  } catch (err: any) {
    showToast(`Lỗi cập nhật: ${err.message}`, "error");
    profile.email = email.value || "";
  } finally {
    isSaving.value = false;
  }
}

// 5. Đổi mật khẩu
async function handleChangePassword() {
  if (!passwords.current || !passwords.new) {
    showToast("Vui lòng nhập đầy đủ mật khẩu.", "error");
    return;
  }
  if (passwords.new !== passwords.confirm) {
    showToast("Mật khẩu mới không khớp!", "error");
    return;
  }

  isChangingPassword.value = true;
  try {
    const res = await fetch(`${BASE_URL}/api/Users/change-password`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${jwt.value}`,
      },
      body: JSON.stringify({
        username: username.value,
        oldPassword: passwords.current,
        newPassword: passwords.new,
      }),
    });

    if (!res.ok) {
      const errMsg = await parseErrorMessage(res);
      throw new Error(errMsg || `Đổi mật khẩu thất bại (${res.status})`);
    }

    showToast("Đổi mật khẩu thành công!", "success");
    passwords.current = "";
    passwords.new = "";
    passwords.confirm = "";
  } catch (err: any) {
    showToast(`Lỗi: ${err.message}`, "error");
  } finally {
    isChangingPassword.value = false;
  }
}
</script>
<style scoped>
.nav-button {
  width: 100%;
  text-align: left;
  padding: 0.75rem 1rem;
  border-radius: 0.5rem;
  transition: background-color 0.2s, color 0.2s;
  font-weight: 500;
  color: #34373d; 
}
.dark .nav-button {
  color: white; 
}
.nav-button.active {
  background-color: #0ea5e9;
  color: white;
  font-weight: 600;
}
.nav-button:not(.active):hover {
  background-color: #0ea5e9;
  color: white;
}
.form-label {
  display: block;
  font-size: 0.875rem;
  font-weight: 600;
  color: inherit;
  margin-bottom: 0.375rem;
}
.form-input {
  width: 100%;
  background-color: #f9fafb;
  border: 1px solid #d1d5db;
  border-radius: 0.5rem;
  padding: 0.75rem 1rem;
  color: #111827;
  outline: none;
  transition: all 0.2s;
}
.dark .form-input {
  background-color: #1f2937;
  border-color: #374151;
  color: #f9fafb;
}
.form-input:focus {
  border-color: #0ea5e9;
  box-shadow: 0 0 0 3px rgba(14, 165, 233, 0.2);
}
.form-input:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
.form-button-primary {
  background-color: #0ea5e9;
  color: white;
  font-weight: 700;
  padding: 0.625rem 1.5rem;
  border-radius: 0.5rem;
  transition: all 0.2s;
}
.form-button-primary:hover:not(:disabled) {
  background-color: #0284c7;
}
.form-button-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
.password-toggle {
  position: absolute;
  top: 50%;
  right: 0.75rem;
  transform: translateY(-50%);
  color: #9ca3af;
  background: none;
  border: none;
  cursor: pointer;
  padding: 0.25rem;
}
.password-toggle:hover {
  color: #111827;
}
.dark .password-toggle:hover {
  color: #f9fafb;
}

/* Animations */
.fade-enter-active, .fade-leave-active { transition: opacity 0.3s ease; }
.fade-enter-from, .fade-leave-to { opacity: 0; }

@keyframes glitter {
  0% { background-position: 0% 50%; }
  50% { background-position: 100% 50%; }
  100% { background-position: 0% 50%; }
}

.premium-avatar-border {
  background: linear-gradient(120deg, #f0b90b, #f8d442, #ffeb7e, #f8d442, #f0b90b);
  background-size: 300% 300%;
  animation: glitter 3s linear infinite;
}

.admin-avatar-border {
  background: linear-gradient(120deg, #065aae, #269edf, #ffb0fe, #269edf, #065aae);
  background-size: 300% 300%;
  animation: glitter 3s linear infinite;
}
</style>