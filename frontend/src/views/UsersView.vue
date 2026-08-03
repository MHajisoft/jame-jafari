<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '../api/client'
import { useAuthStore } from '../stores/auth'
import { useFormValidation } from '../composables/useFormValidation'

const auth = useAuthStore()
const { error, errors, validate, trySubmit, clearErrors } = useFormValidation()
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

function getRules() {
  const r = {
    username: [
      { type: 'required', msg: 'نام کاربری الزامی است' },
      { type: 'minLength', param: 3, msg: 'نام کاربری حداقل ۳ کاراکتر' }
    ]
  }
  if (!editing.value) {
    r.password = [
      { type: 'required', msg: 'رمز عبور الزامی است' },
      { type: 'minLength', param: 4, msg: 'رمز عبور حداقل ۴ کاراکتر' }
    ]
  } else if (form.value.password) {
    r.password = [
      { type: 'minLength', param: 4, msg: 'رمز عبور حداقل ۴ کاراکتر' }
    ]
  }
  if (form.value.email) {
    r.email = [{ type: 'email' }]
  }
  return r
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
  if (!validate(getRules(), form.value)) return
  const normalized = {
    username: form.value.username,
    email: form.value.email?.trim() || null,
    mobile: form.value.mobile?.trim() || null,
    isActive: form.value.isActive,
    permissionIds: form.value.permissionIds
  }
  if (form.value.password) normalized.password = form.value.password
  if (form.value.password) normalized.newPassword = form.value.password

  const payload = editing.value
    ? {
        email: normalized.email,
        mobile: normalized.mobile,
        isActive: normalized.isActive,
        permissionIds: normalized.permissionIds,
        newPassword: normalized.password || null
      }
    : {
        username: normalized.username,
        password: normalized.password,
        email: normalized.email,
        mobile: normalized.mobile,
        isActive: normalized.isActive,
        permissionIds: normalized.permissionIds
      }

  const ok = await trySubmit(async () => {
    if (editing.value) {
      await api.put(`/users/${editing.value}`, payload)
    } else {
      await api.post('/users', payload)
    }
  })
  if (!ok) return
  showModal.value = false
  await load()
}

function openCreate() {
  editing.value = null
  form.value = { username: '', password: '', email: '', mobile: '', isActive: true, permissionIds: [] }
  clearErrors()
  showModal.value = true
}

function openEdit(item) {
  editing.value = item.id
  form.value = {
    username: item.username, password: '', email: item.email || '',
    mobile: item.mobile || '', isActive: item.isActive,
    permissionIds: permissions.value.filter(p => item.permissions.includes(p.code)).map(p => p.id)
  }
  clearErrors()
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
      <div class="modal" style="max-width:560px">
        <h2 class="modal-title">{{ editing ? 'ویرایش کاربر' : 'کاربر جدید' }}</h2>
        <div v-if="error" class="form-error">{{ error }}</div>
        <form @submit.prevent="submit">
          <div v-if="!editing" class="form-group">
            <label>نام کاربری *</label>
            <input v-model="form.username" class="form-control" :class="{ 'field-invalid': errors.username }" required />
            <div v-if="errors.username" class="field-error">{{ errors.username }}</div>
          </div>
          <div class="form-group">
            <label>{{ editing ? 'رمز عبور جدید (اختیاری)' : 'رمز عبور *' }}</label>
            <input v-model="form.password" type="password" class="form-control" :class="{ 'field-invalid': errors.password }" :required="!editing" />
            <div v-if="errors.password" class="field-error">{{ errors.password }}</div>
          </div>
          <div class="grid-2">
            <div class="form-group">
              <label>ایمیل</label>
              <input v-model="form.email" type="email" class="form-control" :class="{ 'field-invalid': errors.email }" />
              <div v-if="errors.email" class="field-error">{{ errors.email }}</div>
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
            <button type="button" class="btn btn-outline" @click="showModal = false">انصراف</button>
            <button type="submit" class="btn">ذخیره</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<style scoped>
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
