import dayjs from 'dayjs'
import jalaliday from 'jalaliday'

dayjs.extend(jalaliday)

export const PERSIAN_MONTHS = [
  'فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور',
  'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'
]

export function toJalaliParts(date) {
  if (!date) return null
  const j = dayjs(date).calendar('jalali')
  return { year: j.year(), month: j.month() + 1, day: j.date() }
}

export function jalaliToGregorian(year, month, day) {
  const y = String(year)
  const m = String(month).padStart(2, '0')
  const d = String(day).padStart(2, '0')
  return dayjs(`${y}/${m}/${d}`, { jalali: true }).format('YYYY-MM-DD')
}

export function getDaysInJalaliMonth(year, month) {
  for (let day = 31; day >= 29; day--) {
    const parsed = dayjs(`${year}/${month}/${day}`, { jalali: true }).calendar('jalali')
    if (parsed.year() === year && parsed.month() + 1 === month) return day
  }
  return 29
}

export function formatJalali(date, pattern = 'D MMMM YYYY') {
  if (!date) return ''
  return dayjs(date).calendar('jalali').locale('fa').format(pattern)
}

export function toPersianDigits(value) {
  return String(value).replace(/\d/g, d => '۰۱۲۳۴۵۶۷۸۹'[d])
}

export function todayGregorian() {
  return dayjs().format('YYYY-MM-DD')
}

export function startOfJalaliMonthGregorian(date = new Date()) {
  const { year, month } = toJalaliParts(date)
  return jalaliToGregorian(year, month, 1)
}
