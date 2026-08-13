import { ref, watch } from 'vue'
import api from '../api/client'

/**
 * Shared avatar field state for entity forms (person, user) and profile.
 * Deferred: file/path held until syncAvatar(id) on form submit.
 * Immediate: uploads/removes as soon as file or path changes (profile).
 */
export function useAvatarField(options = {}) {
  const {
    immediate = false,
    uploadUrl = null,
    deleteUrl = null,
    onUpload = null,
    onRemove = null,
    onError = null
  } = options

  const avatarFile = ref(null)
  const avatarPath = ref('')
  const initialAvatarPath = ref('')
  const busy = ref(false)

  function resetAvatarState(path = '') {
    avatarFile.value = null
    avatarPath.value = path || ''
    initialAvatarPath.value = path || ''
  }

  async function syncAvatar(id) {
    if (avatarFile.value) {
      const fd = new FormData()
      fd.append('file', avatarFile.value)
      await api.post(uploadUrl(id), fd, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
      return
    }
    if (initialAvatarPath.value && !avatarPath.value) {
      await api.delete(deleteUrl(id))
    }
  }

  async function runImmediateUpload(file) {
    if (!file || busy.value) return
    busy.value = true
    try {
      await onUpload(file)
      avatarFile.value = null
    } catch (err) {
      avatarFile.value = null
      onError?.(err)
      throw err
    } finally {
      busy.value = false
    }
  }

  async function runImmediateRemove() {
    if (busy.value) return
    busy.value = true
    try {
      await onRemove()
    } catch (err) {
      onError?.(err)
      throw err
    } finally {
      busy.value = false
    }
  }

  if (immediate) {
    watch(avatarFile, (file) => {
      if (file) runImmediateUpload(file)
    })

    watch(avatarPath, (path, prev) => {
      if (prev && !path && !avatarFile.value) runImmediateRemove()
    })
  }

  return {
    avatarFile,
    avatarPath,
    initialAvatarPath,
    busy,
    resetAvatarState,
    syncAvatar
  }
}
