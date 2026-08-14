<script setup>
import AppSkeleton from './AppSkeleton.vue'
import ListPagination from './ListPagination.vue'

defineProps({
  loading: { type: Boolean, default: false },
  skeletonColumns: { type: Number, default: 6 },
  showPagination: { type: Boolean, default: false },
  page: { type: Number, default: 1 },
  totalPages: { type: Number, default: 1 },
  totalCount: { type: Number, default: 0 },
  rangeStart: { type: Number, default: 0 },
  rangeEnd: { type: Number, default: 0 },
  hasPrev: { type: Boolean, default: false },
  hasNext: { type: Boolean, default: false }
})

defineEmits(['prev', 'next'])
</script>

<template>
  <div class="list-panel card list-panel--paged" :aria-busy="loading">
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
