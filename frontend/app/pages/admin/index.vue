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
];
</script>