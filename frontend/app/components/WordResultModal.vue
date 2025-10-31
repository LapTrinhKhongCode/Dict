<template>
  <div
    class="fixed inset-0 z-40 bg-black bg-opacity-20 flex items-center justify-center"
    @click.self="$emit('close')"
  >
    <div
      class="relative bg-white rounded-xl shadow-2xl w-full max-w-3xl h-[85vh] flex flex-col"
    >
      <button
        @click="$emit('close')"
        class="absolute -top-3 -right-3 z-50 bg-gray-800 text-white rounded-full p-1.5 hover:bg-gray-600 transition"
      >
        <UIcon name="i-lucide-x" class="size-5" />
      </button>

      <div class="p-4 border-b border-gray-200">
        <div class="flex items-center space-x-2">
          <button
            @click="activeTab = 'word'"
            :class="[
              'px-4 py-2 rounded-lg font-medium transition-all',
              activeTab === 'word'
                ? 'bg-blue-600 text-white shadow-sm'
                : 'text-gray-600 hover:bg-gray-100',
            ]"
          >
            Từ vựng
          </button>
          <button
            @click="activeTab = 'kanji'"
            :class="[
              'px-4 py-2 rounded-lg font-medium transition-all',
              activeTab === 'kanji'
                ? 'bg-blue-600 text-white shadow-sm'
                : 'text-gray-600 hover:bg-gray-100',
            ]"
          >
            Hán tự
          </button>
        </div>
      </div>
      <div class="flex-1 overflow-y-auto p-6 space-y-8 modal-scroll-content">
        <div v-if="loading" class="flex items-center justify-center py-12">
          <div class="text-gray-500 flex items-center space-x-2">
            <div
              class="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-500"
            ></div>
            <span>Loading...</span>
          </div>
        </div>

        <div v-if="error" class="bg-red-50 border border-red-200 rounded-lg p-4">
          <div class="flex items-center space-x-2">
            <UIcon name="i-lucide-alert-circle" class="text-red-500 size-5" />
            <span class="text-red-700">{{ error }}</span>
          </div>
        </div>

        <div v-if="activeTab === 'word'" v-show="!loading && !error">
          <div
            v-if="result && result.words && result.words.length > 0"
            class="space-y-6"
          >
            <div
              v-for="word in result.words"
              :key="word._id"
              class="space-y-4"
            >
              <div class="space-y-2">
                <h1 class="text-3xl font-bold text-gray-900">{{ word.word }}</h1>
                <div class="flex items-center space-x-4">
                  <span class="text-lg text-blue-600 font-medium">{{
                    word.phonetic
                  }}</span>
                  <!-- <span v-if="word.short_mean" class="text-gray-600 italic">{{
                    word.short_mean
                  }}</span> -->
                </div>
              </div>

              <div v-if="word.means && word.means.length > 0" class="space-y-3">
                <h3
                  class="font-semibold text-gray-800 flex items-center space-x-2"
                >
                  <UIcon name="i-lucide-book-open" class="size-4" />
                  <span>Nghĩa</span>
                </h3>
                <div class="space-y-3">
                  <div
                    v-for="(meaning, idx) in word.means"
                    :key="idx"
                    class="bg-gray-50 rounded-lg p-4"
                  >
                    <div class="flex items-start justify-between mb-2">
                      <p class="text-gray-800 font-medium">
                        {{ meaning.mean }}
                      </p>
                      <span
                        v-if="meaning.kind"
                        class="text-xs bg-blue-100 text-blue-800 px-2 py-1 rounded-full"
                      >
                        {{ meaning.kind }}
                      </span>
                    </div>
                    <div
                      v-if="meaning.examples && meaning.examples.length > 0"
                      class="mt-3 space-y-2"
                    >
                      <h4 class="text-sm font-medium text-gray-600">
                        Ví dụ:
                      </h4>
                      <div
                        v-for="(example, exIdx) in meaning.examples"
                        :key="exIdx"
                        class="bg-white rounded p-3 border-l-4 border-blue-200"
                      >
                        <p class="text-gray-800 mb-1">
                          {{ example.content }}
                        </p>
                        <p class="text-gray-600 text-sm italic">
                          {{ example.mean }}
                        </p>
                        <p
                          v-if="example.transcription"
                          class="text-gray-500 text-xs mt-1"
                        >
                          {{ example.transcription }}
                        </p>
                      </div>
                    </div>
                  </div>
                </div>
              </div>

              <div
                v-if="word.synsets && word.synsets.length > 0"
                class="space-y-2"
              >
                <h3
                  class="font-semibold text-gray-800 flex items-center space-x-2"
                >
                  <UIcon name="i-lucide-link" class="size-4" />
                  <span>Từ đồng nghĩa</span>
                </h3>
                <div class="flex flex-wrap gap-2">
                  <span
                    v-for="synonym in word.synsets[0]?.entry[0]?.synonym || []"
                    :key="synonym"
                    class="bg-green-100 text-green-800 text-sm px-3 py-1 rounded-full"
                  >
                    {{ synonym }}
                  </span>
                </div>
              </div>

              <div
                v-if="word.images && word.images.length > 0"
                class="space-y-3"
              >
                <h3
                  class="font-semibold text-gray-800 flex items-center space-x-2"
                >
                  <UIcon name="i-lucide-image" class="size-4" />
                  <span>Ảnh minh họa</span>
                </h3>
                <div
                  class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-3"
                >
                  <div
                    v-for="(image, index) in word.images"
                    :key="index"
                    class="relative group"
                  >
                    <img
                      :src="image"
                      :alt="`${word.word} - Image ${index + 1}`"
                      class="w-full h-24 object-cover rounded-lg border hover:scale-105 transition-transform cursor-pointer"
                      @error="$event.target.style.display = 'none'"
                    />
                    
                  </div>
                </div>
              </div>
            </div>
          </div>
          <div v-if="conjugationResult" class="space-y-6">
            <h2 class="text-xl font-semibold border-b pb-2">
              Bảng chia động từ
            </h2>
            <ConjugationTable
              :root="conjugationResult.root"
              :conjugations="conjugationResult.conjugations"
              :originalForm="conjugationResult.originalForm"
            />
          </div>
        </div>

        <div v-if="activeTab === 'kanji'" v-show="!loading && !error" class="space-y-6">
          <div v-if="kanjiResults && kanjiResults.length > 0">
            <div class="flex flex-wrap gap-2 border-b pb-4">
              <button
                v-for="(kanji, index) in kanjiResults"
                :key="kanji._id"
                @click="activeKanjiIndex = index"
                :class="[
                  'px-4 py-2 rounded-lg font-medium transition-all text-xl',
                  activeKanjiIndex === index
                    ? 'bg-gray-800 text-white'
                    : 'bg-gray-100 text-gray-700 hover:bg-gray-200',
                ]"
              >
                {{ kanji.kanji }}
              </button>
            </div>

            <div v-if="selectedKanji" class="space-y-6">
              <div class="flex justify-between items-start">
                <div class="space-y-3">
                  <h1 class="text-6xl font-bold text-gray-900">
                    {{ selectedKanji.kanji }}
                  </h1>
                  <div class="flex flex-col space-y-1">
                    <p class="text-gray-800 text-lg">{{ selectedKanji.mean }}</p>
                    <span class="text-lg text-blue-600 font-medium"
                      >On: {{ selectedKanji.on }}</span
                    >
                    <span class="text-lg text-green-600 font-medium"
                      >Kun: {{ selectedKanji.kun }}</span
                    >
                    <span class="text-sm text-gray-500"
                      >Số nét: {{ selectedKanji.stroke_count }}</span
                    >
                    <span class="text-sm text-gray-500"
                      >Độ phổ biến: {{ selectedKanji.freq }}</span
                    >
                  </div>
                  <div class="flex flex-wrap gap-2">
                    <span
                      v-for="level in selectedKanji.level"
                      :key="level"
                      class="bg-purple-100 text-purple-800 text-sm px-3 py-1 rounded-full"
                    >
                      {{ level }}
                    </span>
                  </div>
                </div>
                
                <div class="flex-shrink-0 ml-4">
                  <KanjiStrokeInResult :kanji="selectedKanji.kanji" />
                </div>
              </div>

              <div class="space-y-2">
                <h3
                  class="font-semibold text-gray-800 flex items-center space-x-2"
                >
                  <UIcon name="i-lucide-book-open" class="size-4" />
                  <span>Nghĩa</span>
                </h3>
                <div v-if="selectedKanji.detail" class="space-y-3">
                  <div
                    v-for="(paragraph, index) in selectedKanji.detail.split('##')"
                    :key="index"
                    class="bg-gray-50 rounded-lg p-4 border-l-4 border-blue-200"
                  >
                    <p class="text-gray-700 text-sm leading-relaxed">
                      {{ paragraph.trim() }}
                    </p>
                  </div>
                </div>
              </div>

              <div
                v-if="selectedKanji.tips && selectedKanji.tips.vi"
                class="space-y-2"
              >
                <h3
                  class="font-semibold text-gray-800 flex items-center space-x-2"
                >
                  <UIcon name="i-lucide-lightbulb" class="size-4" />
                  <span>Mẹo nhớ</span>
                </h3>
                <div class="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
                  <p
                    class="text-gray-800"
                    v-html="selectedKanji.tips.vi"
                  ></p>
                </div>
              </div>

              <div
                v-if="selectedKanji.compDetail && selectedKanji.compDetail.length > 0"
                class="space-y-3"
              >
                <h3
                  class="font-semibold text-gray-800 flex items-center space-x-2"
                >
                  <UIcon name="i-lucide-puzzle" class="size-4" />
                  <span>Bộ</span>
                </h3>
                <div class="flex flex-wrap gap-3">
                  <div
                    v-for="comp in selectedKanji.compDetail"
                    :key="comp.w"
                    class="bg-gray-50 rounded-lg p-3 text-center"
                  >
                    <div class="text-2xl font-bold text-gray-900">
                      {{ comp.w }}
                    </div>
                    <div v-if="comp.h" class="text-sm text-gray-600">
                      {{ comp.h }}
                    </div>
                  </div>
                </div>
              </div>

              <div
                v-if="selectedKanji.examples && selectedKanji.examples.length > 0"
                class="space-y-3"
              >
                <h3
                  class="font-semibold text-gray-800 flex items-center space-x-2"
                >
                  <UIcon name="i-lucide-list" class="size-4" />
                  <span>Ví dụ</span>
                </h3>
                <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
                  <div
                    v-for="(example, idx) in selectedKanji.examples"
                    :key="idx"
                    class="bg-gray-50 rounded-lg p-4"
                  >
                    <div class="flex items-start justify-between mb-2">
                      <h4 class="font-semibold text-gray-900">
                        {{ example.w }}
                      </h4>
                      <span
                        class="text-xs bg-blue-100 text-blue-800 px-2 py-1 rounded-full"
                      >
                        {{ example.h }}
                      </span>
                    </div>
                    <p class="text-gray-800 mb-1">{{ example.m }}</p>
                    <p class="text-gray-600 text-sm">{{ example.p }}</p>
                  </div>
                </div>
              </div>

              <div v-if="selectedKanji.example_on" class="space-y-3">
                <h3
                  class="font-semibold text-gray-800 flex items-center space-x-2"
                >
                  <UIcon name="i-lucide-volume-2" class="size-4" />
                  <span>Ví dụ cách đọc (On)</span>
                </h3>
                <div
                  v-for="(examples, reading) in selectedKanji.example_on"
                  :key="reading"
                  class="space-y-2"
                >
                  <h4 class="font-medium text-gray-700">
                    {{ reading }}
                  </h4>
                  <div class="grid grid-cols-1 md:grid-cols-2 gap-2 ml-4">
                    <div
                      v-for="(example, idx) in examples"
                      :key="idx"
                      class="bg-blue-50 rounded p-3"
                    >
                      <div class="font-medium text-gray-900">
                        {{ example.w }}
                      </div>
                      <div class="text-gray-700 text-sm">{{ example.m }}</div>
                      <div class="text-gray-500 text-xs">{{ example.p }}</div>
                    </div>
                  </div>
                </div>
              </div>

              <div v-if="selectedKanji.example_kun" class="space-y-3">
                <h3
                  class="font-semibold text-gray-800 flex items-center space-x-2"
                >
                  <UIcon name="i-lucide-volume-1" class="size-4" />
                  <span>Ví dụ cách đọc (Kun)</span>
                </h3>
                <div
                  v-for="(examples, reading) in selectedKanji.example_kun"
                  :key="reading"
                  class="space-y-2"
                >
                  <h4 class="font-medium text-gray-700">
                    {{ reading }}
                  </h4>
                  <div class="grid grid-cols-1 md:grid-cols-2 gap-2 ml-4">
                    <div
                      v-for="(example, idx) in examples"
                      :key="idx"
                      class="bg-green-50 rounded p-3"
                    >
                      <div class="font-medium text-gray-900">
                        {{ example.w }}
                      </div>
                      <div class="text-gray-700 text-sm">{{ example.m }}</div>
                      <div class="text-gray-500 text-xs">{{ example.p }}</div>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
          <div v-else>
            <p class="text-gray-500">
              No Kanji information found for this word.
            </p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from "vue";
import ConjugationTable from "~/components/ConjugationTable.vue";
import conjugationsData from "~/data/conjugations_normalized.json";
import KanjiStrokeInResult from "./KanjiStrokeInResult.vue";

// --- Props and Emits ---
const props = defineProps<{
  searchWord: string;
}>();

defineEmits(["close"]);

// --- Local State for Modal ---
const result = ref<any | null>(null);
const conjugationResult = ref<any | null>(null);
const loading = ref(false);
const error = ref("");
const config = useRuntimeConfig();

// --- NEW: Tab State ---
const activeTab = ref<"word" | "kanji">("word");
const kanjiResults = ref<any[] | null>(null);
const activeKanjiIndex = ref(0);

// --- NEW: Computed prop for selected kanji ---
const selectedKanji = computed(() => {
  if (kanjiResults.value && kanjiResults.value[activeKanjiIndex.value]) {
    return kanjiResults.value[activeKanjiIndex.value];
  }
  return null;
});

// --- Duplicated Helper Functions (needed for local fetch) ---
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

// --- NEW: Helper to extract kanji from a string ---
const extractKanji = (text: string): string[] => {
  const kanjiRegex = /[\u4e00-\u9faf\u3400-\u4dbf]/g;
  const matches = text.match(kanjiRegex);
  // Return unique kanji
  return matches ? [...new Set(matches)] : [];
};

// --- Fetch Logic for WORD ---
const fetchWordData = async (word: string) => {
  try {
    const conjugation = checkConjugation(word);
    if (conjugation) {
      conjugationResult.value = conjugation;
    }

    const dictionaryForm = getDictionaryForm(word);
    const apiUrl = `${
      config.public.apiBaseUrl
    }/api/Word/GetWordJson/${encodeURIComponent(dictionaryForm)}`;
    const res = await fetch(apiUrl);
    if (!res.ok) throw new Error("Failed to fetch word");
    const response = await res.json();

    if (response.status === 200 && response.data) {
      result.value = {
        type: "word",
        ...response.data,
      };
    } else {
      throw new Error("Invalid word response format");
    }
  } catch (e: any) {
    console.error("Word fetch error:", e.message);
    error.value = e.message || "Error loading word data";
  }
};

// --- NEW: Fetch Logic for KANJI ---
const fetchKanjiData = async (word: string) => {
  try {
    const kanjiChars = extractKanji(word);
    if (kanjiChars.length === 0) {
      kanjiResults.value = [];
      return;
    }

    // Create a fetch promise for each kanji
    const fetchPromises = kanjiChars.map(async (char) => {
      const apiUrl = `${
        config.public.apiBaseUrl
      }/api/Kanji/GetKanjiJson/${encodeURIComponent(char)}`;
      const res = await fetch(apiUrl);
      if (!res.ok) throw new Error(`Failed to fetch kanji: ${char}`);
      const response = await res.json();

      if (
        response.status === 200 &&
        response.results &&
        response.results.length > 0
      ) {
        return response.results[0]; // Return the first (and likely only) result
      } else {
        throw new Error(`Invalid kanji response for: ${char}`);
      }
    });

    // Run all fetches in parallel
    const results = await Promise.allSettled(fetchPromises);

    // Filter out failed promises and store the successful ones
    const successfulResults = results
      .filter((r) => r.status === "fulfilled")
      .map((r) => (r as PromiseFulfilledResult<any>).value);

    kanjiResults.value = successfulResults;
  } catch (e: any) {
    console.error("Kanji fetch error:", e.message);
    // Don't set global error, word request might have succeeded
  }
};

// --- Fetch ALL data when component is created ---
onMounted(async () => {
  if (props.searchWord) {
    loading.value = true;
    error.value = "";

    // Run both word and kanji fetches at the same time
    await Promise.allSettled([
      fetchWordData(props.searchWord),
      fetchKanjiData(props.searchWord),
    ]);

    loading.value = false;

    // Set error only if word data failed (kanji is optional)
    if (!result.value) {
      error.value = "Failed to load word data.";
    }
  }
});
</script>

<style scoped>
.modal-scroll-content::-webkit-scrollbar {
  width: 8px;
}
.modal-scroll-content::-webkit-scrollbar-track {
  background: #ffffff; /* Track background (white) */
  border-radius: 10px;
}
.modal-scroll-content::-webkit-scrollbar-thumb {
  background-color: #d1d5db; /* gray-300 */
  border-radius: 10px;
  border: 2px solid #ffffff;
  background-clip: padding-box;
}
.modal-scroll-content::-webkit-scrollbar-thumb:hover {
  background-color: #9ca3af; /* gray-400 */
}
</style>