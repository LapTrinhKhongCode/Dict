<script setup lang="ts">
import { useToast } from '@/composables/useToast'

const { toasts } = useToast()
</script>
<template>
  <div class="fixed bottom-6 right-6 z-[99999] flex flex-col space-y-2 items-end">
    <transition-group name="toast" tag="div">
      <div
        v-for="toast in toasts" :key="toast.id"
        class="px-4 py-3 rounded shadow-lg min-w-[220px] text-white font-semibold"
        :class="toast.type === 'success' ? 'bg-green-600' : 'bg-red-600'"
      >
        {{ toast.message }}
      </div>
    </transition-group>
  </div>
</template>
<style scoped>
.toast-enter-active, .toast-leave-active {
  transition: all 0.3s;
}
.toast-enter-from, .toast-leave-to {
  opacity: 0;
  transform: translateY(-20px);
}
</style>


