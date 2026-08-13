import axios from 'axios'
import { useToastStore } from '../stores/toast'
import { useLoadingStore } from '../stores/loading'

const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' }
})

function trackLoading(config, active) {
  if (config?.skipGlobalLoader) return
  try {
    const loading = useLoadingStore()
    if (active) loading.start()
    else loading.stop()
  } catch {
    /* pinia may be unavailable during early boot */
  }
}

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  trackLoading(config, true)
  return config
})

api.interceptors.response.use(
  (r) => {
    trackLoading(r.config, false)
    return r
  },
  (error) => {
    trackLoading(error.config, false)
    if (error.response?.status === 401) {
      localStorage.removeItem('token')
      localStorage.removeItem('user')
      if (!window.location.pathname.includes('/login')) {
        window.location.href = '/login'
      }
    } else if (!error.config?.skipErrorToast) {
      try {
        useToastStore().fromApiError(error)
      } catch {
        /* pinia may be unavailable during early boot */
      }
    }
    return Promise.reject(error)
  }
)

export default api
