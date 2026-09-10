<script setup>
import { computed } from 'vue'
import PrefIconLabelCell from './PrefIconLabelCell.vue'

const props = defineProps({
  active: { type: Boolean, default: false },
  size: { type: [Number, String], default: 18 },
  emptyLabel: { type: String, default: '—' }
})

const text = computed(() => (props.active ? 'فعال' : 'غیرفعال'))
const tone = computed(() => (props.active ? 'success' : 'muted'))
</script>

<template>
  <PrefIconLabelCell :label="text" :size="size" :empty-label="emptyLabel" :tone="tone">
    <template #icon>
      <svg v-if="active" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
        <circle cx="12" cy="12" r="9" />
        <path d="M8.5 12.5l2.5 2.5 4.5-5" />
      </svg>
      <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
        <circle cx="12" cy="12" r="9" />
        <path d="M8 12h8" />
      </svg>
    </template>
  </PrefIconLabelCell>
</template>
