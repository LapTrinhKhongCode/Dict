<template>
  <div class="p-6 space-y-6">
    <SearchBar v-model="searchWord" @search="onSearch" />

    <SearchResult
      :loading="loading"
      :error="error"
      :result="result"
      :conjugation-result="conjugationResult"
      :original-search-word="searchedTerm"
      :has-searched="hasSearched" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import conjugationsData from "~/data/conjugations_normalized.json";
import { toKana } from "wanakana";

import SearchBar from "~/components/SearchBar.vue";
import SearchResult from "~/components/SearchResult.vue";

const route = useRoute();
const router = useRouter();

const searchWord = ref(
  (route.query.word as string) || (route.query.kanji as string) || ""
);
const result = ref<any | null>(null);
const loading = ref(false);
const error = ref("");
const conjugationResult = ref<any | null>(null);
const searchedTerm = ref(searchWord.value);
const hasSearched = ref(false); // <-- 1. ADD THIS STATE

// ... (helper functions isSingleKanji, extractWordLeftOfSlash, getDictionaryForm, checkConjugation are unchanged) ...
const isSingleKanji = (text: string): boolean => {
  const trimmed = text.trim();
  if (trimmed.length !== 1) return false;
  const charCode = trimmed.charCodeAt(0);
  return (
    (charCode >= 0x4e00 && charCode <= 0x9fff) ||
    (charCode >= 0x3400 && charCode <= 0x4dbf) ||
    (charCode >= 0x20000 && charCode <= 0x2a6df)
  );
};
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

// --- Main API Fetch Logic ---
const fetchWord = async (word: string) => {
  hasSearched.value = true;
  try {
    loading.value = true;
    error.value = "";
    result.value = null;
    conjugationResult.value = null;

    let apiUrl: string;
    let response: any;

    if (isSingleKanji(word)) {
      // Use kanji API
      apiUrl = `https://localhost:7084/api/Kanji/GetKanjiJson/${encodeURIComponent(
        word
      )}`;
      const res = await fetch(apiUrl);
      
      // If the request failed (404, 500, etc.), just stop.
      if (!res.ok) return; // <-- CHANGED

      response = await res.json();

      if (
        response.status === 200 &&
        response.results &&
        response.results.length > 0
      ) {
        result.value = {
          type: "kanji",
          kanji: response.results[0],
        };
      } else {
        result.value = null;
      }
    } else {
      // Use word API
      const conjugation = checkConjugation(word);
      if (conjugation) {
        conjugationResult.value = conjugation;
      }
      const dictionaryForm = getDictionaryForm(word);
      apiUrl = `https://localhost:7084/api/Word/GetWordJson/${encodeURIComponent(
        dictionaryForm
      )}`;
      const res = await fetch(apiUrl);

      // If the request failed (404, 500, etc.), just stop.
      if (!res.ok) return; // <-- CHANGED
      
      response = await res.json();

      const hasWordData =
        response.data &&
        response.data.words &&
        response.data.words.length > 0;
      const hasSuggestData =
        response.data &&
        response.data.suggestWords &&
        response.data.suggestWords.length > 0;

      if (response.status === 200 && (hasWordData || hasSuggestData)) {
        result.value = {
          type: "word",
          ...response.data,
        };
      } else {
        result.value = null;
      }
    }
  } catch (e: any) {
    // This will now only catch *unexpected* errors (network down, JSON parse failed)
    error.value = e.message || "Error loading data";
  } finally {
    loading.value = false;
  }
};

// ... (onSearch, onMounted, and watch functions are unchanged) ...
const onSearch = (term: string) => {
  const trimmedWord = term.trim();
  if (!trimmedWord) return;
  const convertedWord = toKana(trimmedWord);
  searchedTerm.value = convertedWord;
  searchWord.value = convertedWord; 
  const queryParam = isSingleKanji(convertedWord) ? "kanji" : "word";
  router.push({ path: "/search", query: { [queryParam]: convertedWord } });
  fetchWord(convertedWord);
};

onMounted(() => {
  if (searchWord.value) {
    searchedTerm.value = searchWord.value;
    fetchWord(searchWord.value);
  }
});

watch(
  () => [route.query.word, route.query.kanji],
  ([newWord, newKanji]) => {
    const queryValue = (typeof newWord === "string" ? newWord : newKanji) as string;
    if (queryValue && queryValue !== searchWord.value) {
      searchWord.value = queryValue;
      searchedTerm.value = queryValue;
      fetchWord(queryValue);
    }
  }
);
</script>