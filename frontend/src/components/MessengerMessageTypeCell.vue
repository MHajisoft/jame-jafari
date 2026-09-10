<script setup>
import { computed } from 'vue'
import PrefIconLabelCell from './PrefIconLabelCell.vue'
import { enumValue, messengerMessageTypeLabel, messengerMessageTypes } from '../utils/enums'

const props = defineProps({
  messageType: { type: [Number, String], default: null },
  label: { type: String, default: '' },
  size: { type: [Number, String], default: 18 },
  emptyLabel: { type: String, default: '—' }
})

const resolved = computed(() => enumValue(messengerMessageTypes, props.messageType, 0))
const text = computed(() => props.label || messengerMessageTypeLabel(props.messageType) || '')
</script>

<template>
  <PrefIconLabelCell :label="text" :size="size" :empty-label="emptyLabel">
    <template v-if="resolved === 1 || resolved === 2 || resolved === 3" #icon>
      <!-- متن -->
      <svg v-if="resolved === 1" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z" />
      </svg>
      <!-- تصویر / ویدیو -->
      <svg v-else-if="resolved === 2" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <rect x="3" y="5" width="18" height="14" rx="2" />
        <circle cx="8.5" cy="10.5" r="1.5" fill="currentColor" stroke="none" />
        <path d="M21 15l-5-5L8 18" />
      </svg>
      <!-- فایل -->
      <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
        <path d="M14 2v6h6" />
      </svg>
    </template>
  </PrefIconLabelCell>
</template>
