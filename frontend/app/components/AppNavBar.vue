<script setup lang="ts">
import { computed, watch, ref, onMounted, onUnmounted } from "vue";
import { useRouter, useRoute } from "vue-router";
import { useRuntimeConfig } from "#app";
import { useJwt } from "@/composables/useJwt";
import { useToast } from "@/composables/useToast";
import { useWorkspace } from "@/composables/useWorkspace";
import * as signalR from '@microsoft/signalr';

const config = useRuntimeConfig();
const { $bus } = useNuxtApp(); // Lấy Event Bus để bắn tín hiệu cho Sidebar
const { jwt, isAuthenticated, logout, avatarUrl, username } = useJwt();
const { showToast } = useToast();
const { getMyPendingInvitations, respondToInvitation } = useWorkspace();
const router = useRouter();
const route = useRoute();

// ==========================================
// 1. LOGIC THÔNG BÁO MẸO HỌC TẬP (TOOLTIPS)
// ==========================================
const notifications = [
  "Nhấn Capslock để tra Katakana.",
  "Bạn có thể tra từ vựng ở mọi nơi trên website.",
  "Chúc bạn một ngày học tập hiệu quả!",
  "Đăng nhập để lưu lại từ vựng của bạn.",
  "Nhận diện từ vựng nhanh chóng với công cụ xử lý hình ảnh.",
  "Sử dụng chế độ tối để bảo vệ mắt khi học ban đêm.",
  "Có thể nhận diện tiếng nhật trong cả tệp PDF đấy!",
  "Dịch đoạn văn nhanh chóng với công cụ dịch tích hợp khi đọc báo.",
  "Tạo danh sách từ vựng cá nhân để ôn tập dễ dàng hơn.",
  "Từ vựng cần ôn tập mỗi ngày sẽ được tính toán tự động.",
  "Sử dụng phím tắt Alt kết hợp bôi đen để tra cứu nhanh hơn.",
  "Hãy thử tính năng flashcard để ghi nhớ từ vựng hiệu quả.",
  "Bạn có thể xem chi tiết từ vựng ngay cả trong mỗi thẻ của Flashcard.",
];

const currentNotificationIndex = ref(0);
let notificationInterval: any = null;

const randomIconColor = ref("text-primary-400");
const iconColors = [
  "text-rose-400", "text-emerald-400", "text-cyan-400", 
  "text-fuchsia-400", "text-amber-400", "text-indigo-400",
];

function getRandomColor() {
  return iconColors[Math.floor(Math.random() * iconColors.length)];
}

const currentNotification = computed(() => notifications[currentNotificationIndex.value]);

function startInterval() {
  if (notificationInterval) clearInterval(notificationInterval);
  notificationInterval = setInterval(() => {
    currentNotificationIndex.value = (currentNotificationIndex.value + 1) % notifications.length;
    randomIconColor.value = getRandomColor();
  }, 5000);
}

function stopInterval() {
  if (notificationInterval) clearInterval(notificationInterval);
  notificationInterval = null;
}

// ==========================================
// 2. LOGIC TÍNH NGÀY THI JLPT TỰ ĐỘNG
// ==========================================
function getFirstSunday(year: number, month: number): Date {
  const date = new Date(year, month, 1);
  const dayOfWeek = date.getDay();
  const firstSundayDate = 1 + ((7 - dayOfWeek) % 7);
  return new Date(year, month, firstSundayDate);
}

function getDaysUntil(targetDate: Date): number {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const target = new Date(targetDate);
  target.setHours(0, 0, 0, 0);
  const diffTime = target.getTime() - today.getTime();
  return diffTime < 0 ? 0 : Math.ceil(diffTime / (1000 * 60 * 60 * 24));
}

const nextJlptDate = computed(() => {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const currentYear = today.getFullYear();
  const july = getFirstSunday(currentYear, 6);
  const dec = getFirstSunday(currentYear, 11);
  if (today < july) return july;
  if (today < dec) return dec;
  return getFirstSunday(currentYear + 1, 6);
});

const diffDay = computed(() => getDaysUntil(nextJlptDate.value));
const dynamicGreeting = computed(() => {
  const hour = new Date().getHours();
  if (hour < 11) return "おはよう";
  if (hour < 17) return "こんにちは";
  return "こんばんは";
});
const targetDateString = computed(() => nextJlptDate.value.toLocaleDateString("vi-VN"));

// ==========================================
// 3. LOGIC LỜI MỜI WORKSPACE (REAL-TIME)
// ==========================================
const pendingInvitations = ref<any[]>([]);
const showNotifications = ref(false);
let hubConnection: signalR.HubConnection | null = null;

async function fetchInvitations() {
  if (!isAuthenticated.value) return;
  try {
    const res: any = await getMyPendingInvitations();
    const rawData = res?.result || res?.data || res;
    pendingInvitations.value = Array.isArray(rawData) ? rawData : [];
  } catch (error) {
    console.error("Lỗi tải thông báo lời mời:", error);
  }
}

function setupSignalR() {
  const token = localStorage.getItem("jwt_token") || jwt.value;
  if (!token || hubConnection) return;

  hubConnection = new signalR.HubConnectionBuilder()
    .withUrl(`${config.public.apiBaseUrl}/notificationHub`, {
      accessTokenFactory: () => token
    })
    .withAutomaticReconnect()
    .build();

  hubConnection.on("ReceiveNewInvitation", (newInvite) => {
    pendingInvitations.value.unshift(newInvite);
    showToast(`Bạn có lời mời mới từ ${newInvite.inviterName}!`, "success");
  });

  hubConnection.start().catch(err => console.error("SignalR Connection Error:", err));
}

async function handleRespond(invitationId: number, accept: boolean) {
  try {
    await respondToInvitation(invitationId, accept);
    showToast(accept ? "Bạn đã tham gia Workspace!" : "Đã từ chối lời mời.", "success");
    
    // Xóa lời mời khỏi danh sách hiển thị
    pendingInvitations.value = pendingInvitations.value.filter((inv) => inv.id !== invitationId);
    
    // NẾU CHẤP NHẬN: Bắn sự kiện để Sidebar cập nhật ngay lập tức
    if (accept) {
      $bus.emit('workspace-updated');
    }

    if (pendingInvitations.value.length === 0) showNotifications.value = false;
  } catch (error) {
    console.error("API ERROR:", error);
    showToast("Có lỗi xảy ra khi xử lý lời mời.", "error");
  }
}

// ==========================================
// 4. AUTH & LIFECYCLE
// ==========================================
watch(isAuthenticated, (val) => {
  if (val) {
    fetchInvitations();
    setupSignalR();
  } else {
    pendingInvitations.value = [];
    hubConnection?.stop();
    hubConnection = null;
  }
}, { immediate: true });

onMounted(startInterval);
onUnmounted(() => {
  stopInterval();
  hubConnection?.stop();
});

function handleLogin() { router.push("/login"); }
async function handleLogout() {
  await logout();
  showToast("Logout successful!", "success");
  router.replace("/login");
}
</script>

<template>
  <header class="bg-white dark:bg-neutral-900 px-6 py-3 flex items-center justify-between border-b border-gray-200 dark:border-neutral-800 transition-colors sticky top-0 z-[100]">
    <div class="flex items-center space-x-10">
      <NuxtLink to="/"><img src="/miyo.png" alt="Logo" class="h-auto w-35" /></NuxtLink>
      <div class="relative flex items-center cursor-pointer group">
        <UIcon name="i-lucide-calendar" class="text-4xl text-gray-500 dark:text-gray-300" />
        <span class="absolute top-3.25 right-0.25 text-gray-800 dark:text-white text-xs font-bold rounded-full px-2 py-0.5">{{ diffDay }}</span>
        <div class="absolute top-10 left-20 -translate-x-1/2 opacity-0 group-hover:opacity-100 transition duration-300 bg-primary-600 text-white text-sm px-3 py-1 rounded-lg whitespace-nowrap shadow-lg z-50">
          Còn {{ diffDay }} ngày tới kỳ thi JLPT ({{ targetDateString }}) !
        </div>
      </div>
    </div>

    <div class="flex-1 flex justify-center items-center px-4">
      <div class="relative w-full max-w-lg group" @mouseenter="stopInterval" @mouseleave="startInterval">
        <div class="relative h-6 overflow-hidden text-gray-500 dark:text-gray-400 text-sm group-hover:opacity-0 transition-opacity duration-300 text-center">
          <Transition name="slide-up">
            <span :key="currentNotification" class="notification-text glow flex items-center justify-center w-full absolute inset-0">
              <UIcon name="i-lucide-sparkles" :class="['mr-2', randomIconColor, 'icon-blink']" />
              {{ currentNotification }}
            </span>
          </Transition>
        </div>
        <div class="absolute top-0 left-0 w-full opacity-0 pointer-events-none group-hover:opacity-100 group-hover:pointer-events-auto transition-all duration-300 z-20">
          <ul class="w-full max-h-56 overflow-y-auto bg-white dark:bg-neutral-800 border border-gray-200 dark:border-neutral-700 rounded-lg shadow-2xl p-2 space-y-1">
            <li v-for="(msg, index) in notifications" :key="index" class="p-2 flex items-center text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-neutral-700 rounded cursor-pointer text-sm" @click="currentNotificationIndex = index">
              <UIcon name="i-lucide-sparkles" class="mr-3 text-primary-500 flex-shrink-0" />
              <span class="truncate">{{ msg }}</span>
            </li>
          </ul>
        </div>
      </div>
    </div>

    <div class="flex items-center space-x-4 relative">
      <span v-if="isAuthenticated" class="text-gray-700 dark:text-gray-300 whitespace-nowrap hidden sm:block">
        {{ dynamicGreeting }}{{ username ? `、 ${username}` : "" }} !
      </span>

      <div v-if="isAuthenticated" class="relative">
        <UButton icon="i-lucide-bell" size="sm" variant="ghost" @click="showNotifications = !showNotifications" class="relative rounded-full" />
        <span v-if="pendingInvitations.length > 0" class="absolute top-1 right-2 w-6 h-6 bg-red-500 text-white text-[10px] font-bold flex items-center justify-center rounded-full border-2 border-white dark:border-neutral-900 pointer-events-none">
          {{ pendingInvitations.length }}
        </span>

        <div v-if="showNotifications" @click="showNotifications = false" class="fixed inset-0 z-40"></div>

        <div v-if="showNotifications" class="absolute right-0 top-full mt-2 w-80 bg-white dark:bg-neutral-800 border border-gray-200 dark:border-neutral-700 rounded-xl shadow-2xl z-50 overflow-hidden">
          <div class="p-4 border-b font-bold text-sm">Thông báo lời mời</div>
          <ul v-if="pendingInvitations.length > 0" class="max-h-72 overflow-y-auto">
            <li v-for="inv in pendingInvitations" :key="inv.id" class="p-4 border-b hover:bg-gray-50 dark:hover:bg-neutral-700/50 transition-colors">
              <div class="flex gap-3">
                <div class="w-10 h-10 rounded-lg bg-blue-100 dark:bg-blue-900/30 text-blue-600 flex items-center justify-center font-bold text-lg flex-shrink-0">
                  {{ inv.workspaceName?.[0]?.toUpperCase() || 'W' }}
                </div>
                <div class="flex-1">
                  <p class="text-sm"><strong>{{ inv.inviterName }}</strong> mời tham gia <strong>{{ inv.workspaceName }}</strong></p>
                  <div class="flex gap-2 mt-3">
                    <button @click="handleRespond(inv.id, true)" class="flex-1 bg-yellow-400 text-black text-xs font-bold py-1.5 rounded-lg">Đồng ý</button>
                    <button @click="handleRespond(inv.id, false)" class="flex-1 bg-gray-200 dark:bg-neutral-700 text-xs font-bold py-1.5 rounded-lg">Từ chối</button>
                  </div>
                </div>
              </div>
            </li>
          </ul>
          <div v-else class="p-8 text-center text-gray-500 text-sm">Bạn chưa có lời mời nào</div>
        </div>
      </div>

      <div class="relative group">
        <div>
          <img v-if="isAuthenticated && avatarUrl" :src="avatarUrl" class="rounded-full w-10 h-10 object-cover cursor-pointer border-2 border-primary-400" />
          <UButton v-else icon="i-lucide-user" size="xl" variant="ghost" />
        </div>
        <div class="absolute right-0 top-full w-32 bg-white dark:bg-neutral-800 border border-gray-200 rounded shadow-lg z-10 opacity-0 group-hover:opacity-100 transition-opacity">
          <button v-if="!isAuthenticated" @click="handleLogin" class="block w-full px-4 py-2 text-left text-sm hover:bg-gray-100 dark:hover:bg-neutral-700">Đăng nhập</button>
          <button v-else @click="handleLogout" class="block w-full px-4 py-2 text-left text-sm hover:bg-gray-100 dark:hover:bg-neutral-700">Đăng xuất</button>
        </div>
      </div>

      <NuxtLink v-if="isAuthenticated" to="/account">
        <UButton icon="i-lucide-settings" size="xl" variant="ghost" />
      </NuxtLink>
    </div>
  </header>
</template>

<style scoped>
.notification-text {
  white-space: nowrap;
}
.slide-up-enter-active, .slide-up-leave-active { transition: all 0.5s ease-in-out; }
.slide-up-enter-from { opacity: 0; transform: translateY(20px); }
.slide-up-leave-to { opacity: 0; transform: translateY(-20px); }

@keyframes glow {
  0% { text-shadow: 0 0 3px rgba(16, 185, 129, 0.3); }
  100% { text-shadow: 0 0 8px rgba(16, 185, 129, 0.7); }
}
@keyframes blink {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.6; transform: scale(0.9); }
}
.dark .glow { animation: glow 1.5s ease-in-out infinite alternate; }
.icon-blink { animation: blink 2s ease-in-out infinite; }
</style>