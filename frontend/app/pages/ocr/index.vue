<template>
  <div class="p-6 space-y-6 bg-gray-800 rounded-xl shadow-lg text-gray-300">
    <h2 class="text-xl font-bold mb-4 border-b border-gray-700 pb-2 text-white">
      OCR (Stream từ ảnh)
    </h2>

    <div class="flex items-center gap-4">
      <label class="flex-grow">
        <span class="sr-only">Chọn file</span>
        <input
          type="file"
          @change="handleFileChange"
          accept="image/*"
          :disabled="isLoading"
          class="block w-full text-sm text-gray-400 file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-sm file:font-semibold file:bg-gray-700 file:text-gray-300 hover:file:bg-gray-600"
        />
      </label>

      <button
        @click="startOCR"
        :disabled="isLoading || !file"
        class="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors disabled:bg-gray-500 disabled:cursor-not-allowed"
      >
        {{ isLoading ? "Đang xử lý..." : "Bắt đầu OCR" }}
      </button>
    </div>

    <hr class="border-t border-gray-700" />

    <div
      v-if="statusMessage"
      class="p-4 bg-gray-900 border border-blue-800 rounded-lg text-blue-300"
    >
      <strong>Trạng thái OCR:</strong> {{ statusMessage }}
    </div>

    <h3 class="text-lg font-semibold text-white">
      Kết quả OCR ({{ results.length }} dòng tìm thấy):
    </h3>

    <ul
      class="ocr-results max-h-[500px] overflow-y-auto border border-gray-700 rounded-lg bg-gray-900 divide-y divide-gray-700"
    >
      <li v-if="!isLoading && results.length === 0" class="p-4 text-gray-500">
        Chưa có kết quả...
      </li>
      <li v-for="item in results" :key="item.line_number" class="p-4">
        <div class="item-text text-lg text-gray-200">
          <strong class="text-green-400">Dòng {{ item.line_number }}:</strong>
          {{ item.text }}
        </div>
        <div class="flex flex-wrap gap-4 mt-3">
          <figure>
            <figcaption class="text-xs text-gray-400 mb-1">
              Ảnh Crop:
            </figcaption>
            <img
              :src="item.crop_dataurl"
              alt="Cropped text"
              class="preview-img"
            />
          </figure>
          <figure>
            <figcaption class="text-xs text-gray-400 mb-1">
              Ảnh Highlight:
            </figcaption>
            <img
              :src="item.vis_dataurl"
              alt="Highlighted text"
              class="preview-img"
            />
          </figure>
        </div>
      </li>
    </ul>
  </div>
</template>

<script setup>
import { ref } from "vue";

// --- (TOÀN BỘ <script setup> CỦA BẠN GIỮ NGUYÊN) ---
const file = ref(null);
const results = ref([]);
const statusMessage = ref("Chưa chọn file");
const isLoading = ref(false);

const config = useRuntimeConfig();

function handleFileChange(event) {
  file.value = event.target.files[0];
  statusMessage.value = "Đã chọn file. Sẵn sàng bắt đầu.";
  results.value = [];
}

async function startOCR() {
  if (!file.value) {
    alert("Vui lòng chọn 1 file ảnh!");
    return;
  }
  isLoading.value = true;
  results.value = [];
  statusMessage.value = "Đang tải file lên...";
  const formData = new FormData();
  formData.append("file", file.value);
  const token = localStorage.getItem("jwt_token");
  if (!token) {
    statusMessage.value = "Lỗi: Bạn chưa đăng nhập!";
    isLoading.value = false;
    return;
  }
  try {
    const response = await fetch(
      `${config.public.apiBaseUrl}/api/Infer/stream`,
      {
        method: "POST",
        body: formData,
        headers: { Authorization: `Bearer ${token}` },
      }
    );
    if (!response.ok) {
      throw new Error(`Lỗi server: ${response.statusText}`);
    }
    const reader = response.body.getReader();
    const decoder = new TextDecoder();
    let buffer = "";
    while (true) {
      const { done, value } = await reader.read();
      if (done) {
        console.log("Stream finished.");
        break;
      }
      buffer += decoder.decode(value, { stream: true });
      const lines = buffer.split("\n\n");
      buffer = lines.pop();
      for (const line of lines) {
        if (line.startsWith("data:")) {
          try {
            const jsonString = line.substring(5).trim();
            const data = JSON.parse(jsonString);
            if (data.status === "result") {
              results.value.push(data);
            } else {
              statusMessage.value = data.message || data.status;
            }
          } catch (e) {
            console.error("Lỗi parse JSON từ stream:", line, e);
          }
        }
      }
    }
  } catch (error) {
    console.error("Lỗi khi gọi OCR:", error);
    statusMessage.value = `Lỗi nghiêm trọng: ${error.message}`;
  } finally {
    isLoading.value = false;
    if (statusMessage.value !== "Hoàn thành.") {
      statusMessage.value = `Xử lý xong. Tìm thấy ${results.value.length} dòng.`;
    }
  }
}
</script>

<style scoped>
/* 5. CSS lại ảnh preview và scrollbar (giống SearchBar) */
.preview-img {
  max-width: 200px;
  height: auto;
  max-height: 50px;
  border: 1px solid #4b5563; /* gray-600 */
  border-radius: 4px;
}

/* Style scrollbar giống SearchBar của bạn */
.ocr-results::-webkit-scrollbar {
  width: 8px;
}
.ocr-results::-webkit-scrollbar-track {
  background: #1f2937; /* gray-800 */
  border-radius: 10px;
}
.ocr-results::-webkit-scrollbar-thumb {
  background-color: #4b5563; /* gray-600 */
  border-radius: 10px;
  border: 2px solid #1f2937; /* gray-800 */
  background-clip: padding-box;
}
.ocr-results::-webkit-scrollbar-thumb:hover {
  background-color: #6b7280; /* gray-500 */
}
</style>
