<script setup>
import { computed } from 'vue'
import { useThemeStore } from '../stores/theme'
import { formatDate, formatDateTime } from '../utils/format'

const props = defineProps({
  value: { type: [String, Date], default: null },
  showTime: { type: Boolean, default: false }
})

const theme = useThemeStore()
const formatted = computed(() =>
  props.showTime
    ? formatDateTime(props.value, theme.dateCulture)
    : formatDate(props.value, theme.dateCulture)
)
</script>

<template>
  <span>{{ formatted }}</span>
</template>
