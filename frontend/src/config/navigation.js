/** Module entry: show page if user can view or operate (create/update). */
const incomeAccess = ['income.view', 'income.create', 'income.update']
const costAccess = ['cost.view', 'cost.create', 'cost.update']
const foodAccess = ['food.view', 'food.create', 'food.update']

export const navItems = [
  { to: '/reports', label: 'گزارشات', title: 'گزارشات', permission: 'reports.view', icon: '📊', tab: 'reports' },
  { to: '/income', label: 'درآمد', title: 'تراکنش‌های درآمد', permissionsAny: incomeAccess, icon: '💰', tab: 'income' },
  { to: '/cost', label: 'هزینه', title: 'تراکنش‌های هزینه', permissionsAny: costAccess, icon: '💸', tab: 'cost' },
  { to: '/persons', label: 'اشخاص', title: 'اشخاص', permission: 'persons.view', icon: '👥', section: 'more' },
  { to: '/accounts', label: 'حساب‌ها', title: 'حساب‌ها', permission: 'accounts.view', icon: '🏦', section: 'more' },
  { to: '/cost-types', label: 'انواع هزینه', title: 'انواع هزینه', permission: 'costtypes.view', icon: '📋', section: 'more' },
  { to: '/general-types', label: 'انواع عمومی', title: 'انواع عمومی', permission: 'generaltypes.view', icon: '🏷️', section: 'more' },
  { to: '/food', label: 'تهیه غذا', title: 'تهیه غذا', permissionsAny: foodAccess, icon: '🍲', section: 'more' },
  { to: '/users', label: 'کاربران', title: 'کاربران', permission: 'users.view', icon: '👤', section: 'more' },
  { to: '/settings', label: 'تنظیمات', title: 'تنظیمات', icon: '⚙️', section: 'more' }
]

export const bottomTabs = [
  { to: '/reports', label: 'گزارشات', icon: '📊', tab: 'reports', permission: 'reports.view' },
  { to: '/income', label: 'درآمد', icon: '💰', tab: 'income', permissionsAny: incomeAccess },
  { to: '/cost', label: 'هزینه', icon: '💸', tab: 'cost', permissionsAny: costAccess },
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

/** First accessible app path after login / visiting `/`. */
export function resolveHomePath(hasPermission) {
  const ordered = [
    { path: '/reports', permission: 'reports.view' },
    { path: '/income', permissionsAny: incomeAccess },
    { path: '/cost', permissionsAny: costAccess },
    { path: '/persons', permission: 'persons.view' },
    { path: '/accounts', permission: 'accounts.view' },
    { path: '/food', permissionsAny: foodAccess },
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
