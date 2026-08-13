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
  return route.path === tab.to || route.path.startsWith(tab.to + '/')
}

function isMoreSection(path) {
  // Bottom primary tabs — everything else (config + ops extras) lives under «بیشتر»
  const primary = ['/income', '/cost', '/more', '/reports', '/login']
  return !primary.includes(path)
}
</script>

<template>
  <nav class="bottom-nav" aria-label="ناوبری اصلی">
    <router-link
      v-for="tab in tabs"
      :key="tab.to"
      :to="tab.to"
      replace
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
  min-height: calc(56px + env(safe-area-inset-bottom, 0));
  background: color-mix(in srgb, var(--surface) 92%, transparent);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
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
  gap: 0.2rem;
  min-height: 48px;
  padding: 0.45rem 0.25rem 0.5rem;
  color: var(--text-muted);
  transition: color 0.2s, background 0.2s;
  -webkit-tap-highlight-color: transparent;
  border-radius: 12px;
}
.nav-item:active {
  background: var(--row-hover);
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
