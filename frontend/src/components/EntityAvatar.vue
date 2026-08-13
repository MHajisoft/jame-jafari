<script setup>
import { computed, ref, watch } from 'vue'
import AvatarPreview from './AvatarPreview.vue'

const props = defineProps({
  /** Relative upload path or absolute URL */
  src: { type: String, default: '' },
  name: { type: String, default: '' },
  size: { type: [Number, String], default: 36 },
  /** Person.IsDead — grayscale + memorial rim */
  deceased: { type: Boolean, default: false },
  /** Tap to open full-size preview (lists / pickers) */
  previewable: { type: Boolean, default: false },
  previewTitle: { type: String, default: '' }
})

const broken = ref(false)
const previewOpen = ref(false)

watch(
  () => props.src,
  () => { broken.value = false }
)

const url = computed(() => {
  const path = String(props.src || '').trim()
  if (!path) return ''
  if (path.startsWith('http://') || path.startsWith('https://') || path.startsWith('/') || path.startsWith('blob:')) {
    return path
  }
  return `/uploads/${path}`
})

const showImage = computed(() => !!url.value && !broken.value)
const canPreview = computed(() => props.previewable && showImage.value)

const previewLabel = computed(() => {
  const title = props.previewTitle || props.name
  return title ? `مشاهده تصویر ${title}` : 'مشاهده تصویر'
})

const initials = computed(() => {
  const raw = String(props.name || '').trim()
  if (!raw) return '؟'
  const parts = raw.split(/\s+/).filter(Boolean)
  if (parts.length >= 2) {
    return (parts[0].charAt(0) + parts[1].charAt(0)).toUpperCase()
  }
  return raw.charAt(0).toUpperCase()
})

const boxStyle = computed(() => {
  const n = Number(props.size) || 36
  return {
    width: `${n}px`,
    height: `${n}px`,
    fontSize: `${Math.max(11, Math.round(n * 0.36))}px`
  }
})

function openPreview(e) {
  if (!canPreview.value) return
  e?.stopPropagation?.()
  e?.preventDefault?.()
  previewOpen.value = true
}

function onPreviewKeydown(e) {
  if (!canPreview.value) return
  if (e.key === 'Enter' || e.key === ' ') {
    e.preventDefault()
    e.stopPropagation()
    previewOpen.value = true
  }
}
</script>

<template>
  <span
    class="entity-avatar"
    :class="{ deceased, previewable: canPreview }"
    :style="boxStyle"
    :role="canPreview ? 'button' : undefined"
    :tabindex="canPreview ? 0 : undefined"
    :aria-label="canPreview ? previewLabel : undefined"
    @click="openPreview"
    @keydown="onPreviewKeydown"
  >
    <img v-if="showImage" :src="url" alt="" @error="broken = true" />
    <span v-else class="entity-avatar-fallback">{{ initials }}</span>
    <span v-if="deceased" class="memorial-band" />
    <span v-if="canPreview" class="preview-lens" aria-hidden="true">
      <svg viewBox="0 0 24 24" width="12" height="12" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round">
        <circle cx="10.5" cy="10.5" r="6.5" />
        <path d="M21 21l-5.5-5.5" />
      </svg>
    </span>
  </span>

  <AvatarPreview
    v-if="canPreview"
    v-model:show="previewOpen"
    :src="url"
    :title="previewTitle || name"
    :deceased="deceased"
  />
</template>

<style scoped>
.entity-avatar {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  border-radius: 50%;
  overflow: hidden;
  background: color-mix(in srgb, var(--primary) 16%, var(--bg));
  color: var(--primary);
  font-weight: 700;
  line-height: 1;
  border: 1px solid color-mix(in srgb, var(--primary) 18%, var(--border));
}
.entity-avatar.previewable {
  cursor: zoom-in;
  -webkit-tap-highlight-color: transparent;
}
.entity-avatar.previewable:focus-visible {
  outline: 2px solid var(--primary);
  outline-offset: 2px;
}
.entity-avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}
.entity-avatar-fallback {
  user-select: none;
}
.entity-avatar.deceased {
  border-color: color-mix(in srgb, var(--text-muted) 45%, var(--border));
  color: color-mix(in srgb, var(--text-muted) 70%, var(--text));
  background: color-mix(in srgb, var(--text-muted) 12%, var(--bg));
}
.entity-avatar.deceased img,
.entity-avatar.deceased .entity-avatar-fallback {
  filter: grayscale(1) brightness(0.92) contrast(0.95);
  opacity: 0.88;
}
.preview-lens {
  position: absolute;
  inset-inline-end: -1px;
  bottom: -1px;
  width: 18px;
  height: 18px;
  border-radius: 50%;
  display: grid;
  place-items: center;
  background: color-mix(in srgb, var(--surface) 88%, transparent);
  color: var(--primary);
  border: 1px solid color-mix(in srgb, var(--primary) 25%, var(--border));
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.12);
  pointer-events: none;
}
.memorial-band {
  position: absolute;
  inset-inline-end: -18%;
  top: 8%;
  width: 62%;
  height: 18%;
  transform: rotate(-38deg);
  background: linear-gradient(
    90deg,
    transparent 0%,
    color-mix(in srgb, var(--text-muted) 55%, #2a3038) 35%,
    color-mix(in srgb, var(--text-muted) 40%, #1f242b) 100%
  );
  opacity: 0.85;
  pointer-events: none;
  z-index: 2;
}
</style>
