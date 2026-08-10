<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import api from '../api/client'
import { genders, genderLabel, enumValue } from '../utils/format'
import { useAuthStore } from '../stores/auth'
import { useToastStore } from '../stores/toast'
import { useFormValidation } from '../composables/useFormValidation'
import { useIsMobile } from '../composables/useMediaQuery'
import AppSelect from '../components/AppSelect.vue'
import PersonSelect from '../components/PersonSelect.vue'
import ClearableInput from '../components/ClearableInput.vue'
import FormHost from '../components/FormHost.vue'
import AppCheckbox from '../components/AppCheckbox.vue'
import RowActions from '../components/RowActions.vue'
import EntityAvatar from '../components/EntityAvatar.vue'
import AvatarPicker from '../components/AvatarPicker.vue'

const auth = useAuthStore()
const toast = useToastStore()
const router = useRouter()
const isMobile = useIsMobile()
const { error, errors, validate, trySubmit, clearErrors, clearFieldError } = useFormValidation()
const items = ref([])
const namePrefixes = ref([])
const showForm = ref(false)
const editing = ref(null)
const avatarFile = ref(null)
const avatarPath = ref('')
const initialAvatarPath = ref('')
const form = ref({
  firstName: '', lastName: '', nickName: '', gender: 1,
  fatherId: '', motherId: '', mobile: '', address: '', namePrefixId: '', isDead: false
})

const avatarName = computed(() =>
  [form.value.firstName, form.value.lastName].filter(Boolean).join(' ') || 'شخص'
)

const canManagePrefixes = computed(() =>
  auth.hasAnyPermission('generaltypes.create', 'generaltypes.update', 'generaltypes.delete', 'generaltypes.view')
)

const rules = {
  firstName: [{ type: 'required', msg: 'نام الزامی است' }],
  mobile: [{ type: 'maxLength', param: 20, msg: 'موبایل حداکثر ۲۰ کاراکتر' }]
}

async function load() {
  const [p, t] = await Promise.all([
    api.get('/persons', { params: { page: 1, pageSize: 20 } }),
    api.get('/general-types', { params: { category: 'NamePrefix' } })
  ])
  items.value = p.data.items
  namePrefixes.value = t.data
}

async function syncPersonPicture(id) {
  if (avatarFile.value) {
    const fd = new FormData()
    fd.append('file', avatarFile.value)
    await api.post(`/persons/${id}/picture`, fd, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })
    return
  }
  if (editing.value && initialAvatarPath.value && !avatarPath.value) {
    await api.delete(`/persons/${id}/picture`)
  }
}

async function submit() {
  if (!validate(rules, form.value)) return
  const payload = {
    firstName: form.value.firstName.trim(),
    lastName: form.value.lastName?.trim() || null,
    nickName: form.value.nickName?.trim() || null,
    gender: +form.value.gender,
    fatherId: form.value.fatherId ? +form.value.fatherId : null,
    motherId: form.value.motherId ? +form.value.motherId : null,
    mobile: form.value.mobile?.trim() || null,
    address: form.value.address?.trim() || null,
    namePrefixId: form.value.namePrefixId ? +form.value.namePrefixId : null,
    isDead: form.value.isDead
  }
  const ok = await trySubmit(async () => {
    let id = editing.value
    if (editing.value) {
      await api.put(`/persons/${editing.value}`, payload)
    } else {
      const { data } = await api.post('/persons', payload)
      id = data.id
    }
    await syncPersonPicture(id)
  }, { successMessage: editing.value ? 'شخص با موفقیت ویرایش شد' : 'شخص با موفقیت ایجاد شد' })
  if (!ok) return
  closeForm()
  editing.value = null
  await load()
}

function resetAvatarState(path = '') {
  avatarFile.value = null
  avatarPath.value = path || ''
  initialAvatarPath.value = path || ''
}

function openCreate() {
  editing.value = null
  form.value = { firstName: '', lastName: '', nickName: '', gender: 1, fatherId: '', motherId: '', mobile: '', address: '', namePrefixId: '', isDead: false }
  resetAvatarState()
  clearErrors()
  showForm.value = true
}

function openEdit(item) {
  editing.value = item.id
  form.value = {
    firstName: item.firstName, lastName: item.lastName || '', nickName: item.nickName || '',
    gender: enumValue(genders, item.gender, 1), fatherId: item.fatherId || '', motherId: item.motherId || '',
    mobile: item.mobile || '', address: item.address || '', namePrefixId: item.namePrefixId || '',
    isDead: item.isDead
  }
  resetAvatarState(item.picturePath || '')
  clearErrors()
  showForm.value = true
}

function closeForm() {
  showForm.value = false
  resetAvatarState()
}

async function remove(id) {
  if (!confirm('حذف این شخص؟')) return
  await api.delete(`/persons/${id}`)
  toast.success('شخص حذف شد')
  await load()
}

onMounted(load)
</script>

<template>
  <div>
    <div class="page-header" :class="{ 'form-mode': showForm && !isMobile }">
      <h1 class="page-title">{{ showForm && !isMobile ? (editing ? 'ویرایش شخص' : 'شخص جدید') : 'اشخاص' }}</h1>
      <button
        v-if="auth.hasPermission('persons.create') && (!showForm || isMobile)"
        class="btn btn-fab-mobile"
        @click="openCreate"
      >
        <span aria-hidden="true">+</span>
        <span class="btn-fab-label">شخص جدید</span>
      </button>
    </div>

    <FormHost :show="showForm" :title="isMobile ? (editing ? 'ویرایش شخص' : 'شخص جدید') : ''" @close="closeForm">
      <div v-if="error" class="form-error">{{ error }}</div>
      <form @submit.prevent="submit">
        <AvatarPicker
          v-model="avatarFile"
          v-model:path="avatarPath"
          :name="avatarName"
          label="تصویر شخص"
        />
        <div class="grid-2">
          <div class="form-group">
            <label>نام *</label>
            <ClearableInput
              v-model="form.firstName"
              :invalid="!!errors.firstName"
              @input="clearFieldError('firstName')"
            />
            <div v-if="errors.firstName" class="field-error">{{ errors.firstName }}</div>
          </div>
          <div class="form-group">
            <label>نام خانوادگی</label>
            <ClearableInput v-model="form.lastName" />
          </div>
        </div>
        <div class="grid-2">
          <div class="form-group">
            <label>نام مستعار</label>
            <ClearableInput v-model="form.nickName" />
          </div>
          <div class="form-group">
            <label>جنسیت</label>
            <AppSelect
              v-model="form.gender"
              :options="genders"
              placeholder="جنسیت"
              :allow-empty="false"
              :searchable="false"
            />
          </div>
        </div>
        <div class="form-group">
          <label>پیشوند نام</label>
          <AppSelect
            v-model="form.namePrefixId"
            :options="namePrefixes"
            option-value="id"
            option-label="name"
            placeholder="بدون پیشوند"
          />
          <button
            v-if="canManagePrefixes"
            type="button"
            class="link-btn"
            @click="router.push({ path: '/general-types', query: { category: 'NamePrefix' } })"
          >
            مدیریت پیشوندها
          </button>
        </div>
          <div class="grid-2">
            <div class="form-group">
              <label>پدر</label>
              <PersonSelect
                v-model="form.fatherId"
                placeholder="انتخاب پدر"
                :gender="1"
                :exclude-id="editing"
              />
            </div>
            <div class="form-group">
              <label>مادر</label>
              <PersonSelect
                v-model="form.motherId"
                placeholder="انتخاب مادر"
                :gender="2"
                :exclude-id="editing"
              />
            </div>
          </div>
        <div class="form-group">
          <label>موبایل</label>
          <ClearableInput
            v-model="form.mobile"
            :invalid="!!errors.mobile"
            @input="clearFieldError('mobile')"
          />
          <div v-if="errors.mobile" class="field-error">{{ errors.mobile }}</div>
        </div>
        <div class="form-group">
          <label>آدرس</label>
          <ClearableInput v-model="form.address" type="textarea" :rows="2" />
        </div>
        <div class="form-group">
          <AppCheckbox v-model="form.isDead" label="فوت شده" />
        </div>
        <div class="modal-actions">
          <button type="button" class="btn btn-outline" @click="closeForm">انصراف</button>
          <button type="submit" class="btn">ذخیره</button>
        </div>
      </form>
    </FormHost>

    <div v-show="!showForm || isMobile">
      <div class="card list-panel">
        <table class="mobile-table">
          <thead>
            <tr>
              <th>نام</th><th>جنسیت</th><th>موبایل</th><th>پدر</th><th>مادر</th><th>وضعیت</th>
              <th v-if="auth.hasAnyPermission('persons.update', 'persons.delete')"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td data-label="نام">
                <div class="entity-cell">
                  <EntityAvatar :src="item.picturePath" :name="item.displayName" />
                  <strong>{{ item.displayName }}</strong>
                </div>
              </td>
              <td data-label="جنسیت">{{ genderLabel(item.gender) }}</td>
              <td data-label="موبایل">{{ item.mobile }}</td>
              <td data-label="پدر">{{ item.fatherName }}</td>
              <td data-label="مادر">{{ item.motherName }}</td>
              <td data-label="وضعیت">
                <span v-if="item.isDead" class="badge badge-danger">فوت شده</span>
                <span v-else class="badge badge-success">فعال</span>
              </td>
              <td v-if="auth.hasAnyPermission('persons.update', 'persons.delete')">
                <RowActions
                  :show-edit="auth.hasPermission('persons.update')"
                  :show-delete="auth.hasPermission('persons.delete')"
                  @edit="openEdit(item)"
                  @delete="remove(item.id)"
                />
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<style scoped>
.link-btn {
  margin-top: 0.45rem;
  border: none;
  background: none;
  color: var(--primary);
  font: inherit;
  font-size: 0.82rem;
  font-weight: 600;
  cursor: pointer;
  padding: 0;
}
<link-btn:hover { text-decoration: underline; }

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
</style>
