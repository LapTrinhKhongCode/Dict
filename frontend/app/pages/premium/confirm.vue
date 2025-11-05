<template>
  <ClientOnly>
    <div
      class="min-h-screen bg-gray-50 text-gray-900 dark:bg-gray-900 dark:text-white p-4 sm:p-8 flex items-center justify-center transition-colors"
    >
      <div class="max-w-md w-full">
        <div class="mb-6">
          <button
            @click="goBackToPremium"
            class="flex items-center text-sm text-primary-600 hover:text-primary-500 dark:text-sky-400 dark:hover:text-sky-300 transition-colors mb-2"
          >
            &larr; Quay lại chọn gói
          </button>
          <h1 class="text-3xl font-bold text-primary-600 dark:text-sky-400">
            Thông tin cá nhân
          </h1>
        </div>

        <div
          class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-transparent rounded-lg p-6 space-y-6 shadow-lg"
        >
          <div
            class="bg-blue-50 border border-blue-300 text-blue-700 dark:bg-blue-900/50 dark:border-blue-700 dark:text-blue-200 text-sm rounded-lg p-4 flex gap-3"
          >
            <svg
              xmlns="http://www.w3.org/2000/svg"
              class="h-5 w-5 flex-shrink-0"
              viewBox="0 0 20 20"
              fill="currentColor"
            >
              <path
                fill-rule="evenodd"
                d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7-4a1 1 0 11-2 0 1 1 0 012 0zM9 9a1 1 0 000 2v3a1 1 0 001 1h1a1 1 0 100-2V9a1 1 0 00-1-1H9z"
                clip-rule="evenodd"
              />
            </svg>
            <span>
              Gói nâng cấp và tất cả quyền lợi đi kèm sẽ được thêm vào tài khoản
              này sau khi quá trình thanh toán thành công!
            </span>
          </div>

          <div>
            <label
              class="block text-sm font-medium text-gray-600 dark:text-gray-400 mb-1"
              >Họ và tên</label
            >
            <div class="form-display">{{ username || "Đang tải..." }}</div>
          </div>
          <div>
            <label
              class="block text-sm font-medium text-gray-600 dark:text-gray-400 mb-1"
              >Tài khoản nâng cấp</label
            >
            <div class="form-display">{{ email || "Đang tải..." }}</div>
          </div>

          <p
            class="text-xs text-gray-500 dark:text-gray-400 text-center"
          >
            Bằng việc nhấn "Tiếp tục thanh toán" bạn xác nhận đã đọc và đồng ý
            với
            <a href="#" class="text-primary-600 dark:text-sky-400 hover:underline"
              >Điều kiện & Điều khoản</a
            >
            cùng
            <a href="#" class="text-primary-600 dark:text-sky-400 hover:underline"
              >Chính sách bảo mật</a
            >
            của chúng tôi.
          </p>

          <button
            @click="confirmPayment"
            class="w-full bg-primary-600 hover:bg-primary-700 dark:bg-blue-600 dark:hover:bg-blue-700 text-white text-lg font-bold py-3 rounded-lg transition-colors"
          >
            Tiếp tục thanh toán
          </button>
        </div>
      </div>
    </div>

    <template #fallback>
      <div
        class="min-h-screen flex items-center justify-center bg-gray-50 dark:bg-gray-900"
      >
        <p class="text-gray-700 dark:text-white text-lg">
          Đang tải thông tin...
        </p>
      </div>
    </template>
  </ClientOnly>
</template>

<script setup lang="ts">
import { useJwt } from '~/composables/useJwt';

const { username, email } = useJwt();

function goBackToPremium() {
  navigateTo('/');
  navigateTo('/premium');
}

function confirmPayment() {
  navigateTo('/premium/pay');
}
</script>

<style scoped>
/* THAY ĐỔI:
  - Tách style mặc định (light mode) ra.
  - Bọc các style cũ (dark mode) trong class .dark
*/

/* --- LIGHT MODE (MẶC ĐỊNH) --- */
.form-display {
  width: 100%;
  background-color: #f3f4f6; /* bg-gray-100 */
  border: 1px solid #d1d5db; /* border-gray-300 */
  border-radius: 0.5rem;
  padding: 0.75rem 1rem;
  color: #374151; /* text-gray-700 */
  font-weight: 500;
}

/* --- DARK MODE --- */
.dark .form-display {
  background-color: #1f2937; /* bg-gray-800 */
  border: 1px solid #4b5563; /* border-gray-600 */
  color: #d1d5db; /* text-gray-300 */
}
</style>