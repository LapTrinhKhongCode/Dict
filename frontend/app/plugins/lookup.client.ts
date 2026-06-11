// plugins/lookup.client.ts
import { 
  useLookupModalVisible, 
  useTranslateModalVisible, 
  useLookupHighlightedWord, 
  useLookupSelectedWord,
  useOcrResultsState,
} from '~/composables/useLookupState'

export default defineNuxtPlugin((nuxtApp) => {
  const isModalVisible          = useLookupModalVisible()
  const isTranslateModalVisible = useTranslateModalVisible()
  const highlightedWord         = useLookupHighlightedWord()
  const selectedWord            = useLookupSelectedWord()
  const ocrResultsState         = useOcrResultsState()

  const route = useRoute()
  const disabledPaths = ['/login', '/confirm-account']

  // ── Helper: parse boundingBox từ Google Vision ────────────────────────
  function parseBbox(raw: any) {
    try {
      const b = typeof raw === 'string' ? JSON.parse(raw) : raw
      if (!b) return null
      const xs = b.map((p: any) => Array.isArray(p) ? p[0] : (p.x ?? 0))
      const ys = b.map((p: any) => Array.isArray(p) ? p[1] : (p.y ?? 0))
      return {
        minX: Math.min(...xs), maxX: Math.max(...xs),
        minY: Math.min(...ys), maxY: Math.max(...ys),
      }
    } catch { return null }
  }

  // ── Helper: lấy câu context từ ocrResults (hướng A — cùng dòng) ──────
  function getContextSentence(keyword: string): string {
  const results = ocrResultsState.value;
  if (!results || results.length === 0) return keyword;

  // 1. Sắp xếp toàn bộ chữ trên trang theo luồng đọc tự nhiên
  // Ưu tiên Y (dòng) trước, sau đó đến X (vị trí trong dòng)
  const sortedResults = [...results].sort((a: any, b: any) => {
    const ba = parseBbox(a.boundingBox);
    const bb = parseBbox(b.boundingBox);
    if (!ba || !bb) return 0;

    // Sai số dòng (Line threshold) khoảng 10px để coi là cùng một dòng
    if (Math.abs(ba.minY - bb.minY) > 10) {
      return ba.minY - bb.minY;
    }
    return ba.minX - bb.minX;
  });

  // 2. Tìm index của từ khóa trong mảng đã sắp xếp
  const targetIdx = sortedResults.findIndex((r: any) => r.wordText === keyword);
  if (targetIdx === -1) return keyword;

  // 3. Lấy "Cửa sổ" ngữ cảnh lan tỏa (15 từ trước và 15 từ sau)
  const windowSize = 15;
  const start = Math.max(0, targetIdx - windowSize);
  const end = Math.min(sortedResults.length, targetIdx + windowSize + 1);

  const contextWords = sortedResults.slice(start, end);
  
  // Trả về chuỗi văn bản đã được khôi phục thứ tự logic
  return contextWords.map((r: any) => r.wordText).join(' ');
}

  // ── RAG state cục bộ ──────────────────────────────────────────────────
  let ragContexts: any[] = []
  let ragKeyword  = ''
  let ragContext  = ''

  // ── SVG icons ─────────────────────────────────────────────────────────
  const svgTranslate = `
    <svg xmlns="http://www.w3.org/2000/svg" 
         width="18" height="18" viewBox="0 0 24 24" 
         fill="none" stroke="currentColor" stroke-width="2.5" 
         stroke-linecap="round" stroke-linejoin="round" 
         style="display:block;margin:0 auto">
      <path d="m5 8 6 6"/>
      <path d="m4 14 6-6 2-3"/>
      <path d="M2 5h12"/>
      <path d="M7 2h1"/>
      <path d="m22 22-5-10-5 10"/>
      <path d="M14 18h6"/>
    </svg>`
const svgSave = `
    <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" viewBox="0 0 24 24" 
         fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" 
         style="display:block;margin:0 auto">
      <path d="M19 21l-7-5-7 5V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z"></path>
    </svg>`
  const svgAI = `
    <svg xmlns="http://www.w3.org/2000/svg" width="15" height="15" viewBox="0 0 24 24"
         fill="currentColor" stroke="none" style="display:block">
      <path d="M12 2l2.4 7.4H22l-6.2 4.5 2.4 7.4L12 17l-6.2 4.3 2.4-7.4L2 9.4h7.6z"/>
    </svg>`

// ── Tạo popup container ───────────────────────────────────────────────
  const popupContainer = document.createElement('div')
  popupContainer.id = 'global-lookup-container'
  popupContainer.innerHTML = `
    <div id="glc-icons">
      <span id="global-lookup-icon"    title="Tra cứu">🔍</span>
      <span id="global-translate-icon" title="Dịch">${svgTranslate}</span>
      <span id="global-save-icon"      title="Lưu từ vựng">${svgSave} <span style="margin-left:4px">Lưu</span></span>
      <span id="global-ai-icon"        title="AI giải thích">
        <span id="ai-icon-inner">${svgAI}</span>
        <span id="ai-btn-label">AI</span>
        <span id="ai-loading" style="display:none">⏳</span>
      </span>
    </div>
    <div id="rag-result-box"  style="display:none"></div>
    <div id="ai-explain-box"  style="display:none"></div>
  `

  Object.assign(popupContainer.style, {
    position:        'absolute',
    display:         'none',
    zIndex:          '9998',
    backgroundColor: '#161b27',
    borderRadius:    '12px',
    boxShadow:       '0 8px 32px rgba(0,0,0,0.6), 0 2px 8px rgba(0,0,0,0.4)',
    userSelect:      'none',
    overflow:        'hidden',
    width:           '380px',
    border:          '1px solid #2d3448',
  })

  document.body.appendChild(popupContainer)

  // ── Inject CSS ────────────────────────────────────────────────────────
  const styleEl = document.createElement('style')
  styleEl.textContent = `
    /* ── Header icon bar ── */
    #glc-icons {
      display: flex;
      align-items: stretch;
      background: #1e2436;
      justify-content: flex-start;
      border-bottom: 1px solid #2d3448;
    }
      /* Trong phần tiêm CSS */
    #global-save-icon {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      padding: 9px 14px;
      cursor: pointer;
      color: #cbd5e1;
      border-right: 1px solid #2d3448;
      transition: background 0.15s, color 0.15s;
      font-size: 0.82rem;
      font-weight: 500;
    }
    #global-save-icon:hover { background: #d97706; color: #fff; }

    #global-lookup-icon,
    #global-translate-icon,
    #global-ai-icon {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: 6px;
      padding: 9px 14px;
      cursor: pointer;
      line-height: 1;
      color: #cbd5e1;
      border-right: 1px solid #2d3448;
      transition: background 0.15s, color 0.15s;
      font-size: 0.82rem;
      font-weight: 500;
      font-family: system-ui, sans-serif;
      white-space: nowrap;
    }
    #global-lookup-icon    { font-size: 1rem; letter-spacing: 0.02em; }
    #global-translate-icon { border-right: 1px solid #2d3448; }
    #global-ai-icon {
      border-right: none;
      margin-left: auto;
      font-weight: 700;
      font-size: 0.85rem;
      letter-spacing: 0.03em;
      animation: ai-shimmer 2.5s ease-in-out infinite;
    }
    @keyframes ai-shimmer {
      0%,100% { color: #a78bfa; }
      50%      { color: #fbbf24; }
    }
    #global-lookup-icon:hover    { background: #1d4ed8; color: #fff; }
    #global-translate-icon:hover { background: #15803d; color: #fff; }
    #global-ai-icon:hover        { background: #4c1d95; color: #fff !important; animation: none; }

    /* ── Content boxes ── */
    #rag-result-box, #ai-explain-box {
      padding: 14px 16px;
      font-size: 0.875rem;
      color: #cbd5e1;
      border-top: 1px solid #2d3448;
      line-height: 1.65;
      max-height: 260px;
      overflow-y: auto;
      font-family: system-ui, sans-serif;
    }

    /* Manual meaning input */
    #glc-manual-meaning:focus {
      border-color: #0ea5e9 !important;
      box-shadow: 0 0 0 2px rgba(14,165,233,0.2);
    }
    #glc-save-confirm:hover { background: #0284c7 !important; }
    #glc-save-cancel:hover { background: #334155 !important; color: #f1f5f9 !important; }

    /* Scrollbar đẹp */
    #rag-result-box::-webkit-scrollbar,
    #ai-explain-box::-webkit-scrollbar {
      width: 4px;
    }
    #rag-result-box::-webkit-scrollbar-track,
    #ai-explain-box::-webkit-scrollbar-track {
      background: transparent;
    }
    #rag-result-box::-webkit-scrollbar-thumb,
    #ai-explain-box::-webkit-scrollbar-thumb {
      background: #3d4a63;
      border-radius: 99px;
    }
    #rag-result-box::-webkit-scrollbar-thumb:hover,
    #ai-explain-box::-webkit-scrollbar-thumb:hover {
      background: #5b6a87;
    }

    /* Loading */
    .rag-loading {
      color: #64748b;
      font-style: italic;
      font-size: 0.85rem;
    }

    /* RAG items */
    .rag-item {
      margin-bottom: 12px;
      padding-bottom: 12px;
      border-bottom: 1px solid #2d344855;
    }
    .rag-item:last-child { border-bottom: none; margin-bottom: 0; padding-bottom: 0; }

    .rag-label {
      display: inline-block;
      background: #2a1f00;
      color: #fbbf24;
      font-weight: 700;
      font-size: 0.75rem;
      padding: 2px 8px;
      border-radius: 99px;
      margin-bottom: 6px;
      letter-spacing: 0.04em;
      border: 1px solid #f0c04033;
    }
    .rag-meaning {
      color: #f1f5f9;
      font-size: 0.9rem;
      font-weight: 500;
      margin-bottom: 4px;
    }
    .rag-example-jp {
      color: #94a3b8;
      font-size: 0.8rem;
      margin-top: 3px;
      padding-left: 8px;
      border-left: 2px solid #3d4a63;
    }
    .rag-example-vi {
      color: #64748b;
      font-size: 0.78rem;
      margin-top: 2px;
      padding-left: 8px;
      border-left: 2px solid #3d4a6355;
      font-style: italic;
    }

    /* AI explain */
    .ai-word {
      font-size: 1.05rem;
      font-weight: 700;
      color: #fbbf24;
      margin-bottom: 8px;
      display: flex;
      align-items: center;
      gap: 6px;
    }
    .ai-meaning {
      color: #f1f5f9;
      font-size: 0.9rem;
      font-weight: 500;
      margin-bottom: 8px;
      padding: 8px 10px;
      background: #1e2d1e;
      border-radius: 6px;
      border-left: 3px solid #22c55e;
    }
    .ai-explain {
      color: #94a3b8;
      font-size: 0.83rem;
      line-height: 1.7;
    }
  `
  document.head.appendChild(styleEl)

  // ── Lấy elements ─────────────────────────────────────────────────────
  const lookupIcon    = document.getElementById('global-lookup-icon')!
  const translateIcon = document.getElementById('global-translate-icon')!
  const aiIcon        = document.getElementById('global-ai-icon')!
  const aiIconInner   = document.getElementById('ai-icon-inner')!
  const aiLoading     = document.getElementById('ai-loading')!
  const ragResultBox  = document.getElementById('rag-result-box')!
  const aiExplainBox  = document.getElementById('ai-explain-box')!
  const saveIcon = document.getElementById('global-save-icon')!
  

  // ── Click: Tra cứu (giữ nguyên) ───────────────────────────────────────
  lookupIcon.addEventListener('click', () => {
    selectedWord.value            = highlightedWord.value
    popupContainer.style.display  = 'none'
    isTranslateModalVisible.value = false
    isModalVisible.value          = true
    console.log('Lookup icon clicked: ' + selectedWord.value + isModalVisible.value)
  })

  // ── Click: Dịch (giữ nguyên) ──────────────────────────────────────────
  translateIcon.addEventListener('click', () => {
    selectedWord.value            = highlightedWord.value
    popupContainer.style.display  = 'none'
    isModalVisible.value          = false
    isTranslateModalVisible.value = true
    console.log('Translate icon clicked: ' + selectedWord.value + isTranslateModalVisible.value)
  })
saveIcon.addEventListener('click', async () => {
    const wordToSave = ragKeyword;
    let meaningToSave = '';
    if (ragContexts && ragContexts.length > 0) {
      meaningToSave = ragContexts[0].meaning || '';
    }

    const isReaderPage = route.path === '/reader' && route.query.projectId;

    // ── Nếu chưa có nghĩa → hiện form nhập trước khi lưu ─────────────────
    if (!meaningToSave) {
      ragResultBox.style.display = 'block';
      aiExplainBox.style.display = 'none';
      ragResultBox.innerHTML = `
        <div style="display:flex;flex-direction:column;gap:8px">
          <span style="color:#94a3b8;font-size:0.8rem">
            Không tìm thấy nghĩa cho <b style="color:#f1f5f9">${wordToSave}</b> trong DB.
            Nhập nghĩa của bạn:
          </span>
          <input
            id="glc-manual-meaning"
            type="text"
            placeholder="Nhập nghĩa / ghi chú..."
            style="width:100%;background:#0f172a;border:1px solid #334155;border-radius:8px;
                   padding:7px 10px;color:#f1f5f9;font-size:0.85rem;outline:none;"
            autofocus
          />
          <div style="display:flex;gap:8px;justify-content:flex-end">
            <button id="glc-save-cancel"
              style="padding:5px 14px;background:#1e293b;color:#94a3b8;border:none;border-radius:6px;
                     font-size:0.8rem;cursor:pointer;">
              Bỏ qua
            </button>
            <button id="glc-save-confirm"
              style="padding:5px 14px;background:#0ea5e9;color:#fff;border:none;border-radius:6px;
                     font-size:0.8rem;font-weight:600;cursor:pointer;">
              ${isReaderPage ? '📚 Lưu sổ tay' : '🔖 Lưu'}
            </button>
          </div>
        </div>`;

      // Focus input
      setTimeout(() => {
        const inp = document.getElementById('glc-manual-meaning') as HTMLInputElement | null;
        if (inp) inp.focus();
      }, 50);

      // Handler cancel
      document.getElementById('glc-save-cancel')?.addEventListener('click', () => {
        popupContainer.style.display = 'none';
      });

      // Handler confirm (enter hoặc click)
      const doSave = async () => {
        const inp = document.getElementById('glc-manual-meaning') as HTMLInputElement | null;
        meaningToSave = inp?.value.trim() || '';
        await performSave(wordToSave, meaningToSave, isReaderPage);
      };

      document.getElementById('glc-save-confirm')?.addEventListener('click', doSave);
      document.getElementById('glc-manual-meaning')?.addEventListener('keydown', (e: any) => {
        if (e.key === 'Enter') doSave();
        if (e.key === 'Escape') popupContainer.style.display = 'none';
      });
      return;
    }

    await performSave(wordToSave, meaningToSave, isReaderPage);
  })

  async function performSave(wordToSave: string, meaningToSave: string, isReaderPage: any) {
    // Nếu đang trên trang reader có projectId → lưu thẳng vào API sổ tay
    if (isReaderPage) {
      const projectId = route.query.projectId;
      const jobId = route.query.jobId ? Number(route.query.jobId) : null;

      saveIcon.style.opacity = '0.5';
      saveIcon.style.pointerEvents = 'none';

      try {
        const config = useRuntimeConfig();
        const token = localStorage.getItem('jwt_token') || '';
        const res = await fetch(`${config.public.apiBaseUrl}/api/projects/${projectId}/vocabularies`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
          body: JSON.stringify({
            wordText: wordToSave,
            contextMeaning: meaningToSave,
            sourceOcrJobId: jobId,
            sourceSentence: ragContext || wordToSave,
          }),
        });

        if (res.ok) {
          ragResultBox.innerHTML = `<span style="color:#4ade80;font-weight:600">✓ Đã lưu "${wordToSave}" vào sổ tay dự án</span>`;
          ragResultBox.style.display = 'block';
          setTimeout(() => { popupContainer.style.display = 'none'; }, 1800);
        } else if (res.status === 409) {
          ragResultBox.innerHTML = `<span style="color:#fbbf24">Từ này đã có trong sổ tay.</span>`;
          ragResultBox.style.display = 'block';
          setTimeout(() => { popupContainer.style.display = 'none'; }, 1600);
        } else {
          ragResultBox.innerHTML = `<span style="color:#f87171">Lỗi lưu từ vựng.</span>`;
          ragResultBox.style.display = 'block';
          setTimeout(() => { popupContainer.style.display = 'none'; }, 1600);
        }
      } catch {
        ragResultBox.innerHTML = `<span style="color:#f87171">Lỗi kết nối.</span>`;
        ragResultBox.style.display = 'block';
        setTimeout(() => { popupContainer.style.display = 'none'; }, 1600);
      } finally {
        saveIcon.style.opacity = '';
        saveIcon.style.pointerEvents = '';
      }
      return;
    }

    // Ngoài reader page: dispatch event để component bắt (Deck cũ)
    popupContainer.style.display = 'none';
    window.dispatchEvent(new CustomEvent('open-vocab-popup', {
      detail: { word: wordToSave, meaning: meaningToSave }
    }));
  }
  // ── Click: AI giải thích (MỚI) ────────────────────────────────────────
  // ── Click: AI giải thích (Cập nhật: Không chặn khi ragContexts rỗng) ──
  aiIcon.addEventListener('click', async () => {
    // Thay vì return ngay, ta cho phép gửi đi. 
    // Chỉ cảnh báo nếu cả Keyword cũng trống (trường hợp hy hữu)
    if (!ragKeyword) return 

    // Reset + show loading
    aiExplainBox.style.display = 'none'
    aiExplainBox.innerHTML     = ''
    aiLoading.style.display    = 'inline'
    aiIconInner.style.display  = 'none'

    try {
      const config = useRuntimeConfig()
      const token  = localStorage.getItem('jwt_token') || ''

      const res = await fetch(`${config.public.apiBaseUrl}/api/Search/rag/explain`, {
        method:  'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization:  `Bearer ${token}`,
        },
        body: JSON.stringify({
          keyword:     ragKeyword,
          context:     ragContext,
          ragContexts: ragContexts, // Gửi mảng rỗng [] lên, AI vẫn xử lý được
        }),
      })

      if (!res.ok) throw new Error('API lỗi')
      const data = await res.json()

      const bestMeaning = data.bestMeaning ?? ''
      const word = data.word || ragKeyword
      const explanation = data.explanation ?? ''

      // Kiểm tra kết quả AI có hợp lệ không
      const isError = !word || bestMeaning === 'Lỗi API' || bestMeaning === 'Lỗi cấu hình Key'
        || explanation.startsWith('Mã lỗi') || explanation.startsWith('Không tìm thấy')

      const isReaderPage = !isError && route.path === '/reader' && route.query.projectId

      if (isError) {
        aiExplainBox.innerHTML = `<span style="color:#f87171">⚠ AI không thể giải thích lúc này. Thử lại sau.</span>`
        aiExplainBox.style.display = 'block'
      } else {
        aiExplainBox.innerHTML = `
          <div class="ai-word">✨ ${word}</div>
          <div class="ai-meaning">${bestMeaning}</div>
          <div class="ai-explain">${explanation}</div>
          ${isReaderPage ? `
          <button id="ai-save-notebook" style="
            margin-top:10px;display:inline-flex;align-items:center;gap:5px;
            padding:5px 12px;background:rgba(99,102,241,0.15);
            border:1px solid rgba(99,102,241,0.3);border-radius:8px;
            color:#818cf8;font-size:0.78rem;font-weight:600;cursor:pointer;
          ">💾 Lưu vào sổ tay</button>` : ''}
        `
        aiExplainBox.style.display = 'block'

        if (isReaderPage) {
          setTimeout(() => {
            const saveBtn = document.getElementById('ai-save-notebook')
            if (!saveBtn) return
            saveBtn.addEventListener('click', async () => {
              saveBtn.textContent = '⏳ Đang lưu...'
              saveBtn.setAttribute('disabled', 'true')
              try {
                const config2 = useRuntimeConfig()
                const token = localStorage.getItem('jwt_token') || ''
                const projectId = route.query.projectId
                const r = await fetch(`${config2.public.apiBaseUrl}/api/projects/${projectId}/vocabularies`, {
                  method: 'POST',
                  headers: { 'Content-Type': 'application/json', Authorization: `Bearer ${token}` },
                  body: JSON.stringify({
                    wordText: ragKeyword,
                    contextMeaning: bestMeaning || explanation.slice(0, 200),
                    sourceSentence: ragContext || null,
                    sourceOcrJobId: null,
                    sourcePageNumber: null,
                  })
                })
                if (r.ok) {
                  saveBtn.textContent = '✓ Đã lưu sổ tay!'
                  saveBtn.style.color = '#34d399'
                  saveBtn.style.borderColor = 'rgba(52,211,153,0.3)'
                  saveBtn.style.background = 'rgba(52,211,153,0.1)'
                } else {
                  saveBtn.textContent = '✗ Lỗi lưu'
                  saveBtn.removeAttribute('disabled')
                }
              } catch {
                saveBtn.textContent = '✗ Lỗi kết nối'
                saveBtn.removeAttribute('disabled')
              }
            })
          }, 50)
        }
      }
    } catch {
      aiExplainBox.innerHTML = `<span style="color:#f87171">Lỗi kết nối AI, thử lại sau.</span>`
      aiExplainBox.style.display = 'block'
    } finally {
      aiLoading.style.display   = 'none'
      aiIconInner.style.display = 'inline'
    }
  })

  // ── mouseup: bôi đen → hiện popup + gọi rag/search ngay ─────────────
  document.addEventListener('mouseup', async (e: MouseEvent) => {
    const isBlocked = disabledPaths.some(p => route.path.startsWith(p))
    if (isBlocked) {
      popupContainer.style.display = 'none'
      return
    }
    const target = e.target as HTMLElement
    if (target.closest('#global-lookup-container')) return

    const selection = window.getSelection()
    const text      = selection?.toString().trim()

    if (!text || text.length === 0) {
      popupContainer.style.display = 'none'
      return
    }

    highlightedWord.value = text

    // Alt+click → mở tra cứu ngay (giữ nguyên behaviour cũ)
    if (e.altKey) {
      selectedWord.value            = text
      popupContainer.style.display  = 'none'
      isTranslateModalVisible.value = false
      isModalVisible.value          = true
      return
    }

    // Reset state
    ragContexts                = []
    ragKeyword                 = text
    ragContext                 = getContextSentence(text)
    ragResultBox.innerHTML     = '<span class="rag-loading">Đang tìm kiếm...</span>'
    ragResultBox.style.display = 'block'
    aiExplainBox.style.display = 'none'
    aiExplainBox.innerHTML     = ''

    // Hiện popup tại vị trí chuột — tránh khuất màn hình
    const POPUP_W = 380
    const POPUP_H = 320 // ước tính
    const vw      = window.innerWidth
    const vh      = window.innerHeight

    let left = e.clientX + window.scrollX
    let top  = e.clientY + window.scrollY + 12

    // Nếu popup sẽ bị tràn bên phải → dịch sang trái
    if (e.clientX + POPUP_W > vw - 16) {
      left = Math.max(window.scrollX + 8, e.clientX + window.scrollX - POPUP_W)
    }
    // Nếu popup sẽ bị tràn bên dưới → hiện phía trên con trỏ
    if (e.clientY + POPUP_H > vh - 16) {
      top = e.clientY + window.scrollY - POPUP_H - 8
    }

    popupContainer.style.left    = `${left}px`
    popupContainer.style.top     = `${top}px`
    popupContainer.style.display = 'block'

    // Gọi rag/search
    try {
      const config = useRuntimeConfig()
      const token  = localStorage.getItem('jwt_token') || ''

      const res = await fetch(`${config.public.apiBaseUrl}/api/Search/rag/search`, {
        method:  'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization:  `Bearer ${token}`,
        },
        body: JSON.stringify({ keyword: ragKeyword, context: ragContext }),
      })

      if (!res.ok) throw new Error()
      const data  = await res.json()
      ragContexts = data.contextUsed || []

      if (ragContexts.length === 0) {
        ragResultBox.innerHTML = `<span style="color:#94a3b8">Không tìm thấy kết quả.</span>`
      } else {
        ragResultBox.innerHTML = ragContexts.slice(0, 3).map((item: any) => `
          <div class="rag-item">
            <div class="rag-label">${item.label ?? ''}</div>
            <div class="rag-meaning">${item.meaning ?? ''}</div>
            ${item.exampleJp ? `<div class="rag-example-jp">${item.exampleJp}</div>` : ''}
            ${item.exampleVi ? `<div class="rag-example-vi">${item.exampleVi}</div>` : ''}
          </div>
        `).join('')
      }
    } catch {
      ragResultBox.innerHTML = `<span style="color:#f87171">Lỗi tìm kiếm.</span>`
    }
  })

  // ── mousedown: click ra ngoài → ẩn popup (giữ nguyên) ────────────────
  document.addEventListener('mousedown', (e: MouseEvent) => {
    const target = e.target as HTMLElement
    if (!target.closest('#global-lookup-container')) {
      popupContainer.style.display = 'none'
    }
  })
})