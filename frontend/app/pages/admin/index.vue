<template>
  <ClientOnly>
    <div
      class="min-h-screen bg-gray-50 text-gray-900 dark:bg-gray-900 dark:text-white p-4 sm:p-8"
    >
      <div class="max-w-7xl mx-auto">
        <h1 class="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Admin Dashboard
        </h1>

        <div class="mb-8">
          <nav
            class="flex space-x-4 border-b border-gray-200 dark:border-gray-700"
            aria-label="Tabs"
          >
            <button
              v-for="tab in tabs"
              :key="tab.id"
              @click="activeTab = tab.id"
              :class="[
                'px-4 py-3 font-medium text-sm rounded-t-lg transition-colors',
                activeTab === tab.id
                  ? 'border-b-2 border-primary-500 text-primary-500 dark:border-sky-500 dark:text-sky-400'
                  : 'text-gray-500 hover:text-gray-700 hover:bg-gray-100 dark:text-gray-400 dark:hover:text-white dark:hover:bg-gray-800',
              ]"
            >
              {{ tab.name }}
            </button>
          </nav>
        </div>

        <div v-if="activeTab === 'overview'">
          <div
            class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6 mb-8"
          >
            <h2
              class="text-2xl font-bold text-gray-900 dark:text-white mb-6"
            >
              Thống kê nhanh
            </h2>
            <div v-if="isLoadingStats" class="text-gray-500 dark:text-gray-400">
              Đang tải thống kê...
            </div>
            <div v-else-if="statsError" class="text-red-500 dark:text-red-400">
              {{ statsError }}
            </div>
            <div v-else>
              <div
                class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-8"
              >
                <div
                  class="bg-gray-100 dark:bg-gray-700 rounded-lg p-4"
                >
                  <div class="text-gray-600 dark:text-gray-400 text-sm mb-1">
                    Tổng số người dùng
                  </div>
                  <div class="text-2xl font-bold text-gray-900 dark:text-white">
                    {{ statistics?.totalUsers || 0 }}
                  </div>
                </div>
                <div
                  class="bg-gray-100 dark:bg-gray-700 rounded-lg p-4"
                >
                  <div class="text-gray-600 dark:text-gray-400 text-sm mb-1">
                    Người dùng mới tháng này
                  </div>
                  <div class="text-2xl font-bold text-gray-900 dark:text-white">
                    {{ statistics?.newUsersThisMonth || 0 }}
                  </div>
                </div>
                <div
                  class="bg-gray-100 dark:bg-gray-700 rounded-lg p-4"
                >
                  <div class="text-gray-600 dark:text-gray-400 text-sm mb-1">
                    Tổng số bộ thẻ
                  </div>
                  <div class="text-2xl font-bold text-gray-900 dark:text-white">
                    {{ statistics?.totalDecks || 0 }}
                  </div>
                </div>
                <div
                  class="bg-gray-100 dark:bg-gray-700 rounded-lg p-4"
                >
                  <div class="text-gray-600 dark:text-gray-400 text-sm mb-1">
                    Bộ thẻ mới tháng này
                  </div>
                  <div class="text-2xl font-bold text-gray-900 dark:text-white">
                    {{ statistics?.newDecksThisMonth || 0 }}
                  </div>
                </div>
                <div
                  class="bg-gray-100 dark:bg-gray-700 rounded-lg p-4"
                >
                  <div class="text-gray-600 dark:text-gray-400 text-sm mb-1">
                    Người dùng Premium
                  </div>
                  <div class="text-2xl font-bold text-gray-900 dark:text-white">
                    {{ statistics?.totalPremiumUsers || 0 }}
                  </div>
                </div>
                <div
                  class="bg-gray-100 dark:bg-gray-700 rounded-lg p-4"
                >
                  <div class="text-gray-600 dark:text-gray-400 text-sm mb-1">
                    Job OCR (tháng này)
                  </div>
                  <div class="text-2xl font-bold text-gray-900 dark:text-white">
                    {{ statistics?.totalOcrJobsThisMonth || 0 }}
                  </div>
                </div>
                <div
                  class="bg-gray-100 dark:bg-gray-700 rounded-lg p-4 col-span-1 md:col-span-2"
                >
                  <div class="text-gray-600 dark:text-gray-400 text-sm mb-1">
                    Tổng dung lượng lưu trữ (MB)
                  </div>
                  <div class="text-2xl font-bold text-gray-900 dark:text-white">
                    {{ statistics?.totalStorageUsedMb?.toFixed(2) || 0 }} MB
                  </div>
                </div>
              </div>

              <div class="grid grid-cols-1 lg:grid-cols-2 gap-6 mt-6">
                <div
                  class="bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg p-6"
                >
                  <h3
                    class="text-xl font-bold text-gray-900 dark:text-white mb-4"
                  >
                    Tăng trưởng người dùng (6 tháng gần nhất)
                  </h3>
                  <div v-if="isLoadingGrowth" class="text-gray-500 dark:text-gray-400">
                    Đang tải dữ liệu...
                  </div>
                  <div v-else-if="growthError" class="text-red-500 dark:text-red-400">
                    {{ growthError }}
                  </div>
                  <div v-else-if="userChartData.length > 0" class="relative">
                    <svg
                      :viewBox="`0 0 ${chartWidth} ${chartHeight}`"
                      class="w-full h-auto"
                    >
                      <defs>
                        <linearGradient
                          id="userAreaGradient"
                          x1="0%"
                          y1="0%"
                          x2="0%"
                          y2="100%"
                        >
                          <stop
                            offset="0%"
                            style="stop-color: #0ea5e9; stop-opacity: 0.3"
                          />
                          <stop
                            offset="100%"
                            style="stop-color: #0ea5e9; stop-opacity: 0.05"
                          />
                        </linearGradient>
                      </defs>
                      <path
                        :d="userAreaPath"
                        fill="url(#userAreaGradient)"
                      />
                      <path
                        :d="userLinePath"
                        fill="none"
                        stroke="#0ea5e9"
                        stroke-width="3"
                        stroke-linecap="round"
                        stroke-linejoin="round"
                      />
                      <circle
                        v-for="(point, index) in userChartPoints"
                        :key="index"
                        :cx="point.x"
                        :cy="point.y"
                        r="5"
                        fill="#0ea5e9"
                        stroke-width="2"
                        class="stroke-white dark:stroke-gray-700"
                      >
                        <title>
                          {{ point.label }}: {{ point.value }} người dùng
                        </title>
                      </circle>
                      <text
                        v-for="(point, index) in userChartPoints"
                        :key="`user-label-${index}`"
                        :x="point.x"
                        :y="chartHeight - 10"
                        text-anchor="middle"
                        font-size="12"
                        class="fill-gray-500 dark:fill-gray-400"
                      >
                        {{ point.monthLabel }}
                      </text>
                      <text
                        v-for="(tick, index) in userYAxisTicks"
                        :key="`user-y-${index}`"
                        x="10"
                        :y="tick.y + 4"
                        font-size="12"
                        text-anchor="start"
                        class="fill-gray-500 dark:fill-gray-400"
                      >
                        {{ tick.value }}
                      </text>
                    </svg>
                  </div>
                </div>
                <div
                  class="bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg p-6"
                >
                  <h3
                    class="text-xl font-bold text-gray-900 dark:text-white mb-4"
                  >
                    Tăng trưởng bộ thẻ (6 tháng gần nhất)
                  </h3>
                  <div v-if="isLoadingGrowth" class="text-gray-500 dark:text-gray-400">
                    Đang tải dữ liệu...
                  </div>
                  <div v-else-if="growthError" class="text-red-500 dark:text-red-400">
                    {{ growthError }}
                  </div>
                  <div v-else-if="deckChartData.length > 0" class="relative">
                    <svg
                      :viewBox="`0 0 ${chartWidth} ${chartHeight}`"
                      class="w-full h-auto"
                    >
                      <defs>
                        <linearGradient
                          id="deckAreaGradient"
                          x1="0%"
                          y1="0%"
                          x2="0%"
                          y2="100%"
                        >
                          <stop
                            offset="0%"
                            style="stop-color: #10b981; stop-opacity: 0.3"
                          />
                          <stop
                            offset="100%"
                            style="stop-color: #10b981; stop-opacity: 0.05"
                          />
                        </linearGradient>
                      </defs>
                      <path :d="deckAreaPath" fill="url(#deckAreaGradient)" />
                      <path
                        :d="deckLinePath"
                        fill="none"
                        stroke="#10b981"
                        stroke-width="3"
                        stroke-linecap="round"
                        stroke-linejoin="round"
                      />
                      <circle
                        v-for="(point, index) in deckChartPoints"
                        :key="index"
                        :cx="point.x"
                        :cy="point.y"
                        r="5"
                        fill="#10b981"
                        stroke-width="2"
                        class="stroke-white dark:stroke-gray-700"
                      >
                        <title>
                          {{ point.label }}: {{ point.value }} bộ thẻ
                        </title>
                      </circle>
                      <text
                        v-for="(point, index) in deckChartPoints"
                        :key="`deck-label-${index}`"
                        :x="point.x"
                        :y="chartHeight - 10"
                        text-anchor="middle"
                        font-size="12"
                        class="fill-gray-500 dark:fill-gray-400"
                      >
                        {{ point.monthLabel }}
                      </text>
                      <text
                        v-for="(tick, index) in deckYAxisTicks"
                        :key="`deck-y-${index}`"
                        x="10"
                        :y="tick.y + 4"
                        font-size="12"
                        text-anchor="start"
                        class="fill-gray-500 dark:fill-gray-400"
                      >
                        {{ tick.value }}
                      </text>
                    </svg>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <div class="grid grid-cols-1 lg:grid-cols-2 gap-8 mb-8">
            <div
              class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6"
            >
              <h2
                class="text-2xl font-bold text-gray-900 dark:text-white mb-6"
              >
                Top từ khóa được tra cứu
              </h2>
              <div v-if="isLoadingOtherStats" class="text-gray-500 dark:text-gray-400">
                Đang tải...
              </div>
              <div v-else-if="otherStatsError" class="text-red-500 dark:text-red-400">
                {{ otherStatsError }}
              </div>
              <div v-else class="simple-table-container">
                <table class="simple-table">
                  <thead>
                    <tr>
                      <th>Từ khóa</th>
                      <th>Số lượt tìm</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in topSearchedWords" :key="item.term">
                      <td>{{ item.term }}</td>
                      <td>{{ item.count }}</td>
                    </tr>
                    <tr v-if="topSearchedWords.length === 0">
                      <td colspan="2" class="text-center">Không có dữ liệu.</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
            <div
              class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6"
            >
              <h2
                class="text-2xl font-bold text-gray-900 dark:text-white mb-6"
              >
                Top từ khóa tìm hụt
              </h2>
              <div v-if="isLoadingOtherStats" class="text-gray-500 dark:text-gray-400">
                Đang tải...
              </div>
              <div v-else-if="otherStatsError" class="text-red-500 dark:text-red-400">
                {{ otherStatsError }}
              </div>
              <div v-else class="simple-table-container">
                <table class="simple-table">
                  <thead>
                    <tr>
                      <th>Từ khóa</th>
                      <th>Số lượt tìm</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="item in topSearchMisses" :key="item.term">
                      <td>{{ item.term }}</td>
                      <td>{{ item.count }}</td>
                    </tr>
                    <tr v-if="topSearchMisses.length === 0">
                      <td colspan="2" class="text-center">Không có dữ liệu.</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        </div>

        <div v-if="activeTab === 'users'">
          <div
            class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6"
          >
            <h2
              class="text-2xl font-bold text-gray-900 dark:text-white mb-6"
            >
              Quản lý Người dùng
            </h2>
            <div class="mb-6">
              <div class="flex gap-4">
                <input
                  type="text"
                  v-model="searchTerm"
                  @keyup.enter="handleSearch"
                  placeholder="Tìm theo username, email..."
                  class="flex-1 form-input"
                />
                <button
                  @click="handleSearch"
                  :disabled="isSearching || !searchTerm.trim()"
                  class="form-button-primary px-6"
                >
                  <span v-if="isSearching">Đang tìm...</span
                  ><span v-else>Tìm kiếm</span>
                </button>
              </div>
            </div>
            <div v-if="isSearching" class="text-gray-500 dark:text-gray-400">
              Đang tìm kiếm...
            </div>
            <div v-else-if="searchError" class="text-red-500 dark:text-red-400">
              {{ searchError }}
            </div>
            <div v-else-if="searchResults.length > 0" class="space-y-4">
              <div class="text-gray-500 dark:text-gray-400 text-sm mb-4">
                Tìm thấy {{ searchTotalCount }} người dùng (trang
                {{ searchPage }}/{{ totalPages }})
              </div>
              <div class="space-y-2">
                <div
                  v-for="user in searchResults"
                  :key="user.id"
                  class="bg-gray-100 dark:bg-gray-700 rounded-lg p-4 transition-all hover:bg-gray-200 dark:hover:bg-gray-600 flex items-center gap-4"
                >
                  <div class="flex-shrink-0">
                    <img
                      :src="user.avatarUrl || '...'"
                      :alt="user.username"
                      class="w-12 h-12 rounded-full object-cover bg-gray-300 dark:bg-gray-600"
                    />
                  </div>
                  <div class="flex-1 min-w-0">
                    <div class="flex items-center gap-2">
                      <h3
                        class="text-lg font-semibold text-gray-900 dark:text-white truncate"
                      >
                        {{ user.username }}
                      </h3>
                      <span
                        v-if="user.role"
                        class="px-2 py-1 text-xs font-semibold rounded bg-sky-600 text-white flex-shrink-0"
                        >{{ user.role }}</span
                      >
                      <span
                        v-if="!user.isActive"
                        class="px-2 py-1 text-xs font-semibold rounded bg-red-600 text-white flex-shrink-0"
                        >ĐÃ KHÓA</span
                      >
                    </div>
                    <p
                      class="text-gray-600 dark:text-gray-400 text-sm truncate"
                    >
                      {{ user.email }}
                    </p>
                  </div>
                  <div class="flex-shrink-0">
                    <button
                      @click.stop="openUserModal(user)"
                      class="form-button-primary px-3 py-1.5 text-sm"
                      title="Chi tiết & Quản lý"
                    >
                      Quản lý
                    </button>
                  </div>
                </div>
              </div>
              <div
                v-if="totalPages > 1"
                class="flex items-center justify-center gap-4 mt-6"
              >
                <button
                  @click="changePage(searchPage - 1)"
                  :disabled="searchPage <= 1 || isSearching"
                  class="form-button-primary px-4 py-2 disabled:opacity-50"
                >
                  Trước
                </button>
                <span class="text-gray-500 dark:text-gray-400"
                  >Trang {{ searchPage }} / {{ totalPages }}</span
                >
                <button
                  @click="changePage(searchPage + 1)"
                  :disabled="searchPage >= totalPages || isSearching"
                  class="form-button-primary px-4 py-2 disabled:opacity-50"
                >
                  Sau
                </button>
              </div>
            </div>
            <div v-else-if="hasSearched" class="text-gray-500 dark:text-gray-400">
              Không tìm thấy người dùng nào.
            </div>
          </div>
        </div>

        <div v-if="activeTab === 'health'">
          <div
            class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6 mb-8"
          >
            <h2
              class="text-2xl font-bold text-gray-900 dark:text-white mb-6"
            >
              Giám sát Hạ tầng Azure (1 Giờ qua)
            </h2>
            <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
              <div
                class="bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg p-6"
              >
                <div v-if="azureVmMetadata" class="mb-4">
                  <h3
                    class="text-xl font-bold text-gray-900 dark:text-white"
                  >
                    {{ azureVmMetadata.name }}
                  </h3>
                  <div class="text-sm text-gray-600 dark:text-gray-400">
                    Đơn vị: {{ azureVmMetadata.unit }} | Khoảng thời gian: 1
                    giờ
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
                  <svg
                    :viewBox="`0 0 ${chartWidth} ${chartHeight}`"
                    class="w-full h-auto"
                  >
                    <defs>
                      <linearGradient
                        id="vmAreaGradient"
                        x1="0%"
                        y1="0%"
                        x2="0%"
                        y2="100%"
                      >
                        <stop
                          offset="0%"
                          style="stop-color: #d946ef; stop-opacity: 0.3"
                        />
                        <stop
                          offset="100%"
                          style="stop-color: #d946ef; stop-opacity: 0.05"
                        />
                      </linearGradient>
                    </defs>
                    <path :d="vmAreaPath" fill="url(#vmAreaGradient)" />
                    <path
                      :d="vmLinePath"
                      fill="none"
                      stroke="#d946ef"
                      stroke-width="3"
                      stroke-linecap="round"
                      stroke-linejoin="round"
                    />
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
                      <title>
                        {{ point.label }}: {{ point.value.toFixed(2) }} %
                      </title>
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
                    <summary
                      class="cursor-pointer text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-white"
                    >
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
                          <tr
                            v-for="item in azureVmMetrics"
                            :key="item.timeStamp"
                          >
                            <td>
                              {{
                                new Date(item.timeStamp).toLocaleString("vi-VN")
                              }}
                            </td>
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

              <div
                class="bg-white dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg p-6"
              >
                <div v-if="azureSqlMetadata" class="mb-4">
                  <h3
                    class="text-xl font-bold text-gray-900 dark:text-white"
                  >
                    {{ azureSqlMetadata.name }}
                  </h3>
                  <div class="text-sm text-gray-600 dark:text-gray-400">
                    Đơn vị: {{ azureSqlMetadata.unit }} | Khoảng thời gian: 1
                    giờ
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
                  <svg
                    :viewBox="`0 0 ${chartWidth} ${chartHeight}`"
                    class="w-full h-auto"
                  >
                    <defs>
                      <linearGradient
                        id="sqlAreaGradient"
                        x1="0%"
                        y1="0%"
                        x2="0%"
                        y2="100%"
                      >
                        <stop
                          offset="0%"
                          style="stop-color: #f97316; stop-opacity: 0.3"
                        />
                        <stop
                          offset="100%"
                          style="stop-color: #f97316; stop-opacity: 0.05"
                        />
                      </linearGradient>
                    </defs>
                    <path :d="sqlAreaPath" fill="url(#sqlAreaGradient)" />
                    <path
                      :d="sqlLinePath"
                      fill="none"
                      stroke="#f97316"
                      stroke-width="3"
                      stroke-linecap="round"
                      stroke-linejoin="round"
                    />
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
                      <title>
                        {{ point.label }}: {{ point.value.toFixed(2) }} %
                      </title>
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
                    <summary
                      class="cursor-pointer text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-white"
                    >
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
                          <tr
                            v-for="item in azureSqlMetrics"
                            :key="item.timeStamp"
                          >
                            <td>
                              {{
                                new Date(item.timeStamp).toLocaleString("vi-VN")
                              }}
                            </td>
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
          </div>

          <div class="grid grid-cols-1 lg:grid-cols-2 gap-8 mb-8">
            <div
              class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6"
            >
              <h2
                class="text-2xl font-bold text-gray-900 dark:text-white mb-6"
              >
                Hiệu suất API
              </h2>
              <div v-if="isLoadingOtherStats" class="text-gray-500 dark:text-gray-400">
                Đang tải...
              </div>
              <div v-else-if="otherStatsError" class="text-red-500 dark:text-red-400">
                {{ otherStatsError }}
              </div>
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
                      <td>
                        {{ item.averageResponseTimeMs?.toFixed(0) || 0 }} ms
                      </td>
                      <td>{{ (item.errorRate * 100).toFixed(1) }} %</td>
                    </tr>
                    <tr v-if="apiPerformance.length === 0">
                      <td colspan="3" class="text-center">Không có dữ liệu.</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
            <div
              class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6"
            >
              <h2
                class="text-2xl font-bold text-gray-900 dark:text-white mb-6"
              >
                Job hệ thống bị lỗi
              </h2>
              <div v-if="isLoadingOtherStats" class="text-gray-500 dark:text-gray-400">
                Đang tải...
              </div>
              <div v-else-if="otherStatsError" class="text-red-500 dark:text-red-400">
                {{ otherStatsError }}
              </div>
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
                      <td>
                        {{ new Date(item.failedAt).toLocaleString("vi-VN") }}
                      </td>
                      <td class="break-all text-red-500 dark:text-red-400">
                        {{ item.errorMessage }}
                      </td>
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
        <div v-if="activeTab === 'content'">
          <div
            class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6"
          >
            <h2
              class="text-2xl font-bold text-gray-900 dark:text-white mb-6"
            >
              Quản lý Flashcard (Bộ thẻ)
            </h2>

            <div class="mb-6">
              <div class="flex gap-4">
                <input
                  type="text"
                  v-model="deckSearchTerm"
                  @keyup.enter="handleDeckSearch"
                  placeholder="Tìm theo tên bộ thẻ, tên tác giả..."
                  class="flex-1 form-input"
                />
                <button
                  @click="handleDeckSearch"
                  :disabled="isDeckSearching"
                  class="form-button-primary px-6"
                >
                  <span v-if="isDeckSearching">Đang tìm...</span>
                  <span v-else>Tìm kiếm</span>
                </button>
              </div>
            </div>

            <div v-if="isDeckSearching" class="text-gray-500 dark:text-gray-400">
              Đang tìm kiếm bộ thẻ...
            </div>
            <div v-else-if="deckSearchError" class="text-red-500 dark:text-red-400">
              {{ deckSearchError }}
            </div>
            <div v-else-if="deckResults.length > 0" class="space-y-4">
              <div class="text-gray-500 dark:text-gray-400 text-sm mb-4">
                Tìm thấy {{ deckSearchTotalCount }} bộ thẻ (trang
                {{ deckSearchPage }}/{{ deckTotalPages }})
              </div>
              <div class="space-y-3">
                <div
                  v-for="deck in deckResults"
                  :key="deck.id"
                  class="bg-gray-100 dark:bg-gray-700 rounded-lg p-4 transition-all"
                >
                  <div class="flex items-center gap-4">
                    <div class="flex-1 min-w-0">
                      <div class="flex items-center gap-3">
                        <h3
                          class="text-lg font-semibold text-gray-900 dark:text-white truncate"
                          :title="deck.name"
                        >
                          {{ deck.name }}
                        </h3>
                        <span
                          :class="[
                            'px-2 py-0.5 text-xs font-semibold rounded',
                            deck.isPublic
                              ? 'bg-green-600 text-white'
                              : 'bg-gray-500 text-gray-200',
                          ]"
                        >
                          {{ deck.isPublic ? "Public" : "Private" }}
                        </span>
                      </div>
                      <p
                        class="text-gray-600 dark:text-gray-400 text-sm truncate"
                        :title="deck.description"
                      >
                        {{ deck.description || "Không có mô tả" }}
                      </p>
                      <div
                        class="flex items-center gap-4 mt-2 text-xs text-gray-500 dark:text-gray-400"
                      >
                        <span>ID: {{ deck.id }}</span>
                        <span
                          >Tác giả: {{ deck.authorName }} (ID:
                          {{ deck.authorId }})</span
                        >
                        <span>Số thẻ: {{ deck.cardCount }}</span>
                      </div>
                    </div>

                    <div class="flex-shrink-0 flex items-center gap-2">
                      <button
                        @click="handleSetVisibility(deck)"
                        :disabled="isActionLoading === deck.id"
                        :class="[
                          'px-3 py-1.5 text-sm rounded-lg transition-colors font-medium text-white',
                          deck.isPublic
                            ? 'bg-yellow-600 hover:bg-yellow-700'
                            : 'bg-sky-600 hover:bg-sky-700',
                        ]"
                        :title="
                          deck.isPublic
                            ? 'Nhấn để Ẩn (Set Private)'
                            : 'Nhấn để Hiện (Set Public)'
                        "
                      >
                        {{ deck.isPublic ? "Ẩn" : "Hiện" }}
                      </button>
                      <button
                        @click="handleDeleteDeck(deck.id, deck.name)"
                        :disabled="isActionLoading === deck.id"
                        class="px-3 py-1.5 text-sm bg-red-600 hover:bg-red-700 text-white rounded-lg transition-colors font-medium"
                        title="Xóa vĩnh viễn bộ thẻ"
                      >
                        Xóa
                      </button>
                    </div>
                  </div>
                </div>
              </div>

              <div
                v-if="deckTotalPages > 1"
                class="flex items-center justify-center gap-4 mt-6"
              >
                <button
                  @click="changeDeckPage(deckSearchPage - 1)"
                  :disabled="deckSearchPage <= 1 || isDeckSearching"
                  class="form-button-primary px-4 py-2 disabled:opacity-50"
                >
                  Trước
                </button>
                <span class="text-gray-500 dark:text-gray-400">
                  Trang {{ deckSearchPage }} / {{ deckTotalPages }}
                </span>
                <button
                  @click="changeDeckPage(deckSearchPage + 1)"
                  :disabled="
                    deckSearchPage >= deckTotalPages || isDeckSearching
                  "
                  class="form-button-primary px-4 py-2 disabled:opacity-50"
                >
                  Sau
                </button>
              </div>
            </div>
            <div v-else-if="hasDeckSearched" class="text-gray-500 dark:text-gray-400">
              Không tìm thấy bộ thẻ nào.
            </div>
          </div>
        </div>
        <UserDetailModal
          :is-open="isUserModalOpen"
          :user="selectedUser"
          :on-lock="handleLockUser"
          :on-update-role="handleUpdateRole"
          :on-reset-password="handleResetPassword"
          :on-delete="handleDeleteUser"
          @close="closeUserModal"
          @refresh-users="fetchUsers"
        />
      </div>
    </div>
  </ClientOnly>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from "vue"; // Đảm bảo có 'watch'
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

// ===================================
// TYPES
// ===================================
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
}
interface DeckAdminDto {
  id: number;
  name: string;
  description: string;
  isPublic: boolean;
  cardCount: number;
  authorId: number;
  authorName: string;
  createdAt: string;
}
interface DeckSearchResponse {
  items: DeckAdminDto[];
  totalCount: number;
  page: number;
  pageSize: number;
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
  items: User[]; // Đã sửa từ 'users' thành 'items'
  totalCount: number;
  page: number;
  pageSize: number;
}
interface MonthlyGrowthData {
  month: string;
  newUserCount: number;
  newDeckCount: number;
}
interface GrowthResponse {
  monthlyData: MonthlyGrowthData[];
}
interface TopSearchedWord {
  term: string;
  count: number;
}
interface TopSearchMiss {
  term: string;
  count: number;
}
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

// ===================================
// TAB STATE
// ===================================
const activeTab = ref("overview");
const tabs = [
  { id: "overview", name: "Tổng quan" },
  { id: "users", name: "Quản lý User" },
  { id: "health", name: "Sức khỏe Hệ thống" },
  { id: "content", name: "Quản lý Flashcard" },
];

// ===================================
// STATE (Tab 1)
// ===================================
const statistics = ref<Statistics | null>(null);
const isLoadingStats = ref(false);
const statsError = ref<string | null>(null);
const growthData = ref<MonthlyGrowthData[]>([]);
const isLoadingGrowth = ref(false);
const growthError = ref<string | null>(null);

// ===================================
// STATE (Tab 1 & 3 - Chung)
// ===================================
const topSearchedWords = ref<TopSearchedWord[]>([]);
const topSearchMisses = ref<TopSearchMiss[]>([]);
const apiPerformance = ref<ApiPerformanceStat[]>([]);
const failedJobs = ref<FailedJob[]>([]);
const isLoadingOtherStats = ref(false);
const otherStatsError = ref<string | null>(null);

// ===================================
// STATE (Tab 3 - Azure)
// ===================================
const azureVmMetrics = ref<AzureMetricDataPoint[]>([]);
const azureVmMetadata = ref<AzureMetricMetadata | null>(null);
const isLoadingVmMetrics = ref(false);
const vmMetricsError = ref<string | null>(null);
const azureSqlMetrics = ref<AzureMetricDataPoint[]>([]);
const azureSqlMetadata = ref<AzureMetricMetadata | null>(null);
const isLoadingSqlMetrics = ref(false);
const sqlMetricsError = ref<string | null>(null);

// ===================================
// STATE (Tab 2 - User)
// ===================================
const searchTerm = ref("");
const searchResults = ref<User[]>([]);
const searchTotalCount = ref(0);
const searchPage = ref(1);
const pageSize = ref(10);
const isSearching = ref(false);
const searchError = ref<string | null>(null);
const hasSearched = ref(false);
const totalPages = computed(() =>
  Math.ceil(searchTotalCount.value / pageSize.value)
);
const isUserModalOpen = ref(false);
const selectedUser = ref<User | null>(null);
const hasUserTabLoaded = ref(false);

// ===================================
// STATE (Tab 4 - Content)
// ===================================
const deckSearchTerm = ref("");
const deckResults = ref<DeckAdminDto[]>([]);
const deckSearchTotalCount = ref(0);
const deckSearchPage = ref(1);
const deckPageSize = ref(10);
const isDeckSearching = ref(false);
const deckSearchError = ref<string | null>(null);
const hasDeckSearched = ref(false);
const deckTotalPages = computed(() =>
  Math.ceil(deckSearchTotalCount.value / deckPageSize.value)
);
const hasDeckTabLoaded = ref(false);
const isActionLoading = ref<number | null>(null);

// ===================================
// API CALLER (Generic)
// ===================================
async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const errorData = await response
      .json()
      .catch(() => ({ message: response.statusText }));
    throw new Error(
      errorData.message || `Yêu cầu thất bại: ${response.status}`
    );
  }
  if (response.status === 204) return {} as T;
  const data = await response.json();
  if (data.isSuccess === false) {
    throw new Error(data.message || "Lỗi không xác định từ API");
  }
  return data.result;
}

// ===================================
// FETCH FUNCTIONS (Tab 1, 3)
// ===================================
async function fetchStatistics() {
  isLoadingStats.value = true;
  statsError.value = null;
  try {
    const response = await fetch(`${BASE_URL}/api/Admin/statistics`, {
      headers: { Authorization: `Bearer ${jwt.value}` },
    });
    statistics.value = await handleResponse<Statistics>(response);
  } catch (err: any) {
    statsError.value = err.message;
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
    growthError.value = err.message;
  } finally {
    isLoadingGrowth.value = false;
  }
}

async function fetchOtherStats() {
  isLoadingOtherStats.value = true;
  otherStatsError.value = null;
  try {
    const [searched, misses, performance, jobs] = await Promise.all([
      fetch(`${BASE_URL}/api/Admin/statistics/top-searched`, {
        headers: { Authorization: `Bearer ${jwt.value}` },
      }),
      fetch(`${BASE_URL}/api/Admin/statistics/search-misses`, {
        headers: { Authorization: `Bearer ${jwt.value}` },
      }),
      fetch(`${BASE_URL}/api/Admin/statistics/api-performance`, {
        headers: { Authorization: `Bearer ${jwt.value}` },
      }),
      fetch(`${BASE_URL}/api/Admin/statistics/failed-jobs`, {
        headers: { Authorization: `Bearer ${jwt.value}` },
      }),
    ]);
    topSearchedWords.value = await handleResponse<TopSearchedWord[]>(searched);
    topSearchMisses.value = await handleResponse<TopSearchMiss[]>(misses);
    apiPerformance.value = await handleResponse<ApiPerformanceStat[]>(
      performance
    );
    failedJobs.value = await handleResponse<FailedJob[]>(jobs);
  } catch (err: any) {
    otherStatsError.value = err.message;
    showToast(`Lỗi: ${err.message}`, "error");
  } finally {
    isLoadingOtherStats.value = false;
  }
}

async function fetchVmCpu() {
  isLoadingVmMetrics.value = true;
  vmMetricsError.value = null;
  try {
    const response = await fetch(`${BASE_URL}/api/Admin/azure/vm-cpu`, {
      headers: { Authorization: `Bearer ${jwt.value}` },
    });
    const jsonString = await handleResponse<string>(response);
    const parsedResult = JSON.parse(jsonString);
    const data = parsedResult.value[0].timeseries[0].data;
    azureVmMetrics.value = data;
    azureVmMetadata.value = {
      name: parsedResult.value[0].name.localizedValue,
      unit: parsedResult.value[0].unit,
      timespan: parsedResult.timespan,
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
    const data = parsedResult.value[0].timeseries[0].data;
    azureSqlMetrics.value = data;
    azureSqlMetadata.value = {
      name: parsedResult.value[0].name.localizedValue,
      unit: parsedResult.value[0].unit,
      timespan: parsedResult.timespan,
      latestValue: data.length > 0 ? data[data.length - 1].average : null,
    };
  } catch (err: any) {
    sqlMetricsError.value = err.message;
  } finally {
    isLoadingSqlMetrics.value = false;
  }
}

// ===================================
// FETCH FUNCTIONS (Tab 4 - Content)
// ===================================
async function loadInitialDecks() {
  if (hasDeckTabLoaded.value) return;
  hasDeckSearched.value = true;
  isDeckSearching.value = true;
  await fetchDecks();
  isDeckSearching.value = false;
  hasDeckTabLoaded.value = true;
}

async function handleDeckSearch() {
  isDeckSearching.value = true;
  deckSearchError.value = null;
  deckSearchPage.value = 1;
  await fetchDecks();
  isDeckSearching.value = false;
  hasDeckSearched.value = true;
}

async function changeDeckPage(newPage: number) {
  if (newPage < 1 || newPage > deckTotalPages.value) return;
  deckSearchPage.value = newPage;
  isDeckSearching.value = true;
  await fetchDecks();
  isDeckSearching.value = false;
}

async function fetchDecks() {
  try {
    const response = await fetch(
      `${BASE_URL}/api/Admin/decks/search?searchTerm=${encodeURIComponent(
        deckSearchTerm.value.trim()
      )}&page=${deckSearchPage.value}&pageSize=${deckPageSize.value}`,
      { headers: { Authorization: `Bearer ${jwt.value}` } }
    );
    const result = await handleResponse<DeckSearchResponse>(response);
    deckResults.value = result.items; // Đảm bảo dùng .items
    deckSearchTotalCount.value = result.totalCount;
  } catch (err: any) {
    deckSearchError.value = err.message;
    deckResults.value = [];
    showToast(`Lỗi: ${err.message}`, "error");
  }
}

async function handleSetVisibility(deck: DeckAdminDto) {
  const newStatus = !deck.isPublic;
  const action = newStatus ? "HIỆN" : "ẨN";
  //if (!confirm(`Bạn có chắc muốn ${action} bộ thẻ "${deck.name}"?`)) return;
  isActionLoading.value = deck.id;
  try {
    const response = await fetch(
      `${BASE_URL}/api/Admin/decks/${deck.id}/visibility`,
      {
        method: "PATCH",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${jwt.value}`,
        },
        body: JSON.stringify({ isPublic: newStatus }),
      }
    );
    await handleResponse(response);
    showToast(`Đã ${action} bộ thẻ thành công.`, "success");
    deck.isPublic = newStatus;
  } catch (err: any) {
    showToast(`Lỗi: ${err.message}`, "error");
  } finally {
    isActionLoading.value = null;
  }
}

async function handleDeleteDeck(deckId: number, deckName: string) {
  if (
    !confirm(
      `Bạn có chắc chắn muốn XÓA VĨNH VIỄN bộ thẻ "${deckName}" (ID: ${deckId})?`
    )
  )
    return;
  isActionLoading.value = deckId;
  try {
    const response = await fetch(`${BASE_URL}/api/Admin/decks/${deckId}`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${jwt.value}` },
    });
    await handleResponse(response);
    showToast("Đã xóa bộ thẻ vĩnh viễn.", "success");
    deckResults.value = deckResults.value.filter((d) => d.id !== deckId);
    deckSearchTotalCount.value--;
  } catch (err: any) {
    showToast(`Lỗi: ${err.message}`, "error");
  } finally {
    isActionLoading.value = null;
  }
}

// ===================================
// CHART LOGIC (Generic)
// ===================================
const chartWidth = 800;
const chartHeight = 300;
const padding = { top: 20, right: 40, bottom: 40, left: 50 };
interface ChartDataPoint {
  value: number;
  label: string;
  monthLabel: string;
}
const last6MonthsData = computed(() =>
  growthData.value.length > 0 ? growthData.value.slice(-6) : []
);
const userChartData = computed(() => {
  const data = last6MonthsData.value;
  if (data.length === 0) return [];
  let cumulative = 0;
  return data.map((item) => {
    cumulative += item.newUserCount;
    const [year, month] = item.month.split("-").map(Number);
    const date = new Date(year, month - 1, 1);
    return {
      value: cumulative,
      label: date.toLocaleDateString("vi-VN", {
        month: "short",
        year: "numeric",
      }),
      monthLabel: date.toLocaleDateString("vi-VN", { month: "short" }),
    };
  });
});
const deckChartData = computed(() => {
  const data = last6MonthsData.value;
  if (data.length === 0) return [];
  let cumulative = 0;
  return data.map((item) => {
    cumulative += item.newDeckCount;
    const [year, month] = item.month.split("-").map(Number);
    const date = new Date(year, month - 1, 1);
    return {
      value: cumulative,
      label: date.toLocaleDateString("vi-VN", {
        month: "short",
        year: "numeric",
      }),
      monthLabel: date.toLocaleDateString("vi-VN", { month: "short" }),
    };
  });
});
const vmChartData = computed((): ChartDataPoint[] => {
  const data = azureVmMetrics.value;
  if (data.length === 0) return [];
  return data.map((item) => {
    const date = new Date(item.timeStamp);
    return {
      value: item.average,
      label: date.toLocaleString("vi-VN"),
      monthLabel: date.toLocaleTimeString("vi-VN", {
        hour: "2-digit",
        minute: "2-digit",
      }),
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
      monthLabel: date.toLocaleTimeString("vi-VN", {
        hour: "2-digit",
        minute: "2-digit",
      }),
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
    const y =
      padding.top +
      plotHeight -
      ((point.value - minValue) / (maxValue - minValue || 1)) * plotHeight;
    return {
      x,
      y,
      value: point.value,
      label: point.label,
      monthLabel: point.monthLabel,
    };
  });
}
const userChartPoints = computed(() => createChartPoints(userChartData.value));
const deckChartPoints = computed(() => createChartPoints(deckChartData.value));
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
const userLinePath = computed(() => createLinePath(userChartPoints.value));
const deckLinePath = computed(() => createLinePath(deckChartPoints.value));
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
  path += ` L ${points[points.length - 1].x} ${bottomY}`;
  path += " Z";
  return path;
}
const userAreaPath = computed(() => createAreaPath(userChartPoints.value));
const deckAreaPath = computed(() => createAreaPath(deckChartPoints.value));
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
const userYAxisTicks = computed(() => createYAxisTicks(userChartData.value));
const deckYAxisTicks = computed(() => createYAxisTicks(deckChartData.value));
const vmYAxisTicks = computed(() => createYAxisTicks(vmChartData.value));
const sqlYAxisTicks = computed(() => createYAxisTicks(sqlChartData.value));

// ===================================
// USER SEARCH & ACTIONS (Tab 2)
// ===================================
async function loadInitialUsers() {
  if (hasUserTabLoaded.value) return;
  hasSearched.value = true;
  isSearching.value = true;
  await fetchUsers();
  isSearching.value = false;
  hasUserTabLoaded.value = true;
}

async function handleSearch() {
  isSearching.value = true;
  searchError.value = null;
  searchPage.value = 1;
  await fetchUsers();
  isSearching.value = false;
  hasSearched.value = true;
}

async function changePage(newPage: number) {
  if (newPage < 1 || newPage > totalPages.value) return;
  searchPage.value = newPage;
  isSearching.value = true;
  await fetchUsers();
  isSearching.value = false;
}

async function fetchUsers() {
  try {
    const response = await fetch(
      `${BASE_URL}/api/Admin/users/search?searchTerm=${encodeURIComponent(
        searchTerm.value.trim()
      )}&page=${searchPage.value}&pageSize=${pageSize.value}`,
      { headers: { Authorization: `Bearer ${jwt.value}` } }
    );
    const result = await handleResponse<SearchResponse>(response);
    searchResults.value = result.items; // Đã sửa thành .items
    searchTotalCount.value = result.totalCount;
  } catch (err: any) {
    searchError.value = err.message;
    searchResults.value = [];
    showToast(`Lỗi: ${err.message}`, "error");
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

async function handleLockUser(
  userId: number,
  isLocked: boolean
): Promise<boolean> {
  try {
    const response = await fetch(
      `${BASE_URL}/api/Admin/users/${userId}/lock-status`,
      {
        method: "PATCH",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${jwt.value}`,
        },
        body: JSON.stringify({ isLocked: isLocked }),
      }
    );
    await handleResponse(response);
    showToast(`Đã ${isLocked ? "khóa" : "mở khóa"} người dùng.`, "success");
    return true;
  } catch (err: any) {
    showToast(`Lỗi: ${err.message}`, "error");
    return false;
  }
}

async function handleUpdateRole(
  userId: number,
  roleNames: string[]
): Promise<boolean> {
  try {
    const response = await fetch(
      `${BASE_URL}/api/Admin/users/${userId}/roles`,
      {
        method: "PATCH",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${jwt.value}`,
        },
        body: JSON.stringify({ roleNames: roleNames }),
      }
    );
    await handleResponse(response);
    showToast("Đã cập nhật vai trò người dùng.", "success");
    return true;
  } catch (err: any) {
    showToast(`Lỗi: ${err.message}`, "error");
    return false;
  }
}

async function handleResetPassword(
  userId: number,
  newPassword: string
): Promise<boolean> {
  try {
    const response = await fetch(
      `${BASE_URL}/api/Admin/users/${userId}/reset-password`,
      {
        method: "PATCH",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${jwt.value}`,
        },
        body: JSON.stringify({ newPassword: newPassword }),
      }
    );
    await handleResponse(response);
    showToast("Đã đặt lại mật khẩu người dùng.", "success");
    return true;
  } catch (err: any) {
    showToast(`Lỗi: ${err.message}`, "error");
    return false;
  }
}

async function handleDeleteUser(userId: number): Promise<boolean> {
  try {
    const response = await fetch(`${BASE_URL}/api/Admin/users/${userId}`, {
      method: "DELETE",
      headers: { Authorization: `Bearer ${jwt.value}` },
    });
    await handleResponse(response);
    showToast("Đã XÓA VĨNH VIỄN người dùng.", "success");
    return true;
  } catch (err: any) {
    showToast(`Lỗi: ${err.message}`, "error");
    return false;
  }
}

// ===================================
// ON MOUNTED & WATCH (ĐÃ SỬA GỌN)
// ===================================

watch(activeTab, (newTab) => {
  if (newTab === "users") {
    loadInitialUsers();
  }
  if (newTab === "content") {
    loadInitialDecks();
  }
});

onMounted(() => {
  // Tải dữ liệu cho các tab mặc định (Tổng quan và Sức khỏe)
  fetchStatistics();
  fetchGrowthData();
  fetchOtherStats();
  fetchVmCpu();
  fetchSqlDbCpu();
});
</script>

<style scoped>
/* ✅ THAY ĐỔI:
  - Tái cấu trúc lại toàn bộ CSS
  - Thêm style cho light mode (mặc định)
  - Bọc style cho dark mode (cũ) trong class .dark
*/

/* --- LIGHT MODE (MẶC ĐỊNH) --- */
.form-input {
  width: 100%;
  background-color: #ffffff;
  border: 1px solid #d1d5db; /* border-gray-300 */
  border-radius: 0.5rem;
  padding: 0.75rem 1rem;
  color: #111827; /* text-gray-900 */
  outline: none;
  transition: border-color 0.2s, box-shadow 0.2s;
}
.form-input:focus {
  border-color: #2563eb; /* primary-600 */
  box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.4);
}
.form-input::placeholder {
  color: #9ca3af; /* text-gray-400 */
}

.form-button-primary {
  background-color: #2563eb; /* primary-600 */
  color: white;
  font-weight: 600;
  padding: 0.75rem 1.5rem;
  border-radius: 0.5rem;
  transition: background-color 0.2s;
  border: none;
  cursor: pointer;
}
.form-button-primary:hover:not(:disabled) {
  background-color: #1d4ed8; /* primary-700 */
}
.form-button-primary:disabled {
  background-color: #e5e7eb; /* bg-gray-200 */
  color: #9ca3af; /* text-gray-400 */
  opacity: 0.7;
  cursor: not-allowed;
}

/* (Style cho bảng) */
.simple-table-container {
  max-height: 400px;
  overflow-y: auto;
  border: 1px solid #e5e7eb; /* border-gray-200 */
  border-radius: 0.5rem;
}
.simple-table {
  width: 100%;
  border-collapse: collapse;
}
.simple-table th,
.simple-table td {
  padding: 0.75rem 1rem;
  text-align: left;
  border-bottom: 1px solid #e5e7eb; /* border-gray-200 */
  color: #111827; /* text-gray-900 */
}
.dark .simple-table td {
  color: #e5e7eb; /* text-gray-200 */
}
.simple-table th {
  background-color: #f9fafb; /* bg-gray-50 */
  font-weight: 600;
  position: sticky;
  top: 0;
  color: #374151; /* text-gray-700 */
}
.simple-table tbody tr:last-child td {
  border-bottom: none;
}
.simple-table tbody tr:hover {
  background-color: #f9fafb; /* bg-gray-50 */
}
.simple-table td.text-center {
  text-align: center;
  color: #6b7280; /* text-gray-500 */
  padding: 1.5rem;
}
.dark .simple-table td.text-center {
  color: #6b7280; /* text-gray-500 */
}
.simple-table td.break-all {
  word-break: break-all;
}

/* --- DARK MODE --- */
.dark .form-input {
  background-color: #374151;
  border: 1px solid #4b5563;
  color: white;
}
.dark .form-input:focus {
  border-color: #0ea5e9;
  box-shadow: 0 0 0 2px rgba(14, 165, 233, 0.4);
}
.dark .form-input::placeholder {
  color: #9ca3af;
}

.dark .form-button-primary {
  background-color: #0ea5e9; /* sky-500 */
  color: white;
}
.dark .form-button-primary:hover:not(:disabled) {
  background-color: #0284c7; /* sky-600 */
}
.dark .form-button-primary:disabled {
  background-color: #4b5563;
  opacity: 0.5;
  color: #9ca3af;
}

.dark .simple-table-container {
  border: 1px solid #4b5563;
}
.dark .simple-table th,
.dark .simple-table td {
  border-bottom: 1px solid #4b5563;
}
.dark .simple-table th {
  background-color: #374151;
  color: #e5e7eb; /* text-gray-200 */
}
.dark .simple-table tbody tr:hover {
  background-color: #3f4a5c;
}
</style>