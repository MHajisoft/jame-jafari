import router from '../router'
import { useToastStore } from '../stores/toast'
import { hasActiveOverlay } from './useOverlayBack'
import { activeFormPage } from './useFormPage'

/** Bottom primary tabs — back here can exit the installed PWA (not nested pages). */
const PRIMARY_EXIT_PATHS = ['/income', '/cost', '/reports', '/more', '/login']

const EXIT_GUARD = 'appExitGuard'
const EXIT_HINT = 'برای خروج دوباره دکمه بازگشت را بزنید'
const EXIT_ARM_MS = 2000

let initialized = false
let exitArmed = false
let armTimer = null
let ensuringGuard = false

function isStandalonePwa() {
  return (
    window.matchMedia('(display-mode: standalone)').matches ||
    window.navigator.standalone === true ||
    document.referrer.includes('android-app://')
  )
}

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
    (key) => key.startsWith('app') && key !== EXIT_GUARD && state[key]
  )
}

function canManageExit() {
  return isStandalonePwa() && isMobileViewport() && isExitRootPath() && !hasBlockingLayer()
}

function ensureExitGuard() {
  if (!canManageExit() || ensuringGuard) return
  if (history.state?.[EXIT_GUARD]) return
  ensuringGuard = true
  try {
    history.pushState({ ...history.state, [EXIT_GUARD]: true }, '')
  } finally {
    queueMicrotask(() => {
      ensuringGuard = false
    })
  }
}

function clearArm() {
  exitArmed = false
  if (armTimer != null) {
    clearTimeout(armTimer)
    armTimer = null
  }
}

function armExit() {
  exitArmed = true
  if (armTimer != null) clearTimeout(armTimer)
  armTimer = window.setTimeout(() => {
    exitArmed = false
    armTimer = null
  }, EXIT_ARM_MS)
  useToastStore().info(EXIT_HINT, { duration: EXIT_ARM_MS })
}

function onPopState() {
  // Let form/overlay handlers settle first
  queueMicrotask(() => {
    if (!isStandalonePwa() || !isMobileViewport()) return
    if (!isExitRootPath() || hasBlockingLayer()) {
      clearArm()
      return
    }

    // Guard still present → this popstate was for another layer; keep armed cleared
    if (history.state?.[EXIT_GUARD]) {
      clearArm()
      return
    }

    if (exitArmed) {
      clearArm()
      // Leave the PWA session — Android closes/minimizes the standalone app
      history.back()
      return
    }

    armExit()
    ensureExitGuard()
  })
}

/**
 * Standalone mobile PWA: on primary tabs, Android/gesture back exits the app
 * after a second press (toast hint). Nested pages/forms/overlays still win first.
 */
export function initPwaExitBack() {
  if (initialized || typeof window === 'undefined') return
  initialized = true

  window.addEventListener('popstate', onPopState)
  router.afterEach(() => {
    queueMicrotask(() => {
      clearArm()
      ensureExitGuard()
    })
  })

  // First gesture makes the guard reliable on browsers that ignore silent pushState
  const onFirstGesture = () => {
    ensureExitGuard()
    window.removeEventListener('pointerdown', onFirstGesture)
  }
  window.addEventListener('pointerdown', onFirstGesture, { passive: true })

  queueMicrotask(() => ensureExitGuard())
}
