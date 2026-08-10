<script setup>
import { ref, onMounted } from 'vue'
import api from '../api/client'
import { useAuthStore } from '../stores/auth'
import { useFormValidation } from '../composables/useFormValidation'
import { useIsMobile } from '../composables/useMediaQuery'
import ClearableInput from '../components/ClearableInput.vue'
import FormHost from '../components/FormHost.vue'
import AppCheckbox from '../components/AppCheckbox.vue'

const auth = useAuthStore()
const isMobile = useIsMobile()
const { error, errors, validate, trySubmit, clearErrors, clearFieldError } = useFormValidation()
const items = ref([])
const showForm = ref(false)
const editing = ref(null)
const form = ref({ name: '', description: '', isActive: true })

const rules = {
  name: [{ type: 'required', msg: 'نام حساب الزامی است' }]
}

async function load() {
  const { data } = await api.get('/accounts', { params: { activeOnly: false } })
  items.value = data
}

async function submit() {
  if (!validate(rules, form.value)) return
  const ok = await trySubmit(async () => {
    if (editing.value) {
      await api.put(`/accounts/${editing.value}`, form.value)
    } else {
      await api.post('/accounts', form.value)
    }
  })
  if (!ok) return
  closeForm()
  await load()
}

function openCreate() {
  editing.value = null
  form.value = { name: '', description: '', isActive: true }
  clearErrors()
  showForm.value = true
}

function openEdit(item) {
  editing.value = item.id
  form.value = { name: item.name, description: item.description || '', isActive: item.isActive }
  clearErrors()
  showForm.value = true
}

function closeForm() {
  showForm.value = false
}

async function remove(id) {
  if (!confirm('حذف این حساب؟')) return
  await api.delete(`/accounts/${id}`)
  await load()
}

onMounted(load)
</script>

<template>
  <div>
    <div class="page-header" :class="{ 'form-mode': showForm && !isMobile }">
      <h1 class="page-title">{{ showForm && !isMobile ? (editing ? 'ویرایش حساب' : 'حساب جدید') : 'حساب‌های مالی' }}</h1>
      <button
        v-if="auth.hasPermission('accounts.create') && (!showForm || isMobile)"
        class="btn btn-fab-mobile"
        @click="openCreate"
      >
        <span aria-hidden="true">+</span>
        <span class="btn-fab-label">حساب جدید</span>
      </button>
    </div>

    <FormHost :show="showForm" :title="isMobile ? (editing ? 'ویرایش حساب' : 'حساب جدید') : ''" @close="closeForm">
      <div v-if="error" class="form-error">{{ error }}</div>
      <form @submit.prevent="submit">
        <div class="form-group">
          <label>نام *</label>
          <ClearableInput
            v-model="form.name"
            :invalid="!!errors.name"
            @input="clearFieldError('name')"
          />
          <div v-if="errors.name" class="field-error">{{ errors.name }}</div>
        </div>
        <div class="form-group">
          <label>توضیحات</label>
          <ClearableInput v-model="form.description" type="textarea" :rows="2" />
        </div>
        <div class="form-group">
          <AppCheckbox v-model="form.isActive" label="فعال" />
        </div>
        <div class="modal-actions">
          <button type="button" class="btn btn-outline" @click="closeForm">انصراف</button>
          <button type="submit" class="btn">ذخیره</button>
        </div>
      </form>
    </FormHost>

    <div v-show="!showForm || isMobile" class="card list-panel">
      <table class="mobile-table">
        <thead><tr><th>نام</th><th>توضیحات</th><th>وضعیت</th><th></th></tr></thead>
        <tbody>
          <tr v-for="item in items" :key="item.id">
            <td data-label="نام"><strong>{{ item.name }}</strong></td>
            <td data-label="توضیحات">{{ item.description }}</td>
            <td data-label="وضعیت">
              <span :class="item.isActive ? 'badge badge-success' : 'badge badge-danger'">
                {{ item.isActive ? 'فعال' : 'غیرفعال' }}
              </span>
            </td>
            <td>
              <div class="table-actions">
                <button
                  v-if="auth.hasPermission('accounts.update')"
                  class="btn btn-sm btn-outline"
                  @click="openEdit(item)"
                >ویرایش</button>
                <button
                  v-if="auth.hasPermission('accounts.delete')"
                  class="btn btn-sm btn-danger"
                  @click="remove(item.id)"
                >حذف</button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
