<template>
  <div
    v-if="isOpen"
    class="fixed inset-0 z-60 flex items-center justify-center p-4"
    style="background-color: rgba(0, 0, 0, 0.5)"
    @click.self="$emit('close')"
  >
    <div
      class="relative rounded-xl shadow-2xl w-full max-w-6xl h-[85vh] flex flex-col"
      @click.stop
    >
      <button
        @click="$emit('close')"
        class="absolute -top-3 -right-3 z-50 bg-gray-200 text-gray-700 hover:bg-gray-300 dark:bg-gray-800 dark:text-white dark:hover:bg-gray-600 rounded-full p-1.5 transition"
      >
        <UIcon name="i-lucide-x" class="size-5" />
      </button>

      <div
        class="translation-block flex flex-col h-full bg-white dark:bg-gray-800 rounded-xl overflow-hidden border border-gray-200 dark:border-gray-700"
      >
        <div class="panel source-panel flex-1 flex flex-col min-w-0">
          <textarea
            v-model="sourceText"
            placeholder="Nhập văn bản (Tiếng Nhật hoặc Tiếng Việt)..."
            maxlength="5000"
            class="flex-1 p-4 text-lg resize-none leading-relaxed focus:outline-none w-full bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500"
          ></textarea>
          <div
            class="panel-footer flex items-center justify-between py-2.5 px-5 border-t h-11 border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800"
          >
            <span
              class="char-count text-xs text-gray-400 dark:text-gray-500"
              >{{ sourceText.length }}/5000</span
            >
            <button
              class="translate-button flex items-center gap-1.5 bg-gray-100 text-gray-700 border border-gray-300 hover:bg-gray-200 dark:bg-gray-700 dark:text-gray-200 dark:border-gray-600 dark:hover:bg-gray-600 rounded-md px-4 text-sm font-bold cursor-pointer"
              :disabled="isLoading"
              :class="{ 'opacity-50 cursor-not-allowed': isLoading }"
              @click="doTranslate"
            >
              <span
                class="icon-translate text-primary-600 dark:text-blue-400"
              >
                <span v-if="isLoading">⏳</span>
                <span v-else>✨</span>
              </span>
              {{ isLoading ? "Đang dịch..." : "Dịch" }}
            </button>
          </div>
        </div>

        <div
          class="panel target-panel flex-1 flex flex-col min-w-0 border-t border-gray-200 dark:border-gray-700"
        >
          <textarea
            v-model="targetText"
            placeholder="Bản dịch"
            readonly
            class="flex-1 p-4 text-lg resize-none leading-relaxed focus:outline-none w-full bg-gray-50 dark:bg-gray-800 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500"
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
  isOpen: boolean; // ✅ THÊM: Prop để điều khiển modal
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

// ✅ THÊM: Watch khi modal mở/đóng
watch(
  () => props.isOpen,
  (newVal) => {
    if (newVal) {
      // Khi modal mở, nếu searchWord có giá trị, tự động dịch
      if (props.searchWord && props.searchWord !== sourceText.value) {
        sourceText.value = props.searchWord;
        doTranslate();
      } else if (sourceText.value) {
        // Nếu đã có sẵn text, cũng dịch
        doTranslate();
      }
    } else {
      // (Tùy chọn) Xóa text khi đóng modal
      // sourceText.value = "";
      // targetText.value = "";
    }
  }
);
</script>

<style scoped>
/* (Toàn bộ style cũ đã bị xóa vì Tailwind đã xử lý) */

/* Thêm style cho textarea đích (readonly) */
.panel.target-panel textarea[readonly] {
  background-color: #f9fafb; /* bg-gray-50 */
}
.dark .panel.target-panel textarea[readonly] {
  background-color: #1f2937; /* bg-gray-800-ish */
}
</style>