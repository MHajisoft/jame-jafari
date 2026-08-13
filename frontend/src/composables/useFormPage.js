import { computed, ref } from 'vue'

/** @type {import('vue').Ref<null | { title: string, close: () => void }>} */
export const activeFormPage = ref(null)

export function useActiveFormPage() {
  return {
    activeFormPage: computed(() => activeFormPage.value),
    isFormPageOpen: computed(() => !!activeFormPage.value)
  }
}

export function registerFormPage({ title, close }) {
  activeFormPage.value = { title: title || '', close }
}

export function updateFormPageTitle(title) {
  if (activeFormPage.value) {
    activeFormPage.value = { ...activeFormPage.value, title: title || '' }
  }
}

export function unregisterFormPage(closeFn) {
  if (!activeFormPage.value) return
  if (!closeFn || activeFormPage.value.close === closeFn) {
    activeFormPage.value = null
  }
}
