<script setup>
import AppSidebar from '../components/AppSidebar.vue'
import AppTopBar from '../components/AppTopBar.vue'
import AppBottomNav from '../components/AppBottomNav.vue'
</script>

<template>
  <div class="layout">
    <AppSidebar class="desktop-only" />
    <AppTopBar class="mobile-only" />
    <main class="main">
      <router-view />
    </main>
    <div class="mobile-only">
      <AppBottomNav />
    </div>
  </div>
</template>

<style scoped>
.layout { min-height: 100vh; min-height: 100dvh; }
.main {
  margin-right: 240px;
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
  .main {
    margin-right: 0;
    padding: 1rem;
    padding-top: calc(56px + env(safe-area-inset-top, 0) + 1rem);
    padding-bottom: calc(64px + env(safe-area-inset-bottom, 0) + 1rem);
    min-height: 100vh;
    min-height: 100dvh;
    overflow-y: auto;
    -webkit-overflow-scrolling: touch;
  }
}
</style>
