export const navItems = [
  { to: '/', label: 'داشبورد', title: 'داشبورد', icon: '🏠', tab: 'home' },
  { to: '/income', label: 'درآمد', title: 'تراکنش‌های درآمد', permission: 'income.view', icon: '💰', tab: 'income' },
  { to: '/cost', label: 'هزینه', title: 'تراکنش‌های هزینه', permission: 'cost.view', icon: '💸', tab: 'cost' },
  { to: '/persons', label: 'اشخاص', title: 'اشخاص', permission: 'persons.view', icon: '👥', section: 'more' },
  { to: '/accounts', label: 'حساب‌ها', title: 'حساب‌ها', permission: 'accounts.manage', icon: '🏦', section: 'more' },
  { to: '/cost-types', label: 'انواع هزینه', title: 'انواع هزینه', permission: 'costtypes.view', icon: '📋', section: 'more' },
  { to: '/general-types', label: 'انواع عمومی', title: 'انواع عمومی', permissionsAny: ['generaltypes.manage', 'costtypes.manage'], icon: '🏷️', section: 'more' },
  { to: '/food', label: 'تهیه غذا', title: 'تهیه غذا', permission: 'food.view', icon: '🍲', section: 'more' },
  { to: '/reports', label: 'گزارشات', title: 'گزارشات', permission: 'reports.view', icon: '📊', section: 'more' },
  { to: '/users', label: 'کاربران', title: 'کاربران', permission: 'users.view', icon: '👤', section: 'more' },
  { to: '/settings', label: 'تنظیمات', title: 'تنظیمات', icon: '⚙️', section: 'more' }
]

export const bottomTabs = [
  { to: '/', label: 'خانه', icon: '🏠', tab: 'home' },
  { to: '/income', label: 'درآمد', icon: '💰', tab: 'income', permission: 'income.view' },
  { to: '/cost', label: 'هزینه', icon: '💸', tab: 'cost', permission: 'cost.view' },
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
