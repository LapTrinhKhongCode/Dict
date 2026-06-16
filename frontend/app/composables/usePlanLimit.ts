/**
 * Composable để check plan limit và hiển thị upgrade prompt
 */
export function usePlanLimit() {
  const { jwt } = useJwt()
  const config = useRuntimeConfig()

  const usage = ref<any>(null)
  const loading = ref(false)

  async function fetchUsage() {
    if (!jwt.value) return
    loading.value = true
    try {
      const res = await $fetch<any>(`${config.public.apiBaseUrl}/api/auth/usage`, {
        headers: { Authorization: `Bearer ${jwt.value}` }
      })
      usage.value = res?.result
    } catch { usage.value = null }
    finally { loading.value = false }
  }

  const isPremium = computed(() => usage.value?.isPremium === true)
  const ocrUsed = computed(() => usage.value?.ocr?.used ?? 0)
  const ocrLimit = computed(() => usage.value?.ocr?.limit ?? 20)
  const ocrUnlimited = computed(() => usage.value?.ocr?.unlimited === true)
  const maxFileMb = computed(() => usage.value?.fileSize?.maxMb ?? 10)

  function checkFileSizeClient(bytes: number): string | null {
    const maxBytes = maxFileMb.value * 1024 * 1024
    if (bytes > maxBytes) {
      return `File quá lớn. Gói ${isPremium.value ? 'Premium' : 'Free'} chỉ hỗ trợ tối đa ${maxFileMb.value}MB.` +
        (isPremium.value ? '' : ' Nâng cấp Premium để upload file đến 500MB.')
    }
    return null
  }

  function checkOcrQuotaClient(): string | null {
    if (ocrUnlimited.value) return null
    if (ocrUsed.value >= ocrLimit.value) {
      return `Bạn đã dùng hết ${ocrLimit.value} lần OCR tháng này (gói Free). Nâng cấp Premium để OCR không giới hạn.`
    }
    return null
  }

  return { usage, loading, isPremium, ocrUsed, ocrLimit, ocrUnlimited, maxFileMb, fetchUsage, checkFileSizeClient, checkOcrQuotaClient }
}
