<template>
  <div 
    class="fixed inset-0 z-40 bg-black bg-opacity-20 flex items-center justify-center" 
    @click.self="$emit('close')"
  >
    <div class="relative bg-white rounded-xl shadow-2xl w-full max-w-3xl h-[85vh] flex flex-col">
      <button 
        @click="$emit('close')" 
        class="absolute -top-3 -right-3 z-50 bg-gray-800 text-white rounded-full p-1.5 hover:bg-gray-600 transition"
      >
        <UIcon name="i-lucide-x" class="size-5" />
      </button>

      <div class="flex-1 overflow-y-auto p-6 space-y-8 modal-scroll-content">
        
        <div v-if="loading" class="flex items-center justify-center py-12">
          <div class="text-gray-500 flex items-center space-x-2">
            <div class="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-500"></div>
            <span>Loading...</span>
          </div>
        </div>

        <div v-if="error" class="bg-red-50 border border-red-200 rounded-lg p-4">
          <div class="flex items-center space-x-2">
            <UIcon name="i-lucide-alert-circle" class="text-red-500 size-5" />
            <span class="text-red-700">{{ error }}</span>
          </div>
        </div>

        <div v-if="result && result.type === 'word' && result.words && result.words.length > 0" class="space-y-6">
          <div v-for="word in result.words" :key="word._id" class="space-y-4">
            <div class="space-y-2">
              <h1 class="text-3xl font-bold text-gray-900">{{ word.word }}</h1>
              <div class="flex items-center space-x-4">
                <span class="text-lg text-blue-600 font-medium">{{ word.phonetic }}</span>
                <span v-if="word.short_mean" class="text-gray-600 italic">{{ word.short_mean }}</span>
              </div>
            </div>

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

            <div v-if="word.synsets && word.synsets.length > 0" class="space-y-2">
              <h3 class="font-semibold text-gray-800 flex items-center space-x-2">
                <UIcon name="i-lucide-link" class="size-4" />
                <span>Synonyms</span>
              </h3>
              <div class="flex flex-wrap gap-2">
                <span v-for="synonym in word.synsets[0]?.entry[0]?.synonym || []" :key="synonym"
                  class="bg-green-100 text-green-800 text-sm px-3 py-1 rounded-full">
                  {{ synonym }}
                </span>
              </div>
            </div>

            <div v-if="word.images && word.images.length > 0" class="space-y-3">
              <h3 class="font-semibold text-gray-800 flex items-center space-x-2">
                <UIcon name="i-lucide-image" class="size-4" />
                <span>Images ({{ word.images.length }})</span>
              </h3>
              <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-3">
                <div v-for="(image, index) in word.images" :key="index" class="relative group">
                  <img :src="image" :alt="`${word.word} - Image ${index + 1}`"
                    class="w-full h-24 object-cover rounded-lg border hover:scale-105 transition-transform cursor-pointer"
                    @error="$event.target.style.display = 'none'" />
                  <div
                    class="absolute top-1 right-1 bg-black bg-opacity-50 text-white text-xs px-1 py-0.5 rounded text-center min-w-[20px]">
                    {{ index + 1 }}
                  </div>
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
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import ConjugationTable from "~/components/ConjugationTable.vue";
import conjugationsData from "~/data/conjugations_normalized.json";

// --- Props and Emits ---
const props = defineProps<{
  searchWord: string
}>();

defineEmits(['close']);

// --- Local State for Modal ---
const result = ref<any | null>(null);
const conjugationResult = ref<any | null>(null);
const loading = ref(false);
const error = ref("");

// --- Duplicated Helper Functions (needed for local fetch) ---
const extractWordLeftOfSlash = (word: string): string => {
  const slashIndex = word.indexOf('/');
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
          originalForm: trimmed !== dictionaryForm ? trimmed : null
        };
      }
    }
  }
  return null;
};

// --- Local Fetch Logic for Modal ---
const fetchModalWord = async (word: string) => {
  try {
    loading.value = true;
    error.value = "";
    result.value = null;
    conjugationResult.value = null;

    // We only need the "word" logic path from your main page
    const conjugation = checkConjugation(word);
    if (conjugation) {
      conjugationResult.value = conjugation;
    }
    
    const dictionaryForm = getDictionaryForm(word);
    
    const apiUrl = `https://localhost:7084/api/Word/GetWordJson/${encodeURIComponent(dictionaryForm)}`;
    const res = await fetch(apiUrl);
    if (!res.ok) throw new Error("Failed to fetch word");
    const response = await res.json();
    
    if (response.status === 200 && response.data) {
      result.value = { 
        type: 'word',
        ...response.data 
      };
    } else {
      throw new Error("Invalid response format");
    }
  } catch (e: any) {
    error.value = e.message || "Error loading data";
  } finally {
    loading.value = false;
  }
};

// --- Fetch data when component is created ---
onMounted(() => {
  if (props.searchWord) {
    fetchModalWord(props.searchWord);
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