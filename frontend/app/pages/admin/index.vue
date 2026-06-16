<template>
  <ClientOnly>
    <div class="min-h-screen bg-gray-50 text-gray-900 dark:bg-gray-900 dark:text-white p-4 sm:p-8">
      <div class="max-w-7xl mx-auto">

        <!-- Header với role badge -->
        <div class="flex items-center justify-between mb-6">
          <div>
            <h1 class="text-3xl font-bold text-gray-900 dark:text-white">Admin Dashboard</h1>
            <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
              Đăng nhập với quyền
              <span class="font-semibold px-2 py-0.5 rounded-full text-xs"
                :class="currentRole === 'ADMIN' ? 'bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400' : 'bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400'">
                {{ currentRole }}
              </span>
            </p>
          </div>
        </div>

        <div class="mb-8">
          <nav class="flex flex-wrap space-x-1 border-b border-gray-200 dark:border-gray-700 gap-y-2">
            <button
              v-for="tab in visibleTabs"
              :key="tab.id"
              @click="activeTab = tab.id"
              :class="[
                'flex items-center gap-2 px-4 py-3 font-medium text-sm rounded-t-lg transition-colors',
                activeTab === tab.id
                  ? 'border-b-2 border-primary-500 text-primary-500 dark:border-sky-500 dark:text-sky-400'
                  : 'text-gray-500 hover:text-gray-700 hover:bg-gray-100 dark:text-gray-400 dark:hover:text-white dark:hover:bg-gray-800',
              ]"
            >
              <span>{{ tab.icon }}</span>
              {{ tab.name }}
              <span v-if="tab.adminOnly" class="text-xs bg-red-100 dark:bg-red-900/30 text-red-600 dark:text-red-400 px-1.5 py-0.5 rounded-full">ADMIN</span>
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
import { ref, computed } from "vue";
import AdminOverview from "~/components/admin/AdminOverview.vue";
import AdminUserManager from "~/components/admin/AdminUserManager.vue";
import AdminHealth from "~/components/admin/AdminHealth.vue";
import AdminContentManager from "~/components/admin/AdminContentManager.vue";
import AdminWorkspaceManager from "~/components/admin/AdminWorkspaceManager.vue";
import { useJwt } from "~/composables/useJwt";

definePageMeta({
  title: "Admin Page",
  middleware: "admin-or-moderator-client",
});

const { role } = useJwt();
const currentRole = computed(() => process.client ? (localStorage.getItem('user_role') || role.value || '') : '')

const activeTab = ref("overview");

const allTabs = [
  { id: "overview",   name: "Tổng quan",          icon: "📊", roles: ["ADMIN", "MODERATOR"] },
  { id: "users",      name: "Quản lý User",        icon: "👥", roles: ["ADMIN"], adminOnly: true },
  { id: "health",     name: "Sức khỏe Hệ thống",  icon: "💚", roles: ["ADMIN"], adminOnly: true },
  { id: "content",    name: "Quản lý Flashcard",   icon: "🃏", roles: ["ADMIN", "MODERATOR"] },
  { id: "workspaces", name: "Quản lý Workspace",   icon: "🏢", roles: ["ADMIN"], adminOnly: true },
  { id: "dictionary", name: "Từ điển",             icon: "📖", roles: ["ADMIN", "MODERATOR"] },
]

const visibleTabs = computed(() =>
  allTabs.filter(t => t.roles.includes(currentRole.value))
)
</script>