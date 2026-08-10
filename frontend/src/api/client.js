import axios from 'axios'
import { useToastStore } from '../stores/toast'

const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' }
})

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  (r) => r,
  (error) => {
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
