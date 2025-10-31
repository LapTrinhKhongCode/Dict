// composables/useLookupState.ts
import { useState } from '#app'

// State cho modal (cửa sổ kết quả)
export const useLookupModalVisible = () => useState<boolean>('lookupModalVisible', () => false)
export const useLookupLoading = () => useState<boolean>('lookupLoading', () => false)
export const useLookupError = () => useState<string>('lookupError', () => '')

// State cho dữ liệu tra cứu
export const useLookupSelectedWord = () => useState<string>('lookupSelectedWord', () => '')
export const useLookupApiResult = () => useState<any | null>('lookupApiResult', () => null)
export const useLookupConjugationResult = () => useState<any | null>('lookupConjugationResult', () => null)