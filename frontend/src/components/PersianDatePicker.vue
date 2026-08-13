<script setup>
import { ref, computed, watch, nextTick, onMounted, onBeforeUnmount } from 'vue'
import {
  PERSIAN_MONTHS,
  PERSIAN_WEEKDAYS,
  MIN_JALALI_YEAR,
  toJalaliParts,
  jalaliToGregorian,
  getDaysInJalaliMonth,
  getJalaliMonthGrid,
  formatJalali,
  todayGregorian,
  toPersianDigits,
  maxJalaliYear
} from '../utils/jalali'
import { useUiPrefsStore } from '../stores/uiPrefs'
import { useOverlayBack } from '../composables/useOverlayBack'

const props = defineProps({
  modelValue: { type: String, default: '' },
  label: { type: String, default: '' },
  placeholder: { type: String, default: 'انتخاب تاریخ...' },
  variant: { type: String, default: 'field' }, // field | bar
  /** When true, field may start with a default and confirm applies draft even if empty. */
  required: { type: Boolean, default: false }
})

const emit = defineEmits(['update:modelValue', 'change'])

const uiPrefs = useUiPrefsStore()

const ITEM_HEIGHT = 40
const VISIBLE_ROWS = 5
const WHEEL_HEIGHT = ITEM_HEIGHT * VISIBLE_ROWS
const PAD = (WHEEL_HEIGHT - ITEM_HEIGHT) / 2

const visible = ref(false)
const isMobile = ref(false)
const draft = ref({ year: 1404, month: 1, day: 1 })
const view = ref({ year: 1404, month: 1 })
const openMenu = ref(null) // 'month' | 'year' | null
const dayWheel = ref(null)
const monthWheel = ref(null)
const yearWheel = ref(null)
const yearMenuList = ref(null)
let scrollTimer = null
/** User changed wheels / picked a day — needed so optional empty fields are not filled on blind confirm. */
const draftTouched = ref(false)

function markDraftTouched() {
  draftTouched.value = true
}

/** Mobile: sheet (action bar) vs modal — preference only applies under 768px. */
const useMobileSheet = computed(
  () => isMobile.value && uiPrefs.datePickerMobileMode === 'sheet'
)

const displayValue = computed(() =>
  props.modelValue ? formatJalali(props.modelValue, 'D MMMM YYYY') : ''
)

const hasValue = computed(() => !!props.modelValue)

const years = computed(() => {
  const max = maxJalaliYear(20)
  return Array.from({ length: max - MIN_JALALI_YEAR + 1 }, (_, i) => MIN_JALALI_YEAR + i)
})

const monthOptions = computed(() =>
  PERSIAN_MONTHS.map((label, i) => ({ value: i + 1, label }))
)

const yearOptions = computed(() =>
  years.value.map((y) => ({ value: y, label: toPersianDigits(y) }))
)

const viewMonthLabel = computed(() => PERSIAN_MONTHS[view.value.month - 1] || '')
const viewYearLabel = computed(() => toPersianDigits(view.value.year))

const days = computed(() => {
  const max = getDaysInJalaliMonth(draft.value.year, draft.value.month)
  return Array.from({ length: max }, (_, i) => i + 1)
})

const monthGrid = computed(() => getJalaliMonthGrid(view.value.year, view.value.month))

const todayParts = computed(() => toJalaliParts(todayGregorian()))

function checkMobile() {
  isMobile.value = window.matchMedia('(max-width: 768px)').matches
}

function clampDraft() {
  const max = getDaysInJalaliMonth(draft.value.year, draft.value.month)
  if (draft.value.day > max) draft.value.day = max
  if (draft.value.year < MIN_JALALI_YEAR) draft.value.year = MIN_JALALI_YEAR
}

function syncDraftFromValue() {
  const parts = toJalaliParts(props.modelValue || todayGregorian())
  const year = Math.max(parts.year, MIN_JALALI_YEAR)
  draft.value = { year, month: parts.month, day: parts.day }
  view.value = { year, month: parts.month }
  clampDraft()
}

function scrollToIndex(el, index) {
  if (!el) return
  el.scrollTop = index * ITEM_HEIGHT
}

function scrollWheelsToDraft() {
  nextTick(() => {
    scrollToIndex(dayWheel.value, draft.value.day - 1)
    scrollToIndex(monthWheel.value, draft.value.month - 1)
    const yearIndex = years.value.indexOf(draft.value.year)
    scrollToIndex(yearWheel.value, yearIndex >= 0 ? yearIndex : 0)
  })
}

async function toggleMenu(menu) {
  openMenu.value = openMenu.value === menu ? null : menu
  if (openMenu.value === 'year') {
    await nextTick()
    const selected = yearMenuList.value?.querySelector('.menu-item.selected')
    selected?.scrollIntoView({ block: 'center' })
  }
}

function closeMenus() {
  openMenu.value = null
}

function open() {
  syncDraftFromValue()
  draftTouched.value = false
  openMenu.value = null
  visible.value = true
  if (useMobileSheet.value) scrollWheelsToDraft()
}

function cancel() {
  openMenu.value = null
  visible.value = false
}

useOverlayBack(visible, cancel, {
  enabled: () => isMobile.value,
  stateKey: 'appDatePicker'
})

function applyDate(year, month, day) {
  const iso = jalaliToGregorian(year, month, day)
  emit('update:modelValue', iso)
  emit('change', iso)
  openMenu.value = null
  visible.value = false
}

function confirm() {
  if (!props.required && !props.modelValue && !draftTouched.value) {
    cancel()
    return
  }
  clampDraft()
  applyDate(draft.value.year, draft.value.month, draft.value.day)
}

function pickDay(cell) {
  closeMenus()
  markDraftTouched()
  if (!cell.current) {
    view.value = { year: cell.year, month: cell.month }
  }
  draft.value = { year: cell.year, month: cell.month, day: cell.day }
  clampDraft()
  applyDate(draft.value.year, draft.value.month, draft.value.day)
}

function goToday() {
  markDraftTouched()
  const t = todayParts.value
  draft.value = { ...t }
  view.value = { year: t.year, month: t.month }
  applyDate(t.year, t.month, t.day)
}

function clearDate(e) {
  if (props.required) return
  e?.stopPropagation?.()
  emit('update:modelValue', '')
  emit('change', '')
  openMenu.value = null
  visible.value = false
}

function shiftMonth(delta) {
  closeMenus()
  let { year, month } = view.value
  month += delta
  if (month < 1) {
    month = 12
    year -= 1
  } else if (month > 12) {
    month = 1
    year += 1
  }
  if (year < MIN_JALALI_YEAR) return
  if (year > maxJalaliYear(20)) return
  view.value = { year, month }
}

function onViewMonthChange(month) {
  view.value = { ...view.value, month: +month }
  closeMenus()
}

function onViewYearChange(year) {
  const y = Math.max(MIN_JALALI_YEAR, +year)
  view.value = { ...view.value, year: y }
  closeMenus()
}

function isSelected(cell) {
  if (!props.modelValue) {
    return cell.year === draft.value.year && cell.month === draft.value.month && cell.day === draft.value.day
  }
  const selected = toJalaliParts(props.modelValue)
  return selected
    && cell.year === selected.year
    && cell.month === selected.month
    && cell.day === selected.day
}

function isToday(cell) {
  const t = todayParts.value
  return t && cell.year === t.year && cell.month === t.month && cell.day === t.day
}

function onWheelScroll(column) {
  markDraftTouched()
  clearTimeout(scrollTimer)
  scrollTimer = setTimeout(() => {
    if (column === 'day' && dayWheel.value) {
      const index = Math.round(dayWheel.value.scrollTop / ITEM_HEIGHT)
      draft.value.day = days.value[Math.min(index, days.value.length - 1)] || 1
      scrollToIndex(dayWheel.value, draft.value.day - 1)
    }
    if (column === 'month' && monthWheel.value) {
      const index = Math.round(monthWheel.value.scrollTop / ITEM_HEIGHT)
      draft.value.month = Math.min(Math.max(index + 1, 1), 12)
      clampDraft()
      scrollToIndex(monthWheel.value, draft.value.month - 1)
      scrollToIndex(dayWheel.value, draft.value.day - 1)
    }
    if (column === 'year' && yearWheel.value) {
      const index = Math.round(yearWheel.value.scrollTop / ITEM_HEIGHT)
      draft.value.year = years.value[Math.min(Math.max(index, 0), years.value.length - 1)]
      clampDraft()
      scrollToIndex(yearWheel.value, years.value.indexOf(draft.value.year))
      scrollToIndex(dayWheel.value, draft.value.day - 1)
    }
  }, 80)
}

watch(() => props.modelValue, () => {
  if (!visible.value) syncDraftFromValue()
})

onMounted(() => {
  checkMobile()
  window.addEventListener('resize', checkMobile)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', checkMobile)
})
</script>

<template>
  <div class="persian-date-picker" :class="`variant-${variant}`">
    <label v-if="label" class="picker-label">{{ label }}</label>
    <div class="date-field" :class="{ 'has-clear': hasValue }" @click="open">
      <input
        readonly
        :value="displayValue"
        :placeholder="placeholder"
        class="form-control date-input"
      />
      <button
        v-if="hasValue && !required"
        type="button"
        class="field-clear"
        tabindex="-1"
        aria-hidden="true"
        title="پاک کردن"
        @mousedown.prevent
        @click="clearDate"
      >
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round">
          <line x1="18" y1="6" x2="6" y2="18" />
          <line x1="6" y1="6" x2="18" y2="18" />
        </svg>
      </button>
      <span class="calendar-icon" aria-hidden="true">
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
          <rect x="3" y="4" width="18" height="18" rx="2" />
          <line x1="16" y1="2" x2="16" y2="6" />
          <line x1="8" y1="2" x2="8" y2="6" />
          <line x1="3" y1="10" x2="21" y2="10" />
        </svg>
      </span>
    </div>

    <Teleport to="body">
      <div
        v-if="visible"
        class="picker-overlay"
        :class="{ mobile: useMobileSheet, desktop: !useMobileSheet }"
        @click.self="cancel"
      >
        <!-- Mobile sheet (action bar): wheel picker -->
        <div v-if="useMobileSheet" class="picker-sheet">
          <div class="picker-wheels" :style="{ height: `${WHEEL_HEIGHT}px` }">
            <div class="wheel-highlight" :style="{ height: `${ITEM_HEIGHT}px`, top: `${PAD}px` }" />
            <div ref="dayWheel" class="wheel-col" @scroll="onWheelScroll('day')">
              <div :style="{ height: `${PAD}px` }" />
              <div
                v-for="d in days"
                :key="`d-${d}`"
                class="wheel-item"
                :class="{ selected: d === draft.day }"
                :style="{ height: `${ITEM_HEIGHT}px` }"
              >
                {{ toPersianDigits(d) }}
              </div>
              <div :style="{ height: `${PAD}px` }" />
            </div>
            <div ref="monthWheel" class="wheel-col" @scroll="onWheelScroll('month')">
              <div :style="{ height: `${PAD}px` }" />
              <div
                v-for="(name, i) in PERSIAN_MONTHS"
                :key="`m-${i}`"
                class="wheel-item"
                :class="{ selected: i + 1 === draft.month }"
                :style="{ height: `${ITEM_HEIGHT}px` }"
              >
                {{ name }}
              </div>
              <div :style="{ height: `${PAD}px` }" />
            </div>
            <div ref="yearWheel" class="wheel-col" @scroll="onWheelScroll('year')">
              <div :style="{ height: `${PAD}px` }" />
              <div
                v-for="y in years"
                :key="`y-${y}`"
                class="wheel-item"
                :class="{ selected: y === draft.year }"
                :style="{ height: `${ITEM_HEIGHT}px` }"
              >
                {{ toPersianDigits(y) }}
              </div>
              <div :style="{ height: `${PAD}px` }" />
            </div>
          </div>
          <div class="picker-actions">
            <button v-if="!required" type="button" class="picker-btn picker-btn-clear" @click="clearDate">پاک کردن</button>
            <button type="button" class="picker-btn picker-btn-cancel" @click="cancel">انصراف</button>
            <button type="button" class="picker-btn picker-btn-ok" @click="confirm">تأیید</button>
          </div>
        </div>

        <!-- Calendar modal (desktop always; mobile when preference is modal) -->
        <div v-else class="picker-modal" role="dialog" aria-modal="true" @click="closeMenus">
          <div class="cal-header" @click.stop>
            <button type="button" class="nav-btn" aria-label="ماه قبل" @click="shiftMonth(-1)">‹</button>
            <div class="cal-selects">
              <div class="theme-select" :class="{ open: openMenu === 'month' }">
                <button type="button" class="theme-select-trigger" @click="toggleMenu('month')">
                  <span>{{ viewMonthLabel }}</span>
                  <span class="caret">▾</span>
                </button>
                <div v-if="openMenu === 'month'" class="theme-select-menu">
                  <button
                    v-for="m in monthOptions"
                    :key="m.value"
                    type="button"
                    class="menu-item"
                    :class="{ selected: m.value === view.month }"
                    @click="onViewMonthChange(m.value)"
                  >
                    {{ m.label }}
                  </button>
                </div>
              </div>
              <div class="theme-select" :class="{ open: openMenu === 'year' }">
                <button type="button" class="theme-select-trigger" @click="toggleMenu('year')">
                  <span>{{ viewYearLabel }}</span>
                  <span class="caret">▾</span>
                </button>
                <div v-if="openMenu === 'year'" ref="yearMenuList" class="theme-select-menu year-menu">
                  <button
                    v-for="y in yearOptions"
                    :key="y.value"
                    type="button"
                    class="menu-item"
                    :class="{ selected: y.value === view.year }"
                    @click="onViewYearChange(y.value)"
                  >
                    {{ y.label }}
                  </button>
                </div>
              </div>
            </div>
            <button type="button" class="nav-btn" aria-label="ماه بعد" @click="shiftMonth(1)">›</button>
          </div>

          <div class="cal-weekdays" @click="closeMenus">
            <span
              v-for="(wd, i) in PERSIAN_WEEKDAYS"
              :key="wd"
              class="weekday"
              :class="{ weekend: i === 6 }"
            >
              {{ wd }}
            </span>
          </div>

          <div class="cal-grid" @click="closeMenus">
            <button
              v-for="(cell, idx) in monthGrid"
              :key="`${cell.year}-${cell.month}-${cell.day}-${idx}`"
              type="button"
              class="cal-day"
              :class="{
                outside: !cell.current,
                today: isToday(cell),
                selected: isSelected(cell),
                weekend: ((idx % 7) === 6) && cell.current
              }"
              @click="pickDay(cell)"
            >
              {{ toPersianDigits(cell.day) }}
            </button>
          </div>

          <div class="cal-footer">
            <button type="button" class="cal-action-btn today-btn" @click="goToday">امروز</button>
            <button v-if="!required" type="button" class="cal-action-btn clear-btn" @click="clearDate">پاک کردن</button>
            <button type="button" class="cal-action-btn close-btn" @click="cancel">بستن</button>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.persian-date-picker { width: 100%; }
.picker-label {
  display: block;
  margin-bottom: 0.35rem;
  font-weight: 600;
  font-size: 0.9rem;
}
.date-field {
  position: relative;
  display: flex;
  align-items: center;
}
.date-input {
  flex: 1;
  width: 100%;
  cursor: pointer;
  background: var(--surface);
  padding-inline-end: 2.75rem;
}
.date-field.has-clear .date-input {
  padding-inline-start: 2.85rem;
}
.field-clear {
  position: absolute;
  inset-inline-start: 0.55rem;
  width: 28px;
  height: 28px;
  border: none;
  border-radius: 999px;
  background: color-mix(in srgb, var(--text-muted) 16%, transparent);
  color: var(--text-muted);
  display: inline-flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  z-index: 1;
}
.field-clear:hover {
  background: color-mix(in srgb, var(--danger) 18%, transparent);
  color: var(--danger);
}
.field-clear:focus {
  outline: none;
}
.calendar-icon {
  position: absolute;
  inset-inline-end: 0.85rem;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--text-muted);
  pointer-events: none;
}
.variant-bar .date-field {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 10px;
}
.variant-bar .date-input {
  border: none;
  min-height: 44px;
  padding: 0.5rem 0.75rem;
  padding-inline-end: 2.75rem;
}
.variant-bar .calendar-icon {
  color: var(--primary);
}

.picker-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.45);
  z-index: 1100;
  display: flex;
  justify-content: center;
  padding: 0;
}
.picker-overlay.mobile {
  align-items: flex-end;
}
.picker-overlay.desktop {
  align-items: center;
  padding: 1rem;
}

.picker-sheet {
  width: 100%;
  max-width: 420px;
  background: var(--surface);
  border-radius: 20px 20px 0 0;
  padding: 1rem 1rem calc(1rem + env(safe-area-inset-bottom, 0));
  animation: sheet-up 0.25s ease-out;
}
.picker-wheels {
  position: relative;
  display: flex;
  overflow: hidden;
  margin-bottom: 1rem;
}
.wheel-highlight {
  position: absolute;
  left: 0.75rem;
  right: 0.75rem;
  background: var(--bg);
  border-radius: 10px;
  pointer-events: none;
  z-index: 0;
}
.wheel-col {
  flex: 1;
  overflow-y: auto;
  scroll-snap-type: y mandatory;
  -webkit-overflow-scrolling: touch;
  scrollbar-width: none;
  position: relative;
  z-index: 1;
  mask-image: linear-gradient(to bottom, transparent, black 28%, black 72%, transparent);
}
.wheel-col::-webkit-scrollbar { display: none; }
.wheel-item {
  display: flex;
  align-items: center;
  justify-content: center;
  scroll-snap-align: center;
  font-size: 1rem;
  color: var(--text-muted);
  transition: color 0.15s, font-size 0.15s;
  text-align: center;
  padding: 0 0.25rem;
}
.wheel-item.selected {
  color: var(--text);
  font-weight: 700;
  font-size: 1.05rem;
}
.picker-actions {
  display: flex;
  gap: 0.75rem;
}
.picker-btn {
  flex: 1;
  min-height: 44px;
  border: none;
  border-radius: 999px;
  font-weight: 600;
  font-size: 0.95rem;
  cursor: pointer;
}
.picker-btn-cancel {
  background: var(--bg);
  color: var(--text);
}
.picker-btn-clear {
  background: color-mix(in srgb, var(--danger) 16%, transparent);
  color: var(--danger);
}
.picker-btn-ok {
  background: var(--primary);
  color: white;
}

.picker-modal {
  width: min(360px, 100%);
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 14px;
  box-shadow: 0 18px 48px rgba(0, 0, 0, 0.28);
  padding: 0.85rem 0.85rem 0.65rem;
  animation: modal-in 0.18s ease-out;
  overflow: visible;
  position: relative;
}
.cal-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  margin-bottom: 0.75rem;
}
.nav-btn {
  width: 36px;
  height: 36px;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--bg);
  color: var(--text);
  font-size: 1.35rem;
  line-height: 1;
  cursor: pointer;
}
.nav-btn:hover {
  border-color: var(--primary);
  color: var(--primary);
}
.cal-selects {
  display: flex;
  gap: 0.4rem;
  flex: 1;
  min-width: 0;
}
.theme-select {
  position: relative;
  flex: 1;
  min-width: 0;
}
.theme-select-trigger {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.35rem;
  appearance: none;
  border: 1px solid var(--border);
  border-radius: 8px;
  background: var(--bg);
  color: var(--text);
  padding: 0.45rem 0.55rem;
  font: inherit;
  cursor: pointer;
  min-height: 36px;
}
.theme-select.open .theme-select-trigger {
  outline: 2px solid var(--primary);
  border-color: transparent;
}
.theme-select-trigger .caret {
  color: var(--text-muted);
  font-size: 0.75rem;
}
.theme-select-menu {
  position: absolute;
  top: calc(100% + 4px);
  inset-inline: 0;
  z-index: 5;
  max-height: 220px;
  overflow-y: auto;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 10px;
  box-shadow: 0 10px 28px rgba(0, 0, 0, 0.22);
  padding: 0.25rem;
}
.theme-select-menu.year-menu {
  max-height: 260px;
}
.menu-item {
  display: block;
  width: 100%;
  border: none;
  background: transparent;
  color: var(--text);
  text-align: right;
  padding: 0.55rem 0.65rem;
  border-radius: 8px;
  font: inherit;
  cursor: pointer;
}
.menu-item:hover {
  background: color-mix(in srgb, var(--primary) 12%, transparent);
}
.menu-item.selected {
  background: color-mix(in srgb, var(--primary) 22%, transparent);
  font-weight: 700;
}

.cal-weekdays {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 0.15rem;
  margin-bottom: 0.35rem;
}
.weekday {
  text-align: center;
  font-size: 0.78rem;
  font-weight: 700;
  color: var(--text-muted);
  padding: 0.25rem 0;
}
.weekday.weekend {
  color: var(--danger);
}

.cal-grid {
  display: grid;
  grid-template-columns: repeat(7, 1fr);
  gap: 0.2rem;
}
.cal-day {
  aspect-ratio: 1;
  border: none;
  border-radius: 8px;
  background: transparent;
  color: var(--text);
  font: inherit;
  font-size: 0.9rem;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.12s, color 0.12s;
}
.cal-day:hover {
  background: color-mix(in srgb, var(--primary) 14%, transparent);
}
.cal-day.outside {
  color: var(--text-muted);
  opacity: 0.45;
}
.cal-day.weekend {
  color: var(--danger);
}
.cal-day.today {
  box-shadow: inset 0 0 0 1.5px var(--primary);
  font-weight: 700;
}
.cal-day.selected {
  background: var(--primary);
  color: white;
  font-weight: 700;
  box-shadow: none;
}
.cal-day.selected.weekend {
  color: white;
}

.cal-footer {
  display: flex;
  justify-content: space-between;
  gap: 0.5rem;
  margin-top: 0.85rem;
  padding-top: 0.65rem;
  border-top: 1px solid var(--border);
}
.cal-action-btn {
  flex: 1;
  border: none;
  border-radius: 999px;
  min-height: 36px;
  font: inherit;
  font-size: 0.85rem;
  font-weight: 700;
  cursor: pointer;
  padding: 0.35rem 0.5rem;
}
.today-btn {
  background: color-mix(in srgb, var(--primary) 16%, transparent);
  color: var(--primary);
}
.clear-btn {
  background: color-mix(in srgb, var(--danger) 16%, transparent);
  color: var(--danger);
}
.close-btn {
  background: var(--bg);
  color: var(--text-muted);
}

@keyframes sheet-up {
  from { transform: translateY(100%); }
  to { transform: translateY(0); }
}
@keyframes modal-in {
  from { opacity: 0; transform: translateY(8px) scale(0.98); }
  to { opacity: 1; transform: translateY(0) scale(1); }
}
</style>
