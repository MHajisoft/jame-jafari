<script setup>
import { ref, watch } from 'vue'
import { formatCurrencyInput, parseCurrencyInput } from '../utils/format'

const props = defineProps({
  modelValue: { type: [Number, String], default: '' },
  placeholder: { type: String, default: '' },
  invalid: { type: Boolean, default: false }
})

const emit = defineEmits(['update:modelValue', 'input'])

const display = ref('')

watch(
  () => props.modelValue,
  (v) => {
    display.value = formatCurrencyInput(v)
  },
  { immediate: true }
)

function onInput(e) {
  const parsed = parseCurrencyInput(e.target.value)
  display.value = formatCurrencyInput(parsed)
  emit('update:modelValue', parsed === '' ? '' : Number(parsed))
  emit('input')
}
</script>

<template>
  <input
    :value="display"
    type="text"
    inputmode="numeric"
    autocomplete="off"
    class="form-control"
    :class="{ 'field-invalid': invalid }"
    :placeholder="placeholder"
    @input="onInput"
  />
</template>
