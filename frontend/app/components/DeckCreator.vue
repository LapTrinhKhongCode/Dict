<template>
  <div class="min-h-screen text-white p-4 sm:p-8">
    <div class="max-w-3xl mx-auto">

      <!-- Header -->
      <div class="mb-6 flex justify-between items-center">
        <div>
          <button @click="emit('go-back')" class="flex items-center text-sm text-sky-400 hover:text-sky-300 transition-colors mb-2">
            &larr; Quay lại
          </button>
          <h1 class="text-3xl font-bold text-sky-400">Tạo sổ tay mới</h1>
        </div>
         <!-- Nút mở Modal Import -->
        <button @click="openImportModal" class="bg-blue-600 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded-lg flex items-center">
           <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 mr-2" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M3 17a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm3.293-7.707a1 1 0 011.414 0L9 10.586V3a1 1 0 112 0v7.586l1.293-1.293a1 1 0 111.414 1.414l-3 3a1 1 0 01-1.414 0l-3-3a1 1 0 010-1.414z" clip-rule="evenodd" /></svg>
          Import
        </button>
      </div>

      <!-- Form create Deck -->
      <div class="bg-gray-800 rounded-lg p-5 mb-6 space-y-4">
        <!-- Deck Info fields -->
         <div>
          <label for="deckName" class="block text-sm font-medium text-gray-300 mb-1">Tên bộ thẻ <span class="text-red-400">*</span></label>
          <input type="text" id="deckName" v-model="newDeck.title" class="form-input" required />
        </div>
        <div>
          <label for="deckDesc" class="block text-sm font-medium text-gray-300 mb-1">Mô tả</label>
          <textarea id="deckDesc" v-model="newDeck.description" rows="3" class="form-input"></textarea>
        </div>
        <div class="flex items-center">
          <input id="isPublic" type="checkbox" v-model="newDeck.isPublic" class="h-4 w-4 rounded bg-gray-700 border-gray-600 text-sky-500 focus:ring-sky-500">
          <label for="isPublic" class="ml-2 block text-sm text-gray-300">Công khai</label>
        </div>
      </div>

      <!-- Add initial Cards (Manual Add) -->
      <div class="bg-gray-800 rounded-lg p-5 mb-6">
        <h2 class="text-xl font-bold text-gray-100 mb-4">Thêm thẻ thủ công ({{ newDeck.cards.length }})</h2>
        <div class="flex flex-col sm:flex-row gap-4 mb-4 pb-4 border-b border-gray-700">
          <input type="text" v-model="newCardInput.frontText" placeholder="Ký tự *" class="form-input flex-1">
          <input type="text" v-model="newCardInput.backText" placeholder="Nghĩa *" class="form-input flex-1">
          <button @click="addCardToList" class="bg-blue-500 hover:bg-blue-600 text-white font-bold py-2 px-4 rounded-lg mt-2 sm:mt-0 sm:ml-auto">Thêm vào danh sách</button>
        </div>
        <div class="space-y-2 max-h-60 overflow-y-auto">
          <!-- List of manually added cards -->
          <div v-for="(card, index) in newDeck.cards" :key="index" class="flex items-center gap-4 p-2 bg-gray-700 rounded-lg text-sm">
            <div class="w-2/5 font-semibold truncate">{{ card.frontText }}</div>
            <div class="w-3/5 truncate">{{ card.backText }}</div>
            <button @click="removeCardFromList(index)" class="p-1 text-red-400 hover:text-red-300 ml-auto flex-shrink-0" title="Xóa khỏi danh sách">
                <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" /></svg>
            </button>
          </div>
          <p v-if="newDeck.cards.length === 0" class="text-center text-gray-500 text-sm py-4">Chưa có thẻ nào được thêm.</p>
        </div>
      </div>

       <!-- Create Deck Button -->
       <div class="text-right">
         <button
            @click="createDeck"
            :disabled="isSaving || !newDeck.title"
            class="bg-green-600 hover:bg-green-700 text-white font-bold py-2 px-6 rounded-lg disabled:bg-gray-500 disabled:cursor-not-allowed"
          >
            <span v-if="isSaving">Đang tạo...</span>
            <span v-else>Tạo bộ thẻ</span>
          </button>
       </div>
    </div>

    <!-- Modal Import -->
    <div v-if="showImportModal" class="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div class="bg-gray-800 rounded-lg shadow-xl w-full max-w-6xl max-h-[150vh] flex flex-col">
        <!-- Modal Header -->
        <div class="flex justify-between items-center p-4 border-b border-gray-700">
          <h2 class="text-xl font-bold text-sky-400">Import dữ liệu thẻ</h2>
          <button @click="closeImportModal" class="text-gray-400 hover:text-white ">&times;</button>
        </div>

        <!-- Modal Body -->
        <div class="p-6 overflow-y-auto space-y-4">
          <!-- <p class="text-sm text-gray-400">Dán dữ liệu của bạn vào đây (từ Word, Excel, Google Docs, etc.). Mỗi thẻ nằm trên một dòng riêng.</p> -->
          <!-- ✅ SỬA: Thêm @keydown.tab.prevent="handleTab" -->
          <textarea
            v-model="importText"
            @keydown.tab.prevent="handleTab"
            rows="8"
            class="form-input"
            placeholder="Ví dụ:&#10;apple, quả táo&#10;banana, quả chuối&#10;hoặc&#10;hello    xin chào"
          ></textarea>

          <!-- Delimiter Options -->
          <div>
            <label class="block text-sm font-medium text-gray-300 mb-2">Phân cách giữa Ký tự và Nghĩa:</label>
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

          <!-- Preview Section -->
          <div>
            <h3 class="text-lg font-semibold text-gray-200 mb-2">Xem trước ({{ parsedCards.length }} thẻ)</h3>
            <div class="space-y-1 max-h-40 overflow-y-auto bg-gray-900 p-3 rounded">
              <div v-if="parsedCards.length === 0" class="text-center text-gray-500 text-sm py-2">Chưa có gì để xem trước.</div>
              <div v-for="(card, index) in parsedCards" :key="index" class="flex gap-4 text-sm p-1 border-b border-gray-700 last:border-b-0">
                <div class="w-1/2 font-mono bg-gray-700 px-2 py-1 rounded truncate" :title="card.frontText">{{ card.frontText || '...' }}</div>
                <div class="w-1/2 font-mono bg-gray-700 px-2 py-1 rounded truncate" :title="card.backText">{{ card.backText || '...' }}</div>
              </div>
            </div>
          </div>
        </div>

        <!-- Modal Footer -->
        <div class="flex justify-end gap-4 p-4 border-t border-gray-700">
          <button @click="closeImportModal" class="px-5 py-2 rounded-lg bg-gray-600 hover:bg-gray-500 font-semibold transition-colors">
            Hủy
          </button>
          <button
            @click="importCards"
            :disabled="parsedCards.length === 0"
            class="px-5 py-2 rounded-lg bg-sky-500 hover:bg-sky-600 font-semibold transition-colors disabled:bg-gray-500 disabled:cursor-not-allowed"
          >
            Import {{ parsedCards.length }} thẻ
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue';
import type { DeckCreateDto, CardCreateDto } from '~/types';

const emit = defineEmits(['go-back', 'deck-created']);

const BASE_URL = 'https://localhost:7084';

const newDeck = ref<DeckCreateDto>({
  title: '',
  description: '',
  isPublic: false,
  cards: []
});
const newCardInput = ref<CardCreateDto>({ frontText: '', backText: '', tags: '' });
const isSaving = ref(false);

// State cho Modal Import
const showImportModal = ref(false);
const importText = ref('');
const importDelimiter = ref<'tab' | 'comma'>('comma');

// Logic parse và preview
const parsedCards = computed(() => {
  const lines = importText.value.split('\n').filter(line => line.trim() !== '');
  const delimiter = importDelimiter.value === 'tab' ? '\t' : ',';

  return lines.map(line => {
    const parts = line.split(delimiter);
    const front = parts[0]?.trim() ?? '';
    const back = parts.slice(1).join(delimiter).trim() ?? '';
    if (front && back) {
      return { frontText: front, backText: back, tags: null } as CardCreateDto;
    }
    return null;
  }).filter((card): card is CardCreateDto => card !== null);
});

// handleResponse
async function handleResponse<T>(response: Response): Promise<T> {
  // ... (implementation remains the same) ...
   if (!response.ok) {
    const errorText = await response.text();
    let errorMessage = errorText;
    try {
        const errorJson = JSON.parse(errorText);
        if (errorJson && errorJson.message) errorMessage = errorJson.message;
        else if (errorJson && errorJson.title) errorMessage = errorJson.title;
    } catch (e) {}
    throw new Error(errorMessage || Yêu cầu thất bại: ${response.status});
  }
  if (response.status === 204) return {} as T; // Handle No Content
  const data = await response.json();
  if (data.isSuccess === false) { // Assuming backend uses isSuccess flag
    throw new Error(data.message || 'Lỗi từ API');
  }
  return (data.result === undefined ? data : data.result) as T; // Return result if exists, else the whole data
}

function addCardToList() {
  // ... (implementation remains the same) ...
  if (!newCardInput.value.frontText || !newCardInput.value.backText) {
    alert("Ký tự và Nghĩa là bắt buộc.");
    return;
  }
  newDeck.value.cards.push({
      frontText: newCardInput.value.frontText,
      backText: newCardInput.value.backText,
      tags: newCardInput.value.tags || null // Ensure tags is null if empty
  });
  newCardInput.value = { frontText: '', backText: '', tags: '' };
}

function removeCardFromList(index: number) {
  // ... (implementation remains the same) ...
   newDeck.value.cards.splice(index, 1);
}

async function createDeck() {
  // ... (implementation remains the same) ...
  if (!newDeck.value.title.trim()) {
    alert("Tên bộ thẻ là bắt buộc.");
    return;
  }
  if (newDeck.value.cards.length === 0 && !confirm("Bạn chưa thêm thẻ nào. Vẫn muốn tạo bộ thẻ trống?")) {
      return;
  }

  isSaving.value = true;
  try {
    const response = await fetch(${BASE_URL}/api/decks, { // Added /api prefix
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            // 'Authorization': Bearer ${YOUR_AUTH_TOKEN} // Add auth if needed
        },
        body: JSON.stringify(newDeck.value)
    });
    const createdDeckData = await handleResponse<{ id: number }>(response);
    alert('Đã tạo bộ thẻ thành công!');
    if (createdDeckData?.id) {
        emit('deck-created', createdDeckData.id);
    } else {
        console.warn("API did not return the new deck ID.");
        emit('go-back');
    }
  } catch (err: any) {
    alert(Lỗi tạo bộ thẻ: ${err.message});
    console.error("createDeck Error:", err);
  } finally {
    isSaving.value = false;
  }
}

// Các hàm xử lý Modal Import
function openImportModal() {
  importText.value = '';
  showImportModal.value = true;
}

function closeImportModal() {
  showImportModal.value = false;
}

function importCards() {
  newDeck.value.cards.push(...parsedCards.value);
  closeImportModal();
}

// ✅ THÊM: Hàm xử lý sự kiện nhấn phím Tab
function handleTab(event: KeyboardEvent) {
  const target = event.target as HTMLTextAreaElement;
  const start = target.selectionStart;
  const end = target.selectionEnd;

  // Chèn ký tự Tab vào vị trí con trỏ
  target.value = target.value.substring(0, start) + "\t" + target.value.substring(end);

  // Di chuyển con trỏ đến sau ký tự Tab vừa chèn
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