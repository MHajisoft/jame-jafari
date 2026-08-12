<script setup>
import { usePwaInstall } from '../composables/usePwaInstall'

const { canShowBanner, canPrompt, showIosHint, promptInstall, dismiss } = usePwaInstall()

async function onInstall() {
  await promptInstall()
}
</script>

<template>
  <div v-if="canShowBanner" class="pwa-banner" role="dialog" aria-label="نصب برنامه">
    <div class="pwa-banner-body">
      <div class="pwa-banner-icon" aria-hidden="true">
        <img src="/icons/icon-192.png" alt="موسسه جامعه جعفری" width="40" height="40" />
      </div>
      <div class="pwa-banner-text">
        <strong>نصب روی گوشی</strong>
        <p v-if="canPrompt">برای دسترسی سریع‌تر مثل اپلیکیشن نصب کنید.</p>
        <p v-else-if="showIosHint">
          در Safari روی دکمه Share بزنید و
          <b>Add to Home Screen</b>
          را انتخاب کنید.
        </p>
      </div>
      <div class="pwa-banner-actions">
        <button v-if="canPrompt" type="button" class="btn btn-sm" @click="onInstall">نصب</button>
        <button type="button" class="btn btn-sm btn-outline" @click="dismiss" aria-label="بستن">بعداً</button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.pwa-banner {
  position: fixed;
  left: 0.75rem;
  right: 0.75rem;
  bottom: calc(72px + env(safe-area-inset-bottom, 0));
  z-index: 260;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 16px;
  box-shadow: 0 8px 28px rgba(0, 0, 0, 0.14);
  padding: 0.85rem;
}
.pwa-banner-body {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}
.pwa-banner-icon {
  flex-shrink: 0;
  width: 40px;
  height: 40px;
  border-radius: 10px;
  overflow: hidden;
}
.pwa-banner-icon img {
  display: block;
  width: 100%;
  height: 100%;
  object-fit: cover;
}
.pwa-banner-text {
  flex: 1;
  min-width: 0;
}
.pwa-banner-text strong {
  display: block;
  font-size: 0.92rem;
  margin-bottom: 0.15rem;
}
.pwa-banner-text p {
  margin: 0;
  color: var(--text-muted);
  font-size: 0.78rem;
  line-height: 1.45;
}
.pwa-banner-actions {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  flex-shrink: 0;
}
.pwa-banner-actions .btn {
  min-height: 36px;
  padding-inline: 0.85rem;
  white-space: nowrap;
}

@media (min-width: 769px) {
  .pwa-banner {
    left: auto;
    right: 1.25rem;
    bottom: 1.25rem;
    width: min(420px, calc(100vw - 2.5rem));
  }
  .pwa-banner-actions {
    flex-direction: row;
  }
}
</style>
