<script setup>
import { ref, onMounted } from 'vue'
import api from '../api/client'
import { useAuthStore } from '../stores/auth'
import { useToastStore } from '../stores/toast'
import { useDialogStore } from '../stores/dialog'
import { useFormValidation } from '../composables/useFormValidation'
import { useAvatarField } from '../composables/useAvatarField'
import { passwordFieldRules } from '../utils/passwordPolicy'
import { useIsMobile } from '../composables/useMediaQuery'
import ClearableInput from '../components/ClearableInput.vue'
import FormHost from '../components/FormHost.vue'
import AppCheckbox from '../components/AppCheckbox.vue'
import PermissionMatrix from '../components/PermissionMatrix.vue'
import { permissionTitle } from '../composables/usePermissionMatrix'
import RowActions from '../components/RowActions.vue'
import EntityAvatar from '../components/EntityAvatar.vue'
import AvatarPicker from '../components/AvatarPicker.vue'
import { usePagedList } from '../composables/usePagedList'
import PagedListPanel from '../components/PagedListPanel.vue'

const auth = useAuthStore()
const toast = useToastStore()
const dialog = useDialogStore()
const isMobile = useIsMobile()
const { error, errors, validate, trySubmit, clearErrors, clearFieldError } = useFormValidation()
const {
  errors: passwordErrors,
  validate: validatePasswordForm,
  trySubmit: tryPasswordSubmit,
  clearErrors: clearPasswordErrors,
  clearFieldError: clearPasswordFieldError
} = useFormValidation()
const permissions = ref([])
const {
  items,
  loading,
  page,
  totalPages,
  totalCount,
  hasPrev,
  hasNext,
  showPagination,
  rangeStart,
  rangeEnd,
  load: loadUsers,
  goPrev,
  goNext,
  reload: reloadUsers
} = usePagedList(async ({ page, pageSize }) => {
  const { data } = await api.get('/users', { params: { page, pageSize } })
  return data
})
const showForm = ref(false)
const showPasswordForm = ref(false)
const passwordTarget = ref(null)
const passwordForm = ref({ newPassword: '', confirmPassword: '' })
const editing = ref(null)
const {
  avatarFile,
  avatarPath,
  initialAvatarPath,
  resetAvatarState,
  syncAvatar: syncUserAvatar
} = useAvatarField({
  uploadUrl: (id) => `/users/${id}/avatar`,
  deleteUrl: (id) => `/users/${id}/avatar`
})
const form = ref({ username: '', password: '', email: '', mobile: '', isActive: true, permissionIds: [] })
const permMatrixRef = ref(null)

function getRules() {
  const r = {
    username: [
      { type: 'required', msg: 'نام کاربری الزامی است' },
      { type: 'minLength', param: 3, msg: 'نام کاربری حداقل ۳ کاراکتر' }
    ]
  }
  if (!editing.value) {
    r.password = passwordFieldRules({ required: true })
  }
  if (form.value.email) {
    r.email = [{ type: 'email' }]
  }
  return r
}

const passwordRules = {
  newPassword: passwordFieldRules({ required: true }),
  confirmPassword: [
    (v, data) => {
      if (!v) return 'تکرار رمز عبور الزامی است'
      if (v !== data.newPassword) return 'تکرار رمز عبور مطابقت ندارد'
      return null
    }
  ]
}

async function load() {
  try {
    const { data } = await api.get('/permissions')
    permissions.value = data
  } catch {
    permissions.value = []
  }
  await loadUsers()
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

  const payload = editing.value
    ? {
        email: normalized.email,
        mobile: normalized.mobile,
        isActive: normalized.isActive,
        permissionIds: normalized.permissionIds
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
    let id = editing.value
    if (editing.value) {
      await api.put(`/users/${editing.value}`, payload)
    } else {
      const { data } = await api.post('/users', payload)
      id = data.id
    }
    await syncUserAvatar(id)
  }, { successMessage: editing.value ? 'کاربر با موفقیت ویرایش شد' : 'کاربر با موفقیت ایجاد شد' })
  if (!ok) return
  permMatrixRef.value?.markSaved?.()
  closeForm()
  await reloadUsers()
}

function openCreate() {
  editing.value = null
  form.value = { username: '', password: '', email: '', mobile: '', isActive: true, permissionIds: [] }
  resetAvatarState()
  clearErrors()
  showForm.value = true
}

function openEdit(item) {
  if (item.isSystemAdmin) return
  editing.value = item.id
  form.value = {
    username: item.username, password: '', email: item.email || '',
    mobile: item.mobile || '', isActive: item.isActive,
    permissionIds: permissions.value.filter(p => item.permissions.includes(p.code)).map(p => p.id)
  }
  resetAvatarState(item.avatarPath || '')
  clearErrors()
  showForm.value = true
}

function closeForm() {
  showForm.value = false
  resetAvatarState()
}

function openChangePassword(item) {
  passwordTarget.value = item
  passwordForm.value = { newPassword: '', confirmPassword: '' }
  clearPasswordErrors()
  showPasswordForm.value = true
}

function closePasswordForm() {
  showPasswordForm.value = false
  passwordTarget.value = null
}

async function submitPasswordChange() {
  if (!validatePasswordForm(passwordRules, passwordForm.value)) return
  const ok = await tryPasswordSubmit(async () => {
    await api.put(`/users/${passwordTarget.value.id}/password`, {
      newPassword: passwordForm.value.newPassword
    })
  }, { successMessage: 'رمز عبور با موفقیت تغییر کرد' })
  if (!ok) return
  closePasswordForm()
}

async function remove(id) {
  if (!(await dialog.confirmDelete('این کاربر'))) return
  await api.delete(`/users/${id}`)
  toast.success('کاربر حذف شد')
  await reloadUsers()
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
      <form class="form-layout-adaptive user-form" @submit.prevent="submit">
        <AvatarPicker
          v-model="avatarFile"
          v-model:path="avatarPath"
          :name="form.username || 'کاربر'"
          label="تصویر"
          class="form-span-full"
        />
        <div class="form-group user-active-row form-span-full">
          <AppCheckbox v-model="form.isActive" label="فعال" />
        </div>
        <div v-if="!editing" class="form-group">
          <label>نام کاربری *</label>
          <ClearableInput
            v-model="form.username"
            :invalid="!!errors.username"
            @input="clearFieldError('username')"
          />
          <div v-if="errors.username" class="field-error">{{ errors.username }}</div>
        </div>
        <div v-if="!editing" class="form-group">
          <label>رمز عبور *</label>
          <ClearableInput
            v-model="form.password"
            type="password"
            :invalid="!!errors.password"
            autocomplete="new-password"
            @input="clearFieldError('password')"
          />
          <div v-if="errors.password" class="field-error">{{ errors.password }}</div>
        </div>
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
        <PermissionMatrix
          ref="permMatrixRef"
          v-model="form.permissionIds"
          :permissions="permissions"
          class="form-span-full"
        />
        <div class="modal-actions" :class="{ 'user-form-actions': isMobile }">
          <button type="button" class="btn btn-outline" @click="closeForm">انصراف</button>
          <button type="submit" class="btn">ذخیره</button>
        </div>
      </form>
    </FormHost>

    <Teleport to="body">
      <div
        v-if="showPasswordForm"
        class="password-modal-overlay"
        @click.self="closePasswordForm"
      >
        <div class="password-modal card" role="dialog" aria-modal="true" aria-labelledby="change-password-title">
          <h2 id="change-password-title" class="password-modal-title">
            تغییر رمز — {{ passwordTarget?.username }}
          </h2>
          <form @submit.prevent="submitPasswordChange">
            <div class="form-group">
              <label>رمز عبور جدید *</label>
              <ClearableInput
                v-model="passwordForm.newPassword"
                type="password"
                :invalid="!!passwordErrors.newPassword"
                autocomplete="new-password"
                @input="clearPasswordFieldError('newPassword')"
              />
              <div v-if="passwordErrors.newPassword" class="field-error">{{ passwordErrors.newPassword }}</div>
            </div>
            <div class="form-group">
              <label>تکرار رمز عبور *</label>
              <ClearableInput
                v-model="passwordForm.confirmPassword"
                type="password"
                :invalid="!!passwordErrors.confirmPassword"
                autocomplete="new-password"
                @input="clearPasswordFieldError('confirmPassword')"
              />
              <div v-if="passwordErrors.confirmPassword" class="field-error">{{ passwordErrors.confirmPassword }}</div>
            </div>
            <div class="modal-actions">
              <button type="button" class="btn btn-outline" @click="closePasswordForm">انصراف</button>
              <button type="submit" class="btn">ذخیره</button>
            </div>
          </form>
        </div>
      </div>
    </Teleport>

    <div v-show="!showForm || isMobile">
      <PagedListPanel
        :loading="loading"
        :skeleton-columns="6"
        :show-pagination="showPagination"
        :page="page"
        :total-pages="totalPages"
        :total-count="totalCount"
        :range-start="rangeStart"
        :range-end="rangeEnd"
        :has-prev="hasPrev"
        :has-next="hasNext"
        @prev="goPrev"
        @next="goNext"
      >
        <table class="mobile-table">
        <thead>
          <tr><th>نام کاربری</th><th>ایمیل</th><th>موبایل</th><th>دسترسی‌ها</th><th>وضعیت</th><th v-if="auth.hasAnyPermission('users.update', 'users.delete', 'users.changepassword', 'audit.view')"></th></tr>
        </thead>
        <tbody>
          <tr v-for="item in items" :key="item.id">
            <td data-label="نام کاربری">
              <div class="entity-cell">
                <EntityAvatar
                  :src="item.avatarPath"
                  :name="item.username"
                  previewable
                  :preview-title="item.username"
                />
                <div class="entity-names">
                  <strong>{{ item.username }}</strong>
                  <span v-if="item.isSystemAdmin" class="badge badge-system">مدیر اصلی</span>
                </div>
              </div>
            </td>
            <td data-label="ایمیل">{{ item.email || '—' }}</td>
            <td data-label="موبایل">{{ item.mobile || '—' }}</td>
            <td data-label="دسترسی‌ها">
              <div class="perm-badges">
                <span v-for="p in item.permissions" :key="p" class="badge badge-perm">{{ permissionTitle(p) }}</span>
                <span v-if="!item.permissions.length" class="text-muted">—</span>
              </div>
            </td>
            <td data-label="وضعیت">
              <span :class="item.isActive ? 'badge badge-on' : 'badge badge-off'">
                {{ item.isActive ? 'فعال' : 'غیرفعال' }}
              </span>
            </td>
            <td v-if="auth.hasAnyPermission('users.update', 'users.delete', 'users.changepassword', 'audit.view')">
              <RowActions
                :show-edit="!item.isSystemAdmin && auth.hasPermission('users.update')"
                :show-change-password="!item.isSystemAdmin && auth.hasPermission('users.changepassword')"
                :show-delete="!item.isSystemAdmin && auth.hasPermission('users.delete')"
                :show-audit="auth.hasPermission('audit.view')"
                :audit="item.audit"
                @edit="openEdit(item)"
                @change-password="openChangePassword(item)"
                @delete="remove(item.id)"
              />
            </td>
          </tr>
        </tbody>
      </table>
      </PagedListPanel>
    </div>
  </div>
</template>

<style scoped>
.entity-cell {
  display: flex;
  align-items: center;
  gap: 0.7rem;
  min-width: 0;
}
.entity-cell strong {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
}
.entity-names {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.25rem;
  min-width: 0;
}
.badge-system {
  font-size: 0.72rem;
  font-weight: 600;
  padding: 0.15rem 0.45rem;
  border-radius: 999px;
  background: color-mix(in srgb, var(--primary) 14%, var(--surface));
  color: var(--primary);
}
.password-modal-overlay {
  position: fixed;
  inset: 0;
  z-index: 220;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
  background: rgba(12, 20, 16, 0.45);
}
.password-modal {
  width: 100%;
  max-width: 420px;
  text-align: right;
}
.password-modal-title {
  margin: 0 0 1rem;
  font-size: 1.05rem;
  color: var(--primary);
}

.user-active-row {
  margin-bottom: 0.5rem;
}

.user-form-actions {
  position: sticky;
  bottom: 0;
  z-index: 5;
  margin-top: 1rem;
  padding: 0.85rem 0 calc(0.15rem + env(safe-area-inset-bottom, 0));
  background: linear-gradient(to top, var(--bg) 72%, color-mix(in srgb, var(--bg) 55%, transparent));
  border-top: 1px solid var(--border);
}
</style>
