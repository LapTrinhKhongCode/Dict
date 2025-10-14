<!-- src/components/CardListPage.vue -->
<template>
  <div class="min-h-screen text-white p-4 sm:p-8">
    <div v-if="set" class="max-w-5xl mx-auto">
      <header class="flex flex-wrap justify-between items-center mb-6 gap-4">
        <div>
          <button @click="emit('go-home')" class="flex items-center text-sm text-sky-400 hover:text-sky-300 transition-colors mb-2">
            &larr; Quay lại trang chủ
          </button>
          <h1 class="text-3xl font-bold text-sky-400">{{ set.title }}</h1>
          <p class="text-gray-400 mt-1">{{ set.description }}</p>
        </div>
        <!-- ✨ SỬA: Cập nhật nút "Bắt đầu học" -->
        <button
          @click="emit('start-review')"
          :disabled="dueCardCount === 0"
          class="bg-sky-500 hover:bg-sky-600 text-white font-bold py-2 px-6 rounded-lg transition-colors disabled:bg-gray-600 disabled:cursor-not-allowed"
        >
          Bắt đầu học ({{ dueCardCount }} từ)
        </button>
      </header>
      <div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
        <div
          v-for="card in set.cards"
          :key="card.id"
          class="bg-gray-800 rounded-lg p-4 flex flex-col justify-between h-40 relative group"
        >
          <div>
            <div class="text-4xl font-semibold mb-2">{{ card.charBig }}</div>
            <p class="text-gray-300">{{ card.meaning }}</p>
          </div>
          <div class="flex justify-between items-center">
            <div class="text-xs text-sky-500 mt-2">
              Lần ôn tới: {{ countdownStrings[card.id] }}
            </div>
            
            <button 
              @click.stop="resetCardProgress(card)" 
              :disabled="resettingCardId === card.id"
              class="absolute top-2 right-2 p-1.5 rounded-full bg-gray-700/50 text-gray-400 hover:bg-sky-500 hover:text-white opacity-0 group-hover:opacity-100 transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
              title="Đặt lại tiến độ thẻ"
            >
              <svg v-if="resettingCardId === card.id" class="animate-spin h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
              </svg>
              <svg v-else xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M4 4v5h5M20 20v-5h-5M4 4l1.5 1.5A9 9 0 0120.5 15" />
              </svg>
            </button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, onUnmounted, computed } from 'vue'; // ✨ SỬA: Thêm 'computed'
import type { DeckDetailDto, CardDto } from '~/types';

const props = defineProps<{ set: DeckDetailDto | null }>();
const emit = defineEmits(['start-review', 'go-home']);

const resettingCardId = ref<number | null>(null);
const BASE_URL = 'https://localhost:7084/api';

const countdownStrings = ref<{ [cardId: number]: string }>({});
let timer: ReturnType<typeof setInterval> | null = null;

// ✨ SỬA: Thêm computed property để đếm số thẻ đến hạn
const dueCardCount = computed(() => {
  if (!props.set) return 0;

  const now = new Date();
  return props.set.cards.filter(card => {
    const dueDate = parseDateAsUTC(card.nextReviewAt);
    // Thẻ được tính là đến hạn nếu là thẻ mới hoặc đã quá hạn
    return dueDate.getFullYear() < 2000 || dueDate <= now;
  }).length;
});

function parseDateAsUTC(dateString: string): Date {
  if (!dateString) return new Date(0);
  const isoString = dateString.replace(' ', 'T') + 'Z';
  return new Date(isoString);
}

const updateCountdowns = () => {
  if (!props.set) return;
  const now = new Date();
  for (const card of props.set.cards) {
    const dueDate = parseDateAsUTC(card.nextReviewAt);
    if (dueDate.getFullYear() < 2000) {
      countdownStrings.value[card.id] = 'Thẻ mới';
      continue;
    }
    const diffSeconds = Math.round((dueDate.getTime() - now.getTime()) / 1000);
    if (diffSeconds < 0) {
      countdownStrings.value[card.id] = 'Sẵn sàng ôn tập';
      continue;
    }
    const diffMinutes = Math.floor(diffSeconds / 60);
    const diffHours = Math.floor(diffMinutes / 60);
    const diffDays = Math.floor(diffHours / 24);
    if (diffMinutes < 60) {
      const minutes = Math.floor(diffSeconds / 60);
      const seconds = diffSeconds % 60;
      countdownStrings.value[card.id] = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
    } else if (diffHours < 24) {
      countdownStrings.value[card.id] = `trong ${diffHours} giờ`;
    } else {
      countdownStrings.value[card.id] = `trong ${diffDays} ngày`;
    }
  }
};

const setupRealtimeUpdates = () => {
  if (timer) clearInterval(timer);
  updateCountdowns();
  timer = setInterval(updateCountdowns, 1000);
};

watch(() => props.set, (newSet) => {
  if (newSet && newSet.cards) {
    setupRealtimeUpdates();
  }
}, { immediate: true, deep: true });

onUnmounted(() => {
  if (timer) clearInterval(timer);
});

async function handleResponse(response: Response) {
  if (!response.ok) {
    const error = await response.json().catch(() => ({ message: 'Lỗi không xác định' }));
    throw new Error(error.message || 'Yêu cầu API thất bại');
  }
  const data = await response.json();
  if (!data.isSuccess) {
    throw new Error(data.message || 'Yêu cầu API thất bại');
  }
  return data;
}

async function resetCardProgress(card: CardDto) {
  if (resettingCardId.value !== null) return;
  if (!confirm(`Bạn có chắc muốn đặt lại tiến độ cho thẻ "${card.meaning}" không?`)) {
    return;
  }
  resettingCardId.value = card.id;
  try {
    const response = await fetch(`${BASE_URL}/review/cards/${card.id}/reset`, {
      method: 'POST'
    });
    await handleResponse(response);
    card.nextReviewAt = new Date(0).toISOString();
  } catch (err: any) {
    console.error("Lỗi khi reset thẻ:", err);
    alert(`Không thể đặt lại tiến độ thẻ: ${err.message}`);
  } finally {
    resettingCardId.value = null;
  }
}
</script>