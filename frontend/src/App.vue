<script setup>
import AppToast from './components/AppToast.vue'
import AppDialog from './components/AppDialog.vue'
import AppGlobalLoader from './components/AppGlobalLoader.vue'
</script>

<template>
  <router-view />
  <AppToast />
  <AppDialog />
  <AppGlobalLoader />
</template>
