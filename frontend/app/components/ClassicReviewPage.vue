<!-- src/components/ClassicReviewPage.vue -->
<template>
  <div class="min-h-screen flex flex-col items-center justify-center p-4 bg-gray-900 text-white">
    <div class="w-full max-w-2xl">
      <!-- Header: Nút quay lại và bộ lọc -->
      <div class="flex justify-between items-center mb-4">
        <button @click="emit('go-to-list')" class="flex items-center text-sm text-sky-400 hover:text-sky-300">
          &larr; Quay lại danh sách
        </button>
        <div class="flex items-center gap-2">
            <span class="text-sm text-gray-400">Lọc:</span>
            <button @click="setFilter('all')" :class="['filter-btn', {'filter-active': filter === 'all'}]">Tất cả</button>
            <button @click="setFilter('not-remembered')" :class="['filter-btn', {'filter-active': filter === 'not-remembered'}]">Chưa nhớ</button>
            <button @click="setFilter('remembered')" :class="['filter-btn', {'filter-active': filter === 'remembered'}]">Đã nhớ</button>
        </div>
      </div>

      <!-- Card Area -->
      <div v-if="currentCard" class="bg-gray-800 rounded-lg p-6 relative">
        <div class="card-scene cursor-pointer" @click="isFlipped = !isFlipped">
          <div :class="['card-inner', { 'is-flipped': isFlipped }]">
            <div class="card-face card-front">{{ currentCard.charBig }}</div>
            <div class="card-face card-back">{{ currentCard.meaning }}</div>
          </div>
        </div>
        <div class="absolute bottom-4 left-6 text-sm text-gray-500">{{ currentIndex + 1 }} / {{ filteredCards.length }}</div>
      </div>
      <div v-else class="text-center text-gray-500 p-10 bg-gray-800 rounded-lg">
        <p>Không có thẻ nào trong bộ lọc này.</p>
      </div>

      <!-- Nút điều khiển -->
      <div class="flex justify-center gap-4 mt-6">
        <button @click="markAs('not-remembered')" class="control-btn bg-red-600 hover:bg-red-700">Chưa nhớ</button>
        <button @click="markAs('remembered')" class="control-btn bg-green-600 hover:bg-green-700">Đã nhớ</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import type { CardDto } from '~/types';

const props = defineProps<{ cards: CardDto[] }>();
const emit = defineEmits(['go-to-list']);

const currentIndex = ref(0);
const isFlipped = ref(false);
const memoryState = ref<Record<number, 'remembered' | 'not-remembered'>>({});
const filter = ref<'all' | 'remembered' | 'not-remembered'>('all');

const filteredCards = computed(() => {
  if (filter.value === 'all') {
    return props.cards;
  }
  return props.cards.filter(card => memoryState.value[card.id] === filter.value);
});

const currentCard = computed(() => filteredCards.value[currentIndex.value] ?? null);

function nextCard() {
    isFlipped.value = false;
    setTimeout(() => { // Đợi card lật lại rồi mới chuyển
        if (currentIndex.value < filteredCards.value.length - 1) {
            currentIndex.value++;
        } else {
            // Quay về thẻ đầu tiên nếu hết
            currentIndex.value = 0;
        }
    }, 200);
}

function markAs(status: 'remembered' | 'not-remembered') {
    if (!currentCard.value) return;
    memoryState.value[currentCard.value.id] = status;
    nextCard();
}

function setFilter(newFilter: 'all' | 'remembered' | 'not-remembered') {
    filter.value = newFilter;
    currentIndex.value = 0;
    isFlipped.value = false;
}
</script>

<!-- ✅ ĐÃ SỬA: Chuyển từ @apply sang CSS thuần túy -->
<style>
.filter-btn {
  padding: 0.25rem 0.75rem; /* px-3 py-1 */
  font-size: 0.75rem; /* text-xs */
  line-height: 1rem;
  border-radius: 9999px; /* rounded-full */
  background-color: rgb(55 65 81); /* bg-gray-700 */
  transition: background-color 150ms cubic-bezier(0.4, 0, 0.2, 1);
}
.filter-btn:hover {
  background-color: rgb(75 85 99); /* hover:bg-gray-600 */
}
.filter-active {
  background-color: rgb(14 165 233) !important; /* bg-sky-500 */
  color: rgb(255 255 255); /* text-white */
}
.control-btn {
  padding: 0.75rem 2rem; /* px-8 py-3 */
  border-radius: 0.5rem; /* rounded-lg */
  font-weight: 700; /* font-bold */
  color: rgb(255 255 255); /* text-white */
  transition-property: color, background-color, border-color, text-decoration-color, fill, stroke;
  transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1);
  transition-duration: 150ms; /* transition-colors */
}
.card-scene {
  perspective: 1000px;
  height: 15rem;
}
.card-inner {
  position: relative;
  width: 100%;
  height: 100%;
  transform-style: preserve-3d;
  transition: transform 0.6s;
}
.card-inner.is-flipped {
  transform: rotateY(180deg);
}
.card-face {
  position: absolute;
  width: 100%;
  height: 100%;
  -webkit-backface-visibility: hidden; /* backface-hidden */
  backface-visibility: hidden;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 2.25rem; /* text-4xl */
  line-height: 2.5rem;
  font-weight: 700; /* font-bold */
  background-color: rgb(55 65 81); /* bg-gray-700 */
  border-radius: 0.5rem; /* rounded-lg */
}
.card-back {
  transform: rotateY(180deg);
}
</style>
