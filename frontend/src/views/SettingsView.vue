<script setup>
import { useThemeStore, THEMES } from '../stores/theme'

const theme = useThemeStore()

const themeLabels = {
  light: 'روشن',
  dark: 'تاریک',
  forest: 'جنگلی',
  lemon: 'لیمویی',
  shirazi: 'شیرازی',
  gold: 'طلایی',
  ocean: 'اقیانوس'
}

const dateCultures = [
  { value: 'jalali', label: 'شمسی (جلالی)' },
  { value: 'gregorian', label: 'میلادی' }
]
</script>

<template>
  <div>
    <div class="page-header">
      <h1 class="page-title">تنظیمات</h1>
    </div>

    <div class="card" style="margin-bottom:1.5rem">
      <h3 style="margin-bottom:1rem">تم ظاهری</h3>
      <div class="grid-3">
        <button
          v-for="t in THEMES"
          :key="t"
          class="btn"
          :class="theme.theme === t ? '' : 'btn-outline'"
          @click="theme.setTheme(t)"
        >
          {{ themeLabels[t] }}
        </button>
      </div>
    </div>

    <div class="card">
      <h3 style="margin-bottom:1rem">فرمت تاریخ</h3>
      <div class="form-group">
        <select :value="theme.dateCulture" class="form-control" @change="theme.setDateCulture($event.target.value)">
          <option v-for="c in dateCultures" :key="c.value" :value="c.value">{{ c.label }}</option>
        </select>
      </div>
      <p class="text-muted" style="margin-top:0.5rem;font-size:0.85rem">
        تاریخ‌ها در سراسر برنامه بر اساس فرهنگ انتخابی نمایش داده می‌شوند.
      </p>
    </div>
  </div>
</template>
