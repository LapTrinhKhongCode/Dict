// File: src/services/kanjiStore.ts

import { ref } from 'vue';

import Papa from 'papaparse';



// 1. Định nghĩa cấu trúc dữ liệu

export interface KanjiItem {

  id: string;

  char: string;

  meanlong: string[];

  jlpt: string;

  mean: string;

  strokes: string;

  freq: string;

  onyomi: string;

  kunyomi: string;

}



// 2. Tạo state dùng chung (shared state)

export const kanjiList = ref<KanjiItem[]>([]);

export const isLoading = ref(false);



// 3. Logic để đảm bảo chỉ load 1 LẦN DUY NHẤT

let loadPromise: Promise<KanjiItem[]> | null = null;



// 4. Các hàm helper (LẤY TỪ FILE KANJISTROKE.VUE CỦA BẠN)

// Chúng ta đặt chúng ở đây để xử lý file phức tạp

function cleanField(s?: string) {

  return (s ?? "").toString().trim().replace(/^\uFEFF/, "").replace(/^"|"$/g, "")

}

const katakana_re = /[\u30A0-\u30FF]/

const hiragana_re = /[\u3040-\u309F]/

function normalizeAllRebs(raw?: string) {

  const s = (raw ?? "").toString().trim().replace(/^"|"$/g, "")

  if (!s) return { ony: "", kun: "", combined: "" }

  const clean = s.replace("，", ",").replace(/\s+/g, " ").trim()

  const dropLeadingFreq = clean.replace(/^\s*\d+\s*[,，]\s*/, "")

  if (dropLeadingFreq !== clean) return normalizeAllRebs(dropLeadingFreq)

  if (clean.includes("|")) {

    const parts = clean.split("|").map(p => p.trim()).filter(Boolean)

    const ony = parts[0] || ""

    const kun = parts.slice(1).join(" | ") || ""

    const combined = [ony, kun].filter(Boolean).join(" | ")

    return { ony, kun, combined }

  }

  const tokens = clean.split(/\s+/).map(t => t.trim()).filter(Boolean)

  const onyTokens = tokens.filter(t => katakana_re.test(t))

  const kunTokens = tokens.filter(t => hiragana_re.test(t) || /[.\-]/.test(t))

  const ony = onyTokens.join(" ")

  const kun = kunTokens.join(" ")

  const combined = [ony, kun].filter(Boolean).join(" | ") || clean

  return { ony, kun, combined }

}

function extractFreqAndAllRebs(freqRaw?: string, allRebsRaw?: string) {

  let freq = cleanField(freqRaw)

  let all = (allRebsRaw ?? "").toString().trim()

  if (!/^\d+$/.test(freq)) {

    const m = (freq || "").match(/(\d{1,})/)

    if (m) {

      freq = m[1]

      const restFromFreqRaw = (freqRaw ?? "").toString().replace(m[0], "").replace(/^,|^，/, "").trim()

      all = (restFromFreqRaw ? (restFromFreqRaw + (all ? " " + all : "")) : all).trim()

    } else {

      const m2 = (all || "").match(/^\s*(\d+)\s*[,，]\s*(.*)$/)

      if (m2) {

        freq = m2[1]

        all = m2[2].trim()

      }

    }

  }

  if (!/^\d+$/.test(freq)) freq = ""

  return { freq, all }

}

function cleanMeanLongAndJlpt(meanRaw?: string, jlptRaw?: string) {
  // Lấy chuỗi gốc một cách an toàn
  const rawMeanString = (meanRaw ?? "").toString();

  // ----- PHẦN THAY ĐỔI CHÍNH -----
  // TRƯỚC ĐÂY:
  // let mean = meanRaw.replace(/##/g, " ").replace(/\s+/g, " ").trim()

  // BÂY GIỜ:
  // 1. Tách chuỗi bằng '##' để tạo một mảng các nghĩa.
  // 2. Với mỗi nghĩa, dùng map để dọn dẹp khoảng trắng thừa bên trong nó.
  // 3. Nối các nghĩa đã được làm sạch lại với nhau bằng ký tự xuống dòng '\n'.
  let mean = rawMeanString
    .split('##')
    .map(part => part.replace(/\s+/g, ' ').trim())
    .join('\n');
  // --------------------------------

  let jlpt = (jlptRaw ?? "").toString().trim();

  // Phần logic trích xuất JLPT vẫn giữ nguyên, nó vẫn hoạt động chính xác
  // vì nó chỉ tìm kiếm ở cuối toàn bộ chuỗi.
  const m = mean.match(/(.*?)[\.\,]?\s*(N[1-5])\s*$/);
  if (m) {
    mean = m[1].trim(); // Lấy phần nghĩa trước JLPT
    if (!jlpt) {
      jlpt = m[2]; // Nếu chưa có JLPT, gán giá trị tìm thấy
    }
  }

  return { mean, jlpt };
}


// 5. Hàm load chính (DÙNG import.meta.glob)

export async function loadKanjiData(): Promise<KanjiItem[]> {

  // Nếu đã load rồi (hoặc đang load) thì trả về ngay

  if (loadPromise) return loadPromise;

  if (kanjiList.value.length > 0) return kanjiList.value;



  isLoading.value = true;

  console.log("Loading complex kanji data (once)...");

  

  loadPromise = (async () => {

    try {

      // Dùng logic load file của Kanjistroke.vue vì nó chi tiết hơn

      const csvFiles = import.meta.glob("/src/assets/kanji_fixed_v2.csv", { as: "raw" });

      const loadCsv = csvFiles["/src/assets/kanji_fixed_v2.csv"];

      if (!loadCsv) throw new Error("Không tìm thấy file /src/assets/kanji_fixed_v2.csv");

      

      const csvText = await loadCsv();

      const result = Papa.parse<any>(csvText, {

        header: true,

        skipEmptyLines: true,

        transformHeader: h => (h || "").toString().trim()

      });



      const rows = result.data as any[];

      const items: KanjiItem[] = rows

        .filter(r => r && (r.Id || r.Character))

        .map((row: any) => {

            const id = cleanField(row.Id);

            const ch = cleanField(row.Character);

            const mraw = row.MeanLong ?? "";

            const jraw = row.JlptLevel ?? "";

            const meanClean = cleanMeanLongAndJlpt(mraw, jraw);

            const meanlong = meanClean.mean;

            const jlpt = meanClean.jlpt ?? "";

            const meaningField = cleanField(row.Meaning);

            const firstMeaning = (meaningField.split(/##|,/)[0] || "").trim();

            const strokes = cleanField(row.StrokeCount);

            const freqRaw = row.Freq ?? "";

            const allRebsRaw = row.AllRebs ?? "";

            const freqAndAll = extractFreqAndAllRebs(freqRaw, allRebsRaw);

            const freq = freqAndAll.freq;

            const normalized = normalizeAllRebs(freqAndAll.all);

            const onyomi = normalized.ony;

            const kunyomi = normalized.kun;

            return {

              id: id || "",

              char: ch || "",

              meanlong: meanlong || "",

              jlpt: jlpt || "",

              mean: firstMeaning || "",

              strokes: strokes || "",

              freq: freq || "",

              onyomi,

              kunyomi,

            };

        })

        .filter(item => item.id && item.char);



      kanjiList.value = items; // <-- Lưu dữ liệu vào state chung

      isLoading.value = false;

      console.log(`Kanji store loaded: ${items.length} items.`);

      return items;

    } catch (error) {

      console.error("Failed to load and parse kanji data:", error);

      isLoading.value = false;

      loadPromise = null; // Cho phép thử lại nếu lỗi

      return [];

    }

  })();



  return loadPromise;

}