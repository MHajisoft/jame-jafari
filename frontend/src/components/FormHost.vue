<script setup>
import { useIsMobile } from '../composables/useMediaQuery'

defineProps({
  show: { type: Boolean, default: false },
  title: { type: String, default: '' }
})

const emit = defineEmits(['close'])
const isMobile = useIsMobile()

defineExpose({ isMobile })
</script>

<template>
  <!-- Mobile: bottom sheet / modal -->
  <div
    v-if="show && isMobile"
    class="modal-overlay"
    @click.self="emit('close')"
  >
    <div class="modal">
      <h2 v-if="title" class="modal-title">{{ title }}</h2>
      <slot :is-mobile="true" />
    </div>
  </div>

  <!-- Desktop: full-width inline form -->
  <div v-else-if="show" class="card form-panel">
    <div v-if="title" class="form-panel-header">
      <h2 class="form-panel-title">{{ title }}</h2>
    </div>
    <slot :is-mobile="false" />
  </div>
</template>
