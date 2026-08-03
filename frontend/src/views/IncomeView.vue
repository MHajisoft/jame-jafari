<script setup>
import { ref, onMounted } from 'vue'
import api from '../api/client'
import { formatMoney, paymentTypes, toInputDate } from '../utils/format'
import { useAuthStore } from '../stores/auth'
import { useFormValidation } from '../composables/useFormValidation'
import DateDisplay from '../components/DateDisplay.vue'
import PersianDatePicker from '../components/PersianDatePicker.vue'
import FileUpload from '../components/FileUpload.vue'

const auth = useAuthStore()
const { error, errors, validate, trySubmit, clearErrors } = useFormValidation()
const items = ref([])
const accounts = ref([])
const persons = ref([])
const costTypes = ref([])
const showModal = ref(false)
const document = ref(null)
const form = ref({
  personId: '', accountId: '', amount: '', paymentType: 1,
  costTypeId: '', trackingCode: '', description: '', transactionDate: toInputDate(new Date())
})

const rules = {
  personId: [{ type: 'required', msg: 'انتخاب شخص الزامی است' }],
  accountId: [{ type: 'required', msg: 'انتخاب حساب الزامی است' }],
  amount: [{ type: 'positiveNumber', msg: 'مبلغ باید بیشتر از صفر باشد' }],
  costTypeId: [{ type: 'required', msg: 'انتخاب نوع هزینه الزامی است' }],
  transactionDate: [{ type: 'required', msg: 'تاریخ الزامی است' }]
}

async function load() {
  const [t, a, p, c] = await Promise.all([
    api.get('/income-transactions'),
    api.get('/accounts'),
    api.get('/persons', { params: { pageSize: 500 } }),
    api.get('/cost-types')
  ])
  items.value = t.data.items
  accounts.value = a.data
  persons.value = p.data.items
  costTypes.value = c.data
}

async function submit() {
  if (!validate(rules, form.value)) return
  const data = JSON.stringify({
    personId: +form.value.personId,
    accountId: +form.value.accountId,
    amount: +form.value.amount,
    paymentType: +form.value.paymentType,
    costTypeId: +form.value.costTypeId,
    trackingCode: form.value.trackingCode || null,
    description: form.value.description || null,
    transactionDate: new Date(form.value.transactionDate).toISOString()
  })
  const fd = new FormData()
  fd.append('data', data)
  if (document.value) fd.append('document', document.value)
  const ok = await trySubmit(async () => {
    await api.post('/income-transactions', fd, { headers: { 'Content-Type': 'multipart/form-data' } })
  })
  if (!ok) return
  showModal.value = false
  document.value = null
  await load()
}

async function remove(id) {
  if (!confirm('حذف این تراکنش؟')) return
  await api.delete(`/income-transactions/${id}`)
  await load()
}

function openCreate() {
  form.value = {
    personId: '', accountId: '', amount: '', paymentType: 1,
    costTypeId: '', trackingCode: '', description: '', transactionDate: toInputDate(new Date())
  }
  document.value = null
  clearErrors()
  showModal.value = true
}

function paymentLabel(v) {
  return paymentTypes.find(p => p.value === v)?.label || v
}

onMounted(load)
</script>

<template>
  <div>
    <div class="page-header">
      <h1 class="page-title">تراکنش‌های درآمد</h1>
      <button v-if="auth.hasPermission('income.create')" class="btn btn-fab-mobile" @click="openCreate">
        <span aria-hidden="true">+</span>
        <span class="btn-fab-label">ثبت درآمد</span>
      </button>
    </div>

    <div class="card">
      <table class="mobile-table">
        <thead>
          <tr>
            <th>تاریخ</th><th>شخص</th><th>حساب</th><th>مبلغ</th><th>نوع پرداخت</th>
            <th>نوع هزینه</th><th>کد رهگیری</th><th>توضیحات</th>
            <th v-if="auth.hasPermission('income.delete')"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in items" :key="item.id">
            <td data-label="تاریخ"><DateDisplay :value="item.transactionDate" /></td>
            <td data-label="شخص">{{ item.personName }}</td>
            <td data-label="حساب">{{ item.accountName }}</td>
            <td class="text-success" data-label="مبلغ">{{ formatMoney(item.amount) }}</td>
            <td data-label="نوع پرداخت">{{ paymentLabel(item.paymentType) }}</td>
            <td data-label="نوع هزینه">{{ item.costTypeName }}</td>
            <td data-label="کد رهگیری">{{ item.trackingCode || '—' }}</td>
            <td data-label="توضیحات">{{ item.description }}</td>
            <td v-if="auth.hasPermission('income.delete')">
              <button class="btn btn-sm btn-danger" @click="remove(item.id)">حذف</button>
            </td>
          </tr>
        </tbody>
      </table>
      <div v-if="!items.length" class="empty-state">تراکنشی ثبت نشده</div>
    </div>

    <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
      <div class="modal">
        <h2 class="modal-title">ثبت درآمد جدید</h2>
        <div v-if="error" class="form-error">{{ error }}</div>
        <form @submit.prevent="submit">
          <div class="form-group">
            <label>شخص *</label>
            <select v-model="form.personId" class="form-control" :class="{ 'field-invalid': errors.personId }" required>
              <option value="">انتخاب کنید</option>
              <option v-for="p in persons" :key="p.id" :value="p.id">{{ p.displayName }}</option>
            </select>
            <div v-if="errors.personId" class="field-error">{{ errors.personId }}</div>
          </div>
          <div class="form-group">
            <label>حساب *</label>
            <select v-model="form.accountId" class="form-control" :class="{ 'field-invalid': errors.accountId }" required>
              <option value="">انتخاب کنید</option>
              <option v-for="a in accounts" :key="a.id" :value="a.id">{{ a.name }}</option>
            </select>
            <div v-if="errors.accountId" class="field-error">{{ errors.accountId }}</div>
          </div>
          <div class="grid-2">
            <div class="form-group">
              <label>مبلغ *</label>
              <input v-model="form.amount" type="number" class="form-control" :class="{ 'field-invalid': errors.amount }" required />
              <div v-if="errors.amount" class="field-error">{{ errors.amount }}</div>
            </div>
            <div class="form-group">
              <label>نوع پرداخت</label>
              <select v-model="form.paymentType" class="form-control">
                <option v-for="p in paymentTypes" :key="p.value" :value="p.value">{{ p.label }}</option>
              </select>
            </div>
          </div>
          <div class="form-group">
            <label>نوع هزینه *</label>
            <select v-model="form.costTypeId" class="form-control" :class="{ 'field-invalid': errors.costTypeId }" required>
              <option value="">انتخاب کنید</option>
              <option v-for="c in costTypes" :key="c.id" :value="c.id">{{ c.name }}</option>
            </select>
            <div v-if="errors.costTypeId" class="field-error">{{ errors.costTypeId }}</div>
          </div>
          <div class="form-group">
            <label>تاریخ</label>
            <PersianDatePicker v-model="form.transactionDate" />
          </div>
          <div class="form-group">
            <label>کد رهگیری <span class="optional">(اختیاری)</span></label>
            <input v-model="form.trackingCode" type="text" class="form-control" placeholder="شماره فاکتور / سریال POS / ..." maxlength="100" />
          </div>
          <div class="form-group">
            <label>توضیحات</label>
            <textarea v-model="form.description" class="form-control" rows="2"></textarea>
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
