<script setup>
import { computed } from 'vue'

const props = defineProps({
  modelValue: { type: [String, Number], default: '' },
  type: { type: String, default: 'text' }, // text | number | password | search | email | textarea
  placeholder: { type: String, default: '' },
  invalid: { type: Boolean, default: false },
  disabled: { type: Boolean, default: false },
  rows: { type: [Number, String], default: 2 },
  maxlength: { type: [Number, String], default: undefined },
  min: { type: [Number, String], default: undefined },
  max: { type: [Number, String], default: undefined },
  step: { type: [Number, String], default: undefined },
  inputmode: { type: String, default: undefined },
  autocomplete: { type: String, default: undefined }
})

const emit = defineEmits(['update:modelValue', 'input', 'keyup'])

const isTextarea = computed(() => props.type === 'textarea')
const hasValue = computed(() => {
  if (props.modelValue === null || props.modelValue === undefined) return false
  return String(props.modelValue).length > 0
})

function onInput(e) {
  emit('update:modelValue', e.target.value)
  emit('input', e)
}

function clear(e) {
  e?.stopPropagation?.()
  if (props.disabled) return
  emit('update:modelValue', '')
  emit('input')
}

function onKeydown(e) {
  if (e.key === 'Escape' && hasValue.value && !props.disabled) {
    e.preventDefault()
    clear()
  }
}

function onKeyup(e) {
  emit('keyup', e)
}
</script>

<template>
  <div class="clearable-input" :class="{ 'has-clear': hasValue, textarea: isTextarea }">
    <textarea
      v-if="isTextarea"
      :value="modelValue"
      class="form-control"
      :class="{ 'field-invalid': invalid }"
      :placeholder="placeholder"
      :disabled="disabled"
      :rows="rows"
      :maxlength="maxlength"
      @input="onInput"
      @keydown="onKeydown"
      @keyup="onKeyup"
    />
    <input
      v-else
      :value="modelValue"
      :type="type"
      class="form-control"
      :class="{ 'field-invalid': invalid }"
      :placeholder="placeholder"
      :disabled="disabled"
      :maxlength="maxlength"
      :min="min"
      :max="max"
      :step="step"
      :inputmode="inputmode"
      :autocomplete="autocomplete"
      @input="onInput"
      @keydown="onKeydown"
      @keyup="onKeyup"
    />
    <button
      v-if="hasValue && !disabled"
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
.clearable-input {
  position: relative;
  width: 100%;
}
.clearable-input.has-clear :is(input, textarea).form-control {
  padding-inline-start: 2.85rem;
}
.clearable-input.textarea .clear-btn {
  top: 0.55rem;
  transform: none;
}
.clear-btn {
  position: absolute;
  inset-inline-start: 0.45rem;
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
