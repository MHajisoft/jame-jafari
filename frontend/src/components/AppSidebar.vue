<script setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { navItems, groupNavItems } from '../config/navigation'
import { useAuthStore } from '../stores/auth'
import AppBrandMark from './AppBrandMark.vue'
import AppAccountChip from './AppAccountChip.vue'

const route = useRoute()
const auth = useAuthStore()
const groups = computed(() => groupNavItems(navItems, auth.hasPermission))
</script>

<template>
  <aside class="sidebar">
    <div class="sidebar-brand">
      <AppBrandMark to="/" size="md" tone="on-dark" />
    </div>

    <nav aria-label="منوی اصلی">
      <section
        v-for="(group, gi) in groups"
        :key="group.id"
        class="nav-group"
        :class="{ 'is-last': gi === groups.length - 1 }"
      >
        <h2 class="nav-group-label">{{ group.label }}</h2>
        <router-link
          v-for="item in group.items"
          :key="item.to"
          :to="item.to"
          :title="item.title"
          :class="{ active: route.path === item.to }"
        >
          <span class="nav-icon" aria-hidden="true">{{ item.icon }}</span>
          <span class="nav-text">{{ item.label }}</span>
        </router-link>
      </section>
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
  width: 248px;
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
  border-bottom: 1px solid rgba(255, 255, 255, 0.12);
}
nav {
  flex: 1;
  padding: 0.65rem 0 1rem;
  overflow-y: auto;
}
.nav-group {
  padding: 0.35rem 0 0.55rem;
}
.nav-group + .nav-group {
  margin-top: 0.35rem;
  border-top: 1px solid rgba(255, 255, 255, 0.1);
  padding-top: 0.75rem;
}
.nav-group-label {
  margin: 0 1.15rem 0.4rem;
  font-size: 0.68rem;
  font-weight: 700;
  letter-spacing: 0.04em;
  color: var(--sidebar-muted, rgba(238, 249, 244, 0.55));
  text-transform: none;
}
nav a {
  display: flex;
  align-items: center;
  gap: 0.7rem;
  margin: 0.1rem 0.45rem;
  padding: 0.62rem 0.8rem;
  border-radius: 10px;
  color: var(--sidebar-text);
  opacity: 0.9;
  transition: background 0.15s, opacity 0.15s;
}
.nav-icon {
  font-size: 1.05rem;
  line-height: 1;
  width: 1.35rem;
  text-align: center;
}
.nav-text {
  font-size: 0.92rem;
  font-weight: 600;
}
nav a:hover,
nav a.active {
  background: rgba(255, 255, 255, 0.12);
  opacity: 1;
}
.sidebar-footer {
  margin-top: auto;
  padding: 0.5rem;
  border-top: 1px solid rgba(255, 255, 255, 0.12);
}
</style>
