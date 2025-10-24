<!-- src/components/QuizPage.vue -->
<template>
  <div class="min-h-screen flex flex-col items-center justify-center p-4 bg-gray-900 text-white">
    <div class="w-full max-w-2xl">
      <button @click="emit('go-to-list')" class="flex items-center text-sm text-sky-400 hover:text-sky-300 mb-4">
        &larr; Quay lại danh sách
      </button>

      <!-- ✅ SỬA: Màn hình kết quả theo lượt -->
      <div v-if="isBatchFinished" class="text-center bg-gray-800 p-8 rounded-lg">
        <h2 class="text-3xl font-bold text-sky-400">Hoàn thành lượt!</h2>
        <p class="text-2xl text-gray-300 my-4">
          Điểm của bạn: <span class="font-bold text-green-400">{{ score }} / {{ currentBatch.length }}</span>
        </p>

        <!-- Nút Tiếp tục (nếu còn thẻ) -->
        <button 
          v-if="remainingCardCount > 0"
          @click="startNextBatch" 
          class="px-6 py-3 bg-sky-500 hover:bg-sky-600 rounded-lg font-bold w-full mb-4 transition-colors"
        >
          Tiếp tục ({{ remainingCardCount }} thẻ còn lại)
        </button>
        
        <!-- Thông báo hoàn thành (nếu hết thẻ) -->
        <div v-else class="my-6">
           <p class="text-2xl text-green-400">🎉 Chúc mừng! Bạn đã hoàn thành tất cả các thẻ!</p>
        </div>

        <!-- Nút Làm lại -->
        <button @click="restartQuiz" class="px-6 py-3 bg-indigo-500 hover:bg-indigo-600 rounded-lg font-bold w-full mb-4 transition-colors">
          Làm lại từ đầu ({{ cards.length }} thẻ)
        </button>
        
        <!-- Nút Quay lại -->
        <button @click="emit('go-to-list')" class="px-6 py-3 bg-gray-600 hover:bg-gray-500 rounded-lg font-bold w-full transition-colors">
          Quay lại danh sách
        </button>
      </div>

      <!-- Màn hình trắc nghiệm -->
      <div v-else-if="currentCard" class="bg-gray-800 p-6 rounded-lg">
        <div class="flex justify-between items-center mb-6">
            <!-- ✅ SỬA: Hiển thị tiến độ của lượt (n / 5) -->
            <h3 class="text-gray-400 font-bold">Câu hỏi {{ currentIndex + 1 }} / {{ currentBatch.length }}</h3>
            <p class="text-gray-400">Điểm: {{ score }}</p>
        </div>
        
        <div class="text-center my-10 min-h-[7rem] flex items-center justify-center">
            <p class="text-6xl font-bold">
              {{ currentCard.charBig }}
            </p>
        </div>
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <button 
                v-for="(option, index) in currentOptions" :key="index"
                @click="selectAnswer(option)"
                :disabled="!!selectedAnswer"
                :class="getButtonClass(option)"
                class="answer-option"
            >
                {{ option }}
            </button>
        </div>
        <div class="text-center mt-6 h-10">
            <button v-if="selectedAnswer" @click="nextQuestion" class="px-8 py-2 bg-indigo-500 rounded-lg font-bold">Tiếp theo</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import type { CardDto } from '~/types';

const props = defineProps<{ cards: CardDto[] }>();
const emit = defineEmits(['go-to-list']);

// ✅ THÊM: Quản lý hàng đợi và lượt học
const BATCH_SIZE = 5;
const sessionQueue = ref<CardDto[]>([]); // Hàng đợi cho toàn bộ buổi học (50 thẻ)
const currentBatch = ref<CardDto[]>([]); // Lượt học hiện tại (5 thẻ)

const currentIndex = ref(0); // Index trong lượt (0-4)
const score = ref(0); // Điểm của lượt hiện tại
const currentOptions = ref<string[]>([]);
const selectedAnswer = ref<string | null>(null);
const isBatchFinished = ref(false); // ✅ SỬA: Đổi tên từ isFinished

// ✅ SỬA: Lấy thẻ hiện tại từ lượt
const currentCard = computed(() => currentBatch.value[currentIndex.value] ?? null);
// ✅ THÊM: Tính số thẻ còn lại trong hàng đợi
const remainingCardCount = computed(() => sessionQueue.value.length);

function shuffleArray<T>(array: T[]): T[] {
  const newArray = [...array];
  for (let i = newArray.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [newArray[i]!, newArray[j]!] = [newArray[j]!, newArray[i]!]; 
  }
  return newArray;
}

function generateOptions() {
  if (!currentCard.value) return;

  const correctAnswer = currentCard.value.meaning;
  // Lấy đáp án sai từ TOÀN BỘ 50 thẻ để tăng độ khó
  const wrongAnswers = props.cards 
    .filter(card => card.id !== currentCard.value!.id)
    .map(card => card.meaning);

  const shuffledWrong = shuffleArray(wrongAnswers);
  let options = [correctAnswer];
  // Luôn tạo 4 lựa chọn (nếu có thể)
  const optionsCount = props.cards.length >= 4 ? 4 : 2; 
  const wrongOptionsCount = optionsCount - 1;
  options.push(...shuffledWrong.slice(0, wrongOptionsCount));
  
  currentOptions.value = shuffleArray(options);
  selectedAnswer.value = null;
}

function selectAnswer(option: string) {
    selectedAnswer.value = option;
    if (option === currentCard.value?.meaning) {
        score.value++;
    }
}

// ✅ SỬA: Logic khi chuyển câu hỏi
function nextQuestion() {
    // Nếu chưa hết lượt (chưa đến câu 5)
    if (currentIndex.value < currentBatch.value.length - 1) {
        currentIndex.value++;
    } else {
        // Hết lượt, hiển thị màn hình kết quả
        isBatchFinished.value = true;
    }
}

// ✅ THÊM: Hàm mới để bắt đầu lượt tiếp theo
function startNextBatch() {
  if (sessionQueue.value.length === 0) {
    // Thực tế không nên xảy ra vì nút đã bị ẩn, nhưng để an toàn
    isBatchFinished.value = true; 
    return;
  }
  
  // Lấy 5 thẻ (hoặc ít hơn) từ hàng đợi
  const batch = sessionQueue.value.splice(0, BATCH_SIZE);
  currentBatch.value = batch;

  // Reset lại lượt
  currentIndex.value = 0;
  score.value = 0;
  isBatchFinished.value = false;
  generateOptions(); // Tạo đáp án cho câu đầu tiên của lượt mới
}

// ✅ SỬA: Hàm này giờ sẽ bắt đầu TOÀN BỘ buổi học
function restartQuiz() {
    // Xáo trộn toàn bộ 50 thẻ vào hàng đợi
    sessionQueue.value = shuffleArray([...props.cards]);
    // Bắt đầu lượt đầu tiên
    startNextBatch();
}

function getButtonClass(option: string) {
    if (!selectedAnswer.value) return 'bg-gray-700 hover:bg-gray-600';
    if (option === currentCard.value?.meaning) return 'bg-green-600';
    if (option === selectedAnswer.value) return 'bg-red-600';
    return 'bg-gray-700 opacity-50';
}

// Bắt đầu buổi học khi component được tải
watch(() => props.cards, (newCards) => {
    if (newCards && newCards.length > 0) {
        restartQuiz();
    }
}, { immediate: true });

// Tạo đáp án mới khi chuyển câu hỏi
watch(currentIndex, (newIndex, oldIndex) => {
  // Chỉ chạy khi index thay đổi VÀ không phải là câu đầu tiên (vì đã chạy trong startNextBatch)
  if (newIndex > 0) {
    generateOptions();
  }
});

</script>

<style>
.answer-option {
  padding: 1rem;
  border-radius: 0.5rem;
  text-align: left;
  transition-property: color, background-color, border-color, text-decoration-color, fill, stroke;
  transition-timing-function: cubic-bezier(0.4, 0, 0.2, 1);
  transition-duration: 200ms;
}
.answer-option:disabled {
  cursor: not-allowed;
}
</style>

