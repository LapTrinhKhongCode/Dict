<!-- src/components/FlashCardSet.vue -->
<template>
  <div class="min-h-screen flex flex-col items-center justify-center p-6 bg-gray-900 relative">
    <div v-if="isLoading" class="text-center text-gray-400">Đang tải phiên học...</div>
    <div v-else-if="error" class="text-center text-red-400 p-4 bg-red-900/50 rounded-lg">{{ error }}</div>
    
    <div v-else-if="isComplete" class="text-center">
      <div class="bg-gray-800 p-8 rounded-lg shadow-xl">
        <h2 class="text-3xl font-bold text-green-400 mb-4">🎉 Chúc mừng! 🎉</h2>
        <p class="text-gray-300 mb-6">Bạn đã hoàn thành phiên học này.</p>
        <button
          @click="emit('go-to-list')"
          class="bg-sky-500 hover:bg-sky-600 text-white font-bold py-2 px-6 rounded-lg transition-colors"
        >
          Quay lại danh sách
        </button>
      </div>
    </div>

    <div v-else class="w-full max-w-2xl relative">
      <FlashCard
        :card="currentCard"
        :remaining="reviewQueue.length"
        @answer="onAnswer"
        :key="currentCard?.id"
        @go-back="emit('go-to-list')"
      />
      <button @click="prevCard" class="nav-btn left-0 -translate-x-16 sm:-translate-x-24">‹</button>
      <button @click="nextCard" class="nav-btn right-0 translate-x-16 sm:translate-x-24">›</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import FlashCard from './FlashCard.vue';
import type { CardDto, AnswerRequestDto } from '~/types/index';
import { useJwt } from '~/composables/useJwt';

const { jwt } = useJwt();

const props = defineProps<{ deckId: number }>();
const emit = defineEmits(['go-to-list']);

const reviewQueue = ref<CardDto[]>([]);
const currentIndex = ref(0);
const isLoading = ref(true);
const error = ref<string | null>(null);

const config = useRuntimeConfig()
const BASE_URL = config.public.apiBaseUrl


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
    const response = await fetch(`${BASE_URL}/api/review/GetQueue/${props.deckId}`,
       { cache: 'no-store', headers: { 'Authorization': `Bearer ${jwt.value}` } }
    );
    reviewQueue.value = await handleResponse<CardDto[]>(response);
    if (reviewQueue.value.length > 1) {
      shuffleQueue();
    }
  } catch (err: any) {
    error.value = `Không thể tải phiên học: ${err.message}. Backend server có đang chạy không?`;
    console.error(err);
  } finally {
    isLoading.value = false;
  }
});

const isComplete = computed(() => reviewQueue.value.length === 0);
const currentCard = computed<CardDto | null>(() => reviewQueue.value[currentIndex.value] ?? null);

function shuffleQueue() {
  for (let i = reviewQueue.value.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [reviewQueue.value[i]!, reviewQueue.value[j]!] = [reviewQueue.value[j]!, reviewQueue.value[i]!];
  }
}

function nextCard() {
  if (reviewQueue.value.length === 0) return;
  currentIndex.value = (currentIndex.value + 1) % reviewQueue.value.length;
}

function prevCard() {
  if (reviewQueue.value.length === 0) return;
  currentIndex.value = (currentIndex.value - 1 + reviewQueue.value.length) % reviewQueue.value.length;
}

async function onAnswer(difficulty: 'again' | 'hard' | 'good' | 'easy') {
  if (!currentCard.value) return;

  const qualityMap = { again: 1, hard: 2, good: 3, easy: 4 };

  const answer: AnswerRequestDto = {
    cardId: currentCard.value.id,
    quality: qualityMap[difficulty] as 1 | 2 | 3 | 4
  };

  try {
    // ✨ SỬA: Cập nhật route API
    const response = await fetch(`${BASE_URL}/api/review/PostAnswer`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' ,
           'Authorization': `Bearer ${jwt.value}`
        },
        body: JSON.stringify(answer)
    });

    const responseData = await handleResponse(response); // Đã bao gồm check lỗi

    reviewQueue.value.splice(currentIndex.value, 1);

    if (currentIndex.value >= reviewQueue.value.length && reviewQueue.value.length > 0) {
      currentIndex.value = 0;
    }
  } catch (err: any) {
    console.error("Không thể lưu tiến độ:", err);
    alert(`Không thể lưu tiến độ của bạn: ${err.message}`);
  }
}
</script>

<style scoped>
.nav-btn {
  position: absolute;
  top: 50%;
  transform: translateY(-50%);
  font-size: 3rem;
  color: #6B7280;
  transition: color 0.2s;
}
.nav-btn:hover {
  color: #FFFFFF;
}
</style>