<template>
  <!-- 
    THAY ĐỔI:
    - Thêm: bg-gray-50, text-gray-900 (cho chế độ sáng)
    - Thêm: dark:bg-neutral-900, dark:text-white (cho chế độ tối)
    - Xóa: text-white (đã chuyển vào dark:)
    - Thêm: transition-colors
  -->
  <div
    class="min-h-screen bg-gray-50 text-gray-900 dark:bg-neutral-900 dark:text-white p-4 sm:p-8 transition-colors"
  >
    <div v-if="set" class="max-w-5xl mx-auto">
      <header
        class="flex flex-wrap justify-between items-center mb-6 gap-4"
      >
        <div>
          <!-- 
            THAY ĐỔI:
            - Thêm: text-primary-600, hover:text-primary-500 (sáng)
            - Thêm: dark:text-sky-400, dark:hover:text-sky-300 (tối)
          -->
          <button
            @click="emit('go-home')"
            class="flex items-center text-sm text-primary-600 hover:text-primary-500 dark:text-sky-400 dark:hover:text-sky-300 transition-colors mb-2"
          >
            &larr; Quay lại trang chủ
          </button>
          <!-- 
            THAY ĐỔI:
            - Thêm: text-primary-600 (sáng)
            - Thêm: dark:text-sky-400 (tối)
          -->
          <h1 class="text-3xl font-bold text-primary-600 dark:text-sky-400">
            {{ set.title }}
          </h1>
          <!-- 
            THAY ĐỔI:
            - Thêm: text-gray-600 (sáng)
            - Thêm: dark:text-gray-400 (tối)
          -->
          <p class="text-gray-600 dark:text-gray-400 mt-1">
            {{ set.description }}
          </p>
        </div>

        <div
          class="flex flex-col sm:flex-row items-stretch sm:items-center gap-3"
        >
          <!-- 
            THAY ĐỔI:
            - Thêm: text-white (để hoạt động với .learn-button)
            - Thêm: disabled:bg-gray-300, disabled:text-gray-500 (sáng)
            - Thêm: dark:disabled:bg-gray-600, dark:disabled:text-gray-400 (tối)
          -->
          <button
            v-if="isOwner"
            @click="emit('start-review', 'srs')"
            :disabled="dueCardCount === 0"
            class="learn-button bg-sky-500 hover:bg-sky-600 text-white disabled:bg-gray-300 disabled:text-gray-500 dark:disabled:bg-gray-600 dark:disabled:text-gray-400"
          >
            FlashCard ({{ dueCardCount }})
          </button>
          <button
            v-if="isOwner"
            @click="emit('start-review', 'quiz')"
            class="learn-button bg-emerald-500 hover:bg-emerald-600 text-white"
          >
            Học trắc nghiệm
          </button>

          <!-- 
            THAY ĐỔI:
            - Thêm: bg-gray-500, hover:bg-gray-600, text-white (sáng)
            - Thêm: dark:bg-gray-600, dark:hover:bg-gray-500 (tối)
          -->
          <button
            v-if="isOwner"
            @click="emit('go-to-edit')"
            class="learn-button bg-gray-500 hover:bg-gray-600 text-white dark:bg-gray-600 dark:hover:bg-gray-500 flex items-center justify-center"
            title="Chỉnh sửa bộ thẻ"
          >
            <svg
              xmlns="http://www.w3.org/2000/svg"
              class="h-5 w-5"
              viewBox="0 0 20 20"
              fill="currentColor"
            >
              <path
                d="M17.414 2.586a2 2 0 00-2.828 0L7 10.172V13h2.828l7.586-7.586a2 2 0 000-2.828z"
              />
              <path
                fill-rule="evenodd"
                d="M2 6a2 2 0 012-2h4a1 1 0 010 2H4v10h10v-4a1 1 0 112 0v4a2 2 0 01-2 2H4a2 2 0 01-2-2V6z"
                clip-rule="evenodd"
              />
            </svg>
          </button>
        </div>
      </header>

      <div
        class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4"
      >
        <!-- 
          THAY ĐỔI (Card):
          - Thêm: bg-white, border, border-gray-200 (sáng)
          - Thêm: dark:bg-neutral-800, dark:border-neutral-700 (tối)
        -->
        <div
          v-for="(card, idx) in set.cards"
          :key="getCardKey(card) ?? idx"
          class="bg-white dark:bg-neutral-800 border border-gray-200 dark:border-neutral-700 rounded-lg p-4 flex flex-col justify-between h-40 relative group"
        >
          <div>
            <!-- (Chữ này sẽ tự đổi màu theo nền) -->
            <div class="text-4xl font-semibold mb-2">{{ card.charBig }}</div>
            <!-- 
              THAY ĐỔI:
              - Thêm: text-gray-600 (sáng)
              - Thêm: dark:text-gray-300 (tối)
            -->
            <p class="text-gray-600 dark:text-gray-300 truncate">
              {{ card.meaning }}
            </p>
          </div>
          <div class="flex justify-between items-center mt-auto">
            <!-- 
              THAY ĐỔI:
              - Thêm: text-primary-600 (sáng)
              - Thêm: dark:text-sky-500 (tối)
            -->
            <div
              v-if="set.authorName == username"
              class="text-xs text-primary-600 dark:text-sky-500"
            >
              Lần ôn tới: {{ getCountdownString(card, idx) }}
            </div>

            <!-- 
              THAY ĐỔI (Reset Button):
              - Thêm: bg-gray-200/50, text-gray-500, hover:bg-primary-500 (sáng)
              - Thêm: dark:bg-gray-700/50, dark:text-gray-400, dark:hover:bg-sky-500 (tối)
            -->
            <button
              v-if="isOwner"
              @click.stop="promptResetCard(card)"
              :disabled="resettingCardId === (getCardKey(card) ?? idx)"
              class="absolute top-2 right-2 p-1.5 rounded-full bg-gray-200/50 text-gray-500 hover:bg-primary-500 hover:text-white dark:bg-gray-700/50 dark:text-gray-400 dark:hover:bg-sky-500 dark:hover:text-white opacity-0 group-hover:opacity-100 transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed"
              title="Đặt lại tiến độ thẻ"
            >
              <svg
                v-if="resettingCardId === (getCardKey(card) ?? idx)"
                class="animate-spin h-4 w-4"
                xmlns="http://www.w3.org/2000/svg"
                fill="none"
                viewBox="0 0 24 24"
              >
                <circle
                  class="opacity-25"
                  cx="12"
                  cy="12"
                  r="10"
                  stroke="currentColor"
                  stroke-width="4"
                ></circle>
                <path
                  class="opacity-75"
                  fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"
                ></path>
              </svg>
              <svg
                v-else
                xmlns="http://www.w3.org/2000/svg"
                class="h-4 w-4"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
                stroke-width="2"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  d="M4 4v5h5M20 20v-5h-5M4 4l1.5 1.5A9 9 0 0120.5 15"
                />
              </svg>
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- 
      (Không thay đổi) 
      Giả định rằng ConfirmationModal.vue đã được thiết kế 
      để hoạt động với cả hai chế độ.
    -->
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
import { ref, computed, watch, onUnmounted } from 'vue'
import type { DeckDetailDto, CardDto } from '~/types'
import ConfirmationModal from './ConfirmationModal.vue'
import { useJwt } from '~/composables/useJwt'

const { username, avatarUrl, isAuthenticated, logout, jwt } = useJwt()

const props = defineProps<{
  set: DeckDetailDto | null,
  currentUserId: number,
  currentUserName: string
}>()

const emit = defineEmits<{
  (e: 'go-home'): void,
  (e: 'start-review', mode: 'srs' | 'quiz'): void,
  (e: 'card-updated'): void,
  (e: 'go-to-edit'): void
}>()

const isOwner = computed(() => {
  if (!props.set || !props.currentUserName) return false
  // @ts-ignore
  const authorNameFromDto = props.set.authorName
  if (authorNameFromDto === undefined) return props.set.userId === props.currentUserId
  return authorNameFromDto === props.currentUserName
})

const isModalOpen = ref(false)
const cardToReset = ref<CardDto | null>(null)
const modalMessage = computed(() => {
  if (!cardToReset.value) return ''
  return `Bạn có chắc muốn đặt lại tiến độ cho thẻ "${cardToReset.value.meaning}" không?`
})
const resettingCardId = ref<number | string | null>(null)

const now = ref(new Date())
let timer: ReturnType<typeof setInterval> | null = null
const config = useRuntimeConfig()
const baseUrl = config.public.apiBaseUrl

function getCardKey(card: any): number | null {
  if (typeof card?.id === 'number') return card.id
  return null
}
function getNextReviewRaw(card: any): string | undefined {
  return card?.nextReviewAt
}
function parseDbDate(dateString?: string | null): Date | null {
  if (!dateString) return null
  const s = dateString.toString().trim()
  if (!s || s.startsWith('0001-01-01')) return null

  let d = new Date(s)
  if (!isNaN(d.getTime())) return d

  let s2 = s.replace(' ', 'T')
  if (!/[zZ]|[+\-]\d{2}(:?\d{2})?$/.test(s2)) s2 = s2 + 'Z'
  d = new Date(s2)
  if (!isNaN(d.getTime())) return d

  const m = s.match(/^(\d{4})-(\d{2})-(\d{2})[ T](\d{2}):(\d{2}):(\d{2})(?:\.(\d+))?Z?$/)
  if (!m) return null
  const [, Y, M, D, hh, mm, ss, frac = '0'] = m
  const yearNum = Number(Y)
  if (yearNum <= 1) return null
  const milli = Number((frac + '000').slice(0, 3))
  return new Date(Date.UTC(Number(Y), Number(M) - 1, Number(D), Number(hh), Number(mm), Number(ss), milli))
}
function pad(n: number) { return n.toString().padStart(2, '0') }
function formatDuration(secondsTotal: number): string {
  const sAbs = Math.abs(secondsTotal)
  const days = Math.floor(sAbs / 86400)
  let s = sAbs % 86400
  const hours = Math.floor(s / 3600)
  s = s % 3600
  const minutes = Math.floor(s / 60)
  const seconds = Math.round(s % 60)

  if (days >= 2) return `trong ${days} ngày`
  if (days === 1) return `trong 1 ngày`
  if (hours > 0) return `trong ${hours} giờ ${minutes} phút`
  if (minutes > 0) return `trong ${minutes} phút ${seconds} giây`
  return `trong ${seconds} giây`
}

const dueCardCount = computed(() => {
  if (!props.set) return 0
  const n = now.value.getTime()
  return props.set.cards.filter(card => {
    const raw = getNextReviewRaw(card)
    const dueDate = parseDbDate(raw)
    if (!dueDate) return true
    return dueDate.getTime() <= n
  }).length
})

function getCountdownString(card: any, idx: number): string {
  const raw = getNextReviewRaw(card)
  const dueDate = parseDbDate(raw)
  const n = now.value
  if (!dueDate) return 'Thẻ mới'
  const diffSeconds = Math.floor((dueDate.getTime() - n.getTime()) / 1000)
  if (diffSeconds <= 0) return 'Sẵn sàng ôn tập'
  return formatDuration(diffSeconds)
}

const setupRealtimeNow = () => {
  if (timer) clearInterval(timer)
  now.value = new Date()
  timer = setInterval(() => { now.value = new Date() }, 1000)
}

watch(() => props.set, (newSet) => {
  if (newSet && newSet.cards) { setupRealtimeNow() }
  else { if (timer) { clearInterval(timer); timer = null } }
}, { immediate: true, deep: true })

onUnmounted(() => { if (timer) clearInterval(timer) })

async function handleResponse(response: Response) {
  if (!response.ok) {
    const errorText = await response.text()
    let errorMessage = errorText
    try {
      const errorJson = JSON.parse(errorText)
      if (errorJson && errorJson.message) errorMessage = errorJson.message
      else if (errorJson && errorJson.title) errorMessage = errorJson.title
    } catch (e) { }
    throw new Error(errorMessage || `Yêu cầu thất bại: ${response.status}`)
  }
  if (response.status === 204) return {}
  const data = await response.json()
  if (data.isSuccess === false) {
    throw new Error(data.message || 'Lỗi từ API')
  }
  return (data.result === undefined ? data : data.result)
}

function promptResetCard(card: CardDto) {
  cardToReset.value = card
  isModalOpen.value = true
}

function closeModal() {
  isModalOpen.value = false
  cardToReset.value = null
}

async function handleConfirmReset() {
  if (!cardToReset.value || resettingCardId.value !== null) return
  const key = getCardKey(cardToReset.value)
  if (key === null) {
    // THAY ĐỔI: Bỏ 'alert' và dùng 'console.error'
    console.error("Không thể tìm thấy ID của thẻ.")
    return
  }
  closeModal()
  resettingCardId.value = key
  try {
    const response = await fetch(`${baseUrl}/api/review/cards/${key}/reset`, {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${jwt.value}` }
    })
    await handleResponse(response)
    emit('card-updated')
    // THAY ĐỔI: Bỏ 'alert', bạn nên dùng (useToast) ở component cha
    console.log('Đặt lại tiến độ thành công!')
  } catch (err: any) {
    console.error("Lỗi khi reset thẻ:", err)
    // THAY ĐỔI: Bỏ 'alert'
    console.error(`Lỗi khi đặt lại thẻ: ${err?.message ?? err}`)
  } finally {
    resettingCardId.value = null
  }
}
</script>

<style>
.learn-button {
  /* THAY ĐỔI:
    - Xóa 'color: white;'
    - Thêm 'text-white' vào các class của button trong template.
  */
  font-weight: 700;
  padding: 0.5rem 1.25rem;
  border-radius: 0.5rem;
  transition: background-color 150ms cubic-bezier(0.4, 0, 0.2, 1);
}
.learn-button:disabled {
  cursor: not-allowed;
}
</style>