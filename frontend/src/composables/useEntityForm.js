import { ref } from 'vue'

/**
 * Shared create/edit form page state used by CRUD list views.
 * @param {() => object} createInitial - factory for a blank form object
 * @param {{ onReset?: () => void }} [options]
 */
export function useEntityForm(createInitial, options = {}) {
  const showForm = ref(false)
  const editing = ref(null)
  const form = ref(createInitial())

  function resetForm(next = null) {
    form.value = next ?? createInitial()
    options.onReset?.()
  }

  function openCreate() {
    editing.value = null
    resetForm()
    showForm.value = true
  }

  function openEdit(id, values) {
    editing.value = id
    resetForm(values)
    showForm.value = true
  }

  function closeForm() {
    showForm.value = false
  }

  return {
    showForm,
    editing,
    form,
    openCreate,
    openEdit,
    closeForm,
    resetForm
  }
}
