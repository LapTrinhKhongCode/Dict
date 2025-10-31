<template>
  <div class="space-y-6">
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

    <div
      v-if="result && (result.words || result.kanjiList)"
      class="grid grid-cols-1 md:grid-cols-12 gap-6"
    >
      <div class="md:col-span-4 lg:col-span-3 space-y-6">
        
        <div v-if="result.type === 'word' && result.words && result.words.length > 0">
          <h3 class="text-lg font-semibold mb-2">Main Results</h3>
          <ul class="space-y-2">
            <li
              v-for="word in result.words"
              :key="word._id"
              @click="selectedItem = word"
              class="p-3 rounded-lg cursor-pointer transition-colors border"
              :class="selectedItem && selectedItem._id === word._id
                ? 'bg-blue-50 border-blue-300'
                : 'bg-white border-transparent hover:bg-gray-100'"
            >
              <h4 class="font-bold text-gray-900">{{ word.word }}</h4>
              <p class="text-sm text-blue-600">{{ word.phonetic }}</p>
              <p class="text-sm text-gray-600 truncate">{{ word.short_mean }}</p>
            </li>
          </ul>
        </div>

        <div v-if="result.type === 'kanji' && result.kanjiList && result.kanjiList.length > 0">
          <h3 class="text-lg font-semibold text-gray-800 mb-2">Main Results</h3>
          <ul class="space-y-2">
            <li
              v-for="kanji in result.kanjiList"
              :key="kanji._id"
              @click="selectedItem = kanji"
              class="p-3 rounded-lg cursor-pointer transition-colors border flex items-center space-x-4"
              :class="selectedItem && selectedItem._id === kanji._id
                ? 'bg-blue-50 border-blue-300'
                : 'bg-white border-transparent hover:bg-gray-100'"
            >
              <h4 class="font-bold text-3xl text-gray-900">{{ kanji.kanji }}</h4>
              <p class="text-sm text-gray-600">{{ kanji.mean }}</p>
            </li>
          </ul>
        </div>
        
        <div
          v-if="result.type === 'word' && result.suggestWords && result.suggestWords.length > 0"
          class="space-y-4"
        >
          <h3 class="text-lg font-semibold mb-2">
            Suggested Words
          </h3>
          <div class="space-y-2">
            <div
              v-for="suggest in visibleSuggestions"
              :key="suggest._id"
              class="bg-white rounded-lg p-3 hover:bg-gray-100 transition-colors cursor-pointer"
              @click="selectSuggestedWord(suggest.word)"
            >
              <h4 class="font-semibold text-gray-900">{{ suggest.word }}</h4>
              <p class="text-sm text-blue-600">{{ suggest.phonetic }}</p>
            </div>
          </div>
          
          <button
            v-if="result.suggestWords && result.suggestWords.length > suggestionLimit"
            @click="showAllSuggestions = !showAllSuggestions"
            class="w-full text-sm font-medium text-blue-600 hover:text-blue-800 p-2 rounded-lg hover:bg-blue-50 transition-colors"
          >
            <span v-if="!showAllSuggestions">
              View More ({{ result.suggestWords.length - suggestionLimit }} more)
            </span>
            <span v-else>
              View Less
            </span>
          </button>
          </div>
      </div>

      <div class="md:col-span-8 lg:col-span-9 space-y-6">
        
        <div
          v-if="result.type === 'kanji' && selectedItem"
          class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 space-y-6"
        >
          <div class="space-y-3">
            <h1 class="text-6xl font-bold text-gray-900">
              {{ selectedItem.kanji }}
            </h1>
            <div class="flex items-center space-x-6">
              <div class="space-y-1">
                <span class="text-lg text-blue-600 font-medium">On: {{ selectedItem.on }}</span>
                <span class="text-lg text-green-600 font-medium">Kun: {{ selectedItem.kun }}</span>
              </div>
              <div class="space-y-1">
                <span class="text-sm text-gray-500">Strokes: {{ selectedItem.stroke_count }}</span>
                <span class="text-sm text-gray-500">Frequency: {{ selectedItem.freq }}</span>
              </div>
            </div>
            <div class="flex flex-wrap gap-2">
              <span
                v-for="level in selectedItem.level"
                :key="level"
                class="bg-purple-100 text-purple-800 text-sm px-3 py-1 rounded-full"
              >
                {{ level }}
              </span>
            </div>
          </div>
          <div class="space-y-2">
            <h3 class="font-semibold text-gray-800 flex items-center space-x-2">
              <UIcon name="i-lucide-book-open" class="size-4" />
              <span>Meaning</span>
            </h3>
            <p class="text-gray-800 text-lg">{{ selectedItem.mean }}</p>
            <div v-if="selectedItem.detail" class="space-y-3">
              <div
                v-for="(paragraph, index) in selectedItem.detail.split('##')"
                :key="index"
                class="bg-gray-50 rounded-lg p-4 border-l-4 border-blue-200"
              >
                <p class="text-gray-700 text-sm leading-relaxed">
                  {{ paragraph.trim() }}
                </p>
              </div>
            </div>
          </div>
          </div>

        <div
          v-if="result.type === 'word' && selectedItem"
          class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 space-y-4"
        >
          <div class="space-y-2">
            <h1 class="text-3xl font-bold text-gray-900">{{ selectedItem.word }}</h1>
            <div class="flex items-center space-x-4">
              <span class="text-lg text-blue-600 font-medium">{{ selectedItem.phonetic }}</span>
              <span v-if="selectedItem.short_mean" class="text-gray-600 italic">{{ selectedItem.short_mean }}</span>
            </div>
          </div>
          <div v-if="selectedItem.means && selectedItem.means.length > 0" class="space-y-3">
            <h3 class="font-semibold text-gray-800 flex items-center space-x-2">
              <UIcon name="i-lucide-book-open" class="size-4" />
              <span>Meanings</span>
            </h3>
            <div class="space-y-3">
              <div
                v-for="(meaning, idx) in selectedItem.means"
                :key="idx"
                class="bg-gray-50 rounded-lg p-4"
              >
                <div class="flex items-start justify-between mb-2">
                  <p class="text-gray-800 font-medium">{{ meaning.mean }}</p>
                  <span
                    v-if="meaning.kind"
                    class="text-xs bg-blue-100 text-blue-800 px-2 py-1 rounded-full"
                  >
                    {{ meaning.kind }}
                  </span>
                </div>
                <div v-if="meaning.examples && meaning.examples.length > 0" class="mt-3 space-y-2">
                  <h4 class="text-sm font-medium text-gray-600">Examples:</h4>
                  <div
                    v-for="(example, exIdx) in meaning.examples"
                    :key="exIdx"
                    class="bg-white rounded p-3 border-l-4 border-blue-200"
                  >
                    <p class="text-gray-800 mb-1">{{ example.content }}</p>
                    <p class="text-gray-600 text-sm italic">{{ example.mean }}</p>
                    <p v-if="example.transcription" class="text-gray-500 text-xs mt-1">
                      {{ example.transcription }}
                    </p>
                  </div>
                </div>
              </div>
            </div>
          </div>
          <div v-if="selectedItem.synsets && selectedItem.synsets.length > 0" class="space-y-2">
            <h3 class="font-semibold text-gray-800 flex items-center space-x-2">
              <UIcon name="i-lucide-link" class="size-4" />
              <span>Synonyms</span>
            </h3>
            <div class="flex flex-wrap gap-2">
              <span
                v-for="synonym in selectedItem.synsets[0]?.entry[0]?.synonym || []"
                :key="synonym"
                class="bg-green-100 text-green-800 text-sm px-3 py-1 rounded-full cursor-pointer"
                @click="selectSynonym(synonym)"
              >
                {{ synonym }}
              </span>
            </div>
          </div>
          <div v-if="selectedItem.images && selectedItem.images.length > 0" class="space-y-3">
            <h3 class="font-semibold text-gray-800 flex items-center space-x-2">
              <UIcon name="i-lucide-image" class="size-4" />
              <span>Images ({{ selectedItem.images.length }})</span>
            </h3>
            <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-3">
              <div
                v-for="(image, index) in selectedItem.images"
                :key="index"
                class="relative group"
              >
                <img
                  :src="image"
                  :alt="`${selectedItem.word} - Image ${index + 1}`"
                  class="w-full h-24 object-cover rounded-lg border hover:scale-105 transition-transform cursor-pointer"
                  @error="$event.target.style.display = 'none'"
                />
                <div
                  class="absolute top-1 right-1 bg-black bg-opacity-50 text-white text-xs px-1 py-0.5 rounded text-center min-w-[20px]"
                >
                  {{ index + 1 }}
                </div>
              </div>
            </div>
          </div>
        </div>

        <div v-if="conjugationResult" class="space-y-6">
          <h2 class="text-xl font-semibold border-b pb-2">Verb Conjugation</h2>
          <ConjugationTable
            :root="conjugationResult.root"
            :conjugations="conjugationResult.conjugations"
            :originalForm="conjugationResult.originalForm"
          />
        </div>

      </div>
    </div>
    <div
      v-if="hasSearched && !result && !conjugationResult && !loading && !error"
      class="text-center py-12"
    >
      <UIcon
        name="i-lucide-search-x"
        class="size-12 text-gray-400 mx-auto mb-4"
      />
      <p class="text-gray-500 text-lg">
        No results found for "{{ originalSearchWord }}"
      </p>
      <p class="text-gray-400 text-sm mt-2">Try a different search term</p>
    </div>

    <WordResultModal
      v-if="showSuggestedWordModal"
      :search-word="modalSearchWord"
      @close="showSuggestedWordModal = false"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch, computed } from "vue"; // <-- IMPORTED computed
import ConjugationTable from "~/components/ConjugationTable.vue";
import WordResultModal from "~/components/WordResultModal.vue";

// --- Props ---
const props = defineProps({
  result: {
    type: Object as () => any | null,
    default: null,
  },
  conjugationResult: {
    type: Object as () => any | null,
    default: null,
  },
  loading: {
    type: Boolean,
    default: false,
  },
  error: {
    type: String,
    default: "",
  },
  originalSearchWord: {
    type: String,
    default: "",
  },
  hasSearched: {
    type: Boolean,
    default: false,
  },
});

// --- NEW: Local state for selected item ---
const selectedItem = ref<any | null>(null);

// --- NEW: State for suggested words toggle ---
const showAllSuggestions = ref(false);
const suggestionLimit = 6;

// --- NEW: Computed property for visible suggestions ---
const visibleSuggestions = computed(() => {
  if (!props.result || !props.result.suggestWords) return [];
  if (showAllSuggestions.value) {
    return props.result.suggestWords;
  }
  return props.result.suggestWords.slice(0, suggestionLimit);
});

// --- NEW: Watcher to select first item ---
watch(
  () => props.result,
  (newResult) => {
    showAllSuggestions.value = false; // <-- RESET toggle on new search
    if (newResult && newResult.type === 'word' && newResult.words && newResult.words.length > 0) {
      selectedItem.value = newResult.words[0];
    } else if (newResult && newResult.type === 'kanji' && newResult.kanjiList && newResult.kanjiList.length > 0) {
      selectedItem.value = newResult.kanjiList[0];
    } else {
      selectedItem.value = null;
    }
  },
  { immediate: true, deep: true } // 'immediate' runs it on load
);


// --- Modal State (unchanged) ---
const showSuggestedWordModal = ref(false);
const modalSearchWord = ref("");

// --- Methods (unchanged) ---
const selectSuggestedWord = (word: string) => {
  modalSearchWord.value = word;
  showSuggestedWordModal.value = true;
};

const selectSynonym = (word: string) => {
  modalSearchWord.value = word;
  showSuggestedWordModal.value = true;
};
</script>