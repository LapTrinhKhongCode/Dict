// types/kanji.ts
export type JishoData = {
  kunyomi?: string
  meaning?: string
  onyomi?: string[]
}

export type KanjiInfo = {
  id: string
  jishoData?: JishoData
  [k: string]: unknown
}
