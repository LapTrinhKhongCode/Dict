<template>
  <div class="space-y-6">
    <div v-if="loading" class="flex items-center justify-center py-12">
      <div class="text-gray-500 dark:text-gray-400 flex items-center space-x-2">
        <div
          class="animate-spin rounded-full h-5 w-5 border-b-2 border-primary-500 dark:border-blue-500"
        ></div>
        <span>Loading...</span>
      </div>
    </div>

    <div
      v-if="error"
      class="bg-red-50 border border-red-200 text-red-700 dark:bg-red-900/50 dark:border-red-700 dark:text-red-400 rounded-lg p-4"
    >
      <div class="flex items-center space-x-2">
        <UIcon name="i-lucide-alert-circle" class="text-red-500 size-5" />
        <span>{{ error }}</span>
      </div>
    </div>

    <div
      v-if="result && (result.words || result.kanjiList)"
      class="grid grid-cols-1 md:grid-cols-12 gap-6"
    >
      <div class="md:col-span-4 lg:col-span-3 space-y-6">
        <div
          v-if="
            result.type === 'word' && result.words && result.words.length > 0
          "
        >
          <h3 class="text-lg font-semibold mb-2 text-gray-900 dark:text-white">
            Kết quả tra cứu
          </h3>
          <ul class="space-y-2">
            <li
              v-for="word in result.words"
              :key="word._id"
              @click="selectItem(word)"
              class="p-3 rounded-lg cursor-pointer transition-colors border"
              :class="
                selectedItem && selectedItem._id === word._id
                  ? 'bg-blue-50 border-blue-300 dark:bg-gray-700 dark:border-blue-500'
                  : 'bg-white border-gray-200 hover:bg-gray-100 dark:bg-gray-800 dark:border-gray-700 dark:hover:bg-gray-700'
              "
            >
              <h4 class="font-bold text-gray-900 dark:text-white">
                {{ word.word }}
              </h4>
              <p class="text-sm text-blue-600 dark:text-blue-400">
                {{ word.phonetic }}
              </p>
              <p class="text-sm text-gray-600 dark:text-gray-400 truncate">
                {{ word.short_mean }}
              </p>
            </li>
          </ul>
        </div>

        <div
          v-if="
            result.type === 'kanji' &&
            result.kanjiList &&
            result.kanjiList.length > 0
          "
        >
          <h3 class="text-lg font-semibold mb-2 text-gray-900 dark:text-white">
            Kết quả tra cứu
          </h3>
          <ul class="space-y-2">
            <li
              v-for="kanji in result.kanjiList"
              :key="kanji._id"
              @click="selectItem(kanji)"
              class="p-3 rounded-lg cursor-pointer transition-colors border flex items-center space-x-4"
              :class="
                selectedItem && selectedItem._id === kanji._id
                  ? 'bg-blue-50 border-blue-300 dark:bg-gray-700 dark:border-blue-500'
                  : 'bg-white border-gray-200 hover:bg-gray-100 dark:bg-gray-800 dark:border-gray-700 dark:hover:bg-gray-700'
              "
            >
              <h4 class="font-bold text-3xl text-gray-900 dark:text-white">
                {{ kanji.kanji }}
              </h4>
              <p class="text-sm text-gray-600 dark:text-gray-400">
                {{ kanji.mean }}
              </p>
            </li>
          </ul>
        </div>

        <div
          v-if="
            result.type === 'word' &&
            result.suggestWords &&
            result.suggestWords.length > 0
          "
          class="space-y-4"
        >
          <h3 class="text-lg font-semibold mb-2 text-gray-900 dark:text-white">
            Các từ liên quan
          </h3>
          <div class="space-y-2">
            <div
              v-for="suggest in visibleSuggestions"
              :key="suggest._id"
              class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-3 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors cursor-pointer"
              @click="selectSuggestedWord(suggest.word)"
            >
              <h4 class="font-semibold text-gray-900 dark:text-white">
                {{ suggest.word }}
              </h4>
              <p class="text-sm text-blue-600 dark:text-blue-400">
                {{ suggest.phonetic }}
              </p>
            </div>
          </div>
          <button
            v-if="
              result.suggestWords &&
              result.suggestWords.length > suggestionLimit
            "
            @click="showAllSuggestions = !showAllSuggestions"
            class="w-full text-sm font-medium text-blue-600 hover:text-blue-800 p-2 rounded-lg hover:bg-blue-50 dark:text-blue-400 dark:hover:text-blue-300 dark:hover:bg-gray-700 transition-colors"
          >
            <span v-if="!showAllSuggestions"> Xem thêm </span>
            <span v-else> Thu gọn </span>
          </button>
        </div>
      </div>

      <div class="md:col-span-8 lg:col-span-9 space-y-6">
        <div
          v-if="result.type === 'kanji' && selectedItem"
          class="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 p-6 space-y-6"
        >
          <div class="flex justify-between items-start">
            <div class="space-y-3">
              <h1 class="text-6xl font-bold text-gray-900 dark:text-white">
                {{ selectedItem.kanji }}
              </h1>
              <div class="flex flex-col space-y-1">
                <p class="text-gray-800 dark:text-gray-200 text-lg">
                  {{ selectedItem.mean }}
                </p>
                <span
                  class="text-lg text-blue-600 dark:text-blue-400 font-medium"
                  >On: {{ selectedItem.on }}</span
                >
                <span
                  class="text-lg text-green-600 dark:text-green-500 font-medium"
                  >Kun: {{ selectedItem.kun }}</span
                >
                <span class="text-sm text-gray-500 dark:text-gray-400"
                  >Số nét: {{ selectedItem.stroke_count }}</span
                >
                <span class="text-sm text-gray-500 dark:text-gray-400"
                  >Độ phổ biến: {{ selectedItem.freq }}</span
                >
              </div>
              <div class="flex flex-wrap gap-2">
                <span
                  v-for="level in selectedItem.level"
                  :key="level"
                  class="bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200 text-sm px-3 py-1 rounded-full"
                >
                  {{ level }}
                </span>
              </div>
            </div>

            <div class="flex-shrink-0 ml-4">
              <KanjiStrokeInResult :kanji="selectedItem.kanji" />
            </div>
          </div>
          <div class="space-y-2">
            <h3
              class="font-semibold text-gray-800 dark:text-gray-200 flex items-center space-x-2"
            >
              <UIcon name="i-lucide-book-open" class="size-4" />
              <span>Nghĩa</span>
            </h3>
            <div v-if="selectedItem.detail" class="space-y-3">
              <div
                v-for="(paragraph, index) in selectedItem.detail.split('##')"
                :key="index"
                class="bg-gray-50 dark:bg-gray-900 rounded-lg p-4 border-l-4 border-blue-200 dark:border-blue-700"
              >
                <p
                  class="text-gray-700 dark:text-gray-300 text-sm leading-relaxed"
                >
                  {{ paragraph.trim() }}
                </p>
              </div>
            </div>
          </div>
          <div
            v-if="selectedItem.tips && selectedItem.tips.vi"
            class="space-y-2"
          >
            <h3
              class="font-semibold text-gray-800 dark:text-gray-200 flex items-center space-x-2"
            >
              <UIcon name="i-lucide-lightbulb" class="size-4" />
              <span>Mẹo nhớ</span>
            </h3>
            <div
              class="bg-yellow-50 border border-yellow-200 dark:bg-yellow-900/50 dark:border-yellow-700/50 rounded-lg p-4"
            >
              <p
                class="text-gray-800 dark:text-yellow-200"
                v-html="selectedItem.tips.vi"
              ></p>
            </div>
          </div>
          <div
            v-if="selectedItem.compDetail && selectedItem.compDetail.length > 0"
            class="space-y-3"
          >
            <h3
              class="font-semibold text-gray-800 dark:text-gray-200 flex items-center space-x-2"
            >
              <UIcon name="i-lucide-puzzle" class="size-4" />
              <span>Bộ</span>
            </h3>
            <div class="flex flex-wrap gap-3">
              <div
                v-for="comp in selectedItem.compDetail"
                :key="comp.w"
                class="bg-gray-50 dark:bg-gray-900 rounded-lg p-3 text-center border border-gray-200 dark:border-gray-700"
              >
                <div class="text-2xl font-bold text-gray-900 dark:text-white">
                  {{ comp.w }}
                </div>
                <div
                  v-if="comp.h"
                  class="text-sm text-gray-600 dark:text-gray-400"
                >
                  {{ comp.h }}
                </div>
              </div>
            </div>
          </div>
          <div
            v-if="selectedItem.examples && selectedItem.examples.length > 0"
            class="space-y-3"
          >
            <h3
              class="font-semibold text-gray-800 dark:text-gray-200 flex items-center space-x-2"
            >
              <UIcon name="i-lucide-list" class="size-4" />
              <span>Ví dụ</span>
            </h3>
            <div class="grid grid-cols-1 md:grid-cols-2 gap-3">
              <div
                v-for="(example, idx) in selectedItem.examples"
                :key="idx"
                class="bg-gray-50 dark:bg-gray-900 rounded-lg p-4 border border-gray-200 dark:border-gray-700"
              >
                <div class="flex items-start justify-between mb-2">
                  <h4 class="font-semibold text-gray-900 dark:text-white">
                    {{ example.w }}
                  </h4>
                  <span
                    class="text-xs bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200 px-2 py-1 rounded-full"
                  >
                    {{ example.h }}
                  </span>
                </div>
                <p class="text-gray-800 dark:text-gray-200 mb-1">
                  {{ example.m }}
                </p>
                <p class="text-gray-600 dark:text-gray-400 text-sm">
                  {{ example.p }}
                </p>
              </div>
            </div>
          </div>
          <div v-if="selectedItem.example_on" class="space-y-3">
            <h3
              class="font-semibold text-gray-800 dark:text-gray-200 flex items-center space-x-2"
            >
              <UIcon name="i-lucide-volume-2" class="size-4" />
              <span>Ví dụ cách đọc (On)</span>
            </h3>
            <div
              v-for="(examples, reading) in selectedItem.example_on"
              :key="reading"
              class="space-y-2"
            >
              <h4 class="font-medium text-gray-700 dark:text-gray-300">
                {{ reading }}
              </h4>
              <div class="grid grid-cols-1 md:grid-cols-2 gap-2 ml-4">
                <div
                  v-for="(example, idx) in examples"
                  :key="idx"
                  class="bg-blue-50 dark:bg-blue-900/50 rounded p-3"
                >
                  <div class="font-medium text-gray-900 dark:text-white">
                    {{ example.w }}
                  </div>
                  <div class="text-gray-700 dark:text-gray-300 text-sm">
                    {{ example.m }}
                  </div>
                  <div class="text-gray-500 dark:text-gray-400 text-xs">
                    {{ example.p }}
                  </div>
                </div>
              </div>
            </div>
          </div>
          <div v-if="selectedItem.example_kun" class="space-y-3">
            <h3
              class="font-semibold text-gray-800 dark:text-gray-200 flex items-center space-x-2"
            >
              <UIcon name="i-lucide-volume-1" class="size-4" />
              <span>Ví dụ cách đọc (Kun)</span>
            </h3>
            <div
              v-for="(examples, reading) in selectedItem.example_kun"
              :key="reading"
              class="space-y-2"
            >
              <h4 class="font-medium text-gray-700 dark:text-gray-300">
                {{ reading }}
              </h4>
              <div class="grid grid-cols-1 md:grid-cols-2 gap-2 ml-4">
                <div
                  v-for="(example, idx) in examples"
                  :key="idx"
                  class="bg-green-50 dark:bg-green-900/50 rounded p-3"
                >
                  <div class="font-medium text-gray-900 dark:text-white">
                    {{ example.w }}
                  </div>
                  <div class="text-gray-700 dark:text-gray-300 text-sm">
                    {{ example.m }}
                  </div>
                  <div class="text-gray-500 dark:text-gray-400 text-xs">
                    {{ example.p }}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        <div
          v-if="result.type === 'word' && selectedItem"
          class="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-200 dark:border-gray-700 p-6 space-y-4 relative"
        >
          <button
            ref="decksButtonRef"
            type="button"
            class="absolute top-4 right-4 z-10 size-9 flex items-center justify-center rounded-full bg-gray-100 text-gray-700 hover:bg-gray-200 dark:bg-gray-900 dark:text-white dark:hover:bg-gray-700 transition-colors"
            @click="toggleDecksPanel"
            aria-label="Lưu vào Deck"
          >
            <div
              v-if="decksLoading"
              class="animate-spin rounded-full h-6 w-6 border-b-2 border-gray-700 dark:border-white"
            ></div>
            <UIcon v-else name="i-lucide-plus" class="size-6" />
          </button>

          <div
            v-if="showDecksPanel"
            ref="decksPanelRef"
            class="absolute top-16 right-4 z-20 w-64 rounded-lg shadow-lg bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 p-4"
          >
            <h4 class="font-medium text-gray-900 dark:text-white mb-2">
              Lưu vào Deck...
            </h4>

            <div
              v-if="decksLoading"
              class="flex items-center justify-center py-2"
            >
              <span class="text-sm text-gray-500 dark:text-gray-400 ml-2"
                >Đang tải decks...</span
              >
            </div>

            <div
              v-else-if="saveLoading"
              class="flex items-center justify-center py-2"
            >
              <div
                class="animate-spin rounded-full h-4 w-4 border-b-2 border-green-500"
              ></div>
              <span class="text-sm text-gray-500 dark:text-gray-400 ml-2"
                >Đang lưu thẻ...</span
              >
            </div>

            <ul
              v-else-if="decks.length > 0"
              class="space-y-1 max-h-48 overflow-y-auto"
            >
              <li
                v-for="deck in decks"
                :key="deck.id"
                class="p-2 rounded-md hover:bg-gray-100 dark:hover:bg-gray-800 cursor-pointer text-gray-800 dark:text-gray-200"
                @click="saveToDeck(deck.id)"
              >
                {{ deck.name }}
              </li>
            </ul>

            <div v-else class="text-gray-500 dark:text-gray-400 text-sm">
              Không tìm thấy deck nào.
            </div>
          </div>
          <div class="space-y-2">
            <h1 class="text-3xl font-bold text-gray-900 dark:text-white">
              {{ selectedItem.word }}
            </h1>
            <div class="flex items-center space-x-4">
              <span
                class="text-lg text-blue-600 dark:text-blue-400 font-medium"
                >{{ selectedItem.phonetic }}</span
              >
            </div>
          </div>
          <div
            v-if="
              selectedItem.pronunciation &&
              selectedItem.pronunciation.length > 0
            "
            class="flex flex-wrap gap-3 pt-3 border-t border-gray-100 dark:border-gray-700"
          >
            <div
              v-for="(pron, index) in selectedItem.pronunciation"
              :key="index"
              class="bg-gray-50 dark:bg-gray-900 rounded-lg p-3 border border-gray-200 dark:border-gray-700"
            >
              <div
                v-if="pron.word !== selectedItem.word"
                class="font-bold text-xl text-gray-900 dark:text-white text-center mb-1"
              >
                {{ pron.word }}
              </div>

              <div
                v-if="pron.transcriptions?.[0]"
                class="flex items-center justify-center text-center"
                :class="pron.word !== selectedItem.word ? '' : 'mt-0'"
              >
                <UIcon
                  name="i-lucide-volume-2"
                  class="size-4 text-blue-500 dark:text-blue-400 mr-1 cursor-pointer"
                  @click="speak(pron.word)"
                />

                <span
                  class="text-lg text-blue-600 dark:text-blue-400 font-medium"
                  >{{ pron.transcriptions[0].kana }}</span
                >
                <span class="text-gray-600 dark:text-gray-400 text-base ml-1"
                  >[{{ pron.transcriptions[0].romaji }}]</span
                >
              </div>
            </div>
          </div>

          <div
            v-if="selectedItem.means && selectedItem.means.length > 0"
            class="space-y-3"
          >
            <h3
              class="font-semibold text-gray-800 dark:text-gray-200 flex items-center space-x-2"
            >
              <UIcon name="i-lucide-book-open" class="size-4" />
              <span>Nghĩa</span>
            </h3>
            <div class="space-y-3">
              <div
                v-for="(meaning, idx) in selectedItem.means"
                :key="idx"
                class="bg-gray-50 dark:bg-gray-900 rounded-lg p-4 border border-gray-200 dark:border-gray-700"
              >
                <div class="flex items-start justify-between mb-2">
                  <p class="text-gray-800 dark:text-gray-200 font-medium">
                    {{ meaning.mean }}
                  </p>
                  <span
                    v-if="meaning.kind"
                    class="text-xs bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200 px-2 py-1 rounded-full"
                  >
                    {{ meaning.kind }}
                  </span>
                </div>
                <div
                  v-if="meaning.examples && meaning.examples.length > 0"
                  class="mt-3 space-y-2"
                >
                  <h4
                    class="text-sm font-medium text-gray-600 dark:text-gray-400"
                  >
                    Ví dụ:
                  </h4>
                  <div
                    v-for="(example, exIdx) in meaning.examples"
                    :key="exIdx"
                    class="bg-white dark:bg-gray-800 rounded p-3 border-l-4 border-blue-200 dark:border-blue-700"
                  >
                    <p class="text-gray-800 dark:text-gray-200 mb-1">
                      {{ example.content }}
                    </p>
                    <p class="text-gray-600 dark:text-gray-400 text-sm italic">
                      {{ example.mean }}
                    </p>
                    <p
                      v-if="example.transcription"
                      class="text-gray-500 dark:text-gray-500 text-xs mt-1"
                    >
                      {{ example.transcription }}
                    </p>
                  </div>
                </div>
              </div>
            </div>
          </div>
          <div
            v-if="selectedItem.synsets && selectedItem.synsets.length > 0"
            class="space-y-2"
          >
            <h3
              class="font-semibold text-gray-800 dark:text-gray-200 flex items-center space-x-2"
            >
              <UIcon name="i-lucide-link" class="size-4" />
              <span>Từ đồng nghĩa</span>
            </h3>

            <div class="flex flex-wrap gap-2">
              <template
                v-for="(synset, i) in selectedItem.synsets"
                :key="`synset-${i}`"
              >
                <template
                  v-for="(entry, j) in synset.entry"
                  :key="`entry-${i}-${j}`"
                >
                  <span
                    v-for="(synonym, k) in entry.synonym"
                    :key="`syn-${i}-${j}-${k}`"
                    class="bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200 text-sm px-3 py-1 rounded-full cursor-pointer"
                    @click="selectSynonym(synonym)"
                  >
                    {{ synonym }}
                  </span>
                </template>
              </template>
            </div>
          </div>
          <div
            v-if="
              selectedItem.opposite_word &&
              selectedItem.opposite_word.length > 0
            "
            class="space-y-2"
          >
            <h3
              class="font-semibold text-gray-800 dark:text-gray-200 flex items-center space-x-2"
            >
              <UIcon name="i-lucide-arrow-left-right" class="size-4" />
              <span>Từ trái nghĩa</span>
            </h3>

            <div class="flex flex-wrap gap-2">
              <span
                v-for="(word, index) in selectedItem.opposite_word"
                :key="`opp-${index}`"
                class="bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200 text-sm px-3 py-1 rounded-full cursor-pointer"
                @click="selectSynonym(word)"
              >
                {{ word }}
              </span>
            </div>
          </div>
          <div
            v-if="selectedItem.images && selectedItem.images.length > 0"
            class="space-y-3"
          >
            <h3
              class="font-semibold text-gray-800 dark:text-gray-200 flex items-center space-x-2"
            >
              <UIcon name="i-lucide-image" class="size-4" />
              <span>Ảnh minh họa</span>
            </h3>
            <div
              class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 gap-3"
            >
              <div
                v-for="(image, index) in visibleImages"
                :key="index"
                class="relative group"
              >
                <img
                  :src="image"
                  :alt="`${selectedItem.word} - Image ${index + 1}`"
                  class="w-full h-24 object-cover rounded-lg border border-gray-200 dark:border-gray-700 hover:scale-105 transition-transform cursor-pointer"
                  @error="$event.target.style.display = 'none'"
                  @click="openImageModal(image)"
                />
              </div>
            </div>
            <button
              v-if="selectedItem.images.length > imageLimit"
              @click="showAllImages = !showAllImages"
              class="text-sm font-medium text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300"
            >
              <span v-if="!showAllImages"> Xem thêm </span>
              <span v-else> Thu gọn </span>
            </button>
          </div>
        </div>

        <div v-if="conjugationResult" class="space-y-6">
          <h2
            class="text-xl font-semibold border-b border-gray-200 dark:border-gray-700 pb-2 text-gray-900 dark:text-white"
          >
            Bảng chia động từ
          </h2>
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
        class="size-12 text-gray-400 dark:text-gray-500 mx-auto mb-4"
      />
      <p class="text-gray-600 dark:text-gray-500 text-lg">
        No results found for "{{ originalSearchWord }}"
      </p>
      <p class="text-gray-500 dark:text-gray-400 text-sm mt-2">
        Try a different search term
      </p>
    </div>

    <WordResultModal
      v-if="showSuggestedWordModal"
      :search-word="modalSearchWord"
      @close="showSuggestedWordModal = false"
      class="z-50"
    />
    <ImageModal
      :is-open="showImageModal"
      :image-url="currentImage"
      @close="showImageModal = false"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch, computed, onMounted, nextTick, onUnmounted } from "vue";
import { useJwt } from "~/composables/useJwt";
import ConjugationTable from "~/components/ConjugationTable.vue";
import WordResultModal from "~/components/WordResultModal.vue";
import KanjiStrokeInResult from "./KanjiStrokeInResult.vue";
import ImageModal from "~/components/ImageModal.vue";
// ✅ SỬA LỖI: Import đúng composable
import { useToast } from "@/composables/useToast";
// ✅ SỬA LỖI: Lấy đúng hàm 'showToast'
const { showToast } = useToast();

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

const selectedItem = ref<any | null>(null);
const showAllSuggestions = ref(false);
const suggestionLimit = 6;
const emit = defineEmits(["itemSelected"]);
watch(
  selectedItem,
  (newItem) => {
    emit("itemSelected", newItem);
  },
  { deep: true },
);
// --- NEW: State for images toggle ---
const showAllImages = ref(false);
const imageLimit = 6;

const visibleSuggestions = computed(() => {
  if (!props.result || !props.result.suggestWords) return [];
  if (showAllSuggestions.value) {
    return props.result.suggestWords;
  }
  return props.result.suggestWords.slice(0, suggestionLimit);
});

// --- NEW: Computed property for visible images ---
const visibleImages = computed(() => {
  if (!selectedItem.value || !selectedItem.value.images) return [];
  if (showAllImages.value) {
    return selectedItem.value.images;
  }
  return selectedItem.value.images.slice(0, imageLimit);
});

watch(
  () => props.result,
  (newResult) => {
    console.log("3. SearchResult nhận được props:", newResult);
    showAllSuggestions.value = false;
    showAllImages.value = false;
    if (
      newResult &&
      newResult.type === "word" &&
      newResult.words &&
      newResult.words.length > 0
    ) {
      selectedItem.value = newResult.words[0];
    } else if (
      newResult &&
      newResult.type === "kanji" &&
      newResult.kanjiList &&
      newResult.kanjiList.length > 0
    ) {
      console.log("4. Đã set selectedItem cho Kanji:", selectedItem.value);
      selectedItem.value = newResult.kanjiList[0];
    } else {
      selectedItem.value = null;
      console.log("5. Chết dở, nó nhảy vào else rồi!");
    }
  },
  { immediate: true, deep: true },
);

const showSuggestedWordModal = ref(false);
const modalSearchWord = ref("");

const showImageModal = ref(false);
const currentImage = ref("");
const config = useRuntimeConfig();

// 2. LẤY TOKEN VÀ TRẠNG THÁI ĐĂNG NHẬP
const { isAuthenticated, jwt } = useJwt();

const decks = ref<any[]>([]);
const decksLoading = ref(false);
const saveLoading = ref(false);
const showDecksPanel = ref(false);
const decksButtonRef = ref<HTMLElement | null>(null); // Ref cho nút
const decksPanelRef = ref<HTMLElement | null>(null); // Ref cho panel

// 3. Hàm mới để ĐÓNG/MỞ panel
const toggleDecksPanel = () => {
  showDecksPanel.value = !showDecksPanel.value;

  if (showDecksPanel.value) {
    // Chỉ fetch khi mở ra
    fetchDecks();
    // Thêm listener để đóng khi click ra ngoài
    setTimeout(() => {
      document.addEventListener("click", handleClickOutside);
    }, 0); // 0 mili-giây là đủ
  } else {
    // Gỡ listener khi đóng
    document.removeEventListener("click", handleClickOutside);
  }
};

// 4. Hàm mới để xử lý click ra ngoài
const handleClickOutside = (event: MouseEvent) => {
  const isClickInsideButton = decksButtonRef.value?.contains(
    event.target as Node,
  );
  const isClickInsidePanel = decksPanelRef.value?.contains(
    event.target as Node,
  );

  // Nếu click ra ngoài cả nút VÀ panel
  if (!isClickInsideButton && !isClickInsidePanel) {
    showDecksPanel.value = false;
    document.removeEventListener("click", handleClickOutside);
  }
};

// 5. Gỡ listener khi component bị hủy (quan trọng)
onUnmounted(() => {
  document.removeEventListener("click", handleClickOutside);
});
/**
 * Hàm gọi API để lấy danh sách decks.
 * Chỉ gọi 1 lần, nếu decks đã có thì không gọi lại.
 */
const fetchDecks = async () => {
  if (decks.value.length > 0) return;

  // ✅ SỬA LỖI: Dùng showToast
  if (!isAuthenticated.value) {
    showToast("Bạn cần đăng nhập để tải decks", "error");
    return;
  }

  const headers = {
    Authorization: `Bearer ${jwt.value}`,
    accept: "*/*",
  };

  decksLoading.value = true;
  try {
    const response = await $fetch<any>(
      `${config.public.apiBaseUrl}/api/Decks/my-decks`,
      {
        method: "GET",
        headers: headers,
      },
    );
    decks.value = response.result;

    if (!decks.value) {
      decks.value = [];
    }
  } catch (e) {
    // ✅ SỬA LỖI: Dùng showToast
    showToast("Không thể tải danh sách decks", "error");
    decks.value = []; // Gán mảng rỗng nếu lỗi
  } finally {
    decksLoading.value = false;
  }
};
/**
 * Hàm gọi API để lưu 1 thẻ vào deck được chọn (ĐÃ THÊM AUTH)
 */
const saveToDeck = async (deckId: string) => {
  if (!selectedItem.value || saveLoading.value) return;

  // ✅ SỬA LỖI: Dùng showToast
  if (!isAuthenticated.value) {
    showToast("Bạn cần đăng nhập để lưu thẻ", "error");
    return;
  }

  saveLoading.value = true; // <-- BẬT LÊN

  const payload = [
    {
      frontText: selectedItem.value.word,
      backText:
        selectedItem.value.means?.[0]?.mean ||
        selectedItem.value.short_mean ||
        "",
      tags: selectedItem.value.phonetic || "",
    },
  ];

  const headers = {
    Authorization: `Bearer ${jwt.value}`,
    "Content-Type": "application/json",
  };

  try {
    await $fetch(`${config.public.apiBaseUrl}/api/Decks/${deckId}/cards`, {
      method: "POST",
      body: payload,
      headers: headers,
    });

    // ✅ SỬA LỖI: Dùng showToast
    showToast(`Đã lưu từ "${selectedItem.value.word}" vào deck.`, "success");
  } catch (e) {
    // ✅ SỬA LỖI: Dùng showToast
    showToast("Không thể lưu thẻ, vui lòng thử lại", "error");
  } finally {
    // (Logic finally giữ nguyên)
    saveLoading.value = false; // <-- TẮT ĐI

    // Đóng panel
    showDecksPanel.value = false;
    document.removeEventListener("click", handleClickOutside);
  }
};
function speak(textToSpeak) {
  // Quan trọng: Phải check 'process.client' vì window.speechSynthesis chỉ tồn tại ở trình duyệt
  if (process.client) {
    if (!window.speechSynthesis) {
      console.error(
        "Rất tiếc, trình duyệt của bạn không hỗ trợ chức năng phát âm.",
      );
      return;
    }

    // Tạo một yêu cầu phát âm mới
    const utterance = new SpeechSynthesisUtterance(textToSpeak);

    // *** Đặt ngôn ngữ là tiếng Nhật ***
    utterance.lang = "ja-JP";

    // (Nên có) Hủy bỏ bất kỳ lần phát âm nào trước đó nếu người dùng click nhanh
    window.speechSynthesis.cancel();

    // Phát âm
    window.speechSynthesis.speak(utterance);
  }
}
const openImageModal = (imageUrl: string) => {
  currentImage.value = imageUrl;
  showImageModal.value = true;
};

const selectSuggestedWord = (word: string) => {
  modalSearchWord.value = word;
  showSuggestedWordModal.value = true;
};

const selectSynonym = (word: string) => {
  modalSearchWord.value = word;
  showSuggestedWordModal.value = true;
};

const selectItem = (item: any) => {
  selectedItem.value = item;
  showAllImages.value = false;
};
</script>
