import { defineStore } from 'pinia'

/** @typedef {{ id: string, label: string, description: string, swatches: string[] }} ThemeMeta */

/** @type {ThemeMeta[]} */
export const THEME_OPTIONS = [
  {
    id: 'emerald',
    label: 'زمردی',
    description: 'تم سازمانی پیش‌فرض؛ سبز آرام و خوانا',
    swatches: ['#143f33', '#1b6b52', '#f3f6f4', '#ffffff']
  },
  {
    id: 'midnight',
    label: 'شب',
    description: 'حالت تاریک با کنتراست مناسب برای شب',
    swatches: ['#080e1a', '#3dcf9a', '#0b1220', '#152238']
  },
  {
    id: 'saffron',
    label: 'زعفرانی',
    description: 'فضای گرم سنگی با تاکید کهربایی',
    swatches: ['#3b2a1d', '#b45309', '#f7f1e8', '#fffaf3']
  },
  {
    id: 'slate',
    label: 'خاکستری',
    description: 'ظاهر مدرن خنثی با آبی سرمه‌ای',
    swatches: ['#1a2740', '#1e3a5f', '#eef2f6', '#ffffff']
  }
]

export const THEMES = THEME_OPTIONS.map((t) => t.id)

const LEGACY_THEME_MAP = {
  light: 'emerald',
  forest: 'emerald',
  dark: 'midnight',
  lemon: 'saffron',
  gold: 'saffron',
  shirazi: 'saffron',
  ocean: 'slate'
}

const DEFAULT_THEME = 'emerald'

function resolveTheme(raw) {
  if (!raw) return DEFAULT_THEME
  if (THEMES.includes(raw)) return raw
  return LEGACY_THEME_MAP[raw] || DEFAULT_THEME
}

export const useThemeStore = defineStore('theme', {
  state: () => ({
    theme: resolveTheme(localStorage.getItem('theme'))
  }),
  getters: {
    currentMeta: (s) => THEME_OPTIONS.find((t) => t.id === s.theme) || THEME_OPTIONS[0]
  },
  actions: {
    setTheme(theme) {
      const next = resolveTheme(theme)
      this.theme = next
      localStorage.setItem('theme', next)
      document.documentElement.setAttribute('data-theme', next)
    },
    init() {
      const next = resolveTheme(this.theme)
      if (next !== this.theme || localStorage.getItem('theme') !== next) {
        this.theme = next
        localStorage.setItem('theme', next)
      }
      document.documentElement.setAttribute('data-theme', next)
      document.documentElement.setAttribute('dir', 'rtl')
      document.documentElement.setAttribute('lang', 'fa')
    }
  }
})
