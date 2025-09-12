<template>
  <h3 class="flex justify-center text-3xl font-medium">BẢNG CHỮ CÁI</h3>
  <div class="p-6">
    <!-- Tabs -->
    <div class="flex mb-4 justify-center ">
      <button
        class="px-20 py-2 text-center font-medium transition"
        :class="activeTab === 'hiragana' ? 'bg-sky-900 text-white' : 'bg-sky-700 text-gray-200'"
        @click="activeTab = 'hiragana'"
      >
        Hiragana
      </button>
      <button
        class="px-20 py-2 text-center font-medium transition"
        :class="activeTab === 'katakana' ? 'bg-sky-900 text-white' : 'bg-sky-700 text-gray-200'"
        @click="activeTab = 'katakana'"
      >
        Katakana
      </button>
    </div>

    <p class="mb-3 text-center text-gray-700">Bấm để xem chi tiết về mỗi chữ</p>

    <!-- Hiển thị Hiragana -->
<div v-if="activeTab === 'hiragana'">
<div
  v-for="(row, rowIndex) in hiraganaGroups"
  :key="'hira-' + rowIndex"
  class="flex flex-wrap gap-4 mb-6 justify-between"
>
  <div
    v-for="(item, index) in row"
    :key="index"
    class="border rounded-lg h-28 flex flex-col items-center justify-center p-6 
           hover:bg-sky-800 cursor-pointer transition duration-200 ease-in-out shadow-md w-48"
    @click="selectedCharacter = { ...item, src: `/hiragana/${item.romaji}.png` }"
  >
    <span class="text-2xl sm:text-3xl md:text-=4xl lg:text-5xl font-japanese">{{ item.char }}</span>
    <span class="text-base sm:text-lg ">{{ item.romaji }}</span>
  </div>
</div>


</div>

<!-- Katakana -->
<div v-else>
  <div
    v-for="(row, rowIndex) in katakanaGroups"
    :key="'kata-' + rowIndex"
  class="flex flex-wrap gap-4 mb-6 justify-between"
  >
    <div
      v-for="(item, index) in row"
      :key="index"
      class="border rounded-lg h-28 flex flex-col items-center justify-center p-6 
           hover:bg-sky-800 cursor-pointer transition duration-200 ease-in-out shadow-md w-48"
      @click="selectedCharacter = { ...item, src: `/katakana/${item.romaji}.png` }"
    >
      <span class="text-2xl sm:text-3xl md:text-=4xl lg:text-5xl font-japanese">{{ item.char }}</span>
      <span class="text-base sm:text-lg ">{{ item.romaji }}</span>
    </div>
  </div>
</div>


    <!-- Popup -->
    <div
      v-if="selectedCharacter"
      class="fixed inset-0  bg-black/70 flex items-center justify-center p-6 z-50"
         @click="selectedCharacter = null"
    >
      <div class="relative w-full max-w-lg bg-transparent rounded-lg p-6 text-white text-center shadow-xl">
        <!-- <button
          @click="selectedCharacter = null"
          class="absolute top-3 right-3 text-gray-400 hover:text-white transition duration-200 ease-in-out focus:outline-none"
        >
          ✕
        </button> -->

        <div class="flex flex-col items-center justify-center pt-8 pb-4">
          <!-- <span class="text-7xl font-japanese mb-4 animate-fadeIn">{{ selectedCharacter.char }}</span> -->
          <img
            v-if="selectedCharacter.src"
            :src="selectedCharacter.src"
            b
            alt="Ảnh ký tự"
            class="w-90 h-90 object-contain mx-auto border rounded bg-white p-2 shadow"
          />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from "vue";
import Papa from "papaparse";

const activeTab = ref("hiragana");
const selectedCharacter = ref(null);
const hiraganaGroups = ref([]);
const katakanaGroups = ref([]);

onMounted(async () => {
  const res = await fetch("/kana_list.csv");
  const text = await res.text();
  const parsed = Papa.parse(text, { header: true });
  const rows = parsed.data.filter(r => r.romaji && r.hira && r.kata);

  // Nhóm kana theo hàng
  const groups = [
    ["a","i","u","e","o"],
    ["ka","ki","ku","ke","ko"],
    ["sa","shi","su","se","so"],
    ["ta","chi","tsu","te","to"],
    ["na","ni","nu","ne","no"],
    ["ha","hi","fu","he","ho"],
    ["ma","mi","mu","me","mo"],
    ["ya","yu","yo"],
    ["ra","ri","ru","re","ro"],
    ["wa","wo"],
    ["n"],

    ["ga","gi","gu","ge","go"],
    ["za","ji","zu","ze","zo"],
    ["da","de","do"],
    ["ba","bi","bu","be","bo"],
    ["pa","pi","pu","pe","po"],

    ["kya","kyu","kyo"],
    ["gya","gyu","gyo"],
    ["sha","shu","sho"],
    ["ja","ju","jo"],
    ["cha","chu","cho"],
    ["nya","nyu","nyo"],
    ["hya","hyu","hyo"],
    ["bya","byu","byo"],
    ["pya","pyu","pyo"],
    ["mya","myu","myo"],
    ["rya","ryu","ryo"]
  ];

  // Gom thành nhóm
  hiraganaGroups.value = groups.map(g =>
    g.map(r => {
      const row = rows.find(x => x.romaji === r);
      return { romaji: r, char: row ? row.hira : "" };
    })
  );

  katakanaGroups.value = groups.map(g =>
    g.map(r => {
      const row = rows.find(x => x.romaji === r);
      return { romaji: r, char: row ? row.kata : "" };
    })
  );
});
</script>

<style>
.font-japanese {
  font-family: "Noto Sans JP", sans-serif;
}

.animate-fadeIn {
  animation: fadeIn 0.5s ease-out;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(-10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}
</style>
