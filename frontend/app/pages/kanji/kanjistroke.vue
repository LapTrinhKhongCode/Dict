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
    <div class="flex gap-2 mt-4">
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
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from "vue"
import { useRoute } from "vue-router"
import HanziWriter from "hanzi-writer"

const route = useRoute()
const kanji = ref(route.params.kanji as string || "永")
const writerInstance = ref<HanziWriter | null>(null)
const writerTarget = ref<HTMLDivElement | null>(null)

// Khởi tạo HanziWriter khi component mount
onMounted(async () => {
  // chờ DOM render xong
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
    onComplete: () => console.log("🎉 Done!")
  })
}
const reset = () => {
  writerInstance.value?.hideCharacter()
// writerInstance.value?.showCharacter({ duration: 0 })
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
