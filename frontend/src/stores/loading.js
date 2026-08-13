import { defineStore } from 'pinia'

const SHOW_DELAY_MS = 180

export const useLoadingStore = defineStore('loading', {
  state: () => ({
    pending: 0,
    visible: false,
    /** @type {ReturnType<typeof setTimeout> | null} */
    _showTimer: null
  }),
  actions: {
    start() {
      this.pending += 1
      if (this.pending === 1 && !this._showTimer) {
        this._showTimer = setTimeout(() => {
          this._showTimer = null
          if (this.pending > 0) this.visible = true
        }, SHOW_DELAY_MS)
      }
    },
    stop() {
      this.pending = Math.max(0, this.pending - 1)
      if (this.pending === 0) {
        if (this._showTimer) {
          clearTimeout(this._showTimer)
          this._showTimer = null
        }
        this.visible = false
      }
    }
  }
})
