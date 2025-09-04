<template>
  <div class="graph-page">
    <h2 class="title">Kanji: {{ kanji }}</h2>

    <!-- Toggle button -->
    <div class="toggle">
      <button
        :class="{ active: mode === 'composition' }"
        @click="mode = 'composition'; buildGraph()"
      >
        Composition
      </button>
      <button
        :class="{ active: mode === 'onyomi' }"
        @click="mode = 'onyomi'; buildGraph()"
      >
        Onyomi
      </button>
    </div>

    <client-only>
      <component
        v-if="ForceGraphComp"
        :is="ForceGraphComp"
        ref="fgRef"
        :graph-data="graphData"
        :nodeCanvasObject="drawNode"
        :nodePointerAreaPaint="paintNodePointerArea"
        :linkWidth="0"
        :linkDirectionalParticles="2"
        :linkDirectionalParticleWidth="3"
        :linkDirectionalParticleColor="()=>'white'"
        :linkDirectionalParticleSpeed="() => 0.004"
        :linkCanvasObject="drawLink"
        :linkCanvasObjectMode="()=>'after'"
        style="width: 100%; height: 600px; display:block;"
        @nodeClick="onNodeClick"
      />
      <div v-else class="loading">Đang tải đồ thị...</div>
    </client-only>
  </div>
</template>

<script setup lang="ts">
import { ref, shallowRef, onMounted } from "vue";
import { useRoute, useRouter } from "vue-router";
import compositionJson from "@/data/composition.json";
import kanjiListJson from "@/data/kanjilist.json"; // thêm file này

type CompositionEntry = { in: string[]; out: string[] };
const composition = compositionJson as Record<string, CompositionEntry>;

type KanjiEntry = { k: string; r: string; m: string; g: number };
const kanjiList = kanjiListJson as KanjiEntry[];

type GraphNode = { id: string; group?: string; x?: number; y?: number; z?: number };
type GraphLink = { source: any; target: any };

const route = useRoute();
const router = useRouter();
const kanji = String(route.params.kanji ?? "");

// --- graph state ---
const ForceGraphComp = ref<any>(null);
const fgRef = ref<any>(null);
const graphData = shallowRef<{ nodes: GraphNode[]; links: GraphLink[] }>({
  nodes: [],
  links: [],
});

const mode = ref<"composition" | "onyomi">("composition");

// --- build graph ---
function buildGraph(newKanji?:
    /// <reference types="d:/DUT/Dict/frontend/node_modules/.vue-global-types/vue_3.5_0.d.ts" />
    string, p0?:
    /// <reference types="d:/DUT/Dict/frontend/node_modules/.vue-global-types/vue_3.5_0.d.ts" />
    string) {
  const nodes: GraphNode[] = [];
  const links: GraphLink[] = [];

  nodes.push({ id: kanji, group: "center" });

  if (mode.value === "composition" && composition[kanji]) {
    composition[kanji].in.forEach((n) => {
      nodes.push({ id: n, group: "in" });
      links.push({ source: n, target: kanji });
    });

    composition[kanji].out.forEach((n) => {
      nodes.push({ id: n, group: "out" });
      links.push({ source: kanji, target: n });
    });
  }

  if (mode.value === "onyomi") {
    const centerEntry = kanjiList.find((k) => k.k === kanji);
    if (centerEntry) {
      const readings = centerEntry.r
        .split(",")
        .map((r) => r.trim())
        .filter(Boolean);

      kanjiList.forEach((entry) => {
        if (entry.k === kanji) return;
        const entryReadings = entry.r.split(",").map((r) => r.trim());
        // check overlap
        const overlap = readings.some((r) => entryReadings.includes(r));
        if (overlap) {
          nodes.push({ id: entry.k, group: "onyomi" });
          links.push({ source: kanji, target: entry.k });
        }
      });
    }
  }

  const uniqueNodes = [...new Map(nodes.map((n) => [n.id, n])).values()];
  graphData.value = { nodes: uniqueNodes, links };
}

// --- node drawing (giữ nguyên code bạn có) ---
function randomGradient(ctx: CanvasRenderingContext2D, x: number, y: number, r: number) {
  const angle = Math.random() * 2 * Math.PI;
  const dx = Math.cos(angle) * r;
  const dy = Math.sin(angle) * r;
  const grad = ctx.createLinearGradient(x - dx, y - dy, x + dx, y + dy);
  grad.addColorStop(0, "#0B6DB0");
  grad.addColorStop(1, "#FFFFFF");
  return grad;
}

function drawNode(
  node: GraphNode & { gradient?: CanvasGradient },
  ctx: CanvasRenderingContext2D,
  globalScale: number
) {
  const center = node.group === "center";
  const r = center ? 26 : 18;
  const strokeWidth = center ? 4 : 2;
  const label = String(node.id ?? "");
  const fontSize = Math.max(10, (center ? 16 : 12) * (1 / globalScale));

  let fill: CanvasGradient | string;
  if (center) {
    fill = "#0B6DB0";
  } else {
    if (!node.gradient) {
      node.gradient = randomGradient(ctx, node.x ?? 0, node.y ?? 0, r);
    }
    fill = node.gradient;
  }

  ctx.beginPath();
  ctx.arc(node.x ?? 0, node.y ?? 0, r, 0, 2 * Math.PI, false);
  ctx.fillStyle = fill;
  ctx.fill();

  ctx.lineWidth = strokeWidth;
  ctx.strokeStyle = "#000";
  ctx.stroke();

  ctx.textAlign = "center";
  ctx.textBaseline = "middle";
  ctx.fillStyle = center ? "#fff" : "#0B2A39";
  ctx.font = `bold ${fontSize}px -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Noto Sans", "Helvetica Neue", Arial`;
  ctx.fillText(label, node.x ?? 0, node.y ?? 0);
}

function paintNodePointerArea(node: GraphNode, color: string, ctx: CanvasRenderingContext2D) {
  const r = node.group === "center" ? 26 : 18;
  ctx.fillStyle = color;
  ctx.beginPath();
  ctx.arc(node.x ?? 0, node.y ?? 0, r + 6, 0, 2 * Math.PI, false);
  ctx.fill();
}

// --- link drawing (giữ nguyên code bạn có) ---
function drawLink(link: GraphLink, ctx: CanvasRenderingContext2D) {
  const s = link.source as any;
  const e = link.target as any;
  if (!s || !e || typeof s.x !== "number" || typeof s.y !== "number" || typeof e.x !== "number" || typeof e.y !== "number") return;

  const grad = ctx.createLinearGradient(s.x, s.y, e.x, e.y);
  grad.addColorStop(0, "white");
  grad.addColorStop(1, "rgba(102,176,224,0.8)");

  ctx.strokeStyle = grad;
  ctx.lineWidth = 1.6;
  ctx.beginPath();
  ctx.moveTo(s.x, s.y);
  ctx.lineTo(e.x, e.y);
  ctx.stroke();

  const R = e.group === "center" ? 26 : 18;
  const TIP_MARGIN = 2;
  const ARROW_LEN = 16;
  const ARROW_W = 12;

  const ang = Math.atan2(e.y - s.y, e.x - s.x);
  const ux = Math.cos(ang), uy = Math.sin(ang);
  const px = -uy, py = ux;

  const tipX = e.x - ux * (R + TIP_MARGIN);
  const tipY = e.y - uy * (R + TIP_MARGIN);

  const leftX  = tipX - ux * ARROW_LEN + px * (ARROW_W / 2);
  const leftY  = tipY - uy * ARROW_LEN + py * (ARROW_W / 2);
  const rightX = tipX - ux * ARROW_LEN - px * (ARROW_W / 2);
  const rightY = tipY - uy * ARROW_LEN - py * (ARROW_W / 2);

  const baseX = tipX - ux * (ARROW_LEN * 0.6);
  const baseY = tipY - uy * (ARROW_LEN * 0.6);

  ctx.beginPath();
  ctx.moveTo(tipX, tipY);
  ctx.lineTo(leftX, leftY);
  ctx.lineTo(baseX, baseY);
  ctx.lineTo(rightX, rightY);
  ctx.closePath();

  ctx.fillStyle = "white";
  ctx.fill();
  ctx.strokeStyle = grad;
  ctx.lineWidth = 1.2;
  ctx.stroke();
}

// --- mount ---
onMounted(async () => {
  const mod = await import("vue-force-graph");
  ForceGraphComp.value = mod.VueForceGraph2D || mod.default;

  buildGraph(); // build graph lần đầu

  setTimeout(() => {
    const fg = fgRef.value;
    if (fg?.d3Force) {
      try {
        (fg.d3Force("link") as any)?.distance?.(110);
        (fg.d3Force("charge") as any)?.strength?.(-350);
      } catch {}
      try {
        fg.centerAt(0, 0, 500);
      } catch {}
    }
  }, 300);
});

function onNodeClick(node: GraphNode | null) {
  if (!node?.id) return;

  if (mode.value === "composition") {
    // điều hướng như cũ
    router.push(`/kanji/${encodeURIComponent(node.id)}`).catch(() => {});
  } else {
    // đang ở chế độ onyomi → chỉ đổi center node tại chỗ
    const newKanji = node.id;
    buildGraph(newKanji, "onyomi");
  }
}
</script>

<style scoped>
.graph-page {
  display: flex;
  flex-direction: column;
  align-items: center;
  width: 100%;
  padding: 1rem;
  background: #0f1720;
  min-height: 100vh;
  box-sizing: border-box;
}
.title {
  color: #cfe8ff;
  font-size: 1.25rem;
  margin-bottom: 0.5rem;
}
.loading {
  color: #bbb;
  padding: 1rem;
}
.toggle {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 0.5rem;
}
.toggle button {
  background: #1e293b;
  color: #cfe8ff;
  border: 1px solid #334155;
  padding: 0.3rem 0.7rem;
  cursor: pointer;
  border-radius: 6px;
}
.toggle button.active {
  background: #0b6db0;
  color: #fff;
}
</style>
