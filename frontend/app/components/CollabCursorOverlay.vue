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
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRuntimeConfig } from '#app'
import { useJwt } from '~/composables/useJwt'
import * as signalR from '@microsoft/signalr'

const props = defineProps<{
  fileId: number | null
  currentPage: number
}>()

const emit = defineEmits<{
  'viewers-updated': [viewers: Array<{ userId: string; userName: string; color: string }>]
}>()

const config = useRuntimeConfig()
const { jwt, avatarUrl: selfAvatarUrl } = useJwt()
const overlayRef = ref<HTMLElement | null>(null)

// Map userId → cursor state với timestamp để auto-expire
const cursors = ref<Record<string, {
  userId: string
  userName: string
  xPct: number
  yPct: number
  page: number
  color: string
  lastSeen: number
}>>({})

// Danh sách người đang xem (không phân biệt trang)
const viewers = ref<Record<string, { userId: string; userName: string; color: string; avatarUrl?: string }>>({})

function emitViewers() {
  emit('viewers-updated', Object.values(viewers.value))
}

// Chỉ hiển thị cursors cùng trang hiện tại
const activeCursors = computed(() =>
  Object.values(cursors.value).filter(c => c.page === props.currentPage)
)

let hubConnection: signalR.HubConnection | null = null
let moveThrottle: ReturnType<typeof setTimeout> | null = null
let expireInterval: ReturnType<typeof setInterval> | null = null

// Hash userName → màu nhất quán
function getColor(str: string): string {
  const colors = ['#e74c3c','#3498db','#2ecc71','#f39c12','#9b59b6','#1abc9c','#e67e22','#e91e63']
  let hash = 0
  for (let i = 0; i < str.length; i++) hash = str.charCodeAt(i) + ((hash << 5) - hash)
  return colors[Math.abs(hash) % colors.length]
}

function setupSignalR() {
  const token = localStorage.getItem('jwt_token') || jwt.value
  if (!token || !props.fileId) return

  hubConnection = new signalR.HubConnectionBuilder()
    .withUrl(`${config.public.apiBaseUrl}/notificationHub`, {
      accessTokenFactory: () => localStorage.getItem('jwt_token') || token,
      transport: signalR.HttpTransportType.WebSockets
        | signalR.HttpTransportType.ServerSentEvents
        | signalR.HttpTransportType.LongPolling,
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000])
    .configureLogging(signalR.LogLevel.None)
    .build()

  hubConnection.on('RoomViewers', (list: any[]) => {
    viewers.value = {}
    for (const u of list) {
      viewers.value[u.userId] = { userId: u.userId, userName: u.userName, color: getColor(u.userId), avatarUrl: u.avatarUrl }
    }
    emitViewers()
  })

  hubConnection.on('CursorMoved', (data: any) => {
    cursors.value[data.userId] = {
      userId: data.userId,
      userName: data.userName,
      xPct: data.xPct,
      yPct: data.yPct,
      page: data.page,
      color: getColor(data.userId),
      lastSeen: Date.now(),
    }
  })

  hubConnection.on('CursorLeft', (userId: string) => {
    delete cursors.value[userId]
  })

  hubConnection.on('UserJoined', (data: any) => {
    viewers.value[data.userId] = { userId: data.userId, userName: data.userName, color: getColor(data.userId), avatarUrl: data.avatarUrl }
    emitViewers()
  })

  hubConnection.on('UserLeft', (userId: string) => {
    delete viewers.value[userId]
    delete cursors.value[userId]
    emitViewers()
  })

  hubConnection.start()
    .then(() => {
      hubConnection?.invoke('JoinDocumentRoom', Number(props.fileId), selfAvatarUrl.value ?? null)
    })
    .catch(() => {})
}

function handleMouseMove(e: MouseEvent) {
  if (!hubConnection || hubConnection.state !== signalR.HubConnectionState.Connected) return
  if (!overlayRef.value) return
  if (moveThrottle) return

  moveThrottle = setTimeout(() => { moveThrottle = null }, 50)

  const rect = overlayRef.value.getBoundingClientRect()
  const xPct = Math.max(0, Math.min(1, (e.clientX - rect.left) / rect.width))
  const yPct = Math.max(0, Math.min(1, (e.clientY - rect.top) / rect.height))

  hubConnection.invoke('BroadcastCursor', Number(props.fileId), xPct, yPct, props.currentPage)
    .catch(() => {})
}

// Auto-expire cursors không cập nhật sau 4 giây (phòng mất kết nối đột ngột)
function startExpireTimer() {
  expireInterval = setInterval(() => {
    const now = Date.now()
    for (const [id, c] of Object.entries(cursors.value)) {
      if (now - c.lastSeen > 4000) delete cursors.value[id]
    }
  }, 1000)
}

watch(() => props.fileId, (newId) => {
  if (newId && hubConnection?.state === signalR.HubConnectionState.Connected) {
    hubConnection.invoke('JoinDocumentRoom', Number(newId)).catch(() => {})
  }
})

onMounted(() => {
  setupSignalR()
  startExpireTimer()
  // Gắn vào parent element (PDF section) thay vì overlay để nhận đúng events
  overlayRef.value?.parentElement?.addEventListener('mousemove', handleMouseMove)
})

onUnmounted(() => {
  if (moveThrottle) clearTimeout(moveThrottle)
  if (expireInterval) clearInterval(expireInterval)
  overlayRef.value?.parentElement?.removeEventListener('mousemove', handleMouseMove)

  if (hubConnection) {
    if (props.fileId && hubConnection.state === signalR.HubConnectionState.Connected) {
      hubConnection.invoke('LeaveCursor', Number(props.fileId)).catch(() => {})
    }
    hubConnection.stop()
  }
})
</script>

<style scoped>
.cursor-fade-enter-active { transition: opacity 0.2s ease; }
.cursor-fade-leave-active { transition: opacity 0.3s ease; }
.cursor-fade-enter-from, .cursor-fade-leave-to { opacity: 0; }
</style>
