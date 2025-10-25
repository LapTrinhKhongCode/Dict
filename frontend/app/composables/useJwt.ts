import { ref, watch, computed } from 'vue'

const JWT_KEY = 'jwt_token'
const USERNAME_KEY = 'user_name'
const AVATAR_KEY = 'user_avatar_url'

const jwt = ref<string | null>(null)
const username = ref<string | null>(null)
const avatarUrl = ref<string | null>(null)

if (process.client) {
  jwt.value = localStorage.getItem(JWT_KEY)
  username.value = localStorage.getItem(USERNAME_KEY)
  avatarUrl.value = localStorage.getItem(AVATAR_KEY)
}

watch(jwt, (newValue) => {
  if (!process.client) return
  if (newValue) {
    localStorage.setItem(JWT_KEY, newValue)
  } else {
    localStorage.removeItem(JWT_KEY)
  }
})

watch(username, (newValue) => {
  if (!process.client) return
  if (newValue) {
    localStorage.setItem(USERNAME_KEY, newValue)
  } else {
    localStorage.removeItem(USERNAME_KEY)
  }
})

watch(avatarUrl, (newValue) => {
  if (!process.client) return
  if (newValue) {
    localStorage.setItem(AVATAR_KEY, newValue)
  } else {
    localStorage.removeItem(AVATAR_KEY)
  }
})

export function useJwt() {
  // Login: set JWT and user info (overloaded)
  function login(token: string, name?: string, avatar?: string) {
    jwt.value = token
    if (name !== undefined) username.value = name
    if (avatar !== undefined) avatarUrl.value = avatar
  }

  function logout() {
    jwt.value = null
    username.value = null
    avatarUrl.value = null
  }

  return {
    jwt,
    username,
    avatarUrl,
    login,
    logout,
    isAuthenticated: computed(() => !!jwt.value)
  }
}
