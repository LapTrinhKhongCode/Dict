import { fileURLToPath } from 'node:url'

// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  runtimeConfig: {
    public: {
      apiBaseUrl: process.env.NUXT_PUBLIC_API_BASE_URL || 'https://localhost:7084',
      geminiApiKey: process.env.NUXT_PUBLIC_GEMINI_API_KEY || ''
    }
  },
  devtools: { enabled: true },
  css: [
    '@/assets/css/main.css',
    'pdfjs-dist/web/pdf_viewer.css', 
  ],

  nitro: {
    alias: {
      papaparse: fileURLToPath(new URL('./papaparse-mock.js', import.meta.url))
    },
    compressPublicAssets: true,
  },

  vite: {
    build: {
      rollupOptions: {
        output: {
          // Tách các thư viện nặng thành chunk riêng → browser cache hiệu quả hơn
          manualChunks: {
            'signalr': ['@microsoft/signalr'],
            'pdfjs': ['pdfjs-dist'],
            'wanakana': ['wanakana'],
          }
        }
      }
    }
  },

  icon: {
    customCollections: [{
      prefix: 'custom',
      dir: './app/assets/icons'
    }]
  },

  routeRules: {
    // Tắt SSR cho mọi trang cần auth (jwt từ localStorage → không đọc được server-side)
    '/': { ssr: false },
    '/search': { ssr: false },
    '/search/**': { ssr: false },
    '/kanji': { ssr: false },
    '/kanji/**': { ssr: false },
    '/alphabet': { ssr: false },
    '/explore': { ssr: false },
    '/explore/**': { ssr: false },
    '/reading': { ssr: false },
    '/reading/**': { ssr: false },
    '/workspaces': { ssr: false },
    '/workspaces/**': { ssr: false },
    '/projects': { ssr: false },
    '/projects/**': { ssr: false },
    '/admin': { ssr: false },
    '/admin/**': { ssr: false },
    '/reader': { ssr: false },
    '/reader/**': { ssr: false },
    '/sensei': { ssr: false },
    // /login và /confirm-account giữ SSR (trang public, không cần auth)
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