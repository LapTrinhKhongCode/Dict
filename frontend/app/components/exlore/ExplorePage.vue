<template>
  <div
    class="min-h-screen bg-gray-50 text-gray-900 dark:bg-gray-900 dark:text-white p-4 sm:p-8 transition-colors"
  >
    <div class="max-w-7xl mx-auto">
      <div class="mb-6 flex items-center justify-between">
        <button
          @click="emit('go-back')"
          class="flex items-center text-sm text-emerald-600 hover:text-emerald-500 dark:text-emerald-400 dark:hover:text-emerald-300 transition-colors"
        >
          &larr; Quay lại trang chủ
        </button>
        <h1 class="text-2xl font-bold text-emerald-600 dark:text-emerald-400">
          Khám phá bộ thẻ
        </h1>
        <div></div>
      </div>

      <div class="mb-8 max-w-lg mx-auto">
        <div class="relative">
          <input
            type="text"
            v-model="searchTerm"
            @keyup.enter="performSearch"
            placeholder="Tìm kiếm bộ thẻ công khai theo tên..."
            class="w-full bg-white text-gray-900 border border-gray-300 rounded-full pl-12 pr-4 py-3 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500 dark:bg-gray-800 dark:text-white dark:border-gray-600 dark:focus:ring-emerald-500 text-lg"
          />
          <svg
            class="absolute left-4 top-1/2 transform -translate-y-1/2 h-6 w-6 text-gray-400"
            xmlns="http://www.w3.org/2000/svg"
            viewBox="0 0 20 20"
            fill="currentColor"
          >
            <path
              fill-rule="evenodd"
              d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z"
              clip-rule="evenodd"
            />
          </svg>
        </div>
      </div>

      <div
        v-if="isSearching"
        class="text-center text-gray-500 dark:text-gray-400 py-20"
      >
        Đang tìm kiếm...
      </div>
      <div
        v-else-if="error"
        class="text-center text-red-700 bg-red-100 dark:text-red-400 dark:bg-red-900/50 p-4 rounded-lg"
      >
        {{ error }}
      </div>
      <div
        v-else-if="filteredDecks.length === 0 && !searchTerm"
        class="text-center text-gray-600 dark:text-gray-500 py-20"
      >
        Không có bộ thẻ công khai nào.
      </div>
      <div
        v-else-if="filteredDecks.length === 0 && searchTerm"
        class="text-center text-gray-600 dark:text-gray-500 py-20"
      >
        Không tìm thấy bộ thẻ nào khớp với "{{ searchTerm }}".
      </div>
      <div
        v-else
        class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6"
      >
        <DeckCard
          v-for="deck in paginatedDecks"
          :key="deck.id"
          :deck="deck"
          :current-user-id="currentUserId"
          :current-user-name="currentUserName"
          variant="explore"
          @select-set="emit('select-set', deck.id)"
          @save-deck="saveDeck"
          @show-login-notice="emit('show-login-notice')"
        />
      </div>

      <div
        v-if="totalPages > 1 && !isSearching"
        class="mt-8 flex justify-center items-center space-x-2 text-gray-600 dark:text-gray-400"
      >
        <button
          @click="prevPage"
          :disabled="currentPage === 1"
          class="pagination-btn disabled:opacity-50 disabled:cursor-not-allowed"
        >
          &lt; Trước
        </button>
        <span v-for="page in pageNumbers" :key="page">
          <button
            v-if="typeof page === 'number'"
            @click="goToPage(page)"
            :class="[
              'pagination-btn',
              { 'bg-emerald-600 text-white': currentPage === page },
            ]"
          >
            {{ page }}
          </button>
          <span v-else class="px-2">...</span>
        </span>
        <button
          @click="nextPage"
          :disabled="currentPage === totalPages"
          class="pagination-btn disabled:opacity-50 disabled:cursor-not-allowed"
        >
          Sau &gt;
        </button>
      </div>
    </div>

    <div
      v-if="showSaveSuccess"
      class="fixed bottom-5 right-5 bg-green-600 text-white py-2 px-4 rounded-lg shadow-lg animate-bounce z-50"
    >
      Đã lưu vào Sổ tay!
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import type { DeckSummaryDto } from '~/types';
import DeckCard from '../DeckCard.vue';
import { useJwt } from '~/composables/useJwt';

const { jwt } = useJwt();

const props = defineProps<{
  currentUserId: number;
  currentUserName: string;
  initialDecks: DeckSummaryDto[];
}>();

const emit = defineEmits(['select-set', 'go-back', 'show-login-notice', 'save-deck-success']);

const config = useRuntimeConfig()
const BASE_URL = config.public.apiBaseUrl || 'https://localhost:7084';

const allExploreDecks = ref<DeckSummaryDto[]>(props.initialDecks || []);
const isSearching = ref(false);
const error = ref<string | null>(null);
const searchTerm = ref('');
const showSaveSuccess = ref(false);

const currentPage = ref(1);
const itemsPerPage = ref(12); // 3 rows * 4 columns

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const errorText = await response.text();
    let errorMessage = errorText;
    try {
      const errorJson = JSON.parse(errorText);
      if (errorJson?.message) errorMessage = errorJson.message;
      else if (errorJson?.title) errorMessage = errorJson.title;
    } catch (e) {}
    throw new Error(errorMessage || `Yêu cầu thất bại: ${response.status}`);
  }
  if (response.status === 204) return {} as T;
  try {
    const data = await response.json();
    if (data.isSuccess === false) {
      throw new Error(data.message || 'Lỗi từ API');
    }
    return (data.result === undefined ? data : data.result) as T;
  } catch (e) {
    console.error("JSON Parse Error:", e);
    throw new Error("Invalid response from server.");
  }
}

watch(() => props.initialDecks, (newDecks) => {
  allExploreDecks.value = newDecks || [];
  currentPage.value = 1;
}, { deep: true });

const filteredDecks = computed(() => {
  const decks = allExploreDecks.value || [];
  return decks.filter(deck => deck.authorName !== props.currentUserName);
});

const totalPages = computed(() => {
  return Math.ceil(filteredDecks.value.length / itemsPerPage.value);
});

const paginatedDecks = computed(() => {
  const start = (currentPage.value - 1) * itemsPerPage.value;
  const end = start + itemsPerPage.value;
  return filteredDecks.value.slice(start, end);
});

function nextPage() { if (currentPage.value < totalPages.value) { currentPage.value++; } }
function prevPage() { if (currentPage.value > 1) { currentPage.value--; } }
function goToPage(page: number) { if (page >= 1 && page <= totalPages.value) { currentPage.value = page; } }

const pageNumbers = computed(() => {
  const total = totalPages.value; const current = currentPage.value; const delta = 1;
  const range: number[] = []; const rangeWithDots: (number | string)[] = []; let l: number | undefined;
  range.push(1);
  for (let i = current - delta; i <= current + delta; i++) { if (i > 1 && i < total) { range.push(i); } }
  if (total > 1) range.push(total);
  const uniqueRange = [...new Set(range)].sort((a, b) => a - b);
  for (const i of uniqueRange) {
    if (l !== undefined) {
      if (i - l === 2) {
        rangeWithDots.push(l + 1);
      } else if (i - l !== 1) {
        rangeWithDots.push('...');
      }
    }
    rangeWithDots.push(i);
    l = i;
  }
  return rangeWithDots;
});


async function performSearch() {
  error.value = null;
  isSearching.value = true;
  currentPage.value = 1; // Reset page on new search
  try {
    let url = `${BASE_URL}/api/decks/public`;
    if (searchTerm.value.trim()) {
      url = `${BASE_URL}/api/decks/search?name=${encodeURIComponent(searchTerm.value.trim())}`;
    }
    const response = await fetch(url, { cache: 'no-store' });
    allExploreDecks.value = await handleResponse<DeckSummaryDto[]>(response);
  } catch (err: any) {
    error.value = `Lỗi tìm kiếm: ${err.message}`;
    allExploreDecks.value = [];
  } finally {
    isSearching.value = false;
  }
}

async function saveDeck(deckId: number) {
  try {
    const response = await fetch(`${BASE_URL}/api/decks/${deckId}/save`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${jwt.value}` },
    });
    await handleResponse<DeckSummaryDto>(response);
    showSaveSuccess.value = true;
    emit('save-deck-success');
    setTimeout(() => { showSaveSuccess.value = false; }, 2500);
  } catch (err: any) {
    alert(`Lưu thất bại: ${err.message}`);
    console.error("saveDeck Error (ExplorePage):", err);
  }
}

// Watch searchTerm to reset page? Optional, might be annoying if typing pauses
// watch(searchTerm, () => { currentPage.value = 1; });
</script>

<style scoped>
/* THAY ĐỔI:
  - Tách style mặc định (light mode) ra.
  - Bọc các style cũ (dark mode) trong class .dark
*/

/* --- LIGHT MODE (MẶC ĐỊNH) --- */
.pagination-btn {
  padding: 0.5rem 0.75rem;
  border-radius: 0.375rem;
  background-color: #ffffff; /* bg-white */
  border: 1px solid #e5e7eb; /* border-gray-200 */
  color: #374151; /* text-gray-700 */
  transition: background-color 0.2s;
  min-width: 2.5rem;
  text-align: center;
}
.pagination-btn:hover:not(:disabled) {
  background-color: #f3f4f6; /* bg-gray-100 */
}

/* --- DARK MODE --- */
.dark .pagination-btn {
  background-color: #374151; /* bg-gray-700 */
  border-color: transparent;
  color: #e5e7eb; /* text-gray-200 */
}
.dark .pagination-btn:hover:not(:disabled) {
  background-color: #4b5563; /* bg-gray-600 */
}

/* --- ACTIVE (Giữ nguyên, chỉ thêm border) --- */
.pagination-btn.bg-emerald-600 {
  /* Active page style for Explore */
  background-color: #059669; /* Tailwind emerald-600 */
  color: white;
  border-color: transparent;
}
</style>