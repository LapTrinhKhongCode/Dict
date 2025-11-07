<template>
  <nav
    class="flex flex-col space-y-1 h-full p-4 bg-gray-100 dark:bg-neutral-800 text-gray-600 dark:text-gray-400 transition-colors"
  >
    <NuxtLink
      to="/search"
      class="flex items-center space-x-3 px-3 py-2 rounded-lg transition-colors hover:bg-gray-200 hover:text-gray-900 dark:hover:bg-neutral-700 dark:hover:text-white [&.router-link-exact-active]:bg-primary-100 [&.router-link-exact-active]:text-primary-700 [&.router-link-exact-active]:font-semibold dark:[&.router-link-exact-active]:bg-neutral-700 dark:[&.router-link-exact-active]:text-primary-400"
    >
      <UIcon name="i-lucide-search" class="w-6 h-6" />
      <span>Tra cứu</span>
    </NuxtLink>

    <NuxtLink
      to="/alphabet"
      class="flex items-center space-x-3 px-3 py-2 rounded-lg transition-colors hover:bg-gray-200 hover:text-gray-900 dark:hover:bg-neutral-700 dark:hover:text-white [&.router-link-exact-active]:bg-primary-100 [&.router-link-exact-active]:text-primary-700 [&.router-link-exact-active]:font-semibold dark:[&.router-link-exact-active]:bg-neutral-700 dark:[&.router-link-exact-active]:text-primary-400"
    >
      <UIcon name="i-custom-alphabet" class="w-6 h-6" />
      <span>Bảng chữ cái</span>
    </NuxtLink>

    <NuxtLink
      to="/kanji"
      class="flex items-center space-x-3 px-3 py-2 rounded-lg transition-colors hover:bg-gray-200 hover:text-gray-900 dark:hover:bg-neutral-700 dark:hover:text-white [&.router-link-exact-active]:bg-primary-100 [&.router-link-exact-active]:text-primary-700 [&.router-link-exact-active]:font-semibold dark:[&.router-link-exact-active]:bg-neutral-700 dark:[&.router-link-exact-active]:text-primary-400"
    >
      <UIcon name="i-custom-kanji" class="w-6 h-6" />
      <span>Hán tự</span>
    </NuxtLink>

    <!-- <NuxtLink
      to="/word"
      class="flex items-center space-x-3 px-3 py-2 rounded-lg transition-colors hover:bg-gray-200 hover:text-gray-900 dark:hover:bg-neutral-700 dark:hover:text-white [&.router-link-exact-active]:bg-primary-100 [&.router-link-exact-active]:text-primary-700 [&.router-link-exact-active]:font-semibold dark:[&.router-link-exact-active]:bg-neutral-700 dark:[&.router-link-exact-active]:text-primary-400"
    >
      <UIcon name="i-custom-word" class="w-6 h-6" />
      <span>Từ vựng</span>
    </NuxtLink> -->

    <NuxtLink
      to="/explore"
      class="flex items-center space-x-3 px-3 py-2 rounded-lg transition-colors hover:bg-gray-200 hover:text-gray-900 dark:hover:bg-neutral-700 dark:hover:text-white [&.router-link-exact-active]:bg-primary-100 [&.router-link-exact-active]:text-primary-700 [&.router-link-exact-active]:font-semibold dark:[&.router-link-exact-active]:bg-neutral-700 dark:[&.router-link-exact-active]:text-primary-400"
    >
      <UIcon name="i-lucide-copy" class="w-6 h-6" />
      <span>Flashcard</span>
    </NuxtLink>

    <NuxtLink
      to="/ocr"
      class="flex items-center space-x-3 px-3 py-2 rounded-lg transition-colors hover:bg-gray-200 hover:text-gray-900 dark:hover:bg-neutral-700 dark:hover:text-white [&.router-link-exact-active]:bg-primary-100 [&.router-link-exact-active]:text-primary-700 [&.router-link-exact-active]:font-semibold dark:[&.router-link-exact-active]:bg-neutral-700 dark:[&.router-link-exact-active]:text-primary-400"
    >
      <UIcon name="i-lucide-scan-text" class="w-6 h-6" />
      <span>Nhận dạng văn bản</span>
    </NuxtLink>

    <NuxtLink
      to="/reading"
      class="flex items-center space-x-3 px-3 py-2 rounded-lg transition-colors hover:bg-gray-200 hover:text-gray-900 dark:hover:bg-neutral-700 dark:hover:text-white [&.router-link-exact-active]:bg-primary-100 [&.router-link-exact-active]:text-primary-700 [&.router-link-exact-active]:font-semibold dark:[&.router-link-exact-active]:bg-neutral-700 dark:[&.router-link-exact-active]:text-primary-400"
    >
      <UIcon name="i-lucide-newspaper" class="w-6 h-6" />
      <span>Luyện đọc</span>
    </NuxtLink>

    <div
      class="px-3 pt-6 pb-2 text-xs font-semibold text-gray-500 dark:text-gray-400 uppercase tracking-wider"
    >
      Settings
    </div>
    <NuxtLink
      v-if="isAuthenticated && role === 'ADMIN'"
      to="/admin"
      class="flex items-center space-x-3 px-3 py-2 rounded-lg transition-colors hover:bg-gray-200 hover:text-gray-900 dark:hover:bg-neutral-700 dark:hover:text-white [&.router-link-exact-active]:bg-primary-100 [&.router-link-exact-active]:text-primary-700 [&.router-link-exact-active]:font-semibold dark:[&.router-link-exact-active]:bg-neutral-700 dark:[&.router-link-exact-active]:text-primary-400"
    >
      <UIcon name="i-lucide-settings" class="w-6 h-6" />
      <span>Trang Admin</span>
    </NuxtLink>

    <NuxtLink
      to="/premium"
      class="flex items-center space-x-3 px-3 py-2 rounded-lg transition-colors hover:bg-gray-200 hover:text-gray-900 dark:hover:bg-neutral-700 dark:hover:text-white [&.router-link-exact-active]:bg-primary-100 [&.router-link-exact-active]:text-primary-700 [&.router-link-exact-active]:font-semibold dark:[&.router-link-exact-active]:bg-neutral-700 dark:[&.router-link-exact-active]:text-primary-400"
    >
      <UIcon name="i-lucide-credit-card" class="w-6 h-6" />
      <span>Premium</span>
    </NuxtLink>
  </nav>
</template>

<script setup lang="ts">
import { useJwt } from "@/composables/useJwt";
const { isAuthenticated, role } = useJwt();
</script>
