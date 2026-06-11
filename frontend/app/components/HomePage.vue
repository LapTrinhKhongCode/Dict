<template>
  <div
    class="min-h-screen bg-gray-50 text-gray-900 dark:bg-neutral-900 dark:text-white p-4 sm:p-8 relative transition-colors"
  >
    <div
      v-if="showLoginNotice"
      class="fixed top-5 right-5 bg-yellow-500 text-gray-900 py-2 px-4 rounded-lg shadow-lg z-50 animate-fade-in-out"
    >
      Vui lòng đăng nhập để lưu bộ thẻ!
    </div>
    <div
      v-if="showSaveSuccess"
      class="fixed bottom-5 right-5 bg-green-600 text-white py-2 px-4 rounded-lg shadow-lg animate-bounce z-50"
    >
      Đã lưu vào Sổ tay!
    </div>

    <div class="max-w-7xl mx-auto">
      <section v-if="isAuthenticated" class="mb-12">
        <div class="flex justify-between items-center mb-4">
          <h2 class="text-3xl font-bold text-primary-600 dark:text-sky-400 flex items-center">
            <svg
              xmlns="http://www.w3.org/2000/svg"
              class="h-7 w-7 mr-2"
              viewBox="0 0 20 20"
              fill="currentColor"
            >
              <path
                d="M9 4.804A7.968 7.968 0 005.5 4c-1.255 0-2.443.29-3.5.804v10A7.969 7.969 0 015.5 16c1.255 0 2.443-.29 3.5-.804V4.804zM14.5 4c-1.255 0-2.443.29-3.5.804v10A7.969 7.969 0 0114.5 16c1.255 0 2.443-.29 3.5.804v-10A7.968 7.968 0 0014.5 4z"
              />
            </svg>
            Sổ tay
          </h2>
          <button
            @click="emit('go-to-my-decks')"
            class="text-sm text-primary-600 hover:text-primary-500 dark:text-sky-400 dark:hover:text-sky-300"
          >
            Xem thêm
          </button>
          <button
            @click="emit('go-to-project-notebook')"
            class="text-sm text-purple-500 hover:text-purple-400 ml-3 font-semibold"
          >
            📚 Sổ tay Dự án
          </button>
        </div>
        <div class="flex flex-wrap flex-col">
          <button
            @click="emit('go-to-create-deck')"
            class="bg-primary-600 hover:bg-primary-700 dark:bg-sky-600 dark:hover:bg-sky-700 rounded-lg p-0 flex items-center justify-center h-10 w-10 text-center text-white transition-colors mb-5 mr-6 shrink-0"
          >
            <span class="text-2xl font-bold leading-none">+</span>
          </button>
          <div class="flex-grow">
            <div
              v-if="isLoadingMy"
              class="text-center text-gray-500 dark:text-gray-400 py-10"
            >
              Đang tải sổ tay...
            </div>
            <div
              v-else-if="errorMy"
              class="text-center text-red-700 bg-red-100 dark:text-red-400 dark:bg-red-900/50 p-4 rounded-lg"
            >
              {{ errorMy }}
            </div>
            <div
              v-else-if="myDecksInternal.length === 0"
              class="text-center text-gray-400 dark:text-gray-500 py-10"
            >
              Bạn chưa có sổ tay nào.
            </div>
            <div
              v-else
              class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6"
            >
              <DeckCard
                v-for="deck in myDecksInternal.slice(0, 7)"
                :key="`my-${deck.id}`"
                :deck="deck"
                :current-user-id="currentUserId"
                :current-user-name="currentUserName"
                variant="my"
                @select-set="emit('select-set', deck.id)"
                @show-login-notice="displayLoginNotice"
              />
            </div>
          </div>
        </div>
      </section>

      <section>
        <div class="flex justify-between items-center mb-4">
          <h2 class="text-3xl font-bold text-emerald-600 dark:text-emerald-400 flex items-center">
            <svg
              xmlns="http://www.w3.org/2000/svg"
              class="h-7 w-7 mr-2"
              viewBox="0 0 20 20"
              fill="currentColor"
            >
              <path
                fill-rule="evenodd"
                d="M10 18a8 8 0 100-16 8 8 0 000 16zM4.332 8.027a6.012 6.012 0 011.912-2.706C6.512 5.73 6.974 6 7.5 6A1.5 1.5 0 019 7.5V8a2 2 0 004 0 2 2 0 011.523-1.943A5.977 5.977 0 0116 10c0 .34-.028.675-.083 1H15a2 2 0 00-2 2v2.197A5.973 5.973 0 0110 16v-2a2 2 0 00-2-2 2 2 0 01-2-2 2 2 0 00-1.668-.973z"
                clip-rule="evenodd"
              />
            </svg>
            Khám phá
          </h2>
          <button
            @click="emit('go-to-explore')"
            class="text-sm text-emerald-600 hover:text-emerald-500 dark:text-emerald-400 dark:hover:text-emerald-300"
          >
            Xem thêm
          </button>
        </div>
        <div class="mb-6 max-w-md">
          <input
            type="text"
            v-model="searchTerm"
            @keyup.enter="searchDecks"
            placeholder="Tìm kiếm theo tên bộ thẻ..."
            class="w-full bg-white text-gray-900 border border-gray-300 rounded-lg px-4 py-2 focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500 dark:bg-gray-800 dark:text-white dark:border-gray-600 dark:focus:ring-emerald-500"
          />
        </div>
        <div
          v-if="isLoadingExplore"
          class="text-center text-gray-500 dark:text-gray-400 py-10"
        >
          Đang tìm kiếm...
        </div>
        <div
          v-else-if="errorExplore"
          class="text-center text-red-700 bg-red-100 dark:text-red-400 dark:bg-red-900/50 p-4 rounded-lg"
        >
          {{ errorExplore }}
        </div>
        <div
          v-else-if="filteredExploreDecks.length === 0"
          class="text-center text-gray-400 dark:text-gray-500 py-10"
        >
          Không tìm thấy bộ thẻ nào khớp.
        </div>
        <div
          v-else
          class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6"
        >
          <DeckCard
            v-for="deck in filteredExploreDecks.slice(0, 8)"
            :key="`exp-${deck.id}`"
            :deck="deck"
            :current-user-id="currentUserId"
            :current-user-name="currentUserName"
            variant="explore"
            @select-set="emit('select-set', deck.id)"
            @save-deck="saveDeck"
            @show-login-notice="displayLoginNotice"
          />
        </div>
      </section>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue';
import type { DeckSummaryDto } from '~/types';
import DeckCard from './DeckCard.vue';
import { useJwt } from '~/composables/useJwt';

const { isAuthenticated, jwt } = useJwt();

const props = defineProps<{
  currentUserId: number,
  currentUserName: string
}>();
const emit = defineEmits([
  'select-set',
  'go-to-create-deck',
  'go-to-my-decks',
  'go-to-explore',
  'go-to-project-notebook',
  'show-login-notice',
  'update:my-decks',
  'update:explore-decks'
]);

const config = useRuntimeConfig()
const BASE_URL = config.public.apiBaseUrl || 'https://localhost:7084';

const myDecksInternal = ref<DeckSummaryDto[]>([]);
const exploreDecksInternal = ref<DeckSummaryDto[]>([]);
const isLoadingMy = ref(true);
const isLoadingExplore = ref(true);
const errorMy = ref<string | null>(null);
const errorExplore = ref<string | null>(null);
const searchTerm = ref('');
const showSaveSuccess = ref(false);
const showLoginNotice = ref(false);

const filteredExploreDecks = computed(() => {
  return exploreDecksInternal.value.filter(deck => deck.authorName !== props.currentUserName);
});

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const errorText = await response.text();
    let errorMessage = errorText;
    try { const errorJson = JSON.parse(errorText); if (errorJson?.message) errorMessage = errorJson.message; else if (errorJson?.title) errorMessage = errorJson.title; } catch (e) { }
    throw new Error(errorMessage || `Yêu cầu thất bại: ${response.status}`);
  }
  if (response.status === 204) return {} as T;
  try { const data = await response.json(); if (data.isSuccess === false) { throw new Error(data.message || 'Lỗi từ API'); } return (data.result === undefined ? data : data.result) as T; }
  catch (e) { console.error("JSON Parse Error:", e); throw new Error("Invalid response from server."); }
}

async function fetchMyDecks() {
  if (!isAuthenticated.value) { myDecksInternal.value = []; isLoadingMy.value = false; emit('update:my-decks', []); return; } // Emit empty on logout
  isLoadingMy.value = true; errorMy.value = null;
  try {
    const response = await fetch(`${BASE_URL}/api/Decks/my-decks`, { cache: 'no-store', headers: { 'Authorization': `Bearer ${jwt.value}` } });
    const decks = await handleResponse<DeckSummaryDto[]>(response);
    myDecksInternal.value = decks;
    emit('update:my-decks', decks);
  } catch (err: any) { errorMy.value = `Lỗi tải Sổ tay: ${err.message}`; console.error("fetchMyDecks Error:", err); myDecksInternal.value = []; emit('update:my-decks', []); }// Emit empty on error
  finally { isLoadingMy.value = false; }
}

async function fetchExploreDecks(query: string = '') {
  isLoadingExplore.value = true; errorExplore.value = null;
  let url = `${BASE_URL}/api/decks/public`;
  if (query.trim()) { url = `${BASE_URL}/api/decks/search?name=${encodeURIComponent(query.trim())}`; }
  try {
    const response = await fetch(url, { cache: 'no-store' });
    const decks = await handleResponse<DeckSummaryDto[]>(response);
    exploreDecksInternal.value = decks;
    emit('update:explore-decks', decks);
  } catch (err: any) { errorExplore.value = `Lỗi tải Khám phá: ${err.message}`; console.error("fetchExploreDecks Error:", err); exploreDecksInternal.value = []; emit('update:explore-decks', []); }// Emit empty on error
  finally { isLoadingExplore.value = false; }
}

function searchDecks() { fetchExploreDecks(searchTerm.value); }

async function saveDeck(deckId: number) {
  try {
    const response = await fetch(`${BASE_URL}/api/decks/${deckId}/save`, { method: 'POST', headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${jwt.value}` } });
    await handleResponse<DeckSummaryDto>(response);
    showSaveSuccess.value = true; setTimeout(() => { showSaveSuccess.value = false; }, 2500);
    fetchMyDecks(); // Refresh "Sổ tay" list and emit update
  } catch (err: any) { alert(`Lưu thất bại: ${err.message}`); console.error("saveDeck Error:", err); }
}

let noticeTimeout: ReturnType<typeof setTimeout> | null = null;
function displayLoginNotice() {
  if (noticeTimeout) clearTimeout(noticeTimeout);
  showLoginNotice.value = true;
  noticeTimeout = setTimeout(() => { showLoginNotice.value = false; noticeTimeout = null; }, 3000);
}

watch(isAuthenticated, (loggedIn) => {
  // Refetch myDecks when login status changes
  fetchMyDecks();
}, { immediate: true }); // Use immediate: true if you want it to run on initial load too

// No need to fetch explore decks on auth change unless required

onMounted(() => {
  // fetchMyDecks() is called by the watcher now
  fetchExploreDecks(); // Fetch explore decks on mount
});
</script>

<style>
/* (Giữ nguyên style animation) */
@keyframes fadeInOut {
  0%,
  100% {
    opacity: 0;
    transform: translateY(-10px);
  }
  10%,
  90% {
    opacity: 1;
    transform: translateY(0);
  }
}
.animate-fade-in-out {
  animation: fadeInOut 3s ease-in-out forwards;
}
</style>