import { useJwt } from "@/composables/useJwt";
import { useToast } from "@/composables/useToast"; // 1. Import useToast

// middleware/auth-client.ts
export default defineNuxtRouteMiddleware((to, from) => {
  // Bỏ qua hoàn toàn khi chạy server-side
  if (process.server) return

  // Client-side: đọc localStorage trực tiếp
  const token = localStorage.getItem('jwt_token')
  if (!token) {
    return navigateTo('/login')
  }
})