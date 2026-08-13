<script setup>
import { onMounted, reactive } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { useToastStore } from '../stores/toast'
import { useDialogStore } from '../stores/dialog'
import { useFormValidation } from '../composables/useFormValidation'
import { useAvatarField } from '../composables/useAvatarField'
import { passwordFieldRules } from '../utils/passwordPolicy'
import AvatarPicker from '../components/AvatarPicker.vue'
import ClearableInput from '../components/ClearableInput.vue'

const router = useRouter()
const auth = useAuthStore()
const toast = useToastStore()
const dialog = useDialogStore()
const { error, errors, validate, trySubmit, clearErrors, clearFieldError } = useFormValidation()

const { avatarFile, busy: avatarBusy } = useAvatarField({
  immediate: true,
  onUpload: async (file) => {
    await auth.uploadAvatar(file)
    toast.success('تصویر به‌روز شد')
  },
  onError: (err) => trySubmit(async () => { throw err })
})

const profileForm = reactive({ email: '', mobile: '' })
const passwordForm = reactive({ currentPassword: '', newPassword: '', confirmPassword: '' })

const profileRules = {
  email: [{ type: 'email', msg: 'فرمت ایمیل نامعتبر است' }],
  mobile: [{ type: 'maxLength', param: 20, msg: 'موبایل حداکثر ۲۰ کاراکتر' }]
}

const passwordRules = {
  currentPassword: [{ type: 'required', msg: 'رمز عبور فعلی الزامی است' }],
  newPassword: passwordFieldRules({ required: true }),
  confirmPassword: [
    (v, data) => {
      if (!v) return 'تکرار رمز عبور الزامی است'
      if (v !== data.newPassword) return 'تکرار رمز عبور مطابقت ندارد'
      return null
    }
  ]
}

async function onAvatarPathUpdate(path) {
  if (path !== '' || !auth.avatarPath) return
  await removeAvatar()
}

function syncFormsFromAuth() {
  profileForm.email = auth.email || ''
  profileForm.mobile = auth.mobile || ''
}

async function load() {
  try {
    await auth.fetchProfile()
  } catch {
    /* keep cached */
  }
  syncFormsFromAuth()
}

async function saveProfile() {
  if (!validate(profileRules, profileForm)) return
  const ok = await trySubmit(async () => {
    await auth.updateProfile({
      email: profileForm.email?.trim() || null,
      mobile: profileForm.mobile?.trim() || null
    })
  }, { successMessage: 'اطلاعات پروفایل ذخیره شد' })
  if (!ok) return
  syncFormsFromAuth()
}

async function savePassword() {
  if (!validate(passwordRules, passwordForm)) return
  const ok = await trySubmit(async () => {
    await auth.changePassword({
      currentPassword: passwordForm.currentPassword,
      newPassword: passwordForm.newPassword
    })
  }, { successMessage: 'رمز عبور با موفقیت تغییر کرد' })
  if (!ok) return
  passwordForm.currentPassword = ''
  passwordForm.newPassword = ''
  passwordForm.confirmPassword = ''
}

async function removeAvatar() {
  if (!auth.avatarPath) return
  if (!(await dialog.confirmDelete('تصویر'))) return
  clearErrors()
  try {
    await auth.removeAvatar()
    toast.success('تصویر حذف شد')
  } catch (err) {
    await trySubmit(async () => { throw err })
  }
}

onMounted(load)

function logout() {
  auth.logout()
  router.push('/login')
}
</script>

<template>
  <div class="profile-page">
    <div class="page-header">
      <h1 class="page-title">پروفایل کاربری</h1>
    </div>

    <div v-if="error" class="form-error">{{ error }}</div>

    <div class="profile-layout">
      <section class="card profile-avatar-card">
        <AvatarPicker
          v-model="avatarFile"
          :path="auth.avatarPath || ''"
          :name="auth.username"
          :disabled="avatarBusy"
          label="تصویر"
          class="profile-avatar-picker"
          @update:path="onAvatarPathUpdate"
        />
        <div class="avatar-meta">
          <h2 class="avatar-name">{{ auth.username }}</h2>
          <p class="text-muted">حساب کاربری شما</p>
        </div>
      </section>

      <div class="profile-forms">
        <section class="card form-panel">
          <h3 class="section-title">اطلاعات حساب</h3>
          <div v-if="error" class="form-error">{{ error }}</div>
          <form @submit.prevent="saveProfile">
            <div class="form-group form-span-full">
              <label>نام کاربری</label>
              <input class="form-control" :value="auth.username" disabled />
            </div>
            <div class="grid-2">
              <div class="form-group">
                <label>ایمیل</label>
                <ClearableInput
                  v-model="profileForm.email"
                  inputmode="email"
                  :invalid="!!errors.email"
                  @input="clearFieldError('email')"
                />
                <div v-if="errors.email" class="field-error">{{ errors.email }}</div>
              </div>
              <div class="form-group">
                <label>موبایل</label>
                <ClearableInput
                  v-model="profileForm.mobile"
                  :invalid="!!errors.mobile"
                  @input="clearFieldError('mobile')"
                />
                <div v-if="errors.mobile" class="field-error">{{ errors.mobile }}</div>
              </div>
            </div>
            <div class="modal-actions">
              <button type="submit" class="btn">ذخیره اطلاعات</button>
            </div>
          </form>
        </section>

        <section class="card form-panel">
          <h3 class="section-title">تغییر رمز عبور</h3>
          <form @submit.prevent="savePassword">
            <div class="form-group form-span-full">
              <label>رمز عبور فعلی *</label>
              <ClearableInput
                v-model="passwordForm.currentPassword"
                type="password"
                :invalid="!!errors.currentPassword"
                autocomplete="current-password"
                @input="clearFieldError('currentPassword')"
              />
              <div v-if="errors.currentPassword" class="field-error">{{ errors.currentPassword }}</div>
            </div>
            <div class="grid-2">
              <div class="form-group">
                <label>رمز عبور جدید *</label>
                <ClearableInput
                  v-model="passwordForm.newPassword"
                  type="password"
                  :invalid="!!errors.newPassword"
                  autocomplete="new-password"
                  @input="clearFieldError('newPassword')"
                />
                <div v-if="errors.newPassword" class="field-error">{{ errors.newPassword }}</div>
              </div>
              <div class="form-group">
                <label>تکرار رمز عبور *</label>
                <ClearableInput
                  v-model="passwordForm.confirmPassword"
                  type="password"
                  :invalid="!!errors.confirmPassword"
                  autocomplete="new-password"
                  @input="clearFieldError('confirmPassword')"
                />
                <div v-if="errors.confirmPassword" class="field-error">{{ errors.confirmPassword }}</div>
              </div>
            </div>
            <div class="modal-actions">
              <button type="submit" class="btn">تغییر رمز</button>
            </div>
          </form>
        </section>
      </div>
    </div>

    <button type="button" class="logout-btn" @click="logout">خروج از حساب</button>
  </div>
</template>

<style scoped>
.profile-layout {
  display: grid;
  grid-template-columns: minmax(240px, 280px) minmax(0, 1fr);
  gap: 1rem;
  align-items: start;
}
.profile-forms {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  min-width: 0;
}
.profile-avatar-card {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  gap: 0.85rem;
  position: sticky;
  top: 1rem;
}
.profile-avatar-picker {
  width: 100%;
  margin-bottom: 0;
}
.profile-avatar-picker :deep(.avatar-stage) {
  align-items: center;
}
.profile-avatar-picker :deep(.avatar-actions) {
  justify-content: center;
}
.avatar-name {
  font-size: 1.15rem;
  font-weight: 700;
  margin: 0;
}
.section-title {
  font-size: 1rem;
  font-weight: 700;
  margin: 0 0 1rem;
}
.form-success {
  background: color-mix(in srgb, var(--success) 16%, transparent);
  color: var(--success);
  padding: 0.5rem 0.75rem;
  border-radius: 8px;
  font-size: 0.85rem;
  margin-bottom: 0.75rem;
}
.form-control:disabled {
  opacity: 0.75;
  cursor: not-allowed;
}
.logout-btn {
  width: 100%;
  margin-top: 1rem;
  padding: 0.9rem;
  border: 1px solid var(--border);
  border-radius: 12px;
  background: var(--surface);
  color: var(--danger);
  font-weight: 600;
  cursor: pointer;
}

@media (max-width: 768px) {
  .profile-layout {
    grid-template-columns: 1fr;
  }
  .profile-avatar-card {
    position: static;
    flex-direction: row;
    flex-wrap: wrap;
    text-align: right;
    align-items: flex-start;
  }
  .profile-avatar-picker :deep(.avatar-stage) {
    align-items: flex-start;
  }
  .profile-avatar-picker :deep(.avatar-actions) {
    justify-content: flex-start;
  }
  .avatar-meta { flex: 1; min-width: 0; text-align: right; }
}
</style>
