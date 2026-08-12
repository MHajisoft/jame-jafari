<script setup>
defineProps({
  /** sm = top bar, md = sidebar, lg = hero */
  size: { type: String, default: 'md' },
  showTitle: { type: Boolean, default: true },
  showTagline: { type: Boolean, default: false },
  title: { type: String, default: 'موسسه جامعه جعفری' },
  tagline: { type: String, default: 'سامانه مدیریت مالی' },
  /** on-dark = sidebar; on-surface = top bar / cards */
  tone: { type: String, default: 'on-dark' },
  to: { type: String, default: '' }
})
</script>

<template>
  <component
    :is="to ? 'router-link' : 'div'"
    :to="to || undefined"
    class="brand-mark"
    :class="[`size-${size}`, `tone-${tone}`]"
    :aria-label="to ? title : undefined"
  >
    <div class="brand-logo" aria-hidden="true">
      <img src="/logo.png" alt="" />
    </div>
    <div v-if="showTitle || showTagline" class="brand-copy">
      <strong v-if="showTitle" class="brand-title">{{ title }}</strong>
      <span v-if="showTagline" class="brand-tagline">{{ tagline }}</span>
    </div>
  </component>
</template>

<style scoped>
.brand-mark {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  min-width: 0;
  text-decoration: none;
  color: inherit;
}
.brand-mark[href] {
  transition: background 0.15s, opacity 0.15s;
}
.size-sm { gap: 0.35rem; }
.size-lg { gap: 0.9rem; }

.brand-logo {
  flex-shrink: 0;
  border-radius: 12px;
  overflow: hidden;
  background: rgba(255, 255, 255, 0.08);
  box-shadow: 0 0 0 1px rgba(255, 255, 255, 0.12);
}
.size-sm .brand-logo {
  width: 36px;
  height: 36px;
  border-radius: 10px;
  box-shadow: 0 0 0 1px color-mix(in srgb, var(--border) 80%, transparent);
  background: var(--bg-elevated);
}
.size-md .brand-logo { width: 46px; height: 46px; }
.size-lg .brand-logo {
  width: 64px;
  height: 64px;
  border-radius: 16px;
  box-shadow: 0 4px 14px rgba(0, 0, 0, 0.12);
}

.brand-logo img {
  width: 100%;
  height: 100%;
  object-fit: cover;
  display: block;
}

.brand-copy {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  min-width: 0;
}
.brand-title {
  font-size: 0.95rem;
  line-height: 1.3;
  font-weight: 700;
}
.size-lg .brand-title { font-size: 1.05rem; }
.brand-tagline {
  font-size: 0.85rem;
  line-height: 1.25;
}

.tone-on-dark .brand-tagline { color: var(--sidebar-muted); }
.tone-on-surface .brand-tagline { color: var(--text-muted); }

.size-sm .brand-copy { display: none; }
</style>
