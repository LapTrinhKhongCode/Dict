<template>
  <div class="flex h-full" style="max-height: calc(100vh - 64px);">
    
    <aside class="w-1/3 bg-white p-4 overflow-y-auto shadow-lg text-gray-900">
      <h2 class="text-xl font-bold mb-4 border-b pb-2">Danh sách Bài báo</h2>
      
      <div v-if="loading" class="text-center text-gray-500">Đang tải...</div>
      <div v-if="error" class="text-red-500">{{ error }}</div>
      
      <ul v-if="!loading && !error" class="space-y-2">
        <li
          v-for="article in articles"
          :key="article.url"
          @click="selectArticle(article)"
          class="p-3 rounded-lg cursor-pointer hover:bg-gray-100 transition-colors"
          :class="{ 'bg-blue-100': selectedArticle?.url === article.url }"
        >
          <div v-html="article.title" class="article-list-title"></div>
        </li>
      </ul>
    </aside>

    <main class="w-2/3 p-6 overflow-y-auto"> 
      
      <div class="flex justify-end mb-4">
        <button 
          @click="toggleFurigana" 
          class="px-4 py-2 bg-blue-600 text-white rounded-lg hover:bg-blue-700 transition-colors text-sm font-medium"
        >
          {{ showFurigana ? 'Tắt Furigana (ルビを隠す)' : 'Bật Furigana (ルビを表示)' }}
        </button>
      </div>

      <div v-if="!selectedArticle" class="text-center text-gray-400 mt-10">
        Hãy chọn một bài báo từ danh sách bên trái để bắt đầu đọc.
      </div>
      
      <div 
        v-else 
        class="article-content bg-gray-800 p-8 rounded-lg shadow-inner"
        :class="{ 'hide-furigana': !showFurigana }" 
      >
        <div v-html="selectedArticle.title" class="text-3xl font-bold mb-4 article-title"></div>
        
        <img 
          :src="selectedArticle.image_url" 
          alt="Article Image" 
          class="w-full h-auto object-cover rounded-lg mb-6"
          onerror="this.style.display='none'"
          referrerpolicy="no-referrer" >
        
        <div v-html="selectedArticle.content_html" class="text-xl leading-relaxed article-body"></div>
      </div>
    </main>
    
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from "vue";

// --- ĐỊNH NGHĨA CẤU TRÚC DỮ LIỆU ---
interface Article {
  // [SỬA LỖI] Dùng 'title'
  title: string; 
  image_url: string;
  content_html: string;
  url: string;
}

// --- STATE CỦA TRANG NÀY ---
const articles = ref<Article[]>([]);
const selectedArticle = ref<Article | null>(null);
const loading = ref(true);
const error = ref<string | null>(null);
const showFurigana = ref(true);

// --- (onMounted và các hàm khác giữ nguyên) ---

onMounted(async () => {
  try {
    const response = await fetch('/nhk_easy_news_raw_html.json');
    
    if (!response.ok) {
      throw new Error("Không thể tải file nhk_easy_news_raw_html.json. Hãy đặt nó vào thư mục /public.");
    }
    
    articles.value = await response.json();

    if (articles.value && articles.value.length > 0) {
      selectArticle(articles.value[0]);
    }
    
  } catch (e: any) {
    console.error(e);
    error.value = e.message;
  } finally {
    loading.value = false;
  }
});

const selectArticle = (article: Article) => {
  selectedArticle.value = article;
};
const toggleFurigana = () => {
  showFurigana.value = !showFurigana.value;
};
</script>

<style>
/* CSS (Không 'scoped')
  - Bên phải (Nền tối bg-gray-800) -> Chữ sáng
  - Bên trái (Nền trắng bg-white) -> Chữ tối
*/

/* === BÊN PHẢI (NỘI DUNG BÀI ĐỌC) === */

.article-body p,
.article-body span {
  color: #D1D5DB; /* Chữ sáng (Tailwind gray-300) */
}

.article-title h1,
.article-title span {
  color: #FFFFFF; /* Trắng */
}

.article-body ruby, 
.article-title ruby {
  ruby-position: over;
}
.article-body rt, 
.article-title rt {
  font-size: 0.6em;
  user-select: none;
  opacity: 0.7;
  color: #9CA3AF; /* Xám nhạt */
}
.article-body p {
  margin-bottom: 2em;
}

/* === BÊN TRÁI (DANH SÁCH) === */

.article-list-title h1,
.article-list-title span,
.article-list-title ruby {
  font-size: 1rem;
  font-weight: 600;
  line-height: 1.6;
  color: #111827 !important; /* Chữ đen */
}

.article-list-title rt {
  font-size: 0.5em;
  opacity: 0.8;
  color: #4B5563 !important; /* Chữ xám */
}

/* CSS ĐỂ ẨN FURIGANA */
.hide-furigana rt {
  display: none; 
}
.hide-furigana ruby {
  ruby-position: unset; 
}
</style>