<script setup>
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { navItems } from '../config/navigation'
import { useActiveFormPage } from '../composables/useFormPage'

const route = useRoute()
const router = useRouter()
const { activeFormPage, isFormPageOpen } = useActiveFormPage()

const listTitle = computed(() => {
  if (route.path === '/more') return 'بیشتر'
  if (route.path === '/profile') return 'پروفایل'
  const item = navItems.find(n => n.to === route.path)
  return item?.title || 'جامعه جعفری'
})

const title = computed(() => activeFormPage.value?.title || listTitle.value)

const primaryPaths = ['/', '/income', '/cost', '/more']
const showBack = computed(() => isFormPageOpen.value || !primaryPaths.includes(route.path))

function onBack() {
  if (activeFormPage.value) {
    activeFormPage.value.close()
    return
  }
  router.push('/more')
}
</script>

<template>
  <header class="top-bar">
    <div class="top-bar-inner">
      <div class="side side-start">
        <button
          v-if="showBack"
          type="button"
          class="back-btn"
          aria-label="بازگشت"
          @click="onBack"
        >›</button>
        <router-link
          to="/"
          class="brand-mark"
          aria-label="موسسه جامعه جعفری"
        >
          <img src="/logo.png" alt="" width="36" height="36" />
        </router-link>
      </div>

      <h1 class="top-title">{{ title }}</h1>

      <div class="side side-end">
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
  background: color-mix(in srgb, var(--surface) 94%, transparent);
  backdrop-filter: blur(12px);
  -webkit-backdrop-filter: blur(12px);
  border-bottom: 1px solid var(--border);
  padding-top: env(safe-area-inset-top, 0);
}
.top-bar-inner {
  position: relative;
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 56px;
  padding: 0 0.75rem;
  gap: 0.5rem;
}
.side {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  min-width: 76px;
  z-index: 1;
}
.side-start { justify-content: flex-start; }
.side-end { justify-content: flex-end; }
.back-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  font-size: 1.45rem;
  line-height: 1;
  color: var(--primary);
  border-radius: 10px;
  flex-shrink: 0;
  border: none;
  background: transparent;
  cursor: pointer;
  font: inherit;
  padding: 0;
}
.brand-mark {
  width: 36px;
  height: 36px;
  border-radius: 10px;
  overflow: hidden;
  flex-shrink: 0;
  box-shadow: 0 0 0 1px color-mix(in srgb, var(--border) 80%, transparent);
  background: var(--bg-elevated);
}
.brand-mark img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}
.top-title {
  position: absolute;
  left: 80px;
  right: 80px;
  top: 50%;
  transform: translateY(-50%);
  margin: 0;
  font-size: 1.02rem;
  font-weight: 700;
  text-align: center;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  line-height: 1.25;
  pointer-events: none;
}
</style>
