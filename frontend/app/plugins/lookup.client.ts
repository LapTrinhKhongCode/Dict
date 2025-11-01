// plugins/lookup.client.ts

export default defineNuxtPlugin((nuxtApp) => {
  // Lấy global state
  const isModalVisible = useLookupModalVisible()
  const highlightedWord = useLookupHighlightedWord() // State tạm
  const selectedWord = useLookupSelectedWord()     // State tra cứu

  // 1. Tạo Pop-up Icon (Giữ nguyên)
  const popup = document.createElement('div')
  popup.id = 'global-lookup-popup'
  popup.innerHTML = '🔍'
  // (Giữ nguyên toàn bộ code style cho popup...)
  popup.style.position = 'absolute';
  popup.style.display = 'none';
  popup.style.zIndex = '9998';
  popup.style.padding = '5px';
  popup.style.backgroundColor = '#333';
  popup.style.color = 'white';
  popup.style.borderRadius = '5px';
  popup.style.cursor = 'pointer';
  popup.style.fontSize = '1.2rem';
  popup.style.lineHeight = '1';
  popup.style.userSelect = 'none';
  popup.onmouseover = () => { popup.style.backgroundColor = '#007bff'; };
  popup.onmouseout = () => { popup.style.backgroundColor = '#333'; };
  
  document.body.appendChild(popup)

  // 2. Gắn Listener Toàn cục
  
  // Click vào icon
  popup.addEventListener('click', () => {
    selectedWord.value = highlightedWord.value // Sao chép Tạm -> Tra cứu
    popup.style.display = 'none'
    isModalVisible.value = true // Mở modal
  })

  // Thả chuột (mouseup) ở BẤT CỨ ĐÂU
  document.addEventListener('mouseup', (e: MouseEvent) => {
    const target = e.target as HTMLElement
    
    // Bỏ qua nếu click vào chính pop-up
    if (target.closest('#global-lookup-popup')) {
      return
    }
    
    // (Bỏ qua nếu bôi đen trong ô input, v.v.)
    if (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA') {
        // (Trừ khi target là #reading-content, 
        // nhưng chúng ta có thể bỏ qua logic phức tạp đó vì nó vẫn hoạt động)
    }

    const selection = window.getSelection()
    const text = selection?.toString().trim()

    if (!text || text.length === 0) {
      popup.style.display = 'none'
      return
    }

    // Luôn cập nhật state "Tạm"
    highlightedWord.value = text 

    if (e.altKey) {
      // Chế độ 2: Tra ngay
      selectedWord.value = text // Sao chép Tạm -> Tra cứu
      popup.style.display = 'none'
      isModalVisible.value = true 
    } else {
      // Chế độ 1: Chỉ hiện icon
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