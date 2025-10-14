<template>
  <div class="p-6 space-y-6">
    <!-- Search bar -->
    <div class="flex items-center border rounded-2xl px-3 py-2 w-full max-w-md">
      <input
        v-model="searchWord"
        type="text"
        placeholder="Enter word..."
        class="flex-grow bg-transparent outline-none text-base"
        @keyup.enter="goSearch"
      />
      <button
        @click="goSearch"
        class="text-gray-500 hover:text-gray-700 transition"
      >
        <UIcon name="i-lucide-search" class="size-5" />
      </button>
    </div>

    <!-- Loading state -->
    <div v-if="loading" class="flex items-center justify-center py-12">
      <div class="text-gray-500 flex items-center space-x-2">
        <div class="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-500"></div>
        <span>Loading...</span>
      </div>
    </div>

    <!-- Error state -->
    <div v-if="error" class="bg-red-50 border border-red-200 rounded-lg p-4">
      <div class="flex items-center space-x-2">
        <UIcon name="i-lucide-alert-circle" class="text-red-500 size-5" />
        <span class="text-red-700">{{ error }}</span>
      </div>
    </div>

    <!-- Search Results -->
    <div v-if="result" class="space-y-8">
      <!-- Kanji Results -->
      <div v-if="result.type === 'kanji' && result.kanji" class="space-y-6">
        <h2 class="text-xl font-semibold border-b pb-2">Kanji Result</h2>
        
        <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 space-y-6">
          <!-- Kanji Header -->
          <div class="space-y-3">
            <h1 class="text-6xl font-bold text-gray-900">{{ result.kanji.kanji }}</h1>
            <div class="flex items-center space-x-6">
              <div class="space-y-1">
                <span class="text-lg text-blue-600 font-medium">On: {{ result.kanji.on }}</span>
                <span class="text-lg text-green-600 font-medium">Kun: {{ result.kanji.kun }}</span>
              </div>
              <div class="space-y-1">
                <span class="text-sm text-gray-500">Strokes: {{ result.kanji.stroke_count }}</span>
                <span class="text-sm text-gray-500">Frequency: {{ result.kanji.freq }}</span>
              </div>
            </div>
            <div class="flex flex-wrap gap-2">
              <span v-for="level in result.kanji.level" :key="level" 
                    class="bg-purple-100 text-purple-800 text-sm px-3 py-1 rounded-full">
                {{ level }}
              </span>
            </div>
          </div>

          <!-- Meaning -->
          <div class="space-y-2">
            <h3 class="font-semibold text-gray-800 flex items-center space-x-2">
              <UIcon name="i-lucide-book-open" class="size-4" />
              <span>Meaning</span>
            </h3>
            <p class="text-gray-800 text-lg">{{ result.kanji.mean }}</p>
            <div v-if="result.kanji.detail" class="space-y-3">
              <div v-for="(paragraph, index) in result.kanji.detail.split('##')" :key="index" 
                   class="bg-gray-50 rounded-lg p-4 border-l-4 border-blue-200">
                <p class="text-gray-700 text-sm leading-relaxed">{{ paragraph.trim() }}</p>
              </div>
            </div>
          </div>

          <!-- Tips -->
          <div v-if="result.kanji.tips && result.kanji.tips.vi" class="space-y-2">
            <h3 class="font-semibold text-gray-800 flex items-center space-x-2">
              <UIcon name="i-lucide-lightbulb" class="size-4" />
              <span>Memory Tip</span>
            </h3>
            <div class="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
              <p class="text-gray-800" v-html="result.kanji.tips.vi"></p>
            </div>
          </div>

          <!-- Components -->
          <div v-if="result.kanji.compDetail && result.kanji.compDetail.length > 0" class="space-y-3">
            <h3 class="font-semibold text-gray-800 flex items-center space-x-2">
              <UIcon name="i-lucide-puzzle" class="size-4" />
              <span>Components</span>
            </h3>
            <div class="flex flex-wrap gap-3">
              <div v-for="comp in result.kanji.compDetail" :key="comp.w" 
                   class="bg-gray-50 rounded-lg p-3 text-center">
                <div class="text-2xl font-bold text-gray-900">{{ comp.w }}</div>
                <div v-if="comp.h" class="text-sm text-gray-600">{{ comp.h }}</div>
              </div>
            </div>
          </div>

          <!-- Examples -->
          <div v-if="result.kanji.examples && result.kanji.examples.length > 0" class="space-y-3">
            <h3 class="font-semibold text-gray-800 flex items-center space-x-2">
              <UIcon name="i-lucide-list" class="size-4" />
              <span>Examples</span>
            </h3>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div v-for="(example, idx) in result.kanji.examples" :key="idx" 
                   class="bg-gray-50 rounded-lg p-4">
                <div class="flex items-start justify-between mb-2">
                  <h4 class="font-semibold text-gray-900">{{ example.w }}</h4>
                  <span class="text-xs bg-blue-100 text-blue-800 px-2 py-1 rounded-full">
                    {{ example.h }}
                  </span>
                </div>
                <p class="text-gray-800 mb-1">{{ example.m }}</p>
                <p class="text-gray-600 text-sm">{{ example.p }}</p>
              </div>
            </div>
          </div>

          <!-- On Reading Examples -->
          <div v-if="result.kanji.example_on" class="space-y-3">
            <h3 class="font-semibold text-gray-800 flex items-center space-x-2">
              <UIcon name="i-lucide-volume-2" class="size-4" />
              <span>On Reading Examples</span>
            </h3>
            <div v-for="(examples, reading) in result.kanji.example_on" :key="reading" class="space-y-2">
              <h4 class="font-medium text-gray-700">{{ reading }} reading:</h4>
              <div class="grid grid-cols-1 md:grid-cols-2 gap-2 ml-4">
                <div v-for="(example, idx) in examples" :key="idx" 
                     class="bg-blue-50 rounded p-3">
                  <div class="font-medium text-gray-900">{{ example.w }}</div>
                  <div class="text-gray-700 text-sm">{{ example.m }}</div>
                  <div class="text-gray-500 text-xs">{{ example.p }}</div>
                </div>
              </div>
            </div>
          </div>

          <!-- Kun Reading Examples -->
          <div v-if="result.kanji.example_kun" class="space-y-3">
            <h3 class="font-semibold text-gray-800 flex items-center space-x-2">
              <UIcon name="i-lucide-volume-1" class="size-4" />
              <span>Kun Reading Examples</span>
            </h3>
            <div v-for="(examples, reading) in result.kanji.example_kun" :key="reading" class="space-y-2">
              <h4 class="font-medium text-gray-700">{{ reading }} reading:</h4>
              <div class="grid grid-cols-1 md:grid-cols-2 gap-2 ml-4">
                <div v-for="(example, idx) in examples" :key="idx" 
                     class="bg-green-50 rounded p-3">
                  <div class="font-medium text-gray-900">{{ example.w }}</div>
                  <div class="text-gray-700 text-sm">{{ example.m }}</div>
                  <div class="text-gray-500 text-xs">{{ example.p }}</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Main Word Results -->
      <div v-if="result.type === 'word' && result.words && result.words.length > 0" class="space-y-6">
        <h2 class="text-xl font-semibold border-b pb-2">Main Results</h2>
        
        <div v-for="word in result.words" :key="word._id" class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 space-y-4">
          <!-- Word Header -->
          <div class="space-y-2">
            <h1 class="text-3xl font-bold text-gray-900">{{ word.word }}</h1>
            <div class="flex items-center space-x-4">
              <span class="text-lg text-blue-600 font-medium">{{ word.phonetic }}</span>
              <span v-if="word.short_mean" class="text-gray-600 italic">{{ word.short_mean }}</span>
            </div>
          </div>

          <!-- Meanings -->
          <div v-if="word.means && word.means.length > 0" class="space-y-3">
            <h3 class="font-semibold text-gray-800 flex items-center space-x-2">
              <UIcon name="i-lucide-book-open" class="size-4" />
              <span>Meanings</span>
            </h3>
            <div class="space-y-3">
              <div v-for="(meaning, idx) in word.means" :key="idx" class="bg-gray-50 rounded-lg p-4">
                <div class="flex items-start justify-between mb-2">
                  <p class="text-gray-800 font-medium">{{ meaning.mean }}</p>
                  <span v-if="meaning.kind" class="text-xs bg-blue-100 text-blue-800 px-2 py-1 rounded-full">
                    {{ meaning.kind }}
                  </span>
                </div>
                
                <!-- Examples -->
                <div v-if="meaning.examples && meaning.examples.length > 0" class="mt-3 space-y-2">
                  <h4 class="text-sm font-medium text-gray-600">Examples:</h4>
                  <div v-for="(example, exIdx) in meaning.examples" :key="exIdx" class="bg-white rounded p-3 border-l-4 border-blue-200">
                    <p class="text-gray-800 mb-1">{{ example.content }}</p>
                    <p class="text-gray-600 text-sm italic">{{ example.mean }}</p>
                    <p v-if="example.transcription" class="text-gray-500 text-xs mt-1">{{ example.transcription }}</p>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Synonyms -->
          <div v-if="word.synsets && word.synsets.length > 0" class="space-y-2">
            <h3 class="font-semibold text-gray-800 flex items-center space-x-2">
              <UIcon name="i-lucide-link" class="size-4" />
              <span>Synonyms</span>
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

          <!-- Images -->
          <div v-if="word.images && word.images.length > 0" class="space-y-3">
            <h3 class="font-semibold text-gray-800 flex items-center space-x-2">
              <UIcon name="i-lucide-image" class="size-4" />
              <span>Images ({{ word.images.length }})</span>
            </h3>
            <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-3">
              <div 
                v-for="(image, index) in word.images" 
                :key="index"
                class="relative group"
              >
                <img 
                  :src="image" 
                  :alt="`${word.word} - Image ${index + 1}`"
                  class="w-full h-24 object-cover rounded-lg border hover:scale-105 transition-transform cursor-pointer"
                  @error="$event.target.style.display='none'"
                />
                <div class="absolute top-1 right-1 bg-black bg-opacity-50 text-white text-xs px-1 py-0.5 rounded text-center min-w-[20px]">
                  {{ index + 1 }}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Suggested Words -->
      <div v-if="result.type === 'word' && result.suggestWords && result.suggestWords.length > 0" class="space-y-4">
        <h2 class="text-xl font-semibold text-gray-800 border-b pb-2">Suggested Words</h2>
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          <div 
            v-for="suggest in result.suggestWords" 
            :key="suggest._id"
            class="bg-white rounded-lg shadow-sm border border-gray-200 p-4 hover:shadow-md transition-shadow cursor-pointer"
            @click="selectSuggestedWord(suggest.word)"
          >
      <div class="space-y-2">
              <h3 class="font-semibold text-lg text-gray-900">{{ suggest.word }}</h3>
              <p class="text-blue-600 font-medium">{{ suggest.phonetic }}</p>
              <p class="text-gray-600 text-sm italic">{{ suggest.short_mean }}</p>
              <div v-if="suggest.means && suggest.means.length > 0" class="space-y-1">
                <div v-for="(meaning, idx) in suggest.means" :key="idx" class="text-sm">
                  <span class="text-gray-800">{{ meaning.mean }}</span>
                  <span v-if="meaning.kind" class="text-xs bg-gray-100 text-gray-600 px-2 py-0.5 rounded ml-2">
                    {{ meaning.kind }}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Conjugation Results -->
      <div v-if="conjugationResult" class="space-y-6">
        <h2 class="text-xl font-semibold border-b pb-2">Verb Conjugation</h2>
        <ConjugationTable 
          :root="conjugationResult.root" 
          :conjugations="conjugationResult.conjugations"
          :originalForm="conjugationResult.originalForm"
        />
      </div>

      <!-- No Results -->
      <div v-if="!result && !conjugationResult && !loading && !error" 
           class="text-center py-12">
        <UIcon name="i-lucide-search-x" class="size-12 text-gray-400 mx-auto mb-4" />
        <p class="text-gray-500 text-lg">No results found for "{{ searchWord }}"</p>
        <p class="text-gray-400 text-sm mt-2">Try a different search term</p>
      </div>
    </div>

  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import ConjugationTable from "~/components/ConjugationTable.vue";
import conjugationsData from "~/data/conjugations_normalized.json";

const route = useRoute();
const router = useRouter();

const searchWord = ref((route.query.word as string) || (route.query.kanji as string) || "");
const result = ref<any | null>(null);
const loading = ref(false);
const error = ref("");
const conjugationResult = ref<any | null>(null);

// Image modal state
const showImageModal = ref(false);
const showFullImageModal = ref(false);
const currentImages = ref<string[]>([]);
const currentWord = ref("");
const fullImageUrl = ref("");
const fullImageIndex = ref(0);

// Check if input is a single kanji character
const isSingleKanji = (text: string): boolean => {
  // Remove whitespace and check if it's exactly one character
  const trimmed = text.trim();
  if (trimmed.length !== 1) return false;
  
  // Check if it's a kanji character (CJK Unified Ideographs range)
  const charCode = trimmed.charCodeAt(0);
  return (charCode >= 0x4E00 && charCode <= 0x9FFF) || 
         (charCode >= 0x3400 && charCode <= 0x4DBF) || 
         (charCode >= 0x20000 && charCode <= 0x2A6DF);
};

// Extract word left of "/" if it exists
const extractWordLeftOfSlash = (word: string): string => {
  const slashIndex = word.indexOf('/');
  return slashIndex !== -1 ? word.substring(0, slashIndex) : word;
};

// Convert any form to dictionary form using byForm
const getDictionaryForm = (word: string): string => {
  const trimmed = word.trim();
  if (!trimmed) return trimmed;
  
  // Check if the word exists in byForm data
  if (conjugationsData.byForm && conjugationsData.byForm[trimmed]) {
    const dictionaryFormWithSlash = conjugationsData.byForm[trimmed];
    // Extract word left of "/" from the dictionary form
    return extractWordLeftOfSlash(dictionaryFormWithSlash);
  }
  
  // If no byForm match, return the original word (also extract left of "/" if needed)
  return extractWordLeftOfSlash(trimmed);
};

// Check if word has conjugation data
const checkConjugation = (word: string): any | null => {
  const trimmed = word.trim();
  if (!trimmed) return null;
  
  // First, convert to dictionary form using byForm
  const dictionaryForm = getDictionaryForm(trimmed);
  
  // For normalized data, we need to find the byRoot key that matches our dictionary form
  // The byRoot keys have "/" format, so we need to find the one that starts with our dictionary form
  if (conjugationsData.byRoot) {
    // Look for a byRoot key that starts with our dictionary form + "/"
    const targetKey = `${dictionaryForm}/`;
    
    for (const [key, conjugations] of Object.entries(conjugationsData.byRoot)) {
      if (key.startsWith(targetKey)) {
        return {
          root: dictionaryForm,
          conjugations: conjugations,
          originalForm: trimmed !== dictionaryForm ? trimmed : null
        };
      }
    }
  }
  
  return null;
};

// Fetch API
const fetchWord = async (word: string) => {
  try {
    loading.value = true;
    error.value = "";
    result.value = null;
    conjugationResult.value = null;

    let apiUrl: string;
    let response: any;

    if (isSingleKanji(word)) {
      // Use kanji API for single kanji
      apiUrl = `https://localhost:7084/api/Kanji/GetKanjiJson/${encodeURIComponent(word)}`;
      const res = await fetch(apiUrl);
      if (!res.ok) throw new Error("Failed to fetch kanji");
      response = await res.json();
      
      // Handle kanji API response structure
      if (response.status === 200 && response.results && response.results.length > 0) {
        result.value = { 
          type: 'kanji',
          kanji: response.results[0] 
        };
      } else {
        throw new Error("No kanji results found");
      }
    } else {
      // Check for conjugation data first
      const conjugation = checkConjugation(word);
      if (conjugation) {
        conjugationResult.value = conjugation;
      }
      
      // Use dictionary form for API call (API only accepts dictionary forms)
      const dictionaryForm = getDictionaryForm(word);
      
      // Use word API for regular words
      apiUrl = `https://localhost:7084/api/Word/GetWordJson/${encodeURIComponent(dictionaryForm)}`;
      const res = await fetch(apiUrl);
      if (!res.ok) throw new Error("Failed to fetch word");
      response = await res.json();
      
      // Handle word API response structure
      if (response.status === 200 && response.data) {
        result.value = { 
          type: 'word',
          ...response.data 
        };
      } else {
        throw new Error("Invalid response format");
      }
    }
  } catch (e: any) {
    error.value = e.message || "Error loading data";
  } finally {
    loading.value = false;
  }
};

// Trigger search
const goSearch = () => {
  if (!searchWord.value.trim()) return;
  
  const trimmedWord = searchWord.value.trim();
  const queryParam = isSingleKanji(trimmedWord) ? 'kanji' : 'word';
  
  router.push({ path: "/", query: { [queryParam]: trimmedWord } });
  fetchWord(trimmedWord);
};

// Select suggested word
const selectSuggestedWord = (word: string) => {
  searchWord.value = word;
  goSearch();
};

// Image modal functions
const openImageModal = (images: string[], word: string) => {
  currentImages.value = images;
  currentWord.value = word;
  showImageModal.value = true;
};

const closeImageModal = () => {
  showImageModal.value = false;
  currentImages.value = [];
  currentWord.value = "";
};

const openFullImageModal = (imageUrl: string, index: number) => {
  fullImageUrl.value = imageUrl;
  fullImageIndex.value = index;
  showFullImageModal.value = true;
};

const closeFullImageModal = () => {
  showFullImageModal.value = false;
  fullImageUrl.value = "";
  fullImageIndex.value = 0;
};

const previousImage = () => {
  if (fullImageIndex.value > 0) {
    fullImageIndex.value--;
    fullImageUrl.value = currentImages.value[fullImageIndex.value];
  }
};

const nextImage = () => {
  if (fullImageIndex.value < currentImages.value.length - 1) {
    fullImageIndex.value++;
    fullImageUrl.value = currentImages.value[fullImageIndex.value];
  }
};

// Keyboard navigation for full image modal
const handleKeydown = (event: KeyboardEvent) => {
  if (showFullImageModal.value) {
    if (event.key === 'Escape') {
      closeFullImageModal();
    } else if (event.key === 'ArrowLeft') {
      previousImage();
    } else if (event.key === 'ArrowRight') {
      nextImage();
    }
  }
};

// On first load, fetch if URL has ?word=
onMounted(() => {
  if (searchWord.value) {
    fetchWord(searchWord.value);
  }
  // Add keyboard event listener
  document.addEventListener('keydown', handleKeydown);
});

// Clean up event listener
onBeforeUnmount(() => {
  document.removeEventListener('keydown', handleKeydown);
});

// If user manually changes query (?word=xxx or ?kanji=xxx), react to it
watch(
  () => [route.query.word, route.query.kanji],
  ([newWord, newKanji]) => {
    const queryValue = (typeof newWord === "string" ? newWord : newKanji) as string;
    if (queryValue && queryValue !== searchWord.value) {
      searchWord.value = queryValue;
      fetchWord(queryValue);
    }
  }
);
</script>
