<template>
  <div class="min-h-screen text-white p-4 sm:p-8">
    <div class="max-w-4xl mx-auto">
      
      <!-- Header -->
      <div class="mb-6">
        <button @click="emit('go-to-list')" class="flex items-center text-sm text-sky-400 hover:text-sky-300 transition-colors mb-2">
          &larr; Quay lại danh sách
        </button>
        <h1 class="text-3xl font-bold text-sky-400">Chỉnh sửa bộ thẻ</h1>
      </div>

      <!-- Vùng nguy hiểm: Xóa Deck -->
      <div class="bg-gray-800 rounded-lg p-5 mb-6">
        <h2 class="text-xl font-bold text-red-400 mb-3">Vùng nguy hiểm</h2>
        <p class="text-gray-400 text-sm mb-4">
          Hành động này không thể hoàn tác. Toàn bộ thẻ và tiến độ học sẽ bị xóa vĩnh viễn.
        </p>
        <button @click="promptDeleteDeck" class="bg-red-600 hover:bg-red-700 text-white font-bold py-2 px-4 rounded-lg">
          Xóa vĩnh viễn bộ thẻ này
        </button>
      </div>

      <!-- Chỉnh sửa thông tin Deck -->
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
            <button @click="saveDeckInfo" class="bg-sky-500 hover:bg-sky-600 text-white font-bold py-2 px-5 rounded-lg">
              Lưu thông tin
            </button>
            <div class="flex items-center">
              <input id="isPublic" type="checkbox" v-model="editableSet.isPublic" class="h-4 w-4 rounded bg-gray-700 border-gray-600 text-sky-500 focus:ring-sky-500">
              <label for="isPublic" class="ml-2 block text-sm text-gray-300">Công khai (cho phép người khác xem)</label>
            </div>
          </div>
        </div>
      </div>

      <!-- Quản lý Cards -->
      <div class="bg-gray-800 rounded-lg p-5">
        <h2 class="text-xl font-bold text-gray-100 mb-4">Quản lý thẻ ({{ editableSet.cards.length }})</h2>
        
        <!-- Thêm thẻ mới -->
        <div class="flex gap-4 mb-4 pb-4 border-b border-gray-700">
          <input type="text" v-model="newCard.charBig" placeholder="Ký tự (e.g., 黄)" class="form-input flex-1">
          <input type="text" v-model="newCard.meaning" placeholder="Nghĩa (e.g., Màu vàng)" class="form-input flex-1">
          <!-- <input type="text" v-model="newCard.pinyin" placeholder="Pinyin (e.g., huáng)" class="form-input flex-1"> -->
          <button @click="addCard" class="bg-green-500 hover:bg-green-600 text-white font-bold py-2 px-4 rounded-lg">Thêm</button>
        </div>

        <!-- Danh sách thẻ hiện tại -->
        <div class="space-y-2 max-h-96 overflow-y-auto">
          <div v-for="card in editableSet.cards" :key="card.id" class="flex items-center gap-4 p-2 bg-gray-700 rounded-lg">
            <input type="text" v-model="card.charBig" class="form-input-sm w-3/4">
            <input type="text" v-model="card.meaning" class="form-input-sm w-2/3 ml-10">
            <button @click="updateCard(card)" class="p-2 text-sky-400 hover:text-sky-300" title="Lưu thẻ">
              <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5 " viewBox="0 0 20 20" fill="currentColor"><path d="M7.707 10.293a1 1 0 10-1.414 1.414l3 3a1 1 0 001.414 0l3-3a1 1 0 00-1.414-1.414L11 11.586V6a1 1 0 10-2 0v5.586L7.707 10.293zM3 17a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1z" /></svg>
            </button>
            <button @click="deleteCard(card.id)" class="p-2 text-red-400 hover:text-red-300" title="Xóa thẻ">
              <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" /></svg>
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Modal xác nhận xóa -->
    <ConfirmationModal
:is-open="isModalOpen"
 title="Xác nhận XÓA"
 :message="modalMessage"
 confirmation-text="delete"
 @confirm="handleConfirmDeleteDeck"
 @cancel="closeModal"
 />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue';
import type { DeckDetailDto, CardDto } from '~/types';
import ConfirmationModal from './ConfirmationModal.vue';

const props = defineProps<{ initialSet: DeckDetailDto }>();
const emit = defineEmits(['go-to-list', 'deck-updated']);

const BASE_URL = 'https://localhost:7084/api';

const editableSet = ref<DeckDetailDto>(JSON.parse(JSON.stringify(props.initialSet)));
const newCard = ref({ charBig: '', meaning: '', pinyin: '' });
const isModalOpen = ref(false);
const modalMessage = ref('');

// Giả lập hàm handleResponse, bạn cần thay thế bằng logic thực
// Hàm handleResponse đã sửa lỗi "body stream already read"
async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    // 1. Đọc body lỗi MỘT LẦN DUY NHẤT dưới dạng text.
    //    Đọc dưới dạng text là an toàn nhất vì lỗi có thể không phải là JSON.
    const errorText = await response.text(); 

    // 2. Ném lỗi với nội dung đã đọc được.
    //    Bạn có thể thử parse text thành JSON ở đây nếu muốn, 
    //    nhưng không gọi lại response.json().
    let errorMessage = errorText;
    try {
        // Thử xem nội dung lỗi có phải là JSON chuẩn của bạn không
        const errorJson = JSON.parse(errorText);
        if (errorJson && errorJson.message) {
            errorMessage = errorJson.message;
        }
    } catch (e) {
        // Không phải JSON, giữ nguyên errorText
    }
    
    throw new Error(errorMessage || `Yêu cầu thất bại: ${response.status}`); 
  }
  
  // Nếu không lỗi (response.ok), đọc body MỘT LẦN DUY NHẤT dưới dạng JSON
  const data = await response.json(); 
  
  // Kiểm tra cờ thành công của backend (nếu có)
  // Lưu ý: Backend của bạn trả về { isSuccess: true, result: ... }
  if (data.isSuccess === false) { 
     // Nếu backend báo lỗi dù status là 2xx
     throw new Error(data.message || 'Lỗi logic từ API'); 
  }

  // Giả sử dữ liệu thành công nằm trong 'result' hoặc là chính data
  return (data.result || data) as T; 
}

async function saveDeckInfo() {
  const deckId = editableSet.value.id;
  const deckDto = {
    title: editableSet.value.title,
    description: editableSet.value.description,
    isPublic: editableSet.value.isPublic
  };
  
  try {
    const response = await fetch(`${BASE_URL}/decks/${deckId}`, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' /* ...Auth header... */ },
      body: JSON.stringify(deckDto)
    });
    await handleResponse(response);
    alert('Đã cập nhật thông tin Deck!');
    emit('deck-updated');
  } catch (err: any) { 
    console.error("Lỗi cập nhật Deck:", err);
    alert(`Lỗi: ${err.message}`); 
  }
}

async function addCard() {
  const deckId = editableSet.value.id;
  const cardDto = [{
    charBig: newCard.value.charBig,
    meaning: newCard.value.meaning,
    pinyin: newCard.value.pinyin
  }];

  if (!cardDto[0]?.charBig || !cardDto[0].meaning) {
    alert("Ký tự và Nghĩa là bắt buộc.");
    return;
  }

  try {
    const response = await fetch(`${BASE_URL}/decks/${deckId}/cards`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' /* ...Auth header... */ },
      body: JSON.stringify(cardDto)
    });
    const newCardsData = await handleResponse(response) as { result: CardDto[] }; 
    
    if (newCardsData.result && newCardsData.result[0]) {
      editableSet.value.cards.push(newCardsData.result[0]);
    }

    newCard.value = { charBig: '', meaning: '', pinyin: '' };
  } catch (err: any) { 
    console.error("Lỗi thêm Card:", err);
    alert(`Lỗi: ${err.message}`); 
  }
}

async function updateCard(card: CardDto) {
  const cardId = card.id;
  
  // ✅ SỬA Ở ĐÂY: Tạo DTO đúng với yêu cầu của backend
  const cardUpdateDto = {
    FrontText: card.charBig,  // Map charBig -> FrontText
    BackText: card.meaning,   // Map meaning -> BackText
    tags: null       // Giả sử backend cũng cần Pinyin (kiểm tra lại DTO C# nếu cần)
  };

  try {
    const response = await fetch(`${BASE_URL}/cards/${cardId}`, { 
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' /* ...Auth header... */ },
      // Gửi đi DTO đã được map đúng tên trường
      body: JSON.stringify(cardUpdateDto) 
    });
    await handleResponse(response);
    alert('Đã cập nhật thẻ!');
  } catch (err: any) { 
    console.error("Lỗi cập nhật Card:", err);
    // ✅ SỬA: Hiển thị lỗi rõ ràng hơn
    alert(`Lỗi cập nhật thẻ: ${err.message}`); 
  }
}
async function deleteCard(cardId: number) {
  if (!confirm(`Bạn có chắc muốn xóa thẻ "${editableSet.value.cards.find(c => c.id === cardId)?.meaning}"?`)) return;
  
  try {
    const response = await fetch(`${BASE_URL}/cards/${cardId}`, {
      method: 'DELETE',
      headers: { /* ...Auth header... */ }
    });
    await handleResponse(response);
    editableSet.value.cards = editableSet.value.cards.filter(c => c.id !== cardId);
    alert('Đã xóa thẻ.');
  } catch (err: any) { 
    console.error("Lỗi xóa Card:", err);
    alert(`Lỗi: ${err.message}`); 
  }
}

// --- Logic Xóa Deck ---
function promptDeleteDeck() {
  modalMessage.value = `Hành động này sẽ xóa vĩnh viễn bộ thẻ "${editableSet.value.title}". Toàn bộ tiến độ học sẽ bị mất.`;
  isModalOpen.value = true;
}

function closeModal() {
  isModalOpen.value = false;
}

async function handleConfirmDeleteDeck() {
  closeModal();
  const deckId = editableSet.value.id;
  
  try {
    const response = await fetch(`${BASE_URL}/decks/${deckId}`, {
      method: 'DELETE',
      headers: { /* ...Auth header... */ }
    });
    await handleResponse(response);
    alert('Đã xóa Deck thành công.');
    emit('go-to-list'); 
  } catch (err: any) { 
    console.error("Lỗi xóa Deck:", err);
    alert(`Lỗi: ${err.message}`); 
  }
}

</script>

<!-- ✅ SỬA: Chuyển sang CSS thuần túy -->
<style scoped>
.form-input {
  width: 100%;
  background-color: #1f2937; /* bg-gray-900? Maybe bg-gray-800 is #1f2937 */
  border: 1px solid #4b5563; /* border-gray-600 */
  border-radius: 0.5rem; /* rounded-lg */
  padding-left: 0.75rem; /* px-3 */
  padding-right: 0.75rem; /* px-3 */
  padding-top: 0.5rem; /* py-2 */
  padding-bottom: 0.5rem; /* py-2 */
  color: white;
  outline: none; /* focus:outline-none */
  --tw-ring-offset-shadow: var(--tw-ring-inset) 0 0 0 var(--tw-ring-offset-width) var(--tw-ring-offset-color);
  --tw-ring-shadow: var(--tw-ring-inset) 0 0 0 calc(2px + var(--tw-ring-offset-width)) var(--tw-ring-color);
  box-shadow: var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow);
}
.form-input:focus {
  --tw-ring-color: #0ea5e9; /* focus:ring-sky-500 */
   box-shadow: var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow), var(--tw-shadow, 0 0 #0000);
   border-color: #0ea5e9; /* Implicit focus border color change */
}

.form-input-sm {
  background-color: #1f2937; /* bg-gray-900? Maybe bg-gray-800 is #1f2937 */
  border: 1px solid #4b5563; /* border-gray-600 */
  border-radius: 0.25rem; /* rounded */
  padding-left: 0.5rem; /* px-2 */
  padding-right: 0.5rem; /* px-2 */
  padding-top: 0.25rem; /* py-1 */
  padding-bottom: 0.25rem; /* py-1 */
  color: white;
  outline: none; /* focus:outline-none */
  --tw-ring-offset-shadow: var(--tw-ring-inset) 0 0 0 var(--tw-ring-offset-width) var(--tw-ring-offset-color);
  --tw-ring-shadow: var(--tw-ring-inset) 0 0 0 calc(1px + var(--tw-ring-offset-width)) var(--tw-ring-color); /* focus:ring-1 */
  box-shadow: var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow, 0 0 #0000), var(--tw-shadow);
}
.form-input-sm:focus {
  --tw-ring-color: #0ea5e9; /* focus:ring-sky-500 */
  box-shadow: var(--tw-ring-offset-shadow, 0 0 #0000), var(--tw-ring-shadow), var(--tw-shadow, 0 0 #0000);
  border-color: #0ea5e9; /* Implicit focus border color change */
}
</style>

