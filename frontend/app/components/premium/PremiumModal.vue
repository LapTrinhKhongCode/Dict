<template>
  <div
    v-if="isOpen"
    class="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-70 p-4"
    @click.self="emit('close')"
  >
    <div
      class="bg-white dark:bg-gray-800 rounded-lg shadow-xl p-6 w-full max-w-md mx-auto border border-gray-200 dark:border-transparent"
    >
      <div
        class="flex justify-between items-center border-b border-gray-200 dark:border-gray-700 pb-3 mb-4"
      >
        <h2 class="text-xl font-bold text-gray-900 dark:text-gray-100">
          Thông báo
        </h2>
        <button
          @click="emit('close')"
          class="text-gray-400 hover:text-gray-600 dark:hover:text-white text-3xl leading-none"
          title="Đóng"
        >
          &times;
        </button>
      </div>

      <p class="text-gray-700 dark:text-gray-300 mb-8 text-lg">
        Bạn cần nâng cấp lên bản premium để có thể sử dụng tính năng này
      </p>

      <div class="flex justify-end gap-4">
        <button
          @click="emit('upgrade')"
          class="px-6 py-2 rounded-lg bg-primary-600 hover:bg-primary-700 dark:bg-blue-600 dark:hover:bg-blue-700 text-white font-semibold transition-colors"
        >
          Nâng cấp
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
// Component nhận prop 'isOpen' để quyết định có hiển thị hay không
defineProps<{
  isOpen: boolean;
}>();

// Component phát ra hai sự kiện: 'close' (để đóng modal) và 'upgrade' (để xử lý nâng cấp)
const emit = defineEmits(['close', 'upgrade']);
</script>

<style scoped>
/* (Style này không cần thay đổi) */
/* Thêm một chút hiệu ứng mờ dần cho modal */
.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.2s ease;
}
.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}
/* Bạn có thể bọc modal trong <Transition> để sử dụng các class này */
</style>