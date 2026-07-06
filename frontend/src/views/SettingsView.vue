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
</script>

<template>
  <div>
    <div class="page-header">
      <h1 class="page-title">تنظیمات</h1>
    </div>

    <div class="card">
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
  </div>
</template>
