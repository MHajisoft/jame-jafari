import { defineStore } from 'pinia'
import api from '../api/client'

function persistUser(state) {
  localStorage.setItem('user', JSON.stringify({
    id: state.id,
    username: state.username,
    email: state.email,
    mobile: state.mobile,
    avatarPath: state.avatarPath,
    permissions: state.permissions
  }))
}

function applyProfile(state, profile) {
  state.id = profile.id
  state.username = profile.username
  state.email = profile.email || ''
  state.mobile = profile.mobile || ''
  state.avatarPath = profile.avatarPath || ''
  if (Array.isArray(profile.permissions)) {
    state.permissions = profile.permissions
  }
}

export const useAuthStore = defineStore('auth', {
  state: () => ({
    token: localStorage.getItem('token') || '',
    id: null,
    username: '',
    email: '',
    mobile: '',
    avatarPath: '',
    permissions: []
  }),
  getters: {
    isAuthenticated: (s) => !!s.token,
    hasPermission: (s) => (code) => s.permissions.includes(code),
    hasAnyPermission: (s) => (...codes) => codes.some((c) => s.permissions.includes(c)),
    avatarUrl: (s) => (s.avatarPath ? `/uploads/${s.avatarPath}` : ''),
    initials: (s) => (s.username?.charAt(0)?.toUpperCase() || '؟')
  },
  actions: {
    async login(username, password) {
      const { data } = await api.post('/auth/login', { username, password })
      this.token = data.token
      localStorage.setItem('token', data.token)
      applyProfile(this, data)
      persistUser(this)
    },
    loadFromStorage() {
      const user = localStorage.getItem('user')
      if (!user) return
      try {
        const parsed = JSON.parse(user)
        this.id = parsed.id ?? null
        this.username = parsed.username || ''
        this.email = parsed.email || ''
        this.mobile = parsed.mobile || ''
        this.avatarPath = parsed.avatarPath || ''
        this.permissions = parsed.permissions || []
      } catch {
        /* ignore corrupt storage */
      }
    },
    async fetchProfile() {
      const { data } = await api.get('/profile')
      applyProfile(this, data)
      persistUser(this)
      return data
    },
    async updateProfile(payload) {
      const { data } = await api.put('/profile', payload)
      applyProfile(this, data)
      persistUser(this)
      return data
    },
    async changePassword(payload) {
      await api.put('/profile/password', payload)
    },
    async uploadAvatar(file) {
      const fd = new FormData()
      fd.append('file', file)
      const { data } = await api.post('/profile/avatar', fd, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
      applyProfile(this, data)
      persistUser(this)
      return data
    },
    async removeAvatar() {
      const { data } = await api.delete('/profile/avatar')
      applyProfile(this, data)
      persistUser(this)
      return data
    },
    logout() {
      this.token = ''
      this.id = null
      this.username = ''
      this.email = ''
      this.mobile = ''
      this.avatarPath = ''
      this.permissions = []
      localStorage.removeItem('token')
      localStorage.removeItem('user')
    }
  }
})
