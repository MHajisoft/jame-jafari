<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { useToastStore } from '../stores/toast'
import { useDialogStore } from '../stores/dialog'
import { useFormValidation } from '../composables/useFormValidation'
import { useIsMobile } from '../composables/useMediaQuery'
import ClearableInput from '../components/ClearableInput.vue'

const router = useRouter()
const auth = useAuthStore()
const toast = useToastStore()
const dialog = useDialogStore()
const isMobile = useIsMobile()
const { error, errors, validate, trySubmit, clearErrors, clearFieldError } = useFormValidation()

const profileForm = reactive({ email: '', mobile: '' })
const passwordForm = reactive({ currentPassword: '', newPassword: '', confirmPassword: '' })
const avatarBusy = ref(false)
const sheetOpen = ref(false)
const fileInput = ref(null)
const cameraInput = ref(null)

const profileRules = {
  email: [{ type: 'email', msg: 'فرمت ایمیل نامعتبر است' }],
  mobile: [{ type: 'maxLength', param: 20, msg: 'موبایل حداکثر ۲۰ کاراکتر' }]
}

const passwordRules = {
  currentPassword: [{ type: 'required', msg: 'رمز عبور فعلی الزامی است' }],
  newPassword: [
    { type: 'required', msg: 'رمز عبور جدید الزامی است' },
    { type: 'minLength', param: 4, msg: 'رمز عبور حداقل ۴ کاراکتر' }
  ],
  confirmPassword: [
    (v, data) => {
      if (!v) return 'تکرار رمز عبور الزامی است'
      if (v !== data.newPassword) return 'تکرار رمز عبور مطابقت ندارد'
      return null
    }
  ]
}

const avatarSrc = computed(() => auth.avatarUrl)

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

function openAvatarPicker() {
  if (isMobile.value) sheetOpen.value = true
  else fileInput.value?.click()
}

function closeSheet() {
  sheetOpen.value = false
}

function openGallery() {
  fileInput.value?.click()
}

function openCamera() {
  cameraInput.value?.click()
}

async function onAvatarChange(e) {
  const file = e.target.files?.[0]
  e.target.value = ''
  if (!file) return
  sheetOpen.value = false
  avatarBusy.value = true
  clearErrors()
  try {
    await auth.uploadAvatar(file)
    toast.success('تصویر پروفایل به‌روز شد')
  } catch (err) {
    await trySubmit(async () => { throw err })
  } finally {
    avatarBusy.value = false
  }
}

async function removeAvatar() {
  if (!auth.avatarPath) return
  sheetOpen.value = false
  if (!(await dialog.confirmDelete('تصویر پروفایل'))) return
  avatarBusy.value = true
  clearErrors()
  try {
    await auth.removeAvatar()
    toast.success('تصویر پروفایل حذف شد')
  } catch (err) {
    await trySubmit(async () => { throw err })
  } finally {
    avatarBusy.value = false
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
        <button
          type="button"
          class="avatar-hit"
          :disabled="avatarBusy"
          :aria-label="auth.avatarPath ? 'تغییر تصویر پروفایل' : 'افزودن تصویر پروفایل'"
          @click="openAvatarPicker"
        >
          <span class="avatar-wrap">
            <img v-if="avatarSrc" :src="avatarSrc" alt="" class="avatar-img" />
            <span v-else class="avatar-fallback">{{ auth.initials }}</span>
          </span>
          <span class="avatar-badge" aria-hidden="true">
            <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M4 8h3l2-2h6l2 2h3v11H4z" />
              <circle cx="12" cy="13" r="3.2" />
            </svg>
          </span>
        </button>

        <div class="avatar-meta">
          <h2 class="avatar-name">{{ auth.username }}</h2>
          <p class="text-muted">حساب کاربری شما</p>
          <p class="avatar-hint text-muted">برای {{ auth.avatarPath ? 'تغییر' : 'افزودن' }} تصویر، روی عکس بزنید</p>
          <button
            v-if="auth.avatarPath"
            type="button"
            class="avatar-remove"
            :disabled="avatarBusy"
            @click="removeAvatar"
          >
            حذف تصویر
          </button>
        </div>

        <input ref="fileInput" type="file" accept="image/*" hidden @change="onAvatarChange" />
        <input ref="cameraInput" type="file" accept="image/*" capture="environment" hidden @change="onAvatarChange" />
      </section>

      <div class="profile-forms">
        <section class="card form-panel">
          <h3 class="section-title">اطلاعات حساب</h3>
          <div v-if="error" class="form-error">{{ error }}</div>
          <form @submit.prevent="saveProfile">
            <div class="form-group">
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
            <div class="form-group">
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

    <Teleport to="body">
      <div v-if="sheetOpen && isMobile" class="attach-overlay" @click.self="closeSheet">
        <div class="attach-sheet" role="dialog" aria-modal="true" aria-label="تصویر پروفایل">
          <div class="sheet-handle" />
          <p class="sheet-title">تصویر پروفایل</p>
          <button type="button" class="sheet-option" @click="openCamera">
            <span class="option-icon camera" aria-hidden="true">📷</span>
            <span class="option-text">
              <strong>دوربین</strong>
              <small>گرفتن عکس جدید</small>
            </span>
          </button>
          <button type="button" class="sheet-option" @click="openGallery">
            <span class="option-icon gallery" aria-hidden="true">🖼</span>
            <span class="option-text">
              <strong>گالری</strong>
              <small>انتخاب از تصاویر دستگاه</small>
            </span>
          </button>
          <button
            v-if="auth.avatarPath"
            type="button"
            class="sheet-option danger"
            @click="removeAvatar"
          >
            <span class="option-icon" aria-hidden="true">🗑</span>
            <span class="option-text">
              <strong>حذف تصویر</strong>
              <small>بازگشت به حروف اول نام</small>
            </span>
          </button>
          <button type="button" class="sheet-cancel" @click="closeSheet">انصراف</button>
        </div>
      </div>
    </Teleport>
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
.avatar-hit {
  position: relative;
  border: none;
  background: transparent;
  padding: 0;
  cursor: pointer;
  border-radius: 50%;
  -webkit-tap-highlight-color: transparent;
}
.avatar-hit:disabled { opacity: 0.65; cursor: not-allowed; }
.avatar-hit:focus-visible {
  outline: 2px solid var(--primary);
  outline-offset: 3px;
}
.avatar-wrap {
  display: block;
  width: 120px;
  height: 120px;
  border-radius: 50%;
  overflow: hidden;
  border: 3px solid color-mix(in srgb, var(--primary) 35%, var(--border));
  background: var(--bg);
}
.avatar-badge {
  position: absolute;
  inset-inline-end: 4px;
  bottom: 4px;
  width: 34px;
  height: 34px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: var(--primary);
  color: var(--on-primary);
  border-radius: 50%;
  border: 2px solid var(--surface);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.16);
}
.avatar-badge svg { display: block; }
.avatar-img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}
.avatar-fallback {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: var(--primary);
  color: white;
  font-size: 2.4rem;
  font-weight: 700;
}
.avatar-name {
  font-size: 1.15rem;
  font-weight: 700;
  margin: 0;
}
.avatar-hint {
  margin: 0.35rem 0 0;
  font-size: 0.82rem;
  line-height: 1.4;
}
.avatar-remove {
  border: none;
  background: transparent;
  color: var(--danger);
  font: inherit;
  font-size: 0.85rem;
  font-weight: 600;
  padding: 0.35rem 0;
  margin-top: 0.15rem;
  cursor: pointer;
  min-height: 44px;
}
.avatar-remove:disabled { opacity: 0.55; cursor: not-allowed; }
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

.attach-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.45);
  z-index: 1200;
  display: flex;
  align-items: flex-end;
  justify-content: center;
}
.attach-sheet {
  width: 100%;
  max-width: 420px;
  background: var(--surface);
  border-radius: 20px 20px 0 0;
  padding: 0.75rem 1rem calc(1rem + env(safe-area-inset-bottom, 0));
}
.sheet-handle {
  width: 36px;
  height: 4px;
  border-radius: 999px;
  background: var(--border);
  margin: 0 auto 0.75rem;
}
.sheet-title {
  text-align: center;
  font-weight: 700;
  margin: 0 0 0.75rem;
}
.sheet-option {
  display: flex;
  align-items: center;
  gap: 0.85rem;
  width: 100%;
  padding: 0.85rem 0.5rem;
  border: none;
  border-bottom: 1px solid var(--border);
  background: transparent;
  color: var(--text);
  text-align: right;
  cursor: pointer;
  min-height: 56px;
}
.sheet-option.danger strong { color: var(--danger); }
.option-icon {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.25rem;
  background: var(--bg);
  flex-shrink: 0;
}
.option-text {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}
.option-text small { color: var(--text-muted); }
.sheet-cancel {
  width: 100%;
  min-height: 44px;
  margin-top: 0.5rem;
  border: none;
  border-radius: 999px;
  background: var(--bg);
  color: var(--text);
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
    align-items: center;
  }
  .avatar-wrap {
    width: 88px;
    height: 88px;
  }
  .avatar-fallback { font-size: 1.8rem; }
  .avatar-badge {
    width: 30px;
    height: 30px;
    inset-inline-end: 2px;
    bottom: 2px;
  }
  .avatar-meta { flex: 1; min-width: 0; text-align: right; }
}
</style>
