<template>
  <div class="p-6 space-y-6 bg-gray-50 dark:bg-neutral-900 transition-colors">
   
    <div
      class="bg-white dark:bg-neutral-800 border border-gray-200 dark:border-neutral-700 rounded-xl p-4 space-y-4"
    >
      <SearchBar v-model="searchWord" @search="onSearch" />

      <div class="flex items-center space-x-2">
        <button
          @click="setView('word')"
          :class="[
            'px-4 py-2 rounded-lg font-medium transition-all border',
            viewMode === 'word'
              ? 'text-primary-600 border-primary-500 font-semibold dark:text-green-500 dark:border-green-500' // Active state
              : 'text-gray-500 border-transparent hover:text-gray-800 dark:text-gray-400 dark:hover:text-gray-200', // Inactive state
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
              ? 'text-primary-600 border-primary-500 font-semibold dark:text-green-500 dark:border-green-500' // Active state
              : 'text-gray-500 border-transparent hover:text-gray-800 dark:text-gray-400 dark:hover:text-gray-200', // Inactive state
          ]"
        >
          <div class="flex items-center space-x-2 cursor-pointer">
            <UIcon name="i-lucide-book" class="size-4" />
            <span>Hán tự</span>
          </div>
        </button>
      </div>
    </div>
       <!-- Banner - visible only before search -->
    <div v-if="!hasSearched" class="rounded-xl overflow-hidden">
      <img 
        src="/banner.png" 
        alt="Miyo Dictionary Banner" 
        class="w-full h-auto object-cover"
      />
    </div>

    <!-- Show TranslationBlock when no search has been performed -->
    <div v-if="!hasSearched" class="mt-2">
      <h2 class="text-lg font-semibold text-gray-800 dark:text-gray-200 mb-3 flex items-center gap-2">
        <span>✨</span>
        <span>Dịch bằng AI</span>
      </h2>
      <TranslationBlock />
    </div>

    <!-- Show SearchResult after a search is made -->
    <SearchResult
      v-else
      :loading="loading"
      :error="error"
      :result="currentResult"
      :conjugation-result="currentConjugationResult"
      :original-search-word="searchedTerm"
      :has-searched="hasSearched"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, computed } from "vue";
import { useRoute, useRouter } from "vue-router";
import conjugationsData from "~/data/conjugations_normalized.json";
import { toKana } from "wanakana";

import SearchBar from "~/components/SearchBar.vue";
import SearchResult from "~/components/SearchResult.vue";
import TranslationBlock from "~/components/translate/TranslationBlock.vue";

const route = useRoute();
const router = useRouter();

const searchWord = ref((route.query.q as string) || "");

// --- Separate result states ---
const wordResult = ref<any | null>(null);
const kanjiResult = ref<any | null>(null);
const conjugationResult = ref<any | null>(null);
// ---

const loading = ref(false);
const error = ref("");
const searchedTerm = ref(searchWord.value);
const hasSearched = ref(false);
const config = useRuntimeConfig();

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

// ... (helper functions extractWordLeftOfSlash, getDictionaryForm, checkConjugation are unchanged) ...
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

// --- Function to change the view in the URL ---
const setView = (mode: "word" | "kanji") => {
  router.push({ query: { ...route.query, view: mode } });
};
// ---

// --- Main API Fetch Logic (MODIFIED) ---
const fetchAll = async (word: string) => {
  if (!word) return;

  hasSearched.value = true;
  loading.value = true;
  error.value = "";
  wordResult.value = null;
  kanjiResult.value = null;
  conjugationResult.value = null;

  // Word Fetch Logic
  const fetchWordData = async () => {
    try {
      const conjugation = checkConjugation(word);
      if (conjugation) {
        conjugationResult.value = conjugation;
      }
      const dictionaryForm = getDictionaryForm(word);
      const apiUrl = `${config.public.apiBaseUrl}/api/Word/GetWordJson/${encodeURIComponent(
        dictionaryForm
      )}`;
      const res = await fetch(apiUrl);
      if (!res.ok) throw new Error("Word data not found");
      const response = await res.json();

      const hasWordData =
        response.data &&
        response.data.words &&
        response.data.words.length > 0;
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

  // Kanji Fetch Logic
  const fetchKanjiData = async () => {
    try {
      const apiUrl = `${config.public.apiBaseUrl}/api/Kanji/GetKanjiJson/${encodeURIComponent(
        word
      )}`;
      const res = await fetch(apiUrl);
      if (!res.ok) throw new Error("Kanji data not found");
      const response = await res.json();

      if (
        response.status === 200 &&
        response.results &&
        response.results.length > 0
      ) {
        // --- THIS IS THE CHANGE ---
        // We now save the entire list, not just the first item.
        kanjiResult.value = {
          type: "kanji",
          kanjiList: response.results, // Was: kanji: response.results[0]
        };
        // --- END OF CHANGE ---
      }
    } catch (e: any) {
      console.error("Kanji fetch failed:", e.message);
    }
  };

  // --- Run both fetches in parallel ---
  await Promise.allSettled([fetchWordData(), fetchKanjiData()]);

  loading.value = false;

  if (!wordResult.value && !kanjiResult.value) {
    error.value = "No results found for word or kanji.";
  }
};

// --- MODIFIED: onSearch ---
const onSearch = (term: string) => {
  const trimmedWord = term.trim();
  if (!trimmedWord) return;

  const convertedWord = toKana(trimmedWord);

  router.push({
    path: "/search",
    query: { q: convertedWord, view: viewMode.value },
  });
};

// --- MODIFIED: Watcher ---
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