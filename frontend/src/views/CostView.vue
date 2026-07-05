<script setup>
import { ref, onMounted } from 'vue'
import api from '../api/client'
import { formatMoney, toInputDate } from '../utils/format'
import { useAuthStore } from '../stores/auth'
import DateDisplay from '../components/DateDisplay.vue'
import FileUpload from '../components/FileUpload.vue'

const auth = useAuthStore()
const items = ref([])
const accounts = ref([])
const costTypes = ref([])
const showModal = ref(false)
const document = ref(null)
const form = ref({
  accountId: '', amount: '', costTypeId: '', description: '', transactionDate: toInputDate(new Date())
})

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
  const data = JSON.stringify({
    ...form.value,
    accountId: +form.value.accountId,
    amount: +form.value.amount,
    costTypeId: +form.value.costTypeId,
    transactionDate: new Date(form.value.transactionDate).toISOString()
  })
  const fd = new FormData()
  fd.append('data', data)
  if (document.value) fd.append('document', document.value)
  await api.post('/cost-transactions', fd, { headers: { 'Content-Type': 'multipart/form-data' } })
  showModal.value = false
  document.value = null
  await load()
}

async function remove(id) {
  if (!confirm('حذف این تراکنش؟')) return
  await api.delete(`/cost-transactions/${id}`)
  await load()
}

onMounted(load)
</script>

<template>
  <div>
    <div class="page-header">
      <h1 class="page-title">تراکنش‌های هزینه</h1>
      <button v-if="auth.hasPermission('cost.create')" class="btn" @click="showModal = true">+ ثبت هزینه</button>
    </div>

    <div class="card">
      <table>
        <thead>
          <tr>
            <th>تاریخ</th>
            <th>حساب</th>
            <th>مبلغ</th>
            <th>نوع هزینه</th>
            <th>توضیحات</th>
            <th v-if="auth.hasPermission('cost.delete')"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in items" :key="item.id">
            <td><DateDisplay :value="item.transactionDate" /></td>
            <td>{{ item.accountName }}</td>
            <td class="text-danger">{{ formatMoney(item.amount) }}</td>
            <td>{{ item.costTypeName }}</td>
            <td>{{ item.description }}</td>
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
        <div class="form-group">
          <label>حساب</label>
          <select v-model="form.accountId" class="form-control" required>
            <option value="">انتخاب کنید</option>
            <option v-for="a in accounts" :key="a.id" :value="a.id">{{ a.name }}</option>
          </select>
        </div>
        <div class="form-group">
          <label>مبلغ</label>
          <input v-model="form.amount" type="number" class="form-control" required />
        </div>
        <div class="form-group">
          <label>نوع هزینه</label>
          <select v-model="form.costTypeId" class="form-control" required>
            <option value="">انتخاب کنید</option>
            <option v-for="c in costTypes" :key="c.id" :value="c.id">{{ c.name }}</option>
          </select>
        </div>
        <div class="form-group">
          <label>تاریخ</label>
          <input v-model="form.transactionDate" type="date" class="form-control" />
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
          <button class="btn btn-outline" @click="showModal = false">انصراف</button>
          <button class="btn" @click="submit">ثبت</button>
        </div>
      </div>
    </div>
  </div>
</template>
