<template>
  <div
    v-if="isOpen"
    class="fixed inset-0 z-50 flex items-center justify-center bg-black bg-opacity-70 p-4"
    @click.self="close"
  >
    <div
      class="bg-gray-800 rounded-lg shadow-xl p-6 w-full max-w-3xl max-h-[90vh] overflow-y-auto mx-4"
    >
      <!-- Header -->
      <div class="flex items-center justify-between mb-6">
        <h2 class="text-2xl font-bold text-white">Chi tiết người dùng</h2>
        <button
          @click="close"
          class="text-gray-400 hover:text-white transition-colors"
          aria-label="Đóng"
        >
          <svg
            xmlns="http://www.w3.org/2000/svg"
            class="h-6 w-6"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M6 18L18 6M6 6l12 12"
            />
          </svg>
        </button>
      </div>

      <!-- User Info -->
      <div v-if="user" class="mb-6">
        <div class="flex items-center gap-6 mb-6">
          <div class="flex-shrink-0">
            <img
              :src="user.avatarUrl || defaultAvatarUrl"
              :alt="user.username"
              class="w-24 h-24 rounded-full object-cover bg-gray-700 border-4 border-gray-600"
            />
          </div>
          <div class="flex-1">
            <div class="flex items-center gap-3 mb-2">
              <h3 class="text-2xl font-bold text-white">{{ user.username }}</h3>
              <span
                v-if="user.role"
                class="px-3 py-1 text-sm font-semibold rounded bg-sky-600 text-white"
              >
                {{ user.role }}
              </span>
              <span
                :class="[
                  'px-3 py-1 text-sm font-semibold rounded',
                  user.isActive
                    ? 'bg-green-600 text-white'
                    : 'bg-red-600 text-white',
                ]"
              >
                {{ user.isActive ? 'Hoạt động' : 'Không hoạt động' }}
              </span>
            </div>
            <p class="text-gray-400 text-lg mb-2">{{ user.email }}</p>
            <p class="text-gray-500 text-sm">ID: {{ user.id }}</p>
          </div>
        </div>
      </div>

      <!-- Decks Section -->
      <div>
        <h3 class="text-xl font-bold text-white mb-4">
          Bộ thẻ ({{ user?.decks?.length || 0 }})
        </h3>
        <div v-if="!user || !user.decks || user.decks.length === 0" class="text-gray-400">
          Người dùng này chưa có bộ thẻ nào.
        </div>
        <div v-else class="space-y-3">
          <div
            v-for="deck in user.decks"
            :key="deck.id"
            class="bg-gray-700 rounded-lg p-4 hover:bg-gray-600 transition-colors"
          >
            <div class="flex items-start justify-between gap-4">
              <div class="flex-1">
                <div class="flex items-center gap-3 mb-2">
                  <h4 class="text-lg font-semibold text-white">{{ deck.name }}</h4>
                  <!-- Public/Private Indicator -->
                  <div class="flex items-center">
                    <svg
                      v-if="deck.isPublic"
                      xmlns="http://www.w3.org/2000/svg"
                      class="h-5 w-5 text-green-400"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                      title="Công khai"
                    >
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="2"
                        d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
                      />
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="2"
                        d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"
                      />
                    </svg>
                    <svg
                      v-else
                      xmlns="http://www.w3.org/2000/svg"
                      class="h-5 w-5 text-gray-400"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                      title="Riêng tư"
                    >
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        stroke-width="2"
                        d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21"
                      />
                    </svg>
                  </div>
                </div>
                <p v-if="deck.description" class="text-gray-400 text-sm mb-2">
                  {{ deck.description }}
                </p>
                <div class="flex items-center gap-4 text-xs text-gray-500">
                  <span>Số thẻ: {{ deck.cardCount }}</span>
                  <span>Tác giả: {{ deck.authorName }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Footer -->
      <div class="mt-6 flex justify-end">
        <button
          @click="close"
          class="px-6 py-2 rounded-lg bg-gray-700 text-white hover:bg-gray-600 transition-colors font-semibold"
        >
          Đóng
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from "vue";

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

const props = defineProps<{
  isOpen: boolean;
  user: User | null;
}>();

const emit = defineEmits<{
  close: [];
}>();

const defaultAvatarUrl =
  "https://storage.googleapis.com/maker-studio-project-media-prod/11072a6a-d42a-41e9-8902-613d0a6a0e69/images/220b33b9-a99f-4318-9126-d66a6a96f131.jpg";

function close() {
  emit("close");
}
</script>

<style scoped>
/* Modal animations */
.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.2s ease;
}
.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}
</style>

