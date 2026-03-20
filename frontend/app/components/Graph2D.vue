<template>
  <client-only>
    <VueForceGraph2D
      ref="fgRef"
      :graphData="data"
      :width="props.bounds.width"
      :height="props.bounds.height"
      :backgroundColor="backgroundColor"
      :warmupTicks="10"
      :nodeLabel="nodeLabelFn"
      :nodeCanvasObject="paintNode"
      :nodePointerAreaPaint="nodePointerAreaPaint"
      :linkCanvasObject="linkCanvasObject"
      :linkColor="linkColor"
      :linkDirectionalArrowLength="4"
      :linkDirectionalArrowColor="
        () => (colorMode.value === 'dark' ? '#ffffff' : '#000000')
      "
      :linkDirectionalArrowRelPos="linkArrowRelPos"
      :linkDirectionalParticles="3"
      :linkDirectionalParticleSpeed="0.004"
      :linkDirectionalParticleWidth="() => (props.showParticles ? 2 : 0)"
      :linkDirectionalParticleColor="
        () => (colorMode.value === 'dark' ? '#ffffff' : '#000000')
      "
      :enablePointerInteraction="true"
      :enableNodeDrag="true"
      :enableZoomInteraction="true"
      :onNodeClick="handleClick"
      :onNodeHover="handleNodeHover"
    />
  </client-only>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import { useRouter, useColorMode } from '#imports';
import { VueForceGraph2D } from 'vue-force-graph';
import type {
  NodeObject,
  LinkObject,
  GraphData,
  BothGraphData,
} from '~/types/graph';
import type { KanjiInfo } from '~/types/kanji';
import kanjilistRaw from '@/../data/kanjilist.json'; // optional mapping for jishoData

// Props
const props = defineProps<{
  kanjiInfo: KanjiInfo;
  graphData: BothGraphData | null;
  showOutLinks: boolean;
  showParticles: boolean;
  triggerFocus: number;
  bounds: { width: number; height: number };
}>();

// composables & refs
const router = useRouter();
const colorMode = useColorMode();
const fgRef = ref<{
  zoomToFit?: (
    ms?: number,
    paddingPx?: number,
    filter?: (n: NodeObject) => boolean
  ) => void;
} | null>(null);

const data = ref<GraphData>({ nodes: [], links: [] });
const hoverNode = ref<NodeObject | null>(null);

// build joyo/jinmeiyo lists if kanjilist exists
type KanjiListItem = { g?: number; k?: string };
const kanjilist = kanjilistRaw as unknown as KanjiListItem[];
const joyoList = kanjilist.filter((el) => el.g === 1).map((el) => String(el.k));
const jinmeiyoList = kanjilist
  .filter((el) => el.g === 2)
  .map((el) => String(el.k));

// sync props.graphData -> data
watch(
  () => [props.graphData, props.showOutLinks],
  () => {
    data.value = props.showOutLinks
      ? props.graphData?.withOutLinks ?? { nodes: [], links: [] }
      : props.graphData?.noOutLinks ?? { nodes: [], links: [] };
  },
  { immediate: true }
);

// watch for triggerFocus or data change to zoomToFit
watch(
  () => [
    data.value.nodes.length,
    props.kanjiInfo?.id,
    props.triggerFocus,
    props.bounds?.width,
  ],
  () => {
    if (props.kanjiInfo?.id && data.value.nodes.length > 0) {
      setTimeout(
        () => fgRef.value?.zoomToFit?.(1000, props.bounds.width * 0.1),
        100
      );
    }
  }
);

// handlers
function handleClick(node: NodeObject) {
  void router.push(`/${node.id}`);
}
function handleNodeHover(node: NodeObject | null, _prev?: NodeObject | null) {
  hoverNode.value = node;
}

// node label
const nodeLabelFn = (n: NodeObject) =>
  `${(n.data as any)?.jishoData?.kunyomi ?? ''}<br/>${
    (n.data as any)?.jishoData?.meaning ?? ''
  }`;

// paint node
function paintNode(node: NodeObject, ctx: CanvasRenderingContext2D) {
  const label = String(node.id);
  const fontSize = 6;
  ctx.font = `${fontSize}px Sans-Serif`;
  const textWidth = ctx.measureText(label).width;
  const bckgDimensions = [textWidth, fontSize].map((n) => n + fontSize * 0.2);

  // ✅ THAY ĐỔI: Logic màu sắc cho cả 2 chế độ
  let color: string;
  const isDark = colorMode.value === 'dark';

  if (props.kanjiInfo?.id === node.id || node.id === hoverNode.value?.id) {
    color = '#2B99CF'; // Accent color (hoạt động tốt trên cả 2 nền)
  } else if (joyoList.includes(String(node.id))) {
    color = isDark ? '#80c2e2' : '#475569'; // Sáng (dark mode) / Tối (light mode)
  } else if (jinmeiyoList.includes(String(node.id))) {
    color = isDark ? '#d5ebf5' : '#64748b'; // Rất sáng (dark) / Tối vừa (light)
  } else {
    color = isDark ? '#fff' : '#334155'; // Trắng (dark) / Tối nhất (light)
  }
  // (Màu viền giữ nguyên màu đen, nổi bật trên các màu fill)
  const radius = (bckgDimensions[1] / 2) * 1.5;

  if (node.x != null && node.y != null) {
    ctx.beginPath();
    ctx.arc(node.x, node.y, radius * 1.1, 0, 2 * Math.PI, false);
    ctx.fillStyle = '#000000'; // Viền đen
    ctx.fill();

    ctx.beginPath();
    ctx.arc(node.x, node.y, radius, 0, 2 * Math.PI, false);
    ctx.fillStyle = color; // Fill (đã tính toán ở trên)
    ctx.fill();

    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';

    // ✅ THAY ĐỔI: Màu chữ dựa trên màu nền
    // Nếu là màu accent, HOẶC đang ở light mode (nền tối), dùng chữ trắng
    if (color === '#2B99CF' || !isDark) {
      ctx.fillStyle = 'white';
    } else {
      // Ngược lại (dark mode, nền sáng), dùng chữ đen
      ctx.fillStyle = 'black';
    }
    ctx.fillText(label, node.x, node.y);
  }
}

// node pointer hit area
function nodePointerAreaPaint(
  node: NodeObject,
  color: string,
  ctx: CanvasRenderingContext2D
) {
  const label = String(node.id);
  const fontSize = 6;
  ctx.font = `${fontSize}px Sans-Serif`;
  const textWidth = ctx.measureText(label).width;
  const bckgDimensions = [textWidth, fontSize].map((n) => n + fontSize * 0.2);
  const radius = (bckgDimensions[1] / 2) * 1.5;

  if (node.x != null && node.y != null) {
    ctx.beginPath();
    ctx.arc(node.x, node.y, radius, 0, 2 * Math.PI, false);
    ctx.fillStyle = color;
    ctx.fill();
  }
}

// same onyomi function (safe)
function sameOn(kanji1: string, kanji2: string) {
  const k1 = data.value.nodes.find((o) => String(o.id) === String(kanji1));
  const k2 = data.value.nodes.find((o) => String(o.id) === String(kanji2));
  const on1 = (k1?.data as any)?.jishoData?.onyomi ?? [];
  const on2 = (k2?.data as any)?.jishoData?.onyomi ?? [];
  if (!Array.isArray(on1) || !Array.isArray(on2)) return '';
  return on1.filter((v: string) => on2.includes(v)).join(', ');
}

// link canvas
function linkCanvasObject(link: LinkObject, ctx: CanvasRenderingContext2D) {
  const s = link.source as NodeObject | string;
  const t = link.target as NodeObject | string;
  if (
    typeof s === 'object' &&
    typeof t === 'object' &&
    s.x != null &&
    s.y != null &&
    t.x != null &&
    t.y != null
  ) {
    const sx = s.x;
    const sy = s.y;
    const tx = t.x;
    const ty = t.y;
    const midX = (sx + tx) / 2;
    const midY = (sy + ty) / 2;
    const linkText = sameOn(
      String((s as NodeObject).id),
      String((t as NodeObject).id)
    );

    ctx.beginPath();
    ctx.moveTo(sx, sy);
    ctx.lineTo(tx, ty);
    ctx.lineWidth = 0.25;
    // ✅ THAY ĐỔI: Dùng hàm linkColor() nhất quán
    ctx.strokeStyle = linkColor();
    ctx.stroke();

    if (linkText) {
      const fontSize = 4;
      ctx.font = `${fontSize}px Sans-Serif`;
      ctx.save();
      ctx.translate(midX, midY);
      ctx.textAlign = 'center';
      ctx.textBaseline = 'middle';
      // ✅ THAY ĐỔI: Dùng hàm linkColor() nhất quán
      ctx.fillStyle = linkColor();
      ctx.fillText(linkText, 0, 0);
      ctx.restore();
    }
  }
}

function linkArrowRelPos({ source, target }: { source: any; target: any }) {
  if (
    typeof source === 'object' &&
    typeof target === 'object' &&
    source.x != null &&
    target.x != null &&
    source.y != null &&
    target.y != null
  ) {
    const dx = target.x - source.x;
    const dy = target.y - source.y;
    const linkLength = Math.hypot(dx, dy);
    return (linkLength - 3) / linkLength;
  }
  return 0.8;
}

// helpers
// (Hàm này đã tốt, đọc từ CSS variable)
const backgroundColor = computed(() => 'var(--color-background)');

// (Hàm này đã tốt, đọc từ CSS variable)
const linkColor = () => {
  if (typeof document === 'undefined')
    return colorMode.value === 'dark' ? '#ffffff' : '#000000';
  return (
    getComputedStyle(document.body).getPropertyValue('--color-foreground') ||
    (colorMode.value === 'dark' ? '#ffffff' : '#000000')
  );
};
</script>

<style scoped>
/* ensure canvas region uses parent's size */
:host,
div {
  width: 100%;
  height: 100%;
}
</style>