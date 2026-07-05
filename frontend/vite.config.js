import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { VitePWA } from 'vite-plugin-pwa'

export default defineConfig({
  plugins: [
    vue(),
    VitePWA({
      registerType: 'autoUpdate',
      includeAssets: ['favicon.svg'],
      manifest: {
        name: 'جامع جعفری',
        short_name: 'جامع جعفری',
        description: 'سامانه مدیریت مالی جامع جعفری',
        theme_color: '#1a5f4a',
        background_color: '#f5f5f5',
        display: 'standalone',
        lang: 'fa',
        dir: 'rtl',
        icons: [
          { src: 'favicon.svg', sizes: 'any', type: 'image/svg+xml', purpose: 'any' }
        ]
      },
      workbox: {
        globPatterns: ['**/*.{js,css,html,ico,png,svg,woff2}']
      }
    })
  ],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: 'http://localhost:5093', changeOrigin: true },
      '/uploads': { target: 'http://localhost:5093', changeOrigin: true }
    }
  }
})
