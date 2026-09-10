<script setup>
import MessengerKindCell from './MessengerKindCell.vue'
import MessengerMessageTypeCell from './MessengerMessageTypeCell.vue'
import MessengerMessageStatusCell from './MessengerMessageStatusCell.vue'

defineProps({
  messengerKind: { type: [Number, String], default: null },
  messageType: { type: [Number, String], default: null },
  status: { type: [Number, String], default: null },
  errorMessage: { type: String, default: '' },
  errorExpanded: { type: Boolean, default: false }
})

const emit = defineEmits(['toggle-error'])
</script>

<template>
  <div class="message-meta-cluster" role="group" aria-label="مشخصات پیام">
    <MessengerKindCell :kind="messengerKind" />
    <span class="meta-sep" aria-hidden="true" />
    <MessengerMessageTypeCell :message-type="messageType" />
    <span class="meta-sep" aria-hidden="true" />
    <span class="status-cell">
      <MessengerMessageStatusCell :status="status" />
      <button
        v-if="errorMessage"
        type="button"
        class="status-error-trigger"
        aria-label="نمایش خطا"
        :aria-expanded="errorExpanded"
        @click.stop="emit('toggle-error', $event)"
      >
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
          <circle cx="12" cy="12" r="9" />
          <path d="M12 8v5M12 16h.01" stroke-linecap="round" />
        </svg>
      </button>
    </span>
  </div>
</template>

<style scoped>
.message-meta-cluster {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  min-width: 0;
  max-width: 100%;
}
.meta-sep {
  width: 1px;
  height: 1rem;
  flex-shrink: 0;
  background: color-mix(in srgb, var(--border) 90%, transparent);
}
.status-cell {
  display: inline-flex;
  align-items: center;
  gap: 0.2rem;
  white-space: nowrap;
}
.status-error-trigger {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 1.35rem;
  height: 1.35rem;
  margin-inline-start: 0.1rem;
  padding: 0;
  border: none;
  border-radius: 999px;
  background: var(--danger-soft);
  color: var(--danger);
  vertical-align: middle;
  cursor: pointer;
}
.status-error-trigger:hover {
  filter: brightness(0.96);
}
.status-error-trigger svg {
  width: 0.95rem;
  height: 0.95rem;
}
</style>
