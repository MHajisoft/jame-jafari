import { defineStore } from 'pinia'
import {
  CURRENCY_DISPLAY_OPTIONS,
  currencyUnitLabel,
  resolveCurrencyUnit
} from '../utils/currency'

/** @typedef {'sheet' | 'modal'} DatePickerMobileMode */
/** @typedef {import('../utils/currency').CurrencyDisplayUnit} CurrencyDisplayUnit */

export { CURRENCY_DISPLAY_OPTIONS }

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

const DATE_PICKER_STORAGE_KEY = 'ui.datePickerMobileMode'
const CURRENCY_UNIT_STORAGE_KEY = 'ui.currencyDisplayUnit'
const DEFAULT_MODE = 'sheet'

function resolveMode(raw) {
  if (raw === 'sheet' || raw === 'modal') return raw
  if (raw === 'action-bar' || raw === 'bar') return 'sheet'
  return DEFAULT_MODE
}

export const useUiPrefsStore = defineStore('uiPrefs', {
  state: () => ({
    /** @type {DatePickerMobileMode} */
    datePickerMobileMode: resolveMode(localStorage.getItem(DATE_PICKER_STORAGE_KEY)),
    /** @type {CurrencyDisplayUnit} */
    currencyDisplayUnit: resolveCurrencyUnit(localStorage.getItem(CURRENCY_UNIT_STORAGE_KEY))
  }),
  getters: {
    datePickerMobileModeMeta: (s) =>
      DATE_PICKER_MOBILE_MODES.find((m) => m.id === s.datePickerMobileMode) || DATE_PICKER_MOBILE_MODES[0],
    currencyDisplayUnitMeta: (s) =>
      CURRENCY_DISPLAY_OPTIONS.find((o) => o.id === s.currencyDisplayUnit) || CURRENCY_DISPLAY_OPTIONS[0],
    currencyUnitLabel: (s) => currencyUnitLabel(s.currencyDisplayUnit)
  },
  actions: {
    setDatePickerMobileMode(mode) {
      const next = resolveMode(mode)
      this.datePickerMobileMode = next
      localStorage.setItem(DATE_PICKER_STORAGE_KEY, next)
    },
    setCurrencyDisplayUnit(unit) {
      const next = resolveCurrencyUnit(unit)
      this.currencyDisplayUnit = next
      localStorage.setItem(CURRENCY_UNIT_STORAGE_KEY, next)
    },
    init() {
      const dateMode = resolveMode(this.datePickerMobileMode)
      if (dateMode !== this.datePickerMobileMode) {
        this.datePickerMobileMode = dateMode
      }
      localStorage.setItem(DATE_PICKER_STORAGE_KEY, dateMode)

      const currencyUnit = resolveCurrencyUnit(this.currencyDisplayUnit)
      if (currencyUnit !== this.currencyDisplayUnit) {
        this.currencyDisplayUnit = currencyUnit
      }
      localStorage.setItem(CURRENCY_UNIT_STORAGE_KEY, currencyUnit)
    }
  }
})
