import dayjs from 'dayjs'
import jalaliday from 'jalaliday'
import { formatJalali } from './jalali'

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
