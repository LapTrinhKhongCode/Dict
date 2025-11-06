<script setup lang="ts">
// (Script của bạn được giữ nguyên)
</script>

<template>
  <div class="flex flex-col min-h-screen">
    <!-- 🔹 Top Navigation -->
    <AppNavBar class="fixed top-0 left-0 right-0 z-50 shadow-md" />

    <!-- 🔹 Sidebar -->
    <aside
      class="fixed top-[64px] left-0 w-80 h-[calc(100vh-64px)] bg-gray-800 text-white p-4 overflow-y-auto"
    >
      <AppSideBar />
    </aside>

    <!-- 🔹 Main Content -->
    <main
      class="ml-80 mt-[64px] bg-neutral-700 p-6 overflow-y-auto h-[calc(100vh-64px)]"
    >
      <slot />
    </main>
  </div>
</template>