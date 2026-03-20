<template>
  <div class="flex h-full" style="max-height: calc(100vh - 64px)">
    <aside
      class="w-1/3 bg-white dark:bg-gray-800 p-4 overflow-y-auto shadow-lg text-gray-900 dark:text-gray-100 border-r border-gray-200 dark:border-gray-700"
    >
      <h2 class="text-xl font-bold mb-4 border-b border-gray-200 dark:border-gray-700 pb-2">
        Danh sách Bài báo
      </h2>

      <div v-if="loading" class="text-center text-gray-500 dark:text-gray-400">
        Đang tải...
      </div>
      <div v-if="error" class="text-red-500 dark:text-red-400">{{ error }}</div>

      <ul v-if="!loading && !error" class="space-y-2">
        <li
          v-for="article in articles"
          :key="article.url"
          @click="selectArticle(article)"
          class="p-3 rounded-lg cursor-pointer hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
          :class="{
            'bg-blue-100 dark:bg-blue-900/50':
              selectedArticle?.url === article.url,
          }"
        >
          <div v-html="article.title" class="article-list-title"></div>
        </li>
      </ul>
    </aside>

    <main class="w-2/3 p-6 overflow-y-auto bg-gray-50 dark:bg-gray-900">
      <div class="flex justify-end mb-4">
        <button
          @click="toggleFurigana"
          class="px-4 py-2 bg-primary-600 hover:bg-primary-700 dark:bg-blue-600 dark:hover:bg-blue-700 text-white rounded-lg transition-colors text-sm font-medium"
        >
          {{
            showFurigana
              ? "Tắt Furigana (ルビを隠す)"
              : "Bật Furigana (ルビを表示)"
          }}
        </button>
      </div>

      <div
        v-if="!selectedArticle"
        class="text-center text-gray-500 dark:text-gray-400 mt-10"
      >
        Hãy chọn một bài báo từ danh sách bên trái để bắt đầu đọc.
      </div>

      <div
        v-else
        class="article-content bg-white dark:bg-gray-800 p-8 rounded-lg shadow-lg border border-gray-200 dark:border-transparent"
        :class="{ 'hide-furigana': !showFurigana }"
      >
        <div
          v-html="selectedArticle.title"
          class="text-3xl font-bold mb-4 article-title"
        ></div>

        <img
          :src="selectedArticle.image_url"
          alt="Article Image"
          class="w-full h-auto object-cover rounded-lg mb-6"
          onerror="this.style.display='none'"
          referrerpolicy="no-referrer"
        />

        <div
          v-html="selectedArticle.content_html"
          class="text-xl leading-relaxed article-body"
        ></div>
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
    const response = await fetch("/nhk_easy_news_raw_html.json");

    if (!response.ok) {
      throw new Error(
        "Không thể tải file nhk_easy_news_raw_html.json. Hãy đặt nó vào thư mục /public."
      );
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
/* THAY ĐỔI:
  - Tái cấu trúc lại toàn bộ khối style
  - Thêm style cho light mode (mặc định)
  - Bọc style cho dark mode (cũ) trong class .dark
*/

/* === CÁC STYLE CHUNG (CHO CẢ 2 BÊN) === */
.article-body ruby,
.article-title ruby,
.article-list-title ruby {
  ruby-position: over;
}
.article-body rt,
.article-title rt,
.article-list-title rt {
  font-size: 0.6em;
  user-select: none;
  opacity: 0.7;
}
.article-body p {
  margin-bottom: 2em;
}

/* === BÊN PHẢI (NỘI DUNG BÀI ĐỌC) === */

/* Light Mode (Mặc định) */
.article-body p,
.article-body span {
  color: #374151; /* gray-700 */
}
.article-title h1,
.article-title span {
  color: #111827; /* gray-900 */
}
.article-body rt,
.article-title rt {
  color: #6b7280; /* gray-500 */
}

/* Dark Mode */
.dark .article-body p,
.dark .article-body span {
  color: #d1d5db; /* gray-300 */
}
.dark .article-title h1,
.dark .article-title span {
  color: #ffffff; /* White */
}
.dark .article-body rt,
.dark .article-title rt {
  color: #9ca3af; /* gray-400 */
}

/* === BÊN TRÁI (DANH SÁCH) === */

/* Light Mode (Mặc định - như code gốc của bạn) */
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
  color: #4b5563 !important; /* Chữ xám */
}

/* Dark Mode */
.dark .article-list-title h1,
.dark .article-list-title span,
.dark .article-list-title ruby {
  color: #e5e7eb !important; /* Chữ trắng-xám */
}
.dark .article-list-title rt {
  color: #9ca3af !important; /* Chữ xám nhạt */
}

/* === CSS ĐỂ ẨN FURIGANA (Giữ nguyên) === */
.hide-furigana rt {
  display: none;
}
.hide-furigana ruby {
  ruby-position: unset;
}
</style>