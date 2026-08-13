<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import api from '../api/client'
import { ApiPaths } from '../api/paths'
import PersonCell from './PersonCell.vue'
import { useOverlayBack } from '../composables/useOverlayBack'

const props = defineProps({
  modelValue: { type: [String, Number], default: '' },
  placeholder: { type: String, default: 'انتخاب شخص' },
  searchPlaceholder: { type: String, default: 'جستجو نام، لقب، پدر یا مادر...' },
  gender: { type: [Number, String], default: null }, // 1 Male, 2 Female
  excludeId: { type: [Number, String], default: null },
  allowEmpty: { type: Boolean, default: true },
  invalid: { type: Boolean, default: false },
  disabled: { type: Boolean, default: false },
  pageSize: { type: Number, default: 20 },
  minSearchLength: { type: Number, default: 1 }
})

const emit = defineEmits(['update:modelValue', 'change'])

const SHEET_MIN_VH = 42
const SHEET_MAX_VH = 92
const SHEET_DEFAULT_VH = 62

const open = ref(false)
const query = ref('')
const isMobile = ref(false)
const triggerRef = ref(null)
const searchRef = ref(null)
const listRef = ref(null)
const panelStyle = ref({})
const sheetHeightVh = ref(SHEET_DEFAULT_VH)
const dragging = ref(false)

const items = ref([])
const selected = ref(null)
const page = ref(1)
const totalCount = ref(0)
const loading = ref(false)
const loadingMore = ref(false)

let dragStartY = 0
let dragStartVh = SHEET_DEFAULT_VH
let searchTimer = null
let requestSeq = 0

const hasValue = computed(() =>
  !(props.modelValue === '' || props.modelValue === null || props.modelValue === undefined)
)

const canClear = computed(() => props.allowEmpty && hasValue.value)
const canSearch = computed(() => query.value.trim().length >= props.minSearchLength)
const hasMore = computed(() => canSearch.value && items.value.length < totalCount.value)

const selectedParentHint = computed(() => parentHintName(selected.value))

function personFullName(p) {
  if (!p) return ''
  return [p.firstName, p.lastName].filter(Boolean).join(' ')
}

function parentHintName(p) {
  if (!p) return ''
  if (p.fatherFirstName) return p.fatherFirstName
  if (p.motherFirstName) return p.motherFirstName
  // Stale payload fallback when first-name fields are missing
  const fromDisplay = (name) => {
    if (!name) return ''
    const parts = String(name).trim().split(/\s+/).filter(Boolean)
    if (parts.length === 0) return ''
    // "Prefix First Last" → second token; otherwise first token
    return parts.length >= 3 ? parts[1] : parts[0]
  }
  if (p.fatherName) return fromDisplay(p.fatherName)
  if (p.motherName) return fromDisplay(p.motherName)
  return ''
}

function parentsLine(p) {
  const hint = parentHintName(p)
  if (!hint) return ''
  if (p.fatherFirstName || p.fatherName) return 'پدر: ' + hint
  return 'مادر: ' + hint
}

function checkMobile() {
  isMobile.value = window.matchMedia('(max-width: 768px)').matches
}

function sameId(a, b) {
  if (a === '' || a === null || a === undefined) return b === '' || b === null || b === undefined
  return String(a) === String(b)
}

function buildParams(pageNum) {
  const params = {
    page: pageNum,
    pageSize: props.pageSize
  }
  const q = query.value.trim()
  if (q) params.search = q
  if (props.gender !== null && props.gender !== undefined && props.gender !== '') {
    params.gender = props.gender
  }
  return params
}

function filterExcluded(list) {
  if (props.excludeId === null || props.excludeId === undefined || props.excludeId === '') return list
  return list.filter((p) => !sameId(p.id, props.excludeId))
}

function resetResults() {
  items.value = []
  totalCount.value = 0
  page.value = 1
  loading.value = false
  loadingMore.value = false
}

async function fetchPage(pageNum, { append = false } = {}) {
  if (!canSearch.value) {
    requestSeq += 1
    resetResults()
    return
  }
  const seq = ++requestSeq
  if (append) loadingMore.value = true
  else loading.value = true
  try {
    const { data } = await api.get(ApiPaths.lookups.persons, { params: buildParams(pageNum) })
    if (seq !== requestSeq) return
    const next = filterExcluded(data.items || [])
    items.value = append ? [...items.value, ...next] : next
    totalCount.value = data.totalCount || 0
    page.value = pageNum
  } finally {
    if (seq === requestSeq) {
      loading.value = false
      loadingMore.value = false
    }
  }
}

function hasParentHintFields(p) {
  return !!(p && (p.fatherFirstName || p.motherFirstName || p.fatherName || p.motherName))
}

async function ensureSelected() {
  if (!hasValue.value) {
    selected.value = null
    return
  }
  // Keep current selection only when it already has parent disambiguation fields
  if (selected.value && sameId(selected.value.id, props.modelValue) && hasParentHintFields(selected.value)) {
    return
  }
  const cached = items.value.find((p) => sameId(p.id, props.modelValue))
  if (cached && hasParentHintFields(cached)) {
    selected.value = cached
    return
  }
  try {
    const { data } = await api.get(ApiPaths.person(props.modelValue), { skipErrorToast: true })
    selected.value = data
  } catch {
    if (cached) {
      selected.value = cached
      return
    }
    if (selected.value && sameId(selected.value.id, props.modelValue)) return
    selected.value = { id: props.modelValue, firstName: `#${props.modelValue}`, lastName: '' }
  }
}

function positionDesktopPanel() {
  if (!triggerRef.value || isMobile.value) {
    panelStyle.value = {}
    return
  }
  const rect = triggerRef.value.getBoundingClientRect()
  const spaceBelow = window.innerHeight - rect.bottom
  const preferredHeight = 360
  const openUp = spaceBelow < preferredHeight && rect.top > spaceBelow
  panelStyle.value = {
    position: 'fixed',
    zIndex: 1200,
    width: `${Math.max(rect.width, 320)}px`,
    left: `${rect.left}px`,
    maxHeight: `${Math.min(preferredHeight, openUp ? rect.top - 12 : spaceBelow - 12)}px`,
    ...(openUp
      ? { bottom: `${window.innerHeight - rect.top + 6}px` }
      : { top: `${rect.bottom + 6}px` })
  }
}

async function openSelect() {
  if (props.disabled) return
  open.value = true
  query.value = ''
  sheetHeightVh.value = SHEET_DEFAULT_VH
  resetResults()
  await ensureSelected()
  await nextTick()
  positionDesktopPanel()
  searchRef.value?.focus()
}

function closeSelect() {
  open.value = false
  query.value = ''
  dragging.value = false
}

const mobileSheetOpen = computed(() => open.value && isMobile.value)
useOverlayBack(mobileSheetOpen, closeSelect, {
  enabled: () => isMobile.value,
  stateKey: 'appPersonSelect'
})

function toggle() {
  if (open.value) closeSelect()
  else openSelect()
}

function selectPerson(person) {
  selected.value = person
  emit('update:modelValue', person.id)
  emit('change', person.id)
  closeSelect()
}

function clearSelection(e) {
  e?.stopPropagation?.()
  if (!props.allowEmpty || props.disabled) return
  selected.value = null
  emit('update:modelValue', '')
  emit('change', '')
  if (open.value && isMobile.value) closeSelect()
}

function onSearchInput() {
  clearTimeout(searchTimer)
  searchTimer = setTimeout(() => {
    if (!canSearch.value) {
      requestSeq += 1
      resetResults()
      return
    }
    fetchPage(1)
  }, 300)
}

async function onListScroll(e) {
  const el = e.target
  if (!hasMore.value || loadingMore.value || loading.value) return
  if (el.scrollTop + el.clientHeight >= el.scrollHeight - 48) {
    await fetchPage(page.value + 1, { append: true })
  }
}

function clampVh(vh) {
  return Math.min(SHEET_MAX_VH, Math.max(SHEET_MIN_VH, vh))
}

function onHandlePointerDown(e) {
  if (!isMobile.value) return
  dragging.value = true
  dragStartY = e.clientY
  dragStartVh = sheetHeightVh.value
  e.currentTarget.setPointerCapture?.(e.pointerId)
}

function onHandlePointerMove(e) {
  if (!dragging.value) return
  const deltaY = dragStartY - e.clientY
  const deltaVh = (deltaY / window.innerHeight) * 100
  sheetHeightVh.value = clampVh(dragStartVh + deltaVh)
}

function onHandlePointerUp(e) {
  if (!dragging.value) return
  dragging.value = false
  e.currentTarget.releasePointerCapture?.(e.pointerId)
  const mid = (SHEET_MIN_VH + SHEET_MAX_VH) / 2
  if (sheetHeightVh.value > mid + 8) sheetHeightVh.value = SHEET_MAX_VH
  else if (sheetHeightVh.value < mid - 8) sheetHeightVh.value = SHEET_DEFAULT_VH
}

function lockScroll(lock) {
  document.body.style.overflow = lock ? 'hidden' : ''
}

function onKeydown(e) {
  if (e.key === 'Escape' && open.value) {
    e.preventDefault()
    closeSelect()
  }
}

function onResize() {
  checkMobile()
  if (open.value) positionDesktopPanel()
}

watch(() => props.modelValue, () => {
  ensureSelected()
})

watch(open, (v) => {
  if (isMobile.value) lockScroll(v)
})

onMounted(() => {
  checkMobile()
  ensureSelected()
  window.addEventListener('resize', onResize)
  window.addEventListener('keydown', onKeydown)
})

onBeforeUnmount(() => {
  clearTimeout(searchTimer)
  lockScroll(false)
  window.removeEventListener('resize', onResize)
  window.removeEventListener('keydown', onKeydown)
})
</script>

<template>
  <div class="person-select" :class="{ open, invalid, disabled }">
    <div class="select-trigger-wrap" :class="{ 'has-clear': canClear }">
      <button
        ref="triggerRef"
        type="button"
        class="select-trigger form-control"
        :class="{ 'field-invalid': invalid, placeholder: !selected, 'has-avatar': !!selected }"
        :disabled="disabled"
        :aria-expanded="open"
        @click="toggle"
      >
        <div v-if="selected" class="select-selected-block">
          <div class="trigger-person-slot">
            <PersonCell :person="selected" :size="28" :previewable="false" />
          </div>
          <span v-if="selectedParentHint" class="select-parent">{{ selectedParentHint }}</span>
        </div>
        <span v-else class="select-value placeholder">{{ placeholder }}</span>
        <span class="select-caret" aria-hidden="true">▾</span>
      </button>
      <button
        v-if="canClear"
        type="button"
        class="clear-btn"
        tabindex="-1"
        aria-hidden="true"
        title="پاک کردن"
        @mousedown.prevent
        @click="clearSelection"
      >
        ×
      </button>
    </div>

    <Teleport to="body">
      <div v-if="open" class="select-layer" :class="{ mobile: isMobile }" @click.self="closeSelect">
        <div
          v-if="isMobile"
          class="select-sheet"
          :class="{ dragging }"
          :style="{ height: `${sheetHeightVh}vh` }"
        >
          <div
            class="sheet-handle"
            @pointerdown="onHandlePointerDown"
            @pointermove="onHandlePointerMove"
            @pointerup="onHandlePointerUp"
            @pointercancel="onHandlePointerUp"
          >
            <span class="handle-bar" />
          </div>
          <div class="sheet-header">
            <h3 class="sheet-title">{{ placeholder }}</h3>
            <button v-if="canClear" type="button" class="sheet-clear" @click="clearSelection">پاک کردن</button>
          </div>
          <div class="sheet-search">
            <input
              ref="searchRef"
              v-model="query"
              type="search"
              class="form-control"
              :placeholder="searchPlaceholder"
              @input="onSearchInput"
            />
          </div>
          <div ref="listRef" class="option-list" @scroll="onListScroll">
            <button
              v-for="p in items"
              :key="p.id"
              type="button"
              class="person-option"
              :class="{ selected: sameId(modelValue, p.id) }"
              @click="selectPerson(p)"
            >
              <div class="person-info">
                <div class="person-option-cell">
                  <PersonCell :person="p" :size="40" :previewable="false" />
                </div>
                <div v-if="parentsLine(p)" class="person-parents">{{ parentsLine(p) }}</div>
              </div>
            </button>
            <div v-if="!canSearch" class="option-status">برای جستجو تایپ کنید</div>
            <div v-else-if="loading" class="option-status">در حال بارگذاری...</div>
            <div v-else-if="!items.length" class="option-status">موردی یافت نشد</div>
            <div v-else-if="loadingMore" class="option-status">موارد بیشتر...</div>
          </div>
        </div>

        <div v-else class="select-panel" :style="panelStyle">
          <div class="panel-toolbar">
            <input
              ref="searchRef"
              v-model="query"
              type="search"
              class="form-control"
              :placeholder="searchPlaceholder"
              @click.stop
              @input="onSearchInput"
            />
            <button v-if="canClear" type="button" class="panel-clear" @click="clearSelection">پاک کردن</button>
          </div>
          <div ref="listRef" class="option-list" @scroll="onListScroll">
            <button
              v-for="p in items"
              :key="p.id"
              type="button"
              class="person-option"
              :class="{ selected: sameId(modelValue, p.id) }"
              @click="selectPerson(p)"
            >
              <div class="person-info">
                <div class="person-option-cell">
                  <PersonCell :person="p" :size="40" :previewable="false" />
                </div>
                <div v-if="parentsLine(p)" class="person-parents">{{ parentsLine(p) }}</div>
              </div>
            </button>
            <div v-if="!canSearch" class="option-status">برای جستجو تایپ کنید</div>
            <div v-else-if="loading" class="option-status">در حال بارگذاری...</div>
            <div v-else-if="!items.length" class="option-status">موردی یافت نشد</div>
            <div v-else-if="loadingMore" class="option-status">موارد بیشتر...</div>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.person-select { width: 100%; position: relative; }
.select-trigger-wrap { position: relative; display: flex; align-items: center; }
.select-trigger {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  width: 100%;
  text-align: right;
  cursor: pointer;
  min-height: 48px;
  padding-block: 0.35rem;
}
.select-trigger.has-avatar { gap: 0.55rem; }
.select-selected-block {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.1rem;
}
.trigger-person-slot {
  flex-shrink: 0;
  display: inline-flex;
  max-width: 100%;
}
.person-option-cell {
  max-width: 100%;
}
.select-name.deceased,
.person-name .first.deceased {
  color: color-mix(in srgb, var(--text-muted) 50%, var(--text));
}
.has-clear .select-trigger { padding-inline-start: 2.85rem; }
.select-trigger.placeholder .select-value,
.select-value.placeholder { color: var(--text-muted); }
.select-trigger:disabled { opacity: 0.6; cursor: not-allowed; }
.select-value {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.1rem;
  overflow: hidden;
}
.select-value.placeholder {
  display: block;
  white-space: nowrap;
  text-overflow: ellipsis;
}
.select-main {
  display: flex;
  align-items: baseline;
  gap: 0.4rem;
  min-width: 0;
  max-width: 100%;
}
.select-name {
  font-weight: 600;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.select-parent {
  font-size: 0.72rem;
  line-height: 1.2;
  color: var(--text-muted);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 100%;
}
.select-caret { color: var(--text-muted); }
.person-select.open .select-caret { color: var(--primary); }
.clear-btn {
  position: absolute;
  inset-inline-start: 0.55rem;
  width: 28px;
  height: 28px;
  border: none;
  border-radius: 999px;
  background: color-mix(in srgb, var(--text-muted) 16%, transparent);
  color: var(--text-muted);
  cursor: pointer;
  z-index: 1;
  font-size: 1rem;
  line-height: 1;
}
.clear-btn:focus {
  outline: none;
}

.select-layer { position: fixed; inset: 0; z-index: 1200; }
.select-layer.mobile {
  background: rgba(0, 0, 0, 0.45);
  display: flex;
  align-items: flex-end;
  justify-content: center;
}

.select-panel {
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 12px;
  box-shadow: 0 12px 32px rgba(0, 0, 0, 0.18);
  overflow: hidden;
  display: flex;
  flex-direction: column;
}
.panel-toolbar {
  display: flex;
  gap: 0.5rem;
  padding: 0.65rem;
  border-bottom: 1px solid var(--border);
}
.panel-toolbar .form-control { flex: 1; min-height: 40px; }
.panel-clear {
  border: none;
  background: color-mix(in srgb, var(--danger) 14%, transparent);
  color: var(--danger);
  border-radius: 999px;
  padding: 0.4rem 0.75rem;
  font-size: 0.8rem;
  font-weight: 600;
  cursor: pointer;
  white-space: nowrap;
}

.select-sheet {
  width: 100%;
  max-width: 560px;
  background: var(--surface);
  border-radius: 20px 20px 0 0;
  padding: 0 0 calc(0.75rem + env(safe-area-inset-bottom, 0));
  display: flex;
  flex-direction: column;
  transition: height 0.18s ease;
}
.select-sheet.dragging { transition: none; }
.sheet-handle {
  display: flex;
  justify-content: center;
  padding: 0.7rem 0 0.35rem;
  cursor: grab;
  touch-action: none;
}
.handle-bar {
  width: 44px;
  height: 5px;
  border-radius: 999px;
  background: var(--border);
}
.sheet-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 0.15rem 1rem 0.65rem;
}
.sheet-title { font-size: 1rem; font-weight: 700; margin: 0; }
.sheet-clear {
  border: none;
  background: color-mix(in srgb, var(--danger) 14%, transparent);
  color: var(--danger);
  border-radius: 999px;
  padding: 0.3rem 0.75rem;
  font-size: 0.8rem;
  font-weight: 600;
  cursor: pointer;
}
.sheet-search { padding: 0 1rem 0.75rem; }
.sheet-search .form-control {
  min-height: 44px;
  border-radius: 12px;
  background: var(--bg);
}

.option-list {
  overflow-y: auto;
  -webkit-overflow-scrolling: touch;
  flex: 1;
  min-height: 0;
}
.person-option {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  width: 100%;
  border: none;
  background: transparent;
  color: var(--text);
  text-align: right;
  padding: 0.7rem 1rem;
  font: inherit;
  cursor: pointer;
  border-bottom: 1px solid color-mix(in srgb, var(--border) 70%, transparent);
  -webkit-tap-highlight-color: transparent;
  transition: background-color 0.12s ease;
}
@media (hover: hover) and (pointer: fine) {
  .person-option:hover,
  .person-option:focus-visible {
    background: color-mix(in srgb, var(--primary) 10%, transparent);
    outline: none;
  }
}
.person-option:focus-visible {
  outline: 2px solid color-mix(in srgb, var(--primary) 45%, transparent);
  outline-offset: -2px;
}
.person-option:active {
  background: color-mix(in srgb, var(--primary) 12%, transparent);
}
.person-option.selected {
  background: color-mix(in srgb, var(--primary) 14%, transparent);
  box-shadow: inset 3px 0 0 var(--primary);
}
[dir="rtl"] .person-option.selected {
  box-shadow: inset -3px 0 0 var(--primary);
}
.person-avatar-slot {
  flex-shrink: 0;
  display: inline-flex;
}
.person-info { min-width: 0; flex: 1; }
.person-name {
  display: flex;
  align-items: baseline;
  gap: 0.35rem;
  flex-wrap: wrap;
}
.person-name .first { font-weight: 700; }
.person-name .last {
  font-size: 0.82rem;
  color: var(--text-muted);
  font-weight: 500;
}
.person-parents {
  margin-top: 0.15rem;
  font-size: 0.75rem;
  color: var(--text-muted);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.option-status {
  padding: 1rem;
  text-align: center;
  color: var(--text-muted);
  font-size: 0.85rem;
}
</style>
