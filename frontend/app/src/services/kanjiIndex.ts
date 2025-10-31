// File: src/services/kanjiStore.ts
import { ref } from 'vue'
import Papa from 'papaparse'

// 1. Định nghĩa cấu trúc dữ liệu
export interface KanjiItem {
  id: string
  char: string
  meanlong: string
  jlpt: string
  mean: string
  strokes: string
  freq: string
  onyomi: string
  kunyomi: string
}

// 2. State dùng chung
export const kanjiList = ref<KanjiItem[]>([])
export const isLoading = ref(false)

// 3. Chỉ load 1 lần
let loadPromise: Promise<KanjiItem[]> | null = null

// 4. Các hàm helper
function cleanField(s?: string) {
  return (s ?? '').toString().trim().replace(/^\uFEFF/, '').replace(/^"|"$/g, '')
}

const katakana_re = /[\u30A0-\u30FF]/
const hiragana_re = /[\u3040-\u309F]/

function normalizeAllRebs(raw?: string) {
  const s = (raw ?? '').toString().trim().replace(/^"|"$/g, '')
  if (!s) return { ony: '', kun: '', combined: '' }

  const clean = s.replace('，', ',').replace(/\s+/g, ' ').trim()
  const dropLeadingFreq = clean.replace(/^\s*\d+\s*[,，]\s*/, '')
  if (dropLeadingFreq !== clean) return normalizeAllRebs(dropLeadingFreq)

  if (clean.includes('|')) {
    const parts = clean.split('|').map(p => p.trim()).filter(Boolean)
    const ony = parts[0] || ''
    const kun = parts.slice(1).join(' | ') || ''
    return { ony, kun, combined: [ony, kun].filter(Boolean).join(' | ') }
  }

  const tokens = clean.split(/\s+/).map(t => t.trim()).filter(Boolean)
  const onyTokens = tokens.filter(t => katakana_re.test(t))
  const kunTokens = tokens.filter(t => hiragana_re.test(t) || /[.\-]/.test(t))
  const ony = onyTokens.join(' ')
  const kun = kunTokens.join(' ')
  const combined = [ony, kun].filter(Boolean).join(' | ') || clean
  return { ony, kun, combined }
}

function extractFreqAndAllRebs(freqRaw?: string, allRebsRaw?: string) {
  let freq = cleanField(freqRaw)
  let all = (allRebsRaw ?? '').toString().trim()

  if (!/^\d+$/.test(freq)) {
    const m = (freq || '').match(/(\d{1,})/)
    if (m) {
      freq = m[1]
      const restFromFreqRaw = (freqRaw ?? '').toString().replace(m[0], '').replace(/^,|^，/, '').trim()
      all = (restFromFreqRaw ? restFromFreqRaw + (all ? ' ' + all : '') : all).trim()
    } else {
      const m2 = (all || '').match(/^\s*(\d+)\s*[,，]\s*(.*)$/)
      if (m2) {
        freq = m2[1]
        all = m2[2].trim()
      }
    }
  }

  if (!/^\d+$/.test(freq)) freq = ''
  return { freq, all }
}

// 5. Hàm load dữ liệu
export async function loadKanjiData(): Promise<KanjiItem[]> {
  if (loadPromise) return loadPromise
  if (kanjiList.value.length > 0) return kanjiList.value

  isLoading.value = true
  console.log('Loading kanji.csv ...')

  loadPromise = (async () => {
    try {
      const csvFiles = import.meta.glob('/src/assets/kanji.csv', { as: 'raw' })
      const loadCsv = csvFiles['/src/assets/kanji.csv']
      if (!loadCsv) throw new Error('Không tìm thấy file /src/assets/kanji.csv')

      const csvText = await loadCsv()
      const result = Papa.parse<any>(csvText, {
        header: true,
        skipEmptyLines: true,
        transformHeader: h => (h || '').toString().trim()
      })

      const rows = result.data as any[]
      const items: KanjiItem[] = rows
        .filter(r => r && (r.Id || r.Character))
        .map((row: any) => {
          const id = cleanField(row.Id)
          const ch = cleanField(row.Character)
          const jlpt = cleanField(row.JlptLevel)
          const strokes = cleanField(row.StrokeCount)
          const freqRaw = row.Freq ?? ''
          const allRebsRaw = row.AllRebs ?? ''
          const freqAndAll = extractFreqAndAllRebs(freqRaw, allRebsRaw)
          const freq = freqAndAll.freq
          const normalized = normalizeAllRebs(freqAndAll.all)
          const onyomi = normalized.ony
          const kunyomi = normalized.kun

          // ✅ Chỉ lấy nghĩa đầu tiên (trước dấu phẩy hoặc ##)
          const meaningField = cleanField(row.Meaning)
          const firstMeaning = (meaningField.split(/##|,/)[0] || '').trim()

          return {
            id: id || '',
            char: ch || '',
            meanlong: firstMeaning,
            jlpt: jlpt || '',
            mean: firstMeaning,
            strokes: strokes || '',
            freq: freq || '',
            onyomi,
            kunyomi
          }
        })
        .filter(item => item.id && item.char)

      kanjiList.value = items
      isLoading.value = false
      console.log(`Kanji store loaded: ${items.length} items.`)
      return items
    } catch (error) {
      console.error('Failed to load and parse kanji data:', error)
      isLoading.value = false
      loadPromise = null
      return []
    }
  })()

  return loadPromise
}
