<script setup>
import AppSidebar from '../components/AppSidebar.vue'
import AppTopBar from '../components/AppTopBar.vue'
import AppBottomNav from '../components/AppBottomNav.vue'
import PwaInstallBanner from '../components/PwaInstallBanner.vue'
import { useActiveFormPage } from '../composables/useFormPage'

const { isFormPageOpen } = useActiveFormPage()
</script>

<template>
  <div class="layout" :class="{ 'form-open': isFormPageOpen }">
    <AppSidebar class="desktop-only" />
    <AppTopBar class="mobile-only" />
    <main class="main">
      <router-view />
    </main>
    <div v-show="!isFormPageOpen" class="mobile-only">
      <AppBottomNav />
    </div>
    <PwaInstallBanner v-if="!isFormPageOpen" />
  </div>
</template>

<style scoped>
.layout { min-height: 100vh; min-height: 100dvh; }
.main {
  margin-right: 248px;
  padding: 1.25rem 1.5rem 1.5rem;
  max-width: 100%;
  min-height: 100vh;
  min-height: 100dvh;
  box-sizing: border-box;
}
.desktop-only { display: block; }
.mobile-only { display: none; }

@media (max-width: 768px) {
  .desktop-only { display: none !important; }
  .mobile-only { display: block !important; }
  .layout {
    height: 100vh;
    height: 100dvh;
    overflow: hidden;
  }
  .main {
    margin-right: 0;
    padding: 1rem;
    padding-top: calc(56px + env(safe-area-inset-top, 0) + 1rem);
    padding-bottom: calc(72px + env(safe-area-inset-bottom, 0) + 1rem);
    height: 100%;
    min-height: 0;
    overflow-y: auto;
    -webkit-overflow-scrolling: touch;
    overscroll-behavior-y: contain;
    touch-action: pan-y;
  }
  :global(html.pwa-standalone) .main {
    padding-bottom: calc(76px + env(safe-area-inset-bottom, 0) + 0.5rem);
  }
  .layout.form-open .main {
    overflow: hidden;
  }
}
</style>
