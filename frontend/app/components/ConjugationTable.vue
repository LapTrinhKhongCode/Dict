<template>
  <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 space-y-6">
    <div class="space-y-3">
      <h2 class="text-2xl font-bold text-gray-900 flex items-center space-x-2">
        <UIcon name="i-lucide-table" class="size-6" />
        <span>Chia động từ</span>
      </h2>
      <div class="flex items-center space-x-4">
        <h3 class="text-xl font-semibold text-blue-600">{{ root }}</h3>
        <span class="text-sm text-gray-500">Thể từ điển</span>
        <div v-if="originalForm && typeof originalForm === 'string' && originalForm !== root" class="flex items-center space-x-2">
          <span class="text-sm text-gray-400">←</span>
          <span class="text-sm text-gray-600 italic">từ "{{ originalForm }}"</span>
        </div>
      </div>
    </div>

    <div class="overflow-x-auto">
      <table class="w-full border-collapse">
        <thead>
          <tr class="bg-gray-50">
            <th class="border border-gray-200 px-4 py-3 text-left font-semibold text-gray-700">
              Thể
            </th>
            <th class="border border-gray-200 px-4 py-3 text-left font-semibold text-gray-700">
              Dạng chia
            </th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(conjugation, form) in conjugations" :key="form" class="hover:bg-gray-50">
            <td class="border border-gray-200 px-4 py-3 font-medium text-gray-800">
              {{ form }}
            </td>
            <td class="border border-gray-200 px-4 py-3 text-gray-900">
              {{ getJapaneseForm(conjugation) }}
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="originalForm && typeof originalForm === 'string' && originalForm !== root" class="bg-green-50 border border-green-200 rounded-lg p-4">
      <div class="flex items-start space-x-2">
        <UIcon name="i-lucide-arrow-right-left" class="size-5 text-green-600 mt-0.5" />
        <div class="text-sm text-green-800">
          <p class="font-medium mb-1">Chuyển đổi thể:</p>
          <p class="text-xs">Thể bạn tìm kiếm ("{{ originalForm }}") đã được chuyển đổi về thể từ điển ("{{ root }}") để hiển thị bảng chia động từ đầy đủ.</p>
        </div>
      </div>
    </div>

    <div class="bg-blue-50 border border-blue-200 rounded-lg p-4">
      <div class="flex items-start space-x-2">
        <UIcon name="i-lucide-info" class="size-5 text-blue-600 mt-0.5" />
        <div class="text-sm text-blue-800">
          <p class="font-medium mb-1">Hướng dẫn các thể:</p>
          <ul class="space-y-1 text-xs">
            <li>• <strong>Thể Từ điển (辞書):</strong> Thể cơ bản, nguyên mẫu</li>
            <li>• <strong>Thể Quá khứ (た):</strong> Dạng quá khứ (thường kết thúc bằng た/だ)</li>
            <li>• <strong>Thể Phủ định (未然):</strong> Dạng phủ định (thường kết thúc bằng ない)</li>
            <li>• <strong>Thể Lịch sự (丁寧):</strong> Dạng lịch sự (thường kết thúc bằng ます)</li>
            <li>• <strong>Thể Te (て):</strong> Dùng để nối câu (thường kết thúc bằng て/で)</li>
            <li>• <strong>Thể Khả năng (可能):</strong> Dạng 'có thể làm' (đuôi える/られる)</li>
            <li>• <strong>Thể Bị động (受身):</strong> Dạng bị động (đuôi れる/られる)</li>
            <li>• <strong>Thể Sai khiến (使役):</strong> Dạng sai khiến (đuôi せる/させる)</li>
          </ul>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
interface ConjugationData {
  [form: string]: string;
}

interface Props {
  root: string;
  conjugations: ConjugationData;
  originalForm?: string | null;
}

const props = defineProps<Props>();

// Extract Japanese form from conjugation string
const getJapaneseForm = (conjugation: string): string => {
  // Format: "食べる/たべるる" -> "食べる"
  const slashIndex = conjugation.indexOf('/');
  return slashIndex !== -1 ? conjugation.substring(0, slashIndex) : conjugation;
};
</script>