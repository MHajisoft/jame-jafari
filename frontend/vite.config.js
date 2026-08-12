import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { VitePWA } from 'vite-plugin-pwa'

export default defineConfig({
  plugins: [
    vue(),
    VitePWA({
      registerType: 'autoUpdate',
      includeAssets: [
        'favicon.png',
        'logo.png',
        'icons/favicon-16.png',
        'icons/favicon-32.png',
        'icons/apple-touch-icon.png',
        'icons/icon-192.png',
        'icons/icon-512.png',
        'icons/icon-512-maskable.png',
        'icons/logo-512.png',
        'icons/og-image.png'
      ],
      manifest: {
        id: '/',
        name: 'موسسه جامعه جعفری',
        short_name: 'جامعه جعفری',
        description: 'سامانه مدیریت مالی موسسه جامعه جعفری — نسخه وب‌اپ موبایل',
        theme_color: '#1a5f4a',
        background_color: '#1a5f4a',
        display: 'standalone',
        display_override: ['standalone', 'minimal-ui'],
        orientation: 'portrait-primary',
        start_url: '/?source=pwa',
        scope: '/',
        lang: 'fa',
        dir: 'rtl',
        categories: ['finance', 'business', 'productivity'],
        prefer_related_applications: false,
        icons: [
          {
            src: 'icons/icon-192.png',
            sizes: '192x192',
            type: 'image/png',
            purpose: 'any'
          },
          {
            src: 'icons/icon-512.png',
            sizes: '512x512',
            type: 'image/png',
            purpose: 'any'
          },
          {
            src: 'icons/icon-512-maskable.png',
            sizes: '512x512',
            type: 'image/png',
            purpose: 'maskable'
          }
        ]
      },
      workbox: {
        globPatterns: ['**/*.{js,css,html,ico,png,svg,woff2,woff}'],
        globIgnores: ['**/icon-1024.png', '**/og-image.png'],
        maximumFileSizeToCacheInBytes: 3 * 1024 * 1024,
        navigateFallback: '/index.html',
        navigateFallbackDenylist: [/^\/api\//, /^\/uploads\//],
        runtimeCaching: [
          {
            urlPattern: ({ url }) => url.pathname.startsWith('/api/'),
            handler: 'NetworkOnly',
            method: 'GET'
          },
          {
            urlPattern: ({ url }) => url.pathname.startsWith('/uploads/'),
            handler: 'NetworkFirst',
            options: {
              cacheName: 'uploads-cache',
              expiration: {
                maxEntries: 64,
                maxAgeSeconds: 60 * 60 * 24 * 7
              }
            }
          }
        ]
      },
      devOptions: {
        enabled: false
      }
    })
  ],
  server: {
    host: true,
    port: 5173,
    strictPort: true,
    proxy: {
      '/api': { target: 'http://localhost:5093', changeOrigin: true },
      '/uploads': { target: 'http://localhost:5093', changeOrigin: true }
    }
  }
})
