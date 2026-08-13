import { onBeforeUnmount, onMounted, ref, watch } from 'vue'

/** @type {import('vue').Ref<null | { close: () => void }>} */
export const activeOverlay = ref(null)

export function hasActiveOverlay() {
  return !!activeOverlay.value
}

/** Top-bar back: close the frontmost overlay. Returns true if handled. */
export function closeActiveOverlay() {
  if (!activeOverlay.value) return false
  activeOverlay.value.close()
  return true
}

/**
 * History + hardware/gesture back for mobile overlays (preview, bottom sheet, …).
 * Same contract as FormHost: pushState on open, pop on close, popstate closes overlay.
 *
 * Must spread `history.state` so Vue Router's position/current keys stay intact.
 *
 * @param {import('vue').Ref<boolean>} isOpen
 * @param {() => void} requestClose
 * @param {{ enabled?: () => boolean, stateKey?: string }} [options]
 */
export function useOverlayBack(isOpen, requestClose, options = {}) {
  const stateKey = options.stateKey ?? 'appOverlay'
  const isEnabled = options.enabled ?? (() => true)

  const pushed = ref(false)
  let closingFromPop = false
  let closeFn = requestClose

  function pushHistory() {
    if (!isEnabled()) return
    if (!pushed.value) {
      history.pushState({ ...history.state, [stateKey]: true }, '')
      pushed.value = true
    }
  }

  function popHistory() {
    // Always unwind our entry if we pushed — even if enabled flipped off (e.g. resize).
    if (pushed.value && !closingFromPop) {
      const shouldBack = !!history.state?.[stateKey]
      pushed.value = false
      if (shouldBack) history.back()
    } else {
      pushed.value = false
    }
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
    // Still on this overlay's history entry — ignore unrelated pops (e.g. child closed).
    if (history.state?.[stateKey]) return

    closingFromPop = true
    pushed.value = false
    activeOverlay.value = null
    requestClose()
    queueMicrotask(() => { closingFromPop = false })
  }

  watch(
    isOpen,
    (open) => {
      if (open) pushHistory()
      else popHistory()
      syncRegistry(open)
    },
    { immediate: true }
  )

  watch(
    () => isEnabled(),
    (ok) => {
      if (!ok) {
        if (isOpen.value) requestClose()
        else syncRegistry(false)
      } else {
        syncRegistry(isOpen.value)
      }
    }
  )

  onMounted(() => window.addEventListener('popstate', onPopState))
  onBeforeUnmount(() => {
    window.removeEventListener('popstate', onPopState)
    if (activeOverlay.value?.close === closeFn) activeOverlay.value = null
    if (pushed.value && history.state?.[stateKey]) {
      pushed.value = false
      history.back()
    }
  })

  return { requestClose: closeFn }
}
