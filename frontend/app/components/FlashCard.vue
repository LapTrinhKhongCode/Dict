<!-- src/components/FlashCard.vue -->
<template>
  <div v-if="cardSafe" class="w-full max-w-xl mx-auto">
    
    <!-- Nút quay lại -->
    <div class="mb-4">
      <button @click="emit('go-back')" class="flex items-center text-sm text-sky-400 hover:text-sky-300 transition-colors">
        <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4 mr-1.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
          <path stroke-linecap="round" stroke-linejoin="round" d="M15 19l-7-7 7-7" />
        </svg>
        Quay lại danh sách
      </button>
    </div>
    
    <div class="bg-gray-800 rounded-lg shadow-xl p-6 font-sans text-white">
      <div class="mb-6">
        <div class="card-scene cursor-pointer" @click="flip = !flip">
          <div :class="['card-inner', { 'is-flipped': flip }]">
            <div class="card-face card-front">
              <div class="text-6xl">{{ cardSafe.charBig }}</div>
              <!-- DÒNG HIỂN THỊ charSmall ĐÃ BỊ XÓA VÌ KHÔNG CÒN TỒN TẠI TRONG CardDto -->
              <div class="text-lg text-gray-400 mt-2">{{ cardSafe.pinyin }}</div>
            </div>
            <div class="card-face card-back">
              <div class="font-semibold text-3xl">{{ cardSafe.meaning }}</div>
              <div class="mt-4 text-sm text-gray-400">Nhấn để lật lại</div>
            </div>
          </div>
        </div>
      </div>
    <div class="flex justify-center">XEM CHI TIẾT</div>
      <div class="border-t border-gray-700 my-4"></div>

      <div v-if="flip" class="animate-fade-in">
        <div class="flex justify-between text-sm text-gray-400 mb-3 text-center">
          <div class="flex-1" v-for="btn in BUTTONS" :key="btn.key">{{ btn.labelTime }}</div>
        </div>
        <div class="flex gap-3 mb-4">
          <button class="answer-btn bg-red-600 hover:bg-red-700" @click="emitAnswer('again')">Again</button>
          <button class="answer-btn bg-orange-500 hover:bg-orange-600" @click="emitAnswer('hard')">Hard</button>
          <button class="answer-btn bg-green-500 hover:bg-green-600" @click="emitAnswer('good')">Good</button>
          <button class="answer-btn bg-sky-500 hover:bg-sky-600" @click="emitAnswer('easy')">Easy</button>
        </div>
      </div>

      <div class="flex items-center justify-between text-sm text-gray-400 h-8">
         <div v-if="!flip">
          Còn lại: <span class="text-sky-400 font-semibold">{{ remaining }} thẻ</span>
         </div>
         <div v-else></div> <!-- Placeholder to keep height consistent -->
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
// THAY ĐỔI: Import CardDto từ file types mới
import type { CardDto } from '~/types';

// THAY ĐỔI: defineProps để chấp nhận CardDto
const props = defineProps<{ card: CardDto | null; remaining?: number }>();
const emit = defineEmits(['answer', 'go-back']);

const flip = ref(false);

watch(() => props.card, () => { flip.value = false; });

function emitAnswer(difficulty: 'again' | 'hard' | 'good' | 'easy') {
  emit('answer', difficulty);
}

const cardSafe = computed<CardDto | null>(() => props.card ?? null);

const BUTTONS = [
  { key: 'again', labelTime: '< 1 phút' },
  { key: 'hard', labelTime: '< 10 phút' },
  { key: 'good', labelTime: '2 ngày' },
  { key: 'easy', labelTime: '4 ngày' }
] as const;
</script>

<style scoped>
.card-scene {
  perspective: 1000px;
  height: 12rem;
}
.card-inner {
  position: relative;
  width: 100%;
  height: 100%;
  transform-style: preserve-3d;
  transition: transform 0.6s cubic-bezier(0.2, 0.8, 0.2, 1);
}
.card-inner.is-flipped {
  transform: rotateY(180deg);
}
.card-face {
  position: absolute;
  width: 100%;
  height: 100%;
  backface-visibility: hidden;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  border-radius: 0.5rem;
  background-color: #374151; /* bg-gray-700 */
}
.card-back {
  transform: rotateY(180deg);
}
.answer-btn {
  flex: 1;
  padding-top: 0.5rem;
  padding-bottom: 0.5rem;
  border-radius: 0.5rem; /* rounded-lg */
  border: none;
  font-size: 0.875rem;
  font-weight: bold;
  color: white;
  transition: background-color 0.2s;
}
.animate-fade-in {
  animation: fadeIn 0.3s ease-in-out;
}
@keyframes fadeIn {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}
</style>