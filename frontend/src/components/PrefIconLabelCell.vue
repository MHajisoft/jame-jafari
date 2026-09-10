<script setup>
import { computed, useSlots } from 'vue'
import { useUiPrefsStore } from '../stores/uiPrefs'

const props = defineProps({
  label: { type: String, default: '' },
  size: { type: [Number, String], default: 18 },
  emptyLabel: { type: String, default: '—' },
  tone: { type: String, default: '' }
})

const slots = useSlots()
const uiPrefs = useUiPrefsStore()

const text = computed(() => (props.label || '').trim())
const hasValue = computed(() => !!text.value || !!slots.icon)
const showIcon = computed(() => uiPrefs.showTableIcon && !!slots.icon)
const showText = computed(() => uiPrefs.showTableText || !slots.icon)
const iconSize = computed(() => {
  if (showIcon.value && !uiPrefs.showTableText) return Math.max(Number(props.size) || 18, 22)
  return Number(props.size) || 18
})
</script>

<template>
  <span
    v-if="hasValue"
    class="pref-icon-label-cell"
    :class="[
      { 'icon-only': showIcon && !showText },
      tone ? `tone-${tone}` : null
    ]"
    :title="text || undefined"
  >
    <span
      v-if="showIcon"
      class="pref-icon-label-icon"
      :style="{ width: `${iconSize}px`, height: `${iconSize}px` }"
      aria-hidden="true"
    >
      <slot name="icon" :size="iconSize" />
    </span>
    <span v-if="showText" class="pref-icon-label-text">{{ text || emptyLabel }}</span>
  </span>
  <span v-else class="pref-icon-label-empty">{{ emptyLabel }}</span>
</template>

<style scoped>
.pref-icon-label-cell {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  min-width: 0;
  max-width: 100%;
  color: inherit;
}
.pref-icon-label-cell.icon-only {
  justify-content: center;
}
.pref-icon-label-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  color: currentColor;
}
.pref-icon-label-icon :deep(svg) {
  width: 100%;
  height: 100%;
  display: block;
}
.pref-icon-label-text {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.pref-icon-label-empty {
  color: var(--text-muted);
}
.tone-success { color: var(--success); }
.tone-danger { color: var(--danger); }
.tone-warning { color: var(--warning); }
.tone-muted { color: var(--text-muted); }
</style>
