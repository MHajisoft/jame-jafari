<script setup>
import { computed, ref, watch } from 'vue'

const props = defineProps({
  /** Relative upload path or absolute URL */
  src: { type: String, default: '' },
  name: { type: String, default: '' },
  size: { type: [Number, String], default: 36 }
})

const broken = ref(false)

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
</script>

<template>
  <span class="entity-avatar" :style="boxStyle" aria-hidden="true">
    <img v-if="showImage" :src="url" alt="" @error="broken = true" />
    <span v-else class="entity-avatar-fallback">{{ initials }}</span>
  </span>
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
.entity-avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}
.entity-avatar-fallback {
  user-select: none;
}
</style>
