import { defineStore } from 'pinia'

const SHOW_DELAY_MS = 180

const MSG = {
  load: 'در حال بارگذاری…',
  save: 'در حال ذخیره…',
  send: 'در حال ارسال…',
  remove: 'در حال حذف…',
  login: 'در حال ورود…',
  sync: 'در حال همگام‌سازی…'
}

function requestPath(config) {
  const raw = String(config?.url || '')
  let path = raw.split('?')[0]
  if (/^https?:\/\//i.test(path)) {
    try {
      path = new URL(raw).pathname
    } catch {
      /* keep path */
    }
  }
  path = path.toLowerCase()
  if (path.startsWith('/api/')) path = path.slice(4)
  return path
}

function requestQuery(config) {
  const raw = String(config?.url || '')
  const i = raw.indexOf('?')
  return i >= 0 ? raw.slice(i + 1).toLowerCase() : ''
}

export function loaderMessageForRequest(config) {
  if (config?.loaderMessage) return config.loaderMessage

  const method = (config?.method || 'get').toLowerCase()
  const path = requestPath(config)
  const query = requestQuery(config)

  if (method === 'get') return MSG.load

  if (method === 'delete') {
    if (path.includes('/messages/') && query.includes('scope=remote'))
      return 'در حال حذف از گفتگو…'
    return MSG.remove
  }

  if (method === 'put' || method === 'patch') return MSG.save

  if (method === 'post') {
    if (path.includes('/auth/login')) return MSG.login
    if (path.includes('/sync-contacts')) return MSG.sync
    if (path.includes('/sync-rubika') || path.includes('/discover-rubika')) return MSG.sync
    if (path.includes('/send-receipt')) return MSG.send
    if (path === '/messages' || path === '/messages/') return MSG.send
    return MSG.save
  }

  return MSG.load
}

export const useLoadingStore = defineStore('loading', {
  state: () => ({
    pending: 0,
    visible: false,
    message: MSG.load,
    /** @type {ReturnType<typeof setTimeout> | null} */
    _showTimer: null
  }),
  actions: {
    start(message) {
      this.pending += 1
      if (message) this.message = message
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
        this.message = MSG.load
      }
    }
  }
})
