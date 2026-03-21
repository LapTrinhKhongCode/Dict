<template>
  <div class="flex flex-col min-h-screen">
    <AppNavBar class="fixed top-0 left-0 right-0 z-50 shadow-md" />

    <div class="fixed top-[64px] left-0 h-[calc(100vh-64px)] flex">
      <WorkspaceSidebar v-if="isAuthenticated" @panel-change="onPanelChange" />
      <aside
        class="w-56 h-full bg-white text-gray-900 border-r border-gray-200 dark:bg-gray-800 dark:text-white dark:border-gray-700 overflow-y-auto transition-colors flex-shrink-0"
      >
        <AppSideBar />
      </aside>
    </div>

    <!-- main: p-0 overflow-hidden cho full-bleed pages (sensei, reader...) -->
    <!-- p-6 overflow-y-auto cho pages thông thường -->
    <main
      :class="[
        'mt-[64px] h-[calc(100vh-64px)] transition-all duration-200',
        isAuthenticated ? 'ml-[294px]' : 'ml-56',
        isFullBleed ? 'p-0 overflow-hidden' : 'p-6 overflow-y-auto bg-gray-50 dark:bg-neutral-900'
      ]"
    >
      <slot />
    </main>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRoute } from 'vue-router'
import { useJwt } from '~/composables/useJwt'

const { isAuthenticated } = useJwt()
const route = useRoute()
const panelOpen = ref(false)

// Trang full-bleed: không có padding, không scroll ngoài
const fullBleedRoutes = ['/sensei', '/reader']
const isFullBleed = computed(() =>
  fullBleedRoutes.some(r => route.path.startsWith(r))
)

function onPanelChange(isOpen: boolean) {
  panelOpen.value = isOpen
}
</script>