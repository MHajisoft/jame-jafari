import { createApp, h, ref } from 'vue'
import router from '../router'
import { hasActiveOverlay } from './useOverlayBack'
import { activeFormPage } from './useFormPage'

/** Bottom primary tabs — back here can exit / leave after a confirm press. */
const PRIMARY_EXIT_PATHS = ['/income', '/cost', '/reports', '/more', '/login']

const EXIT_GUARD = 'appExitGuard'
const EXIT_HINT = 'برای خروج دوباره دکمه بازگشت را بزنید'
const EXIT_ARM_MS = 2000

let initialized = false
let lastArmAt = 0
let allowingExit = false
let hintApi = null

function isMobileViewport() {
  return window.matchMedia('(max-width: 768px)').matches
}

function currentPath() {
  return router.currentRoute.value?.path || window.location.pathname
}

function isExitRootPath(path = currentPath()) {
  return PRIMARY_EXIT_PATHS.includes(path)
}

function hasBlockingLayer() {
  if (hasActiveOverlay()) return true
  if (activeFormPage.value) return true
  const state = history.state
  if (!state || typeof state !== 'object') return false
  return Object.keys(state).some(
    (key) => key.startsWith('app') && key !== EXIT_GUARD && !!state[key]
  )
}

function canManageExit() {
  return isMobileViewport() && isExitRootPath() && !hasBlockingLayer()
}

function ensureHintUi() {
  if (hintApi || typeof document === 'undefined') return hintApi
  const visible = ref(false)
  const message = ref(EXIT_HINT)
  let hideTimer = null

  const Host = {
    setup() {
      return () =>
        visible.value
          ? h(
              'div',
              {
                class: 'pwa-exit-hint',
                role: 'status',
                'aria-live': 'assertive'
              },
              message.value
            )
          : null
    }
  }

  const mountEl = document.createElement('div')
  mountEl.id = 'pwa-exit-hint-host'
  document.body.appendChild(mountEl)
  createApp(Host).mount(mountEl)

  hintApi = {
    show(text = EXIT_HINT, ms = EXIT_ARM_MS) {
      message.value = text
      visible.value = true
      if (hideTimer != null) clearTimeout(hideTimer)
      hideTimer = window.setTimeout(() => {
        visible.value = false
        hideTimer = null
      }, ms)
    },
    hide() {
      visible.value = false
      if (hideTimer != null) {
        clearTimeout(hideTimer)
        hideTimer = null
      }
    }
  }
  return hintApi
}

function isArmed() {
  return lastArmAt > 0 && Date.now() - lastArmAt < EXIT_ARM_MS
}

function clearArm() {
  lastArmAt = 0
  hintApi?.hide()
}

function showExitHint() {
  lastArmAt = Date.now()
  ensureHintUi()?.show(EXIT_HINT, EXIT_ARM_MS)
}

function pushExitGuard() {
  if (history.state?.[EXIT_GUARD]) return
  history.pushState({ ...(history.state || {}), [EXIT_GUARD]: true }, '')
}

function maintainExitGuard() {
  if (!canManageExit()) return
  pushExitGuard()
}

function onPopState() {
  if (allowingExit) {
    allowingExit = false
    return
  }

  if (!isMobileViewport()) return

  if (hasBlockingLayer() || !isExitRootPath()) {
    clearArm()
    return
  }

  if (history.state?.[EXIT_GUARD]) return

  if (isArmed()) {
    clearArm()
    allowingExit = true
    history.back()
    return
  }

  // First back at root: show hint and restore guard immediately (sync)
  showExitHint()
  pushExitGuard()
}

/**
 * Mobile: on primary tabs, system back shows an exit hint and leaves only on
 * a second press within 2s. Overlays/forms/nested routes still win first.
 */
export function initPwaExitBack() {
  if (initialized || typeof window === 'undefined') return
  initialized = true

  ensureHintUi()
  window.addEventListener('popstate', onPopState)

  router.afterEach((to) => {
    if (!PRIMARY_EXIT_PATHS.includes(to.path)) {
      clearArm()
      return
    }
    maintainExitGuard()
  })

  window.addEventListener('pageshow', () => maintainExitGuard())
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible') maintainExitGuard()
  })
  document.addEventListener('pointerdown', () => maintainExitGuard(), { passive: true })

  maintainExitGuard()
}
