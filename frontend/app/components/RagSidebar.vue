<template>
  <!-- Fixed right sidebar matching app design system -->
  <div
    :style="open ? { width: sidebarWidth + 'px' } : { width: '32px' }"
    class="fixed right-0 top-[64px] h-[calc(100vh-64px)] z-30 flex bg-white dark:bg-neutral-900 border-l border-gray-200 dark:border-neutral-700 transition-[width] duration-100 shadow-xl overflow-visible"
  >
    <!-- Drag resize handle (only when open) -->
    <div
      v-if="open"
      @mousedown="onResizeStart"
      class="absolute left-0 top-0 w-1 h-full cursor-col-resize z-20 hover:bg-yellow-400/40 transition-colors group"
      title="Kéo để thay đổi kích thước"
    >
      <div class="absolute left-0 top-1/2 -translate-y-1/2 w-1 h-12 rounded-full bg-gray-300 dark:bg-neutral-600 group-hover:bg-yellow-400 transition-colors opacity-0 group-hover:opacity-100" />
    </div>
    <!-- Collapse/expand tab on left edge -->
    <button
      @click="open = !open"
      class="absolute -left-4 top-1/2 -translate-y-1/2 z-20 w-7 h-12 bg-white dark:bg-neutral-800 border border-gray-200 dark:border-neutral-700 rounded-l-xl flex items-center justify-center text-gray-400 hover:text-yellow-500 dark:hover:text-yellow-400 hover:border-yellow-400/50 transition-all shadow-md"
      :title="open ? 'Thu gọn AI RAG' : 'Mở AI RAG'"
    >
      <svg class="w-3 h-3 transition-transform duration-300" :class="open ? '' : 'rotate-180'" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M9 5l7 7-7 7"/>
      </svg>
    </button>

    <!-- Collapsed -->
    <div v-if="!open" class="flex flex-col items-center justify-center w-8 h-full cursor-pointer gap-1 select-none" @click="open = true">
      <span class="text-yellow-500 dark:text-yellow-400 text-[9px] font-bold" style="writing-mode:vertical-rl;letter-spacing:0.12em">🌐 AI RAG</span>
    </div>

    <!-- Expanded -->
    <div v-if="open" class="flex w-full h-full overflow-hidden">

      <!-- Chat area (bên trái) -->
      <div class="flex-1 flex flex-col min-w-0 overflow-hidden bg-white dark:bg-neutral-900">
        <!-- Header -->
        <div class="px-3 pt-3 pb-2 border-b border-gray-200 dark:border-neutral-700 bg-gray-50 dark:bg-neutral-800 shrink-0 flex items-center gap-2 min-w-0">
          <button @click="showSessions = !showSessions" class="text-gray-400 dark:text-neutral-500 hover:text-gray-700 dark:hover:text-white transition shrink-0" title="Danh sách hội thoại">
            <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16"/></svg>
          </button>
          <span class="text-[11px] font-semibold text-gray-700 dark:text-white flex items-center gap-1 truncate">
            🌐 <span class="text-yellow-500 dark:text-yellow-400">AI RAG</span>
            <span class="text-gray-400 dark:text-neutral-500 font-normal truncate">— {{ scopeLabel }}</span>
          </span>
        </div>

        <!-- WorkspaceRagChat -->
        <div class="flex-1 overflow-hidden">
          <WorkspaceRagChat
            :workspace-id="workspaceId ?? null"
            :project-id="projectId ?? null"
            :scope-label="scopeLabel"
            :load-session-id="loadSessionId"
            @highlight-doc="emit('highlight-doc', $event)"
            @session-saved="loadSessions"
          />
        </div>
      </div>

      <!-- Sessions list (bên phải) -->
      <div :class="['shrink-0 border-l border-gray-200 dark:border-neutral-700 bg-white dark:bg-neutral-900 flex flex-col transition-all duration-200 overflow-hidden', showSessions ? 'w-36' : 'w-0']">
        <div class="px-3 pt-3 pb-2 shrink-0 flex items-center justify-between">
          <span class="text-[9px] font-bold text-gray-400 dark:text-neutral-500 uppercase tracking-widest whitespace-nowrap">Gần đây</span>
          <button
            @click="newSession"
            class="text-[9px] text-blue-500 dark:text-blue-400 hover:text-blue-700 dark:hover:text-blue-300 transition whitespace-nowrap"
            title="Hội thoại mới"
          >+ Mới</button>
        </div>
        <div class="flex-1 overflow-y-auto custom-scrollbar px-1 pb-2">
          <div v-if="sessions.length === 0" class="text-[10px] text-gray-400 dark:text-neutral-600 px-2 py-1 italic">Chưa có hội thoại</div>

          <!-- Pinned -->
          <div v-if="sessions.some(s => s.isPinned)" class="px-2 pt-2 pb-0.5">
            <span class="text-[8px] text-gray-400 dark:text-neutral-600 uppercase tracking-widest">📌 Đã ghim</span>
          </div>

          <template v-for="s in sessions" :key="s.id">
            <!-- Divider before unpinned -->
            <div v-if="!s.isPinned && sessions.some(x => x.isPinned)" class="mx-2 my-1 h-px bg-gray-200 dark:bg-neutral-700" />

            <!-- Inline rename mode -->
            <div v-if="editingId === s.id" class="px-2 py-1">
              <input
                v-model="editingTitle"
                @keydown.enter="saveRename(s.id)"
                @keydown.escape="editingId = null"
                @blur="saveRename(s.id)"
                autofocus
                class="w-full text-[10px] bg-white dark:bg-neutral-800 border border-blue-400 rounded px-1.5 py-0.5 outline-none text-gray-700 dark:text-neutral-200"
              />
            </div>

            <!-- Normal mode -->
            <button
              v-else
              @click="activeSessionId = s.id; loadSessionId = s.id"
              :class="[
                'w-full text-left px-2 py-1.5 rounded-lg text-[10px] transition group relative mb-0.5',
                activeSessionId === s.id
                  ? 'bg-yellow-50 dark:bg-yellow-400/10 text-yellow-700 dark:text-yellow-400'
                  : 'text-gray-600 dark:text-neutral-400 hover:bg-gray-100 dark:hover:bg-neutral-800'
              ]"
            >
              <div class="truncate pr-10 font-medium">{{ s.isPinned ? '📌 ' : '' }}{{ s.title }}</div>
              <div class="text-[9px] text-gray-400 dark:text-neutral-600 mt-0.5">{{ s.messageCount }} tin</div>
              <!-- Action icons on hover -->
              <div class="absolute right-1 top-1.5 opacity-0 group-hover:opacity-100 flex gap-1 transition">
                <span @click.stop="togglePin(s.id)" :title="s.isPinned ? 'Bỏ ghim' : 'Ghim'" class="cursor-pointer text-gray-400 hover:text-yellow-500 transition text-[9px]">{{ s.isPinned ? '🔓' : '📌' }}</span>
                <span @click.stop="startRename(s)" title="Đổi tên" class="cursor-pointer text-gray-400 hover:text-blue-500 transition text-[9px]">✏️</span>
                <span @click.stop="deleteSession(s.id)" title="Xóa" class="cursor-pointer text-gray-400 hover:text-red-500 transition text-[9px]">✕</span>
              </div>
            </button>
          </template>
        </div>
      </div>

    </div>
  </div>

  <!-- Spacer -->
  <div :style="open ? { width: sidebarWidth + 'px' } : { width: '32px' }" class="shrink-0 transition-[width] duration-100 pointer-events-none" aria-hidden="true" />
</template>

<script setup lang="ts">
import { ref, computed, watch, nextTick } from 'vue'

const props = defineProps<{
  workspaceId?: number | null
  projectId?: number | null
}>()

const emit = defineEmits<{
  (e: 'highlight-doc', jobId: number | null): void
  (e: 'new-session'): void
  (e: 'load-session', sessionId: number): void
}>()

const open = ref(true)
const showSessions = ref(true)
const sidebarWidth = ref(380)
const minWidth = 260
const maxWidth = 600

function onResizeStart(e: MouseEvent) {
  e.preventDefault()
  const startX = e.clientX
  const startW = sidebarWidth.value
  function onMove(ev: MouseEvent) {
    const delta = startX - ev.clientX
    sidebarWidth.value = Math.min(maxWidth, Math.max(minWidth, startW + delta))
  }
  function onUp() {
    window.removeEventListener('mousemove', onMove)
    window.removeEventListener('mouseup', onUp)
  }
  window.addEventListener('mousemove', onMove)
  window.addEventListener('mouseup', onUp)
}

type SessionSummary = { id: number; title: string; messageCount: number; updatedAt: string; isPinned: boolean }
const sessions = ref<SessionSummary[]>([])
const activeSessionId = ref<number | null>(null)
const loadSessionId = ref<number | null>(null)
const editingId = ref<number | null>(null)
const editingTitle = ref('')

const scopeType = computed(() => props.projectId ? 'project' : 'workspace')
const scopeId = computed(() => props.projectId ?? props.workspaceId ?? 0)

const scopeLabel = computed(() => {
  if (props.projectId) return 'Dự án'
  if (props.workspaceId) return 'Workspace'
  return 'Tài liệu'
})

function getAuthHeaders() {
  const token = typeof localStorage !== 'undefined' ? localStorage.getItem('jwt_token') || '' : ''
  return { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` }
}

const config = useRuntimeConfig()

async function loadSessions() {
  if (!scopeId.value) return
  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/chat/sessions?scopeType=${scopeType.value}&scopeId=${scopeId.value}`, { headers: getAuthHeaders() })
    if (res.ok) sessions.value = await res.json()
  } catch { /* silent */ }
}

async function newSession() {
  activeSessionId.value = null
  loadSessionId.value = -1
  await nextTick()
  loadSessionId.value = null
  emit('new-session')
  await loadSessions()
}

async function deleteSession(id: number) {
  try {
    await fetch(`${config.public.apiBaseUrl}/api/chat/sessions/${id}`, { method: 'DELETE', headers: getAuthHeaders() })
    sessions.value = sessions.value.filter(s => s.id !== id)
    if (activeSessionId.value === id) { activeSessionId.value = null; emit('new-session') }
  } catch { /* silent */ }
}

async function togglePin(id: number) {
  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/chat/sessions/${id}/pin`, { method: 'PATCH', headers: getAuthHeaders() })
    if (res.ok) {
      const data = await res.json()
      const s = sessions.value.find(x => x.id === id)
      if (s) s.isPinned = data.isPinned
      sessions.value = [...sessions.value].sort((a, b) => (b.isPinned ? 1 : 0) - (a.isPinned ? 1 : 0))
    }
  } catch { /* silent */ }
}

function startRename(s: SessionSummary) {
  editingId.value = s.id
  editingTitle.value = s.title
}

async function saveRename(id: number) {
  if (!editingTitle.value.trim()) { editingId.value = null; return }
  try {
    const res = await fetch(`${config.public.apiBaseUrl}/api/chat/sessions/${id}/title`, {
      method: 'PATCH',
      headers: getAuthHeaders(),
      body: JSON.stringify({ title: editingTitle.value.trim() })
    })
    if (res.ok) {
      const s = sessions.value.find(x => x.id === id)
      if (s) s.title = editingTitle.value.trim()
    }
  } catch { /* silent */ }
  editingId.value = null
}

watch([() => props.workspaceId, () => props.projectId], () => { sessions.value = []; loadSessions() }, { immediate: true })
</script>

<style scoped>
.custom-scrollbar::-webkit-scrollbar { width: 3px; }
.custom-scrollbar::-webkit-scrollbar-track { background: transparent; }
.custom-scrollbar::-webkit-scrollbar-thumb { background-color: #d1d5db; border-radius: 10px; }
.dark .custom-scrollbar::-webkit-scrollbar-thumb { background-color: #404040; }
</style>
