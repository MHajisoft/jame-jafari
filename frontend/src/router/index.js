import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const routes = [
  { path: '/login', name: 'login', component: () => import('../views/LoginView.vue'), meta: { guest: true } },
  {
    path: '/',
    component: () => import('../layouts/AppLayout.vue'),
    meta: { requiresAuth: true },
    children: [
      { path: '', name: 'dashboard', component: () => import('../views/DashboardView.vue') },
      { path: 'income', name: 'income', component: () => import('../views/IncomeView.vue'), meta: { permission: 'income.view' } },
      { path: 'cost', name: 'cost', component: () => import('../views/CostView.vue'), meta: { permission: 'cost.view' } },
      { path: 'persons', name: 'persons', component: () => import('../views/PersonsView.vue'), meta: { permission: 'persons.view' } },
      { path: 'accounts', name: 'accounts', component: () => import('../views/AccountsView.vue'), meta: { permission: 'accounts.manage' } },
      { path: 'cost-types', name: 'cost-types', component: () => import('../views/CostTypesView.vue'), meta: { permission: 'costtypes.view' } },
      { path: 'food', name: 'food', component: () => import('../views/FoodView.vue'), meta: { permission: 'food.view' } },
      { path: 'users', name: 'users', component: () => import('../views/UsersView.vue'), meta: { permission: 'users.view' } },
      { path: 'reports', name: 'reports', component: () => import('../views/ReportsView.vue'), meta: { permission: 'reports.view' } },
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
  if (to.meta.guest && auth.isAuthenticated) return '/'
  if (to.meta.permission && !auth.hasPermission(to.meta.permission)) return '/'
})

export default router
