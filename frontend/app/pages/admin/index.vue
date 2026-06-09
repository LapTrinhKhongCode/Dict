<template>
  <ClientOnly>
    <div class="min-h-screen bg-gray-50 text-gray-900 dark:bg-gray-900 dark:text-white p-4 sm:p-8">
      <div class="max-w-7xl mx-auto">
        <h1 class="text-3xl font-bold text-gray-900 dark:text-white mb-6">
          Admin Dashboard
        </h1>

        <div class="mb-8">
          <nav class="flex flex-wrap space-x-4 border-b border-gray-200 dark:border-gray-700 gap-y-2">
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

        <AdminOverview v-if="activeTab === 'overview'" />
        <AdminUserManager v-if="activeTab === 'users'" />
        <AdminHealth v-if="activeTab === 'health'" />
        <AdminContentManager v-if="activeTab === 'content'" />
        <AdminWorkspaceManager v-if="activeTab === 'workspaces'" />
        <div v-if="activeTab === 'dictionary'" class="py-6">
          <NuxtLink to="/admin/word" class="inline-flex items-center gap-2 px-5 py-3 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-xl transition-colors">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.75 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253"/></svg>
            Mở trang Quản lý Từ điển
          </NuxtLink>
          <p class="text-sm text-gray-500 mt-2">Trang quản lý đầy đủ với tìm kiếm, phân trang, sửa/xóa từ.</p>
        </div>
        
      </div>
    </div>
  </ClientOnly>
</template>

<script setup lang="ts">
import { ref } from "vue";
import AdminOverview from "~/components/admin/AdminOverview.vue";
import AdminUserManager from "~/components/admin/AdminUserManager.vue";
import AdminHealth from "~/components/admin/AdminHealth.vue";
import AdminContentManager from "~/components/admin/AdminContentManager.vue";
import AdminWorkspaceManager from "~/components/admin/AdminWorkspaceManager.vue";

definePageMeta({
  title: "Admin Page",
  middleware: "admin-only-client",
});

const activeTab = ref("overview");

const tabs = [
  { id: "overview", name: "Tổng quan" },
  { id: "users", name: "Quản lý User" },
  { id: "health", name: "Sức khỏe Hệ thống" },
  { id: "content", name: "Quản lý Flashcard" },
  { id: "workspaces", name: "Quản lý Workspace" },
  { id: "dictionary", name: "Từ điển" },
];
</script>