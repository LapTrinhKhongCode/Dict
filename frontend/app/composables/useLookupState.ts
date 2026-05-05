// composables/useLookupState.ts
import { useState } from '#app'

// State cho modal tra cứu (CŨ)
export const useLookupModalVisible = () => useState<boolean>('lookupModalVisible', () => false)

// State cho modal DỊCH (MỚI)
export const useTranslateModalVisible = () => useState<boolean>('translateModalVisible', () => false)

// State TẠM: Lưu chữ vừa bôi đen (CŨ)
export const useLookupHighlightedWord = () => useState<string>('lookupHighlightedWord', () => '')

// State TRA CỨU: "Bắn" lệnh tra cứu cho cả 2 modal (CŨ)
export const useLookupSelectedWord = () => useState<string>('lookupSelectedWord', () => '')

export const useOcrResultsState = () => useState<any[]>('ocrResults', () => [])