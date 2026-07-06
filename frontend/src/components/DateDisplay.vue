<script setup>
import { computed } from 'vue'
import { formatDate, formatDateTime } from '../utils/format'

const props = defineProps({
  value: { type: [String, Date], default: null },
  showTime: { type: Boolean, default: false }
})

const formatted = computed(() =>
  props.showTime ? formatDateTime(props.value) : formatDate(props.value)
)
</script>

<template>
  <span>{{ formatted }}</span>
</template>
