<template>
  <div class="min-h-screen text-white p-4 sm:p-8 bg-gray-900">
    <div class="max-w-6xl mx-auto flex flex-col md:flex-row gap-8">
      <div class="w-full md:w-1/4 shrink-0">
        <div class="bg-gray-800 rounded-lg p-6 sticky top-24">
          <div class="flex flex-col items-center mb-6">
            <img
              :src="avatarUrl || 'https://storage.googleapis.com/maker-studio-project-media-prod/11072a6a-d42a-41e9-8902-613d0a6a0e69/images/220b33b9-a99f-4318-9126-d66a6a96f131.jpg'"
              alt="Avatar"
              class="w-32 h-32 rounded-full border-4 border-sky-500 object-cover mb-4 bg-gray-700"
            />
            <h2 class="text-2xl font-bold text-white text-center">{{ username || 'Người dùng' }}</h2>
            <p class="text-sm text-gray-400">{{ role !== 'USER' ? 'Premium Member' : 'Thành viên' }}</p>
          </div>

          <nav class="space-y-2">
            <button @click="selectTab('general')" :class="['nav-button', { 'active': currentTab === 'general' }]">
              Giới thiệu chung
            </button>
            <button @click="selectTab('security')" :class="['nav-button', { 'active': currentTab === 'security' }]">
              Bảo mật
            </button>
          </nav>
        </div>
      </div>

      <div class="w-full md:w-3/4">
        <div v-if="currentTab === 'general'" class="bg-gray-800 rounded-lg p-6 sm:p-8">
          <h2 class="text-2xl font-bold text-white mb-6">Thông tin cá nhân</h2>

          <div class="space-y-6">
            <div>
              <label for="email" class="form-label">Email</label>
              <input type="email" id="email" :value="email" class="form-input" disabled />
              <p class="text-xs text-gray-500 mt-1">Bạn không thể thay đổi email.</p>
            </div>

            <div>
              <label for="name" class="form-label">Họ tên</label>
              <input type="text" id="name" v-model="profile.name" class="form-input" placeholder="Tên của bạn" />
            </div>

            <div>
              <label class="form-label">Ảnh đại diện</label>
              <div class="flex items-center gap-4">
                <img :src="profile.avatarPreview || profile.avatarUrl" alt="Current Avatar" class="w-16 h-16 rounded-full object-cover bg-gray-700" />
                <input type="file" @change="handleFileSelect" accept="image/*" class="text-sm text-gray-400 file:mr-4 file:py-2 file:px-4 file:rounded-full file:border-0 file:text-sm file:font-semibold file:bg-sky-600 file:text-white hover:file:bg-sky-700 cursor-pointer" />
              </div>
            </div>

            <div class="text-right pt-4">
              <button @click="handleProfileUpdate" class="form-button-primary" :disabled="isSaving">
                <span v-if="isSaving">Đang cập nhật...</span>
                <span v-else>Cập nhật thông tin</span>
              </button>
            </div>
          </div>
        </div>

        <div v-if="currentTab === 'security'" class="bg-gray-800 rounded-lg p-6 sm:p-8">
          <h2 class="text-2xl font-bold text-white mb-6">Bảo mật</h2>

          <div class="space-y-6 max-w-lg">
           <!-- Mật khẩu hiện tại -->
<div>
  <label for="current-pass" class="form-label">Mật khẩu hiện tại</label>
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
      :aria-label="showPass.current ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'"
    >
      <svg v-if="!showPass.current" xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor"><path d="M10 12a2 2 0 100-4 2 2 0 000 4z"/><path fill-rule="evenodd" d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.022 7-9.542 7S1.732 14.057.458 10zM14 10a4 4 0 11-8 0 4 4 0 018 0z" clip-rule="evenodd"/></svg>
      <svg v-else xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M3.707 2.293a1 1 0 00-1.414 1.414l14 14a1 1 0 001.414-1.414l-1.473-1.473A10.014 10.014 0 0019.542 10C18.268 5.943 14.478 3 10 3a9.958 9.958 0 00-4.512 1.074L3.707 2.293zM10 12a2 2 0 100-4 2 2 0 000 4z" clip-rule="evenodd"/><path d="M10 17a9.953 9.953 0 01-4.512-1.074l-1.473 1.473a1 1 0 101.414 1.414l14-14a1 1 0 10-1.414-1.414L10 17z"/></svg>
    </button>
  </div>
</div>

<!-- Mật khẩu mới -->
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
      <svg v-if="!showPass.new" xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor"><path d="M10 12a2 2 0 100-4 2 2 0 000 4z"/><path fill-rule="evenodd" d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.022 7-9.542 7S1.732 14.057.458 10zM14 10a4 4 0 11-8 0 4 4 0 018 0z" clip-rule="evenodd"/></svg>
      <svg v-else xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M3.707 2.293a1 1 0 00-1.414 1.414l14 14a1 1 0 001.414-1.414l-1.473-1.473A10.014 10.014 0 0019.542 10C18.268 5.943 14.478 3 10 3a9.958 9.958 0 00-4.512 1.074L3.707 2.293zM10 12a2 2 0 100-4 2 2 0 000 4z" clip-rule="evenodd"/><path d="M10 17a9.953 9.953 0 01-4.512-1.074l-1.473 1.473a1 1 0 101.414 1.414l14-14a1 1 0 10-1.414-1.414L10 17z"/></svg>
    </button>
  </div>
</div>

<!-- Nhập lại mật khẩu mới -->
<div>
  <label for="confirm-pass" class="form-label">Nhập lại mật khẩu mới</label>
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
      :aria-label="showPass.confirm ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'"
    >
      <svg v-if="!showPass.confirm" xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor"><path d="M10 12a2 2 0 100-4 2 2 0 000 4z"/><path fill-rule="evenodd" d="M.458 10C1.732 5.943 5.522 3 10 3s8.268 2.943 9.542 7c-1.274 4.057-5.022 7-9.542 7S1.732 14.057.458 10zM14 10a4 4 0 11-8 0 4 4 0 018 0z" clip-rule="evenodd"/></svg>
      <svg v-else xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M3.707 2.293a1 1 0 00-1.414 1.414l14 14a1 1 0 001.414-1.414l-1.473-1.473A10.014 10.014 0 0019.542 10C18.268 5.943 14.478 3 10 3a9.958 9.958 0 00-4.512 1.074L3.707 2.293zM10 12a2 2 0 100-4 2 2 0 000 4z" clip-rule="evenodd"/><path d="M10 17a9.953 9.953 0 01-4.512-1.074l-1.473 1.473a1 1 0 101.414 1.414l14-14a1 1 0 10-1.414-1.414L10 17z"/></svg>
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
        </div>

      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useJwt } from '~/composables/useJwt';
import { useToast } from '~/composables/useToast';

const { username, email, avatarUrl, role, jwt } = useJwt();
const config = useRuntimeConfig();
const BASE_URL = config.public.apiBaseUrl || 'https://localhost:7084';
const isChangingPassword = ref(false);
type Tab = 'general' | 'security';
const currentTab = ref<Tab>('general');
const isSaving = ref(false);

const { showToast } = useToast();

const profile = reactive({
  name: username.value || '',
  email: email.value || '',
  avatarUrl: avatarUrl.value || 'https://storage.googleapis.com/maker-studio-project-media-prod/11072a6a-d42a-41e9-8902-613d0a6a0e69/images/220b33b9-a99f-4318-9126-d66a6a96f131.jpg',
  avatarFile: null as File | null,
  avatarPreview: null as string | null,
  birthdate: '',
  gender: '',
});

const passwords = reactive({
  current: '',
  new: '',
  confirm: '',
});

const showPass = reactive({
  current: false,
  new: false,
  confirm: false,
});

function selectTab(tab: Tab) {
  currentTab.value = tab;
}

function handleFileSelect(event: Event) {
  const target = event.target as HTMLInputElement;
  if (target.files && target.files[0]) {
    const file = target.files[0];
    profile.avatarFile = file;
    const reader = new FileReader();
    reader.onload = (e) => {
      profile.avatarPreview = e.target?.result as string;
    };
    reader.readAsDataURL(file);
  }
}

async function handleResponse(response: Response) {
  if (!response.ok) {
    const errorText = await response.text();
    let errorMessage = errorText;
    try {
      const errorJson = JSON.parse(errorText);
      if (errorJson?.errors) { errorMessage = Object.values(errorJson.errors).flat().join(' '); }
      else if (errorJson?.message) { errorMessage = errorJson.message; }
      else if (errorJson?.title) { errorMessage = errorJson.title; }
    } catch (e) {}
    throw new Error(errorMessage || `Yêu cầu thất bại: ${response.status}`);
  }
  if (response.status === 204) return {};
  const data = await response.json();
  if (data.isSuccess === false) { throw new Error(data.message || 'Lỗi không xác định từ API'); }
  return data.result;
}

async function handleProfileUpdate() {
  if (!username.value) {
    showToast("Không tìm thấy tên người dùng để cập nhật.", 'error');
    return;
  }
  isSaving.value = true;

  const formData = new FormData();
  formData.append('Username', profile.name);

  if (profile.avatarFile) {
    formData.append('AvatarUrl', profile.avatarFile);
  }

  try {
    const response = await fetch(`${BASE_URL}/api/users/by-username/${username.value}`, {
      method: 'PUT',
      headers: { 'Authorization': `Bearer ${jwt.value}` },
      body: formData,
    });

    const updatedUser = await handleResponse(response) as { username: string, avatarUrl: string };

    showToast("Cập nhật thông tin thành công!", 'success');

    if (updatedUser.username) {
      username.value = updatedUser.username;
      profile.name = updatedUser.username;
    }
    if (updatedUser.avatarUrl) {
      const newAvatarUrl = `${updatedUser.avatarUrl}?t=${new Date().getTime()}`;
      avatarUrl.value = newAvatarUrl;
      profile.avatarUrl = newAvatarUrl;
    }

    profile.avatarFile = null;
    profile.avatarPreview = null;

  } catch (err: any) {
    console.error("Lỗi cập nhật profile:", err);
    showToast(`Lỗi: ${err.message}`, 'error');
  } finally {
    isSaving.value = false;
  }
}

async function handleChangePassword() {
  if (passwords.new !== passwords.confirm) {
    showToast("Mật khẩu mới không khớp!", 'error');
    return;
  }
  if (!passwords.current || !passwords.new) {
    showToast("Vui lòng nhập đầy đủ mật khẩu.", 'error');
    return;
  }
  if (!username.value) {
    showToast("Không tìm thấy thông tin người dùng.", 'error');
    return;
  }

  isChangingPassword.value = true;

  const body = {
    username: username.value,
    oldPassword: passwords.current,
    newPassword: passwords.new
  };

  try {
    const response = await fetch(`${BASE_URL}/api/Users/change-password`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${jwt.value}`
      },
      body: JSON.stringify(body)
    });

    await handleResponse(response);

    showToast("Đổi mật khẩu thành công!", 'success');

    passwords.current = '';
    passwords.new = '';
    passwords.confirm = '';

  } catch (err: any) {
    console.error("Lỗi đổi mật khẩu:", err);
    showToast(`Lỗi: ${err.message}`, 'error');
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
  color: #d1d5db;
}
.nav-button.active {
  background-color: #0ea5e9;
  color: white;
  font-weight: 600;
}
.nav-button:not(.active):hover {
  background-color: #374151;
}
.form-label {
  display: block;
  font-size: 0.875rem;
  font-weight: 500;
  color: #d1d5db;
  margin-bottom: 0.25rem;
}
.form-input {
  width: 100%;
  background-color: #374151;
  border: 1px solid #4b5563;
  border-radius: 0.5rem;
  padding: 0.75rem 1rem;
  padding-right: 2.75rem;
  color: white;
  outline: none;
  transition: border-color 0.2s, box-shadow 0.2s;
}
.form-input:focus {
  border-color: #0ea5e9;
  box-shadow: 0 0 0 2px rgba(14, 165, 233, 0.4);
}
.form-input:disabled {
  background-color: #4b5563;
  opacity: 0.7;
  cursor: not-allowed;
}
input[type="date"] {
  color-scheme: dark;
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
.form-button-primary:disabled {
  background-color: #4b5563;
  opacity: 0.5;
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
  z-index: 20;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 0.25rem;
}
.password-toggle:hover {
  color: white;
}

/* Hide native password reveal / clear buttons in different engines */
input[type="password"]::-ms-reveal,
input[type="password"]::-ms-clear {
  display: none;
}
input[type="password"]::-webkit-textfield-decoration-button,
input[type="password"]::-webkit-contacts-auto-fill-button,
input[type="password"]::-webkit-clear-button,
input[type="password"]::-webkit-password-reveal-button {
  display: none !important;
  -webkit-appearance: none;
  appearance: none;
  pointer-events: none;
}
</style>
