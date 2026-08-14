<script setup>
import { computed } from 'vue'
import { toPersianDigits } from '../utils/jalali'

const props = defineProps({
  page: { type: Number, required: true },
  totalPages: { type: Number, required: true },
  totalCount: { type: Number, required: true },
  rangeStart: { type: Number, default: 0 },
  rangeEnd: { type: Number, default: 0 },
  hasPrev: { type: Boolean, default: false },
  hasNext: { type: Boolean, default: false },
  loading: { type: Boolean, default: false }
})

defineEmits(['prev', 'next'])

const summary = computed(() => {
  if (!props.totalCount) return 'موردی یافت نشد'
  return `${fa(props.rangeStart)}–${fa(props.rangeEnd)} از ${fa(props.totalCount)}`
})

const pageLabel = computed(() => `صفحه ${fa(props.page)} از ${fa(props.totalPages)}`)

function fa(n) {
  return toPersianDigits(String(n))
}
</script>

<template>
  <nav class="list-pagination" aria-label="صفحه‌بندی">
    <div class="list-pagination-meta">
      <span class="list-pagination-range">{{ summary }}</span>
      <span class="list-pagination-page">{{ pageLabel }}</span>
    </div>

    <div class="list-pagination-actions">
      <button
        type="button"
        class="list-pagination-nav"
        :disabled="!hasPrev || loading"
        aria-label="صفحه قبل"
        @click="$emit('prev')"
      >
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" aria-hidden="true">
          <path d="M15 6l-6 6 6 6" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
        <span class="list-pagination-nav-text">قبلی</span>
      </button>
      <button
        type="button"
        class="list-pagination-nav"
        :disabled="!hasNext || loading"
        aria-label="صفحه بعد"
        @click="$emit('next')"
      >
        <span class="list-pagination-nav-text">بعدی</span>
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" aria-hidden="true">
          <path d="M9 6l6 6-6 6" stroke-linecap="round" stroke-linejoin="round" />
        </svg>
      </button>
    </div>
  </nav>
</template>

<style scoped>
.list-pagination {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 1rem;
  width: 100%;
}
.list-pagination-meta {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.12rem;
  min-width: 0;
  text-align: start;
}
.list-pagination-range {
  font-size: 0.88rem;
  color: var(--text-muted);
  line-height: 1.3;
}
.list-pagination-page {
  font-size: 0.84rem;
  font-weight: 700;
  color: var(--text);
  line-height: 1.25;
}
.list-pagination-actions {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  flex-shrink: 0;
}
.list-pagination-nav {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.35rem;
  min-height: 38px;
  padding: 0.4rem 0.85rem;
  border: 1px solid var(--border);
  border-radius: 10px;
  background: var(--bg);
  color: var(--text);
  font: inherit;
  font-size: 0.84rem;
  font-weight: 600;
  cursor: pointer;
  white-space: nowrap;
}
.list-pagination-nav svg {
  width: 1.05rem;
  height: 1.05rem;
  flex-shrink: 0;
}
.list-pagination-nav:hover:not(:disabled) {
  border-color: color-mix(in srgb, var(--primary) 45%, var(--border));
  background: color-mix(in srgb, var(--primary) 8%, var(--bg));
  color: var(--primary);
}
.list-pagination-nav:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

@media (min-width: 769px) {
  .list-pagination-actions {
    margin-inline-end: 0;
  }
}

@media (max-width: 768px) {
  .list-pagination {
    flex-direction: column;
    align-items: stretch;
    gap: 0.55rem;
  }
  .list-pagination-meta {
    align-items: center;
    text-align: center;
  }
  .list-pagination-actions {
    justify-content: center;
    width: 100%;
  }
  .list-pagination-nav {
    min-height: 44px;
    padding: 0.45rem 1rem;
    border-radius: 12px;
    background: var(--surface);
  }
}
</style>
