import { defineStore } from 'pinia'

export const THEMES = ['light', 'dark', 'forest', 'lemon', 'shirazi', 'gold', 'ocean']

export const useThemeStore = defineStore('theme', {
  state: () => ({
    theme: localStorage.getItem('theme') || 'light',
    dateCulture: localStorage.getItem('dateCulture') || 'jalali'
  }),
  actions: {
    setTheme(theme) {
      if (!THEMES.includes(theme)) return
      this.theme = theme
      localStorage.setItem('theme', theme)
      document.documentElement.setAttribute('data-theme', theme)
    },
    setDateCulture(culture) {
      this.dateCulture = culture
      localStorage.setItem('dateCulture', culture)
    },
    init() {
      document.documentElement.setAttribute('data-theme', this.theme)
      document.documentElement.setAttribute('dir', 'rtl')
      document.documentElement.setAttribute('lang', 'fa')
    }
  }
})
