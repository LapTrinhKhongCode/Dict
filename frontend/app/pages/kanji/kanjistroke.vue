
<template>
  <div class="p-4">
<ClientOnly>
<div 
  ref="writerTarget" 
  style="width:150px;height:150px" 
  class="flex justify-self-center content-center border-2 border-amber-200 rounded-lg shadow-inner"
  :style="{
    backgroundImage: `
      linear-gradient(rgba(0, 0, 0, 0.08) 1px, transparent 1px),
      linear-gradient(90deg, rgba(0, 0, 0, 0.08) 1px, transparent 1px)
    `,
    backgroundSize: '15px 15px',
    backgroundColor: '#fffbeb'
  }"
></div>
</ClientOnly>

    <!-- Controls -->
    <div class="flex gap-2 mt-4  justify-self-center content-center" >
      <button @click="animate" class="px-4 py-2 bg-blue-500 text-white rounded">
        Animate
      </button>
      <button @click="quiz" class="px-4 py-2 bg-green-500 text-white rounded">
        Quiz
      </button>
      <button @click="reset" class="px-4 py-2 bg-gray-500 text-white rounded">
        Reset
      </button>
    </div>
  </div>
    <div class="bg-[#1e293b] text-white p-6 rounded-lg shadow-lg w-[380px]">
    <h2 class="text-lg font-bold mb-4">
      Chi tiết chữ kanji <span v-if="selectedKanji" class="text-2xl">{{ selectedKanji.char }}</span>
    </h2>

  <div v-if="selectedKanji" class="space-y-3">
    <!-- Hán tự -->
    <div class="flex items-center space-x-2">
      <span class="bg-[#1e40af] text-white px-3 py-1 rounded-full text-sm">Hán tự</span>
      <span>{{ selectedKanji.char }} - {{ selectedKanji.mean }}</span>
    </div>

    <!-- Kunyomi -->
    <div class="flex items-center space-x-2">
      <span class="bg-[#1e40af] text-white px-3 py-1 rounded-full text-sm">Kunyomi</span>
      <span class="text-blue-400">{{ selectedKanji.kunyomi }}</span>
    </div>

    <!-- Onyomi -->
    <div class="flex items-center space-x-2">
      <span class="bg-[#1e40af] text-white px-3 py-1 rounded-full text-sm">Onyomi</span>
      <span class="text-red-400">{{ selectedKanji.onyomi }}</span>
    </div>

    <!-- Số nét -->
    <div class="flex items-center space-x-2">
      <span class="bg-[#1e40af] text-white px-3 py-1 rounded-full text-sm">Số nét</span>
      <span>{{ selectedKanji.strokes }}</span>
    </div>

    <!-- JLPT -->
    <div class="flex items-center space-x-2">
      <span class="bg-[#1e40af] text-white px-3 py-1 rounded-full text-sm">JLPT</span>
      <span>{{ selectedKanji.jlpt }}</span>
    </div>

     <!-- FREQ -->
      <div class="flex items-center space-x-2">
      <span class="bg-[#1e40af] text-white px-3 py-1 rounded-full text-sm">Độ phổ biến</span>
      <span>{{ selectedKanji.freq }}</span>
    </div>
       <div class="flex items-center space-x-2">
      <span class="bg-[#1e40af] text-white px-3 py-1 rounded-full text-sm">Nghĩa</span>
      <span>{{ selectedKanji.meanlong }}</span>
    </div>
  </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, nextTick, computed } from "vue"
import { useRoute } from "vue-router"
import HanziWriter from "hanzi-writer"
import Papa from "papaparse"
let kanjiLoadPromise: Promise<KanjiItem[]> | null = null

async function loadKanjiOnce(): Promise<KanjiItem[]> {
  if (kanjiLoadPromise) return kanjiLoadPromise

  kanjiLoadPromise = (async () => {
const csvFiles = import.meta.glob("/src/assets/kanji_fixed.csv", { as: "raw" })
const loadCsv = csvFiles["/src/assets/kanji_fixed.csv"]
if (!loadCsv) throw new Error("Không tìm thấy file kanji_fixed.csv")

const csvText = await loadCsv()
const result = Papa.parse<any>(csvText, {
  header: true,
  skipEmptyLines: true,
  transformHeader: h => (h || "").toString().trim()
})


    const rows = result.data as any[]

    const items: KanjiItem[] = rows
      .filter(r => r && (r.Id || r.Character))
      .map((row: any) => {
        const id = cleanField(row.Id)
        const ch = cleanField(row.Character)
        const mraw = row.MeanLong ?? ""
        const jraw = row.JlptLevel ?? ""
        const meanClean = cleanMeanLongAndJlpt(mraw, jraw)
        const meanlong = meanClean.mean
        const jlpt = meanClean.jlpt ?? ""
        const meaningField = cleanField(row.Meaning)
        const firstMeaning = (meaningField.split(/##|,/)[0] || "").trim()
        const strokes = cleanField(row.StrokeCount)
        const freqRaw = row.Freq ?? ""
        const allRebsRaw = row.AllRebs ?? ""
        const freqAndAll = extractFreqAndAllRebs(freqRaw, allRebsRaw)
        const freq = freqAndAll.freq
        const normalized = normalizeAllRebs(freqAndAll.all)
        const onyomi = normalized.ony
        const kunyomi = normalized.kun
        return {
          id: id || "",
          char: ch || "",
          meanlong: meanlong || "",
          jlpt: jlpt || "",
          mean: firstMeaning || "",
          strokes: strokes || "",
          freq: freq || "",
          onyomi,
          kunyomi,
        } as KanjiItem
      })
      .filter(item => item.id && item.char)

    return items
  })()

  return kanjiLoadPromise
}

interface KanjiRow {
  Id?: string
  Character?: string
  MeanLong?: string
  JlptLevel?: string
  Meaning?: string
  StrokeCount?: string
  Freq?: string
  AllRebs?: string
}

interface KanjiItem {
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

const kanjiList = ref<KanjiItem[]>([])
const route = useRoute()
const kanji = ref((route.params.kanji as string) || "永")
const selectedKanji = computed(() => {
  return kanjiList.value.find(k => k.char === kanji.value) || null
})
const writerInstance = ref<HanziWriter | null>(null)
const writerTarget = ref<HTMLDivElement | null>(null)

function cleanField(s?: string) {
  return (s ?? "").toString().trim().replace(/^\uFEFF/, "").replace(/^"|"$/g, "")
}

const katakana_re = /[\u30A0-\u30FF]/
const hiragana_re = /[\u3040-\u309F]/

function normalizeAllRebs(raw?: string) {
  const s = (raw ?? "").toString().trim().replace(/^"|"$/g, "")
  if (!s) return { ony: "", kun: "", combined: "" }
  const clean = s.replace("，", ",").replace(/\s+/g, " ").trim()
  const dropLeadingFreq = clean.replace(/^\s*\d+\s*[,，]\s*/, "")
  if (dropLeadingFreq !== clean) return normalizeAllRebs(dropLeadingFreq)
  if (clean.includes("|")) {
    const parts = clean.split("|").map(p => p.trim()).filter(Boolean)
    const ony = parts[0] || ""
    const kun = parts.slice(1).join(" | ") || ""
    const combined = [ony, kun].filter(Boolean).join(" | ")
    return { ony, kun, combined }
  }
  const tokens = clean.split(/\s+/).map(t => t.trim()).filter(Boolean)
  const onyTokens = tokens.filter(t => katakana_re.test(t))
  const kunTokens = tokens.filter(t => hiragana_re.test(t) || /[.\-]/.test(t))
  const ony = onyTokens.join(" ")
  const kun = kunTokens.join(" ")
  const combined = [ony, kun].filter(Boolean).join(" | ") || clean
  return { ony, kun, combined }
}

function extractFreqAndAllRebs(freqRaw?: string, allRebsRaw?: string) {
  let freq = cleanField(freqRaw)
  let all = (allRebsRaw ?? "").toString().trim()
  if (!/^\d+$/.test(freq)) {
    const m = (freq || "").match(/(\d{1,})/)
    if (m) {
      freq = m[1]
      const restFromFreqRaw = (freqRaw ?? "").toString().replace(m[0], "").replace(/^,|^，/, "").trim()
      all = (restFromFreqRaw ? (restFromFreqRaw + (all ? " " + all : "")) : all).trim()
    } else {
      const m2 = (all || "").match(/^\s*(\d+)\s*[,，]\s*(.*)$/)
      if (m2) {
        freq = m2[1]
        all = m2[2].trim()
      }
    }
  }
  if (!/^\d+$/.test(freq)) freq = ""
  return { freq, all }
}

function cleanMeanLongAndJlpt(meanRaw?: string, jlptRaw?: string) {
  let mean = (meanRaw ?? "").toString().trim()
  mean = mean.replace(/##/g, " ").replace(/\s+/g, " ").trim()
  let jlpt = (jlptRaw ?? "").toString().trim()
  const m = (mean || "").match(/(.*?)[\.\,]?\s*(N[1-5])\s*$/)
  if (m) {
    mean = m[1].trim()
    if (!jlpt) jlpt = m[2]
  }
  return { mean, jlpt }
}

onMounted(async () => {
  await nextTick()
  if (writerTarget.value) {
    writerInstance.value = HanziWriter.create(writerTarget.value, kanji.value, {
      width: 150,
      height: 150,
      padding: 20,
      strokeColor: "#333",
      highlightColor: "#f00",
      showOutline: true,
      showCharacter: true,
      strokeAnimationSpeed: 0.5,
    })
  }

  try {
    const items = await loadKanjiOnce()
    kanjiList.value = items
  } catch (err) {
    console.error("Không load được kanji_fixed.csv:", err)
  }
})

watch(() => route.params.kanji, async (newKanji) => {
  if (!newKanji) return
  kanji.value = decodeURIComponent(newKanji as string)
  await nextTick()
  if (writerTarget.value) {
    writerTarget.value.innerHTML = ""
    writerInstance.value = HanziWriter.create(writerTarget.value, kanji.value, {
      width: 150,
      height: 150,
      padding: 20,
      strokeColor: "#333",
      highlightColor: "#f00",
      showOutline: true,
      showCharacter: true,
      strokeAnimationSpeed: 0.5,
    })
  }
})

const animate = () => writerInstance.value?.animateCharacter()
const quiz = () => {
  writerInstance.value?.quiz({
    onMistake: (strokeData) => console.log("❌ Mistake", strokeData),
    onCorrectStroke: (strokeData) => console.log("✔️ Correct", strokeData),
    onComplete: () => console.log("🎉 Done!"),
  })
}
const reset = () => {
  writerInstance.value?.hideCharacter()
}
</script>



<style scoped>
button {
  transition: background 0.2s;
}
button:hover {
  opacity: 0.8;
}

</style>
