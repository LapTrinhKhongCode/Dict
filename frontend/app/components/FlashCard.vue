<template>
  <div
    class="min-h-screen bg-gray-50 text-gray-900 dark:bg-neutral-900 dark:text-white p-4 sm:p-8 transition-colors"
  >
    <div v-if="cardSafe" class="w-full max-w-xl mx-auto">
      <div class="mb-4">
        <button
          @click="emit('go-back')"
          class="flex items-center text-sm text-primary-600 hover:text-primary-500 dark:text-sky-400 dark:hover:text-sky-300 transition-colors"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            class="h-4 w-4 mr-1.5"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            stroke-width="2"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              d="M15 19l-7-7 7-7"
            />
          </svg>
          Quay lại danh sách
        </button>
      </div>

      <div
        class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-neutral-700 rounded-lg shadow-xl p-6 font-sans text-gray-900 dark:text-white"
      >
        <div class="mb-6">
          <div class="card-scene cursor-pointer" @click="flip = !flip">
            <div :class="['card-inner', { 'is-flipped': flip }]">
              <div class="card-face card-front">
                <div class="text-6xl">{{ cardSafe.charBig }}</div>
                <div class="text-lg text-gray-500 dark:text-gray-400 mt-2">
                  {{ cardSafe.pinyin }}
                </div>
              </div>
              <div class="card-face card-back">
                <div class="font-semibold text-3xl">{{ cardSafe.meaning }}</div>
                <div class="mt-4 text-sm text-gray-500 dark:text-gray-400">
                  Nhấn để lật lại
                </div>
              </div>
            </div>
          </div>
        </div>

        <div class="flex justify-center">
          <button
            @click="openDetailModal"
            class="text-sm font-medium text-primary-600 hover:text-primary-500 dark:text-sky-400 dark:hover:text-sky-300 transition-all focus:outline-none px-4 py-2 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700"
          >
            XEM CHI TIẾT
          </button>
        </div>
        <div class="border-t border-gray-200 dark:border-gray-700 my-4"></div>

        <div v-if="flip" class="animate-fade-in">
          <div
            class="flex justify-between text-sm text-gray-500 dark:text-gray-400 mb-3 text-center"
          >
            <div class="flex-1" v-for="btn in BUTTONS" :key="btn.key">
              {{ btn.labelTime }}
            </div>
          </div>
          <div class="flex gap-3 mb-4">
            <button
              class="answer-btn bg-red-600 hover:bg-red-700"
              @click="emitAnswer('again')"
            >
              Again
            </button>
            <button
              class="answer-btn bg-orange-500 hover:bg-orange-600"
              @click="emitAnswer('hard')"
            >
              Hard
            </button>
            <button
              class="answer-btn bg-green-500 hover:bg-green-600"
              @click="emitAnswer('good')"
            >
              Good
            </button>
            <button
              class="answer-btn bg-sky-500 hover:bg-sky-600"
              @click="emitAnswer('easy')"
            >
              Easy
            </button>
          </div>
        </div>

        <div
          class="flex items-center justify-between text-sm text-gray-600 dark:text-gray-400 h-8"
        >
          <div v-if="!flip">
            Còn lại:
            <span
              class="text-primary-600 dark:text-sky-400 font-semibold"
              >{{ remaining }} thẻ</span
            >
          </div>
          <div v-else></div>
        </div>
      </div>
    </div>

    <WordResultModal
      v-if="showWordModal"
      :search-word="modalSearchWord"
      @close="showWordModal = false"
      class="z-50"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from "vue";
// THAY ĐỔI: Import CardDto từ file types mới
import type { CardDto } from "~/types";
// START: THÊM IMPORT
import WordResultModal from "~/components/WordResultModal.vue";
// END: THÊM IMPORT

// THAY ĐỔI: defineProps để chấp nhận CardDto
const props = defineProps<{ card: CardDto | null; remaining?: number }>();
const emit = defineEmits(["answer", "go-back"]);

const flip = ref(false);

// START: THÊM STATE CHO MODAL
const showWordModal = ref(false);
const modalSearchWord = ref("");
// END: THÊM STATE

watch(
  () => props.card,
  () => {
    flip.value = false;
  }
);

function emitAnswer(difficulty: "again" | "hard" | "good" | "easy") {
  emit("answer", difficulty);
}

// START: THÊM HÀM MỞ MODAL
function openDetailModal() {
  if (cardSafe.value) {
    // Lấy `charBig` (mặt trước) làm từ khóa tìm kiếm
    modalSearchWord.value = cardSafe.value.charBig;
    showWordModal.value = true;
  }
}
// END: THÊM HÀM

const cardSafe = computed<CardDto | null>(() => props.card ?? null);

const BUTTONS = [
  { key: "again", labelTime: "< 1 phút" },
  { key: "hard", labelTime: "< 10 phút" },
  { key: "good", labelTime: "2 ngày" },
  { key: "easy", labelTime: "4 ngày" },
] as const;
</script>

<style scoped>
/* (Các style cấu trúc & animation giữ nguyên) */
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
  /* THAY ĐỔI:
    - Thêm style cho light mode (mặc định)
    - Bọc style dark mode (cũ) trong .dark
  */
  /* Light Mode */
  background-color: #f9fafb; /* bg-gray-50 */
  border: 1px solid #e5e7eb; /* border-gray-200 */
  color: #111827; /* text-gray-900 */
}
.card-back {
  transform: rotateY(180deg);
}

/* Dark Mode */
.dark .card-face {
  background-color: #374151; /* bg-gray-700 */
  border: none;
  color: white;
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
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>