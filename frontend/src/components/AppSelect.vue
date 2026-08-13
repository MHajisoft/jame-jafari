<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { matchesAllTokens } from '../utils/selectSearch'

const props = defineProps({
  modelValue: { type: [String, Number, Boolean], default: '' },
  options: { type: Array, default: () => [] },
  optionValue: { type: String, default: 'value' },
  optionLabel: { type: String, default: 'label' },
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

const normalized = computed(() =>
  props.options.map((opt) => {
    if (opt == null || typeof opt !== 'object') {
      return { value: opt, label: String(opt ?? '') }
    }
    return {
      value: opt[props.optionValue],
      label: String(opt[props.optionLabel] ?? '')
    }
  })
)

const hasValue = computed(() =>
  !(props.modelValue === '' || props.modelValue === null || props.modelValue === undefined)
)

const selectedLabel = computed(() => {
  if (!hasValue.value) return ''
  const match = normalized.value.find((o) => String(o.value) === String(props.modelValue))
  return match?.label || ''
})

const filtered = computed(() => {
  if (!props.searchable) return normalized.value
  return normalized.value.filter((o) => matchesAllTokens(o.label, query.value))
})

/** Always show search when enabled (same approach as PersonSelect). */
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

function positionDesktopPanel() {
  if (!triggerRef.value || isMobile.value) {
    panelStyle.value = {}
    return
  }
  const rect = triggerRef.value.getBoundingClientRect()
  const spaceBelow = window.innerHeight - rect.bottom
  const preferredHeight = Math.min(320, Math.max(180, filtered.value.length * 44 + (showSearch.value ? 64 : 12)))
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

function toggle() {
  if (open.value) closeSelect()
  else openSelect()
}

function selectValue(value) {
  emit('update:modelValue', value)
  emit('change', value)
  closeSelect()
}

function clearSelection(e) {
  e?.stopPropagation?.()
  if (!props.allowEmpty || props.disabled) return
  emit('update:modelValue', '')
  emit('change', '')
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

  // Snap to nearest comfortable height
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
  <div class="app-select" :class="{ open, invalid, disabled }">
    <div class="select-trigger-wrap" :class="{ 'has-clear': canClear }">
      <button
        ref="triggerRef"
        type="button"
        class="select-trigger form-control"
        :class="{ 'field-invalid': invalid, placeholder: !selectedLabel }"
        :disabled="disabled"
        :aria-expanded="open"
        aria-haspopup="listbox"
        @click="toggle"
      >
        <span class="select-value">{{ selectedLabel || placeholder }}</span>
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
            <button
              v-if="canClear"
              type="button"
              class="sheet-clear"
              @click="clearSelection"
            >
              پاک کردن
            </button>
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
          <div class="option-list" role="listbox">
            <button
              v-for="opt in filtered"
              :key="String(opt.value)"
              type="button"
              class="option-item"
              :class="{ selected: sameValue(modelValue, opt.value) }"
              role="option"
              @click="selectValue(opt.value)"
            >
              {{ opt.label }}
            </button>
            <div v-if="!filtered.length" class="option-empty">{{ emptyMessage }}</div>
          </div>
        </div>

        <div
          v-else
          class="select-panel"
          :style="panelStyle"
          role="listbox"
        >
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
            <button
              v-if="canClear"
              type="button"
              class="panel-clear"
              @click="clearSelection"
            >
              پاک کردن
            </button>
          </div>
          <div class="option-list">
            <button
              v-for="opt in filtered"
              :key="String(opt.value)"
              type="button"
              class="option-item"
              :class="{ selected: sameValue(modelValue, opt.value) }"
              role="option"
              @click="selectValue(opt.value)"
            >
              {{ opt.label }}
            </button>
            <div v-if="!filtered.length" class="option-empty">{{ emptyMessage }}</div>
          </div>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.app-select {
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
}
.has-clear .select-trigger {
  padding-inline-start: 2.85rem;
}
.select-trigger.placeholder .select-value {
  color: var(--text-muted);
}
.select-trigger:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}
.select-value {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.select-caret {
  display: inline-flex;
  color: var(--text-muted);
  transition: transform 0.2s;
  flex-shrink: 0;
}
.app-select.open .select-caret {
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
.clear-btn:focus {
  outline: none;
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
.panel-search .form-control {
  min-height: 40px;
}
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
  height: 56vh;
  max-height: 92vh;
  background: var(--surface);
  border-radius: 20px 20px 0 0;
  padding: 0 0 calc(0.75rem + env(safe-area-inset-bottom, 0));
  display: flex;
  flex-direction: column;
  animation: sheet-up 0.25s ease-out;
  transition: height 0.18s ease;
}
.select-sheet.dragging {
  transition: none;
}
.sheet-handle {
  display: flex;
  justify-content: center;
  align-items: center;
  width: 100%;
  border: none;
  background: transparent;
  padding: 0.7rem 0 0.35rem;
  cursor: grab;
  touch-action: none;
  user-select: none;
}
.sheet-handle:active {
  cursor: grabbing;
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
.sheet-title {
  font-size: 1rem;
  font-weight: 700;
  color: var(--text);
}
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
.sheet-search {
  padding: 0 1rem 0.75rem;
}
.sheet-search .form-control {
  min-height: 44px;
  border-radius: 12px;
  background: var(--bg);
}

.option-list {
  overflow-y: auto;
  -webkit-overflow-scrolling: touch;
  overscroll-behavior: contain;
  flex: 1;
  min-height: 0;
  touch-action: pan-y;
}
.option-item {
  display: block;
  width: 100%;
  border: none;
  background: transparent;
  color: var(--text);
  text-align: right;
  padding: 0.85rem 1rem;
  font: inherit;
  cursor: pointer;
  transition: background 0.15s;
  -webkit-tap-highlight-color: transparent;
}
@media (hover: hover) and (pointer: fine) {
  .option-item:hover,
  .option-item:focus-visible {
    background: color-mix(in srgb, var(--primary) 12%, transparent);
    outline: none;
  }
}
.option-item:focus-visible {
  outline: 2px solid color-mix(in srgb, var(--primary) 45%, transparent);
  outline-offset: -2px;
}
.option-item:active {
  background: color-mix(in srgb, var(--primary) 12%, transparent);
}
.option-item.selected {
  background: color-mix(in srgb, var(--primary) 14%, transparent);
  color: var(--text);
  font-weight: 700;
  box-shadow: inset 3px 0 0 var(--primary);
}
[dir="rtl"] .option-item.selected {
  box-shadow: inset -3px 0 0 var(--primary);
}
.select-sheet .option-item {
  padding: 0.95rem 1.1rem;
  border-bottom: 1px solid color-mix(in srgb, var(--border) 70%, transparent);
}
.select-sheet .option-item:last-child {
  border-bottom: none;
}
.option-empty {
  padding: 1.25rem 1rem;
  text-align: center;
  color: var(--text-muted);
  font-size: 0.9rem;
}

@keyframes sheet-up {
  from { transform: translateY(100%); }
  to { transform: translateY(0); }
}
@keyframes panel-in {
  from { opacity: 0; transform: translateY(-4px); }
  to { opacity: 1; transform: translateY(0); }
}
</style>
