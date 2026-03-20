<template>
  <div
    class="min-h-screen p-4 sm:p-8 bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-white transition-colors"
  >
    <div class="max-w-6xl mx-auto flex flex-col md:flex-row gap-8">
      <!-- SIDEBAR -->
      <div class="w-full md:w-1/4 shrink-0">
        <div class="bg-white dark:bg-gray-800 rounded-lg p-6 sticky top-24">
          <div class="flex flex-col items-center mb-6">
            <div
              :class="[
                'w-32 h-32 rounded-full mb-4', // Base size, shape, and margin
                role === 'Admin'
                  ? 'p-1 admin-avatar-border' // IF Admin: Apply diamond style
                  : role === 'Premium'
                  ? 'p-1 premium-avatar-border' // ELSE IF Premium: Apply gold style
                  : 'border-4 border-sky-500', // ELSE User: Apply standard blue border
              ]"
            >
              <img
                :src="avatarUrl || defaultAvatar"
                alt="Avatar"
                class="w-full h-full rounded-full object-cover bg-gray-700"
              />
            </div>
            <h2
              class="text-2xl font-bold text-black dark:text-white text-center"
            >
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
            <button
              @click="selectTab('general')"
              :class="['nav-button', { active: currentTab === 'general' }]"
            >
              Giới thiệu chung
            </button>
            <button
              @click="selectTab('security')"
              :class="['nav-button', { active: currentTab === 'security' }]"
            >
              Bảo mật
            </button>
            <button
              @click="selectTab('theme')"
              :class="['nav-button', { active: currentTab === 'theme' }]"
            >
              Đổi nền
            </button>
          </nav>
        </div>
      </div>

      <!-- MAIN -->
      <div class="w-full md:w-3/4 space-y-6">
        <!-- GENERAL -->
        <section
          v-if="currentTab === 'general'"
          class="bg-white dark:bg-gray-800 rounded-lg p-6 sm:p-8"
        >
          <h2 class="text-2xl font-bold text-black dark:text-white mb-6">
            Thông tin cá nhân
          </h2>

          <div class="space-y-6">
            <div>
              <label for="email" class="form-label">Email</label>
              <input
                type="email"
                id="email"
                :value="email"
                class="form-input"
              />
            </div>

            <div>
              <label for="name" class="form-label">Username</label>
              <input
                type="text"
                id="name"
                v-model="profile.name"
                class="form-input"
                placeholder="Tên của bạn"
                disabled
              />
              <p class="text-xs text-gray-500 dark:text-gray-400 mt-1">
                Bạn không thể thay đổi username.
              </p>
            </div>

            <div>
              <label class="form-label">Ảnh đại diện</label>
              <div class="flex items-center gap-4">
                <img
                  :src="profile.avatarPreview || profile.avatarUrl"
                  alt="Current Avatar"
                  class="w-16 h-16 rounded-full object-cover bg-gray-700"
                />
                <input
                  type="file"
                  @change="handleFileSelect"
                  accept="image/*"
                  class="text-sm text-gray-400 file:mr-4 file:py-2 file:px-4 file:rounded-full file:border-0 file:text-sm file:font-semibold file:bg-sky-600 file:text-white hover:file:bg-sky-700 cursor-pointer"
                />
              </div>
            </div>

            <div class="text-right pt-4">
              <button
                @click="handleProfileUpdate"
                class="form-button-primary"
                :disabled="isSaving"
              >
                <span v-if="isSaving">Đang cập nhật...</span>
                <span v-else>Cập nhật thông tin</span>
              </button>
            </div>
          </div>
        </section>

        <!-- SECURITY -->
        <section
          v-if="currentTab === 'security'"
          class="bg-white dark:bg-gray-800 rounded-lg p-6 sm:p-8"
        >
          <h2 class="text-2xl font-bold text-black dark:text-white mb-6">
            Bảo mật
          </h2>

          <div class="space-y-6 max-w-lg">
            <div>
              <label for="current-pass" class="form-label"
                >Mật khẩu hiện tại</label
              >
              <div class="input-with-toggle relative">
                <input
                  :type="showPass.current ? 'text' : 'password'"
                  id="current-pass"
                  v-model="passwords.current"
                  class="form-input pr-10"
                  placeholder="••••••••"
                  autocomplete="current-password"
                />
                <button
                  type="button"
                  @click="showPass.current = !showPass.current"
                  class="password-toggle"
                  :aria-pressed="showPass.current"
                  :aria-label="
                    showPass.current ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'
                  "
                >
                  <svg
                    v-if="!showPass.current"
                    xmlns="http://www.w3.org/2000/svg"
                    class="h-5 w-5"
                    viewBox="0 0 20 20"
                    fill="currentColor"
                  >
                    <path d="M10 12a2 2 0 100-4 2 2 0 000 4z" />
                    <path
                      fill-rule="evenodd"
                      d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.022 7-9.542 7S1.732 14.057.458 10zM14 10a4 4 0 11-8 0 4 4 0 018 0z"
                      clip-rule="evenodd"
                    />
                  </svg>
                  <svg
                    v-else
                    xmlns="http://www.w3.org/2000/svg"
                    class="h-5 w-5"
                    viewBox="0 0 20 20"
                    fill="currentColor"
                  >
                    <path
                      fill-rule="evenodd"
                      d="M3.707 2.293a1 1 0 00-1.414 1.414l14 14a1 1 0 001.414-1.414l-1.473-1.473A10.014 10.014 0 0019.542 10C18.268 5.943 14.478 3 10 3a9.958 9.958 0 00-4.512 1.074L3.707 2.293zM10 12a2 2 0 100-4 2 2 0 000 4z"
                      clip-rule="evenodd"
                    />
                    <path
                      d="M10 17a9.953 9.953 0 01-4.512-1.074l-1.473 1.473a1 1 0 101.414 1.414l14-14a1 1 0 10-1.414-1.414L10 17z"
                    />
                  </svg>
                </button>
              </div>
            </div>

            <div>
              <label for="new-pass" class="form-label">Mật khẩu mới</label>
              <div class="input-with-toggle relative">
                <input
                  :type="showPass.new ? 'text' : 'password'"
                  id="new-pass"
                  v-model="passwords.new"
                  class="form-input pr-10"
                  placeholder="••••••••"
                  autocomplete="new-password"
                />
                <button
                  type="button"
                  @click="showPass.new = !showPass.new"
                  class="password-toggle"
                  :aria-pressed="showPass.new"
                  :aria-label="showPass.new ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'"
                >
                  <svg
                    v-if="!showPass.new"
                    xmlns="http://www.w3.org/2000/svg"
                    class="h-5 w-5"
                    viewBox="0 0 20 20"
                    fill="currentColor"
                  >
                    <path d="M10 12a2 2 0 100-4 2 2 0 000 4z" />
                    <path
                      fill-rule="evenodd"
                      d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.022 7-9.542 7S1.732 14.057.458 10zM14 10a4 4 0 11-8 0 4 4 0 018 0z"
                      clip-rule="evenodd"
                    />
                  </svg>
                  <svg
                    v-else
                    xmlns="http://www.w3.org/2000/svg"
                    class="h-5 w-5"
                    viewBox="0 0 20 20"
                    fill="currentColor"
                  >
                    <path
                      fill-rule="evenodd"
                      d="M3.707 2.293a1 1 0 00-1.414 1.414l14 14a1 1 0 001.414-1.414l-1.473-1.473A10.014 10.014 0 0019.542 10C18.268 5.943 14.478 3 10 3a9.958 9.958 0 00-4.512 1.074L3.707 2.293zM10 12a2 2 0 100-4 2 2 0 000 4z"
                      clip-rule="evenodd"
                    />
                    <path
                      d="M10 17a9.953 9.953 0 01-4.512-1.074l-1.473 1.473a1 1 0 101.414 1.414l14-14a1 1 0 10-1.414-1.414L10 17z"
                    />
                  </svg>
                </button>
              </div>
            </div>

            <div>
              <label for="confirm-pass" class="form-label"
                >Nhập lại mật khẩu mới</label
              >
              <div class="input-with-toggle relative">
                <input
                  :type="showPass.confirm ? 'text' : 'password'"
                  id="confirm-pass"
                  v-model="passwords.confirm"
                  class="form-input pr-10"
                  placeholder="••••••••"
                  autocomplete="new-password"
                />
                <button
                  type="button"
                  @click="showPass.confirm = !showPass.confirm"
                  class="password-toggle"
                  :aria-pressed="showPass.confirm"
                  :aria-label="
                    showPass.confirm ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'
                  "
                >
                  <svg
                    v-if="!showPass.confirm"
                    xmlns="http://www.w3.org/2000/svg"
                    class="h-5 w-5"
                    viewBox="0 0 20 20"
                    fill="currentColor"
                  >
                    <path d="M10 12a2 2 0 100-4 2 2 0 000 4z" />
                    <path
                      fill-rule="evenodd"
                      d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.022 7-9.542 7S1.732 14.057.458 10zM14 10a4 4 0 11-8 0 4 4 0 018 0z"
                      clip-rule="evenodd"
                    />
                  </svg>
                  <svg
                    v-else
                    xmlns="http://www.w3.org/2000/svg"
                    class="h-5 w-5"
                    viewBox="0 0 20 20"
                    fill="currentColor"
                  >
                    <path
                      fill-rule="evenodd"
                      d="M3.707 2.293a1 1 0 00-1.414 1.414l14 14a1 1 0 001.414-1.414l-1.473-1.473A10.014 10.014 0 0019.542 10C18.268 5.943 14.478 3 10 3a9.958 9.958 0 00-4.512 1.074L3.707 2.293zM10 12a2 2 0 100-4 2 2 0 000 4z"
                      clip-rule="evenodd"
                    />
                    <path
                      d="M10 17a9.953 9.953 0 01-4.512-1.074l-1.473 1.473a1 1 0 101.414 1.414l14-14a1 1 0 10-1.414-1.414L10 17z"
                    />
                  </svg>
                </button>
              </div>
            </div>

            <div class="text-right pt-4">
              <button
                @click="handleChangePassword"
                class="form-button-primary"
                :disabled="isChangingPassword"
              >
                <span v-if="isChangingPassword">Đang đổi...</span>
                <span v-else>Đổi mật khẩu</span>
              </button>
            </div>
          </div>
        </section>

        <!-- THEME -->
        <section
          v-if="currentTab === 'theme'"
          class="bg-white dark:bg-gray-800 rounded-lg p-6 sm:p-8"
        >
          <h2 class="text-2xl font-bold text-black dark:text-white mb-6">
            Đổi nền
          </h2>

          <div class="space-y-6">
            <div>
              <label class="form-label text-black dark:text-white"
                >Chọn màu nền</label
              >
              <div class="flex items-center gap-3 mt-2">
                <span class="form-label">Chế độ ban đêm</span>

                <!-- Toggle -->
                <label class="relative inline-block w-12 h-6">
                  <input
                    type="checkbox"
                    v-model="isDarkMode"
                    @change="toggleTheme"
                    class="sr-only peer"
                  />

                  <!-- track -->
                  <span
                    class="absolute inset-0 rounded-full bg-gray-400 transition-colors peer-checked:bg-sky-500"
                  ></span>

                  <!-- dot -->
                  <span
                    class="absolute left-0.5 top-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform duration-300 peer-checked:translate-x-6"
                  ></span>
                </label>

                <span class="text-sm text-gray-500 dark:text-gray-300">{{
                  isDarkMode ? "Bật" : "Tắt"
                }}</span>
              </div>
            </div>
          </div>
        </section>
      </div>
    </div>
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

// profile reactive
const profile = reactive({
  name: username.value || "",
  email: email.value || "",
  avatarUrl:
    avatarUrl.value ||
    "https://storage.googleapis.com/maker-studio-project-media-prod/11072a6a-d42a-41e9-8902-613d0a6a0e69/images/220b33b9-a99f-4318-9126-d66a6a96f131.jpg",
  avatarFile: null as File | null,
  avatarPreview: null as string | null,
  birthdate: "",
  gender: "",
});

const passwords = reactive({ current: "", new: "", confirm: "" });
const showPass = reactive({ current: false, new: false, confirm: false });

// theme inject (provided by app.vue)
const theme = inject("theme") as { value: "dark" | "light" } | undefined;
const setTheme = inject("setTheme") as
  | ((mode: "dark" | "light") => void)
  | undefined;

const isDarkMode = ref<boolean>(true);
const defaultAvatar =
  "https://storage.googleapis.com/maker-studio-project-media-prod/11072a6a-d42a-41e9-8902-613d0a6a0e69/images/220b33b9-a99f-4318-9126-d66a6a96f131.jpg";

onMounted(() => {
  isDarkMode.value = !!(theme?.value === "dark");
});

// Toggle theme by calling setTheme provided by app.vue (app.vue handles DOM/localStorage)
function toggleTheme() {
  const mode = isDarkMode.value ? "dark" : "light";
  setTheme?.(mode);
  showToast?.(
    `Đã chuyển sang chế độ ${mode === "dark" ? "ban đêm 🌙" : "ban ngày ☀️"}`,
    "success"
  );
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

async function handleResponse(response: Response) {
  if (!response.ok) {
    const errorText = await response.text();
    let errorMessage = errorText;
    try {
      const errorJson = JSON.parse(errorText);
      if (errorJson?.errors)
        errorMessage = Object.values(errorJson.errors).flat().join(" ");
      else if (errorJson?.message) errorMessage = errorJson.message;
      else if (errorJson?.title) errorMessage = errorJson.title;
    } catch (e) {}
    throw new Error(errorMessage || `Yêu cầu thất bại: ${response.status}`);
  }
  if (response.status === 204) return {};
  const data = await response.json();
  if (data.isSuccess === false)
    throw new Error(data.message || "Lỗi không xác định từ API");
  return data.result;
}

async function handleProfileUpdate() {
  if (!username.value) {
    showToast("Không tìm thấy tên người dùng để cập nhật.", "error");
    return;
  }
  isSaving.value = true;
  const formData = new FormData();
  formData.append("Username", profile.name);
  if (profile.avatarFile) formData.append("AvatarUrl", profile.avatarFile);

  try {
    const res = await fetch(
      `${BASE_URL}/api/users/by-username/${username.value}`,
      {
        method: "PUT",
        headers: { Authorization: `Bearer ${jwt.value}` },
        body: formData,
      }
    );
    const updatedUser = (await handleResponse(res)) as {
      username?: string;
      avatarUrl?: string;
    };
    showToast("Cập nhật thông tin thành công!", "success");
    if (updatedUser.username) {
      username.value = updatedUser.username;
      profile.name = updatedUser.username;
    }
    if (updatedUser.avatarUrl) {
      const newAvatarUrl = `${updatedUser.avatarUrl}?t=${Date.now()}`;
      (avatarUrl as any).value = newAvatarUrl;
      profile.avatarUrl = newAvatarUrl;
    }
    profile.avatarFile = null;
    profile.avatarPreview = null;
  } catch (err: any) {
    console.error("Lỗi cập nhật profile:", err);
    showToast?.(`Lỗi: ${err.message}`, "error");
  } finally {
    isSaving.value = false;
  }
}

async function handleChangePassword() {
  if (passwords.new !== passwords.confirm) {
    showToast("Mật khẩu mới không khớp!", "error");
    return;
  }
  if (!passwords.current || !passwords.new) {
    showToast("Vui lòng nhập đầy đủ mật khẩu.", "error");
    return;
  }
  if (!username.value) {
    showToast("Không tìm thấy thông tin người dùng.", "error");
    return;
  }

  isChangingPassword.value = true;
  const body = {
    username: username.value,
    oldPassword: passwords.current,
    newPassword: passwords.new,
  };

  try {
    const res = await fetch(`${BASE_URL}/api/Users/change-password`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${jwt.value}`,
      },
      body: JSON.stringify(body),
    });
    await handleResponse(res);
    showToast("Đổi mật khẩu thành công!", "success");
    passwords.current = "";
    passwords.new = "";
    passwords.confirm = "";
  } catch (err: any) {
    console.error("Lỗi đổi mật khẩu:", err);
    showToast?.(`Lỗi: ${err.message}`, "error");
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
  color: #374151; /* dark text by default; container handles dark/text color */
}
.nav-button.active {
  background-color: #0ea5e9;
  color: white;
  font-weight: 600;
}
.nav-button:not(.active):hover {
  background-color: #f3f4f6;
}
.form-label {
  display: block;
  font-size: 0.875rem;
  font-weight: 500;
  color: inherit;
  margin-bottom: 0.25rem;
}
.form-input {
  width: 100%;
  background-color: #f3f4f6;
  border: 1px solid #e5e7eb;
  border-radius: 0.5rem;
  padding: 0.75rem 1rem;
  padding-right: 2.75rem;
  color: inherit;
  outline: none;
  transition: border-color 0.2s, box-shadow 0.2s;
}
.dark .form-input {
  background-color: #374151;
  border-color: #4b5563;
  color: #fff;
}
.form-input:focus {
  border-color: #0ea5e9;
  box-shadow: 0 0 0 2px rgba(14, 165, 233, 0.2);
}
.form-input:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}
.form-button-primary {
  background-color: #0ea5e9;
  color: white;
  font-weight: 700;
  padding: 0.625rem 1.25rem;
  border-radius: 0.5rem;
  transition: background-color 0.2s;
}
.form-button-primary:hover:not(:disabled) {
  background-color: #0284c7;
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
  z-index: 20;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.25rem;
}
.password-toggle:hover {
  color: #111827;
}

.switch {
  position: relative;
  display: inline-block;
  width: 46px;
  height: 26px;
}
.switch input {
  opacity: 0;
  width: 0;
  height: 0;
}
.slider {
  position: absolute;
  cursor: pointer;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: #9ca3af;
  transition: 0.3s;
  border-radius: 9999px;
}
.slider::before {
  position: absolute;
  content: "";
  height: 18px;
  width: 18px;
  left: 4px;
  bottom: 4px;
  background-color: white;
  transition: 0.3s;
  border-radius: 50%;
}
.dot {
  transition: transform 0.3s;
}
.translate-x-6 {
  transform: translateX(24px);
}
/* === ADD STYLES BELOW TO THE END of <style scoped> === */

/* This animation is used by BOTH border effects */
@keyframes glitter {
  0% {
    background-position: 0% 50%;
  }
  50% {
    background-position: 100% 50%;
  }
  100% {
    background-position: 0% 50%;
  }
}

/* 1. Glittering Golden (for Premium) */
.premium-avatar-border {
  background: linear-gradient(
    120deg,
    #f0b90b, /* Dark Gold */
    #f8d442, /* Light Gold */
    #ffeb7e, /* Bright Yellow */
    #f8d442, /* Light Gold */
    #f0b90b /* Dark Gold */
  );
  background-size: 300% 300%;
  animation: glitter 3s linear infinite;
}

/* 2. Shining Diamond (for Admin) */
.admin-avatar-border {
  /* Diamond/silver/light blue gradient */
  background: linear-gradient(
    120deg,
    #065aae, /* Bolder Blue */
    #269edf, /* White Sparkle */
    #ffb0fe, /* Lighter Icy Blue */
    #269edf, /* White Sparkle */
    #065aae  /* Bolder Blue */
  );
  background-size: 300% 300%;
  animation: glitter 3s linear infinite; /* We re-use the same animation */
}
</style>
