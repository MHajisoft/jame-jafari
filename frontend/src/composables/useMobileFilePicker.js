import { nextTick, ref } from 'vue'
import { useToastStore } from '../stores/toast'

/** Delay before opening gallery input after sheet closes (mobile browsers). */
export const MOBILE_PICKER_DELAY_MS = 220
/** Extra delay for camera — sheet must leave DOM before native camera opens. */
export const MOBILE_CAMERA_DELAY_MS = 480

const CAPTURE_RETRY_MS = 150
const CAPTURE_MAX_RETRIES = 6
const CAPTURE_CAMERA_RETRIES = 14
const INPUT_CLEAR_DELAY_MS = 320
const PICKER_CANCEL_MS = 1200

/**
 * True while the OS camera/gallery is open (from input click until change/cancel).
 * FormHost must ignore popstate during this window.
 */
export const nativeFilePickerActive = ref(false)

/**
 * True from sheet-close until native picker completes — defers overlay history.pop
 * during the pre-click delay (before nativeFilePickerActive is set).
 */
export const deferOverlayHistoryPop = ref(false)

let pickerCancelTimer = null
let pickerChangePending = false

function clearPickerCancelTimer() {
  if (pickerCancelTimer) {
    clearTimeout(pickerCancelTimer)
    pickerCancelTimer = null
  }
}

function beginNativeFilePicker() {
  nativeFilePickerActive.value = true
  pickerChangePending = false
  window.addEventListener('focus', onNativePickerReturn, true)
  document.addEventListener('visibilitychange', onNativePickerVisibility, true)
}

function endNativeFilePicker() {
  nativeFilePickerActive.value = false
  deferOverlayHistoryPop.value = false
  pickerChangePending = false
  clearPickerCancelTimer()
  window.removeEventListener('focus', onNativePickerReturn, true)
  document.removeEventListener('visibilitychange', onNativePickerVisibility, true)
}

function onNativePickerVisibility() {
  if (!nativeFilePickerActive.value && !deferOverlayHistoryPop.value) return
  // Leaving for camera — do not treat as cancel.
  if (document.visibilityState === 'hidden') {
    clearPickerCancelTimer()
    return
  }
  schedulePickerCancelCheck()
}

function onNativePickerReturn() {
  if (!nativeFilePickerActive.value && !deferOverlayHistoryPop.value) return
  schedulePickerCancelCheck()
}

function schedulePickerCancelCheck() {
  if (pickerChangePending) return
  clearPickerCancelTimer()
  pickerCancelTimer = window.setTimeout(() => {
    if (pickerChangePending) return
    if (nativeFilePickerActive.value || deferOverlayHistoryPop.value) {
      endNativeFilePicker()
    }
  }, PICKER_CANCEL_MS)
}

/**
 * @param {File} file
 * @returns {File}
 */
export function normalizeCaptureFile(file) {
  if (!file?.size) return file
  const type = file.type || 'image/jpeg'
  const hasExt = /\.[a-z0-9]+$/i.test(file.name || '')
  if (hasExt && file.type) return file
  const ext = type.includes('png') ? '.png' : type.includes('webp') ? '.webp' : '.jpg'
  const name = hasExt ? file.name : `capture-${Date.now()}${ext}`
  return new File([file], name, { type, lastModified: file.lastModified || Date.now() })
}

async function waitForCaptureFiles(input, isCamera) {
  const maxRetries = isCamera ? CAPTURE_CAMERA_RETRIES : CAPTURE_MAX_RETRIES
  for (let attempt = 0; attempt < maxRetries; attempt++) {
    const list = input.files
    if (list?.length) {
      const files = Array.from(list).filter((f) => f && f.size > 0)
      if (files.length) return files
    }
    await new Promise((r) => window.setTimeout(r, CAPTURE_RETRY_MS))
  }
  return []
}

/**
 * @param {Event} event
 * @param {{ onEmpty?: () => void }} [options]
 * @returns {Promise<File[]>}
 */
export async function readInputFiles(event, { onEmpty } = {}) {
  pickerChangePending = true
  clearPickerCancelTimer()

  const input = event.target
  if (!input) {
    onEmpty?.()
    endNativeFilePicker()
    return []
  }

  try {
    const isCamera = input.hasAttribute('capture')
    let files = await waitForCaptureFiles(input, isCamera)
    if (!files.length) {
      onEmpty?.()
    } else {
      files = files.map(normalizeCaptureFile)
    }

    window.setTimeout(() => {
      input.value = ''
    }, INPUT_CLEAR_DELAY_MS)

    return files
  } finally {
    endNativeFilePicker()
  }
}

export function emptyCaptureToast() {
  try {
    useToastStore().error('عکس از دوربین دریافت نشد. دوباره تلاش کنید.')
  } catch {
    /* pinia unavailable */
  }
}

/** @param {HTMLInputElement} el */
function openFileInput(el, camera) {
  if (!camera && typeof el.showPicker === 'function') {
    try {
      el.showPicker()
      return
    } catch {
      /* fallback */
    }
  }
  el.click()
}

/**
 * Close sheet, wait for unmount, then open native picker.
 * @param {import('vue').Ref<boolean>} sheetOpenRef
 * @param {import('vue').Ref<HTMLInputElement | null>} inputRef
 * @param {{ camera?: boolean }} [options]
 */
export function scheduleInputClickAfterSheetClose(sheetOpenRef, inputRef, { camera = false } = {}) {
  deferOverlayHistoryPop.value = true
  sheetOpenRef.value = false
  const delay = camera ? MOBILE_CAMERA_DELAY_MS : MOBILE_PICKER_DELAY_MS

  nextTick().then(() => {
    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        window.setTimeout(() => {
          const el = inputRef.value
          if (!el) {
            endNativeFilePicker()
            return
          }
          beginNativeFilePicker()
          openFileInput(el, camera)
        }, delay)
      })
    })
  })
}

/** Desktop / direct open without sheet. */
export function scheduleInputClick(inputRef, { camera = false } = {}) {
  deferOverlayHistoryPop.value = true
  const delay = camera ? MOBILE_CAMERA_DELAY_MS : MOBILE_PICKER_DELAY_MS
  window.setTimeout(() => {
    const el = inputRef.value
    if (!el) {
      endNativeFilePicker()
      return
    }
    beginNativeFilePicker()
    openFileInput(el, camera)
  }, delay)
}

/** Used by overlay back to defer history.pop while picker pipeline is active. */
export function isOverlayHistoryDeferred() {
  return nativeFilePickerActive.value || deferOverlayHistoryPop.value
}
