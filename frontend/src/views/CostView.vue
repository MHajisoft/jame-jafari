<script setup>
import { computed, ref, onMounted } from 'vue'
import api from '../api/client'
import { ApiPaths } from '../api/paths'
import { formatMoney, toInputDate } from '../utils/format'
import DocumentAttachmentList from '../components/DocumentAttachmentList.vue'
import { useAuthStore } from '../stores/auth'
import { useDialogStore } from '../stores/dialog'
import { useLookupsStore } from '../stores/lookups'
import { useFormValidation } from '../composables/useFormValidation'
import { useEntityForm } from '../composables/useEntityForm'
import { useIsMobile } from '../composables/useMediaQuery'
import DateDisplay from '../components/DateDisplay.vue'
import PersianDatePicker from '../components/PersianDatePicker.vue'
import TransactionAttachmentsField from '../components/TransactionAttachmentsField.vue'
import CurrencyInput from '../components/CurrencyInput.vue'
import AppSelect from '../components/AppSelect.vue'
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
const formLookupsReady = ref(false)
const pendingDocuments = ref([])
const existingAttachments = ref([])

function blankForm() {
  return {
    accountId: '', amount: '', costTypeId: '', trackingCode: '', description: '', transactionDate: toInputDate(new Date())
  }
}

const { showForm, editing, form, openCreate, openEdit, closeForm } = useEntityForm(blankForm, {
  onReset: () => {
    pendingDocuments.value = []
    if (!editing.value) existingAttachments.value = []
    clearErrors()
  }
})

const rules = {
  accountId: [{ type: 'required', msg: 'انتخاب حساب الزامی است' }],
  amount: [{ type: 'positiveNumber', msg: 'مبلغ باید بیشتر از صفر باشد' }],
  costTypeId: [{ type: 'required', msg: 'انتخاب نوع هزینه الزامی است' }],
  transactionDate: [{ type: 'required', msg: 'تاریخ الزامی است' }]
}

const pageTitle = computed(() => {
  if (showForm.value && !isMobile.value) {
    return editing.value ? 'ویرایش هزینه' : 'ثبت هزینه جدید'
  }
  return 'تراکنش‌های هزینه'
})

const formTitle = computed(() => (editing.value ? 'ویرایش هزینه' : 'ثبت هزینه جدید'))

async function load() {
  loading.value = true
  formLookupsReady.value = false
  try {
    const t = await api.get(ApiPaths.costTransactions)
    items.value = t.data.items
    try {
      const [a, c] = await Promise.all([
        lookups.getAccounts({ activeOnly: true }),
        lookups.getCostTypes({ activeOnly: true })
      ])
      accounts.value = a
      costTypes.value = c
      formLookupsReady.value = true
    } catch {
      accounts.value = []
      costTypes.value = []
    }
  } finally {
    loading.value = false
  }
}

function onExistingAttachmentsChange(list) {
  existingAttachments.value = list
  syncListAttachments(list)
}

function syncListAttachments(list) {
  if (!editing.value) return
  const item = items.value.find(i => i.id === editing.value)
  if (item) item.attachments = list
}

function startEdit(item) {
  openEdit(item.id, {
    accountId: item.accountId,
    amount: item.amount,
    costTypeId: item.costTypeId,
    trackingCode: item.trackingCode || '',
    description: item.description || '',
    transactionDate: toInputDate(item.transactionDate)
  })
  existingAttachments.value = [...(item.attachments || [])]
  pendingDocuments.value = []
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
  for (const file of pendingDocuments.value) {
    if (auth.hasPermission('attachments.add')) fd.append('documents', file)
  }
  const ok = await trySubmit(async () => {
    if (editing.value) {
      await api.put(ApiPaths.costTransaction(editing.value), fd, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
    } else {
      await api.post(ApiPaths.costTransactions, fd, {
        headers: { 'Content-Type': 'multipart/form-data' }
      })
    }
  }, {
    successMessage: editing.value ? 'هزینه ویرایش شد' : 'هزینه با موفقیت ثبت شد'
  })
  if (!ok) return
  closeForm()
  pendingDocuments.value = []
  existingAttachments.value = []
  await load()
}

async function remove(id) {
  if (!(await dialog.confirmDelete('این تراکنش'))) return
  const ok = await trySubmit(async () => {
    await api.delete(ApiPaths.costTransaction(id))
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
      :show-create="auth.hasPermission('cost.create') && formLookupsReady && (!showForm || isMobile)"
      create-label="ثبت هزینه"
      @create="openCreate"
    />

    <p
      v-if="auth.hasPermission('cost.create') && !loading && !formLookupsReady"
      class="form-error list-lookup-hint"
    >
      بارگذاری لیست حساب‌ها یا انواع هزینه ممکن نشد؛ ثبت هزینه جدید غیرفعال است.
    </p>

    <FormHost :show="showForm" :title="isMobile ? formTitle : ''" @close="closeForm">
      <div v-if="error" class="form-error">{{ error }}</div>
      <form class="form-layout-adaptive" @submit.prevent="submit">
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
          <label>تاریخ</label>
          <PersianDatePicker v-model="form.transactionDate" />
        </div>
        <div class="form-group form-span-2">
          <label>کد رهگیری <span class="optional">(اختیاری)</span></label>
          <ClearableInput v-model="form.trackingCode" placeholder="شماره فاکتور / سریال POS / ..." :maxlength="100" />
        </div>
        <div class="form-group form-span-full">
          <label>توضیحات</label>
          <ClearableInput v-model="form.description" type="textarea" :rows="2" />
        </div>
        <div
          v-if="auth.hasAnyPermission('attachments.view', 'attachments.add', 'attachments.delete')"
          class="form-group form-span-full"
        >
          <label>پیوست‌ها (فاکتور/رسید)</label>
          <p
            v-if="editing && auth.hasPermission('attachments.view') && auth.hasPermission('attachments.delete') && existingAttachments.length"
            class="text-muted"
            style="margin: 0 0 0.5rem; font-size: 0.85rem"
          >
            برای حذف هر پیوست، روی دکمه حذف (×) آن کلیک کنید.
          </p>
          <TransactionAttachmentsField
            v-model:pending="pendingDocuments"
            :existing="existingAttachments"
            :transaction-id="editing"
            :delete-attachment-path="ApiPaths.costTransactionAttachment"
            :can-view="auth.hasPermission('attachments.view')"
            :can-add="auth.hasPermission('attachments.add')"
            :can-delete="auth.hasPermission('attachments.delete')"
            @update:existing="onExistingAttachmentsChange"
          />
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
              <th>تاریخ</th><th>حساب</th><th>مبلغ</th><th>نوع هزینه</th><th>کد رهگیری</th><th>توضیحات</th>
              <th v-if="auth.hasPermission('attachments.view')">پیوست</th>
              <th v-if="auth.hasAnyPermission('cost.update', 'cost.delete')"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td data-label="تاریخ"><DateDisplay :value="item.transactionDate" /></td>
              <td data-label="حساب">{{ item.accountName || '—' }}</td>
              <td class="text-danger" data-label="مبلغ">{{ formatMoney(item.amount) }}</td>
              <td data-label="نوع هزینه">{{ item.costTypeName || '—' }}</td>
              <td data-label="کد رهگیری">{{ item.trackingCode || '—' }}</td>
              <td data-label="توضیحات">{{ item.description || '—' }}</td>
              <td v-if="auth.hasPermission('attachments.view')" data-label="پیوست">
                <DocumentAttachmentList :attachments="item.attachments" />
              </td>
              <td v-if="auth.hasAnyPermission('cost.update', 'cost.delete')">
                <RowActions
                  :show-edit="auth.hasPermission('cost.update')"
                  :show-delete="auth.hasPermission('cost.delete')"
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
