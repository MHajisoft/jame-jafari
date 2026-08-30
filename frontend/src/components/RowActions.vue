<script>
let closeActiveMenu = null
</script>

<script setup>
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'
import AuditInfoPanel from './AuditInfoPanel.vue'

const props = defineProps({
  showEdit: { type: Boolean, default: false },
  showDelete: { type: Boolean, default: false },
  showChangePassword: { type: Boolean, default: false },
  showAudit: { type: Boolean, default: false },
  audit: { type: Object, default: null },
  extras: { type: Array, default: () => [] },
  editLabel: { type: String, default: 'ویرایش' },
  deleteLabel: { type: String, default: 'حذف' },
  changePasswordLabel: { type: String, default: 'تغییر رمز' },
  auditLabel: { type: String, default: 'اطلاعات ثبت' }
})

const emit = defineEmits(['edit', 'delete', 'change-password', 'extra'])

const open = ref(false)
const auditOpen = ref(false)
const triggerRef = ref(null)
const menuRef = ref(null)
const menuStyle = ref({})

const safeExtras = computed(() => (Array.isArray(props.extras) ? props.extras : []))
const extrasMain = computed(() => safeExtras.value.filter((x) => !x.danger))
const extrasDanger = computed(() => safeExtras.value.filter((x) => x.danger))

const hasAudit = computed(() => props.showAudit && !!props.audit)
const hasActions = computed(() =>
  props.showEdit
  || props.showChangePassword
  || hasAudit.value
  || props.showDelete
  || safeExtras.value.length > 0
)

function close() {
  open.value = false
  if (closeActiveMenu === close) closeActiveMenu = null
}

async function toggle(event) {
  event.stopPropagation()
  if (open.value) {
    close()
    return
  }
  closeActiveMenu?.()
  closeActiveMenu = close
  open.value = true
  await nextTick()
  positionMenu()
}

function positionMenu() {
  const btn = triggerRef.value
  const menu = menuRef.value
  if (!btn || !menu) return
  const rect = btn.getBoundingClientRect()
  const mw = menu.offsetWidth
  const mh = menu.offsetHeight
  let top = rect.bottom + 6
  if (top + mh > window.innerHeight - 12) top = Math.max(12, rect.top - mh - 6)
  let left = rect.right - mw
  left = Math.min(Math.max(12, left), window.innerWidth - mw - 12)
  menuStyle.value = { top: `${top}px`, left: `${left}px` }
}

function onDocumentClick(event) {
  if (!open.value) return
  if (triggerRef.value?.contains(event.target) || menuRef.value?.contains(event.target)) return
  close()
}

function onKeydown(event) {
  if (event.key === 'Escape') close()
}

function run(fn) {
  close()
  fn()
}

function onExtra(item) {
  if (item.disabled) return
  run(() => emit('extra', item.id))
}

watch(open, (isOpen) => {
  if (isOpen) {
    document.addEventListener('click', onDocumentClick)
    document.addEventListener('keydown', onKeydown)
    document.addEventListener('scroll', close, true)
    window.addEventListener('resize', close)
  } else {
    document.removeEventListener('click', onDocumentClick)
    document.removeEventListener('keydown', onKeydown)
    document.removeEventListener('scroll', close, true)
    window.removeEventListener('resize', close)
  }
})

onBeforeUnmount(() => {
  close()
  document.removeEventListener('click', onDocumentClick)
  document.removeEventListener('keydown', onKeydown)
  document.removeEventListener('scroll', close, true)
  window.removeEventListener('resize', close)
})
</script>

<template>
  <div v-if="hasActions" class="row-actions">
    <button
      ref="triggerRef"
      type="button"
      class="icon-btn"
      aria-haspopup="menu"
      :aria-expanded="open"
      aria-label="عملیات"
      title="عملیات"
      @click="toggle"
    >
      <svg viewBox="0 0 24 24" aria-hidden="true">
        <circle cx="12" cy="5" r="1.85" fill="currentColor" />
        <circle cx="12" cy="12" r="1.85" fill="currentColor" />
        <circle cx="12" cy="19" r="1.85" fill="currentColor" />
      </svg>
    </button>

    <Teleport to="body">
      <div
        v-if="open"
        ref="menuRef"
        class="row-actions-menu"
        role="menu"
        :style="menuStyle"
        @click.stop
      >
        <button
          v-if="showEdit"
          type="button"
          role="menuitem"
          class="row-actions-item"
          @click="run(() => emit('edit'))"
        >
          {{ editLabel }}
        </button>
        <button
          v-if="showChangePassword"
          type="button"
          role="menuitem"
          class="row-actions-item"
          @click="run(() => emit('change-password'))"
        >
          {{ changePasswordLabel }}
        </button>
        <button
          v-for="item in extrasMain"
          :key="item.id"
          type="button"
          role="menuitem"
          class="row-actions-item"
          :disabled="item.disabled"
          :title="item.title || undefined"
          @click="onExtra(item)"
        >
          {{ item.label }}
        </button>
        <button
          v-if="hasAudit"
          type="button"
          role="menuitem"
          class="row-actions-item"
          @click="run(() => { auditOpen = true })"
        >
          {{ auditLabel }}
        </button>
        <button
          v-for="item in extrasDanger"
          :key="item.id"
          type="button"
          role="menuitem"
          class="row-actions-item is-danger"
          :disabled="item.disabled"
          :title="item.title || undefined"
          @click="onExtra(item)"
        >
          {{ item.label }}
        </button>
        <button
          v-if="showDelete"
          type="button"
          role="menuitem"
          class="row-actions-item is-danger"
          @click="run(() => emit('delete'))"
        >
          {{ deleteLabel }}
        </button>
      </div>
    </Teleport>

    <AuditInfoPanel v-model:show="auditOpen" :audit="audit" />
  </div>
</template>

<style scoped>
.row-actions {
  display: inline-flex;
  align-items: center;
  justify-content: flex-end;
}

.icon-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2.15rem;
  height: 2.15rem;
  padding: 0;
  border: 1px solid transparent;
  border-radius: 10px;
  background: transparent;
  color: var(--text-muted);
  cursor: pointer;
  -webkit-tap-highlight-color: transparent;
  transition:
    background-color 0.15s ease-out,
    color 0.15s ease-out,
    border-color 0.15s ease-out,
    transform 0.12s ease-out;
}
.icon-btn svg {
  width: 1.05rem;
  height: 1.05rem;
  display: block;
}

@media (hover: hover) and (pointer: fine) {
  .icon-btn:hover {
    background: color-mix(in srgb, var(--primary) 12%, transparent);
    color: var(--primary);
  }
}

.icon-btn:focus-visible {
  outline: 2px solid var(--primary);
  outline-offset: 2px;
}
.icon-btn:active {
  transform: scale(0.96);
}

@media (max-width: 768px), (hover: none), (pointer: coarse) {
  .icon-btn {
    width: 2.625rem;
    height: 2.625rem;
    border-radius: 12px;
    background: var(--surface);
    border-color: var(--border);
    color: var(--text-muted);
    box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
  }
  .icon-btn svg {
    width: 1.125rem;
    height: 1.125rem;
  }
  .icon-btn:active {
    background: color-mix(in srgb, var(--primary) 10%, var(--surface));
    border-color: color-mix(in srgb, var(--primary) 28%, var(--border));
    color: var(--primary);
    box-shadow: none;
  }
}
</style>

<style>
.row-actions-menu {
  position: fixed;
  z-index: 3100;
  min-width: 11.5rem;
  padding: 0.35rem;
  border-radius: 12px;
  border: 1px solid var(--border);
  background: var(--surface);
  box-shadow: 0 8px 28px rgba(0, 0, 0, 0.14);
}

.row-actions-item {
  display: block;
  width: 100%;
  padding: 0.55rem 0.75rem;
  border: none;
  border-radius: 8px;
  background: transparent;
  color: var(--text);
  font: inherit;
  font-size: 0.9rem;
  font-weight: 600;
  text-align: start;
  cursor: pointer;
}

.row-actions-item:hover:not(:disabled),
.row-actions-item:focus-visible {
  background: color-mix(in srgb, var(--primary) 10%, transparent);
  color: var(--primary);
  outline: none;
}

.row-actions-item.is-danger {
  color: var(--danger);
}

.row-actions-item.is-danger:hover:not(:disabled),
.row-actions-item.is-danger:focus-visible {
  background: color-mix(in srgb, var(--danger) 10%, transparent);
  color: var(--danger);
}

.row-actions-item:disabled {
  opacity: 0.45;
  cursor: not-allowed;
}
</style>
