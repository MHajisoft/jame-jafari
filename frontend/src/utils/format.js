import dayjs from 'dayjs'
import jalaliday from 'jalaliday'

dayjs.extend(jalaliday)

export function formatDate(date, culture = 'jalali', format = 'YYYY/MM/DD') {
  if (!date) return ''
  const d = dayjs(date)
  if (culture === 'jalali') {
    return d.calendar('jalali').locale('fa').format(format)
  }
  if (culture === 'hijri') {
    return d.format('YYYY/MM/DD')
  }
  return d.format('YYYY-MM-DD')
}

export function formatDateTime(date, culture = 'jalali') {
  if (!date) return ''
  const d = dayjs(date)
  if (culture === 'jalali') {
    return d.calendar('jalali').format('YYYY/MM/DD HH:mm')
  }
  return d.format('YYYY-MM-DD HH:mm')
}

export function toInputDate(date) {
  return dayjs(date).format('YYYY-MM-DD')
}

export function formatMoney(amount) {
  return new Intl.NumberFormat('fa-IR').format(amount || 0)
}

export const paymentTypes = [
  { value: 1, label: 'نقد' },
  { value: 2, label: 'کارتخوان' },
  { value: 3, label: 'چک' },
  { value: 4, label: 'انتقال بانکی' }
]

export const genders = [
  { value: 1, label: 'مرد' },
  { value: 2, label: 'زن' },
  { value: 3, label: 'سایر' }
]
