<template>
  <div class="vocab-container">
    <div class="vocab-header">
      <span class="vocab-header-title">Sổ tay từ vựng</span>
      <button v-if="vocabList.length > 0" class="btn-create-deck" @click="showDeckModal = true">
        + Tạo Flashcard
      </button>
    </div>
    
    <div class="vocab-list">
      <div v-if="vocabList.length === 0" class="vocab-empty">
        Bôi đen từ trong tài liệu để tra cứu và lưu từ vựng.
      </div>
      <div v-for="(v, i) in vocabList" :key="v.id || i" class="vocab-item">
        <div class="vocab-content">
          <div class="vocab-item-word">{{ v.wordText }}</div>
          <div class="vocab-item-meaning">{{ v.contextMeaning }}</div>
        </div>
        <button class="vocab-item-del" @click="deleteVocab(i, v.id)">✕</button>
      </div>
    </div>

    <Transition name="modal">
      <div v-if="showDeckModal" class="modal-overlay" @click.self="showDeckModal = false">
        <div class="deck-modal">
          <h2 class="deck-modal-title">Tạo bộ thẻ ghi nhớ</h2>
          <div class="deck-modal-section">
            <label class="deck-label">Tên bộ thẻ</label>
            <input v-model="deckForm.name" class="deck-input" :placeholder="pdfName || 'Từ vựng mới'" />
          </div>
          <div class="deck-modal-section">
            <label class="deck-label">Chọn từ để tạo thẻ</label>
            <div class="deck-select-all">
              <input type="checkbox" id="selectAll" :checked="selectedIndices.length === vocabList.length" @change="toggleSelectAll" />
              <label for="selectAll" style="cursor:pointer">Chọn tất cả ({{ vocabList.length }} từ)</label>
            </div>
            <div class="deck-vocab-picks">
              <label v-for="(v, i) in vocabList" :key="i" class="deck-vocab-pick">
                <input type="checkbox" :value="i" v-model="selectedIndices" />
                <span class="pick-word">{{ v.wordText }}</span>
                <span class="pick-meaning">{{ v.contextMeaning }}</span>
              </label>
            </div>
          </div>
          <div class="deck-modal-actions">
            <button class="btn-cancel" @click="showDeckModal = false">Hủy</button>
            <button class="btn-create" :disabled="!selectedIndices.length || creatingDeck" @click="createDeck">
              {{ creatingDeck ? 'Đang tạo...' : `Tạo ${selectedIndices.length} thẻ` }}
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'
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
const showDeckModal = ref(false)
const deckForm = ref({ name: '' })
const selectedIndices = ref([])
const creatingDeck = ref(false)

// THEO DÕI NẾU ĐỔI PROJECT THÌ LOAD LẠI TỪ VỰNG
watch(() => props.projectId, () => { loadVocabs() })

async function loadVocabs() {
  if (props.projectId) {
    // 1. Tải từ Database C#
    try {
      const res = await fetch(`${config.public.apiBaseUrl}/api/projects/${props.projectId}/vocabularies`, {
        headers: { 'Authorization': `Bearer ${getToken()}` }
      })
      if (res.ok) vocabList.value = await res.json() || []
    } catch (e) { console.error("Lỗi tải từ vựng:", e) }
  } else {
    // 2. Nếu file không có Project, tải từ bộ nhớ máy tính
    const localData = localStorage.getItem(LOCAL_STORAGE_KEY)
    if (localData) vocabList.value = JSON.parse(localData)
  }
}

// ĐÃ THÊM LẠI LOGIC GỌI API ĐỂ TỪ VỰNG KHÔNG BAO GIỜ BỊ MẤT
async function addNewVocab(vocabInfo) {
  if (props.projectId) {
    // 1. Lưu vào Database C#
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
    // 2. Lưu vào bộ nhớ máy tính nếu là file tự do
    const newItem = { id: Date.now(), wordText: vocabInfo.wordText, contextMeaning: vocabInfo.contextMeaning }
    const idx = vocabList.value.findIndex(v => v.wordText === newItem.wordText)
    if (idx >= 0) vocabList.value[idx] = newItem
    else vocabList.value.unshift(newItem)
    
    localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify(vocabList.value))
    showToast("Đã lưu từ vựng cục bộ", "success")
  }
}

async function deleteVocab(index, id) {
  if (props.projectId && id) {
    // Xóa trên Database
    await fetch(`${config.public.apiBaseUrl}/api/projects/${props.projectId}/vocabularies/${id}`, {
      method: 'DELETE', headers: { 'Authorization': `Bearer ${getToken()}` }
    })
  }
  
  // Xóa trên UI
  vocabList.value.splice(index, 1)
  
  // Cập nhật lại LocalStorage
  if (!props.projectId) {
    localStorage.setItem(LOCAL_STORAGE_KEY, JSON.stringify(vocabList.value))
  }
}

function toggleSelectAll(e) {
  if (e.target.checked) selectedIndices.value = vocabList.value.map((_, i) => i)
  else selectedIndices.value = []
}

async function createDeck() {
  if (!selectedIndices.value.length) return
  creatingDeck.value = true
  try {
    const selectedVocabs = selectedIndices.value.map(i => vocabList.value[i])
    const name = deckForm.value.name || props.pdfName || 'Từ vựng PDF'
    const res = await fetch(`${config.public.apiBaseUrl}/api/decks`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${getToken()}` },
      body: JSON.stringify({
        title: name,
        description: `Tạo từ tài liệu: ${props.pdfName}`,
        isPublic: false,
        cards: selectedVocabs.map(v => ({ frontText: v.wordText, backText: v.contextMeaning, tags: null }))
      })
    })
    if (res.ok) {
      showDeckModal.value = false
      selectedIndices.value = []
      deckForm.value.name = ''
      showToast(`Đã tạo bộ thẻ "${name}"!`, 'success')
    } else {
      showToast('Tạo deck thất bại.', 'error')
    }
  } finally {
    creatingDeck.value = false
  }
}

// Mở hàm này để component cha (index.vue) gọi
defineExpose({ addNewVocab })

onMounted(() => {
  loadVocabs()
})
</script>

<style scoped>
.vocab-container { display: flex; flex-direction: column; height: 100%; }
.vocab-header { display: flex; align-items: center; justify-content: space-between; padding: 12px 16px; border-bottom: 1px solid var(--border, #30363d); flex-shrink: 0; }
.vocab-header-title { font-size: 14px; font-weight: 600; color: var(--text, #c9d1d9); }
.btn-create-deck { background: var(--accent, #f0c040); color: #111; border: none; border-radius: 7px; padding: 5px 12px; font-size: 12px; font-weight: 600; cursor: pointer; transition: opacity 0.2s; }
.btn-create-deck:hover { opacity: 0.85; }
.vocab-list { flex: 1; overflow-y: auto; padding: 8px; display: flex; flex-direction: column; gap: 6px; }
.vocab-empty { text-align: center; color: #888; font-size: 13px; padding: 40px 20px; }
.vocab-item { display: flex; align-items: flex-start; gap: 8px; background: var(--surface2, #21262d); border: 1px solid var(--border, #30363d); border-radius: 8px; padding: 8px 10px; transition: border-color 0.15s; }
.vocab-item:hover { border-color: #5b8dee; }
.vocab-content { flex: 1; }
.vocab-item-word { font-size: 13px; font-weight: 600; color: #5b8dee; }
.vocab-item-meaning { font-size: 12px; color: #8b949e; line-height: 1.4; margin-top: 2px; }
.vocab-item-del { background: transparent; border: none; color: #999; cursor: pointer; font-size: 14px; padding: 2px 4px; border-radius: 4px; }
.vocab-item-del:hover { color: #ef4444; background: rgba(239, 68, 68, 0.1); }

/* Modal Styles */
.modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.7); backdrop-filter: blur(2px); display: flex; align-items: center; justify-content: center; z-index: 1000; }
.deck-modal { background: var(--surface, #1c2128); border: 1px solid #30363d; border-radius: 12px; padding: 24px; width: 100%; max-width: 450px; max-height: 85vh; display: flex; flex-direction: column; gap: 16px; box-shadow: 0 20px 60px rgba(0,0,0,0.5); }
.deck-modal-title { font-size: 18px; font-weight: 700; margin: 0; color: #c9d1d9; }
.deck-modal-section { display: flex; flex-direction: column; gap: 6px; }
.deck-label { font-size: 13px; font-weight: 600; color: #8b949e; }
.deck-input { width: 100%; padding: 8px 12px; background: #0d1117; border: 1px solid #30363d; color: #c9d1d9; border-radius: 6px; outline: none; }
.deck-input:focus { border-color: #5b8dee; }
.deck-select-all { font-size: 13px; padding: 6px 0; border-bottom: 1px solid #30363d; margin-bottom: 4px; color: #8b949e; }
.deck-vocab-picks { display: flex; flex-direction: column; gap: 4px; max-height: 200px; overflow-y: auto; }
.deck-vocab-pick { display: flex; align-items: center; gap: 10px; padding: 6px 8px; background: #21262d; border: 1px solid #30363d; border-radius: 6px; cursor: pointer; }
.deck-vocab-pick:hover { border-color: #5b8dee; }
.pick-word { font-size: 13px; font-weight: 600; color: #5b8dee; width: 80px; flex-shrink: 0; }
.pick-meaning { font-size: 12px; color: #8b949e; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.deck-modal-actions { display: flex; justify-content: flex-end; gap: 10px; margin-top: 10px; }
.btn-cancel { background: transparent; color: #c9d1d9; border: 1px solid #30363d; padding: 6px 16px; border-radius: 6px; cursor: pointer; }
.btn-cancel:hover { background: #30363d; }
.btn-create { background: #5b8dee; color: #fff; border: none; padding: 6px 20px; border-radius: 6px; font-weight: 600; cursor: pointer; }
.btn-create:disabled { opacity: 0.5; cursor: not-allowed; }
.modal-enter-active, .modal-leave-active { transition: opacity 0.2s ease; }
.modal-enter-from, .modal-leave-to { opacity: 0; }
</style>