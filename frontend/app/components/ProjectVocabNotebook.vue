<template>
  <div class="nb-root" :class="{ 'is-review': reviewMode, 'nb-dark': isDark }">

    <!-- ═══════════════════════════════════════════
         REVIEW MODE
    ═══════════════════════════════════════════ -->
    <Transition name="mode-slide">
    <div v-if="reviewMode" class="review-shell">

      <div class="review-topbar">
        <button class="nb-back-btn" @click="exitReview">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M19 12H5M12 5l-7 7 7 7"/></svg>
          Sổ tay
        </button>
        <div class="review-counter">
          <span class="review-counter-cur">{{ reviewIndex + 1 }}</span>
          <span class="review-counter-sep">/</span>
          <span class="review-counter-tot">{{ reviewQueue.length }}</span>
          <span v-if="reviewDone.size > 0" class="review-done-badge">
            <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3"><path d="M5 13l4 4L19 7"/></svg>
            {{ reviewDone.size }}
          </span>
        </div>
        <button class="nb-ghost-btn" @click="shuffleQueue" title="Xáo thứ tự">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <polyline points="16 3 21 3 21 8"/><line x1="4" y1="20" x2="21" y2="3"/>
            <polyline points="21 16 21 21 16 21"/><line x1="15" y1="15" x2="21" y2="21"/>
          </svg>
        </button>
      </div>

      <div class="review-progress-wrap">
        <div class="review-progress-bar" :style="{ width: (reviewDone.size / reviewQueue.length * 100) + '%' }" />
      </div>

      <div class="card-scene" @click="flipCard">
        <div class="card-body" :class="{ flipped: cardFlipped }">

          <div class="card-face card-front">
            <div class="card-front-inner">
              <p class="card-word">{{ currentCard?.wordText }}</p>
              <p class="card-hint">nhấn để xem nghĩa</p>
            </div>
          </div>

          <div class="card-face card-back">
            <div class="card-back-top">
              <p class="card-word-sm">{{ currentCard?.wordText }}</p>
              <p v-if="currentCard?.contextMeaning" class="card-meaning">{{ currentCard.contextMeaning }}</p>
              <p v-else class="card-meaning-empty">Chưa có nghĩa</p>
              <div v-if="currentCard?.sourceSentence" class="card-context">
                <span class="card-context-label">ngữ cảnh</span>
                <p class="card-context-text">{{ currentCard.sourceSentence }}</p>
              </div>
              <button v-if="currentCard?.sourceOcrJobId" class="card-doc-link" @click.stop="openInReader(currentCard)">
                <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                  <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/>
                </svg>
                {{ currentCard.sourceFileName || 'Tài liệu' }} · Trang {{ currentCard.sourcePage }}
              </button>
            </div>
          </div>

        </div>
      </div>

      <!-- Buttons always outside the 3D flip — consistent layout regardless of card height -->
      <div class="card-actions" @click.stop>
        <button class="card-btn-done" :disabled="!cardFlipped" @click="markDone">
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><path d="M5 13l4 4L19 7"/></svg>
          Đã nhớ
        </button>
        <button class="card-btn-next" @click="nextCard">
          Tiếp
          <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M5 12h14M12 5l7 7-7 7"/></svg>
        </button>
      </div>

      <Transition name="fade-up">
        <div v-if="reviewDone.size === reviewQueue.length && reviewQueue.length > 0" class="review-complete">
          <div class="review-complete-icon">🎉</div>
          <p class="review-complete-title">Hoàn thành phiên ôn!</p>
          <p class="review-complete-sub">Bạn đã ôn xong {{ reviewQueue.length }} từ vựng</p>
          <div class="review-complete-actions">
            <button class="nb-btn-primary" @click="restartReview">Ôn lại từ đầu</button>
            <button class="nb-btn-ghost" @click="exitReview">Về sổ tay</button>
          </div>
        </div>
      </Transition>

    </div>
    </Transition>

    <!-- ═══════════════════════════════════════════
         NOTEBOOK LIST MODE
    ═══════════════════════════════════════════ -->
    <Transition name="mode-slide">
    <div v-if="!reviewMode" class="nb-shell">

      <div class="nb-header">
        <button class="nb-back-btn" @click="emit('go-back')">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M19 12H5M12 5l-7 7 7 7"/></svg>
          Quay lại
        </button>
        <h1 class="nb-title">Sổ tay Dự án</h1>
        <button v-if="filtered.length > 0" class="nb-btn-practice" @click="startReview">
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <rect x="2" y="3" width="20" height="14" rx="2"/><line x1="8" y1="21" x2="16" y2="21"/><line x1="12" y1="17" x2="12" y2="21"/>
          </svg>
          Luyện tập · {{ filtered.length }}
        </button>
        <div v-else class="nb-spacer"></div>
      </div>

      <div class="nb-toolbar">
        <div class="nb-search-wrap">
          <svg class="nb-search-icon" width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
          </svg>
          <input v-model="search" type="text" placeholder="Tìm từ vựng..." class="nb-search-input" />
        </div>
        <select v-model="filterProject" class="nb-select">
          <option value="">Tất cả dự án</option>
          <option v-for="p in projectList" :key="p.id" :value="p.id">{{ p.name }}</option>
        </select>
        <button v-if="filtered.length > 0" class="nb-btn-select-all" @click="toggleSelectAll">
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5">
            <rect x="3" y="3" width="18" height="18" rx="3"/>
            <path v-if="allSelected" d="M7 12l4 4 6-7"/>
          </svg>
          {{ allSelected ? 'Bỏ chọn' : 'Chọn tất cả' }}
        </button>
        <Transition name="toolbar-btn">
          <button v-if="selectedIds.length > 0" class="nb-btn-deck" @click="exportToDeck" :disabled="exporting">
            <span v-if="exporting" class="spin-sm"></span>
            <svg v-else width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="2" y="4" width="20" height="16" rx="2"/><path d="M2 10h20"/>
            </svg>
            Tạo Deck · {{ selectedIds.length }}
          </button>
        </Transition>
      </div>

      <div v-if="loading" class="nb-loading">
        <div class="nb-spinner"></div>
      </div>

      <div v-else-if="filtered.length === 0" class="nb-empty">
        <div class="nb-empty-icon">📖</div>
        <p class="nb-empty-title">Chưa có từ vựng nào</p>
        <p class="nb-empty-sub">Bôi đen từ trong Reader rồi nhấn <b>Lưu sổ tay</b></p>
      </div>

      <div v-else class="nb-list">
        <div v-for="v in filtered" :key="v.id" class="nb-item" :class="{ 'is-selected': selectedIds.includes(v.id) }">
          <label class="nb-check-wrap">
            <input type="checkbox" :value="v.id" v-model="selectedIds" class="nb-check" />
            <span class="nb-check-box"></span>
          </label>
          <div class="nb-item-body">
            <div class="nb-item-top">
              <span class="nb-word">{{ v.wordText }}</span>
              <span class="nb-project-tag">{{ v.projectName }}</span>
              <!-- SRS progress badge -->
              <span v-if="v.reviewReps && v.reviewReps > 0" class="nb-srs-badge" :class="getSrsBadgeClass(v)">
                {{ getSrsBadgeLabel(v) }}
              </span>
            </div>
            <p v-if="v.contextMeaning" class="nb-meaning">{{ v.contextMeaning }}</p>
            <p v-if="v.sourceSentence" class="nb-sentence">"{{ v.sourceSentence }}"</p>
            <div class="nb-item-actions">
              <template v-if="v.sourceOcrJobId">
                <span class="nb-source-info">
                  <svg width="11" height="11" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/>
                  </svg>
                  {{ v.sourceFileName || 'Tài liệu' }} · tr.{{ v.sourcePage }}
                </span>
                <button class="nb-link-btn" @click="openInReader(v)">Mở tài liệu</button>
                <span class="nb-dot">·</span>
              </template>
              <button class="nb-link-btn nb-link-purple" @click="findOccurrences(v)">Tìm trong dự án</button>
            </div>
          </div>
          <span class="nb-date">{{ formatDate(v.createdAt) }}</span>
        </div>
      </div>

    </div>
    </Transition>

    <!-- Occurrences Modal -->
    <Transition name="modal">
      <div v-if="occModal.visible" class="modal-backdrop" @click.self="occModal.visible = false">
        <div class="modal-box">
          <div class="modal-header">
            <div>
              <p class="modal-title">"{{ occModal.word }}"</p>
              <p class="modal-sub">trong dự án {{ occModal.projectName }}</p>
            </div>
            <button class="modal-close" @click="occModal.visible = false">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
            </button>
          </div>
          <div class="modal-body">
            <div v-if="occModal.loading" class="nb-loading"><div class="nb-spinner"></div></div>
            <div v-else-if="occModal.items.length === 0" class="modal-empty">Không tìm thấy trong tài liệu OCR</div>
            <div v-else class="occ-list">
              <div v-for="occ in occModal.items" :key="occ.fileId + '-' + occ.page" class="occ-item" @click="goToOccurrence(occ)">
                <div class="occ-item-top">
                  <span class="occ-filename">{{ occ.fileName }}</span>
                  <span class="occ-page-badge">Trang {{ occ.page }}</span>
                </div>
                <p v-if="occ.snippet" class="occ-snippet">{{ occ.snippet }}</p>
                <p class="occ-count">{{ occ.matchCount }} lần xuất hiện</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    </Transition>

    <!-- Deck Modal -->
    <Transition name="modal">
      <div v-if="deckModal.visible" class="modal-backdrop" @click.self="deckModal.visible = false">
        <div class="modal-box modal-sm">
          <div class="modal-header">
            <p class="modal-title">Tạo bộ thẻ Flashcard</p>
            <button class="modal-close" @click="deckModal.visible = false">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>
            </button>
          </div>
          <div class="modal-body">
            <p class="modal-hint">Tạo deck từ <b>{{ selectedIds.length }}</b> từ đã chọn. Để ôn kèm ngữ cảnh, dùng <b>Luyện tập</b> trong sổ tay.</p>
            <input v-model="deckModal.name" type="text" placeholder="Đặt tên bộ thẻ..." class="modal-input" @keydown.enter="confirmCreateDeck" />
            <div class="modal-footer">
              <button class="nb-btn-ghost" @click="deckModal.visible = false">Hủy</button>
              <button class="nb-btn-primary" @click="confirmCreateDeck" :disabled="exporting">
                <span v-if="exporting" class="spin-sm"></span>
                Tạo deck
              </button>
            </div>
          </div>
        </div>
      </div>
    </Transition>

  </div>
</template>

<script setup>
import { ref, computed, onMounted, inject } from 'vue'
import { useRouter } from 'vue-router'
import { useJwt } from '~/composables/useJwt'
import { useToast } from '~/composables/useToast'

const emit = defineEmits(['go-back'])
const router = useRouter()
const config = useRuntimeConfig()
const { jwt } = useJwt()
const { showToast } = useToast()

// Sync dark mode via injected theme from app.vue
const theme = inject('theme')
const isDark = computed(() => theme?.value === 'dark')

const vocabs = ref([])
const loading = ref(false)
const search = ref('')
const filterProject = ref('')
const selectedIds = ref([])
const exporting = ref(false)
const occModal = ref({ visible: false, word: '', projectId: null, projectName: '', loading: false, items: [] })
const deckModal = ref({ visible: false, name: '' })

const reviewMode = ref(false)
const reviewQueue = ref([])
const reviewIndex = ref(0)
const reviewDone = ref(new Set())
const cardFlipped = ref(false)

const currentCard = computed(() => reviewQueue.value[reviewIndex.value] ?? null)

const projectList = computed(() => {
  const map = {}
  vocabs.value.forEach(v => { if (!map[v.projectId]) map[v.projectId] = { id: v.projectId, name: v.projectName } })
  return Object.values(map)
})

const filtered = computed(() => {
  let list = vocabs.value
  if (filterProject.value) list = list.filter(v => v.projectId === Number(filterProject.value))
  if (search.value.trim()) {
    const q = search.value.toLowerCase()
    list = list.filter(v =>
      v.wordText.toLowerCase().includes(q) ||
      (v.contextMeaning || '').toLowerCase().includes(q) ||
      (v.sourceSentence || '').toLowerCase().includes(q)
    )
  }
  return list
})

const allSelected = computed(() =>
  filtered.value.length > 0 && filtered.value.every(v => selectedIds.value.includes(v.id))
)

function toggleSelectAll() {
  if (allSelected.value) {
    selectedIds.value = []
  } else {
    selectedIds.value = filtered.value.map(v => v.id)
  }
}

function startReview() {
  reviewQueue.value = [...filtered.value]
  reviewIndex.value = 0
  reviewDone.value = new Set()
  cardFlipped.value = false
  reviewMode.value = true
}
function exitReview() { reviewMode.value = false; cardFlipped.value = false }
function restartReview() { reviewDone.value = new Set(); reviewIndex.value = 0; cardFlipped.value = false }
function flipCard() { cardFlipped.value = !cardFlipped.value }

function nextCard() {
  // quality=2 (Hard) — still counts as a review attempt
  postSrsAnswer(currentCard.value, 2)
  cardFlipped.value = false
  const next = (reviewIndex.value + 1) % reviewQueue.value.length
  setTimeout(() => { reviewIndex.value = next }, 150)
}

function markDone() {
  // quality=4 (Easy) — remembered well
  postSrsAnswer(currentCard.value, 4)
  reviewDone.value = new Set([...reviewDone.value, currentCard.value?.id])
  if (reviewDone.value.size < reviewQueue.value.length) {
    let next = (reviewIndex.value + 1) % reviewQueue.value.length
    let tries = 0
    while (reviewDone.value.has(reviewQueue.value[next]?.id) && tries < reviewQueue.value.length) {
      next = (next + 1) % reviewQueue.value.length; tries++
    }
    cardFlipped.value = false
    setTimeout(() => { reviewIndex.value = next }, 150)
  }
}

async function postSrsAnswer(card, quality) {
  if (!card?.cardId) return
  try {
    await fetch(`${config.public.apiBaseUrl}/api/review/PostAnswer`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${jwt.value}` },
      body: JSON.stringify({ cardId: card.cardId, quality })
    })
    // Optimistically update local reps count for badge
    const v = vocabs.value.find(x => x.id === card.id)
    if (v) {
      v.reviewReps = (v.reviewReps || 0) + 1
      v.lastReviewedAt = new Date().toISOString()
    }
  } catch {}
}

// SRS badge helpers
function getSrsBadgeClass(v) {
  if (!v.dueDate) return 'srs-new'
  const due = new Date(v.dueDate.endsWith('Z') ? v.dueDate : v.dueDate + 'Z')
  if (due <= new Date()) return 'srs-due'
  if ((v.reviewReps || 0) >= 5) return 'srs-learned'
  return 'srs-learning'
}
function getSrsBadgeLabel(v) {
  if (!v.dueDate) return '🆕'
  const due = new Date(v.dueDate.endsWith('Z') ? v.dueDate : v.dueDate + 'Z')
  if (due <= new Date()) return '📅 Ôn hôm nay'
  if ((v.reviewReps || 0) >= 5) return '✓ Đã nhớ'
  return `📖 ${v.reviewReps}x`
}

function shuffleQueue() {
  const arr = [...reviewQueue.value]
  for (let i = arr.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [arr[i], arr[j]] = [arr[j], arr[i]]
  }
  reviewQueue.value = arr; reviewIndex.value = 0; reviewDone.value = new Set(); cardFlipped.value = false
}

async function loadVocabs() {
  loading.value = true
  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/projects/my-vocabs`, { headers: { Authorization: `Bearer ${jwt.value}` } })
    if (res.ok) vocabs.value = await res.json()
  } catch { showToast('Lỗi tải sổ tay', 'error') }
  finally { loading.value = false }
}

function formatDate(iso) {
  if (!iso) return ''
  const d = new Date(iso.endsWith('Z') ? iso : iso + 'Z')
  const diff = Date.now() - d.getTime()
  if (diff < 3600000) return `${Math.floor(diff / 60000)} phút trước`
  if (diff < 86400000) return `${Math.floor(diff / 3600000)} giờ trước`
  return d.toLocaleDateString('vi-VN')
}
function openInReader(v) {
  if (!v.sourceOcrJobId) return
  router.push(`/reader?jobId=${v.sourceOcrJobId}&projectId=${v.projectId}&page=${v.sourcePage || 1}`)
}
async function findOccurrences(v) {
  occModal.value = { visible: true, word: v.wordText, projectId: v.projectId, projectName: v.projectName, loading: true, items: [] }
  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/projects/${v.projectId}/vocabularies/${encodeURIComponent(v.wordText)}/occurrences`, { headers: { Authorization: `Bearer ${jwt.value}` } })
    if (res.ok) occModal.value.items = await res.json()
  } catch {}
  occModal.value.loading = false
}
function goToOccurrence(occ) {
  occModal.value.visible = false
  router.push(`/reader?jobId=${occ.fileId}&projectId=${occModal.value.projectId}&page=${occ.page}`)
}
function exportToDeck() { deckModal.value = { visible: true, name: '' } }
async function confirmCreateDeck() {
  if (!deckModal.value.name.trim()) return
  exporting.value = true
  try {
    const selectedVocabs = vocabs.value.filter(v => selectedIds.value.includes(v.id))
    const res = await fetch(`${config.public.apiBaseUrl}/api/decks`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${jwt.value}` },
      body: JSON.stringify({
        title: deckModal.value.name,
        description: `Sổ tay dự án - ${new Date().toLocaleDateString('vi-VN')}`,
        isPublic: false,
        cards: selectedVocabs.map(v => ({ frontText: v.wordText, backText: v.contextMeaning || v.sourceSentence || '', tags: null }))
      })
    })
    if (res.ok) {
      showToast(`Đã tạo deck "${deckModal.value.name}"`, 'success')
      selectedIds.value = []; deckModal.value.visible = false
    } else { showToast('Lỗi tạo deck', 'error') }
  } catch { showToast('Lỗi kết nối', 'error') }
  finally { exporting.value = false }
}

onMounted(loadVocabs)
</script>

<style scoped>
/* ─── Root & CSS variables ──────────────────────────────────────────── */
.nb-root {
  min-height: 100vh;
  background: var(--nb-bg);
  color: var(--nb-text);
  --nb-bg:        #f8fafc;
  --nb-surface:   #ffffff;
  --nb-border:    #e2e8f0;
  --nb-muted:     #64748b;
  --nb-text:      #0f172a;
  --nb-text-soft: #475569;
  --nb-sky:       #0ea5e9;
  --nb-sky-soft:  #e0f2fe;
  --nb-purple:    #8b5cf6;
  /* review tokens — light */
  --nb-rv-bg:             #f1f5f9;
  --nb-rv-progress-bg:    rgba(0,0,0,0.08);
  --nb-rv-counter-cur:    #0f172a;
  --nb-rv-counter-muted:  #64748b;
  --nb-rv-ctx-bg:         rgba(0,0,0,0.04);
  --nb-rv-ctx-border:     rgba(0,0,0,0.08);
  --nb-rv-ctx-text:       #475569;
  --nb-rv-btn-bg:         rgba(0,0,0,0.06);
  --nb-rv-btn-border:     rgba(0,0,0,0.1);
  --nb-rv-btn-text:       #475569;
  --nb-rv-btn-hover-bg:   rgba(0,0,0,0.1);
  --nb-rv-btn-hover-text: #0f172a;
  --nb-rv-spin-border:    rgba(0,0,0,0.2);
  transition: background 0.3s;
}
.nb-dark {
  --nb-bg:        #0f172a;
  --nb-surface:   #1e293b;
  --nb-border:    #334155;
  --nb-muted:     #94a3b8;
  --nb-text:      #f1f5f9;
  --nb-text-soft: #94a3b8;
  --nb-sky-soft:  #0c4a6e;
  /* review tokens — dark */
  --nb-rv-bg:             #0f172a;
  --nb-rv-progress-bg:    rgba(255,255,255,0.08);
  --nb-rv-counter-cur:    #f1f5f9;
  --nb-rv-counter-muted:  #6b7280;
  --nb-rv-ctx-bg:         rgba(255,255,255,0.04);
  --nb-rv-ctx-border:     rgba(255,255,255,0.08);
  --nb-rv-ctx-text:       #cbd5e1;
  --nb-rv-btn-bg:         rgba(255,255,255,0.1);
  --nb-rv-btn-border:     rgba(255,255,255,0.22);
  --nb-rv-btn-text:       #cbd5e1;
  --nb-rv-btn-hover-bg:   rgba(255,255,255,0.18);
  --nb-rv-btn-hover-text: #f1f5f9;
  --nb-rv-spin-border:    rgba(255,255,255,0.3);
}

/* ─── Shared ────────────────────────────────────────────────────────── */
.nb-back-btn {
  display: flex; align-items: center; gap: 6px;
  font-size: 13px; color: var(--nb-muted);
  background: none; border: none; cursor: pointer;
  padding: 6px 8px; border-radius: 8px;
  transition: color 0.15s, background 0.15s;
}
.nb-back-btn:hover { color: var(--nb-text); background: var(--nb-border); }

.nb-btn-primary {
  display: inline-flex; align-items: center; gap: 6px;
  padding: 8px 18px; border-radius: 10px; border: none; cursor: pointer;
  background: var(--nb-sky); color: #fff; font-size: 13px; font-weight: 600;
  transition: opacity 0.15s, transform 0.1s;
}
.nb-btn-primary:hover { opacity: 0.88; }
.nb-btn-primary:active { transform: scale(0.97); }
.nb-btn-primary:disabled { opacity: 0.5; cursor: not-allowed; }

.nb-btn-ghost {
  display: inline-flex; align-items: center; gap: 6px;
  padding: 8px 16px; border-radius: 10px; border: 1px solid var(--nb-border); cursor: pointer;
  background: transparent; color: var(--nb-text-soft); font-size: 13px; font-weight: 500;
  transition: color 0.15s, background 0.15s;
}
.nb-btn-ghost:hover { color: var(--nb-text); background: var(--nb-surface); }

.nb-ghost-btn {
  display: flex; align-items: center; background: none; border: none; cursor: pointer;
  color: var(--nb-rv-counter-muted); padding: 6px; border-radius: 8px;
  transition: color 0.15s, background 0.15s;
}
.nb-ghost-btn:hover { color: var(--nb-text); background: var(--nb-border); }

/* ─── Notebook shell ─────────────────────────────────────────────────── */
.nb-shell { padding: 24px 20px 48px; max-width: 800px; margin: 0 auto; }
.nb-spacer { width: 96px; }

.nb-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 24px; }
.nb-title { font-size: 18px; font-weight: 700; color: var(--nb-text); letter-spacing: -0.3px; }

.nb-btn-practice {
  display: inline-flex; align-items: center; gap: 6px;
  padding: 8px 16px; border-radius: 10px; border: none; cursor: pointer;
  background: linear-gradient(135deg, #6366f1 0%, #8b5cf6 100%);
  color: #fff; font-size: 13px; font-weight: 600;
  box-shadow: 0 2px 8px rgba(99,102,241,0.3);
  transition: box-shadow 0.2s, transform 0.1s;
}
.nb-btn-practice:hover { box-shadow: 0 4px 16px rgba(99,102,241,0.45); }
.nb-btn-practice:active { transform: scale(0.97); }

.nb-toolbar { display: flex; gap: 10px; margin-bottom: 20px; flex-wrap: wrap; }
.nb-search-wrap { flex: 1; min-width: 160px; position: relative; }
.nb-search-icon { position: absolute; left: 11px; top: 50%; transform: translateY(-50%); color: var(--nb-muted); pointer-events: none; }
.nb-search-input {
  width: 100%; padding: 9px 12px 9px 34px;
  background: var(--nb-surface); border: 1px solid var(--nb-border);
  border-radius: 10px; font-size: 13px; color: var(--nb-text); outline: none;
  transition: border-color 0.15s, box-shadow 0.15s;
}
.nb-search-input::placeholder { color: var(--nb-muted); }
.nb-search-input:focus { border-color: var(--nb-sky); box-shadow: 0 0 0 3px rgba(14,165,233,0.12); }

.nb-select {
  padding: 9px 12px; background: var(--nb-surface); border: 1px solid var(--nb-border);
  border-radius: 10px; font-size: 13px; color: var(--nb-text); outline: none; cursor: pointer;
}
.nb-btn-deck {
  display: inline-flex; align-items: center; gap: 6px;
  padding: 9px 16px; border-radius: 10px; border: none; cursor: pointer;
  background: var(--nb-sky); color: #fff; font-size: 13px; font-weight: 600; white-space: nowrap;
  transition: opacity 0.15s;
}
.nb-btn-deck:hover { opacity: 0.88; }
.nb-btn-deck:disabled { opacity: 0.5; cursor: not-allowed; }

.nb-btn-select-all {
  display: inline-flex; align-items: center; gap: 6px;
  padding: 9px 14px; border-radius: 10px; cursor: pointer; white-space: nowrap;
  font-size: 13px; font-weight: 600;
  background: var(--nb-surface); color: var(--nb-muted);
  border: 1px solid var(--nb-border);
  transition: background 0.15s, color 0.15s, border-color 0.15s;
}
.nb-btn-select-all:hover { color: var(--nb-text); border-color: var(--nb-sky); }

.nb-loading { display: flex; justify-content: center; padding: 80px 0; }
.nb-spinner {
  width: 28px; height: 28px; border-radius: 50%;
  border: 3px solid var(--nb-border); border-top-color: var(--nb-sky);
  animation: spin 0.7s linear infinite;
}
.nb-empty { text-align: center; padding: 80px 20px; color: var(--nb-muted); }
.nb-empty-icon { font-size: 44px; margin-bottom: 12px; }
.nb-empty-title { font-size: 15px; font-weight: 600; color: var(--nb-text-soft); margin-bottom: 4px; }
.nb-empty-sub { font-size: 13px; }

.nb-list { display: flex; flex-direction: column; gap: 6px; }
.nb-item {
  display: flex; align-items: flex-start; gap: 12px;
  padding: 14px 16px; background: var(--nb-surface);
  border: 1px solid var(--nb-border); border-radius: 14px;
  transition: border-color 0.15s, box-shadow 0.15s;
}
.nb-item:hover { border-color: var(--nb-sky); box-shadow: 0 2px 12px rgba(14,165,233,0.08); }
.nb-item.is-selected { border-color: var(--nb-sky); background: var(--nb-sky-soft); }

.nb-check-wrap { display: flex; align-items: flex-start; margin-top: 3px; cursor: pointer; }
.nb-check { display: none; }
.nb-check-box {
  width: 17px; height: 17px; border-radius: 5px; flex-shrink: 0;
  border: 2px solid var(--nb-border); background: var(--nb-surface);
  transition: border-color 0.15s, background 0.15s; position: relative;
}
.nb-check:checked + .nb-check-box { background: var(--nb-sky); border-color: var(--nb-sky); }
.nb-check:checked + .nb-check-box::after {
  content: ''; position: absolute; top: 2px; left: 5px;
  width: 5px; height: 8px;
  border: 2px solid #fff; border-top: none; border-left: none;
  transform: rotate(45deg);
}

.nb-item-body { flex: 1; min-width: 0; }
.nb-item-top { display: flex; align-items: center; gap: 8px; margin-bottom: 4px; flex-wrap: wrap; }
.nb-word { font-size: 15px; font-weight: 700; color: var(--nb-sky); }
.nb-project-tag { font-size: 11px; padding: 2px 8px; border-radius: 20px; background: var(--nb-border); color: var(--nb-muted); }
.nb-meaning { font-size: 13px; color: var(--nb-text-soft); margin-bottom: 4px; line-height: 1.5; }
.nb-sentence {
  font-size: 12px; color: var(--nb-muted); font-style: italic;
  border-left: 2px solid var(--nb-border); padding-left: 8px; margin-bottom: 8px;
  display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden;
}
.nb-item-actions { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.nb-source-info { display: inline-flex; align-items: center; gap: 4px; font-size: 11px; color: var(--nb-muted); }
.nb-dot { font-size: 11px; color: var(--nb-border); }
.nb-link-btn { font-size: 11px; font-weight: 600; color: var(--nb-sky); background: none; border: none; cursor: pointer; padding: 0; transition: opacity 0.15s; }
.nb-link-btn:hover { opacity: 0.75; }
.nb-link-purple { color: var(--nb-purple); }
.nb-date { font-size: 11px; color: var(--nb-muted); white-space: nowrap; margin-top: 2px; }

/* ─── Review shell ───────────────────────────────────────────────────── */
.review-shell {
  min-height: 100vh;
  display: flex; flex-direction: column; align-items: center;
  padding: 20px 16px 40px;
  background: var(--nb-rv-bg);
}
.review-topbar {
  display: flex; align-items: center; justify-content: space-between;
  width: 100%; max-width: 680px; margin-bottom: 14px;
}
.review-counter { display: flex; align-items: center; gap: 4px; font-size: 14px; color: var(--nb-rv-counter-muted); }
.review-counter-cur { font-weight: 700; font-size: 16px; color: var(--nb-rv-counter-cur); }
.review-counter-sep,
.review-counter-tot { color: var(--nb-rv-counter-muted); }
.review-done-badge {
  display: inline-flex; align-items: center; gap: 4px;
  margin-left: 10px; padding: 2px 8px; border-radius: 20px;
  background: rgba(16,185,129,0.12); color: #059669;
  font-size: 11px; font-weight: 600;
}
.nb-dark .review-done-badge { color: #34d399; background: rgba(16,185,129,0.15); }

.review-progress-wrap {
  width: 100%; max-width: 680px; height: 3px; border-radius: 99px;
  background: var(--nb-rv-progress-bg); overflow: hidden; margin-bottom: 32px;
}
.review-progress-bar {
  height: 100%; border-radius: 99px;
  background: linear-gradient(90deg, #10b981, #34d399);
  transition: width 0.6s cubic-bezier(0.4,0,0.2,1);
}

/* ─── Flip card ──────────────────────────────────────────────────────── */
.card-scene { width: 100%; max-width: 680px; cursor: pointer; perspective: 1200px; user-select: none; }
.card-body {
  position: relative; width: 100%;
  /* grid-stack: both faces occupy same cell → card-body height = tallest face */
  display: grid; grid-template-areas: "stack";
  transition: transform 0.55s cubic-bezier(0.4,0,0.2,1);
  transform-style: preserve-3d;
}
.card-body.flipped { transform: rotateY(180deg); }

.card-face {
  grid-area: stack; border-radius: 20px;
  backface-visibility: hidden; -webkit-backface-visibility: hidden;
}
.card-front {
  background: var(--nb-surface); border: 1px solid var(--nb-border);
  box-shadow: 0 4px 24px rgba(0,0,0,0.07);
  display: flex; align-items: center; justify-content: center;
}
.card-front-inner { text-align: center; padding: 48px 32px; }
.card-word { font-size: 42px; font-weight: 800; letter-spacing: -1px; color: var(--nb-text); line-height: 1.1; margin-bottom: 16px; }
.card-hint { font-size: 12px; color: var(--nb-muted); letter-spacing: 0.5px; }

.card-back {
  background: var(--nb-surface); border: 1px solid var(--nb-border);
  box-shadow: 0 4px 24px rgba(0,0,0,0.07);
  transform: rotateY(180deg);
  display: flex; flex-direction: column;
  padding: 28px 28px 24px;
}
.card-back-top { flex: 1; }
.card-word-sm { font-size: 20px; font-weight: 700; color: var(--nb-text); margin-bottom: 10px; }
.card-meaning { font-size: 22px; font-weight: 600; color: var(--nb-sky); line-height: 1.4; margin-bottom: 20px; }
.card-meaning-empty { font-size: 14px; color: var(--nb-muted); font-style: italic; margin-bottom: 20px; }

.card-context {
  background: var(--nb-rv-ctx-bg); border: 1px solid var(--nb-rv-ctx-border);
  border-radius: 12px; padding: 14px 16px; margin-bottom: 16px;
}
.card-context-label {
  display: block; font-size: 10px; font-weight: 700; letter-spacing: 1px;
  text-transform: uppercase; color: var(--nb-muted); margin-bottom: 6px;
}
.card-context-text { font-size: 13px; color: var(--nb-rv-ctx-text); line-height: 1.6; font-style: italic; }

.card-doc-link {
  display: inline-flex; align-items: center; gap: 5px;
  font-size: 12px; font-weight: 600; color: var(--nb-sky);
  background: var(--nb-sky-soft); border: 1px solid rgba(14,165,233,0.2);
  border-radius: 8px; padding: 5px 10px; cursor: pointer; transition: opacity 0.15s;
}
.card-doc-link:hover { opacity: 0.8; }

.card-actions { display: flex; gap: 10px; margin-top: 16px; width: 100%; max-width: 680px; }
.card-btn-done {
  flex: 1; display: flex; align-items: center; justify-content: center; gap: 6px;
  padding: 12px; border-radius: 12px; border: none; cursor: pointer;
  background: linear-gradient(135deg, #10b981, #059669); color: #fff;
  font-size: 14px; font-weight: 700;
  box-shadow: 0 2px 12px rgba(16,185,129,0.25);
  transition: box-shadow 0.2s, transform 0.1s;
}
.card-btn-done:hover { box-shadow: 0 4px 20px rgba(16,185,129,0.4); }
.card-btn-done:active { transform: scale(0.97); }
.card-btn-done:disabled { opacity: 0.45; cursor: default; box-shadow: none; }

.card-btn-next {
  flex: 1; display: flex; align-items: center; justify-content: center; gap: 6px;
  padding: 12px; border-radius: 12px; border: 1px solid var(--nb-rv-btn-border); cursor: pointer;
  background: var(--nb-rv-btn-bg); color: var(--nb-rv-btn-text);
  font-size: 14px; font-weight: 600; transition: background 0.15s, color 0.15s;
}
.card-btn-next:hover { background: var(--nb-rv-btn-hover-bg); color: var(--nb-rv-btn-hover-text); }

.review-complete {
  text-align: center; padding: 40px 20px;
  background: var(--nb-surface); border: 1px solid var(--nb-border);
  border-radius: 20px; max-width: 360px; width: 100%; margin-top: 24px;
  box-shadow: 0 4px 24px rgba(0,0,0,0.07);
}
.review-complete-icon { font-size: 48px; margin-bottom: 12px; }
.review-complete-title { font-size: 20px; font-weight: 800; color: var(--nb-text); margin-bottom: 6px; }
.review-complete-sub { font-size: 13px; color: var(--nb-muted); margin-bottom: 24px; }
.review-complete-actions { display: flex; gap: 10px; justify-content: center; flex-wrap: wrap; }

/* ─── Modals ─────────────────────────────────────────────────────────── */
.modal-backdrop {
  position: fixed; inset: 0; z-index: 9999;
  display: flex; align-items: center; justify-content: center; padding: 16px;
  background: rgba(0,0,0,0.45); backdrop-filter: blur(6px);
}
.modal-box {
  background: var(--nb-surface); border: 1px solid var(--nb-border);
  border-radius: 18px; box-shadow: 0 24px 60px rgba(0,0,0,0.15);
  width: 100%; max-width: 520px;
  max-height: 80vh; display: flex; flex-direction: column; overflow: hidden;
}
.modal-sm { max-width: 360px; }
.modal-header { display: flex; align-items: flex-start; justify-content: space-between; padding: 20px 20px 0; }
.modal-title { font-size: 15px; font-weight: 700; color: var(--nb-text); }
.modal-sub { font-size: 12px; color: var(--nb-muted); margin-top: 2px; }
.modal-close {
  background: none; border: none; cursor: pointer;
  color: var(--nb-muted); padding: 4px; border-radius: 6px;
  transition: color 0.15s, background 0.15s;
}
.modal-close:hover { color: var(--nb-text); background: var(--nb-border); }
.modal-body { padding: 16px 20px 20px; overflow-y: auto; flex: 1; }
.modal-hint { font-size: 12px; color: var(--nb-muted); margin-bottom: 14px; line-height: 1.5; }
.modal-input {
  width: 100%; padding: 10px 12px;
  background: var(--nb-bg); border: 1px solid var(--nb-border);
  border-radius: 10px; font-size: 13px; color: var(--nb-text);
  outline: none; margin-bottom: 16px;
  transition: border-color 0.15s, box-shadow 0.15s;
}
.modal-input:focus { border-color: var(--nb-sky); box-shadow: 0 0 0 3px rgba(14,165,233,0.12); }
.modal-input::placeholder { color: var(--nb-muted); }
.modal-empty { text-align: center; color: var(--nb-muted); padding: 40px 0; font-size: 13px; }
.modal-footer { display: flex; gap: 8px; justify-content: flex-end; }

.occ-list { display: flex; flex-direction: column; gap: 6px; }
.occ-item {
  padding: 12px 14px; border-radius: 12px; border: 1px solid var(--nb-border);
  cursor: pointer; transition: border-color 0.15s, background 0.15s;
}
.occ-item:hover { border-color: var(--nb-sky); background: var(--nb-sky-soft); }
.occ-item-top { display: flex; align-items: center; justify-content: space-between; margin-bottom: 4px; }
.occ-filename { font-size: 13px; font-weight: 600; color: var(--nb-text); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.occ-page-badge { font-size: 11px; padding: 2px 8px; border-radius: 20px; flex-shrink: 0; margin-left: 8px; background: var(--nb-sky-soft); color: var(--nb-sky); font-weight: 600; }
.occ-snippet { font-size: 12px; color: var(--nb-text-soft); font-style: italic; margin-bottom: 4px; display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; }
.occ-count { font-size: 11px; color: var(--nb-muted); }

/* ─── Misc ───────────────────────────────────────────────────────────── */
.spin-sm {
  display: inline-block; width: 14px; height: 14px; border-radius: 50%;
  border: 2px solid var(--nb-rv-spin-border); border-top-color: currentColor;
  animation: spin 0.6s linear infinite;
}
@keyframes spin { to { transform: rotate(360deg); } }

.nb-srs-badge {
  font-size: 10px; font-weight: 700; padding: 2px 7px; border-radius: 20px;
  white-space: nowrap;
}
.srs-due    { background: rgba(239,68,68,0.12);  color: #ef4444; }
.srs-new    { background: rgba(99,102,241,0.12); color: #6366f1; }
.srs-learning { background: rgba(245,158,11,0.12); color: #f59e0b; }
.srs-learned  { background: rgba(16,185,129,0.12); color: #10b981; }

/* ─── Transitions ────────────────────────────────────────────────────── */
.modal-enter-active, .modal-leave-active { transition: opacity 0.2s, transform 0.2s; }
.modal-enter-from, .modal-leave-to { opacity: 0; transform: scale(0.95); }
.mode-slide-enter-active, .mode-slide-leave-active { transition: opacity 0.25s, transform 0.25s; }
.mode-slide-enter-from { opacity: 0; transform: translateX(20px); }
.mode-slide-leave-to   { opacity: 0; transform: translateX(-20px); }
.fade-up-enter-active, .fade-up-leave-active { transition: opacity 0.3s, transform 0.3s; }
.fade-up-enter-from, .fade-up-leave-to { opacity: 0; transform: translateY(16px); }
.toolbar-btn-enter-active, .toolbar-btn-leave-active { transition: opacity 0.2s, transform 0.2s; }
.toolbar-btn-enter-from, .toolbar-btn-leave-to { opacity: 0; transform: scale(0.85); }
</style>
