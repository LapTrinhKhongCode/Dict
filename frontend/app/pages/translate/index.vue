<template>
  <div
    id="translator-app-wrapper"
    class="bg-gray-50 dark:bg-gray-800 font-sans p-6 min-h-screen transition-colors"
  >
    <div id="translator-app" class="max-w-4xl mx-auto relative">
      <TranslationBlock
        v-for="block in translationBlocks"
        :key="block.id"
        @remove="removeBlock(block.id)"
      />

      <div
        class="add-block-container text-center mt-4 bg-gray-50 dark:bg-gray-800"
      >
        <button
          @click="addTranslationBlock"
          class="add-button w-full border border-dashed bg-white dark:bg-gray-800 border-gray-300 dark:border-gray-600 rounded-lg text-primary-600 dark:text-blue-500 cursor-pointer text-base py-4 px-8 transition-all duration-300 inline-flex items-center justify-center min-h-[80px] font-medium hover:border-primary-500 dark:hover:border-blue-500 hover:text-primary-500 dark:hover:text-blue-400"
        >
          <span class="plus-icon text-2xl mr-2">+</span> Thêm bản dịch
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
// ✅ THAY ĐỔI: Chuyển sang <script setup>
import { ref } from "vue";
import TranslationBlock from "../../components/translate/TranslationBlock.vue";

// Định nghĩa kiểu dữ liệu cho block
interface Block {
  id: number;
}

// data() -> ref
const translationBlocks = ref<Block[]>([]);
const nextBlockId = ref(0);

// methods -> function
function addTranslationBlock() {
  translationBlocks.value.push({ id: nextBlockId.value++ });
}

function removeBlock(id: number) {
  if (translationBlocks.value.length > 1) {
    translationBlocks.value = translationBlocks.value.filter(
      (block) => block.id !== id
    );
  } else {
    alert("Bạn không thể xóa khối dịch cuối cùng!");
  }
}

// created() -> Chạy trực tiếp trong setup
addTranslationBlock();
</script>