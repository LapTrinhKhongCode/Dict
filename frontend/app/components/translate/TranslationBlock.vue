<template>
  <div
    class="translation-block flex bg-gray-800 rounded-xl border border-gray-700 mb-5 overflow-hidden relative shadow-md"
  >
    <div class="panel source-panel flex-1 flex flex-col min-w-0">
      <textarea
        v-model="sourceText"
        placeholder="Nhập văn bản (Tiếng Nhật hoặc Tiếng Việt)..."
        maxlength="5000"
        class="flex-1 border-gray-100 p-4 text-lg resize-none min-h-[200px] leading-relaxed focus:outline-none w-full bg-gray-800 text-gray-100 placeholder-gray-500"
      ></textarea>

      <div
        class="panel-footer flex items-center justify-between py-2.5 px-5 border-t h-11 border-gray-700 bg-gray-800"
      >
        <span class="char-count text-xs text-gray-500"
          >{{ sourceText.length }}/5000</span
        >

        <!--
          CẬP NHẬT:
          - Thêm :disabled="isLoading"
          - Thêm class CSS để làm mờ nút khi 'isLoading'
          - Thay đổi văn bản của nút khi 'isLoading'
        -->
        <button
          class="translate-button flex items-center gap-1.5 bg-gray-700 text-gray-200 border border-gray-600 rounded-md px-4 text-sm font-bold cursor-pointer hover:bg-gray-600"
          :disabled="isLoading"
          :class="{ 'opacity-50 cursor-not-allowed': isLoading }"
          @click="doTranslate"
        >
          <span class="icon-translate text-blue-400">
            <!-- Hiển thị icon loading hoặc icon mặc định -->
            <span v-if="isLoading">⏳</span>
            <span v-else>✨</span>
          </span>
          {{ isLoading ? "Đang dịch..." : "Dịch" }}
        </button>
      </div>
    </div>

    <div
      class="panel target-panel flex-1 flex flex-col min-w-0 border-l border-gray-700"
    >
      <textarea
        v-model="targetText"
        placeholder="Bản dịch"
        readonly
        class="flex-1 p-4 text-lg border-gray-100 resize-none min-h-[200px] leading-relaxed focus:outline-none w-full bg-gray-800 text-gray-100 placeholder-gray-500"
      ></textarea>

      <div
        class="panel-footer flex items-center justify-end py-2.5 px-4 border-t border-gray-700 bg-gray-800"
      >
        <button
          class="close-button bg-transparent text-2xl text-gray-500 cursor-pointer px-1.5 leading-none hover:text-red-500"
          @click="$emit('remove')"
          title="Đóng khối này"
        >
          ×
        </button>
      </div>
    </div>
  </div>
</template>
<script>
// KHÔNG gọi 'useRuntimeConfig' ở đây

export default {
  name: "TranslationBlock",

  props: {
    searchWord: {
      type: String,
      default: "",
    },
  },
  emits: ["remove"],

  // --- SỬA LỖI 1: Thêm hàm setup() ---
  setup() {
    // Gọi 'useRuntimeConfig' BÊN TRONG setup()
    const config = useRuntimeConfig();

    // Trả về 'config' để 'this' có thể truy cập
    return {
      config, // -> 'this.config'
    };
  },
  // --- KẾT THÚC SỬA 1 ---

  data() {
    return {
      sourceLanguage: "Japanese",
      targetLanguage: "Vietnamese",
      sourceText: "",
      targetText: "",
      languages: ["Japanese", "Vietnamese", "English", "Korean", "Chinese"],
      isLoading: false,
      debounceTimer: null,
      isUpdatingFromProp: false,
    };
  },

  watch: {
    sourceText(newValue) {
      if (this.isUpdatingFromProp) {
        return;
      }
      if (this.debounceTimer) {
        clearTimeout(this.debounceTimer);
      }
      if (newValue.trim() === "") {
        this.targetText = "";
        return;
      }
      this.debounceTimer = setTimeout(() => {
        this.doTranslate();
      }, 500);
    },

    searchWord: {
      handler(newWord) {
        if (newWord && newWord !== this.sourceText) {
          this.isUpdatingFromProp = true;
          this.sourceText = newWord;
          this.doTranslate();
          this.$nextTick(() => {
            this.isUpdatingFromProp = false;
          });
        }
      },
      immediate: true,
    },
  },

  // (Xóa mounted() - đã được 'immediate: true' thay thế)

  methods: {
    swapLanguages() {
      // ... (giữ nguyên)
    },

    async doTranslate() {
      if (this.debounceTimer) {
        clearTimeout(this.debounceTimer);
      }
      if (this.sourceText.trim() === "") {
        this.targetText = "";
        return;
      }
      this.isLoading = true;
      this.targetText = "Đang dịch...";

      // --- SỬA LỖI 2: Lấy API_URL từ 'this.config' ---
      const API_URL = `${this.config.public.apiBaseUrl}/api/Infer/translate`;
      // --- KẾT THÚC SỬA 2 ---

      const payload = {
        text: this.sourceText,
        tgt_lang: "string",
      };

      try {
        const response = await fetch(API_URL, {
          // Dùng API_URL mới
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
        this.targetText = data.translated_text;
      } catch (error) {
        console.error("Lỗi khi gọi API dịch:", error);
        this.targetText = `[Lỗi: ${error.message}]`;
      } finally {
        this.isLoading = false;
      }
    },
  },
};
</script>

<style scoped>
/* (Style của bạn giữ nguyên) */
.target-panel textarea[readonly] {
  background-color: #1f2937;
}
</style>
