/**
 * App enums with Persian display titles.
 * API may send numeric values or JsonStringEnumConverter names — both resolve.
 */

export const genders = [
  { value: 1, key: 'Male', label: 'مرد' },
  { value: 2, key: 'Female', label: 'زن' }
]

export const paymentTypes = [
  { value: 1, key: 'Cash', label: 'نقد' },
  { value: 2, key: 'Pos', label: 'کارتخوان' },
  { value: 3, key: 'Cheque', label: 'چک' },
  { value: 4, key: 'BankTransference', label: 'انتقال بانکی' }
]

export const transactionDirections = [
  { value: 1, key: 'Income', label: 'درآمد' },
  { value: 2, key: 'Cost', label: 'هزینه' }
]

export const generalTypeCategories = [
  { value: 1, key: 'Unit', label: 'واحد' },
  { value: 2, key: 'NamePrefix', label: 'پیشوند نام' }
]

export const messengerKinds = [
  { value: 1, key: 'Bale', label: 'بله' }
]

export const baleMessageTypes = [
  { value: 1, key: 'Text', label: 'متن' },
  { value: 2, key: 'Photo', label: 'تصویر / ویدیو' },
  { value: 3, key: 'File', label: 'فایل' }
]

export const baleMessageStatuses = [
  { value: 1, key: 'Pending', label: 'در انتظار' },
  { value: 2, key: 'Sent', label: 'ارسال‌شده' },
  { value: 3, key: 'Failed', label: 'ناموفق' },
  { value: 4, key: 'Deleted', label: 'حذف از گفتگو' },
  { value: 5, key: 'AwaitingContact', label: 'در انتظار اتصال' }
]

function matchesOption(opt, value) {
  if (value === null || value === undefined || value === '') return false
  if (opt.value === value || opt.value === Number(value)) return true
  if (opt.key && String(opt.key).toLowerCase() === String(value).toLowerCase()) return true
  if (opt.label && String(opt.label) === String(value)) return true
  return false
}

/** Persian title for an enum option list. */
export function enumLabel(options, value, fallback = '') {
  if (value === null || value === undefined || value === '') return fallback
  const found = options.find((o) => matchesOption(o, value))
  return found?.label ?? (fallback || String(value))
}

/** Normalize API enum (number or name) to numeric option value for selects. */
export function enumValue(options, value, defaultValue = '') {
  if (value === null || value === undefined || value === '') return defaultValue
  const found = options.find((o) => matchesOption(o, value))
  return found ? found.value : defaultValue
}

export const genderLabel = (v) => enumLabel(genders, v)
export const paymentTypeLabel = (v) => enumLabel(paymentTypes, v)
export const transactionDirectionLabel = (v) => enumLabel(transactionDirections, v)
export const generalTypeCategoryLabel = (v) => enumLabel(generalTypeCategories, v)
export const messengerKindLabel = (v) => enumLabel(messengerKinds, v)
export const baleMessageTypeLabel = (v) => enumLabel(baleMessageTypes, v)
export const baleMessageStatusLabel = (v) => enumLabel(baleMessageStatuses, v)
export const isBaleMessageType = (v, typeValue) => enumValue(baleMessageTypes, v, 0) === typeValue
