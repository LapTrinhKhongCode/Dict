<template>
  <div class="min-h-screen text-white p-4 sm:p-8">
    <div class="max-w-3xl mx-auto">

      <!-- Header -->
      <div class="mb-6 flex justify-between items-center"> 
        <div>
          <button @click="emit('go-to-list')" class="flex items-center text-sm text-sky-400 hover:text-sky-300 transition-colors mb-2">
            &larr; Quay lại danh sách
          </button>
          <h1 class="text-3xl font-bold text-sky-400">Chỉnh sửa bộ thẻ</h1>
        </div>
        <button @click="openImportModal" class="bg-blue-600 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded-lg flex items-center ml-4">
           <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 mr-2" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M3 17a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm3.293-7.707a1 1 0 011.414 0L9 10.586V3a1 1 0 112 0v7.586l1.293-1.293a1 1 0 111.414 1.414l-3 3a1 1 0 01-1.414 0l-3-3a1 1 0 010-1.414z" clip-rule="evenodd" /></svg>
          Import Thẻ
        </button>
      </div>

      <div class="bg-gray-800 rounded-lg p-5 mb-6">
        <h2 class="text-xl font-bold text-red-400 mb-3">Vùng nguy hiểm</h2>
        <p class="text-gray-400 text-sm mb-4">
          Hành động này không thể hoàn tác. Toàn bộ thẻ và tiến độ học sẽ bị xóa vĩnh viễn.
        </p>
        <button @click="promptDeleteDeck" class="bg-red-600 hover:bg-red-700 text-white font-bold py-2 px-4 rounded-lg">
          Xóa vĩnh viễn bộ thẻ này
        </button>
      </div>

      <div class="bg-gray-800 rounded-lg p-5 mb-6">
        <h2 class="text-xl font-bold text-gray-100 mb-4">Thông tin bộ thẻ</h2>
        <div class="space-y-4">
          <div>
            <label for="deckName" class="block text-sm font-medium text-gray-300 mb-1">Tên bộ thẻ</label>
            <input type="text" id="deckName" v-model="editableSet.title" class="form-input" />
          </div>
          <div>
            <label for="deckDesc" class="block text-sm font-medium text-gray-300 mb-1">Mô tả</label>
            <textarea id="deckDesc" v-model="editableSet.description" rows="3" class="form-input"></textarea>
          </div>
          <div class="flex items-center justify-between">
            <button @click="saveDeckInfo" :disabled="isSavingDeck" class="bg-sky-500 hover:bg-sky-600 text-white font-bold py-2 px-5 rounded-lg disabled:bg-gray-500 disabled:cursor-not-allowed">
              <span v-if="isSavingDeck">Đang lưu...</span>
              <span v-else>Lưu thông tin</span>
            </button>
            <div class="flex items-center">
              <input id="isPublic" type="checkbox" v-model="editableSet.isPublic" @change="saveDeckInfo" class="h-4 w-4 rounded bg-gray-700 border-gray-600 text-sky-500 focus:ring-sky-500">
              <label for="isPublic" class="ml-2 block text-sm text-gray-300">Công khai</label>
            </div>
          </div>
        </div>
      </div>


      <div class="bg-gray-800 rounded-lg p-5">
        <h2 class="text-xl font-bold text-gray-100 mb-4">Quản lý thẻ ({{ editableSet.cards.length }})</h2>


        <div class="flex flex-col sm:flex-row gap-4 mb-4 pb-4 border-b border-gray-700">
          <input type="text" v-model="newCardInput.frontText" placeholder="Mặt trước (Ký tự *)" class="form-input flex-1">
          <input type="text" v-model="newCardInput.backText" placeholder="Mặt sau (Nghĩa *)" class="form-input flex-1">
          <input type="text" v-model="newCardInput.tags" placeholder="Tags (Pinyin, optional)" class="form-input flex-1">
          <button @click="addCard" :disabled="isAddingCard" class="bg-green-500 hover:bg-green-600 text-white font-bold py-2 px-4 rounded-lg mt-2 sm:mt-0 sm:ml-auto disabled:bg-gray-500 disabled:cursor-not-allowed">
             <span v-if="isAddingCard">Đang thêm...</span>
             <span v-else>Thêm</span>
          </button>
        </div>


        <div class="space-y-2 max-h-96 overflow-y-auto">
          <div v-if="editableSet.cards.length === 0" class="text-center text-gray-500 text-sm py-4">Chưa có thẻ nào trong bộ này.</div>
          <div v-for="card in editableSet.cards" :key="card.id" class="flex items-center gap-4 p-2 bg-gray-700 rounded-lg">
            <input type="text" v-model="card.charBig" placeholder="Ký tự" class="form-input-sm w-1/4">
            <input type="text" v-model="card.meaning" placeholder="Nghĩa" class="form-input-sm w-1/3">
            <input type="text" v-model="card.pinyin" placeholder="Pinyin" class="form-input-sm w-1/3">
            <button @click="updateCard(card)" :disabled="card.isSaving" class="p-2 text-sky-400 hover:text-sky-300 disabled:text-gray-500 disabled:cursor-not-allowed" title="Lưu thẻ">
                <svg v-if="card.isSaving" class="animate-spin h-5 w-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>
                <svg v-else xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor"><path d="M7.707 10.293a1 1 0 10-1.414 1.414l3 3a1 1 0 001.414 0l3-3a1 1 0 00-1.414-1.414L11 11.586V6a1 1 0 10-2 0v5.586L7.707 10.293zM3 17a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1z" /></svg>
            </button>
            <button @click="promptDeleteCard(card)" :disabled="card.isDeleting" class="p-2 text-red-400 hover:text-red-300 disabled:text-gray-500 disabled:cursor-not-allowed" title="Xóa thẻ">
               <svg v-if="card.isDeleting" class="animate-spin h-5 w-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>
               <svg v-else xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" /></svg>
            </button>
          </div>
        </div>
      </div>
    </div>

    <ConfirmationModal
      :is-open="isDeleteDeckModalOpen"
      title="Xác nhận XÓA Bộ Thẻ"
      :message="deleteDeckModalMessage"
      confirmation-text="delete"
      @confirm="handleConfirmDeleteDeck"
      @cancel="closeDeleteDeckModal"
    />
    <ConfirmationModal
      :is-open="isDeleteCardModalOpen"
      title="Xác nhận XÓA Thẻ"
      :message="deleteCardModalMessage"
      confirmation-text="delete"
      @confirm="handleConfirmDeleteCard"
      @cancel="closeDeleteCardModal"
    />
    <div v-if="showImportModal" class="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-75 p-4">
      <div class="bg-gray-800 rounded-lg shadow-xl w-full max-w-2xl max-h-[90vh] flex flex-col">
        {/* Modal Header */}
        <div class="flex justify-between items-center p-4 border-b border-gray-700">
          <h2 class="text-xl font-bold text-sky-400">Import dữ liệu thẻ</h2>
          <button @click="closeImportModal" class="text-gray-400 hover:text-white text-2xl leading-none">&times;</button>
        </div>

        <div class="p-6 overflow-y-auto space-y-4">
          <p class="text-sm text-gray-400">Dán dữ liệu của bạn vào đây. Mỗi thẻ nằm trên một dòng riêng.</p>
          <textarea
            v-model="importText"
            @keydown.tab.prevent="handleTab"
            rows="8"
            class="form-input"
            placeholder="Ví dụ:&#10;apple, quả táo&#10;banana, quả chuối&#10;hoặc&#10;hello    xin chào"
          ></textarea>

          <div>
            <label class="block text-sm font-medium text-gray-300 mb-2">Phân cách giữa Mặt trước và Mặt sau:</label>
            <div class="flex gap-4">
              <label class="flex items-center">
                <input type="radio" v-model="importDelimiter" value="tab" class="form-radio">
                <span class="ml-2">Tab</span>
              </label>
              <label class="flex items-center">
                <input type="radio" v-model="importDelimiter" value="comma" class="form-radio">
                <span class="ml-2">Dấu phẩy (,)</span>
              </label>
            </div>
          </div>

          <div>
            <h3 class="text-lg font-semibold text-gray-200 mb-2">Xem trước ({{ parsedCards.length }} thẻ)</h3>
            <div class="space-y-1 max-h-40 overflow-y-auto bg-gray-900 p-3 rounded">
              <div v-if="parsedCards.length === 0" class="text-center text-gray-500 text-sm py-2">Chưa có gì để xem trước.</div>
              <div v-for="(card, index) in parsedCards" :key="`preview-${index}`" class="flex gap-4 text-sm p-1 border-b border-gray-700 last:border-b-0">
                <div class="w-1/2 font-mono bg-gray-700 px-2 py-1 rounded truncate" :title="card.frontText">{{ card.frontText || '...' }}</div>
                <div class="w-1/2 font-mono bg-gray-700 px-2 py-1 rounded truncate" :title="card.backText">{{ card.backText || '...' }}</div>
              </div>
            </div>
             <p v-if="parseError" class="text-red-400 text-xs mt-1">{{ parseError }}</p>
          </div>
        </div>
        <div class="flex justify-end gap-4 p-4 border-t border-gray-700">
          <button @click="closeImportModal" class="px-5 py-2 rounded-lg bg-gray-600 hover:bg-gray-500 font-semibold transition-colors">
            Hủy
          </button>
          <button
            @click="importCards"
            :disabled="parsedCards.length === 0 || isAddingCard"
            class="px-5 py-2 rounded-lg bg-sky-500 hover:bg-sky-600 font-semibold transition-colors disabled:bg-gray-500 disabled:cursor-not-allowed"
          >
            <span v-if="isAddingCard">Đang import...</span>
            <span v-else>Import {{ parsedCards.length }} thẻ</span>
          </button>
        </div>
      </div>
    </div>

  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue'; // ✅ THÊM computed, watch
import type { DeckDetailDto, CardDto, DeckUpdateDto, CardCreateDto, CardUpdateDto } from '~/types';
import ConfirmationModal from './ConfirmationModal.vue';
import { useJwt } from '~/composables/useJwt';

const { jwt } = useJwt();

interface EditableCardDto extends CardDto {
  isSaving?: boolean;
  isDeleting?: boolean;
}

const props = defineProps<{ initialSet: DeckDetailDto }>();
const emit = defineEmits(['go-to-list', 'deck-updated']);

const BASE_URL = 'https://localhost:7084';

const editableSet = reactive<DeckDetailDto>(JSON.parse(JSON.stringify(props.initialSet)));
editableSet.cards = editableSet.cards.map(card => ({ ...card, isSaving: false, isDeleting: false }));

const newCardInput = ref<CardCreateDto>({ frontText: '', backText: '', tags: '' });

const isSavingDeck = ref(false);
const isAddingCard = ref(false); // Shared flag for single add and import

// Delete Deck Modal State
const isDeleteDeckModalOpen = ref(false);
const deleteDeckModalMessage = ref('');

// Delete Card Modal State
const isDeleteCardModalOpen = ref(false);
const deleteCardModalMessage = ref('');
const cardToDelete = ref<EditableCardDto | null>(null);

// ✅ THÊM: State cho Modal Import
const showImportModal = ref(false);
const importText = ref('');
const importDelimiter = ref<'tab' | 'comma'>('comma');
const parseError = ref<string | null>(null); // To show parsing errors

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
async function saveDeckInfo() { /* ... implementation ... */ }
function promptDeleteDeck() { /* ... implementation ... */ }
function closeDeleteDeckModal() { /* ... implementation ... */ }
async function handleConfirmDeleteDeck() { /* ... implementation ... */ }


// --- Card Operations ---
async function addCard() {
    // Uses newCardInput
    const cardDto: CardCreateDto = { /* ... create DTO ... */ };
    if (!cardDto.frontText?.trim() || !cardDto.backText?.trim()) { /* ... validation ... */ return; }
    await addCardsApiCall([cardDto]); // Call helper
    newCardInput.value = { frontText: '', backText: '', tags: '' }; // Reset form after success
}

async function updateCard(card: EditableCardDto) { /* ... implementation ... */ }
function promptDeleteCard(card: EditableCardDto) { /* ... implementation ... */ }
function closeDeleteCardModal() { /* ... implementation ... */ }
async function handleConfirmDeleteCard() { /* ... implementation ... */ }


// ✅ THÊM: Logic parse và preview
const parsedCards = computed(() => {
  parseError.value = null; // Reset error on change
  const lines = importText.value.split('\n').filter(line => line.trim() !== '');
  const delimiter = importDelimiter.value === 'tab' ? '\t' : ',';
  const cards: CardCreateDto[] = [];
  let lineErrors = 0;

  lines.forEach((line, index) => {
    const parts = line.split(delimiter);
    // Expecting exactly 2 or 3 parts (Front, Back, optional Tags)
    if (parts.length >= 2) {
        const front = parts[0]?.trim() ?? '';
        const back = parts[1]?.trim() ?? '';
        const tags = parts[2]?.trim() || null; // Tags are optional
        if (front && back) {
            cards.push({ frontText: front, backText: back, tags: tags });
        } else {
             lineErrors++;
             console.warn(`Skipping line ${index + 1} due to empty front/back: "${line}"`);
        }
    } else if (line.trim()) { // Only count non-empty lines with wrong format as errors
        lineErrors++;
        console.warn(`Skipping line ${index + 1} due to incorrect delimiter count: "${line}"`);
    }
  });

  if(lineErrors > 0) {
      parseError.value = `Đã bỏ qua ${lineErrors} dòng không đúng định dạng (cần ít nhất 2 cột được phân cách bởi '${delimiter === '\t' ? 'Tab' : ','}').`;
  }

  return cards;
});

// ✅ THÊM: Hàm gọi API chung để thêm thẻ (nhận mảng CardCreateDto)
async function addCardsApiCall(cardsToAdd: CardCreateDto[]) {
    if (cardsToAdd.length === 0) return; // Nothing to add

    const deckId = editableSet.id;
    isAddingCard.value = true; // Use the shared loading flag

    try {
        const response = await fetch(`${BASE_URL}/api/decks/${deckId}/cards`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${jwt.value}`
            },
            body: JSON.stringify(cardsToAdd) // Send the array
        });
        const newCardsData = await handleResponse<{ result?: CardDto[] }>(response);

        if (newCardsData.result && newCardsData.result.length > 0) {
            newCardsData.result.forEach(newApiCard => {
                editableSet.cards.push({ ...newApiCard, isSaving: false, isDeleting: false });
            });
            return true; // Indicate success
        } else {
            console.warn("API did not return created card data. Re-fetching might be needed.");
            emit('deck-updated'); // Trigger full refresh as fallback
            return false; // Indicate potential issue
        }
    } catch (err: any) {
        alert(`Lỗi thêm thẻ: ${err.message}`);
        console.error("addCardsApiCall Error:", err);
        return false; // Indicate failure
    } finally {
        isAddingCard.value = false;
    }
}


// ✅ THÊM: Các hàm xử lý Modal Import
function openImportModal() {
  importText.value = '';
  parseError.value = null; // Reset error
  showImportModal.value = true;
}

function closeImportModal() {
  showImportModal.value = false;
}

async function importCards() {
  const cardsToImport = parsedCards.value;
  if (cardsToImport.length === 0) {
      alert("Không có thẻ hợp lệ nào để import.");
      return;
  }

  const success = await addCardsApiCall(cardsToImport);

  if (success) {
      closeImportModal();
      alert(`Đã import thành công ${cardsToImport.length} thẻ!`);
  }
  // Error is handled within addCardsApiCall via alert
}


// ✅ THÊM: Hàm xử lý sự kiện nhấn phím Tab trong textarea import
function handleTab(event: KeyboardEvent) {
  const target = event.target as HTMLTextAreaElement;
  const start = target.selectionStart;
  const end = target.selectionEnd;
  target.value = target.value.substring(0, start) + "\t" + target.value.substring(end);
  target.selectionStart = target.selectionEnd = start + 1;
}

</script>

<style scoped>
.form-input {
  width: 100%;
  background-color: #1f2937;
  border: 1px solid #4b5563;
  border-radius: 0.5rem;
  padding: 0.5rem 0.75rem;
  color: white;
  outline: none;
  transition: border-color 0.2s, box-shadow 0.2s;
}
.form-input:focus {
  border-color: #0ea5e9;
  box-shadow: 0 0 0 2px rgba(14, 165, 233, 0.4);
}
.form-input-sm {
  background-color: #1f2937;
  border: 1px solid #4b5563;
  border-radius: 0.375rem;
  padding: 0.25rem 0.5rem;
  color: white;
  outline: none;
  transition: border-color 0.2s, box-shadow 0.2s;
}
.form-input-sm:focus {
  border-color: #0ea5e9;
  box-shadow: 0 0 0 1px #0ea5e9;
}
/* Style cho radio button */
.form-radio {
  height: 1rem;
  width: 1rem;
  color: #0369a1;
  background-color: #374151;
  border-color: #4b5563;
  border-radius: 100%;
  vertical-align: middle;
  appearance: none;
  position: relative;
}
.form-radio:checked {
   background-color: #0ea5e9;
   border-color: #0ea5e9;
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
   box-shadow: 0 0 0 3px rgba(14, 165, 233, 0.4), 0 0 0 1px #0ea5e9;
}
</style>

