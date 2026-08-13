import { registerSW } from 'virtual:pwa-register'

/** How often to poll for a new service worker while the app stays open. */
const CHECK_INTERVAL_MS = 30 * 60 * 1000

let initialized = false
let checkTimer = null
let swRegistration = null

function checkForUpdate() {
  swRegistration?.update?.().catch(() => {})
}

function onVisibilityChange() {
  if (document.visibilityState === 'visible') checkForUpdate()
}

function scheduleChecks(registration) {
  swRegistration = registration || null
  if (!registration) return
  clearInterval(checkTimer)
  checkTimer = window.setInterval(checkForUpdate, CHECK_INTERVAL_MS)
}

/**
 * Register the PWA service worker in autoUpdate mode.
 * New builds activate and reload the page without a user prompt.
 * Call once at app boot.
 */
export function initPwaUpdate() {
  if (initialized || typeof window === 'undefined') return
  initialized = true

  registerSW({
    immediate: true,
    onRegisteredSW(_swUrl, registration) {
      scheduleChecks(registration)
    }
  })

  document.addEventListener('visibilitychange', onVisibilityChange)
}
