<script setup lang="ts">
import { useJwt } from '@/composables/useJwt'
import { useRouter } from 'vue-router'
import { watch } from 'vue'
import { useToast } from '@/composables/useToast'
const target = "2025-12-7";
const diffDay = computed(() => getDaysUntil(target));

const { jwt, isAuthenticated, logout, avatarUrl, username } = useJwt()
const { showToast } = useToast()
const router = useRouter()

watch(jwt, (newJwt) => {
  console.log('[JWT atom changed]', newJwt)
}, { immediate: true })

function handleLogin() {
  router.push('/login')
}
function handleLogout() {
  logout()
  showToast('Logout successful!', 'success')
}
</script>

<template>
  <header class="bg-neutral-900 px-6 py-3 flex items-center justify-between">
    <!-- Left: Logo -->
    <div class="flex items-center space-x-10">
      <NuxtLink to="/">
        <img src="/logo.png" alt="Logo" class="h-10 w-auto" />
      </NuxtLink>

      <div class="relative flex items-center cursor-pointer group">
        <UIcon name="i-lucide-calendar" class="text-4xl text-gray-300" />
        <span
          class="absolute top-3.25 right-0.25 text-white text-xs font-bold rounded-full px-2 py-0.5"
        >
          {{ diffDay }}
        </span>
        <div
          class="absolute top-10 left-20 -translate-x-1/2 opacity-0 group-hover:opacity-100 transition duration-300 bg-primary-600 text-sm px-3 py-1 rounded-lg whitespace-nowrap shadow-lg"
        >
          {{ diffDay }} days left until JLPT test
        </div>
      </div>
    </div>

    <!-- Center: Nav Links -->
    <div class="flex-1 flex justify-center">
      <nav class="flex space-x-4 text-gray-400">
        <NuxtLink
          to="/"
          class="flex items-center space-x-2 px-3 py-2 rounded-lg transition-colors hover:bg-gray-700 hover:text-white [&.router-link-exact-active]:bg-neutral-600 [&.router-link-exact-active]:text-primary-400"
        >
          <UIcon name="i-lucide-house" class="w-6 h-6" />
          <span>Home</span>
        </NuxtLink>

        <NuxtLink
          to="/translate"
          class="flex items-center space-x-2 px-3 py-2 rounded-lg transition-colors hover:bg-gray-700 hover:text-white [&.router-link-exact-active]:bg-neutral-600 [&.router-link-exact-active]:text-primary-400"
        >
          <UIcon name="i-custom-translation" class="w-6 h-6" />
          <span>Translate</span>
        </NuxtLink>
      </nav>
    </div>

    <!-- Right: User/Settings -->
    <div class="flex items-center space-x-4 relative">
      <div class="relative group">
        <div>
          <img v-if="isAuthenticated && avatarUrl" :src="avatarUrl" :alt="username || 'user'" class="rounded-full w-10 h-10 object-cover cursor-pointer border-2 border-primary-400" :title="username" />
          <UButton v-else icon="i-lucide-user" size="xl" variant="ghost" />
        </div>
        <div
          class="absolute right-0 mt-2 w-32 bg-neutral-800 border border-neutral-700 rounded shadow-lg z-10 opacity-0 group-hover:opacity-100 transition-opacity"
        >
          <button
            v-if="!isAuthenticated"
            @click="handleLogin"
            class="block w-full px-4 py-2 text-left text-gray-200 hover:bg-neutral-700"
          >
            Login
          </button>
          <button
            v-else
            @click="handleLogout"
            class="block w-full px-4 py-2 text-left text-gray-200 hover:bg-neutral-700"
          >
            Logout
          </button>
        </div>
      </div>
      <UButton icon="i-lucide-settings" size="xl" variant="ghost" />
    </div>
  </header>
</template>

    <!-- <div class="flex-1 flex justify-center">

      <nav class="flex space-x-4 text-gray-400">

        <NuxtLink

          to="/"

          class="flex items-center space-x-2 px-3 py-2 rounded-lg transition-colors hover:bg-gray-700 hover:text-white [&.router-link-exact-active]:bg-neutral-600 [&.router-link-exact-active]:text-primary-400"

        >

          <UIcon name="i-lucide-house" class="w-6 h-6" />

          <span>Home</span>

        </NuxtLink>



        <NuxtLink

          to="/translate"

          class="flex items-center space-x-2 px-3 py-2 rounded-lg transition-colors hover:bg-gray-700 hover:text-white [&.router-link-exact-active]:bg-neutral-600 [&.router-link-exact-active]:text-primary-400"

        >

          <UIcon name="i-custom-translation" class="w-6 h-6" />

          <span>Translate</span>

        </NuxtLink>

      </nav>

    </div>



    <!-- Right: User/Settings -->
<!-- 
    <div class="flex items-center space-x-4">

      <UButton icon="i-lucide-user" size="xl" variant="ghost" />

      <UButton icon="i-lucide-settings" size="xl" variant="ghost" />

    </div>

  </header>

</template> -->


