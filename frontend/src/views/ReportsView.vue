<script setup>
import { ref, onMounted } from 'vue'
import api from '../api/client'
import { formatMoney, toInputDate } from '../utils/format'
import DateDisplay from '../components/DateDisplay.vue'

const from = ref(toInputDate(new Date(new Date().getFullYear(), new Date().getMonth(), 1)))
const to = ref(toInputDate(new Date()))
const summary = ref(null)
const balances = ref([])
const costTypes = ref([])
const personIncome = ref([])
const foodCosts = ref([])

async function load() {
  const params = { from: new Date(from.value).toISOString(), to: new Date(to.value).toISOString() }
  const [s, b, c, p, f] = await Promise.all([
    api.get('/reports/summary', { params }),
    api.get('/reports/account-balances', { params }),
    api.get('/reports/cost-types', { params }),
    api.get('/reports/person-income', { params }),
    api.get('/reports/food-costs', { params })
  ])
  summary.value = s.data
  balances.value = b.data
  costTypes.value = c.data
  personIncome.value = p.data
  foodCosts.value = f.data
}

onMounted(load)
</script>

<template>
  <div>
    <div class="page-header">
      <h1 class="page-title">گزارشات</h1>
      <div style="display:flex;gap:0.5rem">
        <input v-model="from" type="date" class="form-control" style="width:auto" />
        <input v-model="to" type="date" class="form-control" style="width:auto" />
        <button class="btn" @click="load">اعمال</button>
      </div>
    </div>

    <div v-if="summary" class="grid-3" style="margin-bottom:1.5rem">
      <div class="stat-card">
        <div class="label">کل درآمد</div>
        <div class="value text-success">{{ formatMoney(summary.totalIncome) }}</div>
      </div>
      <div class="stat-card">
        <div class="label">کل هزینه</div>
        <div class="value text-danger">{{ formatMoney(summary.totalCost) }}</div>
      </div>
      <div class="stat-card">
        <div class="label">مانده</div>
        <div class="value">{{ formatMoney(summary.balance) }}</div>
      </div>
    </div>

    <div class="card" style="margin-bottom:1.5rem">
      <h3 style="margin-bottom:1rem">موجودی حساب‌ها</h3>
      <table>
        <thead><tr><th>حساب</th><th>درآمد</th><th>هزینه</th><th>مانده</th></tr></thead>
        <tbody>
          <tr v-for="b in balances" :key="b.accountId">
            <td>{{ b.accountName }}</td>
            <td class="text-success">{{ formatMoney(b.totalIncome) }}</td>
            <td class="text-danger">{{ formatMoney(b.totalCost) }}</td>
            <td><strong>{{ formatMoney(b.balance) }}</strong></td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="card" style="margin-bottom:1.5rem">
      <h3 style="margin-bottom:1rem">تحلیل بر اساس نوع هزینه</h3>
      <table>
        <thead><tr><th>نوع</th><th>درآمد</th><th>هزینه</th><th>خالص</th></tr></thead>
        <tbody>
          <tr v-for="c in costTypes" :key="c.costTypeId">
            <td>{{ c.costTypeName }}</td>
            <td class="text-success">{{ formatMoney(c.totalIncome) }}</td>
            <td class="text-danger">{{ formatMoney(c.totalCost) }}</td>
            <td>{{ formatMoney(c.net) }}</td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="card" style="margin-bottom:1.5rem">
      <h3 style="margin-bottom:1rem">درآمد اشخاص</h3>
      <table>
        <thead><tr><th>شخص</th><th>تعداد</th><th>مجموع</th></tr></thead>
        <tbody>
          <tr v-for="p in personIncome" :key="p.personId">
            <td>{{ p.personName }}</td>
            <td>{{ p.transactionCount }}</td>
            <td class="text-success">{{ formatMoney(p.totalAmount) }}</td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="card">
      <h3 style="margin-bottom:1rem">هزینه تهیه غذا</h3>
      <table>
        <thead><tr><th>غذا</th><th>تاریخ</th><th>تعداد</th><th>هزینه واحد</th><th>کل</th></tr></thead>
        <tbody>
          <tr v-for="f in foodCosts" :key="f.foodId">
            <td>{{ f.foodName }}</td>
            <td><DateDisplay :value="f.cookDate" /></td>
            <td>{{ f.totalCount }}</td>
            <td>{{ formatMoney(f.costPerUnit) }}</td>
            <td class="text-danger">{{ formatMoney(f.totalCost) }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>
