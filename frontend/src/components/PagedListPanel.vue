<script setup>
import AppSkeleton from './AppSkeleton.vue'
import ListPagination from './ListPagination.vue'

defineProps({
  loading: { type: Boolean, default: false },
  skeletonColumns: { type: Number, default: 6 },
  showPagination: { type: Boolean, default: false },
  showRefresh: { type: Boolean, default: false },
  page: { type: Number, default: 1 },
  totalPages: { type: Number, default: 1 },
  totalCount: { type: Number, default: 0 },
  rangeStart: { type: Number, default: 0 },
  rangeEnd: { type: Number, default: 0 },
  hasPrev: { type: Boolean, default: false },
  hasNext: { type: Boolean, default: false }
})

defineEmits(['prev', 'next', 'refresh'])
</script>

<template>
  <div class="list-panel card list-panel--paged" :aria-busy="loading">
    <div v-if="showRefresh || $slots.actions" class="list-panel-toolbar">
      <div class="list-panel-toolbar-start">
        <slot name="actions" />
      </div>
      <button
        v-if="showRefresh"
        type="button"
        class="btn btn-outline btn-sm list-refresh-btn"
        :disabled="loading"
        title="به‌روزرسانی"
        aria-label="به‌روزرسانی فهرست"
        @click="$emit('refresh')"
      >
        <span class="list-refresh-icon" aria-hidden="true">↻</span>
        <span class="list-refresh-label">به‌روزرسانی</span>
      </button>
    </div>
    <AppSkeleton v-if="loading" :columns="skeletonColumns" />
    <template v-else>
      <div class="list-panel-body">
        <slot />
      </div>
      <ListPagination
        v-if="showPagination"
        class="list-panel-footer"
        :page="page"
        :total-pages="totalPages"
        :total-count="totalCount"
        :range-start="rangeStart"
        :range-end="rangeEnd"
        :has-prev="hasPrev"
        :has-next="hasNext"
        :loading="loading"
        @prev="$emit('prev')"
        @next="$emit('next')"
      />
    </template>
  </div>
</template>

<style scoped>
.list-panel-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
  padding: 0.65rem 0.85rem 0;
  margin-bottom: 0.15rem;
}

.list-panel-toolbar-start {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  align-items: center;
  min-width: 0;
  flex: 1;
}

.list-refresh-btn {
  display: inline-flex;
  align-items: center;
  gap: 0.35rem;
  flex-shrink: 0;
  margin-inline-start: auto;
}

.list-refresh-icon {
  font-size: 1.05rem;
  line-height: 1;
}

@media (max-width: 768px) {
  .list-panel-toolbar {
    padding: 0.55rem 0.7rem 0;
  }

  .list-refresh-label {
    /* keep label — users need clear re-search affordance on mobile too */
  }
}
</style>
