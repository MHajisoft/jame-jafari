import router from '../router'
import { useToastStore } from '../stores/toast'
import { hasActiveOverlay } from './useOverlayBack'
import { activeFormPage } from './useFormPage'

/** Bottom primary tabs — back here can exit the installed PWA (not nested pages). */
const PRIMARY_EXIT_PATHS = ['/income', '/cost', '/reports', '/more', '/login']

const EXIT_GUARD = 'appExitGuard'
const EXIT_HINT = 'برای خروج دوباره دکمه بازگشت را بزنید'
const EXIT_ARM_MS = 2000
const PWA_FLAG = 'jj-pwa-standalone'

let initialized = false
/** Timestamp of the last exit toast / first back at root. */
let lastArmAt = 0
let allowingExit = false

function markStandalone() {
  try {
    sessionStorage.setItem(PWA_FLAG, '1')
  } catch {
    /* private mode */
  }
}

function isStandalonePwa() {
  if (typeof window === 'undefined') return false
  const now =
    window.matchMedia('(display-mode: standalone)').matches ||
    window.matchMedia('(display-mode: minimal-ui)').matches ||
    window.navigator.standalone === true ||
    document.documentElement.classList.contains('pwa-standalone') ||
    document.referrer.includes('android-app://')
  if (now) {
    markStandalone()
    return true
  }
  try {
    return sessionStorage.getItem(PWA_FLAG) === '1'
  } catch {
    return false
  }
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
    (key) => key.startsWith('app') && key !== EXIT_GUARD && !!state[key]
  )
}

function canManageExit() {
  return isStandalonePwa() && isMobileViewport() && isExitRootPath() && !hasBlockingLayer()
}

function showExitHint() {
  lastArmAt = Date.now()
  try {
    useToastStore().info(EXIT_HINT, { duration: EXIT_ARM_MS })
  } catch {
    /* pinia not ready */
  }
}

function isArmed() {
  return lastArmAt > 0 && Date.now() - lastArmAt < EXIT_ARM_MS
}

function clearArm() {
  lastArmAt = 0
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
  // Let the second-press navigation leave without trapping again
  if (allowingExit) {
    allowingExit = false
    return
  }

  if (!isStandalonePwa() || !isMobileViewport()) return

  // Overlay / form / nested page owns this back
  if (hasBlockingLayer() || !isExitRootPath()) {
    clearArm()
    return
  }

  // Guard entry still current (shouldn't normally happen on a user back)
  if (history.state?.[EXIT_GUARD]) return

  // Second back within window → exit PWA
  if (isArmed()) {
    clearArm()
    allowingExit = true
    history.back()
    return
  }

  // First back: toast + put guard back immediately (sync — before paint/unload)
  showExitHint()
  pushExitGuard()
}

/**
 * Standalone mobile PWA: on primary tabs, system back shows a toast and exits
 * only on a second press within 2s. Overlays/forms/nested routes still win first.
 */
export function initPwaExitBack() {
  if (initialized || typeof window === 'undefined') return
  initialized = true

  if (isStandalonePwa()) markStandalone()

  window.addEventListener('popstate', onPopState)

  router.afterEach((to) => {
    if (!PRIMARY_EXIT_PATHS.includes(to.path)) {
      clearArm()
      return
    }
    // Sync — Vue Router has already committed history for this navigation
    maintainExitGuard()
  })

  window.addEventListener('pageshow', () => maintainExitGuard())
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible') maintainExitGuard()
  })

  // Keep guard on top after user interaction (also satisfies gesture requirements)
  document.addEventListener(
    'pointerdown',
    () => {
      maintainExitGuard()
    },
    { passive: true }
  )

  maintainExitGuard()
}
