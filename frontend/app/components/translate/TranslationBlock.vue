<template>
  <div
    class="translation-block flex bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 mb-5 overflow-hidden relative shadow-md"
  >
    <div class="panel source-panel flex-1 flex flex-col min-w-0">
      <textarea
        v-model="sourceText"
        placeholder="Nhập văn bản (Tiếng Nhật hoặc Tiếng Việt)..."
        maxlength="5000"
        class="flex-1 p-4 text-lg resize-none min-h-[200px] leading-relaxed focus:outline-none w-full bg-white dark:bg-gray-800 text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500"
      ></textarea>

      <div
        class="panel-footer flex items-center justify-between py-2.5 px-5 border-t h-11 border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800"
      >
        <span class="char-count text-xs text-gray-400 dark:text-gray-500"
          >{{ sourceText.length }}/5000</span
        >

        <button
          class="translate-button flex items-center gap-1.5 bg-gray-100 text-gray-700 border border-gray-300 hover:bg-gray-200 dark:bg-gray-700 dark:text-gray-200 dark:border-gray-600 dark:hover:bg-gray-600 rounded-md px-4 text-sm font-bold cursor-pointer"
          :disabled="isLoading"
          :class="{ 'opacity-50 cursor-not-allowed': isLoading }"
          @click="doTranslate"
        >
          <span class="icon-translate text-primary-600 dark:text-blue-400">
            <span v-if="isLoading">⏳</span>
            <span v-else>✨</span>
          </span>
          {{ isLoading ? "Đang dịch..." : "Dịch" }}
        </button>
      </div>
    </div>

    <div
      class="panel target-panel flex-1 flex flex-col min-w-0 border-l border-gray-200 dark:border-gray-700"
    >
      <textarea
        v-model="targetText"
        placeholder="Bản dịch"
        readonly
        class="flex-1 p-4 text-lg resize-none min-h-[200px] leading-relaxed focus:outline-none w-full text-gray-900 dark:text-gray-100 placeholder-gray-400 dark:placeholder-gray-500"
      ></textarea>

      <div
        class="panel-footer flex items-center justify-end py-2.5 px-4 border-t border-gray-200 dark:border-gray-700 bg-white dark:bg-gray-800"
      >
        <button
          class="close-button bg-transparent text-2xl text-gray-400 dark:text-gray-500 cursor-pointer px-1.5 leading-none hover:text-red-500"
          @click="$emit('remove')"
          title="Đóng khối này"
        >
          ×
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
// ✅ THAY ĐỔI: Chuyển sang <script setup lang="ts">
import { ref, watch } from "vue";
import { useRuntimeConfig } from "#app";

// --- Props ---
const props = defineProps<{
  searchWord: {
    type: String;
    default: "";
  };
}>();

// --- Emits ---
const emit = defineEmits(["remove"]);

// --- Config (Từ setup() cũ) ---
const config = useRuntimeConfig();

// --- Data -> Refs ---
const sourceText = ref("");
const targetText = ref("");
const isLoading = ref(false);
const debounceTimer = ref<NodeJS.Timeout | null>(null);
const isUpdatingFromProp = ref(false);

// --- Watch: sourceText ---
watch(sourceText, (newValue) => {
  if (isUpdatingFromProp.value) {
    return;
  }
  if (debounceTimer.value) {
    clearTimeout(debounceTimer.value);
  }
  if (newValue.trim() === "") {
    targetText.value = "";
    return;
  }
  debounceTimer.value = setTimeout(() => {
    doTranslate();
  }, 500);
});

// --- Watch: searchWord (Prop) ---
watch(
  () => props.searchWord,
  (newWord) => {
    if (newWord && newWord !== sourceText.value) {
      isUpdatingFromProp.value = true;
      sourceText.value = newWord;
      doTranslate(); // Dịch ngay lập tức khi prop thay đổi

      // Reset cờ sau khi DOM đã cập nhật
      nextTick(() => {
        isUpdatingFromProp.value = false;
      });
    }
  },
  { immediate: true }
);

// --- Method: doTranslate ---
async function doTranslate() {
  if (debounceTimer.value) {
    clearTimeout(debounceTimer.value);
  }
  if (sourceText.value.trim() === "") {
    targetText.value = "";
    return;
  }
  isLoading.value = true;
  targetText.value = "Đang dịch...";

  const API_URL = `${config.public.apiBaseUrl}/api/Infer/translate`;
  const payload = {
    text: sourceText.value,
    tgt_lang: "string", // (Giữ nguyên từ code của bạn)
  };

  try {
    const response = await fetch(API_URL, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(payload),
    });

    if (!response.ok) {
      const errorData = await response.json();
      throw new Error(errorData.detail || `Lỗi ${response.status}`);
    }

    const data = await response.json();
    targetText.value = data.translated_text;
  } catch (error: any) {
    console.error("Lỗi khi gọi API dịch:", error);
    targetText.value = `[Lỗi: ${error.message}]`;
  } finally {
    isLoading.value = false;
  }
}
</script>

<style scoped>
/* THAY ĐỔI:
  - Tái cấu trúc lại style cho cả 2 chế độ
*/

/* Light Mode: Nền xám nhạt cho textarea readonly */
.target-panel textarea[readonly] {
  background-color: #f9fafb; /* bg-gray-50 */
}

/* Dark Mode: Nền xám tối cho textarea readonly */
.dark .target-panel textarea[readonly] {
  background-color: #1f2937; /* bg-gray-800-ish */
}
</style>