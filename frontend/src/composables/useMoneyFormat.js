import { computed } from 'vue'
import { useUiPrefsStore } from '../stores/uiPrefs'
import { formatMoney as formatMoneyBase } from '../utils/format'

/** Reactive money formatter bound to Settings → currency display unit. */
export function useMoneyFormat() {
  const uiPrefs = useUiPrefsStore()
  const currencyUnit = computed(() => uiPrefs.currencyDisplayUnit)
  const unitLabel = computed(() => uiPrefs.currencyUnitLabel)

  /** Convert from Rial + suffix — for standalone inline text (hints). */
  function formatMoney(amount, options = {}) {
    return formatMoneyBase(amount, { unit: currencyUnit.value, ...options })
  }

  /** Convert from Rial, numbers only — for tables and KPI values. */
  function formatAmount(amount, options = {}) {
    return formatMoneyBase(amount, { unit: currencyUnit.value, showUnit: false, ...options })
  }

  return { currencyUnit, unitLabel, formatMoney, formatAmount }
}
