<template>
  <nav class="flex flex-col space-y-1 text-gray-400 h-full">
    <!-- Main Section -->
    <!-- <div class="px-3 py-2 text-sm font-medium text-gray-400">Main Section</div> -->

    <NuxtLink to="/search" class="sidebar-link-style">
      <UIcon name="i-lucide-search" class="text-3xl" />
      <span>Tra cứu</span>
    </NuxtLink>

    <NuxtLink to="/alphabet" class="sidebar-link-style">
      <UIcon name="i-custom-alphabet" class="text-3xl" />
      <span>Bảng chữ cái</span>
    </NuxtLink>

    <NuxtLink to="/kanji" class="sidebar-link-style">
      <UIcon name="i-custom-kanji" class="text-3xl" />
      <span>Hán tự</span>
    </NuxtLink>

    <NuxtLink to="/word" class="sidebar-link-style">
      <UIcon name="i-custom-word" class="text-3xl" />
      <span>Từ vựng</span>
    </NuxtLink>
       <NuxtLink to="/explore" class="sidebar-link-style">
      <UIcon name="i-lucide-copy" class="text-3xl" />
      <span>Flashcard</span>
    </NuxtLink>
    <NuxtLink to="/ocr" class="sidebar-link-style">
      <UIcon name="i-lucide-scan-text" class="text-3xl" />
      <span>OCR</span>
    </NuxtLink>
    <NuxtLink to="/reading" class="sidebar-link-style">
      <UIcon name="i-lucide-newspaper" class="text-3xl" />
      <span>Luyện đọc</span>
    </NuxtLink>
    <!-- Settings -->
    <div class="px-3 py-2 text-sm font-medium text-gray-400 mt-4">Settings</div>
    <NuxtLink class="sidebar-link-style">
      <UIcon name="i-lucide-settings" />
      <span>Account</span>
    </NuxtLink>

    <NuxtLink to="premium" class="sidebar-link-style">
      <UIcon name="i-lucide-credit-card" />
      <span>Premium</span>
    </NuxtLink>
    
 
  </nav>
</template>
