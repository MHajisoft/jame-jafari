import { defineStore } from 'pinia'
import api from '../api/client'
import { ApiPaths } from '../api/paths'

const ACCOUNTS_TTL_MS = 15 * 60 * 1000
const COST_TYPES_TTL_MS = 15 * 60 * 1000
const GENERAL_TYPES_TTL_MS = 30 * 60 * 1000

function isFresh(entry, ttl) {
  return !!entry?.data && Date.now() - entry.at < ttl
}

function accountsPath(admin) {
  return admin ? ApiPaths.accounts : ApiPaths.lookups.accounts
}

function costTypesPath(admin) {
  return admin ? ApiPaths.costTypes : ApiPaths.lookups.costTypes
}

function generalTypesPath(admin) {
  return admin ? ApiPaths.generalTypes : ApiPaths.lookups.generalTypes
}

/**
 * In-memory lookup cache (mirrors backend FusionCache TTLs).
 * Form/select pages use /lookups/* (transaction-scoped read).
 * Admin CRUD pages pass admin: true for full list APIs.
 */
export const useLookupsStore = defineStore('lookups', {
  state: () => ({
    /** @type {Record<string, { data: any[], at: number }>} */
    accounts: {},
    /** @type {Record<string, { data: any[], at: number }>} */
    costTypes: {},
    /** @type {Record<string, { data: any[], at: number }>} */
    generalTypes: {}
  }),
  actions: {
    accountsKey(activeOnly, admin) {
      return `${String(!!activeOnly)}:${String(!!admin)}`
    },
    costTypesKey(isIngredient, activeOnly, admin) {
      const ing = isIngredient === undefined || isIngredient === null ? 'all' : String(!!isIngredient)
      return `${ing}:${String(!!activeOnly)}:${String(!!admin)}`
    },
    generalTypesKey(category, includeInactive, admin) {
      return `${category}:${String(!!includeInactive)}:${String(!!admin)}`
    },

    async getAccounts({ activeOnly = true, force = false, admin = false } = {}) {
      const key = this.accountsKey(activeOnly, admin)
      const cached = this.accounts[key]
      if (!force && isFresh(cached, ACCOUNTS_TTL_MS)) return cached.data

      const { data } = await api.get(accountsPath(admin), {
        params: { activeOnly },
        skipErrorToast: !admin
      })
      this.accounts[key] = { data, at: Date.now() }
      return data
    },

    async getCostTypes({ isIngredient = null, activeOnly = true, force = false, admin = false } = {}) {
      const key = this.costTypesKey(isIngredient, activeOnly, admin)
      const cached = this.costTypes[key]
      if (!force && isFresh(cached, COST_TYPES_TTL_MS)) return cached.data

      const params = { activeOnly }
      if (isIngredient !== null && isIngredient !== undefined) params.isIngredient = isIngredient
      const { data } = await api.get(costTypesPath(admin), { params, skipErrorToast: !admin })
      this.costTypes[key] = { data, at: Date.now() }
      return data
    },

    async getGeneralTypes(category, { includeInactive = false, force = false, admin = false } = {}) {
      const useAdmin = admin || includeInactive
      const key = this.generalTypesKey(category, includeInactive, useAdmin)
      const cached = this.generalTypes[key]
      if (!force && isFresh(cached, GENERAL_TYPES_TTL_MS)) return cached.data

      const params = useAdmin ? { category, includeInactive } : { category }
      const { data } = await api.get(generalTypesPath(useAdmin), {
        params,
        skipErrorToast: !useAdmin
      })
      this.generalTypes[key] = { data, at: Date.now() }
      return data
    },

    invalidateAccounts() {
      this.accounts = {}
    },
    invalidateCostTypes() {
      this.costTypes = {}
    },
    invalidateGeneralTypes() {
      this.generalTypes = {}
    },
    invalidateAll() {
      this.invalidateAccounts()
      this.invalidateCostTypes()
      this.invalidateGeneralTypes()
    }
  }
})
