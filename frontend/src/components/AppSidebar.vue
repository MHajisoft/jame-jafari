<script setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import { navItems, filterNavItems } from '../config/navigation'

const route = useRoute()
const auth = useAuthStore()
const items = computed(() => filterNavItems(navItems, auth.hasPermission))
</script>

<template>
  <aside class="sidebar">
    <router-link to="/profile" class="brand" :class="{ active: route.path === '/profile' }">
      <div class="brand-avatar">
        <img v-if="auth.avatarUrl" :src="auth.avatarUrl" alt="" />
        <span v-else>{{ auth.initials }}</span>
      </div>
      <div class="brand-text">
        <h1>موسسه جامعه جعفری</h1>
        <p class="user">{{ auth.username }}</p>
      </div>
    </router-link>
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
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 1.25rem 1rem;
  border-bottom: 1px solid rgba(255,255,255,0.15);
  color: inherit;
  text-decoration: none;
  transition: background 0.15s;
}
.brand:hover,
.brand.active {
  background: rgba(255,255,255,0.12);
}
.brand-avatar {
  width: 42px;
  height: 42px;
  border-radius: 50%;
  overflow: hidden;
  flex-shrink: 0;
  background: rgba(255,255,255,0.2);
  display: flex;
  align-items: center;
  justify-content: center;
  font-weight: 700;
}
.brand-avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
.brand-text { min-width: 0; }
.brand h1 {
  font-size: 0.95rem;
  margin: 0;
  line-height: 1.3;
}
.user {
  font-size: 0.8rem;
  color: var(--sidebar-muted);
  margin-top: 0.15rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
nav { flex: 1; padding: 1rem 0; overflow-y: auto; }
nav a {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem 1.25rem;
  color: var(--sidebar-text);
  opacity: 0.88;
  transition: all 0.2s;
}
nav a:hover, nav a.active {
  background: rgba(255,255,255,0.12);
  opacity: 1;
}
</style>
