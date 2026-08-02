import { defineStore } from 'pinia'
import api from '../api/client'

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: localStorage.getItem('token') || '',
    username: '',
    permissions: []
  }),
  getters: {
    isAuthenticated: (s) => !!s.token,
    hasPermission: (s) => (code) => s.permissions.includes(code)
  },
  actions: {
    async login(username, password) {
      const { data } = await api.post('/auth/login', { username, password })
      this.token = data.token
      this.username = data.username
      this.permissions = data.permissions
      localStorage.setItem('token', data.token)
      localStorage.setItem('user', JSON.stringify({ username: data.username, permissions: data.permissions }))
    },
    loadFromStorage() {
      const user = localStorage.getItem('user')
      if (user) {
        const parsed = JSON.parse(user)
        this.username = parsed.username
        this.permissions = parsed.permissions
      }
    },
    logout() {
      this.token = ''
      this.username = ''
      this.permissions = []
      localStorage.removeItem('token')
      localStorage.removeItem('user')
    }
  }
})
