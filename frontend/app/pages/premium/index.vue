<template>
  <div class="min-h-screen bg-gray-900 text-white p-4 sm:p-8">
    <div class="max-w-5xl mx-auto">

      <div class="relative w-full rounded-lg overflow-hidden mb-12">
        <img
          src="https://adsinhouse.eupgroup.net/uploads/images/3291_top4web_2_vi.webp"
          alt="3 bước nâng cấp"
          class="w-full object-cover max-h-24 md:max-h-32"
        />
        </div>

      <div v-if="role !== 'USER'" class="flex flex-col items-center justify-center mt-12 space-y-4 text-center">
        <h1 class="text-6xl font-extrabold bg-gradient-to-r from-yellow-400 via-yellow-500 to-amber-600 bg-clip-text text-transparent animate-pulse drop-shadow-lg">
          Bạn đã là thành viên VIP
        </h1>
        <div class="flex items-center space-x-3 text-yellow-400 text-lg">
          <i class="fa-solid fa-crown text-3xl"></i>
          <span class="text-2xl font-semibold">Chúc mừng bạn đã mở khóa tất cả quyền lợi cao cấp!</span>
        </div>
        <button @click="goHome" class="mt-8 px-6 py-3 bg-sky-600 hover:bg-sky-700 text-white font-bold rounded-lg transition-colors shadow-lg">
          Trải nghiệm quyền lợi VIP
        </button>
      </div>

      <div v-if="role === 'USER'" class="flex flex-col items-center justify-center mt-12 mb-16">
        
        <h2 class="text-4xl sm:text-5xl font-extrabold text-white mb-10 text-center leading-tight">
          Nâng cấp tài khoản của bạn để trải nghiệm
          <span class="bg-gradient-to-r from-blue-400 to-purple-600 bg-clip-text text-transparent">không giới hạn</span>
        </h2>

        <div
          class="relative bg-gradient-to-br from-gray-800 to-gray-950 p-6 rounded-2xl shadow-3xl border border-blue-700/50
                 w-full max-w-sm cursor-pointer transition-transform transform hover:scale-105 hover:border-blue-500
                 flex flex-col items-center text-center overflow-hidden"
          @click="selectLifetime"
        >
          <div class="absolute top-4 right-4 bg-red-600 text-white text-sm font-bold px-4 py-1 rounded-full shadow-lg rotate-3">
            -50% TRỌN ĐỜI
          </div>

          <div class="relative w-32 h-32 rounded-full border-4 border-blue-500 bg-gray-700 flex items-center justify-center -mt-4 mb-6 shadow-xl">
            <img 
              src="/assets/icons/miyo.jpg"
              alt="Avatar nhân vật"
              class="w-full h-full object-cover rounded-full"
            />
            <span class="absolute -bottom-1 -right-1 bg-yellow-400 text-gray-900 text-xs font-bold px-2 py-0.5 rounded-full shadow-md">
              M
            </span>
          </div>

          <h3 class="text-4xl font-extrabold text-blue-400 mb-2 drop-shadow-md">Gói Trọn Đời</h3>
          <p class="text-lg text-gray-400 mb-6 max-w-xs">Mở khóa tất cả tính năng cao cấp vĩnh viễn.</p>

          <div class="flex flex-col items-center mb-8">
            <span class="text-6xl font-extrabold text-yellow-400 leading-none">500.000 đ</span>
            <span class="text-xl text-gray-500 line-through mt-1">999.000 đ</span>
          </div>

          <button class="w-full bg-gradient-to-r from-blue-600 to-blue-800 hover:from-blue-700 hover:to-blue-900
                         text-white text-xl font-bold py-4 rounded-xl shadow-lg transition-all duration-300 transform hover:scale-100">
            Nâng cấp ngay!
          </button>
        </div>
      </div>

    </div>
  </div>
</template>

<script setup lang="ts">
import { useJwt } from '~/composables/useJwt'; // Đảm bảo đường dẫn đúng

const { username, role, jwt } = useJwt();

function selectLifetime() {
  if (jwt.value) { // Sử dụng .value với ref/computed
    navigateTo('/premium/confirm');
  } else {
    navigateTo('/login');
  }
}

function goHome() {
  navigateTo('/'); // Điều hướng về trang chủ
}
</script>

<style scoped>
/* Thêm các shadow tùy chỉnh nếu cần, ví dụ: */
.shadow-3xl {
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.2), 0 10px 10px -5px rgba(0, 0, 0, 0.05);
}
</style>