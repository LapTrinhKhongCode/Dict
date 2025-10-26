<!-- components/HomePage.vue -->
<template>
  <div class="min-h-screen text-white p-4 sm:p-8">
    <div class="max-w-7xl mx-auto">

      <!-- Section: Sổ tay -->
      <section class="mb-12">
        <div class="flex justify-between items-center mb-4">
          <h2 class="text-3xl font-bold text-sky-400 flex items-center">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-7 w-7 mr-2" viewBox="0 0 20 20" fill="currentColor"><path d="M9 4.804A7.968 7.968 0 005.5 4c-1.255 0-2.443.29-3.5.804v10A7.969 7.969 0 015.5 16c1.255 0 2.443-.29 3.5-.804V4.804zM14.5 4c-1.255 0-2.443.29-3.5.804v10A7.969 7.969 0 0114.5 16c1.255 0 2.443-.29 3.5.804v-10A7.968 7.968 0 0014.5 4z" /></svg>
            Sổ tay
          </h2>
          <button @click="emit('go-to-my-decks')" class="text-sm text-sky-400 hover:text-sky-300">Xem thêm</button>
        </div>
        <div class="">
          <button @click="emit('go-to-create-deck')" class="bg-sky-600 hover:bg-sky-700 rounded-lg p-5 flex flex-col items-center justify-center h-10 text-center transition-colors mr-6 pr-5 mb-5 w-10">
            <!-- <svg xmlns="http://www.w3.org/2000/svg" class="h-12 w-12 mb-2" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M12 6v6m0 0v6m0-6h6m-6 0H6" /></svg> -->
            <span class="text-xl font-bold">+</span>
          </button>
        <div v-if="isLoadingMy" class="text-center text-gray-400 py-10">Đang tải sổ tay...</div>
        <div v-else-if="errorMy" class="text-center text-red-400 p-4 bg-red-900/50 rounded-lg">{{ errorMy }}</div>
        <div v-else-if="myDecks.length === 0" class="text-center text-gray-500 py-10">Bạn chưa có sổ tay nào.</div>
        
        <div v-else class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6" >
          <DeckCard
            v-for="deck in myDecks.slice(0, 7)" 
            :key="`my-${deck.id}`"
            :deck="deck"
            :current-user-id="currentUserId"
            :current-user-name="currentUserName"
            variant="my"
            @select-set="emit('select-set', deck.id)"
          />
        </div>
        </div>
      </section>

      <!-- Section: Khám phá -->
      <section>
        <div class="flex justify-between items-center mb-4">
          <h2 class="text-3xl font-bold text-emerald-400 flex items-center">
             <svg xmlns="http://www.w3.org/2000/svg" class="h-7 w-7 mr-2" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM4.332 8.027a6.012 6.012 0 011.912-2.706C6.512 5.73 6.974 6 7.5 6A1.5 1.5 0 019 7.5V8a2 2 0 004 0 2 2 0 011.523-1.943A5.977 5.977 0 0116 10c0 .34-.028.675-.083 1H15a2 2 0 00-2 2v2.197A5.973 5.973 0 0110 16v-2a2 2 0 00-2-2 2 2 0 01-2-2 2 2 0 00-1.668-.973z" clip-rule="evenodd" /></svg>
            Khám phá
          </h2>
          <button @click="emit('go-to-explore')" class="text-sm text-emerald-400 hover:text-emerald-300">Xem thêm</button>
        </div>
        <div class="mb-6 max-w-md">
           <input
              type="text"
              v-model="searchTerm"
              @keyup.enter="searchDecks"
              placeholder="Tìm kiếm theo tên bộ thẻ..."
              class="w-full bg-gray-800 border border-gray-600 rounded-lg px-4 py-2 text-white focus:outline-none focus:ring-2 focus:ring-emerald-500"
            />
        </div>
        <div v-if="isLoadingExplore" class="text-center text-gray-400 py-10">Đang tìm kiếm...</div>
        <div v-else-if="errorExplore" class="text-center text-red-400 p-4 bg-red-900/50 rounded-lg">{{ errorExplore }}</div>
        <div v-else-if="exploreDecks.length === 0" class="text-center text-gray-500 py-10">Không tìm thấy bộ thẻ nào khớp.</div>
        <div v-else class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
     <DeckCard
            v-for="deck in filteredExploreDecks.slice(0, 8)"
            :key="`exp-${deck.id}`"
            :deck="deck"
            :current-user-id="currentUserId"
            :current-user-name="currentUserName"
            variant="explore"
            @select-set="emit('select-set', deck.id)"
            @save-deck="saveDeck"
          />
        </div>
      </section>
    </div>

    <!-- Save Success Notification -->
    <div v-if="showSaveSuccess" class="fixed bottom-5 right-5 bg-green-600 text-white py-2 px-4 rounded-lg shadow-lg animate-bounce z-50">
      Đã lưu vào Sổ tay!
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import type { DeckSummaryDto } from '~/types';
import DeckCard from './DeckCard.vue';
import { useJwt } from '~/composables/useJwt';

const { username, avatarUrl, isAuthenticated, logout, jwt } = useJwt();

const props = defineProps<{ 
  currentUserId: number, 
  currentUserName: string 
}>();
const emit = defineEmits(['select-set', 'go-to-create-deck', 'go-to-my-decks', 'go-to-explore']);

const config = useRuntimeConfig()
const BASE_URL = config.public.apiBaseUrl

const myDecks = ref<DeckSummaryDto[]>([]);
const exploreDecks = ref<DeckSummaryDto[]>([]);
const isLoadingMy = ref(true);
const isLoadingExplore = ref(true);
const errorMy = ref<string | null>(null);
const errorExplore = ref<string | null>(null);
const searchTerm = ref('');
const showSaveSuccess = ref(false);

// --- API Handling (Should be centralized in a service/util) ---
async function handleResponse<T>(response: Response): Promise<T> {
  // ... (Same implementation as in App.vue) ...
    if (!response.ok) {
        const errorText = await response.text();
        let errorMessage = errorText;
        try {
            const errorJson = JSON.parse(errorText);
            if (errorJson && errorJson.message) errorMessage = errorJson.message;
            else if (errorJson && errorJson.title) errorMessage = errorJson.title;
        } catch (e) {}
        throw new Error(errorMessage || `Yêu cầu thất bại: ${response.status}`);
    }
    if (response.status === 204) return {} as T;
    const data = await response.json();
    if (data.isSuccess === false) {
        throw new Error(data.message || 'Lỗi từ API');
    }
    return (data.result === undefined ? data : data.result) as T;
}
// -----------------------------------------------------------------

const filteredExploreDecks = computed(() => {
  // Lọc ra những deck có authorName khác với tên người dùng hiện tại
  
  return exploreDecks.value.filter(deck => deck.authorName !== props.currentUserName);
});

async function fetchMyDecks() {
  console.log(props.currentUserName)
  isLoadingMy.value = true;
  errorMy.value = null;
  try {
    // Assuming GET /api/decks/my-decks requires authentication
    const response = await fetch(`${BASE_URL}/api/Decks/my-decks`, {
      cache: 'no-store',
      headers: { 'Authorization': `Bearer ${jwt.value}` } 
    });
    // Assuming API returns ResponseDTO<List<DeckSummaryDto>>
    myDecks.value = await handleResponse<DeckSummaryDto[]>(response);
  } catch (err: any) {
    errorMy.value = `Lỗi tải Sổ tay: ${err.message}`;
    console.error("fetchMyDecks Error:", err);
  } finally {
    isLoadingMy.value = false;
  }
}

async function fetchExploreDecks(query: string = '') {
  isLoadingExplore.value = true;
  errorExplore.value = null;
  let url = `${BASE_URL}/api/decks/public`; // API for public decks
  if (query.trim()) {
      url = `${BASE_URL}/api/decks/search?name=${encodeURIComponent(query.trim())}`;
  }

  try {
    const response = await fetch(url, { cache: 'no-store' });
     // Assuming API returns ResponseDTO<List<DeckSummaryDto>>
    exploreDecks.value = await handleResponse<DeckSummaryDto[]>(response);
  } catch (err: any) {
    errorExplore.value = `Lỗi tải Khám phá: ${err.message}`;
    console.error("fetchExploreDecks Error:", err);
    exploreDecks.value = []; // Clear results on error
  } finally {
    isLoadingExplore.value = false;
  }
}

function searchDecks() {
    fetchExploreDecks(searchTerm.value);
}

async function saveDeck(deckId: number) {
  try {
    // Assuming POST /api/decks/{deckId}/save requires authentication
    const response = await fetch(`${BASE_URL}/api/decks/${deckId}/save`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${jwt.value}` 
        },
    });
    // API returns ResponseDTO<DeckSummaryDto> of the SAVED deck
    await handleResponse<DeckSummaryDto>(response); 
    
    showSaveSuccess.value = true;
    setTimeout(() => { showSaveSuccess.value = false; }, 2500); // Show longer
    
    fetchMyDecks(); // Refresh "Sổ tay" list
  } catch (err: any) {
      alert(`Lưu thất bại: ${err.message}`);
      console.error("saveDeck Error:", err);
  }
}

onMounted(() => {
  fetchMyDecks();
  fetchExploreDecks(); // Load initial public decks
});
</script>
