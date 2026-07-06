<script setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { navItems } from '../config/navigation'

const route = useRoute()

const title = computed(() => {
  if (route.path === '/more') return 'بیشتر'
  const item = navItems.find(n => n.to === route.path)
  return item?.title || 'جامعه جعفری'
})

const primaryPaths = ['/', '/income', '/cost', '/more']
const showBack = computed(() => !primaryPaths.includes(route.path))
</script>

<template>
  <header class="top-bar">
    <div class="top-bar-inner">
      <router-link v-if="showBack" to="/more" class="back-btn" aria-label="بازگشت">›</router-link>
      <div v-else class="back-spacer" />
      <h1 class="top-title">{{ title }}</h1>
      <div class="top-actions">
        <slot />
      </div>
    </div>
  </header>
</template>

<style scoped>
.top-bar {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  z-index: 200;
  background: var(--surface);
  border-bottom: 1px solid var(--border);
  padding-top: env(safe-area-inset-top, 0);
}
.top-bar-inner {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  height: 56px;
  padding: 0 0.75rem;
}
.back-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  font-size: 1.5rem;
  line-height: 1;
  color: var(--primary);
  border-radius: 10px;
}
.back-spacer { width: 40px; flex-shrink: 0; }
.top-title {
  flex: 1;
  font-size: 1.05rem;
  font-weight: 700;
  text-align: center;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.top-actions {
  min-width: 40px;
  display: flex;
  justify-content: flex-end;
}
</style>
