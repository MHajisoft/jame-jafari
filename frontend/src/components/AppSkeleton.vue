<script setup>
defineProps({
  /** Table row placeholders */
  rows: { type: Number, default: 6 },
  /** Visible table columns (including actions if shown) */
  columns: { type: Number, default: 4 },
  /** 'table' | 'lines' */
  variant: { type: String, default: 'table' }
})
</script>

<template>
  <div class="app-skeleton" aria-hidden="true">
    <div v-if="variant === 'lines'" class="skeleton-lines">
      <span v-for="n in rows" :key="n" class="skeleton-line" :style="{ width: n === rows ? '62%' : '100%' }" />
    </div>
    <table v-else class="skeleton-table mobile-table">
      <thead>
        <tr>
          <th v-for="c in columns" :key="`h-${c}`">
            <span class="skeleton-block skeleton-block--sm" />
          </th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="r in rows" :key="r">
          <td v-for="c in columns" :key="`${r}-${c}`" data-label="">
            <span
              class="skeleton-block"
              :class="{ 'skeleton-block--sm': c === columns }"
              :style="{ width: c === 1 ? '72%' : c === columns ? '48%' : '86%' }"
            />
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.app-skeleton {
  pointer-events: none;
}

.skeleton-table {
  width: 100%;
}

.skeleton-block {
  display: inline-block;
  height: 0.95rem;
  border-radius: 6px;
  background: linear-gradient(
    90deg,
    color-mix(in srgb, var(--border) 70%, transparent) 0%,
    color-mix(in srgb, var(--border) 35%, var(--surface)) 50%,
    color-mix(in srgb, var(--border) 70%, transparent) 100%
  );
  background-size: 200% 100%;
  animation: skeleton-shimmer 1.2s ease-in-out infinite;
}

.skeleton-block--sm {
  height: 0.82rem;
}

.skeleton-lines {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  padding: 0.5rem 0;
}

.skeleton-line {
  display: block;
  height: 0.95rem;
  border-radius: 6px;
  background: linear-gradient(
    90deg,
    color-mix(in srgb, var(--border) 70%, transparent) 0%,
    color-mix(in srgb, var(--border) 35%, var(--surface)) 50%,
    color-mix(in srgb, var(--border) 70%, transparent) 100%
  );
  background-size: 200% 100%;
  animation: skeleton-shimmer 1.2s ease-in-out infinite;
}

@keyframes skeleton-shimmer {
  0% { background-position: 100% 0; }
  100% { background-position: -100% 0; }
}
</style>
