import { useJwt } from "@/composables/useJwt";
import { useToast } from "@/composables/useToast";

export default defineNuxtRouteMiddleware((to, from) => {
  const { showToast } = useToast();

  // Ensure immediate check on hard reload
  if (process.client) {
    const token = localStorage.getItem('jwt_token');
    const storedRole = localStorage.getItem('user_role');

    if (!token || storedRole !== 'ADMIN') {
      showToast("Bạn không có quyền truy cập trang này.", "error");
      // If there's a previous page, stay on it; on hard refresh, redirect to home to avoid 404
      if (from && from.path && from.path !== to.path) {
        return abortNavigation();
      }
      return navigateTo('/');
    }
    // If token exists and role is ADMIN, allow navigation
    return;
  }

  // SSR or fallback safety using reactive state
  const { jwt, role } = useJwt();
  if (!jwt.value || role.value !== 'ADMIN') {
    showToast("Bạn không có quyền truy cập trang này.", "error");
    if (from && from.path && from.path !== to.path) {
      return abortNavigation();
    }
    return navigateTo('/');
  }
});


