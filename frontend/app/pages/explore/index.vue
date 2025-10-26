<!-- app.vue -->
<template>
  <div id="app" class="bg-gray-900 min-h-screen">
    <!-- Loading Indicator -->
    <div v-if="isLoading" class="flex justify-center items-center h-screen">
      <div class="text-white text-xl flex items-center">
        <svg class="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>
        <span>Đang xử lý...</span>
      </div>
    </div>

    <!-- Views -->
    <HomePage
      v-else-if="currentView === 'home'"
      :current-user-id="currentUserId"
      :current-user-name="currentUserName"  
      @select-set="selectSet"
      @go-to-create-deck="goToCreateDeck"
      @go-to-my-decks="goToMyDecksList"
      @go-to-explore="goToExploreList"
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
        <SrsReviewPage v-if="learningMode === 'srs'" :deck-id="selectedSet.id" @go-to-list="goToList" />
        <QuizPage v-if="learningMode === 'quiz'" :cards="selectedSet.cards" @go-to-list="goToList" @card-updated="goToList" />
        <!-- Add ClassicReviewPage back if needed -->
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
    <!-- Placeholder for full list views -->
    <!-- <MyDecksPage v-else-if="currentView === 'my-decks-list'" ... /> -->
    <!-- <ExplorePage v-else-if="currentView === 'explore-list'" ... /> -->

  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import type { DeckDetailDto, DeckSummaryDto } from '~/types';
import HomePage from '~/components/HomePage.vue';
import CardListPage from '~/components/CardListPage.vue';
import SrsReviewPage from '~/components/FlashCardSet.vue';
// import ClassicReviewPage from '~/components/ClassicReviewPage.vue';
import QuizPage from '~/components/QuizPage.vue';
import DeckEditor from '~/components/DeckEditor.vue';
import DeckCreator from '~/components/DeckCreator.vue';
const BASE_URL = 'https://localhost:7084'; // Ensure no trailing /api
const config = useRuntimeConfig()
const BASE_URL = config.apiBaseUrl

import { useJwt } from '~/composables/useJwt';

const { username, avatarUrl, isAuthenticated, logout, jwt } = useJwt();
type View = 'home' | 'list' | 'review' | 'edit-deck' | 'create-deck'; // Simplified views for now
type LearningMode = 'srs' | 'classic' | 'quiz';

// --- User State (Replace with actual auth logic) ---
const currentUserId = ref(1); 
const currentUserName = ref(username); // <-- Replace with actual username
// --------------------------------------------------

const currentView = ref<View>('home');
const selectedSet = ref<DeckDetailDto | null>(null);
const isLoading = ref(false);
const learningMode = ref<LearningMode>('srs');

// --- API Handling ---
async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const errorText = await response.text();
    let errorMessage = errorText;
    try {
        const errorJson = JSON.parse(errorText);
        if (errorJson && errorJson.message) errorMessage = errorJson.message;
        else if (errorJson && errorJson.title) errorMessage = errorJson.title; // Handle ASP.NET Core validation errors
    } catch (e) { /* Ignore parsing error, use raw text */ }
    throw new Error(errorMessage || `Yêu cầu thất bại: ${response.status}`);
  }
  // Handle 204 No Content for DELETE requests
  if (response.status === 204) {
      return {} as T; // Return an empty object or adjust based on expected type
  }
  const data = await response.json();
  if (data.isSuccess === false) {
    throw new Error(data.message || 'Lỗi từ API');
  }
  // Assuming successful data is in 'result' or the root object
  return (data.result === undefined ? data : data.result) as T;
}

// --- Data Fetching ---
async function fetchDeckDetail(id: number): Promise<DeckDetailDto | null> {
    try {
     const response = await fetch(`${BASE_URL}/api/decks/${id}`, {
  cache: 'no-store',
  headers: {
    'Authorization': `Bearer ${jwt.value}`
  }
});

        // Assuming the API returns ResponseDTO<DeckDetailDto>
        return await handleResponse<DeckDetailDto>(response);
    } catch (error) {
        console.error("Không thể tải chi tiết bộ thẻ:", error);
        alert(`Không thể tải chi tiết bộ thẻ: ${error instanceof Error ? error.message : String(error)}`);
        return null; // Return null on error
    }
}

async function refreshSelectedSet() {
  if (!selectedSet.value) return;
  isLoading.value = true;
  const updatedSet = await fetchDeckDetail(selectedSet.value.id);
  if (updatedSet) {
      selectedSet.value = updatedSet; // Update only if fetch succeeded
  } else {
      // Handle case where deck might have been deleted - go home?
      goHome();
  }
  isLoading.value = false;
}

async function selectSet(setId: number) {
  isLoading.value = true;
  const fetchedSet = await fetchDeckDetail(setId);
  if (fetchedSet) {
      selectedSet.value = fetchedSet;
      currentView.value = 'list';
  } else {
      currentView.value = 'home'; // Go back home if fetching fails
  }
  isLoading.value = false;
}

// --- Navigation ---
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
  await refreshSelectedSet(); // Refresh data before showing the list
  currentView.value = 'list';
  isLoading.value = false;
}

function goToEditDeck() {
  currentView.value = 'edit-deck';
}

function goToCreateDeck() {
  currentView.value = 'create-deck';
}

function handleDeckCreated(newDeckId: number) {
  // Go to the newly created deck's list page
  selectSet(newDeckId);
}

// Placeholders for full list navigation
function goToMyDecksList() { alert("Chuyển đến trang 'Sổ tay của tôi' (chưa implement)"); }
function goToExploreList() { alert("Chuyển đến trang 'Khám phá' (chưa implement)"); }

</script>
