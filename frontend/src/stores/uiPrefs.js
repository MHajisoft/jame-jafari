import { defineStore } from 'pinia'
import {
  CURRENCY_DISPLAY_OPTIONS,
  currencyUnitLabel,
  resolveCurrencyUnit
} from '../utils/currency'

/** @typedef {'sheet' | 'modal'} DatePickerMobileMode */
/** @typedef {import('../utils/currency').CurrencyDisplayUnit} CurrencyDisplayUnit */
/** @typedef {'icon' | 'text' | 'icon-text'} TableLabelMode */
/** @typedef {TableLabelMode} MessengerLabelMode */

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

/** How symbolic table cells (messenger / type / status) are shown. */
export const TABLE_LABEL_MODES = [
  {
    id: 'icon',
    label: 'فقط آیکون',
    hint: 'برچسب با نگه داشتن روی آیکون دیده می‌شود'
  },
  {
    id: 'icon-text',
    label: 'آیکون و متن',
    hint: 'آیکون همراه برچسب'
  },
  {
    id: 'text',
    label: 'فقط متن',
    hint: 'بدون آیکون'
  }
]
/** @deprecated Use TABLE_LABEL_MODES */
export const MESSENGER_LABEL_MODES = TABLE_LABEL_MODES

const DATE_PICKER_STORAGE_KEY = 'ui.datePickerMobileMode'
const CURRENCY_UNIT_STORAGE_KEY = 'ui.currencyDisplayUnit'
const TABLE_LABEL_STORAGE_KEY = 'ui.tableLabelMode'
const MESSENGER_LABEL_STORAGE_KEY = 'ui.messengerLabelMode'
const DEFAULT_MODE = 'sheet'
const DEFAULT_TABLE_LABEL_MODE = 'icon'

function resolveMode(raw) {
  if (raw === 'sheet' || raw === 'modal') return raw
  if (raw === 'action-bar' || raw === 'bar') return 'sheet'
  return DEFAULT_MODE
}

function resolveTableLabelMode(raw) {
  if (raw === 'icon' || raw === 'text' || raw === 'icon-text') return raw
  return DEFAULT_TABLE_LABEL_MODE
}

function readStoredTableLabelMode() {
  return resolveTableLabelMode(
    localStorage.getItem(TABLE_LABEL_STORAGE_KEY) ?? localStorage.getItem(MESSENGER_LABEL_STORAGE_KEY)
  )
}

export const useUiPrefsStore = defineStore('uiPrefs', {
  state: () => ({
    /** @type {DatePickerMobileMode} */
    datePickerMobileMode: resolveMode(localStorage.getItem(DATE_PICKER_STORAGE_KEY)),
    /** @type {CurrencyDisplayUnit} */
    currencyDisplayUnit: resolveCurrencyUnit(localStorage.getItem(CURRENCY_UNIT_STORAGE_KEY)),
    /** @type {TableLabelMode} */
    tableLabelMode: readStoredTableLabelMode()
  }),
  getters: {
    datePickerMobileModeMeta: (s) =>
      DATE_PICKER_MOBILE_MODES.find((m) => m.id === s.datePickerMobileMode) || DATE_PICKER_MOBILE_MODES[0],
    currencyDisplayUnitMeta: (s) =>
      CURRENCY_DISPLAY_OPTIONS.find((o) => o.id === s.currencyDisplayUnit) || CURRENCY_DISPLAY_OPTIONS[0],
    currencyUnitLabel: (s) => currencyUnitLabel(s.currencyDisplayUnit),
    tableLabelModeMeta: (s) =>
      TABLE_LABEL_MODES.find((m) => m.id === s.tableLabelMode) || TABLE_LABEL_MODES[0],
    /** @deprecated Use tableLabelMode */
    messengerLabelMode: (s) => s.tableLabelMode,
    messengerLabelModeMeta: (s) =>
      TABLE_LABEL_MODES.find((m) => m.id === s.tableLabelMode) || TABLE_LABEL_MODES[0],
    showTableIcon: (s) => s.tableLabelMode !== 'text',
    showTableText: (s) => s.tableLabelMode !== 'icon',
    showMessengerIcon: (s) => s.tableLabelMode !== 'text',
    showMessengerText: (s) => s.tableLabelMode !== 'icon'
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
    setTableLabelMode(mode) {
      const next = resolveTableLabelMode(mode)
      this.tableLabelMode = next
      localStorage.setItem(TABLE_LABEL_STORAGE_KEY, next)
      localStorage.removeItem(MESSENGER_LABEL_STORAGE_KEY)
    },
    /** @deprecated Use setTableLabelMode */
    setMessengerLabelMode(mode) {
      this.setTableLabelMode(mode)
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

      const tableMode = resolveTableLabelMode(this.tableLabelMode)
      if (tableMode !== this.tableLabelMode) {
        this.tableLabelMode = tableMode
      }
      localStorage.setItem(TABLE_LABEL_STORAGE_KEY, tableMode)
      localStorage.removeItem(MESSENGER_LABEL_STORAGE_KEY)
    }
  }
})
