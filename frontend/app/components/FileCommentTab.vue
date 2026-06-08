<template>
  <div class="flex flex-col h-full bg-[#0d1117] border-l border-[#30363d] text-[#c9d1d9]">
    <div class="p-4 border-b border-[#30363d] bg-[#161b22] flex items-center justify-between shrink-0">
      <h3 class="font-bold text-white flex items-center gap-2">
        <svg class="w-5 h-5 text-[#f0c040]" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 8h2a2 2 0 012 2v6a2 2 0 01-2 2h-2v4l-4-4H9a1.994 1.994 0 01-1.414-.586m0 0L11 14h4a2 2 0 002-2V6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2v4l.586-.586z"/></svg>
        Thảo luận
      </h3>
      <span class="text-[10px] font-mono bg-[#21262d] border border-[#30363d] text-gray-400 px-2 py-1 rounded uppercase tracking-wider">Trang: {{ currentPage }}</span>
    </div>

    <div class="flex-1 overflow-y-auto p-4 space-y-6 custom-scrollbar relative bg-[#0d1117]">
      <div v-if="loading" class="absolute inset-0 flex justify-center items-center bg-[#0d1117]/80 z-10">
        <div class="w-6 h-6 border-2 border-[#f0c040] border-t-transparent rounded-full animate-spin"></div>
      </div>

      <div v-if="comments.length === 0 && !loading" class="text-center text-gray-500 text-sm mt-10">
        <div class="w-16 h-16 mx-auto mb-4 bg-[#161b22] rounded-full flex items-center justify-center border border-[#30363d]">
          <svg class="w-8 h-8 opacity-20" fill="currentColor" viewBox="0 0 20 20"><path d="M2 5a2 2 0 012-2h7a2 2 0 012 2v4a2 2 0 01-2 2H9l-3 3v-3H4a2 2 0 01-2-2V5z"/></svg>
        </div>
        Chưa có bình luận nào.<br/>Hãy đặt câu hỏi đầu tiên!
      </div>

      <div v-for="c in comments" :key="c.id" class="flex gap-3">
        <div class="w-8 h-8 rounded-lg bg-gradient-to-br from-[#1f6feb] to-[#58a6ff] text-white flex items-center justify-center font-bold text-xs shrink-0 uppercase shadow-lg">
          {{ c.userName.charAt(0) }}
        </div>

        <div class="flex-1 min-w-0">
          <div class="bg-[#161b22] border border-[#30363d] rounded-xl rounded-tl-none p-3 relative group hover:border-[#484f58] transition-colors">
            <div class="flex items-center justify-between mb-1.5">
              <span class="font-bold text-sm text-[#58a6ff]">{{ c.userName }}</span>
              <span class="text-[10px] text-gray-500 font-mono">{{ formatDate(c.createdAt) }}</span>
            </div>
            
            <p class="text-[13px] leading-relaxed text-[#c9d1d9] whitespace-pre-wrap break-words" :class="{'italic text-gray-600': c.isDeleted}">
              {{ c.content }}
            </p>

            <div class="flex items-center gap-2 mt-2.5">
               <button v-if="c.pageNumber && !c.isDeleted" @click="emitJumpToPage(c.pageNumber)" 
                class="inline-flex items-center gap-1.5 text-[10px] font-bold bg-[#21262d] border border-[#30363d] text-[#f0c040] hover:bg-[#30363d] px-2 py-1 rounded transition-all">
                <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"/></svg>
                TRANG {{ c.pageNumber }}
              </button>
            </div>

            <button v-if="c.userId === currentUserId && !c.isDeleted" @click="confirmDeleteClick(c.id)" 
              class="absolute bottom-2 right-2 opacity-0 group-hover:opacity-100 text-gray-500 hover:text-red-400 p-1.5 hover:bg-red-500/10 rounded-lg transition-all" title="Thu hồi">
              <svg class="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"/></svg>
            </button>
          </div>
        </div>
      </div>
    </div>

    <div class="p-4 bg-[#161b22] border-t border-[#30363d] shrink-0 shadow-[0_-10px_20px_rgba(0,0,0,0.2)]">
      <div class="flex flex-col gap-3">
        <textarea v-model="newComment" rows="2" placeholder="Nhập thảo luận của bạn..." 
          class="w-full bg-[#0d1117] border border-[#30363d] text-[#c9d1d9] text-sm rounded-xl p-3 outline-none focus:border-[#58a6ff] focus:ring-1 focus:ring-[#58a6ff] transition-all resize-none placeholder-gray-600"
          @keydown.enter.exact.prevent="submitComment"></textarea>
        
        <div class="flex items-center justify-between">
          <label class="flex items-center gap-2 text-[11px] text-gray-500 cursor-pointer hover:text-gray-300 transition-colors">
            <input type="checkbox" v-model="attachPage" class="rounded border-[#484f58] bg-[#0d1117] text-[#f0c040] focus:ring-[#f0c040] focus:ring-offset-0" />
            Gắn thẻ trang {{ currentPage }}
          </label>

          <button @click="submitComment" :disabled="!newComment.trim() || submitting" 
            class="bg-[#f0c040] hover:bg-[#e3b330] disabled:bg-[#30363d] disabled:text-gray-500 text-black px-5 py-2 rounded-xl text-xs font-black uppercase tracking-widest transition-all flex items-center gap-2 shadow-lg active:scale-95">
            <span v-if="submitting" class="w-3 h-3 border-2 border-black border-t-transparent rounded-full animate-spin"></span>
            Gửi ngay
          </button>
        </div>
      </div>
    </div>
  </div>

  <Teleport to="body">
    <Transition name="modal-fade">
      <div v-if="showDeleteConfirm" class="fixed inset-0 z-[10000] flex items-center justify-center bg-black/80 backdrop-blur-sm p-4" @click.self="showDeleteConfirm = false">
        <div class="bg-[#161b22] border border-[#30363d] rounded-2xl p-6 w-full max-w-sm shadow-2xl overflow-hidden relative">
          <div class="absolute top-0 left-0 right-0 h-1 bg-red-500"></div>

          <div class="flex items-center gap-4 mb-4">
            <div class="w-12 h-12 rounded-full bg-red-500/10 flex items-center justify-center text-red-500 shrink-0">
              <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"/></svg>
            </div>
            <div>
              <h3 class="text-lg font-bold text-white leading-tight">Xác nhận thu hồi</h3>
              <p class="text-xs text-gray-500 mt-1 uppercase font-bold tracking-tighter">Bình luận của bạn</p>
            </div>
          </div>
          
          <p class="text-sm text-gray-400 mb-6 leading-relaxed">Sau khi thu hồi, người khác sẽ không thể xem nội dung này. Hành động này không thể hoàn tác.</p>
          
          <div class="flex gap-3">
            <button @click="showDeleteConfirm = false" class="flex-1 px-4 py-2.5 text-sm font-bold rounded-xl border border-[#30363d] text-gray-300 hover:bg-[#21262d] transition-all">
              Hủy
            </button>
            <button @click="executeDelete" class="flex-1 px-4 py-2.5 text-sm font-bold rounded-xl bg-red-600 hover:bg-red-700 text-white transition-all shadow-lg active:scale-95">
              Thu hồi
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, nextTick } from 'vue'
import { useRuntimeConfig } from '#app'
import { useJwt } from '~/composables/useJwt'
import { useFileComment } from '~/composables/useFileComment'
import { useToast } from '~/composables/useToast'
import * as signalR from '@microsoft/signalr'

const props = defineProps<{
  fileId: number
  currentPage: number
}>()

const emit = defineEmits(['jump-to-page'])

const config = useRuntimeConfig()
const { jwt, userId: currentUserId } = useJwt()
const { getComments, addComment, deleteComment } = useFileComment()
const { showToast } = useToast()

const comments = ref<any[]>([])
const loading = ref(false)
const newComment = ref('')
const attachPage = ref(true)
const submitting = ref(false)
const showDeleteConfirm = ref(false)
const commentIdToDelete = ref<number | null>(null)
let hubConnection: signalR.HubConnection | null = null

function formatDate(d: string) {
  if (!d) return ''
  const dateString = d.endsWith('Z') ? d : d + 'Z'
  const date = new Date(dateString)
  const now = new Date()
  const diff = Math.floor((now.getTime() - date.getTime()) / 60000)
  if (diff < 1) return 'Vừa xong'
  if (diff < 60) return `${diff}m`
  if (diff < 1440) return `${Math.floor(diff / 60)}h`
  return date.toLocaleDateString('vi-VN', { month: '2-digit', day: '2-digit' })
}

function setupSignalR() {
  const token = localStorage.getItem('jwt_token') || jwt.value
  if (!token) return

  hubConnection = new signalR.HubConnectionBuilder()
    .withUrl(`${config.public.apiBaseUrl}/notificationHub`, {
      accessTokenFactory: () => token,
      // Fallback: WebSockets → ServerSentEvents → LongPolling
      // Cần thiết khi production proxy không forward WebSocket upgrade headers
      transport: signalR.HttpTransportType.WebSockets
        | signalR.HttpTransportType.ServerSentEvents
        | signalR.HttpTransportType.LongPolling,
      skipNegotiation: false,
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Warning)
    .build()

  hubConnection.on("ReceiveNewComment", (newCmt) => {
    comments.value.push(newCmt)
    scrollToBottom()
  })

  hubConnection.on("CommentDeleted", (commentId) => {
    const target = comments.value.find(c => c.id === commentId)
    if (target) {
      target.isDeleted = true
      target.content = "Bình luận này đã bị thu hồi."
    }
  })

  hubConnection.start()
    .then(() => {
      hubConnection?.invoke("JoinDocumentRoom", Number(props.fileId)) 
    })
    .catch(err => console.error("SignalR Error:", err))
}

async function fetchComments() {
  loading.value = true
  try {
    const res: any = await getComments(props.fileId)
    comments.value = res?.result || res?.data || res || []
    scrollToBottom()
  } catch (error) {
    console.error("Fetch comments error", error)
  } finally {
    loading.value = false
  }
}

async function submitComment() {
  if (!newComment.value.trim() || submitting.value) return
  submitting.value = true
  
  try {
    const payload = {
      mediaStoreId: Number(props.fileId),
      content: newComment.value.trim(),
      pageNumber: attachPage.value ? props.currentPage : null
    }
    const res: any = await addComment(payload)
    if (res && res.isSuccess === false) {
      showToast(res.message, "error")
      return
    }
    newComment.value = ''
  } catch (error) {
    showToast("Không thể gửi bình luận", "error")
  } finally {
    submitting.value = false
  }
}

function confirmDeleteClick(id: number) {
  commentIdToDelete.value = id
  showDeleteConfirm.value = true
}

async function executeDelete() {
  if (!commentIdToDelete.value) return
  try {
    await deleteComment(commentIdToDelete.value)
    showDeleteConfirm.value = false
    commentIdToDelete.value = null
    showToast("Đã thu hồi bình luận", "success")
  } catch (error) {
    showToast("Lỗi xóa bình luận", "error")
  }
}

function emitJumpToPage(page: number) {
  emit('jump-to-page', page)
}

function scrollToBottom() {
  nextTick(() => {
    const container = document.querySelector('.custom-scrollbar')
    if (container) container.scrollTop = container.scrollHeight
  })
}

onMounted(() => {
  fetchComments()
  setupSignalR()
})

onUnmounted(() => {
  if (hubConnection) {
    hubConnection.invoke("LeaveDocumentRoom", Number(props.fileId)).catch(() => {})
    hubConnection.stop()
  }
})
</script>

<style scoped>
.custom-scrollbar::-webkit-scrollbar { width: 4px; }
.custom-scrollbar::-webkit-scrollbar-track { background: transparent; }
.custom-scrollbar::-webkit-scrollbar-thumb { background: #30363d; border-radius: 10px; }
.custom-scrollbar::-webkit-scrollbar-thumb:hover { background: #484f58; }

.modal-fade-enter-active, .modal-fade-leave-active { transition: all 0.2s ease-out; }
.modal-fade-enter-from, .modal-fade-leave-to { opacity: 0; transform: scale(0.95); }

/* Animation cho comment mới */
.flex.gap-3 { animation: slideIn 0.3s ease-out; }
@keyframes slideIn {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}
</style>