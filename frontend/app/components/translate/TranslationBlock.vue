<template>
  <div class="translation-block flex bg-gray-800 rounded-xl border border-gray-700 mb-5 overflow-hidden relative shadow-md">
    
    <div class="panel source-panel flex-1 flex flex-col min-w-0">
      <textarea
        v-model="sourceText"
        placeholder="Nhập văn bản (Tiếng Nhật hoặc Tiếng Việt)..."
        maxlength="5000"
        class="flex-1 border-gray-100 p-4 text-lg resize-none min-h-[200px] leading-relaxed focus:outline-none w-full bg-gray-800 text-gray-100 placeholder-gray-500"
      ></textarea>

      <div class="panel-footer flex items-center justify-between py-2.5 px-5 border-t h-11 border-gray-700 bg-gray-800">
        <span class="char-count text-xs text-gray-500">{{ sourceText.length }}/5000</span>
        
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
          {{ isLoading ? 'Đang dịch...' : 'Dịch' }}
        </button>
      </div>
    </div>

    <div class="panel target-panel flex-1 flex flex-col min-w-0 border-l border-gray-700">
      <textarea
        v-model="targetText"
        placeholder="Bản dịch"
        readonly
        class="flex-1 p-4 text-lg border-gray-100 resize-none min-h-[200px] leading-relaxed focus:outline-none w-full bg-gray-800 text-gray-100 placeholder-gray-500"
      ></textarea>

      <div class="panel-footer flex items-center justify-end py-2.5 px-4 border-t border-gray-700 bg-gray-800">
        <button class="close-button bg-transparent text-2xl text-gray-500 cursor-pointer px-1.5 leading-none hover:text-red-500" @click="$emit('remove')" title="Đóng khối này">×</button>
      </div>
    </div>
  </div>
</template>

<script>

// Định nghĩa URL của API (đang chạy từ single_file_api_server.py)

const API_URL = 'http://127.0.0.1:8000/translate';



export default {

  name: 'TranslationBlock',

  emits: ['remove'],

  data() {

    return {

      sourceLanguage: 'Japanese', 

      targetLanguage: 'Vietnamese',

      sourceText: '',

      targetText: '',

      languages: ['Japanese', 'Vietnamese', 'English', 'Korean', 'Chinese'],

      isLoading: false, 

    };

  },



  // --- BẮT ĐẦU THÊM MỚI ---

  /**

   * Thêm 'watch' để theo dõi các biến data

   */

  watch: {

    /**

     * Bất cứ khi nào sourceText thay đổi, hàm này sẽ chạy.

     * @param {string} newValue - Giá trị mới của sourceText

     */

    sourceText(newValue) {

      // Nếu giá trị mới là một chuỗi rỗng (sau khi đã loại bỏ khoảng trắng)

      // thì tự động xóa luôn targetText.

      if (newValue.trim() === '') {

        this.targetText = '';

      }

    }

  },

  // --- KẾT THÚC THÊM MỚI ---



  methods: {

    swapLanguages() {

      // ... (code hàm swapLanguages của bạn giữ nguyên)

      [this.sourceLanguage, this.targetLanguage] = [this.targetLanguage, this.sourceLanguage];

      

      if (!this.targetText.startsWith('[Lỗi') && this.targetText.trim() !== '' && !this.isLoading) {

         [this.sourceText, this.targetText] = [this.targetText, this.sourceText];

      } else {

         this.targetText = '';

      }

    },



    async doTranslate() {

      // ... (code hàm doTranslate của bạn giữ nguyên)

      if (this.sourceText.trim() === '') {

        this.targetText = '';

        return;

      }



      this.isLoading = true;

      this.targetText = 'Đang dịch...';



      const langMap = {

        'Japanese': 'ja',

        'Vietnamese': 'vi'

      };

      

      const targetLangCode = langMap[this.targetLanguage];



      if (!targetLangCode) {

        this.targetText = `[Lỗi: API không hỗ trợ dịch sang '${this.targetLanguage}'.]`;

        this.isLoading = false;

        return;

      }



      const payload = {

        text: this.sourceText,

        tgt_lang: targetLangCode

      };



      try {

        const response = await fetch(API_URL, {

          method: 'POST',

          headers: {

            'Content-Type': 'application/json',

          },

          body: JSON.stringify(payload),

        });



        if (!response.ok) {

          const errorData = await response.json();

          throw new Error(errorData.detail || `Lỗi ${response.status}: ${response.statusText}`);

        }



        const data = await response.json();

        this.targetText = data.translated_text;



      } catch (error) {

        console.error('Lỗi khi gọi API dịch:', error);

        this.targetText = `[Lỗi: ${error.message}]`;

      } finally {

        this.isLoading = false;

      }

    }

  }

};

</script>

<style scoped>
/* Thêm một chút hiệu ứng cho textarea khi loading */
.target-panel textarea[readonly] {
  background-color: #1f2937; /* Màu bg-gray-800 */
}
</style>
