<script setup>
import { ref, onMounted } from 'vue'
import api from '../api/client'
import { useAuthStore } from '../stores/auth'
import { useFormValidation } from '../composables/useFormValidation'

const auth = useAuthStore()
const { error, errors, validate, trySubmit, clearErrors, clearFieldError } = useFormValidation()
const items = ref([])
const showModal = ref(false)
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
  showModal.value = false
  await load()
}

function openCreate() {
  editing.value = null
  form.value = { name: '', description: '', isActive: true }
  clearErrors()
  showModal.value = true
}

function openEdit(item) {
  editing.value = item.id
  form.value = { name: item.name, description: item.description || '', isActive: item.isActive }
  clearErrors()
  showModal.value = true
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
    <div class="page-header">
      <h1 class="page-title">حساب‌های مالی</h1>
      <button v-if="auth.hasPermission('accounts.manage')" class="btn btn-fab-mobile" @click="openCreate">
        <span aria-hidden="true">+</span>
        <span class="btn-fab-label">حساب جدید</span>
      </button>
    </div>

    <div class="card">
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
              <button class="btn btn-sm btn-outline" @click="openEdit(item)">ویرایش</button>
              <button class="btn btn-sm btn-danger" @click="remove(item.id)">حذف</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
      <div class="modal">
        <h2 class="modal-title">{{ editing ? 'ویرایش حساب' : 'حساب جدید' }}</h2>
        <div v-if="error" class="form-error">{{ error }}</div>
        <form @submit.prevent="submit">
          <div class="form-group">
            <label>نام *</label>
            <input
              v-model="form.name"
              class="form-control"
              :class="{ 'field-invalid': errors.name }"
              @input="clearFieldError('name')"
            />
            <div v-if="errors.name" class="field-error">{{ errors.name }}</div>
          </div>
          <div class="form-group">
            <label>توضیحات</label>
            <textarea v-model="form.description" class="form-control" rows="2"></textarea>
          </div>
          <div class="form-group">
            <label><input v-model="form.isActive" type="checkbox" /> فعال</label>
          </div>
          <div class="modal-actions">
            <button type="button" class="btn btn-outline" @click="showModal = false">انصراف</button>
            <button type="submit" class="btn">ذخیره</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>
