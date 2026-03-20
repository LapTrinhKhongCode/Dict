<script setup lang="ts">
import { ref, shallowRef, onMounted, onUnmounted } from "vue";
import KanjiReadingsCard from "~/components/KanjiReadingsCard.vue";

// ✅ SỬA: Đã thêm useRouter trở lại, giữ nguyên useRoute
import { useRoute, useRouter } from "vue-router";

import compositionJson from "@/data/composition_filtered.json";
import kanjiListJson from "@/data/kanjilist_filtered.json";
import { validateKanji } from "@/utils/kanjiValidator";
import * as THREE from "three";
import Kanjistroke from "../../components/kanjistroke.vue";

type CompositionEntry = { in: string[]; out: string[] };
const composition = compositionJson as Record<string, CompositionEntry>;
type KanjiEntry = { k: string; r: string; m: string; g: number };
const kanjiList = kanjiListJson as KanjiEntry[];

// Map tra cứu onyomi
const onyomiMap = ref<Record<string, string[]>>({});
type GraphNode = {
  id: string;
  group?: string;
  x?: number;
  y?: number;
  z?: number;
};
type GraphLink = { source: any; target: any };

const route = useRoute();
const router = useRouter(); // ✅ ĐÃ THÊM LẠI

// --- THAY ĐỔI LỚN: Dùng ref nội bộ để quản lý kanji trung tâm ---
// Điều này ngăn component bị remount khi click
const centerKanji = ref(String(route.params.kanji ?? ""));
// -------------------------------------------------------------

// --- graph state ---
const ForceGraphComp = ref<any>(null);
const fgRef = ref<any>(null);
const graphData = shallowRef<{ nodes: GraphNode[]; links: GraphLink[] }>({
  nodes: [],
  links: [],
});
const mode = ref<"composition" | "onyomi">("composition");
const dimension = ref<"2d" | "3d">("2d");

// Dùng centerKanji.value để validate
const isValidKanji = validateKanji(centerKanji.value);
if (!isValidKanji) {
  throw createError({
    statusCode: 404,
    statusMessage: "Kanji Not Found",
    fatal: true,
  });
}

// Hàm build map tra cứu (chạy 1 lần)
function buildOnyomiMap() {
  console.time("buildOnyomiMap");
  const map: Record<string, string[]> = {};
  for (const entry of kanjiList) {
    const readings = entry.r
      .split(",")
      .map((r) => r.trim())
      .filter(Boolean);
    for (const reading of readings) {
      if (!map[reading]) {
        map[reading] = [];
      }
      map[reading].push(entry.k);
    }
  }
  onyomiMap.value = map;
  console.timeEnd("buildOnyomiMap");
}

async function setDimension(dim: "2d" | "3d") {
  dimension.value = dim;
  localStorage.setItem("graph-dimension", dim);
  const mod = await import("vue-force-graph");
  ForceGraphComp.value =
    dim === "2d" ? mod.VueForceGraph2D || mod.default : mod.VueForceGraph3D;

  // Chờ render xong rồi apply forces và reset camera
  setTimeout(() => {
    applyForces();
    // Nếu là 3D, reset camera về vị trí mặc định
    if (dim === "3d") {
      const fg = fgRef.value;
      if (fg && fg.cameraPosition) {
        fg.cameraPosition(
          { x: 0, y: 0, z: 100 },
          { x: 0, y: 0, z: 0 },
          1000
        );
        // Thử nhiều cách để truy cập controls
        setTimeout(() => {
          try {
            let controls = null;
            // Cách 1: Thử qua phương thức controls()
            if (typeof fg.controls === "function") {
              controls = fg.controls();
              console.log("Controls via method:", controls);
            }
            // Cách 3: Thử tìm trong instance của fg
            if (!controls && (fg as any)._controls) {
              controls = (fg as any)._controls;
              console.log("Controls via instance:", controls);
            }
            // Áp dụng auto-rotate nếu tìm thấy controls
            if (controls && typeof controls.autoRotate !== "undefined") {
              controls.autoRotate = true;
              controls.autoRotateSpeed = 50.0;
              console.log("Auto-rotate enabled successfully");
            } else {
              console.warn("Cannot find controls with autoRotate property");
              startManualRotation();
            }
          } catch (error) {
            console.error("Error accessing controls:", error);
          }
        }, 500);
      }
    }
  }, 300);
}

// Fallback: Tự implement auto-rotate nếu không tìm thấy controls
let rotationAnimationId: number | null = null;
function startManualRotation() {
  if (rotationAnimationId) {
    cancelAnimationFrame(rotationAnimationId);
  }
  const fg = fgRef.value;
  if (!fg || !fg.camera || !fg.scene) return;
  let angle = 0;
  const rotationSpeed = 0.003;
  function animate() {
    if (dimension.value !== "3d") {
      rotationAnimationId = null;
      return;
    }
    angle += rotationSpeed;
    // Cập nhật camera position để xoay
    const radius = 250;
    const x = Math.sin(angle) * radius;
    const z = Math.cos(angle) * radius;
    if (fg.cameraPosition) {
      fg.cameraPosition(
        { x, y: 0, z },
        { x: 0, y: 0, z: 0 },
        0 // không animate
      );
    }
    rotationAnimationId = requestAnimationFrame(animate);
  }
  rotationAnimationId = requestAnimationFrame(animate);
  console.log("Manual rotation started");
}
function stopManualRotation() {
  if (rotationAnimationId) {
    cancelAnimationFrame(rotationAnimationId);
    rotationAnimationId = null;
    console.log("Manual rotation stopped");
  }
}
// Cập nhật hàm toggleAutoRotate
function toggleAutoRotate(enabled: boolean) {
  if (dimension.value !== "3d") return;
  const fg = fgRef.value;
  if (!fg) return;
  setTimeout(() => {
    try {
      // Thử dùng controls tiêu chuẩn trước
      const controls = fg.controls ? fg.controls() : null;
      if (controls && typeof controls.autoRotate !== "undefined") {
        controls.autoRotate = enabled;
        console.log("Standard auto-rotate:", enabled);
        return;
      }
      // Fallback sang manual rotation
      if (enabled) {
        startManualRotation();
      } else {
        stopManualRotation();
      }
    } catch (error) {
      console.error("Error toggling rotation:", error);
    }
  }, 100);
}
// Sửa hàm applyForces để thêm lực cho 3D
function applyForces() {
  const fg = fgRef.value;
  if (!fg?.d3Force) return;
  try {
    if (dimension.value === "3d") {
      // GIẢM khoảng cách link - node sẽ gần nhau hơn
      (fg.d3Force("link") as any)?.distance?.(25);
      // TĂNG lực đẩy - node sẽ xa nhau hơn nhưng cân bằng với khoảng cách link
      (fg.d3Force("charge") as any)?.strength?.(-200);
      // Thêm lực hấp dẫn để giữ nodes ở trung tâm
      (fg.d3Force("center") as any)?.strength?.(0.3);
    } else {
      // 2D: cũng có thể điều chỉnh tương tự
      (fg.d3Force("link") as any)?.distance?.(100);
      (fg.d3Force("charge") as any)?.strength?.(-300);
    }
    // Reset lại layout
    fg.d3ReheatSimulation();
  } catch (err) {
    console.warn("Force config error:", err);
  }
  try {
    if (dimension.value === "2d") {
      fg.centerAt(0, 0);
      fg.zoom(2, 500);
    }
    // Logic 3D camera đã được chuyển vào buildGraph
  } catch {}
}

// --- build graph ---
// Hàm buildGraph đã được tối ưu, giờ dùng centerKanji.value làm mặc định
function buildGraph(newKanji?: string, keepDimension = true) {
  const nodes: GraphNode[] = [];
  const links: GraphLink[] = [];
  // Sửa ở đây: Dùng state nội bộ centerKanji.value
  const center = newKanji ?? centerKanji.value;
  nodes.push({ id: center, group: "center" });
  if (mode.value === "composition" && composition[center]) {
    composition[center].in.forEach((n) => {
      nodes.push({ id: n, group: "in" });
      links.push({ source: n, target: center });
    });
    composition[center].out.forEach((n) => {
      nodes.push({ id: n, group: "out" });
      links.push({ source: center, target: n });
    });
  }
  // Logic Onyomi đã được tối ưu
  if (mode.value === "onyomi") {
    const centerEntry = kanjiList.find((k) => k.k === center);
    console.debug("buildGraph - onyomi center:", center, "entry:", centerEntry);
    if (centerEntry) {
      const readings = centerEntry.r
        .split(",")
        .map((r) => r.trim())
        .filter(Boolean);
      console.debug("readings:", readings);
      const relatedKanji = new Set<string>();
      for (const reading of readings) {
        // Tra cứu map O(1)
        const matches = onyomiMap.value[reading];
        if (matches) {
          matches.forEach((k) => {
            if (k !== center) {
              relatedKanji.add(k);
            }
          });
        }
      }
      relatedKanji.forEach((k) => {
        nodes.push({ id: k, group: "onyomi" });
        links.push({ source: center, target: k });
      });
    }
  }
  // Loại trùng node
  const uniqueNodes = [...new Map(nodes.map((n) => [n.id, n])).values()];
  // Gán toàn bộ object -> reactivity đảm bảo
  graphData.value = { nodes: uniqueNodes, links };
  // Xóa tọa độ cũ cho 3D
  if (dimension.value === "3d") {
    graphData.value.nodes.forEach((n) => {
      n.x = n.y = n.z = undefined;
    });
  }
  // Logic reset camera được hợp nhất vào đây
  setTimeout(() => {
    const fg = fgRef.value;
    if (!fg) return;
    applyForces(); // Áp dụng lực cho cả 2D/3D
    // Reset camera 3D
    if (dimension.value === "3d") {
      try {
        fg.cameraPosition(
          { x: 0, y: 0, z: 250 },
          { x: 0, y: 0, z: 0 },
          1000
        );
      } catch (e) {
        console.warn("3D reset error:", e);
      }
    }
  }, 300);
  if (!keepDimension) {
    setDimension(dimension.value || "2d");
  }
}

function randomBlueToWhite(): string {
  const start = { r: 11, g: 109, b: 176 }; // #0B6DB0
  const end = { r: 255, g: 255, b: 255 }; // trắng
  const t = Math.random(); // 0 → 1
  const r = Math.round(start.r + (end.r - start.r) * t);
  const g = Math.round(start.g + (end.g - start.g) * t);
  const b = Math.round(start.b + (end.b - start.b) * t);
  return `rgb(${r},${g},${b})`;
}

//Vẽ node 2d
function drawNode(
  node: GraphNode & { color?: string },
  ctx: CanvasRenderingContext2D,
  globalScale: number
) {
  const center = node.group === "center";
  const r = center ? 26 : 18;
  const strokeWidth = center ? 1 : 1;
  const label = String(node.id ?? "");
  const fontSize = Math.max(10, (center ? 30 : 20) * (1 / globalScale));
  let fill: string;
  if (center) {
    fill = "#0B6DB0";
  } else {
    if (!node.color) {
      node.color = randomBlueToWhite(); // gán màu solid random
    }
    fill = node.color;
  }
  ctx.beginPath();
  ctx.arc(node.x ?? 0, node.y ?? 0, r, 0, 2 * Math.PI, false);
  ctx.fillStyle = fill;
  ctx.fill();
  ctx.lineWidth = strokeWidth;
  ctx.strokeStyle = "#000"; // đen nhạt, 30% opacity
  ctx.stroke();
  ctx.textAlign = "center";
  ctx.textBaseline = "middle";
  ctx.fillStyle = "#000"; //Màu chữ
  ctx.font = `bold ${fontSize}px -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Noto Sans", "Helvetica Neue", Arial`;
  ctx.fillText(label, node.x ?? 0, node.y ?? 0);
}

//Vẽ node 3d
function makeNodeText(node: GraphNode & { color?: string }) {
  const group = new THREE.Group();
  const radius = node.group === "center" ? 10 : 5;
  // Sphere
  const geometry = new THREE.SphereGeometry(radius, 32, 32);
  const material = new THREE.MeshPhongMaterial({
    color: node.group === "center" ? 0x0b6db0 : randomBlueToWhite(),
    shininess: 80,
    specular: 0xffffff,
  });
  const circle = new THREE.Mesh(geometry, material);
  // Glow
  const glowGeom = new THREE.SphereGeometry(radius * 1.25, 32, 32);
  const glowMat = new THREE.MeshBasicMaterial({
    color: node.group === "center" ? 0x66d7ff : 0xffffff,
    transparent: true,
    opacity: 0.08,
  });
  const glow = new THREE.Mesh(glowGeom, glowMat);
  // === Label cố định bằng canvas ===
  const canvas = document.createElement("canvas");
  const ctx = canvas.getContext("2d")!;
  const fontPx = node.group === "center" ? 400 : 350;
  const padding = 20;
  ctx.font = `${fontPx}px Arial`;
  const text = String(node.id);
  const metrics = ctx.measureText(text);
  const textW = Math.ceil(metrics.width);
  const textH = Math.ceil(fontPx * 1.2);
  canvas.width = textW + padding * 2;
  canvas.height = textH + padding * 2;
  // Vẽ lại chữ
  ctx.font = `${fontPx}px Arial`;
  ctx.textBaseline = "top";
  ctx.fillStyle = "#000000";
  ctx.fillText(text, padding, padding);
  const texture = new THREE.CanvasTexture(canvas);
  texture.needsUpdate = true;
  // Tạo plane từ texture
  const scaleFactor = 0.02 * (radius / 10); // hệ số tỉ lệ
  const w = canvas.width * scaleFactor;
  const h = canvas.height * scaleFactor;
  const labelPlane = new THREE.Mesh(
    new THREE.PlaneGeometry(w, h),
    new THREE.MeshBasicMaterial({
      map: texture,
      transparent: true,
      depthTest: false,
      depthWrite: false,
    })
  );
  // Luôn hướng về camera (billboard style)
  labelPlane.onBeforeRender = function (renderer, scene, camera) {
    this.quaternion.copy(camera.quaternion);
  };
  labelPlane.position.set(0, 0, 0);
  labelPlane.renderOrder = 100;
  // === Center node thì ưu tiên hiển thị ===
  if (node.group === "center") {
    circle.renderOrder = 10;
    material.depthTest = false;
    material.depthWrite = false;
  }
  group.add(glow);
  group.add(circle);
  group.add(labelPlane);
  return group;
}

function paintNodePointerArea(
  node: GraphNode,
  color: string,
  ctx: CanvasRenderingContext2D
) {
  const r = node.group === "center" ? 26 : 18;
  ctx.fillStyle = color;
  ctx.beginPath();
  ctx.arc(node.x ?? 0, node.y ?? 0, r + 6, 0, 2 * Math.PI, false);
  ctx.fill();
}

//Vẽ link
function drawLink(link: GraphLink, ctx: CanvasRenderingContext2D) {
  const s = link.source as any;
  const e = link.target as any;
  if (
    !s ||
    !e ||
    typeof s.x !== "number" ||
    typeof s.y !== "number" ||
    typeof e.x !== "number" ||
    typeof e.y !== "number"
  )
    return;
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
  const TIP_MARGIN = 1;
  const ARROW_LEN = 18;
  const ARROW_W = 12;
  const ang = Math.atan2(e.y - s.y, e.x - s.x);
  const ux = Math.cos(ang),
    uy = Math.sin(ang);
  const px = -uy,
    py = ux;
  const tipX = e.x - ux * (R + TIP_MARGIN);
  const tipY = e.y - uy * (R + TIP_MARGIN);
  const leftX = tipX - ux * ARROW_LEN + px * (ARROW_W / 2);
  const leftY = tipY - uy * ARROW_LEN + py * (ARROW_W / 2);
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

// --- MỚI: Hàm xử lý khi user bấm nút Back/Forward ---
function handlePopState(event: PopStateEvent) {
  // Lấy kanji từ URL mới
  const newKanjiFromUrl = window.location.pathname.split("/").pop();
  if (newKanjiFromUrl) {
    const decodedKanji = decodeURIComponent(newKanjiFromUrl);
    // Chỉ update nếu nó thực sự khác với state hiện tại
    if (decodedKanji !== centerKanji.value) {
      console.log("PopState event: changing kanji to", decodedKanji);
      centerKanji.value = decodedKanji; // Cập nhật state
      buildGraph(decodedKanji); // Build lại graph cho kanji này
    }
  }
}
// --- onMounted đã được cập nhật ---
onMounted(async () => {
  console.log("Component mounted");
  // Build map tra cứu
  buildOnyomiMap();
  const savedDim =
    (localStorage.getItem("graph-dimension") as "2d" | "3d") || "2d";
  dimension.value = savedDim;
  await setDimension(savedDim);
  buildGraph(); // Build lần đầu với centerKanji.value
  // --- MỚI: Lắng nghe sự kiện back/forward của trình duyệt ---
  window.addEventListener("popstate", handlePopState);
  setTimeout(() => {
    console.log("fgRef value:", fgRef.value);
  }, 300);
});
// --- MỚI: Thêm onUnmounted để dọn dẹp listener ---
onUnmounted(() => {
  window.removeEventListener("popstate", handlePopState);
  stopManualRotation(); // Cũng nên dọn dẹp animation
});
// -------------------------------------------------

// --- onNodeClick (GIỮ NGUYÊN CODE GỐC CỦA BẠN) ---
function onNodeClick(node: GraphNode | null) {
  if (!node?.id) return;
  // Nếu click vào node đang là trung tâm -> không làm gì
  if (node.id === centerKanji.value) {
    console.log("Clicked on the same center node. Ignoring.");
    return;
  }
  // Dùng setTimeout(0) để đảm bảo mượt
  setTimeout(() => {
    if (mode.value === "composition") {
      // 1. Cập nhật state nội bộ (reactive ref)
      centerKanji.value = node.id!;
      // 2. Cập nhật URL (KHÔNG reload trang)
      // Dùng pushState để user có thể "back" về kanji trước
      const newUrl = `/kanji/${encodeURIComponent(node.id!)}`;
      history.pushState({ kanji: node.id }, "", newUrl);
      // 3. Build lại graph (component không bị remount)
      buildGraph(node.id);
    } else {
      // Chế độ "onyomi"
      // Chỉ build lại graph, không đổi URL, không đổi state
      buildGraph(node.id);
    }
  }, 0);
}
</script>

<template>
  <div class="graph-page-container">
    <div class="header-section">
      <h1 class="main-title">Kanji Graph Explorer</h1>
      <div class="toggle-container">
        <!-- <div class="mode-toggle">
          <span class="toggle-label">Mode:</span>
          <button
            :class="{ active: mode === 'composition' }"
            @click="
              mode = 'composition';
              buildGraph();
            "
          >
            Composition
          </button>
          <button
            :class="{ active: mode === 'onyomi' }"
            @click="
              mode = 'onyomi';
              buildGraph();
            "
          >
            Onyomi
          </button>
        </div> -->
        <div class="dimension-toggle">
          <span class="toggle-label">View:</span>
          <button
            :class="{ active: dimension === '2d' }"
            @click="setDimension('2d')"
          >
            2D
          </button>
          <button
            :class="{ active: dimension === '3d' }"
            @click="setDimension('3d')"
          >
            3D
          </button>
        </div>
      </div>
    </div>
    <div class="main-content flex-container">
      <div class="stroke-panel">
        <div class="kanji-card">
          <h2 class="kanji-title">Kanji: {{ centerKanji }}</h2>
          <Kanjistroke :kanji="centerKanji" />
        </div>
      </div>
      <div class="graph-panel">
        <div class="graph-container">
          <client-only>
            <component
              v...for="mean in selectedKanji.meanlong.split('\n')"
              v-if="ForceGraphComp"
              :is="ForceGraphComp"
              ref="fgRef"
              :graph-data="graphData"
              :nodeCanvasObject="dimension === '2d' ? drawNode : undefined"
              :nodePointerAreaPaint="
                dimension === '2d' ? paintNodePointerArea : undefined
              "
              :linkWidth="1"
              :linkDirectionalParticles="2"
              :linkDirectionalParticleWidth="dimension === '2d' ? 5 : 1"
              :linkDirectionalParticleColor="() => 'white'"
              :linkDirectionalParticleSpeed="() => 0.005"
              :linkCanvasObject="dimension === '2d' ? drawLink : undefined"
              :linkCanvasObjectMode="
                dimension === '2d' ? () => 'after' : undefined
              "
              :nodeThreeObject="dimension === '3d' ? makeNodeText : undefined"
              class="force-graph-component"
              @nodeClick="onNodeClick"
            />
            <div v-else class="loading-container">
              <div class="loading-spinner"></div>
              <p>Đang tải đồ thị...</p>
            </div>
          </client-only>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* ✅ THAY ĐỔI:
  - Tái cấu trúc lại toàn bộ CSS
  - Sửa các lỗi layout (220vh, 60vh)
  - Thêm style cho light mode (mặc định)
  - Bọc style cho dark mode (cũ) trong class .dark
*/

/* --- LIGHT MODE (MẶC ĐỊNH) --- */
.graph-page-container {
  width: 100%;
  min-height: 100vh; /* Đảm bảo cao ít nhất 1 màn hình */
  display: flex;
  flex-direction: column;
  background-color: #f9fafb; /* bg-gray-50 */
  color: #111827; /* text-gray-900 */
  box-sizing: border-box;
}
.header-section {
  padding: 0.5rem 1rem;
  border-bottom: 1px solid #e5e7eb; /* border-gray-200 */
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
}
.main-title {
  font-size: 1.8rem;
  margin: 0;
  color: #2563eb; /* primary-600 */
  font-weight: 600;
}
.toggle-container {
  display: flex;
  gap: 1.5rem;
}
.mode-toggle,
.dimension-toggle {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}
.toggle-label {
  font-size: 0.9rem;
  color: #4b5569; /* text-gray-600 */
}
.main-content {
  display: flex;
  gap: 1.5rem;
  padding: 1rem;
  box-sizing: border-box;
  flex: 1; /* ✅ THÊM: Để lấp đầy không gian còn lại */
}
.flex-container {
  display: flex;
  flex-direction: row;
  width: 100%;
  min-height: 600px; /* Giữ chiều cao tối thiểu */
  height: 100%; /* Lấp đầy .main-content */
}
.stroke-panel {
  flex: 0 0 320px; /* Chiều rộng cố định, ổn định */
  display: flex;
  flex-direction: column;
}
.kanji-card {
  background: #ffffff; /* bg-white */
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.05);
  border: 1px solid #e5e7eb; /* border-gray-200 */
  height: auto; /* Tự động co dãn */
  width: 100%; /* Vừa với panel */
  display: flex;
  flex-direction: column;
}
.kanji-title {
  font-size: 1.5rem;
  margin-top: 0;
  margin-bottom: 1rem;
  color: #2563eb; /* primary-600 */
  text-align: center;
}
.kanji-info {
  margin-top: auto;
  padding-top: 1rem;
  border-top: 1px solid #e5e7eb; /* border-gray-200 */
}
.graph-panel {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}
.graph-container {
  background: #ffffff; /* bg-white */
  border-radius: 12px;
  padding: 1rem;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.05);
  border: 1px solid #e5e7eb; /* border-gray-200 */
  flex: 1;
  position: relative;
  overflow: hidden;
  min-height: 600px; /* Giữ chiều cao tối thiểu */
}
.force-graph-component {
  position: absolute;
  align-items: start;
  justify-content: center;
  inset: 0;
  width: 100%;
  height: 100%;
  display: flex;
  border-radius: 8px;
}
button {
  background: #e5e7eb; /* bg-gray-200 */
  color: #374151; /* text-gray-700 */
  border: none;
  padding: 0.5rem 1rem;
  cursor: pointer;
  border-radius: 6px;
  font-size: 0.9rem;
  transition: all 0.2s ease;
}
button:hover {
  background: #d1d5db; /* bg-gray-300 */
}
button.active {
  background: #2563eb; /* primary-600 */
  color: #fff;
}
.loading-container {
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  height: 100%;
  gap: 1rem;
}
.loading-spinner {
  width: 40px;
  height: 40px;
  border: 4px solid #e5e7eb; /* border-gray-200 */
  border-top: 4px solid #2563eb; /* primary-600 */
  border-radius: 50%;
  animation: spin 1s linear infinite;
}
@keyframes spin {
  0% {
    transform: rotate(0deg);
  }
  100% {
    transform: rotate(360deg);
  }
}

/* --- DARK MODE --- */
.dark .graph-page-container {
  background-color: #0f172a;
  color: #e2e8f0;
}
.dark .header-section {
  border-bottom: 1px solid #334155;
}
.dark .main-title {
  color: #7dd3fc;
}
.dark .toggle-label {
  color: #94a3b8; /* ✅ SỬA LỖI TYPO: 'source:' -> 'color:' */
}
.dark .kanji-card {
  background: #1e293b;
  border-color: transparent;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
}
.dark .kanji-title {
  color: #7dd3fc;
}
.dark .kanji-info {
  border-top: 1px solid #334155;
}
.dark .graph-container {
  background: #1e293b;
  border-color: transparent;
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
}
.dark button {
  background: #334155;
  color: #cfe8ff;
}
.dark button:hover {
  background: #475569;
}
.dark button.active {
  background: #0ea5e9;
  color: #fff;
}
.dark .loading-spinner {
  border: 4px solid #334155;
  border-top: 4px solid #0ea5e9;
}

/* --- Responsive (Đã sửa lỗi) --- */
@media (max-width: 968px) {
  .main-content {
    flex-direction: column;
  }
  .stroke-panel {
    flex: 0 0 auto;
    max-width: 100%;
  }
  .header-section {
    flex-direction: column;
    align-items: flex-start;
    gap: 1rem;
  }
  .toggle-container {
    width: 100%;
    justify-content: space-between;
  }
}
@media (max-width: 640px) {
  .toggle-container {
    flex-direction: column;
    gap: 0.5rem;
  }
  .mode-toggle,
  .dimension-toggle {
    width: 100%;
    justify-content: space-between;
  }
  .graph-page-container {
    padding: 0.5rem;
  }
  .main-content {
    padding: 0.5rem;
  }
  .kanji-card {
    padding: 1rem;
  }
}
/* ❌ ĐÃ XÓA: Media query 'height: 300vh' không cần thiết */
</style>