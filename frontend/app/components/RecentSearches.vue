<template>
  <div v-if="recentSearches.length > 0" class="space-y-4">
    <div class="flex items-center justify-between">
      <h2 class="text-lg font-semibold text-gray-800 dark:text-gray-200 flex items-center gap-2">
        <UIcon name="i-lucide-history" class="size-5" />
        <span>Lịch sử tra cứu</span>
      </h2>
      <button
        @click="handleClear"
        class="text-sm text-gray-500 hover:text-red-500 dark:text-gray-400 dark:hover:text-red-400 transition-colors flex items-center gap-1"
      >
        <UIcon name="i-lucide-trash-2" class="size-4" />
        <span>Xóa tất cả</span>
      </button>
    </div>

    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5 gap-3">
      <div
        v-for="entry in recentSearches"
        :key="entry.word"
        @click="handleClick(entry.word)"
        class="group relative bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-xl p-4 cursor-pointer hover:border-primary-400 dark:hover:border-green-500 hover:shadow-md transition-all"
      >
        <!-- Remove button -->
        <button
          @click.stop="handleRemove(entry.word)"
          class="absolute top-2 right-2 opacity-0 group-hover:opacity-100 text-gray-400 hover:text-red-500 dark:text-gray-500 dark:hover:text-red-400 transition-opacity"
          title="Xóa"
        >
          <UIcon name="i-lucide-x" class="size-4" />
        </button>

        <!-- Word -->
        <h3 class="font-bold text-lg text-gray-900 dark:text-white truncate pr-6">
          {{ entry.word }}
        </h3>

        <!-- Phonetic (yomigata) -->
        <p class="text-sm text-blue-600 dark:text-blue-400 truncate">
          {{ entry.phonetic }}
        </p>

        <!-- Short meaning -->
        <p class="text-sm text-gray-600 dark:text-gray-400 mt-1 line-clamp-2">
          {{ entry.short_mean }}
        </p>
      </div>
    </div>
  </div>

  <!-- Empty state -->
  <div v-else class="text-center py-12">
    <UIcon name="i-lucide-search" class="size-12 text-gray-300 dark:text-gray-600 mx-auto mb-4" />
    <p class="text-gray-500 dark:text-gray-400">
      Chưa có lịch sử tra cứu
    </p>
    <p class="text-gray-400 dark:text-gray-500 text-sm mt-1">
      Hãy tìm kiếm từ vựng để bắt đầu
    </p>
  </div>
</template>

<script setup lang="ts">
import { useRecentSearches } from '~/composables/useRecentSearches';
import { useRouter } from 'vue-router';

const router = useRouter();
const { recentSearches, clearRecentSearches, removeRecentSearch } = useRecentSearches();

const handleClick = (word: string) => {
  router.push({
    path: '/search',
    query: { q: word, view: 'word' },
  });
};

const handleRemove = (word: string) => {
  removeRecentSearch(word);
};

const handleClear = () => {
  clearRecentSearches();
};
</script>

<style scoped>
.line-clamp-2 {
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
</style>
