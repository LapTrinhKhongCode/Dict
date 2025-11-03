<script setup lang="ts">
// 1. Thêm 'computed' vào import
import { computed, watch } from "vue";
import { useJwt } from "@/composables/useJwt";
import { useRouter } from "vue-router";
import { useToast } from "@/composables/useToast";

// --- LOGIC MỚI ĐỂ TÍNH NGÀY THI TỰ ĐỘNG ---

/**
 * Hàm helper để tìm ngày Chủ Nhật đầu tiên của một tháng cụ thể.
 * @param year Năm (ví dụ: 2025)
 * @param month Tháng (0-indexed: 0=Tháng 1, 6=Tháng 7, 11=Tháng 12)
 */
function getFirstSunday(year: number, month: number): Date {
  const date = new Date(year, month, 1);
  const dayOfWeek = date.getDay(); // 0 = Sunday, 1 = Monday, ...

  // Tính ngày Chủ Nhật đầu tiên
  // Nếu ngày 1 là T2 (day=1), (7-1)%7 = 6. Ngày CN = 1 + 6 = 7.
  // Nếu ngày 1 là CN (day=0), (7-0)%7 = 0. Ngày CN = 1 + 0 = 1.
  const firstSundayDate = 1 + ((7 - dayOfWeek) % 7);

  return new Date(year, month, firstSundayDate);
}

/**
 * Hàm helper để tính số ngày còn lại (giống hàm getDaysUntil bạn đang dùng)
 */
function getDaysUntil(targetDate: Date): number {
  const today = new Date();
  today.setHours(0, 0, 0, 0); // Chuẩn hóa về 0 giờ sáng

  const target = new Date(targetDate);
  target.setHours(0, 0, 0, 0); // Chuẩn hóa ngày target

  const diffTime = target.getTime() - today.getTime();

  // Nếu đã qua, trả về 0
  if (diffTime < 0) return 0;

  // Làm tròn lên
  return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
}

/**
 * Tự động tính toán ngày thi JLPT tiếp theo
 */
const nextJlptDate = computed(() => {
  const today = new Date();
  today.setHours(0, 0, 0, 0); // Chuẩn hóa về 0h sáng để so sánh
  const currentYear = today.getFullYear();

  // 1. Lấy 2 ngày thi của năm nay
  const jlptJulyThisYear = getFirstSunday(currentYear, 6); // 6 = Tháng 7
  const jlptDecThisYear = getFirstSunday(currentYear, 11); // 11 = Tháng 12

  // 2. So sánh với ngày hôm nay
  // Nếu hôm nay VẪN CHƯA TỚI ngày thi tháng 7
  if (today < jlptJulyThisYear) {
    return jlptJulyThisYear;
  }

  // Nếu hôm nay ĐÃ QUA tháng 7, nhưng CHƯA TỚI ngày thi tháng 12
  if (today < jlptDecThisYear) {
    return jlptDecThisYear;
  }

  // 3. Nếu đã qua cả 2 kỳ thi năm nay (hoặc đang là ngày thi tháng 12)
  // Ngày thi tiếp theo sẽ là kỳ tháng 7 của NĂM SAU
  return getFirstSunday(currentYear + 1, 6);
});

// --- SỬA LẠI CÁC BIẾN CŨ CỦA BẠN ---

// const target = "2025-12-7"; // <-- BỎ DÒNG NÀY
const diffDay = computed(() => getDaysUntil(nextJlptDate.value)); // <-- SỬA DÒNG NÀY
const dynamicGreeting = computed(() => {
  const currentHour = new Date().getHours();

  // Logic thời gian dựa trên các lời chào tiếng Nhật:
  if (currentHour < 11) {
    // 00:00 - 10:59 (Tương ứng với おはよう)
    return "おはよう";
  } else if (currentHour < 17) {
    // 11:00 - 16:59 (Tương ứng với こんにちは)
    return "こんにちは";
  } else {
    // 17:00 - 23:59 (Tương ứng với こんばんは)
    return "こんばんは";
  }
});
// (Tùy chọn) Thêm một computed để hiển thị ngày target cho đẹp
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
  logout();
  showToast("Logout successful!", "success");
}
</script>
<template>
  <header class="bg-neutral-900 px-6 py-3 flex items-center justify-between">
    <!-- Left: Logo -->
    <div class="flex items-center space-x-10">
      <NuxtLink to="/">
        <img src="/miyo.png" alt="Logo" class="h-auto w-35" />
      </NuxtLink>

      <div class="relative flex items-center cursor-pointer group">
        <UIcon name="i-lucide-calendar" class="text-4xl text-gray-300" />
        <span
          class="absolute top-3.25 right-0.25 text-white text-xs font-bold rounded-full px-2 py-0.5"
        >
          {{ diffDay }}
        </span>
        <div
          class="absolute top-10 left-20 -translate-x-1/2 opacity-0 group-hover:opacity-100 transition duration-300 bg-primary-600 text-sm px-3 py-1 rounded-lg whitespace-nowrap shadow-lg"
        >
          Còn {{ diffDay }} ngày tới kỳ thi JLPT ({{ targetDateString }}) !
        </div>
      </div>
    </div>

    <!-- Center: Nav Links -->
    <div class="flex-1 flex justify-center">
      <nav class="flex space-x-4 text-gray-400">
        <NuxtLink
          to="/"
          class="flex items-center space-x-2 px-3 py-2 rounded-lg transition-colors hover:bg-gray-700 hover:text-white [&.router-link-exact-active]:bg-neutral-600 [&.router-link-exact-active]:text-primary-400"
        >
          <UIcon name="i-lucide-house" class="w-6 h-6" />
          <span>Trang chủ</span>
        </NuxtLink>

        <NuxtLink
          to="/translate"
          class="flex items-center space-x-2 px-3 py-2 rounded-lg transition-colors hover:bg-gray-700 hover:text-white [&.router-link-exact-active]:bg-neutral-600 [&.router-link-exact-active]:text-primary-400"
        >
          <UIcon name="i-custom-translation" class="w-6 h-6" />
          <span>Dịch</span>
        </NuxtLink>
      </nav>
    </div>

    <!-- Right: User/Settings -->
    <div class="flex items-center space-x-4 relative">
      <span v-if="isAuthenticated" class="text-gray-300 whitespace-nowrap">
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
          class="absolute right-0 mt-2 w-32 bg-neutral-800 border border-neutral-700 rounded shadow-lg z-10 opacity-0 pointer-events-none group-hover:opacity-100 group-hover:pointer-events-auto transition-opacity"
        >
          <button
            v-if="!isAuthenticated"
            @click="handleLogin"
            class="block w-full px-4 py-2 text-left text-gray-200 hover:bg-neutral-700"
          >
            Đăng nhập
          </button>
          <button
            v-else
            @click="handleLogout"
            class="block w-full px-4 py-2 text-left text-gray-200 hover:bg-neutral-700"
          >
            Đăng xuất
          </button>
        </div>
      </div>
      <NuxtLink to="/account">
        <UButton icon="i-lucide-settings" size="xl" variant="ghost"> </UButton>
      </NuxtLink>
    </div>
  </header>
</template>
