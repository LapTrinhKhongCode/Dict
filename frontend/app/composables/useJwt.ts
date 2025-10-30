import { ref, watch, computed } from 'vue'

const JWT_KEY = 'jwt_token'
const USERNAME_KEY = 'user_name'
const EMAIL_KEY = 'user_email'
const ROLE_KEY = 'user_role'
const AVATAR_KEY = 'user_avatar_url'
const USERID_KEY = 'user_id'

const jwt = ref<string | null>(null)
const username = ref<string | null>(null)
const email = ref<string | null>(null)
const role = ref<string | null>(null)
const avatarUrl = ref<string | null>(null)
const userId = ref<number | null>(null)

if (process.client) {
  jwt.value = localStorage.getItem(JWT_KEY)
  username.value = localStorage.getItem(USERNAME_KEY)
  email.value = localStorage.getItem(EMAIL_KEY)
  role.value = localStorage.getItem(ROLE_KEY)
  avatarUrl.value = localStorage.getItem(AVATAR_KEY)
  const id = localStorage.getItem(USERID_KEY)
  userId.value = id ? Number(id) : null
}

watch(jwt, (v) => v ? localStorage.setItem(JWT_KEY, v) : localStorage.removeItem(JWT_KEY))
watch(username, (v) => v ? localStorage.setItem(USERNAME_KEY, v) : localStorage.removeItem(USERNAME_KEY))
watch(email, (v) => v ? localStorage.setItem(EMAIL_KEY, v) : localStorage.removeItem(EMAIL_KEY))
watch(role, (v) => v ? localStorage.setItem(ROLE_KEY, v) : localStorage.removeItem(ROLE_KEY))
watch(avatarUrl, (v) => v ? localStorage.setItem(AVATAR_KEY, v) : localStorage.removeItem(AVATAR_KEY))
watch(userId, (v) => v != null ? localStorage.setItem(USERID_KEY, String(v)) : localStorage.removeItem(USERID_KEY))

export function useJwt() {
  function login(token: string, usernameVal?: string, avatarVal?: string, emailVal?: string, roleVal?: string, userIdVal?: number) {
    jwt.value = token
    if (usernameVal !== undefined) username.value = usernameVal
    if (avatarVal !== undefined) avatarUrl.value = avatarVal
    if (emailVal !== undefined) email.value = emailVal
    if (roleVal !== undefined) role.value = roleVal
    if (userIdVal !== undefined) userId.value = userIdVal
  }

  function logout() {
    jwt.value = null
    username.value = null
    avatarUrl.value = null
    email.value = null
    role.value = null
    userId.value = null
  }

  return {
    jwt,
    username,
    email,
    role,
    avatarUrl,
    userId,
    login,
    logout,
    isAuthenticated: computed(() => !!jwt.value)
  }
}
