<template>
  <NuxtLayout>
    <NuxtPage />
  </NuxtLayout>
  
  <ToastDisplay />
  
  <WordResultModal
    v-if="isModalVisible"
    :search-word="selectedWord"
    @close="isModalVisible = false"
  />
  
</template>

<script setup>
import ToastDisplay from '@/components/ToastDisplay.vue'
import WordResultModal from '~/components/WordResultModal.vue'

// Import các state toàn cục (Giờ chỉ cần 2 state)
import { 
  useLookupModalVisible,
  useLookupSelectedWord,
} from '~/composables/useLookupState' 

// Gán state
const isModalVisible = useLookupModalVisible()
const selectedWord = useLookupSelectedWord()

// KHÔNG CẦN import SearchResult, apiResult, loading, error ở đây nữa
</script>

<style>
/* XÓA TOÀN BỘ CSS CỦA MODAL CŨ (modal-overlay, modal-content) 
  Vì component <WordResultModal> của bạn sẽ tự quản lý style của nó.
*/
</style>