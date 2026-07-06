<script setup>
import { ref, computed, watch, nextTick } from 'vue'
import {
  PERSIAN_MONTHS,
  toJalaliParts,
  jalaliToGregorian,
  getDaysInJalaliMonth,
  formatJalali,
  todayGregorian,
  toPersianDigits
} from '../utils/jalali'

const props = defineProps({
  modelValue: { type: String, default: '' },
  label: { type: String, default: '' },
  placeholder: { type: String, default: 'انتخاب تاریخ...' },
  variant: { type: String, default: 'field' } // field | bar
})

const emit = defineEmits(['update:modelValue', 'change'])

const ITEM_HEIGHT = 40
const VISIBLE_ROWS = 5
const WHEEL_HEIGHT = ITEM_HEIGHT * VISIBLE_ROWS
const PAD = (WHEEL_HEIGHT - ITEM_HEIGHT) / 2

const visible = ref(false)
const draft = ref({ year: 1404, month: 1, day: 1 })
const dayWheel = ref(null)
const monthWheel = ref(null)
const yearWheel = ref(null)
let scrollTimer = null

const displayValue = computed(() =>
  props.modelValue ? formatJalali(props.modelValue, 'D MMMM YYYY') : ''
)

const years = computed(() => {
  const current = toJalaliParts(todayGregorian())?.year || 1404
  return Array.from({ length: 101 }, (_, i) => current - 50 + i)
})

const days = computed(() => {
  const max = getDaysInJalaliMonth(draft.value.year, draft.value.month)
  return Array.from({ length: max }, (_, i) => i + 1)
})

function clampDraft() {
  const max = getDaysInJalaliMonth(draft.value.year, draft.value.month)
  if (draft.value.day > max) draft.value.day = max
}

function syncDraftFromValue() {
  const parts = toJalaliParts(props.modelValue || todayGregorian())
  draft.value = { ...parts }
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
    scrollToIndex(yearWheel.value, yearIndex >= 0 ? yearIndex : 50)
  })
}

function open() {
  syncDraftFromValue()
  visible.value = true
  scrollWheelsToDraft()
}

function cancel() {
  visible.value = false
}

function confirm() {
  clampDraft()
  const iso = jalaliToGregorian(draft.value.year, draft.value.month, draft.value.day)
  emit('update:modelValue', iso)
  emit('change', iso)
  visible.value = false
}

function onWheelScroll(column) {
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
</script>

<template>
  <div class="persian-date-picker" :class="`variant-${variant}`">
    <label v-if="label" class="picker-label">{{ label }}</label>
    <div class="date-field" @click="open">
      <input
        readonly
        :value="displayValue"
        :placeholder="placeholder"
        class="form-control date-input"
      />
      <button type="button" class="picker-trigger" @click.stop="open">انتخاب</button>
    </div>

    <Teleport to="body">
      <div v-if="visible" class="picker-overlay" @click.self="cancel">
        <div class="picker-sheet">
          <div class="picker-wheels" :style="{ height: `${WHEEL_HEIGHT}px` }">
            <div class="wheel-highlight" :style="{ height: `${ITEM_HEIGHT}px`, top: `${PAD}px` }" />
            <div
              ref="dayWheel"
              class="wheel-col"
              @scroll="onWheelScroll('day')"
            >
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
            <div
              ref="monthWheel"
              class="wheel-col"
              @scroll="onWheelScroll('month')"
            >
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
            <div
              ref="yearWheel"
              class="wheel-col"
              @scroll="onWheelScroll('year')"
            >
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
            <button type="button" class="picker-btn picker-btn-cancel" @click="cancel">انصراف</button>
            <button type="button" class="picker-btn picker-btn-ok" @click="confirm">تأیید</button>
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
  display: flex;
  align-items: stretch;
  gap: 0.5rem;
}
.date-input {
  flex: 1;
  cursor: pointer;
  background: var(--surface);
}
.picker-trigger {
  flex-shrink: 0;
  padding: 0 1rem;
  border: 1px solid var(--border);
  border-radius: 999px;
  background: var(--bg);
  color: var(--text);
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  white-space: nowrap;
}
.variant-bar .date-field {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 10px;
  padding: 0.25rem 0.25rem 0.25rem 0.5rem;
}
.variant-bar .date-input {
  border: none;
  min-height: 40px;
  padding: 0.5rem;
}
.variant-bar .picker-trigger {
  border: none;
  background: var(--primary);
  color: white;
  min-height: 40px;
}

.picker-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.45);
  z-index: 1100;
  display: flex;
  align-items: flex-end;
  justify-content: center;
  padding: 0;
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
.picker-btn-ok {
  background: var(--primary);
  color: white;
}

@keyframes sheet-up {
  from { transform: translateY(100%); }
  to { transform: translateY(0); }
}
</style>
