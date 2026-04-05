<template>
  <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6">
    <h2 class="text-2xl font-bold text-gray-900 dark:text-white mb-6">Quản lý Flashcard (Bộ thẻ)</h2>
    <div class="mb-6 flex gap-4">
      <input type="text" v-model="deckSearchTerm" @keyup.enter="handleDeckSearch" placeholder="Tìm theo tên bộ thẻ, tác giả..." class="flex-1 form-input" />
      <button @click="handleDeckSearch" :disabled="isDeckSearching" class="form-button-primary px-6">
        <span v-if="isDeckSearching">Đang tìm...</span><span v-else>Tìm kiếm</span>
      </button>
    </div>

    <div v-if="deckSearchError" class="text-red-500">{{ deckSearchError }}</div>
    <div v-else-if="deckResults.length > 0" class="space-y-4">
      <div class="text-gray-500 text-sm mb-4">Tìm thấy {{ deckSearchTotalCount }} bộ thẻ (trang {{ deckSearchPage }}/{{ deckTotalPages }})</div>
      <div class="space-y-3">
        <div v-for="deck in deckResults" :key="deck.id" class="bg-gray-100 dark:bg-gray-700 rounded-lg p-4 flex items-center justify-between">
          <div class="flex-1 min-w-0 pr-4">
            <h3 class="text-lg font-semibold text-gray-900 dark:text-white truncate">
              {{ deck.name }}
              <span :class="['ml-2 px-2 py-0.5 text-xs font-semibold rounded', deck.isPublic ? 'bg-green-600 text-white' : 'bg-gray-500 text-white']">{{ deck.isPublic ? "Public" : "Private" }}</span>
            </h3>
            <p class="text-gray-600 dark:text-gray-400 text-sm truncate">{{ deck.description || "Không có mô tả" }}</p>
            <div class="text-xs text-gray-500 mt-2">ID: {{ deck.id }} | Tác giả: {{ deck.authorName }} | Số thẻ: {{ deck.cardCount }}</div>
          </div>
          <div class="flex gap-2">
            <button @click="handleSetVisibility(deck)" :disabled="isActionLoading === deck.id" :class="['px-3 py-1.5 text-sm rounded-lg font-medium text-white', deck.isPublic ? 'bg-yellow-600' : 'bg-sky-600']">
              {{ deck.isPublic ? "Ẩn" : "Hiện" }}
            </button>
            <button @click="handleDeleteDeck(deck.id, deck.name)" :disabled="isActionLoading === deck.id" class="px-3 py-1.5 text-sm bg-red-600 text-white rounded-lg font-medium">Xóa</button>
          </div>
        </div>
      </div>
      <div v-if="deckTotalPages > 1" class="flex justify-center gap-4 mt-6">
        <button @click="changeDeckPage(deckSearchPage - 1)" :disabled="deckSearchPage <= 1" class="form-button-primary px-4 py-2">Trước</button>
        <span class="text-gray-500">Trang {{ deckSearchPage }} / {{ deckTotalPages }}</span>
        <button @click="changeDeckPage(deckSearchPage + 1)" :disabled="deckSearchPage >= deckTotalPages" class="form-button-primary px-4 py-2">Sau</button>
      </div>
    </div>
    <div v-else-if="hasDeckSearched" class="text-gray-500">Không tìm thấy bộ thẻ nào.</div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { useJwt } from "~/composables/useJwt";
import { useToast } from "~/composables/useToast";

const { jwt } = useJwt();
const config = useRuntimeConfig();
const BASE_URL = config.public.apiBaseUrl || "https://localhost:7084";
const { showToast } = useToast();

const deckSearchTerm = ref("");
const deckResults = ref<any[]>([]);
const deckSearchTotalCount = ref(0);
const deckSearchPage = ref(1);
const deckPageSize = ref(10);
const isDeckSearching = ref(false);
const deckSearchError = ref<string | null>(null);
const hasDeckSearched = ref(false);
const deckTotalPages = computed(() => Math.ceil(deckSearchTotalCount.value / deckPageSize.value));
const isActionLoading = ref<number | null>(null);

onMounted(() => { handleDeckSearch(); });

async function handleDeckSearch() { isDeckSearching.value = true; deckSearchPage.value = 1; await fetchDecks(); isDeckSearching.value = false; hasDeckSearched.value = true; }
async function changeDeckPage(newPage: number) { deckSearchPage.value = newPage; await fetchDecks(); }

async function fetchDecks() {
  try {
    const res = await fetch(`${BASE_URL}/api/Admin/decks/search?searchTerm=${encodeURIComponent(deckSearchTerm.value)}&page=${deckSearchPage.value}&pageSize=${deckPageSize.value}`, { headers: { Authorization: `Bearer ${jwt.value}` } });
    const data = await res.json();
    if (data.isSuccess === false) throw new Error(data.message);
    deckResults.value = data.result.items; deckSearchTotalCount.value = data.result.totalCount;
  } catch (err: any) { deckSearchError.value = err.message; deckResults.value = []; }
}

async function handleSetVisibility(deck: any) {
  const newStatus = !deck.isPublic; isActionLoading.value = deck.id;
  try {
    const res = await fetch(`${BASE_URL}/api/Admin/decks/${deck.id}/visibility`, { method: "PATCH", headers: { "Content-Type": "application/json", Authorization: `Bearer ${jwt.value}` }, body: JSON.stringify({ isPublic: newStatus }) });
    if (!res.ok) throw new Error("Cập nhật thất bại");
    showToast(`Đã cập nhật trạng thái.`, "success"); deck.isPublic = newStatus;
  } catch (e) {} finally { isActionLoading.value = null; }
}

async function handleDeleteDeck(deckId: number, deckName: string) {
  if (!confirm(`Xóa vĩnh viễn bộ thẻ "${deckName}"?`)) return;
  isActionLoading.value = deckId;
  try {
    const res = await fetch(`${BASE_URL}/api/Admin/decks/${deckId}`, { method: "DELETE", headers: { Authorization: `Bearer ${jwt.value}` } });
    if (!res.ok) throw new Error("Xóa thất bại");
    showToast("Đã xóa bộ thẻ.", "success");
    deckResults.value = deckResults.value.filter(d => d.id !== deckId); deckSearchTotalCount.value--;
  } catch (e) {} finally { isActionLoading.value = null; }
}
</script>