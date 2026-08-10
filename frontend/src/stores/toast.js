import { defineStore } from 'pinia'

let seq = 0

const DEFAULT_DURATION = {
  success: 3200,
  error: 5200,
  warning: 4200,
  info: 3800
}

function messageFromApiError(error) {
  const status = error?.response?.status
  const data = error?.response?.data

  if (!error?.response) {
    if (error?.code === 'ECONNABORTED') {
      return 'زمان درخواست به پایان رسید. دوباره تلاش کنید.'
    }
    return 'ارتباط با سرور برقرار نشد. اتصال اینترنت را بررسی کنید.'
  }

  if (typeof data === 'string' && data.trim()) return data.trim()

  if (data && typeof data === 'object') {
    if (data.errors && typeof data.errors === 'object') {
      const first = Object.values(data.errors).flat().find(Boolean)
      if (first) return String(first)
      return 'لطفاً خطاهای فرم را بررسی کنید.'
    }
    if (data.detail) return String(data.detail)
    if (data.title && data.title !== 'One or more validation errors occurred.') {
      return String(data.title)
    }
    if (data.message) return String(data.message)
  }

  switch (status) {
    case 400:
      return 'درخواست نامعتبر است.'
    case 401:
      return 'نشست شما منقضی شده است. دوباره وارد شوید.'
    case 403:
      return 'شما دسترسی لازم برای این عملیات را ندارید.'
    case 404:
      return 'مورد درخواستی یافت نشد.'
    case 409:
      return 'این مورد با داده‌های موجود تداخل دارد.'
    case 422:
      return 'اطلاعات ارسال‌شده قابل پردازش نیست.'
    case 429:
      return 'تعداد درخواست‌ها زیاد است. کمی بعد تلاش کنید.'
    case 500:
    case 502:
    case 503:
    case 504:
      return 'خطای سرور رخ داد. لطفاً دوباره تلاش کنید.'
    default:
      return 'خطایی رخ داد. لطفاً دوباره تلاش کنید.'
  }
}

export const useToastStore = defineStore('toast', {
  state: () => ({
    items: []
  }),
  actions: {
    push(type, message, options = {}) {
      const text = String(message || '').trim()
      if (!text) return null

      const id = ++seq
      const duration = options.duration ?? DEFAULT_DURATION[type] ?? 4000
      const item = { id, type, message: text, duration }
      this.items.push(item)

      if (duration > 0) {
        window.setTimeout(() => this.dismiss(id), duration)
      }
      return id
    },
    success(message, options) {
      return this.push('success', message, options)
    },
    error(message, options) {
      return this.push('error', message, options)
    },
    warning(message, options) {
      return this.push('warning', message, options)
    },
    info(message, options) {
      return this.push('info', message, options)
    },
    dismiss(id) {
      this.items = this.items.filter((t) => t.id !== id)
    },
    clear() {
      this.items = []
    },
    fromApiError(error) {
      const status = error?.response?.status
      // 401 handled by redirect; avoid noisy toast during logout redirect
      if (status === 401) return null
      return this.error(messageFromApiError(error))
    }
  }
})

export { messageFromApiError }
