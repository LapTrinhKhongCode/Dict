<template>
    <div class="space-y-4 flex flex-col items-center">
      <ClientOnly>
        <div 
          ref="writerTarget" 
          style="width: 150px; height: 150px" 
          class="flex justify-self-center content-center border-2 border-gray-200 rounded-lg shadow-inner"
          :style="{
            backgroundImage: `
              linear-gradient(rgba(0, 0, 0, 0.08) 1px, transparent 1px),
              linear-gradient(90deg, rgba(0, 0, 0, 0.08) 1px, transparent 1px)
            `,
            backgroundSize: '15px 15px',
            backgroundColor: '#fafafa'
          }"
        ></div>
      </ClientOnly>
  
      <div class="flex gap-2 justify-self-center content-center" >
        <button
        @click="animate"
        title="Thứ tự"
        class="p-2 bg-blue-900 text-white rounded hover:bg-blue-600 transition-colors"
      >
        <UIcon name="i-lucide-play" class="size-5" />
      </button>
      <button
        @click="quiz"
        title="Tập vẽ"
        class="p-2 bg-blue-900 text-white rounded hover:bg-green-600 transition-colors"
      >
        <UIcon name="i-lucide-pen-line" class="size-5" />
      </button>
      <button
        @click="reset"
        title="Reset"
        class="p-2 bg-blue-900 text-white rounded hover:bg-gray-600 transition-colors"
      >
        <UIcon name="i-lucide-rotate-ccw" class="size-5" />
      </button>
      </div>
    </div>
  </template>
  
  <script setup lang="ts">
  import { ref, watch, onUnmounted } from "vue";
  import HanziWriter from "hanzi-writer";
  
  const props = defineProps({
    kanji: {
      type: String,
      required: true,
    }
  });
  
  const writerInstance = ref<HanziWriter | null>(null);
  const writerTarget = ref<HTMLDivElement | null>(null);
  
  // This watch function creates/updates the writer when the kanji prop or the target div is ready
  watch([() => props.kanji, writerTarget], ([newKanji, targetEl]) => {
    if (!newKanji || !targetEl) {
      return; // Not ready yet
    }
  
    if (writerInstance.value) {
      // Writer already exists, just update the character
      writerInstance.value.setCharacter(newKanji);
    } else {
      // Create a new writer instance
      writerInstance.value = HanziWriter.create(targetEl, newKanji, {
        width: 150,
        height: 150,
        padding: 20,
        strokeColor: "#333",       // Dark stroke
        highlightColor: "#0ea5e9", // Blue highlight
        showOutline: true,
        showCharacter: true,
        strokeAnimationSpeed: 1.2,
      });
    }
  }, {
    immediate: true // Try to create as soon as possible
  });
  
  // --- Control Methods ---
  const animate = () => writerInstance.value?.animateCharacter();
  const quiz = () => {
    writerInstance.value?.quiz({
      onMistake: (strokeData) => console.log("Mistake", strokeData),
      onCorrectStroke: (strokeData) => console.log("Correct", strokeData),
      onComplete: () => console.log("Done!"),
    });
  };
  const reset = () => {
    writerInstance.value?.cancelQuiz();
    writerInstance.value?.showCharacter();
  };
  
  onUnmounted(() => {
    // Clean up the HanziWriter instance
    if (writerInstance.value) {
      if (writerTarget.value) {
        writerTarget.value.innerHTML = '';
      }
      writerInstance.value = null;
    }
  });
  </script>