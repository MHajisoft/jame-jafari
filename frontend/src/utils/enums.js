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
