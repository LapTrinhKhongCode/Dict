import { useJwt } from "@/composables/useJwt";
import { useToast } from "@/composables/useToast"; // 1. Import useToast

export default defineNuxtRouteMiddleware((to, from) => {
  // Fast path: on client hard-reload, check localStorage synchronously
  if (process.client) {
    const token = localStorage.getItem('jwt_token');
    if (token) {
      // Token exists, allow navigation without flicker/false redirect
      return;
    }
  }

  // Fallback to reactive auth state
  const { isAuthenticated } = useJwt();
  const { showToast } = useToast(); // 2. Get the showToast function

  // Check if the user is authenticated
  if (!isAuthenticated.value) {
    // 3. Show the toast notification
    showToast("Bạn phải đăng nhập để truy cập trang này.", "error");

    // 4. Redirect them to the login page
    return navigateTo("/login");
  }

  // If they are authenticated, allow them to proceed.
});