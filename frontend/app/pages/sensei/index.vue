<template>
  <div class="relative h-full font-sans bg-gray-50 text-gray-900 dark:bg-gray-900 dark:text-gray-100 transition-colors overflow-hidden flex flex-col">
    <div class="absolute inset-0 z-0 pointer-events-none opacity-50 dark:opacity-20"
         style="background-image: linear-gradient(rgba(150,150,150,0.1) 1px, transparent 1px), linear-gradient(90deg, rgba(150,150,150,0.1) 1px, transparent 1px); background-size: 40px 40px;">
    </div>
    <div class="absolute -top-24 -right-24 w-96 h-96 z-0 pointer-events-none rounded-full"
         style="background: radial-gradient(circle, rgba(201,162,39,0.08) 0%, transparent 70%);">
    </div>

    <div class="relative z-10 flex h-full flex-1 overflow-hidden">
      <aside class="w-[260px] shrink-0 bg-gray-100 dark:bg-gray-800/80 border-r border-gray-200 dark:border-gray-700 flex flex-col overflow-y-auto custom-scrollbar transition-colors">
        <div class="p-5 border-b border-gray-200 dark:border-gray-700">
          <div class="flex flex-col">
            <span class="font-serif text-[28px] font-bold text-yellow-600 dark:text-yellow-400 leading-none">先生</span>
            <span class="text-[11px] tracking-widest uppercase text-gray-500 dark:text-gray-400 mt-0.5">AI Sensei</span>
          </div>
        </div>

        <div class="p-3 flex flex-col gap-1.5 border-b border-gray-200 dark:border-gray-700">
          <button :class="[
              'flex items-center gap-2.5 p-2.5 rounded-xl border text-left transition-all w-full',
              mode === 'tutor' 
                ? 'bg-yellow-100/50 dark:bg-yellow-900/20 border-yellow-300/50 dark:border-yellow-700/50 text-gray-900 dark:text-white shadow-sm' 
                : 'border-transparent bg-transparent text-gray-500 dark:text-gray-400 hover:bg-gray-200/50 dark:hover:bg-gray-700 hover:text-gray-900 dark:hover:text-gray-100'
            ]"
            @click="switchMode('tutor')">
            <span class="text-xl shrink-0">📖</span>
            <div>
              <p class="text-[13px] font-semibold leading-none">Tutor</p>
              <p class="text-[11px] text-gray-500 dark:text-gray-400 mt-1">Hỏi đáp tự do</p>
            </div>
          </button>
          <button :class="[
              'flex items-center gap-2.5 p-2.5 rounded-xl border text-left transition-all w-full',
              mode === 'practice' 
                ? 'bg-blue-100/50 dark:bg-blue-900/20 border-blue-300/50 dark:border-blue-700/50 text-gray-900 dark:text-white shadow-sm' 
                : 'border-transparent bg-transparent text-gray-500 dark:text-gray-400 hover:bg-gray-200/50 dark:hover:bg-gray-700 hover:text-gray-900 dark:hover:text-gray-100'
            ]"
            @click="switchMode('practice')">
            <span class="text-xl shrink-0">💬</span>
            <div>
              <p class="text-[13px] font-semibold leading-none">Practice</p>
              <p class="text-[11px] text-gray-500 dark:text-gray-400 mt-1">Hội thoại roleplay</p>
            </div>
          </button>
        </div>

        <div v-if="mode === 'practice'" class="p-3 border-b border-gray-200 dark:border-gray-700">
          <p class="text-[10px] font-bold tracking-widest uppercase text-gray-500 dark:text-gray-400 mb-2">Chọn tình huống</p>
          <div class="flex flex-col gap-1">
            <button v-for="s in scenarios" :key="s.id"
              :class="[
                'flex items-center gap-2 px-2.5 py-2 rounded-lg border text-left transition-all w-full',
                selectedScenario?.id === s.id 
                  ? 'bg-blue-50 dark:bg-blue-900/30 border-blue-200 dark:border-blue-700/50 text-blue-700 dark:text-blue-400 font-medium' 
                  : 'border-transparent bg-transparent text-gray-600 dark:text-gray-400 hover:bg-gray-200/50 dark:hover:bg-gray-700 hover:text-gray-900 dark:hover:text-gray-100'
              ]"
              @click="selectScenario(s)">
              <span class="text-base">{{ s.icon }}</span>
              <span class="text-[13px]">{{ s.name }}</span>
            </button>
          </div>
        </div>

        <div class="p-3 border-b border-gray-200 dark:border-gray-700">
          <p class="text-[10px] font-bold tracking-widest uppercase text-gray-500 dark:text-gray-400 mb-2">Ngữ cảnh của bạn</p>

          <div class="flex flex-col gap-1.5 mb-2.5" v-if="contextLoaded">
            <div class="flex items-center gap-2 text-xs text-gray-600 dark:text-gray-400">
              <span class="text-sm">📚</span>
              <span>{{ totalVocabCount }} từ vựng đã lưu</span>
            </div>
            <div class="flex items-center gap-2 text-xs text-gray-600 dark:text-gray-400">
              <span class="text-sm">🃏</span>
              <span>{{ totalCardCount }} flashcard</span>
            </div>
            <div class="flex items-center gap-2 text-xs text-gray-600 dark:text-gray-400" v-if="detectedLevel">
              <span class="text-sm">🎯</span>
              <span>AI phát hiện: ~{{ detectedLevel }}</span>
            </div>
          </div>
          <div v-else class="flex items-center gap-2 text-xs text-gray-500 dark:text-gray-400">
            <div class="mini-dots"><span></span><span></span><span></span></div>
            <span>Đang tải ngữ cảnh...</span>
          </div>

          <div v-if="sampleVocabs.length" class="flex flex-wrap gap-1 mt-2">
            <span v-for="v in sampleVocabs" :key="v" 
              class="font-serif text-[11px] px-2 py-0.5 bg-gray-200 dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-full text-gray-600 dark:text-gray-300">
              {{ v }}
            </span>
          </div>
        </div>

        <div class="p-3">
          <p class="text-[10px] font-bold tracking-widest uppercase text-gray-500 dark:text-gray-400 mb-2">Gợi ý</p>
          <button v-for="q in currentQuickPrompts" :key="q"
            class="block w-full text-left px-2.5 py-1.5 bg-transparent border border-gray-200 dark:border-gray-700 rounded-lg text-gray-600 dark:text-gray-400 text-xs mb-1.5 hover:border-blue-400 dark:hover:border-blue-500 hover:text-gray-900 dark:hover:text-gray-200 transition-all"
            @click="sendQuick(q)">
            {{ q }}
          </button>
        </div>
      </aside>

      <main class="flex-1 flex flex-col bg-white dark:bg-[#111827] overflow-hidden transition-colors">
        <div class="flex items-center justify-between px-5 py-3 bg-white dark:bg-[#1f2937] border-b border-gray-200 dark:border-gray-700 shrink-0 transition-colors">
          <div class="flex items-center gap-2 text-[13px] text-gray-600 dark:text-gray-300 font-medium">
            <div :class="['w-2 h-2 rounded-full', apiKey ? 'bg-green-500 shadow-[0_0_6px_#22c55e]' : 'bg-red-500']"></div>
            <span>{{ mode === 'tutor' ? 'Tutor Mode' : `Practice: ${selectedScenario?.name || 'Chọn tình huống'}` }}</span>
          </div>
          <div class="flex gap-1.5">
            <button @click="clearChat" title="Xóa chat"
              class="w-8 h-8 rounded-lg bg-transparent border border-gray-200 dark:border-gray-600 text-gray-500 dark:text-gray-400 flex items-center justify-center hover:border-gray-400 dark:hover:border-gray-400 hover:text-gray-900 dark:hover:text-white transition-all">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" class="w-4 h-4">
                <polyline points="1 4 1 10 7 10"/><polyline points="23 20 23 14 17 14"/>
                <path d="M20.49 9A9 9 0 0 0 5.64 5.64L1 10m22 4l-4.64 4.36A9 9 0 0 1 3.51 15"/>
              </svg>
            </button>
          </div>
        </div>

        <div class="flex-1 overflow-y-auto p-6 flex flex-col gap-6 custom-scrollbar" ref="messagesEl">
          <div v-if="messages.length === 0" class="flex-1 flex flex-col items-center justify-center text-center gap-3 p-10">
            <div class="font-serif text-6xl font-bold text-yellow-500/40 dark:text-yellow-400/30 leading-none">
              {{ mode === 'tutor' ? '質問' : '会話' }}
            </div>
            <h2 class="text-xl font-semibold text-gray-900 dark:text-white mt-2">
              {{ mode === 'tutor' ? 'Hỏi bất cứ điều gì về tiếng Nhật' : 'Luyện hội thoại với AI' }}
            </h2>
            <p class="text-sm text-gray-500 dark:text-gray-400 max-w-[360px] leading-relaxed">
              {{ mode === 'tutor'
                ? 'AI sẽ giải thích ngữ pháp, từ vựng dựa trên trình độ và flashcard của bạn'
                : 'Chọn tình huống ở menu bên trái rồi bắt đầu luyện tập' }}
            </p>
            <div v-if="mode === 'practice' && selectedScenario" 
              class="flex items-center gap-3.5 bg-gray-50 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl px-5 py-4 mt-3">
              <span class="text-3xl">{{ selectedScenario.icon }}</span>
              <div class="text-left">
                <p class="text-[15px] font-semibold text-gray-900 dark:text-white">{{ selectedScenario.name }}</p>
                <p class="text-[13px] text-gray-500 dark:text-gray-400">{{ selectedScenario.description }}</p>
              </div>
              <button @click="startPractice"
                class="ml-4 bg-yellow-500 hover:bg-yellow-600 text-gray-900 border-none rounded-xl px-5 py-2.5 text-sm font-bold transition-all hover:-translate-y-0.5">
                Bắt đầu →
              </button>
            </div>
          </div>

          <div v-for="(msg, i) in messages" :key="i"
            :class="['flex gap-3 items-start msg-enter', msg.role === 'user' ? 'flex-row-reverse' : '']">
            <div :class="[
              'w-8 h-8 rounded-xl flex items-center justify-center text-[13px] font-bold shrink-0',
              msg.role === 'user' 
                ? 'bg-yellow-500 text-gray-900' 
                : 'bg-gray-100 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 font-serif text-yellow-600 dark:text-yellow-400'
            ]">
              {{ msg.role === 'user' ? (username?.[0]?.toUpperCase() || 'U') : '先' }}
            </div>

            <div :class="['max-w-[78%] flex flex-col gap-2', msg.role === 'user' ? 'items-end' : '']">
              
              <div v-if="!msg.analysisData" 
                :class="[
                  'rounded-2xl px-4 py-3 text-[14px] leading-[1.7]',
                  msg.role === 'user' 
                    ? 'bg-yellow-500 text-gray-900' 
                    : 'bg-gray-50 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 text-gray-900 dark:text-gray-100'
                ]" 
                v-html="formatText(msg.content)">
              </div>

              <div v-if="msg.analysisData" class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl p-4 flex flex-col gap-3 shadow-sm">
                <div class="font-serif text-lg font-bold text-gray-900 dark:text-white px-3 py-2 border-l-[3px] border-yellow-500 leading-relaxed bg-gray-50 dark:bg-gray-900/50 rounded-r-lg">
                  {{ msg.analysisData.sentence }}
                </div>
                
                <div v-if="msg.analysisData.translation" class="text-sm text-gray-800 dark:text-gray-200 bg-blue-50 dark:bg-blue-900/30 rounded-xl px-3.5 py-2.5 border border-blue-100 dark:border-blue-800/50">
                  🇻🇳 {{ msg.analysisData.translation }}
                </div>
                
                <div v-if="msg.analysisData.explanation" class="text-[13px] text-gray-600 dark:text-gray-300 leading-relaxed">
                  {{ msg.analysisData.explanation }}
                </div>

                <div v-if="msg.analysisData.grammar?.length" class="flex flex-col gap-1.5 mt-1">
                  <p class="text-[11px] font-bold uppercase tracking-widest text-gray-500 dark:text-gray-400 mb-1">📐 Ngữ pháp</p>
                  <div v-for="g in msg.analysisData.grammar" :key="g.pattern" class="bg-gray-50 dark:bg-gray-700/50 border border-gray-200 dark:border-gray-600 rounded-xl p-3">
                    <div class="flex items-center gap-2 mb-1.5">
                      <span class="text-[15px] font-bold text-yellow-600 dark:text-yellow-400">{{ g.pattern }}</span>
                      <span class="text-[10px] font-bold px-2 py-0.5 rounded-full" :style="jlptStyle(g.jlpt)">{{ g.jlpt }}</span>
                    </div>
                    <p class="text-[13px] text-gray-800 dark:text-gray-200">{{ g.meaning }}</p>
                    <p v-if="g.example" class="text-[12px] text-gray-500 dark:text-gray-400 italic mt-1.5">例: {{ g.example }}</p>
                  </div>
                </div>

                <div v-if="msg.analysisData.vocabulary?.length" class="flex flex-col gap-1.5 mt-1">
                  <p class="text-[11px] font-bold uppercase tracking-widest text-gray-500 dark:text-gray-400 mb-1">📚 Từ vựng</p>
                  <div v-for="v in msg.analysisData.vocabulary" :key="v.word" 
                    class="flex items-center gap-2 flex-wrap bg-gray-50 dark:bg-gray-700/50 border border-gray-200 dark:border-gray-600 rounded-lg px-3 py-2">
                    <span class="font-serif text-[15px] font-bold text-gray-900 dark:text-white">{{ v.word }}</span>
                    <span v-if="v.reading" class="text-[12px] text-gray-500 dark:text-gray-400">【{{ v.reading }}】</span>
                    <span class="text-[13px] text-gray-800 dark:text-gray-200 flex-1">{{ v.meaning }}</span>
                    <span class="text-[10px] font-bold px-2 py-0.5 rounded-full" :style="jlptStyle(v.jlpt)">{{ v.jlpt }}</span>
                  </div>
                </div>

                <div v-if="msg.analysisData.correction" class="bg-red-50 dark:bg-red-900/20 border border-red-200 dark:border-red-800/50 rounded-xl p-3 mt-1">
                  <p class="text-[12px] font-bold text-red-600 dark:text-red-400 mb-1">✏️ Góp ý</p>
                  <p class="text-[13px] text-gray-800 dark:text-gray-200">{{ msg.analysisData.correction }}</p>
                </div>
              </div>

              <div v-if="msg.jpResponse" class="bg-blue-50 dark:bg-blue-900/20 border border-blue-100 dark:border-blue-800/50 rounded-xl p-3 mt-1">
                <!-- <span class="block text-[10px] font-bold uppercase tracking-widest text-blue-600 dark:text-blue-400 mb-1.5">AI trả lời:</span> -->
                <span class="font-serif text-[16px] text-gray-900 dark:text-white leading-relaxed">{{ msg.jpResponse }}</span>
              </div>

            </div>
          </div>

          <div v-if="loading" class="flex gap-3 items-start msg-enter">
            <div class="w-8 h-8 rounded-xl bg-gray-100 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 flex items-center justify-center font-serif text-[13px] font-bold text-yellow-600 dark:text-yellow-400 shrink-0">
              先
            </div>
            <div class="bg-gray-100 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl px-4 py-3">
              <div class="mini-dots"><span></span><span></span><span></span></div>
            </div>
          </div>
        </div>

        <div class="px-5 py-4 bg-white dark:bg-[#1f2937] border-t border-gray-200 dark:border-gray-700 shrink-0 transition-colors">
          <div v-if="mode === 'practice' && !practiceStarted && !selectedScenario"
            class="text-center text-[13px] text-gray-500 dark:text-gray-400 py-2">
            ← Chọn tình huống ở menu bên trái để bắt đầu luyện tập
          </div>
          <div v-else class="flex gap-2.5 items-end">
            <textarea
              v-model="userInput"
              class="flex-1 bg-gray-50 dark:bg-gray-800 border border-gray-300 dark:border-gray-600 text-gray-900 dark:text-white rounded-xl px-4 py-3 text-[14px] resize-none max-h-[140px] overflow-y-auto outline-none focus:border-blue-500 dark:focus:border-blue-400 focus:ring-2 focus:ring-blue-500/20 dark:focus:ring-blue-400/20 transition-all placeholder-gray-400 dark:placeholder-gray-500 custom-scrollbar leading-[1.5]"
              :placeholder="inputPlaceholder"
              rows="1"
              @keydown.enter.exact.prevent="send"
              @input="autoResize"
              ref="inputEl"
            ></textarea>
            <button @click="send" :disabled="!userInput.trim() || loading || !apiKey"
              class="w-11 h-11 shrink-0 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-300 dark:disabled:bg-gray-700 disabled:text-gray-500 dark:disabled:text-gray-500 text-white rounded-xl flex items-center justify-center transition-all disabled:opacity-70 disabled:cursor-not-allowed hover:-translate-y-0.5">
              <svg viewBox="0 0 24 24" fill="currentColor" class="w-5 h-5">
                <path d="M2.01 21L23 12 2.01 3 2 10l15 2-15 2z"/>
              </svg>
            </button>
          </div>
          <p v-if="!apiKey" class="text-center text-[12px] text-yellow-600 dark:text-yellow-500 mt-2">⚠ Chưa có Gemini API Key. Hãy cấu hình trong .env</p>
        </div>
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, nextTick } from 'vue'
import { useRuntimeConfig } from '#app'
import { useJwt } from '~/composables/useJwt'

definePageMeta({ layout: 'default', middleware: 'auth-client' })
const config = useRuntimeConfig()
const { jwt, username } = useJwt()
const apiKey = config.public.geminiApiKey || ''

// ── State ─────────────────────────────────────────────────────────
const mode = ref<'tutor' | 'practice'>('tutor')
const messages = ref<any[]>([])
const userInput = ref('')
const loading = ref(false)
const messagesEl = ref<HTMLElement | null>(null)
const inputEl = ref<HTMLTextAreaElement | null>(null)
const practiceStarted = ref(false)
const detectedLevel = ref('')

// ── User context ──────────────────────────────────────────────────
const contextLoaded = ref(false)
const totalVocabCount = ref(0)
const totalCardCount = ref(0)
const sampleVocabs = ref<string[]>([])
const userVocabContext = ref('')

async function loadUserContext() {
  if (!jwt.value) return
  const headers = { Authorization: `Bearer ${jwt.value}` }
  const base = config.public.apiBaseUrl

  try {
    const decksRes = await fetch(`${base}/api/decks/my-decks`, { headers })
    if (decksRes.ok) {
      const data = await decksRes.json()
      const decks = data.result || data
      totalCardCount.value = decks.reduce((s: number, d: any) => s + (d.cardCount || 0), 0)

      if (decks.length > 0) {
        const detailRes = await fetch(`${base}/api/decks/${decks[0].id}`, { headers })
        if (detailRes.ok) {
          const detail = await detailRes.json()
          const cards = detail.result?.cards || detail.cards || []
          const samples = cards.slice(0, 8).map((c: any) => c.charBig || c.frontText || '')
          sampleVocabs.value = samples.filter(Boolean)
          userVocabContext.value = `Flashcard của user (${totalCardCount.value} thẻ). Mẫu: ${samples.slice(0, 10).join(', ')}`
        }
      }
    }
  } catch (e) {}

  contextLoaded.value = true
}

// ── Scenarios ─────────────────────────────────────────────────────
const scenarios = [
  { id: 'shopping', icon: '🛒', name: 'Mua sắm', description: 'Tại cửa hàng, siêu thị Nhật', prompt: 'Đóng vai nhân viên cửa hàng Nhật Bản. Bắt đầu bằng câu chào và hỏi user muốn gì.' },
  { id: 'restaurant', icon: '🍜', name: 'Nhà hàng', description: 'Gọi món, hỏi về thực đơn', prompt: 'Đóng vai phục vụ nhà hàng Nhật. Chào khách và đưa thực đơn.' },
  { id: 'interview', icon: '💼', name: 'Phỏng vấn', description: 'Xin việc làm bằng tiếng Nhật', prompt: 'Đóng vai nhà tuyển dụng Nhật Bản đang phỏng vấn xin việc.' },
  { id: 'station', icon: '🚃', name: 'Ga tàu', description: 'Hỏi đường, mua vé tàu', prompt: 'Đóng vai nhân viên ga tàu Nhật. User cần hỏi về tàu điện.' },
  { id: 'doctor', icon: '🏥', name: 'Bệnh viện', description: 'Khám bệnh, mô tả triệu chứng', prompt: 'Đóng vai bác sĩ Nhật Bản. Chào và hỏi user hôm nay có vấn đề gì.' },
  { id: 'friends', icon: '👋', name: 'Gặp bạn bè', description: 'Hội thoại thông thường', prompt: 'Đóng vai người bạn Nhật trẻ tuổi. Nói chuyện thân mật, dùng thể thông thường.' },
]
const selectedScenario = ref<any>(null)

function selectScenario(s: any) {
  selectedScenario.value = s
  practiceStarted.value = false
  messages.value = []
}

async function startPractice() {
  if (!selectedScenario.value) return
  practiceStarted.value = true
  await sendToGemini('__start__', true)
}

// ── Quick prompts ─────────────────────────────────────────────────
const tutorPrompts = [
  'Giải thích て形 là gì?',
  'Sự khác biệt は và が?',
  'Khi nào dùng でしょう?',
  'Giải thích ～ている vs ～てある',
]
const scenarioPrompts: Record<string, string[]> = {
  shopping: ['これはいくらですか？', 'これをください', 'サイズはありますか？', '試着してもいいですか？'],
  restaurant: ['メニューをください', 'これをひとつください', 'お会計をお願いします', 'おすすめは何ですか？'],
  interview: ['よろしくお願いします', '私は〜が得意です', '御社を志望した理由は〜', '質問があります'],
  station: ['〜駅まで何番線ですか？', '切符を一枚ください', '乗り換えはどこですか？', '終電は何時ですか？'],
  doctor: ['頭が痛いです', 'お腹が痛いです', 'いつからですか？', 'どんな薬がありますか？'],
  friends: ['最近どう？', '週末何してた？', '一緒に行こう！', 'それはすごいね！'],
}

const currentQuickPrompts = computed(() => {
  if (mode.value === 'tutor') return tutorPrompts
  if (selectedScenario.value && scenarioPrompts[selectedScenario.value.id]) {
    return scenarioPrompts[selectedScenario.value.id]
  }
  return ['Mua sắm', 'Nhà hàng', 'Ga tàu', 'Gặp bạn bè']
})

function switchMode(m: 'tutor' | 'practice') {
  mode.value = m
  messages.value = []
  practiceStarted.value = false
  selectedScenario.value = null
}

// ── JLPT badge style ──────────────────────────────────────────────
function jlptStyle(level: string) {
  const map: Record<string, string> = {
    N5: '#4ade80', N4: '#60a5fa', N3: '#f59e0b', N2: '#f87171', N1: '#c084fc'
  }
  const color = map[level] || '#9ca3af'
  return `background:${color}22;color:${color};border:1px solid ${color}44`
}

// ── Build system prompt ───────────────────────────────────────────
function buildSystemPrompt(isPractice = false) {
  const vocabCtx = userVocabContext.value ? `\n\nNgữ cảnh người học: ${userVocabContext.value}` : ''

  if (isPractice && selectedScenario.value) {
    return `Bạn là AI luyện hội thoại tiếng Nhật. ${selectedScenario.value.prompt}
Quy tắc QUAN TRỌNG:
- Luôn trả lời bằng JSON với format:
{
  "type": "practice_response",
  "jp_response": "câu trả lời tiếng Nhật tự nhiên",
  "translation": "dịch nghĩa tiếng Việt",
  "correction": "góp ý nếu user mắc lỗi ngữ pháp (để trống nếu đúng)",
  "vocabulary": [{"word":"từ mới","reading":"cách đọc","meaning":"nghĩa","jlpt":"N?"}],
  "next_hint": "gợi ý câu tiếp theo bằng tiếng Việt"
}
- Phản hồi tự nhiên như người Nhật thật
- Tự động detect JLPT level từ input và điều chỉnh độ khó${vocabCtx}`
  }

  return `Bạn là AI Sensei — gia sư tiếng Nhật thông minh.
Khi phân tích câu/ngữ pháp tiếng Nhật, trả về JSON:
{
  "type": "japanese_analysis",
  "sentence": "câu được phân tích",
  "translation": "dịch tiếng Việt tự nhiên",
  "explanation": "giải thích tổng quan",
  "grammar": [{"pattern":"mẫu ngữ pháp","jlpt":"N?","meaning":"ý nghĩa","example":"ví dụ"}],
  "vocabulary": [{"word":"từ","reading":"cách đọc","meaning":"nghĩa","jlpt":"N?"}]
}
Khi hỏi bình thường, trả lời text thông thường bằng tiếng Việt.
Tự động phát hiện JLPT level từ từ vựng và câu hỏi của user, điều chỉnh độ khó giải thích phù hợp.${vocabCtx}`
}

// ── Gemini call ───────────────────────────────────────────────────
const MODELS = ['gemini-2.5-flash', 'gemini-2.0-flash-lite']

async function callGemini(systemPrompt: string, history: any[], userText: string) {
  for (const model of MODELS) {
    try {
      const res = await fetch(
        `https://generativelanguage.googleapis.com/v1beta/models/${model}:generateContent?key=${apiKey}`,
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            system_instruction: { parts: [{ text: systemPrompt }] },
            contents: [...history, { role: 'user', parts: [{ text: userText }] }],
            generationConfig: { temperature: 0.8, maxOutputTokens: 2048 }
          })
        }
      )
      const data = await res.json()
      if (data.error) throw data.error
      return data.candidates?.[0]?.content?.parts?.[0]?.text || ''
    } catch (e: any) {
      if (e.status === 'RESOURCE_EXHAUSTED') continue
      throw e
    }
  }
  throw new Error('Hết quota API')
}

// ── Parse response ────────────────────────────────────────────────
function parseResponse(raw: string, isPractice: boolean) {
  try {
    const jsonMatch = raw.match(/\{[\s\S]*\}/)
    if (jsonMatch) {
      const data = JSON.parse(jsonMatch[0])
      if (data.type === 'japanese_analysis') {
        const levels = data.grammar?.map((g: any) => g.jlpt).filter(Boolean) || []
        if (levels.length && !detectedLevel.value) {
          detectedLevel.value = levels[0]
        }
        return { analysisData: data, content: data.explanation || '' }
      }
      if (data.type === 'practice_response') {
        return {
          jpResponse: data.jp_response,
          content: data.translation || '',
          analysisData: data.vocabulary?.length ? {
            vocabulary: data.vocabulary,
            correction: data.correction
          } : null
        }
      }
    }
  } catch {}
  return { content: raw, analysisData: null }
}

// ── Send message ──────────────────────────────────────────────────
async function sendToGemini(text: string, isStart = false) {
  const isPractice = mode.value === 'practice'
  const systemPrompt = buildSystemPrompt(isPractice)

  const history = messages.value.map(m => ({
    role: m.role === 'user' ? 'user' : 'model',
    parts: [{ text: m.jpResponse || m.content }]
  }))

  const prompt = isStart ? `[BẮT ĐẦU TÌNH HUỐNG: ${selectedScenario.value?.name}]` : text

  loading.value = true
  await scrollBottom()

  try {
    const raw = await callGemini(systemPrompt, history, prompt)
    const parsed = parseResponse(raw, isPractice)
    messages.value.push({ role: 'model', ...parsed })
  } catch (e: any) {
    messages.value.push({ role: 'model', content: `❌ Lỗi: ${e.message || e}` })
  } finally {
    loading.value = false
    await scrollBottom()
  }
}

async function send() {
  const text = userInput.value.trim()
  if (!text || loading.value || !apiKey) return

  messages.value.push({ role: 'user', content: text })
  userInput.value = ''
  if (inputEl.value) inputEl.value.style.height = 'auto'

  await sendToGemini(text)
}

function sendQuick(q: string) {
  userInput.value = q
  if (mode.value === 'practice' && selectedScenario.value && !practiceStarted.value) {
    startPractice()
    return
  }
  send()
}

function clearChat() {
  messages.value = []
  practiceStarted.value = false
}

// ── Utils ─────────────────────────────────────────────────────────
function formatText(text: string) {
  return text
    .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
    .replace(/\*(.*?)\*/g, '<em>$1</em>')
    .replace(/`([^`]+)`/g, '<code class="bg-gray-200 dark:bg-gray-700 px-1 py-0.5 rounded text-xs font-mono">$1</code>')
    .replace(/\n/g, '<br />')
}

async function scrollBottom() {
  await nextTick()
  if (messagesEl.value) messagesEl.value.scrollTop = messagesEl.value.scrollHeight
}

function autoResize(e: Event) {
  const el = e.target as HTMLTextAreaElement
  el.style.height = 'auto'
  el.style.height = Math.min(el.scrollHeight, 140) + 'px'
}

const inputPlaceholder = computed(() => {
  if (mode.value === 'practice') {
    if (!selectedScenario.value) return 'Chọn tình huống bên trái...'
    if (!practiceStarted.value) return 'Nhấn "Bắt đầu" để luyện tập...'
    return `Trả lời bằng tiếng Nhật... (${selectedScenario.value.name})`
  }
  return 'Hỏi về ngữ pháp, từ vựng tiếng Nhật...'
})

onMounted(() => {
  loadUserContext()
})
</script>

<style scoped>
@import url('https://fonts.googleapis.com/css2?family=Noto+Serif+JP:wght@400;700&display=swap');

/* Hiệu ứng gõ chữ (Dấu 3 chấm) */
.mini-dots { display: flex; gap: 3px; align-items: center; }
.mini-dots span {
  width: 5px; height: 5px; border-radius: 50%;
  background-color: currentColor;
  animation: dot-bounce 1.2s infinite;
}
.mini-dots span:nth-child(2) { animation-delay: 0.2s; }
.mini-dots span:nth-child(3) { animation-delay: 0.4s; }

@keyframes dot-bounce {
  0%, 80%, 100% { transform: translateY(0); opacity: 0.6; }
  40% { transform: translateY(-4px); opacity: 1; }
}

/* Hiệu ứng trượt tin nhắn lên */
.msg-enter {
  animation: fadeUp 0.3s ease-out forwards;
}
@keyframes fadeUp {
  from { opacity: 0; transform: translateY(12px); }
  to { opacity: 1; transform: translateY(0); }
}

/* Custom thanh cuộn */
.custom-scrollbar::-webkit-scrollbar {
  width: 6px;
}
.custom-scrollbar::-webkit-scrollbar-track {
  background: transparent;
}
.custom-scrollbar::-webkit-scrollbar-thumb {
  background-color: #cbd5e1; /* Tailwind gray-300 */
  border-radius: 10px;
}
:global(.dark) .custom-scrollbar::-webkit-scrollbar-thumb {
  background-color: #4b5563; /* Tailwind gray-600 */
}
</style>