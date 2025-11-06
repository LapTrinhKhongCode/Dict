<template>
  <ClientOnly>
    <div class="min-h-screen text-white p-4 sm:p-8 bg-gray-900">
      <div class="max-w-7xl mx-auto">
        <h1 class="text-3xl font-bold text-white mb-8">Admin Dashboard</h1>

        <!-- Statistics Section -->
        <div class="bg-gray-800 rounded-lg p-6 mb-8">
          <h2 class="text-2xl font-bold text-white mb-6">Thống kê</h2>
          <div v-if="isLoadingStats" class="text-gray-400">Đang tải thống kê...</div>
          <div v-else-if="statsError" class="text-red-400">{{ statsError }}</div>
          <div v-else>
            <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
            <div class="bg-gray-700 rounded-lg p-4">
              <div class="text-gray-400 text-sm mb-1">Tổng số người dùng</div>
              <div class="text-2xl font-bold text-white">{{ statistics?.totalUsers || 0 }}</div>
            </div>
            <div class="bg-gray-700 rounded-lg p-4">
              <div class="text-gray-400 text-sm mb-1">Người dùng mới tháng này</div>
              <div class="text-2xl font-bold text-white">{{ statistics?.newUsersThisMonth || 0 }}</div>
            </div>
            <div class="bg-gray-700 rounded-lg p-4">
              <div class="text-gray-400 text-sm mb-1">Tổng số bộ thẻ</div>
              <div class="text-2xl font-bold text-white">{{ statistics?.totalDecks || 0 }}</div>
            </div>
            <div class="bg-gray-700 rounded-lg p-4">
              <div class="text-gray-400 text-sm mb-1">Bộ thẻ mới tháng này</div>
              <div class="text-2xl font-bold text-white">{{ statistics?.newDecksThisMonth || 0 }}</div>
            </div>
            <div class="bg-gray-700 rounded-lg p-4">
              <div class="text-gray-400 text-sm mb-1">Người dùng Premium</div>
              <div class="text-2xl font-bold text-white">{{ statistics?.totalPremiumUsers || 0 }}</div>
            </div>
            <div class="bg-gray-700 rounded-lg p-4">
              <div class="text-gray-400 text-sm mb-1">Tổng công việc OCR tháng này</div>
              <div class="text-2xl font-bold text-white">{{ statistics?.totalOcrJobsThisMonth || 0 }}</div>
            </div>
            <div class="bg-gray-700 rounded-lg p-4">
              <div class="text-gray-400 text-sm mb-1">Dung lượng lưu trữ (MB)</div>
              <div class="text-2xl font-bold text-white">{{ statistics?.totalStorageUsedMb || 0 }}</div>
            </div>
          </div>

          <!-- Charts Section -->
          <div class="grid grid-cols-1 lg:grid-cols-2 gap-6 mt-6">
            <!-- User Growth Chart -->
            <div class="bg-gray-700 rounded-lg p-6">
              <h3 class="text-xl font-bold text-white mb-4">Tăng trưởng người dùng (6 tháng gần nhất)</h3>
              <div v-if="isLoadingGrowth" class="text-gray-400">Đang tải dữ liệu...</div>
              <div v-else-if="growthError" class="text-red-400">{{ growthError }}</div>
              <div v-else-if="userChartData.length > 0" class="relative">
                <svg :viewBox="`0 0 ${chartWidth} ${chartHeight}`" class="w-full h-auto" preserveAspectRatio="xMidYMid meet">
                  <defs>
                    <linearGradient id="userAreaGradient" x1="0%" y1="0%" x2="0%" y2="100%">
                      <stop offset="0%" style="stop-color:#0ea5e9;stop-opacity:0.3" />
                      <stop offset="100%" style="stop-color:#0ea5e9;stop-opacity:0.05" />
                    </linearGradient>
                  </defs>
                  
                  <path
                    :d="userAreaPath"
                    fill="url(#userAreaGradient)"
                    class="transition-all duration-300"
                  />
                  
                  <path
                    :d="userLinePath"
                    fill="none"
                    stroke="#0ea5e9"
                    stroke-width="3"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    class="transition-all duration-300"
                  />
                  
                  <circle
                    v-for="(point, index) in userChartPoints"
                    :key="index"
                    :cx="point.x"
                    :cy="point.y"
                    r="5"
                    fill="#0ea5e9"
                    stroke="#1e293b"
                    stroke-width="2"
                    class="transition-all duration-300 hover:r-7 cursor-pointer"
                  >
                    <title>{{ point.label }}: {{ point.value }} người dùng</title>
                  </circle>
                  
                  <text
                    v-for="(point, index) in userChartPoints"
                    :key="`user-label-${index}`"
                    :x="point.x"
                    :y="chartHeight - 10"
                    text-anchor="middle"
                    fill="#9ca3af"
                    font-size="12"
                    font-family="system-ui"
                  >
                    {{ point.monthLabel }}
                  </text>
                  
                  <text
                    v-for="(tick, index) in userYAxisTicks"
                    :key="`user-y-${index}`"
                    x="10"
                    :y="tick.y + 4"
                    fill="#9ca3af"
                    font-size="12"
                    font-family="system-ui"
                    text-anchor="start"
                  >
                    {{ tick.value }}
                  </text>
                </svg>
              </div>
              <div v-else class="text-gray-400">Chưa có dữ liệu để hiển thị</div>
            </div>

            <!-- Deck Growth Chart -->
            <div class="bg-gray-700 rounded-lg p-6">
              <h3 class="text-xl font-bold text-white mb-4">Tăng trưởng bộ thẻ (6 tháng gần nhất)</h3>
              <div v-if="isLoadingGrowth" class="text-gray-400">Đang tải dữ liệu...</div>
              <div v-else-if="growthError" class="text-red-400">{{ growthError }}</div>
              <div v-else-if="deckChartData.length > 0" class="relative">
                <svg :viewBox="`0 0 ${chartWidth} ${chartHeight}`" class="w-full h-auto" preserveAspectRatio="xMidYMid meet">
                  <defs>
                    <linearGradient id="deckAreaGradient" x1="0%" y1="0%" x2="0%" y2="100%">
                      <stop offset="0%" style="stop-color:#10b981;stop-opacity:0.3" />
                      <stop offset="100%" style="stop-color:#10b981;stop-opacity:0.05" />
                    </linearGradient>
                  </defs>
                  
                  <path
                    :d="deckAreaPath"
                    fill="url(#deckAreaGradient)"
                    class="transition-all duration-300"
                  />
                  
                  <path
                    :d="deckLinePath"
                    fill="none"
                    stroke="#10b981"
                    stroke-width="3"
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    class="transition-all duration-300"
                  />
                  
                  <circle
                    v-for="(point, index) in deckChartPoints"
                    :key="index"
                    :cx="point.x"
                    :cy="point.y"
                    r="5"
                    fill="#10b981"
                    stroke="#1e293b"
                    stroke-width="2"
                    class="transition-all duration-300 hover:r-7 cursor-pointer"
                  >
                    <title>{{ point.label }}: {{ point.value }} bộ thẻ</title>
                  </circle>
                  
                  <text
                    v-for="(point, index) in deckChartPoints"
                    :key="`deck-label-${index}`"
                    :x="point.x"
                    :y="chartHeight - 10"
                    text-anchor="middle"
                    fill="#9ca3af"
                    font-size="12"
                    font-family="system-ui"
                  >
                    {{ point.monthLabel }}
                  </text>
                  
                  <text
                    v-for="(tick, index) in deckYAxisTicks"
                    :key="`deck-y-${index}`"
                    x="10"
                    :y="tick.y + 4"
                    fill="#9ca3af"
                    font-size="12"
                    font-family="system-ui"
                    text-anchor="start"
                  >
                    {{ tick.value }}
                  </text>
                </svg>
              </div>
              <div v-else class="text-gray-400">Chưa có dữ liệu để hiển thị</div>
            </div>
          </div>
          </div>
        </div>

        <!-- User Detail Modal -->
        <UserDetailModal
          :is-open="isUserModalOpen"
          :user="selectedUser"
          @close="closeUserModal"
        />

        <!-- User Search Section -->
        <div class="bg-gray-800 rounded-lg p-6">
          <h2 class="text-2xl font-bold text-white mb-6">Tìm kiếm người dùng</h2>
          
          <div class="mb-6">
            <div class="flex gap-4">
              <input
                type="text"
                v-model="searchTerm"
                @keyup.enter="handleSearch"
                placeholder="Nhập từ khóa tìm kiếm (username, email)..."
                class="flex-1 form-input"
              />
              <button
                @click="handleSearch"
                :disabled="isSearching || !searchTerm.trim()"
                class="form-button-primary px-6"
              >
                <span v-if="isSearching">Đang tìm...</span>
                <span v-else>Tìm kiếm</span>
              </button>
            </div>
          </div>

          <!-- Search Results -->
          <div v-if="isSearching" class="text-gray-400">Đang tìm kiếm...</div>
          <div v-else-if="searchError" class="text-red-400">{{ searchError }}</div>
          <div v-else-if="searchResults.length > 0" class="space-y-4">
            <div class="text-gray-400 text-sm mb-4">
              Tìm thấy {{ searchTotalCount }} người dùng (trang {{ searchPage }}/{{ totalPages }})
            </div>
            <div class="space-y-2">
              <div
                v-for="user in searchResults"
                :key="user.id"
                :class="[
                  'bg-gray-700 rounded-lg p-4 transition-all hover:bg-gray-600',
                  selectedUserIds.has(user.id) ? 'ring-2 ring-sky-500 bg-gray-600' : ''
                ]"
              >
                <div class="flex items-center gap-4">
                  <div class="flex-shrink-0">
                    <img
                      :src="user.avatarUrl || 'https://storage.googleapis.com/maker-studio-project-media-prod/11072a6a-d42a-41e9-8902-613d0a6a0e69/images/220b33b9-a99f-4318-9126-d66a6a96f131.jpg'"
                      :alt="user.username"
                      class="w-12 h-12 rounded-full object-cover bg-gray-600"
                    />
                  </div>
                  <div class="flex-1 cursor-pointer" @click="openUserModal(user)">
                    <div class="flex items-center gap-2">
                      <h3 class="text-lg font-semibold text-white">{{ user.username }}</h3>
                      <span
                        v-if="user.role"
                        class="px-2 py-1 text-xs font-semibold rounded bg-sky-600 text-white"
                      >
                        {{ user.role }}
                      </span>
                    </div>
                    <p class="text-gray-400 text-sm">{{ user.email }}</p>
                    <div class="flex items-center gap-4 mt-2 text-xs text-gray-500">
                      <span>ID: {{ user.id }}</span>
                      <span>Trạng thái: {{ user.isActive ? 'Hoạt động' : 'Không hoạt động' }}</span>
                      <span>Bộ thẻ: {{ user.decks?.length || 0 }}</span>
                    </div>
                  </div>
                  <div class="flex-shrink-0 flex items-center gap-2">
                    <button
                      @click.stop="openUserModal(user)"
                      class="px-3 py-1.5 text-sm bg-sky-600 hover:bg-sky-700 text-white rounded-lg transition-colors font-medium"
                      title="Xem chi tiết"
                    >
                      Chi tiết
                    </button>
                    <div
                      @click.stop="toggleUserSelection(user.id)"
                      :class="[
                        'w-6 h-6 rounded border-2 flex items-center justify-center transition-colors cursor-pointer',
                        selectedUserIds.has(user.id)
                          ? 'bg-sky-500 border-sky-500'
                          : 'border-gray-500 hover:border-gray-400'
                      ]"
                    >
                      <svg
                        v-if="selectedUserIds.has(user.id)"
                        xmlns="http://www.w3.org/2000/svg"
                        class="w-4 h-4 text-white"
                        viewBox="0 0 20 20"
                        fill="currentColor"
                      >
                        <path
                          fill-rule="evenodd"
                          d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z"
                          clip-rule="evenodd"
                        />
                      </svg>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <!-- Pagination -->
            <div v-if="totalPages > 1" class="flex items-center justify-center gap-4 mt-6">
              <button
                @click="changePage(searchPage - 1)"
                :disabled="searchPage <= 1 || isSearching"
                class="form-button-primary px-4 py-2 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Trước
              </button>
              <span class="text-gray-400">
                Trang {{ searchPage }} / {{ totalPages }}
              </span>
              <button
                @click="changePage(searchPage + 1)"
                :disabled="searchPage >= totalPages || isSearching"
                class="form-button-primary px-4 py-2 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Sau
              </button>
            </div>

            <!-- Selected Users Info -->
            <div v-if="selectedUserIds.size > 0" class="mt-6 p-4 bg-sky-900/20 border border-sky-500 rounded-lg">
              <div class="text-sky-400 font-semibold mb-2">
                Đã chọn {{ selectedUserIds.size }} người dùng
              </div>
              <div class="flex flex-wrap gap-2">
                <span
                  v-for="userId in Array.from(selectedUserIds)"
                  :key="userId"
                  class="px-3 py-1 bg-sky-600 text-white rounded-full text-sm"
                >
                  ID: {{ userId }}
                </span>
              </div>
            </div>
          </div>
          <div v-else-if="hasSearched" class="text-gray-400">
            Không tìm thấy người dùng nào.
          </div>
        </div>
      </div>
    </div>
  </ClientOnly>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue";
import { useJwt } from "~/composables/useJwt";
import { useToast } from "~/composables/useToast";
import UserDetailModal from "~/components/UserDetailModal.vue";

definePageMeta({
  title: "Admin Page",
  middleware: "admin-only-client",
});

const { jwt } = useJwt();
const config = useRuntimeConfig();
const BASE_URL = config.public.apiBaseUrl || "https://localhost:7084";
const { showToast } = useToast();

// Types
interface Statistics {
  totalUsers: number;
  newUsersThisMonth: number;
  totalDecks: number;
  newDecksThisMonth: number;
  totalPremiumUsers: number;
  totalOcrJobsThisMonth: number;
  totalStorageUsedMb: number;
}

interface Deck {
  id: number;
  name: string;
  description: string;
  isPublic: boolean;
  cardCount: number;
  authorName: string;
  authorImageUrl: string | null;
  nowAuthorName: string | null;
  nowAuthorImageUrl: string | null;
}

interface User {
  id: number;
  username: string;
  email: string;
  isActive: boolean;
  role: string | null;
  avatarUrl: string;
  createdAt: string;
  updatedAt: string;
  decks: Deck[];
}

interface SearchResponse {
  users: User[];
  totalCount: number;
  page: number;
  pageSize: number;
}

// Statistics
const statistics = ref<Statistics | null>(null);
const isLoadingStats = ref(false);
const statsError = ref<string | null>(null);

// Growth chart data
interface MonthlyGrowthData {
  month: string;
  newUserCount: number;
  newDeckCount: number;
}

interface GrowthResponse {
  monthlyData: MonthlyGrowthData[];
}

const growthData = ref<MonthlyGrowthData[]>([]);
const isLoadingGrowth = ref(false);
const growthError = ref<string | null>(null);
const chartWidth = 800;
const chartHeight = 300;
const padding = { top: 20, right: 40, bottom: 40, left: 50 };

// Search
const searchTerm = ref("");
const searchResults = ref<User[]>([]);
const searchTotalCount = ref(0);
const searchPage = ref(1);
const pageSize = ref(10);
const isSearching = ref(false);
const searchError = ref<string | null>(null);
const hasSearched = ref(false);
const selectedUserIds = ref<Set<number>>(new Set());

// User detail modal
const isUserModalOpen = ref(false);
const selectedUser = ref<User | null>(null);

const totalPages = computed(() => Math.ceil(searchTotalCount.value / pageSize.value));

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const errorText = await response.text();
    let errorMessage = errorText;
    try {
      const errorJson = JSON.parse(errorText);
      if (errorJson?.errors) {
        errorMessage = Object.values(errorJson.errors).flat().join(" ");
      } else if (errorJson?.message) {
        errorMessage = errorJson.message;
      } else if (errorJson?.title) {
        errorMessage = errorJson.title;
      }
    } catch (e) {}
    throw new Error(errorMessage || `Yêu cầu thất bại: ${response.status}`);
  }
  if (response.status === 204) return {} as T;
  const data = await response.json();
  if (data.isSuccess === false) {
    throw new Error(data.message || "Lỗi không xác định từ API");
  }
  return data.result;
}

async function fetchStatistics() {
  isLoadingStats.value = true;
  statsError.value = null;
  try {
    const response = await fetch(`${BASE_URL}/api/Admin/statistics`, {
      headers: { Authorization: `Bearer ${jwt.value}` },
    });
    statistics.value = await handleResponse<Statistics>(response);
  } catch (err: any) {
    console.error("Lỗi tải thống kê:", err);
    statsError.value = err.message || "Không thể tải thống kê";
    showToast(`Lỗi: ${err.message}`, "error");
  } finally {
    isLoadingStats.value = false;
  }
}

async function fetchGrowthData() {
  isLoadingGrowth.value = true;
  growthError.value = null;
  try {
    const response = await fetch(`${BASE_URL}/api/Admin/statistics/growth`, {
      headers: { Authorization: `Bearer ${jwt.value}` },
    });
    const result = await handleResponse<GrowthResponse>(response);
    growthData.value = result.monthlyData || [];
  } catch (err: any) {
    console.error("Lỗi tải dữ liệu tăng trưởng:", err);
    growthError.value = err.message || "Không thể tải dữ liệu tăng trưởng";
    showToast(`Lỗi: ${err.message}`, "error");
  } finally {
    isLoadingGrowth.value = false;
  }
}

// Get last 6 months of data
const last6MonthsData = computed(() => {
  if (growthData.value.length === 0) return [];
  // API returns data in chronological order, get last 6 items
  return growthData.value.slice(-6);
});

// User chart data (cumulative)
const userChartData = computed(() => {
  const data = last6MonthsData.value;
  if (data.length === 0) return [];

  let cumulative = 0;
  return data.map((item) => {
    cumulative += item.newUserCount;
    const [year, month] = item.month.split("-").map(Number);
    const date = new Date(year, month - 1, 1);
    return {
      month: item.month,
      cumulative,
      label: date.toLocaleDateString("vi-VN", { month: "short", year: "numeric" }),
      monthLabel: date.toLocaleDateString("vi-VN", { month: "short" }),
    };
  });
});

// Deck chart data (cumulative)
const deckChartData = computed(() => {
  const data = last6MonthsData.value;
  if (data.length === 0) return [];

  let cumulative = 0;
  return data.map((item) => {
    cumulative += item.newDeckCount;
    const [year, month] = item.month.split("-").map(Number);
    const date = new Date(year, month - 1, 1);
    return {
      month: item.month,
      cumulative,
      label: date.toLocaleDateString("vi-VN", { month: "short", year: "numeric" }),
      monthLabel: date.toLocaleDateString("vi-VN", { month: "short" }),
    };
  });
});

// Helper function to create chart points
function createChartPoints(data: { cumulative: number; label: string; monthLabel: string }[]) {
  if (data.length === 0) return [];

  const maxValue = Math.max(...data.map((d) => d.cumulative), 1);
  const minValue = Math.min(...data.map((d) => d.cumulative), 0);

  const plotWidth = chartWidth - padding.left - padding.right;
  const plotHeight = chartHeight - padding.top - padding.bottom;

  return data.map((point, index) => {
    const divisor = data.length > 1 ? data.length - 1 : 1;
    const x = padding.left + (index / divisor) * plotWidth;
    const y =
      padding.top +
      plotHeight -
      ((point.cumulative - minValue) / (maxValue - minValue || 1)) * plotHeight;

    return {
      x,
      y,
      value: point.cumulative,
      label: point.label,
      monthLabel: point.monthLabel,
    };
  });
}

// User chart points
const userChartPoints = computed(() => createChartPoints(userChartData.value));

// Deck chart points
const deckChartPoints = computed(() => createChartPoints(deckChartData.value));

// Helper function to create line path
function createLinePath(points: { x: number; y: number }[]) {
  if (points.length === 0) return "";
  let path = `M ${points[0].x} ${points[0].y}`;
  for (let i = 1; i < points.length; i++) {
    path += ` L ${points[i].x} ${points[i].y}`;
  }
  return path;
}

// User line path
const userLinePath = computed(() => createLinePath(userChartPoints.value));

// Deck line path
const deckLinePath = computed(() => createLinePath(deckChartPoints.value));

// Helper function to create area path
function createAreaPath(points: { x: number; y: number }[]) {
  if (points.length === 0) return "";
  const bottomY = chartHeight - padding.bottom;
  let path = `M ${points[0].x} ${bottomY}`;
  path += ` L ${points[0].x} ${points[0].y}`;
  for (let i = 1; i < points.length; i++) {
    path += ` L ${points[i].x} ${points[i].y}`;
  }
  path += ` L ${points[points.length - 1].x} ${bottomY}`;
  path += " Z";
  return path;
}

// User area path
const userAreaPath = computed(() => createAreaPath(userChartPoints.value));

// Deck area path
const deckAreaPath = computed(() => createAreaPath(deckChartPoints.value));

// Helper function to create Y-axis ticks
function createYAxisTicks(data: { cumulative: number }[]) {
  if (data.length === 0) return [];
  const maxValue = Math.max(...data.map((d) => d.cumulative), 1);
  const minValue = Math.min(...data.map((d) => d.cumulative), 0);
  const plotHeight = chartHeight - padding.top - padding.bottom;
  const numTicks = 5;
  const ticks = [];
  for (let i = 0; i <= numTicks; i++) {
    const value = minValue + ((maxValue - minValue) / numTicks) * i;
    const y = padding.top + plotHeight - (i / numTicks) * plotHeight;
    ticks.push({
      value: Math.round(value),
      y,
    });
  }
  return ticks;
}

// User Y-axis ticks
const userYAxisTicks = computed(() => createYAxisTicks(userChartData.value));

// Deck Y-axis ticks
const deckYAxisTicks = computed(() => createYAxisTicks(deckChartData.value));

async function handleSearch() {
  if (!searchTerm.value.trim()) {
    showToast("Vui lòng nhập từ khóa tìm kiếm", "error");
    return;
  }

  isSearching.value = true;
  searchError.value = null;
  searchPage.value = 1; // Reset to first page on new search

  try {
    const response = await fetch(
      `${BASE_URL}/api/Admin/users/search?searchTerm=${encodeURIComponent(
        searchTerm.value.trim()
      )}&page=${searchPage.value}&pageSize=${pageSize.value}`,
      {
        headers: { Authorization: `Bearer ${jwt.value}` },
      }
    );
    const result = await handleResponse<SearchResponse>(response);
    searchResults.value = result.users;
    searchTotalCount.value = result.totalCount;
    hasSearched.value = true;
    selectedUserIds.value.clear(); // Clear selection on new search
  } catch (err: any) {
    console.error("Lỗi tìm kiếm:", err);
    searchError.value = err.message || "Không thể tìm kiếm";
    searchResults.value = [];
    showToast(`Lỗi: ${err.message}`, "error");
  } finally {
    isSearching.value = false;
  }
}

async function changePage(newPage: number) {
  if (newPage < 1 || newPage > totalPages.value || !searchTerm.value.trim()) {
    return;
  }

  searchPage.value = newPage;
  isSearching.value = true;
  searchError.value = null;

  try {
    const response = await fetch(
      `${BASE_URL}/api/Admin/users/search?searchTerm=${encodeURIComponent(
        searchTerm.value.trim()
      )}&page=${searchPage.value}&pageSize=${pageSize.value}`,
      {
        headers: { Authorization: `Bearer ${jwt.value}` },
      }
    );
    const result = await handleResponse<SearchResponse>(response);
    searchResults.value = result.users;
    searchTotalCount.value = result.totalCount;
  } catch (err: any) {
    console.error("Lỗi tải trang:", err);
    searchError.value = err.message || "Không thể tải trang";
    showToast(`Lỗi: ${err.message}`, "error");
  } finally {
    isSearching.value = false;
  }
}

function toggleUserSelection(userId: number) {
  if (selectedUserIds.value.has(userId)) {
    selectedUserIds.value.delete(userId);
  } else {
    selectedUserIds.value.add(userId);
  }
}

function openUserModal(user: User) {
  selectedUser.value = user;
  isUserModalOpen.value = true;
}

function closeUserModal() {
  isUserModalOpen.value = false;
  selectedUser.value = null;
}

onMounted(() => {
  fetchStatistics();
  fetchGrowthData();
});
</script>

<style scoped>
.form-input {
  width: 100%;
  background-color: #374151;
  border: 1px solid #4b5563;
  border-radius: 0.5rem;
  padding: 0.75rem 1rem;
  color: white;
  outline: none;
  transition: border-color 0.2s, box-shadow 0.2s;
}
.form-input:focus {
  border-color: #0ea5e9;
  box-shadow: 0 0 0 2px rgba(14, 165, 233, 0.4);
}
.form-input::placeholder {
  color: #9ca3af;
}
.form-button-primary {
  background-color: #0ea5e9;
  color: white;
  font-weight: 600;
  padding: 0.75rem 1.5rem;
  border-radius: 0.5rem;
  transition: background-color 0.2s;
  border: none;
  cursor: pointer;
}
.form-button-primary:hover:not(:disabled) {
  background-color: #0284c7;
}
.form-button-primary:disabled {
  background-color: #4b5563;
  opacity: 0.5;
  cursor: not-allowed;
}
</style>
