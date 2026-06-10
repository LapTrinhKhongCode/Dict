<!-- app.vue -->
<template>
  <NuxtLoadingIndicator color="#f0c040" :height="3" />
  <NuxtLayout>
    <NuxtPage :transition="{ name: 'page' }" />
  </NuxtLayout>

  <ToastDisplay />

  <LazyWordResultModal
    v-if="isLookupModalVisible"
    :search-word="selectedWord"
    @close="isLookupModalVisible = false"
  />

  <LazyTranslationModal
    v-if="isTranslateModalVisible"
    :is-open="isTranslateModalVisible"
    :search-word="selectedWord"
    @close="isTranslateModalVisible = false"
  />
</template>

<script setup lang="ts">
import { onMounted, provide, watch } from "vue";
import ToastDisplay from "@/components/ToastDisplay.vue";
import {
  useLookupModalVisible,
  useTranslateModalVisible,
  useLookupSelectedWord,
} from "~/composables/useLookupState";

// modal states
const isLookupModalVisible = useLookupModalVisible();
const isTranslateModalVisible = useTranslateModalVisible();
const selectedWord = useLookupSelectedWord();

// SSR-safe state (useState auto-imported by Nuxt)
const theme = useState<"dark" | "light">("theme", () => "dark");

// Khi client mount: sync từ localStorage -> theme và set class
onMounted(() => {
  if (process.client) {
    const saved = localStorage.getItem("theme");
    if (saved === "dark" || saved === "light")
      theme.value = saved as "dark" | "light";
    document.documentElement.classList.toggle("dark", theme.value === "dark");
  }
});

// Khi theme thay đổi: lưu vào localStorage (client) và gắn class
watch(theme, (val) => {
  if (process.client) {
    try {
      localStorage.setItem("theme", val);
    } catch (e) {}
    document.documentElement.classList.toggle("dark", val === "dark");
  }
});

// provide cho các component con
const setTheme = (mode: "dark" | "light") => {
  theme.value = mode;
};
provide("theme", theme);
provide("setTheme", setTheme);
</script>

<style>
html,
body {
  transition: background-color 0.3s, color 0.3s;
}

.light {
  background-color: #f9f9f9;
  color: #222;
}

.dark {
  background-color: #121212;
  color: #f5f5f5;
}

/* Page transition — no mode, both pages animate simultaneously to avoid blank on rapid nav */
.page-enter-active {
  transition: opacity 0.12s ease;
}
.page-leave-active {
  transition: opacity 0.08s ease;
  position: absolute;
  width: 100%;
}
.page-enter-from {
  opacity: 0;
}
.page-leave-to {
  opacity: 0;
}
</style>
