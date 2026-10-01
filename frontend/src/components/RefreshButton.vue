<script setup>
defineProps({
  loading: { type: Boolean, default: false },
  label: { type: String, default: 'به‌روزرسانی' }
})

defineEmits(['click'])
</script>

<template>
  <button
    type="button"
    class="btn btn-outline btn-sm list-refresh-btn"
    :disabled="loading"
    :title="label"
    :aria-label="label"
    @click="$emit('click')"
  >
    <span class="list-refresh-icon" aria-hidden="true">↻</span>
    <span class="list-refresh-label">{{ label }}</span>
  </button>
</template>

<style scoped>
.list-refresh-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  flex-shrink: 0;
}

.list-refresh-icon {
  font-size: 1.05rem;
  line-height: 1;
}
</style>
