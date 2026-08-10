<script setup>
import { useThemeStore, THEME_OPTIONS } from '../stores/theme'

const theme = useThemeStore()
</script>

<template>
  <div>
    <div class="page-header">
      <h1 class="page-title">تنظیمات</h1>
    </div>

    <div class="card theme-card">
      <div class="theme-card-head">
        <h3>تم ظاهری</h3>
        <p class="text-muted">یکی از تم‌های زیر را برای ظاهر برنامه انتخاب کنید.</p>
      </div>

      <div class="theme-grid" role="listbox" aria-label="انتخاب تم">
        <button
          v-for="opt in THEME_OPTIONS"
          :key="opt.id"
          type="button"
          class="theme-option"
          role="option"
          :aria-selected="theme.theme === opt.id"
          :class="{ active: theme.theme === opt.id }"
          @click="theme.setTheme(opt.id)"
        >
          <div class="theme-swatches" aria-hidden="true">
            <span
              v-for="(color, i) in opt.swatches"
              :key="`${opt.id}-${i}`"
              class="swatch"
              :style="{ background: color }"
            />
          </div>
          <div class="theme-meta">
            <div class="theme-title-row">
              <strong>{{ opt.label }}</strong>
              <span v-if="theme.theme === opt.id" class="theme-check" aria-hidden="true">
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                  <path d="M5 13l4 4L19 7" />
                </svg>
              </span>
            </div>
            <span class="theme-desc">{{ opt.description }}</span>
          </div>
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.theme-card-head {
  margin-bottom: 1.1rem;
}
.theme-card-head h3 {
  margin: 0 0 0.35rem;
  font-size: 1.05rem;
}
.theme-card-head p {
  margin: 0;
  font-size: 0.9rem;
  line-height: 1.5;
}

.theme-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.85rem;
}

.theme-option {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  text-align: right;
  padding: 0.85rem;
  border-radius: 14px;
  border: 1px solid var(--border);
  background: var(--surface);
  color: var(--text);
  cursor: pointer;
  transition: border-color 0.18s, box-shadow 0.18s, transform 0.18s;
}
.theme-option:hover {
  border-color: color-mix(in srgb, var(--primary) 40%, var(--border));
  box-shadow: var(--shadow);
}
.theme-option.active {
  border-color: var(--primary);
  box-shadow: 0 0 0 1px color-mix(in srgb, var(--primary) 55%, transparent), var(--shadow);
  background: color-mix(in srgb, var(--primary) 6%, var(--surface));
}

.theme-swatches {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 0.35rem;
  height: 2.4rem;
}
.swatch {
  display: block;
  border-radius: 8px;
  border: 1px solid color-mix(in srgb, var(--text) 10%, transparent);
}

.theme-meta {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
}
.theme-title-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
}
.theme-check {
  color: var(--primary);
  width: 1.1rem;
  height: 1.1rem;
  display: grid;
  place-items: center;
}
.theme-check svg {
  width: 100%;
  height: 100%;
  display: block;
}
.theme-desc {
  color: var(--text-muted);
  font-size: 0.8rem;
  line-height: 1.45;
}

@media (max-width: 640px) {
  .theme-grid {
    grid-template-columns: 1fr;
  }
}
</style>
