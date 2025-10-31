<template>
  <NuxtLayout>
    <NuxtPage />
  </NuxtLayout>
  
  <ToastDisplay />
  
  <div v-if="isModalVisible" class="modal-overlay" @click="closeModal">
    <div class="modal-content" @click.stop>
      
      <button @click="closeModal" class="modal-close-button">&times;</button>
      
      <SearchResult
        :loading="loading"
        :error="error"
        :result="apiResult"
        :conjugation-result="conjugationResult"
        :original-search-word="selectedWord"
        :has-searched="true"
      />
    </div>
  </div>
  
</template>

<script setup>
import ToastDisplay from '@/components/ToastDisplay.vue'

// --- 2. THÊM LOGIC STATE TOÀN CỤC (Import từ composables) ---
import { 
  useLookupModalVisible,
  useLookupSelectedWord,
  useLookupApiResult,
  useLookupConjugationResult,
  useLookupLoading,
  useLookupError
} from '~/composables/useLookupState' 

// Import component SearchResult
import SearchResult from '~/components/SearchResult.vue'

// Gán state vào các biến
const isModalVisible = useLookupModalVisible()
const selectedWord = useLookupSelectedWord()
const apiResult = useLookupApiResult()
const conjugationResult = useLookupConjugationResult()
const loading = useLookupLoading()
const error = useLookupError()

// Hàm đóng modal
const closeModal = () => {
  isModalVisible.value = false
  apiResult.value = null
  conjugationResult.value = null
  error.value = ''
}
</script>

<style>
/* 3. THÊM CSS MODAL VÀO GLOBAL (hoặc file css chính) */
/* (Xóa <style scoped> trong file layout/default.vue nếu bạn đã lỡ dán vào đó) */
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background-color: rgba(0, 0, 0, 0.6);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
}

.modal-content {
  background: white;
  padding: 2rem;
  border-radius: 8px;
  width: 90%;
  max-width: 800px;
  max-height: 80vh; 
  overflow-y: auto; 
  position: relative;
  box-shadow: 0 4px 15px rgba(0,0,0,0.2);
}

.modal-close-button {
  position: absolute;
  top: 10px;
  right: 15px;
  font-size: 2rem;
  font-weight: bold;
  color: #888;
  background: none;
  border: none;
  cursor: pointer;
  line-height: 1;
}
.modal-close-button:hover {
  color: #000;
}
</style>