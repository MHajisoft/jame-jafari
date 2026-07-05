<script setup>
import { ref, onMounted } from 'vue'
import api from '../api/client'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const items = ref([])
const showModal = ref(false)
const editing = ref(null)
const form = ref({ name: '', description: '', isActive: true })

async function load() {
  const { data } = await api.get('/accounts', { params: { activeOnly: false } })
  items.value = data
}

async function submit() {
  if (editing.value) {
    await api.put(`/accounts/${editing.value}`, form.value)
  } else {
    await api.post('/accounts', form.value)
  }
  showModal.value = false
  await load()
}

function openCreate() {
  editing.value = null
  form.value = { name: '', description: '', isActive: true }
  showModal.value = true
}

function openEdit(item) {
  editing.value = item.id
  form.value = { name: item.name, description: item.description || '', isActive: item.isActive }
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
      <button v-if="auth.hasPermission('accounts.manage')" class="btn" @click="openCreate">+ حساب جدید</button>
    </div>

    <div class="card">
      <table>
        <thead>
          <tr><th>نام</th><th>توضیحات</th><th>وضعیت</th><th></th></tr>
        </thead>
        <tbody>
          <tr v-for="item in items" :key="item.id">
            <td><strong>{{ item.name }}</strong></td>
            <td>{{ item.description }}</td>
            <td>
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
        <div class="form-group">
          <label>نام</label>
          <input v-model="form.name" class="form-control" required />
        </div>
        <div class="form-group">
          <label>توضیحات</label>
          <textarea v-model="form.description" class="form-control" rows="2"></textarea>
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
