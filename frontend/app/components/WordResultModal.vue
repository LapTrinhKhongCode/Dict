<template>
  <div
    class="fixed inset-0 z-60 bg-black bg-opacity-20 flex items-center justify-center"
    style="background-color: rgba(0, 0, 0, 0.5)"
    @click.self="$emit('close')"
  >
    <div
      class="relative bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-xl shadow-2xl w-full max-w-6xl h-[85vh] flex flex-col"
      @click.stop
    >
      <button
        class="absolute top-4 right-4 text-gray-600 hover:text-gray-900 dark:text-gray-400 dark:hover:text-white text-3xl font-bold bg-white dark:bg-gray-800 bg-opacity-80 dark:bg-opacity-80 rounded-full w-10 h-10 flex items-center justify-center transition leading-none pb-1 z-50"
        @click="$emit('close')"
      >
        &times;
      </button>

      <div class="p-4 border-b border-gray-200 dark:border-gray-700">
        <div class="flex items-center space-x-2">
          <button
            @click="activeTab = 'word'"
            :class="[
              'px-4 py-2 rounded-lg font-medium transition-all',
              activeTab === 'word'
                ? 'bg-primary-600 text-white shadow-sm'
                : 'text-gray-600 hover:bg-gray-100 dark:text-gray-400 dark:hover:bg-gray-700',
            ]"
          >
            Từ vựng
          </button>
          <button
            @click="activeTab = 'kanji'"
            :class="[
              'px-4 py-2 rounded-lg font-medium transition-all',
              activeTab === 'kanji'
                ? 'bg-primary-600 text-white shadow-sm'
                : 'text-gray-600 hover:bg-gray-100 dark:text-gray-400 dark:hover:bg-gray-700',
            ]"
          >
            Hán tự
          </button>
        </div>
      </div>
      <div
        class="flex-1 overflow-y-auto p-6 space-y-8 modal-scroll-content bg-gray-50 dark:bg-gray-800"
      >
        <div v-if="loading" class="flex items-center justify-center py-12">
          <div
            class="text-gray-500 dark:text-gray-400 flex items-center space-x-2"
          >
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

        <div v-if="activeTab === 'word'" v-show="!loading && !error">
          <div v-if="selectedWord" class="space-y-6 relative">
            <button
              ref="decksButtonRef"
              type="button"
              class="absolute top-0 right-0 z-10 size-9 flex items-center justify-center rounded-full bg-gray-100 text-gray-700 hover:bg-gray-200 dark:bg-gray-900 dark:text-white dark:hover:bg-gray-700 transition-colors"
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
              class="absolute top-12 right-0 z-20 w-64 rounded-lg shadow-lg bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-800 p-4"
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
                {{ selectedWord.word }}
              </h1>
              <div class="flex items-center space-x-4">
                <span
                  class="text-lg text-blue-600 dark:text-blue-400 font-medium"
                  >{{ selectedWord.phonetic }}</span
                >
              </div>
            </div>
            <div
              v-if="
                selectedWord.pronunciation &&
                selectedWord.pronunciation.length > 0
              "
              class="flex flex-wrap gap-3 pt-3 border-t border-gray-100 dark:border-gray-700"
            >
              <div
                v-for="(pron, index) in selectedWord.pronunciation"
                :key="index"
                class="bg-gray-100 dark:bg-gray-900 rounded-lg p-3 border border-gray-200 dark:border-gray-700"
              >
                <div
                  v-if="pron.word !== selectedWord.word"
                  class="font-bold text-xl text-gray-900 dark:text-white text-center mb-1"
                >
                  {{ pron.word }}
                </div>
                <div
                  v-if="pron.transcriptions?.[0]"
                  class="flex items-center justify-center text-center"
                  :class="pron.word !== selectedWord.word ? '' : 'mt-0'"
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
                  <span
                    class="text-gray-600 dark:text-gray-400 text-base ml-1"
                    >[{{ pron.transcriptions[0].romaji }}]</span
                  >
                </div>
              </div>
            </div>
            <div
              v-if="selectedWord.means && selectedWord.means.length > 0"
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
                  v-for="(meaning, idx) in selectedWord.means"
                  :key="idx"
                  class="bg-gray-100 dark:bg-gray-900 rounded-lg p-4 border border-gray-200 dark:border-gray-700"
                >
                  <div class="flex items-start justify-between mb-2">
                    <p
                      class="text-gray-800 dark:text-gray-200 font-medium"
                    >
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
                      <p
                        class="text-gray-600 dark:text-gray-400 text-sm italic"
                      >
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
              v-if="selectedWord.synsets && selectedWord.synsets.length > 0"
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
                  v-for="(synset, i) in selectedWord.synsets"
                  :key="`synset-${i}`"
                >
                  <template
                    v-for="(entry, j) in synset.entry"
                    :key="`entry-${i}-${j}`"
                  >
                    <span
                      v-for="(synonym, k) in entry.synonym"
                      :key="`syn-${i}-${j}-${k}`"
                      class="bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200 text-sm px-3 py-1 rounded-full cursor-pointer hover:bg-green-200 dark:hover:bg-green-800 transition-colors"
                      @click="searchNewWord(synonym)"
                    >
                      {{ synonym }}
                    </span>
                  </template>
                </template>
              </div>
            </div>
            <div
              v-if="
                selectedWord.opposite_word &&
                selectedWord.opposite_word.length > 0
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
                  v-for="(word, index) in selectedWord.opposite_word"
                  :key="`opp-${index}`"
                  class="bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200 text-sm px-3 py-1 rounded-full cursor-pointer hover:bg-red-200 dark:hover:bg-red-800 transition-colors"
                  @click="searchNewWord(word)"
                >
                  {{ word }}
                </span>
              </div>
            </div>
            <div
              v-if="selectedWord.images && selectedWord.images.length > 0"
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
                    :alt="`${selectedWord.word} - Image ${index + 1}`"
                    class="w-full h-24 object-cover rounded-lg border border-gray-200 dark:border-gray-700 hover:scale-105 transition-transform cursor-pointer"
                    @error="$event.target.style.display = 'none'"
                    @click="openImageModal(image)"
                  />
                </div>
              </div>
              <button
                v-if="selectedWord.images.length > imageLimit"
                @click="showAllImages = !showAllImages"
                class="text-sm font-medium text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300"
              >
                <span v-if="!showAllImages"> Xem thêm </span>
                <span v-else> Thu gọn </span>
              </button>
            </div>
          </div>
          <div v-else-if="!loading && !error">
            <p class="text-gray-500 dark:text-gray-400">
              Không tìm thấy thông tin từ vựng.
            </p>
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

          <div
            v-if="
              result && result.suggestWords && result.suggestWords.length > 0
            "
            class="space-y-4 pt-6 border-t border-gray-200 dark:border-gray-700 mt-6"
          >
            <h3
              class="text-lg font-semibold mb-2 text-gray-900 dark:text-white"
            >
              Các từ liên quan
            </h3>
            <div class="space-y-2">
              <div
                v-for="suggest in visibleSuggestions"
                :key="suggest._id"
                class="bg-white dark:bg-gray-800 rounded-lg p-3 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors cursor-pointer border border-gray-200 dark:border-gray-700"
                @click="searchNewWord(suggest.word)"
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
              class="w-full text-sm font-medium text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 p-2 rounded-lg hover:bg-blue-50 dark:hover:bg-gray-700 transition-colors"
            >
              <span v-if="!showAllSuggestions"> Xem thêm </span>
              <span v-else> Thu gọn </span>
            </button>
          </div>
        </div>

        <div
          v-if="activeTab === 'kanji'"
          v-show="!loading && !error"
          class="space-y-6"
        >
          <div v-if="kanjiResults && kanjiResults.length > 0">
            <div
              class="flex flex-wrap gap-2 border-b border-gray-200 dark:border-gray-700 pb-4"
            >
              <button
                v-for="(kanji, index) in kanjiResults"
                :key="kanji._id"
                @click="activeKanjiIndex = index"
                :class="[
                  'px-4 py-2 rounded-lg font-medium transition-all text-xl',
                  activeKanjiIndex === index
                    ? 'bg-primary-600 text-white'
                    : 'bg-gray-100 text-gray-700 hover:bg-gray-200 dark:bg-gray-700 dark:text-gray-200 dark:hover:bg-gray-600',
                ]"
              >
                {{ kanji.kanji }}
              </button>
            </div>

            <div v-if="selectedKanji" class="space-y-6 pt-6">
              <div class="flex justify-between items-start">
                <div class="space-y-3">
                  <h1 class="text-6xl font-bold text-gray-900 dark:text-white">
                    {{ selectedKanji.kanji }}
                  </h1>
                  <div class="flex flex-col space-y-1">
                    <p class="text-gray-800 dark:text-gray-200 text-lg">
                      {{ selectedKanji.mean }}
                    </p>
                    <span
                      class="text-lg text-blue-600 dark:text-blue-400 font-medium"
                      >On: {{ selectedKanji.on }}</span
                    >
                    <span
                      class="text-lg text-green-600 dark:text-green-500 font-medium"
                      >Kun: {{ selectedKanji.kun }}</span
                    >
                    <span class="text-sm text-gray-500 dark:text-gray-400"
                      >Số nét: {{ selectedKanji.stroke_count }}</span
                    >
                    <span class="text-sm text-gray-500 dark:text-gray-400"
                      >Độ phổ biến: {{ selectedKanji.freq }}</span
                    >
                  </div>
                  <div class="flex flex-wrap gap-2">
                    <span
                      v-for="level in selectedKanji.level"
                      :key="level"
                      class="bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200 text-sm px-3 py-1 rounded-full"
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
                  class="font-semibold text-gray-800 dark:text-gray-200 flex items-center space-x-2"
                >
                  <UIcon name="i-lucide-book-open" class="size-4" />
                  <span>Nghĩa</span>
                </h3>
                <div v-if="selectedKanji.detail" class="space-y-3">
                  <div
                    v-for="(paragraph, index) in selectedKanji.detail.split(
                      '##'
                    )"
                    :key="index"
                    class="bg-gray-100 dark:bg-gray-900 rounded-lg p-4 border-l-4 border-blue-200 dark:border-blue-700"
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
                v-if="selectedKanji.tips && selectedKanji.tips.vi"
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
                    v-html="selectedKanji.tips.vi"
                  ></p>
                </div>
              </div>
              <div
                v-if="
                  selectedKanji.compDetail && selectedKanji.compDetail.length > 0
                "
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
                    v-for="comp in selectedKanji.compDetail"
                    :key="comp.w"
                    class="bg-gray-100 dark:bg-gray-900 rounded-lg p-3 text-center border border-gray-200 dark:border-gray-700"
                  >
                    <div
                      class="text-2xl font-bold text-gray-900 dark:text-white"
                    >
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
                v-if="
                  selectedKanji.examples && selectedKanji.examples.length > 0
                "
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
                    v-for="(example, idx) in selectedKanji.examples"
                    :key="idx"
                    class="bg-gray-100 dark:bg-gray-900 rounded-lg p-4 border border-gray-200 dark:border-gray-700"
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
              <div v-if="selectedKanji.example_on" class="space-y-3">
                <h3
                  class="font-semibold text-gray-800 dark:text-gray-200 flex items-center space-x-2"
                >
                  <UIcon name="i-lucide-volume-2" class="size-4" />
                  <span>Ví dụ cách đọc (On)</span>
                </h3>
                <div
                  v-for="(examples, reading) in selectedKanji.example_on"
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
              <div v-if="selectedKanji.example_kun" class="space-y-3">
                <h3
                  class="font-semibold text-gray-800 dark:text-gray-200 flex items-center space-x-2"
                >
                  <UIcon name="i-lucide-volume-1" class="size-4" />
                  <span>Ví dụ cách đọc (Kun)</span>
                </h3>
                <div
                  v-for="(examples, reading) in selectedKanji.example_kun"
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
          </div>
          <div v-else>
            <p class="text-gray-500 dark:text-gray-400">
              Không tìm thấy thông tin Hán tự cho từ này.
            </p>
          </div>
        </div>
      </div>
    </div>
  </div>

  <ImageModal
    :is-open="showImageModal"
    :image-url="currentImage"
    @close="showImageModal = false"
  />
</template>

<script setup lang="ts">
import { ref, onMounted, computed, onUnmounted, watch } from "vue";
import ConjugationTable from "~/components/ConjugationTable.vue";
import conjugationsData from "~/data/conjugations_normalized.json";
import KanjiStrokeInResult from "./KanjiStrokeInResult.vue";
import { useJwt } from "~/composables/useJwt";
import ImageModal from "~/components/ImageModal.vue";
// ✅ SỬA LỖI: Import đúng composable
import { useToast } from "@/composables/useToast";
// ✅ SỬA LỖI: Lấy đúng hàm 'showToast'
const { showToast } = useToast();

// --- Props and Emits ---
const props = defineProps<{
  searchWord: string;
}>();

defineEmits(["close"]);

// --- State để Reload ---
const currentSearchWord = ref(props.searchWord);

// --- Local State for Modal ---
const result = ref<any | null>(null);
const conjugationResult = ref<any | null>(null);
const loading = ref(false);
const error = ref("");
const config = useRuntimeConfig();

// --- Tab State ---
const activeTab = ref<"word" | "kanji">("word");
const kanjiResults = ref<any[] | null>(null);
const activeKanjiIndex = ref(0);

// --- Computed Props ---
const selectedWord = computed(() => {
  if (result.value && result.value.words && result.value.words.length > 0) {
    return result.value.words[0];
  }
  return null;
});

const selectedKanji = computed(() => {
  if (kanjiResults.value && kanjiResults.value[activeKanjiIndex.value]) {
    return kanjiResults.value[activeKanjiIndex.value];
  }
  return null;
});

// --- Logic "Xem thêm ảnh" ---
const showAllImages = ref(false);
const imageLimit = 6;
const visibleImages = computed(() => {
  if (!selectedWord.value || !selectedWord.value.images) return [];
  if (showAllImages.value) {
    return selectedWord.value.images;
  }
  return selectedWord.value.images.slice(0, imageLimit);
});
const showImageModal = ref(false);
const currentImage = ref("");
const openImageModal = (imageUrl: string) => {
  currentImage.value = imageUrl;
  showImageModal.value = true;
};
watch(selectedWord, (newWord) => {
  if (newWord) {
    showAllImages.value = false;
  }
});

// --- ============================================= ---
// --- START: LOGIC MỚI CHO SUGGEST WORDS ---
// --- ============================================= ---
const showAllSuggestions = ref(false);
const suggestionLimit = 6; // Giới hạn hiển thị ban đầu

const visibleSuggestions = computed(() => {
  if (!result.value || !result.value.suggestWords) return [];
  if (showAllSuggestions.value) {
    return result.value.suggestWords;
  }
  return result.value.suggestWords.slice(0, suggestionLimit);
});
// --- ============================================= ---
// --- END: LOGIC MỚI CHO SUGGEST WORDS ---
// --- ============================================= ---

// --- Logic "Lưu vào Deck" ---
const { isAuthenticated, jwt } = useJwt();
const decks = ref<any[]>([]);
const decksLoading = ref(false);
const saveLoading = ref(false);
const showDecksPanel = ref(false);
const decksButtonRef = ref<HTMLElement | null>(null);
const decksPanelRef = ref<HTMLElement | null>(null);

const toggleDecksPanel = () => {
  showDecksPanel.value = !showDecksPanel.value;
  if (showDecksPanel.value) {
    fetchDecks();
    setTimeout(() => {
      document.addEventListener("click", handleClickOutside);
    }, 0);
  } else {
    document.removeEventListener("click", handleClickOutside);
  }
};
const handleClickOutside = (event: MouseEvent) => {
  const isClickInsideButton = decksButtonRef.value?.contains(
    event.target as Node
  );
  const isClickInsidePanel = decksPanelRef.value?.contains(
    event.target as Node
  );
  if (!isClickInsideButton && !isClickInsidePanel) {
    showDecksPanel.value = false;
    document.removeEventListener("click", handleClickOutside);
  }
};
onUnmounted(() => {
  document.removeEventListener("click", handleClickOutside);
});
const fetchDecks = async () => {
  if (decks.value.length > 0) return;
  if (!isAuthenticated.value) {
    // ✅ SỬA LỖI: Dùng showToast
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
      { method: "GET", headers: headers }
    );
    decks.value = response.result || [];
  } catch (e) {
    // ✅ SỬA LỖI: Dùng showToast
    showToast("Không thể tải danh sách decks", "error");
    decks.value = [];
  } finally {
    decksLoading.value = false;
  }
};
const saveToDeck = async (deckId: string) => {
  if (!selectedWord.value || saveLoading.value) return;
  if (!isAuthenticated.value) {
    // ✅ SỬA LỖI: Dùng showToast
    showToast("Bạn cần đăng nhập để lưu thẻ", "error");
    return;
  }
  saveLoading.value = true;
  const payload = [
    {
      frontText: selectedWord.value.word,
      backText:
        selectedWord.value.means?.[0]?.mean ||
        selectedWord.value.short_mean ||
        "",
      tags: selectedWord.value.phonetic || "",
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
    showToast(
      `Đã lưu từ "${selectedWord.value.word}" vào deck.`,
      "success"
    );
  } catch (e) {
    // ✅ SỬA LỖI: Dùng showToast
    showToast("Không thể lưu thẻ, vui lòng thử lại", "error");
  } finally {
    saveLoading.value = false;
    showDecksPanel.value = false;
    document.removeEventListener("click", handleClickOutside);
  }
};
function speak(textToSpeak) {
  // Quan trọng: Phải check 'process.client' vì window.speechSynthesis chỉ tồn tại ở trình duyệt
  if (process.client) {
    if (!window.speechSynthesis) {
      console.error(
        "Rất tiếc, trình duyệt của bạn không hỗ trợ chức năng phát âm."
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
// --- Helper Functions (Giữ nguyên) ---
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
const extractKanji = (text: string): string[] => {
  const kanjiRegex = /[\u4e00-\u9faf\u3400-\u4dbf]/g;
  const matches = text.match(kanjiRegex);
  return matches ? [...new Set(matches)] : [];
};

// --- Logic Fetch Data ---
const fetchWordData = async (word: string) => {
  try {
    const conjugation = checkConjugation(word);
    conjugationResult.value = conjugation; // Gán luôn, kể cả là null

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
      result.value = null;
    }
  } catch (e: any) {
    console.error("Word fetch error:", e.message);
    error.value = e.message || "Error loading word data";
    result.value = null;
  }
};

const fetchKanjiData = async (word: string) => {
  try {
    const kanjiChars = extractKanji(word);
    if (kanjiChars.length === 0) {
      kanjiResults.value = [];
      return;
    }
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
        return response.results[0];
      } else {
        throw new Error(`Invalid kanji response for: ${char}`);
      }
    });

    const results = await Promise.allSettled(fetchPromises);
    const successfulResults = results
      .filter((r) => r.status === "fulfilled")
      .map((r) => (r as PromiseFulfilledResult<any>).value);

    kanjiResults.value = successfulResults;
    if (successfulResults.length > 0) {
      activeKanjiIndex.value = 0;
    }
  } catch (e: any) {
    console.error("Kanji fetch error:", e.message);
    kanjiResults.value = [];
  }
};

/**
 * Hàm fetchData tổng, gọi cả word và kanji.
 */
const fetchData = async (word: string) => {
  if (!word) return;

  loading.value = true;
  error.value = "";
  // Reset tất cả state
  result.value = null;
  kanjiResults.value = null;
  conjugationResult.value = null;
  activeKanjiIndex.value = 0;
  activeTab.value = "word";
  showDecksPanel.value = false;
  showAllSuggestions.value = false; // <-- THÊM RESET NÀY

  await Promise.allSettled([fetchWordData(word), fetchKanjiData(word)]);

  loading.value = false;

  if (
    !result.value &&
    (!kanjiResults.value || kanjiResults.value.length === 0)
  ) {
    error.value = `Không tìm thấy kết quả cho "${word}".`;
  }
};

// --- Logic Reload Modal ---
const searchNewWord = (word: string) => {
  if (word && word !== currentSearchWord.value) {
    currentSearchWord.value = word;
  }
};
// --- onMounted và watch ---
onMounted(() => {
  fetchData(currentSearchWord.value);
});

watch(currentSearchWord, (newWord, oldWord) => {
  if (newWord && newWord !== oldWord) {
    fetchData(newWord);
  }
});

watch(
  () => props.searchWord,
  (newWord) => {
    if (newWord && newWord !== currentSearchWord.value) {
      currentSearchWord.value = newWord;
    }
  }
);
</script>

<style scoped>
/* ✅ SỬA LỖI: Cập nhật style cho thanh cuộn */
.modal-scroll-content::-webkit-scrollbar {
  width: 8px;
}
/* Light Mode */
.modal-scroll-content::-webkit-scrollbar-track {
  background: #f1f5f9; /* bg-slate-100 */
  border-radius: 10px;
}
.modal-scroll-content::-webkit-scrollbar-thumb {
  background-color: #d1d5db; /* gray-300 */
  border-radius: 10px;
  border: 2px solid #f1f5f9; /* bg-slate-100 */
  background-clip: padding-box;
}
.modal-scroll-content::-webkit-scrollbar-thumb:hover {
  background-color: #9ca3af; /* gray-400 */
}
/* Dark Mode */
.dark .modal-scroll-content::-webkit-scrollbar-track {
  background: #1e293b; /* bg-slate-800 */
}
.dark .modal-scroll-content::-webkit-scrollbar-thumb {
  background-color: #475569; /* bg-slate-600 */
  border: 2px solid #1e293b;
}
.dark .modal-scroll-content::-webkit-scrollbar-thumb:hover {
  background-color: #64748b; /* bg-slate-500 */
}
</style>