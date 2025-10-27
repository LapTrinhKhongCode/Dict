<template>
  <div class="min-h-screen text-white p-4 sm:p-8">
    <div class="max-w-7xl mx-auto">
      <div class="mb-6 flex items-center justify-between">
        <button @click="emit('go-back')" class="flex items-center text-sm text-sky-400 hover:text-sky-300 transition-colors">
          &larr; Quay lại trang chủ
        </button>
         <h1 class="text-2xl font-bold text-sky-400">Sổ tay của tôi</h1>
         <div></div>
      </div>
      <div class="mb-6 flex flex-col sm:flex-row justify-between items-center gap-4">
          <div class="relative w-full sm:max-w-xs">
             <input
                type="text"
                v-model="searchTerm"
                placeholder="Tìm kiếm trong sổ tay..."
                class="w-full bg-gray-800 border border-gray-600 rounded-lg pl-10 pr-4 py-2 text-white focus:outline-none focus:ring-2 focus:ring-sky-500"
              />
              <svg class="absolute left-3 top-1/2 transform -translate-y-1/2 h-5 w-5 text-gray-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                 <path fill-rule="evenodd" d="M8 4a4 4 0 100 8 4 4 0 000-8zM2 8a6 6 0 1110.89 3.476l4.817 4.817a1 1 0 01-1.414 1.414l-4.816-4.816A6 6 0 012 8z" clip-rule="evenodd" />
              </svg>
          </div>
         <button @click="emit('go-to-create-deck')" class="bg-sky-600 hover:bg-sky-700 text-white font-bold py-2 px-5 rounded-lg flex items-center w-full sm:w-auto shrink-0">
             <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 mr-2" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M12 6v6m0 0v6m0-6h6m-6 0H6" /></svg>
            Tạo sổ tay mới
          </button>
      </div>


      <div v-if="filteredDecks.length === 0 && !searchTerm" class="text-center text-gray-500 py-20">Bạn chưa có sổ tay nào.</div>
      <div v-else-if="filteredDecks.length === 0 && searchTerm" class="text-center text-gray-500 py-20">Không tìm thấy sổ tay nào khớp với "{{ searchTerm }}".</div>

      <div v-else class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
        <DeckCard
          v-for="deck in paginatedDecks" 
          :key="deck.id"
          :deck="deck"
          :current-user-id="currentUserId"
          :current-user-name="currentUserName"
          variant="my"
          @select-set="emit('select-set', deck.id)"
        />
      </div>

      <div v-if="totalPages > 1" class="mt-8 flex justify-center items-center space-x-2 text-gray-400">
          <button @click="prevPage" :disabled="currentPage === 1" class="pagination-btn disabled:opacity-50 disabled:cursor-not-allowed">
              &lt; Trước
          </button>
          <span v-for="page in pageNumbers" :key="page">
              <button
                  v-if="typeof page === 'number'"
                  @click="goToPage(page)"
                  :class="['pagination-btn', { 'bg-sky-600 text-white': currentPage === page }]"
              >
                  {{ page }}
              </button>
              <span v-else class="px-2">...</span>
          </span>
          <button @click="nextPage" :disabled="currentPage === totalPages" class="pagination-btn disabled:opacity-50 disabled:cursor-not-allowed">
              Sau &gt;
          </button>
      </div>

    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import type { DeckSummaryDto } from '~/types';
import DeckCard from '../DeckCard.vue';

const props = defineProps<{
  currentUserId: number;
  currentUserName: string;
  initialDecks: DeckSummaryDto[];
}>();

const emit = defineEmits(['select-set', 'go-back', 'go-to-create-deck']);

const searchTerm = ref('');
const allMyDecks = ref<DeckSummaryDto[]>(props.initialDecks || []);

// ✅ THÊM: Pagination State
const currentPage = ref(1);
const itemsPerPage = ref(12); // 3 rows * 4 columns

watch(() => props.initialDecks, (newDecks) => {
    allMyDecks.value = newDecks || [];
    currentPage.value = 1; // Reset page when initial data changes
}, { deep: true });

// Filtered decks based on search term
const filteredDecks = computed(() => {
  const decks = allMyDecks.value;
  if (!searchTerm.value) { return decks; }
  const lowerSearch = searchTerm.value.toLowerCase();
  return decks.filter(deck =>
    deck.name.toLowerCase().includes(lowerSearch) ||
    deck.description?.toLowerCase().includes(lowerSearch)
  );
});

// ✅ THÊM: Pagination Computed Properties
const totalPages = computed(() => {
    return Math.ceil(filteredDecks.value.length / itemsPerPage.value);
});

const paginatedDecks = computed(() => {
    const start = (currentPage.value - 1) * itemsPerPage.value;
    const end = start + itemsPerPage.value;
    return filteredDecks.value.slice(start, end);
});

// ✅ THÊM: Pagination Navigation Methods
function nextPage() {
    if (currentPage.value < totalPages.value) {
        currentPage.value++;
    }
}
function prevPage() {
    if (currentPage.value > 1) {
        currentPage.value--;
    }
}
function goToPage(page: number) {
    if (page >= 1 && page <= totalPages.value) {
        currentPage.value = page;
    }
}

// ✅ THÊM: Generate page numbers for controls (with ellipsis)
const pageNumbers = computed(() => {
    const total = totalPages.value;
    const current = currentPage.value;
    const delta = 1; // How many pages around current to show
    const range = [];
    const rangeWithDots: (number | string)[] = [];
    let l: number | undefined = undefined;

    range.push(1); // Always show first page
    for (let i = current - delta; i <= current + delta; i++) {
        if (i > 1 && i < total) {
            range.push(i);
        }
    }
    if (total > 1) range.push(total); // Always show last page

    range.sort((a,b)=> a-b) // Ensure sorted unique numbers

    for (const i of range) {
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


// Reset page when search term changes
watch(searchTerm, () => {
    currentPage.value = 1;
});

</script>


<style scoped>
.pagination-btn {
    padding: 0.5rem 0.75rem;
    border-radius: 0.375rem; /* rounded-md */
    background-color: #374151; /* bg-gray-700 */
    transition: background-color 0.2s;
    min-width: 2.5rem; /* Ensure number buttons have width */
    text-align: center;
}
.pagination-btn:hover:not(:disabled) {
    background-color: #4b5563; /* bg-gray-600 */
}
.pagination-btn.bg-sky-600 { /* Active page style */
     background-color: #0284c7;
     color: white;
}
</style>