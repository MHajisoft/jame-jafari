<script setup>
import { onBeforeUnmount, onMounted, toRef, watch } from 'vue'
import { useIsMobile } from '../composables/useMediaQuery'
import { useOverlayBack } from '../composables/useOverlayBack'

const props = defineProps({
  show: { type: Boolean, default: false },
  src: { type: String, default: '' },
  kind: { type: String, default: 'file' }
})

const emit = defineEmits(['update:show'])
const isMobile = useIsMobile()
const showRef = toRef(props, 'show')

function close() {
  emit('update:show', false)
}

useOverlayBack(showRef, close, {
  enabled: () => isMobile.value && !!props.src,
  stateKey: 'appDocPreview'
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
    <Transition name="doc-preview-fade">
      <div
        v-if="show && src"
        class="doc-preview-overlay"
        :class="{ mobile: isMobile }"
        @click.self="close"
      >
        <div class="doc-preview-panel" role="dialog" aria-modal="true" aria-label="پیش‌نمایش پیوست">
          <header class="doc-preview-head">
            <h2 class="doc-preview-title">پیش‌نمایش پیوست</h2>
            <button type="button" class="doc-preview-close" aria-label="بستن" @click="close">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true">
                <path d="M18 6L6 18M6 6l12 12" />
              </svg>
            </button>
          </header>

          <div class="doc-preview-body">
            <img v-if="kind === 'image'" :src="src" alt="پیش‌نمایش تصویر پیوست" class="doc-preview-image" />
            <iframe
              v-else-if="kind === 'pdf'"
              :src="src"
              title="پیش‌نمایش PDF"
              class="doc-preview-pdf"
            />
            <div v-else class="doc-preview-fallback">
              <span class="doc-preview-fallback-icon" aria-hidden="true">📎</span>
              <p>پیش‌نمایش برای این نوع فایل در دسترس نیست.</p>
            </div>
          </div>

          <footer class="doc-preview-foot">
            <a
              :href="src"
              target="_blank"
              rel="noopener noreferrer"
              class="btn btn-outline"
              download
            >
              دانلود
            </a>
            <button type="button" class="btn" @click="close">بستن</button>
          </footer>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.doc-preview-overlay {
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

.doc-preview-overlay.mobile {
  align-items: flex-end;
  padding: 0;
}

.doc-preview-panel {
  width: min(920px, 100%);
  max-height: min(90vh, 900px);
  display: flex;
  flex-direction: column;
  background: var(--surface);
  border-radius: 16px;
  box-shadow: var(--shadow);
  overflow: hidden;
}

.doc-preview-overlay.mobile .doc-preview-panel {
  width: 100%;
  max-height: 92vh;
  border-radius: 20px 20px 0 0;
}

.doc-preview-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 0.85rem 1rem;
  border-bottom: 1px solid var(--border);
}

.doc-preview-title {
  margin: 0;
  font-size: 1rem;
  font-weight: 700;
}

.doc-preview-close {
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

.doc-preview-close svg {
  width: 1.1rem;
  height: 1.1rem;
}

.doc-preview-body {
  flex: 1;
  min-height: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  background: color-mix(in srgb, var(--bg) 65%, var(--surface));
  padding: 0.75rem;
}

.doc-preview-image {
  max-width: 100%;
  max-height: min(68vh, 720px);
  object-fit: contain;
  border-radius: 8px;
}

.doc-preview-pdf {
  width: 100%;
  height: min(68vh, 720px);
  border: none;
  border-radius: 8px;
  background: #fff;
}

.doc-preview-fallback {
  text-align: center;
  color: var(--text-muted);
  padding: 1.5rem;
}

.doc-preview-fallback-icon {
  display: block;
  font-size: 2rem;
  margin-bottom: 0.5rem;
}

.doc-preview-fallback p {
  margin: 0;
  font-size: 0.92rem;
}

.doc-preview-foot {
  display: flex;
  justify-content: flex-end;
  gap: 0.65rem;
  padding: 0.85rem 1rem calc(0.85rem + env(safe-area-inset-bottom, 0));
  border-top: 1px solid var(--border);
}

.doc-preview-overlay.mobile .doc-preview-foot {
  flex-direction: column-reverse;
}

.doc-preview-overlay.mobile .doc-preview-foot .btn {
  width: 100%;
  justify-content: center;
  min-height: 44px;
}

.doc-preview-fade-enter-active,
.doc-preview-fade-leave-active {
  transition: opacity 0.18s ease;
}

.doc-preview-fade-enter-from,
.doc-preview-fade-leave-to {
  opacity: 0;
}

.doc-preview-overlay.mobile .doc-preview-panel {
  animation: doc-sheet-up 0.24s ease-out;
}

@keyframes doc-sheet-up {
  from { transform: translateY(100%); }
  to { transform: translateY(0); }
}
</style>
