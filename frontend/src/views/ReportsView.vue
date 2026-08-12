<script setup>
import { ref, onMounted } from 'vue'
import api from '../api/client'
import { ApiPaths } from '../api/paths'
import { formatMoney } from '../utils/format'
import { todayGregorian, startOfJalaliMonthGregorian } from '../utils/jalali'
import DateDisplay from '../components/DateDisplay.vue'
import PersianDatePicker from '../components/PersianDatePicker.vue'

const from = ref(startOfJalaliMonthGregorian())
const to = ref(todayGregorian())
const summary = ref(null)
const balances = ref([])
const costTypes = ref([])
const personIncome = ref([])
const foodCosts = ref([])

async function load() {
  const params = { from: new Date(from.value).toISOString(), to: new Date(to.value).toISOString() }
  const [s, b, c, p, f] = await Promise.all([
    api.get(ApiPaths.reports.summary, { params }),
    api.get(ApiPaths.reports.accountBalances, { params }),
    api.get(ApiPaths.reports.costTypes, { params }),
    api.get(ApiPaths.reports.personIncome, { params }),
    api.get(ApiPaths.reports.foodCosts, { params })
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
  <div class="reports-page">
    <header class="reports-header">
      <h1 class="page-title">گزارشات</h1>
    </header>

    <section class="card reports-filters">
      <div class="reports-filter-grid">
        <PersianDatePicker v-model="from" variant="bar" label="از" />
        <PersianDatePicker v-model="to" variant="bar" label="تا" />
        <button type="button" class="btn reports-apply-btn" @click="load">اعمال</button>
      </div>
    </section>

    <section v-if="summary" class="reports-summary">
      <div class="stat-card stat-card-kpi">
        <span class="label">کل درآمد</span>
        <span class="value text-success">{{ formatMoney(summary.totalIncome) }}</span>
      </div>
      <div class="stat-card stat-card-kpi">
        <span class="label">کل هزینه</span>
        <span class="value text-danger">{{ formatMoney(summary.totalCost) }}</span>
      </div>
      <div class="stat-card stat-card-kpi">
        <span class="label">مانده</span>
        <span class="value">{{ formatMoney(summary.balance) }}</span>
      </div>
    </section>

    <section class="card report-section">
      <h3 class="report-section-title">موجودی حساب‌ها</h3>
      <div class="report-table-wrap">
        <table class="report-table">
          <thead>
            <tr>
              <th>حساب</th>
              <th class="num">درآمد</th>
              <th class="num">هزینه</th>
              <th class="num">مانده</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="b in balances" :key="b.accountId">
              <td data-label="حساب">{{ b.accountName }}</td>
              <td class="num text-success" data-label="درآمد">{{ formatMoney(b.totalIncome) }}</td>
              <td class="num text-danger" data-label="هزینه">{{ formatMoney(b.totalCost) }}</td>
              <td class="num" data-label="مانده"><strong>{{ formatMoney(b.balance) }}</strong></td>
            </tr>
            <tr v-if="!balances.length">
              <td colspan="4" class="report-empty">داده‌ای یافت نشد</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="card report-section">
      <h3 class="report-section-title">تحلیل بر اساس نوع هزینه</h3>
      <div class="report-table-wrap">
        <table class="report-table">
          <thead>
            <tr>
              <th>نوع</th>
              <th class="num">درآمد</th>
              <th class="num">هزینه</th>
              <th class="num">خالص</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="c in costTypes" :key="c.costTypeId">
              <td data-label="نوع">{{ c.costTypeName }}</td>
              <td class="num text-success" data-label="درآمد">{{ formatMoney(c.totalIncome) }}</td>
              <td class="num text-danger" data-label="هزینه">{{ formatMoney(c.totalCost) }}</td>
              <td class="num" data-label="خالص">{{ formatMoney(c.net) }}</td>
            </tr>
            <tr v-if="!costTypes.length">
              <td colspan="4" class="report-empty">داده‌ای یافت نشد</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="card report-section">
      <h3 class="report-section-title">درآمد اشخاص</h3>
      <div class="report-table-wrap">
        <table class="report-table">
          <thead>
            <tr>
              <th>شخص</th>
              <th class="num">تعداد</th>
              <th class="num">مجموع</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="p in personIncome" :key="p.personId">
              <td data-label="شخص">{{ p.personName }}</td>
              <td class="num" data-label="تعداد">{{ p.transactionCount }}</td>
              <td class="num text-success" data-label="مجموع">{{ formatMoney(p.totalAmount) }}</td>
            </tr>
            <tr v-if="!personIncome.length">
              <td colspan="3" class="report-empty">داده‌ای یافت نشد</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <section class="card report-section">
      <h3 class="report-section-title">هزینه تهیه غذا</h3>
      <div class="report-table-wrap">
        <table class="report-table report-table-wide">
          <thead>
            <tr>
              <th>غذا</th>
              <th>تاریخ</th>
              <th class="num">تعداد</th>
              <th class="num">هزینه واحد</th>
              <th class="num">کل</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="f in foodCosts" :key="f.foodId">
              <td data-label="غذا">{{ f.foodName }}</td>
              <td data-label="تاریخ"><DateDisplay :value="f.cookDate" /></td>
              <td class="num" data-label="تعداد">{{ f.totalCount }}</td>
              <td class="num" data-label="هزینه واحد">{{ formatMoney(f.costPerUnit) }}</td>
              <td class="num text-danger" data-label="کل">{{ formatMoney(f.totalCost) }}</td>
            </tr>
            <tr v-if="!foodCosts.length">
              <td colspan="5" class="report-empty">داده‌ای یافت نشد</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>

<style scoped>
.reports-page {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.reports-header {
  margin-bottom: 0.25rem;
}
.reports-header .page-title {
  margin: 0;
}

.reports-filters {
  padding: 1rem 1.15rem;
}
.reports-filter-grid {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr) auto;
  gap: 0.85rem 1rem;
  align-items: end;
  max-width: 760px;
}
.reports-apply-btn {
  min-height: 44px;
  min-width: 5.5rem;
  align-self: end;
}

.reports-summary {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 1rem;
}
.stat-card-kpi {
  display: flex;
  flex-direction: row;
  justify-content: space-between;
  align-items: center;
  gap: 1rem;
  padding: 1rem 1.15rem;
  min-height: 4.5rem;
}
.stat-card-kpi .label {
  margin: 0;
  font-size: 0.88rem;
  font-weight: 600;
  color: var(--text-muted);
  white-space: nowrap;
}
.stat-card-kpi .value {
  margin: 0;
  font-size: 1.2rem;
  font-weight: 700;
  text-align: left;
  direction: ltr;
  unicode-bidi: plaintext;
}

.report-section {
  padding: 0;
  overflow: hidden;
}
.report-section-title {
  margin: 0;
  padding: 1rem 1.15rem 0.75rem;
  font-size: 1rem;
  font-weight: 700;
}
.report-table-wrap {
  overflow-x: auto;
}
.report-table {
  width: 100%;
  border-collapse: collapse;
  table-layout: fixed;
}
.report-table th,
.report-table td {
  padding: 0.75rem 1rem;
  text-align: right;
  border-top: 1px solid var(--border);
  vertical-align: middle;
}
.report-table thead th {
  background: var(--bg-elevated);
  font-size: 0.78rem;
  color: var(--text-muted);
  font-weight: 700;
  border-top: none;
}
.report-table tbody tr:hover td {
  background: color-mix(in srgb, var(--primary) 5%, transparent);
}
.report-table .num {
  text-align: left;
  direction: ltr;
  unicode-bidi: plaintext;
  white-space: nowrap;
}
.report-table th:nth-child(1),
.report-table td:nth-child(1) { width: 34%; }
.report-table th:nth-child(2),
.report-table td:nth-child(2) { width: 22%; }
.report-table th:nth-child(3),
.report-table td:nth-child(3) { width: 22%; }
.report-table th:nth-child(4),
.report-table td:nth-child(4) { width: 22%; }
.report-table-wide th:nth-child(1),
.report-table-wide td:nth-child(1) { width: 26%; }
.report-table-wide th:nth-child(2),
.report-table-wide td:nth-child(2) { width: 18%; }
.report-table-wide th:nth-child(3),
.report-table-wide td:nth-child(3) { width: 14%; }
.report-table-wide th:nth-child(4),
.report-table-wide td:nth-child(4) { width: 20%; }
.report-table-wide th:nth-child(5),
.report-table-wide td:nth-child(5) { width: 22%; }
.report-empty {
  text-align: center;
  color: var(--text-muted);
  padding: 1.25rem;
}

@media (max-width: 768px) {
  .reports-filter-grid {
    grid-template-columns: 1fr;
    max-width: none;
  }
  .reports-apply-btn {
    width: 100%;
  }
  .reports-summary {
    grid-template-columns: 1fr;
  }
  .stat-card-kpi {
    min-height: auto;
  }

  .report-table thead {
    display: none;
  }
  .report-table tbody tr {
    display: block;
    padding: 0.85rem 1rem;
    border-top: 1px solid var(--border);
  }
  .report-table tbody tr:first-child {
    border-top: none;
  }
  .report-table td {
    display: flex;
    justify-content: space-between;
    align-items: baseline;
    gap: 1rem;
    padding: 0.35rem 0;
    border: none;
    width: auto !important;
  }
  .report-table td::before {
    content: attr(data-label);
    font-size: 0.78rem;
    color: var(--text-muted);
    font-weight: 600;
    text-align: right;
    flex: 1;
  }
  .report-table td.num {
    text-align: left;
  }
  .report-table td.report-empty {
    display: block;
    text-align: center;
  }
  .report-table td.report-empty::before {
    display: none;
  }
}
</style>
