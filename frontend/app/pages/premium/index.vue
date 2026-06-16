<template>
  <div class="min-h-screen bg-gray-50 text-gray-900 dark:bg-gray-900 dark:text-white p-4 sm:p-8 transition-colors">
    <div class="max-w-5xl mx-auto">

      <!-- Tab selector -->
      <div class="flex items-center justify-center gap-2 mb-10">
        <button @click="activeTab = 'personal'"
          :class="['px-6 py-2.5 rounded-xl font-semibold text-sm transition-all', activeTab === 'personal' ? 'bg-indigo-600 text-white shadow-lg' : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 border border-gray-200 dark:border-gray-700']">
          🏠 Cá nhân
        </button>
        <button @click="activeTab = 'org'"
          :class="['px-6 py-2.5 rounded-xl font-semibold text-sm transition-all', activeTab === 'org' ? 'bg-indigo-600 text-white shadow-lg' : 'bg-white dark:bg-gray-800 text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700 border border-gray-200 dark:border-gray-700']">
          🏢 Tổ chức
        </button>
      </div>

      <!-- ═══ TAB CÁ NHÂN ═══ -->
      <div v-if="activeTab === 'personal'">
        <div v-if="meLoading" class="flex justify-center mt-10">
          <div class="w-10 h-10 border-4 border-gray-200 border-t-indigo-500 rounded-full animate-spin"/>
        </div>

        <!-- Đã Premium -->
        <div v-else-if="isPremium" class="flex flex-col items-center text-center space-y-4">
          <div class="text-6xl">✨</div>
          <h1 class="text-4xl font-extrabold bg-gradient-to-r from-yellow-400 to-amber-600 bg-clip-text text-transparent">Premium đang hoạt động</h1>
          <div class="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-2xl p-6 w-full max-w-sm text-left">
            <p class="text-xs uppercase tracking-wider text-gray-400 mb-3">Plan cá nhân</p>
            <div class="flex items-center justify-between mb-4">
              <span class="text-xl font-bold text-indigo-600 dark:text-indigo-400">Premium</span>
              <span class="text-xs bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400 px-2.5 py-1 rounded-full font-semibold">● Đang hoạt động</span>
            </div>
            <div class="space-y-2 text-sm text-gray-600 dark:text-gray-400">
              <div class="flex items-center gap-2"><span class="text-green-500">✓</span> OCR không giới hạn (workspace cá nhân)</div>
              <div class="flex items-center gap-2"><span class="text-green-500">✓</span> Upload file đến 500MB</div>
              <div class="flex items-center gap-2"><span class="text-green-500">✓</span> AI giải thích nâng cao</div>
            </div>
            <div class="mt-4 pt-4 border-t border-gray-100 dark:border-gray-700 text-xs text-gray-400">
              Hết hạn: <span class="font-semibold text-gray-600 dark:text-gray-300">{{ premiumExpiry ?? 'Trọn đời' }}</span>
            </div>
          </div>
          <div class="flex gap-3">
            <button @click="navigateTo('/')" class="px-6 py-2.5 bg-indigo-600 hover:bg-indigo-700 text-white font-bold rounded-xl transition-colors text-sm">Trải nghiệm ngay</button>
            <button @click="openPortal" :disabled="portalLoading" class="px-6 py-2.5 border border-gray-300 dark:border-gray-600 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-xl transition-colors text-sm font-medium">
              {{ portalLoading ? 'Đang mở...' : 'Huỷ / Quản lý subscription' }}
            </button>
          </div>
          <p class="text-xs text-gray-400">Hủy subscription bất kỳ lúc nào từ trang quản lý billing.</p>
        </div>

        <!-- Chưa Premium -->
        <div v-else class="flex flex-col items-center">
          <h2 class="text-4xl font-extrabold text-center mb-3">
            Nâng cấp để trải nghiệm
            <span class="bg-gradient-to-r from-blue-400 to-purple-600 bg-clip-text text-transparent">không giới hạn</span>
          </h2>
          <p class="text-gray-500 dark:text-gray-400 mb-8 text-center text-sm max-w-md">
            Dùng riêng cho workspace cá nhân. Dùng thử 7 ngày miễn phí.
          </p>
          <div class="flex flex-col sm:flex-row gap-6">
            <div class="relative bg-white dark:bg-gray-800 border-2 border-indigo-500 rounded-2xl p-8 flex flex-col items-center text-center w-72 shadow-xl hover:scale-105 transition-transform cursor-pointer" @click="checkout('monthly')">
              <span class="text-sm font-semibold text-indigo-500 uppercase mb-3">Hàng tháng</span>
              <span class="text-5xl font-extrabold text-gray-900 dark:text-white">$9<span class="text-2xl">.99</span></span>
              <span class="text-gray-400 text-sm mt-1 mb-6">/tháng</span>
              <ul class="text-left text-sm text-gray-600 dark:text-gray-400 space-y-2 mb-8 w-full">
                <li>✅ OCR không giới hạn</li><li>✅ Upload file 500MB</li><li>✅ AI nâng cao</li>
              </ul>
              <button class="w-full bg-indigo-600 hover:bg-indigo-700 text-white font-bold py-3 rounded-xl" :disabled="loading === 'monthly'">
                {{ loading === 'monthly' ? 'Đang chuyển...' : 'Bắt đầu dùng thử' }}
              </button>
            </div>
            <div class="relative bg-gradient-to-br from-indigo-600 to-purple-700 rounded-2xl p-8 flex flex-col items-center text-center w-72 shadow-xl hover:scale-105 transition-transform cursor-pointer" @click="checkout('yearly')">
              <div class="absolute -top-3 bg-yellow-400 text-gray-900 text-xs font-bold px-3 py-1 rounded-full">TIẾT KIỆM 30%</div>
              <span class="text-sm font-semibold text-indigo-200 uppercase mb-3">Hàng năm</span>
              <span class="text-5xl font-extrabold text-white">$84<span class="text-2xl">/năm</span></span>
              <span class="text-indigo-200 text-sm mt-1 mb-6">= $7/tháng</span>
              <ul class="text-left text-sm text-indigo-100 space-y-2 mb-8 w-full">
                <li>✅ Tất cả tính năng Monthly</li><li>✅ Ưu tiên hỗ trợ</li><li>✅ 2 tháng miễn phí</li>
              </ul>
              <button class="w-full bg-white text-indigo-700 hover:bg-indigo-50 font-bold py-3 rounded-xl" :disabled="loading === 'yearly'">
                {{ loading === 'yearly' ? 'Đang chuyển...' : 'Chọn gói năm' }}
              </button>
            </div>
          </div>
          <p v-if="errorMsg" class="text-red-500 mt-6">{{ errorMsg }}</p>
          <p class="text-gray-400 text-xs mt-6">Thanh toán an toàn qua Stripe. Hủy bất kỳ lúc nào.</p>
        </div>
      </div>

      <!-- ═══ TAB TỔ CHỨC ═══ -->
      <div v-if="activeTab === 'org'">
        <h2 class="text-4xl font-extrabold text-center mb-3">
          Plan cho <span class="bg-gradient-to-r from-indigo-400 to-purple-600 bg-clip-text text-transparent">Tổ chức</span>
        </h2>
        <p class="text-gray-500 dark:text-gray-400 mb-8 text-center text-sm max-w-md mx-auto">
          Khi tổ chức nâng cấp, <b>tất cả thành viên</b> trong workspace tổ chức được dùng không giới hạn.
        </p>

        <!-- Org selector -->
        <div v-if="myOrgs.length > 0" class="flex justify-center mb-8">
          <select v-model="selectedOrgId" class="bg-white dark:bg-gray-800 border border-gray-300 dark:border-gray-600 rounded-xl px-4 py-2.5 text-sm font-medium focus:outline-none focus:border-indigo-500">
            <option :value="null" disabled>-- Chọn tổ chức --</option>
            <option v-for="org in myOrgs" :key="org.id" :value="org.id">
              {{ org.name }} ({{ org.myRole }}) — Plan hiện tại: {{ org.orgPlan }}
            </option>
          </select>
        </div>
        <div v-else class="text-center text-gray-400 mb-8 text-sm">
          Bạn chưa có tổ chức nào. <NuxtLink to="/org" class="text-indigo-500 hover:underline">Tạo tổ chức</NuxtLink>
        </div>

        <!-- Org plan cards -->
        <div class="flex flex-col sm:flex-row gap-6 justify-center">
          <!-- Team -->
          <div class="bg-white dark:bg-gray-800 border-2 border-green-500 rounded-2xl p-8 w-72 flex flex-col">
            <span class="text-sm font-bold text-green-600 uppercase mb-3">Team</span>
            <span class="text-5xl font-extrabold text-gray-900 dark:text-white mb-1">$29<span class="text-2xl">/tháng</span></span>
            <span class="text-gray-400 text-xs mb-6">Cho toàn bộ tổ chức</span>
            <ul class="text-sm text-gray-600 dark:text-gray-400 space-y-2 mb-8 flex-1">
              <li>✅ OCR không giới hạn — cả team</li>
              <li>✅ Upload file 500MB — cả team</li>
              <li>✅ Tạo nhiều workspace tổ chức</li>
              <li>✅ Member không cần mua riêng</li>
            </ul>
            <div v-if="selectedOrgPlan === 'TEAM'" class="text-center text-green-600 font-semibold text-sm mb-3">● Đang dùng plan này</div>
            <button v-else @click="orgCheckout('team')" :disabled="!selectedOrgId || orgLoading === 'team'"
              class="w-full bg-green-600 hover:bg-green-700 disabled:opacity-50 text-white font-bold py-3 rounded-xl transition-colors">
              {{ orgLoading === 'team' ? 'Đang chuyển...' : (selectedOrgId ? 'Nâng cấp Team' : 'Chọn tổ chức trước') }}
            </button>
          </div>

          <!-- Enterprise -->
          <div class="bg-gradient-to-br from-gray-900 to-gray-800 border-2 border-purple-500 rounded-2xl p-8 w-72 flex flex-col">
            <span class="text-sm font-bold text-purple-400 uppercase mb-3">Enterprise</span>
            <span class="text-5xl font-extrabold text-white mb-1">$99<span class="text-2xl">/tháng</span></span>
            <span class="text-gray-400 text-xs mb-6">Unlimited everything</span>
            <ul class="text-sm text-gray-300 space-y-2 mb-8 flex-1">
              <li>✅ Tất cả tính năng Team</li>
              <li>✅ Audit log</li>
              <li>✅ Ưu tiên hỗ trợ 24/7</li>
              <li>✅ SLA guarantee</li>
            </ul>
            <div v-if="selectedOrgPlan === 'ENTERPRISE'" class="text-center text-purple-400 font-semibold text-sm mb-3">● Đang dùng plan này</div>
            <button v-else @click="orgCheckout('enterprise')" :disabled="!selectedOrgId || orgLoading === 'enterprise'"
              class="w-full bg-purple-600 hover:bg-purple-700 disabled:opacity-50 text-white font-bold py-3 rounded-xl transition-colors">
              {{ orgLoading === 'enterprise' ? 'Đang chuyển...' : (selectedOrgId ? 'Nâng cấp Enterprise' : 'Chọn tổ chức trước') }}
            </button>
          </div>
        </div>

        <!-- Manage existing subscription -->
        <div v-if="selectedOrgId && selectedOrgPlan !== 'FREE'" class="flex justify-center mt-6">
          <button @click="openOrgPortal" :disabled="orgPortalLoading"
            class="px-6 py-2 border border-gray-300 dark:border-gray-600 hover:bg-gray-100 dark:hover:bg-gray-800 rounded-xl text-sm font-medium transition-colors">
            {{ orgPortalLoading ? 'Đang mở...' : 'Huỷ / Quản lý subscription tổ chức' }}
          </button>
        </div>

        <p v-if="orgErrorMsg" class="text-red-500 text-center mt-4 text-sm">{{ orgErrorMsg }}</p>
        <p class="text-gray-400 text-xs text-center mt-6">Thanh toán qua Stripe. Hủy bất kỳ lúc nào.</p>
      </div>

    </div>
  </div>
</template>

<script setup lang="ts">
definePageMeta({ middleware: 'auth-client', ssr: false })

const { jwt } = useJwt()
const config = useRuntimeConfig()

const activeTab = ref<'personal' | 'org'>('personal')
const loading = ref<'monthly' | 'yearly' | null>(null)
const portalLoading = ref(false)
const errorMsg = ref('')
const meLoading = ref(true)
const meData = ref<any>(null)

// Org state
const myOrgs = ref<any[]>([])
const selectedOrgId = ref<number | null>(null)
const orgLoading = ref<'team' | 'enterprise' | null>(null)
const orgPortalLoading = ref(false)
const orgErrorMsg = ref('')

const selectedOrgPlan = computed(() =>
  myOrgs.value.find(o => o.id === selectedOrgId.value)?.orgPlan ?? 'FREE'
)

async function fetchMe() {
  if (!jwt.value) return
  meLoading.value = true
  try {
    const res = await $fetch<any>(`${config.public.apiBaseUrl}/api/auth/me`, {
      headers: { Authorization: `Bearer ${jwt.value}` }
    })
    meData.value = res?.result ?? null
  } catch { meData.value = null }
  finally { meLoading.value = false }
}

async function fetchOrgs() {
  if (!jwt.value) return
  try {
    const res = await $fetch<any>(`${config.public.apiBaseUrl}/api/organizations/my`, {
      headers: { Authorization: `Bearer ${jwt.value}` }
    })
    myOrgs.value = res?.result ?? []
    // Auto-select first org user is OWNER/ADMIN of
    const adminOrg = myOrgs.value.find(o => o.myRole === 'OWNER' || o.myRole === 'ADMIN')
    if (adminOrg) selectedOrgId.value = adminOrg.id
  } catch { myOrgs.value = [] }
}

const isPremium = computed(() => meData.value?.isPremiumActive === true)
const premiumExpiry = computed(() => {
  if (!meData.value?.premiumExpiresAt) return null
  return new Date(meData.value.premiumExpiresAt).toLocaleDateString('vi-VN')
})

async function checkout(plan: 'monthly' | 'yearly') {
  loading.value = plan
  errorMsg.value = ''
  try {
    const priceId = plan === 'monthly' ? config.public.stripePriceMonthly : config.public.stripePriceYearly
    const res = await $fetch<any>(`${config.public.apiBaseUrl}/api/stripe/create-checkout`, {
      method: 'POST', headers: { Authorization: `Bearer ${jwt.value}`, 'Content-Type': 'application/json' },
      body: { priceId },
    })
    window.location.href = res.result.url
  } catch { errorMsg.value = 'Có lỗi xảy ra, vui lòng thử lại.'; loading.value = null }
}

async function orgCheckout(plan: 'team' | 'enterprise') {
  if (!selectedOrgId.value) return
  orgLoading.value = plan
  orgErrorMsg.value = ''
  try {
    const res = await $fetch<any>(`${config.public.apiBaseUrl}/api/stripe/create-org-checkout`, {
      method: 'POST', headers: { Authorization: `Bearer ${jwt.value}`, 'Content-Type': 'application/json' },
      body: { orgId: selectedOrgId.value, plan },
    })
    window.location.href = res.result.url
  } catch (e: any) {
    orgErrorMsg.value = e?.data?.message || 'Có lỗi xảy ra.'
    orgLoading.value = null
  }
}

async function openPortal() {
  portalLoading.value = true
  try {
    const res = await $fetch<any>(`${config.public.apiBaseUrl}/api/stripe/portal`, {
      method: 'POST', headers: { Authorization: `Bearer ${jwt.value}` }
    })
    window.location.href = res.result.url
  } catch { errorMsg.value = 'Không thể mở trang quản lý billing.' }
  finally { portalLoading.value = false }
}

async function openOrgPortal() {
  if (!selectedOrgId.value) return
  orgPortalLoading.value = true
  try {
    const res = await $fetch<any>(`${config.public.apiBaseUrl}/api/stripe/org-portal/${selectedOrgId.value}`, {
      method: 'POST', headers: { Authorization: `Bearer ${jwt.value}` }
    })
    window.location.href = res.result.url
  } catch { orgErrorMsg.value = 'Không thể mở trang quản lý billing.' }
  finally { orgPortalLoading.value = false }
}

onMounted(() => { fetchMe(); fetchOrgs() })
</script>