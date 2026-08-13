import { computed, ref } from 'vue'
import { navItems } from '../config/navigation'

export const MODULE_LABELS = {
  accounts: 'حساب‌ها',
  income: 'درآمد',
  cost: 'هزینه',
  users: 'کاربران',
  persons: 'اشخاص',
  costtypes: 'انواع هزینه',
  food: 'تهیه غذا',
  reports: 'گزارشات',
  generaltypes: 'انواع عمومی',
  attachments: 'پیوست‌ها'
}

const ACTION_LABELS = {
  view: 'مشاهده',
  create: 'ایجاد',
  update: 'ویرایش',
  delete: 'حذف',
  add: 'افزودن',
  changepassword: 'تغییر رمز'
}

export const CORE_COLUMNS = [
  { key: 'view', label: 'مشاهده' },
  { key: 'create', label: 'ایجاد' },
  { key: 'update', label: 'ویرایش' },
  { key: 'delete', label: 'حذف' }
]

/** Modules with non-standard action sets (no nav menu). */
const MODULE_COLUMNS = {
  attachments: [
    { key: 'view', label: 'مشاهده' },
    { key: 'add', label: 'افزودن' },
    { key: 'delete', label: 'حذف' }
  ]
}

const MODULE_ORDER = [
  'accounts', 'cost', 'costtypes', 'generaltypes',
  'income', 'persons', 'food', 'attachments', 'reports', 'users'
]

const moduleIcons = Object.fromEntries(
  navItems
    .filter(n => n.permission || n.permissionsAny?.length)
    .map(n => {
      const code = n.permission || n.permissionsAny[0]
      return [code.split('.')[0], n.icon]
    })
)
moduleIcons.attachments = '📎'

export function permActionLabel(code) {
  return ACTION_LABELS[code.split('.')[1]] || code.split('.')[1]
}

export function permissionTitle(code) {
  const [mod] = String(code).split('.')
  const moduleName = MODULE_LABELS[mod] || mod
  return `${moduleName} · ${permActionLabel(code)}`
}

export function buildModules(permissions) {
  const grouped = {}
  for (const p of permissions) {
    (grouped[p.module] ??= []).push(p)
  }

  const orderedKeys = MODULE_ORDER
    .filter(m => grouped[m])
    .concat(Object.keys(grouped).filter(m => !MODULE_ORDER.includes(m)))

  return orderedKeys.map(moduleKey => {
    const perms = grouped[moduleKey]
    const columns = MODULE_COLUMNS[moduleKey] || CORE_COLUMNS
    const columnKeys = columns.map(c => c.key)
    const slots = Object.fromEntries(columnKeys.map(a => [a, null]))
    const extra = []

    for (const p of perms) {
      const action = p.code.split('.')[1]
      if (columnKeys.includes(action)) {
        slots[action] = { id: p.id, code: p.code }
      } else {
        extra.push({ key: action, label: permActionLabel(p.code), id: p.id, code: p.code })
      }
    }

    const rows = [
      ...columns.map(col => ({
        key: col.key,
        label: col.label,
        id: slots[col.key]?.id ?? null,
        applicable: !!slots[col.key]
      })),
      ...extra.map(e => ({
        key: e.key,
        label: e.label,
        id: e.id,
        applicable: true
      }))
    ]

    return {
      key: moduleKey,
      name: MODULE_LABELS[moduleKey] || moduleKey,
      icon: moduleIcons[moduleKey] || '📁',
      columns,
      slots,
      extra,
      rows
    }
  })
}

function applicableIds(mod) {
  const ids = (mod.columns || CORE_COLUMNS).map(c => mod.slots[c.key]?.id).filter(Boolean)
  for (const e of mod.extra) ids.push(e.id)
  return ids
}

function selectionState(ids, selectedSet) {
  const applicable = ids.filter(Boolean)
  if (!applicable.length) return 'na'
  const count = applicable.filter(id => selectedSet.has(id)).length
  if (count === 0) return 'none'
  if (count === applicable.length) return 'all'
  return 'some'
}

export function usePermissionMatrix(permissions, permissionIds) {
  const search = ref('')
  const scrollTop = ref(0)

  const modules = computed(() => buildModules(permissions.value))
  const selectedSet = computed(() => new Set(permissionIds.value))

  const filteredModules = computed(() => {
    const q = search.value.trim()
    if (!q) return modules.value
    return modules.value.filter(m => m.name.includes(q))
  })

  function setIds(next) {
    permissionIds.value = next
  }

  function toggleId(id, on) {
    const next = new Set(permissionIds.value)
    if (on) next.add(id)
    else next.delete(id)
    setIds([...next])
  }

  function toggleRow(mod, on) {
    const ids = applicableIds(mod)
    const next = new Set(permissionIds.value)
    for (const id of ids) {
      if (on) next.add(id)
      else next.delete(id)
    }
    setIds([...next])
  }

  function selectAll(on) {
    if (on) {
      const all = modules.value.flatMap(applicableIds)
      setIds([...new Set(all)])
    } else {
      setIds([])
    }
  }

  function rowState(mod) {
    return selectionState(applicableIds(mod), selectedSet.value)
  }

  function isGranted(id) {
    return selectedSet.value.has(id)
  }

  function captureScroll(el) {
    if (el) scrollTop.value = el.scrollTop
  }

  function restoreScroll(el) {
    if (el && scrollTop.value) el.scrollTop = scrollTop.value
  }

  return {
    search,
    filteredModules,
    rowState,
    isGranted,
    toggleId,
    toggleRow,
    selectAll,
    captureScroll,
    restoreScroll
  }
}
