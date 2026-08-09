<script setup>
import { ref, onMounted } from 'vue'
import api from '../api/client'
import { useAuthStore } from '../stores/auth'
import { useFormValidation } from '../composables/useFormValidation'

const auth = useAuthStore()
const { error, errors, validate, trySubmit, clearErrors, clearFieldError } = useFormValidation()
const items = ref([])
const units = ref([])
const showModal = ref(false)
const editing = ref(null)
const form = ref({ name: '', description: '', isIngredient: false, unitId: '', isActive: true })

function rules() {
  const r = {
    name: [{ type: 'required', msg: 'نام الزامی است' }]
  }
  if (form.value.isIngredient) {
    r.unitId = [{ type: 'required', msg: 'انتخاب واحد الزامی است' }]
  }
  return r
}

async function load() {
  const [c, u] = await Promise.all([
    api.get('/cost-types', { params: { isIngredient: null } }),
    api.get('/general-types', { params: { category: 'Unit' } })
  ])
  items.value = c.data
  units.value = u.data
}

async function submit() {
  if (!validate(rules(), form.value)) return
  const payload = {
    ...form.value,
    unitId: form.value.unitId ? +form.value.unitId : null
  }
  const ok = await trySubmit(async () => {
    if (editing.value) {
      await api.put(`/cost-types/${editing.value}`, payload)
    } else {
      await api.post('/cost-types', payload)
    }
  })
  if (!ok) return
  showModal.value = false
  await load()
}

function openCreate() {
  editing.value = null
  form.value = { name: '', description: '', isIngredient: false, unitId: '', isActive: true }
  clearErrors()
  showModal.value = true
}

function openEdit(item) {
  editing.value = item.id
  form.value = {
    name: item.name, description: item.description || '',
    isIngredient: item.isIngredient, unitId: item.unitId || '', isActive: item.isActive
  }
  clearErrors()
  showModal.value = true
}

async function remove(id) {
  if (!confirm('حذف این نوع هزینه؟')) return
  await api.delete(`/cost-types/${id}`)
  await load()
}

onMounted(load)
</script>

<template>
  <div>
    <div class="page-header">
      <h1 class="page-title">انواع هزینه</h1>
      <button v-if="auth.hasPermission('costtypes.manage')" class="btn btn-fab-mobile" @click="openCreate">
        <span aria-hidden="true">+</span>
        <span class="btn-fab-label">نوع جدید</span>
      </button>
    </div>

    <div class="card">
      <table class="mobile-table">
        <thead><tr><th>نام</th><th>مواد اولیه</th><th>واحد</th><th>وضعیت</th><th v-if="auth.hasPermission('costtypes.manage')"></th></tr></thead>
        <tbody>
          <tr v-for="item in items" :key="item.id">
            <td data-label="نام"><strong>{{ item.name }}</strong></td>
            <td data-label="مواد اولیه">{{ item.isIngredient ? '✓' : '—' }}</td>
            <td data-label="واحد">{{ item.unitName || '—' }}</td>
            <td data-label="وضعیت">
              <span :class="item.isActive ? 'badge badge-success' : 'badge badge-danger'">
                {{ item.isActive ? 'فعال' : 'غیرفعال' }}
              </span>
            </td>
            <td v-if="auth.hasPermission('costtypes.manage')">
              <button class="btn btn-sm btn-outline" @click="openEdit(item)">ویرایش</button>
              <button class="btn btn-sm btn-danger" @click="remove(item.id)">حذف</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
      <div class="modal">
        <h2 class="modal-title">{{ editing ? 'ویرایش' : 'نوع هزینه جدید' }}</h2>
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
            <label><input v-model="form.isIngredient" type="checkbox" /> مواد اولیه (برای تهیه غذا)</label>
          </div>
          <div v-if="form.isIngredient" class="form-group">
            <label>واحد *</label>
            <select
              v-model="form.unitId"
              class="form-control"
              :class="{ 'field-invalid': errors.unitId }"
              @change="clearFieldError('unitId')"
            >
              <option value="">انتخاب کنید</option>
              <option v-for="u in units" :key="u.id" :value="u.id">{{ u.name }}</option>
            </select>
            <div v-if="errors.unitId" class="field-error">{{ errors.unitId }}</div>
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
