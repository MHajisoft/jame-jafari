<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import api from '../api/client'
import { genders, genderLabel, enumValue, toInputDate } from '../utils/format'
import { useAuthStore } from '../stores/auth'
import { useToastStore } from '../stores/toast'
import { useDialogStore } from '../stores/dialog'
import { useLookupsStore } from '../stores/lookups'
import { useFormValidation } from '../composables/useFormValidation'
import { useAvatarField } from '../composables/useAvatarField'
import { useIsMobile } from '../composables/useMediaQuery'
import AppSelect from '../components/AppSelect.vue'
import PersonCell from '../components/PersonCell.vue'
import PersonSelect from '../components/PersonSelect.vue'
import ClearableInput from '../components/ClearableInput.vue'
import FormHost from '../components/FormHost.vue'
import AppCheckbox from '../components/AppCheckbox.vue'
import RowActions from '../components/RowActions.vue'
import AvatarPicker from '../components/AvatarPicker.vue'
import { usePagedList } from '../composables/usePagedList'
import PagedListPanel from '../components/PagedListPanel.vue'
import PersianDatePicker from '../components/PersianDatePicker.vue'

const auth = useAuthStore()
const toast = useToastStore()
const dialog = useDialogStore()
const lookups = useLookupsStore()
const router = useRouter()
const isMobile = useIsMobile()
const { error, errors, validate, trySubmit, clearErrors, clearFieldError } = useFormValidation()
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
  load: loadPersons,
  goPrev,
  goNext,
  reload: reloadPersons
} = usePagedList(async ({ page, pageSize }) => {
  const { data } = await api.get('/persons', { params: { page, pageSize } })
  return data
})
const namePrefixes = ref([])
const showForm = ref(false)
const editing = ref(null)
const {
  avatarFile,
  avatarPath,
  initialAvatarPath,
  resetAvatarState,
  syncAvatar: syncPersonPicture
} = useAvatarField({
  uploadUrl: (id) => `/persons/${id}/picture`,
  deleteUrl: (id) => `/persons/${id}/picture`
})
const form = ref({
  firstName: '', lastName: '', nickName: '', gender: 1,
  fatherId: '', motherId: '', mobile: '', address: '', namePrefixId: '',
  isDead: false, deathDate: ''
})

const avatarName = computed(() =>
  [form.value.firstName, form.value.lastName].filter(Boolean).join(' ') || 'شخص'
)

const canManagePrefixes = computed(() =>
  auth.hasAnyPermission('generaltypes.create', 'generaltypes.update', 'generaltypes.delete', 'generaltypes.view')
)

const rules = {
  firstName: [{ type: 'required', msg: 'نام الزامی است' }],
  mobile: [{ type: 'maxLength', param: 20, msg: 'موبایل حداکثر ۲۰ کاراکتر' }],
  deathDate: [
    (value, data) => {
      if (!data.isDead) return null
      if (!value?.trim()) return 'تاریخ وفات الزامی است'
      return null
    }
  ]
}

watch(() => form.value.isDead, (dead) => {
  if (!dead) {
    form.value.deathDate = ''
    clearFieldError('deathDate')
  }
})

async function load() {
  try {
    namePrefixes.value = await lookups.getGeneralTypes('NamePrefix')
  } catch {
    namePrefixes.value = []
  }
  await loadPersons()
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
    isDead: form.value.isDead,
    deathDate: form.value.isDead ? form.value.deathDate || null : null
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
  await reloadPersons()
}

function openCreate() {
  editing.value = null
  form.value = { firstName: '', lastName: '', nickName: '', gender: 1, fatherId: '', motherId: '', mobile: '', address: '', namePrefixId: '', isDead: false, deathDate: '' }
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
    isDead: item.isDead,
    deathDate: item.deathDate ? toInputDate(item.deathDate) : ''
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
  if (!(await dialog.confirmDelete('این شخص'))) return
  await api.delete(`/persons/${id}`)
  toast.success('شخص حذف شد')
  await reloadPersons()
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
      <form class="form-layout-adaptive" @submit.prevent="submit">
        <AvatarPicker
          v-model="avatarFile"
          v-model:path="avatarPath"
          :name="avatarName"
          label="تصویر"
          class="form-span-full"
        />
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
        <div class="form-group">
          <label>لقب</label>
          <ClearableInput v-model="form.nickName" />
        </div>
        <div class="form-group">
          <label>جنسیت</label>
          <AppSelect
            v-model="form.gender"
            :options="genders"
            placeholder="جنسیت"
            :allow-empty="false"
          />
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
          <label>وضعیت حیات</label>
          <AppCheckbox v-model="form.isDead" label="درگذشته است" />
        </div>
        <div v-if="form.isDead" class="form-group">
          <label>تاریخ وفات *</label>
          <PersianDatePicker
            v-model="form.deathDate"
            @change="clearFieldError('deathDate')"
          />
          <div v-if="errors.deathDate" class="field-error">{{ errors.deathDate }}</div>
        </div>
        <div class="form-group form-span-full">
          <label>آدرس</label>
          <ClearableInput v-model="form.address" type="textarea" :rows="2" />
        </div>
        <div class="modal-actions">
          <button type="button" class="btn btn-outline" @click="closeForm">انصراف</button>
          <button type="submit" class="btn">ذخیره</button>
        </div>
      </form>
    </FormHost>

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
            <tr>
              <th>نام</th><th>جنسیت</th><th>موبایل</th><th>پدر</th><th>مادر</th>
              <th v-if="auth.hasAnyPermission('persons.update', 'persons.delete', 'audit.view')"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td data-label="نام">
                <PersonCell :person="item" />
              </td>
              <td data-label="جنسیت">{{ genderLabel(item.gender) }}</td>
              <td data-label="موبایل">{{ item.mobile || '—' }}</td>
              <td data-label="پدر">
                <PersonCell
                  :person="item.fatherSummary"
                  :display-name="item.fatherSummary ? '' : (item.fatherName || '')"
                  :previewable="!!item.fatherSummary"
                />
              </td>
              <td data-label="مادر">
                <PersonCell
                  :person="item.motherSummary"
                  :display-name="item.motherSummary ? '' : (item.motherName || '')"
                  :previewable="!!item.motherSummary"
                />
              </td>
              <td v-if="auth.hasAnyPermission('persons.update', 'persons.delete', 'audit.view')">
                <RowActions
                  :show-edit="auth.hasPermission('persons.update')"
                  :show-delete="auth.hasPermission('persons.delete')"
                  :show-audit="auth.hasPermission('audit.view')"
                  :audit="item.audit"
                  @edit="openEdit(item)"
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
.link-btn:hover { text-decoration: underline; }
</style>
