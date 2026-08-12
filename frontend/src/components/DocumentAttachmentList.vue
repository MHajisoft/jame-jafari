<script setup>
import DocumentAttachmentButton from './DocumentAttachmentButton.vue'

defineProps({
  attachments: { type: Array, default: () => [] }
})
</script>

<template>
  <div v-if="attachments?.length" class="doc-attach-list">
    <DocumentAttachmentButton
      v-for="att in attachments"
      :key="att.id"
      :path="att.path"
    />
  </div>
  <span v-else class="text-muted doc-attach-empty">—</span>
</template>

<style scoped>
.doc-attach-list {
  display: inline-flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  align-items: center;
  justify-content: flex-end;
  max-width: 100%;
}

.doc-attach-empty {
  display: inline-block;
}
</style>
