<script setup>
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const navItems = computed(() => [
  { to: '/', label: 'داشبورد', icon: '🏠' },
  { to: '/income', label: 'درآمد', permission: 'income.view', icon: '💰' },
  { to: '/cost', label: 'هزینه', permission: 'cost.view', icon: '💸' },
  { to: '/persons', label: 'اشخاص', permission: 'persons.view', icon: '👥' },
  { to: '/accounts', label: 'حساب‌ها', permission: 'accounts.manage', icon: '🏦' },
  { to: '/cost-types', label: 'انواع هزینه', permission: 'costtypes.view', icon: '📋' },
  { to: '/food', label: 'تهیه غذا', permission: 'food.view', icon: '🍲' },
  { to: '/reports', label: 'گزارشات', permission: 'reports.view', icon: '📊' },
  { to: '/users', label: 'کاربران', permission: 'users.view', icon: '👤' },
  { to: '/settings', label: 'تنظیمات', icon: '⚙️' }
].filter(item => !item.permission || auth.hasPermission(item.permission)))

function logout() {
  auth.logout()
  router.push('/login')
}
</script>

<template>
  <aside class="sidebar">
    <div class="brand">
      <h1>جامع جعفری</h1>
      <p class="user">{{ auth.username }}</p>
    </div>
    <nav>
      <router-link
        v-for="item in navItems"
        :key="item.to"
        :to="item.to"
        :class="{ active: route.path === item.to }"
      >
        <span>{{ item.icon }}</span>
        {{ item.label }}
      </router-link>
    </nav>
    <button class="logout-btn" @click="logout">خروج</button>
  </aside>
</template>

<style scoped>
.sidebar {
  width: 240px;
  min-height: 100vh;
  background: var(--sidebar);
  color: var(--sidebar-text);
  display: flex;
  flex-direction: column;
  position: fixed;
  right: 0;
  top: 0;
  z-index: 100;
}
.brand {
  padding: 1.5rem 1rem;
  border-bottom: 1px solid rgba(255,255,255,0.15);
}
.brand h1 { font-size: 1.1rem; }
.user { font-size: 0.8rem; opacity: 0.8; margin-top: 0.25rem; }
nav { flex: 1; padding: 1rem 0; overflow-y: auto; }
nav a {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem 1.25rem;
  color: var(--sidebar-text);
  opacity: 0.85;
  transition: all 0.2s;
}
nav a:hover, nav a.active {
  background: rgba(255,255,255,0.15);
  opacity: 1;
}
.logout-btn {
  margin: 1rem;
  padding: 0.75rem;
  background: rgba(255,255,255,0.15);
  border: none;
  border-radius: 8px;
  color: var(--sidebar-text);
  cursor: pointer;
}
@media (max-width: 768px) {
  .sidebar {
    width: 100%;
    min-height: auto;
    position: relative;
  }
}
</style>
