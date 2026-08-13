<script setup>
import { onBeforeUnmount, onMounted, toRef, watch } from 'vue'
import { useIsMobile } from '../composables/useMediaQuery'
import { useOverlayBack } from '../composables/useOverlayBack'

const props = defineProps({
  show: { type: Boolean, default: false },
  src: { type: String, default: '' },
  title: { type: String, default: '' },
  deceased: { type: Boolean, default: false }
})

const emit = defineEmits(['update:show'])
const isMobile = useIsMobile()
const showRef = toRef(props, 'show')

function close() {
  emit('update:show', false)
}

useOverlayBack(showRef, close, {
  enabled: () => isMobile.value && !!props.src,
  stateKey: 'appAvatarPreview'
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
    <Transition name="avatar-preview-fade">
      <div
        v-if="show && src"
        class="avatar-preview-overlay"
        :class="{ mobile: isMobile }"
        @click.self="close"
      >
        <div class="avatar-preview-panel" role="dialog" aria-modal="true" :aria-label="title || 'پیش‌نمایش تصویر'">
          <header class="avatar-preview-head">
            <h2 class="avatar-preview-title">{{ title || 'تصویر' }}</h2>
            <button type="button" class="avatar-preview-close" aria-label="بستن" @click="close">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true">
                <path d="M18 6L6 18M6 6l12 12" />
              </svg>
            </button>
          </header>

          <div class="avatar-preview-body">
            <img
              :src="src"
              :alt="title ? `تصویر ${title}` : 'تصویر'"
              class="avatar-preview-image"
              :class="{ deceased }"
            />
          </div>

          <footer class="avatar-preview-foot">
            <p class="avatar-preview-hint text-muted">برای تشخیص چهره، تصویر در اندازه واقعی نمایش داده می‌شود.</p>
            <button type="button" class="btn" @click="close">بستن</button>
          </footer>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.avatar-preview-overlay {
  position: fixed;
  inset: 0;
  z-index: 1350;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
  background: rgba(12, 20, 16, 0.58);
  backdrop-filter: blur(2px);
}

.avatar-preview-overlay.mobile {
  align-items: flex-end;
  padding: 0;
}

.avatar-preview-panel {
  width: min(560px, 100%);
  max-height: min(94vh, 900px);
  display: flex;
  flex-direction: column;
  background: var(--surface);
  border-radius: 16px;
  box-shadow: var(--shadow);
  overflow: hidden;
}

.avatar-preview-overlay.mobile .avatar-preview-panel {
  width: 100%;
  max-height: 94vh;
  border-radius: 20px 20px 0 0;
  animation: avatar-sheet-up 0.24s ease-out;
}

.avatar-preview-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 0.85rem 1rem;
  border-bottom: 1px solid var(--border);
}

.avatar-preview-title {
  margin: 0;
  font-size: 1rem;
  font-weight: 700;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.avatar-preview-close {
  display: grid;
  place-items: center;
  width: 2.25rem;
  height: 2.25rem;
  border: none;
  border-radius: 10px;
  background: var(--bg);
  color: var(--text);
  cursor: pointer;
  flex-shrink: 0;
}

.avatar-preview-close svg {
  width: 1.1rem;
  height: 1.1rem;
}

.avatar-preview-body {
  flex: 1;
  min-height: 0;
  overflow: auto;
  -webkit-overflow-scrolling: touch;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
  background: color-mix(in srgb, var(--bg) 70%, var(--surface));
}

.avatar-preview-image {
  width: auto;
  height: auto;
  max-width: none;
  max-height: none;
  object-fit: none;
  border-radius: 8px;
  box-shadow: 0 4px 24px rgba(0, 0, 0, 0.12);
}

.avatar-preview-overlay:not(.mobile) .avatar-preview-image {
  max-width: min(512px, calc(100vw - 3rem));
  max-height: min(512px, calc(100vh - 12rem));
}

.avatar-preview-overlay.mobile .avatar-preview-image {
  max-width: min(512px, 100%);
  max-height: none;
}

.avatar-preview-image.deceased {
  filter: grayscale(1) brightness(0.92) contrast(0.95);
  opacity: 0.92;
}

.avatar-preview-foot {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 0.85rem 1rem calc(0.85rem + env(safe-area-inset-bottom, 0));
  border-top: 1px solid var(--border);
}

.avatar-preview-hint {
  margin: 0;
  font-size: 0.78rem;
  line-height: 1.4;
  flex: 1;
  min-width: 0;
}

.avatar-preview-overlay.mobile .avatar-preview-foot {
  flex-direction: column;
  align-items: stretch;
}

.avatar-preview-overlay.mobile .avatar-preview-foot .btn {
  width: 100%;
  justify-content: center;
  min-height: 44px;
}

.avatar-preview-fade-enter-active,
.avatar-preview-fade-leave-active {
  transition: opacity 0.18s ease;
}

.avatar-preview-fade-enter-from,
.avatar-preview-fade-leave-to {
  opacity: 0;
}

@keyframes avatar-sheet-up {
  from { transform: translateY(100%); }
  to { transform: translateY(0); }
}
</style>
