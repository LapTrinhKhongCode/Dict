<template>
  <div
    class="h-screen w-full bg-gray-50 dark:bg-gray-900 flex flex-col items-center justify-center relative"
  >
    <div
      v-if="imageUrl"
      class="w-full h-full flex flex-col z-10 transition-all"
    >
      <WorkspaceReader
        class="w-full h-full"
        :image-url="imageUrl"
        :results="ocrResults"
        :project-id="projectId"
        :ocr-job-id="jobId"
      />
    </div>

    <transition name="toast">
      <div
        v-if="isLoadingResults"
        class="fixed bottom-10 right-10 z-[9999] flex items-center gap-3 px-5 py-3 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-full shadow-2xl"
      >
        <div
          class="animate-spin rounded-full h-5 w-5 border-2 border-blue-600 border-t-transparent"
        ></div>
        <span class="text-sm font-semibold text-gray-700 dark:text-gray-200"
          >Trợ lý AI đang quét tài liệu...</span
        >
      </div>
    </transition>

    <div
      v-if="error"
      class="p-8 bg-white dark:bg-gray-800 border border-red-200 dark:border-red-900 text-center rounded-xl shadow-lg max-w-md z-30"
    >
      <svg
        class="w-16 h-16 text-red-500 mx-auto mb-4"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
      >
        <path
          stroke-linecap="round"
          stroke-linejoin="round"
          stroke-width="1.5"
          d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
        ></path>
      </svg>
      <h3 class="font-bold text-xl text-gray-800 dark:text-white mb-2">
        Không tìm thấy tài liệu
      </h3>
      <p class="text-gray-600 dark:text-gray-400 mb-6">{{ error }}</p>
      <button
        @click="$router.push(`/projects/${projectId}`)"
        class="px-5 py-2.5 bg-gray-100 dark:bg-gray-700 text-gray-700 dark:text-gray-200 font-semibold rounded-lg hover:bg-gray-200 dark:hover:bg-gray-600 transition"
      >
        Quay lại Danh sách
      </button>
    </div>
  </div>
</template>

<script setup>
definePageMeta({ layout: 'default', ssr: false, middleware: 'auth-client' })

import { ref, onMounted } from "vue";
import { useRoute } from "vue-router";
import WorkspaceReader from "~/components/WorkspaceReader.vue";

// 🚀 TÍCH HỢP GLOBAL TOAST CỦA BẠN
// Lưu ý: Đổi tên composable này thành tên hàm thực tế mà bạn đang dùng trong project nhé!
// Ví dụ: const { showToast } = useToast();
// Ở đây mình ví dụ dùng biến showToast
const showToast = (message, type) => {
  console.log(`[Global Toast] ${type.toUpperCase()}: ${message}`);
  // TODO: Gọi composable toast của bạn tại đây, ví dụ: addToast({ message, type })
};

const route = useRoute();
const projectId = route.params.id;
const jobId = route.params.fileId;
const config = useRuntimeConfig();

const imageUrl = ref(null);
const ocrResults = ref(null);
const isLoadingResults = ref(false);
const error = ref(null);

onMounted(async () => {
  const token = localStorage.getItem("jwt_token");
  if (!token) {
    error.value = "Vui lòng đăng nhập.";
    return;
  }

  // 1. Tải ảnh từ Cache
  const cachedMeta = sessionStorage.getItem(`ocr_view_meta_${jobId}`);
  if (cachedMeta) {
    const meta = JSON.parse(cachedMeta);
    imageUrl.value = meta.imageUrl;
  }

  // 2. Bật Loading Cục Bộ (Không tự tắt)
  isLoadingResults.value = true;
  error.value = null;

  try {
    const apiUrl = `${
      config.public.apiBaseUrl || "https://localhost:7084"
    }/api/Infer/job/${jobId}`;
    const response = await fetch(apiUrl, {
      method: "GET",
      headers: { Authorization: `Bearer ${token}` },
    });

    if (!response.ok) throw new Error("Không thể kết nối với máy chủ AI.");
    const jobDetailData = await response.json();

    if (!imageUrl.value) {
      imageUrl.value = jobDetailData.imageUrl;
    }

    ocrResults.value = jobDetailData.results;

    // Gọi Global Toast báo thành công!
    showToast("Trợ lý AI đã quét xong tài liệu!", "success");
  } catch (err) {
    console.error("Lỗi load OCR:", err);
    error.value = `Lỗi: ${err.message}`;

    // Gọi Global Toast báo lỗi!
    showToast("Có lỗi xảy ra khi phân tích tài liệu.", "error");
  } finally {
    // Tắt Loading cục bộ
    isLoadingResults.value = false;
  }
});
</script>

<style scoped>
/* Hiệu ứng trượt Toast */
.toast-enter-active,
.toast-leave-active {
  transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
}
.toast-enter-from,
.toast-leave-to {
  opacity: 0;
  transform: translateY(20px) scale(0.95);
}
</style>
