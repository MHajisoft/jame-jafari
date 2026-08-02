<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '../api/client'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const items = ref([])
const permissions = ref([])
const showModal = ref(false)
const editing = ref(null)
const form = ref({ username: '', password: '', email: '', mobile: '', isActive: true, permissionIds: [] })

const moduleLabels = {
  accounts: 'حساب‌ها',
  income: 'درآمد',
  cost: 'هزینه',
  users: 'کاربران',
  persons: 'اشخاص',
  costtypes: 'انواع هزینه',
  food: 'تهیه غذا',
  reports: 'گزارشات',
  generaltypes: 'انواع عمومی'
}

const groupedPermissions = computed(() => {
  const groups = {}
  for (const p of permissions.value) {
    (groups[p.module] ??= []).push(p)
  }
  return Object.entries(groups).map(([module, perms]) => ({
    module,
    label: moduleLabels[module] || module,
    perms
  }))
})

async function load() {
  const [u, p] = await Promise.all([
    api.get('/users'),
    api.get('/permissions')
  ])
  items.value = u.data.items
  permissions.value = p.data
}

async function submit() {
  if (editing.value) {
    await api.put(`/users/${editing.value}`, {
      email: form.value.email,
      mobile: form.value.mobile,
      isActive: form.value.isActive,
      permissionIds: form.value.permissionIds,
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
  form.value = { username: '', password: '', email: '', mobile: '', isActive: true, permissionIds: [] }
  showModal.value = true
}

function openEdit(item) {
  editing.value = item.id
  form.value = {
    username: item.username, password: '', email: item.email || '',
    mobile: item.mobile || '', isActive: item.isActive,
    permissionIds: permissions.value.filter(p => item.permissions.includes(p.code)).map(p => p.id)
  }
  showModal.value = true
}

function toggleGroup(group, on) {
  const ids = group.perms.map(p => p.id)
  if (on) {
    form.value.permissionIds = [...new Set([...form.value.permissionIds, ...ids])]
  } else {
    form.value.permissionIds = form.value.permissionIds.filter(id => !ids.includes(id))
  }
}

function groupState(group) {
  const ids = group.perms.map(p => p.id)
  const selected = form.value.permissionIds.filter(id => ids.includes(id)).length
  if (selected === 0) return 'none'
  if (selected === ids.length) return 'all'
  return 'some'
}

function permLabel(code) {
  const map = { view: 'مشاهده', manage: 'مدیریت', create: 'ایجاد', delete: 'حذف' }
  const action = code.split('.')[1]
  return map[action] || action
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
      <button v-if="auth.hasPermission('users.manage')" class="btn btn-fab-mobile" @click="openCreate">
        <span aria-hidden="true">+</span>
        <span class="btn-fab-label">کاربر جدید</span>
      </button>
    </div>

    <div class="card">
      <table class="mobile-table">
        <thead>
          <tr><th>نام کاربری</th><th>ایمیل</th><th>موبایل</th><th>دسترسی‌ها</th><th>وضعیت</th><th v-if="auth.hasPermission('users.manage')"></th></tr>
        </thead>
        <tbody>
          <tr v-for="item in items" :key="item.id">
            <td data-label="نام کاربری"><strong>{{ item.username }}</strong></td>
            <td data-label="ایمیل">{{ item.email }}</td>
            <td data-label="موبایل">{{ item.mobile }}</td>
            <td data-label="دسترسی‌ها">
              <div class="perm-badges">
                <span v-for="p in item.permissions" :key="p" class="badge badge-perm">{{ p }}</span>
                <span v-if="!item.permissions.length" class="text-muted">—</span>
              </div>
            </td>
            <td data-label="وضعیت">
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
          <label>دسترسی‌ها (هر مورد به‌صورت جداگانه)</label>
          <div v-for="group in groupedPermissions" :key="group.module" class="perm-group">
            <div class="perm-group-head">
              <label>
                <input
                  type="checkbox"
                  :checked="groupState(group) === 'all'"
                  :indeterminate.prop="groupState(group) === 'some'"
                  @change="toggleGroup(group, $event.target.checked)"
                />
                <strong>{{ group.label }}</strong>
              </label>
            </div>
            <div class="perm-group-items">
              <label v-for="p in group.perms" :key="p.id">
                <input v-model="form.permissionIds" type="checkbox" :value="p.id" />
                {{ permLabel(p.code) }}
              </label>
            </div>
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

<style scoped>
.perm-badges {
  display: flex;
  flex-wrap: wrap;
  gap: 0.25rem;
}
.badge-perm {
  font-size: 0.65rem;
  padding: 0.15rem 0.4rem;
  border-radius: 6px;
  background: var(--surface);
  border: 1px solid var(--border);
  color: var(--text-muted);
  white-space: nowrap;
}
.perm-group {
  border: 1px solid var(--border);
  border-radius: 8px;
  padding: 0.5rem 0.75rem;
  margin-bottom: 0.5rem;
}
.perm-group-head {
  padding-bottom: 0.35rem;
  margin-bottom: 0.35rem;
  border-bottom: 1px solid var(--border);
}
.perm-group-items {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem 1.25rem;
  padding-right: 1.4rem;
}
.perm-group-items label {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  font-size: 0.85rem;
}
</style>
