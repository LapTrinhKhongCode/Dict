<template>
  <div
    class="min-h-screen bg-gray-50 dark:bg-gray-900 transition-colors"
    @dragover.prevent="isDragging = true"
    @dragleave.prevent="isDragging = false"
    @drop.prevent="handleDrop"
  >
    <div
      v-if="isDragging"
      class="fixed inset-0 bg-blue-600/90 z-50 flex flex-col items-center justify-center text-white backdrop-blur-sm transition-opacity duration-300"
    >
      <svg
        class="w-24 h-24 mb-6 animate-bounce"
        fill="none"
        stroke="currentColor"
        viewBox="0 0 24 24"
      >
        <path
          stroke-linecap="round"
          stroke-linejoin="round"
          stroke-width="1.5"
          d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
        ></path>
      </svg>
      <p class="text-3xl font-bold">Thả file PDF hoặc Ảnh vào đây</p>
      <p class="text-xl mt-2 opacity-80">
        để Trợ lý AI bắt đầu nhận diện ngay lập tức!
      </p>
    </div>

    <header
      class="bg-white dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 shadow-sm transition-colors sticky top-0 z-40"
    >
      <nav
        class="max-w-[1700px] mx-auto px-6 py-3 flex items-center justify-between"
      >
        <div class="flex items-center gap-4">
          <button
            @click="$router.push('/projects')"
            class="p-2 rounded-full hover:bg-gray-100 dark:hover:bg-gray-700 text-gray-500"
          >
            <svg
              class="w-5 h-5"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M10 19l-7-7m0 0l7-7m-7 7h18"
              ></path>
            </svg>
          </button>
          <h1
            class="text-2xl font-bold text-gray-900 dark:text-white truncate max-w-[400px]"
          >
            Khoa CNTT - Đại học Bách Khoa ĐN
          </h1>
        </div>

        <div class="flex items-center gap-4 w-1/3">
          <div class="relative w-full">
            <input
              type="text"
              placeholder="Tìm kiếm tài liệu tiếng Nhật..."
              class="w-full p-2.5 pl-10 border rounded-full bg-gray-50 dark:bg-gray-700 dark:border-gray-600 dark:text-gray-100 outline-none focus:ring-2 focus:ring-blue-300 transition-all text-sm"
            />
            <svg
              class="absolute left-3.5 top-3 w-4 h-4 text-gray-400"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2"
                d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
              ></path>
            </svg>
          </div>
        </div>

        <div class="flex items-center gap-3">
          <button
            @click="$refs.fileInput.click()"
            class="px-5 py-2.5 bg-blue-600 text-white rounded-full font-semibold text-sm hover:bg-blue-700 transition flex items-center gap-2 shadow"
          >
            <svg
              class="w-4 h-4"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                stroke-width="2.5"
                d="M12 4v16m8-8H4"
              ></path>
            </svg>
            Tải lên tài liệu mới
          </button>
          <input
            type="file"
            ref="fileInput"
            @change="handleFileChange"
            accept="image/*,application/pdf"
            class="hidden"
          />
        </div>
      </nav>
    </header>

    <main class="max-w-[1700px] mx-auto p-6 md:p-8">
      <div
        v-if="isLoading"
        class="text-center p-20 text-gray-500 flex flex-col items-center gap-4"
      >
        <div
          class="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"
        ></div>
        <p>Đang tải danh sách tài liệu...</p>
      </div>
      <div
        v-if="error"
        class="bg-red-50 border border-red-200 text-red-700 p-4 rounded-lg text-center"
      >
        {{ error }}
      </div>

      <div
        v-if="!isLoading && !files.length"
        class="text-center p-20 py-32 bg-white dark:bg-gray-800 rounded-3xl border border-gray-100 dark:border-gray-700 shadow-xl border-dashed"
      >
        <svg
          class="mx-auto w-24 h-24 mb-6 text-gray-300 dark:text-gray-600"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            stroke-width="1.5"
            d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
          ></path>
        </svg>
        <h3 class="text-2xl font-bold text-gray-800 dark:text-gray-100">
          Project này chưa có tài liệu nào!
        </h3>
        <p class="text-gray-500 mt-2">
          Bạn hãy bấm nút Upload hoặc **kéo thả** file PDF vào đây để trợ lý AI
          xử lý.
        </p>
      </div>

      <div
        v-if="!isLoading && files.length"
        class="bg-white dark:bg-gray-800 shadow-xl rounded-2xl overflow-hidden border border-gray-100 dark:border-gray-700"
      >
        <table class="w-full text-sm text-left">
          <thead
            class="text-xs text-gray-500 uppercase bg-gray-50/50 dark:bg-gray-700/50 border-b dark:border-gray-700"
          >
            <tr>
              <th scope="col" class="px-6 py-4 w-5/12">Tên tài liệu</th>
              <th scope="col" class="px-6 py-4">Loại</th>
              <th scope="col" class="px-6 py-4">Trạng thái AI</th>
              <th scope="col" class="px-6 py-4">Ngày tạo</th>
              <th scope="col" class="px-6 py-4 w-[120px]">Thao tác</th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="file in files"
              :key="file.id"
              class="bg-white dark:bg-gray-800 border-b dark:border-gray-700 hover:bg-gray-50 dark:hover:bg-gray-700/50 cursor-pointer transition"
              @click="$router.push(`/projects/${projectId}/file/${file.id}`)"
            >
              <td
                class="px-6 py-4 font-medium text-gray-900 dark:text-gray-100 flex items-center gap-3"
              >
                <svg
                  v-if="file.type === 'pdf'"
                  class="w-6 h-6 text-red-500"
                  fill="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    d="M12 11.5l2-1.5 2 1.5-1.333 1.333-1.333-1.333-1.333 1.333L12 11.5z"
                  ></path>
                  <path
                    fill-rule="evenodd"
                    d="M12 2C6.477 2 2 6.477 2 12s4.477 10 10 10 10-4.477 10-10S17.523 2 12 2zM9.5 7h5a1 1 0 011 1v7a1 1 0 01-1 1h-5a1 1 0 01-1-1V8a1 1 0 011-1z"
                    clip-rule="evenodd"
                  ></path>
                </svg>
                <svg
                  v-else
                  class="w-6 h-6 text-blue-500"
                  fill="currentColor"
                  viewBox="0 0 24 24"
                >
                  <path
                    fill-rule="evenodd"
                    d="M4 5a2 2 0 012-2h8.586a1 1 0 01.707.293l4.414 4.414a1 1 0 01.293.707V19a2 2 0 01-2 2H6a2 2 0 01-2-2V5zm10 1V4.5l3.5 3.5H16a2 2 0 01-2-2z"
                    clip-rule="evenodd"
                  ></path>
                </svg>
                <span class="truncate">{{ file.name }}</span>
              </td>
              <td
                class="px-6 py-4 text-gray-500 dark:text-gray-400 font-mono uppercase text-xs"
              >
                {{ file.type }}
              </td>
              <td class="px-6 py-4">
                <span
                  :class="getStatusBadgeClass(file.status)"
                  class="text-xs font-semibold px-2.5 py-1 rounded-full whitespace-nowrap"
                >
                  {{ formatStatus(file.status) }}
                </span>
              </td>
              <td class="px-6 py-4 text-gray-500 dark:text-gray-400">
                {{ formatDate(file.createdAt) }}
              </td>
              <td class="px-6 py-4 flex gap-2 justify-center" @click.stop>
                <button
                  class="p-1.5 rounded hover:bg-gray-100 dark:hover:bg-gray-700 text-blue-600"
                  title="Xem kết quả"
                >
                  <svg
                    class="w-4 h-4"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
                    ></path>
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z"
                    ></path>
                  </svg>
                </button>
                <button
                  class="p-1.5 rounded hover:bg-gray-100 dark:hover:bg-gray-700 text-red-600"
                  title="Xóa tài liệu"
                >
                  <svg
                    class="w-4 h-4"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      stroke-linecap="round"
                      stroke-linejoin="round"
                      stroke-width="2"
                      d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
                    ></path>
                  </svg>
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </main>
  </div>
</template>

<script setup>
import { ref, onMounted } from "vue";
import { useRoute, useRouter } from "vue-router";

const route = useRoute();
const router = useRouter();
const config = useRuntimeConfig();

// --- STATES ---
const projectId = route.params.id;
const files = ref([]);
const isLoading = ref(true);
const error = ref(null);
const isDragging = ref(false);

// --- HÀM LẤY DANH SÁCH FILE THẬT TỪ C# ---
// --- HÀM LẤY DANH SÁCH FILE THẬT TỪ C# ---
async function fetchFiles() {
  isLoading.value = true;
  error.value = null;

  const token = localStorage.getItem("jwt_token");
  if (!token) {
    error.value = "Vui lòng đăng nhập để xem danh sách tài liệu.";
    isLoading.value = false;
    return;
  }

  try {
    // Gọi thẳng vào API C# vừa viết
    const response = await fetch(
      `${
        config.public.apiBaseUrl || "https://localhost:7084"
      }/api/Projects/${projectId}/files`,
      {
        method: "GET",
        headers: {
          Authorization: `Bearer ${token}`,
        },
      },
    );

    if (!response.ok) {
      throw new Error("Không thể tải danh sách tài liệu từ máy chủ.");
    }

    // Gán dữ liệu thật vào biến files để UI tự động render lại bảng
    const data = await response.json();
    files.value = data;
  } catch (err) {
    console.error("Lỗi fetch danh sách file:", err);
    error.value = err.message;
  } finally {
    isLoading.value = false;
  }
}

// --- HÀM XỬ LÝ UPLOAD FILE CHUẨN (Ghép API C# đã viết) ---
// Thay thế hàm handleFileUpload cũ bằng hàm này
async function handleFileUpload(pickedFile) {
  if (!pickedFile) return;

  const token = localStorage.getItem("jwt_token");
  if (!token) {
    alert("Vui lòng đăng nhập để upload file.");
    return;
  }

  // Tạo FormData giống hệt thuộc tính -F trong lệnh curl
  const formData = new FormData();
  formData.append("image", pickedFile);
  formData.append("projectId", projectId);

  isLoading.value = true;
  error.value = null; // Reset lỗi cũ nếu có

  try {
    // Gọi API của C# (Nhớ thêm ?saveAnnotated=false giống curl của bạn)
    const apiUrl = `${
      config.public.apiBaseUrl || "https://localhost:7084"
    }/api/Infer/upload-and-infer?saveAnnotated=false`;

    const response = await fetch(apiUrl, {
      method: "POST",
      body: formData,
      headers: {
        Authorization: `Bearer ${token}`,
        // KHÔNG set Content-Type ở đây, trình duyệt sẽ tự động set 'multipart/form-data' kèm boundary chuẩn
      },
    });

    if (!response.ok) {
      const errText = await response.text();
      throw new Error(errText || response.statusText);
    }

    const jobData = await response.json();

    // Lưu tạm jobId và imageUrl vào sessionStorage cho trang sau
    // (Lưu ý: jobData lúc này CHƯA có cục results tọa độ chữ đâu nhé)
    sessionStorage.setItem(
      `ocr_view_meta_${jobData.jobId}`,
      JSON.stringify({
        jobId: jobData.jobId,
        imageUrl: jobData.imageUrl,
      }),
    );

    // Chuyển hướng thẳng sang màn hình Đọc tài liệu
    router.push(`/projects/${projectId}/file/${jobData.jobId}`);
  } catch (err) {
    console.error("Lỗi Upload:", err);
    error.value = `Lỗi hệ thống: ${err.message}`;
  } finally {
    isLoading.value = false;
  }
}

// --- LOGIC HỖ TRỢ KÉO & THẢ (Drag & Drop Logic) ---
function handleFileChange(event) {
  const selectedFile = event.target.files[0];
  handleFileUpload(selectedFile);
}

function handleDrop(event) {
  isDragging.value = false;
  const selectedFile = event.dataTransfer.files[0];
  // Kiểm tra loại file
  if (
    !["application/pdf", "image/jpeg", "image/png"].includes(selectedFile.type)
  ) {
    alert("Chỉ hỗ trợ file Ảnh (JPG, PNG) hoặc PDF.");
    return;
  }
  handleFileUpload(selectedFile);
}

// --- HÀM BỔ TRỢ (UI Helpers) ---
function getStatusBadgeClass(status) {
  switch (status) {
    case "completed":
      return "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300";
    case "processing":
      return "bg-yellow-100 text-yellow-800 dark:bg-yellow-900/50 dark:text-yellow-300";
    case "failed":
      return "bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300";
    default:
      return "bg-gray-100 text-gray-800";
  }
}

function formatStatus(status) {
  switch (status) {
    case "completed":
      return "Xong";
    case "processing":
      return "Đang quét AI";
    case "failed":
      return "Lỗi";
    default:
      return "Chờ";
  }
}

function formatDate(dateString) {
  if (!dateString) return "";
  const date = new Date(dateString);
  return (
    date.toLocaleDateString("vi-VN") +
    " - " +
    date.toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" })
  );
}

// --- Khởi tạo ---
onMounted(() => {
  fetchFiles();
});
</script>

<style>
/* CSS để disable việc bôi đen toàn bộ app khi đang kéo thả file */
.fixed.inset-0.bg-blue-600\/90 * {
  pointer-events: none;
  user-select: none;
}
</style>
