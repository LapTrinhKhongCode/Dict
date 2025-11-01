<template>
  <div>
    <h2>Đang xác nhận tài khoản...</h2>
    <p v-if="error" style="color: red;">{{ error }}</p>
    <p v-if="success" style="color: green;">{{ success }}</p>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';

// 1. Lấy các hàm composables của Nuxt 3
const config = useRuntimeConfig(); // Để lấy config.public.apiBaseUrl
const route = useRoute();         // Để lấy query params từ URL
const router = useRouter();       // Để chuyển trang

// 2. Tạo các biến trạng thái (state)
const error = ref(null);
const success = ref(null);

// 3. Dùng onMounted để chạy code khi component được tải
// (Tương đương với 'mounted' trong Options API)
onMounted(async () => {
  
  // Lấy token tạm thời từ URL
  const confirmationToken = route.query.token;

  if (!confirmationToken) {
    error.value = 'Link xác nhận không hợp lệ.';
    return;
  }

  try {
    // 1. TẠO URL (Theo phong cách của bạn)
    const url = `${config.public.apiBaseUrl}/api/Auth/confirm-registration`;

    // 2. TẠO TÙY CHỌN FETCH
    const fetchOptions = {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ token: confirmationToken }),
    };

    // 3. GỌI API (Theo phong cách của bạn)
    const response = await fetch(url, fetchOptions);
    const data = await response.json(); // Đây là ResponseDTO

    // 4. KIỂM TRA KẾT QUẢ
    if (!response.ok || !data.isSuccess) {
      // Ném lỗi nếu API trả về thất bại (ví dụ: token hết hạn)
      throw new Error(data.message || 'Xác nhận thất bại.');
    }

    // 5. THÀNH CÔNG: Lấy token JWT (token "vĩnh viễn")
    const loginToken = data.result.token;
    if (!loginToken) {
      throw new Error('Không nhận được token đăng nhập từ máy chủ.');
    }

    // 6. ✨ LƯU TOKEN VĨNH VIỄN
    localStorage.setItem('auth_token', loginToken);

    // (Tùy chọn: Cập nhật Pinia store tại đây)
    // const authStore = useAuthStore();
    // authStore.setUser(data.result);

    success.value = 'Xác nhận thành công! Đang chuyển hướng...';

    // 7. CHUYỂN HƯỚNG về trang chủ
    setTimeout(() => {
      router.push('/');
    }, 2000);

  } catch (err) {
    // 8. BẮT LỖI
    error.value = err.message || 'Đã xảy ra lỗi. Vui lòng thử lại.';
  }
});
</script>