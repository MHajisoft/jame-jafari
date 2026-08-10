<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import api from '../api/client'
import { genders } from '../utils/format'
import { useAuthStore } from '../stores/auth'
import { useFormValidation } from '../composables/useFormValidation'
import { useIsMobile } from '../composables/useMediaQuery'
import AppSelect from '../components/AppSelect.vue'
import PersonSelect from '../components/PersonSelect.vue'
import ClearableInput from '../components/ClearableInput.vue'
import FormHost from '../components/FormHost.vue'
import AppCheckbox from '../components/AppCheckbox.vue'

const auth = useAuthStore()
const router = useRouter()
const isMobile = useIsMobile()
const { error, errors, validate, trySubmit, clearErrors, clearFieldError } = useFormValidation()
const items = ref([])
const namePrefixes = ref([])
const search = ref('')
const showForm = ref(false)
const editing = ref(null)
const form = ref({
  firstName: '', lastName: '', nickName: '', gender: 1,
  fatherId: '', motherId: '', mobile: '', address: '', namePrefixId: '', isDead: false
})

const canManagePrefixes = computed(() =>
  auth.hasAnyPermission('generaltypes.create', 'generaltypes.update', 'generaltypes.delete', 'generaltypes.view')
)

const rules = {
  firstName: [{ type: 'required', msg: 'نام الزامی است' }],
  mobile: [{ type: 'maxLength', param: 20, msg: 'موبایل حداکثر ۲۰ کاراکتر' }]
}

async function load() {
  const [p, t] = await Promise.all([
    api.get('/persons', { params: { search: search.value, page: 1, pageSize: 20 } }),
    api.get('/general-types', { params: { category: 'NamePrefix' } })
  ])
  items.value = p.data.items
  namePrefixes.value = t.data
}

async function submit() {
  if (!validate(rules, form.value)) return
  const payload = {
    firstName: form.value.firstName.trim(),
    lastName: form.value.lastName?.trim() || null,
    nickName: form.value.nickName?.trim() || null,
    gender: form.value.gender,
    fatherId: form.value.fatherId ? +form.value.fatherId : null,
    motherId: form.value.motherId ? +form.value.motherId : null,
    mobile: form.value.mobile?.trim() || null,
    address: form.value.address?.trim() || null,
    namePrefixId: form.value.namePrefixId ? +form.value.namePrefixId : null,
    isDead: form.value.isDead
  }
  const ok = await trySubmit(async () => {
    if (editing.value) {
      await api.put(`/persons/${editing.value}`, payload)
    } else {
      await api.post('/persons', payload)
    }
  })
  if (!ok) return
  closeForm()
  editing.value = null
  await load()
}

function openCreate() {
  editing.value = null
  form.value = { firstName: '', lastName: '', nickName: '', gender: 1, fatherId: '', motherId: '', mobile: '', address: '', namePrefixId: '', isDead: false }
  clearErrors()
  showForm.value = true
}

function openEdit(item) {
  editing.value = item.id
  form.value = {
    firstName: item.firstName, lastName: item.lastName || '', nickName: item.nickName || '',
    gender: item.gender, fatherId: item.fatherId || '', motherId: item.motherId || '',
    mobile: item.mobile || '', address: item.address || '', namePrefixId: item.namePrefixId || '',
    isDead: item.isDead
  }
  clearErrors()
  showForm.value = true
}

function closeForm() {
  showForm.value = false
}

async function remove(id) {
  if (!confirm('حذف این شخص؟')) return
  await api.delete(`/persons/${id}`)
  await load()
}

function genderLabel(v) {
  return genders.find(g => g.value === v)?.label || v
}

function onSearchKeyup(e) {
  if (e.key === 'Enter') load()
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
      <div class="card" style="margin-bottom:1rem">
        <ClearableInput v-model="search" type="search" placeholder="جستجو..." @keyup="onSearchKeyup" />
      </div>

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
              <td data-label="نام"><strong>{{ item.displayName }}</strong></td>
              <td data-label="جنسیت">{{ genderLabel(item.gender) }}</td>
              <td data-label="موبایل">{{ item.mobile }}</td>
              <td data-label="پدر">{{ item.fatherName }}</td>
              <td data-label="مادر">{{ item.motherName }}</td>
              <td data-label="وضعیت">
                <span v-if="item.isDead" class="badge badge-danger">فوت شده</span>
                <span v-else class="badge badge-success">فعال</span>
              </td>
              <td v-if="auth.hasAnyPermission('persons.update', 'persons.delete')">
                <div class="table-actions">
                  <button
                    v-if="auth.hasPermission('persons.update')"
                    class="btn btn-sm btn-outline"
                    @click="openEdit(item)"
                  >ویرایش</button>
                  <button
                    v-if="auth.hasPermission('persons.delete')"
                    class="btn btn-sm btn-danger"
                    @click="remove(item.id)"
                  >حذف</button>
                </div>
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
.link-btn:hover { text-decoration: underline; }
</style>
