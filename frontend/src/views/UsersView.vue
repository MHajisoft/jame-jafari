<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '../api/client'
import { useAuthStore } from '../stores/auth'
import { useToastStore } from '../stores/toast'
import { useFormValidation } from '../composables/useFormValidation'
import { useIsMobile } from '../composables/useMediaQuery'
import ClearableInput from '../components/ClearableInput.vue'
import FormHost from '../components/FormHost.vue'
import AppCheckbox from '../components/AppCheckbox.vue'
import RowActions from '../components/RowActions.vue'

const auth = useAuthStore()
const toast = useToastStore()
const isMobile = useIsMobile()
const { error, errors, validate, trySubmit, clearErrors, clearFieldError } = useFormValidation()
const items = ref([])
const permissions = ref([])
const showForm = ref(false)
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
  const order = { view: 1, create: 2, update: 3, delete: 4 }
  const groups = {}
  for (const p of permissions.value) {
    (groups[p.module] ??= []).push(p)
  }
  return Object.entries(groups).map(([module, perms]) => ({
    module,
    label: moduleLabels[module] || module,
    perms: [...perms].sort((a, b) => {
      const aa = order[a.code.split('.')[1]] || 99
      const bb = order[b.code.split('.')[1]] || 99
      return aa - bb
    })
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
  }, { successMessage: editing.value ? 'کاربر با موفقیت ویرایش شد' : 'کاربر با موفقیت ایجاد شد' })
  if (!ok) return
  closeForm()
  await load()
}

function openCreate() {
  editing.value = null
  form.value = { username: '', password: '', email: '', mobile: '', isActive: true, permissionIds: [] }
  clearErrors()
  showForm.value = true
}

function openEdit(item) {
  editing.value = item.id
  form.value = {
    username: item.username, password: '', email: item.email || '',
    mobile: item.mobile || '', isActive: item.isActive,
    permissionIds: permissions.value.filter(p => item.permissions.includes(p.code)).map(p => p.id)
  }
  clearErrors()
  showForm.value = true
}

function closeForm() {
  showForm.value = false
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
  const map = { view: 'مشاهده', create: 'ایجاد', update: 'ویرایش', delete: 'حذف' }
  const action = code.split('.')[1]
  return map[action] || action
}

function permissionTitle(code) {
  const [mod] = String(code).split('.')
  const moduleName = moduleLabels[mod] || mod
  return `${moduleName} · ${permLabel(code)}`
}

async function remove(id) {
  if (!confirm('حذف این کاربر؟')) return
  await api.delete(`/users/${id}`)
  toast.success('کاربر حذف شد')
  await load()
}

onMounted(load)
</script>

<template>
  <div>
    <div class="page-header" :class="{ 'form-mode': showForm && !isMobile }">
      <h1 class="page-title">{{ showForm && !isMobile ? (editing ? 'ویرایش کاربر' : 'کاربر جدید') : 'مدیریت کاربران' }}</h1>
      <button
        v-if="auth.hasPermission('users.create') && (!showForm || isMobile)"
        class="btn btn-fab-mobile"
        @click="openCreate"
      >
        <span aria-hidden="true">+</span>
        <span class="btn-fab-label">کاربر جدید</span>
      </button>
    </div>

    <FormHost :show="showForm" :title="isMobile ? (editing ? 'ویرایش کاربر' : 'کاربر جدید') : ''" @close="closeForm">
      <div v-if="error" class="form-error">{{ error }}</div>
      <form @submit.prevent="submit">
        <div v-if="!editing" class="form-group">
          <label>نام کاربری *</label>
          <ClearableInput
            v-model="form.username"
            :invalid="!!errors.username"
            @input="clearFieldError('username')"
          />
          <div v-if="errors.username" class="field-error">{{ errors.username }}</div>
        </div>
        <div class="form-group">
          <label>{{ editing ? 'رمز عبور جدید (اختیاری)' : 'رمز عبور *' }}</label>
          <ClearableInput
            v-model="form.password"
            type="password"
            :invalid="!!errors.password"
            @input="clearFieldError('password')"
          />
          <div v-if="errors.password" class="field-error">{{ errors.password }}</div>
        </div>
        <div class="grid-2">
          <div class="form-group">
            <label>ایمیل</label>
            <ClearableInput
              v-model="form.email"
              inputmode="email"
              :invalid="!!errors.email"
              @input="clearFieldError('email')"
            />
            <div v-if="errors.email" class="field-error">{{ errors.email }}</div>
          </div>
          <div class="form-group">
            <label>موبایل</label>
            <ClearableInput v-model="form.mobile" />
          </div>
        </div>
        <div class="form-group">
          <label>دسترسی‌ها (هر مورد به‌صورت جداگانه)</label>
          <div class="perm-groups">
            <div v-for="group in groupedPermissions" :key="group.module" class="perm-group">
              <div class="perm-group-head">
                <AppCheckbox
                  :model-value="groupState(group) === 'all'"
                  :indeterminate="groupState(group) === 'some'"
                  @change="toggleGroup(group, $event.target.checked)"
                >
                  <strong>{{ group.label }}</strong>
                </AppCheckbox>
              </div>
              <div class="perm-group-items">
                <AppCheckbox
                  v-for="p in group.perms"
                  :key="p.id"
                  v-model="form.permissionIds"
                  :value="p.id"
                >
                  {{ permLabel(p.code) }}
                </AppCheckbox>
              </div>
            </div>
          </div>
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
        <thead>
          <tr><th>نام کاربری</th><th>ایمیل</th><th>موبایل</th><th>دسترسی‌ها</th><th>وضعیت</th><th v-if="auth.hasAnyPermission('users.update', 'users.delete')"></th></tr>
        </thead>
        <tbody>
          <tr v-for="item in items" :key="item.id">
            <td data-label="نام کاربری"><strong>{{ item.username }}</strong></td>
            <td data-label="ایمیل">{{ item.email }}</td>
            <td data-label="موبایل">{{ item.mobile }}</td>
            <td data-label="دسترسی‌ها">
              <div class="perm-badges">
                <span v-for="p in item.permissions" :key="p" class="badge badge-perm">{{ permissionTitle(p) }}</span>
                <span v-if="!item.permissions.length" class="text-muted">—</span>
              </div>
            </td>
            <td data-label="وضعیت">
              <span :class="item.isActive ? 'badge badge-success' : 'badge badge-danger'">
                {{ item.isActive ? 'فعال' : 'غیرفعال' }}
              </span>
            </td>
            <td v-if="auth.hasAnyPermission('users.update', 'users.delete')">
              <RowActions
                :show-edit="auth.hasPermission('users.update')"
                :show-delete="auth.hasPermission('users.delete')"
                @edit="openEdit(item)"
                @delete="remove(item.id)"
              />
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.perm-groups {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.65rem;
}
.perm-group {
  border: 1px solid var(--border);
  border-radius: 8px;
  padding: 0.45rem 0.65rem;
  margin: 0;
  background: var(--bg);
}
.perm-group-head {
  padding-bottom: 0.3rem;
  margin-bottom: 0.3rem;
  border-bottom: 1px solid var(--border);
}
.perm-group-items {
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem 1rem;
  padding-right: 0.15rem;
}
.perm-group-items :deep(.app-checkbox) {
  font-size: 0.85rem;
  margin-bottom: 0;
}
.perm-group-head :deep(.app-checkbox) {
  font-weight: 600;
}
@media (max-width: 900px) {
  .perm-groups { grid-template-columns: 1fr; }
}
</style>
