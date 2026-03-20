<template>
  <div class="flex flex-col min-h-screen">
    <AppNavBar class="fixed top-0 left-0 right-0 z-50 shadow-md" />

    <div class="fixed top-[64px] left-0 h-[calc(100vh-64px)] flex">

      <!-- WorkspaceSidebar: chỉ chiếm w-14 (icon bar) -->
      <!-- Panel project dùng absolute nên không đẩy sidebar nav -->
      <WorkspaceSidebar v-if="isAuthenticated" @panel-change="onPanelChange" />

      <!-- Sidebar nav cũ -->
      <aside
        class="w-56 h-full bg-white text-gray-900 border-r border-gray-200 dark:bg-gray-800 dark:text-white dark:border-gray-700 overflow-y-auto transition-colors flex-shrink-0"
      >
        <AppSideBar />
      </aside>

    </div>

    <!-- main: margin cố định = w-14 icon + w-56 sidenav -->
    <main
      :class="[
        'mt-[64px] bg-gray-50 dark:bg-neutral-900 p-6 overflow-y-auto h-[calc(100vh-64px)] transition-all duration-200',
        isAuthenticated ? 'ml-[294px]' : 'ml-56'
      ]"
    >
      <slot />
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useJwt } from '~/composables/useJwt'

const { isAuthenticated } = useJwt()
const panelOpen = ref(false)

function onPanelChange(isOpen: boolean) {
  panelOpen.value = isOpen
}
</script>