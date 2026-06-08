<template>
  <div
    class="p-4 space-y-4 bg-white dark:bg-gray-800 rounded-xl shadow-lg h-full flex flex-col transition-colors"
  >
    <div
      class="flex items-center gap-4 border-b border-gray-200 dark:border-gray-700 pb-4"
    >
      <h2
        class="text-xl font-bold text-gray-900 dark:text-white whitespace-nowrap"
      >
        OCR Workspace
      </h2>
      <input
        type="file"
        @change="handleFileChange"
        accept="image/*"
        :disabled="isLoading"
        class="block w-full text-sm text-gray-500 file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-sm file:font-semibold file:bg-blue-50 file:text-blue-700 hover:file:bg-blue-100 dark:file:bg-gray-700 dark:file:text-gray-300"
      />
      <select
        v-model="selectedProjectId"
        class="p-2 border rounded-lg dark:bg-gray-700 dark:text-white dark:border-gray-600"
      >
        <option :value="null">Không thuộc workspace</option>
        <option v-for="ws in projects" :key="ws.id" :value="ws.id">
          {{ ws.name }}
        </option>
      </select>

      <button
        @click="startOCR"
        :disabled="isLoading || !file"
        class="px-6 py-2 bg-blue-600 text-white font-semibold rounded-lg hover:bg-blue-700 transition disabled:bg-gray-400 disabled:cursor-not-allowed whitespace-nowrap"
      >
        {{ isLoading ? "Đang quét..." : "Bắt đầu quét AI" }}
      </button>
    </div>

    <div
      v-if="statusMessage"
      class="text-sm text-blue-600 dark:text-blue-400 font-medium"
    >
      Trạng thái: {{ statusMessage }}
    </div>

    <div class="flex-1 flex gap-6 overflow-hidden min-h-[70vh]">
      <div
        class="w-1/2 bg-gray-100 dark:bg-gray-900 rounded-lg p-2 flex items-center justify-center relative border border-gray-200 dark:border-gray-700"
      >
        <div
          v-if="!originalImageUrl"
          class="text-gray-400 dark:text-gray-500 flex flex-col items-center"
        >
          <svg
            class="w-16 h-16 mb-2 opacity-50"
            fill="none"
            stroke="currentColor"
            viewBox="0 0 24 24"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              stroke-width="2"
              d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"
            ></path>
          </svg>
          <p>Khu vực xem trước tài liệu</p>
        </div>

        <div
          v-else
          class="image-overlay-container w-full h-full relative flex items-center justify-center overflow-hidden"
        >
          <img
            :src="originalImageUrl"
            alt="Bản gốc"
            ref="originalImageRef"
            @load="updateImageDimensions"
            class="max-w-full max-h-full object-contain shadow-sm rounded"
          />

          <div
            v-for="(item, index) in parsedResults"
            :key="index"
            class="ocr-bbox"
            :class="{ 'is-hovered': hoveredIndex === index }"
            :style="calculateBoxStyle(item.parsedBbox)"
            @mouseover="hoveredIndex = index"
            @mouseleave="hoveredIndex = -1"
            :title="item.wordText"
          ></div>
        </div>
      </div>

      <div class="w-1/2 flex flex-col gap-3">
        <h3 class="text-lg font-semibold text-gray-800 dark:text-gray-200">
          Văn bản trích xuất
        </h3>

        <textarea
          v-model="detectedText"
          class="flex-1 w-full p-4 border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 focus:ring-2 focus:ring-blue-500 focus:border-blue-500 resize-none font-mono leading-relaxed"
          placeholder="Kết quả OCR sẽ hiển thị tại đây để bạn chỉnh sửa..."
        ></textarea>

        <div class="flex gap-3 justify-end mt-2">
          <button
            class="px-4 py-2 bg-gray-200 dark:bg-gray-700 text-gray-800 dark:text-gray-200 font-medium rounded hover:bg-gray-300 dark:hover:bg-gray-600 transition"
          >
            Sao chép
          </button>
          <button
            class="px-4 py-2 bg-green-600 text-white font-medium rounded hover:bg-green-700 transition"
          >
            Lưu vào Dự án
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from "vue";
import { useJwt } from "~/composables/useJwt";

// --- STATES ---
const file = ref(null);
const originalImageUrl = ref(null);
const originalImageRef = ref(null);
const imageDimensions = ref({ nativeWidth: 0, nativeHeight: 0 });

const isLoading = ref(false);
const statusMessage = ref("Chưa chọn file");
const selectedProjectId = ref(null);
const projects = ref([]); // Danh sách project từ API

// Data từ API
const detectedText = ref("");
const rawResults = ref([]); // Mảng chứa wordText và boundingBox JSON string

const hoveredIndex = ref(-1);
const config = useRuntimeConfig();
const { jwt } = useJwt();

// Load project list từ backend (workspace hiện tại nếu có, hoặc tất cả workspace)
onMounted(async () => {
  try {
    const res = await $fetch(`${config.public.apiBaseUrl}/api/workspaces`, {
      headers: { Authorization: `Bearer ${jwt.value ?? ""}` },
    });
    // Lấy tất cả workspace để user chọn project/workspace
    projects.value = Array.isArray(res) ? res : [];
  } catch {
    // Không fatal — chỉ bỏ dropdown nếu API lỗi
  }
});

// --- LOGIC GIAO DIỆN ---
function handleFileChange(event) {
  file.value = event.target.files[0];
  if (!file.value) return;

  statusMessage.value = "Sẵn sàng quét.";
  detectedText.value = "";
  rawResults.value = [];
  imageDimensions.value = { nativeWidth: 0, nativeHeight: 0 };

  if (originalImageUrl.value) URL.revokeObjectURL(originalImageUrl.value);
  originalImageUrl.value = URL.createObjectURL(file.value);
}

function updateImageDimensions() {
  if (originalImageRef.value) {
    imageDimensions.value = {
      nativeWidth: originalImageRef.value.naturalWidth,
      nativeHeight: originalImageRef.value.naturalHeight,
    };
  }
}

// --- LOGIC PARSE BOUNDING BOX TỪ GOOGLE VISION ---
// Google trả về string: "[[x1,y1], [x2,y2], [x3,y3], [x4,y4]]"
const parsedResults = computed(() => {
  return rawResults.value
    .map((item) => {
      let parsedBbox = null;
      try {
        const arr = JSON.parse(item.boundingBox); // Parse chuỗi JSON
        if (arr && arr.length > 0) {
          // Tìm x nhỏ nhất, lớn nhất, y nhỏ nhất, lớn nhất
          const xs = arr.map((pt) => pt[0]);
          const ys = arr.map((pt) => pt[1]);
          parsedBbox = [
            Math.min(...xs), // x_min
            Math.min(...ys), // y_min
            Math.max(...xs), // x_max
            Math.max(...ys), // y_max
          ];
        }
      } catch (e) {
        console.error("Lỗi parse Bbox:", e);
      }
      return { ...item, parsedBbox };
    })
    .filter((item) => item.parsedBbox !== null);
});

// --- LOGIC VẼ BOX ĐÈ LÊN ẢNH (TỶ LỆ CHUẨN) ---
function calculateBoxStyle(bbox) {
  const { nativeWidth, nativeHeight } = imageDimensions.value;
  if (!bbox || !originalImageRef.value || nativeWidth === 0)
    return { display: "none" };

  const container = originalImageRef.value.parentElement;
  if (!container) return { display: "none" };

  const containerWidth = container.clientWidth;
  const containerHeight = container.clientHeight;

  // Tính tỷ lệ thu nhỏ của ảnh khi dùng object-fit: contain
  const ratioX = containerWidth / nativeWidth;
  const ratioY = containerHeight / nativeHeight;
  const ratio = Math.min(ratioX, ratioY);

  // Kích thước thực tế của ảnh đang hiển thị trên màn hình
  const displayedWidth = nativeWidth * ratio;
  const displayedHeight = nativeHeight * ratio;

  // Canh lề (Khoảng trống bị dư ra do object-fit)
  const offsetX = (containerWidth - displayedWidth) / 2;
  const offsetY = (containerHeight - displayedHeight) / 2;

  const [x_min, y_min, x_max, y_max] = bbox;

  return {
    top: `${y_min * ratio + offsetY}px`,
    left: `${x_min * ratio + offsetX}px`,
    width: `${(x_max - x_min) * ratio}px`,
    height: `${(y_max - y_min) * ratio}px`,
  };
}

// --- LOGIC GỌI API (C# BACKEND MỚI) ---
async function startOCR() {
  if (!file.value) return;

  const token = jwt.value;
  if (!token) {
    statusMessage.value = "Lỗi: Chưa đăng nhập.";
    return;
  }

  isLoading.value = true;
  statusMessage.value = "Đang gửi ảnh cho Google Cloud Vision xử lý...";

  const formData = new FormData();
  formData.append("image", file.value);
  if (selectedProjectId.value) {
    formData.append("projectId", selectedProjectId.value);
  }

  try {
    // Gọi thẳng endpoint trả về JSON của C#
    const apiUrl = `${
      config.public.apiBaseUrl || "https://localhost:7084"
    }/api/Infer/upload-and-infer`;

    const response = await fetch(apiUrl, {
      method: "POST",
      body: formData,
      headers: {
        Authorization: `Bearer ${token}`,
      },
    });

    if (!response.ok) {
      const errText = await response.text();
      throw new Error(errText || response.statusText);
    }

    // Backend Google C# trả về 1 cục JSON duy nhất
    const data = await response.json();

    // Gán dữ liệu vào UI
    detectedText.value = data.detectedText || "";
    if (data.results && Array.isArray(data.results)) {
      rawResults.value = data.results;
    }

    statusMessage.value = `Hoàn tất! Bóc tách thành công văn bản.`;
  } catch (error) {
    console.error("Lỗi OCR:", error);
    statusMessage.value = `Lỗi: ${error.message}`;
  } finally {
    isLoading.value = false;
  }
}
</script>

<style scoped>
/* Box viền đỏ cho chữ */
.ocr-bbox {
  position: absolute;
  border: 1.5px solid rgba(59, 130, 246, 0.4); /* Màu xanh nhạt */
  background-color: transparent;
  transition: all 0.15s ease-in-out;
  cursor: crosshair;
  border-radius: 2px;
}

/* Khi rê chuột vào chữ trên ảnh */
.ocr-bbox.is-hovered {
  background-color: rgba(250, 204, 21, 0.4); /* Highlight vàng */
  border-color: rgba(234, 179, 8, 0.8);
  box-shadow: 0 0 8px rgba(250, 204, 21, 0.8);
  z-index: 20;
}

/* Tùy chỉnh thanh cuộn cho Textarea */
textarea::-webkit-scrollbar {
  width: 8px;
}
textarea::-webkit-scrollbar-track {
  background: transparent;
}
textarea::-webkit-scrollbar-thumb {
  background-color: #cbd5e1;
  border-radius: 10px;
}
.dark textarea::-webkit-scrollbar-thumb {
  background-color: #475569;
}
</style>
