<script setup>
import { computed, ref, onMounted } from 'vue'
import api from '../api/client'
import { ApiPaths } from '../api/paths'
import { formatMoney, paymentTypes, paymentTypeLabel, toInputDate } from '../utils/format'
import { useAuthStore } from '../stores/auth'
import { useDialogStore } from '../stores/dialog'
import { useLookupsStore } from '../stores/lookups'
import { useFormValidation } from '../composables/useFormValidation'
import { useEntityForm } from '../composables/useEntityForm'
import { useIsMobile } from '../composables/useMediaQuery'
import DateDisplay from '../components/DateDisplay.vue'
import PersianDatePicker from '../components/PersianDatePicker.vue'
import FileUpload from '../components/FileUpload.vue'
import CurrencyInput from '../components/CurrencyInput.vue'
import AppSelect from '../components/AppSelect.vue'
import PersonSelect from '../components/PersonSelect.vue'
import ClearableInput from '../components/ClearableInput.vue'
import FormHost from '../components/FormHost.vue'
import RowActions from '../components/RowActions.vue'
import PageHeader from '../components/PageHeader.vue'

const auth = useAuthStore()
const dialog = useDialogStore()
const lookups = useLookupsStore()
const isMobile = useIsMobile()
const { error, errors, validate, trySubmit, clearErrors, clearFieldError } = useFormValidation()

const items = ref([])
const accounts = ref([])
const costTypes = ref([])
const loading = ref(false)
const document = ref(null)

function blankForm() {
  return {
    personId: '', accountId: '', amount: '', paymentType: 1,
    costTypeId: '', trackingCode: '', description: '', transactionDate: toInputDate(new Date())
  }
}

const { showForm, editing, form, openCreate, openEdit, closeForm } = useEntityForm(blankForm, {
  onReset: () => {
    document.value = null
    clearErrors()
  }
})

const rules = {
  personId: [{ type: 'required', msg: 'انتخاب شخص الزامی است' }],
  accountId: [{ type: 'required', msg: 'انتخاب حساب الزامی است' }],
  amount: [{ type: 'positiveNumber', msg: 'مبلغ باید بیشتر از صفر باشد' }],
  costTypeId: [{ type: 'required', msg: 'انتخاب نوع هزینه الزامی است' }],
  transactionDate: [{ type: 'required', msg: 'تاریخ الزامی است' }]
}

const pageTitle = computed(() => {
  if (showForm.value && !isMobile.value) {
    return editing.value ? 'ویرایش درآمد' : 'ثبت درآمد جدید'
  }
  return 'تراکنش‌های درآمد'
})

const formTitle = computed(() => (editing.value ? 'ویرایش درآمد' : 'ثبت درآمد جدید'))

async function load() {
  loading.value = true
  try {
    const [t, a, c] = await Promise.all([
      api.get(ApiPaths.incomeTransactions),
      lookups.getAccounts({ activeOnly: true }),
      lookups.getCostTypes({ activeOnly: true })
    ])
    items.value = t.data.items
    accounts.value = a
    costTypes.value = c
  } finally {
    loading.value = false
  }
}

function startEdit(item) {
  openEdit(item.id, {
    personId: item.personId,
    accountId: item.accountId,
    amount: item.amount,
    paymentType: item.paymentType,
    costTypeId: item.costTypeId,
    trackingCode: item.trackingCode || '',
    description: item.description || '',
    transactionDate: toInputDate(item.transactionDate)
  })
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
    if (editing.value) {
      await api.put(ApiPaths.incomeTransaction(editing.value), fd, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
    } else {
      await api.post(ApiPaths.incomeTransactions, fd, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
    }
  }, {
    successMessage: editing.value ? 'درآمد ویرایش شد' : 'درآمد با موفقیت ثبت شد'
  })
  if (!ok) return
  closeForm()
  document.value = null
  await load()
}

async function remove(id) {
  if (!(await dialog.confirmDelete('این تراکنش'))) return
  const ok = await trySubmit(async () => {
    await api.delete(ApiPaths.incomeTransaction(id))
  }, { successMessage: 'تراکنش حذف شد' })
  if (!ok) return
  await load()
}

onMounted(() => load().catch(() => {}))
</script>

<template>
  <div>
    <PageHeader
      :title="pageTitle"
      :form-mode="showForm && !isMobile"
      :show-create="auth.hasPermission('income.create') && (!showForm || isMobile)"
      create-label="ثبت درآمد"
      @create="openCreate"
    />

    <FormHost :show="showForm" :title="isMobile ? formTitle : ''" @close="closeForm">
      <div v-if="error" class="form-error">{{ error }}</div>
      <form @submit.prevent="submit">
        <div class="form-group">
          <label>شخص *</label>
          <PersonSelect
            v-model="form.personId"
            placeholder="انتخاب شخص"
            :invalid="!!errors.personId"
            @change="clearFieldError('personId')"
          />
          <div v-if="errors.personId" class="field-error">{{ errors.personId }}</div>
        </div>
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
        <div class="grid-2">
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
            <label>نوع پرداخت</label>
            <AppSelect
              v-model="form.paymentType"
              :options="paymentTypes"
              placeholder="نوع پرداخت"
              :allow-empty="false"
              :searchable="false"
            />
          </div>
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
          <label>پیوست (فاکتور/رسید){{ editing ? ' — در صورت انتخاب، جایگزین می‌شود' : '' }}</label>
          <FileUpload v-model="document" />
        </div>
        <div class="modal-actions">
          <button type="button" class="btn btn-outline" @click="closeForm">انصراف</button>
          <button type="submit" class="btn">{{ editing ? 'ذخیره' : 'ثبت' }}</button>
        </div>
      </form>
    </FormHost>

    <div v-show="!showForm || isMobile" class="card list-panel">
      <p v-if="loading" class="list-status">در حال بارگذاری…</p>
      <template v-else>
        <table class="mobile-table">
          <thead>
            <tr>
              <th>تاریخ</th><th>شخص</th><th>حساب</th><th>مبلغ</th><th>نوع پرداخت</th>
              <th>نوع هزینه</th><th>کد رهگیری</th><th>توضیحات</th>
              <th v-if="auth.hasAnyPermission('income.update', 'income.delete')"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td data-label="تاریخ"><DateDisplay :value="item.transactionDate" /></td>
              <td data-label="شخص">{{ item.personName || '—' }}</td>
              <td data-label="حساب">{{ item.accountName || '—' }}</td>
              <td class="text-success" data-label="مبلغ">{{ formatMoney(item.amount) }}</td>
              <td data-label="نوع پرداخت">{{ paymentTypeLabel(item.paymentType) }}</td>
              <td data-label="نوع هزینه">{{ item.costTypeName || '—' }}</td>
              <td data-label="کد رهگیری">{{ item.trackingCode || '—' }}</td>
              <td data-label="توضیحات">{{ item.description || '—' }}</td>
              <td v-if="auth.hasAnyPermission('income.update', 'income.delete')">
                <RowActions
                  :show-edit="auth.hasPermission('income.update')"
                  :show-delete="auth.hasPermission('income.delete')"
                  @edit="startEdit(item)"
                  @delete="remove(item.id)"
                />
              </td>
            </tr>
          </tbody>
        </table>
        <div v-if="!items.length" class="empty-state">تراکنشی ثبت نشده</div>
      </template>
    </div>
  </div>
</template>
