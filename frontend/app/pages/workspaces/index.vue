<template>
  <div class="bg-gray-50 dark:bg-gray-900 min-h-screen transition-colors">
    <div class="max-w-6xl mx-auto px-6 py-10">

      <!-- Header -->
      <div class="flex items-center justify-between mb-8">
        <div>
          <h1 class="text-2xl font-bold text-gray-900 dark:text-white">Workspace của tôi</h1>
          <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">Quản lý các không gian làm việc</p>
        </div>
        <button @click="showCreate = true"
          class="flex items-center gap-2 bg-yellow-400 hover:bg-yellow-500 text-gray-900 font-semibold px-4 py-2 rounded-lg text-sm transition-colors">
          <span class="text-lg leading-none">+</span> Tạo workspace
        </button>
      </div>

      <div v-if="pending" class="flex justify-center py-20">
        <div class="w-6 h-6 border-2 border-yellow-400 border-t-transparent rounded-full animate-spin"/>
      </div>

      <div v-else class="space-y-10">

        <!-- ── PERSONAL ──────────────────────────────────────────── -->
        <section>
          <div class="flex items-center gap-3 mb-4">
            <div class="flex items-center gap-2">
              <span class="text-lg">🏠</span>
              <h2 class="font-bold text-gray-700 dark:text-gray-300 text-sm uppercase tracking-wider">Cá nhân</h2>
            </div>
            <div class="flex-1 h-px bg-gray-200 dark:bg-gray-700"/>
            <span class="text-xs text-gray-400">Dùng plan cá nhân của bạn</span>
          </div>

          <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
            <div v-for="ws in personalWorkspaces" :key="ws.id"
              @click="goTo(ws.id)"
              class="group bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl p-5 cursor-pointer hover:border-blue-400 dark:hover:border-blue-500 hover:-translate-y-1 hover:shadow-lg transition-all duration-200">
              <div class="flex items-center justify-between mb-4">
                <div class="w-10 h-10 rounded-xl bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-white font-bold text-lg">
                  {{ ws.name[0].toUpperCase() }}
                </div>
                <span :class="roleClass(ws.myRole)" class="text-xs font-semibold px-2.5 py-1 rounded-full">
                  {{ ws.myRole }}
                </span>
              </div>
              <p class="font-semibold text-gray-900 dark:text-white truncate mb-1">{{ ws.name }}</p>
              <p class="text-xs text-gray-400 dark:text-gray-500 mb-4 line-clamp-2 min-h-[2rem]">
                {{ ws.description || "Chưa có mô tả" }}
              </p>
              <div class="flex items-center justify-between text-xs text-gray-400 dark:text-gray-500">
                <span>👥 {{ ws.memberCount }} thành viên</span>
                <span class="px-2 py-0.5 rounded-full font-semibold"
                  :class="myTier === 'PREMIUM' ? 'bg-indigo-100 dark:bg-indigo-900/30 text-indigo-600 dark:text-indigo-400' : 'bg-gray-100 dark:bg-gray-700 text-gray-500'">
                  {{ myTier }}
                </span>
              </div>
            </div>

            <div v-if="personalWorkspaces.length === 0"
              class="col-span-full text-center py-10 text-gray-400 dark:text-gray-500 text-sm">
              Chưa có workspace cá nhân nào
            </div>
          </div>
        </section>

        <!-- ── ORGANIZATION ──────────────────────────────────────── -->
        <section v-if="orgWorkspaces.length > 0">
          <div class="flex items-center gap-3 mb-4">
            <div class="flex items-center gap-2">
              <span class="text-lg">🏢</span>
              <h2 class="font-bold text-gray-700 dark:text-gray-300 text-sm uppercase tracking-wider">Tổ chức</h2>
            </div>
            <div class="flex-1 h-px bg-gray-200 dark:bg-gray-700"/>
            <span class="text-xs text-gray-400">Dùng plan của tổ chức</span>
          </div>

          <!-- Group by org -->
          <div v-for="org in orgGroups" :key="org.orgId" class="mb-6">
            <div class="flex items-center gap-2 mb-3">
              <span class="text-sm font-semibold text-gray-600 dark:text-gray-400">{{ org.orgName }}</span>
              <span class="text-xs px-2 py-0.5 rounded-full font-bold"
                :class="{
                  'bg-gray-100 text-gray-500 dark:bg-gray-700': org.orgPlan === 'FREE',
                  'bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400': org.orgPlan === 'TEAM',
                  'bg-purple-100 text-purple-700 dark:bg-purple-900/30 dark:text-purple-400': org.orgPlan === 'ENTERPRISE',
                }">
                {{ org.orgPlan }}
              </span>
              <span v-if="org.orgPlan === 'FREE'" class="text-xs text-gray-400">• Shared 20 OCR/tháng</span>
              <span v-else-if="org.orgPlan === 'TEAM'" class="text-xs text-green-500">• OCR không giới hạn</span>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
              <div v-for="ws in org.workspaces" :key="ws.id"
                @click="goTo(ws.id)"
                class="group relative bg-white dark:bg-gray-800 border-2 border-indigo-200 dark:border-indigo-800 rounded-2xl p-5 cursor-pointer hover:border-indigo-400 dark:hover:border-indigo-500 hover:-translate-y-1 hover:shadow-lg transition-all duration-200">
                <!-- Org indicator strip -->
                <div class="absolute top-0 left-0 right-0 h-1 rounded-t-2xl bg-gradient-to-r from-indigo-500 to-purple-500"/>
                <div class="flex items-center justify-between mb-4 mt-1">
                  <div class="w-10 h-10 rounded-xl bg-gradient-to-br from-indigo-500 to-purple-600 flex items-center justify-center text-white font-bold text-lg">
                    {{ ws.name[0].toUpperCase() }}
                  </div>
                  <span :class="roleClass(ws.myRole)" class="text-xs font-semibold px-2.5 py-1 rounded-full">
                    {{ ws.myRole }}
                  </span>
                </div>
                <p class="font-semibold text-gray-900 dark:text-white truncate mb-1">{{ ws.name }}</p>
                <p class="text-xs text-gray-400 dark:text-gray-500 mb-4 line-clamp-2 min-h-[2rem]">
                  {{ ws.description || "Workspace tổ chức" }}
                </p>
                <div class="flex items-center justify-between text-xs text-gray-400 dark:text-gray-500">
                  <span>👥 {{ ws.memberCount }} thành viên</span>
                  <span class="text-indigo-400 dark:text-indigo-500 font-medium">Org</span>
                </div>
              </div>
            </div>
          </div>
        </section>

      </div>

      <!-- Empty state -->
      <div v-if="!pending && workspaces.length === 0" class="text-center py-24">
        <div class="text-6xl mb-4">📁</div>
        <h3 class="text-lg font-semibold text-gray-700 dark:text-gray-300 mb-2">Chưa có workspace nào</h3>
        <p class="text-sm text-gray-400 mb-6">Nhấn "Tạo workspace" để bắt đầu</p>
        <button @click="showCreate = true" class="bg-yellow-400 hover:bg-yellow-500 text-gray-900 font-semibold px-6 py-2.5 rounded-lg text-sm transition-colors">
          + Tạo workspace đầu tiên
        </button>
      </div>

    </div>

    <!-- Create modal -->
    <Transition name="modal">
      <div v-if="showCreate" class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4"
        @click.self="showCreate = false; form = { name: '', description: '', organizationId: null };">
        <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-2xl p-6 w-full max-w-md border border-gray-200 dark:border-gray-700">
          <h2 class="text-xl font-bold text-gray-900 dark:text-white mb-6">Tạo Workspace mới</h2>

          <div class="space-y-4">
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">
                Tên workspace <span class="text-red-500">*</span>
              </label>
              <input v-model="form.name"
                class="w-full bg-gray-50 dark:bg-gray-900 border border-gray-300 dark:border-gray-600 text-gray-900 dark:text-white rounded-lg px-3.5 py-2.5 text-sm outline-none focus:border-blue-500 dark:focus:border-blue-400 transition-colors"
                placeholder="VD: Dự án tiếng Nhật N2"
                @keyup.enter="handleCreate" />
            </div>
            <div>
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Mô tả</label>
              <textarea v-model="form.description" rows="3"
                class="w-full bg-gray-50 dark:bg-gray-900 border border-gray-300 dark:border-gray-600 text-gray-900 dark:text-white rounded-lg px-3.5 py-2.5 text-sm outline-none focus:border-blue-500 dark:focus:border-blue-400 transition-colors resize-none"
                placeholder="Mô tả ngắn..."/>
            </div>

            <!-- Org selection -->
            <div v-if="myOrgs.length > 0">
              <label class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5">Thuộc tổ chức</label>
              <select v-model="form.organizationId"
                class="w-full bg-gray-50 dark:bg-gray-900 border border-gray-300 dark:border-gray-600 text-gray-900 dark:text-white rounded-lg px-3.5 py-2.5 text-sm outline-none focus:border-blue-500 dark:focus:border-blue-400">
                <option :value="null">🏠 Cá nhân (dùng plan cá nhân)</option>
                <option v-for="org in myOrgs" :key="org.id" :value="org.id">
                  🏢 {{ org.name }} — Plan {{ org.orgPlan }}
                </option>
              </select>
              <p v-if="form.organizationId" class="text-xs text-indigo-500 dark:text-indigo-400 mt-1.5 flex items-center gap-1">
                <span>✓</span> Tất cả {{ myOrgs.find(o=>o.id===form.organizationId)?.memberCount || '' }} thành viên tổ chức sẽ được thêm tự động
              </p>
            </div>
          </div>

          <div class="flex justify-end gap-3 mt-6">
            <button @click="showCreate = false; form = { name: '', description: '', organizationId: null };"
              class="px-4 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors">
              Hủy
            </button>
            <button @click="handleCreate" :disabled="!form.name.trim() || creating"
              class="px-5 py-2 text-sm rounded-lg bg-yellow-400 hover:bg-yellow-500 disabled:opacity-50 disabled:cursor-not-allowed text-gray-900 font-semibold transition-colors">
              {{ creating ? "Đang tạo..." : "Tạo workspace" }}
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </div>
</template>

<script setup lang="ts">
definePageMeta({ middleware: "auth-client" });
import { ref, computed, onMounted } from "vue";
import { useRouter } from "vue-router";
import { useWorkspace } from "~/composables/useWorkspace";
import { useJwt } from "~/composables/useJwt";

const router = useRouter();
const { getMyWorkspaces, createWorkspace } = useWorkspace();

const workspaces = ref<any[]>([]);
const pending = ref(true);
const showCreate = ref(false);
const creating = ref(false);
const form = ref({ name: "", description: "", organizationId: null as number | null });
const myOrgs = ref<any[]>([]);

const personalWorkspaces = computed(() => workspaces.value.filter(w => w.ownerType !== 'ORGANIZATION'))
const orgWorkspaces = computed(() => workspaces.value.filter(w => w.ownerType === 'ORGANIZATION'))
const orgGroups = computed(() => {
  const map = new Map<number, any>()
  for (const ws of orgWorkspaces.value) {
    if (!map.has(ws.organizationId)) {
      map.set(ws.organizationId, { orgId: ws.organizationId, orgName: ws.orgName, orgPlan: ws.orgPlan, workspaces: [] })
    }
    map.get(ws.organizationId).workspaces.push(ws)
  }
  return [...map.values()]
})

// Fetch personal tier của user hiện tại để hiện trên personal workspace
const myTier = ref<'FREE' | 'PREMIUM'>('FREE')
async function fetchMyTier() {
  if (!jwt.value) return
  try {
    const res = await $fetch<any>(`${useRuntimeConfig().public.apiBaseUrl}/api/auth/me`, {
      headers: { Authorization: `Bearer ${jwt.value}` }
    }).catch(() => null)
    myTier.value = res?.result?.isPremiumActive ? 'PREMIUM' : 'FREE'
  } catch { myTier.value = 'FREE' }
}

function roleClass(role: string) {
  if (role === 'OWNER') return 'bg-purple-100 dark:bg-purple-900/30 text-purple-700 dark:text-purple-400'
  if (role === 'ADMIN') return 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-400'
  if (role === 'VIEWER') return 'bg-gray-100 dark:bg-gray-700 text-gray-500 dark:text-gray-400'
  return 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400'
}

async function load() {
  try {
    pending.value = true;
    workspaces.value = await getMyWorkspaces();
    const orgsRes = await $fetch<any>(`${useRuntimeConfig().public.apiBaseUrl}/api/organizations/my`, {
      headers: { Authorization: `Bearer ${useJwt().jwt.value}` }
    }).catch(() => null)
    myOrgs.value = orgsRes?.result ?? []
    await fetchMyTier()
  } catch (e) {
    console.error(e);
  } finally {
    pending.value = false;
  }
}

async function handleCreate() {
  if (!form.value.name.trim() || creating.value) return;
  try {
    creating.value = true;
    const ws = await createWorkspace({
      name: form.value.name,
      description: form.value.description,
      organizationId: form.value.organizationId ?? undefined,
    });
    workspaces.value.push(ws);
    showCreate.value = false;
    form.value = { name: "", description: "", organizationId: null };
  } catch (e) {
    console.error(e);
  } finally {
    creating.value = false;
  }
}

function goTo(id: number) {
  router.push(`/workspaces/${id}`);
}

onMounted(() => {
  const { isAuthenticated } = useJwt();
  if (!isAuthenticated.value) { router.push("/login"); return; }
  load();
});
</script>

<style scoped>
.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.2s ease;
}
.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}
</style>
