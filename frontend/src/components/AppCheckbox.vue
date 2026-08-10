<script setup>
import { computed, useAttrs } from 'vue'

const props = defineProps({
  modelValue: { type: [Boolean, Array], default: false },
  value: { default: undefined },
  label: { type: String, default: '' },
  disabled: { type: Boolean, default: false },
  indeterminate: { type: Boolean, default: false }
})

const emit = defineEmits(['update:modelValue', 'change'])
const attrs = useAttrs()

const isArrayMode = computed(() => Array.isArray(props.modelValue))

const checked = computed(() => {
  if (isArrayMode.value) return props.modelValue.includes(props.value)
  return !!props.modelValue
})

function onChange(e) {
  const isChecked = e.target.checked
  if (isArrayMode.value) {
    const next = new Set(props.modelValue)
    if (isChecked) next.add(props.value)
    else next.delete(props.value)
    emit('update:modelValue', [...next])
  } else {
    emit('update:modelValue', isChecked)
  }
  emit('change', e)
}
</script>

<script>
export default { inheritAttrs: false }
</script>

<template>
  <label class="app-checkbox" :class="{ disabled, 'has-label': !!label || $slots.default }">
    <input
      type="checkbox"
      class="app-checkbox-input"
      :checked="checked"
      :value="value"
      :disabled="disabled"
      :indeterminate.prop="indeterminate"
      v-bind="attrs"
      @change="onChange"
    />
    <span class="app-checkbox-box" aria-hidden="true">
      <svg class="check-icon" viewBox="0 0 16 16" fill="none">
        <path d="M3.5 8.5L6.5 11.5L12.5 4.5" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
      </svg>
      <span class="indeterminate-icon" />
    </span>
    <span v-if="label || $slots.default" class="app-checkbox-label">
      <slot>{{ label }}</slot>
    </span>
  </label>
</template>

<style scoped>
.app-checkbox {
  display: inline-flex;
  align-items: center;
  gap: 0.55rem;
  cursor: pointer;
  user-select: none;
  margin: 0;
  font-weight: 500;
  font-size: 0.9rem;
  color: var(--text);
  line-height: 1.3;
}
.app-checkbox.has-label { width: fit-content; max-width: 100%; }
.app-checkbox.disabled {
  opacity: 0.55;
  cursor: not-allowed;
}
.app-checkbox-input {
  position: absolute;
  opacity: 0;
  width: 1px;
  height: 1px;
  margin: 0;
  pointer-events: none;
}
.app-checkbox-box {
  width: 1.15rem;
  height: 1.15rem;
  flex-shrink: 0;
  border-radius: 6px;
  border: 1.5px solid var(--border);
  background: var(--surface);
  color: transparent;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  transition: background 0.15s ease, border-color 0.15s ease, box-shadow 0.15s ease, color 0.15s ease;
  box-shadow: inset 0 1px 2px rgba(0, 0, 0, 0.04);
}
.check-icon {
  width: 0.75rem;
  height: 0.75rem;
  opacity: 0;
  transform: scale(0.7);
  transition: opacity 0.12s ease, transform 0.12s ease;
}
.indeterminate-icon {
  display: none;
  width: 0.55rem;
  height: 2px;
  border-radius: 999px;
  background: currentColor;
}
.app-checkbox-input:focus-visible + .app-checkbox-box {
  outline: 2px solid var(--primary);
  outline-offset: 2px;
}
.app-checkbox:hover:not(.disabled) .app-checkbox-box {
  border-color: color-mix(in srgb, var(--primary) 55%, var(--border));
}
.app-checkbox-input:checked + .app-checkbox-box,
.app-checkbox-input:indeterminate + .app-checkbox-box {
  background: var(--primary);
  border-color: var(--primary);
  color: #fff;
  box-shadow: none;
}
.app-checkbox-input:checked + .app-checkbox-box .check-icon {
  opacity: 1;
  transform: scale(1);
}
.app-checkbox-input:indeterminate + .app-checkbox-box .check-icon {
  display: none;
}
.app-checkbox-input:indeterminate + .app-checkbox-box .indeterminate-icon {
  display: block;
}
.app-checkbox-label { min-width: 0; }
</style>
