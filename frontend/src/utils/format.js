import dayjs from 'dayjs'
import jalaliday from 'jalaliday'
import { formatJalali } from './jalali'

export {
  genders,
  paymentTypes,
  transactionDirections,
  generalTypeCategories,
  enumLabel,
  enumValue,
  genderLabel,
  paymentTypeLabel,
  transactionDirectionLabel,
  generalTypeCategoryLabel
} from './enums'

dayjs.extend(jalaliday)

export function formatDate(date) {
  if (!date) return ''
  return formatJalali(date, 'YYYY/MM/DD')
}

export function formatDateTime(date) {
  if (!date) return ''
  const jalali = dayjs(date).calendar('jalali').locale('fa').format('YYYY/MM/DD')
  const time = dayjs(date).format('HH:mm')
  return `${jalali} ${time}`
}

export function toInputDate(date) {
  return dayjs(date).format('YYYY-MM-DD')
}

export function formatMoney(amount) {
  return new Intl.NumberFormat('fa-IR').format(amount || 0)
}

/** Convert Persian/Arabic digits to Western digits. */
export function toEnglishDigits(value) {
  return String(value ?? '')
    .replace(/[۰-۹]/g, (d) => '۰۱۲۳۴۵۶۷۸۹'.indexOf(d))
    .replace(/[٠-٩]/g, (d) => '٠١٢٣٤٥٦٧٨٩'.indexOf(d))
}

/** Digits-only string from a currency field (supports separators & Persian digits). */
export function parseCurrencyInput(value) {
  const digits = toEnglishDigits(value)
    .replace(/[٬،,]/g, '')
    .replace(/[^\d]/g, '')
  return digits.replace(/^0+(?=\d)/, '')
}

/** Format a number/string with thousand separators for currency inputs. */
export function formatCurrencyInput(value) {
  if (value === '' || value === null || value === undefined) return ''
  const digits = parseCurrencyInput(value)
  if (!digits) return ''
  return digits.replace(/\B(?=(\d{3})+(?!\d))/g, ',')
}

/** Server upload path → browser URL */
export function documentUrl(path) {
  const p = String(path || '').trim()
  if (!p) return ''
  if (p.startsWith('http://') || p.startsWith('https://') || p.startsWith('/')) return p
  return `/uploads/${p}`
}

export function documentFileName(path) {
  const p = String(path || '').trim()
  if (!p) return 'پیوست'
  return p.split('/').pop() || 'پیوست'
}

export function isImageDocument(path) {
  return /\.(jpe?g|png|gif|webp)$/i.test(String(path || ''))
}

export function isPdfDocument(path, mime = '') {
  if (mime === 'application/pdf') return true
  return /\.pdf$/i.test(String(path || ''))
}

/** @returns {'image'|'pdf'|'file'} */
export function documentKind(path, mime = '') {
  if (mime.startsWith('image/') || isImageDocument(path)) return 'image'
  if (isPdfDocument(path, mime)) return 'pdf'
  return 'file'
}
