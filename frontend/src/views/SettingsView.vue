<script setup>
import { computed, onMounted, ref } from 'vue'
import api from '../api/client'
import { ApiPaths } from '../api/paths'
import { useAuthStore } from '../stores/auth'
import { useThemeStore, THEME_OPTIONS } from '../stores/theme'
import {
  useUiPrefsStore,
  CURRENCY_DISPLAY_OPTIONS,
  DATE_PICKER_MOBILE_MODES,
  TABLE_LABEL_MODES,
  ALL_MESSENGER_KINDS
} from '../stores/uiPrefs'
import { enumValue, messengerKinds, messengerKindLabel } from '../utils/format'
import { useIsMobile } from '../composables/useMediaQuery'
import { usePwaInstall } from '../composables/usePwaInstall'
import { useFormValidation } from '../composables/useFormValidation'
import MessengerKindIcon from '../components/MessengerKindIcon.vue'

const theme = useThemeStore()
const uiPrefs = useUiPrefsStore()
const auth = useAuthStore()
const isMobile = useIsMobile()
const { trySubmit } = useFormValidation()
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

const canManageMessaging = computed(
  () => auth.hasPermission('messages.view') || auth.hasPermission('messages.send')
)
const canSendMessages = computed(() => auth.hasPermission('messages.send'))

const messagingConfig = ref({
  isConfigured: false,
  availableMessengers: [],
  canRegisterWebhooks: false
})

/** Static setup steps when bot token is missing from appsettings / .env. */
const MESSENGER_SETUP = {
  1: {
    env: 'BALE_BOT_TOKEN',
    appsettings: 'Bale:BotToken',
    extra: 'اختیاری: BALE_BOT_USERNAME و BALE_WEBHOOK_SECRET'
  },
  2: {
    env: 'RUBIKA_BOT_TOKEN',
    appsettings: 'Rubika:BotToken',
    extra: 'اختیاری: RUBIKA_BOT_USERNAME و RUBIKA_WEBHOOK_SECRET'
  },
  3: {
    env: 'TELEGRAM_BOT_TOKEN',
    appsettings: 'Telegram:BotToken',
    extra: 'اختیاری: TELEGRAM_BOT_USERNAME و TELEGRAM_WEBHOOK_SECRET'
  }
}

const messengerRows = computed(() => {
  const byKind = new Map(
    (messagingConfig.value.availableMessengers || []).map((m) => [
      enumValue(messengerKinds, m.kind, 0),
      m
    ])
  )
  return ALL_MESSENGER_KINDS.map((kind) => {
    const info = byKind.get(kind)
    const setup = MESSENGER_SETUP[kind]
    return {
      kind,
      label: info?.label || messengerKindLabel(kind),
      isConfigured: !!info?.isConfigured,
      enabled: uiPrefs.isMessengerEnabled(kind),
      setup
    }
  })
})

async function loadMessagingConfig() {
  if (!canManageMessaging.value) return
  try {
    const { data } = await api.get(ApiPaths.messagesConfig, { skipGlobalLoader: true })
    messagingConfig.value = data
  } catch {
    messagingConfig.value = {
      isConfigured: false,
      availableMessengers: [],
      canRegisterWebhooks: false
    }
  }
}

function onMessengerToggle(row, event) {
  if (!row.isConfigured) {
    event.target.checked = false
    return
  }
  uiPrefs.setMessengerEnabled(row.kind, event.target.checked)
}

async function syncContacts() {
  const ok = await trySubmit(async () => {
    await api.post(ApiPaths.messagesSyncContacts, null, {
      loaderMessage: 'در حال همگام‌سازی…'
    })
  }, { successMessage: 'همگام‌سازی مخاطبین انجام شد' })
  if (!ok) return
  await loadMessagingConfig()
}

async function registerWebhooks() {
  const ok = await trySubmit(async () => {
    await api.post(ApiPaths.messagesRegisterWebhooks, null, {
      loaderMessage: 'در حال ثبت وب‌هوک…'
    })
  }, { successMessage: 'وب‌هوک پیام‌رسان ثبت شد' })
  if (!ok) return
  await loadMessagingConfig()
}

async function installApp() {
  await promptInstall()
}

onMounted(() => {
  loadMessagingConfig()
})
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

    <div class="card currency-card">
      <div class="theme-card-head">
        <h3>واحد پول</h3>
        <p class="text-muted">مبالغ در سیستم همیشه به ریال ذخیره می‌شوند.</p>
      </div>
      <div
        class="datepicker-mode-grid"
        role="listbox"
        aria-label="واحد نمایش پول"
      >
        <button
          v-for="opt in CURRENCY_DISPLAY_OPTIONS"
          :key="opt.id"
          type="button"
          class="datepicker-mode-option"
          role="option"
          :aria-selected="uiPrefs.currencyDisplayUnit === opt.id"
          :class="{ active: uiPrefs.currencyDisplayUnit === opt.id }"
          @click="uiPrefs.setCurrencyDisplayUnit(opt.id)"
        >
          <strong>{{ opt.label }}</strong>
          <span class="text-muted">{{ opt.hint }}</span>
        </button>
      </div>
    </div>

    <div class="card table-label-card">
      <div class="theme-card-head">
        <h3>نمایش نمادین جداول</h3>
        <p class="text-muted">برای ستون‌هایی مثل پیام‌رسان، نوع و وضعیت. در حالت فقط آیکون، این‌ها در مرکز پیام در یک ردیف/ستون «مشخصات» جمع می‌شوند.</p>
      </div>
      <div
        class="datepicker-mode-grid"
        role="listbox"
        aria-label="حالت نمایش نمادین جداول"
      >
        <button
          v-for="opt in TABLE_LABEL_MODES"
          :key="opt.id"
          type="button"
          class="datepicker-mode-option"
          role="option"
          :aria-selected="uiPrefs.tableLabelMode === opt.id"
          :class="{ active: uiPrefs.tableLabelMode === opt.id }"
          @click="uiPrefs.setTableLabelMode(opt.id)"
        >
          <strong>{{ opt.label }}</strong>
          <span class="text-muted">{{ opt.hint }}</span>
        </button>
      </div>
    </div>

    <div v-if="canManageMessaging" class="card messengers-card">
      <div class="theme-card-head">
        <h3>پیام‌رسان‌ها</h3>
        <p class="text-muted">
          انتخاب کنید کدام پیام‌رسان در مرکز پیام و ارسال رسید درآمد استفاده شود.
          فعال‌سازی فقط وقتی توکن در سرور تنظیم شده باشد ممکن است.
        </p>
      </div>

      <ul class="messenger-pref-list" role="list">
        <li v-for="row in messengerRows" :key="row.kind" class="messenger-pref-row">
          <div class="messenger-pref-main">
            <MessengerKindIcon :kind="row.kind" :size="22" :title="row.label" />
            <div class="messenger-pref-text">
              <strong>{{ row.label }}</strong>
              <span v-if="row.isConfigured" class="messenger-pref-status ok">پیکربندی‌شده</span>
              <span v-else class="messenger-pref-status warn">توکن تنظیم نشده</span>
            </div>
            <label class="messenger-pref-switch">
              <input
                type="checkbox"
                :checked="row.enabled && row.isConfigured"
                :disabled="!row.isConfigured"
                :aria-label="`فعال‌سازی ${row.label}`"
                @change="onMessengerToggle(row, $event)"
              />
              <span class="switch-ui" aria-hidden="true" />
            </label>
          </div>
          <p v-if="!row.isConfigured" class="field-hint messenger-setup-hint">
            توکن را در محیط اجرا بگذارید:
            <code dir="ltr">{{ row.setup.env }}</code>
            یا در
            <code dir="ltr">appsettings.Development.local.json</code>
            کلید
            <code dir="ltr">{{ row.setup.appsettings }}</code>.
            {{ row.setup.extra }}.
            سپس API را دوباره راه‌اندازی کنید.
          </p>
        </li>
      </ul>

      <div v-if="canSendMessages" class="messenger-ops">
        <div class="theme-card-head messenger-ops-head">
          <h4>اتصال مخاطبین (وب‌هوک)</h4>
          <p class="text-muted">
            مسیر اصلی: پس از تنظیم
            <code>MESSAGING_PUBLIC_BASE_URL</code>
            (HTTPS عمومی)، «ثبت وب‌هوک» را بزنید تا
            <code>/start</code>
            فوری دکمه اشتراک موبایل را بفرستد. همگام‌سازی فقط پشتیبان است.
          </p>
        </div>
        <p v-if="!messagingConfig.canRegisterWebhooks" class="field-hint messenger-setup-hint">
          آدرس عمومی HTTPS تنظیم نشده است. در
          <code>.env</code>
          متغیر
          <code>MESSAGING_PUBLIC_BASE_URL</code>
          یا در appsettings کلید
          <code dir="ltr">Messaging:PublicBaseUrl</code>
          را بگذارید (مثلاً
          <code dir="ltr">https://app.example.com</code>
          بدون اسلش پایانی)، سپس API را راه‌اندازی کنید.
        </p>
        <div class="messenger-ops-actions">
          <button
            type="button"
            class="btn btn-primary btn-sm"
            :disabled="!messagingConfig.canRegisterWebhooks"
            :title="messagingConfig.canRegisterWebhooks ? undefined : 'Messaging:PublicBaseUrl باید HTTPS عمومی باشد'"
            @click="registerWebhooks"
          >
            ثبت وب‌هوک
          </button>
          <button type="button" class="btn btn-outline btn-sm" @click="syncContacts">
            همگام‌سازی مخاطبین
          </button>
        </div>
        <p v-if="messagingConfig.baleWebhookUrl || messagingConfig.rubikaWebhookUrl || messagingConfig.telegramWebhookUrl" class="field-hint webhook-urls">
          <template v-if="messagingConfig.baleWebhookUrl">
            بله: <code dir="ltr">{{ messagingConfig.baleWebhookUrl }}</code>
            <br />
          </template>
          <template v-if="messagingConfig.rubikaWebhookUrl">
            روبیکا: <code dir="ltr">{{ messagingConfig.rubikaWebhookUrl }}</code>
            <br />
          </template>
          <template v-if="messagingConfig.telegramWebhookUrl">
            تلگرام: <code dir="ltr">{{ messagingConfig.telegramWebhookUrl }}</code>
          </template>
        </p>
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

.table-label-card {
  margin-top: 1rem;
}
.messengers-card {
  margin-top: 1rem;
}
.messenger-pref-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}
.messenger-pref-row {
  padding: 0.75rem 0.85rem;
  border-radius: 12px;
  border: 1px solid var(--border);
  background: var(--bg);
}
.messenger-pref-main {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}
.messenger-pref-text {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  text-align: start;
}
.messenger-pref-text strong {
  font-size: 0.95rem;
}
.messenger-pref-status {
  font-size: 0.78rem;
  line-height: 1.4;
}
.messenger-pref-status.ok {
  color: var(--success-soft-text, var(--primary));
}
.messenger-pref-status.warn {
  color: color-mix(in srgb, var(--warning, #c9a227) 85%, var(--text));
}
.messenger-pref-switch {
  position: relative;
  display: inline-flex;
  width: 2.75rem;
  height: 1.55rem;
  flex-shrink: 0;
  cursor: pointer;
}
.messenger-pref-switch input {
  position: absolute;
  inset: 0;
  opacity: 0;
  margin: 0;
  cursor: inherit;
}
.messenger-pref-switch input:disabled {
  cursor: not-allowed;
}
.switch-ui {
  display: block;
  width: 100%;
  height: 100%;
  border-radius: 999px;
  background: color-mix(in srgb, var(--text) 18%, var(--surface));
  border: 1px solid var(--border);
  transition: background 0.15s, border-color 0.15s;
}
.switch-ui::after {
  content: '';
  position: absolute;
  top: 0.18rem;
  inset-inline-start: 0.18rem;
  width: 1.1rem;
  height: 1.1rem;
  border-radius: 999px;
  background: var(--surface);
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.18);
  transition: inset-inline-start 0.15s;
}
.messenger-pref-switch input:checked + .switch-ui {
  background: var(--primary);
  border-color: var(--primary);
}
.messenger-pref-switch input:checked + .switch-ui::after {
  inset-inline-start: calc(100% - 1.28rem);
}
.messenger-pref-switch input:disabled + .switch-ui {
  opacity: 0.55;
}
.messenger-pref-switch input:focus-visible + .switch-ui {
  outline: 2px solid color-mix(in srgb, var(--primary) 55%, transparent);
  outline-offset: 2px;
}
.messenger-setup-hint {
  margin: 0.55rem 0 0;
  line-height: 1.55;
}
.messenger-ops {
  margin-top: 1.15rem;
  padding-top: 1rem;
  border-top: 1px solid var(--border);
}
.messenger-ops-head h4 {
  margin: 0;
  font-size: 0.98rem;
}
.messenger-ops-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.55rem;
  margin-top: 0.75rem;
}
.webhook-urls {
  margin: 0.65rem 0 0;
  line-height: 1.55;
}

.datepicker-card {
  margin-top: 1rem;
}
.currency-card {
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
