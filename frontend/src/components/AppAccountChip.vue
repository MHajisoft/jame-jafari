<script setup>
import { computed } from 'vue'
import { useAuthStore } from '../stores/auth'
import EntityAvatar from './EntityAvatar.vue'

const props = defineProps({
  /** compact = avatar only (mobile top bar); row = avatar + name */
  variant: { type: String, default: 'row' },
  tone: { type: String, default: 'default' },
  subtitle: { type: String, default: '' },
  showChevron: { type: Boolean, default: false }
})

const auth = useAuthStore()

const displayName = computed(() => auth.username?.trim() || 'کاربر')
const avatarSize = computed(() => (props.variant === 'compact' ? 34 : 40))
</script>

<template>
  <router-link
    to="/profile"
    class="account-chip"
    :class="[`variant-${variant}`, `tone-${tone}`]"
    :aria-label="variant === 'compact' ? `پروفایل ${displayName}` : undefined"
  >
    <EntityAvatar
      :src="auth.avatarUrl"
      :name="displayName"
      :size="avatarSize"
    />
    <span v-if="variant !== 'compact'" class="account-text">
      <span class="account-name">{{ displayName }}</span>
      <span v-if="subtitle" class="account-sub">{{ subtitle }}</span>
    </span>
    <span v-if="showChevron" class="account-chevron" aria-hidden="true">‹</span>
  </router-link>
</template>

<style scoped>
.account-chip {
  display: flex;
  align-items: center;
  gap: 0.65rem;
  min-width: 0;
  text-decoration: none;
  color: inherit;
  border-radius: 12px;
  transition: background 0.15s, transform 0.15s;
  -webkit-tap-highlight-color: transparent;
}
.account-chip:active { transform: scale(0.98); }

.variant-row {
  padding: 0.65rem 0.75rem;
  width: 100%;
  box-sizing: border-box;
}
.variant-compact {
  padding: 0.15rem;
  border-radius: 50%;
}

.account-text {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 0.1rem;
}
.account-name {
  font-weight: 700;
  font-size: 0.92rem;
  line-height: 1.25;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.account-sub {
  font-size: 0.78rem;
  line-height: 1.2;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.account-chevron {
  font-size: 1.35rem;
  line-height: 1;
  flex-shrink: 0;
  opacity: 0.55;
}

.tone-default .account-sub { color: var(--text-muted); }
.tone-on-dark {
  color: var(--sidebar-text);
}
.tone-on-dark .account-sub { color: var(--sidebar-muted); }
.tone-on-dark:hover,
.tone-on-dark.router-link-active {
  background: rgba(255, 255, 255, 0.12);
}

.tone-default:hover,
.tone-default.router-link-active {
  background: color-mix(in srgb, var(--primary) 8%, var(--bg));
}

.tone-on-dark :deep(.entity-avatar) {
  background: rgba(255, 255, 255, 0.14);
  color: var(--sidebar-text);
  border-color: rgba(255, 255, 255, 0.2);
}
</style>
