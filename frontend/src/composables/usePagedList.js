import { computed, ref } from 'vue'

function scrollListToTop() {
  if (typeof window === 'undefined') return
  const main = document.querySelector('.main')
  if (main && main.scrollHeight > main.clientHeight) {
    main.scrollTo({ top: 0, behavior: 'smooth' })
    return
  }
  window.scrollTo({ top: 0, behavior: 'smooth' })
}

/**
 * Server-paged list state. `fetchPage` receives { page, pageSize, ...extra } and returns
 * { items, totalCount, page, pageSize } (ASP.NET PagedResult camelCase).
 */
export function usePagedList(fetchPage, { pageSize: defaultPageSize = 20 } = {}) {
  const items = ref([])
  const loading = ref(false)
  const page = ref(1)
  const totalCount = ref(0)
  const pageSize = defaultPageSize
  const extraParams = ref({})

  const totalPages = computed(() =>
    Math.max(1, Math.ceil(totalCount.value / pageSize))
  )
  const hasPrev = computed(() => page.value > 1)
  const hasNext = computed(() => page.value < totalPages.value)
  const showPagination = computed(() => totalCount.value > 0)
  const rangeStart = computed(() =>
    totalCount.value === 0 ? 0 : (page.value - 1) * pageSize + 1
  )
  const rangeEnd = computed(() =>
    Math.min(page.value * pageSize, totalCount.value)
  )

  async function load(params) {
    if (params !== undefined) {
      extraParams.value = { ...extraParams.value, ...params }
    }
    loading.value = true
    try {
      const data = await fetchPage({
        page: page.value,
        pageSize,
        ...extraParams.value
      })
      items.value = data?.items ?? []
      totalCount.value = data?.totalCount ?? 0
      if (data?.page) page.value = data.page

      if (!items.value.length && totalCount.value > 0 && page.value > 1) {
        page.value -= 1
        return load()
      }
    } finally {
      loading.value = false
    }
  }

  async function goPrev() {
    if (!hasPrev.value || loading.value) return
    page.value -= 1
    await load()
    scrollListToTop()
  }

  async function goNext() {
    if (!hasNext.value || loading.value) return
    page.value += 1
    await load()
    scrollListToTop()
  }

  async function reload() {
    await load()
  }

  async function resetPage() {
    page.value = 1
    await load()
  }

  return {
    items,
    loading,
    page,
    pageSize,
    totalCount,
    totalPages,
    hasPrev,
    hasNext,
    showPagination,
    rangeStart,
    rangeEnd,
    load,
    goPrev,
    goNext,
    reload,
    resetPage
  }
}
