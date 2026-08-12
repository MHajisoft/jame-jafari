<script setup>
import { computed, onMounted } from 'vue'
import { useAuthStore } from '../stores/auth'
import { navItems, filterNavItems } from '../config/navigation'

const auth = useAuthStore()

const menuItems = computed(() =>
  filterNavItems(navItems.filter(n => n.section === 'more'), auth.hasPermission)
)

onMounted(() => {
  if (auth.isAuthenticated) auth.fetchProfile().catch(() => {})
})
</script>

<template>
  <div class="more-page">
    <div class="brand-hero card">
      <img class="brand-hero-logo" src="/logo.png" alt="موسسه جامعه جعفری" width="64" height="64" />
      <div class="brand-hero-text">
        <strong>موسسه جامعه جعفری</strong>
        <span class="text-muted">سامانه مدیریت مالی</span>
      </div>
    </div>

    <router-link to="/profile" class="user-card card">
      <div class="user-avatar">
        <img v-if="auth.avatarUrl" :src="auth.avatarUrl" alt="" />
        <span v-else>{{ auth.initials }}</span>
      </div>
      <div class="user-meta">
        <div class="user-name">{{ auth.username }}</div>
        <div class="text-muted user-sub">مشاهده و ویرایش پروفایل</div>
      </div>
      <span class="user-chevron" aria-hidden="true">‹</span>
    </router-link>

    <div class="menu-grid">
      <router-link
        v-for="item in menuItems"
        :key="item.to"
        :to="item.to"
        class="menu-item card"
      >
        <span class="menu-icon">{{ item.icon }}</span>
        <span class="menu-label">{{ item.label }}</span>
      </router-link>
    </div>
  </div>
</template>

<style scoped>
.more-page { padding-bottom: 1rem; }
.brand-hero {
  display: flex;
  align-items: center;
  gap: 0.9rem;
  margin-bottom: 1rem;
  padding: 1rem;
}
.brand-hero-logo {
  width: 64px;
  height: 64px;
  border-radius: 16px;
  object-fit: cover;
  flex-shrink: 0;
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.12);
}
.brand-hero-text {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  min-width: 0;
}
.brand-hero-text strong {
  font-size: 1.05rem;
  line-height: 1.3;
}
.brand-hero-text span {
  font-size: 0.85rem;
}
.user-card {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1.25rem;
  color: inherit;
  text-decoration: none;
}
.user-avatar {
  width: 52px;
  height: 52px;
  border-radius: 50%;
  background: var(--primary);
  color: white;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.25rem;
  font-weight: 700;
  flex-shrink: 0;
  overflow: hidden;
}
.user-avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
.user-meta { flex: 1; min-width: 0; }
.user-name { font-weight: 700; font-size: 1.05rem; }
.user-sub { font-size: 0.85rem; margin-top: 0.15rem; }
.user-chevron {
  font-size: 1.4rem;
  color: var(--text-muted);
  line-height: 1;
}
.menu-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 0.75rem;
  margin-bottom: 1.5rem;
}
.menu-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
  padding: 1.25rem 0.75rem;
  color: var(--text);
  transition: transform 0.15s;
  -webkit-tap-highlight-color: transparent;
}
.menu-item:active { transform: scale(0.97); }
.menu-icon { font-size: 1.75rem; }
.menu-label { font-size: 0.85rem; font-weight: 600; text-align: center; }
</style>
