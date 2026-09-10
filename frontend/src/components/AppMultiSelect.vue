<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { matchesAllTokens } from '../utils/selectSearch'
import { useOverlayBack } from '../composables/useOverlayBack'
import MessengerKindIcon from './MessengerKindIcon.vue'

const props = defineProps({
  modelValue: { type: Array, default: () => [] },
  options: { type: Array, default: () => [] },
  optionValue: { type: String, default: 'value' },
  optionLabel: { type: String, default: 'label' },
  /** Optional field on each option for section headers (e.g. messenger name). */
  optionGroup: { type: String, default: 'group' },
  /** Optional messenger kind (or other) icon key on each option. */
  optionIcon: { type: String, default: 'icon' },
  placeholder: { type: String, default: 'انتخاب کنید' },
  searchable: { type: Boolean, default: true },
  searchPlaceholder: { type: String, default: 'جستجو...' },
  allowEmpty: { type: Boolean, default: true },
  invalid: { type: Boolean, default: false },
  disabled: { type: Boolean, default: false }
})

const emit = defineEmits(['update:modelValue', 'change'])

const SHEET_MIN_VH = 42
const SHEET_MAX_VH = 92
const SHEET_DEFAULT_VH = 56

const open = ref(false)
const query = ref('')
const isMobile = ref(false)
const triggerRef = ref(null)
const searchRef = ref(null)
const panelStyle = ref({})
const sheetHeightVh = ref(SHEET_DEFAULT_VH)
const dragging = ref(false)

let dragStartY = 0
let dragStartVh = SHEET_DEFAULT_VH

const selectedValues = computed(() => (Array.isArray(props.modelValue) ? props.modelValue : []))

const normalized = computed(() =>
  props.options.map((opt) => {
    if (opt == null || typeof opt !== 'object') {
      return { value: opt, label: String(opt ?? ''), group: '', tagLabel: String(opt ?? ''), icon: null }
    }
    const label = String(opt[props.optionLabel] ?? '')
    const group = String(opt[props.optionGroup] ?? '').trim()
    const icon = opt[props.optionIcon]
    return {
      value: opt[props.optionValue],
      label,
      group,
      tagLabel: group ? `${group} · ${label}` : label,
      icon: icon === undefined || icon === '' ? null : icon
    }
  })
)

const selectedTags = computed(() =>
  selectedValues.value
    .map((value) => normalized.value.find((o) => sameValue(o.value, value)))
    .filter(Boolean)
)

const hasValue = computed(() => selectedValues.value.length > 0)

const filtered = computed(() => {
  if (!props.searchable) return normalized.value
  return normalized.value.filter((o) =>
    matchesAllTokens(o.tagLabel || o.label, query.value)
  )
})

/** Flat list with optional group header rows for rendering. */
const filteredRows = computed(() => {
  const rows = []
  let lastGroup = null
  for (const opt of filtered.value) {
    const g = opt.group || ''
    if (g && g !== lastGroup) {
      rows.push({ type: 'group', key: `g:${g}`, label: g, icon: opt.icon })
      lastGroup = g
    }
    rows.push({ type: 'option', key: `o:${String(opt.value)}`, opt })
  }
  return rows
})

const showSearch = computed(() => props.searchable)
const canClear = computed(() => props.allowEmpty && hasValue.value)
const emptyMessage = computed(() =>
  query.value.trim() ? 'موردی یافت نشد' : 'موردی برای نمایش نیست'
)

function checkMobile() {
  isMobile.value = window.matchMedia('(max-width: 768px)').matches
}

function sameValue(a, b) {
  return String(a) === String(b)
}

function isSelected(value) {
  return selectedValues.value.some((v) => sameValue(v, value))
}

function emitValues(next) {
  emit('update:modelValue', next)
  emit('change', next)
}

function positionDesktopPanel() {
  if (!triggerRef.value || isMobile.value) {
    panelStyle.value = {}
    return
  }
  const rect = triggerRef.value.getBoundingClientRect()
  const spaceBelow = window.innerHeight - rect.bottom
  const preferredHeight = Math.min(320, Math.max(180, filteredRows.value.length * 40 + (showSearch.value ? 64 : 12)))
  const openUp = spaceBelow < preferredHeight && rect.top > spaceBelow
  panelStyle.value = {
    position: 'fixed',
    zIndex: 1200,
    width: `${rect.width}px`,
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
  await nextTick()
  positionDesktopPanel()
  if (showSearch.value) searchRef.value?.focus()
}

function closeSelect() {
  open.value = false
  query.value = ''
  dragging.value = false
}

const mobileSheetOpen = computed(() => open.value && isMobile.value)
useOverlayBack(mobileSheetOpen, closeSelect, {
  enabled: () => isMobile.value,
  stateKey: 'appMultiSelect'
})

function toggle() {
  if (open.value) closeSelect()
  else openSelect()
}

function toggleValue(value) {
  const next = isSelected(value)
    ? selectedValues.value.filter((v) => !sameValue(v, value))
    : [...selectedValues.value, value]
  emitValues(next)
}

function removeTag(value, e) {
  e?.stopPropagation?.()
  if (props.disabled) return
  if (!props.allowEmpty && selectedValues.value.length <= 1) return
  emitValues(selectedValues.value.filter((v) => !sameValue(v, value)))
}

function clearSelection(e) {
  e?.stopPropagation?.()
  if (!props.allowEmpty || props.disabled) return
  emitValues([])
  if (open.value && isMobile.value) closeSelect()
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

watch(open, (v) => {
  if (isMobile.value) lockScroll(v)
})

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

watch(query, async () => {
  if (!isMobile.value && open.value) {
    await nextTick()
    positionDesktopPanel()
  }
})

onMounted(() => {
  checkMobile()
  window.addEventListener('resize', onResize)
  window.addEventListener('keydown', onKeydown)
})

onBeforeUnmount(() => {
  lockScroll(false)
  window.removeEventListener('resize', onResize)
  window.removeEventListener('keydown', onKeydown)
})
</script>

<template>
  <div class="app-multi-select" :class="{ open, invalid, disabled }">
    <div class="select-trigger-wrap" :class="{ 'has-clear': canClear }">
      <button
        ref="triggerRef"
        type="button"
        class="select-trigger form-control"
        :class="{ 'field-invalid': invalid, placeholder: !hasValue }"
        :disabled="disabled"
        :aria-expanded="open"
        aria-haspopup="listbox"
        @click="toggle"
      >
        <span v-if="hasValue" class="tag-list">
          <span
            v-for="tag in selectedTags"
            :key="String(tag.value)"
            class="tag"
            @click.stop
          >
            <span class="tag-label">
              <MessengerKindIcon v-if="tag.icon != null" :kind="tag.icon" :size="16" />
              <span>{{ tag.tagLabel || tag.label }}</span>
            </span>
            <button
              type="button"
              class="tag-remove"
              :disabled="disabled || (!allowEmpty && selectedValues.length <= 1)"
              :aria-label="`حذف ${tag.tagLabel || tag.label}`"
              @mousedown.prevent
              @click="removeTag(tag.value, $event)"
            >
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round">
                <line x1="18" y1="6" x2="6" y2="18" />
                <line x1="6" y1="6" x2="18" y2="18" />
              </svg>
            </button>
          </span>
        </span>
        <span v-else class="select-placeholder">{{ placeholder }}</span>
        <span class="select-caret" aria-hidden="true">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
            <polyline points="6 9 12 15 18 9" />
          </svg>
        </span>
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
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round">
          <line x1="18" y1="6" x2="6" y2="18" />
          <line x1="6" y1="6" x2="18" y2="18" />
        </svg>
      </button>
    </div>

    <Teleport to="body">
      <div v-if="open" class="select-layer" :class="{ mobile: isMobile }" @click.self="closeSelect">
        <div
          v-if="isMobile"
          class="select-sheet"
          :class="{ dragging }"
          :style="{ height: `${sheetHeightVh}vh` }"
          role="dialog"
          aria-modal="true"
        >
          <div
            class="sheet-handle"
            aria-label="کشیدن برای بزرگ‌نمایی"
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
            <button type="button" class="sheet-done" @click="closeSelect">تأیید</button>
          </div>
          <div v-if="showSearch" class="sheet-search">
            <input
              ref="searchRef"
              v-model="query"
              type="search"
              class="form-control"
              :placeholder="searchPlaceholder"
            />
          </div>
          <div class="option-list" role="listbox" aria-multiselectable="true">
            <template v-for="row in filteredRows" :key="row.key">
              <div v-if="row.type === 'group'" class="option-group" role="presentation">
                <MessengerKindIcon v-if="row.icon != null" :kind="row.icon" :size="16" />
                <span>{{ row.label }}</span>
              </div>
              <button
                v-else
                type="button"
                class="option-item"
                :class="{ selected: isSelected(row.opt.value) }"
                role="option"
                :aria-selected="isSelected(row.opt.value)"
                @click="toggleValue(row.opt.value)"
              >
                <span class="option-check" aria-hidden="true">
                  <svg v-if="isSelected(row.opt.value)" width="14" height="14" viewBox="0 0 16 16" fill="none">
                    <path d="M3.5 8.5L6.5 11.5L12.5 4.5" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
                  </svg>
                </span>
                <MessengerKindIcon v-if="row.opt.icon != null" :kind="row.opt.icon" :size="18" />
                <span>{{ row.opt.label }}</span>
              </button>
            </template>
            <div v-if="!filtered.length" class="option-empty">{{ emptyMessage }}</div>
          </div>
        </div>

        <div v-else class="select-panel" :style="panelStyle" role="listbox" aria-multiselectable="true">
          <div v-if="showSearch || canClear" class="panel-toolbar">
            <div v-if="showSearch" class="panel-search">
              <input
                ref="searchRef"
                v-model="query"
                type="search"
                class="form-control"
                :placeholder="searchPlaceholder"
                @click.stop
              />
            </div>
            <button v-if="canClear" type="button" class="panel-clear" @click="clearSelection">پاک کردن</button>
          </div>
          <div class="option-list">
            <template v-for="row in filteredRows" :key="row.key">
              <div v-if="row.type === 'group'" class="option-group" role="presentation">
                <MessengerKindIcon v-if="row.icon != null" :kind="row.icon" :size="16" />
                <span>{{ row.label }}</span>
              </div>
              <button
                v-else
                type="button"
                class="option-item"
                :class="{ selected: isSelected(row.opt.value) }"
                role="option"
                :aria-selected="isSelected(row.opt.value)"
                @click="toggleValue(row.opt.value)"
              >
                <span class="option-check" aria-hidden="true">
                  <svg v-if="isSelected(row.opt.value)" width="14" height="14" viewBox="0 0 16 16" fill="none">
                    <path d="M3.5 8.5L6.5 11.5L12.5 4.5" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" />
                  </svg>
                </span>
                <MessengerKindIcon v-if="row.opt.icon != null" :kind="row.opt.icon" :size="18" />
                <span>{{ row.opt.label }}</span>
              </button>
            </template>
            <div v-if="!filtered.length" class="option-empty">{{ emptyMessage }}</div>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.app-multi-select {
  width: 100%;
  position: relative;
}
.select-trigger-wrap {
  position: relative;
  display: flex;
  align-items: center;
}
.select-trigger {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  width: 100%;
  text-align: right;
  cursor: pointer;
  min-height: 42px;
  height: auto;
  padding-block: 0.4rem;
}
.has-clear .select-trigger {
  padding-inline-start: 2.85rem;
}
.select-trigger.placeholder .select-placeholder {
  color: var(--text-muted);
}
.select-trigger:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
.select-placeholder {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.tag-list {
  flex: 1;
  display: flex;
  flex-wrap: wrap;
  gap: 0.35rem;
  min-width: 0;
}
.tag {
  display: inline-flex;
  align-items: center;
  gap: 0.2rem;
  max-width: 100%;
  padding-block: 0.15rem;
  padding-inline-start: 0.5rem;
  padding-inline-end: 0.2rem;
  border-radius: 999px;
  border: 1px solid color-mix(in srgb, var(--primary) 32%, var(--border));
  background: color-mix(in srgb, var(--primary) 14%, var(--surface));
  color: var(--primary);
  font-size: 0.8rem;
  font-weight: 600;
  line-height: 1.3;
}
.tag-label {
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
  min-width: 0;
  overflow: hidden;
}
.tag-label > span {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.tag-remove {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 1.15rem;
  height: 1.15rem;
  border: none;
  border-radius: 999px;
  background: transparent;
  color: inherit;
  cursor: pointer;
  padding: 0;
}
.tag-remove:hover:not(:disabled) {
  background: color-mix(in srgb, var(--danger) 18%, transparent);
  color: var(--danger);
}
.tag-remove:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}
.select-caret {
  display: inline-flex;
  color: var(--text-muted);
  transition: transform 0.2s;
  flex-shrink: 0;
}
.app-multi-select.open .select-caret {
  transform: rotate(180deg);
  color: var(--primary);
}
.clear-btn {
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
.clear-btn:hover {
  background: color-mix(in srgb, var(--danger) 18%, transparent);
  color: var(--danger);
}

.select-layer {
  position: fixed;
  inset: 0;
  z-index: 1200;
}
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
  animation: panel-in 0.16s ease-out;
}
.panel-toolbar {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.65rem;
  border-bottom: 1px solid var(--border);
  background: var(--surface);
}
.panel-search {
  flex: 1;
  min-width: 0;
}
.panel-clear {
  border: none;
  background: transparent;
  color: var(--danger);
  font-size: 0.82rem;
  font-weight: 600;
  cursor: pointer;
  white-space: nowrap;
  padding: 0.25rem 0.35rem;
}

.select-sheet {
  width: 100%;
  max-width: 560px;
  background: var(--surface);
  border-radius: 18px 18px 0 0;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  transition: height 0.18s ease;
}
.select-sheet.dragging {
  transition: none;
}
.sheet-handle {
  display: flex;
  justify-content: center;
  padding: 0.55rem 0 0.2rem;
  touch-action: none;
  cursor: grab;
}
.handle-bar {
  width: 2.4rem;
  height: 4px;
  border-radius: 999px;
  background: color-mix(in srgb, var(--text-muted) 45%, transparent);
}
.sheet-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.35rem 1rem 0.65rem;
}
.sheet-title {
  margin: 0;
  flex: 1;
  font-size: 1rem;
  font-weight: 700;
}
.sheet-clear,
.sheet-done {
  border: none;
  background: transparent;
  font-size: 0.85rem;
  font-weight: 600;
  cursor: pointer;
  padding: 0.25rem 0.35rem;
}
.sheet-clear { color: var(--danger); }
.sheet-done { color: var(--primary); }
.sheet-search {
  padding: 0 1rem 0.65rem;
}

.option-list {
  overflow: auto;
  flex: 1;
  padding: 0.35rem;
  -webkit-overflow-scrolling: touch;
}
.option-group {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.55rem 0.75rem 0.25rem;
  font-size: 0.75rem;
  font-weight: 700;
  color: var(--text-muted);
  letter-spacing: 0.01em;
  user-select: none;
}
.option-item {
  width: 100%;
  display: flex;
  align-items: center;
  gap: 0.65rem;
  text-align: right;
  border: none;
  background: transparent;
  color: var(--text);
  padding: 0.7rem 0.75rem;
  border-radius: 10px;
  cursor: pointer;
  font-size: 0.92rem;
}
.option-item:hover {
  background: color-mix(in srgb, var(--primary) 10%, transparent);
}
.option-item.selected {
  background: color-mix(in srgb, var(--primary) 16%, transparent);
  color: var(--primary);
  font-weight: 600;
}
.option-check {
  width: 1.15rem;
  height: 1.15rem;
  flex-shrink: 0;
  border-radius: 6px;
  border: 1.5px solid var(--border);
  background: var(--surface);
  color: #fff;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}
.option-item.selected .option-check {
  background: var(--primary);
  border-color: var(--primary);
}
.option-empty {
  padding: 1rem 0.75rem;
  text-align: center;
  color: var(--text-muted);
  font-size: 0.88rem;
}

@keyframes panel-in {
  from { opacity: 0; transform: translateY(-4px); }
  to { opacity: 1; transform: translateY(0); }
}
</style>
