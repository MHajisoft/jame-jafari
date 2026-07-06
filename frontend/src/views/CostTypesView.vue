<script setup>
import { ref, onMounted } from 'vue'
import api from '../api/client'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const items = ref([])
const units = ref([])
const showModal = ref(false)
const editing = ref(null)
const form = ref({ name: '', description: '', isIngredient: false, unitId: '', isActive: true })

async function load() {
  const [c, u] = await Promise.all([
    api.get('/cost-types', { params: { isIngredient: null } }),
    api.get('/general-types', { params: { category: 'Unit' } })
  ])
  items.value = c.data
  units.value = u.data
}

async function submit() {
  const payload = {
    ...form.value,
    unitId: form.value.unitId ? +form.value.unitId : null
  }
  if (editing.value) {
    await api.put(`/cost-types/${editing.value}`, payload)
  } else {
    await api.post('/cost-types', payload)
  }
  showModal.value = false
  await load()
}

function openCreate() {
  editing.value = null
  form.value = { name: '', description: '', isIngredient: false, unitId: '', isActive: true }
  showModal.value = true
}

function openEdit(item) {
  editing.value = item.id
  form.value = {
    name: item.name, description: item.description || '',
    isIngredient: item.isIngredient, unitId: item.unitId || '', isActive: item.isActive
  }
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
        <thead>
          <tr><th>نام</th><th>مواد اولیه</th><th>واحد</th><th>وضعیت</th><th v-if="auth.hasPermission('costtypes.manage')"></th></tr>
        </thead>
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
        <div class="form-group">
          <label>نام</label>
          <input v-model="form.name" class="form-control" required />
        </div>
        <div class="form-group">
          <label>توضیحات</label>
          <textarea v-model="form.description" class="form-control" rows="2"></textarea>
        </div>
        <div class="form-group">
          <label><input v-model="form.isIngredient" type="checkbox" /> مواد اولیه (برای تهیه غذا)</label>
        </div>
        <div v-if="form.isIngredient" class="form-group">
          <label>واحد</label>
          <select v-model="form.unitId" class="form-control">
            <option value="">انتخاب کنید</option>
            <option v-for="u in units" :key="u.id" :value="u.id">{{ u.name }}</option>
          </select>
        </div>
        <div class="form-group">
          <label><input v-model="form.isActive" type="checkbox" /> فعال</label>
        </div>
        <div class="modal-actions">
          <button class="btn btn-outline" @click="showModal = false">انصراف</button>
          <button class="btn" @click="submit">ذخیره</button>
        </div>
      </div>
    </div>
  </div>
</template>
