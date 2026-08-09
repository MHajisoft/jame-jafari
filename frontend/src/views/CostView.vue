<script setup>
import { ref, onMounted } from 'vue'
import api from '../api/client'
import { formatMoney, toInputDate } from '../utils/format'
import { useAuthStore } from '../stores/auth'
import { useFormValidation } from '../composables/useFormValidation'
import DateDisplay from '../components/DateDisplay.vue'
import PersianDatePicker from '../components/PersianDatePicker.vue'
import FileUpload from '../components/FileUpload.vue'
import CurrencyInput from '../components/CurrencyInput.vue'
import AppSelect from '../components/AppSelect.vue'
import ClearableInput from '../components/ClearableInput.vue'

const auth = useAuthStore()
const { error, errors, validate, trySubmit, clearErrors, clearFieldError } = useFormValidation()
const items = ref([])
const accounts = ref([])
const costTypes = ref([])
const showModal = ref(false)
const document = ref(null)
const form = ref({
  accountId: '', amount: '', costTypeId: '', trackingCode: '', description: '', transactionDate: toInputDate(new Date())
})

const rules = {
  accountId: [{ type: 'required', msg: 'انتخاب حساب الزامی است' }],
  amount: [{ type: 'positiveNumber', msg: 'مبلغ باید بیشتر از صفر باشد' }],
  costTypeId: [{ type: 'required', msg: 'انتخاب نوع هزینه الزامی است' }],
  transactionDate: [{ type: 'required', msg: 'تاریخ الزامی است' }]
}

async function load() {
  const [t, a, c] = await Promise.all([
    api.get('/cost-transactions'),
    api.get('/accounts'),
    api.get('/cost-types')
  ])
  items.value = t.data.items
  accounts.value = a.data
  costTypes.value = c.data
}

async function submit() {
  if (!validate(rules, form.value)) return
  const data = JSON.stringify({
    accountId: +form.value.accountId,
    amount: +form.value.amount,
    costTypeId: +form.value.costTypeId,
    trackingCode: form.value.trackingCode || null,
    description: form.value.description || null,
    transactionDate: new Date(form.value.transactionDate).toISOString()
  })
  const fd = new FormData()
  fd.append('data', data)
  if (document.value) fd.append('document', document.value)
  const ok = await trySubmit(async () => {
    await api.post('/cost-transactions', fd, { headers: { 'Content-Type': 'multipart/form-data' } })
  })
  if (!ok) return
  showModal.value = false
  document.value = null
  await load()
}

async function remove(id) {
  if (!confirm('حذف این تراکنش؟')) return
  await api.delete(`/cost-transactions/${id}`)
  await load()
}

function openCreate() {
  form.value = {
    accountId: '', amount: '', costTypeId: '', trackingCode: '', description: '', transactionDate: toInputDate(new Date())
  }
  document.value = null
  clearErrors()
  showModal.value = true
}

onMounted(load)
</script>

<template>
  <div>
    <div class="page-header">
      <h1 class="page-title">تراکنش‌های هزینه</h1>
      <button v-if="auth.hasPermission('cost.create')" class="btn btn-fab-mobile" @click="openCreate">
        <span aria-hidden="true">+</span>
        <span class="btn-fab-label">ثبت هزینه</span>
      </button>
    </div>

    <div class="card">
      <table class="mobile-table">
        <thead>
          <tr>
            <th>تاریخ</th><th>حساب</th><th>مبلغ</th><th>نوع هزینه</th><th>کد رهگیری</th><th>توضیحات</th>
            <th v-if="auth.hasPermission('cost.delete')"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in items" :key="item.id">
            <td data-label="تاریخ"><DateDisplay :value="item.transactionDate" /></td>
            <td data-label="حساب">{{ item.accountName }}</td>
            <td class="text-danger" data-label="مبلغ">{{ formatMoney(item.amount) }}</td>
            <td data-label="نوع هزینه">{{ item.costTypeName }}</td>
            <td data-label="کد رهگیری">{{ item.trackingCode || '—' }}</td>
            <td data-label="توضیحات">{{ item.description }}</td>
            <td v-if="auth.hasPermission('cost.delete')">
              <button class="btn btn-sm btn-danger" @click="remove(item.id)">حذف</button>
            </td>
          </tr>
        </tbody>
      </table>
      <div v-if="!items.length" class="empty-state">تراکنشی ثبت نشده</div>
    </div>

    <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
      <div class="modal">
        <h2 class="modal-title">ثبت هزینه جدید</h2>
        <div v-if="error" class="form-error">{{ error }}</div>
        <form @submit.prevent="submit">
          <div class="form-group">
            <label>حساب *</label>
            <AppSelect
              v-model="form.accountId"
              :options="accounts"
              option-value="id"
              option-label="name"
              placeholder="انتخاب کنید"
              :invalid="!!errors.accountId"
              @change="clearFieldError('accountId')"
            />
            <div v-if="errors.accountId" class="field-error">{{ errors.accountId }}</div>
          </div>
          <div class="form-group">
            <label>مبلغ *</label>
            <CurrencyInput
              v-model="form.amount"
              :invalid="!!errors.amount"
              placeholder="مثلاً 1,500,000"
              @input="clearFieldError('amount')"
            />
            <div v-if="errors.amount" class="field-error">{{ errors.amount }}</div>
          </div>
          <div class="form-group">
            <label>نوع هزینه *</label>
            <AppSelect
              v-model="form.costTypeId"
              :options="costTypes"
              option-value="id"
              option-label="name"
              placeholder="انتخاب کنید"
              :invalid="!!errors.costTypeId"
              @change="clearFieldError('costTypeId')"
            />
            <div v-if="errors.costTypeId" class="field-error">{{ errors.costTypeId }}</div>
          </div>
          <div class="form-group">
            <label>تاریخ</label>
            <PersianDatePicker v-model="form.transactionDate" />
          </div>
          <div class="form-group">
            <label>کد رهگیری <span class="optional">(اختیاری)</span></label>
            <ClearableInput v-model="form.trackingCode" placeholder="شماره فاکتور / سریال POS / ..." :maxlength="100" />
          </div>
          <div class="form-group">
            <label>توضیحات</label>
            <ClearableInput v-model="form.description" type="textarea" :rows="2" />
          </div>
          <div class="form-group">
            <label>پیوست (فاکتور/رسید)</label>
            <FileUpload v-model="document" />
          </div>
          <div class="modal-actions">
            <button type="button" class="btn btn-outline" @click="showModal = false">انصراف</button>
            <button type="submit" class="btn">ثبت</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>
