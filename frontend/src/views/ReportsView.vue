<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import api from '../api/client'
import { ApiPaths } from '../api/paths'
import { useAuthStore } from '../stores/auth'
import { formatMoney } from '../utils/format'
import { todayGregorian, startOfJalaliMonthGregorian } from '../utils/jalali'
import DateDisplay from '../components/DateDisplay.vue'
import PersianDatePicker from '../components/PersianDatePicker.vue'
import NickBadge from '../components/NickBadge.vue'

const auth = useAuthStore()

const SECTIONS = [
  { id: 'accounts', label: 'حساب‌ها' },
  { id: 'costTypes', label: 'نوع هزینه' },
  { id: 'persons', label: 'اشخاص' },
  { id: 'food', label: 'غذا' }
]

const from = ref(startOfJalaliMonthGregorian())
const to = ref(todayGregorian())
const activeSection = ref('accounts')
const loading = ref(false)
const error = ref('')
const summary = ref(null)
const balances = ref([])
const costTypes = ref([])
const personIncome = ref([])
const foodCosts = ref([])

let loadSeq = 0

const rangeLabel = computed(() => {
  if (!summary.value) return ''
  return 'بازه انتخاب‌شده'
})

const sectionCounts = computed(() => ({
  accounts: balances.value.length,
  costTypes: costTypes.value.length,
  persons: personIncome.value.length,
  food: foodCosts.value.length
}))

async function load() {
  const seq = ++loadSeq
  loading.value = true
  error.value = ''
  try {
    const params = {
      from: new Date(from.value).toISOString(),
      to: new Date(to.value).toISOString()
    }
    const [s, b, c, p, f] = await Promise.all([
      api.get(ApiPaths.reports.summary, { params }),
      api.get(ApiPaths.reports.accountBalances, { params }),
      api.get(ApiPaths.reports.costTypes, { params }),
      api.get(ApiPaths.reports.personIncome, { params }),
      api.get(ApiPaths.reports.foodCosts, { params })
    ])
    if (seq !== loadSeq) return
    summary.value = s.data
    balances.value = b.data
    costTypes.value = c.data
    personIncome.value = p.data
    foodCosts.value = f.data
  } catch {
    if (seq !== loadSeq) return
    error.value = 'بارگذاری گزارش با خطا روبه‌رو شد'
  } finally {
    if (seq === loadSeq) loading.value = false
  }
}

watch([from, to], () => {
  load()
})

onMounted(load)
</script>

<template>
  <div class="reports-page">
    <header class="reports-hero">
      <div class="reports-hero-text">
        <h1 class="page-title">گزارشات</h1>
        <p class="reports-subtitle">خلاصه مالی و جزئیات بازه انتخابی</p>
        <router-link
          v-if="auth.hasPermission('deathanniversaries.view')"
          to="/reports/death-anniversaries"
          class="reports-alt-link"
        >
          گزارش سالگرد وفات →
        </router-link>
      </div>
    </header>

    <section class="card reports-filters" aria-label="فیلتر بازه">
      <div class="reports-filter-grid">
        <PersianDatePicker v-model="from" variant="bar" label="از" required />
        <PersianDatePicker v-model="to" variant="bar" label="تا" required />
        <button type="button" class="btn reports-apply-btn" :disabled="loading" @click="load">
          {{ loading ? 'در حال بارگذاری…' : 'به‌روزرسانی' }}
        </button>
      </div>
    </section>

    <p v-if="error" class="form-error reports-error">{{ error }}</p>

    <section class="reports-kpi" aria-label="خلاصه">
      <article class="kpi-card kpi-income">
        <span class="kpi-label">کل درآمد</span>
        <span class="kpi-value text-success">
          {{ summary ? formatMoney(summary.totalIncome) : '—' }}
        </span>
      </article>
      <article class="kpi-card kpi-cost">
        <span class="kpi-label">کل هزینه</span>
        <span class="kpi-value text-danger">
          {{ summary ? formatMoney(summary.totalCost) : '—' }}
        </span>
      </article>
      <article class="kpi-card kpi-balance">
        <span class="kpi-label">مانده</span>
        <span class="kpi-value">
          {{ summary ? formatMoney(summary.balance) : '—' }}
        </span>
      </article>
    </section>

    <div class="reports-sections" role="tablist" aria-label="بخش‌های گزارش">
      <button
        v-for="sec in SECTIONS"
        :key="sec.id"
        type="button"
        role="tab"
        class="section-tab"
        :class="{ active: activeSection === sec.id }"
        :aria-selected="activeSection === sec.id"
        @click="activeSection = sec.id"
      >
        <span>{{ sec.label }}</span>
        <span class="section-count">{{ sectionCounts[sec.id] }}</span>
      </button>
    </div>

    <section class="card report-panel" :aria-busy="loading">
      <div v-if="loading && !summary" class="report-loading">در حال آماده‌سازی گزارش…</div>

      <template v-else-if="activeSection === 'accounts'">
        <header class="report-panel-head">
          <h2 class="report-panel-title">موجودی حساب‌ها</h2>
          <p class="report-panel-hint">{{ rangeLabel }}</p>
        </header>
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
                <td colspan="4" class="report-empty">داده‌ای در این بازه نیست</td>
              </tr>
            </tbody>
          </table>
        </div>
      </template>

      <template v-else-if="activeSection === 'costTypes'">
        <header class="report-panel-head">
          <h2 class="report-panel-title">تحلیل نوع هزینه</h2>
        </header>
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
                <td colspan="4" class="report-empty">داده‌ای در این بازه نیست</td>
              </tr>
            </tbody>
          </table>
        </div>
      </template>

      <template v-else-if="activeSection === 'persons'">
        <header class="report-panel-head">
          <h2 class="report-panel-title">درآمد اشخاص</h2>
        </header>
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
                <td data-label="شخص">
                  <span class="person-name-with-nick">
                    <span>{{ p.personName }}</span>
                    <NickBadge :value="p.personNickName" />
                  </span>
                </td>
                <td class="num" data-label="تعداد">{{ p.transactionCount }}</td>
                <td class="num text-success" data-label="مجموع">{{ formatMoney(p.totalAmount) }}</td>
              </tr>
              <tr v-if="!personIncome.length">
                <td colspan="3" class="report-empty">داده‌ای در این بازه نیست</td>
              </tr>
            </tbody>
          </table>
        </div>
      </template>

      <template v-else>
        <header class="report-panel-head">
          <h2 class="report-panel-title">هزینه تهیه غذا</h2>
        </header>
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
                <td colspan="5" class="report-empty">داده‌ای در این بازه نیست</td>
              </tr>
            </tbody>
          </table>
        </div>
      </template>
    </section>
  </div>
</template>

<style scoped>
.reports-page {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  padding-bottom: 0.5rem;
}
.reports-hero-text {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}
.reports-hero .page-title { margin: 0; }
.reports-subtitle {
  margin: 0;
  color: var(--text-muted);
  font-size: 0.9rem;
}
.reports-alt-link {
  margin-top: 0.35rem;
  font-size: 0.88rem;
  font-weight: 600;
  color: var(--primary);
  text-decoration: none;
  width: fit-content;
}
.reports-alt-link:hover { text-decoration: underline; }

.reports-filters { padding: 1rem 1.15rem; }
.reports-filter-grid {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr) auto;
  gap: 0.85rem 1rem;
  align-items: end;
}
.reports-apply-btn {
  min-height: 44px;
  min-width: 7rem;
  align-self: end;
}
.reports-error { margin: 0; }

.reports-kpi {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 0.75rem;
}
.kpi-card {
  display: flex;
  flex-direction: column;
  gap: 0.45rem;
  padding: 1rem 1.1rem;
  border-radius: var(--radius, 12px);
  background: var(--surface);
  border: 1px solid color-mix(in srgb, var(--border) 85%, transparent);
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.03);
  min-height: 5rem;
}
.kpi-income {
  border-color: color-mix(in srgb, var(--success) 28%, var(--border));
  background: color-mix(in srgb, var(--success) 6%, var(--surface));
}
.kpi-cost {
  border-color: color-mix(in srgb, var(--danger) 28%, var(--border));
  background: color-mix(in srgb, var(--danger) 5%, var(--surface));
}
.kpi-balance {
  border-color: color-mix(in srgb, var(--primary) 28%, var(--border));
  background: color-mix(in srgb, var(--primary) 6%, var(--surface));
}
.kpi-label {
  font-size: 0.8rem;
  font-weight: 600;
  color: var(--text-muted);
}
.kpi-value {
  font-size: 1.15rem;
  font-weight: 750;
  direction: ltr;
  unicode-bidi: plaintext;
  text-align: start;
  line-height: 1.25;
}

.reports-sections {
  display: flex;
  gap: 0.45rem;
  overflow-x: auto;
  -webkit-overflow-scrolling: touch;
  padding-bottom: 0.15rem;
  scrollbar-width: thin;
}
.section-tab {
  flex: 0 0 auto;
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  border: 1px solid var(--border);
  background: var(--surface);
  color: var(--text-muted);
  border-radius: 999px;
  padding: 0.45rem 0.85rem;
  font: inherit;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  white-space: nowrap;
  -webkit-tap-highlight-color: transparent;
}
.section-tab.active {
  color: var(--primary);
  border-color: color-mix(in srgb, var(--primary) 40%, var(--border));
  background: color-mix(in srgb, var(--primary) 10%, var(--surface));
}
.section-count {
  min-width: 1.35rem;
  height: 1.35rem;
  padding: 0 0.35rem;
  border-radius: 999px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  font-size: 0.72rem;
  background: color-mix(in srgb, var(--text-muted) 12%, transparent);
  color: var(--text);
}
.section-tab.active .section-count {
  background: color-mix(in srgb, var(--primary) 18%, transparent);
  color: var(--primary);
}

.report-panel {
  padding: 0;
  overflow: hidden;
  min-height: 12rem;
}
.report-panel-head {
  padding: 1rem 1.15rem 0.65rem;
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 0.75rem;
}
.report-panel-title {
  margin: 0;
  font-size: 1rem;
  font-weight: 700;
}
.report-panel-hint {
  margin: 0;
  font-size: 0.78rem;
  color: var(--text-muted);
}
.report-loading {
  padding: 2rem 1rem;
  text-align: center;
  color: var(--text-muted);
}
.report-table-wrap { overflow-x: auto; }
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
@media (hover: hover) and (pointer: fine) {
  .report-table tbody tr:hover td {
    background: color-mix(in srgb, var(--primary) 5%, transparent);
  }
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
  }
  .reports-apply-btn { width: 100%; }
  .reports-kpi { grid-template-columns: 1fr; }
  .kpi-card {
    flex-direction: row;
    align-items: center;
    justify-content: space-between;
    min-height: auto;
  }
  .kpi-value { text-align: end; }

  .report-table thead { display: none; }
  .report-table tbody tr {
    display: block;
    padding: 0.85rem 1rem;
    border-top: 1px solid var(--border);
  }
  .report-table tbody tr:first-child { border-top: none; }
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
  .report-table td.num { text-align: left; }
  .report-table td.report-empty {
    display: block;
    text-align: center;
  }
  .report-table td.report-empty::before { display: none; }
}
</style>
