<script setup lang="ts">
// (TOÀN BỘ SCRIPT SETUP GIỮ NGUYÊN TỪ CODE GỐC CỦA BẠN)
import { computed, watch, ref, onMounted, onUnmounted } from "vue";
import { useJwt } from "@/composables/useJwt";
import { useRouter } from "vue-router";
import { useToast } from "@/composables/useToast";
import { useRoute } from "vue-router";

// --- LOGIC MỚI CHO THÔNG BÁO ---
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
let notificationInterval: ReturnType<typeof setInterval> | null = null;

// Thêm ref cho màu icon ngẫu nhiên (để "lung linh" hơn)
const randomIconColor = ref("text-primary-400");
const iconColors = [
  "text-rose-400",
  "text-emerald-400",
  "text-cyan-400",
  "text-fuchsia-400",
  "text-amber-400",
  "text-indigo-400",
];

function getRandomColor() {
  const randomIndex = Math.floor(Math.random() * iconColors.length);
  return iconColors[randomIndex];
}

// Computed property để lấy message hiện tại
const currentNotification = computed(() => {
  return notifications[currentNotificationIndex.value];
});

// Hàm để BẮT ĐẦU interval
function startInterval() {
  // Xóa interval cũ nếu có
  if (notificationInterval) {
    clearInterval(notificationInterval);
  }
  // Tạo interval mới
  notificationInterval = setInterval(() => {
    currentNotificationIndex.value =
      (currentNotificationIndex.value + 1) % notifications.length;
    // Đổi màu icon mỗi lần chuyển
    randomIconColor.value = getRandomColor();
  }, 5000); // Đổi message mỗi 5 giây
}

// Hàm để DỪNG interval
function stopInterval() {
  if (notificationInterval) {
    clearInterval(notificationInterval);
  }
  notificationInterval = null;
}

// Bắt đầu vòng lặp khi component được mount
onMounted(startInterval);

// Dọn dẹp interval khi component bị unmount
onUnmounted(stopInterval);

// --- KẾT THÚC LOGIC THÔNG BÁO ---

// --- LOGIC MỚI ĐỂ TÍNH NGÀY THI TỰ ĐỘNG ---

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
  if (diffTime < 0) return 0;
  return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
}

const nextJlptDate = computed(() => {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const currentYear = today.getFullYear();
  const jlptJulyThisYear = getFirstSunday(currentYear, 6);
  const jlptDecThisYear = getFirstSunday(currentYear, 11);
  if (today < jlptJulyThisYear) {
    return jlptJulyThisYear;
  }
  if (today < jlptDecThisYear) {
    return jlptDecThisYear;
  }
  return getFirstSunday(currentYear + 1, 6);
});

// --- SỬA LẠI CÁC BIẾN CŨ CỦA BẠN ---

const diffDay = computed(() => getDaysUntil(nextJlptDate.value));
const dynamicGreeting = computed(() => {
  const currentHour = new Date().getHours();
  if (currentHour < 11) {
    return "おはよう";
  } else if (currentHour < 17) {
    return "こんにちは";
  } else {
    return "こんばんは";
  }
});
const targetDateString = computed(() => {
  return nextJlptDate.value.toLocaleDateString("vi-VN", {
    day: "numeric",
    month: "numeric",
    year: "numeric",
  });
});

// --- PHẦN CODE CÒN LẠI CỦA BẠN (giữ nguyên) ---
const { jwt, isAuthenticated, logout, avatarUrl, username } = useJwt();
const { showToast } = useToast();
const router = useRouter();
const route = useRoute();

watch(
  jwt,
  (newJwt) => {
    console.log("[JWT atom changed]", newJwt);
  },
  { immediate: true }
);

function handleLogin() {
  router.push("/login");
}
function handleLogout() {
  if (route.path === "/account") {
    router.push("/");
  }
  logout();
  showToast("Logout successful!", "success");
}
</script>

<template>
  <header
    class="bg-white dark:bg-neutral-900 px-6 py-3 flex items-center justify-between border-b border-gray-200 dark:border-neutral-800 transition-colors"
  >
    <div class="flex items-center space-x-10">
      <NuxtLink to="/">
        <img src="/miyo.png" alt="Logo" class="h-auto w-35" />
      </NuxtLink>
      <div class="relative flex items-center cursor-pointer group">
        <UIcon
          name="i-lucide-calendar"
          class="text-4xl text-gray-500 dark:text-gray-300"
        />
        <span
          class="absolute top-3.25 right-0.25 text-gray-800 dark:text-white text-xs font-bold rounded-full px-2 py-0.5"
        >
          {{ diffDay }}
        </span>
        <div
          class="absolute top-10 left-20 -translate-x-1/2 opacity-0 group-hover:opacity-100 transition duration-300 bg-primary-600 text-white text-sm px-3 py-1 rounded-lg whitespace-nowrap shadow-lg"
        >
          Còn {{ diffDay }} ngày tới kỳ thi JLPT ({{ targetDateString }}) !
        </div>
      </div>
    </div>

    <div class="flex-1 flex justify-center items-center px-4">
      <div
        class="relative w-full max-w-lg group"
        title="Mẹo hữu ích (di chuột để xem tất cả)"
        @mouseenter="stopInterval"
        @mouseleave="startInterval"
      >
        <div
          class="relative h-6 overflow-hidden text-gray-500 dark:text-gray-400 text-sm group-hover:opacity-0 group-hover:pointer-events-none transition-opacity duration-300"
        >
          <Transition name="slide-up">
            <span :key="currentNotification" class="notification-text glow">
              <UIcon
                name="i-lucide-sparkles"
                :class="['mr-2', randomIconColor, 'icon-blink']"
              />
              {{ currentNotification }}
            </span>
          </Transition>
        </div>

        <div
          class="absolute top-0 left-0 w-full opacity-0 pointer-events-none group-hover:opacity-100 group-hover:pointer-events-auto transition-all duration-300 z-20"
        >
          <ul
            class="w-full max-h-56 overflow-y-auto bg-white dark:bg-neutral-800 border border-gray-200 dark:border-neutral-700 rounded-lg shadow-2xl p-2 space-y-1"
          >
            <li
              v-for="(msg, index) in notifications"
              :key="index"
              class="p-2 flex items-center text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-neutral-700 rounded cursor-pointer text-sm"
              @click="currentNotificationIndex = index"
            >
              <UIcon
                name="i-lucide-sparkles"
                class="mr-3 text-primary-500 dark:text-primary-400 flex-shrink-0"
              />
              <span class="truncate">{{ msg }}</span>
            </li>
          </ul>
        </div>
      </div>
    </div>

    <div class="flex items-center space-x-4 relative">
      <span
        v-if="isAuthenticated"
        class="text-gray-700 dark:text-gray-300 whitespace-nowrap"
      >
        {{ dynamicGreeting }}{{ username ? `、 ${username}` : "" }} !
      </span>
      <div class="relative group">
        <div>
          <img
            v-if="isAuthenticated && avatarUrl"
            :src="avatarUrl"
            :alt="username || 'user'"
            class="rounded-full w-10 h-10 object-cover cursor-pointer border-2 border-primary-400"
            :title="username"
          />
          <UButton v-else icon="i-lucide-user" size="xl" variant="ghost" />
        </div>
        <div
          class="absolute right-0 top-full w-32 bg-white dark:bg-neutral-800 border border-gray-200 dark:border-neutral-700 rounded shadow-lg z-10 opacity-0 pointer-events-none group-hover:opacity-100 group-hover:pointer-events-auto transition-opacity"
        >
          <button
            v-if="!isAuthenticated"
            @click="handleLogin"
            class="block w-full px-4 py-2 text-left text-gray-700 hover:bg-gray-100 dark:text-gray-200 dark:hover:bg-neutral-700"
          >
            Đăng nhập
          </button>
          <button
            v-else
            @click="handleLogout"
            class="block w-full px-4 py-2 text-left text-gray-700 hover:bg-gray-100 dark:text-gray-200 dark:hover:bg-neutral-700"
          >
            Đăng xuất
          </button>
        </div>
      </div>
      <NuxtLink v-if="isAuthenticated" to="/account">
        <UButton icon="i-lucide-settings" size="xl" variant="ghost"> </UButton>
      </NuxtLink>
    </div>
  </header>
</template>

<style>
/* (Style giữ nguyên từ code của bạn) */
.notification-text {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  position: absolute;
  left: 0;
  white-space: nowrap;
}

.slide-up-enter-active,
.slide-up-leave-active {
  transition: all 0.5s ease-in-out;
}
.slide-up-enter-from {
  opacity: 0;
  transform: translateY(20px);
}
.slide-up-leave-to {
  opacity: 0;
  transform: translateY(-20px);
}

@keyframes glow {
  0% {
    text-shadow: 0 0 3px rgba(16, 185, 129, 0.3);
  }
  100% {
    text-shadow: 0 0 8px rgba(16, 185, 129, 0.7);
  }
}

@keyframes blink {
  0%,
  100% {
    opacity: 1;
    transform: scale(1);
  }
  50% {
    opacity: 0.6;
    transform: scale(0.9);
  }
}

.dark .glow {
  animation: glow 1.5s ease-in-out infinite alternate;
}

.icon-blink {
  animation: blink 2s ease-in-out infinite;
}
</style>