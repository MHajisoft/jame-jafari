<script setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { navItems, filterNavItems } from '../config/navigation'
import { useAuthStore } from '../stores/auth'
import AppBrandMark from './AppBrandMark.vue'
import AppAccountChip from './AppAccountChip.vue'

const route = useRoute()
const auth = useAuthStore()
const items = computed(() => filterNavItems(navItems, auth.hasPermission))
</script>

<template>
  <aside class="sidebar">
    <div class="sidebar-brand">
      <AppBrandMark to="/" size="md" tone="on-dark" />
    </div>

    <nav>
      <router-link
        v-for="item in items"
        :key="item.to"
        :to="item.to"
        :class="{ active: route.path === item.to }"
      >
        <span>{{ item.icon }}</span>
        {{ item.label }}
      </router-link>
    </nav>

    <footer class="sidebar-footer">
      <AppAccountChip
        variant="row"
        tone="on-dark"
        subtitle="پروفایل"
        :class="{ active: route.path === '/profile' }"
      />
    </footer>
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
.sidebar-brand {
  padding: 1.25rem 1rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.15);
}
nav {
  flex: 1;
  padding: 1rem 0;
  overflow-y: auto;
}
nav a {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem 1.25rem;
  color: var(--sidebar-text);
  opacity: 0.88;
  transition: all 0.2s;
}
nav a:hover,
nav a.active {
  background: rgba(255, 255, 255, 0.12);
  opacity: 1;
}
.sidebar-footer {
  margin-top: auto;
  padding: 0.5rem;
  border-top: 1px solid rgba(255, 255, 255, 0.15);
}
</style>
