<template>
  <div
    v-if="isOpen"
    class="fixed inset-0 z-50 flex items-center justify-center bg-opacity-70 p-4"
    @click.self="emit('cancel')"
  >
    <div
      class="bg-white  dark:bg-gray-800 rounded-lg shadow-xl p-6 w-full max-w-sm mx-4 border border-gray-200 dark:border-transparent"
    >
      <h3 class="text-xl font-bold text-red-600 dark:text-red-400 mb-4">
        {{ title }}
      </h3>

      <p class="text-gray-700 dark:text-gray-300 mb-6">
        {{ message }}
      </p>

      <div class="flex justify-end gap-4">
        <button
          @click="emit('cancel')"
          class="px-5 py-2 rounded-lg bg-gray-200 text-gray-700 hover:bg-gray-300 dark:bg-gray-600 dark:text-white dark:hover:bg-gray-500 font-semibold transition-colors"
        >
          Hủy
        </button>
        <button
          @click="emit('confirm')"
          class="px-5 py-2 rounded-lg bg-red-600 hover:bg-red-700 text-white font-semibold transition-colors"
        >
          Xác nhận
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  isOpen: boolean;
  title?: string;
  message: string;
}>();

const emit = defineEmits(['confirm', 'cancel']);
</script>

<style scoped>
/* (Style này không cần thay đổi) */
.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.2s ease;
}
.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}
</style>