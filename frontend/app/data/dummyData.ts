// src/data/dummyData.ts

// Định nghĩa kiểu dữ liệu dùng chung
export type Card = {
  id: number;
  charBig: string;
  charSmall: string;
  pinyin: string;
  meaning: string;
  nextReviewAt: number;
};

export type FlashcardSet = {
  id: number;
  title: string;
  creator: {
    name: string;
    avatarUrl: string;
  };
  views: number;
  cards: Card[];
};

// Dữ liệu giả
export const allSets: FlashcardSet[] = [
  {
    id: 1,
    title: 'N1-Mimikara - new',
    creator: {
      name: 'Minh Hà',
      avatarUrl: 'https://i.pravatar.cc/40?u=minhha'
    },
    views: 24609,
    cards: [
      { id: 101, charBig: '挨拶', charSmall: '挨拶', pinyin: 'aisatsu', meaning: 'Chào hỏi', nextReviewAt: 0 },
      { id: 102, charBig: '勘定', charSmall: '勘定', pinyin: 'kanjou', meaning: 'Thanh toán, tính toán', nextReviewAt: 0 },
      { id: 103, charBig: '規律', charSmall: '規律', pinyin: 'kiritsu', meaning: 'Quy luật, kỷ luật', nextReviewAt: 0 },
      { id: 104, charBig: '香水', charSmall: '香水', pinyin: 'kousui', meaning: 'Nước hoa', nextReviewAt: 0 },
      { id: 105, charBig: '残酷', charSmall: '残酷', pinyin: 'zankoku', meaning: 'Tàn khốc, dã man', nextReviewAt: 0 },
    ]
  },
  {
    id: 2,
    title: 'Genki I - Vocabulary',
    creator: {
      name: 'Anh Tuấn',
      avatarUrl: 'https://i.pravatar.cc/40?u=anhtuan'
    },
    views: 15234,
    cards: [
      { id: 201, charBig: '私', charSmall: '私', pinyin: 'watashi', meaning: 'Tôi', nextReviewAt: 0 },
      { id: 202, charBig: '学生', charSmall: '学生', pinyin: 'gakusei', meaning: 'Học sinh, sinh viên', nextReviewAt: 0 },
      { id: 203, charBig: '先生', charSmall: '先生', pinyin: 'sensei', meaning: 'Giáo viên', nextReviewAt: 0 },
      { id: 204, charBig: '大学', charSmall: '大学', pinyin: 'daigaku', meaning: 'Đại học', nextReviewAt: 0 },
    ]
  },
  {
    id: 3,
    title: 'JLPT N5 Kanji',
    creator: {
      name: 'Yuki',
      avatarUrl: 'https://i.pravatar.cc/40?u=yuki'
    },
    views: 89750,
    cards: [
      { id: 301, charBig: '人', charSmall: '人', pinyin: 'hito', meaning: 'Người', nextReviewAt: 0 },
      { id: 302, charBig: '日', charSmall: '日', pinyin: 'hi/nichi', meaning: 'Ngày, Mặt trời', nextReviewAt: 0 },
      { id: 303, charBig: '月', charSmall: '月', pinyin: 'tsuki/getsu', meaning: 'Tháng, Mặt trăng', nextReviewAt: 0 },
      { id: 304, charBig: '火', charSmall: '火', pinyin: 'hi/ka', meaning: 'Lửa', nextReviewAt: 0 },
      { id: 305, charBig: '水', charSmall: '水', pinyin: 'mizu/sui', meaning: 'Nước', nextReviewAt: 0 },
      { id: 306, charBig: '木', charSmall: '木', pinyin: 'ki/moku', meaning: 'Cây', nextReviewAt: 0 },
      { id: 307, charBig: '金', charSmall: '金', pinyin: 'kane/kin', meaning: 'Vàng, Tiền', nextReviewAt: 0 },
      { id: 308, charBig: '土', charSmall: '土', pinyin: 'tsuchi/do', meaning: 'Đất', nextReviewAt: 0 },
    ]
  }
];
