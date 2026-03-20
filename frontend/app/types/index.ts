// ~/types/index.ts

/** DTO tóm tắt cho một bộ thẻ, dùng ở trang chủ */
export interface DeckSummaryDto {
    id: number;
    name: string;        // ✨ SỬA: từ 'title' thành 'name'
    description: string;
    cardCount: number;
    avatar?: string; 
    authorUsername?: string,
    isPublic: boolean;
    authorName: string; 
    authorImageUrl: string; // ✨ SỬA: từ 'creatorName' thành 'authorName'
      nowAuthorName: string | null; // Current owner if saved/copied
  nowAuthorImageUrl: string | null;
}

export interface DeckCreateDto {
  title: string;
  description?: string | null;
  isPublic: boolean;
  cards: CardCreateDto[]; 
}
export interface CardCreateDto {
  frontText: string; 
  backText: string;
  tags?: string | null;
}
/** DTO cho một thẻ flashcard đơn lẻ */
export interface CardDto {
    id: number;
    charBig: string;
    pinyin: string;
    meaning: string;
    nextReviewAt: string; 
}

/** DTO chi tiết cho một bộ thẻ, dùng ở trang danh sách thẻ */
export interface DeckDetailDto {
    id: number;
    title: string;
    description: string;
    cards: CardDto[];
    userId: number;
    isPublic: boolean;
    authorName: string;
}

/** DTO cho request khi người dùng trả lời một thẻ */
export interface AnswerRequestDto {
    cardId: number;
    quality: 1 | 2 | 3 | 4; 
}