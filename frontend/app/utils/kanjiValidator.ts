export function validateKanji(character: string): boolean {
  if (!character || character.length !== 1) {
    return false;
  }
  
  // Regex cho kanji (Bao gồm CJK Unified Ideographs và extensions)
  const kanjiPattern = /^[\u4e00-\u9faf\u3400-\u4dbf\uf900-\ufaff]$/;
  
  return kanjiPattern.test(character);
}

/**
 * Kiểm tra xem chuỗi có chứa kanji hợp lệ không
 */
export function containsValidKanji(text: string): boolean {
  if (!text) return false;
  
  const kanjiPattern = /[\u4e00-\u9faf\u3400-\u4dbf\uf900-\ufaff]/;
  return kanjiPattern.test(text);
}