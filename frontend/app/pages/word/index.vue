<!-- app.vue -->
<template>
  <div id="app" class="bg-gray-900 min-h-screen">
    <!-- Hiển thị loading toàn cục -->
    <div v-if="isLoading" class="flex justify-center items-center h-screen">
      <div class="text-white text-xl flex items-center">
        <svg class="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
          <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
          <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
        </svg>
        <span>Đang xử lý...</span>
      </div>
    </div>

    <!-- Hiển thị các trang -->
    <HomePage
      v-else-if="currentView === 'home'"
      @select-set="selectSet"
    />
    <CardListPage
      v-else-if="currentView === 'list' && selectedSet"
      :set="selectedSet"
      @start-review="startReview"
      @go-home="goHome"
    />
    <FlashCardSet
      v-else-if="currentView === 'review' && selectedSet"
      :deck-id="selectedSet.id"
      @go-to-list="goToList"
    />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import type { DeckDetailDto } from '~/types';

const BASE_URL = 'https://localhost:7084/api';

type View = 'home' | 'list' | 'review';
const currentView = ref<View>('home');
const selectedSet = ref<DeckDetailDto | null>(null);
const isLoading = ref(false);

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: 'Lỗi không xác định' }));
    throw new Error(error.message || 'Yêu cầu API thất bại');
  }
  const data = await response.json();
  if (data.isSuccess) {
    return data.result as T;
  } else {
    throw new Error(data.message || 'Yêu cầu API thất bại');
  }
}

async function refreshSelectedSet() {
  if (!selectedSet.value) return;
  try {
    // ✨ SỬA: Cập nhật route API
    const response = await fetch(`${BASE_URL}/decks/${selectedSet.value.id}`);
    selectedSet.value = await handleResponse<DeckDetailDto>(response);
  } catch (error) {
    console.error("Không thể làm mới dữ liệu bộ thẻ:", error);
  }
}

async function selectSet(setId: number) {
  isLoading.value = true;
  try {
    // ✨ SỬA: Cập nhật route API
    const response = await fetch(`${BASE_URL}/decks/${setId}`);
    selectedSet.value = await handleResponse<DeckDetailDto>(response);
    currentView.value = 'list';
  } catch (error) {
    console.error("Không thể tải chi tiết bộ thẻ:", error);
    alert("Không thể tải chi tiết bộ thẻ. Vui lòng thử lại.");
  } finally {
    isLoading.value = false;
  }
}

function startReview() {
  if (selectedSet.value) {
    currentView.value = 'review';
  }
}

function goHome() {
  currentView.value = 'home';
  selectedSet.value = null;
}

async function goToList() {
  isLoading.value = true;
  await refreshSelectedSet();
  currentView.value = 'list';
  isLoading.value = false;
}
</script>