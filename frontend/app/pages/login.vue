<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useJwt } from '@/composables/useJwt'
import { apiUrl } from '@/utils/api'
import { useToast } from '@/composables/useToast'

const mode = ref<'login' | 'register'>('login')
const username = ref('')
const password = ref('')
const email = ref('')
const error = ref('')
const success = ref('')
const loading = ref(false)
const touched = ref(false)
const router = useRouter()
const { login } = useJwt()
const { showToast } = useToast()

// Field error state
const fieldErrors = ref<{ [k: string]: string }>({})

const isEmailValid = () => /.+@.+\..+/.test(email.value)
const isFormValid = () => {
  if (mode.value === 'login') {
    return username.value.trim() && password.value.trim()
  } else {
    return username.value.trim() && password.value.trim() && email.value.trim() && isEmailValid()
  }
}

async function handleAuth() {
  touched.value = true
  error.value = ''
  success.value = ''
  fieldErrors.value = {}
  if (!isFormValid()) return
  loading.value = true
  try {
    let response
    let fetchOptions = {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: mode.value === 'login'
        ? JSON.stringify({ username: username.value, password: password.value })
        : JSON.stringify({ username: username.value, email: email.value, password: password.value })
    }
    const url = mode.value === 'login' ? apiUrl('/Auth/login') : apiUrl('/Auth/register')
    response = await fetch(url, fetchOptions)
    const data = await response.json()

    // handle .errors for register validation
    if (data.errors) {
      Object.entries(data.errors).forEach(([key, msgs]) => {
        if (Array.isArray(msgs)) fieldErrors.value[key.toLowerCase()] = msgs[0]
      })
      error.value = data.title || data.message || 'Validation error.'
      return
    }
    // Handle registration success (result is null, isSuccess true)
    if (mode.value === 'register' && data.isSuccess && !data.result) {
      success.value = 'Registration successful! Please log in.'
      showToast('Registration successful! Please log in.', 'success')
      // Autofill username and password
      mode.value = 'login'
      // wait a tick for template to update
      setTimeout(() => {
        touched.value = false
        password.value = password.value // keep as is
        username.value = username.value // keep as is
      }, 0)
      return
    }
    if (!data.isSuccess || !data.result) {
      error.value = data.message || 'An error occurred'
      return
    }
    if (data.result.token) {
      login(data.result.token, data.result.username, data.result.avatarUrl)
      showToast('Login successful!', 'success')
      await router.push('/')
    } else {
      error.value = 'No token returned from server.'
    }
  } catch (e) {
    error.value = 'Network error or invalid server response.'
  } finally {
    loading.value = false
  }
}
</script>
<template>
  <div class="flex items-center justify-center min-h-screen bg-neutral-900">
    <div class="bg-neutral-800 rounded-lg shadow-lg p-8 w-full max-w-md">
      <div class="flex items-center justify-between mb-6">
        <h2 class="text-2xl font-bold text-white">
          {{ mode === 'login' ? 'Login' : 'Register' }}
        </h2>
        <button @click="() => {mode = (mode === 'login' ? 'register' : 'login'); touched = false; fieldErrors = {}; success = ''}" class="text-primary-400 hover:underline text-sm">
          {{ mode === 'login' ? 'Need an account? Register' : 'Have an account? Login' }}
        </button>
      </div>
      <div v-if="success" class="mb-4 text-green-400">
        {{ success }}
      </div>
      <div class="mb-4">
        <label class="block text-gray-200 mb-1">Username</label>
        <input v-model="username" class="w-full px-3 py-2 rounded bg-neutral-700 text-white focus:outline-none" />
        <div v-if="touched && !username" class="text-xs text-red-400 mt-1">Username is required</div>
        <div v-if="fieldErrors.username" class="text-xs text-red-400 mt-1">{{ fieldErrors.username }}</div>
      </div>
      <div v-if="mode === 'register'" class="mb-4">
        <label class="block text-gray-200 mb-1">Email</label>
        <input v-model="email" type="email" class="w-full px-3 py-2 rounded bg-neutral-700 text-white focus:outline-none" />
        <div v-if="touched && !email" class="text-xs text-red-400 mt-1">Email is required</div>
        <div v-if="touched && email && !isEmailValid()" class="text-xs text-red-400 mt-1">Email is invalid</div>
        <div v-if="fieldErrors.email" class="text-xs text-red-400 mt-1">{{ fieldErrors.email }}</div>
      </div>
      <div class="mb-4">
        <label class="block text-gray-200 mb-1">Password</label>
        <input v-model="password" type="password" class="w-full px-3 py-2 rounded bg-neutral-700 text-white focus:outline-none" />
        <div v-if="touched && !password" class="text-xs text-red-400 mt-1">Password is required</div>
        <div v-if="fieldErrors.password" class="text-xs text-red-400 mt-1">{{ fieldErrors.password }}</div>
      </div>
      <div v-if="error" class="text-red-400 mb-4">{{ error }}</div>
      <button @click="handleAuth" :disabled="loading || !isFormValid()" class="w-full bg-primary-600 hover:bg-primary-700 text-white py-2 rounded disabled:opacity-60">
        {{ loading ? (mode === 'login' ? 'Logging in...' : 'Registering...') : (mode === 'login' ? 'Login' : 'Register') }}
      </button>
    </div>
  </div>
</template>
