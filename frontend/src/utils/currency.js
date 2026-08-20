/** @typedef {'rial' | 'toman'} CurrencyDisplayUnit */

export const RIAL_PER_TOMAN = 10

export const CURRENCY_UNITS = /** @type {const} */ ({
  rial: 'rial',
  toman: 'toman'
})

export const CURRENCY_UNIT_LABELS = /** @type {Record<CurrencyDisplayUnit, string>} */ ({
  rial: 'ریال',
  toman: 'تومان'
})

/** Settings → currency display options. */
export const CURRENCY_DISPLAY_OPTIONS = [
  {
    id: 'rial',
    label: 'ریال',
    hint: 'همان واحد ذخیره در سیستم'
  },
  {
    id: 'toman',
    label: 'تومان',
    hint: 'نمایش و ورود به تومان؛ ذخیره به ریال'
  }
]

const DEFAULT_UNIT = CURRENCY_UNITS.rial

export function resolveCurrencyUnit(raw) {
  if (raw === 'rial' || raw === 'toman') return raw
  return DEFAULT_UNIT
}

export function currencyUnitLabel(unit) {
  return CURRENCY_UNIT_LABELS[resolveCurrencyUnit(unit)] || CURRENCY_UNIT_LABELS.rial
}

/** Rial (storage) → display amount in selected unit. */
export function toDisplayAmount(rialAmount, unit = DEFAULT_UNIT) {
  const n = Number(rialAmount)
  if (!Number.isFinite(n)) return 0
  if (resolveCurrencyUnit(unit) === CURRENCY_UNITS.toman) {
    return n / RIAL_PER_TOMAN
  }
  return n
}

/** User-entered display amount → Rial for storage/API. */
export function fromDisplayAmount(displayAmount, unit = DEFAULT_UNIT) {
  const n = Number(displayAmount)
  if (!Number.isFinite(n)) return 0
  if (resolveCurrencyUnit(unit) === CURRENCY_UNITS.toman) {
    return n * RIAL_PER_TOMAN
  }
  return n
}

/**
 * Format a Rial amount for display in the selected unit.
 * Toman: up to 1 decimal when not a whole Toman (amount % 10 !== 0 in Rial).
 */
export function formatDisplayAmount(rialAmount, unit = DEFAULT_UNIT) {
  const resolved = resolveCurrencyUnit(unit)
  const n = Number(rialAmount)
  if (!Number.isFinite(n)) {
    return new Intl.NumberFormat('fa-IR').format(0)
  }

  if (resolved === CURRENCY_UNITS.toman) {
    const hasFraction = Math.abs(n % RIAL_PER_TOMAN) > 1e-9
    const display = n / RIAL_PER_TOMAN
    return new Intl.NumberFormat('fa-IR', {
      minimumFractionDigits: hasFraction ? 1 : 0,
      maximumFractionDigits: hasFraction ? 1 : 0
    }).format(display)
  }

  return new Intl.NumberFormat('fa-IR', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
  }).format(Math.round(n))
}

/** Placeholder examples for CurrencyInput per unit. */
export function currencyInputPlaceholder(unit = DEFAULT_UNIT) {
  return resolveCurrencyUnit(unit) === CURRENCY_UNITS.toman
    ? 'مثلاً 150,000'
    : 'مثلاً 1,500,000'
}
