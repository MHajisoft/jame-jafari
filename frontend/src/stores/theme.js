import { defineStore } from 'pinia'

/** @typedef {{ id: string, label: string, swatches: string[] }} ThemeMeta */

/** @type {ThemeMeta[]} */
export const THEME_OPTIONS = [
  {
    id: 'emerald',
    label: 'زمردی',
    swatches: ['#0d3d2e', '#15956f', '#d1fae5', '#ffffff']
  },
  {
    id: 'midnight',
    label: 'تیره',
    swatches: ['#141414', '#2dd4a8', '#2e2e2e', '#ececec']
  },
  {
    id: 'saffron',
    label: 'زعفرانی',
    swatches: ['#3d2818', '#d97706', '#fed7aa', '#fffcf7']
  },
  {
    id: 'slate',
    label: 'یاسی',
    swatches: ['#2e1065', '#7c3aed', '#c4b5fd', '#ffffff']
  },
  {
    id: 'plastic',
    label: 'آبی براق',
    swatches: ['#0a4f9c', '#1a8cff', '#7dd3fc', '#f7fbff']
  },
  {
    id: 'plastic-dark',
    label: 'آبی تیره',
    swatches: ['#040b16', '#38bdf8', '#12243a', '#e8f2ff']
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
  ocean: 'slate',
  azure: 'plastic',
  navy: 'plastic-dark'
}

const DEFAULT_THEME = 'emerald'

/** Status-bar / PWA theme-color per theme */
const THEME_COLORS = {
  emerald: '#15956f',
  midnight: '#1c1c1c',
  saffron: '#3d2818',
  slate: '#2e1065',
  plastic: '#1a8cff',
  'plastic-dark': '#07111f'
}

function resolveTheme(raw) {
  if (!raw) return DEFAULT_THEME
  if (THEMES.includes(raw)) return raw
  return LEGACY_THEME_MAP[raw] || DEFAULT_THEME
}

function applyThemeColor(themeId) {
  const color = THEME_COLORS[themeId] || THEME_COLORS[DEFAULT_THEME]
  document.querySelectorAll('meta[name="theme-color"]').forEach((el) => {
    el.setAttribute('content', color)
  })
}

function applyStandaloneClass() {
  const standalone =
    window.matchMedia('(display-mode: standalone)').matches ||
    window.navigator.standalone === true
  document.documentElement.classList.toggle('pwa-standalone', standalone)
  document.body.classList.toggle('pwa-standalone', standalone)
}

export const useThemeStore = defineStore('theme', {
  state: () => ({
    theme: resolveTheme(localStorage.getItem('theme'))
  }),
  getters: {
    currentMeta: (s) => THEME_OPTIONS.find((t) => t.id === s.theme) || THEME_OPTIONS[0],
    themeColor: (s) => THEME_COLORS[s.theme] || THEME_COLORS[DEFAULT_THEME]
  },
  actions: {
    setTheme(theme) {
      const next = resolveTheme(theme)
      this.theme = next
      localStorage.setItem('theme', next)
      document.documentElement.setAttribute('data-theme', next)
      applyThemeColor(next)
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
      applyThemeColor(next)
      applyStandaloneClass()
    }
  }
})
