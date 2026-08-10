<script setup>
defineProps({
  showEdit: { type: Boolean, default: false },
  showDelete: { type: Boolean, default: false },
  editLabel: { type: String, default: 'ویرایش' },
  deleteLabel: { type: String, default: 'حذف' }
})

defineEmits(['edit', 'delete'])
</script>

<template>
  <div class="row-actions" role="group" aria-label="عملیات">
    <button
      v-if="showEdit"
      type="button"
      class="icon-btn"
      :aria-label="editLabel"
      :title="editLabel"
      @click="$emit('edit')"
    >
      <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <path
          d="M12 20h9"
          stroke="currentColor"
          stroke-width="1.75"
          stroke-linecap="round"
        />
        <path
          d="M16.5 3.5a2.12 2.12 0 0 1 3 3L7 19l-4 1 1-4L16.5 3.5z"
          stroke="currentColor"
          stroke-width="1.75"
          stroke-linecap="round"
          stroke-linejoin="round"
        />
      </svg>
    </button>
    <button
      v-if="showDelete"
      type="button"
      class="icon-btn icon-btn-danger"
      :aria-label="deleteLabel"
      :title="deleteLabel"
      @click="$emit('delete')"
    >
      <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <path
          d="M3 6h18"
          stroke="currentColor"
          stroke-width="1.75"
          stroke-linecap="round"
        />
        <path
          d="M8 6V4.8A1.8 1.8 0 0 1 9.8 3h4.4A1.8 1.8 0 0 1 16 4.8V6m2 0v13.2A1.8 1.8 0 0 1 16.2 21H7.8A1.8 1.8 0 0 1 6 19.2V6"
          stroke="currentColor"
          stroke-width="1.75"
          stroke-linecap="round"
          stroke-linejoin="round"
        />
        <path d="M10 11v6M14 11v6" stroke="currentColor" stroke-width="1.75" stroke-linecap="round" />
      </svg>
    </button>
  </div>
</template>

<style scoped>
.row-actions {
  display: inline-flex;
  align-items: center;
  gap: 0.2rem;
}
.icon-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2.15rem;
  height: 2.15rem;
  padding: 0;
  border: none;
  border-radius: 9px;
  background: transparent;
  color: var(--text-muted);
  cursor: pointer;
  transition:
    background-color 0.15s ease-out,
    color 0.15s ease-out,
    transform 0.12s ease-out;
}
.icon-btn svg {
  width: 1.05rem;
  height: 1.05rem;
  display: block;
}
.icon-btn:hover {
  background: color-mix(in srgb, var(--primary) 12%, transparent);
  color: var(--primary);
}
.icon-btn:focus-visible {
  outline: 2px solid var(--primary);
  outline-offset: 2px;
}
.icon-btn:active {
  transform: scale(0.96);
}
.icon-btn-danger:hover {
  background: color-mix(in srgb, var(--danger) 12%, transparent);
  color: var(--danger);
}
.icon-btn-danger:focus-visible {
  outline-color: var(--danger);
}

@media (max-width: 768px) {
  .icon-btn {
    width: 2.4rem;
    height: 2.4rem;
    background: color-mix(in srgb, var(--surface) 70%, var(--bg));
    border: 1px solid color-mix(in srgb, var(--border) 80%, transparent);
  }
  .icon-btn svg {
    width: 1.1rem;
    height: 1.1rem;
  }
}
</style>
