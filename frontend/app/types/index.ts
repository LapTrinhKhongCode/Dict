// ~/types/index.ts

/** DTO tóm tắt cho một bộ thẻ, dùng ở trang chủ */
export interface DeckSummaryDto {
    id: number;
    name: string;        // ✨ SỬA: từ 'title' thành 'name'
    description: string;
    cardCount: number;
    isPublic: boolean;
    authorName: string;  // ✨ SỬA: từ 'creatorName' thành 'authorName'
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
}

/** DTO cho request khi người dùng trả lời một thẻ */
export interface AnswerRequestDto {
    cardId: number;
    quality: 1 | 2 | 3 | 4; 
}