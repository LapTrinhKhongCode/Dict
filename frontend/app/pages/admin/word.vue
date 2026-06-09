<template>
  <ClientOnly>
    <div class="min-h-screen bg-gray-50 dark:bg-gray-900 text-gray-900 dark:text-white p-4 sm:p-8">
      <div class="max-w-6xl mx-auto">

        <!-- HEADER -->
        <div class="flex items-center gap-3 mb-6">
          <NuxtLink to="/admin" class="p-2 rounded-full bg-gray-200 dark:bg-gray-800 hover:bg-gray-300 dark:hover:bg-gray-700">
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18"/>
            </svg>
          </NuxtLink>
          <div>
            <h1 class="text-2xl font-bold">Quản lý Từ điển</h1>
            <p class="text-sm text-gray-500">CRUD bảng con (Word, Sense, Gloss, Example, Kanji...) rồi rebuild JSON</p>
          </div>
          <div class="ml-auto flex gap-2">
            <button @click="reloadTrie()" :disabled="trieReloading"
              class="px-4 py-2 bg-indigo-500 hover:bg-indigo-600 text-white text-sm font-medium rounded-xl disabled:opacity-40">
              {{ trieReloading ? '⏳ Đang reload...' : '🌲 Reload Trie' }}
            </button>
            <button @click="showBatchModal = true" class="px-4 py-2 bg-amber-500 hover:bg-amber-600 text-white text-sm font-medium rounded-xl">
              🔄 Rebuild Batch
            </button>
          </div>
        </div>

        <!-- TOOLBAR -->
        <div class="flex gap-3 mb-4 flex-wrap">
          <div class="relative flex-1 min-w-[200px] max-w-sm">
            <input v-model="searchInput" @keyup.enter="doSearch" type="text" placeholder="Tìm theo Label hoặc nghĩa..."
              class="w-full pl-9 pr-3 py-2 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500"/>
            <svg class="absolute left-2.5 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"/>
            </svg>
          </div>
          <select v-model="typeFilter" @change="doSearch" class="px-3 py-2 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-xl">
            <option value="">Tất cả loại</option>
            <option value="word">word</option>
            <option value="kanji">kanji</option>
          </select>
          <!-- Kanji filters — chỉ hiện khi type=kanji -->
          <template v-if="typeFilter === 'kanji'">
            <select v-model="jlptFilter" @change="doSearch" class="px-3 py-2 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-xl">
              <option value="">Tất cả JLPT</option>
              <option value="N1">N1</option>
              <option value="N2">N2</option>
              <option value="N3">N3</option>
              <option value="N4">N4</option>
              <option value="N5">N5</option>
            </select>
            <input v-model.number="strokeMinFilter" @change="doSearch" type="number" min="1" max="30" placeholder="Nét ≥"
              class="w-24 px-3 py-2 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500"/>
            <input v-model.number="strokeMaxFilter" @change="doSearch" type="number" min="1" max="30" placeholder="Nét ≤"
              class="w-24 px-3 py-2 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-xl focus:outline-none focus:ring-2 focus:ring-blue-500"/>
          </template>
          <button @click="doSearch" class="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-xl">Tìm kiếm</button>
          <button @click="clearSearch" v-if="appliedSearch || typeFilter || jlptFilter || strokeMinFilter || strokeMaxFilter" class="px-3 py-2 bg-gray-200 dark:bg-gray-700 text-sm rounded-xl">Xoá lọc</button>
        </div>

        <div class="text-sm text-gray-500 mb-3">
          Tổng: <b>{{ totalCount.toLocaleString() }}</b> từ
          <span v-if="appliedSearch"> — Lọc: "<b class="text-blue-600">{{ appliedSearch }}</b>"</span>
        </div>

        <!-- TABLE -->
        <div class="bg-white dark:bg-gray-800 rounded-2xl border border-gray-200 dark:border-gray-700 overflow-hidden">
          <div v-if="loading" class="p-8 text-center text-gray-400">Đang tải...</div>
          <table v-else class="w-full text-sm">
            <thead class="bg-gray-50 dark:bg-gray-700/50 text-xs uppercase text-gray-500">
              <tr>
                <th class="px-4 py-3 text-left">Label</th>
                <th class="px-4 py-3 text-left">Loại</th>
                <th class="px-4 py-3 text-left">Phonetic</th>
                <th class="px-4 py-3 text-left">Nghĩa ngắn / Meaning</th>
                <th v-if="typeFilter==='kanji'" class="px-4 py-3 text-center">JLPT</th>
                <th v-if="typeFilter==='kanji'" class="px-4 py-3 text-center">Nét</th>
                <th class="px-4 py-3 text-center">Thao tác</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-gray-100 dark:divide-gray-700">
              <tr v-for="entry in entries" :key="entry.id" class="hover:bg-gray-50 dark:hover:bg-gray-700/30">
                <td class="px-4 py-3 font-medium font-japanese">{{ entry.label }}</td>
                <td class="px-4 py-3">
                  <span :class="entry.type === 'kanji' ? 'bg-purple-100 text-purple-700 dark:bg-purple-900/50 dark:text-purple-300' : 'bg-blue-100 text-blue-700 dark:bg-blue-900/50 dark:text-blue-300'"
                    class="px-2 py-0.5 rounded text-xs font-medium">{{ entry.type }}</span>
                </td>
                <td class="px-4 py-3 text-gray-500 font-japanese">{{ entry.phonetic }}</td>
                <td class="px-4 py-3 text-gray-500 truncate max-w-xs">
                  {{ entry.type === 'kanji' ? entry.meaning : entry.shortMean }}
                </td>
                <td v-if="typeFilter==='kanji'" class="px-4 py-3 text-center">
                  <span v-if="entry.jlptLevel" class="px-2 py-0.5 rounded text-xs font-medium bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-300">{{ entry.jlptLevel }}</span>
                  <span v-else class="text-gray-300">—</span>
                </td>
                <td v-if="typeFilter==='kanji'" class="px-4 py-3 text-center text-gray-500 text-xs">
                  {{ entry.strokeCount ?? '—' }}
                </td>
                <td class="px-4 py-3 text-center whitespace-nowrap">
                  <button @click="openEditor(entry)" class="text-blue-600 hover:underline text-xs mr-3">✏️ Sửa</button>
                  <button @click="rebuildOne(entry)" :disabled="rebuildingId === entry.id"
                    class="text-green-600 hover:underline text-xs disabled:opacity-40">
                    {{ rebuildingId === entry.id ? '...' : '🔄 Rebuild' }}
                  </button>
                </td>
              </tr>
              <tr v-if="!entries.length">
                <td :colspan="typeFilter==='kanji' ? 7 : 5" class="px-4 py-8 text-center text-gray-400">Không có dữ liệu</td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- PAGINATION -->
        <div class="flex items-center justify-between mt-4 text-sm">
          <span class="text-gray-500">Trang {{ currentPage }} / {{ totalPages }}</span>
          <div class="flex gap-2">
            <button @click="goToPage(currentPage-1)" :disabled="currentPage===1"
              class="px-3 py-1 rounded bg-gray-200 dark:bg-gray-700 disabled:opacity-40">←</button>
            <button @click="goToPage(currentPage+1)" :disabled="currentPage>=totalPages"
              class="px-3 py-1 rounded bg-gray-200 dark:bg-gray-700 disabled:opacity-40">→</button>
          </div>
        </div>

        <!-- Toast -->
        <Transition name="fade">
          <div v-if="toast" class="fixed bottom-6 right-6 z-50 px-5 py-3 rounded-xl shadow-lg text-sm font-medium"
            :class="toast.ok ? 'bg-green-600 text-white' : 'bg-red-500 text-white'">
            {{ toast.msg }}
          </div>
        </Transition>
      </div>
    </div>

    <!-- ======= EDITOR MODAL ======= -->
    <Teleport to="body">
      <div v-if="editorOpen" class="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4" @click.self="editorOpen=false">
        <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-2xl w-full max-w-4xl max-h-[92vh] flex flex-col">

          <!-- Modal header -->
          <div class="flex items-center justify-between px-6 py-4 border-b border-gray-200 dark:border-gray-700 flex-shrink-0">
            <div class="flex items-center gap-3">
              <h2 class="text-lg font-bold font-japanese">{{ editorEntry?.label }}</h2>
              <span :class="editorEntry?.type === 'kanji' ? 'bg-purple-100 text-purple-700' : 'bg-blue-100 text-blue-700'"
                class="px-2 py-0.5 rounded text-xs font-medium">{{ editorEntry?.type }}</span>
            </div>
            <div class="flex gap-2">
              <button @click="rebuildOne(editorEntry, true)" :disabled="rebuildingId === editorEntry?.id"
                class="px-4 py-1.5 bg-green-600 hover:bg-green-700 text-white text-sm rounded-lg disabled:opacity-40">
                {{ rebuildingId === editorEntry?.id ? 'Đang rebuild...' : '🔄 Rebuild JSON' }}
              </button>
              <button @click="editorOpen=false" class="p-1.5 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-700 text-lg leading-none">✕</button>
            </div>
          </div>

          <!-- Tabs -->
          <div class="flex gap-1 px-6 pt-3 flex-shrink-0 border-b border-gray-200 dark:border-gray-700">
            <button v-for="tab in editorTabs" :key="tab.id" @click="editorTab=tab.id"
              :class="editorTab===tab.id ? 'border-b-2 border-blue-600 text-blue-600 font-medium' : 'text-gray-500 hover:text-gray-700 dark:hover:text-gray-300'"
              class="px-3 pb-2 text-sm transition-colors">{{ tab.label }}</button>
          </div>

          <!-- Modal body -->
          <div v-if="editorLoading" class="p-8 text-center text-gray-400 flex-1">Đang tải...</div>
          <div v-else class="overflow-y-auto p-6 space-y-6 flex-1">

            <!-- TAB: WORD INFO -->
            <div v-show="editorTab==='word'">
              <div v-for="word in detail.words" :key="word.id" class="p-4 bg-gray-50 dark:bg-gray-700/40 rounded-xl mb-4">
                <div class="text-xs font-semibold text-gray-400 uppercase mb-3">Word #{{ word.id }}</div>
                <div class="grid grid-cols-2 gap-3">
                  <div>
                    <label class="block text-xs text-gray-500 mb-1">WordText (chữ viết)</label>
                    <input v-model="word.wordText" class="w-full px-2 py-1.5 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 font-japanese"/>
                  </div>
                  <div>
                    <label class="block text-xs text-gray-500 mb-1">Phonetic (kana)</label>
                    <input v-model="word.phonetic" class="w-full px-2 py-1.5 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 font-japanese"/>
                  </div>
                  <div>
                    <label class="block text-xs text-gray-500 mb-1">Romaji</label>
                    <input v-model="word.romaji" class="w-full px-2 py-1.5 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"/>
                  </div>
                  <div>
                    <label class="block text-xs text-gray-500 mb-1">ShortMean (nghĩa ngắn)</label>
                    <input v-model="word.shortMean" class="w-full px-2 py-1.5 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"/>
                  </div>
                  <div>
                    <label class="block text-xs text-gray-500 mb-1">Weight (độ ưu tiên)</label>
                    <input v-model.number="word.weight" type="number" class="w-full px-2 py-1.5 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"/>
                  </div>
                  <div class="flex items-end">
                    <button @click="saveWord(word)" :disabled="savingId==='w'+word.id"
                      class="w-full px-3 py-1.5 bg-blue-600 hover:bg-blue-700 text-white text-xs rounded-lg disabled:opacity-40">
                      {{ savingId==='w'+word.id ? '...' : '💾 Lưu Word' }}
                    </button>
                  </div>
                </div>
                <!-- Opposite words -->
                <div v-if="word.opposites?.length" class="mt-3 pt-3 border-t border-gray-200 dark:border-gray-600">
                  <div class="text-xs text-gray-500 mb-2">Từ trái nghĩa (opposite_word):</div>
                  <div class="flex flex-wrap gap-2">
                    <span v-for="opp in word.opposites" :key="opp.id"
                      class="px-2 py-1 bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-300 text-xs rounded-full font-japanese">
                      {{ opp.relatedWordText }}
                    </span>
                  </div>
                </div>
              </div>
              <div v-if="!detail.words?.length" class="text-sm text-gray-400 italic text-center py-4">Không có Word nào.</div>
            </div>

            <!-- TAB: SENSES & NGHĨA -->
            <div v-show="editorTab==='senses'">
              <div v-for="sense in detail.senses" :key="sense.id" class="p-4 bg-gray-50 dark:bg-gray-700/40 rounded-xl mb-4">
                <!-- Sense header -->
                <div class="flex gap-3 items-end mb-3">
                  <div class="flex-1">
                    <label class="block text-xs text-gray-500 mb-1">Pos (loại từ: noun, verb, adj...)</label>
                    <input v-model="sense.pos" class="w-full px-2 py-1.5 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"/>
                  </div>
                  <div class="w-20">
                    <label class="block text-xs text-gray-500 mb-1">Order</label>
                    <input v-model.number="sense.senseOrder" type="number" class="w-full px-2 py-1.5 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"/>
                  </div>
                  <button @click="saveSense(sense)" :disabled="savingId==='s'+sense.id"
                    class="px-3 py-1.5 bg-blue-600 hover:bg-blue-700 text-white text-xs rounded-lg disabled:opacity-40 whitespace-nowrap">
                    {{ savingId==='s'+sense.id ? '...' : '�� Lưu Sense' }}
                  </button>
                </div>

                <!-- Glosses (nghĩa) -->
                <div class="pl-3 border-l-2 border-blue-300 dark:border-blue-700 mb-3">
                  <div class="flex items-center justify-between mb-2">
                    <div class="text-xs text-gray-400 font-semibold">Glosses — nghĩa (mean)</div>
                    <button @click="addGloss(sense)" :disabled="savingId==='addg'+sense.id"
                      class="px-2 py-0.5 bg-blue-100 hover:bg-blue-200 dark:bg-blue-900/40 dark:hover:bg-blue-900/70 text-blue-700 dark:text-blue-300 text-xs rounded-lg disabled:opacity-40">
                      + Thêm Gloss
                    </button>
                  </div>
                  <div v-for="gloss in sense.glosses" :key="gloss.id" class="flex gap-2 mb-2">
                    <input v-model="gloss.text" class="flex-1 px-2 py-1.5 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500" placeholder="Nghĩa..."/>
                    <button @click="saveGloss(gloss)" :disabled="savingId==='g'+gloss.id"
                      class="px-3 py-1.5 bg-blue-600 hover:bg-blue-700 text-white text-xs rounded-lg disabled:opacity-40">
                      {{ savingId==='g'+gloss.id ? '...' : '💾' }}
                    </button>
                  </div>
                  <div v-if="!sense.glosses?.length" class="text-xs text-gray-400 italic">Không có gloss.</div>
                </div>

                <!-- Examples (ví dụ) -->
                <div class="pl-3 border-l-2 border-green-300 dark:border-green-700">
                  <div class="flex items-center justify-between mb-2">
                    <div class="text-xs text-gray-400 font-semibold">Examples — ví dụ (examples[])</div>
                    <button @click="addExample(sense)" :disabled="savingId==='addex'+sense.id"
                      class="px-2 py-0.5 bg-green-100 hover:bg-green-200 dark:bg-green-900/40 dark:hover:bg-green-900/70 text-green-700 dark:text-green-300 text-xs rounded-lg disabled:opacity-40">
                      + Thêm Example
                    </button>
                  </div>
                  <div v-for="ex in sense.examples" :key="ex.id" class="mb-3 p-3 bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-600">
                    <div class="grid grid-cols-1 gap-2">
                      <div>
                        <label class="block text-xs text-gray-400 mb-1">Câu JP (content)</label>
                        <input v-model="ex.contentJp" class="w-full px-2 py-1.5 text-sm bg-gray-50 dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-green-500 font-japanese"/>
                      </div>
                      <div>
                        <label class="block text-xs text-gray-400 mb-1">Dịch (mean)</label>
                        <input v-model="ex.contentTranslated" class="w-full px-2 py-1.5 text-sm bg-gray-50 dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-green-500"/>
                      </div>
                      <div class="flex gap-2">
                        <div class="flex-1">
                          <label class="block text-xs text-gray-400 mb-1">Phiên âm (transcription)</label>
                          <input v-model="ex.transcription" class="w-full px-2 py-1.5 text-sm bg-gray-50 dark:bg-gray-700 border border-gray-200 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-green-500"/>
                        </div>
                        <div class="flex items-end">
                          <button @click="saveExample(ex)" :disabled="savingId==='ex'+ex.id"
                            class="px-3 py-1.5 bg-green-600 hover:bg-green-700 text-white text-xs rounded-lg disabled:opacity-40">
                            {{ savingId==='ex'+ex.id ? '...' : '💾' }}
                          </button>
                        </div>
                      </div>
                    </div>
                  </div>
                  <div v-if="!sense.examples?.length" class="text-xs text-gray-400 italic">Không có ví dụ.</div>
                </div>
              </div>
              <div v-if="!detail.senses?.length" class="text-sm text-gray-400 italic text-center py-4">Không có Sense nào.</div>
              <!-- Thêm Sense mới -->
              <button @click="addSense()" :disabled="savingId==='addsense'"
                class="mt-2 w-full py-2 border-2 border-dashed border-gray-300 dark:border-gray-600 text-gray-500 dark:text-gray-400 text-sm rounded-xl hover:border-blue-400 hover:text-blue-600 dark:hover:text-blue-400 transition-colors disabled:opacity-40">
                + Thêm Sense mới
              </button>
            </div>

            <!-- TAB: KANJI (chỉ hiện khi type=kanji) -->
            <div v-show="editorTab==='kanji'">
              <!-- Reading Elements (On/Kun) -->
              <section class="mb-6">
                <div class="flex items-center justify-between mb-3">
                  <h3 class="text-sm font-semibold uppercase text-gray-500">ReadingElements — Âm đọc (on/kun)</h3>
                  <button @click="addReadingElement()" :disabled="savingId==='addre'"
                    class="px-2 py-0.5 bg-purple-100 hover:bg-purple-200 dark:bg-purple-900/40 text-purple-700 dark:text-purple-300 text-xs rounded-lg disabled:opacity-40">
                    + Thêm
                  </button>
                </div>
                <div v-for="re in detail.readingElements" :key="re.id" class="flex gap-2 mb-2 items-center">
                  <div class="flex-1">
                    <input v-model="re.reb" class="w-full px-2 py-1.5 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-purple-500 font-japanese" placeholder="Âm đọc (VD: カン、くだ.り)"/>
                  </div>
                  <div class="w-32">
                    <input v-model="re.reNoKanji" class="w-full px-2 py-1.5 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-purple-500" placeholder="ReNoKanji"/>
                  </div>
                  <button @click="saveReadingElement(re)" :disabled="savingId==='re'+re.id"
                    class="px-3 py-1.5 bg-purple-600 hover:bg-purple-700 text-white text-xs rounded-lg disabled:opacity-40">
                    {{ savingId==='re'+re.id ? '...' : '💾' }}
                  </button>
                </div>
                <div v-if="!detail.readingElements?.length" class="text-sm text-gray-400 italic">Không có ReadingElement.</div>
              </section>

              <!-- Kanji base info -->
              <section v-if="detail.kanji" class="mb-6">
                <h3 class="text-sm font-semibold uppercase text-gray-500 mb-3">Kanji — Thông tin cơ bản</h3>
                <div class="p-4 bg-gray-50 dark:bg-gray-700/40 rounded-xl">
                  <div class="grid grid-cols-2 gap-3">
                    <div>
                      <label class="block text-xs text-gray-500 mb-1">Nghĩa HanViet (mean)</label>
                      <input v-model="detail.kanji.meaning" class="w-full px-2 py-1.5 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-purple-500"/>
                    </div>
                    <div>
                      <label class="block text-xs text-gray-500 mb-1">Cấp JLPT (level)</label>
                      <input v-model="detail.kanji.jlptLevel" class="w-full px-2 py-1.5 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-purple-500" placeholder="N1, N2, N3..."/>
                    </div>
                    <div>
                      <label class="block text-xs text-gray-500 mb-1">Số nét (stroke_count)</label>
                      <input v-model.number="detail.kanji.strokeCount" type="number" class="w-full px-2 py-1.5 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-purple-500"/>
                    </div>
                    <div>
                      <label class="block text-xs text-gray-500 mb-1">Độ phổ biến (freq)</label>
                      <input v-model.number="detail.kanji.freq" type="number" class="w-full px-2 py-1.5 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-purple-500"/>
                    </div>
                  </div>
                  <button @click="saveKanji()" :disabled="savingId==='kanji'" class="mt-3 px-4 py-1.5 bg-purple-600 hover:bg-purple-700 text-white text-xs rounded-lg disabled:opacity-40">
                    {{ savingId==='kanji' ? '...' : '💾 Lưu Kanji' }}
                  </button>
                </div>
              </section>

              <!-- Senses trong kanji (HanViet, Detail, Tips) -->
              <section class="mb-6">
                <h3 class="text-sm font-semibold uppercase text-gray-500 mb-3">Senses — HanViet / Detail / Tips</h3>
                <div v-for="sense in detail.senses" :key="sense.id" class="p-3 bg-gray-50 dark:bg-gray-700/40 rounded-xl mb-2 flex gap-3 items-center">
                  <div class="w-28 shrink-0">
                    <label class="block text-xs text-gray-400 mb-1">Pos (loại)</label>
                    <input v-model="sense.pos" class="w-full px-2 py-1 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-purple-500" placeholder="HanViet/Detail/Tips"/>
                  </div>
                  <div class="flex-1">
                    <label class="block text-xs text-gray-400 mb-1">Nội dung (Gloss text)</label>
                    <input v-if="sense.glosses?.[0]" v-model="sense.glosses[0].text" class="w-full px-2 py-1 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-purple-500"/>
                    <span v-else class="text-xs text-gray-400 italic">Chưa có gloss</span>
                  </div>
                  <div class="flex gap-1 shrink-0">
                    <button @click="saveSense(sense)" :disabled="savingId==='s'+sense.id" class="px-2 py-1 bg-purple-600 hover:bg-purple-700 text-white text-xs rounded-lg disabled:opacity-40">Sense</button>
                    <button v-if="sense.glosses?.[0]" @click="saveGloss(sense.glosses[0])" :disabled="savingId==='g'+sense.glosses[0].id" class="px-2 py-1 bg-blue-600 hover:bg-blue-700 text-white text-xs rounded-lg disabled:opacity-40">Gloss</button>
                  </div>
                </div>
                <div v-if="!detail.senses?.length" class="text-sm text-gray-400 italic">Không có Sense.</div>
              </section>

              <!-- KanjiExamples -->
              <section v-if="detail.kanji">
                <div class="flex items-center justify-between mb-3">
                  <h3 class="text-sm font-semibold uppercase text-gray-500">KanjiExamples — Ví dụ (examples)</h3>
                  <button @click="addKanjiExample()" :disabled="savingId==='addkex'"
                    class="px-2 py-0.5 bg-blue-100 hover:bg-blue-200 dark:bg-blue-900/40 text-blue-700 dark:text-blue-300 text-xs rounded-lg disabled:opacity-40">
                    + Thêm Example
                  </button>
                </div>
                <div v-for="ex in detail.kanji.examples" :key="ex.id" class="p-3 bg-gray-50 dark:bg-gray-700/40 rounded-xl mb-3">
                  <div class="grid grid-cols-3 gap-2 mb-2">
                    <div>
                      <label class="block text-xs text-gray-400 mb-1">Type (on/kun)</label>
                      <input v-model="ex.exampleType" class="w-full px-2 py-1 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"/>
                    </div>
                    <div>
                      <label class="block text-xs text-gray-400 mb-1">ReadingGroup</label>
                      <input v-model="ex.readingGroup" class="w-full px-2 py-1 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 font-japanese"/>
                    </div>
                    <div>
                      <label class="block text-xs text-gray-400 mb-1">Word (w)</label>
                      <input v-model="ex.word" class="w-full px-2 py-1 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 font-japanese"/>
                    </div>
                    <div>
                      <label class="block text-xs text-gray-400 mb-1">Meaning (m)</label>
                      <input v-model="ex.meaning" class="w-full px-2 py-1 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"/>
                    </div>
                    <div>
                      <label class="block text-xs text-gray-400 mb-1">Reading (p — kana)</label>
                      <input v-model="ex.reading" class="w-full px-2 py-1 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 font-japanese"/>
                    </div>
                    <div>
                      <label class="block text-xs text-gray-400 mb-1">HanViet (h)</label>
                      <input v-model="ex.hanViet" class="w-full px-2 py-1 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500"/>
                    </div>
                  </div>
                  <button @click="saveKanjiExample(ex)" :disabled="savingId==='kex'+ex.id"
                    class="px-3 py-1 bg-blue-600 hover:bg-blue-700 text-white text-xs rounded-lg disabled:opacity-40">
                    {{ savingId==='kex'+ex.id ? '...' : '💾 Lưu ví dụ' }}
                  </button>
                </div>
                <div v-if="!detail.kanji.examples?.length" class="text-sm text-gray-400 italic">Không có KanjiExample.</div>
              </section>
            </div>

          </div>
        </div>
      </div>

      <!-- BATCH REBUILD MODAL -->
      <div v-if="showBatchModal" class="fixed inset-0 bg-black/50 z-50 flex items-center justify-center p-4" @click.self="showBatchModal=false">
        <div class="bg-white dark:bg-gray-800 rounded-2xl shadow-2xl w-full max-w-sm p-6">
          <h2 class="text-lg font-bold mb-4">Rebuild Batch</h2>
          <div class="space-y-3">
            <div>
              <label class="block text-sm text-gray-600 dark:text-gray-300 mb-1">Loại từ</label>
              <select v-model="batchType" class="w-full px-3 py-2 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-xl">
                <option value="word">word</option>
                <option value="kanji">kanji</option>
              </select>
            </div>
            <div>
              <label class="block text-sm text-gray-600 dark:text-gray-300 mb-1">Số lượng tối đa</label>
              <input v-model.number="batchLimit" type="number" min="1" max="500"
                class="w-full px-3 py-2 text-sm bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-xl"/>
            </div>
          </div>
          <div v-if="batchResult" class="mt-4 p-3 bg-green-50 dark:bg-green-900/30 rounded-lg text-sm">
            ✅ {{ batchResult }}
          </div>
          <div class="flex gap-3 mt-5">
            <button @click="runBatch" :disabled="batchRunning"
              class="flex-1 py-2 bg-amber-500 hover:bg-amber-600 text-white text-sm font-medium rounded-xl disabled:opacity-40">
              {{ batchRunning ? 'Đang chạy...' : '🔄 Chạy Rebuild' }}
            </button>
            <button @click="showBatchModal=false; batchResult=''" class="px-4 py-2 bg-gray-200 dark:bg-gray-700 text-sm rounded-xl">Đóng</button>
          </div>
        </div>
      </div>
    </Teleport>
  </ClientOnly>
</template>

<script setup lang="ts">
definePageMeta({ layout: 'admin' })

const config = useRuntimeConfig()
const base = config.public.apiBaseUrl

interface EntryRow { id: number; label: string; type: string; phonetic?: string; shortMean?: string; meaning?: string; strokeCount?: number; jlptLevel?: string; freq?: number; grade?: number }

interface WordRelationDto { id: number; relatedWordId: number; relatedWordText?: string; relationType: string }
interface WordDto { id: number; wordText?: string; phonetic?: string; romaji?: string; shortMean?: string; weight?: number; opposites: WordRelationDto[] }
interface GlossDto { id: number; text?: string }
interface ExampleDto { id: number; contentJp?: string; contentTranslated?: string; transcription?: string }
interface SenseDto { id: number; pos?: string; senseOrder?: number; glosses: GlossDto[]; examples: ExampleDto[] }
interface ReadingElementDto { id: number; reb?: string; reNoKanji?: string; rePri?: string }
interface KanjiExampleDto { id: number; exampleType?: string; readingGroup?: string; word?: string; meaning?: string; reading?: string; hanViet?: string }
interface KanjiDto { id: number; character?: string; strokeCount?: number; jlptLevel?: string; meaning?: string; freq?: number; examples: KanjiExampleDto[] }
interface DetailDto { id: number; label: string; type: string; words: WordDto[]; senses: SenseDto[]; kanji?: KanjiDto; readingElements: ReadingElementDto[] }

// --- List state ---
const entries = ref<EntryRow[]>([])
const totalCount = ref(0)
const pageSize = 20
const currentPage = ref(1)
const searchInput = ref('')
const appliedSearch = ref('')
const typeFilter = ref('')
const jlptFilter = ref('')
const strokeMinFilter = ref<number | null>(null)
const strokeMaxFilter = ref<number | null>(null)
const loading = ref(false)

const totalPages = computed(() => Math.max(1, Math.ceil(totalCount.value / pageSize)))

const toast = ref<{ ok: boolean; msg: string } | null>(null)
function showToast(msg: string, ok = true) {
  toast.value = { ok, msg }
  setTimeout(() => toast.value = null, 3000)
}

function getToken() {
  return localStorage.getItem('jwt_token') || ''
}

async function fetchEntries() {
  loading.value = true
  try {
    const params = new URLSearchParams({
      page: String(currentPage.value),
      pageSize: String(pageSize),
      ...(appliedSearch.value ? { search: appliedSearch.value } : {}),
      ...(typeFilter.value ? { type: typeFilter.value } : {}),
      ...(jlptFilter.value ? { jlptLevel: jlptFilter.value } : {}),
      ...(strokeMinFilter.value !== null ? { strokeMin: String(strokeMinFilter.value) } : {}),
      ...(strokeMaxFilter.value !== null ? { strokeMax: String(strokeMaxFilter.value) } : {}),
    })
    const res = await $fetch<any>(`${base}/api/admin/entries?${params}`, {
      headers: { Authorization: `Bearer ${getToken()}` }
    })
    entries.value = res.result?.items ?? []
    totalCount.value = res.result?.totalCount ?? 0
  } catch (e) {
    console.error('[admin/word] fetchEntries error:', e)
    showToast('Lỗi tải dữ liệu', false)
  } finally {
    loading.value = false
  }
}

function doSearch() {
  appliedSearch.value = searchInput.value.trim()
  currentPage.value = 1
  fetchEntries()
}
function clearSearch() {
  searchInput.value = ''
  appliedSearch.value = ''
  typeFilter.value = ''
  jlptFilter.value = ''
  strokeMinFilter.value = null
  strokeMaxFilter.value = null
  currentPage.value = 1
  fetchEntries()
}
function goToPage(p: number) {
  if (p < 1 || p > totalPages.value) return
  currentPage.value = p
  fetchEntries()
}

// --- Rebuild single ---
const rebuildingId = ref<number | null>(null)
async function rebuildOne(entry: EntryRow | null | undefined, fromEditor = false) {
  if (!entry) return
  rebuildingId.value = entry.id
  try {
    const res = await $fetch<any>(`${base}/api/admin/entries/${entry.id}/rebuild`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${getToken()}` }
    })
    showToast(res.message ?? `Rebuild "${entry.label}" thành công`)
  } catch {
    showToast('Rebuild thất bại', false)
  } finally {
    rebuildingId.value = null
  }
}

// --- Editor modal ---
const editorOpen = ref(false)
const editorEntry = ref<EntryRow | null>(null)
const editorLoading = ref(false)
const editorTab = ref('word')
const detail = ref<DetailDto>({ id: 0, label: '', type: 'word', words: [], senses: [], readingElements: [] })

const editorTabs = computed(() => {
  const tabs = [{ id: 'word', label: '📝 Word' }, { id: 'senses', label: '📖 Senses & Examples' }]
  if (detail.value.type === 'kanji') tabs.push({ id: 'kanji', label: '漢 Kanji' })
  return tabs
})

async function openEditor(entry: EntryRow) {
  editorEntry.value = entry
  editorTab.value = entry.type === 'kanji' ? 'kanji' : 'word'
  editorOpen.value = true
  editorLoading.value = true
  try {
    const res = await $fetch<any>(`${base}/api/admin/entries/${entry.id}/detail`, {
      headers: { Authorization: `Bearer ${getToken()}` }
    })
    detail.value = res.result
  } catch (e) {
    console.error('[admin/word] openEditor error:', e)
    showToast('Lỗi tải chi tiết', false)
    editorOpen.value = false
  } finally {
    editorLoading.value = false
  }
}

const savingId = ref<string | null>(null)

async function saveWord(word: WordDto) {
  savingId.value = 'w' + word.id
  try {
    await $fetch<any>(`${base}/api/admin/words/${word.id}`, {
      method: 'PATCH',
      headers: { Authorization: `Bearer ${getToken()}` },
      body: { wordText: word.wordText, phonetic: word.phonetic, romaji: word.romaji, shortMean: word.shortMean, weight: word.weight }
    })
    showToast('Đã lưu Word ✓')
  } catch { showToast('Lỗi lưu Word', false) }
  finally { savingId.value = null }
}

async function saveSense(sense: SenseDto) {
  savingId.value = 's' + sense.id
  try {
    await $fetch<any>(`${base}/api/admin/senses/${sense.id}`, {
      method: 'PATCH',
      headers: { Authorization: `Bearer ${getToken()}` },
      body: { pos: sense.pos, senseOrder: sense.senseOrder }
    })
    showToast('Đã lưu Sense ✓')
  } catch { showToast('Lỗi lưu Sense', false) }
  finally { savingId.value = null }
}

async function saveGloss(gloss: GlossDto) {
  savingId.value = 'g' + gloss.id
  try {
    await $fetch<any>(`${base}/api/admin/glosses/${gloss.id}`, {
      method: 'PATCH',
      headers: { Authorization: `Bearer ${getToken()}` },
      body: { text: gloss.text }
    })
    showToast('Đã lưu Gloss ✓')
  } catch { showToast('Lỗi lưu Gloss', false) }
  finally { savingId.value = null }
}

async function saveExample(ex: ExampleDto) {
  savingId.value = 'ex' + ex.id
  try {
    await $fetch<any>(`${base}/api/admin/examples/${ex.id}`, {
      method: 'PATCH',
      headers: { Authorization: `Bearer ${getToken()}` },
      body: { contentJp: ex.contentJp, contentTranslated: ex.contentTranslated, transcription: ex.transcription }
    })
    showToast('Đã lưu Example ✓')
  } catch { showToast('Lỗi lưu Example', false) }
  finally { savingId.value = null }
}

async function saveReadingElement(re: ReadingElementDto) {
  savingId.value = 're' + re.id
  try {
    await $fetch<any>(`${base}/api/admin/reading-elements/${re.id}`, {
      method: 'PATCH',
      headers: { Authorization: `Bearer ${getToken()}` },
      body: { reb: re.reb, reNoKanji: re.reNoKanji, rePri: re.rePri }
    })
    showToast('Đã lưu ReadingElement ✓')
  } catch { showToast('Lỗi lưu ReadingElement', false) }
  finally { savingId.value = null }
}

async function saveKanji() {
  if (!detail.value.kanji) return
  savingId.value = 'kanji'
  try {
    await $fetch<any>(`${base}/api/admin/kanji/${detail.value.kanji.id}`, {
      method: 'PATCH',
      headers: { Authorization: `Bearer ${getToken()}` },
      body: { strokeCount: detail.value.kanji.strokeCount, jlptLevel: detail.value.kanji.jlptLevel, meaning: detail.value.kanji.meaning, freq: detail.value.kanji.freq }
    })
    showToast('Đã lưu Kanji ✓')
  } catch { showToast('Lỗi lưu Kanji', false) }
  finally { savingId.value = null }
}

async function saveKanjiExample(ex: KanjiExampleDto) {
  savingId.value = 'kex' + ex.id
  try {
    await $fetch<any>(`${base}/api/admin/kanji-examples/${ex.id}`, {
      method: 'PATCH',
      headers: { Authorization: `Bearer ${getToken()}` },
      body: { exampleType: ex.exampleType, readingGroup: ex.readingGroup, word: ex.word, meaning: ex.meaning, reading: ex.reading, hanViet: ex.hanViet }
    })
    showToast('Đã lưu KanjiExample ✓')
  } catch { showToast('Lỗi lưu KanjiExample', false) }
  finally { savingId.value = null }
}

// --- ADD mới bảng con ---

async function addGloss(sense: SenseDto) {
  savingId.value = 'addg' + sense.id
  try {
    const res = await $fetch<any>(`${base}/api/admin/senses/${sense.id}/glosses`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${getToken()}` },
      body: { text: '' }
    })
    sense.glosses.push(res.result)
    showToast('Đã thêm Gloss mới ✓')
  } catch { showToast('Lỗi thêm Gloss', false) }
  finally { savingId.value = null }
}

async function addExample(sense: SenseDto) {
  savingId.value = 'addex' + sense.id
  try {
    const res = await $fetch<any>(`${base}/api/admin/senses/${sense.id}/examples`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${getToken()}` },
      body: { contentJp: '', contentTranslated: '', transcription: '' }
    })
    sense.examples.push(res.result)
    showToast('Đã thêm Example mới ✓')
  } catch { showToast('Lỗi thêm Example', false) }
  finally { savingId.value = null }
}

async function addSense() {
  savingId.value = 'addsense'
  try {
    const res = await $fetch<any>(`${base}/api/admin/entries/${detail.value.id}/senses`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${getToken()}` },
      body: { pos: '' }
    })
    detail.value.senses.push(res.result)
    showToast('Đã thêm Sense mới ✓')
  } catch { showToast('Lỗi thêm Sense', false) }
  finally { savingId.value = null }
}

async function addReadingElement() {
  savingId.value = 'addre'
  try {
    const res = await $fetch<any>(`${base}/api/admin/entries/${detail.value.id}/reading-elements`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${getToken()}` },
      body: { reb: '' }
    })
    detail.value.readingElements.push(res.result)
    showToast('Đã thêm ReadingElement mới ✓')
  } catch { showToast('Lỗi thêm ReadingElement', false) }
  finally { savingId.value = null }
}

async function addKanjiExample() {
  if (!detail.value.kanji) return
  savingId.value = 'addkex'
  try {
    const res = await $fetch<any>(`${base}/api/admin/kanji/${detail.value.kanji.id}/examples`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${getToken()}` },
      body: { exampleType: 'on', word: '', meaning: '', reading: '' }
    })
    detail.value.kanji.examples.push(res.result)
    showToast('Đã thêm KanjiExample mới ✓')
  } catch { showToast('Lỗi thêm KanjiExample', false) }
  finally { savingId.value = null }
}

// --- Trie reload ---
const trieReloading = ref(false)
async function reloadTrie() {
  trieReloading.value = true
  try {
    const res = await $fetch<any>(`${base}/api/admin/reload-trie`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${getToken()}` }
    })
    showToast(res.message ?? 'Reload Trie thành công ✓')
  } catch { showToast('Reload Trie thất bại', false) }
  finally { trieReloading.value = false }
}

// --- Batch rebuild ---
const showBatchModal = ref(false)
const batchType = ref('word')
const batchLimit = ref(100)
const batchRunning = ref(false)
const batchResult = ref('')

async function runBatch() {
  batchRunning.value = true
  batchResult.value = ''
  try {
    const res = await $fetch<any>(`${base}/api/admin/entries/rebuild-batch?type=${batchType.value}&limit=${batchLimit.value}`, {
      method: 'POST',
      headers: { Authorization: `Bearer ${getToken()}` }
    })
    batchResult.value = res.message ?? 'Hoàn thành'
  } catch { batchResult.value = 'Lỗi khi chạy batch' }
  finally { batchRunning.value = false }
}

onMounted(fetchEntries)
</script>

<style scoped>
.font-japanese { font-family: 'Noto Sans JP', sans-serif; }
.fade-enter-active, .fade-leave-active { transition: opacity .3s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
</style>
