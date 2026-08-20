<script setup>
import { computed, nextTick, ref, watch } from 'vue'
import { useUiPrefsStore } from '../stores/uiPrefs'
import { currencyInputPlaceholder, fromDisplayAmount, toDisplayAmount } from '../utils/currency'
import { formatCurrencyInput, parseCurrencyInput } from '../utils/format'

const props = defineProps({
  modelValue: { type: [Number, String], default: '' },
  placeholder: { type: String, default: '' },
  invalid: { type: Boolean, default: false },
  /** Show unit badge (ریال / تومان) at the end of the input. */
  showUnit: { type: Boolean, default: true }
})

const emit = defineEmits(['update:modelValue', 'input'])

const uiPrefs = useUiPrefsStore()
const display = ref('')
const inputRef = ref(null)
const composing = ref(false)

const hasValue = computed(() => display.value !== '')
const resolvedPlaceholder = computed(() => props.placeholder || currencyInputPlaceholder(uiPrefs.currencyDisplayUnit))
const unitLabel = computed(() => uiPrefs.currencyUnitLabel)

function rialToDisplayString(rialValue) {
  if (rialValue === '' || rialValue === null || rialValue === undefined) return ''
  const displayAmount = toDisplayAmount(rialValue, uiPrefs.currencyDisplayUnit)
  const rounded = Math.round(displayAmount)
  return formatCurrencyInput(String(rounded))
}

function syncFromModelValue(v) {
  const next = rialToDisplayString(v)
  display.value = next
  syncDom(next)
}

watch(
  () => [props.modelValue, uiPrefs.currencyDisplayUnit],
  ([v]) => syncFromModelValue(v),
  { immediate: true }
)

function syncDom(next) {
  nextTick(() => {
    if (inputRef.value && inputRef.value.value !== next) {
      inputRef.value.value = next
    }
  })
}

function commit(raw) {
  const parsed = parseCurrencyInput(raw)
  const next = formatCurrencyInput(parsed)
  display.value = next
  if (inputRef.value) inputRef.value.value = next

  if (parsed === '') {
    emit('update:modelValue', '')
  } else {
    const rial = fromDisplayAmount(Number(parsed), uiPrefs.currencyDisplayUnit)
    emit('update:modelValue', rial)
  }
  emit('input')
}

function onInput(e) {
  if (composing.value) return
  commit(e.target.value)
}

function onCompositionStart() {
  composing.value = true
}

function onCompositionEnd(e) {
  composing.value = false
  commit(e.target.value)
}

function onKeydown(e) {
  if (e.key === 'Escape' && hasValue.value) {
    e.preventDefault()
    clear()
    return
  }
  if (e.ctrlKey || e.metaKey || e.altKey) return
  const nav = new Set([
    'Backspace', 'Delete', 'Tab', 'Escape', 'Enter',
    'ArrowLeft', 'ArrowRight', 'ArrowUp', 'ArrowDown',
    'Home', 'End'
  ])
  if (nav.has(e.key)) return
  if (e.key.length !== 1) return
  if (!/[0-9۰-۹٠-٩]/.test(e.key)) {
    e.preventDefault()
  }
}

function onPaste(e) {
  e.preventDefault()
  const text = e.clipboardData?.getData('text') ?? ''
  commit(`${display.value}${text}`)
}

function clear(e) {
  e?.stopPropagation?.()
  display.value = ''
  if (inputRef.value) inputRef.value.value = ''
  emit('update:modelValue', '')
  emit('input')
}
</script>

<template>
  <div class="currency-input" :class="{ 'has-clear': hasValue, 'has-unit': showUnit }">
    <span v-if="showUnit" class="unit-badge" aria-hidden="true">{{ unitLabel }}</span>
    <input
      ref="inputRef"
      :value="display"
      type="text"
      inputmode="numeric"
      autocomplete="off"
      class="form-control"
      :class="{ 'field-invalid': invalid }"
      :placeholder="resolvedPlaceholder"
      @keydown="onKeydown"
      @paste="onPaste"
      @compositionstart="onCompositionStart"
      @compositionend="onCompositionEnd"
      @input="onInput"
    />
    <button
      v-if="hasValue"
      type="button"
      class="clear-btn"
      tabindex="-1"
      aria-hidden="true"
      title="پاک کردن (Esc)"
      @mousedown.prevent
      @click="clear"
    >
      <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round">
        <line x1="18" y1="6" x2="6" y2="18" />
        <line x1="6" y1="6" x2="18" y2="18" />
      </svg>
    </button>
  </div>
</template>

<style scoped>
.currency-input {
  position: relative;
  width: 100%;
}
.currency-input.has-clear .form-control {
  padding-inline-start: 2.85rem;
}
.currency-input.has-unit .form-control {
  padding-inline-end: 3.4rem;
}
.unit-badge {
  position: absolute;
  inset-inline-end: 0;
  top: 0;
  bottom: 0;
  display: flex;
  align-items: center;
  padding: 0 0.65rem;
  font-size: 0.78rem;
  font-weight: 600;
  color: var(--text-muted);
  background: color-mix(in srgb, var(--primary) 8%, var(--surface, var(--bg)));
  border-inline-start: 1px solid var(--border);
  border-start-end-radius: calc(var(--radius, 8px) - 1px);
  border-end-end-radius: calc(var(--radius, 8px) - 1px);
  pointer-events: none;
  white-space: nowrap;
  z-index: 1;
  user-select: none;
}
.clear-btn {
  position: absolute;
  inset-inline-start: 0.55rem;
  top: 50%;
  transform: translateY(-50%);
  width: 28px;
  height: 28px;
  border: none;
  border-radius: 999px;
  background: color-mix(in srgb, var(--text-muted) 16%, transparent);
  color: var(--text-muted);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  z-index: 1;
}
.clear-btn:hover {
  background: color-mix(in srgb, var(--danger) 18%, transparent);
  color: var(--danger);
}
.clear-btn:focus {
  outline: none;
}
</style>
