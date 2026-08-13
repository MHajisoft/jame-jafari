import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { resolveHomePath } from '../config/navigation'

const routes = [
  { path: '/login', name: 'login', component: () => import('../views/LoginView.vue'), meta: { guest: true } },
  {
    path: '/',
    component: () => import('../layouts/AppLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        name: 'home',
        redirect: () => resolveHomePath(useAuthStore().hasPermission)
      },
      {
        path: 'income',
        name: 'income',
        component: () => import('../views/IncomeView.vue'),
        meta: { permissionsAny: ['income.view', 'income.create', 'income.update'] }
      },
      {
        path: 'cost',
        name: 'cost',
        component: () => import('../views/CostView.vue'),
        meta: { permissionsAny: ['cost.view', 'cost.create', 'cost.update'] }
      },
      { path: 'persons', name: 'persons', component: () => import('../views/PersonsView.vue'), meta: { permission: 'persons.view' } },
      { path: 'accounts', name: 'accounts', component: () => import('../views/AccountsView.vue'), meta: { permission: 'accounts.view' } },
      { path: 'cost-types', name: 'cost-types', component: () => import('../views/CostTypesView.vue'), meta: { permission: 'costtypes.view' } },
      { path: 'general-types', name: 'general-types', component: () => import('../views/GeneralTypesView.vue'), meta: { permission: 'generaltypes.view' } },
      {
        path: 'food',
        name: 'food',
        component: () => import('../views/FoodView.vue'),
        meta: { permissionsAny: ['food.view', 'food.create', 'food.update'] }
      },
      { path: 'users', name: 'users', component: () => import('../views/UsersView.vue'), meta: { permission: 'users.view' } },
      { path: 'reports', name: 'reports', component: () => import('../views/ReportsView.vue'), meta: { permission: 'reports.view' } },
      {
        path: 'reports/death-anniversaries',
        name: 'death-anniversaries-report',
        component: () => import('../views/DeathAnniversariesReportView.vue'),
        meta: { permission: 'deathanniversaries.view' }
      },
      { path: 'settings', name: 'settings', component: () => import('../views/SettingsView.vue') },
      { path: 'profile', name: 'profile', component: () => import('../views/ProfileView.vue') },
      { path: 'more', name: 'more', component: () => import('../views/MoreView.vue') }
    ]
  }
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

router.beforeEach((to) => {
  const auth = useAuthStore()
  if (to.meta.requiresAuth && !auth.isAuthenticated) return '/login'
  if (to.meta.guest && auth.isAuthenticated) return resolveHomePath(auth.hasPermission)
  if (to.meta.permission && !auth.hasPermission(to.meta.permission)) {
    return resolveHomePath(auth.hasPermission)
  }
  if (to.meta.permissionsAny?.length && !to.meta.permissionsAny.some((p) => auth.hasPermission(p))) {
    return resolveHomePath(auth.hasPermission)
  }
})

export default router
