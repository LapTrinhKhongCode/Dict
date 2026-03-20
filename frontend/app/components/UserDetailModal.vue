<template>
  <div
    v-if="isOpen"
    class="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-70 p-4"
    @click.self="close"
  >
    <div
      class="bg-white dark:bg-gray-800 rounded-lg shadow-xl p-6 w-full max-w-4xl max-h-[90vh] overflow-y-auto mx-4 border border-gray-200 dark:border-transparent"
    >
      <div
        v-if="isLoadingAction"
        class="absolute inset-0 z-10 flex items-center justify-center bg-white dark:bg-gray-800 bg-opacity-75 dark:bg-opacity-75 rounded-lg"
      >
        <svg
          class="animate-spin h-8 w-8 text-primary-500 dark:text-sky-400"
          xmlns="http://www.w3.org/2000/svg"
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
          ></circle>
          <path
            class="opacity-75"
            fill="currentColor"
            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
          ></path>
        </svg>
      </div>

      <div class="flex items-center justify-between mb-6">
        <h2 class="text-2xl font-bold text-gray-900 dark:text-white">
          Quản lý người dùng
        </h2>
        <button
          @click="close"
          class="text-gray-400 hover:text-gray-600 dark:hover:text-white"
          aria-label="Đóng"
        >
          <svg
            class="h-6 w-6"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M6 18L18 6M6 6l12 12"
            />
          </svg>
        </button>
      </div>

      <div v-if="user" class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div class="space-y-6">
          <div
            class="bg-gray-50 dark:bg-gray-700 rounded-lg p-5 border border-gray-200 dark:border-gray-600"
          >
            <div class="flex items-center gap-4 mb-4">
              <img
                :src="user.avatarUrl || defaultAvatarUrl"
                :alt="user.username"
                class="w-20 h-20 rounded-full object-cover bg-gray-300 dark:bg-gray-600 border-4 border-gray-300 dark:border-gray-500"
              />
              <div class="flex-1">
                <h3 class="text-2xl font-bold text-gray-900 dark:text-white">
                  {{ user.username }}
                </h3>
                <p class="text-gray-600 dark:text-gray-400 text-lg">
                  {{ user.email }}
                </p>
                <p class="text-gray-500 dark:text-gray-500 text-sm">
                  ID: {{ user.id }}
                </p>
              </div>
            </div>
            <div class="space-y-2 text-sm">
              <div class="flex justify-between">
                <span class="text-gray-600 dark:text-gray-400">Trạng thái:</span
                ><span
                  :class="[
                    'px-2 py-0.5 font-semibold rounded',
                    user.isActive
                      ? 'bg-green-600 text-white'
                      : 'bg-red-600 text-white',
                  ]"
                  >{{ user.isActive ? "Hoạt động" : "Đã khóa" }}</span
                >
              </div>
              <div class="flex justify-between">
                <span class="text-gray-600 dark:text-gray-400">Vai trò:</span
                ><span
                  class="font-semibold text-primary-600 dark:text-sky-400"
                  >{{ user.role }}</span
                >
              </div>
              <div class="flex justify-between">
                <span class="text-gray-600 dark:text-gray-400"
                  >Ngày tham gia:</span
                ><span class="text-gray-800 dark:text-gray-300">{{
                  new Date(user.createdAt).toLocaleString("vi-VN")
                }}</span>
              </div>
              <div class="flex justify-between">
                <span class="text-gray-600 dark:text-gray-400"
                  >Cập nhật cuối:</span
                ><span class="text-gray-800 dark:text-gray-300">{{
                  new Date(user.updatedAt).toLocaleString("vi-VN")
                }}</span>
              </div>
            </div>
          </div>

          <div v-if="user.role !== 'ADMIN'" class="space-y-6">
            <div
              class="bg-gray-50 dark:bg-gray-700 rounded-lg p-5 space-y-5 border border-gray-200 dark:border-gray-600"
            >
              <h3
                class="text-xl font-bold text-gray-900 dark:text-white mb-2"
              >
                Hành động nhanh
              </h3>

              <div>
                <button
                  @click="toggleLock"
                  :disabled="isLoadingAction"
                  :class="[
                    'w-full text-white font-semibold py-3 px-6 rounded transition-colors cursor-pointer border-none disabled:bg-gray-400 dark:disabled:bg-gray-600 disabled:opacity-50 disabled:cursor-not-allowed',
                    user.isActive
                      ? 'bg-yellow-500 hover:bg-yellow-600 dark:bg-yellow-600 dark:hover:bg-yellow-700'
                      : 'bg-green-500 hover:bg-green-600 dark:bg-green-600 dark:hover:bg-green-700',
                  ]"
                >
                  {{
                    user.isActive
                      ? "Khóa người dùng này"
                      : "Mở khóa người dùng"
                  }}
                </button>
              </div>

              <div>
                <label
                  for="role-select"
                  class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2"
                  >Thay đổi vai trò</label
                >
                <div class="flex gap-2">
                  <select
                    id="role-select"
                    v-model="selectedRole"
                    class="form-input flex-1"
                  >
                    <option value="USER">USER</option>
                    <option value="PREMIUM_USER">PREMIUM_USER</option>
                    <option value="MODERATOR">MODERATOR</option>
                    <option value="ADMIN">ADMIN</option>
                  </select>
                  <button
                    @click="updateRole"
                    :disabled="isLoadingAction || selectedRole === user.role"
                    class="text-white font-semibold py-3 px-6 rounded transition-colors cursor-pointer border-none bg-primary-600 hover:bg-primary-700 dark:bg-sky-600 dark:hover:bg-sky-700 disabled:bg-gray-400 dark:disabled:bg-gray-600 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Lưu
                  </button>
                </div>
              </div>

              <div>
                <label
                  for="new-password"
                  class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2"
                  >Đặt lại mật khẩu</label
                >
                <div class="flex gap-2">
                  <input
                    id="new-password"
                    type="text"
                    v-model="newPassword"
                    placeholder="Nhập mật khẩu mới..."
                    class="form-input flex-1"
                  />
                  <button
                    @click="resetPassword"
                    :disabled="isLoadingAction || !newPassword.trim()"
                    class="text-white font-semibold py-3 px-6 rounded transition-colors cursor-pointer border-none bg-primary-600 hover:bg-primary-700 dark:bg-sky-600 dark:hover:bg-sky-700 disabled:bg-gray-400 dark:disabled:bg-gray-600 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    Lưu
                  </button>
                </div>
              </div>
            </div>

            <div
              class="bg-gray-50 dark:bg-gray-700 rounded-lg p-5 border-2 border-red-500/50"
            >
              <h3 class="text-xl font-bold text-red-500 dark:text-red-400 mb-3">
                Khu vực nguy hiểm
              </h3>
              <p class="text-gray-600 dark:text-gray-400 text-sm mb-4">
                Hành động này sẽ xóa vĩnh viễn người dùng và không thể hoàn tác.
              </p>
              <button
                @click="confirmDelete"
                :disabled="isLoadingAction"
                class="w-full text-white font-semibold py-3 px-6 rounded transition-colors cursor-pointer border-none bg-red-600 hover:bg-red-700 disabled:bg-gray-400 dark:disabled:bg-gray-600 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Xóa vĩnh viễn người dùng
              </button>
            </div>
          </div>

          <div
            v-else
            class="bg-gray-50 dark:bg-gray-700 rounded-lg p-5 border border-gray-200 dark:border-gray-600"
          >
            <h3
              class="text-xl font-bold text-primary-600 dark:text-sky-400 mb-2"
            >
              Tài khoản Quản trị viên
            </h3>
            <p class="text-gray-700 dark:text-gray-300">
              Không thể thực hiện hành động (Khóa, Đổi Role, Xóa) trên tài khoản
              ADMIN từ giao diện này để đảm bảo an toàn.
            </p>
          </div>
        </div>

        <div
          class="bg-gray-50 dark:bg-gray-700 rounded-lg p-5 border border-gray-200 dark:border-gray-600"
        >
          <h3 class="text-xl font-bold text-gray-900 dark:text-white mb-4">
            Bộ thẻ sở hữu ({{ user?.decks?.length || 0 }})
          </h3>
          <div
            v-if="!user || !user.decks || user.decks.length === 0"
            class="text-gray-500 dark:text-gray-400"
          >
            Người dùng này chưa có bộ thẻ nào.
          </div>
          <div
            v-else
            class="space-y-3 max-h-[60vh] overflow-y-auto pr-2 deck-list"
          >
            <div
              v-for="deck in user.decks"
              :key="deck.id"
              class="bg-white dark:bg-gray-600 rounded-lg p-4 border border-gray-200 dark:border-gray-500"
            >
              <div class="flex items-start justify-between gap-4">
                <div class="flex-1">
                  <div class="flex items-center gap-3 mb-2">
                    <h4 class="text-lg font-semibold text-gray-900 dark:text-white">
                      {{ deck.name }}
                    </h4>
                    <svg
                      v-if="deck.isPublic"
                      class="h-5 w-5 text-green-500 dark:text-green-400"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                      title="Công khai"
                    >
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="2"
                        d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
                      />
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="2"
                        d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"
                      />
                    </svg>
                    <svg
                      v-else
                      class="h-5 w-5 text-gray-400 dark:text-gray-400"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                      title="Riêng tư"
                    >
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="2"
                        d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21"
                      />
                    </svg>
                  </div>
                  <div
                    class="flex items-center gap-4 text-xs text-gray-500 dark:text-gray-400"
                  >
                    <span>ID: {{ deck.id }}</span>
                    <span>Số thẻ: {{ deck.cardCount }}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div class="mt-6 flex justify-end">
        <button
          @click="close"
          :disabled="isLoadingAction"
          class="px-6 py-2 rounded-lg bg-gray-200 text-gray-700 hover:bg-gray-300 dark:bg-gray-700 dark:text-white dark:hover:bg-gray-600 transition-colors font-semibold disabled:opacity-50"
        >
          Đóng
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
// (TOÀN BỘ SCRIPT SETUP GIỮ NGUYÊN)
import { ref, watch, type PropType } from "vue";

// --- TYPES ---
interface Deck {
  id: number;
  name: string;
  description: string;
  isPublic: boolean;
  cardCount: number;
  authorName: string;
  authorImageUrl: string | null;
  nowAuthorName: string | null;
  nowAuthorImageUrl: string | null;
}
interface User {
  id: number;
  username: string;
  email: string;
  isActive: boolean;
  role: string | null;
  avatarUrl: string;
  createdAt: string;
  updatedAt: string;
  decks: Deck[];
}

// --- PROPS (NHẬN TỪ ADMIN.VUE) ---
const props = defineProps({
  isOpen: Boolean,
  user: Object as () => User | null,
  onLock: Function as PropType<
    (userId: number, isLocked: boolean) => Promise<boolean>
  >,
  onUpdateRole: Function as PropType<
    (userId: number, roles: string[]) => Promise<boolean>
  >,
  onResetPassword: Function as PropType<
    (userId: number, newPass: string) => Promise<boolean>
  >,
  onDelete: Function as PropType<(userId: number) => Promise<boolean>>,
});

// --- EMITS (GỬI LÊN ADMIN.VUE) ---
const emit = defineEmits<{
  close: [];
  "refresh-users": [];
}>();

// --- STATE NỘI BỘ ---
const defaultAvatarUrl =
  "https://storage.googleapis.com/maker-studio-project-media-prod/11072a6a-d42a-41e9-8902-613d0a6a0e69/images/220b33b9-a99f-4318-9126-d66a6a96f131.jpg";
const isLoadingAction = ref(false);
const selectedRole = ref("USER");
const newPassword = ref("");

// --- WATCHER ---
watch(
  () => props.user,
  (newUser) => {
    if (newUser) {
      selectedRole.value = newUser.role || "USER";
      newPassword.value = "";
    }
  }
);

// --- HÀM HÀNH ĐỘNG ---

// 1. Khóa / Mở khóa
async function toggleLock() {
  if (!props.user || !props.onLock) return;
  isLoadingAction.value = true;
  const success = await props.onLock(props.user.id, props.user.isActive);
  isLoadingAction.value = false;
  if (success) {
    emit("refresh-users");
    emit("close");
  }
}

// 2. Cập nhật Role
async function updateRole() {
  if (
    !props.user ||
    !props.onUpdateRole ||
    selectedRole.value === props.user.role
  )
    return;
  isLoadingAction.value = true;
  const success = await props.onUpdateRole(props.user.id, [selectedRole.value]);
  isLoadingAction.value = false;
  if (success) {
    emit("refresh-users");
    emit("close");
  }
}

// 3. Đặt lại Mật khẩu
async function resetPassword() {
  if (!props.user || !props.onResetPassword || !newPassword.value.trim())
    return;
  isLoadingAction.value = true;
  const success = await props.onResetPassword(
    props.user.id,
    newPassword.value.trim()
  );
  isLoadingAction.value = false;
  if (success) {
    newPassword.value = "";
  }
}

// 4. Xóa người dùng
async function confirmDelete() {
  if (!props.user || !props.onDelete) return;
  if (
    confirm(
      `Bạn có chắc chắn muốn XÓA VĨNH VIỄN người dùng ${props.user.username}? Hành động này không thể hoàn tác.`
    )
  ) {
    isLoadingAction.value = true;
    const success = await props.onDelete(props.user.id);
    isLoadingAction.value = false;
    if (success) {
      emit("refresh-users");
      emit("close");
    }
  }
}

// Hàm đóng modal
function close() {
  if (isLoadingAction.value) return;
  emit("close");
}
</script>

<style scoped>
/* THAY ĐỔI:
  - Tái cấu trúc lại style cho form-input
  - Thêm style cho thanh cuộn
*/

/* --- LIGHT MODE (MẶC ĐỊNH) --- */
.form-input {
  width: 100%;
  background-color: #ffffff;
  border: 1px solid #d1d5db; /* border-gray-300 */
  border-radius: 0.5rem;
  padding: 0.75rem 1rem;
  color: #111827; /* text-gray-900 */
  outline: none;
  transition: border-color 0.2s, box-shadow 0.2s;
}
.form-input:focus {
  border-color: #2563eb; /* primary-600 */
  box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.4);
}
.form-input::placeholder {
  color: #9ca3af; /* text-gray-400 */
}

/* --- DARK MODE --- */
.dark .form-input {
  background-color: #374151; /* bg-gray-700 */
  border: 1px solid #4b5563; /* border-gray-600 */
  color: white;
}
.dark .form-input:focus {
  border-color: #0ea5e9; /* sky-500 */
  box-shadow: 0 0 0 2px rgba(14, 165, 233, 0.4);
}
.dark .form-input::placeholder {
  color: #9ca3af; /* text-gray-400 */
}

/* --- SCROLLBAR --- */
/* Light Mode */
.deck-list::-webkit-scrollbar {
  width: 8px;
}
.deck-list::-webkit-scrollbar-track {
  background: #f3f4f6; /* bg-gray-100 */
  border-radius: 10px;
}
.deck-list::-webkit-scrollbar-thumb {
  background-color: #d1d5db; /* gray-300 */
  border-radius: 10px;
  border: 2px solid #f3f4f6;
  background-clip: padding-box;
}
.deck-list::-webkit-scrollbar-thumb:hover {
  background-color: #9ca3af; /* gray-400 */
}
/* Dark Mode */
.dark .deck-list::-webkit-scrollbar-track {
  background: #4b5563; /* bg-gray-600 */
}
.dark .deck-list::-webkit-scrollbar-thumb {
  background-color: #6b7280; /* gray-500 */
  border: 2px solid #4b5563;
}
.dark .deck-list::-webkit-scrollbar-thumb:hover {
  background-color: #9ca3af; /* gray-400 */
}
</style>