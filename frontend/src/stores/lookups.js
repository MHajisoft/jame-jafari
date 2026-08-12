import { defineStore } from 'pinia'
import api from '../api/client'

const ACCOUNTS_TTL_MS = 15 * 60 * 1000
const COST_TYPES_TTL_MS = 15 * 60 * 1000
const GENERAL_TYPES_TTL_MS = 30 * 60 * 1000

function isFresh(entry, ttl) {
  return !!entry?.data && Date.now() - entry.at < ttl
}

/**
 * In-memory lookup cache (mirrors backend FusionCache TTLs).
 * Use for dropdown/filter data shared across pages.
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
    accountsKey(activeOnly) {
      return String(!!activeOnly)
    },
    costTypesKey(isIngredient, activeOnly) {
      const ing = isIngredient === undefined || isIngredient === null ? 'all' : String(!!isIngredient)
      return `${ing}:${String(!!activeOnly)}`
    },
    generalTypesKey(category, includeInactive) {
      return `${category}:${String(!!includeInactive)}`
    },

    async getAccounts({ activeOnly = true, force = false } = {}) {
      const key = this.accountsKey(activeOnly)
      const cached = this.accounts[key]
      if (!force && isFresh(cached, ACCOUNTS_TTL_MS)) return cached.data

      const { data } = await api.get('/accounts', { params: { activeOnly } })
      this.accounts[key] = { data, at: Date.now() }
      return data
    },

    async getCostTypes({ isIngredient = null, activeOnly = true, force = false } = {}) {
      const key = this.costTypesKey(isIngredient, activeOnly)
      const cached = this.costTypes[key]
      if (!force && isFresh(cached, COST_TYPES_TTL_MS)) return cached.data

      const params = { activeOnly }
      if (isIngredient !== null && isIngredient !== undefined) params.isIngredient = isIngredient
      const { data } = await api.get('/cost-types', { params })
      this.costTypes[key] = { data, at: Date.now() }
      return data
    },

    async getGeneralTypes(category, { includeInactive = false, force = false } = {}) {
      const key = this.generalTypesKey(category, includeInactive)
      const cached = this.generalTypes[key]
      if (!force && isFresh(cached, GENERAL_TYPES_TTL_MS)) return cached.data

      const { data } = await api.get('/general-types', {
        params: { category, includeInactive }
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
