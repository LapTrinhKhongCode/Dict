import { ref } from 'vue'

export interface Toast {
  id: number;
  message: string;
  type: 'success' | 'error';
}

const toasts = ref<Toast[]>([])
let toastId = 0

export function useToast() {
  function showToast(message: string, type: 'success' | 'error' = 'success', timeout = 3000) {
    const id = toastId++
    toasts.value.push({ id, message, type })
    setTimeout(() => {
      toasts.value = toasts.value.filter(t => t.id !== id)
    }, timeout)
  }
  return { toasts, showToast }
}
