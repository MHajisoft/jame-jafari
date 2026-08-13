<script setup>
defineProps({
  /** Maps to Person.IsDead — shown only when true. */
  isDead: { type: Boolean, default: false }
})
</script>

<template>
  <span
    v-if="isDead"
    class="life-badge"
    title="درگذشته"
    role="img"
    aria-label="درگذشته"
  >
    درگذشته
  </span>
</template>

<style scoped>
.life-badge {
  flex-shrink: 0;
  display: inline-block;
  font-size: 0.68rem;
  font-weight: 600;
  line-height: 1;
  padding: 0.18rem 0.45rem;
  border-radius: 999px;
  max-width: 7rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  vertical-align: baseline;
  background: transparent;
  color: var(--text-muted);
  border: 1px solid color-mix(in srgb, var(--text-muted) 42%, var(--border));
}
</style>
