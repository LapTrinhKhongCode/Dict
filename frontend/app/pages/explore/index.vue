<template>
  <div
    id="app"
    class="bg-gray-50 dark:bg-gray-900 min-h-screen relative transition-colors"
  >
    <div
      v-if="showLoginNotice"
      class="fixed top-5 right-5 bg-yellow-500 text-gray-900 py-2 px-4 rounded-lg shadow-lg z-[100] animate-fade-in-out"
    >
      Vui lòng đăng nhập để thực hiện!
    </div>
    <div
      v-if="showSaveSuccessGlobal"
      class="fixed bottom-5 right-5 bg-green-600 text-white py-2 px-4 rounded-lg shadow-lg animate-bounce z-[100]"
    >
      Đã lưu vào Sổ tay!
    </div>

    <div v-if="isLoading" class="flex justify-center items-center h-screen">
      <div
        class="text-gray-700 dark:text-white text-xl flex items-center"
      >
        <svg
          class="animate-spin -ml-1 mr-3 h-5 w-5 text-gray-700 dark:text-white"
          xmlns="http://www.w3.org/2000/svg"
          fill="none"
          viewBox="0 0 24 24"
        >
          <circle
            class="opacity-25"
            cx="12"
            cy="12"
            r="10"
            stroke="currentColor"
            stroke-width="4"
          ></circle>
          <path
            class="opacity-75"
            fill="currentColor"
            d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
          ></path>
        </svg>
        <span>Đang xử lý...</span>
      </div>
    </div>

    <HomePage
      v-else-if="currentView === 'home'"
      :current-user-id="currentUserId"
      :current-user-name="currentUserName"
      @select-set="selectSet"
      @go-to-create-deck="goToCreateDeck"
      @go-to-my-decks="goToMyDecksList"
      @go-to-explore="goToExploreList"
      @show-login-notice="displayLoginNotice"
      @update:my-decks="updateMyDecks"
      @update:explore-decks="updateExploreDecks"
    />
    <CardListPage
      v-else-if="currentView === 'list' && selectedSet"
      :set="selectedSet"
      :current-user-id="currentUserId"
      :current-user-name="currentUserName"
      @start-review="startReview"
      @go-home="goHome"
      @card-updated="goToList"
      @go-to-edit="goToEditDeck"
    />
    <div v-else-if="currentView === 'review' && selectedSet">
      <SrsReviewPage
        v-if="learningMode === 'srs'"
        :deck-id="selectedSet.id"
        @go-to-list="goToList"
      />
      <QuizPage
        v-if="learningMode === 'quiz'"
        :cards="selectedSet.cards"
        @go-to-list="goToList"
        @card-updated="goToList"
      />
    </div>
    <DeckEditor
      v-else-if="currentView === 'edit-deck' && selectedSet"
      :initial-set="selectedSet"
      @go-to-list="goToList"
      @deck-updated="goToList"
    />
    <DeckCreator
      v-else-if="currentView === 'create-deck'"
      @go-back="goHome"
      @deck-created="handleDeckCreated"
    />
    <MyDecksPage
      v-else-if="currentView === 'my-decks-list'"
      :current-user-id="currentUserId"
      :current-user-name="currentUserName"
      :initial-decks="myDecks"
      @select-set="selectSet"
      @go-back="goHome"
      @go-to-create-deck="goToCreateDeck"
    />
    <ExplorePage
      v-else-if="currentView === 'explore-list'"
      :current-user-id="currentUserId"
      :current-user-name="currentUserName"
      :initial-decks="exploreDecks"
      @select-set="selectSet"
      @go-back="goHome"
      @show-login-notice="displayLoginNotice"
      @save-deck-success="displaySaveSuccessAndRefreshMyDecks"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import type { DeckDetailDto, DeckSummaryDto } from '~/types';
import HomePage from '~/components/HomePage.vue';
import CardListPage from '~/components/CardListPage.vue';
import SrsReviewPage from '~/components/FlashCardSet.vue';
import QuizPage from '~/components/QuizPage.vue';
import DeckEditor from '~/components/DeckEditor.vue';
import DeckCreator from '~/components/DeckCreator.vue';
import MyDecksPage from '~/components/exlore/MyDecksPage.vue';
import ExplorePage from '~/components/exlore/ExplorePage.vue';
import { useJwt } from '~/composables/useJwt';

const { jwt, username, isAuthenticated } = useJwt();

type View =
  | 'home'
  | 'list'
  | 'review'
  | 'edit-deck'
  | 'create-deck'
  | 'my-decks-list'
  | 'explore-list';
type LearningMode = 'srs' | 'quiz';

const config = useRuntimeConfig();
const BASE_URL = config.public.apiBaseUrl || 'https://localhost:7084';

const currentUserId = ref(1); // Placeholder
const currentUserName = computed(() => username.value || 'Guest');

const currentView = ref<View>('home');
const selectedSet = ref<DeckDetailDto | null>(null);
const isLoading = ref(false);
const learningMode = ref<LearningMode>('srs');

const showLoginNotice = ref(false);
const showSaveSuccessGlobal = ref(false);

const myDecks = ref<DeckSummaryDto[]>([]);
const exploreDecks = ref<DeckSummaryDto[]>([]);

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const errorText = await response.text();
    let errorMessage = errorText;
    try {
      const errorJson = JSON.parse(errorText);
      if (errorJson?.errors) {
        errorMessage = Object.values(errorJson.errors).flat().join(' ');
      } else if (errorJson?.message) {
        errorMessage = errorJson.message;
      } else if (errorJson?.title) {
        errorMessage = errorJson.title;
      }
    } catch (e) {
      /* Ignore */
    }
    throw new Error(errorMessage || `Yêu cầu thất bại: ${response.status}`);
  }
  if (response.status === 204) return {} as T;
  try {
    const data = await response.json();
    if (data.isSuccess === false) {
      throw new Error(data.message || 'Lỗi không xác định từ API');
    }
    return (data.result === undefined ? data : data.result) as T;
  } catch (e) {
    console.error('Failed to parse API response as JSON:', e);
    throw new Error('Phản hồi không hợp lệ từ máy chủ.');
  }
}
async function fetchDeckDetail(id: number): Promise<DeckDetailDto | null> {
  isLoading.value = true;
  try {
    const response = await fetch(`${BASE_URL}/api/decks/${id}`, {
      cache: 'no-store',
      headers: isAuthenticated.value
        ? { Authorization: `Bearer ${jwt.value}` }
        : {},
    });
    return await handleResponse<DeckDetailDto>(response);
  } catch (error) {
    console.error('Không thể tải chi tiết bộ thẻ:', error);
    alert(
      `Không thể tải chi tiết bộ thẻ: ${
        error instanceof Error ? error.message : String(error)
      }`
    );
    return null;
  } finally {
    isLoading.value = false;
  }
}
async function refreshSelectedSet() {
  if (selectedSet.value) {
    const updatedSet = await fetchDeckDetail(selectedSet.value.id);
    if (updatedSet) {
      selectedSet.value = updatedSet;
    } else {
      goHome();
    }
  }
}
async function selectSet(setId: number) {
  const fetchedSet = await fetchDeckDetail(setId);
  if (fetchedSet) {
    selectedSet.value = fetchedSet;
    currentView.value = 'list';
  }
}
function startReview(mode: LearningMode) {
  if (selectedSet.value) {
    learningMode.value = mode;
    currentView.value = 'review';
  }
}
function goHome() {
  currentView.value = 'home';
  selectedSet.value = null;
}
async function goToList() {
  isLoading.value = true;
  await refreshSelectedSet();
  isLoading.value = false;
  if (selectedSet.value) {
    currentView.value = 'list';
  }
}
function goToEditDeck() {
  currentView.value = 'edit-deck';
}
function goToCreateDeck() {
  currentView.value = 'create-deck';
}
function goToMyDecksList() {
  currentView.value = 'my-decks-list';
}
function goToExploreList() {
  currentView.value = 'explore-list';
}

async function handleDeckCreated(newDeckId: number) {
  await selectSet(newDeckId);
  await refreshMyDecksInBackground();
}

async function refreshMyDecksInBackground() {
  if (!isAuthenticated.value) return;
  try {
    const response = await fetch(`${BASE_URL}/api/Decks/my-decks`, {
      cache: 'no-store',
      headers: { Authorization: `Bearer ${jwt.value}` },
    });
    myDecks.value = await handleResponse<DeckSummaryDto[]>(response);
  } catch (err) {
    console.error('Error refreshing my decks in background:', err);
  }
}

let noticeTimeout: ReturnType<typeof setTimeout> | null = null;
function displayLoginNotice() {
  if (noticeTimeout) clearTimeout(noticeTimeout);
  showLoginNotice.value = true;
  showSaveSuccessGlobal.value = false;
  noticeTimeout = setTimeout(() => {
    showLoginNotice.value = false;
    noticeTimeout = null;
  }, 3000);
}

let saveSuccessTimeout: ReturnType<typeof setTimeout> | null = null;
async function displaySaveSuccessAndRefreshMyDecks() {
  if (saveSuccessTimeout) clearTimeout(saveSuccessTimeout);
  showSaveSuccessGlobal.value = true;
  showLoginNotice.value = false;
  saveSuccessTimeout = setTimeout(() => {
    showSaveSuccessGlobal.value = false;
    saveSuccessTimeout = null;
  }, 2500);
  await refreshMyDecksInBackground();
}

function updateMyDecks(decks: DeckSummaryDto[]) {
  myDecks.value = decks;
}
function updateExploreDecks(decks: DeckSummaryDto[]) {
  exploreDecks.value = decks;
}
</script>

<style>
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
@keyframes bounceSimple {
  0%,
  100% {
    transform: translateY(0);
  }
  50% {
    transform: translateY(-10px);
  }
}
.animate-bounce {
  animation: bounceSimple 1s ease-in-out;
}
</style>