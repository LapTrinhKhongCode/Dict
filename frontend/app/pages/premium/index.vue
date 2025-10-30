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

      <!-- Banner (giả lập từ ảnh) -->
      <div class="w-full mt-10 h-300  rounded-lg flex items-center justify-center text-2xl font-bold text-blue-200 mb-30">
           <div class=" flex-col mb-50 w-full h-300 rounded-lg flex items-center justify-center text-2xl font-bold text-blue-200 mb-8 overflow-hidden">
  <img 
    src="https://lh3.googleusercontent.com/rd-gg-dl/ABS2GSm4XkTtp4WAM0tIaoXPsn1L_G_80KMq8r63J8pmmgDDX99rQ3z8a2x7LyiwMxJH6INKkubxC7GqG-5UtOyOD2KswZ4rDanHWwSu0wWDNJCyQvvSA2rjen_nLLr1cn6gvozcWcOrvFKbflSzMRYaOhYpySjTHGtPuMR0wjMBMfsG56gdQUXeAHk3qKryrrlkXFRxlMHkPg4g4cx2PoxsO5oHAEZumBpidziHXg729S_ihcwRK2PFHjMAADQ2OLYMVQYR8hMByEGUTTRICNvtqzVh4nFMbyL-NK6sXU4oUoKuG0X9p9IcpvHhccUreYHOwEmY7nMJ-Ah8T4Qqr0hxG0yJIhAFUpve2JLZcRX7GU4RZOmB8rstnOfriGmMD63v5YNliC9F0WSmVxP_Dp17vEvhGUDZhu2IdlN_NoMU5_IX8T44V0d0Wk40sfUt-vxOQUqfQgGU7traCQM_uL2Y2wDmUMc-D7FuscVFE0BdNEeMnLtNu9wNZMAnEJD-573h1sE54OmyX6sWWQW41RhuvPSaQmnIAYT5kOATStic0UNIF2m0nNs4aFxgHRskNG8dfwX0NxLcCYX6yRkMVMfSuiSH9QnPWje9EYNERZhtj8ffSVdlzv5Cf4qzxl69rpa602l3V__GY9eDBVekymSHocI2HSu_KQ8YGcTGG8wA9X2wdvm9mBua33MmTjRwvQr8BPi4_IFnhYV-rN8JHFb38334TdOmgHptFz5Ru2YYpI8h2I00Is2EyITaoKk2iDuJqyptnnF2FZsL379-QZRHnHr7764HFAVZDhGiXxgbFmUDPafwDvqciJZYNOIBWrGsiIElaGzuP1RF2dJYnp2lEoHVP2uwOzqVuJ0w5Ydvvf_QmVFvIU77pA7YDYNZ2_UsKtJKso_5JSNyEZ0wU4oI3JiSFK15zFIPimshIoEx34fDxYldS2Nfhs9VHM5DmxInnzPDL9M35toz4QIRT57L1xdBMnWhgJ08hPhmWwPA7juejXB8TAvsG6Yxl_WI25tsb-BJsmf-DA4tHjDvAQNFeivO769mW75dXtmHdlHSNalLnEEGahxYfj5SIjI9GTkkUcrG_okEtaK8JQsXNbuVt5DDOupHXJZpCeX9XwGK2dOuuWB6C2uIXfBS8kxSB7yMU1ukcRtBe0ezYsaJkwqwDCS2gAgNysSrET89mRd-CZGftMCQ-bjY2iaBZcj_cC0zuzb7PRBQSOL6tyB4r3m3NZwWh1yj5mrwK6b2fB_J9WcIP9nmy0Kltcyx_2RT8LLhzfzjgryH51pH1lOsJnUUlY9Jvze78Bepra8bunBnj6-6Rv-h=s1024-rj"
    alt="Preview"
    class="object-contain h-full max-h-200"
  />
      <div
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
// Nuxt sẽ tự động import navigateTo
function selectLifetime() {
  navigateTo('/premium/confirm');
}

function goHome() {
  navigateTo('/'); // Điều hướng về trang chủ
}
</script>
