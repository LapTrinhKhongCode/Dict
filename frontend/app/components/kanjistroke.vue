<template>
  <div classa="p-4">
    <ClientOnly>
      <div
        ref="writerTarget"
        style="width: 150px; height: 150px"
        class="flex justify-self-center content-center border-2 rounded-lg shadow-inner kanji-writer-grid"
      ></div>
    </ClientOnly>

    <div class="flex gap-2 mt-4 justify-self-center content-center">
      <button @click="animate" class="px-4 py-2 bg-blue-500 text-white rounded">
        Animate
      </button>
      <button @click="quiz" class="px-4 py-2 bg-green-500 text-white rounded">
        Quiz
      </button>
      <button @click="reset" class="px-4 py-2 bg-gray-500 text-white rounded">
        Reset
      </button>
    </div>
  </div>

  <div
    class="bg-white text-gray-900 border border-gray-200 dark:bg-slate-800 dark:text-white dark:border-transparent p-6 rounded-lg shadow-lg w-[400px]"
  >
    <h2 class="text-lg font-bold mb-4">
      Chi tiết chữ kanji
      <span v-if="selectedKanji" class="text-2xl">{{
        selectedKanji.char
      }}</span>
    </h2>

    <div v-if="selectedKanji" class="space-y-3">
      <div class="flex items-center space-x-2">
        <span
          class="bg-blue-100 text-blue-800 dark:bg-[#1e40af] dark:text-white px-3 py-1 rounded-full text-sm"
          >Hán tự</span
        >
        <span>{{ selectedKanji.char }} - {{ selectedKanji.mean }}</span>
      </div>

      <div v-if="selectedKanji.onyomi!" class="flex items-center space-x-2">
        <span
          class="bg-blue-100 text-blue-800 dark:bg-[#1e40af] dark:text-white px-3 py-1 rounded-full text-sm"
          >Kunyomi</span
        >
        <span class="text-blue-600 dark:text-blue-400">{{
          selectedKanji.kunyomi
        }}</span>
      </div>

      <div v-if="selectedKanji.onyomi!" class="flex items-center space-x-2">
        <span
          class="bg-blue-100 text-blue-800 dark:bg-[#1e40af] dark:text-white px-3 py-1 rounded-full text-sm"
          >Onyomi</span
        >
        <span class="text-red-600 dark:text-red-400">{{
          selectedKanji.onyomi
        }}</span>
      </div>

      <div
        v-if="parseInt(selectedKanji.strokes) > 0"
        class="flex items-center space-x-2"
      >
        <span
          class="bg-blue-100 text-blue-800 dark:bg-[#1e40af] dark:text-white px-3 py-1 rounded-full text-sm"
          >Số nét</span
        >
        <span>{{ selectedKanji.strokes }}</span>
      </div>

      <div class="flex items-center space-x-2">
        <span
          class="bg-blue-100 text-blue-800 dark:bg-[#1e40af] dark:text-white px-3 py-1 rounded-full text-sm"
          >JLPT</span
        >
        <span>{{ selectedKanji.jlpt }}</span>
      </div>

      <div class="flex items-center space-x-2">
        <span
          class="bg-blue-100 text-blue-800 dark:bg-[#1e40af] dark:text-white px-3 py-1 rounded-full text-sm"
          >Độ phổ biến</span
        >
        <span>{{ selectedKanji.freq }}</span>
      </div>

      <div class="flex items-start flex-col">
        <span
          class="bg-blue-100 text-blue-800 dark:bg-[#1e40af] dark:text-white px-3 py-1 rounded-full text-sm flex-shrink-0"
        >
          Nghĩa
        </span>

        <div class="flex flex-col space-y-2 mt-5">
          <span
            v-for="(meaningBlock, index) in selectedKanji.meanlong.split('\n')"
            :key="index"
            class="block border-2 bg-gray-50 border-blue-200 dark:bg-slate-700 dark:border-blue-700 p-2 rounded-md"
          >
            {{ meaningBlock }}
          </span>
        </div>
      </div>
    </div>

    <div v-else class="space-y-3">
      Đang tải dữ liệu cho: {{ props.kanji }}...
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, computed } from "vue";
import HanziWriter from "hanzi-writer";

// import Papa from "papaparse" // <-- XÓA

// --- MỚI: Import "Kho" và Interface ---
import {
  kanjiList,
  loadKanjiData,
  type KanjiItem,
} from "../src/services/kanjiStore";

// --- Props (Giữ nguyên) ---
const props = defineProps({
  kanji: {
    type: String,
    required: true,
  },
});

// --- XÓA TOÀN BỘ LOGIC LOAD DỮ LIỆU ---
// let kanjiLoadPromise: Promise<KanjiItem[]> | null = null
// async function loadKanjiOnce(): Promise<KanjiItem[]> { ... }
// interface KanjiRow { ... }
// (Xóa tất cả các hàm helper: cleanField, normalizeAllRebs, ...)
// const kanjiList = ref<KanjiItem[]>([]) // (Đã import)
// -----------------------------------------

// --- State (Giữ nguyên) ---
const writerInstance = ref<HanziWriter | null>(null);
const writerTarget = ref<HTMLDivElement | null>(null);

// --- Computed (Dùng kanjiList từ "Kho") ---
const selectedKanji = computed(() => {
  return kanjiList.value.find((k) => k.char === props.kanji) || null;
});

// --- onMounted (Đã sửa) ---
// Chỉ cần gọi loadKanjiData.
// Nó sẽ trả về ngay lập tức vì trang Grid đã load rồi.
onMounted(async () => {
  // Sửa lỗi "đơ 1s":
  // Hàm này sẽ trả về ngay lập tức vì Grid đã "hâm nóng" cache.
  await loadKanjiData();
});

// --- SỬA LỖI "KHÔNG VẼ" BẰNG WATCH ---
// Watch này sẽ theo dõi CẢ kanji VÀ target DOM element
watch(
  [() => props.kanji, writerTarget],
  ([newKanji, targetEl]) => {
    // Chỉ chạy khi CẢ hai đều đã sẵn sàng
    if (!newKanji || !targetEl) {
      return;
    }

    // Nếu chúng ta đã có cả kanji và div...
    if (writerInstance.value) {
      // 1. Writer đã tồn tại (do click node graph): Chỉ cần set chữ
      console.log("Kanjistroke: Đang set character thành", newKanji);
      writerInstance.value.setCharacter(newKanji);
    } else {
      // 2. Writer chưa tồn tại (do load từ Grid): Tạo mới
      // (An toàn vì targetEl đã được đảm bảo tồn tại)
      console.log("Kanjistroke: Đang tạo writer cho", newKanji);
      writerInstance.value = HanziWriter.create(targetEl, newKanji, {
        width: 150,
        height: 150,
        padding: 20,
        // THAY ĐỔI: Style nét vẽ cho phù hợp
        strokeColor: "#333", // Nét vẽ màu đen (hoạt động trên cả 2 nền)
        highlightColor: "#f00", // Màu highlight (đỏ)
        outlineColor: "#ddd", // Màu outline (xám nhạt)
        // Bỏ 'showOutline' và 'showCharacter' để dùng style nét vẽ
        strokeAnimationSpeed: 1.2,
      });
    }
  },
  {
    immediate: true, // Chạy 1 lần ngay khi mount để thử tạo
  }
);
// ------------------------------------------

// --- Các hàm điều khiển (Giữ nguyên) ---
const animate = () => writerInstance.value?.animateCharacter();
const quiz = () => {
  writerInstance.value?.quiz({
    onMistake: (strokeData) => console.log("❌ Mistake", strokeData),
    onCorrectStroke: (strokeData) => console.log("✔️ Correct", strokeData),
    onComplete: () => console.log("🎉 Done!"),
  });
};
const reset = () => {
  writerInstance.value?.cancelQuiz();
  writerInstance.value?.showCharacter();
};
</script>

<style scoped>
/* (Style nút bấm giữ nguyên) */
button {
  transition: background 0.2s;
}
button:hover {
  opacity: 0.8;
}

/* THAY ĐỔI:
  - Thêm class .kanji-writer-grid để xử lý nền và lưới 
  cho cả 2 chế độ (thay thế cho :style inline)
*/
.kanji-writer-grid {
  /* Light Mode (default) */
  background-color: #fffbeb; /* bg-amber-50 */
  background-image: linear-gradient(rgba(0, 0, 0, 0.08) 1px, transparent 1px),
    linear-gradient(90deg, rgba(0, 0, 0, 0.08) 1px, transparent 1px);
  background-size: 15px 15px;
  border-color: #fde68a; /* border-amber-200 */
}

.dark .kanji-writer-grid {
  /* Dark Mode */
  background-color: #1e293b; /* bg-slate-800 */
  background-image: linear-gradient(
      rgba(255, 255, 255, 0.1) 1px,
      transparent 1px
    ),
    linear-gradient(90deg, rgba(255, 255, 255, 0.1) 1px, transparent 1px);
  border-color: #475569; /* border-slate-600 */
}
</style>