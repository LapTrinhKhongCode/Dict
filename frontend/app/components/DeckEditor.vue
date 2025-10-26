<template>
  <div class="min-h-screen text-white p-4 sm:p-8">
    <div class="max-w-3xl mx-auto">

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

      <!-- Quản lý Cards -->
      <div class="bg-gray-800 rounded-lg p-5">
        <h2 class="text-xl font-bold text-gray-100 mb-4">Quản lý thẻ ({{ editableSet.cards.length }})</h2>

        <!-- ✅ SỬA: Form thêm thẻ mới (dùng frontText, backText) -->
        <div class="flex flex-col sm:flex-row gap-4 mb-4 pb-4 border-b border-gray-700">
          <input type="text" v-model="newCardInput.frontText" placeholder="Mặt trước (Ký tự *)" class="form-input flex-1">
          <input type="text" v-model="newCardInput.backText" placeholder="Mặt sau (Nghĩa *)" class="form-input flex-1">
          <!-- <input type="text" v-model="newCardInput.tags" placeholder="Tags (Pinyin, optional)" class="form-input flex-1"> -->
          <button @click="addCard" :disabled="isAddingCard" class="bg-green-500 hover:bg-green-600 text-white font-bold py-2 px-4 rounded-lg mt-2 sm:mt-0 sm:ml-auto disabled:bg-gray-500 disabled:cursor-not-allowed">
             <span v-if="isAddingCard">Đang thêm...</span>
             <span v-else>Thêm</span>
          </button>
        </div>

        <!-- Danh sách thẻ hiện tại -->
        <div class="space-y-2 max-h-96 overflow-y-auto">
          <div v-if="editableSet.cards.length === 0" class="text-center text-gray-500 text-sm py-4">Chưa có thẻ nào trong bộ này.</div>
          <!-- ✅ SỬA: v-model vẫn dùng charBig, meaning, pinyin (từ CardDto) -->
          <div v-for="card in editableSet.cards" :key="card.id" class="flex items-center gap-4 p-2 bg-gray-700 rounded-lg">
            <input type="text" v-model="card.charBig" placeholder="Ký tự" class="form-input-sm w-1/4">
            <input type="text" v-model="card.meaning" placeholder="Nghĩa" class="form-input-sm w-1/3">
            <input type="text" v-model="card.pinyin" placeholder="Pinyin" class="form-input-sm w-1/3">
            <!-- Nút Lưu thẻ (update) -->
            <button @click="updateCard(card)" :disabled="card.isSaving" class="p-2 text-sky-400 hover:text-sky-300 disabled:text-gray-500 disabled:cursor-not-allowed" title="Lưu thẻ">
                <svg v-if="card.isSaving" class="animate-spin h-5 w-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>
                <svg v-else xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor"><path d="M7.707 10.293a1 1 0 10-1.414 1.414l3 3a1 1 0 001.414 0l3-3a1 1 0 00-1.414-1.414L11 11.586V6a1 1 0 10-2 0v5.586L7.707 10.293zM3 17a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1z" /></svg>
            </button>
            <!-- Nút Xóa thẻ -->
            <button @click="deleteCard(card)" :disabled="card.isDeleting" class="p-2 text-red-400 hover:text-red-300 disabled:text-gray-500 disabled:cursor-not-allowed" title="Xóa thẻ">
               <svg v-if="card.isDeleting" class="animate-spin h-5 w-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>
               <svg v-else xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor"><path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" /></svg>
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Modal xác nhận xóa Deck -->
    <ConfirmationModal
      :is-open="isDeleteDeckModalOpen"
      title="Xác nhận XÓA Bộ Thẻ"
      :message="deleteDeckModalMessage"
      confirmation-text="delete"
      @confirm="handleConfirmDeleteDeck"
      @cancel="closeDeleteDeckModal"
    />
     <!-- Modal xác nhận xóa Card -->
    <ConfirmationModal
      :is-open="isDeleteCardModalOpen"
      title="Xác nhận XÓA Thẻ"
      :message="deleteCardModalMessage"
      confirmation-text="delete"
      @confirm="handleConfirmDeleteCard"
      @cancel="closeDeleteCardModal"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue'; // Import reactive
import type { DeckDetailDto, CardDto, DeckUpdateDto, CardCreateDto, CardUpdateDto } from '~/types'; // Import necessary types
import ConfirmationModal from './ConfirmationModal.vue';

// Define CardDto with optional saving/deleting state for UI feedback
interface EditableCardDto extends CardDto {
  isSaving?: boolean;
  isDeleting?: boolean;
}

const props = defineProps<{ initialSet: DeckDetailDto }>();
const emit = defineEmits(['go-to-list', 'deck-updated']);

const config = useRuntimeConfig()

const BASE_URL = config.public.apiBaseUrl



// Use reactive for easier nested updates
const editableSet = reactive<DeckDetailDto>(JSON.parse(JSON.stringify(props.initialSet)));
// Initialize cards with saving/deleting state
editableSet.cards = editableSet.cards.map(card => ({ ...card, isSaving: false, isDeleting: false }));


// ✅ SỬA: newCardInput dùng frontText, backText, tags
const newCardInput = ref<CardCreateDto>({ frontText: '', backText: '', tags: '' });

// State flags for loading indicators
const isSavingDeck = ref(false);
const isAddingCard = ref(false);

// State for Delete Deck Modal
const isDeleteDeckModalOpen = ref(false);
const deleteDeckModalMessage = ref('');

// State for Delete Card Modal
const isDeleteCardModalOpen = ref(false);
const deleteCardModalMessage = ref('');
const cardToDelete = ref<EditableCardDto | null>(null);


// handleResponse (centralized error handling)
async function handleResponse<T>(response: Response): Promise<T> {
   if (!response.ok) {
    const errorText = await response.text();
    let errorMessage = errorText;
    try {
        const errorJson = JSON.parse(errorText);
        // Prioritize specific backend messages if available
        if (errorJson?.errors) { // Handle ASP.NET Core validation errors
            errorMessage = Object.values(errorJson.errors).flat().join(' ');
        } else if (errorJson?.message) {
            errorMessage = errorJson.message;
        } else if (errorJson?.title) { // Handle problem details title
            errorMessage = errorJson.title;
        }
    } catch (e) { /* Ignore parsing errors, keep original text */ }
    throw new Error(errorMessage || `Yêu cầu thất bại: ${response.status}`);
  }
  if (response.status === 204) return {} as T; // Handle No Content
  const data = await response.json();
  // Assuming backend wrapper { isSuccess: bool, message: string, result: T }
  if (data.isSuccess === false) {
    throw new Error(data.message || 'Lỗi không xác định từ API');
  }
  // Return the actual result if nested, otherwise the whole data object
  return (data.result === undefined ? data : data.result) as T;
}


// --- Deck Operations ---
async function saveDeckInfo() {
  isSavingDeck.value = true;
  const deckId = editableSet.id;
  // Use DeckUpdateDto type
  const deckDto: DeckUpdateDto = {
    title: editableSet.title,
    description: editableSet.description,
    isPublic: editableSet.isPublic ?? false // Handle null
  };

  try {
    const response = await fetch(`${BASE_URL}/api/decks/${deckId}`, { // Added /api
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', /* 'Authorization': `Bearer ${TOKEN}` */ },
      body: JSON.stringify(deckDto)
    });
    await handleResponse(response);
    alert('Đã cập nhật thông tin Deck!');
    // No need to emit deck-updated if only toggling public? Depends on desired UX.
    // If title/desc change, emitting is good.
    // emit('deck-updated');
  } catch (err: any) {
    alert(`Lỗi cập nhật Deck: ${err.message}`);
    console.error("saveDeckInfo Error:", err);
    // Revert checkbox on error? Optional.
    // editableSet.isPublic = !editableSet.isPublic;
  } finally {
      isSavingDeck.value = false;
  }
}

function promptDeleteDeck() {
  deleteDeckModalMessage.value = `Hành động này sẽ xóa vĩnh viễn bộ thẻ "${editableSet.title}". Toàn bộ tiến độ học sẽ bị mất.`;
  isDeleteDeckModalOpen.value = true;
}

function closeDeleteDeckModal() {
  isDeleteDeckModalOpen.value = false;
}

async function handleConfirmDeleteDeck() {
  closeDeleteDeckModal();
  isSavingDeck.value = true; // Use general saving flag or a specific deleting flag
  const deckId = editableSet.id;
  try {
    const response = await fetch(`${BASE_URL}/api/decks/${deckId}`, { // Added /api
      method: 'DELETE',
      headers: { /* 'Authorization': `Bearer ${TOKEN}` */ }
    });
    await handleResponse(response);
    alert('Đã xóa Deck thành công.');
    emit('go-to-list'); // Go back, App.vue will handle refresh implicitly
  } catch (err: any) {
    alert(`Lỗi xóa Deck: ${err.message}`);
    console.error("handleConfirmDeleteDeck Error:", err);
  } finally {
      isSavingDeck.value = false;
  }
}

// --- Card Operations ---
async function addCard() {
  const deckId = editableSet.id;
  // Use CardCreateDto type for input
  const cardDto: CardCreateDto = {
    frontText: newCardInput.value.frontText,
    backText: newCardInput.value.backText,
    // tags: newCardInput.value.tags || null // Ensure null if empty
  };

  if (!cardDto.frontText?.trim() || !cardDto.backText?.trim()) {
    alert("Mặt trước và Mặt sau là bắt buộc.");
    return;
  }
  isAddingCard.value = true;
  try {
    const response = await fetch(`${BASE_URL}/api/decks/${deckId}/cards`, { // Added /api
      method: 'POST',
      headers: { 'Content-Type': 'application/json', /* 'Authorization': `Bearer ${TOKEN}` */ },
      body: JSON.stringify([cardDto]) // Send as an array as per backend? Check API. If single: JSON.stringify(cardDto)
    });
    // Assuming API returns the created CardDto(s) in the result field
    const newCardsData = await handleResponse<{ result?: CardDto[] }>(response); // Adjust expected type

    if (newCardsData.result && newCardsData.result.length > 0) {
        // Add the new card(s) returned by API (with correct ID and structure)
        newCardsData.result.forEach(newApiCard => {
            editableSet.cards.push({ ...newApiCard, isSaving: false, isDeleting: false });
        });
    } else {
        // Fallback if API doesn't return created card - less ideal
        // editableSet.cards.push({ id: Date.now(), charBig: cardDto.frontText, meaning: cardDto.backText, pinyin: cardDto.tags, nextReviewAt: '', isSaving: false, isDeleting: false });
        console.warn("API did not return created card data. Added optimistically.");
         // Re-fetch the whole deck for consistency if needed: emit('deck-updated');
    }

    newCardInput.value = { frontText: '', backText: '', tags: '' }; // Reset form
  } catch (err: any) {
    alert(`Lỗi thêm thẻ: ${err.message}`);
    console.error("addCard Error:", err);
  } finally {
      isAddingCard.value = false;
  }
}

async function updateCard(card: EditableCardDto) {
  if (card.isSaving) return;
  card.isSaving = true; // Set loading state for this specific card
  const cardId = card.id;
  // Use CardUpdateDto type, map fields
  const cardDto: CardUpdateDto = {
    FrontText: card.charBig, // Map from CardDto field used in v-model
    BackText: card.meaning,  // Map from CardDto field used in v-model
    Tags: card.pinyin       // Map from CardDto field used in v-model
  };

  try {
    const response = await fetch(`${BASE_URL}/api/cards/${cardId}`, { // Correct endpoint
      method: 'PUT',
      headers: { 'Content-Type': 'application/json', /* 'Authorization': `Bearer ${TOKEN}` */ },
      body: JSON.stringify(cardDto)
    });
    await handleResponse(response);
    // Optionally show a temporary success indicator on the card itself
    // alert('Đã cập nhật thẻ!'); // Avoid alerts for each card save
  } catch (err: any) {
    alert(`Lỗi cập nhật thẻ ${cardId}: ${err.message}`);
    console.error("updateCard Error:", err);
    // Optionally revert changes in UI if save fails? More complex.
  } finally {
      card.isSaving = false; // Reset loading state for this card
  }
}

function promptDeleteCard(card: EditableCardDto) {
  deleteCardModalMessage.value = `Bạn có chắc muốn xóa thẻ "${card.meaning || card.charBig}" không?`;
  cardToDelete.value = card;
  isDeleteCardModalOpen.value = true;
}

function closeDeleteCardModal() {
    isDeleteCardModalOpen.value = false;
    cardToDelete.value = null;
}

async function handleConfirmDeleteCard() {
    if (!cardToDelete.value || cardToDelete.value.isDeleting) return;
    const card = cardToDelete.value;
    const cardId = card.id;
    
    closeDeleteCardModal();
    card.isDeleting = true; // Set loading state

    try {
        const response = await fetch(`${BASE_URL}/api/cards/${cardId}`, { // Correct endpoint
            method: 'DELETE',
            headers: { /* 'Authorization': `Bearer ${TOKEN}` */ }
        });
        await handleResponse(response);
        // Remove from UI list
        editableSet.cards = editableSet.cards.filter(c => c.id !== cardId);
        // alert('Đã xóa thẻ.'); // Avoid alert
    } catch (err: any) {
        alert(`Lỗi xóa thẻ ${cardId}: ${err.message}`);
        console.error("deleteCard Error:", err);
        card.isDeleting = false; // Reset loading state only on error
    }
    // No finally block needed here, card is removed on success
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
  background-color: #1f2937; /* Adjusted to match form-input */
  border: 1px solid #4b5563;
  border-radius: 0.375rem; /* rounded-md often looks better than rounded */
  padding: 0.25rem 0.5rem; /* py-1 px-2 */
  color: white;
  outline: none;
  transition: border-color 0.2s, box-shadow 0.2s;
}
.form-input-sm:focus {
  border-color: #0ea5e9;
  box-shadow: 0 0 0 1px #0ea5e9; /* focus:ring-1 */
}
</style>

