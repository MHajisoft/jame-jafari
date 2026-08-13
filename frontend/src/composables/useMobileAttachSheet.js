import { computed, ref, unref } from 'vue'
import { useIsMobile } from './useMediaQuery'
import { useOverlayBack } from './useOverlayBack'
import {
  readInputFiles,
  scheduleInputClickAfterSheetClose,
  emptyCaptureToast
} from './useMobileFilePicker'

/** Shared overlay history key — avatar + attachment sheets are mutually exclusive. */
export const MOBILE_ATTACH_SHEET_STATE_KEY = 'appMobileAttachSheet'

/**
 * Unified mobile attach sheet + native camera/gallery pipeline.
 * Used by AvatarPicker and TransactionAttachmentsField (same behavior, different camera).
 *
 * @param {{
 *   onFiles: (files: File[]) => void,
 *   cameraFacing?: 'user' | 'environment' | (() => 'user' | 'environment'),
 *   enabled?: () => boolean
 * }} options
 */
export function useMobileAttachSheet({ onFiles, cameraFacing = 'environment', enabled }) {
  const isMobile = useIsMobile()
  const sheetOpen = ref(false)
  const fileInput = ref(null)
  const cameraInput = ref(null)

  const captureAttr = computed(() => {
    const facing = typeof cameraFacing === 'function' ? cameraFacing() : unref(cameraFacing)
    return facing === 'user' ? 'user' : 'environment'
  })

  function closeSheet() {
    sheetOpen.value = false
  }

  useOverlayBack(sheetOpen, closeSheet, {
    enabled: () => isMobile.value && (enabled?.() ?? true),
    stateKey: MOBILE_ATTACH_SHEET_STATE_KEY
  })

  async function onFileChange(e) {
    const files = await readInputFiles(e, { onEmpty: emptyCaptureToast })
    if (!files.length) return
    onFiles(files)
  }

  function openSheetOrFile() {
    if (isMobile.value) sheetOpen.value = true
    else fileInput.value?.click()
  }

  function openGallery() {
    scheduleInputClickAfterSheetClose(sheetOpen, fileInput)
  }

  function openCamera() {
    scheduleInputClickAfterSheetClose(sheetOpen, cameraInput, { camera: true })
  }

  return {
    isMobile,
    sheetOpen,
    fileInput,
    cameraInput,
    captureAttr,
    closeSheet,
    onFileChange,
    openSheetOrFile,
    openGallery,
    openCamera
  }
}
