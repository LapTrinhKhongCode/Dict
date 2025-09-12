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
  width: 400,
  height: 220,
  language: "ja",
  numOfWords: 1,
  numOfReturn: 64,
});

const inputCallback = (result: string[], err: string) => {
  if (err) return;
  const kanjiList = (searchlist as { k: string }[]).map(e => e.k);
  inputSuggestions.value = result.filter(entry => kanjiList.includes(entry)).slice(0, 6);
};

const eraseKanji = () => {
  if (canvas.value) canvas.value.erase();
  inputSuggestions.value = [];
};

// Hàm phân cụm các nét vẽ thành các chữ riêng biệt
// Hàm phân cụm các nét vẽ thành các chữ riêng biệt
const clusterStrokes = (trace: number[][][]) => {
  if (trace.length <= 1) return trace.length;
  
  const clusters: number[][] = [];
  const strokeCenters: {x: number, y: number}[] = [];
  
  // Tính toán trung tâm của mỗi nét vẽ
  for (const stroke of trace) {
    let sumX = 0;
    let sumY = 0;
    let pointCount = 0;
    
    for (const point of stroke) {
      if (point && point.length >= 2) {
        sumX += point[0] || 0;
        sumY += point[1] || 0;
        pointCount++;
      }
    }
    
    if (pointCount > 0) {
      strokeCenters.push({
        x: sumX / pointCount,
        y: sumY / pointCount
      });
    } else {
      strokeCenters.push({ x: 0, y: 0 });
    }
  }
  
  // Phân cụm dựa trên khoảng cách giữa các nét vẽ
  for (let i = 0; i < strokeCenters.length; i++) {
    let addedToCluster = false;
    
    for (const cluster of clusters) {
      if (cluster.length === 0) continue;
      
      const lastStrokeIndex = cluster[cluster.length - 1];
      // Kiểm tra lastStrokeIndex có hợp lệ không
      if (lastStrokeIndex === undefined || lastStrokeIndex >= strokeCenters.length) continue;
      
      const lastCenter = strokeCenters[lastStrokeIndex];
      const currentCenter = strokeCenters[i];
      
      if (!lastCenter || !currentCenter) continue;
      
      const distance = Math.sqrt(
        Math.pow(currentCenter.x - lastCenter.x, 2) +
        Math.pow(currentCenter.y - lastCenter.y, 2)
      );
      
      // Nếu khoảng cách đủ gần, thêm vào cụm hiện tại
      if (distance < 100) {
        cluster.push(i);
        addedToCluster = true;
        break;
      }
    }
    
    // Nếu không thuộc cụm nào, tạo cụm mới
    if (!addedToCluster) {
      clusters.push([i]);
    }
  }
  
  return clusters.length;
};

const recognizeKanji = () => {
  if (!canvas.value) return;

  const trace = canvas.value.getTrace();
  
  if (!trace || trace.length === 0) return;
  
  // Sử dụng thuật toán phân cụm để xác định số chữ
  const estimatedWords = clusterStrokes(trace);
  
  // Gán vào inputOptions trước khi gửi
  inputOptions.numOfWords = estimatedWords;

  console.log("Estimated words:", estimatedWords);

  canvas.value.recognize(trace, inputOptions, inputCallback);
};

const goToKanji = (kanji: string) => {
  eraseKanji();
  router.push(`/kanji/${kanji}`);
};

onMounted(async () => {
  isClient.value = true;
  await nextTick();

  if (canvasRef.value) {
    canvas.value = new Handwriting.Canvas(canvasRef.value, "light");
    
    const c = canvasRef.value;
    const ctx = c.getContext("2d");
    
    if (!ctx) return;
    
    let drawing = false;
    
    const getPos = (e: MouseEvent | TouchEvent) => {
      const rect = c.getBoundingClientRect();
      let clientX: number = 0;
      let clientY: number = 0;

      if (e instanceof MouseEvent) {
        clientX = e.clientX;
        clientY = e.clientY;
      } else {
        const touch = e.touches[0];
        if (!touch) return { x: 0, y: 0 };
        clientX = touch.clientX;
        clientY = touch.clientY;
      }

      const scaleX = c.width / rect.width;
      const scaleY = c.height / rect.height;

      return {
        x: (clientX - rect.left) * scaleX,
        y: (clientY - rect.top) * scaleY,
      };
    };

    const start = (e: MouseEvent | TouchEvent) => {
      if (!ctx) return;
      
      ctx.strokeStyle = "white"; 
      e.preventDefault();
      drawing = true;
      const pos = getPos(e);
      ctx.beginPath();
      ctx.moveTo(pos.x, pos.y);
    };
    
    const move = (e: MouseEvent | TouchEvent) => {
      if (!drawing || !ctx) return;
      const pos = getPos(e);
      ctx.lineTo(pos.x, pos.y);
      ctx.stroke();
    };
    
    const end = () => { 
      drawing = false; 
    };

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
  <div class="relative w-[400px] h-[220px] mx-auto bg-background">
    <div class="absolute left-1/2 h-full border-l border-dashed border-slate-600/20 pointer-events-none z-10" />
    <div class="absolute top-1/2 w-full border-t border-dashed border-slate-600/20 pointer-events-none z-10" />

    <ClientOnly>
      <canvas
        ref="canvasRef"
        width="400"
        height="220"
        class="relative w-[400px] h-[220px] border border-light rounded-lg cursor-crosshair bg-muted"
      />
    </ClientOnly>

    <div class="h-10 w-full pt-2 flex items-center justify-between">
      <button class="w-8 h-8 flex items-center justify-center bg-red-500 text-white rounded" @click="eraseKanji">✖</button>

      <div class="flex flex-wrap justify-center gap-1">
        <template v-for="(suggestion, index) in inputSuggestions" :key="index">
          <button
            class="w-8 h-8 flex items-center justify-center bg-transparent rounded hover:bg-gray-100"
            @click="() => goToKanji(suggestion)"
          >
            {{ suggestion }}
          </button>
        </template>
      </div>

      <button class="w-8 h-8 flex items-center justify-center bg-blue-500 text-white rounded" @click="recognizeKanji">🔍</button>
    </div>
  </div>
</template>
