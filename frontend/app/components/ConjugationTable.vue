<template>
  <div class="bg-white rounded-xl shadow-sm border border-gray-200 p-6 space-y-6">
    <!-- Header -->
    <div class="space-y-3">
      <h2 class="text-2xl font-bold text-gray-900 flex items-center space-x-2">
        <UIcon name="i-lucide-table" class="size-6" />
        <span>Verb Conjugation</span>
      </h2>
       <div class="flex items-center space-x-4">
         <h3 class="text-xl font-semibold text-blue-600">{{ root }}</h3>
         <span class="text-sm text-gray-500">Dictionary form</span>
         <div v-if="originalForm && originalForm !== root" class="flex items-center space-x-2">
           <span class="text-sm text-gray-400">←</span>
           <span class="text-sm text-gray-600 italic">from "{{ originalForm }}"</span>
         </div>
       </div>
    </div>

    <!-- Conjugation Table -->
    <div class="overflow-x-auto">
      <table class="w-full border-collapse">
        <thead>
          <tr class="bg-gray-50">
            <th class="border border-gray-200 px-4 py-3 text-left font-semibold text-gray-700">
              Form
            </th>
            <th class="border border-gray-200 px-4 py-3 text-left font-semibold text-gray-700">
              Reading
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

     <!-- Form Conversion Info -->
     <div v-if="originalForm && originalForm !== root" class="bg-green-50 border border-green-200 rounded-lg p-4">
       <div class="flex items-start space-x-2">
         <UIcon name="i-lucide-arrow-right-left" class="size-5 text-green-600 mt-0.5" />
         <div class="text-sm text-green-800">
           <p class="font-medium mb-1">Form Conversion:</p>
           <p class="text-xs">The searched form "{{ originalForm }}" was converted to its dictionary form "{{ root }}" to show the complete conjugation table.</p>
         </div>
       </div>
     </div>

     <!-- Additional Info -->
    <div class="bg-blue-50 border border-blue-200 rounded-lg p-4">
      <div class="flex items-start space-x-2">
        <UIcon name="i-lucide-info" class="size-5 text-blue-600 mt-0.5" />
        <div class="text-sm text-blue-800">
          <p class="font-medium mb-1">Conjugation Guide:</p>
          <ul class="space-y-1 text-xs">
            <li>• <strong>Dictionary (辞書):</strong> Base form of the verb</li>
            <li>• <strong>Past (た):</strong> Past tense form</li>
            <li>• <strong>Negative (未然):</strong> Negative form</li>
            <li>• <strong>Polite (丁寧):</strong> Polite form</li>
            <li>• <strong>Te (て):</strong> Te-form for connecting verbs</li>
            <li>• <strong>Potential (可能):</strong> Can do form</li>
            <li>• <strong>Passive (受身):</strong> Passive form</li>
            <li>• <strong>Causative (使役):</strong> Causative form</li>
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

