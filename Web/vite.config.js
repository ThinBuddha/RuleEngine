import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  root: process.cwd(),
  cacheDir: './.vite',
  plugins: [vue()],
  base: './',
  server: {
    proxy: {
      '/api': {
        target: process.env.RULES_API_URL || 'http://localhost:5215',
        changeOrigin: true,
        ws: true,
      },
    },
  },
})
