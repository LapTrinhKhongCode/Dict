<template>
  <div
    v-if="data"
    :class="['max-w-6xl mx-auto p-4', compact ? 'text-sm' : 'text-base']"
  >
    <header class="flex items-center justify-between mb-6">
      <div class="flex items-center gap-4">
        <!-- <div
          class="rounded-full bg-slate-700 w-16 h-16 flex items-center justify-center text-2xl font-bold text-slate-100"
        >
          {{ kanji }}
        </div>
        <div>
          <h1 class="text-2xl font-bold text-slate-100">
            {{ kanji }} — Readings
          </h1>
          <p class="text-sm text-slate-400 mt-1">
            Onyomi &amp; Kunyomi with grouped example vocabulary
          </p>
        </div> -->
      </div>

      <div class="flex items-center gap-3">
      </div>
    </header>

    <nav class="mb-4 flex gap-2">
      <button
        @click="activeTab = 'kunyomi'"
        :class="[
          'px-3 py-2 rounded-full w-25',
          activeTab === 'kunyomi'
            ? 'bg-indigo-600/30 text-indigo-100'
            : 'bg-slate-800/40 text-slate-300',
        ]"
      >
        Kunyomi
      </button>
      <button
        @click="activeTab = 'onyomi'"
        :class="[
          'px-3 py-2 rounded-full w-25',
          activeTab === 'onyomi'
            ? 'bg-indigo-600/30 text-indigo-100'
            : 'bg-slate-800/40 text-slate-300',
        ]"
      >
        Onyomi
      </button>
      <button
        @click="activeTab = 'all'"
        :class="[
          'px-3 py-2 rounded-full w-25',
          activeTab === 'all'
            ? 'bg-indigo-600/30 text-indigo-100'
            : 'bg-slate-800/40 text-slate-300',
        ] "
      >
        Tất cả
      </button>
    </nav>

    <!-- Kunyomi -->
    <section v-if="activeTab === 'kunyomi' || activeTab === 'all'" class="mb-6">
  <div class="flex items-center justify-between mb-3">
    <h3 class="text-xl font-semibold text-slate-200">Kunyomi</h3>
    <span class="text-xs text-slate-400">
      {{ filtered.kunyomi.length }} reading{{ filtered.kunyomi.length !== 1 ? "s" : "" }}
    </span>
  </div>

  <div class="flex flex-col gap-5">
    <article
      v-for="r in filtered.kunyomi"
      :key="r.reading"
      class="bg-slate-800/60 border border-slate-700 rounded-2xl p-4"
    >
      <div class="flex items-start gap-4">
        <div class="flex-shrink-0">
          <div class="rounded-md ml-3 bg-slate-700/40 px-3 py-2 text-xl font-medium text-slate-100">
            {{ r.reading }}
          </div>
        </div>
        <div class="flex-1">
          <!-- <div class="text-sm text-slate-300 mb-2">
            読み: <span class="font-medium">{{ r.reading }}</span>
          </div> -->

          <div class="flex flex-col gap-4">
            <div
              v-if="r.examples && r.examples.length"
              v-for="(ex, i) in r.examples"
              :key="i"
              class="flex justify-between p-2 mr-5 rounded-md bg-slate-900/30 border border-slate-700"
            >
              <div class="flex flex-col gap-2">
                <div class="text-2xl font-semibold text-slate-100">
                  {{ ex.word }}
                </div>
                <div class="text-base text-slate-400">
                  {{ ex.phonetic }}
                </div>
              </div>
              <div class="text-lg text-slate-300 mt-1 text-right">
                <template v-for="(word, i) in ex.meaning.split(' ')" :key="i">
                  {{ word }}
                  <span v-if="(i + 1) % 7 === 0"><br /></span>
                </template>
              </div>
            </div>
            <div v-else class="text-xs text-slate-500 italic">
              Chưa có ví dụ
            </div>
          </div>
        </div>
      </div>
    </article>
  </div>
</section>


    <!-- Onyomi -->
    <section v-if="activeTab === 'onyomi' || activeTab === 'all'" class="mb-6">
      <div class="flex items-center justify-between mb-3">
        <h3 class="text-xl font-semibold text-slate-200">Onyomi</h3>
        <span class="text-xs text-slate-400">
          {{ filtered.onyomi.length }} reading{{
            filtered.onyomi.length !== 1 ? "s" : ""
          }}
        </span>
      </div>

      <div class="flex flex-col gap-5">
        <article
          v-for="r in filtered.onyomi"
          :key="r.reading"
          class="bg-slate-800/60 border border-slate-700 rounded-2xl p-4"
        >
          <div class="flex items-start gap-4">
            <div class="flex-shrink-0">
              <div
                class="rounded-md ml-3 bg-slate-700/40 px-3 py-2 text-xl font-medium text-slate-100"
              >
                {{ r.reading }}
              </div>
            </div>
            <div class="flex-1">
              <!-- <div class="text-sm text-slate-300 mb-2">
                読み: <span class="font-medium">{{ r.reading }}</span>
              </div> -->

              <div class="flex flex-col gap-4">
                <div
                  v-if="r.examples && r.examples.length"
                  v-for="(ex, i) in r.examples"
                  :key="i"
                  class="flex justify-between pl-3 pr-5 pt-4 pb-3 p-2 mr-5 rounded-md bg-slate-900/30 border border-slate-700"
                >
                  <div class="flex flex-col gap-2">
                    <div class="text-2xl font-semibold text-slate-100">
                      {{ ex.word }}
                    </div>
                    <div class="text-base text-slate-400">
                      {{ ex.phonetic }}
                    </div>
                  </div>
                  <div class="text-lg text-slate-300 mt-1 text-right">
                    <template
                      v-for="(word, i) in ex.meaning.split(' ')"
                      :key="i"
                    >
                      {{ word }}
                      <span v-if="(i + 1) % 7 === 0"><br /></span>
                    </template>
                  </div>
                </div>
                <div v-else class="text-xs text-slate-500 italic">
                  Chưa có ví dụ
                </div>
              </div>
            </div>
          </div>
        </article>
      </div>
    </section>
  </div>
  <div v-else class="text-slate-400 text-center mt-12">
    Không tìm thấy dữ liệu cho "{{ kanji }}"
  </div>
</template>

<script setup>
import { ref, computed } from "vue";
import { useRoute } from "vue-router";
import kanjiDict from "~/data/kanji_words.json"; 

const route = useRoute();
const kanji = route.params.kanji || "永"; // fallback nếu chưa có param

// lấy dữ liệu từ JSON
const data = kanjiDict[kanji] || null;

const query = ref("");
const showOnlyWithExamples = ref(false);
const activeTab = ref("kunyomi");
const compact = false;

function matchesQuery(r) {
  if (!query.value) return true;
  const q = query.value.toLowerCase();
  if (r.reading.toLowerCase().includes(q)) return true;
  if (r.examples) {
    for (const ex of r.examples) {
      if ((ex.word || "").toLowerCase().includes(q)) return true;
      if ((ex.reading || "").toLowerCase().includes(q)) return true;
      if ((ex.meaning || "").toLowerCase().includes(q)) return true;
    }
  }
  return false;
}

function normalizeReadings(obj) {
  if (!obj) return [];
  return Object.entries(obj).map(([reading, examples]) => ({
    reading,
    // lấy tối đa 6 ví dụ
    examples: (examples || []).slice(0, 6).map((ex) => ({
      word: ex.word,
      phonetic: ex.phonetic, // giữ đúng tên
      meaning: ex.meaning || "",
    })),
  }));
}

const filtered = computed(() => {
  const kunyomiArr = Array.isArray(data?.kunyomi)
    ? data.kunyomi
    : normalizeReadings(data?.kunyomi);

  const onyomiArr = Array.isArray(data?.onyomi)
    ? data.onyomi
    : normalizeReadings(data?.onyomi);

  return {
    kunyomi: kunyomiArr.filter(
      (r) =>
        matchesQuery(r) &&
        (!showOnlyWithExamples.value || (r.examples && r.examples.length))
    ),
    onyomi: onyomiArr.filter(
      (r) =>
        matchesQuery(r) &&
        (!showOnlyWithExamples.value || (r.examples && r.examples.length))
    ),
  };
});
</script>
