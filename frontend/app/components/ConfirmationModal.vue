<template>
  <!-- Lớp phủ nền -->
  <div 
    v-if="isOpen" 
    class="fixed inset-0 z-50 flex items-center justify-center bg-opacity-10"
    @click.self="emit('cancel')"
  >
    <!-- Bảng Modal -->
    <div class="bg-gray-800 rounded-lg shadow-xl p-6 w-full max-w-sm mx-4">
      <!-- Tiêu đề -->
      <h3 class="text-xl font-bold text-sky-400 mb-4">
        {{ title }}
      </h3>
      
      <!-- Thông điệp -->
      <p class="text-gray-300 mb-6">
        {{ message }}
      </p>
      
      <!-- Các nút hành động -->
      <div class="flex justify-end gap-4">
        <button 
          @click="emit('cancel')"
          class="px-5 py-2 rounded-lg bg-gray-600 hover:bg-gray-500 font-semibold transition-colors"
        >
          Hủy
        </button>
        <button 
          @click="emit('confirm')"
          class="px-5 py-2 rounded-lg bg-red-600 hover:bg-red-700 font-semibold transition-colors"
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
