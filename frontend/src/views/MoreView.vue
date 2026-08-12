<script setup>
import { computed, onMounted } from 'vue'
import { useAuthStore } from '../stores/auth'
import { navItems, filterNavItems } from '../config/navigation'
import AppBrandMark from '../components/AppBrandMark.vue'
import AppAccountChip from '../components/AppAccountChip.vue'

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
      <AppBrandMark size="lg" tone="on-surface" show-tagline />
    </div>

    <AppAccountChip
      class="user-card card"
      variant="row"
      subtitle="مشاهده و ویرایش پروفایل"
      show-chevron
    />

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
  margin-bottom: 1rem;
  padding: 1rem;
}
.user-card {
  margin-bottom: 1.25rem;
  padding: 0.85rem 1rem !important;
}
.user-card :deep(.account-name) {
  font-size: 1.05rem;
}
.user-card :deep(.account-sub) {
  font-size: 0.85rem;
}
.user-card :deep(.entity-avatar) {
  width: 52px !important;
  height: 52px !important;
  font-size: 1.15rem !important;
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
