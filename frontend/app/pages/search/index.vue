<template>
  <div class="p-6 space-y-6 bg-gray-50 dark:bg-neutral-900 transition-colors">
    <div
      class="bg-white dark:bg-neutral-800 border border-gray-200 dark:border-neutral-700 rounded-xl p-4 space-y-4"
    >
      <SearchBar
        v-model="searchWord"
        @search="onSearch"
        :search-result-ref="searchResultRef"
      />
      <div class="flex items-center space-x-2">
        <button
          @click="setView('word')"
          :class="[
            'px-4 py-2 rounded-lg font-medium transition-all border',
            viewMode === 'word'
              ? 'text-primary-600 border-primary-500 font-semibold dark:text-green-500 dark:border-green-500'
              : 'text-gray-500 border-transparent hover:text-gray-800 dark:text-gray-400 dark:hover:text-gray-200',
          ]"
        >
          <div class="flex items-center space-x-2 cursor-pointer">
            <UIcon name="i-lucide-file-text" class="size-4" />
            <span>Từ vựng</span>
          </div>
        </button>
        <button
          @click="setView('kanji')"
          :class="[
            'px-4 py-2 rounded-lg font-medium transition-all border',
            viewMode === 'kanji'
              ? 'text-primary-600 border-primary-500 font-semibold dark:text-green-500 dark:border-green-500'
              : 'text-gray-500 border-transparent hover:text-gray-800 dark:text-gray-400 dark:hover:text-gray-200',
          ]"
        >
          <div class="flex items-center space-x-2 cursor-pointer">
            <UIcon name="i-lucide-book" class="size-4" />
            <span>Hán tự</span>
          </div>
        </button>
      </div>
    </div>
    <div id="search-results-area" ref="searchResultRef">
      <!-- Show recent searches when no search has been performed -->
      <RecentSearches v-if="!hasSearched" />

      <!-- Show search results after a search is made -->
      <SearchResult
        v-else
        :loading="loading"
        :error="error"
        :result="currentResult"
        :conjugation-result="currentConjugationResult"
        :original-search-word="searchedTerm"
        :has-searched="hasSearched"
        @item-selected="handleSelectionChange"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, computed } from "vue";
import { useRoute, useRouter } from "vue-router";
import conjugationsData from "~/data/conjugations_normalized.json";
import { toKana } from "wanakana";

import SearchBar from "~/components/SearchBar.vue";
import SearchResult from "~/components/SearchResult.vue";
import RecentSearches from "~/components/RecentSearches.vue";
import { useRecentSearches } from '~/composables/useRecentSearches';

const route = useRoute();
const router = useRouter();

const searchWord = ref((route.query.q as string) || "");

// --- Separate result states ---
const wordResult = ref<any | null>(null);
const kanjiResult = ref<any | null>(null);
const conjugationResult = ref<any | null>(null);
const selectedWordItem = ref<any | null>(null); // State để nhận từ được chọn từ SearchResult
const searchResultRef = ref(null);
// ---

const loading = ref(false);
const error = ref("");
const searchedTerm = ref(searchWord.value);
const hasSearched = ref(false);
const config = useRuntimeConfig();
const { addRecentSearch } = useRecentSearches();

// --- Computed properties to drive the UI ---
const viewMode = computed(() => {
  if (route.query.view === "kanji") return "kanji";
  return "word";
});

const currentResult = computed(() => {
  if (viewMode.value === "kanji") {
    return kanjiResult.value;
  }
  return wordResult.value;
});

const currentConjugationResult = computed(() => {
  if (viewMode.value === "kanji") {
    return null;
  }
  return conjugationResult.value;
});
// ---

// ... (helper functions - Giữ nguyên) ...
const extractWordLeftOfSlash = (word: string): string => {
  const slashIndex = word.indexOf("/");
  return slashIndex !== -1 ? word.substring(0, slashIndex) : word;
};
const getDictionaryForm = (word: string): string => {
  const trimmed = word.trim();
  if (!trimmed) return trimmed;
  if (conjugationsData.byForm && conjugationsData.byForm[trimmed]) {
    const dictionaryFormWithSlash = conjugationsData.byForm[trimmed];
    return extractWordLeftOfSlash(dictionaryFormWithSlash);
  }
  return extractWordLeftOfSlash(trimmed);
};
const checkConjugation = (word: string): any | null => {
  const trimmed = word.trim();
  if (!trimmed) return null;
  const dictionaryForm = getDictionaryForm(trimmed);
  if (conjugationsData.byRoot) {
    const targetKey = `${dictionaryForm}/`;
    for (const [key, conjugations] of Object.entries(conjugationsData.byRoot)) {
      if (key.startsWith(targetKey)) {
        return {
          root: dictionaryForm,
          conjugations: conjugations,
          originalForm: trimmed !== dictionaryForm ? trimmed : null,
        };
      }
    }
  }
  return null;
};

// --- Function to change the view in the URL (Giữ nguyên) ---
const setView = (mode: "word" | "kanji") => {
  router.push({ query: { ...route.query, view: mode } });
};

// --- NEW: Kanji Fetch Logic (Hàm độc lập) ---
const fetchKanjiDataOnly = async (kanjiSearchTerm: string) => {
  // Không cần clear lỗi chính ở đây, chỉ cần clear kết quả cũ
  kanjiResult.value = null;

  try {
    const apiUrl = `${
      config.public.apiBaseUrl
    }/api/Kanji/GetKanjiJson/${encodeURIComponent(
      kanjiSearchTerm // <-- Sử dụng từ khóa/từ có hán tự được chọn
    )}`;
    const res = await fetch(apiUrl);
    if (!res.ok) throw new Error("Kanji data not found");
    const response = await res.json();

    if (
      response.status === 200 &&
      response.results &&
      response.results.length > 0
    ) {
      kanjiResult.value = {
        type: "kanji",
        kanjiList: response.results,
      };
    }
  } catch (e: any) {
    console.error("Kanji fetch failed:", e.message);
  }
};
// ---

// --- NEW: Function to handle selection change from child ---
const handleSelectionChange = (item: any) => {
  // 1. Lưu selected item vào state
  selectedWordItem.value = item;

  // 2. Tự động chạy lại tìm kiếm Kanji nếu item mới có word
  if (item && item.word) {
    fetchKanjiDataOnly(item.word);
  }
};

// --- Main API Fetch Logic (ĐÃ SỬA) ---
const fetchAll = async (word: string) => {
  if (!word) return;

  hasSearched.value = true;
  loading.value = true;
  error.value = "";
  wordResult.value = null;
  kanjiResult.value = null;
  conjugationResult.value = null;
  selectedWordItem.value = null; // Reset selection on new search

  // 1. Chạy Word Fetch (Word Fetch phải chạy trước)
  const fetchWordData = async () => {
    try {
      const conjugation = checkConjugation(word);
      conjugationResult.value = conjugation;

      const dictionaryForm = getDictionaryForm(word);
      const apiUrl = `${
        config.public.apiBaseUrl
      }/api/Word/GetWordJson/${encodeURIComponent(dictionaryForm)}`;
      const res = await fetch(apiUrl);
      if (!res.ok) throw new Error("Word data not found");
      const response = await res.json();

      const hasWordData =
        response.data && response.data.words && response.data.words.length > 0;
      const hasSuggestData =
        response.data &&
        response.data.suggestWords &&
        response.data.suggestWords.length > 0;

      if (response.status === 200 && (hasWordData || hasSuggestData)) {
        wordResult.value = {
          type: "word",
          ...response.data,
        };
      }
    } catch (e: any) {
      console.error("Word fetch failed:", e.message);
    }
  };

  await fetchWordData();

  // 2. Xác định từ khóa tra Kanji
  // Ưu tiên 1: Từ khóa từ kết quả Word đầu tiên (nếu có)
  const kanjiSearchTerm = wordResult.value?.words?.[0]?.word || word;

  // 3. Gọi Kanji Fetch với từ khóa đã ưu tiên (Hàm đã tách)
  await fetchKanjiDataOnly(kanjiSearchTerm);

  loading.value = false;

  // Save to recent searches if word result found
  if (wordResult.value?.words?.[0]) {
    const firstWord = wordResult.value.words[0];
    addRecentSearch({
      word: firstWord.word,
      phonetic: firstWord.phonetic || '',
      short_mean: firstWord.short_mean || '',
    });
  }

  if (!wordResult.value && !kanjiResult.value) {
    error.value = "No results found for word or kanji.";
  }
};

// --- MODIFIED: onSearch và Watcher (Giữ nguyên) ---
const onSearch = (term: string) => {
  const trimmedWord = term.trim();
  if (!trimmedWord) return;

  const convertedWord = toKana(trimmedWord);

  router.push({
    path: "/search",
    query: { q: convertedWord, view: viewMode.value },
  });
};

watch(
  () => route.query.q,
  (newQueryValue) => {
    const queryValue = newQueryValue as string;
    if (queryValue) {
      searchWord.value = queryValue;
      searchedTerm.value = queryValue;
      fetchAll(queryValue);
    } else {
      hasSearched.value = false;
      searchWord.value = "";
      searchedTerm.value = "";
      wordResult.value = null;
      kanjiResult.value = null;
      conjugationResult.value = null;
    }
  },
  { immediate: true }
);
</script>