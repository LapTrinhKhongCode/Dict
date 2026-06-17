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

      <!-- Search results (dùng chung cho cả JP→VN và VN→JP) -->
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
import { useRecentSearches } from "~/composables/useRecentSearches";

const route = useRoute();
const router = useRouter();

const searchWord = ref((route.query.q as string) || "");

// --- Separate result states ---
const wordResult = ref<any | null>(null);
const kanjiResult = ref<any | null>(null);
const conjugationResult = ref<any | null>(null);
const selectedWordItem = ref<any | null>(null);
const searchResultRef = ref(null);
// ---

const loading = ref(false);
const error = ref("");
const searchedTerm = ref(searchWord.value);
const hasSearched = ref(false);
const config = useRuntimeConfig();
const { addRecentSearch } = useRecentSearches();

// --- Search mode from URL ---
const searchMode = computed(() => (route.query.mode as string) === 'vi-ja' ? 'vi-ja' : 'ja-vi');

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

// --- NEW: Hàm Lazy Load Kanji thông minh ---
const loadKanjiIfNeeded = async () => {
  // Nếu đã có kết quả Kanji rồi thì không gọi lại API nữa
  if (kanjiResult.value) return;

  // Xác định từ khóa (Ưu tiên từ đang chọn > từ kết quả word > từ search gốc)
  const termToSearch =
    selectedWordItem.value?.word ||
    wordResult.value?.words?.[0]?.word ||
    searchedTerm.value;

  // Chỉ gọi API nếu trong chuỗi có chứa ký tự Hán Tự (Kanji)
  const hasKanji = /[\u4e00-\u9faf]/.test(termToSearch);

  if (hasKanji) {
    loading.value = true;
    await fetchKanjiDataOnly(termToSearch);
    loading.value = false;
  }
};

// --- Function to change the view in the URL (ĐÃ SỬA THÊM LAZY LOAD) ---
const setView = async (mode: "word" | "kanji") => {
  router.push({ query: { ...route.query, view: mode } });

  // NẾU bấm sang tab Kanji, kích hoạt hàm kiểm tra và load Kanji
  if (mode === "kanji") {
    await loadKanjiIfNeeded();
  }
};

// --- NEW: Kanji Fetch Logic (Hàm độc lập - GIỮ NGUYÊN CÁCH BÓC TÁCH CỦA ÔNG) ---
// Trả về 1 chuỗi dính liền các chữ Hán duy nhất (VD: "大学")
const extractKanjiString = (text: string): string => {
  const kanjiRegex = /[\u4e00-\u9faf\u3400-\u4dbf]/g;
  const matches = text.match(kanjiRegex);
  return matches ? [...new Set(matches)].join("") : "";
};

// Hàm gọi API nay đã sạch sẽ và tối ưu tuyệt đối
// --- HÀM TRONG INDEX.VUE ---
const fetchKanjiDataOnly = async (kanjiSearchTerm: string) => {
  kanjiResult.value = null;

  try {
    const kanjiString = extractKanjiString(kanjiSearchTerm);
    if (!kanjiString) return;

    const apiUrl = `${
      config.public.apiBaseUrl
    }/api/Kanji/GetKanjiJson/${encodeURIComponent(kanjiString)}`;
    const res = await fetch(apiUrl);

    if (!res.ok) throw new Error("Kanji data not found");
    const response = await res.json();

    if (
      response.status === 200 &&
      response.results &&
      response.results.length > 0
    ) {
      const validKanjiList = [];

      response.results.forEach((item: any) => {
        if (item && item.results && item.results.length > 0) {
          validKanjiList.push(item.results[0]);
        } else if (item && item.kanji) {
          validKanjiList.push(item);
        } else if (item && item.data && item.data.length > 0) {
          validKanjiList.push(item.data[0]);
        }
      });

      // Nếu gọt xong mà vẫn có mảng hợp lệ thì mới gán
      if (validKanjiList.length > 0) {
        kanjiResult.value = {
          type: "kanji",
          kanjiList: validKanjiList,
        };
      } else {
        console.warn("Lỗi: Dữ liệu Kanji lấy về không có cấu trúc hợp lệ.");
      }
    }
  } catch (e: any) {
    console.error("Kanji fetch failed:", e.message);
  }
};

// --- NEW: Function to handle selection change from child (ĐÃ SỬA THÊM LAZY LOAD) ---
// --- SỬA LẠI HÀM NÀY TRONG INDEX.VUE ---
const handleSelectionChange = async (item: any) => {
  // 1. Lưu selected item vào state
  selectedWordItem.value = item;

  // >>> CHỐT CHẶN SINH TỬ <<<
  // Nếu item được chọn là một chữ Kanji (có thuộc tính .kanji)
  // thì DỪNG LẠI, KHÔNG LÀM GÌ CẢ! Giữ nguyên giao diện!
  if (item && item.kanji) {
    return;
  }

  // 2. Chỉ khi chọn Từ Vựng (có thuộc tính .word) thì mới reset Kanji cũ để load mới
  kanjiResult.value = null;

  if (viewMode.value === "kanji" && item && item.word) {
    await loadKanjiIfNeeded();
  }
};

// --- Main API Fetch Logic ---
const fetchAll = async (word: string) => {
  if (!word) return;

  hasSearched.value = true;
  loading.value = true;
  error.value = "";
  wordResult.value = null;
  kanjiResult.value = null;
  conjugationResult.value = null;
  selectedWordItem.value = null;

  const fetchWordData = async () => {
    try {
      let apiUrl: string;

      if (searchMode.value === 'vi-ja') {
        // VN→JP: gọi endpoint mới, trả về cùng format (suggestWords)
        apiUrl = `${config.public.apiBaseUrl}/api/Word/SearchByViMeaning/${encodeURIComponent(word.trim())}`;
      } else {
        // JP→VN: flow cũ
        const conjugation = checkConjugation(word);
        conjugationResult.value = conjugation;
        const dictionaryForm = getDictionaryForm(word);
        apiUrl = `${config.public.apiBaseUrl}/api/Word/GetWordJson/${encodeURIComponent(dictionaryForm)}`;
      }

      const res = await fetch(apiUrl);
      if (!res.ok) throw new Error("Word data not found");
      const response = await res.json();

      const hasWordData = response.data?.words?.length > 0;
      const hasSuggestData = response.data?.suggestWords?.length > 0;

      if (response.status === 200 && (hasWordData || hasSuggestData)) {
        wordResult.value = { type: "word", ...response.data };
      }
    } catch (e: any) {
      console.error("Word fetch failed:", e.message);
    }
  };

  await fetchWordData();

  // Kanji lazy load chỉ cho JP→VN
  if (searchMode.value === 'ja-vi' && viewMode.value === "kanji") {
    await loadKanjiIfNeeded();
  }

  loading.value = false;

  if (searchMode.value === 'ja-vi' && wordResult.value?.words?.[0]) {
    const firstWord = wordResult.value.words[0];
    addRecentSearch({
      word: firstWord.word,
      phonetic: firstWord.phonetic || "",
      short_mean: firstWord.short_mean || "",
    });
  }

  if (!wordResult.value && viewMode.value !== "kanji") {
    error.value = searchMode.value === 'vi-ja'
      ? "Không tìm thấy từ tiếng Nhật nào."
      : "Không tìm thấy dữ liệu từ vựng.";
  }
};

// --- onSearch: lưu mode vào URL ---
const onSearch = (term: string, mode: 'ja-vi' | 'vi-ja' = 'ja-vi') => {
  const trimmedWord = term.trim();
  if (!trimmedWord) return;

  const convertedWord = mode === 'ja-vi' ? toKana(trimmedWord) : trimmedWord;

  router.push({
    path: "/search",
    query: { q: convertedWord, view: viewMode.value, mode },
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
  { immediate: true },
);
</script>
