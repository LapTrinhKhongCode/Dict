<template>
    <div class="space-y-6">
      <div
        v-if="result && result.words && result.words.length > 0"
        class="space-y-6"
      >
        <h2 class="text-xl font-semibold border-b pb-2">Main Results</h2>
        <div
          v-for="word in result.words"
          :key="word._id"
          class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 space-y-4"
        >
          <div class="space-y-2">
            <h1 class="text-3xl font-bold text-gray-900">{{ word.word }}</h1>
            </div>
          <div v-if="word.means && word.means.length > 0" class="space-y-3">
            </div>
          <div v-if="word.synsets && word.synsets.length > 0" class="space-y-2">
            <h3 class="font-semibold text-gray-800 flex items-center space-x-2">
              <UIcon name="i-lucide-link" class="size-4" />
              <span>Synonyms</span>
            </h3>
            <div class="flex flex-wrap gap-2">
              <span
                v-for="synonym in word.synsets[0]?.entry[0]?.synonym || []"
                :key="synonym"
                class="bg-green-100 text-green-800 text-sm px-3 py-1 rounded-full cursor-pointer"
                @click="selectSynonym(synonym)"
              >
                {{ synonym }}
              </span>
            </div>
          </div>
          <div v-if="word.images && word.images.length > 0" class="space-y-3">
            </div>
        </div>
      </div>
  
      <div
        v-if="result && result.suggestWords && result.suggestWords.length > 0"
        class="space-y-4"
      >
        <h2 class="text-xl font-semibold text-gray-800 border-b pb-2">
          Suggested Words
        </h2>
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          <div
            v-for="suggest in result.suggestWords"
            :key="suggest._id"
            class="bg-white rounded-lg shadow-sm border border-gray-200 p-4 hover:shadow-md transition-shadow cursor-pointer"
            @click="selectSuggestedWord(suggest.word)"
          >
            <div class="space-y-2">
              <h3 class="font-semibold text-lg text-gray-900">
                {{ suggest.word }}
              </h3>
              <p class="text-blue-600 font-medium">{{ suggest.phonetic }}</p>
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
  </template>
  
  <script setup lang="ts">
  // This component just displays data and emits events
  import ConjugationTable from "~/components/ConjugationTable.vue";
  
  // --- Props ---
  defineProps({
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
  // We emit these events so the parent (SearchResult.vue) can open the modal
  const emit = defineEmits(["select-word", "select-synonym"]);
  
  const selectSuggestedWord = (word: string) => {
    emit("select-word", word);
  };
  
  const selectSynonym = (word: string) => {
    emit("select-synonym", word);
  };
  </script>