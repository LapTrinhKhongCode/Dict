<template>
  <div id="translator-app-wrapper" class="bg-gray-800 font-sans p-6 min-h-screen">
    
    <div id="translator-app" class="max-w-4xl mx-auto relative">
      <TranslationBlock
        v-for="block in translationBlocks"
        :key="block.id"
        @remove="removeBlock(block.id)"
      />

      <div class="add-block-container text-center mt-4 bg-gray-800 ">
        <button 
          @click="addTranslationBlock" 
          class="add-button w-full  border border-dashed bg-gray-800  border-gray-300 rounded-lg text-blue-500 cursor-pointer text-base py-4 px-8 transition-all duration-300 inline-flex items-center justify-center min-h-[80px] font-medium hover:border-blue-500 hover:text-blue-400"
        >
          <span class="plus-icon text-2xl mr-2 ">+</span> Thêm bản dịch
        </button>
      </div>
    </div>
  </div>
</template>

<script>
// Cập nhật đường dẫn này nếu cần
import TranslationBlock from '../../components/translate/TranslationBlock.vue';

export default {
  name: 'TranslatePage', // Đổi tên từ 'App' để rõ ràng hơn
  components: {
    TranslationBlock
  },
  data() {
    return {
      translationBlocks: [],
      nextBlockId: 0
    };
  },
  created() {
    this.addTranslationBlock();
  },
  methods: {
    addTranslationBlock() {
      this.translationBlocks.push({ id: this.nextBlockId++ });
    },
    removeBlock(id) {
      if (this.translationBlocks.length > 1) {
        this.translationBlocks = this.translationBlocks.filter(block => block.id !== id);
      } else {
        alert("Bạn không thể xóa khối dịch cuối cùng!");
      }
    }
  }
};
</script>