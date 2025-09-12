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
  </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch, nextTick } from "vue"
import { useRoute } from "vue-router"
import HanziWriter from "hanzi-writer"
import Papa from "papaparse"

interface KanjiRow {
  Id: string
  Character: string
  JlptLevel: string
  Meaning: string
  StrokeCount: string
  Freq: string
  AllRebs?: string
}

interface KanjiItem {
  id: string
  char: string
  jlpt: string
  mean: string
  strokes: string
  freq: string
  onyomi: string
  kunyomi: string
}

const kanjiList = ref<KanjiItem[]>([])

const route = useRoute()
const kanji = ref(route.params.kanji as string || "永")
const selectedKanji = computed(() => {
  return kanjiList.value.find(k => k.char === kanji.value) || null
})
const writerInstance = ref<HanziWriter | null>(null)
const writerTarget = ref<HTMLDivElement | null>(null)

// Khởi tạo HanziWriter khi component mount
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

  const response = await fetch("/kanji.csv")
  const csvText = await response.text()

  // 👇 Thêm generic <KanjiRow>
  const result = Papa.parse<KanjiRow>(csvText, {
    header: true,
    skipEmptyLines: true,
  })

  // 👇 Chỉ định rõ row: KanjiRow
  kanjiList.value = result.data.map((row: KanjiRow) => {
    const firstMeaning = (row.Meaning?.split(",")[0] || "").trim()

    // Tách Onyomi và Kunyomi
    let onyomi = ""
    let kunyomi = ""

    if (row.AllRebs) {
      const parts = row.AllRebs.split("|").map((p: string) => p.trim())
      onyomi = parts[0] || ""
      kunyomi = parts[1] || ""
    }

    return {
      id: row.Id,
      char: row.Character,
      jlpt: row.JlptLevel,
      mean: firstMeaning,
      strokes: row.StrokeCount,
      freq: row.Freq,
      onyomi,
      kunyomi,
    }
  })
})

// Watch route param để đổi chữ
watch(() => route.params.kanji, async (newKanji) => {
  if (!newKanji) return

  kanji.value = decodeURIComponent(newKanji as string)

  await nextTick() // đảm bảo div tồn tại

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

// Hàm điều khiển
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
