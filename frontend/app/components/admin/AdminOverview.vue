<template>
  <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6 mb-8">
    <h2 class="text-2xl font-bold text-gray-900 dark:text-white mb-6">Thống kê nhanh</h2>
    <div v-if="isLoadingStats" class="text-gray-500 dark:text-gray-400">Đang tải thống kê...</div>
    <div v-else-if="statsError" class="text-red-500 dark:text-red-400">{{ statsError }}</div>
    <div v-else>
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
        <div class="bg-gray-100 dark:bg-gray-700 rounded-lg p-4">
          <div class="text-gray-600 dark:text-gray-400 text-sm mb-1">Tổng số người dùng</div>
          <div class="text-2xl font-bold text-gray-900 dark:text-white">{{ statistics?.totalUsers || 0 }}</div>
        </div>
        <div class="bg-gray-100 dark:bg-gray-700 rounded-lg p-4">
          <div class="text-gray-600 dark:text-gray-400 text-sm mb-1">Người dùng mới (tháng)</div>
          <div class="text-2xl font-bold text-gray-900 dark:text-white">{{ statistics?.newUsersThisMonth || 0 }}</div>
        </div>
        <div class="bg-gray-100 dark:bg-gray-700 rounded-lg p-4">
          <div class="text-gray-600 dark:text-gray-400 text-sm mb-1">Tổng số bộ thẻ</div>
          <div class="text-2xl font-bold text-gray-900 dark:text-white">{{ statistics?.totalDecks || 0 }}</div>
        </div>
        <div class="bg-gray-100 dark:bg-gray-700 rounded-lg p-4">
          <div class="text-gray-600 dark:text-gray-400 text-sm mb-1">Bộ thẻ mới (tháng)</div>
          <div class="text-2xl font-bold text-gray-900 dark:text-white">{{ statistics?.newDecksThisMonth || 0 }}</div>
        </div>
        <div class="bg-gray-100 dark:bg-gray-700 rounded-lg p-4">
          <div class="text-gray-600 dark:text-gray-400 text-sm mb-1">Người dùng Premium</div>
          <div class="text-2xl font-bold text-gray-900 dark:text-white">{{ statistics?.totalPremiumUsers || 0 }}</div>
        </div>
        <div class="bg-gray-100 dark:bg-gray-700 rounded-lg p-4">
          <div class="text-gray-600 dark:text-gray-400 text-sm mb-1">Job OCR (tháng này)</div>
          <div class="text-2xl font-bold text-gray-900 dark:text-white">{{ statistics?.totalOcrJobsThisMonth || 0 }}</div>
        </div>
        <div class="bg-gray-100 dark:bg-gray-700 rounded-lg p-4 col-span-1 md:col-span-2">
          <div class="text-gray-600 dark:text-gray-400 text-sm mb-1">Tổng dung lượng lưu trữ (MB)</div>
          <div class="text-2xl font-bold text-gray-900 dark:text-white">{{ statistics?.totalStorageUsedMb?.toFixed(2) || 0 }} MB</div>
        </div>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6 mt-6 mb-8">
        <div class="bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg p-6">
          <h3 class="text-xl font-bold text-gray-900 dark:text-white mb-4">Tăng trưởng người dùng (6 tháng)</h3>
          <div v-if="isLoadingGrowth" class="text-gray-500 dark:text-gray-400">Đang tải...</div>
          <div v-else-if="userChartData.length > 0" class="relative">
            <svg :viewBox="`0 0 ${chartWidth} ${chartHeight}`" class="w-full h-auto">
              <defs>
                <linearGradient id="userAreaGradient" x1="0%" y1="0%" x2="0%" y2="100%">
                  <stop offset="0%" style="stop-color: #0ea5e9; stop-opacity: 0.3" />
                  <stop offset="100%" style="stop-color: #0ea5e9; stop-opacity: 0.05" />
                </linearGradient>
              </defs>
              <path :d="userAreaPath" fill="url(#userAreaGradient)" />
              <path :d="userLinePath" fill="none" stroke="#0ea5e9" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" />
              <circle v-for="(point, idx) in userChartPoints" :key="idx" :cx="point.x" :cy="point.y" r="5" fill="#0ea5e9" stroke-width="2" class="stroke-white dark:stroke-gray-700"><title>{{ point.label }}: {{ point.value }} users</title></circle>
              <text v-for="(point, idx) in userChartPoints" :key="`u-lb-${idx}`" :x="point.x" :y="chartHeight - 10" text-anchor="middle" font-size="12" class="fill-gray-500">{{ point.monthLabel }}</text>
              <text v-for="(tick, idx) in userYAxisTicks" :key="`u-tk-${idx}`" x="10" :y="tick.y + 4" font-size="12" text-anchor="start" class="fill-gray-500">{{ tick.value }}</text>
            </svg>
          </div>
        </div>
        <div class="bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg p-6">
          <h3 class="text-xl font-bold text-gray-900 dark:text-white mb-4">Tăng trưởng bộ thẻ (6 tháng)</h3>
          <div v-if="isLoadingGrowth" class="text-gray-500 dark:text-gray-400">Đang tải...</div>
          <div v-else-if="deckChartData.length > 0" class="relative">
            <svg :viewBox="`0 0 ${chartWidth} ${chartHeight}`" class="w-full h-auto">
              <defs>
                <linearGradient id="deckAreaGradient" x1="0%" y1="0%" x2="0%" y2="100%">
                  <stop offset="0%" style="stop-color: #10b981; stop-opacity: 0.3" />
                  <stop offset="100%" style="stop-color: #10b981; stop-opacity: 0.05" />
                </linearGradient>
              </defs>
              <path :d="deckAreaPath" fill="url(#deckAreaGradient)" />
              <path :d="deckLinePath" fill="none" stroke="#10b981" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" />
              <circle v-for="(point, idx) in deckChartPoints" :key="idx" :cx="point.x" :cy="point.y" r="5" fill="#10b981" stroke-width="2" class="stroke-white dark:stroke-gray-700"><title>{{ point.label }}: {{ point.value }} decks</title></circle>
              <text v-for="(point, idx) in deckChartPoints" :key="`d-lb-${idx}`" :x="point.x" :y="chartHeight - 10" text-anchor="middle" font-size="12" class="fill-gray-500">{{ point.monthLabel }}</text>
              <text v-for="(tick, idx) in deckYAxisTicks" :key="`d-tk-${idx}`" x="10" :y="tick.y + 4" font-size="12" text-anchor="start" class="fill-gray-500">{{ tick.value }}</text>
            </svg>
          </div>
        </div>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-2 gap-8">
        <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6">
          <h2 class="text-2xl font-bold text-gray-900 dark:text-white mb-6">Top từ khóa được tra cứu</h2>
          <div class="simple-table-container">
            <table class="simple-table">
              <thead><tr><th>Từ khóa</th><th>Số lượt tìm</th></tr></thead>
              <tbody>
                <tr v-for="item in topSearchedWords" :key="item.term"><td>{{ item.term }}</td><td>{{ item.count }}</td></tr>
                <tr v-if="topSearchedWords.length === 0"><td colspan="2" class="text-center">Không có dữ liệu.</td></tr>
              </tbody>
            </table>
          </div>
        </div>
        <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6">
          <h2 class="text-2xl font-bold text-gray-900 dark:text-white mb-6">Top từ khóa tìm hụt</h2>
          <div class="simple-table-container">
            <table class="simple-table">
              <thead><tr><th>Từ khóa</th><th>Số lượt tìm</th></tr></thead>
              <tbody>
                <tr v-for="item in topSearchMisses" :key="item.term"><td>{{ item.term }}</td><td>{{ item.count }}</td></tr>
                <tr v-if="topSearchMisses.length === 0"><td colspan="2" class="text-center">Không có dữ liệu.</td></tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { useJwt } from "~/composables/useJwt";
import { useRuntimeConfig } from "#imports";

const { jwt } = useJwt();
const config = useRuntimeConfig();
const BASE_URL = config.public.apiBaseUrl || "https://localhost:7084";

const statistics = ref<any>(null);
const isLoadingStats = ref(false);
const statsError = ref<string | null>(null);

const growthData = ref<any[]>([]);
const isLoadingGrowth = ref(false);

const topSearchedWords = ref<any[]>([]);
const topSearchMisses = ref<any[]>([]);

onMounted(() => {
  fetchStatistics();
  fetchGrowthData();
  fetchOtherStats();
});

async function handleResponse(response: Response) {
  if (!response.ok) throw new Error("Yêu cầu thất bại");
  const data = await response.json();
  return data.result;
}

async function fetchStatistics() {
  isLoadingStats.value = true;
  try {
    const response = await fetch(`${BASE_URL}/api/Admin/statistics`, { headers: { Authorization: `Bearer ${jwt.value}` } });
    statistics.value = await handleResponse(response);
  } catch (err: any) { statsError.value = err.message; } finally { isLoadingStats.value = false; }
}

async function fetchGrowthData() {
  isLoadingGrowth.value = true;
  try {
    const response = await fetch(`${BASE_URL}/api/Admin/statistics/growth`, { headers: { Authorization: `Bearer ${jwt.value}` } });
    const result: any = await handleResponse(response);
    growthData.value = result.monthlyData || [];
  } catch (err: any) {} finally { isLoadingGrowth.value = false; }
}

async function fetchOtherStats() {
  try {
    const [searched, misses] = await Promise.all([
      fetch(`${BASE_URL}/api/Admin/statistics/top-searched`, { headers: { Authorization: `Bearer ${jwt.value}` } }),
      fetch(`${BASE_URL}/api/Admin/statistics/search-misses`, { headers: { Authorization: `Bearer ${jwt.value}` } }),
    ]);
    topSearchedWords.value = await handleResponse(searched);
    topSearchMisses.value = await handleResponse(misses);
  } catch (err: any) {}
}

// Chart Logic (Tương tự như file cũ của bạn)
const chartWidth = 800; const chartHeight = 300; const padding = { top: 20, right: 40, bottom: 40, left: 50 };
const last6MonthsData = computed(() => growthData.value.length > 0 ? growthData.value.slice(-6) : []);

const userChartData = computed(() => {
  let cum = 0; return last6MonthsData.value.map((item) => { cum += item.newUserCount; const d = new Date(item.month); return { value: cum, label: d.toLocaleDateString("vi-VN"), monthLabel: d.toLocaleDateString("vi-VN", { month: "short" }) }; });
});
const deckChartData = computed(() => {
  let cum = 0; return last6MonthsData.value.map((item) => { cum += item.newDeckCount; const d = new Date(item.month); return { value: cum, label: d.toLocaleDateString("vi-VN"), monthLabel: d.toLocaleDateString("vi-VN", { month: "short" }) }; });
});

function createChartPoints(data: any[]) {
  if (data.length === 0) return [];
  const max = Math.max(...data.map(d => d.value), 1); const min = Math.min(...data.map(d => d.value), 0);
  const plotW = chartWidth - padding.left - padding.right; const plotH = chartHeight - padding.top - padding.bottom;
  return data.map((p, i) => ({ x: padding.left + (i / Math.max(data.length - 1, 1)) * plotW, y: padding.top + plotH - ((p.value - min) / (max - min || 1)) * plotH, ...p }));
}
const userChartPoints = computed(() => createChartPoints(userChartData.value));
const deckChartPoints = computed(() => createChartPoints(deckChartData.value));

function createLinePath(points: any[]) { return points.length ? `M ${points[0].x} ${points[0].y} ` + points.slice(1).map(p => `L ${p.x} ${p.y}`).join(" ") : ""; }
function createAreaPath(points: any[]) { return points.length ? `M ${points[0].x} ${chartHeight - padding.bottom} L ${points[0].x} ${points[0].y} ` + points.slice(1).map(p => `L ${p.x} ${p.y}`).join(" ") + ` L ${points[points.length - 1].x} ${chartHeight - padding.bottom} Z` : ""; }

const userLinePath = computed(() => createLinePath(userChartPoints.value));
const deckLinePath = computed(() => createLinePath(deckChartPoints.value));
const userAreaPath = computed(() => createAreaPath(userChartPoints.value));
const deckAreaPath = computed(() => createAreaPath(deckChartPoints.value));

function createYAxisTicks(data: any[]) {
  if (data.length === 0) return [];
  const max = Math.max(...data.map(d => d.value), 1); const min = Math.min(...data.map(d => d.value), 0);
  const numTicks = 5; const plotH = chartHeight - padding.top - padding.bottom;
  return Array.from({ length: numTicks + 1 }).map((_, i) => ({ value: Math.round(min + ((max - min) / numTicks) * i), y: padding.top + plotH - (i / numTicks) * plotH }));
}
const userYAxisTicks = computed(() => createYAxisTicks(userChartData.value));
const deckYAxisTicks = computed(() => createYAxisTicks(deckChartData.value));
</script>