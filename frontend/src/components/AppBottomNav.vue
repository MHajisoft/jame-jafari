<script setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { bottomTabs, filterNavItems } from '../config/navigation'

const route = useRoute()
const auth = useAuthStore()

const tabs = computed(() => filterNavItems(bottomTabs, auth.hasPermission))

function isActive(tab) {
  if (tab.tab === 'more') return route.path === '/more' || isMoreSection(route.path)
  return route.path === tab.to
}

function isMoreSection(path) {
  return !['/', '/income', '/cost', '/more', '/login'].includes(path)
}
</script>

<template>
  <nav class="bottom-nav" aria-label="ناوبری اصلی">
    <router-link
      v-for="tab in tabs"
      :key="tab.to"
      :to="tab.to"
      class="nav-item"
      :class="{ active: isActive(tab) }"
    >
      <span class="nav-icon">{{ tab.icon }}</span>
      <span class="nav-label">{{ tab.label }}</span>
    </router-link>
  </nav>
</template>

<style scoped>
.bottom-nav {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  z-index: 200;
  display: flex;
  flex-direction: row;
  align-items: stretch;
  background: var(--surface);
  border-top: 1px solid var(--border);
  padding-bottom: env(safe-area-inset-bottom, 0);
  box-shadow: 0 -2px 12px rgba(0, 0, 0, 0.06);
}
.nav-item {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 0.15rem;
  padding: 0.5rem 0.25rem 0.6rem;
  color: var(--text-muted);
  transition: color 0.2s;
  -webkit-tap-highlight-color: transparent;
}
.nav-item.active {
  color: var(--primary);
}
.nav-icon {
  font-size: 1.35rem;
  line-height: 1;
}
.nav-label {
  font-size: 0.65rem;
  font-weight: 600;
}
</style>
