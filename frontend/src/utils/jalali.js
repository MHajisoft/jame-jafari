import dayjs from 'dayjs'
import jalaliday from 'jalaliday'

dayjs.extend(jalaliday)

export const PERSIAN_MONTHS = [
  'فروردین', 'اردیبهشت', 'خرداد', 'تیر', 'مرداد', 'شهریور',
  'مهر', 'آبان', 'آذر', 'دی', 'بهمن', 'اسفند'
]

export const PERSIAN_WEEKDAYS = ['ش', 'ی', 'د', 'س', 'چ', 'پ', 'ج']

export const MIN_JALALI_YEAR = 1300

/** Jalaali leap-year breaks (same source as jalaali-js). */
const JALALI_BREAKS = [
  -61, 9, 38, 199, 426, 686, 756, 818, 1111, 1181,
  1210, 1635, 2060, 2097, 2192, 2262, 2324, 2394, 2456, 3178
]

function div(a, b) {
  return ~~(a / b)
}

function mod(a, b) {
  return a - ~~(a / b) * b
}

/**
 * Accurate leap-year check for the algorithmic Persian calendar.
 * Esfand has 30 days in leap years, 29 otherwise.
 */
export function isJalaliLeapYear(jy) {
  const bl = JALALI_BREAKS.length
  let jp = JALALI_BREAKS[0]
  let jump = 0
  let leap = 0
  let n = 0

  if (jy < jp || jy >= JALALI_BREAKS[bl - 1]) {
    return [1, 5, 9, 13, 17, 22, 26, 30].includes(((jy % 33) + 33) % 33)
  }

  for (let i = 1; i < bl; i += 1) {
    const jm = JALALI_BREAKS[i]
    jump = jm - jp
    if (jy < jm) break
    jp = jm
  }

  n = jy - jp
  if (jump - n < 6) n = n - jump + div(jump + 4, 33) * 33
  leap = mod(mod(n + 1, 33) - 1, 4)
  if (leap === -1) leap = 4
  return leap === 0
}

export function toJalaliParts(date) {
  if (!date) return null
  const j = dayjs(date).calendar('jalali')
  return { year: j.year(), month: j.month() + 1, day: j.date() }
}

export function jalaliToGregorian(year, month, day) {
  const maxDay = getDaysInJalaliMonth(year, month)
  const safeDay = Math.min(Math.max(1, day), maxDay)
  const y = String(year)
  const m = String(month).padStart(2, '0')
  const d = String(safeDay).padStart(2, '0')
  return dayjs(`${y}/${m}/${d}`, { jalali: true }).format('YYYY-MM-DD')
}

export function getDaysInJalaliMonth(year, month) {
  if (month >= 1 && month <= 6) return 31
  if (month >= 7 && month <= 11) return 30
  if (month === 12) return isJalaliLeapYear(year) ? 30 : 29
  return 30
}

/** Saturday-first month grid with adjacent-month fillers (42 cells). */
export function getJalaliMonthGrid(year, month) {
  const daysInMonth = getDaysInJalaliMonth(year, month)
  const prevMonth = month === 1 ? 12 : month - 1
  const prevYear = month === 1 ? year - 1 : year
  const daysInPrev = getDaysInJalaliMonth(prevYear, prevMonth)
  const nextMonth = month === 12 ? 1 : month + 1
  const nextYear = month === 12 ? year + 1 : year

  const firstGregorian = jalaliToGregorian(year, month, 1)
  // dayjs: 0=Sun ... 6=Sat → Persian index Sat=0
  const firstWeekday = (dayjs(firstGregorian).day() + 1) % 7

  const cells = []
  for (let i = 0; i < firstWeekday; i += 1) {
    const day = daysInPrev - firstWeekday + i + 1
    cells.push({ year: prevYear, month: prevMonth, day, current: false })
  }
  for (let day = 1; day <= daysInMonth; day += 1) {
    cells.push({ year, month, day, current: true })
  }
  let nextDay = 1
  while (cells.length < 42) {
    cells.push({ year: nextYear, month: nextMonth, day: nextDay, current: false })
    nextDay += 1
  }
  return cells
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

export function maxJalaliYear(pad = 20) {
  return (toJalaliParts(todayGregorian())?.year || 1404) + pad
}

export const PERSIAN_SEASONS = ['بهار', 'تابستان', 'پاییز', 'زمستان']

/** @param {number} jalaliMonth 1–12 */
export function jalaliSeasonIndex(jalaliMonth) {
  return Math.floor((jalaliMonth - 1) / 3)
}

export function jalaliSeasonLabel(jalaliMonth) {
  return PERSIAN_SEASONS[jalaliSeasonIndex(jalaliMonth)] || ''
}
