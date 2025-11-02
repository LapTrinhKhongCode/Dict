// plugins/lookup.client.ts
import { 
  useLookupModalVisible, 
  useTranslateModalVisible, 
  useLookupHighlightedWord, 
  useLookupSelectedWord 
} from '~/composables/useLookupState'

export default defineNuxtPlugin((nuxtApp) => {
  // Lấy global state
  const isModalVisible = useLookupModalVisible()
  const isTranslateModalVisible = useTranslateModalVisible()
  const highlightedWord = useLookupHighlightedWord()
  const selectedWord = useLookupSelectedWord()

  // --- 1. Tạo Pop-up CONTAINER (Sửa innerHTML) ---
  const popupContainer = document.createElement('div')
  popupContainer.id = 'global-lookup-container'
  
  // [ĐÃ SỬA] Thay "T" bằng SVG thô của icon 'lucide:languages'
  const svgIcon = `
    <svg xmlns="http://www.w3.org/2000/svg" 
         width="18" height="18" viewBox="0 0 24 24" 
         fill="none" stroke="currentColor" stroke-width="2.5" 
         stroke-linecap="round" stroke-linejoin="round" 
         style="display: block; margin: 0 auto;">
      <path d="m5 8 6 6"/>
      <path d="m4 14 6-6 2-3"/>
      <path d="M2 5h12"/>
      <path d="M7 2h1"/>
      <path d="m22 22-5-10-5 10"/>
      <path d="M14 18h6"/>
    </svg>
  `
  
  popupContainer.innerHTML = `
    <span id="global-lookup-icon" title="Tra cứu">🔍</span>
    <span id="global-translate-icon" title="Dịch">${svgIcon}</span>
  `
  
  // (Style cho container)
  popupContainer.style.position = 'absolute';
  popupContainer.style.display = 'none';
  popupContainer.style.zIndex = '9998';
  popupContainer.style.backgroundColor = '#333';
  popupContainer.style.borderRadius = '5px';
  popupContainer.style.boxShadow = '0 2px 5px rgba(0,0,0,0.2)';
  popupContainer.style.userSelect = 'none';
  popupContainer.style.overflow = 'hidden';
  
  document.body.appendChild(popupContainer)

  // --- 2. Lấy 2 icon đó và thêm Style (Sửa style) ---
  const lookupIcon = document.getElementById('global-lookup-icon')
  const translateIcon = document.getElementById('global-translate-icon')

  // [ĐÃ SỬA] Căn chỉnh 2 icon (emoji và SVG)
  const iconBaseStyle = `
    display: inline-flex; /* Dùng flex để căn giữa */
    align-items: center;
    justify-content: center;
    padding: 6px 9px; /* Tăng padding 1 chút */
    cursor: pointer;
    line-height: 1;
    color: white;
  `
  // Style cho icon Tra cứu (emoji)
  lookupIcon.style.cssText = iconBaseStyle + 'font-size: 1.1rem; border-right: 1px solid #555;';
  // Style cho icon Dịch (SVG)
  translateIcon.style.cssText = iconBaseStyle; 
  // --- KẾT THÚC SỬA ---

  lookupIcon.onmouseover = () => { lookupIcon.style.backgroundColor = '#007bff'; };
  lookupIcon.onmouseout = () => { lookupIcon.style.backgroundColor = 'transparent'; };
  translateIcon.onmouseover = () => { translateIcon.style.backgroundColor = '#28a745'; };
  translateIcon.onmouseout = () => { translateIcon.style.backgroundColor = 'transparent'; };
  
  // --- 3. Gắn Listener Toàn cục (Đã sửa logic Reset) ---
  
  lookupIcon.addEventListener('click', () => {
    selectedWord.value = highlightedWord.value
    popupContainer.style.display = 'none'
    isTranslateModalVisible.value = false // Tắt Dịch
    isModalVisible.value = true      // Bật Tra cứu
  })

  translateIcon.addEventListener('click', () => {
    selectedWord.value = highlightedWord.value
    popupContainer.style.display = 'none'
    isModalVisible.value = false      // Tắt Tra cứu
    isTranslateModalVisible.value = true // Bật Dịch
  })

  document.addEventListener('mouseup', (e: MouseEvent) => {
    const target = e.target as HTMLElement
    if (target.closest('#global-lookup-container')) {
      return
    }
    const selection = window.getSelection()
    const text = selection?.toString().trim()
    if (!text || text.length === 0) {
      popupContainer.style.display = 'none'
      return
    }
    highlightedWord.value = text 
    if (e.altKey) {
      selectedWord.value = text
      popupContainer.style.display = 'none'
      isTranslateModalVisible.value = false
      isModalVisible.value = true
    } else {
      popupContainer.style.top = `${e.clientY + window.scrollY + 5}px`
      popupContainer.style.left = `${e.clientX + window.scrollX}px`
      popupContainer.style.display = 'block'
    }
  })
  
  document.addEventListener('mousedown', (e: MouseEvent) => {
    const target = e.target as HTMLElement
    if (!target.closest('#global-lookup-container')) {
      popupContainer.style.display = 'none'
    }
  })
})