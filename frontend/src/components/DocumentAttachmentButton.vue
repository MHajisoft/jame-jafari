<script setup>
import { computed, ref } from 'vue'
import { documentKind, documentUrl } from '../utils/format'
import DocumentPreview from './DocumentPreview.vue'

const props = defineProps({
  path: { type: String, default: '' }
})

const previewOpen = ref(false)

const url = computed(() => documentUrl(props.path))
const kind = computed(() => documentKind(props.path))
const isThumb = computed(() => kind.value === 'image')
</script>

<template>
  <button
    type="button"
    class="doc-attach-btn"
    :class="{
      'doc-attach-btn--thumb': isThumb,
      [`doc-attach-btn--${kind}`]: !isThumb
    }"
    aria-label="مشاهده پیوست"
    @click="previewOpen = true"
  >
    <img
      v-if="isThumb"
      :src="url"
      alt=""
      class="doc-attach-thumb"
      loading="lazy"
      decoding="async"
    />
    <svg v-else-if="kind === 'pdf'" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
      <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
      <polyline points="14 2 14 8 20 8" />
      <line x1="16" y1="13" x2="8" y2="13" />
      <line x1="16" y1="17" x2="8" y2="17" />
    </svg>
    <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
      <path d="M21.44 11.05l-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48" />
    </svg>
  </button>

  <DocumentPreview v-model:show="previewOpen" :src="url" :kind="kind" />
</template>

<style scoped>
.doc-attach-btn {
  display: inline-grid;
  place-items: center;
  flex-shrink: 0;
  width: 2.5rem;
  height: 2.5rem;
  padding: 0;
  border: 1px solid var(--border);
  border-radius: 10px;
  background: var(--surface);
  color: var(--text-muted);
  cursor: pointer;
  -webkit-tap-highlight-color: transparent;
  touch-action: manipulation;
  transition:
    transform 0.12s ease-out,
    background-color 0.15s ease-out,
    border-color 0.15s ease-out,
    color 0.15s ease-out,
    box-shadow 0.15s ease-out;
}

.doc-attach-btn svg {
  width: 1.125rem;
  height: 1.125rem;
}

.doc-attach-btn--thumb {
  padding: 2px;
  overflow: hidden;
  background: var(--bg);
}

.doc-attach-thumb {
  width: 100%;
  height: 100%;
  object-fit: cover;
  border-radius: 7px;
  display: block;
}

.doc-attach-btn--pdf {
  color: color-mix(in srgb, var(--danger) 72%, var(--text-muted));
  background: color-mix(in srgb, var(--danger-soft, #fee2e2) 55%, var(--surface));
}

.doc-attach-btn:focus-visible {
  outline: 2px solid var(--primary);
  outline-offset: 2px;
}

.doc-attach-btn:active {
  transform: scale(0.96);
  border-color: color-mix(in srgb, var(--primary) 35%, var(--border));
  box-shadow: 0 0 0 3px color-mix(in srgb, var(--primary) 12%, transparent);
}

.doc-attach-btn--thumb:active {
  background: var(--surface);
}

.doc-attach-btn--pdf:active {
  color: var(--danger);
  background: color-mix(in srgb, var(--danger-soft, #fee2e2) 80%, var(--surface));
}

@media (max-width: 768px), (hover: none), (pointer: coarse) {
  .doc-attach-btn {
    width: 2.75rem;
    height: 2.75rem;
    border-radius: 12px;
  }

  .doc-attach-btn svg {
    width: 1.2rem;
    height: 1.2rem;
  }

  .doc-attach-thumb {
    border-radius: 9px;
  }
}

@media (hover: hover) and (pointer: fine) {
  .doc-attach-btn:hover {
    border-color: color-mix(in srgb, var(--primary) 28%, var(--border));
    color: var(--primary);
    background: color-mix(in srgb, var(--primary) 6%, var(--surface));
  }

  .doc-attach-btn--thumb:hover {
    background: color-mix(in srgb, var(--primary) 4%, var(--bg));
  }
}
</style>
