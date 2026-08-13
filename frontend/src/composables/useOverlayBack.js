import { onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { deferOverlayHistoryPop, nativeFilePickerActive } from './useMobileFilePicker'

/** @type {import('vue').Ref<null | { close: () => void }>} */
export const activeOverlay = ref(null)

export function hasActiveOverlay() {
  return !!activeOverlay.value
}

export function closeActiveOverlay() {
  if (!activeOverlay.value) return false
  activeOverlay.value.close()
  return true
}

function isHistoryDeferred(deferPop) {
  return deferPop() || nativeFilePickerActive.value || deferOverlayHistoryPop.value
}

/**
 * @param {import('vue').Ref<boolean>} isOpen
 * @param {() => void} requestClose
 * @param {{ enabled?: () => boolean, stateKey?: string, deferPop?: () => boolean }} [options]
 */
export function useOverlayBack(isOpen, requestClose, options = {}) {
  const stateKey = options.stateKey ?? 'appOverlay'
  const isEnabled = options.enabled ?? (() => true)
  const deferPop = options.deferPop ?? (() => false)

  const pushed = ref(false)
  let closingFromPop = false
  const closeFn = requestClose

  function pushHistory() {
    if (!isEnabled()) return
    if (!pushed.value) {
      history.pushState({ ...history.state, [stateKey]: true }, '')
      pushed.value = true
    }
  }

  function releaseHistory() {
    if (!pushed.value || closingFromPop) return
    const shouldBack = !!history.state?.[stateKey]
    pushed.value = false
    if (shouldBack) history.back()
  }

  function popHistory() {
    if (isHistoryDeferred(deferPop)) return
    releaseHistory()
  }

  function syncRegistry(open) {
    if (!isEnabled()) {
      if (activeOverlay.value?.close === closeFn) activeOverlay.value = null
      return
    }
    if (open) activeOverlay.value = { close: closeFn }
    else if (activeOverlay.value?.close === closeFn) activeOverlay.value = null
  }

  function onPopState() {
    if (!isOpen.value || !isEnabled()) return
    if (nativeFilePickerActive.value || deferOverlayHistoryPop.value) return
    if (history.state?.[stateKey]) return

    closingFromPop = true
    pushed.value = false
    activeOverlay.value = null
    requestClose()
    queueMicrotask(() => { closingFromPop = false })
  }

  function maybeReleaseAfterPicker() {
    if (!isHistoryDeferred(deferPop) && !isOpen.value && pushed.value) {
      releaseHistory()
    }
  }

  watch(isOpen, (open) => {
    if (open) pushHistory()
    else popHistory()
    syncRegistry(open)
  }, { immediate: true })

  watch([nativeFilePickerActive, deferOverlayHistoryPop], maybeReleaseAfterPicker)

  watch(() => isEnabled(), (ok) => {
    if (!ok) {
      if (isOpen.value) requestClose()
      else syncRegistry(false)
    } else {
      syncRegistry(isOpen.value)
    }
  })

  onMounted(() => window.addEventListener('popstate', onPopState))
  onBeforeUnmount(() => {
    window.removeEventListener('popstate', onPopState)
    if (activeOverlay.value?.close === closeFn) activeOverlay.value = null
    if (pushed.value && history.state?.[stateKey]) {
      pushed.value = false
      history.back()
    }
  })

  return { requestClose: closeFn, releaseHistory }
}
