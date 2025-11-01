<template>
  <div class="min-h-screen text-white p-4 sm:p-8">
    <div class="max-w-5xl mx-auto">
        <img src="https://adsinhouse.eupgroup.net/uploads/images/3291_top4web_2_vi.webp"/>
      <!-- Header -->
      <div class="mb-6">
        <!-- Nút quay lại trang chủ -->
        <!-- <button @click="goHome" class="flex items-center text-sm text-sky-400 hover:text-sky-300 transition-colors mb-2">
          &larr; Quay lại trang chủ
        </button> -->
        <!-- <h1 class="text-3xl font-bold text-center text-sky-400">Nâng cấp tài khoản</h1> -->
      </div>
   <div
  v-if="role != 'USER' && role!"
  class="flex flex-col items-center justify-center mt-12 space-y-4 text-center"
>
  <div
    class="text-6xl font-extrabold bg-gradient-to-r from-yellow-400 via-yellow-500 to-amber-600 bg-clip-text text-transparent animate-pulse"
  >
    Bạn đã là thành viên VIP
  </div>
  <div class="flex items-center space-x-2 text-yellow-400 text-lg">
    <i class="fa-solid fa-crown"></i>
    <span class="text-2xl">Chúc mừng bạn đã mở khóa tất cả quyền lợi cao cấp!</span>
  </div>
</div>

      <!-- Banner (giả lập từ ảnh) -->
      <div class="w-full mt-10 h-300  rounded-lg flex items-center justify-center text-2xl font-bold text-blue-200 mb-30">
           <div class=" flex-col mb-50 w-full h-300 rounded-lg flex items-center justify-center text-2xl font-bold text-blue-200 mb-8 overflow-hidden">
  <img 
    src="../../assets/icons/pay.jpg"
    alt="Preview"
    class="object-contain h-full max-h-200"
  />
      <div v-if="role == 'USER'"
          class=" shadow-2xl w-full max-w-sm cursor-pointer transition-transform transform hover:scale-105 mb-30"
          @click="selectLifetime"
        >
          <!-- Thẻ "Most Popular" -->
          <div class="relative text-center">
            <!-- <span class="absolute top-0 left-1/2 -translate-x-1/2 -translate-y-1/2 bg-yellow-400 text-gray-900 text-sm font-bold px-4 py-1 rounded-full uppercase">
              Most Popular
            </span> -->
          </div>
          
          <!-- Nội dung gói -->
          <div class="p-8 pt-10 text-center">
            <!-- <h3 class="text-2xl font-bold text-white mb-2">Trọn đời</h3>
            <p class="text-lg text-gray-400 mb-6">Basic version</p>
            
            <div class="mb-4">
              <span class="text-5xl font-bold text-yellow-400">500.000 đ</span>
            </div> -->

            <button class="w-full  bg-yellow-500 hover:bg-yellow-600 text-gray-900 text-lg font-bold py-3 rounded-lg transition-colors">
              Nâng cấp ngay
            </button>
          </div>
        </div>
      </div>
</div>

      </div>

      <!-- Lựa chọn gói -->
      <div class="flex justify-center">
        <!-- Gói Trọn Đời -->
    
    </div>
  </div>
</template>

<script setup lang="ts">
import { useJwt} from '~/composables/useJwt';

const { username, role, jwt } = useJwt();
// Nuxt sẽ tự động import navigateTo
function selectLifetime() {
  if(jwt!)
{
  navigateTo('/premium/confirm');
}
else{
  navigateTo('/login');
}
}

function goHome() {
  navigateTo('/'); // Điều hướng về trang chủ
}
</script>
