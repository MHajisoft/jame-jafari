<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import api from '../api/client'
import { ApiPaths } from '../api/paths'
import { todayGregorian, toPersianDigits, PERSIAN_MONTHS } from '../utils/jalali'
import { enumValue, formatDate, messengerKinds } from '../utils/format'
import { useAuthStore } from '../stores/auth'
import { useUiPrefsStore } from '../stores/uiPrefs'
import { useDialogStore } from '../stores/dialog'
import { useToastStore } from '../stores/toast'
import { useFormValidation } from '../composables/useFormValidation'
import EntityAvatar from '../components/EntityAvatar.vue'
import NickBadge from '../components/NickBadge.vue'
import PersonLifeStatus from '../components/PersonLifeStatus.vue'
import AppMultiSelect from '../components/AppMultiSelect.vue'
import RefreshButton from '../components/RefreshButton.vue'

const SCOPES = [
  { id: 'Day', label: 'امروز', hint: 'سالگرد وفات در همین روز شمسی' },
  { id: 'Week', label: 'هفته جاری', hint: 'وفات در همین هفته (شنبه تا جمعه)' },
  { id: 'Month', label: 'ماه جاری', hint: 'وفات در همین ماه شمسی (هر روز)' },
  { id: 'Season', label: 'فصل جاری', hint: 'وفات در همین فصل شمسی (بهار، تابستان، …)' }
]

const NOTIFY_KINDS = [
  { id: 1, label: 'متن', hint: 'پیام متنی برای هر نفر' },
  { id: 2, label: 'متن و تصویر', hint: 'کارت تسلیت با نام و سالگرد' }
]

const auth = useAuthStore()
const uiPrefs = useUiPrefsStore()
const dialog = useDialogStore()
const toast = useToastStore()
const { trySubmit } = useFormValidation()

const scope = ref('Day')
const loading = ref(false)
const error = ref('')
const report = ref(null)
const allChannels = ref([])
const channelIds = ref([])
const notifyKind = ref(2)
const sending = ref(false)

const canNotify = computed(
  () => auth.hasPermission('messages.send') && auth.hasPermission('deathanniversaries.view')
)

const activeScope = computed(() => SCOPES.find((s) => s.id === scope.value) || SCOPES[0])

const itemCount = computed(() => report.value?.items?.length ?? 0)

const channelOptions = computed(() =>
  allChannels.value
    .filter((c) => uiPrefs.isMessengerEnabled(enumValue(messengerKinds, c.messengerKind, 0)))
    .map((c) => ({
      value: c.id,
      label: c.name,
      group: messengerKindLabelSafe(c.messengerKind),
      icon: enumValue(messengerKinds, c.messengerKind, 1)
    }))
)

function messengerKindLabelSafe(kind) {
  const n = enumValue(messengerKinds, kind, 0)
  return messengerKinds.find((m) => m.value === n)?.label || 'پیام‌رسان'
}

function formatJalaliDeath(item) {
  const month = PERSIAN_MONTHS[item.jalaliDeathMonth - 1] || ''
  return `${toPersianDigits(item.jalaliDeathDay)} ${month} ${toPersianDigits(item.jalaliDeathYear)}`
}

function yearsLabel(years) {
  if (years <= 0) return 'سال جاری'
  return `${toPersianDigits(years)} سال`
}

async function loadChannels() {
  if (!canNotify.value) {
    allChannels.value = []
    return
  }
  try {
    const { data } = await api.get(ApiPaths.lookups.messageChannels, {
      params: { activeOnly: true },
      skipGlobalLoader: true
    })
    allChannels.value = Array.isArray(data) ? data : []
  } catch {
    allChannels.value = []
  }
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const { data } = await api.get(ApiPaths.reports.deathAnniversaries, {
      params: {
        scope: scope.value,
        referenceDate: new Date(todayGregorian()).toISOString()
      }
    })
    report.value = data
  } catch {
    error.value = 'بارگذاری گزارش سالگرد وفات با خطا روبه‌رو شد'
    report.value = null
  } finally {
    loading.value = false
  }
}

async function sendNotify() {
  if (!canNotify.value) return
  if (!itemCount.value) {
    toast.warning('در این بازه کسی ثبت نشده است')
    return
  }
  if (!channelIds.value.length) {
    toast.warning('حداقل یک کانال را انتخاب کنید')
    return
  }

  const kindLabel = NOTIFY_KINDS.find((k) => k.id === notifyKind.value)?.label || ''
  if (!(await dialog.confirm({
    message: `ارسال ${kindLabel} سالگرد برای ${toPersianDigits(itemCount.value)} نفر به ${toPersianDigits(channelIds.value.length)} کانال؟`,
    confirmText: 'ارسال'
  }))) return

  sending.value = true
  const ok = await trySubmit(async () => {
    const { data } = await api.post(
      ApiPaths.reports.deathAnniversariesNotify,
      {
        scope: scope.value,
        referenceDate: new Date(todayGregorian()).toISOString(),
        messageChannelIds: [...channelIds.value],
        kind: notifyKind.value
      },
      { loaderMessage: 'در حال ارسال…' }
    )
    if (data.failed > 0) {
      toast.warning(
        `ارسال: ${toPersianDigits(data.sent)} موفق، ${toPersianDigits(data.failed)} ناموفق` +
          (data.warning ? ` — ${data.warning}` : '')
      )
    } else {
      toast.success(`ارسال شد: ${toPersianDigits(data.sent)} پیام`)
    }
  })
  sending.value = false
  if (!ok) return
}

watch(scope, load)

onMounted(async () => {
  await Promise.all([load(), loadChannels()])
})
</script>

<template>
  <div class="death-report-page">
    <header class="death-report-hero">
      <div class="death-report-hero-text">
        <router-link to="/reports" class="back-link">← گزارشات مالی</router-link>
        <h1 class="page-title">سالگرد وفات</h1>
        <p class="death-report-subtitle">
          فهرست درگذشتگان بر اساس تقویم شمسی — تطابق روز، ماه یا فصل وفات با «امروز»
        </p>
      </div>
    </header>

    <section class="card death-report-filters" aria-label="فیلتر بازه">
      <div class="death-filter-head">
        <p class="filter-intro">بازه مرجع: <strong>{{ report?.scopeLabelFa || '…' }}</strong></p>
        <RefreshButton :loading="loading" @click="load" />
      </div>
      <div class="scope-tabs" role="tablist" aria-label="نوع بازه">
        <button
          v-for="opt in SCOPES"
          :key="opt.id"
          type="button"
          role="tab"
          class="scope-tab"
          :class="{ active: scope === opt.id }"
          :aria-selected="scope === opt.id"
          @click="scope = opt.id"
        >
          <span class="scope-tab-label">{{ opt.label }}</span>
          <span class="scope-tab-hint">{{ opt.hint }}</span>
        </button>
      </div>
      <p class="filter-note text-muted">
        {{ activeScope.hint }}
      </p>
    </section>

    <section v-if="canNotify" class="card death-notify-card" aria-label="ارسال به کانال‌ها">
      <div class="theme-card-head">
        <h3>ارسال به کانال‌های پیام</h3>
        <p class="text-muted">
          برای هر نفر در بازهٔ انتخاب‌شده، پیام به کانال‌های انتخابی فرستاده می‌شود (سالگرد چندم).
        </p>
      </div>

      <div class="notify-kind-grid" role="listbox" aria-label="نوع پیام">
        <button
          v-for="opt in NOTIFY_KINDS"
          :key="opt.id"
          type="button"
          class="notify-kind-option"
          role="option"
          :aria-selected="notifyKind === opt.id"
          :class="{ active: notifyKind === opt.id }"
          @click="notifyKind = opt.id"
        >
          <strong>{{ opt.label }}</strong>
          <span class="text-muted">{{ opt.hint }}</span>
        </button>
      </div>

      <div class="form-group notify-channels">
        <label>کانال / گروه</label>
        <AppMultiSelect
          v-model="channelIds"
          :options="channelOptions"
          placeholder="انتخاب کانال یا گروه"
          search-placeholder="جستجوی کانال…"
        />
        <p v-if="!channelOptions.length" class="field-hint">
          کانال فعالی نیست یا پیام‌رسان آن در تنظیمات خاموش است.
        </p>
      </div>

      <div class="notify-actions">
        <button
          type="button"
          class="btn"
          :disabled="sending || !itemCount || !channelIds.length"
          @click="sendNotify"
        >
          ارسال سالگرد
        </button>
      </div>
    </section>

    <p v-if="error" class="form-error death-report-error">{{ error }}</p>

    <section class="card report-panel" :aria-busy="loading">
      <header class="report-panel-head">
        <h2 class="report-panel-title">درگذشتگان</h2>
        <p class="report-panel-hint">
          {{ loading ? 'در حال بارگذاری…' : `${toPersianDigits(itemCount)} نفر` }}
        </p>
      </header>

      <div v-if="loading && !report" class="report-loading">در حال آماده‌سازی…</div>

      <div v-else class="report-table-wrap">
        <table class="report-table death-report-table">
          <thead>
            <tr>
              <th>نام</th>
              <th>تاریخ وفات</th>
              <th>سالگرد</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in report?.items || []" :key="item.personId">
              <td data-label="نام">
                <div class="person-cell">
                  <EntityAvatar
                    :src="item.picturePath"
                    :name="item.displayName"
                    deceased
                    :size="40"
                    previewable
                    :preview-title="item.displayName"
                  />
                  <div class="person-names">
                    <span class="person-name-row">
                      <strong class="name-deceased">{{ item.displayName }}</strong>
                      <NickBadge :value="item.nickName" />
                      <PersonLifeStatus :is-dead="true" />
                    </span>
                  </div>
                </div>
              </td>
              <td data-label="تاریخ وفات">
                <span>{{ formatJalaliDeath(item) }}</span>
                <small class="greg-hint text-muted">{{ formatDate(item.deathDate) }}</small>
              </td>
              <td data-label="سالگرد">{{ yearsLabel(item.yearsSinceDeath) }}</td>
            </tr>
            <tr v-if="!loading && !(report?.items?.length)">
              <td colspan="3" class="report-empty">در این بازه کسی ثبت نشده است</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </div>
</template>

<style scoped>
.death-report-page {
  display: flex;
  flex-direction: column;
  gap: 1rem;
  padding-bottom: 1rem;
}

.death-report-hero-text {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.back-link {
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--primary);
  text-decoration: none;
  width: fit-content;
}
.back-link:hover { text-decoration: underline; }

.death-report-subtitle {
  margin: 0;
  color: var(--text-muted);
  font-size: 0.92rem;
  line-height: 1.5;
  max-width: 36rem;
}

.death-report-filters {
  padding: 1rem;
}

.death-filter-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  margin-bottom: 0.85rem;
}

.filter-intro {
  margin: 0;
  font-size: 0.95rem;
}

.scope-tabs {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 0.5rem;
}

@media (max-width: 900px) {
  .scope-tabs {
    grid-template-columns: repeat(2, 1fr);
  }
}

@media (max-width: 480px) {
  .scope-tabs {
    grid-template-columns: 1fr;
  }
}

.scope-tab {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.2rem;
  padding: 0.75rem 0.85rem;
  border: 1px solid var(--border);
  border-radius: 12px;
  background: var(--bg);
  color: var(--text);
  cursor: pointer;
  text-align: right;
  min-height: 56px;
}
.scope-tab.active {
  border-color: var(--primary);
  background: color-mix(in srgb, var(--primary) 8%, var(--surface));
}
.scope-tab-label {
  font-weight: 700;
  font-size: 0.95rem;
}
.scope-tab-hint {
  font-size: 0.78rem;
  color: var(--text-muted);
  line-height: 1.35;
}

.filter-note {
  margin: 0.75rem 0 0;
  font-size: 0.85rem;
  line-height: 1.45;
}

.death-notify-card {
  padding: 1rem;
}

.theme-card-head {
  margin-bottom: 0.85rem;
}
.theme-card-head h3 {
  margin: 0;
  font-size: 1.05rem;
}
.theme-card-head p {
  margin: 0.35rem 0 0;
  font-size: 0.9rem;
  line-height: 1.5;
}

.notify-kind-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.65rem;
  margin-bottom: 0.85rem;
}

.notify-kind-option {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  text-align: start;
  padding: 0.85rem 0.9rem;
  border-radius: 12px;
  border: 1px solid var(--border);
  background: var(--bg);
  color: var(--text);
  cursor: pointer;
  min-height: 44px;
}
.notify-kind-option.active {
  border-color: var(--primary);
  box-shadow:
    0 0 0 2px color-mix(in srgb, var(--primary) 30%, transparent),
    var(--shadow);
}
.notify-kind-option.active strong {
  color: var(--primary);
}

.notify-channels {
  margin-bottom: 0.85rem;
}

.notify-actions {
  display: flex;
  justify-content: flex-start;
}

.death-report-error {
  margin: 0;
}

.report-panel {
  padding: 0;
  overflow: hidden;
}

.report-panel-head {
  padding: 1rem 1rem 0.5rem;
  border-bottom: 1px solid var(--border);
}

.report-panel-title {
  margin: 0;
  font-size: 1.05rem;
}

.report-panel-hint {
  margin: 0.25rem 0 0;
  font-size: 0.85rem;
  color: var(--text-muted);
}

.report-loading {
  padding: 2rem 1rem;
  text-align: center;
  color: var(--text-muted);
}

.report-table-wrap {
  overflow-x: auto;
}

.death-report-table {
  width: 100%;
  border-collapse: collapse;
}

.death-report-table th,
.death-report-table td {
  padding: 0.75rem 1rem;
  border-bottom: 1px solid var(--border);
  vertical-align: middle;
}

.person-cell {
  display: flex;
  align-items: center;
  gap: 0.65rem;
  min-width: 0;
}

.person-names {
  min-width: 0;
  flex: 1;
}

.person-name-row {
  display: inline-flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.35rem;
}

.name-deceased {
  color: color-mix(in srgb, var(--text-muted) 55%, var(--text));
  font-weight: 600;
}

.greg-hint {
  display: block;
  margin-top: 0.15rem;
  font-size: 0.78rem;
}

.report-empty {
  text-align: center;
  color: var(--text-muted);
  padding: 2rem 1rem !important;
}

@media (max-width: 768px) {
  .death-report-table thead {
    display: none;
  }
  .death-report-table tr {
    display: block;
    padding: 0.85rem 1rem;
    border-bottom: 1px solid var(--border);
  }
  .death-report-table td {
    display: block;
    border: none;
    padding: 0.25rem 0;
  }
  .death-report-table td::before {
    content: attr(data-label) ': ';
    font-weight: 600;
    color: var(--text-muted);
    font-size: 0.82rem;
  }
  .death-report-table td:first-child::before {
    display: none;
  }
  .notify-kind-grid {
    grid-template-columns: 1fr;
  }
}
</style>
