<template>
  <div class="vocab-container">
    <div class="vocab-header">
      <span class="vocab-header-title">Sổ tay từ vựng</span>
      
      <!-- Hiện các hành động khi có từ được tick chọn -->
      <div v-if="selectedVocabs.length > 0" class="header-actions">
        <button class="btn-create-deck" @click="showDeckModal = true">
          Tạo Flashcard ({{ selectedVocabs.length }})
        </button>
        <button class="btn-delete-bulk" @click="confirmBulkDelete">
          Xóa ({{ selectedVocabs.length }})
        </button>
      </div>
    </div>
    
    <!-- Dòng chọn tất cả -->
    <div v-if="vocabList.length > 0" class="vocab-list-controls">
      <label class="select-all-label">
        <input 
          type="checkbox" 
          :checked="selectedVocabs.length === vocabList.length" 
          @change="toggleSelectAllBulk"
        /> 
        Chọn tất cả
      </label>
    </div>

    <div class="vocab-list">
      <div v-if="vocabList.length === 0" class="vocab-empty">
        Bôi đen từ trong tài liệu để tra cứu và lưu từ vựng.
      </div>
      <div v-for="(v, i) in vocabList" :key="v.id || i" class="vocab-item">
        <!-- Checkbox chọn -->
        <input type="checkbox" :value="v" v-model="selectedVocabs" class="vocab-checkbox" />
        
        <!-- Khu vực hiển thị chữ, click vào để sửa -->
        <div class="vocab-content" @click="openEditModal(v, i)" title="Click để sửa nghĩa">
          <div class="vocab-item-word">{{ v.wordText }}</div>
          <div class="vocab-item-meaning">{{ v.contextMeaning }}</div>
        </div>

        <!-- Nút Xóa 1 từ -->
        <button class="vocab-item-del" @click.stop="confirmSingleDelete(i, v)" title="Xóa từ này">✕</button>
      </div>
    </div>

    <!-- MODAL SỬA NGHĨA TỪ -->
    <Transition name="modal">
      <div v-if="showEditModal" class="modal-overlay" @click.self="showEditModal = false">
        <div class="deck-modal">
          <h2 class="deck-modal-title">
            Sửa từ: <span style="color: #f0c040;">{{ editingVocab?.wordText }}</span>
          </h2>
          <div class="deck-modal-section">
            <label class="deck-label">Nghĩa của từ</label>
            <textarea 
              v-model="editingVocabMeaning" 
              class="deck-input" 
              rows="4" 
              placeholder="Nhập nghĩa mới..."
            ></textarea>
          </div>
          <div class="deck-modal-actions">
            <button class="btn-cancel" @click="showEditModal = false">Hủy</button>
            <button class="btn-create" :disabled="savingEdit" @click="saveEditVocab">
              {{ savingEdit ? 'Đang lưu...' : 'Lưu thay đổi' }}
            </button>
          </div>
        </div>
      </div>
    </Transition>

    <!-- MODAL TẠO FLASHCARD -->
    <Transition name="modal">
      <div v-if="showDeckModal" class="modal-overlay" @click.self="showDeckModal = false">
        <div class="deck-modal">
          <h2 class="deck-modal-title">Tạo bộ thẻ ghi nhớ</h2>
          <div class="deck-modal-section">
            <label class="deck-label">Tên bộ thẻ</label>
            <input v-model="deckForm.name" class="deck-input" :placeholder="pdfName || 'Từ vựng PDF'" />
          </div>
          <div class="deck-modal-section">
            <label class="deck-label">Danh sách {{ selectedVocabs.length }} từ sẽ được tạo:</label>
            <div class="deck-vocab-picks">
              <div v-for="(v, i) in selectedVocabs" :key="i" class="deck-vocab-pick no-hover">
                <span class="pick-word" style="font-size: 1.1rem; font-weight: bold;">{{ v.wordText }}</span>
                <span class="pick-meaning" style="font-size: 0.95rem; color: #8b949e;">{{ v.contextMeaning }}</span>
              </div>
            </div>
          </div>
          <div class="deck-modal-actions">
            <button class="btn-cancel" @click="showDeckModal = false">Hủy</button>
            <button class="btn-create" :disabled="creatingDeck" @click="createDeck">
              {{ creatingDeck ? 'Đang tạo...' : `Tạo Flashcard` }}
            </button>
          </div>
        </div>
      </div>
    </Transition>

    <!-- MODAL CONFIRM XÓA (Kèm danh sách từ) -->
    <Transition name="modal">
      <div v-if="showConfirmDelete" class="modal-overlay" @click.self="cancelDelete">
        <div class="confirm-modal">
          <div class="confirm-icon">⚠️</div>
          <h2 class="confirm-title">Xác nhận xóa</h2>
          <p class="confirm-desc">
            Bạn có chắc chắn muốn xóa <b>{{ wordsToDelete.length }}</b> từ vựng dưới đây không? Hành động này không thể hoàn tác.
          </p>
          
          <!-- Danh sách từ sẽ bị xóa -->
          <div class="delete-vocab-list">
            <div v-for="(v, i) in wordsToDelete" :key="i" class="delete-vocab-item">
              <span class="delete-word">{{ v.wordText }}</span>
              <span class="delete-meaning">{{ v.contextMeaning }}</span>
            </div>
          </div>

          <div class="confirm-actions">
            <button class="btn-cancel" @click="cancelDelete">Hủy bỏ</button>
            <button class="btn-danger" @click="executeDelete">Xóa ngay</button>
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRuntimeConfig } from '#app'
import { useToast } from '~/composables/useToast'

const props = defineProps({
  projectId: { type: [String, Number], default: null },
  pdfName: { type: String, default: '' }
})

const config = useRuntimeConfig()
const { showToast } = useToast()
const getToken = () => localStorage.getItem('jwt_token') || ''
const LOCAL_STORAGE_KEY = 'global_saved_vocabularies'

const vocabList = ref([])
const selectedVocabs = ref([])

// State cho Flashcard
const showDeckModal = ref(false)
const deckForm = ref({ name: '' })
const creatingDeck = ref(false)

// State cho Sửa từ vựng
const showEditModal = ref(false)
const editingVocab = ref(null)
const editingVocabIndex = ref(-1)
const editingVocabMeaning = ref('')
const savingEdit = ref(false)

// State cho Xóa từ vựng (Confirm Modal)
const showConfirmDelete = ref(false)
const deleteTarget = ref('bulk') // 'bulk' hoặc 'single'
const singleDeleteInfo = ref(null) // Lưu thông tin {index, vocab} khi xóa 1 từ

// Tạo Computed tự động lấy danh sách từ vựng sẽ bị xóa để hiển thị lên Form
const wordsToDelete = computed(() => {
  if (deleteTarget.value === 'single' && singleDeleteInfo.value) {
    return [singleDeleteInfo.value.vocab]
  } else if (deleteTarget.value === 'bulk') {
    return selectedVocabs.value
  }
  return []
})

watch(() => props.projectId, () => { loadVocabs() })

async function loadVocabs() {
  if (props.projectId) {
    try {
      const res = await fetch(`${config.public.apiBaseUrl}/api/projects/${props.projectId}/vocabularies`, {
        headers: { 'Authorization': `Bearer ${getToken()}` }
      })
      if (res.ok) vocabList.value = await res.json() || []
    } catch (e) { console.error("Lỗi tải từ vựng:", e) }
  } else {
    const localData = localStorage.getItem(LOCAL_STORAGE_KEY)
    if (localData) vocabList.value = JSON.parse(localData)
  }
}

async function addNewVocab(vocabInfo) {
  if (props.projectId) {
    try {
      const res = await fetch(`${config.public.apiBaseUrl}/api/projects/${props.projectId}/vocabularies`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${getToken()}` },
        body: JSON.stringify({ wordText: vocabInfo.wordText, contextMeaning: vocabInfo.contextMeaning })
      })
      
      if (res.ok) {
        const savedVocab = await res.json()
        const idx = vocabList.value.findIndex(v => v.wordText === savedVocab.wordText)
        if (idx >= 0) vocabList.value[idx] = savedVocab
        else vocabList.value.unshift(savedVocab)
        showToast("Đã lưu từ vựng vào Project", "success")
      }
    } catch (e) { console.error("Lỗi lưu từ vựng API:", e) }
  } else {
    const newItem = { id: Date.now(), wordText: vocabInfo.wordText, contextMeaning: vocabInfo.contextMeaning }
    const idx = vocabList.value.findIndex(v => v.wordText === newItem.wordText)
    if (idx >= 0) vocabList.value[idx] = newItem
    else vocabList.value.unshift(newItem)
    
    localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify(vocabList.value))
    showToast("Đã lưu từ vựng cục bộ", "success")
  }
}

function toggleSelectAllBulk(e) {
  if (e.target.checked) selectedVocabs.value = [...vocabList.value]
  else selectedVocabs.value = []
}

// ==================== LOGIC XÓA ====================
function confirmSingleDelete(index, vocab) {
  deleteTarget.value = 'single'
  singleDeleteInfo.value = { index, vocab }
  showConfirmDelete.value = true
}

function confirmBulkDelete() {
  deleteTarget.value = 'bulk'
  showConfirmDelete.value = true
}

function cancelDelete() {
  showConfirmDelete.value = false
  singleDeleteInfo.value = null
}

async function executeDelete() {
  showConfirmDelete.value = false

  if (deleteTarget.value === 'single') {
    // Xóa 1 từ
    const { index, vocab } = singleDeleteInfo.value
    if (props.projectId && vocab.id) {
      await fetch(`${config.public.apiBaseUrl}/api/projects/${props.projectId}/vocabularies/${vocab.id}`, {
        method: 'DELETE', headers: { 'Authorization': `Bearer ${getToken()}` }
      })
    }
    vocabList.value.splice(index, 1)
    
    // Bỏ check nếu từ đó đang được tick
    selectedVocabs.value = selectedVocabs.value.filter(v => v.id !== vocab.id)
    
    if (!props.projectId) localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify(vocabList.value))
    showToast("Đã xóa từ vựng", "success")
    
  } else if (deleteTarget.value === 'bulk') {
    // Xóa nhiều từ
    if (!selectedVocabs.value.length) return

    if (props.projectId) {
      try {
        const ids = selectedVocabs.value.map(v => v.id).filter(id => id)
        const queryStr = ids.map(id => `vocabIds=${id}`).join('&')
        await fetch(`${config.public.apiBaseUrl}/api/projects/${props.projectId}/vocabularies?${queryStr}`, {
          method: 'DELETE', headers: { 'Authorization': `Bearer ${getToken()}` }
        })
      } catch (e) { console.error("Lỗi xóa hàng loạt:", e) }
    }

    vocabList.value = vocabList.value.filter(v => !selectedVocabs.value.includes(v))
    selectedVocabs.value = []
    
    if (!props.projectId) localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify(vocabList.value))
    showToast("Đã xóa các từ đã chọn", "success")
  }
}

// ==================== LOGIC SỬA TỪ VỰNG ====================
function openEditModal(vocab, index) {
  editingVocab.value = vocab
  editingVocabIndex.value = index
  editingVocabMeaning.value = vocab.contextMeaning
  showEditModal.value = true
}

async function saveEditVocab() {
  if (!editingVocab.value) return
  savingEdit.value = true

  try {
    if (props.projectId && editingVocab.value.id) {
      await fetch(`${config.public.apiBaseUrl}/api/projects/${props.projectId}/vocabularies/${editingVocab.value.id}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${getToken()}` },
        body: JSON.stringify({ contextMeaning: editingVocabMeaning.value })
      })
    }
    
    vocabList.value[editingVocabIndex.value].contextMeaning = editingVocabMeaning.value
    if (!props.projectId) localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify(vocabList.value))
    
    showToast("Đã cập nhật nghĩa từ vựng", "success")
    showEditModal.value = false
  } catch (e) {
    showToast("Có lỗi xảy ra", "error")
  } finally {
    savingEdit.value = false
  }
}

// ==================== LOGIC FLASHCARD ====================
async function createDeck() {
  if (!selectedVocabs.value.length) return
  creatingDeck.value = true
  try {
    const name = deckForm.value.name || props.pdfName || 'Từ vựng PDF'
    const res = await fetch(`${config.public.apiBaseUrl}/api/decks`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${getToken()}` },
      body: JSON.stringify({
        title: name,
        description: `Tạo từ tài liệu: ${props.pdfName}`,
        isPublic: false,
        cards: selectedVocabs.value.map(v => ({ frontText: v.wordText, backText: v.contextMeaning, tags: null }))
      })
    })
    if (res.ok) {
      showDeckModal.value = false
      selectedVocabs.value = [] // Bỏ tick sau khi tạo thành công
      deckForm.value.name = ''
      showToast(`Đã tạo bộ thẻ "${name}"!`, 'success')
    } else {
      showToast('Tạo deck thất bại.', 'error')
    }
  } finally {
    creatingDeck.value = false
  }
}

defineExpose({ addNewVocab })

onMounted(() => {
  loadVocabs()
})
</script>

<style scoped>
.vocab-container { display: flex; flex-direction: column; height: 100%; }
.vocab-header { display: flex; align-items: center; justify-content: space-between; padding: 12px 16px; border-bottom: 1px solid var(--border, #30363d); flex-shrink: 0; min-height: 55px; }
.vocab-header-title { font-size: 14px; font-weight: 600; color: var(--text, #c9d1d9); }
.header-actions { display: flex; gap: 8px; }

.btn-create-deck { background: #5b8dee; color: #fff; border: none; border-radius: 6px; padding: 6px 12px; font-size: 12px; font-weight: 600; cursor: pointer; transition: opacity 0.2s; }
.btn-create-deck:hover { opacity: 0.85; }

.btn-delete-bulk { background: #ef4444; color: #fff; border: none; border-radius: 6px; padding: 6px 12px; font-size: 12px; font-weight: 600; cursor: pointer; transition: opacity 0.2s; }
.btn-delete-bulk:hover { opacity: 0.85; }

.vocab-list-controls { padding: 8px 16px; border-bottom: 1px solid #30363d; background: #161b22; }
.select-all-label { font-size: 13px; color: #8b949e; cursor: pointer; display: flex; align-items: center; gap: 8px; font-weight: 600; }

.vocab-list { flex: 1; overflow-y: auto; padding: 8px; display: flex; flex-direction: column; gap: 6px; }
.vocab-empty { text-align: center; color: #888; font-size: 13px; padding: 40px 20px; }

.vocab-item { display: flex; align-items: flex-start; gap: 10px; background: var(--surface2, #21262d); border: 1px solid var(--border, #30363d); border-radius: 8px; padding: 8px 10px; transition: border-color 0.15s, background 0.15s; }
.vocab-item:hover { border-color: #5b8dee; background: #262c36; }

.vocab-checkbox { margin-top: 4px; cursor: pointer; transform: scale(1.1); }

.vocab-content { flex: 1; cursor: pointer; padding-right: 8px; }
.vocab-item-word { font-size: 14px; font-weight: 600; color: #5b8dee; margin-bottom: 2px; }
.vocab-item-meaning { font-size: 12px; color: #8b949e; line-height: 1.4; display: -webkit-box; -webkit-line-clamp: 3; -webkit-box-orient: vertical; overflow: hidden; }

.vocab-item-del { background: transparent; border: none; color: #6e7681; cursor: pointer; font-size: 14px; padding: 2px 6px; border-radius: 4px; transition: 0.2s; }
.vocab-item-del:hover { color: #ef4444; background: rgba(239, 68, 68, 0.1); }

/* Modal Styles chung */
.modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.7); backdrop-filter: blur(2px); display: flex; align-items: center; justify-content: center; z-index: 1000; }
.deck-modal { background: var(--surface, #1c2128); border: 1px solid #30363d; border-radius: 12px; padding: 24px; width: 100%; max-width: 450px; max-height: 85vh; display: flex; flex-direction: column; gap: 16px; box-shadow: 0 20px 60px rgba(0,0,0,0.5); }
.deck-modal-title { font-size: 18px; font-weight: 700; margin: 0; color: #c9d1d9; }
.deck-modal-section { display: flex; flex-direction: column; gap: 6px; }
.deck-label { font-size: 13px; font-weight: 600; color: #8b949e; }
.deck-input { width: 100%; padding: 10px 12px; background: #0d1117; border: 1px solid #30363d; color: #c9d1d9; border-radius: 6px; outline: none; resize: vertical; font-family: inherit;}
.deck-input:focus { border-color: #5b8dee; }
.deck-vocab-picks { display: flex; flex-direction: column; gap: 6px; max-height: 250px; overflow-y: auto; background: #0d1117; padding: 10px; border-radius: 6px; border: 1px solid #30363d; }
.deck-vocab-pick { display: flex; flex-direction: column; gap: 2px; padding-bottom: 6px; border-bottom: 1px dashed #30363d; }
.deck-vocab-pick:last-child { border-bottom: none; padding-bottom: 0; }

.deck-modal-actions { display: flex; justify-content: flex-end; gap: 10px; margin-top: 10px; }
.btn-cancel { background: transparent; color: #c9d1d9; border: 1px solid #30363d; padding: 6px 16px; border-radius: 6px; cursor: pointer; }
.btn-cancel:hover { background: #30363d; }
.btn-create { background: #5b8dee; color: #fff; border: none; padding: 6px 20px; border-radius: 6px; font-weight: 600; cursor: pointer; }
.btn-create:disabled { opacity: 0.5; cursor: not-allowed; }

/* Custom Confirm Modal */
.confirm-modal { background: #1c2128; border: 1px solid #30363d; border-radius: 12px; padding: 24px; width: 100%; max-width: 450px; display: flex; flex-direction: column; align-items: center; text-align: center; gap: 12px; box-shadow: 0 20px 60px rgba(0,0,0,0.5); }
.confirm-icon { font-size: 32px; line-height: 1; }
.confirm-title { font-size: 18px; font-weight: 700; color: #c9d1d9; margin: 0; }
.confirm-desc { font-size: 14px; color: #8b949e; line-height: 1.5; margin-bottom: 8px; }

/* Styles cho danh sách từ sắp bị xóa */
.delete-vocab-list { display: flex; flex-direction: column; gap: 6px; max-height: 180px; overflow-y: auto; background: #0d1117; padding: 10px; border-radius: 6px; border: 1px solid #30363d; width: 100%; text-align: left; }
.delete-vocab-item { display: flex; flex-direction: column; gap: 2px; padding-bottom: 6px; border-bottom: 1px dashed #30363d; }
.delete-vocab-item:last-child { border-bottom: none; padding-bottom: 0; }
.delete-word { font-size: 14px; font-weight: bold; color: #ef4444; }
.delete-meaning { font-size: 15px; color: #8b949e; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }

.confirm-actions { display: flex; gap: 10px; width: 100%; justify-content: center; margin-top: 6px; }
.btn-danger { background: #ef4444; color: #fff; border: none; padding: 8px 24px; border-radius: 6px; font-weight: 600; cursor: pointer; transition: 0.2s; }
.btn-danger:hover { background: #dc2626; }

.modal-enter-active, .modal-leave-active { transition: opacity 0.2s ease, transform 0.2s ease; }
.modal-enter-from, .modal-leave-to { opacity: 0; transform: scale(0.95); }
</style>