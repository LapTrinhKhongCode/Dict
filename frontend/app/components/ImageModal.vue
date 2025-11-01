<template>
  <div 
    v-if="isOpen" 
    class="fixed inset-0 z-70 flex items-center justify-center" 
    style="background-color: rgba(0, 0, 0, 0.5);" 
    @click.self="$emit('close')"
  >
    <div class="relative">
      <img :src="imageUrl" alt="Phóng to ảnh" class="max-h-[90vh] max-w-[90vw] object-contain" />
      <button
        class="absolute top-4 right-4 text-white text-3xl font-bold bg-gray-800 bg-opacity-50 rounded-full w-10 h-10 flex items-center justify-center hover:bg-opacity-75 transition leading-none pb-1"
        @click="$emit('close')"
        >
        &times;
    </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { defineProps, defineEmits } from 'vue';

defineProps({
  isOpen: {
    type: Boolean,
    default: false,
  },
  imageUrl: {
    type: String,
    required: true,
  },
});

defineEmits(['close']);
</script>

<style scoped>
/* Bạn có thể thêm CSS tùy chỉnh tại đây nếu cần */
</style>