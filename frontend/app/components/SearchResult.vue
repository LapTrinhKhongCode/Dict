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
      v-if="result && result.type === 'kanji' && result.kanji"
      class="space-y-6"
    >
      <h2 class="text-xl font-semibold border-b pb-2">Kanji Result</h2>

      <div
        class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 space-y-6"
      >
        <div class="space-y-3">
          <h1 class="text-6xl font-bold text-gray-900">
            {{ result.kanji.kanji }}
          </h1>

          <div class="flex items-center space-x-6">
            <div class="space-y-1">
              <span class="text-lg text-blue-600 font-medium"
                >On: {{ result.kanji.on }}</span
              >

              <span class="text-lg text-green-600 font-medium"
                >Kun: {{ result.kanji.kun }}</span
              >
            </div>

            <div class="space-y-1">
              <span class="text-sm text-gray-500"
                >Strokes: {{ result.kanji.stroke_count }}</span
              >

              <span class="text-sm text-gray-500"
                >Frequency: {{ result.kanji.freq }}</span
              >
            </div>
          </div>

          <div class="flex flex-wrap gap-2">
            <span
              v-for="level in result.kanji.level"
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

          <p class="text-gray-800 text-lg">{{ result.kanji.mean }}</p>

          <div v-if="result.kanji.detail" class="space-y-3">
            <div
              v-for="(paragraph, index) in result.kanji.detail.split('##')"
              :key="index"
              class="bg-gray-50 rounded-lg p-4 border-l-4 border-blue-200"
            >
              <p class="text-gray-700 text-sm leading-relaxed">
                {{ paragraph.trim() }}
              </p>
            </div>
          </div>
        </div>

        <div v-if="result.kanji.tips && result.kanji.tips.vi" class="space-y-2">
          <h3 class="font-semibold text-gray-800 flex items-center space-x-2">
            <UIcon name="i-lucide-lightbulb" class="size-4" />

            <span>Memory Tip</span>
          </h3>

          <div class="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
            <p class="text-gray-800" v-html="result.kanji.tips.vi"></p>
          </div>
        </div>

        <div
          v-if="result.kanji.compDetail && result.kanji.compDetail.length > 0"
          class="space-y-3"
        >
          <h3 class="font-semibold text-gray-800 flex items-center space-x-2">
            <UIcon name="i-lucide-puzzle" class="size-4" />

            <span>Components</span>
          </h3>

          <div class="flex flex-wrap gap-3">
            <div
              v-for="comp in result.kanji.compDetail"
              :key="comp.w"
              class="bg-gray-50 rounded-lg p-3 text-center"
            >
              <div class="text-2xl font-bold text-gray-900">{{ comp.w }}</div>

              <div v-if="comp.h" class="text-sm text-gray-600">
                {{ comp.h }}
              </div>
            </div>
          </div>
        </div>

        <div
          v-if="result.kanji.examples && result.kanji.examples.length > 0"
          class="space-y-3"
        >
          <h3 class="font-semibold text-gray-800 flex items-center space-x-2">
            <UIcon name="i-lucide-list" class="size-4" />

            <span>Examples</span>
          </h3>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
            <div
              v-for="(example, idx) in result.kanji.examples"
              :key="idx"
              class="bg-gray-50 rounded-lg p-4"
            >
              <div class="flex items-start justify-between mb-2">
                <h4 class="font-semibold text-gray-900">{{ example.w }}</h4>

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

        <div v-if="result.kanji.example_on" class="space-y-3">
          <h3 class="font-semibold text-gray-800 flex items-center space-x-2">
            <UIcon name="i-lucide-volume-2" class="size-4" />

            <span>On Reading Examples</span>
          </h3>

          <div
            v-for="(examples, reading) in result.kanji.example_on"
            :key="reading"
            class="space-y-2"
          >
            <h4 class="font-medium text-gray-700">{{ reading }} reading:</h4>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-2 ml-4">
              <div
                v-for="(example, idx) in examples"
                :key="idx"
                class="bg-blue-50 rounded p-3"
              >
                <div class="font-medium text-gray-900">{{ example.w }}</div>

                <div class="text-gray-700 text-sm">{{ example.m }}</div>

                <div class="text-gray-500 text-xs">{{ example.p }}</div>
              </div>
            </div>
          </div>
        </div>

        <div v-if="result.kanji.example_kun" class="space-y-3">
          <h3 class="font-semibold text-gray-800 flex items-center space-x-2">
            <UIcon name="i-lucide-volume-1" class="size-4" />

            <span>Kun Reading Examples</span>
          </h3>

          <div
            v-for="(examples, reading) in result.kanji.example_kun"
            :key="reading"
            class="space-y-2"
          >
            <h4 class="font-medium text-gray-700">{{ reading }} reading:</h4>

            <div class="grid grid-cols-1 md:grid-cols-2 gap-2 ml-4">
              <div
                v-for="(example, idx) in examples"
                :key="idx"
                class="bg-green-50 rounded p-3"
              >
                <div class="font-medium text-gray-900">{{ example.w }}</div>

                <div class="text-gray-700 text-sm">{{ example.m }}</div>

                <div class="text-gray-500 text-xs">{{ example.p }}</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div
      v-if="
        result &&
        result.type === 'word' &&
        result.words &&
        result.words.length > 0
      "
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

          <div class="flex items-center space-x-4">
            <span class="text-lg text-blue-600 font-medium">{{
              word.phonetic
            }}</span>

            <span v-if="word.short_mean" class="text-gray-600 italic">{{
              word.short_mean
            }}</span>
          </div>
        </div>

        <div v-if="word.means && word.means.length > 0" class="space-y-3">
          <h3 class="font-semibold text-gray-800 flex items-center space-x-2">
            <UIcon name="i-lucide-book-open" class="size-4" />

            <span>Meanings</span>
          </h3>

          <div class="space-y-3">
            <div
              v-for="(meaning, idx) in word.means"
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

              <div
                v-if="meaning.examples && meaning.examples.length > 0"
                class="mt-3 space-y-2"
              >
                <h4 class="text-sm font-medium text-gray-600">Examples:</h4>

                <div
                  v-for="(example, exIdx) in meaning.examples"
                  :key="exIdx"
                  class="bg-white rounded p-3 border-l-4 border-blue-200"
                >
                  <p class="text-gray-800 mb-1">{{ example.content }}</p>

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
          <h3 class="font-semibold text-gray-800 flex items-center space-x-2">
            <UIcon name="i-lucide-image" class="size-4" />

            <span>Images ({{ word.images.length }})</span>
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

              <div
                class="absolute top-1 right-1 bg-black bg-opacity-50 text-white text-xs px-1 py-0.5 rounded text-center min-w-[20px]"
              >
                {{ index + 1 }}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div
      v-if="
        result &&
        result.type === 'word' &&
        result.suggestWords &&
        result.suggestWords.length > 0
      "
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

            <p class="text-gray-600 text-sm italic">
              {{ suggest.short_mean }}
            </p>

            <div
              v-if="suggest.means && suggest.means.length > 0"
              class="space-y-1"
            >
              <div
                v-for="(meaning, idx) in suggest.means"
                :key="idx"
                class="text-sm"
              >
                <span class="text-gray-800">{{ meaning.mean }}</span>

                <span
                  v-if="meaning.kind"
                  class="text-xs bg-gray-100 text-gray-600 px-2 py-0.5 rounded ml-2"
                >
                  {{ meaning.kind }}
                </span>
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
import { ref } from "vue";

import ConjugationTable from "~/components/ConjugationTable.vue";

import WordResultModal from "~/components/WordResultModal.vue";

// --- Props ---

// This component receives all its data from the parent

defineProps({
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

  // We need this to display "No results for '...'"

  originalSearchWord: {
    type: String,

    default: "",
  },

  hasSearched: {
    // <-- 1. DEFINE THE PROP

    type: Boolean,

    default: false,
  },
});

// --- Modal State ---

// This state lives here, as this component is responsible for

// displaying the results that *trigger* the modal.

const showSuggestedWordModal = ref(false);

const modalSearchWord = ref("");

// --- Methods ---

// Select suggested word

const selectSuggestedWord = (word: string) => {
  modalSearchWord.value = word;

  showSuggestedWordModal.value = true;
};

// Select synonym

const selectSynonym = (word: string) => {
  modalSearchWord.value = word;

  showSuggestedWordModal.value = true;
};

// NOTE: The image modal state and functions were removed

// as they were not connected to any click handlers in the template.

// If you add image modals, their state and open/close methods

// would also live here.
</script>
