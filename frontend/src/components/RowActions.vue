<script setup>
defineProps({
  showEdit: { type: Boolean, default: false },
  showDelete: { type: Boolean, default: false },
  showChangePassword: { type: Boolean, default: false },
  editLabel: { type: String, default: 'ویرایش' },
  deleteLabel: { type: String, default: 'حذف' },
  changePasswordLabel: { type: String, default: 'تغییر رمز' }
})

defineEmits(['edit', 'delete', 'change-password'])
</script>

<template>
  <div class="row-actions" role="group" aria-label="عملیات">
    <button
      v-if="showEdit"
      type="button"
      class="icon-btn"
      :aria-label="editLabel"
      :title="editLabel"
      @click.stop="$emit('edit')"
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
      v-if="showChangePassword"
      type="button"
      class="icon-btn"
      :aria-label="changePasswordLabel"
      :title="changePasswordLabel"
      @click.stop="$emit('change-password')"
    >
      <svg viewBox="0 0 24 24" fill="none" aria-hidden="true">
        <path
          d="M7 11V8a5 5 0 0 1 10 0v3"
          stroke="currentColor"
          stroke-width="1.75"
          stroke-linecap="round"
        />
        <rect
          x="5"
          y="11"
          width="14"
          height="10"
          rx="2"
          stroke="currentColor"
          stroke-width="1.75"
        />
        <circle cx="12" cy="15" r="1.25" fill="currentColor" />
      </svg>
    </button>
    <button
      v-if="showDelete"
      type="button"
      class="icon-btn icon-btn-danger"
      :aria-label="deleteLabel"
      :title="deleteLabel"
      @click.stop="$emit('delete')"
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
  gap: 0.35rem;
}
.icon-btn {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2.15rem;
  height: 2.15rem;
  padding: 0;
  border: 1px solid transparent;
  border-radius: 10px;
  background: transparent;
  color: var(--text-muted);
  cursor: pointer;
  -webkit-tap-highlight-color: transparent;
  transition:
    background-color 0.15s ease-out,
    color 0.15s ease-out,
    border-color 0.15s ease-out,
    transform 0.12s ease-out;
}
.icon-btn svg {
  width: 1.05rem;
  height: 1.05rem;
  display: block;
}

/* Desktop: subtle until hover/focus */
@media (hover: hover) and (pointer: fine) {
  .icon-btn:hover {
    background: color-mix(in srgb, var(--primary) 12%, transparent);
    color: var(--primary);
  }
  .icon-btn-danger:hover {
    background: color-mix(in srgb, var(--danger) 12%, transparent);
    color: var(--danger);
  }
}

.icon-btn:focus-visible {
  outline: 2px solid var(--primary);
  outline-offset: 2px;
}
.icon-btn:active {
  transform: scale(0.96);
}
.icon-btn-danger:focus-visible {
  outline-color: var(--danger);
}

/* Mobile / touch: always visible chrome (no hover dependency) */
@media (max-width: 768px), (hover: none), (pointer: coarse) {
  .icon-btn {
    width: 2.625rem;
    height: 2.625rem;
    border-radius: 12px;
    background: var(--surface);
    border-color: var(--border);
    color: var(--text-muted);
    box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
  }
  .icon-btn svg {
    width: 1.125rem;
    height: 1.125rem;
  }
  .icon-btn:active {
    transform: scale(0.96);
    background: color-mix(in srgb, var(--primary) 10%, var(--surface));
    border-color: color-mix(in srgb, var(--primary) 28%, var(--border));
    color: var(--primary);
    box-shadow: none;
  }
  .icon-btn-danger {
    color: var(--danger-soft-text, var(--danger));
    background: var(--danger-soft, color-mix(in srgb, var(--danger) 10%, var(--surface)));
    border-color: color-mix(in srgb, var(--danger) 22%, var(--border));
  }
  .icon-btn-danger:active {
    background: color-mix(in srgb, var(--danger) 16%, var(--danger-soft, var(--surface)));
    border-color: color-mix(in srgb, var(--danger) 35%, var(--border));
    color: var(--danger);
  }
}
</style>
