<template>
  <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6 mb-8">
    <h2 class="text-2xl font-bold text-gray-900 dark:text-white mb-6">
      Giám sát Hạ tầng Azure (1 Giờ qua)
    </h2>
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      
      <div class="bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg p-6">
        <div v-if="azureVmMetadata" class="mb-4">
          <h3 class="text-xl font-bold text-gray-900 dark:text-white">
            {{ azureVmMetadata.name }}
          </h3>
          <div class="text-sm text-gray-600 dark:text-gray-400">
            Đơn vị: {{ azureVmMetadata.unit }} | Khoảng thời gian: 1 giờ
          </div>
          <div class="text-3xl font-bold text-fuchsia-500 dark:text-fuchsia-400 mt-2">
            {{ azureVmMetadata.latestValue?.toFixed(2) ?? "..." }} %
          </div>
        </div>
        <h3 v-else class="text-xl font-bold text-gray-900 dark:text-white mb-4">
          Azure VM: % CPU
        </h3>

        <div v-if="isLoadingVmMetrics" class="text-gray-500 dark:text-gray-400">
          Đang tải dữ liệu...
        </div>
        <div v-else-if="vmMetricsError" class="text-red-500 dark:text-red-400">
          {{ vmMetricsError }}
        </div>
        <div v-else-if="vmChartData.length > 0" class="relative">
          <svg :viewBox="`0 0 ${chartWidth} ${chartHeight}`" class="w-full h-auto">
            <defs>
              <linearGradient id="vmAreaGradient" x1="0%" y1="0%" x2="0%" y2="100%">
                <stop offset="0%" style="stop-color: #d946ef; stop-opacity: 0.3" />
                <stop offset="100%" style="stop-color: #d946ef; stop-opacity: 0.05" />
              </linearGradient>
            </defs>
            <path :d="vmAreaPath" fill="url(#vmAreaGradient)" />
            <path :d="vmLinePath" fill="none" stroke="#d946ef" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" />
            <circle
              v-for="(point, index) in vmChartPoints"
              :key="index"
              :cx="point.x"
              :cy="point.y"
              r="5"
              fill="#d946ef"
              stroke-width="2"
              class="stroke-white dark:stroke-gray-700"
            >
              <title>{{ point.label }}: {{ point.value.toFixed(2) }} %</title>
            </circle>
            <text
              v-for="(point, index) in vmChartPoints"
              :key="`vm-label-${index}`"
              :x="point.x"
              :y="chartHeight - 10"
              text-anchor="middle"
              font-size="12"
              class="fill-gray-500 dark:fill-gray-400"
            >
              {{ point.monthLabel }}
            </text>
            <text
              v-for="(tick, index) in vmYAxisTicks"
              :key="`vm-y-${index}`"
              x="10"
              :y="tick.y + 4"
              font-size="12"
              text-anchor="start"
              class="fill-gray-500 dark:fill-gray-400"
            >
              {{ tick.value }}%
            </text>
          </svg>

          <details class="mt-4">
            <summary class="cursor-pointer text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-white">
              Xem dữ liệu thô
            </summary>
            <div class="simple-table-container mt-2">
              <table class="simple-table">
                <thead>
                  <tr>
                    <th>Thời gian</th>
                    <th>Giá trị (TB)</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="item in azureVmMetrics" :key="item.timeStamp">
                    <td>{{ new Date(item.timeStamp).toLocaleString("vi-VN") }}</td>
                    <td>{{ item.average.toFixed(3) }} %</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </details>
        </div>
        <div v-else class="text-gray-500 dark:text-gray-400">
          Chưa có dữ liệu để hiển thị
        </div>
      </div>

      <div class="bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg p-6">
        <div v-if="azureSqlMetadata" class="mb-4">
          <h3 class="text-xl font-bold text-gray-900 dark:text-white">
            {{ azureSqlMetadata.name }}
          </h3>
          <div class="text-sm text-gray-600 dark:text-gray-400">
            Đơn vị: {{ azureSqlMetadata.unit }} | Khoảng thời gian: 1 giờ
          </div>
          <div class="text-3xl font-bold text-orange-500 dark:text-orange-400 mt-2">
            {{ azureSqlMetadata.latestValue?.toFixed(2) ?? "..." }} %
          </div>
        </div>
        <h3 v-else class="text-xl font-bold text-gray-900 dark:text-white mb-4">
          Azure SQL DB: % CPU
        </h3>

        <div v-if="isLoadingSqlMetrics" class="text-gray-500 dark:text-gray-400">
          Đang tải dữ liệu...
        </div>
        <div v-else-if="sqlMetricsError" class="text-red-500 dark:text-red-400">
          {{ sqlMetricsError }}
        </div>
        <div v-else-if="sqlChartData.length > 0" class="relative">
          <svg :viewBox="`0 0 ${chartWidth} ${chartHeight}`" class="w-full h-auto">
            <defs>
              <linearGradient id="sqlAreaGradient" x1="0%" y1="0%" x2="0%" y2="100%">
                <stop offset="0%" style="stop-color: #f97316; stop-opacity: 0.3" />
                <stop offset="100%" style="stop-color: #f97316; stop-opacity: 0.05" />
              </linearGradient>
            </defs>
            <path :d="sqlAreaPath" fill="url(#sqlAreaGradient)" />
            <path :d="sqlLinePath" fill="none" stroke="#f97316" stroke-width="3" stroke-linecap="round" stroke-linejoin="round" />
            <circle
              v-for="(point, index) in sqlChartPoints"
              :key="index"
              :cx="point.x"
              :cy="point.y"
              r="5"
              fill="#f97316"
              stroke-width="2"
              class="stroke-white dark:stroke-gray-700"
            >
              <title>{{ point.label }}: {{ point.value.toFixed(2) }} %</title>
            </circle>
            <text
              v-for="(point, index) in sqlChartPoints"
              :key="`sql-label-${index}`"
              :x="point.x"
              :y="chartHeight - 10"
              text-anchor="middle"
              font-size="12"
              class="fill-gray-500 dark:fill-gray-400"
            >
              {{ point.monthLabel }}
            </text>
            <text
              v-for="(tick, index) in sqlYAxisTicks"
              :key="`sql-y-${index}`"
              x="10"
              :y="tick.y + 4"
              font-size="12"
              text-anchor="start"
              class="fill-gray-500 dark:fill-gray-400"
            >
              {{ tick.value }}%
            </text>
          </svg>

          <details class="mt-4">
            <summary class="cursor-pointer text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-white">
              Xem dữ liệu thô
            </summary>
            <div class="simple-table-container mt-2">
              <table class="simple-table">
                <thead>
                  <tr>
                    <th>Thời gian</th>
                    <th>Giá trị (TB)</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="item in azureSqlMetrics" :key="item.timeStamp">
                    <td>{{ new Date(item.timeStamp).toLocaleString("vi-VN") }}</td>
                    <td>{{ item.average.toFixed(3) }} %</td>
                  </tr>
                </tbody>
              </table>
            </div>
          </details>
        </div>
        <div v-else class="text-gray-500 dark:text-gray-400">
          Chưa có dữ liệu để hiển thị
        </div>
      </div>
    </div>

    <div class="grid grid-cols-1 lg:grid-cols-2 gap-8 mt-8">
      <div class="bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg p-6">
        <h2 class="text-xl font-bold text-gray-900 dark:text-white mb-6">Hiệu suất API</h2>
        <div v-if="isLoadingOtherStats" class="text-gray-500 dark:text-gray-400">Đang tải...</div>
        <div v-else-if="otherStatsError" class="text-red-500 dark:text-red-400">{{ otherStatsError }}</div>
        <div v-else class="simple-table-container">
          <table class="simple-table">
            <thead>
              <tr>
                <th>Endpoint</th>
                <th>TB Phản hồi (ms)</th>
                <th>Tỷ lệ lỗi</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="item in apiPerformance" :key="item.endpoint">
                <td class="break-all">{{ item.endpoint }}</td>
                <td>{{ item.averageResponseTimeMs?.toFixed(0) || 0 }} ms</td>
                <td>{{ (item.errorRate * 100).toFixed(1) }} %</td>
              </tr>
              <tr v-if="apiPerformance.length === 0">
                <td colspan="3" class="text-center">Không có dữ liệu.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <div class="bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg p-6">
        <h2 class="text-xl font-bold text-gray-900 dark:text-white mb-6">Job hệ thống bị lỗi</h2>
        <div v-if="isLoadingOtherStats" class="text-gray-500 dark:text-gray-400">Đang tải...</div>
        <div v-else-if="otherStatsError" class="text-red-500 dark:text-red-400">{{ otherStatsError }}</div>
        <div v-else class="simple-table-container">
          <table class="simple-table">
            <thead>
              <tr>
                <th>Loại Job</th>
                <th>Thời gian</th>
                <th>Chi tiết lỗi</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="item in failedJobs" :key="item.id">
                <td>{{ item.jobType }}</td>
                <td>{{ new Date(item.failedAt).toLocaleString("vi-VN") }}</td>
                <td class="break-all text-red-500 dark:text-red-400">{{ item.errorMessage }}</td>
              </tr>
              <tr v-if="failedJobs.length === 0">
                <td colspan="3" class="text-center">Không có job lỗi.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { useJwt } from "~/composables/useJwt";
import { useToast } from "~/composables/useToast";
import { useRuntimeConfig } from "#imports";

// ===================================
// TYPES (Bê y nguyên từ file cũ của bạn)
// ===================================
interface ApiPerformanceStat {
  endpoint: string;
  averageResponseTimeMs: number;
  errorRate: number;
}
interface FailedJob {
  id: number;
  jobType: string;
  failedAt: string;
  errorMessage: string;
}
interface AzureMetricDataPoint {
  timeStamp: string;
  average: number;
}
interface AzureMetricMetadata {
  name: string;
  unit: string;
  timespan: string;
  latestValue: number | null;
}
interface ChartDataPoint {
  value: number;
  label: string;
  monthLabel: string;
}

const { jwt } = useJwt();
const config = useRuntimeConfig();
const BASE_URL = config.public.apiBaseUrl || "https://localhost:7084";
const { showToast } = useToast();

// ===================================
// STATE
// ===================================
const azureVmMetrics = ref<AzureMetricDataPoint[]>([]);
const azureVmMetadata = ref<AzureMetricMetadata | null>(null);
const isLoadingVmMetrics = ref(false);
const vmMetricsError = ref<string | null>(null);

const azureSqlMetrics = ref<AzureMetricDataPoint[]>([]);
const azureSqlMetadata = ref<AzureMetricMetadata | null>(null);
const isLoadingSqlMetrics = ref(false);
const sqlMetricsError = ref<string | null>(null);

const apiPerformance = ref<ApiPerformanceStat[]>([]);
const failedJobs = ref<FailedJob[]>([]);
const isLoadingOtherStats = ref(false);
const otherStatsError = ref<string | null>(null);

// ===================================
// API CALLER
// ===================================
async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const errorData = await response.json().catch(() => ({ message: response.statusText }));
    throw new Error(errorData.message || `Yêu cầu thất bại: ${response.status}`);
  }
  if (response.status === 204) return {} as T;
  const data = await response.json();
  if (data.isSuccess === false) throw new Error(data.message || "Lỗi không xác định từ API");
  return data.result;
}

// ===================================
// FETCH FUNCTIONS
// ===================================
async function fetchVmCpu() {
  isLoadingVmMetrics.value = true;
  vmMetricsError.value = null;
  try {
    const response = await fetch(`${BASE_URL}/api/Admin/azure/vm-cpu`, {
      headers: { Authorization: `Bearer ${jwt.value}` },
    });
    const jsonString = await handleResponse<string>(response);
    const parsedResult = JSON.parse(jsonString);
    const metric = parsedResult.value?.[0];
    const timeseries = metric?.timeseries?.[0];
    const data: AzureMetricDataPoint[] = timeseries?.data ?? [];
    azureVmMetrics.value = data;
    azureVmMetadata.value = {
      name: metric?.name?.localizedValue ?? 'VM CPU',
      unit: metric?.unit ?? '%',
      timespan: parsedResult.timespan ?? '',
      latestValue: data.length > 0 ? data[data.length - 1].average : null,
    };
  } catch (err: any) {
    vmMetricsError.value = err.message;
  } finally {
    isLoadingVmMetrics.value = false;
  }
}

async function fetchSqlDbCpu() {
  isLoadingSqlMetrics.value = true;
  sqlMetricsError.value = null;
  try {
    const response = await fetch(`${BASE_URL}/api/Admin/azure/sql-db`, {
      headers: { Authorization: `Bearer ${jwt.value}` },
    });
    const jsonString = await handleResponse<string>(response);
    const parsedResult = JSON.parse(jsonString);
    const metric = parsedResult.value?.[0];
    const timeseries = metric?.timeseries?.[0];
    const data: AzureMetricDataPoint[] = timeseries?.data ?? [];
    azureSqlMetrics.value = data;
    azureSqlMetadata.value = {
      name: metric?.name?.localizedValue ?? 'SQL CPU',
      unit: metric?.unit ?? '%',
      timespan: parsedResult.timespan ?? '',
      latestValue: data.length > 0 ? data[data.length - 1].average : null,
    };
  } catch (err: any) {
    sqlMetricsError.value = err.message;
  } finally {
    isLoadingSqlMetrics.value = false;
  }
}

async function fetchJobsAndApis() {
  isLoadingOtherStats.value = true;
  otherStatsError.value = null;
  try {
    const [performance, jobs] = await Promise.all([
      fetch(`${BASE_URL}/api/Admin/statistics/api-performance`, { headers: { Authorization: `Bearer ${jwt.value}` } }),
      fetch(`${BASE_URL}/api/Admin/statistics/failed-jobs`, { headers: { Authorization: `Bearer ${jwt.value}` } }),
    ]);
    apiPerformance.value = await handleResponse<ApiPerformanceStat[]>(performance);
    failedJobs.value = await handleResponse<FailedJob[]>(jobs);
  } catch (err: any) {
    otherStatsError.value = err.message;
    showToast(`Lỗi: ${err.message}`, "error");
  } finally {
    isLoadingOtherStats.value = false;
  }
}

onMounted(() => {
  fetchVmCpu();
  fetchSqlDbCpu();
  fetchJobsAndApis();
});

// ===================================
// CHART LOGIC VẼ LẠI 100% NHƯ CŨ
// ===================================
const chartWidth = 800;
const chartHeight = 300;
const padding = { top: 20, right: 40, bottom: 40, left: 50 };

const vmChartData = computed((): ChartDataPoint[] => {
  const data = azureVmMetrics.value;
  if (data.length === 0) return [];
  return data.map((item) => {
    const date = new Date(item.timeStamp);
    return {
      value: item.average,
      label: date.toLocaleString("vi-VN"),
      monthLabel: date.toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" }),
    };
  });
});

const sqlChartData = computed((): ChartDataPoint[] => {
  const data = azureSqlMetrics.value;
  if (data.length === 0) return [];
  return data.map((item) => {
    const date = new Date(item.timeStamp);
    return {
      value: item.average,
      label: date.toLocaleString("vi-VN"),
      monthLabel: date.toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" }),
    };
  });
});

function createChartPoints(data: ChartDataPoint[]) {
  if (data.length === 0) return [];
  const maxValue = Math.max(...data.map((d) => d.value), 1);
  const minValue = Math.min(...data.map((d) => d.value), 0);
  const plotWidth = chartWidth - padding.left - padding.right;
  const plotHeight = chartHeight - padding.top - padding.bottom;
  return data.map((point, index) => {
    const divisor = data.length > 1 ? data.length - 1 : 1;
    const x = padding.left + (index / divisor) * plotWidth;
    const y = padding.top + plotHeight - ((point.value - minValue) / (maxValue - minValue || 1)) * plotHeight;
    return {
      x, y, value: point.value, label: point.label, monthLabel: point.monthLabel,
    };
  });
}

const vmChartPoints = computed(() => createChartPoints(vmChartData.value));
const sqlChartPoints = computed(() => createChartPoints(sqlChartData.value));

function createLinePath(points: { x: number; y: number }[]) {
  if (points.length === 0) return "";
  let path = `M ${points[0].x} ${points[0].y}`;
  for (let i = 1; i < points.length; i++) {
    path += ` L ${points[i].x} ${points[i].y}`;
  }
  return path;
}

const vmLinePath = computed(() => createLinePath(vmChartPoints.value));
const sqlLinePath = computed(() => createLinePath(sqlChartPoints.value));

function createAreaPath(points: { x: number; y: number }[]) {
  if (points.length === 0) return "";
  const bottomY = chartHeight - padding.bottom;
  let path = `M ${points[0].x} ${bottomY}`;
  path += ` L ${points[0].x} ${points[0].y}`;
  for (let i = 1; i < points.length; i++) {
    path += ` L ${points[i].x} ${points[i].y}`;
  }
  path += ` L ${points[points.length - 1].x} ${bottomY} Z`;
  return path;
}

const vmAreaPath = computed(() => createAreaPath(vmChartPoints.value));
const sqlAreaPath = computed(() => createAreaPath(sqlChartPoints.value));

function createYAxisTicks(data: ChartDataPoint[]) {
  if (data.length === 0) return [];
  const maxValue = Math.max(...data.map((d) => d.value), 1);
  const minValue = Math.min(...data.map((d) => d.value), 0);
  const plotHeight = chartHeight - padding.top - padding.bottom;
  const numTicks = 5;
  const ticks = [];
  for (let i = 0; i <= numTicks; i++) {
    const value = minValue + ((maxValue - minValue) / numTicks) * i;
    const y = padding.top + plotHeight - (i / numTicks) * plotHeight;
    ticks.push({ value: Math.round(value), y });
  }
  return ticks;
}

const vmYAxisTicks = computed(() => createYAxisTicks(vmChartData.value));
const sqlYAxisTicks = computed(() => createYAxisTicks(sqlChartData.value));
</script>