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
      <button @click="reset">Reset (full fallback)</button>
      <button @click="toggleParticles">Toggle particles ({{ showParticles }})</button>
      <button @click="triggerFocusOnce">Focus main</button>
    </div>

    <div ref="containerRef" class="graph-container">
      <Graph2D
        v-if="graphReady"
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

<script setup lang="ts">
import { ref, reactive, onMounted, onBeforeUnmount, watch } from 'vue'
import Graph2D from '~/components/Graph2D.vue'
import radicalsRaw from '~/data/composition.json' // <-- your radical JSON (the format you posted)
import kanjilistRaw from '~/data/kanjilist.json' // optional, for jishoData
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

const kanjilist = (kanjilistRaw as unknown) as Array<{ k?: string; kunyomi?: string; meaning?: string; onyomi?: string[] }>
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

  // initial build: use BFS around root
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

  // build BFS subgraph around root
  const filteredRaw = buildSubgraphAround(root.value, radicalsRaw as any, depth.value, maxNodes.value)

  // create kanjiInfoMap from kanjilist for nicer labels
  const kanjiMap: Record<string, any> = {}
  for (const it of kanjilist) {
    if (it.k) {
      kanjiMap[String(it.k)] = {
        kunyomi: it.kunyomi ?? '',
        meaning: it.meaning ?? '',
        onyomi: it.onyomi ?? [],
      }
    }
  }

  const both = convertRadicalJson(filteredRaw, { kanjiInfoMap: kanjiMap })
  graphData.value = both
  // update kanjiInfo.jishoData if available
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
