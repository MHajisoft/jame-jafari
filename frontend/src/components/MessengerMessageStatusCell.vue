<script setup>
import { computed } from 'vue'
import PrefIconLabelCell from './PrefIconLabelCell.vue'
import { enumValue, messengerMessageStatusLabel, messengerMessageStatuses } from '../utils/enums'

const props = defineProps({
  status: { type: [Number, String], default: null },
  label: { type: String, default: '' },
  size: { type: [Number, String], default: 18 },
  emptyLabel: { type: String, default: '—' }
})

const resolved = computed(() => enumValue(messengerMessageStatuses, props.status, 0))
const text = computed(() => props.label || messengerMessageStatusLabel(props.status) || '')
const tone = computed(() => {
  if (resolved.value === 2) return 'success'
  if (resolved.value === 3) return 'danger'
  if (resolved.value === 1 || resolved.value === 5) return 'warning'
  if (resolved.value === 4) return 'muted'
  return ''
})
</script>

<template>
  <PrefIconLabelCell :label="text" :size="size" :empty-label="emptyLabel" :tone="tone">
    <template v-if="resolved >= 1 && resolved <= 5" #icon>
      <!-- در انتظار -->
      <svg v-if="resolved === 1" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <circle cx="12" cy="12" r="9" />
        <path d="M12 7v5l3 2" />
      </svg>
      <!-- ارسال‌شده -->
      <svg v-else-if="resolved === 2" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
        <path d="M20 6L9 17l-5-5" />
      </svg>
      <!-- ناموفق -->
      <svg v-else-if="resolved === 3" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <circle cx="12" cy="12" r="9" />
        <path d="M15 9l-6 6M9 9l6 6" />
      </svg>
      <!-- حذف از گفتگو -->
      <svg v-else-if="resolved === 4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <path d="M3 6h18" />
        <path d="M8 6V4h8v2" />
        <path d="M19 6l-1 14H6L5 6" />
      </svg>
      <!-- در انتظار اتصال -->
      <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" />
        <circle cx="9" cy="7" r="4" />
        <path d="M19 8v6M22 11h-6" />
      </svg>
    </template>
  </PrefIconLabelCell>
</template>
