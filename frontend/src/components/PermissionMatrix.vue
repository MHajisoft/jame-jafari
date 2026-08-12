<script setup>
import { computed, nextTick, ref, toRef } from 'vue'
import { useIsMobile } from '../composables/useMediaQuery'
import { CORE_COLUMNS, usePermissionMatrix } from '../composables/usePermissionMatrix'
import AppCheckbox from './AppCheckbox.vue'
import ClearableInput from './ClearableInput.vue'

const props = defineProps({
  permissions: { type: Array, default: () => [] },
  modelValue: { type: Array, default: () => [] }
})

const emit = defineEmits(['update:modelValue'])

const permissionIds = computed({
  get: () => props.modelValue,
  set: v => emit('update:modelValue', v)
})

const permissionsRef = toRef(props, 'permissions')
const isMobile = useIsMobile()
const scrollEl = ref(null)

const {
  search,
  filteredModules,
  rowState,
  isGranted,
  toggleId,
  toggleRow,
  selectAll,
  captureScroll,
  restoreScroll
} = usePermissionMatrix(permissionsRef, permissionIds)

function markSaved() {
  captureScroll(scrollEl.value)
  nextTick(() => restoreScroll(scrollEl.value))
}

defineExpose({ markSaved })
</script>

<template>
  <section class="perm-matrix" aria-labelledby="perm-matrix-title">
    <header class="perm-matrix-head">
      <div class="perm-matrix-head-top">
        <h3 id="perm-matrix-title" class="perm-matrix-title">دسترسی‌ها</h3>
        <div v-if="!isMobile" class="perm-bulk">
          <button type="button" class="btn btn-outline btn-sm" @click="selectAll(true)">
            انتخاب همه
          </button>
          <button type="button" class="btn btn-outline btn-sm" @click="selectAll(false)">
            پاک کردن همه
          </button>
        </div>
      </div>
      <ClearableInput
        v-model="search"
        type="search"
        placeholder="جستجوی ماژول…"
        class="perm-search"
      />
    </header>

    <div
      ref="scrollEl"
      class="perm-cards"
      :class="{ 'perm-cards--mobile': isMobile }"
      @scroll="captureScroll($event.target)"
    >
      <article
        v-for="mod in filteredModules"
        :key="mod.key"
        class="perm-card"
      >
        <header class="perm-card-head">
          <span class="perm-module-icon" aria-hidden="true">{{ mod.icon }}</span>
          <h4 class="perm-card-title">{{ mod.name }}</h4>
          <AppCheckbox
            class="perm-card-head-check"
            :model-value="rowState(mod) === 'all'"
            :indeterminate="rowState(mod) === 'some'"
            :aria-label="`انتخاب همه دسترسی‌های ${mod.name}`"
            @change="toggleRow(mod, $event.target.checked)"
          />
        </header>

        <!-- Desktop: horizontal checkbox row -->
        <div v-if="!isMobile" class="perm-card-row">
          <template v-for="col in CORE_COLUMNS" :key="col.key">
            <label v-if="mod.slots[col.key]" class="perm-chip">
              <AppCheckbox
                :model-value="isGranted(mod.slots[col.key].id)"
                @change="toggleId(mod.slots[col.key].id, $event.target.checked)"
              />
              <span>{{ col.label }}</span>
            </label>
            <span v-else class="perm-chip perm-chip--na" aria-label="غیرقابل اعمال">
              <span class="perm-na">{{ col.label }}</span>
              <span class="perm-dash">—</span>
            </span>
          </template>
          <label
            v-for="ex in mod.extra"
            :key="ex.id"
            class="perm-chip"
          >
            <AppCheckbox
              :model-value="isGranted(ex.id)"
              @change="toggleId(ex.id, $event.target.checked)"
            />
            <span>{{ ex.label }}</span>
          </label>
        </div>

        <!-- Mobile: vertical touch rows -->
        <ul v-else class="perm-mobile-list">
          <li v-for="row in mod.rows.filter(r => r.applicable)" :key="row.key">
            <button
              type="button"
              class="perm-mobile-row"
              :aria-pressed="isGranted(row.id)"
              @click="toggleId(row.id, !isGranted(row.id))"
            >
              <span class="perm-mobile-label">{{ row.label }}</span>
              <span class="perm-mobile-control" aria-hidden="true">
                <AppCheckbox
                  :model-value="isGranted(row.id)"
                  tabindex="-1"
                />
              </span>
            </button>
          </li>
        </ul>
      </article>

      <p v-if="!filteredModules.length" class="perm-empty text-muted">
        ماژولی یافت نشد.
      </p>
    </div>
  </section>
</template>

<style scoped>
.perm-matrix {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
}

.perm-matrix-head {
  position: sticky;
  top: 0;
  z-index: 3;
  padding-bottom: 0.35rem;
  background: linear-gradient(
    to bottom,
    var(--surface) 75%,
    color-mix(in srgb, var(--surface) 40%, transparent)
  );
}

@media (min-width: 769px) {
  .perm-matrix-head {
    position: static;
    background: transparent;
    padding-bottom: 0;
  }
}

.perm-matrix-head-top {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  margin-bottom: 0.65rem;
}

.perm-matrix-title {
  margin: 0;
  font-size: 1.05rem;
  font-weight: 700;
  color: var(--text);
}

.perm-bulk {
  display: flex;
  gap: 0.45rem;
  flex-shrink: 0;
}

.btn-sm {
  min-height: 34px;
  padding: 0.3rem 0.7rem;
  font-size: 0.82rem;
}

.perm-search {
  width: 100%;
}

.perm-cards {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.85rem;
}

.perm-cards--mobile {
  grid-template-columns: 1fr;
}

.perm-card {
  background: var(--surface);
  border: 1px solid color-mix(in srgb, var(--border) 90%, transparent);
  border-radius: var(--radius, 12px);
  box-shadow: var(--shadow);
  padding: 0.85rem 1rem;
  transition: border-color 0.18s, box-shadow 0.18s;
}

.perm-card:hover {
  border-color: color-mix(in srgb, var(--primary) 28%, var(--border));
}

.perm-card-head {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 0.75rem;
  padding-bottom: 0.65rem;
  border-bottom: 1px solid color-mix(in srgb, var(--border) 80%, transparent);
}

.perm-card-head-check {
  flex-shrink: 0;
  margin-inline-start: auto;
}

@media (min-width: 769px) {
  .perm-card-head-check {
    order: -1;
    margin-inline-start: 0;
    margin-inline-end: 0;
  }
}

.perm-module-icon {
  font-size: 1.05rem;
  line-height: 1;
  flex-shrink: 0;
}

.perm-card-title {
  margin: 0;
  flex: 1;
  font-size: 0.95rem;
  font-weight: 700;
  color: var(--text);
  text-align: right;
  min-width: 0;
}

.perm-card-row {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem 0.85rem;
  align-items: center;
}

.perm-chip {
  display: inline-flex;
  align-items: center;
  gap: 0.4rem;
  font-size: 0.84rem;
  font-weight: 500;
  color: var(--text);
  cursor: pointer;
  user-select: none;
}

.perm-chip--na {
  cursor: default;
  color: var(--text-muted);
  gap: 0.35rem;
}

.perm-na {
  font-size: 0.78rem;
  opacity: 0.75;
}

.perm-dash {
  color: var(--text-muted);
  font-size: 0.9rem;
  user-select: none;
}

.perm-mobile-list {
  list-style: none;
  margin: 0;
  padding: 0;
}

.perm-mobile-list > li {
  margin: 0;
  padding: 0;
}

.perm-mobile-row {
  display: grid;
  grid-template-columns: 1fr auto;
  align-items: center;
  gap: 0.75rem;
  width: 100%;
  min-height: 48px;
  padding: 0.35rem 0;
  border: none;
  border-bottom: 1px solid color-mix(in srgb, var(--border) 70%, transparent);
  background: transparent;
  color: inherit;
  font: inherit;
  text-align: inherit;
  cursor: pointer;
  -webkit-tap-highlight-color: transparent;
}

.perm-mobile-row:active {
  background: color-mix(in srgb, var(--primary) 6%, transparent);
}

.perm-mobile-row:focus-visible {
  outline: 2px solid var(--primary);
  outline-offset: -2px;
  border-radius: 6px;
}

.perm-mobile-label {
  font-size: 0.92rem;
  font-weight: 500;
  color: var(--text);
  text-align: start;
}

.perm-mobile-control {
  display: flex;
  align-items: center;
  justify-content: center;
  min-width: 44px;
  min-height: 44px;
  pointer-events: none;
}

.perm-mobile-control :deep(.app-checkbox) {
  margin: 0;
}

.perm-empty {
  grid-column: 1 / -1;
  text-align: center;
  margin: 0.5rem 0;
  font-size: 0.9rem;
}

@media (max-width: 768px) {
  .perm-matrix-head {
    position: static;
    background: transparent;
    padding-bottom: 0;
  }

  .perm-card {
    padding: 1rem;
    box-shadow: 0 1px 2px rgba(15, 41, 32, 0.04), 0 6px 18px rgba(15, 41, 32, 0.06);
  }

  .perm-card-head {
    margin-bottom: 0.35rem;
    padding-bottom: 0.55rem;
  }

  .perm-mobile-list > li:last-child .perm-mobile-row {
    border-bottom: none;
  }
}

@media (min-width: 769px) and (max-width: 1023px) {
  .perm-cards:not(.perm-cards--mobile) {
    grid-template-columns: 1fr;
  }
}
</style>
