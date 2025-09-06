<script setup lang="ts">
import { ref, reactive, nextTick, onMounted } from "vue";
import { useRouter } from "vue-router";
import searchlist from "@/data/searchlist.json";
import Handwriting from "@/lib/handwriting";

const canvasRef = ref<HTMLCanvasElement | null>(null);
const canvas = ref<InstanceType<typeof Handwriting.Canvas> | null>(null);
const inputSuggestions = ref<string[]>([]);
const router = useRouter();
const isClient = ref(false);

const inputOptions = reactive({
  width: 220,
  height: 220,
  language: "ja",
  numOfWords: 1,
  numOfReturn: 64,
});

const inputCallback = (result: string[], err: string) => {
  if (err) return;
  const kanjiList = (searchlist as { k: string }[]).map(e => e.k);
  inputSuggestions.value = result.filter(entry => kanjiList.includes(entry)).slice(0, 2);
};

const eraseKanji = () => {
  if (canvas.value) canvas.value.erase();
  inputSuggestions.value = [];
};

const recognizeKanji = () => {
  if (!canvas.value) return;
  canvas.value.recognize(canvas.value.getTrace(), inputOptions, inputCallback);
};

const goToKanji = (kanji: string) => {
  eraseKanji();
  router.push(`/kanji/${kanji}`);
};

onMounted(async () => {
  isClient.value = true;
  await nextTick(); // đảm bảo DOM canvas render xong

  if (canvasRef.value) {
    canvas.value = new Handwriting.Canvas(canvasRef.value, "light");

    // Nếu Handwriting không tự handle, tự add event vẽ
    const c = canvasRef.value;
    const ctx = c.getContext("2d")!;
    let drawing = false;
      //  ctx.strokeStyle = "white"; // <- màu trắng
    // ctx.lineWidth = 2;         // có thể tăng/giảm độ dày nét
    // ctx.lineCap = "round";     // bo tròn đầu nét
    // ctx.lineJoin = "round";  

    const getPos = (e: MouseEvent | TouchEvent) => {
      const rect = c.getBoundingClientRect();
      if (e instanceof MouseEvent) return { x: e.clientX - rect.left, y: e.clientY - rect.top };
      const touch = (e as TouchEvent).touches[0];
      if (!touch) return { x: 0, y: 0 };
      return { x: touch.clientX - rect.left, y: touch.clientY - rect.top };
    };

    const start = (e: MouseEvent | TouchEvent) => {
       ctx.strokeStyle = "white"; 
      e.preventDefault();
      drawing = true;
      const pos = getPos(e);
      ctx.beginPath();
      ctx.moveTo(pos.x, pos.y);
    };
    const move = (e: MouseEvent | TouchEvent) => {
      if (!drawing) return;
      const pos = getPos(e);
      ctx.lineTo(pos.x, pos.y);
      ctx.stroke();
    };
    const end = () => { drawing = false; };

    c.addEventListener("mousedown", start);
    c.addEventListener("mousemove", move);
    c.addEventListener("mouseup", end);
    c.addEventListener("mouseleave", end);

    c.addEventListener("touchstart", start);
    c.addEventListener("touchmove", move);
    c.addEventListener("touchend", end);
  }
});
</script>

<template>
  <div class="relative w-[220px] h-[220px] mx-auto bg-background">
    <div class="absolute left-1/2 h-full border-l border-dashed border-slate-600/20 pointer-events-none z-10" />
    <div class="absolute top-1/2 w-full border-t border-dashed border-slate-600/20 pointer-events-none z-10" />

    <ClientOnly>
      <canvas
        ref="canvasRef"
        width="220"
        height="220"
        class="relative w-[220px] h-[220px] border border-light rounded-lg cursor-crosshair bg-muted"
      />
    </ClientOnly>

    <div class="h-10 w-full pt-2 flex items-center justify-between">
      <button class="w-8 h-8 flex items-center justify-center bg-red-500 text-white rounded" @click="eraseKanji">✖</button>

      <template v-for="(suggestion, index) in inputSuggestions" :key="index">
     <button
  class="w-8 h-8 flex items-center justify-center bg-transparent rounded hover:bg-gray-100"
  @click="() => goToKanji(suggestion)"
>
  {{ suggestion }}
</button>

      </template>

      <button class="w-8 h-8 flex items-center justify-center bg-blue-500 text-white rounded" @click="recognizeKanji">🔍</button>
    </div>
  </div>
</template>
