<script setup>
import { ref, onMounted } from 'vue'
import api from '../api/client'
import { genders } from '../utils/format'
import { useAuthStore } from '../stores/auth'
import { useFormValidation } from '../composables/useFormValidation'

const auth = useAuthStore()
const { error, errors, validate, trySubmit, clearErrors, clearFieldError } = useFormValidation()
const items = ref([])
const travelPrefixes = ref([])
const allPersons = ref([])
const search = ref('')
const showModal = ref(false)
const editing = ref(null)
const form = ref({
  firstName: '', lastName: '', nickName: '', gender: 1,
  fatherId: '', motherId: '', mobile: '', address: '', travelPrefixId: '', isDead: false
})

const rules = {
  firstName: [{ type: 'required', msg: 'نام الزامی است' }],
  mobile: [{ type: 'maxLength', param: 20, msg: 'موبایل حداکثر ۲۰ کاراکتر' }]
}

async function load() {
  const [p, t] = await Promise.all([
    api.get('/persons', { params: { search: search.value, pageSize: 100 } }),
    api.get('/general-types', { params: { category: 'TravelPrefix' } })
  ])
  items.value = p.data.items
  travelPrefixes.value = t.data
  allPersons.value = p.data.items
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
    travelPrefixId: form.value.travelPrefixId ? +form.value.travelPrefixId : null,
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
  showModal.value = false
  editing.value = null
  await load()
}

function openCreate() {
  editing.value = null
  form.value = { firstName: '', lastName: '', nickName: '', gender: 1, fatherId: '', motherId: '', mobile: '', address: '', travelPrefixId: '', isDead: false }
  clearErrors()
  showModal.value = true
}

function openEdit(item) {
  editing.value = item.id
  form.value = {
    firstName: item.firstName, lastName: item.lastName || '', nickName: item.nickName || '',
    gender: item.gender, fatherId: item.fatherId || '', motherId: item.motherId || '',
    mobile: item.mobile || '', address: item.address || '', travelPrefixId: item.travelPrefixId || '',
    isDead: item.isDead
  }
  clearErrors()
  showModal.value = true
}

async function remove(id) {
  if (!confirm('حذف این شخص؟')) return
  await api.delete(`/persons/${id}`)
  await load()
}

function genderLabel(v) {
  return genders.find(g => g.value === v)?.label || v
}

onMounted(load)
</script>

<template>
  <div>
    <div class="page-header">
      <h1 class="page-title">اشخاص</h1>
      <button v-if="auth.hasPermission('persons.manage')" class="btn btn-fab-mobile" @click="openCreate">
        <span aria-hidden="true">+</span>
        <span class="btn-fab-label">شخص جدید</span>
      </button>
    </div>

    <div class="card" style="margin-bottom:1rem">
      <input v-model="search" class="form-control" placeholder="جستجو..." @keyup.enter="load" />
    </div>

    <div class="card">
      <table class="mobile-table">
        <thead>
          <tr>
            <th>نام</th><th>جنسیت</th><th>موبایل</th><th>پدر</th><th>مادر</th><th>وضعیت</th>
            <th v-if="auth.hasPermission('persons.manage')"></th>
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
            <td v-if="auth.hasPermission('persons.manage')">
              <button class="btn btn-sm btn-outline" @click="openEdit(item)">ویرایش</button>
              <button class="btn btn-sm btn-danger" @click="remove(item.id)">حذف</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
      <div class="modal">
        <h2 class="modal-title">{{ editing ? 'ویرایش شخص' : 'شخص جدید' }}</h2>
        <div v-if="error" class="form-error">{{ error }}</div>
        <form @submit.prevent="submit">
          <div class="grid-2">
            <div class="form-group">
              <label>نام *</label>
              <input
                v-model="form.firstName"
                class="form-control"
                :class="{ 'field-invalid': errors.firstName }"
                @input="clearFieldError('firstName')"
              />
              <div v-if="errors.firstName" class="field-error">{{ errors.firstName }}</div>
            </div>
            <div class="form-group">
              <label>نام خانوادگی</label>
              <input v-model="form.lastName" class="form-control" />
            </div>
          </div>
          <div class="grid-2">
            <div class="form-group">
              <label>نام مستعار</label>
              <input v-model="form.nickName" class="form-control" />
            </div>
            <div class="form-group">
              <label>جنسیت</label>
              <select v-model="form.gender" class="form-control">
                <option v-for="g in genders" :key="g.value" :value="g.value">{{ g.label }}</option>
              </select>
            </div>
          </div>
          <div class="form-group">
            <label>پیشوند سفر</label>
            <select v-model="form.travelPrefixId" class="form-control">
              <option value="">بدون پیشوند</option>
              <option v-for="t in travelPrefixes" :key="t.id" :value="t.id">{{ t.name }}</option>
            </select>
          </div>
          <div class="grid-2">
            <div class="form-group">
              <label>پدر</label>
              <select v-model="form.fatherId" class="form-control">
                <option value="">—</option>
                <option v-for="p in allPersons" :key="p.id" :value="p.id">{{ p.displayName }}</option>
              </select>
            </div>
            <div class="form-group">
              <label>مادر</label>
              <select v-model="form.motherId" class="form-control">
                <option value="">—</option>
                <option v-for="p in allPersons" :key="p.id" :value="p.id">{{ p.displayName }}</option>
              </select>
            </div>
          </div>
          <div class="form-group">
            <label>موبایل</label>
            <input
              v-model="form.mobile"
              class="form-control"
              :class="{ 'field-invalid': errors.mobile }"
              @input="clearFieldError('mobile')"
            />
            <div v-if="errors.mobile" class="field-error">{{ errors.mobile }}</div>
          </div>
          <div class="form-group">
            <label>آدرس</label>
            <textarea v-model="form.address" class="form-control" rows="2"></textarea>
          </div>
          <div class="form-group">
            <label><input v-model="form.isDead" type="checkbox" /> فوت شده</label>
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
