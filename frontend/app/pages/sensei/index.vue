<template>
  <div class="sensei-shell">
    <!-- Background atmosphere -->
    <div class="bg-grid"></div>
    <div class="bg-glow"></div>

    <div class="sensei-layout">
      <!-- ── Left: Context panel ── -->
      <aside class="context-panel">
        <div class="context-header">
          <div class="sensei-logo">
            <span class="logo-jp">先生</span>
            <span class="logo-en">AI Sensei</span>
          </div>
        </div>

        <!-- Mode switch -->
        <div class="mode-switch">
          <button :class="['mode-btn', mode === 'tutor' ? 'active' : '']"
            @click="switchMode('tutor')">
            <span class="mode-icon">📖</span>
            <div>
              <p class="mode-name">Tutor</p>
              <p class="mode-desc">Hỏi đáp tự do</p>
            </div>
          </button>
          <button :class="['mode-btn', mode === 'practice' ? 'active' : '']"
            @click="switchMode('practice')">
            <span class="mode-icon">💬</span>
            <div>
              <p class="mode-name">Practice</p>
              <p class="mode-desc">Hội thoại roleplay</p>
            </div>
          </button>
        </div>

        <!-- Practice: scenario picker -->
        <div v-if="mode === 'practice'" class="scenario-section">
          <p class="section-label">Chọn tình huống</p>
          <div class="scenario-list">
            <button v-for="s in scenarios" :key="s.id"
              :class="['scenario-btn', selectedScenario?.id === s.id ? 'active' : '']"
              @click="selectScenario(s)">
              <span class="scenario-icon">{{ s.icon }}</span>
              <span class="scenario-name">{{ s.name }}</span>
            </button>
          </div>
        </div>

        <!-- Context: user vocab + deck stats -->
        <div class="user-context">
          <p class="section-label">Ngữ cảnh của bạn</p>

          <div class="context-stat" v-if="contextLoaded">
            <div class="stat-row">
              <span class="stat-icon">📚</span>
              <span class="stat-text">{{ totalVocabCount }} từ vựng đã lưu</span>
            </div>
            <div class="stat-row">
              <span class="stat-icon">🃏</span>
              <span class="stat-text">{{ totalCardCount }} flashcard</span>
            </div>
            <div class="stat-row" v-if="detectedLevel">
              <span class="stat-icon">🎯</span>
              <span class="stat-text">AI phát hiện: ~{{ detectedLevel }}</span>
            </div>
          </div>
          <div v-else class="context-loading">
            <div class="mini-dots"><span></span><span></span><span></span></div>
            <span>Đang tải ngữ cảnh...</span>
          </div>

          <!-- Sample vocab chips -->
          <div v-if="sampleVocabs.length" class="vocab-chips">
            <span v-for="v in sampleVocabs" :key="v" class="vocab-chip">{{ v }}</span>
          </div>
        </div>

        <!-- Quick prompts -->
        <div class="quick-prompts">
          <p class="section-label">Gợi ý</p>
          <button v-for="q in currentQuickPrompts" :key="q"
            class="quick-prompt-btn" @click="sendQuick(q)">
            {{ q }}
          </button>
        </div>
      </aside>

      <!-- ── Right: Chat area ── -->
      <main class="chat-area">
        <!-- Chat header -->
        <div class="chat-topbar">
          <div class="chat-status">
            <div class="status-dot" :class="apiKey ? 'online' : 'offline'"></div>
            <span>{{ mode === 'tutor' ? 'Tutor Mode' : `Practice: ${selectedScenario?.name || 'Chọn tình huống'}` }}</span>
          </div>
          <div class="topbar-actions">
            <button class="topbar-btn" @click="clearChat" title="Xóa chat">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" width="16" height="16">
                <polyline points="1 4 1 10 7 10"/><polyline points="23 20 23 14 17 14"/>
                <path d="M20.49 9A9 9 0 0 0 5.64 5.64L1 10m22 4l-4.64 4.36A9 9 0 0 1 3.51 15"/>
              </svg>
            </button>
          </div>
        </div>

        <!-- Messages -->
        <div class="messages-wrap" ref="messagesEl">
          <!-- Welcome state -->
          <div v-if="messages.length === 0" class="welcome-state">
            <div class="welcome-kanji">{{ mode === 'tutor' ? '質問' : '会話' }}</div>
            <h2 class="welcome-title">
              {{ mode === 'tutor' ? 'Hỏi bất cứ điều gì về tiếng Nhật' : 'Luyện hội thoại với AI' }}
            </h2>
            <p class="welcome-sub">
              {{ mode === 'tutor'
                ? 'AI sẽ giải thích ngữ pháp, từ vựng dựa trên level và vocab của bạn'
                : 'Chọn tình huống bên trái rồi bắt đầu hội thoại' }}
            </p>
            <div v-if="mode === 'practice' && selectedScenario" class="scenario-preview">
              <span class="scenario-preview-icon">{{ selectedScenario.icon }}</span>
              <div>
                <p class="scenario-preview-name">{{ selectedScenario.name }}</p>
                <p class="scenario-preview-desc">{{ selectedScenario.description }}</p>
              </div>
              <button class="btn-start-practice" @click="startPractice">
                Bắt đầu →
              </button>
            </div>
          </div>

          <!-- Messages -->
          <div v-for="(msg, i) in messages" :key="i"
            :class="['msg', msg.role]">
            <div class="msg-avatar">
              {{ msg.role === 'user' ? (username?.[0]?.toUpperCase() || 'U') : '先' }}
            </div>
            <div class="msg-content">
              <!-- Japanese analysis card -->
              <div v-if="msg.analysisData" class="analysis-card">
                <div class="analysis-sentence">{{ msg.analysisData.sentence }}</div>
                <div v-if="msg.analysisData.translation" class="analysis-translation">
                  🇻🇳 {{ msg.analysisData.translation }}
                </div>
                <div v-if="msg.analysisData.explanation" class="analysis-explanation">
                  {{ msg.analysisData.explanation }}
                </div>
                <div v-if="msg.analysisData.grammar?.length" class="analysis-section">
                  <p class="analysis-section-title">📐 Ngữ pháp</p>
                  <div v-for="g in msg.analysisData.grammar" :key="g.pattern" class="grammar-item">
                    <div class="grammar-header">
                      <span class="grammar-pattern">{{ g.pattern }}</span>
                      <span class="jlpt-badge" :style="jlptStyle(g.jlpt)">{{ g.jlpt }}</span>
                    </div>
                    <p class="grammar-meaning">{{ g.meaning }}</p>
                    <p v-if="g.example" class="grammar-example">例: {{ g.example }}</p>
                  </div>
                </div>
                <div v-if="msg.analysisData.vocabulary?.length" class="analysis-section">
                  <p class="analysis-section-title">📚 Từ vựng</p>
                  <div class="vocab-row" v-for="v in msg.analysisData.vocabulary" :key="v.word">
                    <span class="vocab-word">{{ v.word }}</span>
                    <span v-if="v.reading" class="vocab-reading">【{{ v.reading }}】</span>
                    <span class="vocab-meaning">{{ v.meaning }}</span>
                    <span class="jlpt-badge" :style="jlptStyle(v.jlpt)">{{ v.jlpt }}</span>
                  </div>
                </div>
                <!-- Correction card for practice mode -->
                <div v-if="msg.analysisData.correction" class="correction-card">
                  <p class="correction-title">✏️ Góp ý</p>
                  <p class="correction-text">{{ msg.analysisData.correction }}</p>
                </div>
              </div>

              <!-- Normal text -->
              <div v-else class="msg-text" v-html="formatText(msg.content)"></div>

              <!-- Practice: natural Japanese response -->
              <div v-if="msg.jpResponse" class="jp-response">
                <span class="jp-response-label">AI trả lời:</span>
                <span class="jp-response-text">{{ msg.jpResponse }}</span>
              </div>
            </div>
          </div>

          <!-- Typing indicator -->
          <div v-if="loading" class="msg model">
            <div class="msg-avatar">先</div>
            <div class="msg-content">
              <div class="typing-dots"><span></span><span></span><span></span></div>
            </div>
          </div>
        </div>

        <!-- Input area -->
        <div class="input-area">
          <div v-if="mode === 'practice' && !practiceStarted && !selectedScenario"
            class="input-hint">← Chọn tình huống để bắt đầu luyện tập</div>
          <div v-else class="input-wrap">
            <textarea
              v-model="userInput"
              class="chat-input"
              :placeholder="inputPlaceholder"
              rows="1"
              @keydown.enter.exact.prevent="send"
              @input="autoResize"
              ref="inputEl"
            ></textarea>
            <button class="send-btn" @click="send"
              :disabled="!userInput.trim() || loading || !apiKey">
              <svg viewBox="0 0 24 24" fill="currentColor" width="18" height="18">
                <path d="M2.01 21L23 12 2.01 3 2 10l15 2-15 2z"/>
              </svg>
            </button>
          </div>
          <p v-if="!apiKey" class="no-key-warn">⚠ Chưa có Gemini API Key</p>
        </div>
      </main>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, nextTick } from 'vue'
import { useRuntimeConfig } from '#app'
import { useJwt } from '~/composables/useJwt'

definePageMeta({ layout: 'default' })

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
const userVocabContext = ref('') // inject vào prompt

async function loadUserContext() {
  if (!jwt.value) return
  const headers = { Authorization: `Bearer ${jwt.value}` }
  const base = config.public.apiBaseUrl

  try {
    // Load decks → lấy cards count + sample words
    const decksRes = await fetch(`${base}/api/decks/my-decks`, { headers })
    if (decksRes.ok) {
      const data = await decksRes.json()
      const decks = data.result || data
      totalCardCount.value = decks.reduce((s: number, d: any) => s + (d.cardCount || 0), 0)

      // Lấy sample từ deck đầu tiên
      if (decks.length > 0) {
        const detailRes = await fetch(`${base}/api/decks/${decks[0].id}`, { headers })
        if (detailRes.ok) {
          const detail = await detailRes.json()
          const cards = detail.result?.cards || detail.cards || []
          const samples = cards.slice(0, 8).map((c: any) => c.charBig || c.frontText || '')
          sampleVocabs.value = samples.filter(Boolean)

          // Build context string
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
  const vocabCtx = userVocabContext.value
    ? `\n\nNgữ cảnh người học: ${userVocabContext.value}`
    : ''

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
        // Detect level từ grammar
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

  const prompt = isStart
    ? `[BẮT ĐẦU TÌNH HUỐNG: ${selectedScenario.value?.name}]`
    : text

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
    .replace(/`([^`]+)`/g, '<code>$1</code>')
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
@import url('https://fonts.googleapis.com/css2?family=Noto+Serif+JP:wght@400;700&family=DM+Sans:wght@300;400;500;600&display=swap');

.sensei-shell {
  /* Dark mode default — app.vue default theme là "dark" */
  --accent: #e8c84a;
  --accent2: #5b8dee;
  --red: #f87171;
  --green: #4ade80;
  --bg: #121212;
  --surface: #1e1e1e;
  --surface2: #2a2a2a;
  --border: #333333;
  --text: #f5f5f5;
  --text-muted: #9ca3af;

  height: 100%;
  background: var(--bg);
  color: var(--text);
  font-family: 'DM Sans', sans-serif;
  position: relative;
}

/* Light mode — khi html KHÔNG có class "dark" */
:global(html:not(.dark)) .sensei-shell {
  --accent: #c9a227;
  --accent2: #3b73d4;
  --red: #ef4444;
  --green: #22c55e;
  --bg: #f9f9f9;
  --surface: #ffffff;
  --surface2: #f3f4f6;
  --border: #e5e7eb;
  --text: #222222;
  --text-muted: #6b7280;
}

/* Subtle grid chỉ trong content area */
.bg-grid {
  position: absolute; inset: 0; z-index: 0;
  background-image:
    linear-gradient(rgba(0,0,0,0.04) 1px, transparent 1px),
    linear-gradient(90deg, rgba(0,0,0,0.04) 1px, transparent 1px);
  background-size: 40px 40px;
  pointer-events: none;
}
.bg-glow {
  position: absolute; top: -100px; right: -100px;
  width: 400px; height: 400px; z-index: 0;
  background: radial-gradient(circle, rgba(201,162,39,0.08) 0%, transparent 70%);
  pointer-events: none;
}

.sensei-layout {
  position: relative; z-index: 1;
  display: flex;
  height: 100%;
}

/* ── Context panel ── */
.context-panel {
  width: 260px;
  flex-shrink: 0;
  background: var(--surface2);
  border-right: 1px solid var(--border);
  display: flex; flex-direction: column;
  overflow-y: auto; overflow-x: hidden;
  scrollbar-width: thin;
  scrollbar-color: var(--border) transparent;
}

.context-header {
  padding: 20px;
  border-bottom: 1px solid var(--border);
}
.sensei-logo {
  display: flex; flex-direction: column;
}
.logo-jp {
  font-family: 'Noto Serif JP', serif;
  font-size: 28px; font-weight: 700;
  color: var(--accent);
  line-height: 1;
}
.logo-en {
  font-size: 11px; letter-spacing: 0.15em;
  text-transform: uppercase;
  color: var(--text-muted);
  margin-top: 2px;
}

/* Mode switch */
.mode-switch {
  padding: 12px;
  display: flex; flex-direction: column; gap: 6px;
  border-bottom: 1px solid var(--border);
}
.mode-btn {
  display: flex; align-items: center; gap: 10px;
  padding: 10px 12px; border-radius: 10px;
  border: 1px solid transparent;
  background: transparent; cursor: pointer;
  text-align: left; transition: all 0.15s;
  color: var(--text-muted); font-family: inherit; width: 100%;
}
.mode-btn:hover { background: var(--surface2); color: var(--text); }
.mode-btn.active {
  background: rgba(232,200,74,0.08);
  border-color: rgba(232,200,74,0.3);
  color: var(--text);
}
.mode-icon { font-size: 20px; flex-shrink: 0; }
.mode-name { font-size: 13px; font-weight: 600; line-height: 1; }
.mode-desc { font-size: 11px; color: var(--text-muted); margin-top: 2px; }

/* Scenarios */
.scenario-section { padding: 12px; border-bottom: 1px solid var(--border); }
.section-label {
  font-size: 10px; font-weight: 700; letter-spacing: 0.12em;
  text-transform: uppercase; color: var(--text-muted);
  margin-bottom: 8px;
}
.scenario-list { display: flex; flex-direction: column; gap: 4px; }
.scenario-btn {
  display: flex; align-items: center; gap: 8px;
  padding: 8px 10px; border-radius: 8px;
  border: 1px solid transparent; background: transparent;
  cursor: pointer; text-align: left; transition: all 0.15s;
  color: var(--text-muted); font-family: inherit; width: 100%;
}
.scenario-btn:hover { background: var(--surface2); color: var(--text); }
.scenario-btn.active {
  background: rgba(91,141,238,0.1);
  border-color: rgba(91,141,238,0.3);
  color: var(--accent2);
}
.scenario-icon { font-size: 16px; }
.scenario-name { font-size: 13px; font-weight: 500; }

/* User context */
.user-context { padding: 12px; border-bottom: 1px solid var(--border); }
.context-stat { display: flex; flex-direction: column; gap: 6px; margin-bottom: 10px; }
.stat-row { display: flex; align-items: center; gap: 8px; font-size: 12px; color: var(--text-muted); }
.stat-icon { font-size: 14px; }
.context-loading {
  display: flex; align-items: center; gap: 8px;
  font-size: 12px; color: var(--text-muted);
}
.mini-dots { display: flex; gap: 3px; }
.mini-dots span {
  width: 4px; height: 4px; border-radius: 50%;
  background: var(--text-muted); animation: dot-bounce 1.2s infinite;
}
.mini-dots span:nth-child(2) { animation-delay: 0.2s; }
.mini-dots span:nth-child(3) { animation-delay: 0.4s; }
@keyframes dot-bounce { 0%,80%,100% { transform:translateY(0) } 40% { transform:translateY(-4px) } }

.vocab-chips { display: flex; flex-wrap: wrap; gap: 4px; margin-top: 8px; }
.vocab-chip {
  font-family: 'Noto Serif JP', serif;
  font-size: 12px; padding: 2px 8px;
  background: var(--surface2); border: 1px solid var(--border);
  border-radius: 20px; color: var(--text-muted);
}

/* Quick prompts */
.quick-prompts { padding: 12px; }
.quick-prompt-btn {
  display: block; width: 100%;
  text-align: left; padding: 7px 10px;
  background: transparent; border: 1px solid var(--border);
  border-radius: 8px; color: var(--text-muted);
  font-size: 12px; cursor: pointer; margin-bottom: 5px;
  transition: all 0.15s; font-family: inherit;
}
.quick-prompt-btn:hover { border-color: var(--accent2); color: var(--text); }

/* ── Chat area ── */
.chat-area {
  flex: 1; display: flex; flex-direction: column; overflow: hidden;
  background: var(--bg);
}

.chat-topbar {
  display: flex; align-items: center; justify-content: space-between;
  padding: 12px 20px;
  background: var(--surface); border-bottom: 1px solid var(--border);
  flex-shrink: 0;
}
.chat-status { display: flex; align-items: center; gap: 8px; font-size: 13px; color: var(--text-muted); }
.status-dot { width: 7px; height: 7px; border-radius: 50%; }
.status-dot.online { background: var(--green); box-shadow: 0 0 6px var(--green); }
.status-dot.offline { background: var(--red); }
.topbar-actions { display: flex; gap: 6px; }
.topbar-btn {
  width: 30px; height: 30px; border-radius: 8px;
  background: transparent; border: 1px solid var(--border);
  color: var(--text-muted); cursor: pointer; display: flex;
  align-items: center; justify-content: center; transition: all 0.15s;
}
.topbar-btn:hover { border-color: var(--text-muted); color: var(--text); }

/* Messages */
.messages-wrap {
  flex: 1; overflow-y: auto; padding: 24px;
  display: flex; flex-direction: column; gap: 20px;
  scrollbar-width: thin; scrollbar-color: var(--border) transparent;
  background: var(--bg);
}

/* Welcome */
.welcome-state {
  flex: 1; display: flex; flex-direction: column;
  align-items: center; justify-content: center;
  text-align: center; gap: 12px; padding: 40px;
}
.welcome-kanji {
  font-family: 'Noto Serif JP', serif;
  font-size: 64px; font-weight: 700;
  color: var(--accent); opacity: 0.5;
  line-height: 1;
}
.welcome-title { font-size: 20px; font-weight: 600; color: var(--text); }
.welcome-sub { font-size: 14px; color: var(--text-muted); max-width: 360px; line-height: 1.6; }
.scenario-preview {
  display: flex; align-items: center; gap: 14px;
  background: var(--surface); border: 1px solid var(--border);
  border-radius: 14px; padding: 16px 20px;
  margin-top: 10px;
}
.scenario-preview-icon { font-size: 28px; }
.scenario-preview-name { font-size: 15px; font-weight: 600; }
.scenario-preview-desc { font-size: 13px; color: var(--text-muted); }
.btn-start-practice {
  background: var(--accent); color: #111;
  border: none; border-radius: 10px;
  padding: 10px 20px; font-size: 14px; font-weight: 700;
  cursor: pointer; transition: opacity 0.2s; font-family: inherit;
  white-space: nowrap; margin-left: auto;
}
.btn-start-practice:hover { opacity: 0.85; }

/* Messages */
.msg {
  display: flex; gap: 12px; align-items: flex-start;
  animation: fadeUp 0.2s ease;
}
@keyframes fadeUp { from { opacity:0; transform:translateY(8px) } to { opacity:1; transform:translateY(0) } }
.msg.user { flex-direction: row-reverse; }
.msg-avatar {
  width: 32px; height: 32px; border-radius: 10px;
  display: flex; align-items: center; justify-content: center;
  font-size: 13px; font-weight: 700; flex-shrink: 0;
}
.user .msg-avatar { background: var(--accent); color: #111; }
.model .msg-avatar {
  background: var(--surface2); border: 1px solid var(--border);
  font-family: 'Noto Serif JP', serif; color: var(--accent);
}
.msg-content { max-width: 78%; display: flex; flex-direction: column; gap: 8px; }
.user .msg-content { align-items: flex-end; }

.msg-text {
  background: var(--surface); border: 1px solid var(--border);
  border-radius: 14px; padding: 12px 16px;
  font-size: 14px; line-height: 1.7;
}
.user .msg-text { background: var(--accent); color: #111; border-color: var(--accent); }
.msg-text code { background: rgba(0,0,0,0.3); padding: 1px 5px; border-radius: 4px; font-family: monospace; font-size: 12px; }

/* JP response bubble */
.jp-response {
  background: rgba(91,141,238,0.08); border: 1px solid rgba(91,141,238,0.2);
  border-radius: 12px; padding: 10px 14px; font-size: 14px;
}
.jp-response-label { font-size: 10px; color: var(--accent2); font-weight: 700; text-transform: uppercase; letter-spacing: 0.1em; display: block; margin-bottom: 4px; }
.jp-response-text { font-family: 'Noto Serif JP', serif; font-size: 16px; color: var(--text); }

/* Analysis card */
.analysis-card {
  background: var(--surface); border: 1px solid var(--border);
  border-radius: 14px; padding: 16px; display: flex; flex-direction: column; gap: 10px;
}
.analysis-sentence {
  font-family: 'Noto Serif JP', serif;
  font-size: 18px; font-weight: 700; color: var(--text);
  padding: 10px 12px; border-left: 3px solid var(--accent);
  line-height: 1.6;
}
.analysis-translation {
  font-size: 14px; color: var(--text);
  background: rgba(91,141,238,0.06); border-radius: 8px;
  padding: 8px 12px;
}
.analysis-explanation { font-size: 13px; color: var(--text-muted); line-height: 1.6; }
.analysis-section { display: flex; flex-direction: column; gap: 6px; }
.analysis-section-title {
  font-size: 11px; font-weight: 700; text-transform: uppercase;
  letter-spacing: 0.1em; color: var(--text-muted);
}
.grammar-item {
  background: var(--surface); border: 1px solid var(--border);
  border-radius: 8px; padding: 10px 12px;
}
.grammar-header { display: flex; align-items: center; gap: 8px; margin-bottom: 4px; }
.grammar-pattern { font-size: 15px; font-weight: 700; color: var(--accent); }
.jlpt-badge { font-size: 10px; font-weight: 700; padding: 2px 7px; border-radius: 20px; }
.grammar-meaning { font-size: 13px; color: var(--text); }
.grammar-example { font-size: 12px; color: var(--text-muted); font-style: italic; margin-top: 4px; }

.vocab-row {
  display: flex; align-items: center; gap: 8px; flex-wrap: wrap;
  background: var(--surface); border: 1px solid var(--border);
  border-radius: 7px; padding: 6px 10px;
}
.vocab-word { font-family: 'Noto Serif JP', serif; font-size: 15px; font-weight: 700; color: var(--text); }
.vocab-reading { font-size: 12px; color: var(--text-muted); }
.vocab-meaning { font-size: 13px; color: var(--text); flex: 1; }

.correction-card {
  background: rgba(248,113,113,0.08); border: 1px solid rgba(248,113,113,0.2);
  border-radius: 8px; padding: 10px 12px;
}
.correction-title { font-size: 12px; font-weight: 700; color: var(--red); margin-bottom: 4px; }
.correction-text { font-size: 13px; color: var(--text); }

/* Typing */
.typing-dots {
  display: flex; gap: 5px; align-items: center;
  padding: 12px 16px;
  background: var(--surface2); border: 1px solid var(--border);
  border-radius: 14px;
}
.typing-dots span {
  width: 7px; height: 7px; background: var(--text-muted);
  border-radius: 50%; animation: bounce 1.2s infinite;
}
.typing-dots span:nth-child(2) { animation-delay: 0.2s; }
.typing-dots span:nth-child(3) { animation-delay: 0.4s; }
@keyframes bounce { 0%,80%,100%{transform:translateY(0)} 40%{transform:translateY(-6px)} }

/* Input */
.input-area {
  padding: 16px 20px;
  background: var(--surface); border-top: 1px solid var(--border);
  flex-shrink: 0;
}
.input-hint { text-align: center; font-size: 13px; color: var(--text-muted); padding: 8px; }
.input-wrap { display: flex; gap: 10px; align-items: flex-end; }
.chat-input {
  flex: 1; background: var(--surface); border: 1px solid var(--border);
  color: var(--text); border-radius: 12px; padding: 10px 14px;
  font-size: 14px; resize: none; max-height: 140px; overflow-y: auto;
  font-family: inherit; line-height: 1.5; outline: none;
  transition: border-color 0.2s;
}
.chat-input:focus { border-color: var(--accent2); }
.chat-input::placeholder { color: var(--text-muted); }
.send-btn {
  width: 42px; height: 42px; background: var(--accent2);
  border: none; border-radius: 12px; cursor: pointer;
  display: flex; align-items: center; justify-content: center;
  flex-shrink: 0; transition: all 0.2s; color: white;
}
.send-btn:hover:not(:disabled) { background: #4a7de0; transform: translateY(-1px); }
.send-btn:disabled { opacity: 0.4; cursor: not-allowed; }
.no-key-warn { text-align: center; font-size: 12px; color: #f59e0b; margin-top: 6px; }
</style>