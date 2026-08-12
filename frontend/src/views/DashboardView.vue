<script setup>
import { ref, onMounted } from 'vue'
import api from '../api/client'
import { formatMoney } from '../utils/format'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const summary = ref(null)
const balances = ref([])

onMounted(async () => {
  if (auth.hasPermission('reports.view')) {
    const now = new Date()
    const from = new Date(now.getFullYear(), now.getMonth(), 1).toISOString()
    const to = now.toISOString()
    const [s, b] = await Promise.all([
      api.get('/reports/summary', { params: { from, to } }),
      api.get('/reports/account-balances')
    ])
    summary.value = s.data
    balances.value = b.data
  }
})
</script>

<template>
  <div>
    <div class="page-header">
      <h1 class="page-title">داشبورد</h1>
    </div>

    <div v-if="summary" class="grid-3" style="margin-bottom:1.5rem">
      <div class="stat-card">
        <div class="label">درآمد ماه جاری</div>
        <div class="value text-success">{{ formatMoney(summary.totalIncome) }}</div>
      </div>
      <div class="stat-card">
        <div class="label">هزینه ماه جاری</div>
        <div class="value text-danger">{{ formatMoney(summary.totalCost) }}</div>
      </div>
      <div class="stat-card">
        <div class="label">مانده</div>
        <div class="value">{{ formatMoney(summary.balance) }}</div>
      </div>
    </div>

    <div v-if="balances.length" class="card">
      <h3 style="margin-bottom:1rem">موجودی حساب‌ها</h3>
      <table class="mobile-table">
        <thead>
          <tr>
            <th>حساب</th>
            <th>درآمد</th>
            <th>هزینه</th>
            <th>مانده</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="b in balances" :key="b.accountId">
            <td data-label="حساب">{{ b.accountName }}</td>
            <td class="text-success" data-label="درآمد">{{ formatMoney(b.totalIncome) }}</td>
            <td class="text-danger" data-label="هزینه">{{ formatMoney(b.totalCost) }}</td>
            <td data-label="مانده"><strong>{{ formatMoney(b.balance) }}</strong></td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-else-if="!summary" class="card dash-empty">
      <p class="hide-mobile">از منوی کناری بخش‌های مختلف را انتخاب کنید.</p>
      <div class="show-mobile">
        <p>برای شروع، از نوار پایین به درآمد یا هزینه بروید، یا از «بیشتر» بخش‌های دیگر را باز کنید.</p>
        <div class="dash-quick">
          <router-link to="/income" class="btn">درآمد</router-link>
          <router-link to="/cost" class="btn btn-outline">هزینه</router-link>
          <router-link to="/more" class="btn btn-outline">بیشتر</router-link>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.dash-empty p { margin-bottom: 1rem; color: var(--text-muted); }
.dash-quick {
  display: grid;
  grid-template-columns: 1fr;
  gap: 0.65rem;
}
.dash-quick .btn {
  width: 100%;
  justify-content: center;
  text-decoration: none;
}
@media (min-width: 420px) {
  .dash-quick { grid-template-columns: repeat(3, 1fr); }
}
</style>
