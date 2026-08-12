import { defineStore } from 'pinia'

/**
 * App-level confirm / alert dialogs (replaces window.confirm / alert).
 * Usage:
 *   const ok = await dialog.confirm({ title, message, danger: true })
 *   await dialog.alert({ title, message })
 */
export const useDialogStore = defineStore('dialog', {
  state: () => ({
    open: false,
    mode: 'confirm', // 'confirm' | 'alert'
    title: '',
    message: '',
    confirmText: 'تأیید',
    cancelText: 'انصراف',
    danger: false,
    resolve: null
  }),
  actions: {
    confirm(options = {}) {
      return this._open({
        mode: 'confirm',
        title: options.title || 'تأیید',
        message: options.message || '',
        confirmText: options.confirmText || 'تأیید',
        cancelText: options.cancelText || 'انصراف',
        danger: !!options.danger
      })
    },
    alert(options = {}) {
      return this._open({
        mode: 'alert',
        title: options.title || 'توجه',
        message: options.message || '',
        confirmText: options.confirmText || 'متوجه شدم',
        cancelText: '',
        danger: !!options.danger
      }).then(() => true)
    },
    /** Destructive delete shortcut */
    confirmDelete(entityLabel = 'این مورد') {
      return this.confirm({
        title: 'حذف',
        message: `آیا از حذف ${entityLabel} مطمئن هستید؟ این عمل قابل بازگشت نیست.`,
        confirmText: 'حذف',
        cancelText: 'انصراف',
        danger: true
      })
    },
    _open(payload) {
      if (this.open && this.resolve) {
        this.resolve(false)
      }
      return new Promise((resolve) => {
        this.open = true
        this.mode = payload.mode
        this.title = payload.title
        this.message = payload.message
        this.confirmText = payload.confirmText
        this.cancelText = payload.cancelText
        this.danger = payload.danger
        this.resolve = resolve
      })
    },
    accept() {
      const resolve = this.resolve
      this._reset()
      resolve?.(true)
    },
    dismiss() {
      const resolve = this.resolve
      this._reset()
      resolve?.(false)
    },
    _reset() {
      this.open = false
      this.resolve = null
      this.title = ''
      this.message = ''
      this.danger = false
    }
  }
})
