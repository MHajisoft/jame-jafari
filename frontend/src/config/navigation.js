/** Module entry: show page if user can view or operate (create/update). */
const incomeAccess = ['income.view', 'income.create', 'income.update']
const costAccess = ['cost.view', 'cost.create', 'cost.update']
const foodAccess = ['food.view', 'food.create', 'food.update']

/**
 * Sidebar / More menu groups:
 * ops (transactions) → base (master data) → config → reports
 */
export const navGroups = [
  { id: 'ops', label: 'عملیات' },
  { id: 'base', label: 'اطلاعات پایه' },
  { id: 'config', label: 'پیکربندی' },
  { id: 'reports', label: 'گزارشات' }
]

export const navItems = [
  // Operations
  { to: '/income', label: 'درآمد', title: 'تراکنش‌های درآمد', permissionsAny: incomeAccess, icon: '💰', group: 'ops', tab: 'income' },
  { to: '/cost', label: 'هزینه', title: 'تراکنش‌های هزینه', permissionsAny: costAccess, icon: '💸', group: 'ops', tab: 'cost' },
  { to: '/food', label: 'تهیه غذا', title: 'تهیه غذا', permissionsAny: foodAccess, icon: '🍲', group: 'ops', section: 'more' },
  // Base / master data
  { to: '/persons', label: 'اشخاص', title: 'اشخاص', permission: 'persons.view', icon: '👥', group: 'base', section: 'more' },
  { to: '/accounts', label: 'حساب‌ها', title: 'حساب‌ها', permission: 'accounts.view', icon: '🏦', group: 'base', section: 'more' },
  { to: '/cost-types', label: 'انواع هزینه', title: 'انواع هزینه', permission: 'costtypes.view', icon: '📋', group: 'base', section: 'more' },
  { to: '/general-types', label: 'انواع عمومی', title: 'انواع عمومی', permission: 'generaltypes.view', icon: '🏷️', group: 'base', section: 'more' },
  // Configuration
  { to: '/users', label: 'کاربران', title: 'کاربران', permission: 'users.view', icon: '👤', group: 'config', section: 'more' },
  { to: '/settings', label: 'تنظیمات', title: 'تنظیمات', icon: '⚙️', group: 'config', section: 'more' },
  // Reports (sidebar last; bottom tab before «بیشتر»)
  { to: '/reports', label: 'گزارشات', title: 'گزارشات', permission: 'reports.view', icon: '📊', group: 'reports', tab: 'reports' }
]

/**
 * Mobile bottom bar (RTL): primary ops → reports → overflow «بیشتر» at the far end.
 * Standard pattern: More is never in the middle.
 */
export const bottomTabs = [
  { to: '/income', label: 'درآمد', icon: '💰', tab: 'income', permissionsAny: incomeAccess },
  { to: '/cost', label: 'هزینه', icon: '💸', tab: 'cost', permissionsAny: costAccess },
  { to: '/reports', label: 'گزارشات', icon: '📊', tab: 'reports', permission: 'reports.view' },
  { to: '/more', label: 'بیشتر', icon: '☰', tab: 'more' }
]

export function filterNavItems(items, hasPermission) {
  return items.filter((item) => {
    if (item.permissionsAny?.length) {
      return item.permissionsAny.some((p) => hasPermission(p))
    }
    return !item.permission || hasPermission(item.permission)
  })
}

/** Group filtered items preserving navGroups order; skip empty groups. */
export function groupNavItems(items, hasPermission) {
  const filtered = filterNavItems(items, hasPermission)
  return navGroups
    .map((g) => ({
      ...g,
      items: filtered.filter((i) => i.group === g.id)
    }))
    .filter((g) => g.items.length > 0)
}

/** First accessible app path after login / visiting `/` — prefer operations. */
export function resolveHomePath(hasPermission) {
  const ordered = [
    { path: '/income', permissionsAny: incomeAccess },
    { path: '/cost', permissionsAny: costAccess },
    { path: '/food', permissionsAny: foodAccess },
    { path: '/persons', permission: 'persons.view' },
    { path: '/accounts', permission: 'accounts.view' },
    { path: '/reports', permission: 'reports.view' },
    { path: '/more' }
  ]
  for (const item of ordered) {
    if (item.permissionsAny?.length) {
      if (item.permissionsAny.some((p) => hasPermission(p))) return item.path
      continue
    }
    if (!item.permission || hasPermission(item.permission)) return item.path
  }
  return '/more'
}
