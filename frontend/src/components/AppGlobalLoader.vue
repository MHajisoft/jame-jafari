<script setup>
import { storeToRefs } from 'pinia'
import { useLoadingStore } from '../stores/loading'

const { visible } = storeToRefs(useLoadingStore())
</script>

<template>
  <Teleport to="body">
    <Transition name="global-loader">
      <div
        v-if="visible"
        class="global-loader"
        role="status"
        aria-live="polite"
        aria-busy="true"
        aria-label="در حال بارگذاری"
      >
        <div class="global-loader-card">
          <span class="global-loader-spinner" aria-hidden="true" />
          <span class="global-loader-text">در حال بارگذاری…</span>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.global-loader {
  position: fixed;
  inset: 0;
  z-index: 5000;
  display: grid;
  place-items: center;
  background: color-mix(in srgb, var(--overlay, rgba(0, 0, 0, 0.35)) 88%, transparent);
  backdrop-filter: blur(2px);
  pointer-events: all;
}

.global-loader-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.85rem;
  min-width: 10rem;
  padding: 1.1rem 1.35rem;
  border-radius: 14px;
  background: var(--surface);
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.18);
}

.global-loader-spinner {
  width: 2rem;
  height: 2rem;
  border-radius: 50%;
  border: 3px solid color-mix(in srgb, var(--primary) 18%, transparent);
  border-top-color: var(--primary);
  animation: global-loader-spin 0.75s linear infinite;
}

.global-loader-text {
  font-size: 0.9rem;
  font-weight: 600;
  color: var(--text-muted);
}

.global-loader-enter-active,
.global-loader-leave-active {
  transition: opacity 0.18s ease;
}
.global-loader-enter-from,
.global-loader-leave-to {
  opacity: 0;
}

@keyframes global-loader-spin {
  to { transform: rotate(360deg); }
}
</style>
