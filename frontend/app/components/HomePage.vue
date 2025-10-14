<!-- src/components/HomePage.vue -->
<template>
  <div class="min-h-screen text-white p-4 sm:p-8">
    <div class="max-w-6xl mx-auto">
      <header class="mb-8">
        <h1 class="text-4xl font-bold text-sky-400">Khám phá các bộ Flashcard</h1>
        <p class="text-gray-400 mt-2">Chọn một bộ từ vựng để bắt đầu học ngay hôm nay.</p>
      </header>
      
      <div v-if="isLoading" class="text-center text-gray-400">Đang tải các bộ thẻ...</div>
      <div v-else-if="error" class="text-center text-red-400 p-4 bg-red-900/50 rounded-lg">{{ error }}</div>

      <div v-else class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
        <div
          v-for="set in sets"
          :key="set.id"
          @click="emit('select-set', set.id)"
          class="bg-gray-800 rounded-lg p-5 flex flex-col justify-between h-48 cursor-pointer transition-transform transform hover:-translate-y-1 hover:shadow-2xl hover:shadow-sky-500/20"
        >
          <div>
            <h2 class="text-xl font-bold text-gray-100">{{ set.name }}</h2> <!-- Sửa: title -> name -->
            <p class="text-sm text-gray-400 mt-1">{{ set.cardCount }} từ</p>
          </div>
          <div class="flex items-center justify-between mt-4">
            <div class="flex items-center">
              <div class="w-8 h-8 rounded-full mr-3 bg-gray-700"></div>
              <span class="text-sm text-gray-300">{{ set.authorName }}</span> <!-- Sửa: creatorName -> authorName -->
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import type { DeckSummaryDto } from '~/types';

const emit = defineEmits(['select-set']);

const sets = ref<DeckSummaryDto[]>([]);
const isLoading = ref(true);
const error = ref<string | null>(null);

const BASE_URL = 'https://localhost:7084/api';

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

onMounted(async () => {
  try {
    // ✨ SỬA: Cập nhật route API
    const response = await fetch(`${BASE_URL}/decks/public`);
    sets.value = await handleResponse<DeckSummaryDto[]>(response);
  } catch (err: any) {
    error.value = `Không thể tải các bộ thẻ: ${err.message}. Backend server có đang chạy không?`;
    console.error(err);
  } finally {
    isLoading.value = false;
  }
});
</script>