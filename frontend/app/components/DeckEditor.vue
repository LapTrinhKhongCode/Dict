<template>
  <div
    class="min-h-screen bg-gray-50 text-gray-900 dark:bg-neutral-900 dark:text-white p-4 sm:p-8 transition-colors"
  >
    <div class="max-w-3xl mx-auto">
      <div class="mb-6 flex justify-between items-center">
        <div>
          <button
            @click="emit('go-to-list')"
            class="flex items-center text-sm text-primary-600 hover:text-primary-500 dark:text-sky-400 dark:hover:text-sky-300 transition-colors mb-2"
          >
            &larr; Quay lại danh sách
          </button>
          <h1 class="text-3xl font-bold text-primary-600 dark:text-sky-400">
            Chỉnh sửa bộ thẻ
          </h1>
        </div>
        <button
          @click="openImportModal"
          class="bg-primary-600 hover:bg-primary-700 dark:bg-blue-600 dark:hover:bg-blue-700 text-white font-bold py-2 px-4 rounded-lg flex items-center ml-4"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            class="h-5 w-5 mr-2"
            viewBox="0 0 20 20"
            fill="currentColor"
          >
            <path
              fill-rule="evenodd"
              d="M3 17a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm3.293-7.707a1 1 0 011.414 0L9 10.586V3a1 1 0 112 0v7.586l1.293-1.293a1 1 0 111.414 1.414l-3 3a1 1 0 01-1.414 0l-3-3a1 1 0 010-1.414z"
              clip-rule="evenodd"
            />
          </svg>
          Import Thẻ
        </button>
      </div>

      <div
        class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-neutral-700 rounded-lg p-5 mb-6"
      >
        <h2 class="text-xl font-bold text-red-600 dark:text-red-400 mb-3">
          Vùng nguy hiểm
        </h2>
        <p class="text-gray-600 dark:text-gray-400 text-sm mb-4">
          Hành động này không thể hoàn tác. Toàn bộ thẻ và tiến độ học sẽ bị xóa
          vĩnh viễn.
        </p>
        <button
          @click="promptDeleteDeck"
          class="bg-red-600 hover:bg-red-700 text-white font-bold py-2 px-4 rounded-lg"
        >
          Xóa vĩnh viễn bộ thẻ này
        </button>
      </div>

      <div
        class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-neutral-700 rounded-lg p-5 mb-6"
      >
        <h2 class="text-xl font-bold text-gray-900 dark:text-gray-100 mb-4">
          Thông tin bộ thẻ
        </h2>
        <div class="space-y-4">
          <div>
            <label
              for="deckName"
              class="block text-sm font-medium text-gray-600 dark:text-gray-300 mb-1"
              >Tên bộ thẻ</label
            >
            <input
              type="text"
              id="deckName"
              v-model="editableSet.title"
              class="form-input"
            />
          </div>
          <div>
            <label
              for="deckDesc"
              class="block text-sm font-medium text-gray-600 dark:text-gray-300 mb-1"
              >Mô tả</label
            >
            <textarea
              id="deckDesc"
              v-model="editableSet.description"
              rows="3"
              class="form-input"
            ></textarea>
          </div>
          <div class="flex items-center justify-between">
            <button
              @click="saveAllChanges"
              :disabled="isSavingDeck || isAddingCard"
              class="bg-primary-600 hover:bg-primary-700 dark:bg-sky-500 dark:hover:bg-sky-600 text-white font-bold py-2 px-5 rounded-lg disabled:bg-gray-300 dark:disabled:bg-gray-500 disabled:cursor-not-allowed"
            >
              <span v-if="isSavingDeck || isAddingCard">Đang lưu...</span>
              <span v-else>Lưu tất cả thay đổi</span>
            </button>
            <div class="flex items-center">
              <input
                id="isPublic"
                type="checkbox"
                v-model="editableSet.isPublic"
                class="h-4 w-4 rounded bg-gray-100 border-gray-300 text-primary-600 dark:bg-gray-700 dark:border-gray-600 dark:text-sky-500 focus:ring-primary-500 dark:focus:ring-sky-500"
              />
              <label
                for="isPublic"
                class="ml-2 block text-sm text-gray-700 dark:text-gray-300"
                >Công khai</label
              >
            </div>
          </div>
        </div>
      </div>

      <div
        class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-neutral-700 rounded-lg p-5"
      >
        <h2 class="text-xl font-bold text-gray-900 dark:text-gray-100 mb-4">
          Quản lý thẻ ({{ editableSet.cards.length }})
        </h2>

        <div
          class="flex flex-col sm:flex-row gap-4 mb-4 pb-4 border-b border-gray-200 dark:border-gray-700"
        >
          <input
            type="text"
            v-model="newCardInput.frontText"
            placeholder="Mặt trước (Ký tự *)"
            class="form-input flex-1"
          />
          <input
            type="text"
            v-model="newCardInput.backText"
            placeholder="Mặt sau (Nghĩa *)"
            class="form-input flex-1"
          />
          <input
            type="text"
            v-model="newCardInput.tags"
            placeholder="Tags (Pinyin, optional)"
            class="form-input flex-1"
          />
          <button
            @click="addCardToList"
            class="bg-blue-600 hover:bg-blue-700 dark:bg-blue-500 dark:hover:bg-blue-600 text-white font-bold py-2 px-4 rounded-lg mt-2 sm:mt-0 sm:ml-auto"
          >
            Thêm vào danh sách
          </button>
        </div>

        <div class="space-y-2 max-h-96 overflow-y-auto">
          <div
            v-if="editableSet.cards.length === 0"
            class="text-center text-gray-400 dark:text-gray-500 text-sm py-4"
          >
            Chưa có thẻ nào trong bộ này.
          </div>
          <div
            v-for="(card, index) in editableSet.cards"
            :key="card.tempId || card.id"
            class="flex items-center gap-4 p-2 rounded-lg"
            :class="
              card.isNew
                ? 'bg-blue-50 border border-blue-300 dark:bg-blue-900/50 dark:border-blue-700'
                : 'bg-gray-100 dark:bg-gray-700'
            "
          >
            <input
              type="text"
              v-model="card.charBig"
              placeholder="Ký tự"
              class="form-input-sm w-1/4"
            />
            <input
              type="text"
              v-model="card.meaning"
              placeholder="Nghĩa"
              class="form-input-sm w-1/3"
            />
            <input
              type="text"
              v-model="card.pinyin"
              placeholder="Pinyin"
              class="form-input-sm w-1/3"
            />
            <button
              v-if="!card.isNew"
              @click="updateCard(card)"
              :disabled="card.isSaving"
              class="p-2 text-primary-500 hover:text-primary-400 dark:text-sky-400 dark:hover:text-sky-300 disabled:text-gray-500 disabled:cursor-not-allowed"
              title="Lưu thẻ này"
            >
              <svg
                v-if="card.isSaving"
                class="animate-spin h-5 w-5"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
              >
                <circle
                  class="opacity-25"
                  cx="12"
                  cy="12"
                  r="10"
                  stroke="currentColor"
                  stroke-width="4"
                ></circle>
                <path
                  class="opacity-75"
                  fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                ></path>
              </svg>
              <svg
                v-else
                xmlns="http://www.w3.org/2000/svg"
                class="h-5 w-5"
                viewBox="0 0 20 20"
                fill="currentColor"
              >
                <path
                  d="M7.707 10.293a1 1 0 10-1.414 1.414l3 3a1 1 0 001.414 0l3-3a1 1 0 00-1.414-1.414L11 11.586V6a1 1 0 10-2 0v5.586L7.707 10.293zM3 17a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1z"
                />
              </svg>
            </button>
            <button
              @click="removeOrDeleteCard(card, index)"
              :disabled="card.isDeleting"
              class="p-2 text-red-500 hover:text-red-400 dark:text-red-400 dark:hover:text-red-300 disabled:text-gray-500 disabled:cursor-not-allowed"
              title="Xóa thẻ"
            >
              <svg
                v-if="card.isDeleting"
                class="animate-spin h-5 w-5"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
              >
                <circle
                  class="opacity-25"
                  cx="12"
                  cy="12"
                  r="10"
                  stroke="currentColor"
                  stroke-width="4"
                ></circle>
                <path
                  class="opacity-75"
                  fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                ></path>
              </svg>
              <svg
                v-else
                xmlns="http://www.w3.org/2000/svg"
                class="h-5 w-5"
                viewBox="0 0 20 20"
                fill="currentColor"
              >
                <path
                  fill-rule="evenodd"
                  d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z"
                  clip-rule="evenodd"
                />
              </svg>
            </button>
          </div>
        </div>
      </div>
    </div>

    <ConfirmationModal
      :is-open="isDeleteDeckModalOpen"
      title="Xác nhận XÓA Bộ Thẻ"
      :message="deleteDeckModalMessage"
      @confirm="handleConfirmDeleteDeck"
      @cancel="closeDeleteDeckModal"
    />
    <ConfirmationModal
      :is-open="isDeleteCardModalOpen"
      title="Xác nhận XÓA Thẻ"
      :message="deleteCardModalMessage"
      @confirm="handleConfirmDeleteCard"
      @cancel="closeDeleteCardModal"
    />
    <div
      v-if="showImportModal"
      class="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-70 p-4"
    >
      <div
        class="bg-white dark:bg-gray-800 text-gray-900 dark:text-white rounded-lg shadow-xl w-full max-w-2xl max-h-[90vh] flex flex-col"
      >
        <div
          class="flex justify-between items-center p-4 border-b border-gray-200 dark:border-gray-700"
        >
          <h2
            class="text-xl font-bold text-primary-600 dark:text-sky-400"
          >
            Import dữ liệu thẻ
          </h2>
          <button
            @click="closeImportModal"
            class="text-gray-400 hover:text-gray-800 dark:hover:text-white text-2xl leading-none"
          >
            &times;
          </button>
        </div>
        <div class="p-6 overflow-y-auto space-y-4">
          <p class="text-sm text-gray-600 dark:text-gray-400">
            Dán dữ liệu. Mỗi thẻ trên một dòng.
          </p>
          <textarea
            v-model="importText"
            @keydown.tab.prevent="handleTab"
            rows="8"
            class="form-input"
            placeholder="apple, quả táo..."
          ></textarea>
          <div>
            <label
              class="block text-sm font-medium text-gray-600 dark:text-gray-300 mb-2"
              >Phân cách:</label
            >
            <div class="flex gap-4">
              <label class="flex items-center">
                <input
                  type="radio"
                  v-model="importDelimiter"
                  value="tab"
                  class="form-radio"
                />
                <span class="ml-2 text-gray-700 dark:text-gray-200">Tab</span>
              </label>
              <label class="flex items-center">
                <input
                  type="radio"
                  v-model="importDelimiter"
                  value="comma"
                  class="form-radio"
                />
                <span class="ml-2 text-gray-700 dark:text-gray-200">Dấu phẩy</span>
              </label>
            </div>
          </div>
          <div>
            <h3
              class="text-lg font-semibold text-gray-800 dark:text-gray-200 mb-2"
            >
              Xem trước ({{ parsedCards.length }} thẻ)
            </h3>
            <div
              class="space-y-1 max-h-40 overflow-y-auto bg-gray-100 dark:bg-gray-900 p-3 rounded"
            >
              <div
                v-if="parsedCards.length === 0"
                class="text-center text-gray-400 dark:text-gray-500 text-sm py-2"
              >
                ...
              </div>
              <div
                v-for="(card, index) in parsedCards"
                :key="`preview-${index}`"
                class="flex gap-4 text-sm p-1 border-b border-gray-200 dark:border-gray-700 last:border-b-0"
              >
                <div
                  class="w-1/2 font-mono bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-200 px-2 py-1 rounded truncate"
                  :title="card.frontText"
                >
                  {{ card.frontText || "..." }}
                </div>
                <div
                  class="w-1/2 font-mono bg-gray-200 dark:bg-gray-700 text-gray-700 dark:text-gray-200 px-2 py-1 rounded truncate"
                  :title="card.backText"
                >
                  {{ card.backText || "..." }}
                </div>
              </div>
            </div>
            <p v-if="parseError" class="text-red-500 dark:text-red-400 text-xs mt-1">
              {{ parseError }}
            </p>
          </div>
        </div>
        <div
          class="flex justify-end gap-4 p-4 border-t border-gray-200 dark:border-gray-700"
        >
          <button
            @click="closeImportModal"
            class="px-5 py-2 rounded-lg bg-gray-200 text-gray-700 hover:bg-gray-300 dark:bg-gray-600 dark:text-white dark:hover:bg-gray-500 font-semibold transition-colors"
          >
            Hủy
          </button>
          <button
            @click="importCardsToList"
            :disabled="parsedCards.length === 0"
            class="px-5 py-2 rounded-lg bg-primary-600 hover:bg-primary-700 dark:bg-sky-500 dark:hover:bg-sky-600 text-white font-semibold transition-colors disabled:bg-gray-300 dark:disabled:bg-gray-500 disabled:cursor-not-allowed"
          >
            Thêm {{ parsedCards.length }} thẻ vào danh sách
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed } from 'vue';
import type { DeckDetailDto, CardDto, DeckUpdateDto, CardCreateDto, CardUpdateDto } from '~/types';
import ConfirmationModal from './ConfirmationModal.vue';
import { useJwt } from '~/composables/useJwt';

const { jwt } = useJwt();

interface EditableCardDto extends Omit<CardDto, 'id'> {
  id?: number;
  tempId: string;
  isNew?: boolean;
  isSaving?: boolean;
  isDeleting?: boolean;
}

const props = defineProps<{ initialSet: DeckDetailDto }>();
const emit = defineEmits(['go-to-list', 'deck-updated']);

const config = useRuntimeConfig()
const BASE_URL = config.public.apiBaseUrl

const editableSet = reactive<Omit<DeckDetailDto, 'cards'> & { cards: EditableCardDto[] }>({
  ...props.initialSet,
  cards: props.initialSet.cards.map(card => ({
    ...card,
    tempId: `card-${card.id}`,
    isNew: false
  }))
});

const newCardInput = ref<CardCreateDto>({ frontText: '', backText: '', tags: '' });

const isSavingDeck = ref(false);
const isAddingCard = ref(false);

const isDeleteDeckModalOpen = ref(false);
const deleteDeckModalMessage = ref('');

const isDeleteCardModalOpen = ref(false);
const deleteCardModalMessage = ref('');
const cardToDelete = ref<EditableCardDto | null>(null);
const cardIndexToDelete = ref<number | null>(null);

const showImportModal = ref(false);
const importText = ref('');
const importDelimiter = ref<'tab' | 'comma'>('comma');
const parseError = ref<string | null>(null);

// handleResponse
async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const errorText = await response.text();
    let errorMessage = errorText;
    try {
      const errorJson = JSON.parse(errorText);
      if (errorJson?.errors) { errorMessage = Object.values(errorJson.errors).flat().join(' '); }
      else if (errorJson?.message) { errorMessage = errorJson.message; }
      else if (errorJson?.title) { errorMessage = errorJson.title; }
    } catch (e) { /* Ignore */ }
    throw new Error(errorMessage || `Yêu cầu thất bại: ${response.status}`);
  }
  if (response.status === 204) return {} as T;
  const data = await response.json();
  if (data.isSuccess === false) { throw new Error(data.message || 'Lỗi không xác định từ API'); }
  return (data.result === undefined ? data : data.result) as T;
}

// --- Deck Operations ---
async function saveDeckInfoOnly() {
  isSavingDeck.value = true;
  const deckId = editableSet.id;
  const deckDto: DeckUpdateDto = {
    Title: editableSet.title,
    Description: editableSet.description,
    IsPublic: editableSet.isPublic ?? false
  };
  try {
    const response = await fetch(`${BASE_URL}/api/decks/${deckId}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${jwt.value}` },
      body: JSON.stringify(deckDto)
    });
    await handleResponse(response);
    return true;
  } catch (err: any) {
    alert(`Lỗi cập nhật Deck: ${err.message}`);
    console.error("saveDeckInfoOnly Error:", err);
    return false;
  } finally {
    // Don't reset isSavingDeck here, let saveAllChanges do it
  }
}

function promptDeleteDeck() {
  deleteDeckModalMessage.value = `Hành động này sẽ xóa vĩnh viễn bộ thẻ "${editableSet.title}". Toàn bộ tiến độ học sẽ bị mất.`;
  isDeleteDeckModalOpen.value = true;
}

function closeDeleteDeckModal() { isDeleteDeckModalOpen.value = false; }

async function handleConfirmDeleteDeck() {
  closeDeleteDeckModal();
  isSavingDeck.value = true;
  const deckId = editableSet.id;
  try {
    const response = await fetch(`${BASE_URL}/api/decks/${deckId}`, {
      method: 'DELETE',
      headers: { 'Authorization': `Bearer ${jwt.value}` }
    });
    await handleResponse(response);
    alert('Đã xóa Deck thành công.');
    emit('go-to-list');
  } catch (err: any) {
    alert(`Lỗi xóa Deck: ${err.message}`);
    console.error("handleConfirmDeleteDeck Error:", err);
  } finally {
    isSavingDeck.value = false;
  }
}

// --- Card Operations ---
function addCardToList() {
  const front = newCardInput.value.frontText?.trim();
  const back = newCardInput.value.backText?.trim();
  if (!front || !back) { alert("Mặt trước và Mặt sau là bắt buộc."); return; }
  const tempCard: EditableCardDto = {
    tempId: `new-${Date.now()}-${Math.random()}`,
    charBig: front,
    meaning: back,
    pinyin: newCardInput.value.tags || '',
    nextReviewAt: '',
    isNew: true,
  };
  editableSet.cards.push(tempCard);
  newCardInput.value = { frontText: '', backText: '', tags: '' };
}

async function updateCard(card: EditableCardDto) {
  if (card.isNew || card.isSaving || !card.id) return;
  card.isSaving = true;
  const cardId = card.id;
  const cardDto: CardUpdateDto = {
    FrontText: card.charBig,
    BackText: card.meaning,
    Tags: card.pinyin
  };
  try {
    const response = await fetch(`${BASE_URL}/api/cards/${cardId}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${jwt.value}` },
      body: JSON.stringify(cardDto)
    });
    await handleResponse(response);
  } catch (err: any) { alert(`Lỗi cập nhật thẻ ${cardId}: ${err.message}`); }
  finally { card.isSaving = false; }
}

function removeOrDeleteCard(card: EditableCardDto, index: number) {
  if (card.isNew) {
    editableSet.cards.splice(index, 1);
  } else {
    promptDeleteCard(card, index);
  }
}

function promptDeleteCard(card: EditableCardDto, index: number) {
  if (!card.id) { console.error("Invalid card for deletion:", card); return; }
  deleteCardModalMessage.value = `Bạn có chắc muốn xóa thẻ "${card.meaning || card.charBig}" không?`;
  cardToDelete.value = card;
  cardIndexToDelete.value = index;
  isDeleteCardModalOpen.value = true;
}

function closeDeleteCardModal() {
  isDeleteCardModalOpen.value = false;
  cardToDelete.value = null;
  cardIndexToDelete.value = null;
}

async function handleConfirmDeleteCard() {
  if (!cardToDelete.value || cardToDelete.value.isDeleting || !cardToDelete.value.id) return;
  const card = cardToDelete.value;
  const cardId = card.id;
  const indexToRemove = cardIndexToDelete.value;

  closeDeleteCardModal();
  card.isDeleting = true;

  try {
    const response = await fetch(`${BASE_URL}/api/cards/${cardId}`, {
      method: 'DELETE',
      headers: { 'Authorization': `Bearer ${jwt.value}` }
    });
    await handleResponse(response);
    if (indexToRemove !== null) {
      editableSet.cards.splice(indexToRemove, 1);
    }
  } catch (err: any) {
    alert(`Lỗi xóa thẻ ${cardId}: ${err.message}`);
    console.error("deleteCard Error:", err);
    if (card) card.isDeleting = false;
  }
}

// --- Import Logic ---
const parsedCards = computed(() => {
  parseError.value = null;
  const lines = importText.value.split('\n').filter(line => line.trim() !== '');
  const delimiter = importDelimiter.value === 'tab' ? '\t' : ',';
  const cards: CardCreateDto[] = [];
  let lineErrors = 0;
  lines.forEach((line) => {
    const parts = line.split(delimiter);
    if (parts.length >= 2) {
      const front = parts[0]?.trim() ?? '';
      const back = parts[1]?.trim() ?? '';
      const tags = parts.slice(2).join(delimiter).trim() || null;
      if (front && back) {
        cards.push({ frontText: front, backText: back, tags: tags });
      } else { lineErrors++; }
    } else if (line.trim()) { lineErrors++; }
  });
  if (lineErrors > 0) { parseError.value = `Đã bỏ qua ${lineErrors} dòng không đúng định dạng.`; }
  return cards;
});

function importCardsToList() {
  const cardsToImport = parsedCards.value;
  if (cardsToImport.length === 0) { alert("Không có thẻ hợp lệ nào để import."); return; }
  cardsToImport.forEach(parsedCard => {
    const tempCard: EditableCardDto = {
      tempId: `new-${Date.now()}-${Math.random()}`,
      charBig: parsedCard.frontText,
      meaning: parsedCard.backText,
      pinyin: parsedCard.tags || '',
      nextReviewAt: '',
      isNew: true,
    };
    editableSet.cards.push(tempCard);
  });
  closeImportModal();
  alert(`Đã thêm ${cardsToImport.length} thẻ vào danh sách chờ lưu.`);
}

function openImportModal() {
  importText.value = '';
  parseError.value = null;
  showImportModal.value = true;
}
function closeImportModal() { showImportModal.value = false; }
function handleTab(event: KeyboardEvent) {
  const target = event.target as HTMLTextAreaElement;
  const start = target.selectionStart;
  const end = target.selectionEnd;
  target.value = target.value.substring(0, start) + "\t" + target.value.substring(end);
  target.selectionStart = target.selectionEnd = start + 1;
}

// --- Save All ---
async function saveAllChanges() {
  isSavingDeck.value = true;
  const deckSaveSuccess = await saveDeckInfoOnly();
  if (!deckSaveSuccess) { isSavingDeck.value = false; return; }

  const newCardsToApi = editableSet.cards
    .filter(card => card.isNew)
    .map(tempCard => ({
      frontText: tempCard.charBig,
      backText: tempCard.meaning,
      tags: tempCard.pinyin || null
    } as CardCreateDto));

  let cardAddSuccess = true;
  if (newCardsToApi.length > 0) {
    isAddingCard.value = true;
    const deckId = editableSet.id;
    try {
      const response = await fetch(`${BASE_URL}/api/decks/${deckId}/cards`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${jwt.value}` },
        body: JSON.stringify(newCardsToApi)
      });
      const addedCardsData = await handleResponse<{ result?: CardDto[] }>(response);
      if (addedCardsData.result && addedCardsData.result.length > 0) {
        const existingCards = editableSet.cards.filter(card => !card.isNew);
        const newApiCards = addedCardsData.result.map(newApiCard => ({
          ...newApiCard,
          tempId: `card-${newApiCard.id}`,
          isNew: false
        }));
        editableSet.cards = [...existingCards, ...newApiCards];
      } else {
        console.warn("API did not return created card data after bulk add.");
        cardAddSuccess = false;
      }
    } catch (err: any) {
      alert(`Lỗi khi lưu các thẻ mới: ${err.message}`);
      cardAddSuccess = false;
    } finally {
      isAddingCard.value = false;
    }
  }

  isSavingDeck.value = false;

  if (deckSaveSuccess && cardAddSuccess) {
    alert('Đã lưu tất cả thay đổi thành công!');
    emit('deck-updated');
  } else if (deckSaveSuccess && !cardAddSuccess) {
    //  alert('Đã lưu thông tin deck, nhưng có lỗi khi lưu thẻ mới. Vui lòng tải lại trang.');
    emit('deck-updated');
  }
}
</script>

<style scoped>
/* THAY ĐỔI:
  - Tách style mặc định (light mode) ra.
  - Bọc các style cũ (dark mode) trong class .dark
*/

/* --- LIGHT MODE (MẶC ĐỊNH) --- */
.form-input {
  width: 100%;
  background-color: #ffffff;
  border: 1px solid #d1d5db; /* border-gray-300 */
  border-radius: 0.5rem;
  padding: 0.5rem 0.75rem;
  color: #111827; /* text-gray-900 */
  outline: none;
  transition: border-color 0.2s, box-shadow 0.2s;
}
.form-input:focus {
  border-color: #2563eb; /* primary-600 */
  box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.4); /* primary shadow */
}

.form-input-sm {
  background-color: #ffffff;
  border: 1px solid #d1d5db; /* border-gray-300 */
  border-radius: 0.375rem;
  padding: 0.25rem 0.5rem;
  color: #111827; /* text-gray-900 */
  outline: none;
  transition: border-color 0.2s, box-shadow 0.2s;
}
.form-input-sm:focus {
  border-color: #2563eb; /* primary-600 */
  box-shadow: 0 0 0 1px #2563eb;
}

.form-radio {
  height: 1rem;
  width: 1rem;
  color: #2563eb; /* primary-600 */
  background-color: #f3f4f6; /* bg-gray-100 */
  border-color: #d1d5db; /* border-gray-300 */
  border-radius: 100%;
  vertical-align: middle;
  appearance: none;
  position: relative;
}
.form-radio:checked {
  background-color: #2563eb; /* primary-600 */
  border-color: #2563eb; /* primary-600 */
}
.form-radio:checked::before {
  content: '';
  display: block;
  width: 0.5rem;
  height: 0.5rem;
  border-radius: 50%;
  background-color: white;
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
}
.form-radio:focus {
  outline: none;
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.4), 0 0 0 1px #2563eb;
}

/* --- DARK MODE --- */
.dark .form-input {
  background-color: #1f2937;
  border: 1px solid #4b5563;
  color: white;
}
.dark .form-input:focus {
  border-color: #0ea5e9;
  box-shadow: 0 0 0 2px rgba(14, 165, 233, 0.4);
}

.dark .form-input-sm {
  background-color: #1f2937;
  border: 1px solid #4b5563;
  color: white;
}
.dark .form-input-sm:focus {
  border-color: #0ea5e9;
  box-shadow: 0 0 0 1px #0ea5e9;
}

.dark .form-radio {
  color: #0369a1;
  background-color: #374151;
  border-color: #4b5563;
}
.dark .form-radio:checked {
  background-color: #0ea5e9;
  border-color: #0ea5e9;
}
.dark .form-radio:focus {
  outline: none;
  box-shadow: 0 0 0 3px rgba(14, 165, 233, 0.4), 0 0 0 1px #0ea5e9;
}
</style>