<script setup>
import { computed, onBeforeUnmount, onMounted, toRef, watch } from 'vue'
import { useIsMobile } from '../composables/useMediaQuery'
import { useOverlayBack } from '../composables/useOverlayBack'
import DateDisplay from './DateDisplay.vue'

const props = defineProps({
  show: { type: Boolean, default: false },
  audit: { type: Object, default: null }
})

const emit = defineEmits(['update:show'])
const isMobile = useIsMobile()
const showRef = toRef(props, 'show')

const hasUpdate = computed(() => !!props.audit?.updatedAt)

function close() {
  emit('update:show', false)
}

useOverlayBack(showRef, close, {
  enabled: () => isMobile.value && props.show,
  stateKey: 'appAuditInfo'
})

function onKeydown(e) {
  if (!props.show) return
  if (e.key === 'Escape') {
    e.preventDefault()
    close()
  }
}

onMounted(() => window.addEventListener('keydown', onKeydown))
onBeforeUnmount(() => window.removeEventListener('keydown', onKeydown))

watch(
  () => props.show,
  (open) => {
    document.body.style.overflow = open ? 'hidden' : ''
  }
)
</script>

<template>
  <Teleport to="body">
    <Transition name="audit-panel-fade">
      <div
        v-if="show && audit"
        class="audit-panel-overlay"
        :class="{ mobile: isMobile }"
        @click.self="close"
      >
        <div class="audit-panel" role="dialog" aria-modal="true" aria-labelledby="audit-panel-title">
          <header class="audit-panel-head">
            <h2 id="audit-panel-title" class="audit-panel-title">اطلاعات ثبت</h2>
            <button type="button" class="audit-panel-close" aria-label="بستن" @click="close">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true">
                <path d="M18 6L6 18M6 6l12 12" />
              </svg>
            </button>
          </header>

          <div class="audit-panel-body">
            <div class="audit-entry audit-entry--created">
              <div class="audit-entry-marker" aria-hidden="true" />
              <div class="audit-entry-content">
                <h3 class="audit-entry-label">ایجاد</h3>
                <p class="audit-entry-value">
                  <DateDisplay :value="audit.createdAt" show-time />
                </p>
                <p class="audit-entry-meta">
                  توسط:
                  <strong>{{ audit.createdBy || '—' }}</strong>
                </p>
              </div>
            </div>

            <div class="audit-entry" :class="{ 'audit-entry--muted': !hasUpdate }">
              <div class="audit-entry-marker" aria-hidden="true" />
              <div class="audit-entry-content">
                <h3 class="audit-entry-label">آخرین ویرایش</h3>
                <template v-if="hasUpdate">
                  <p class="audit-entry-value">
                    <DateDisplay :value="audit.updatedAt" show-time />
                  </p>
                  <p class="audit-entry-meta">
                    توسط:
                    <strong>{{ audit.updatedBy || '—' }}</strong>
                  </p>
                </template>
                <p v-else class="audit-entry-empty">ویرایش نشده</p>
              </div>
            </div>
          </div>

          <footer class="audit-panel-foot">
            <button type="button" class="btn" @click="close">بستن</button>
          </footer>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.audit-panel-overlay {
  position: fixed;
  inset: 0;
  z-index: 1300;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
  background: rgba(12, 20, 16, 0.55);
  backdrop-filter: blur(2px);
}

.audit-panel-overlay.mobile {
  align-items: flex-end;
  padding: 0;
}

.audit-panel {
  width: min(380px, 100%);
  max-height: min(85vh, 520px);
  display: flex;
  flex-direction: column;
  background: var(--surface);
  border-radius: 16px;
  box-shadow: var(--shadow);
  overflow: hidden;
}

.audit-panel-overlay.mobile .audit-panel {
  width: 100%;
  max-height: 55vh;
  border-radius: 20px 20px 0 0;
}

.audit-panel-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 0.85rem 1rem;
  border-bottom: 1px solid var(--border);
}

.audit-panel-title {
  margin: 0;
  font-size: 1rem;
  font-weight: 700;
}

.audit-panel-close {
  display: grid;
  place-items: center;
  width: 2.25rem;
  height: 2.25rem;
  border: none;
  border-radius: 10px;
  background: var(--bg);
  color: var(--text);
  cursor: pointer;
}

.audit-panel-close svg {
  width: 1.1rem;
  height: 1.1rem;
}

.audit-panel-body {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
  padding: 1rem 1.15rem 1.1rem;
}

.audit-entry {
  position: relative;
  display: flex;
  gap: 0.85rem;
  padding-bottom: 1.15rem;
}

.audit-entry:last-child {
  padding-bottom: 0;
}

.audit-entry-marker {
  flex-shrink: 0;
  width: 0.65rem;
  height: 0.65rem;
  margin-top: 0.35rem;
  border-radius: 50%;
  background: var(--primary);
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--primary) 18%, transparent);
}

.audit-entry--muted .audit-entry-marker {
  background: var(--text-muted);
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--text-muted) 18%, transparent);
}

.audit-entry:not(:last-child)::after {
  content: '';
  position: absolute;
  top: 1rem;
  inset-inline-start: 0.3rem;
  width: 2px;
  height: calc(100% - 0.35rem);
  background: color-mix(in srgb, var(--border) 85%, var(--primary));
}

.audit-entry-content {
  flex: 1;
  min-width: 0;
}

.audit-entry-label {
  margin: 0 0 0.35rem;
  font-size: 0.82rem;
  font-weight: 700;
  color: var(--text-muted);
}

.audit-entry-value {
  margin: 0 0 0.25rem;
  font-size: 0.95rem;
  font-weight: 600;
}

.audit-entry-meta {
  margin: 0;
  font-size: 0.88rem;
  color: var(--text-muted);
}

.audit-entry-meta strong {
  color: var(--text);
  font-weight: 700;
}

.audit-entry-empty {
  margin: 0;
  font-size: 0.9rem;
  color: var(--text-muted);
}

.audit-panel-foot {
  display: flex;
  justify-content: flex-end;
  padding: 0.85rem 1rem calc(0.85rem + env(safe-area-inset-bottom, 0));
  border-top: 1px solid var(--border);
}

.audit-panel-overlay.mobile .audit-panel-foot .btn {
  width: 100%;
  justify-content: center;
  min-height: 44px;
}

.audit-panel-fade-enter-active,
.audit-panel-fade-leave-active {
  transition: opacity 0.18s ease;
}

.audit-panel-fade-enter-from,
.audit-panel-fade-leave-to {
  opacity: 0;
}

.audit-panel-overlay.mobile .audit-panel {
  animation: audit-sheet-up 0.24s ease-out;
}

@keyframes audit-sheet-up {
  from { transform: translateY(100%); }
  to { transform: translateY(0); }
}
</style>
