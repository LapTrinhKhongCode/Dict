<template>
  <NuxtLayout>
    <NuxtPage />
  </NuxtLayout>

  <ToastDisplay />

  <WordResultModal
    v-if="isLookupModalVisible"
    :search-word="selectedWord"
    @close="isLookupModalVisible = false"
  />

  <TranslationModal
    v-if="isTranslateModalVisible"
    :search-word="selectedWord"
    @close="isTranslateModalVisible = false"
  />
</template>

<script setup>
import ToastDisplay from "@/components/ToastDisplay.vue";
import WordResultModal from "~/components/WordResultModal.vue";
import TranslationModal from "~/components/translate/TranslationModal.vue"; // <-- THÊM MỚI

// Import các state toàn cục (thêm state mới)
import {
  useLookupModalVisible,
  useTranslateModalVisible, // <-- THÊM MỚI
  useLookupSelectedWord,
} from "~/composables/useLookupState";

// Gán state
const isLookupModalVisible = useLookupModalVisible();
const isTranslateModalVisible = useTranslateModalVisible(); // <-- THÊM MỚI
const selectedWord = useLookupSelectedWord();
</script>

<style>
/* CSS toàn cục (nếu có) */
</style>
