// composables/useLookupState.ts
import { useState } from '#app'

// State cho modal
export const useLookupModalVisible = () => useState<boolean>('lookupModalVisible', () => false)

// State TẠM: Lưu chữ vừa bôi đen
export const useLookupHighlightedWord = () => useState<string>('lookupHighlightedWord', () => '')

// State TRA CỨU: "Bắn" lệnh tra cứu cho modal
export const useLookupSelectedWord = () => useState<string>('lookupSelectedWord', () => '')