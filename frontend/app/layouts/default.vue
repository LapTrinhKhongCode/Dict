<template>
  <div class="flex flex-col min-h-screen">
    <AppNavBar class="fixed top-0 left-0 right-0 z-50 shadow-md" />

    <div class="fixed top-[64px] left-0 h-[calc(100vh-64px)] z-40">
      <WorkspaceSidebar @panel-change="onPanelChange" />
    </div>

    <main
      :class="[
        'mt-[64px] h-[calc(100vh-64px)] transition-[margin] duration-150',
        panelOpen ? 'ml-[256px]' : 'ml-14',
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

const route = useRoute()
const panelOpen = ref(false)

const fullBleedRoutes = ['/sensei', '/reader']
const isFullBleed = computed(() =>
  fullBleedRoutes.some(r => route.path.startsWith(r))
)

function onPanelChange(isOpen: boolean) {
  panelOpen.value = isOpen
}
</script>