import { useToast } from "@/composables/useToast";

export default defineNuxtRouteMiddleware((to, from) => {
  const { showToast } = useToast();

  if (process.client) {
    const token = localStorage.getItem('jwt_token');
    const storedRole = localStorage.getItem('user_role');

    const allowed = ['ADMIN', 'MODERATOR'];
    if (!token || !storedRole || !allowed.includes(storedRole)) {
      showToast("Bạn không có quyền truy cập trang này.", "error");
      if (from && from.path && from.path !== to.path) return abortNavigation();
      return navigateTo('/');
    }
    return;
  }

  const { jwt, role } = useJwt();
  const allowed = ['ADMIN', 'MODERATOR'];
  if (!jwt.value || !role.value || !allowed.includes(role.value)) {
    return navigateTo('/');
  }
});
