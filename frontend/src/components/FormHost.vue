<script setup>
import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useIsMobile } from '../composables/useMediaQuery'
import { hasActiveOverlay } from '../composables/useOverlayBack'
import {
  registerFormPage,
  unregisterFormPage,
  updateFormPageTitle
} from '../composables/useFormPage'

const props = defineProps({
  show: { type: Boolean, default: false },
  title: { type: String, default: '' }
})

const emit = defineEmits(['close'])
const isMobile = useIsMobile()
const route = useRoute()
const pushed = ref(false)
let closingFromPop = false

function requestClose() {
  emit('close')
}

function pushFormHistory() {
  if (!pushed.value && !history.state?.appForm) {
    history.pushState({ appForm: true, formPath: route.fullPath }, '')
    pushed.value = true
  }
}

function popFormHistory() {
  if (pushed.value && !closingFromPop) {
    pushed.value = false
    if (history.state?.appForm) history.back()
  } else {
    pushed.value = false
  }
}

function syncMobileChrome(show, mobile) {
  if (show && mobile) {
    registerFormPage({
      title: props.title,
      close: requestClose
    })
  } else {
    unregisterFormPage(requestClose)
  }
}

function onPopState() {
  if (hasActiveOverlay()) return
  if (!props.show) return
  if (history.state?.appForm) return
  closingFromPop = true
  pushed.value = false
  unregisterFormPage(requestClose)
  emit('close')
  queueMicrotask(() => { closingFromPop = false })
}

watch(
  () => props.show,
  (show) => {
    if (show) pushFormHistory()
    else popFormHistory()
    syncMobileChrome(show, isMobile.value)
  },
  { immediate: true }
)

watch(isMobile, (mobile) => {
  syncMobileChrome(props.show, mobile)
})

watch(
  () => props.title,
  (title) => {
    if (props.show && isMobile.value) updateFormPageTitle(title)
  }
)

// Leaving the route while form is open should discard form page state
watch(
  () => route.fullPath,
  (to, from) => {
    if (!props.show) return
    if (to !== from) {
      pushed.value = false
      unregisterFormPage(requestClose)
      emit('close')
    }
  }
)

onMounted(() => window.addEventListener('popstate', onPopState))
onBeforeUnmount(() => {
  window.removeEventListener('popstate', onPopState)
  unregisterFormPage(requestClose)
  // Drop the extra history entry if still open so Back doesn't reopen a dead form
  if (pushed.value && history.state?.appForm) {
    pushed.value = false
    history.back()
  }
})

defineExpose({ isMobile })
</script>

<template>
  <!-- Mobile: full-page form state under the top bar -->
  <div v-if="show && isMobile" class="form-page">
    <slot :is-mobile="true" />
  </div>

  <!-- Desktop: full-width inline form -->
  <div v-else-if="show" class="card form-panel">
    <div v-if="title" class="form-panel-header">
      <h2 class="form-panel-title">{{ title }}</h2>
    </div>
    <slot :is-mobile="false" />
  </div>
</template>

<style scoped>
.form-page {
  position: fixed;
  top: calc(56px + env(safe-area-inset-top, 0));
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 180;
  background: var(--bg);
  overflow-y: auto;
  -webkit-overflow-scrolling: touch;
  overscroll-behavior-y: contain;
  padding: 1rem;
  padding-bottom: calc(1rem + env(safe-area-inset-bottom, 0));
}
</style>
