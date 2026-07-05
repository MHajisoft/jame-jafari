<script setup>
import { ref, onMounted } from 'vue'
import api from '../api/client'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const items = ref([])
const roles = ref([])
const showModal = ref(false)
const editing = ref(null)
const form = ref({ username: '', password: '', email: '', mobile: '', isActive: true, roleIds: [] })

async function load() {
  const [u, r] = await Promise.all([
    api.get('/users'),
    api.get('/roles')
  ])
  items.value = u.data.items
  roles.value = r.data
}

async function submit() {
  if (editing.value) {
    await api.put(`/users/${editing.value}`, {
      email: form.value.email,
      mobile: form.value.mobile,
      isActive: form.value.isActive,
      roleIds: form.value.roleIds,
      newPassword: form.value.password || null
    })
  } else {
    await api.post('/users', form.value)
  }
  showModal.value = false
  await load()
}

function openCreate() {
  editing.value = null
  form.value = { username: '', password: '', email: '', mobile: '', isActive: true, roleIds: [] }
  showModal.value = true
}

function openEdit(item) {
  editing.value = item.id
  form.value = {
    username: item.username, password: '', email: item.email || '',
    mobile: item.mobile || '', isActive: item.isActive,
    roleIds: roles.value.filter(r => item.roles.includes(r.name)).map(r => r.id)
  }
  showModal.value = true
}

async function remove(id) {
  if (!confirm('حذف این کاربر؟')) return
  await api.delete(`/users/${id}`)
  await load()
}

onMounted(load)
</script>

<template>
  <div>
    <div class="page-header">
      <h1 class="page-title">مدیریت کاربران</h1>
      <button v-if="auth.hasPermission('users.manage')" class="btn" @click="openCreate">+ کاربر جدید</button>
    </div>

    <div class="card">
      <table>
        <thead>
          <tr><th>نام کاربری</th><th>ایمیل</th><th>موبایل</th><th>نقش</th><th>وضعیت</th><th v-if="auth.hasPermission('users.manage')"></th></tr>
        </thead>
        <tbody>
          <tr v-for="item in items" :key="item.id">
            <td><strong>{{ item.username }}</strong></td>
            <td>{{ item.email }}</td>
            <td>{{ item.mobile }}</td>
            <td>{{ item.roles.join('، ') }}</td>
            <td>
              <span :class="item.isActive ? 'badge badge-success' : 'badge badge-danger'">
                {{ item.isActive ? 'فعال' : 'غیرفعال' }}
              </span>
            </td>
            <td v-if="auth.hasPermission('users.manage')">
              <button class="btn btn-sm btn-outline" @click="openEdit(item)">ویرایش</button>
              <button class="btn btn-sm btn-danger" @click="remove(item.id)">حذف</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
      <div class="modal">
        <h2 class="modal-title">{{ editing ? 'ویرایش کاربر' : 'کاربر جدید' }}</h2>
        <div v-if="!editing" class="form-group">
          <label>نام کاربری *</label>
          <input v-model="form.username" class="form-control" required />
        </div>
        <div class="form-group">
          <label>{{ editing ? 'رمز عبور جدید (اختیاری)' : 'رمز عبور *' }}</label>
          <input v-model="form.password" type="password" class="form-control" :required="!editing" />
        </div>
        <div class="grid-2">
          <div class="form-group">
            <label>ایمیل</label>
            <input v-model="form.email" type="email" class="form-control" />
          </div>
          <div class="form-group">
            <label>موبایل</label>
            <input v-model="form.mobile" class="form-control" />
          </div>
        </div>
        <div class="form-group">
          <label>نقش‌ها</label>
          <div v-for="role in roles" :key="role.id">
            <label>
              <input v-model="form.roleIds" type="checkbox" :value="role.id" />
              {{ role.name }} - {{ role.description }}
            </label>
          </div>
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
