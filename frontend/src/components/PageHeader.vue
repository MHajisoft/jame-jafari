<script setup>
defineProps({
  title: { type: String, required: true },
  formMode: { type: Boolean, default: false },
  showCreate: { type: Boolean, default: false },
  createLabel: { type: String, default: 'جدید' }
})

defineEmits(['create'])
</script>

<template>
  <div class="page-header" :class="{ 'form-mode': formMode }">
    <h1 class="page-title">{{ title }}</h1>
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
</template>
