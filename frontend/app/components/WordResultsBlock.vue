<template>
  <div class="grid grid-cols-1 md:grid-cols-12 gap-6">
    
    <div class="md:col-span-4 lg:col-span-3 space-y-6">
      <div v-if="result && result.type === 'word' && result.words && result.words.length > 0">
        <h3 class="text-lg font-semibold text-gray-800 mb-2">Search Results</h3>
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

      <div v-if="result && result.type === 'kanji' && result.kanjiList && result.kanjiList.length > 0">
        <h3 class="text-lg font-semibold text-gray-800 mb-2">Search Results</h3>
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

      <div v-if="result && result.suggestWords && result.suggestWords.length > 0">
        <h3 class="text-lg font-semibold text-gray-800 mb-2">Suggested Words</h3>
        <div class="space-y-2">
          <div
            v-for="suggest in result.suggestWords"
            :key="suggest._id"
            class="bg-white rounded-lg p-3 hover:bg-gray-100 transition-colors cursor-pointer"
            @click="selectSuggestedWord(suggest.word)"
          >
            <h4 class="font-semibold text-gray-900">{{ suggest.word }}</h4>
            <p class="text-sm text-blue-600">{{ suggest.phonetic }}</p>
          </div>
        </div>
      </div>
    </div>

    <div class="md:col-span-8 lg:col-span-9 space-y-6">
      
      <div v-if="selectedItem && result.type === 'word'" class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 space-y-6">
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
            <div v-for="(meaning, idx) in selectedItem.means" :key="idx" class="bg-gray-50 rounded-lg p-4">
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

        <div v-if="selectedItem.synsets && selectedItem.synsets.length > 0" class="space-y-2">
          <h3 class="font-semibold text-gray-800 flex items-center space-x-2">
            <UIcon name="i-lucide-link" class="size-4" />
            <span>Synonyms</span>
          </h3>
          <div class="flex flex-wrap gap-2">
            <span v-for="synonym in selectedItem.synsets[0]?.entry[0]?.synonym || []" :key="synonym"
              @click="selectSynonym(synonym)"
              class="bg-green-100 text-green-800 text-sm px-3 py-1 rounded-full cursor-pointer hover:bg-green-200">
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
            <div v-for="(image, index) in selectedItem.images" :key="index" class="relative group">
              <img :src="image" :alt="`${selectedItem.word} - Image ${index + 1}`"
                class="w-full h-24 object-cover rounded-lg border hover:scale-105 transition-transform cursor-pointer"
                @error="$event.target.style.display = 'none'" />
            </div>
          </div>
        </div>
      </div>

      <div v-if="selectedItem && result.type === 'kanji'" class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 space-y-4">
        <h1 class="text-6xl font-bold text-gray-900 mb-4">{{ selectedItem.kanji }}</h1>
        <div class="space-y-1">
          <p class="text-lg"><span class="font-semibold text-gray-700">Meaning:</span> {{ selectedItem.mean }}</p>
          <p class="text-lg"><span class="font-semibold text-gray-700">Onyomi:</span> {{ selectedItem.onyomi }}</p>
          <p class="text-lg"><span class="font-semibold text-gray-700">Kunyomi:</span> {{ selectedItem.kunyomi }}</p>
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
</template>

<script setup lang="ts">
import { ref, watch } from "vue";
import ConjugationTable from "~/components/ConjuggationTable.vue";

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
});

// --- Emits ---
const emit = defineEmits(["select-word", "select-synonym"]);

// --- NEW: Local state for selected item ---
const selectedItem = ref<any | null>(null);

// --- NEW: Watcher to select first item ---
watch(
  () => props.result,
  (newResult) => {
    if (newResult && newResult.type === 'word' && newResult.words && newResult.words.length > 0) {
      selectedItem.value = newResult.words[0];
    } else if (newResult && newResult.type === 'kanji' && newResult.kanjiList && newResult.kanjiList.length > 0) {
      selectedItem.value = newResult.kanjiList[0];
    } else {
      selectedItem.value = null;
    }
  },
  { immediate: true, deep: true } // 'immediate' runs it on load, 'deep' watches inside the object
);

// --- Event Handlers ---
const selectSuggestedWord = (word: string) => {
  emit("select-word", word);
};

const selectSynonym = (word: string) => {
  emit("select-synonym", word);
};
</script>