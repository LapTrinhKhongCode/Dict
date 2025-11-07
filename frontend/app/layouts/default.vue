<script setup lang="ts">
// (Script của bạn được giữ nguyên)
</script>

<template>
  <div class="flex flex-col min-h-screen">
    <AppNavBar class="fixed top-0 left-0 right-0 z-50 shadow-md" />

    <aside
      class="fixed top-[64px] left-0 w-80 h-[calc(100vh-64px)] bg-white text-gray-900 border-r border-gray-200 dark:bg-gray-800 dark:text-white dark:border-gray-700 p-4 overflow-y-auto transition-colors"
    >
      <AppSideBar />
    </aside>

    <main
      class="ml-80 mt-[64px] bg-gray-50 dark:bg-neutral-900 p-6 overflow-y-auto h-[calc(100vh-64px)] transition-colors"
    >
      <slot />
    </main>
  </div>
</template>