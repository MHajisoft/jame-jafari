<script setup>
import { useToastStore } from '../stores/toast'

const toast = useToastStore()

const icons = {
  success: 'M5 13l4 4L19 7',
  error: 'M6 18L18 6M6 6l12 12',
  warning: 'M12 9v4m0 4h.01M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z',
  info: 'M12 16v-4m0-4h.01M12 2a10 10 0 100 20 10 10 0 000-20z'
}
</script>

<template>
  <div class="toast-host" aria-live="polite" aria-relevant="additions">
    <TransitionGroup name="toast">
      <div
        v-for="item in toast.items"
        :key="item.id"
        class="toast"
        :class="`toast-${item.type}`"
        role="status"
      >
        <span class="toast-icon" aria-hidden="true">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path :d="icons[item.type] || icons.info" />
          </svg>
        </span>
        <p class="toast-message">{{ item.message }}</p>
        <button
          type="button"
          class="toast-close"
          aria-label="بستن"
          @click="toast.dismiss(item.id)"
        >
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">
            <path d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>
    </TransitionGroup>
  </div>
</template>

<style scoped>
.toast-host {
  position: fixed;
  z-index: 4000;
  inset-inline: 0;
  top: 1rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.55rem;
  padding-inline: 1rem;
  pointer-events: none;
}

.toast {
  pointer-events: auto;
  display: flex;
  align-items: flex-start;
  gap: 0.65rem;
  width: min(420px, 100%);
  padding: 0.8rem 0.85rem;
  border-radius: 12px;
  border: 1px solid var(--border);
  background: var(--surface);
  color: var(--text);
  box-shadow:
    0 10px 30px rgba(15, 23, 42, 0.12),
    0 2px 8px rgba(15, 23, 42, 0.06);
  direction: rtl;
  text-align: right;
}

.toast-icon {
  flex-shrink: 0;
  width: 1.35rem;
  height: 1.35rem;
  margin-top: 0.1rem;
}

.toast-icon svg {
  width: 100%;
  height: 100%;
  display: block;
}

.toast-message {
  flex: 1;
  margin: 0;
  font-size: 0.9rem;
  line-height: 1.55;
  font-weight: 500;
}

.toast-close {
  flex-shrink: 0;
  width: 1.75rem;
  height: 1.75rem;
  border: none;
  border-radius: 8px;
  background: transparent;
  color: var(--text-muted);
  cursor: pointer;
  display: grid;
  place-items: center;
  padding: 0;
}

.toast-close svg {
  width: 1rem;
  height: 1rem;
}

.toast-close:hover {
  background: color-mix(in srgb, var(--text) 8%, transparent);
  color: var(--text);
}

.toast-success {
  border-color: color-mix(in srgb, var(--success) 35%, var(--border));
  background: color-mix(in srgb, var(--success) 10%, var(--surface));
}
.toast-success .toast-icon { color: var(--success); }

.toast-error {
  border-color: color-mix(in srgb, var(--danger) 35%, var(--border));
  background: color-mix(in srgb, var(--danger) 10%, var(--surface));
}
.toast-error .toast-icon { color: var(--danger); }

.toast-warning {
  border-color: color-mix(in srgb, var(--warning, #d97706) 40%, var(--border));
  background: color-mix(in srgb, var(--warning, #d97706) 12%, var(--surface));
}
.toast-warning .toast-icon { color: var(--warning, #d97706); }

.toast-info {
  border-color: color-mix(in srgb, var(--primary) 30%, var(--border));
  background: color-mix(in srgb, var(--primary) 8%, var(--surface));
}
.toast-info .toast-icon { color: var(--primary); }

.toast-enter-active,
.toast-leave-active {
  transition: all 0.28s ease;
}
.toast-enter-from {
  opacity: 0;
  transform: translateY(-10px) scale(0.98);
}
.toast-leave-to {
  opacity: 0;
  transform: translateY(-6px) scale(0.98);
}
.toast-move {
  transition: transform 0.28s ease;
}

@media (max-width: 768px) {
  .toast-host {
    top: calc(56px + env(safe-area-inset-top, 0) + 0.5rem);
    bottom: auto;
    padding-inline: 0.75rem;
  }

  .toast {
    width: 100%;
    border-radius: 14px;
    padding: 0.9rem 0.9rem;
    box-shadow:
      0 12px 28px rgba(15, 23, 42, 0.16),
      0 2px 6px rgba(15, 23, 42, 0.08);
  }

  .toast-message {
    font-size: 0.92rem;
  }
}
</style>
