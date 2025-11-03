<script setup lang="ts">
// (Script của bạn được giữ nguyên)
</script>

<template>
  <div class="flex flex-col min-h-screen">
    <AppNavBar class="fixed top-0 left-0 right-0 z-50 shadow-md" />

    <div class="flex flex-1 pt-[64px]">
      <aside
        class="w-80 bg-gray-100 text-gray-900 border-r border-gray-200 dark:bg-gray-800 dark:text-white dark:border-gray-700 p-4 transition-colors"
      >
        <AppSideBar class="h-full" />
      </aside>

      <main
        class="flex-1 bg-gray-50 dark:bg-neutral-900 p-6 overflow-y-auto transition-colors"
      >
        <slot />
      </main>
    </div>

    </div>
</template>