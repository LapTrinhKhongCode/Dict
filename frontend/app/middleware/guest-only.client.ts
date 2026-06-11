// Redirect to home if user is already logged in (e.g., /login, /register)
export default defineNuxtRouteMiddleware(() => {
  if (process.server) return

  const token = localStorage.getItem('jwt_token')
  if (token) {
    return navigateTo('/')
  }
})
