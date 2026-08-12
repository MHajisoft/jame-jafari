import { computed, onMounted, onBeforeUnmount, ref, readonly } from 'vue'

const STORAGE_DISMISS = 'pwa-install-dismissed-at'
const DISMISS_DAYS = 14

const deferredPrompt = ref(null)
const standalone = ref(false)
const ios = ref(false)
const dismissed = ref(false)
const installed = ref(false)
let listenersBound = false
let displayModeMql = null
let consumerCount = 0

function isStandalone() {
  if (typeof window === 'undefined') return false
  return (
    window.matchMedia('(display-mode: standalone)').matches ||
    window.navigator.standalone === true ||
    document.referrer.includes('android-app://')
  )
}

function isIos() {
  if (typeof navigator === 'undefined') return false
  return /iphone|ipad|ipod/i.test(navigator.userAgent) ||
    (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1)
}

function isDismissedRecently() {
  const raw = localStorage.getItem(STORAGE_DISMISS)
  if (!raw) return false
  const ts = Number(raw)
  if (!Number.isFinite(ts)) return false
  return Date.now() - ts < DISMISS_DAYS * 24 * 60 * 60 * 1000
}

function onBeforeInstallPrompt(e) {
  e.preventDefault()
  deferredPrompt.value = e
}

function onAppInstalled() {
  installed.value = true
  deferredPrompt.value = null
  standalone.value = true
  document.documentElement.classList.add('pwa-standalone')
  document.body.classList.add('pwa-standalone')
}

function onDisplayModeChange() {
  standalone.value = isStandalone()
  if (standalone.value) {
    installed.value = true
    document.documentElement.classList.add('pwa-standalone')
    document.body.classList.add('pwa-standalone')
  }
}

function ensureListeners() {
  if (listenersBound || typeof window === 'undefined') return
  standalone.value = isStandalone()
  ios.value = isIos()
  dismissed.value = isDismissedRecently()
  installed.value = standalone.value
  window.addEventListener('beforeinstallprompt', onBeforeInstallPrompt)
  window.addEventListener('appinstalled', onAppInstalled)
  displayModeMql = window.matchMedia('(display-mode: standalone)')
  displayModeMql.addEventListener?.('change', onDisplayModeChange)
  listenersBound = true
}

function releaseListeners() {
  if (!listenersBound || consumerCount > 0) return
  window.removeEventListener('beforeinstallprompt', onBeforeInstallPrompt)
  window.removeEventListener('appinstalled', onAppInstalled)
  displayModeMql?.removeEventListener?.('change', onDisplayModeChange)
  listenersBound = false
}

export function usePwaInstall() {
  const canPrompt = computed(() => !!deferredPrompt.value && !standalone.value && !installed.value)
  const showIosHint = computed(() => ios.value && !standalone.value && !installed.value)
  const canShowBanner = computed(() => {
    if (standalone.value || installed.value || dismissed.value) return false
    return canPrompt.value || showIosHint.value
  })

  async function promptInstall() {
    if (!deferredPrompt.value) return false
    const promptEvent = deferredPrompt.value
    deferredPrompt.value = null
    promptEvent.prompt()
    const choice = await promptEvent.userChoice
    if (choice?.outcome === 'accepted') {
      installed.value = true
      return true
    }
    return false
  }

  function dismiss() {
    dismissed.value = true
    localStorage.setItem(STORAGE_DISMISS, String(Date.now()))
  }

  onMounted(() => {
    consumerCount += 1
    ensureListeners()
  })

  onBeforeUnmount(() => {
    consumerCount = Math.max(0, consumerCount - 1)
    releaseListeners()
  })

  return {
    standalone: readonly(standalone),
    ios: readonly(ios),
    canPrompt,
    showIosHint,
    canShowBanner,
    promptInstall,
    dismiss
  }
}

/** Call once at app boot so the install event is never missed. */
export function initPwaInstallListeners() {
  ensureListeners()
}
