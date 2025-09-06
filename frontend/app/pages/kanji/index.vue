<<<<<<< HEAD
<script setup>
import { ref, computed, onMounted } from "vue";
import Papa from "papaparse";

const kanjiList = ref([]);
const itemsPerPage = ref(30);
const currentPage = ref(1);

const wordTypes = ["Từ vựng", "Ngữ pháp", "Hán tự"];
const levels = ["N5", "N4", "N3", "N2", "N1"];

const selectedType = ref("Hán tự");
const selectedLevel = ref("N5");
const router = useRouter();
const isSelectedMeaning = ref(true)

onMounted(async () => {
  const response = await fetch("/kanji.csv");
  const csvText = await response.text();

  const result = Papa.parse(csvText, {
    header: true,
    skipEmptyLines: true,
  });

  kanjiList.value = result.data.map((row) => {
    const firstMeaning = row.Meaning.split(",")[0].trim();

    return {
      id: row.Id,
      char: row.Character,
      jlpt: row.JlptLevel,
      mean: firstMeaning,
      strokes: row.StrokeCount,
      freq: row.Freq,
      // onyomi: row.AllRebs?.split("|")[0]?.trim() || "",
      // kunyomi: row.AllRebs?.split("|")[1]?.trim() || "",
    };
  });
});

// --- FILTERED DATA THEO LEVEL ---
const filteredList = computed(() => {
  return kanjiList.value.filter((k) => k.jlpt === selectedLevel.value);
});

// --- PAGINATION ---
const totalPages = computed(() =>
  Math.ceil(filteredList.value.length / itemsPerPage.value)
);

const paginatedList = computed(() => {
  const start = (currentPage.value - 1) * itemsPerPage.value;
  return filteredList.value.slice(start, start + itemsPerPage.value);
});
const pageNumbers = computed(() => {
  const pages = [];
  const start = Math.max(1, currentPage.value - 1);
  const end = Math.min(totalPages.value, start + 2);

  for (let i = start; i <= end; i++) {
    pages.push(i);
  }
  return pages;
});



function goToPage(page) {
  if (page >= 1 && page <= totalPages.value) {
    currentPage.value = page;
  }
}

const goToKanji = (kanji) => {
  router.push(`/kanji/${kanji}`);
};


watch(selectedLevel, () => {
  currentPage.value = 1;
});
</script>

<template>
  <div class="min-h-screen bg-[#0f172a] text-white flex">
    <!-- Sidebar -->
    <aside class="w-64 bg-[#1e293b] p-5 space-y-8">


      <div>
        <h2 class="text-lg font-bold mb-3">Chọn cấp độ</h2>
        <div class="space-y-3">
          <label
            v-for="lv in levels"
            :key="lv"
            class="flex items-center space-x-2 cursor-pointer"
          >
            <input
              type="radio"
              :value="lv"
              v-model="selectedLevel"
              class="accent-blue-500"
            />
            <span>{{ lv }}</span>
          </label>
        </div>
      </div>
    </aside>

    <!-- Main Content -->
    <main class="flex-1 p-6">
      <div class="flex justify-between items-center mb-6">
        <h1 class="text-xl font-bold">
          Hán tự {{ selectedLevel }}
        </h1>
        <div class="space-x-6 text-sm">
          <!-- <label class="inline-flex items-center space-x-2">
            <input type="checkbox" checked class="accent-blue-500" />
            <span>Từ vựng</span>
          </label> -->
          <label class="inline-flex items-center space-x-2">
            <input type="checkbox" checked class="accent-blue-500 w-4 h-4" v-model="isSelectedMeaning"/>
            <span class= text-lg>Nghĩa</span>
          </label>
        </div>
      </div>

      <!-- Kanji Grid -->
      <div
        class="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6 gap-6"
      >
        <div @click="goToKanji(k.char)"
          v-for="k in paginatedList"
          :key="k.id"
          class="flex flex-col items-center h-24 bg-[#1e293b] p-4 rounded-lg hover:bg-[#334155] transition cursor-pointer"
          
        >
          <span  class="text-3xl text-blue-400 font-bold">{{ k.char }}</span>
          <span v-if="isSelectedMeaning" class="text-sm mt-2 text-gray-300">{{ k.mean }}</span>
          <!-- <span class="text-xs text-gray-400 mt-1">On: {{ k.onyomi }}</span>
          <span class="text-xs text-gray-400">Kun: {{ k.kunyomi }}</span> -->
        </div>
      </div>

      <!-- Pagination -->
      <!-- Pagination -->
<div class="flex justify-center mt-8 space-x-3" v-if="totalPages > 1">
  <!-- Prev -->
  <button
    @click="goToPage(currentPage - 1)"
    :disabled="currentPage === 1"
    class="w-9 h-9 flex items-center justify-center rounded-full bg-[#1e293b] hover:bg-[#334155] disabled:opacity-50"
  >
    «
  </button>

  <!-- Dynamic page numbers -->
  <button
    v-for="page in pageNumbers"
    :key="page"
    @click="goToPage(page)"
    class="w-9 h-9 flex items-center justify-center rounded-full"
    :class="page === currentPage
      ? 'bg-blue-500'
      : 'bg-[#1e293b] hover:bg-[#334155]'"
  >
    {{ page }}
  </button>

  <!-- Next -->
  <button
    @click="goToPage(currentPage + 1)"
    :disabled="currentPage === totalPages"
    class="w-9 h-9 flex items-center justify-center rounded-full bg-[#1e293b] hover:bg-[#334155] disabled:opacity-50"
  >
    »
  </button>
</div>

    </main>
  </div>
</template>
=======
<template>
  <div class="page">
    <h1>Kanji graph explorer</h1>

    <div class="controls">
      <label>
        Root kanji:
        <input v-model="root" placeholder="e.g. 日" />
      </label>

      <label>
        Depth:
        <input type="number" v-model.number="depth" min="0" max="6" />
      </label>

      <label>
        Max nodes:
        <input type="number" v-model.number="maxNodes" min="10" max="5000" />
      </label>

      <button @click="build">Build subgraph</button>
      <button @click="reset">Reset</button>
      <button @click="toggleParticles">Toggle particles ({{ showParticles }})</button>
      <button @click="triggerFocusOnce">Focus main</button>
    </div>

    <div ref="containerRef" class="graph-container">
      <Graph2D
        v-if="graphReady && graphData"
        :kanjiInfo="kanjiInfo"
        :graphData="graphData"
        :showOutLinks="showOutLinks"
        :showParticles="showParticles"
        :triggerFocus="triggerFocus"
        :bounds="bounds"
      />
      <div v-else class="loading">Preparing graph...</div>
    </div>
  </div>
</template>

<script lang="ts" setup>
/**
 * Minimal, valid Nuxt 3 page SFC using composition API.
 *
 * Requirements:
 * - ~/data/composition.json must exist (your radicals JSON)
 * - ~/utils/convert-radical-json.ts (convertRadicalJson, buildSubgraphAround) should be present
 * - ~/components/Graph2D.vue should exist (the component we created earlier)
 *
 * If any path not present, adjust imports or move files inside project.
 */

import { ref, reactive, onMounted, onBeforeUnmount, watch } from 'vue'
import Graph2D from '~/components/Graph2D.vue'
import radicalsRaw from '../../../data/composition.json' // <- ensure this file is at project_root/data/composition.json
import { convertRadicalJson, buildSubgraphAround } from '~/utils/convert-radical-json'
import type { BothGraphData } from '~/types/graph'
import type { KanjiInfo } from '~/types/kanji'

const containerRef = ref<HTMLElement | null>(null)
const bounds = reactive({ width: 1000, height: 600 })

const showOutLinks = ref(true)
const showParticles = ref(true)
const triggerFocus = ref(0)
const graphReady = ref(false)

const root = ref<string>(Object.keys(radicalsRaw)[0] ?? '')
const depth = ref<number>(2)
const maxNodes = ref<number>(800)

const kanjiInfo = reactive<KanjiInfo>({ id: root.value, jishoData: { kunyomi: '', meaning: '', onyomi: [] } })
let graphData = ref<BothGraphData | null>(null)
let ro: ResizeObserver | undefined

onMounted(() => {
  if (containerRef.value) {
    bounds.width = containerRef.value.clientWidth || 1000
    bounds.height = containerRef.value.clientHeight || 600
    ro = new ResizeObserver((entries) => {
      for (const e of entries) {
        const cr = e.contentRect
        bounds.width = Math.round(cr.width)
        bounds.height = Math.round(cr.height)
      }
    })
    ro.observe(containerRef.value)
  }
  void build()
})

onBeforeUnmount(() => {
  if (ro && containerRef.value) ro.unobserve(containerRef.value)
})

watch(root, (v) => {
  kanjiInfo.id = v
})

function toggleParticles() {
  showParticles.value = !showParticles.value
}
function triggerFocusOnce() {
  triggerFocus.value++
}
function reset() {
  showOutLinks.value = true
  void build()
}

async function build() {
  graphReady.value = false
  // BFS subgraph around root to avoid rendering whole huge graph
  const filteredRaw = buildSubgraphAround(root.value, radicalsRaw as any, depth.value, maxNodes.value)
  // build BothGraphData (no external kanji mapping here; Graph2D will display id)
  const both = convertRadicalJson(filteredRaw, { kanjiInfoMap: {}, limitNodes: maxNodes.value })
  graphData.value = both
  // update kanjiInfo.jishoData if present
  const mainNode = both.withOutLinks.nodes.find((n) => String(n.id) === String(root.value))
  if (mainNode?.data?.jishoData) kanjiInfo.jishoData = mainNode.data.jishoData as any
  graphReady.value = true
}
</script>

<style scoped>
.page {
  padding: 16px;
}
.controls {
  display: flex;
  gap: 8px;
  flex-wrap: wrap;
  margin-bottom: 12px;
  align-items: center;
}
.graph-container {
  width: 100%;
  height: 720px;
  border: 1px solid rgba(0,0,0,0.08);
  border-radius: 8px;
  overflow: hidden;
  position: relative;
}
.loading {
  display:flex;
  align-items:center;
  justify-content:center;
  height:100%;
  color:#666;
}
</style>
>>>>>>> 97f4f7d (add graph 2d)
