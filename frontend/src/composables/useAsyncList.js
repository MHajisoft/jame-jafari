import { ref } from 'vue'

/**
 * Async list loader with loading + last-error state.
 * API error toasts remain handled by the axios interceptor.
 */
export function useAsyncList(loader) {
  const items = ref([])
  const loading = ref(false)
  const loadError = ref('')

  async function load(...args) {
    loading.value = true
    loadError.value = ''
    try {
      const result = await loader(...args)
      items.value = Array.isArray(result) ? result : (result?.items ?? result ?? [])
      return result
    } catch (e) {
      loadError.value = e?.response?.data?.message || e?.message || 'بارگذاری ناموفق بود'
      throw e
    } finally {
      loading.value = false
    }
  }

  return { items, loading, loadError, load }
}
