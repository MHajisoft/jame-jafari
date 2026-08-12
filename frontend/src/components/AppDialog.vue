<script setup>
import { onBeforeUnmount, onMounted, watch } from 'vue'
import { useDialogStore } from '../stores/dialog'
import { useIsMobile } from '../composables/useMediaQuery'

const dialog = useDialogStore()
const isMobile = useIsMobile()

function onKeydown(e) {
  if (!dialog.open) return
  if (e.key === 'Escape') {
    e.preventDefault()
    dialog.dismiss()
  } else if (e.key === 'Enter' && dialog.mode === 'alert') {
    e.preventDefault()
    dialog.accept()
  }
}

onMounted(() => window.addEventListener('keydown', onKeydown))
onBeforeUnmount(() => window.removeEventListener('keydown', onKeydown))

watch(
  () => dialog.open,
  (open) => {
    document.body.style.overflow = open ? 'hidden' : ''
  }
)
</script>

<template>
  <Teleport to="body">
    <Transition name="dialog-fade">
      <div
        v-if="dialog.open"
        class="dialog-overlay"
        :class="{ mobile: isMobile }"
        @click.self="dialog.dismiss()"
      >
        <div
          class="dialog-panel"
          role="alertdialog"
          aria-modal="true"
          :aria-labelledby="'app-dialog-title'"
          :aria-describedby="'app-dialog-desc'"
        >
          <div v-if="isMobile" class="dialog-handle" aria-hidden="true" />

          <div class="dialog-icon" :class="{ danger: dialog.danger }" aria-hidden="true">
            <svg v-if="dialog.danger" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M12 9v4m0 4h.01" />
              <path d="M10.29 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0Z" />
            </svg>
            <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <circle cx="12" cy="12" r="9" />
              <path d="M12 8v4m0 4h.01" />
            </svg>
          </div>

          <h2 id="app-dialog-title" class="dialog-title">{{ dialog.title }}</h2>
          <p id="app-dialog-desc" class="dialog-message">{{ dialog.message }}</p>

          <div class="dialog-actions" :class="{ stacked: isMobile || dialog.mode === 'alert' }">
            <button
              v-if="dialog.mode === 'confirm'"
              type="button"
              class="btn btn-outline dialog-btn"
              @click="dialog.dismiss()"
            >
              {{ dialog.cancelText }}
            </button>
            <button
              type="button"
              class="btn dialog-btn"
              :class="{ 'btn-danger': dialog.danger }"
              @click="dialog.accept()"
            >
              {{ dialog.confirmText }}
            </button>
          </div>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.dialog-overlay {
  position: fixed;
  inset: 0;
  z-index: 5000;
  background: var(--overlay, rgba(15, 23, 42, 0.48));
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
  padding-bottom: calc(1rem + env(safe-area-inset-bottom, 0));
}
.dialog-overlay.mobile {
  align-items: flex-end;
  padding: 0;
}

.dialog-panel {
  width: min(400px, 100%);
  background: var(--surface);
  color: var(--text);
  border-radius: 18px;
  border: 1px solid var(--border);
  box-shadow: 0 16px 40px rgba(0, 0, 0, 0.18);
  padding: 1.25rem 1.2rem 1.15rem;
  text-align: center;
}
.dialog-overlay.mobile .dialog-panel {
  width: 100%;
  max-width: none;
  border-radius: 20px 20px 0 0;
  border: none;
  padding: 0.65rem 1.1rem calc(1.15rem + env(safe-area-inset-bottom, 0));
  animation: sheet-up 0.25s ease-out;
}

.dialog-handle {
  width: 42px;
  height: 4px;
  border-radius: 999px;
  background: var(--border);
  margin: 0.2rem auto 0.85rem;
}

.dialog-icon {
  width: 48px;
  height: 48px;
  margin: 0 auto 0.75rem;
  border-radius: 50%;
  display: grid;
  place-items: center;
  background: color-mix(in srgb, var(--primary) 12%, transparent);
  color: var(--primary);
}
.dialog-icon.danger {
  background: color-mix(in srgb, var(--danger) 12%, transparent);
  color: var(--danger);
}
.dialog-icon svg {
  width: 24px;
  height: 24px;
  display: block;
}

.dialog-title {
  margin: 0 0 0.4rem;
  font-size: 1.1rem;
  font-weight: 700;
  line-height: 1.35;
}
.dialog-message {
  margin: 0 0 1.15rem;
  color: var(--text-muted);
  font-size: 0.92rem;
  line-height: 1.6;
}

.dialog-actions {
  display: flex;
  gap: 0.6rem;
  justify-content: stretch;
}
.dialog-actions.stacked {
  flex-direction: column-reverse;
}
.dialog-btn {
  flex: 1;
  min-height: 44px;
  justify-content: center;
}

.dialog-fade-enter-active,
.dialog-fade-leave-active {
  transition: opacity 0.2s ease;
}
.dialog-fade-enter-active .dialog-panel,
.dialog-fade-leave-active .dialog-panel {
  transition: transform 0.22s ease, opacity 0.22s ease;
}
.dialog-fade-enter-from,
.dialog-fade-leave-to {
  opacity: 0;
}
.dialog-fade-enter-from .dialog-panel {
  transform: translateY(12px) scale(0.98);
}
.dialog-overlay.mobile.dialog-fade-enter-from .dialog-panel {
  transform: translateY(100%);
}
.dialog-fade-leave-to .dialog-panel {
  transform: translateY(8px) scale(0.98);
}
.dialog-overlay.mobile.dialog-fade-leave-to .dialog-panel {
  transform: translateY(40%);
}

@keyframes sheet-up {
  from { transform: translateY(100%); }
  to { transform: translateY(0); }
}
</style>
