<!-- components/DeckCreator.vue -->
<template>
  <div class="min-h-screen text-white p-4 sm:p-8">
    <div class="max-w-3xl mx-auto">
      
      <div class="mb-6">
        <button @click="emit('go-back')" class="flex items-center text-sm text-sky-400 hover:text-sky-300 transition-colors mb-2">
          &larr; Quay lại
        </button>
        <h1 class="text-3xl font-bold text-sky-400">Tạo sổ tay mới</h1>
      </div>

      <!-- Form create Deck (Uses DeckCreateDto -> Title) -->
      <div class="bg-gray-800 rounded-lg p-5 mb-6 space-y-4">
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
          <label for="isPublic" class="ml-2 block text-sm text-gray-300">Công khai (cho phép người khác xem và lưu)</label>
        </div>
      </div>
      
      <!-- Add initial Cards (Uses CardCreateDto -> charBig, meaning) -->
      <div class="bg-gray-800 rounded-lg p-5 mb-6">
        <h2 class="text-xl font-bold text-gray-100 mb-4">Thêm thẻ ban đầu ({{ newDeck.cards.length }})</h2>
        
        <div class="flex flex-col sm:flex-row gap-4 mb-4 pb-4 border-b border-gray-700">
          <input type="text" v-model="newCardInput.frontText" placeholder="Ký tự *" class="form-input flex-1">
          <input type="text" v-model="newCardInput.backText" placeholder="Nghĩa *" class="form-input flex-1">
          <!-- <input type="text" v-model="newCardInput.tags" placeholder="Pinyin" class="form-input flex-1"> -->
          <button @click="addCardToList" class="bg-blue-500 hover:bg-blue-600 text-white font-bold py-2 px-4 rounded-lg mt-2 sm:mt-0 sm:ml-auto">Thêm vào danh sách</button>
        </div>

        <div class="space-y-2 max-h-60 overflow-y-auto">
          <div v-for="(card, index) in newDeck.cards" :key="index" class="flex items-center gap-4 p-2 bg-gray-700 rounded-lg text-sm">
            <div class="w-2/4 font-semibold">{{ card.frontText }}</div>
            <div class="w-2/3 ">{{ card.backText }}</div>
            <button @click="removeCardFromList(index)" class="p-1 text-red-400 hover:text-red-300 ml-auto" title="Xóa khỏi danh sách">
               <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" /></svg>
            </button>
          </div>
           <p v-if="newDeck.cards.length === 0" class="text-center text-gray-500 text-sm py-4">Chưa có thẻ nào được thêm.</p>
        </div>
      </div>

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
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import type { DeckCreateDto, CardCreateDto } from '~/types';

const emit = defineEmits(['go-back', 'deck-created']);

const BASE_URL = 'https://localhost:7084'; // Base URL only

// Matches DeckCreateDto
const newDeck = ref<DeckCreateDto>({
  title: '',
  description: '',
  isPublic: false,
  cards: [] 
});
// Matches CardCreateDto
const newCardInput = ref<CardCreateDto>({ 
  frontText: '', 
  backText: '', 
  tags: '' 
});
const isSaving = ref(false);

// --- API Handling (Should be centralized) ---
async function handleResponse<T>(response: Response): Promise<T> {
  // ... (Same implementation as in App.vue) ...
    if (!response.ok) {
        const errorText = await response.text();
        let errorMessage = errorText;
        try {
            const errorJson = JSON.parse(errorText);
            if (errorJson && errorJson.message) errorMessage = errorJson.message;
             else if (errorJson && errorJson.title) errorMessage = errorJson.title;
        } catch (e) {}
        throw new Error(errorMessage || `Yêu cầu thất bại: ${response.status}`);
    }
    if (response.status === 204) return {} as T;
    const data = await response.json();
    if (data.isSuccess === false) {
        throw new Error(data.message || 'Lỗi từ API');
    }
    return (data.result === undefined ? data : data.result) as T;
}
// ---------------------------------------------

function addCardToList() {
  if (!newCardInput.value.frontText || !newCardInput.value.backText) {
    alert("Ký tự và Nghĩa là bắt buộc để thêm thẻ.");
    return;
  }
  // Add a copy to the list
  newDeck.value.cards.push({ 
      frontText: newCardInput.value.frontText, 
      backText: newCardInput.value.backText, 
      tags: newCardInput.value.tags || null // Ensure pinyin is null if empty
  });
  // Reset input fields
  newCardInput.value = { frontText: '', backText: '', tags: '' }; 
}

function removeCardFromList(index: number) {
  newDeck.value.cards.splice(index, 1);
}

async function createDeck() {
  if (!newDeck.value.title.trim()) {
    alert("Tên bộ thẻ là bắt buộc.");
    return;
  }
  isSaving.value = true;
  try {
    const response = await fetch(`${BASE_URL}/api/decks`, {
      method: 'POST',
      headers: { 
        'Content-Type': 'application/json',
        // 'Authorization': `Bearer ${YOUR_AUTH_TOKEN}` 
      },
      // Send the whole newDeck object which matches DeckCreateDto
      body: JSON.stringify(newDeck.value) 
    });
    // Assuming API returns ResponseDTO<{ id: number }> or similar
    const createdDeckData = await handleResponse<{ id: number }>(response); 
    
    alert('Đã tạo bộ thẻ thành công!');
    if (createdDeckData?.id) {
        emit('deck-created', createdDeckData.id); // Send new ID back to App.vue
    } else {
        console.warn("API did not return the new deck ID.");
        emit('go-back'); // Fallback: just go back
    }

  } catch (err: any) {
    alert(`Lỗi tạo bộ thẻ: ${err.message}`);
    console.error("createDeck Error:", err);
  } finally {
    isSaving.value = false;
  }
}
</script>

<style scoped>
.form-input {
  width: 100%;
  background-color: #1f2937; /* bg-gray-800 */
  border: 1px solid #4b5563; /* border-gray-600 */
  border-radius: 0.5rem; /* rounded-lg */
  padding: 0.5rem 0.75rem; /* py-2 px-3 */
  color: white;
  outline: none;
  transition: border-color 0.2s, box-shadow 0.2s;
}
.form-input:focus {
  border-color: #0ea5e9; /* focus:ring-sky-500 */
  box-shadow: 0 0 0 2px rgba(14, 165, 233, 0.4); /* focus:ring-2 focus:ring-opacity-40 */
}
</style>
