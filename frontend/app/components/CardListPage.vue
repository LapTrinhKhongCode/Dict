<template>
  <div class="min-h-screen text-white p-4 sm:p-8">
    <div v-if="set" class="max-w-5xl mx-auto">
      <header class="flex flex-wrap justify-between items-center mb-6 gap-4">
        <div>
          <button @click="emit('go-home')" class="flex items-center text-sm text-sky-400 hover:text-sky-300 transition-colors mb-2">
            &larr; Quay lại trang chủ
          </button>
          <h1 class="text-3xl font-bold text-sky-400">{{ set.title }}</h1>
          <p class="text-gray-400 mt-1">{{ set.description }}</p>
        </div>

        <div class="flex flex-col sm:flex-row items-stretch sm:items-center gap-3">
          <button @click="emit('start-review', 'srs')" :disabled="dueCardCount === 0" class="learn-button bg-sky-500 hover:bg-sky-600 disabled:bg-gray-600">
            FlashCard ({{ dueCardCount }})
          </button>
          <button @click="emit('start-review', 'quiz')" class="learn-button bg-emerald-500 hover:bg-emerald-600">
            Học trắc nghiệm
          </button>

          <!-- Nút Chỉnh sửa (hiển thị nếu isOwner là true) -->
          <button
            v-if="isOwner"
            @click="emit('go-to-edit')"
            class="learn-button bg-gray-600 hover:bg-gray-500 flex items-center justify-center"
            title="Chỉnh sửa bộ thẻ"
          >
            <svg xmlns="http://www.w3.org/2000/svg" class="h-5 w-5" viewBox="0 0 20 20" fill="currentColor">
              <path d="M17.414 2.586a2 2 0 00-2.828 0L7 10.172V13h2.828l7.586-7.586a2 2 0 000-2.828z" />
              <path fill-rule="evenodd" d="M2 6a2 2 0 012-2h4a1 1 0 010 2H4v10h10v-4a1 1 0 112 0v4a2 2 0 01-2 2H4a2 2 0 01-2-2V6z" clip-rule="evenodd" />
            </svg>
          </button>
        </div>
      </header>

      <div class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
        <div
          v-for="(card, idx) in set.cards"
          :key="getCardKey(card) ?? idx"
          class="bg-gray-800 rounded-lg p-4 flex flex-col justify-between h-40 relative group" 
        >
          <div>
            <div class="text-4xl font-semibold mb-2">{{ card.charBig }}</div>
            <p class="text-gray-300 truncate">{{ card.meaning }}</p>
          </div>
          <div class="flex justify-between items-center mt-auto"> <!-- Use mt-auto to push to bottom -->
            <div class="text-xs text-sky-500">
              Lần ôn tới: {{ getCountdownString(card, idx) }}
            </div>

            <!-- Nút Reset (hiển thị nếu isOwner là true) -->
            <button
              v-if="isOwner"
              @click.stop="promptResetCard(card)"
              :disabled="resettingCardId === (getCardKey(card) ?? idx)"
              class="absolute top-2 right-2 p-1.5 rounded-full bg-gray-700/50 text-gray-400 hover:bg-sky-500 hover:text-white opacity-0 group-hover:opacity-100 transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
              title="Đặt lại tiến độ thẻ"
            >
              <svg v-if="resettingCardId === (getCardKey(card) ?? idx)" class="animate-spin h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>
              <svg v-else xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M4 4v5h5M20 20v-5h-5M4 4l1.5 1.5A9 9 0 0120.5 15" /></svg>
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Confirmation Modal for Reset -->
    <ConfirmationModal
      :is-open="isModalOpen"
      title="Xác nhận đặt lại"
      :message="modalMessage"
      confirmation-text="reset"
      @confirm="handleConfirmReset"
      @cancel="closeModal"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onUnmounted } from 'vue';
import type { DeckDetailDto, CardDto } from '~/types';
import ConfirmationModal from './ConfirmationModal.vue'; // Import modal
import { useJwt } from '~/composables/useJwt';

const { username, avatarUrl, isAuthenticated, logout, jwt } = useJwt();

// ✅ SỬA: Thêm currentUserName vào props
const props = defineProps<{
  set: DeckDetailDto | null,
  currentUserId: number, // Vẫn giữ lại IDเผื่อ dùng
  currentUserName: string // Tên người dùng đang đăng nhập
}>();

// ✅ SỬA: Thêm emit 'go-to-edit'
const emit = defineEmits<{
  (e: 'go-home'): void,
  (e: 'start-review', mode: 'srs' | 'quiz'): void, // Bỏ 'classic'
  (e: 'card-updated'): void, // Giữ lại cho reset
  (e: 'go-to-edit'): void
}>();

// ✅ SỬA: Logic isOwner so sánh tên (YÊU CẦU BACKEND CUNG CẤP authorName)
const isOwner = computed(() => {
  if (!props.set || !props.currentUserName) {
      console.warn('isOwner check: Missing set data or currentUserName');
      return false;
  }
  // --- QUAN TRỌNG ---
  // Dòng này SẼ KHÔNG HOẠT ĐỘNG ĐÚNG nếu DeckDetailDto từ backend
  // không có trường 'authorName'. Nó hiện chỉ có 'userId'.
  // Bạn cần cập nhật backend để thêm 'authorName' vào DeckDetailDto.
  // @ts-ignore // Tạm thời bỏ qua lỗi TS vì biết DTO đang thiếu trường
  const authorNameFromDto = props.set.authorName; 
  // ------------------

  // if (authorNameFromDto === undefined) {
  //     console.warn("isOwner check: DeckDetailDto is missing 'authorName' field. Falling back to ID check.");
  //     // Fallback: Check using userId if authorName is missing
  //      return props.set.userId === props.currentUserId;
  // }
  
  // So sánh tên (sau khi backend đã cung cấp)
  const isMatch = authorNameFromDto === props.currentUserName;
  console.log(`isOwner check (name): DTO Author='${authorNameFromDto}', Current User='${props.currentUserName}', Match=${isMatch}`);
  return isMatch;
});

// --- State cho modal và reset (đã thêm lại) ---
const isModalOpen = ref(false);
const cardToReset = ref<CardDto | null>(null);
const modalMessage = computed(() => {
  if (!cardToReset.value) return '';
  return `Bạn có chắc muốn đặt lại tiến độ cho thẻ "${cardToReset.value.meaning}" không?`;
});
const resettingCardId = ref<number | string | null>(null);
// ------------------------------------------------

const now = ref(new Date());
let timer: ReturnType<typeof setInterval> | null = null;
const BASE_URL = 'https://localhost:7084'; // Chỉ base URL
const assumeDbIsUTC = true; // Nên là true nếu backend dùng UTC

// --- Helper functions (giữ nguyên) ---
function getCardKey(card: any): number | null {
  if (typeof card?.id === 'number') return card.id;
  return null; // Đơn giản hóa
}
function getNextReviewRaw(card: any): string | undefined {
  return card?.nextReviewAt; // Chỉ cần kiểm tra trường chuẩn
}
function parseDbDate(dateString?: string | null): Date | null {
  if (!dateString) return null;
  const s = dateString.toString().trim();
  if (!s || s.startsWith('0001-01-01')) return null;

  // Cố gắng parse trực tiếp, sau đó thêm 'Z' nếu cần
  let dNative = new Date(s);
  if (!isNaN(dNative.getTime())) return dNative;

  // Thử thêm 'Z' nếu không có thông tin timezone
  if (!s.endsWith('Z') && !s.includes('+') && !s.includes('-')) {
      dNative = new Date(s.replace(' ', 'T') + 'Z');
      if (!isNaN(dNative.getTime())) return dNative;
  }
  
  // Logic cũ dự phòng (ít khả năng cần)
  const m = s.match(/^(\d{4})-(\d{2})-(\d{2})[ T](\d{2}):(\d{2}):(\d{2})(?:\.(\d+))?Z?$/);
  if (!m) return null;
  const [, Y, M, D, hh, mm, ss, frac = "0"] = m;
  const yearNum = Number(Y);
  if (yearNum <= 1) return null;
  const milli = Number((frac + "000").slice(0, 3));
  // Luôn giả định là UTC nếu parse theo regex thành công
  return new Date(Date.UTC(Number(Y), Number(M) - 1, Number(D), Number(hh), Number(mm), Number(ss), milli));
}
function pad(n: number) { return n.toString().padStart(2, '0'); }
function formatDuration(secondsTotal: number): string {
  const sAbs = Math.abs(secondsTotal);

  const days = Math.floor(sAbs / 86400);
  let s = sAbs % 86400; // 🟢 Khai báo biến s ở đây

  const hours = Math.floor(s / 3600);
  s = s % 3600;

  const minutes = Math.floor(s / 60);
  const seconds = Math.round(s % 60); // Làm tròn giây
  if (days >= 2) return `trong ${days} ngày`;
  if (days === 1) return `trong 1 ngày`;
  if (hours > 0) return `trong ${hours} giờ`;
  if (minutes > 0) return `trong ${minutes} phút`;
  return `trong ${seconds} giây`; // Hiển thị giây nếu < 1 phút
}

// --- Computed properties (giữ nguyên dueCardCount) ---
const dueCardCount = computed(() => { /* ... giữ nguyên logic ... */
  if (!props.set) return 0;
  const n = now.value.getTime(); // So sánh mili giây cho chính xác
  return props.set.cards.filter(card => {
    const raw = getNextReviewRaw(card);
    const dueDate = parseDbDate(raw);
    if (!dueDate) return true; 
    return dueDate.getTime() <= n;
  }).length;
});

// --- Countdown logic (giữ nguyên) ---
function getCountdownString(card: any, idx: number): string { /* ... giữ nguyên logic ... */ 
  const raw = getNextReviewRaw(card);
  const dueDate = parseDbDate(raw);
  const n = now.value;
  if (!dueDate) return 'Thẻ mới';
  const diffSeconds = Math.floor((dueDate.getTime() - n.getTime()) / 1000);
  if (diffSeconds <= 0) return 'Sẵn sàng ôn tập';
  return formatDuration(diffSeconds);
}
const setupRealtimeNow = () => { /* ... giữ nguyên logic ... */ 
  if (timer) clearInterval(timer);
  now.value = new Date();
  timer = setInterval(() => { now.value = new Date(); }, 1000);
};

// --- Watchers and Lifecycle (giữ nguyên) ---
watch(() => props.set, (newSet) => { /* ... giữ nguyên logic ... */ 
  if (newSet && newSet.cards) { setupRealtimeNow(); } 
  else { if (timer) { clearInterval(timer); timer = null; } }
}, { immediate: true, deep: true });
onUnmounted(() => { if (timer) clearInterval(timer); });

// --- API Interaction & Modal Handling (Đã thêm lại) ---
async function handleResponse(response: Response) { 
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
  if (response.status === 204) return {}; // Handle No Content
  const data = await response.json();
  if (data.isSuccess === false) {
    throw new Error(data.message || 'Lỗi từ API');
  }
  return (data.result === undefined ? data : data.result);
}

function promptResetCard(card: CardDto) {
  cardToReset.value = card;
  isModalOpen.value = true;
}

function closeModal() {
  isModalOpen.value = false;
  cardToReset.value = null;
}

async function handleConfirmReset() {
  if (!cardToReset.value || resettingCardId.value !== null) return;
  const key = getCardKey(cardToReset.value); 
  if (key === null) {
    alert("Không thể tìm thấy ID của thẻ.");
    return;
  }
  closeModal();
  resettingCardId.value = key;
  try {
    const response = await fetch(`${BASE_URL}/api/review/cards/${key}/reset`, { // Thêm /api
      method: 'POST',
           headers: { 'Authorization': `Bearer ${jwt.value}` } 
    });
    await handleResponse(response);
    emit('card-updated'); // Thông báo cho App.vue để refresh
    alert('Đặt lại tiến độ thành công!');
  } catch (err: any) {
    console.error("Lỗi khi reset thẻ:", err);
    alert(`Không thể đặt lại tiến độ thẻ: ${err.message}`);
  } finally {
    resettingCardId.value = null;
  }
}

</script>

<style>
.learn-button {
  color: white;
  font-weight: 700;
  padding: 0.5rem 1.25rem;
  border-radius: 0.5rem;
  transition: background-color 150ms cubic-bezier(0.4, 0, 0.2, 1);
}
.learn-button:disabled {
  cursor: not-allowed;
}
</style>

