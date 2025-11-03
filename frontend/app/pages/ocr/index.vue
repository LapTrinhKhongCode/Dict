<template>
  <client-only>
    <OcrProcessor />

    <template #fallback>
      <div class.="text-center p-10 text-gray-400">Đang tải trình OCR...</div>
    </template>
  </client-only>
</template>

<script setup>
// Không cần import gì ở đây cả, giữ file này thật sạch
</script>
