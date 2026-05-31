<template>
  <div class="bg-gray-50 dark:bg-gray-900 min-h-screen transition-colors">
    <!-- Page content -->
    <div class="max-w-6xl mx-auto px-6 py-10">
      <!-- Header -->
      <div class="flex items-center justify-between mb-8">
        <div>
          <h1 class="text-2xl font-bold text-gray-900 dark:text-white">
            Workspace của tôi
          </h1>
          <p class="text-sm text-gray-500 dark:text-gray-400 mt-1">
            Quản lý các không gian làm việc
          </p>
        </div>
        <button
          @click="showCreate = true"
          class="flex items-center gap-2 bg-yellow-400 hover:bg-yellow-500 text-gray-900 font-semibold px-4 py-2 rounded-lg text-sm transition-colors"
        >
          <span class="text-lg leading-none">+</span> Tạo workspace
        </button>
      </div>

      <!-- Loading -->
      <div v-if="pending" class="flex justify-center items-center py-20">
        <div
          class="w-6 h-6 border-2 border-yellow-400 border-t-transparent rounded-full animate-spin"
        ></div>
      </div>

      <!-- Grid -->
      <div
        v-else
        class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4"
      >
        <div
          v-for="ws in workspaces"
          :key="ws.id"
          @click="goTo(ws.id)"
          class="group bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl p-5 cursor-pointer hover:border-blue-400 dark:hover:border-blue-500 hover:-translate-y-1 hover:shadow-lg transition-all duration-200"
        >
          <!-- Top row -->
          <div class="flex items-center justify-between mb-4">
            <div
              class="w-10 h-10 rounded-xl bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center text-white font-bold text-lg"
            >
              {{ ws.name[0].toUpperCase() }}
            </div>
            <span
              :class="[
                'text-xs font-semibold px-2.5 py-1 rounded-full',
                ws.myRole === 'ADMIN'
                  ? 'bg-yellow-100 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-400'
                  : 'bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400',
              ]"
            >
              {{ ws.myRole }}
            </span>
          </div>

          <!-- Info -->
          <p class="font-semibold text-gray-900 dark:text-white mb-1 truncate">
            {{ ws.name }}
          </p>
          <p
            class="text-xs text-gray-400 dark:text-gray-500 mb-4 line-clamp-2 min-h-[2rem]"
          >
            {{ ws.description || "Chưa có mô tả" }}
          </p>

          <!-- Footer -->
          <div
            class="flex items-center gap-1 text-xs text-gray-400 dark:text-gray-500"
          >
            <svg
              class="w-3.5 h-3.5"
              fill="none"
              stroke="currentColor"
              stroke-width="2"
              viewBox="0 0 24 24"
            >
              <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2" />
              <circle cx="9" cy="7" r="4" />
              <path d="M23 21v-2a4 4 0 0 0-3-3.87" />
              <path d="M16 3.13a4 4 0 0 1 0 7.75" />
            </svg>
            {{ ws.memberCount }} thành viên
          </div>
        </div>

        <!-- Empty -->
        <div
          v-if="workspaces.length === 0"
          class="col-span-full flex flex-col items-center justify-center py-20 text-gray-400 dark:text-gray-600"
        >
          <svg
            class="w-12 h-12 mb-4 opacity-40"
            fill="none"
            stroke="currentColor"
            stroke-width="1"
            viewBox="0 0 24 24"
          >
            <rect x="2" y="7" width="20" height="14" rx="2" />
            <path d="M16 7V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v2" />
          </svg>
          <p class="text-base font-medium">Chưa có workspace nào</p>
          <p class="text-sm mt-1">Nhấn "Tạo workspace" để bắt đầu</p>
        </div>
      </div>
    </div>

    <!-- ── Modal tạo workspace ── -->
    <Transition name="modal">
      <div
        v-if="showCreate"
        class="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4"
        @click.self="showCreate = false"
      >
        <div
          class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl p-8 w-full max-w-md shadow-2xl"
        >
          <h2 class="text-xl font-bold text-gray-900 dark:text-white mb-6">
            Tạo Workspace mới
          </h2>

          <div class="space-y-4">
            <div>
              <label
                class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5"
              >
                Tên workspace <span class="text-red-500">*</span>
              </label>
              <input
                v-model="form.name"
                class="w-full bg-gray-50 dark:bg-gray-900 border border-gray-300 dark:border-gray-600 text-gray-900 dark:text-white rounded-lg px-3.5 py-2.5 text-sm outline-none focus:border-blue-500 dark:focus:border-blue-400 transition-colors placeholder-gray-400 dark:placeholder-gray-500"
                placeholder="VD: FPT Software"
                @keyup.enter="handleCreate"
              />
            </div>
            <div>
              <label
                class="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1.5"
              >
                Mô tả
              </label>
              <textarea
                v-model="form.description"
                rows="3"
                class="w-full bg-gray-50 dark:bg-gray-900 border border-gray-300 dark:border-gray-600 text-gray-900 dark:text-white rounded-lg px-3.5 py-2.5 text-sm outline-none focus:border-blue-500 dark:focus:border-blue-400 transition-colors placeholder-gray-400 dark:placeholder-gray-500 resize-none"
                placeholder="Mô tả ngắn về workspace..."
              ></textarea>
            </div>
          </div>

          <div class="flex justify-end gap-3 mt-6">
            <button
              @click="
                showCreate = false;
                form = { name: '', description: '' };
              "
              class="px-4 py-2 text-sm rounded-lg border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
            >
              Hủy
            </button>
            <button
              @click="handleCreate"
              :disabled="!form.name.trim() || creating"
              class="px-5 py-2 text-sm rounded-lg bg-yellow-400 hover:bg-yellow-500 disabled:opacity-50 disabled:cursor-not-allowed text-gray-900 font-semibold transition-colors"
            >
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
import { ref, onMounted } from "vue";
import { useRouter } from "vue-router";
import { useWorkspace } from "~/composables/useWorkspace";
import { useJwt } from "~/composables/useJwt";

const router = useRouter();
const { getMyWorkspaces, createWorkspace } = useWorkspace();

const workspaces = ref<any[]>([]);
const pending = ref(true);
const showCreate = ref(false);
const creating = ref(false);
const form = ref({ name: "", description: "" });

async function load() {
  try {
    pending.value = true;
    workspaces.value = await getMyWorkspaces();
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
    const ws = await createWorkspace(form.value);
    workspaces.value.push(ws);
    showCreate.value = false;
    form.value = { name: "", description: "" };
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
  if (!isAuthenticated.value) {
    router.push("/login");
    return;
  }
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
