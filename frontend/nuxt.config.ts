// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  runtimeConfig: {
    public: {
      apiBaseUrl: process.env.NUXT_PUBLIC_API_BASE_URL || 'https://localhost:7084',
      geminiApiKey: process.env.NUXT_PUBLIC_GEMINI_API_KEY || 'AIzaSyDFb-3C6db9o_OKb_x3sNokNNUL7zNWOXc'
    }
  },
  devtools: { enabled: true },
  css: [
    '@/assets/css/main.css',
    'pdfjs-dist/web/pdf_viewer.css',  // CSS text layer của pdfjs
  ],
  icon: {
    customCollections: [{
      prefix: 'custom',
      dir: './app/assets/icons'
    }]
  },
  routeRules: {
    '/reader': { ssr: false },
    '/reader/**': { ssr: false },
    '/': { ssr: false },
  },
  modules: [
    '@nuxt/eslint',
    '@nuxt/fonts',
    '@nuxt/icon',
    '@nuxt/image',
    '@nuxt/scripts',
    '@nuxt/test-utils',
    '@nuxt/ui'
  ]
})