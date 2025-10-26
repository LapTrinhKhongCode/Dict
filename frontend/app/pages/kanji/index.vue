<template>
  <div class="min-h-screen bg-[#0f172a] text-white flex">
    <!-- Sidebar -->
    <aside class="w-64 bg-[#1e293b] p-5 space-y-8">
      <div>
        <h2 class="text-lg font-bold mb-3">Chọn cấp độ</h2>
        <div class="space-y-3">
          <label
            v-for="lv in levels"
            :key="lv"
            class="flex items-center space-x-2 cursor-pointer"
          >
            <input
              type="radio"
              :value="lv"
              v-model="selectedLevel"
              class="accent-blue-500"
            />
            <span>{{ lv }}</span>
          </label>
        </div>
      </div>
    </aside>

    <!-- Main Content -->
    <main class="flex-1 p-6">
      <div class="flex justify-between items-center mb-6">
        <h1 class="text-xl font-bold">
          Hán tự {{ selectedLevel }}
        </h1>
        <div class="space-x-6 text-sm">
          <label class="inline-flex items-center space-x-2">
            <input type="checkbox" checked class="accent-blue-500 w-4 h-4" v-model="isSelectedMeaning"/>
            <span class="text-lg">Nghĩa</span>
          </label>
        </div>
      </div>

      <!-- Kanji Grid -->
      <div
        class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6 gap-6"
      >
        <div @click="goToKanji(k.char)"
          v-for="k in paginatedList"
          :key="k.id"
          class="flex flex-col items-center h-24 bg-[#1e293b] p-4 rounded-lg hover:bg-[#334155] transition cursor-pointer"
        >
          <span class="text-3xl text-blue-400 font-bold">{{ k.char }}</span>
          <span v-if="isSelectedMeaning" class="text-sm mt-2 text-gray-300">{{ k.mean }}</span>
        </div>
      </div>

      <!-- Pagination -->
      <div class="flex justify-center mt-8 space-x-3" v-if="totalPages > 1">
        <!-- Prev -->
        <button
          @click="goToPage(currentPage - 1)"
          :disabled="currentPage === 1"
          class="w-9 h-9 flex items-center justify-center rounded-full bg-[#1e293b] hover:bg-[#334155] disabled:opacity-50"
        >
          «
        </button>

        <!-- Dynamic page numbers -->
        <button
          v-for="page in pageNumbers"
          :key="page"
          @click="goToPage(page)"
          class="w-9 h-9 flex items-center justify-center rounded-full"
          :class="page === currentPage
            ? 'bg-blue-500'
            : 'bg-[#1e293b] hover:bg-[#334155]'"
        >
          {{ page }}
        </button>

        <!-- Next -->
        <button
          @click="goToPage(currentPage + 1)"
          :disabled="currentPage === totalPages"
          class="w-9 h-9 flex items-center justify-center rounded-full bg-[#1e293b] hover:bg-[#334155] disabled:opacity-50"
        >
          »
        </button>
      </div>
    </main>
  </div>
</template>

<script setup>

import { ref, computed, onMounted, watch } from "vue";

// import Papa from "papaparse"; // <-- Không cần nữa

import { useRouter } from "vue-router"; // <-- Thêm import này

import { kanjiList, loadKanjiData } from "../../src/services/kanjiStore"; // <-- IMPORT KHO



// const kanjiList = ref([]); // <-- Dùng kanjiList từ kho

const itemsPerPage = ref(30);

const currentPage = ref(1);



const wordTypes = ["Từ vựng", "Ngữ pháp", "Hán tự"];

const levels = ["N5", "N4", "N3", "N2", "N1"];



const selectedType = ref("Hán tự");

const selectedLevel = ref("N5");

const router = useRouter();

const isSelectedMeaning = ref(true)



onMounted(async () => {

  // --- SỬA Ở ĐÂY ---

  // Chỉ cần gọi hàm load, nó sẽ tự biết chỉ load 1 lần

  await loadKanjiData();

  // --- XÓA TOÀN BỘ LOGIC FETCH VÀ PAPA.PARSE CŨ ---

});



// --- FILTERED DATA THEO LEVEL ---

const filteredList = computed(() => {

  // Dùng kanjiList từ kho

  return kanjiList.value.filter((k) => k.jlpt === selectedLevel.value);

});



// --- PAGINATION ---

// ... (Toàn bộ computed: totalPages, paginatedList, pageNumbers giữ nguyên) ...

const totalPages = computed(() =>

  Math.ceil(filteredList.value.length / itemsPerPage.value)

);



const paginatedList = computed(() => {

  const start = (currentPage.value - 1) * itemsPerPage.value;

  return filteredList.value.slice(start, start + itemsPerPage.value);

});



const pageNumbers = computed(() => {

  const pages = [];

  const start = Math.max(1, currentPage.value - 1);

  const end = Math.min(totalPages.value, start + 2);



  for (let i = start; i <= end; i++) {

    pages.push(i);

  }

  return pages;

});





function goToPage(page) {

  if (page >= 1 && page <= totalPages.value) {

    currentPage.value = page;

  }

}



// Hàm này đã ĐÚNG, không cần sửa

const goToKanji = (kanji) => {

  router.push(`/kanji/${kanji}`);

};



watch(selectedLevel, () => {

  currentPage.value = 1;

});

</script>