<script setup>
import RefreshButton from './RefreshButton.vue'

defineProps({
  title: { type: String, required: true },
  formMode: { type: Boolean, default: false },
  showCreate: { type: Boolean, default: false },
  createLabel: { type: String, default: 'جدید' },
  showRefresh: { type: Boolean, default: false },
  refreshLoading: { type: Boolean, default: false }
})

defineEmits(['create', 'refresh'])
</script>

<template>
  <div class="page-header" :class="{ 'form-mode': formMode }">
    <h1 class="page-title">{{ title }}</h1>
    <div v-if="showRefresh || showCreate" class="page-toolbar">
      <RefreshButton
        v-if="showRefresh"
        :loading="refreshLoading"
        @click="$emit('refresh')"
      />
      <button
        v-if="showCreate"
        type="button"
        class="btn btn-fab-mobile"
        @click="$emit('create')"
      >
        <span aria-hidden="true">+</span>
        <span class="btn-fab-label">{{ createLabel }}</span>
      </button>
    </div>
  </div>
</template>
