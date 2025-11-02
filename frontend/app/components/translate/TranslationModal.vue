<template>
  <div
    class="fixed inset-0 z-60 bg-black bg-opacity-20 flex items-center justify-center"
    style="background-color: rgba(0, 0, 0, 0.5)"
    @click.self="$emit('close')"
  >
    <div
      class="relative bg-white rounded-xl shadow-2xl w-full max-w-6xl h-[85vh] flex flex-col"
      @click.stop
    >
      <button
        @click="$emit('close')"
        class="absolute -top-3 -right-3 z-50 bg-gray-800 text-white rounded-full p-1.5 hover:bg-gray-600 transition"
      >
        <UIcon name="i-lucide-x" class="size-5" />
      </button>

      <div
        class="translation-block flex flex-col h-full bg-gray-800 rounded-xl overflow-hidden"
      >
        <div class="panel source-panel flex-1 flex flex-col min-w-0">
          <textarea
            v-model="sourceText"
            placeholder="Nhập văn bản (Tiếng Nhật hoặc Tiếng Việt)..."
            maxlength="5000"
            class="flex-1 border-gray-100 p-4 text-lg resize-none leading-relaxed focus:outline-none w-full bg-gray-800 text-gray-100 placeholder-gray-500"
          ></textarea>
          <div
            class="panel-footer flex items-center justify-between py-2.5 px-5 border-t h-11 border-gray-700 bg-gray-800"
          >
            <span class="char-count text-xs text-gray-500"
              >{{ sourceText.length }}/5000</span
            >
            <button
              class="translate-button flex items-center gap-1.5 bg-gray-700 text-gray-200 border border-gray-600 rounded-md px-4 text-sm font-bold cursor-pointer hover:bg-gray-600"
              :disabled="isLoading"
              :class="{ 'opacity-50 cursor-not-allowed': isLoading }"
              @click="doTranslate"
            >
              <span class="icon-translate text-blue-400">
                <span v-if="isLoading">⏳</span>
                <span v-else>✨</span>
              </span>
              {{ isLoading ? "Đang dịch..." : "Dịch" }}
            </button>
          </div>
        </div>

        <div
          class="panel target-panel flex-1 flex flex-col min-w-0 border-t border-gray-700"
        >
          <textarea
            v-model="targetText"
            placeholder="Bản dịch"
            readonly
            class="flex-1 p-4 text-lg border-gray-100 resize-none leading-relaxed focus:outline-none w-full bg-gray-800 text-gray-100 placeholder-gray-500"
          ></textarea>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from "vue";

// --- 1. Nhận Prop (chữ bôi đen) & Emits ---
const props = defineProps<{
  searchWord: string;
}>();
const emit = defineEmits(["close"]);

// --- 2. Chuyển đổi Data sang <script setup> ---
const sourceText = ref("");
const targetText = ref("");
const isLoading = ref(false);

const config = useRuntimeConfig();

// --- SỬA LỖI TẠI ĐÂY ---
// Gọi API C# (.NET) của bạn, không gọi Python
const API_URL = `${config.public.apiBaseUrl}/api/Infer/translate`;
// --- KẾT THÚC SỬA LỖI ---

// --- 3. Chuyển đổi Methods ---
async function doTranslate() {
  if (sourceText.value.trim() === "") {
    targetText.value = "";
    return;
  }

  isLoading.value = true;
  targetText.value = "Đang dịch...";

  // API của bạn tự động đảo chiều, nên payload rất đơn giản
  const payload = {
    text: sourceText.value,
    tgt_lang: "string", // (API của bạn yêu cầu trường này, dù không dùng)
  };

  try {
    const response = await fetch(API_URL, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        // (Nếu API C# của bạn yêu cầu JWT, hãy thêm 'Authorization' ở đây)
      },
      body: JSON.stringify(payload),
    });

    if (!response.ok) {
      // Đọc lỗi từ .NET (thường là 'detail' hoặc 'title')
      const errorData = await response.json();
      throw new Error(
        errorData.detail || errorData.title || `Lỗi ${response.status}`
      );
    }

    const data = await response.json();
    targetText.value = data.translated_text; // Đọc response C#
  } catch (error: any) {
    console.error("Lỗi khi gọi API dịch:", error);
    targetText.value = `[Lỗi: ${error.message}]`;
  } finally {
    isLoading.value = false;
  }
}

// --- 4. Chuyển đổi Watch (giữ nguyên) ---
watch(sourceText, (newValue) => {
  if (newValue.trim() === "") {
    targetText.value = "";
  }
});

// --- 5. LOGIC MỚI: Tự động điền và dịch (giữ nguyên) ---
watch(
  () => props.searchWord,
  (newWord) => {
    if (newWord) {
      sourceText.value = newWord;
    }
  },
  { immediate: true }
);

watch(sourceText, (newText, oldText) => {
  // Chỉ tự động dịch nếu text thay đổi (và không phải là lần đầu tiên)
  if (newText && newText !== oldText) {
    doTranslate();
  }
});
</script>
<style scoped>
/* Style cho modal (bạn có thể dán CSS modal toàn cục vào đây
   hoặc để ở app.vue đều được) */
.modal-overlay {
  position: fixed;
  inset: 0;
  z-index: 60;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
}
.modal-content {
  position: relative;
  background: white;
  border-radius: 0.75rem; /* rounded-xl */
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
  width: 100%;
  max-width: 64rem; /* max-w-6xl */
  height: 85vh;
  display: flex;
  flex-direction: column;
}
.modal-close-button {
  position: absolute;
  top: -0.75rem; /* -top-3 */
  right: -0.75rem; /* -right-3 */
  z-index: 50;
  background-color: #1f2937; /* bg-gray-800 */
  color: white;
  border-radius: 9999px; /* rounded-full */
  padding: 0.375rem; /* p-1.5 */
  transition: background-color 0.2s;
}
.modal-close-button:hover {
  background-color: #374151; /* hover:bg-gray-700 */
}

/* Style cho nội dung bên trong */
.translation-block {
  flex-grow: 1; /* Đảm bảo nó lấp đầy modal-content */
}
.panel textarea {
  height: 100%; /* Đảm bảo textarea co giãn */
  min-height: 150px; /* Chiều cao tối thiểu */
}
</style>
