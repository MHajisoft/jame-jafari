<script setup>
defineProps({
  value: { type: String, default: '' }
})
</script>

<template>
  <span v-if="value" class="nick-badge" :title="value">{{ value }}</span>
</template>
