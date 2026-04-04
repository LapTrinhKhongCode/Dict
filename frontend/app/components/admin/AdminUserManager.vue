<template>
  <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6">
    <h2 class="text-2xl font-bold text-gray-900 dark:text-white mb-6">Quản lý Người dùng</h2>
    <div class="mb-6">
      <div class="flex gap-4">
        <input type="text" v-model="searchTerm" @keyup.enter="handleSearch" placeholder="Tìm theo username, email..." class="flex-1 form-input" />
        <button @click="handleSearch" :disabled="isSearching || !searchTerm.trim()" class="form-button-primary px-6">
          <span v-if="isSearching">Đang tìm...</span>
          <span v-else>Tìm kiếm</span>
        </button>
      </div>
    </div>
    
    <div v-if="isSearching" class="text-gray-500">Đang tìm kiếm...</div>
    <div v-else-if="searchError" class="text-red-500">{{ searchError }}</div>
    <div v-else-if="searchResults.length > 0" class="space-y-4">
      <div class="text-gray-500 text-sm mb-4">Tìm thấy {{ searchTotalCount }} người dùng (trang {{ searchPage }}/{{ totalPages }})</div>
      <div class="space-y-2">
        <div v-for="user in searchResults" :key="user.id" class="bg-gray-100 dark:bg-gray-700 rounded-lg p-4 transition-all hover:bg-gray-200 dark:hover:bg-gray-600 flex items-center gap-4">
          <img :src="user.avatarUrl || '...'" :alt="user.username" class="w-12 h-12 rounded-full object-cover bg-gray-300 dark:bg-gray-600" />
          <div class="flex-1 min-w-0">
            <div class="flex items-center gap-2">
              <h3 class="text-lg font-semibold text-gray-900 dark:text-white truncate">{{ user.username }}</h3>
              <span v-if="user.role" class="px-2 py-1 text-xs font-semibold rounded bg-sky-600 text-white flex-shrink-0">{{ user.role }}</span>
              <span v-if="!user.isActive" class="px-2 py-1 text-xs font-semibold rounded bg-red-600 text-white flex-shrink-0">ĐÃ KHÓA</span>
            </div>
            <p class="text-gray-600 dark:text-gray-400 text-sm truncate">{{ user.email }}</p>
          </div>
          <button @click.stop="openUserModal(user)" class="form-button-primary px-3 py-1.5 text-sm">Quản lý</button>
        </div>
      </div>
      <div v-if="totalPages > 1" class="flex justify-center gap-4 mt-6">
        <button @click="changePage(searchPage - 1)" :disabled="searchPage <= 1" class="form-button-primary px-4 py-2">Trước</button>
        <span class="text-gray-500">Trang {{ searchPage }} / {{ totalPages }}</span>
        <button @click="changePage(searchPage + 1)" :disabled="searchPage >= totalPages" class="form-button-primary px-4 py-2">Sau</button>
      </div>
    </div>
    <div v-else-if="hasSearched" class="text-gray-500">Không tìm thấy người dùng nào.</div>

    <UserDetailModal
      :is-open="isUserModalOpen"
      :user="selectedUser"
      :on-lock="handleLockUser"
      :on-update-role="handleUpdateRole"
      :on-reset-password="handleResetPassword"
      :on-delete="handleDeleteUser"
      @close="closeUserModal"
      @refresh-users="fetchUsers"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { useJwt } from "~/composables/useJwt";
import { useToast } from "~/composables/useToast";
import UserDetailModal from "~/components/UserDetailModal.vue";

const { jwt } = useJwt();
const config = useRuntimeConfig();
const BASE_URL = config.public.apiBaseUrl || "https://localhost:7084";
const { showToast } = useToast();

const searchTerm = ref("");
const searchResults = ref<any[]>([]);
const searchTotalCount = ref(0);
const searchPage = ref(1);
const pageSize = ref(10);
const isSearching = ref(false);
const searchError = ref<string | null>(null);
const hasSearched = ref(false);
const totalPages = computed(() => Math.ceil(searchTotalCount.value / pageSize.value));
const isUserModalOpen = ref(false);
const selectedUser = ref<any | null>(null);

onMounted(() => fetchUsers());

async function handleResponse(response: Response) {
  if (!response.ok) throw new Error("Thất bại");
  if (response.status === 204) return {};
  const data = await response.json();
  if (data.isSuccess === false) throw new Error(data.message);
  return data.result;
}

async function handleSearch() { isSearching.value = true; searchPage.value = 1; await fetchUsers(); isSearching.value = false; hasSearched.value = true; }
async function changePage(newPage: number) { searchPage.value = newPage; await fetchUsers(); }

async function fetchUsers() {
  try {
    const res = await fetch(`${BASE_URL}/api/Admin/users/search?searchTerm=${encodeURIComponent(searchTerm.value)}&page=${searchPage.value}&pageSize=${pageSize.value}`, { headers: { Authorization: `Bearer ${jwt.value}` } });
    const result: any = await handleResponse(res);
    searchResults.value = result.items; searchTotalCount.value = result.totalCount;
  } catch (err: any) { searchError.value = err.message; searchResults.value = []; }
}

function openUserModal(user: any) { selectedUser.value = user; isUserModalOpen.value = true; }
function closeUserModal() { isUserModalOpen.value = false; selectedUser.value = null; }

// Gọi API quản lý
async function handleApi(url: string, method: string, body: any, successMsg: string) {
  try {
    const res = await fetch(url, { method, headers: { "Content-Type": "application/json", Authorization: `Bearer ${jwt.value}` }, body: JSON.stringify(body) });
    await handleResponse(res); showToast(successMsg, "success"); return true;
  } catch (err: any) { showToast(`Lỗi: ${err.message}`, "error"); return false; }
}

async function handleLockUser(userId: number, isLocked: boolean) { return handleApi(`${BASE_URL}/api/Admin/users/${userId}/lock-status`, "PATCH", { isLocked }, "Cập nhật thành công."); }
async function handleUpdateRole(userId: number, roleNames: string[]) { return handleApi(`${BASE_URL}/api/Admin/users/${userId}/roles`, "PATCH", { roleNames }, "Đã cập nhật role."); }
async function handleResetPassword(userId: number, newPassword: string) { return handleApi(`${BASE_URL}/api/Admin/users/${userId}/reset-password`, "PATCH", { newPassword }, "Đã reset pass."); }
async function handleDeleteUser(userId: number) { return handleApi(`${BASE_URL}/api/Admin/users/${userId}`, "DELETE", {}, "Đã xóa user vĩnh viễn."); }
</script>