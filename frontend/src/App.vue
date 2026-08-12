<script setup>
import AppToast from './components/AppToast.vue'
import AppDialog from './components/AppDialog.vue'
</script>

<template>
  <router-view />
  <AppToast />
  <AppDialog />
</template>
