import { defineStore } from 'pinia'

/** @typedef {'sheet' | 'modal'} DatePickerMobileMode */

/** Mobile date-picker presentation options (Settings → mobile only). */
export const DATE_PICKER_MOBILE_MODES = [
  {
    id: 'sheet',
    label: 'نوار پایین',
    hint: 'چرخاننده تاریخ از پایین صفحه'
  },
  {
    id: 'modal',
    label: 'مودال',
    hint: 'تقویم در پنجره وسط صفحه'
  }
]

const STORAGE_KEY = 'ui.datePickerMobileMode'
const DEFAULT_MODE = 'sheet'

function resolveMode(raw) {
  if (raw === 'sheet' || raw === 'modal') return raw
  if (raw === 'action-bar' || raw === 'bar') return 'sheet'
  return DEFAULT_MODE
}

export const useUiPrefsStore = defineStore('uiPrefs', {
  state: () => ({
    /** @type {DatePickerMobileMode} */
    datePickerMobileMode: resolveMode(localStorage.getItem(STORAGE_KEY))
  }),
  getters: {
    datePickerMobileModeMeta: (s) =>
      DATE_PICKER_MOBILE_MODES.find((m) => m.id === s.datePickerMobileMode) || DATE_PICKER_MOBILE_MODES[0]
  },
  actions: {
    setDatePickerMobileMode(mode) {
      const next = resolveMode(mode)
      this.datePickerMobileMode = next
      localStorage.setItem(STORAGE_KEY, next)
    },
    init() {
      const next = resolveMode(this.datePickerMobileMode)
      if (next !== this.datePickerMobileMode) {
        this.datePickerMobileMode = next
      }
      localStorage.setItem(STORAGE_KEY, next)
    }
  }
})
