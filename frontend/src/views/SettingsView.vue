<script setup>
import { useThemeStore, THEME_OPTIONS } from '../stores/theme'
import { useUiPrefsStore, DATE_PICKER_MOBILE_MODES } from '../stores/uiPrefs'
import { useIsMobile } from '../composables/useMediaQuery'
import { usePwaInstall } from '../composables/usePwaInstall'

const theme = useThemeStore()
const uiPrefs = useUiPrefsStore()
const isMobile = useIsMobile()
const {
  standalone,
  ios,
  android,
  needsHttps,
  swRegistered,
  canPrompt,
  showIosHint,
  promptInstall
} = usePwaInstall()

async function installApp() {
  await promptInstall()
}
</script>

<template>
  <div>
    <div class="page-header">
      <h1 class="page-title">تنظیمات</h1>
    </div>

    <div class="card theme-card">
      <div class="theme-card-head">
        <h3>تم ظاهری</h3>
      </div>

      <div class="theme-grid" role="listbox" aria-label="انتخاب تم">
        <button
          v-for="opt in THEME_OPTIONS"
          :key="opt.id"
          type="button"
          class="theme-option"
          role="option"
          :aria-selected="theme.theme === opt.id"
          :aria-label="opt.label"
          :class="{ active: theme.theme === opt.id }"
          @click="theme.setTheme(opt.id)"
        >
          <div
            class="theme-preview"
            aria-hidden="true"
            :style="{ background: opt.swatches[3] }"
          >
            <span class="preview-sidebar" :style="{ background: opt.swatches[0] }" />
            <span class="preview-body">
              <span class="preview-bar" :style="{ background: opt.swatches[1] }" />
              <span class="preview-panel" :style="{ background: opt.swatches[2] }" />
            </span>
            <span v-if="theme.theme === opt.id" class="theme-check">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.6" stroke-linecap="round" stroke-linejoin="round">
                <path d="M5 13l4 4L19 7" />
              </svg>
            </span>
          </div>
          <strong class="theme-label">{{ opt.label }}</strong>
        </button>
      </div>
    </div>

    <div v-if="isMobile" class="card datepicker-card">
      <div class="theme-card-head">
        <h3>نمایش انتخابگر تاریخ</h3>
        <p class="text-muted">فقط در حالت موبایل؛ دسکتاپ همیشه تقویم مودال است.</p>
      </div>
      <div
        class="datepicker-mode-grid"
        role="listbox"
        aria-label="حالت انتخابگر تاریخ"
      >
        <button
          v-for="opt in DATE_PICKER_MOBILE_MODES"
          :key="opt.id"
          type="button"
          class="datepicker-mode-option"
          role="option"
          :aria-selected="uiPrefs.datePickerMobileMode === opt.id"
          :class="{ active: uiPrefs.datePickerMobileMode === opt.id }"
          @click="uiPrefs.setDatePickerMobileMode(opt.id)"
        >
          <strong>{{ opt.label }}</strong>
          <span class="text-muted">{{ opt.hint }}</span>
        </button>
      </div>
    </div>

    <div class="card pwa-card">
      <div class="theme-card-head">
        <h3>نصب روی موبایل (PWA)</h3>
        <p class="text-muted">
          برنامه را مثل اپلیکیشن اندروید/iOS روی صفحه اصلی گوشی نصب کنید.
        </p>
      </div>

      <div v-if="standalone" class="pwa-status success">
        در حال اجرا به‌صورت برنامه نصب‌شده هستید (بدون نوار آدرس Chrome).
      </div>
      <div v-else class="pwa-install-block">
        <div v-if="needsHttps" class="pwa-status warn">
          <strong>HTTPS لازم است.</strong>
          اگر سایت را با <code>http://</code> (مثلاً IP یا پورت 8080 بدون SSL) باز کرده‌اید،
          «افزودن به صفحه اصلی» فقط یک میانبر Chrome می‌سازد — با نوار آدرس و تب.
          برای حالت تمام‌صفحه مثل اپ، سایت باید روی <strong>HTTPS</strong> (دامنه + گواهی) مستقر شود،
          سپس از دکمه «نصب برنامه» یا Install app در Chrome نصب کنید.
        </div>
        <button
          v-if="canPrompt"
          type="button"
          class="btn"
          @click="installApp"
        >
          نصب برنامه
        </button>
        <div v-else-if="showIosHint" class="pwa-ios-steps">
          <ol>
            <li>در Safari دکمه Share را بزنید.</li>
            <li>گزینه <strong>Add to Home Screen</strong> را انتخاب کنید.</li>
            <li>روی Add بزنید تا آیکون «جامعه جعفری» روی صفحه اصلی بیاید.</li>
          </ol>
        </div>
        <div v-else-if="android && !needsHttps" class="pwa-android-steps">
          <ol>
            <li>چند ثانیه صبر کنید تا صفحه کامل بارگذاری شود.</li>
            <li>منوی Chrome (⋮) → <strong>Install app</strong> / <strong>نصب برنامه</strong>.</li>
            <li>آیکون جدید را از صفحه اصلی باز کنید — نه از تب Chrome.</li>
          </ol>
          <p v-if="!swRegistered" class="text-muted pwa-fallback">
            سرویس‌ورکر هنوز فعال نشده؛ یک بار صفحه را رفرش کنید و دوباره نصب کنید.
          </p>
        </div>
        <p v-else class="text-muted pwa-fallback">
          در Chrome اندروید (روی HTTPS) از منوی مرورگر گزینه «Install app» / «نصب برنامه» را بزنید.
          برای iOS از Safari استفاده کنید.
        </p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.theme-card-head {
  margin-bottom: 1rem;
}
.theme-card-head h3 {
  margin: 0;
  font-size: 1.05rem;
}
.theme-card-head p {
  margin: 0.35rem 0 0;
  font-size: 0.9rem;
  line-height: 1.5;
}

.theme-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 0.75rem;
}

.theme-option {
  display: flex;
  flex-direction: column;
  gap: 0.55rem;
  text-align: center;
  padding: 0;
  border: none;
  background: transparent;
  color: var(--text);
  cursor: pointer;
  -webkit-tap-highlight-color: transparent;
}
.theme-preview {
  position: relative;
  display: flex;
  height: 4.75rem;
  border-radius: 14px;
  overflow: hidden;
  border: 1px solid var(--border);
  box-shadow: var(--shadow);
  transition: border-color 0.18s, box-shadow 0.18s, transform 0.15s;
}
.preview-sidebar {
  width: 28%;
  flex-shrink: 0;
}
.preview-body {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.4rem;
  padding: 0.55rem 0.5rem;
  min-width: 0;
}
.preview-bar {
  height: 0.55rem;
  width: 58%;
  border-radius: 999px;
  margin-inline-start: auto;
  opacity: 0.95;
}
.preview-panel {
  flex: 1;
  border-radius: 8px;
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.35);
  opacity: 0.92;
}
.theme-label {
  font-size: 0.88rem;
  font-weight: 700;
  line-height: 1.2;
}
.theme-check {
  position: absolute;
  inset-inline-start: 0.45rem;
  bottom: 0.45rem;
  width: 1.35rem;
  height: 1.35rem;
  border-radius: 999px;
  display: grid;
  place-items: center;
  background: var(--primary);
  color: var(--on-primary);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.18);
}
.theme-check svg {
  width: 0.78rem;
  height: 0.78rem;
  display: block;
}

@media (hover: hover) and (pointer: fine) {
  .theme-option:hover .theme-preview {
    border-color: color-mix(in srgb, var(--primary) 45%, var(--border));
    transform: translateY(-1px);
  }
}
.theme-option.active .theme-preview {
  border-color: var(--primary);
  box-shadow:
    0 0 0 2px color-mix(in srgb, var(--primary) 35%, transparent),
    var(--shadow),
    var(--glow-primary, none);
}
.theme-option.active .theme-label {
  color: var(--primary);
}
.theme-option:active .theme-preview {
  transform: scale(0.98);
}

@media (max-width: 768px) {
  .theme-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 0.7rem;
  }
  .theme-preview {
    height: 4.35rem;
  }
}

.datepicker-card {
  margin-top: 1rem;
}
.datepicker-mode-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.65rem;
}
.datepicker-mode-option {
  display: flex;
  flex-direction: column;
  gap: 0.3rem;
  text-align: start;
  padding: 0.85rem 0.9rem;
  border-radius: 12px;
  border: 1px solid var(--border);
  background: var(--bg);
  color: var(--text);
  cursor: pointer;
  -webkit-tap-highlight-color: transparent;
  min-height: 44px;
  transition: border-color 0.15s, box-shadow 0.15s;
}
.datepicker-mode-option strong {
  font-size: 0.95rem;
}
.datepicker-mode-option span {
  font-size: 0.8rem;
  line-height: 1.45;
}
.datepicker-mode-option.active {
  border-color: var(--primary);
  box-shadow:
    0 0 0 2px color-mix(in srgb, var(--primary) 30%, transparent),
    var(--shadow);
}
.datepicker-mode-option.active strong {
  color: var(--primary);
}
.datepicker-mode-option:active {
  transform: scale(0.98);
}

.pwa-card {
  margin-top: 1rem;
}
.pwa-install-block {
  display: flex;
  flex-direction: column;
  gap: 0.85rem;
}
.pwa-status.success,
.pwa-status.warn {
  padding: 0.75rem 0.9rem;
  border-radius: 10px;
  font-size: 0.9rem;
  line-height: 1.55;
}
.pwa-status.success {
  background: var(--success-soft);
  color: var(--success-soft-text);
}
.pwa-status.warn {
  background: color-mix(in srgb, var(--warning, #c9a227) 14%, var(--surface));
  border: 1px solid color-mix(in srgb, var(--warning, #c9a227) 35%, var(--border));
  color: var(--text);
}
.pwa-status.warn code {
  font-size: 0.85em;
}
.pwa-install-block .btn {
  width: 100%;
  justify-content: center;
  min-height: 44px;
}
.pwa-ios-steps ol,
.pwa-android-steps ol {
  margin: 0;
  padding-right: 1.2rem;
  color: var(--text);
  font-size: 0.9rem;
  line-height: 1.7;
}
.pwa-fallback {
  font-size: 0.9rem;
  line-height: 1.55;
  margin: 0;
}
</style>
