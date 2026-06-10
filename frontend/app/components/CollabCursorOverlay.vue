<template>
  <!-- Overlay đặt absolute fill lên PDF section, pointer-events none để không chặn click -->
  <div ref="overlayRef" class="absolute inset-0 overflow-hidden pointer-events-none z-30">
    <TransitionGroup name="cursor-fade">
      <div
        v-for="cursor in activeCursors"
        :key="cursor.userId"
        class="absolute flex items-start gap-1 select-none"
        :style="{ left: cursor.xPct * 100 + '%', top: cursor.yPct * 100 + '%' }"
      >
        <!-- Con trỏ SVG -->
        <svg width="18" height="18" viewBox="0 0 18 18" :style="{ filter: `drop-shadow(0 1px 2px rgba(0,0,0,0.5))` }">
          <path d="M0 0 L0 14 L4 10 L7 17 L9 16 L6 9 L11 9 Z" :fill="cursor.color" stroke="white" stroke-width="1"/>
        </svg>
        <!-- Label tên -->
        <span
          class="text-[10px] font-bold px-1.5 py-0.5 rounded-md whitespace-nowrap shadow-lg mt-3 -ml-1"
          :style="{ backgroundColor: cursor.color, color: '#fff' }"
        >
          {{ cursor.userName }}
        </span>
      </div>
    </TransitionGroup>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, watch, ref } from 'vue'
import { useJwt } from '~/composables/useJwt'
import { useDocumentHub } from '~/composables/useDocumentHub'

const props = defineProps<{
  fileId: number | null
  currentPage: number
}>()

const emit = defineEmits<{
  'viewers-updated': [viewers: Array<{ userId: string; userName: string; color: string }>]
}>()

const { avatarUrl: selfAvatarUrl } = useJwt()
const overlayRef = ref<HTMLElement | null>(null)
const { viewers, cursors, connect, disconnect, broadcastCursor, leaveCursor } = useDocumentHub()

let moveThrottle: ReturnType<typeof setTimeout> | null = null
let expireInterval: ReturnType<typeof setInterval> | null = null

function emitViewers() {
  emit('viewers-updated', Object.values(viewers.value))
}

// Chỉ hiển thị cursors cùng trang hiện tại
const activeCursors = computed(() =>
  Object.values(cursors.value).filter(c => c.page === props.currentPage)
)

function handleMouseMove(e: MouseEvent) {
  if (!overlayRef.value || moveThrottle || !props.fileId) return
  moveThrottle = setTimeout(() => { moveThrottle = null }, 50)
  const rect = overlayRef.value.getBoundingClientRect()
  const xPct = Math.max(0, Math.min(1, (e.clientX - rect.left) / rect.width))
  const yPct = Math.max(0, Math.min(1, (e.clientY - rect.top) / rect.height))
  broadcastCursor(props.fileId, xPct, yPct, props.currentPage)
}

function startExpireTimer() {
  expireInterval = setInterval(() => {
    const now = Date.now()
    for (const [id, c] of Object.entries(cursors.value)) {
      if (now - c.lastSeen > 4000) delete cursors.value[id]
    }
  }, 1000)
}

watch(viewers, () => emitViewers(), { deep: true })

watch(() => props.fileId, async (newId) => {
  if (newId) await connect(newId, selfAvatarUrl.value ?? null)
})

onMounted(async () => {
  if (props.fileId) await connect(props.fileId, selfAvatarUrl.value ?? null)
  startExpireTimer()
  overlayRef.value?.parentElement?.addEventListener('mousemove', handleMouseMove)
})

onUnmounted(() => {
  if (moveThrottle) clearTimeout(moveThrottle)
  if (expireInterval) clearInterval(expireInterval)
  overlayRef.value?.parentElement?.removeEventListener('mousemove', handleMouseMove)
  if (props.fileId) {
    leaveCursor(props.fileId)
    disconnect(props.fileId)
  }
})
</script>

<style scoped>
.cursor-fade-enter-active { transition: opacity 0.2s ease; }
.cursor-fade-leave-active { transition: opacity 0.3s ease; }
.cursor-fade-enter-from, .cursor-fade-leave-to { opacity: 0; }
</style>
