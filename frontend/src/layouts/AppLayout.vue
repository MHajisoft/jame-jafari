<script setup>
import AppSidebar from '../components/AppSidebar.vue'
</script>

<template>
  <div class="layout">
    <AppSidebar />
    <main class="main">
      <router-view />
    </main>
  </div>
</template>

<style scoped>
.layout { display: flex; min-height: 100vh; }
.main {
  flex: 1;
  margin-right: 240px;
  padding: 1.5rem;
  max-width: 100%;
}
@media (max-width: 768px) {
  .layout { flex-direction: column; }
  .main { margin-right: 0; padding: 1rem; }
}
</style>
