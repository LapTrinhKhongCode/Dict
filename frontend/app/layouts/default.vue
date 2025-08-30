<script setup lang="ts">
</script>

<template>
  <div class="flex flex-col min-h-screen">
    <!-- 🔹 Top Navigation -->
   <AppNavBar class="fixed top-0 left-0 right-0 z-50 shadow-md" />

    <!-- 🔹 Sidebar + Main Content -->
    <div class="flex flex-1 pt-[64px]"> <!-- offset for navbar height -->
      <!-- Sidebar -->
      <aside class="w-80 bg-gray-800 text-white p-4">
        <AppSideBar class="h-full" />
      </aside>

      <!-- Main -->
      <main class="flex-1 bg-neutral-700 p-6 overflow-y-auto">
        <slot />
      </main>
    </div>

    <!-- 🔹 Bottom Sidebar / Footer -->
    <footer class="bg-gray-100 border-t p-4 flex justify-around">
      
    </footer>
  </div>
</template>
