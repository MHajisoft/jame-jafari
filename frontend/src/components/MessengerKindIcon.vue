<script setup>
import { computed } from 'vue'
import { enumValue, messengerKinds, messengerKindLabel } from '../utils/enums'

const props = defineProps({
  kind: { type: [Number, String], default: null },
  size: { type: [Number, String], default: 18 },
  title: { type: String, default: '' }
})

const resolved = computed(() => enumValue(messengerKinds, props.kind, 0))
const px = computed(() => `${Number(props.size) || 18}px`)
const label = computed(() => props.title || messengerKindLabel(props.kind) || '')

/** Official brand marks under /icons/messengers (small files, PWA-precached). */
const src = computed(() => {
  if (resolved.value === 1) return '/icons/messengers/bale.svg'
  if (resolved.value === 2) return '/icons/messengers/rubika.png'
  return ''
})
</script>

<template>
  <img
    v-if="src"
    class="messenger-kind-icon"
    :src="src"
    :alt="label"
    :title="label || undefined"
    :width="Number(size) || 18"
    :height="Number(size) || 18"
    :style="{ width: px, height: px }"
    loading="lazy"
    decoding="async"
  />
</template>

<style scoped>
.messenger-kind-icon {
  display: inline-block;
  flex-shrink: 0;
  vertical-align: middle;
  object-fit: contain;
  border-radius: 22%;
}
</style>
