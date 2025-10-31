// plugins/lookup.client.ts
import conjugationsData from '~/data/conjugations_normalized.json'

// --- (Dán các hàm helper 'isSingleKanji', 'extractWordLeftOfSlash', 'getDictionaryForm', 'checkConjugation' của bạn vào đây) ---
const isSingleKanji = (text: string): boolean => {
  const trimmed = text.trim();
  if (trimmed.length !== 1) return false;
  const charCode = trimmed.charCodeAt(0);
  return (
    (charCode >= 0x4e00 && charCode <= 0x9fff) ||
    (charCode >= 0x3400 && charCode <= 0x4dbf) ||
    (charCode >= 0x20000 && charCode <= 0x2a6df)
  );
};
const extractWordLeftOfSlash = (word: string): string => {
  const slashIndex = word.indexOf("/");
  return slashIndex !== -1 ? word.substring(0, slashIndex) : word;
};
const getDictionaryForm = (word: string): string => {
  const trimmed = word.trim();
  if (!trimmed) return trimmed;
  // @ts-ignore
  if (conjugationsData.byForm && conjugationsData.byForm[trimmed]) {
    // @ts-ignore
    const dictionaryFormWithSlash = conjugationsData.byForm[trimmed];
    return extractWordLeftOfSlash(dictionaryFormWithSlash);
  }
  return extractWordLeftOfSlash(trimmed);
};
const checkConjugation = (word: string): any | null => {
  const trimmed = word.trim();
  if (!trimmed) return null;
  const dictionaryForm = getDictionaryForm(trimmed);
  // @ts-ignore
  if (conjugationsData.byRoot) {
    const targetKey = `${dictionaryForm}/`;
    // @ts-ignore
    for (const [key, conjugations] of Object.entries(conjugationsData.byRoot)) {
      if (key.startsWith(targetKey)) {
        return {
          root: dictionaryForm,
          conjugations: conjugations,
          originalForm: trimmed !== dictionaryForm ? trimmed : null,
        };
      }
    }
  }
  return null;
};
// --- (Kết thúc hàm helper) ---


export default defineNuxtPlugin((nuxtApp) => {
  // Lấy global state
  const isModalVisible = useLookupModalVisible()
  const selectedWord = useLookupSelectedWord()
  const apiResult = useLookupApiResult()
  const conjugationResult = useLookupConjugationResult()
  const loading = useLookupLoading()
  const error = useLookupError()
  
  const config = useRuntimeConfig()
  
  // --- 1. Tạo Pop-up Icon và thêm vào <body> ---
  const popup = document.createElement('div')
  popup.id = 'global-lookup-popup'
  popup.innerHTML = '🔍'
  popup.style.position = 'absolute'
  popup.style.display = 'none'
  popup.style.zIndex = '9998'
  popup.style.padding = '5px'
  popup.style.backgroundColor = '#333'
  popup.style.color = 'white'
  popup.style.borderRadius = '5px'
  popup.style.cursor = 'pointer'
  popup.style.fontSize = '1.2rem'
  popup.style.lineHeight = '1'
  popup.style.userSelect = 'none'
  popup.onmouseover = () => { popup.style.backgroundColor = '#007bff'; };
  popup.onmouseout = () => { popup.style.backgroundColor = '#333'; };
  
  document.body.appendChild(popup)

  // --- 2. Hàm Fetch Toàn cục ---
  const fetchWord = async (word: string) => {
    if (loading.value) return // Không fetch nếu đang fetch dở
    selectedWord.value = word
    try {
      loading.value = true
      error.value = ""
      apiResult.value = null
      conjugationResult.value = null

      let apiUrl: string;
      let response: any;

      if (isSingleKanji(word)) {
        apiUrl = `${config.public.apiBaseUrl}/api/Kanji/GetKanjiJson/${encodeURIComponent(word)}`;
        const res = await fetch(apiUrl);
        if (!res.ok) { isModalVisible.value = true; return; } 
        response = await res.json();
        if (response.status === 200 && response.results && response.results.length > 0) {
          apiResult.value = { type: "kanji", kanji: response.results[0] };
        }
      } else {
        const conjugation = checkConjugation(word);
        if (conjugation) {
          conjugationResult.value = conjugation;
        }
        const dictionaryForm = getDictionaryForm(word);
        apiUrl = `${config.public.apiBaseUrl}/api/Word/GetWordJson/${encodeURIComponent(dictionaryForm)}`;
        const res = await fetch(apiUrl);
        if (!res.ok) { isModalVisible.value = true; return; }
        response = await res.json();
        
        const hasWordData = response.data?.words?.length > 0;
        const hasSuggestData = response.data?.suggestWords?.length > 0;
        
        if (response.status === 200 && (hasWordData || hasSuggestData)) {
          apiResult.value = { type: "word", ...response.data };
        }
      }
    } catch (e: any) {
      error.value = e.message || "Error loading data";
    } finally {
      loading.value = false;
      isModalVisible.value = true; // Mở modal toàn cục
    }
  }
  
  // --- 3. Gắn Listener Toàn cục ---
  
  // Click vào icon
  popup.addEventListener('click', () => {
    if (selectedWord.value) {
      popup.style.display = 'none'
      fetchWord(selectedWord.value)
    }
  })

  // Thả chuột (mouseup) ở bất cứ đâu
  document.addEventListener('mouseup', (e: MouseEvent) => {
    const target = e.target as HTMLElement
    // Bỏ qua nếu click vào pop-up hoặc modal
    if (target.closest('#global-lookup-popup') || target.closest('.modal-overlay')) {
      return
    }

    const selection = window.getSelection()
    const text = selection?.toString().trim()

    if (!text || text.length === 0) {
      popup.style.display = 'none'
      return
    }

    selectedWord.value = text

    if (e.altKey) {
      // Chế độ 2: Tra ngay
      popup.style.display = 'none'
      fetchWord(text)
    } else {
      // Chế độ 1: Hiện icon
      popup.style.top = `${e.clientY + window.scrollY + 5}px`
      popup.style.left = `${e.clientX + window.scrollX}px`
      popup.style.display = 'block'
    }
  })
  
  // Nhấn chuột (mousedown) để ẩn icon
  document.addEventListener('mousedown', (e: MouseEvent) => {
    const target = e.target as HTMLElement
    if (!target.closest('#global-lookup-popup')) {
      popup.style.display = 'none'
    }
  })
})