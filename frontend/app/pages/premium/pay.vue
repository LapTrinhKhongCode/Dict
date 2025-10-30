<template>
  <div class="min-h-screen bg-gray-900 text-white p-4 sm:p-8 flex items-center justify-center">
    <div class="max-w-md w-full bg-gray-800 rounded-lg p-6 shadow-xl">
      
      <div v-if="!isSuccess" class="mb-4">
         <button @click="goBackToConfirm" class="flex items-center text-sm text-sky-400 hover:text-sky-300 transition-colors">
          &larr; Quay lại
        </button>
      </div>

      <h1 class="text-2xl font-bold text-sky-400 mb-4 text-center">Thanh toán ZaloPay</h1>

      <div v-if="isSuccess" class="text-center">
        <div class="success-tick mb-4">✓</div>
        <h2 class="text-2xl font-bold text-green-400 mb-2">Thanh toán thành công!</h2>
        <p class="text-gray-300 mb-6">Tài khoản của bạn đã được nâng cấp trọn đời.</p>
        <button @click="goHome" class="w-full bg-sky-500 hover:bg-sky-600 text-white font-bold py-3 rounded-lg transition-colors">
          Quay về trang chủ
        </button>
      </div>

      <div v-else>
        <div id="qrcode-container" class="flex justify-center my-4">
          <div class="qr-code-box">
             <div v-if="isLoading" class="flex items-center justify-center h-full text-gray-400">
               <svg class="animate-spin h-8 w-8" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24"><circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle><path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path></svg>
             </div>
             <!-- ✅ SỬA: Thay thế component bằng thẻ <img> -->
             <img 
                v-if="qrCodeDataUrl" 
                :src="qrCodeDataUrl" 
                alt="Mã QR ZaloPay"
                class="qr-image"
              />
          </div>
        </div>
        
        <div id="result" class="text-center text-gray-300 mb-2 h-5">{{ resultText }}</div>
        <div id="status" class="text-center text-yellow-400 font-semibold h-5 mb-2">{{ statusText }}</div>
        <div id="error" class="text-center text-red-400 h-5">{{ errorText }}</div>
      </div>

    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
// ✅ SỬA: Import thư viện 'qrcode' gốc
import QRCode from 'qrcode';
import { useJwt } from '~/composables/useJwt';

const config = useRuntimeConfig()
const BASE_URL = config.public.apiBaseUrl || 'https://localhost:7084';
const { jwt, userId } = useJwt();

const isLoading = ref(true);
const isSuccess = ref(false);
const resultText = ref('');
const errorText = ref('');
const statusText = ref('');
// ✅ SỬA: Đổi tên biến để chứa Data URL của ảnh QR
const qrCodeDataUrl = ref<string | null>(null);

let checkStatusInterval: ReturnType<typeof setInterval> | null = null;
let currentAppTransId: string | null = null;

// --- Hàm gọi API ---
async function createLifetimeOrder() {
  isLoading.value = true;
  resultText.value = 'Đang tạo đơn hàng...';
  errorText.value = '';
  statusText.value = '';
  qrCodeDataUrl.value = null; // Xóa QR cũ

  try {
    const response = await fetch(`${BASE_URL}/api/subscription/checkout-lifetime`, {
      method: 'POST',
      headers: { 'Authorization': `Bearer ${jwt.value}` }
    });
    const data = await handleResponse(response); // (Giả sử bạn đã có hàm handleResponse)

    const newOrderUrl = data.result?.orderUrl;
    currentAppTransId = data.result?.appTransId;
    if (!newOrderUrl || !currentAppTransId) throw new Error('Thiếu orderUrl hoặc appTransId.');

    resultText.value = 'Quét mã QR bằng ZaloPay Sandbox App.';
    
    // ✅ SỬA: Tạo mã QR thành Data URL
    qrCodeDataUrl.value = await QRCode.toDataURL(newOrderUrl, {
        width: 256,
        margin: 1, // Để vừa khít với box 276px (256 + 10*2 padding)
        color: {
            dark: "#000000",
            light: "#FFFFFF"
        }
    });
    
    startCheckingPaymentStatus();
  } catch (err: any) {
    errorText.value = 'Lỗi: ' + err.message;
  } finally {
      isLoading.value = false;
  }
}

// --- Hàm Handle Response (Cần thiết) ---
async function handleResponse<T>(response: Response): Promise<T> {
   if (!response.ok) {
    const errorText = await response.text();
    let errorMessage = errorText;
    try {
        const errorJson = JSON.parse(errorText);
        if (errorJson?.errors) { errorMessage = Object.values(errorJson.errors).flat().join(' '); }
        else if (errorJson?.message) { errorMessage = errorJson.message; }
        else if (errorJson?.title) { errorMessage = errorJson.title; }
    } catch (e) { /* Ignore */ }
    throw new Error(errorMessage || `Yêu cầu thất bại: ${response.status}`);
  }
  if (response.status === 204) return {} as T;
  const data = await response.json();
  if (data.isSuccess === false) { throw new Error(data.message || 'Lỗi không xác định từ API'); }
  return data as T;
}

// --- Các hàm còn lại (Không thay đổi) ---
async function checkPaymentStatus() {
  if (!currentAppTransId || !userId.value) return;
  statusText.value = 'Đang kiểm tra trạng thái thanh toán...';
  try {
    const res = await fetch(`${BASE_URL}/api/subscription/zalopay-status?appTransId=${currentAppTransId}&userId=${userId.value}`, {
      method: 'GET',
      headers: { 'Authorization': `Bearer ${jwt.value}` }
    });
    if (!res.ok) throw new Error(`Lỗi ${res.status}`);
    const data = await res.json(); 
    if (data.success === true) {
      statusText.value = 'Thanh toán thành công!';
      resultText.value = '';
      errorText.value = '';
      isSuccess.value = true;
      stopCheckingPaymentStatus();
    } else {
      statusText.value = 'Chưa thanh toán hoặc đang xử lý...';
    }
  } catch (err: any) {
    errorText.value = 'Lỗi kiểm tra trạng thái: ' + err.message;
    console.error(err);
  }
}
function startCheckingPaymentStatus() {
  stopCheckingPaymentStatus();
  checkPaymentStatus();
  checkStatusInterval = setInterval(checkPaymentStatus, 5000);
}
function stopCheckingPaymentStatus() {
  if (checkStatusInterval) {
    clearInterval(checkStatusInterval);
    checkStatusInterval = null;
  }
}
function goBackToConfirm() { navigateTo('/premium/confirm'); }
function goHome() { navigateTo('/'); }

// --- Vòng đời Component ---
onMounted(() => {
  // Chỉ cần gọi API, không cần tải thư viện động nữa
  createLifetimeOrder();
});

onUnmounted(() => {
  stopCheckingPaymentStatus();
});

</script>

<style scoped>
.qr-code-box {
  width: 276px;
  height: 276px;
  border: 1px solid #4b5563;
  padding: 10px;
  background-color: white;
  border-radius: 0.5rem;
  display: flex;
  align-items: center;
  justify-content: center;
}
/* ✅ SỬA: Style cho thẻ <img> */
.qr-image {
    width: 256px;
    height: 256px;
}
.success-tick {
  font-size: 100px;
  color: #10B981;
  text-align: center;
  line-height: 1;
}
</style>

