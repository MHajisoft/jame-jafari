<script setup>
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import api from '../api/client'
import { ApiPaths } from '../api/paths'
import {
  baleMessageStatusLabel,
  baleMessageTypeLabel,
  documentUrl,
  enumValue,
  isBaleMessageType,
  messengerKindLabel,
  messengerKinds
} from '../utils/format'
import { useAuthStore } from '../stores/auth'
import { useDialogStore } from '../stores/dialog'
import { useToastStore } from '../stores/toast'
import { useFormValidation } from '../composables/useFormValidation'
import { useEntityForm } from '../composables/useEntityForm'
import { useIsMobile } from '../composables/useMediaQuery'
import { usePagedList } from '../composables/usePagedList'
import AppSelect from '../components/AppSelect.vue'
import FormHost from '../components/FormHost.vue'
import RowActions from '../components/RowActions.vue'
import PersonCell from '../components/PersonCell.vue'
import PageHeader from '../components/PageHeader.vue'
import PagedListPanel from '../components/PagedListPanel.vue'
import DateDisplay from '../components/DateDisplay.vue'
import PersonSelect from '../components/PersonSelect.vue'
import ClearableInput from '../components/ClearableInput.vue'

const auth = useAuthStore()
const dialog = useDialogStore()
const isMobile = useIsMobile()
const { error, errors, validate, trySubmit, clearErrors } = useFormValidation()

const config = ref({ isConfigured: false, availableMessengers: [] })
const channels = ref([])
const personGroups = ref([])
const attachmentFiles = ref([])
const attachmentInput = ref(null)
const attachmentAccept = ref('image/*')
const openErrorId = ref(null)
const errorFlyout = ref(null)

const targetTypeOptions = [
  { value: 1, label: 'کانال / گروه' },
  { value: 2, label: 'گروه اشخاص' },
  { value: 3, label: 'شخص' }
]

const messengerOptions = computed(() =>
  (config.value.availableMessengers || [])
    .filter((m) => m.isConfigured)
    .map((m) => ({
      value: enumValue(messengerKinds, m.kind, 1),
      label: m.label || messengerKindLabel(m.kind)
    }))
)

const channelOptions = computed(() =>
  channels.value.map((c) => ({ value: c.id, label: c.name }))
)

const personGroupOptions = computed(() =>
  personGroups.value.map((g) => ({
    value: g.id,
    label: `${g.name} (${g.memberCount} نفر)`
  }))
)

const attachmentKindOptions = [
  { key: 'image', label: 'تصویر', accept: 'image/*,.jpg,.jpeg,.png,.gif,.webp,.bmp,.heic,.heif' },
  { key: 'video', label: 'ویدیو', accept: 'video/*,.mp4,.mov,.avi,.mkv,.webm,.m4v' },
  { key: 'audio', label: 'صوت', accept: 'audio/*,.mp3,.ogg,.wav,.m4a,.aac,.flac' },
  { key: 'file', label: 'فایل', accept: '.pdf,.doc,.docx,.xls,.xlsx' }
]

function blankForm() {
  const defaultMessenger = messengerOptions.value[0]?.value ?? 1
  return {
    targetType: 1,
    messenger: defaultMessenger,
    messageChannelId: '',
    personGroupId: '',
    personId: '',
    text: '',
    caption: ''
  }
}

const { showForm, editing, form, openEdit, closeForm, resetForm } = useEntityForm(blankForm, {
  onReset: () => {
    attachmentFiles.value = []
    if (attachmentInput.value) attachmentInput.value.value = ''
    clearErrors()
  }
})

const {
  items,
  loading,
  page,
  totalPages,
  totalCount,
  hasPrev,
  hasNext,
  showPagination,
  rangeStart,
  rangeEnd,
  load,
  goPrev,
  goNext,
  reload
} = usePagedList(async ({ page, pageSize }) => {
  const { data } = await api.get(ApiPaths.baleMessages, { params: { page, pageSize } })
  return data
})

const pageTitle = computed(() => {
  if (showForm.value && !isMobile.value) {
    return editing.value ? 'ویرایش پیام' : 'پیام جدید'
  }
  return 'مرکز پیام'
})

const isChannelTarget = computed(() => Number(form.value.targetType) === 1)
const isPersonGroupTarget = computed(() => Number(form.value.targetType) === 2)
const isPersonTarget = computed(() => Number(form.value.targetType) === 3)

const isEditMode = computed(() => !!editing.value)

const editingItem = computed(() => items.value.find((x) => x.id === editing.value))

const editingHasAttachments = computed(() => (editingItem.value?.attachmentPaths?.length || 0) > 0)

const rules = computed(() => {
  if (isEditMode.value) {
    return editingHasAttachments.value
      ? { caption: [{ type: 'required', msg: 'متن پیام الزامی است' }] }
      : { text: [{ type: 'required', msg: 'متن پیام الزامی است' }] }
  }

  const base = {}
  if (isChannelTarget.value) {
    base.messageChannelId = [{ type: 'required', msg: 'انتخاب کانال الزامی است' }]
  } else if (isPersonGroupTarget.value) {
    base.personGroupId = [{ type: 'required', msg: 'انتخاب گروه اشخاص الزامی است' }]
  } else if (isPersonTarget.value) {
    base.personId = [{ type: 'required', msg: 'انتخاب شخص الزامی است' }]
  }
  if (!attachmentFiles.value.length) {
    base.text = [{ type: 'required', msg: 'متن پیام الزامی است' }]
  }
  return base
})

function messageTypeLabel(value) {
  return baleMessageTypeLabel(value) || '—'
}

function attachmentLinkLabel(messageType, index, total) {
  if (isBaleMessageType(messageType, 3)) return total > 1 ? `فایل ${index + 1}/${total}` : 'فایل'
  return total > 1 ? `تصویر ${index + 1}/${total}` : 'تصویر'
}

function messengerLabel(value) {
  return messengerOptions.value.find((o) => o.value === value)?.label
    || messengerKindLabel(value)
    || '—'
}

function toggleError(item, event) {
  if (openErrorId.value === item.id) {
    closeError()
    return
  }
  const rect = event.currentTarget.getBoundingClientRect()
  const maxWidth = Math.min(320, window.innerWidth - 24)
  let right = window.innerWidth - rect.right
  if (window.innerWidth - right - maxWidth < 12) {
    right = Math.max(12, window.innerWidth - maxWidth - 12)
  }
  openErrorId.value = item.id
  errorFlyout.value = {
    message: item.errorMessage,
    top: rect.bottom + 6,
    right,
    maxWidth
  }
}

function closeError() {
  openErrorId.value = null
  errorFlyout.value = null
}

function onDocumentClick() {
  closeError()
}

function onDocumentKeydown(e) {
  if (e.key === 'Escape') closeError()
}

function onDocumentScroll() {
  closeError()
}

function onAttachmentsChange(event) {
  attachmentFiles.value = Array.from(event.target.files || [])
}

function removeAttachment(index) {
  attachmentFiles.value = attachmentFiles.value.filter((_, i) => i !== index)
  if (!attachmentFiles.value.length && attachmentInput.value) attachmentInput.value.value = ''
}

async function pickAttachments(kind) {
  const option = attachmentKindOptions.find((k) => k.key === kind)
  if (!option) return
  attachmentAccept.value = option.accept
  attachmentFiles.value = []
  if (attachmentInput.value) attachmentInput.value.value = ''
  await nextTick()
  attachmentInput.value?.click()
}

function openCompose() {
  editing.value = null
  resetForm(blankForm())
  showForm.value = true
  loadChannels()
}

async function loadChannels() {
  if (!form.value.messenger) {
    channels.value = []
    return
  }
  try {
    const { data } = await api.get(ApiPaths.lookups.messageChannels, {
      params: { messengerKind: enumValue(messengerKinds, form.value.messenger, 1), activeOnly: true }
    })
    channels.value = data
    if (!form.value.messageChannelId && data.length === 1) {
      form.value.messageChannelId = data[0].id
    }
  } catch {
    channels.value = []
  }
}

async function loadPersonGroups() {
  try {
    const { data } = await api.get(ApiPaths.lookups.personGroups, { params: { activeOnly: true } })
    personGroups.value = data
  } catch {
    personGroups.value = []
  }
}

function startEdit(item) {
  openEdit(item.id, {
    text: item.text || '',
    caption: item.caption || ''
  })
}

async function submit() {
  if (!validate(rules.value, form.value)) return

  if (isEditMode.value) {
    const payload = editingHasAttachments.value
      ? { caption: form.value.caption }
      : { text: form.value.text }

    const ok = await trySubmit(async () => {
      await api.put(ApiPaths.baleMessage(editing.value), payload, {
        loaderMessage: 'در حال ذخیره…'
      })
    }, { successMessage: 'پیام ویرایش شد' })
    if (!ok) return
    closeForm()
    await reload()
    return
  }

  if (!config.value.isConfigured) {
    error.value = 'توکن بازو تنظیم نشده است'
    return
  }

  const payload = {
    messenger: enumValue(messengerKinds, form.value.messenger, 1),
    targetType: Number(form.value.targetType),
    messageChannelId: isChannelTarget.value ? Number(form.value.messageChannelId) : null,
    personGroupId: isPersonGroupTarget.value ? Number(form.value.personGroupId) : null,
    personId: isPersonTarget.value ? Number(form.value.personId) : null,
    text: form.value.text?.trim() || null
  }

  const fd = new FormData()
  fd.append('data', JSON.stringify(payload))
  for (const file of attachmentFiles.value) {
    fd.append('files', file)
  }

  let batchSent = false
  const ok = await trySubmit(async () => {
    const { data } = await api.post(ApiPaths.baleMessages, fd, {
      headers: { 'Content-Type': 'multipart/form-data' },
      loaderMessage: 'در حال ارسال…'
    })
    if (data.batch) {
      batchSent = true
      const b = data.batch
      useToastStore().success(
        `ارسال گروهی: ${b.sent} موفق، ${b.failed} ناموفق، ${b.awaitingContact} در انتظار اتصال`
      )
    }
  })
  if (!ok) return
  if (!batchSent) useToastStore().success('پیام ارسال شد')
  closeForm()
  await reload()
}

function messageExtras(item) {
  const extras = []
  if (auth.hasPermission('messages.delete') && item.canDeleteRemote) {
    extras.push({ id: 'remote', label: 'حذف از گفتگو', danger: true })
  }
  if (auth.hasPermission('messages.delete') && item.canDeleteLocal) {
    extras.push({ id: 'local', label: 'حذف از لیست', danger: true })
  }
  return extras
}

function onMessageExtra(item, id) {
  if (id === 'remote') removeRemote(item.id)
  else if (id === 'local') removeLocal(item.id)
}

async function removeRemote(id) {
  if (!(await dialog.confirm({
    message: 'پیام از گفتگو حذف شود؟',
    confirmText: 'حذف از گفتگو',
    danger: true
  }))) return
  const ok = await trySubmit(async () => {
    await api.delete(ApiPaths.baleMessage(id, 'remote'), {
      loaderMessage: 'در حال حذف از گفتگو…'
    })
  }, { successMessage: 'پیام از گفتگو حذف شد' })
  if (!ok) return
  await reload()
}

async function removeLocal(id) {
  if (!(await dialog.confirm({
    message: 'فقط از لیست سامانه حذف شود؟ پیام در پیام‌رسان باقی می‌ماند.',
    confirmText: 'حذف از لیست'
  }))) return
  const ok = await trySubmit(async () => {
    await api.delete(ApiPaths.baleMessage(id, 'local'), {
      loaderMessage: 'در حال حذف…'
    })
  }, { successMessage: 'پیام از لیست حذف شد' })
  if (!ok) return
  await reload()
}

async function syncContacts() {
  const ok = await trySubmit(async () => {
    await api.post(ApiPaths.baleSyncContacts, null, {
      loaderMessage: 'در حال همگام‌سازی…'
    })
  }, { successMessage: 'همگام‌سازی مخاطبین انجام شد' })
  if (!ok) return
  await reload()
}

onMounted(async () => {
  document.addEventListener('click', onDocumentClick)
  document.addEventListener('keydown', onDocumentKeydown)
  document.addEventListener('scroll', onDocumentScroll, true)
  try {
    const { data } = await api.get(ApiPaths.baleConfig)
    config.value = data
  } catch {
    config.value = { isConfigured: false, availableMessengers: [] }
  }
  await Promise.all([loadPersonGroups(), load().catch(() => {})])
})

onBeforeUnmount(() => {
  document.removeEventListener('click', onDocumentClick)
  document.removeEventListener('keydown', onDocumentKeydown)
  document.removeEventListener('scroll', onDocumentScroll, true)
})

watch(() => form.value.messenger, () => {
  if (showForm.value && !isEditMode.value) loadChannels()
})
</script>

<template>
  <div>
    <PageHeader
      :title="pageTitle"
      :show-create="auth.hasPermission('messages.send') && (!showForm || isMobile)"
      create-label="پیام جدید"
      @create="openCompose"
    />

    <div v-if="auth.hasPermission('messages.send')" class="page-actions">
      <button type="button" class="btn btn-outline btn-sm" @click="syncContacts">همگام‌سازی مخاطبین</button>
    </div>

    <div v-if="!config.isConfigured" class="card form-hint-banner">
      توکن بازو در سرور تنظیم نشده است. متغیر <code>BALE_BOT_TOKEN</code> را در محیط اجرا قرار دهید.
    </div>

    <FormHost :show="showForm" @close="closeForm">
      <form class="form-layout-adaptive" @submit.prevent="submit">
        <p v-if="error" class="form-error form-span-full">{{ error }}</p>

        <template v-if="!isEditMode">
          <div class="form-group">
            <label>نوع مقصد</label>
            <AppSelect v-model="form.targetType" :options="targetTypeOptions" />
          </div>
          <div class="form-group">
            <label>پیام‌رسان</label>
            <AppSelect v-model="form.messenger" :options="messengerOptions" @update:model-value="loadChannels" />
          </div>
          <div v-if="isChannelTarget" class="form-group">
            <label>کانال / گروه</label>
            <AppSelect v-model="form.messageChannelId" :options="channelOptions" placeholder="انتخاب کانال" />
            <p v-if="errors.messageChannelId" class="field-error">{{ errors.messageChannelId }}</p>
            <p v-if="!channelOptions.length" class="field-hint">
              کانالی ثبت نشده. از منوی «کانال‌های پیام» یک کانال اضافه کنید.
            </p>
          </div>
          <div v-if="isPersonGroupTarget" class="form-group">
            <label>گروه اشخاص</label>
            <AppSelect v-model="form.personGroupId" :options="personGroupOptions" placeholder="انتخاب گروه" />
            <p v-if="errors.personGroupId" class="field-error">{{ errors.personGroupId }}</p>
          </div>
          <div v-if="isPersonTarget" class="form-group">
            <label>شخص</label>
            <PersonSelect v-model="form.personId" :allow-empty="false" />
            <p v-if="errors.personId" class="field-error">{{ errors.personId }}</p>
          </div>
          <div class="form-group form-span-full">
            <label>{{ attachmentFiles.length ? 'متن پیام (اختیاری)' : 'متن پیام' }}</label>
            <ClearableInput
              v-model="form.text"
              type="textarea"
              class="message-compose-text"
              :rows="8"
              :invalid="!!errors.text"
            />
            <p v-if="errors.text" class="field-error">{{ errors.text }}</p>
          </div>
          <div class="form-group form-span-full">
            <label>پیوست</label>
            <div class="attach-kind-row">
              <button
                v-for="kind in attachmentKindOptions"
                :key="kind.key"
                type="button"
                class="btn btn-outline btn-sm"
                @click="pickAttachments(kind.key)"
              >
                {{ kind.label }}
              </button>
            </div>
            <input
              ref="attachmentInput"
              type="file"
              hidden
              multiple
              :accept="attachmentAccept"
              @change="onAttachmentsChange"
            />
            <ul v-if="attachmentFiles.length" class="attach-tokens">
              <li v-for="(file, index) in attachmentFiles" :key="`${file.name}-${index}`" class="attach-token">
                <span class="attach-token-name">{{ file.name }}</span>
                <button
                  type="button"
                  class="attach-token-remove"
                  :aria-label="`حذف ${file.name}`"
                  @click="removeAttachment(index)"
                >
                  ×
                </button>
              </li>
            </ul>
          </div>
        </template>

        <template v-else>
          <div v-if="editingHasAttachments" class="form-group form-span-full">
            <label>متن پیام</label>
            <ClearableInput
              v-model="form.caption"
              type="textarea"
              class="message-compose-text"
              :rows="6"
              :invalid="!!errors.caption"
            />
            <p v-if="errors.caption" class="field-error">{{ errors.caption }}</p>
          </div>
          <div v-else class="form-group form-span-full">
            <label>متن پیام</label>
            <ClearableInput
              v-model="form.text"
              type="textarea"
              class="message-compose-text"
              :rows="6"
              :invalid="!!errors.text"
            />
            <p v-if="errors.text" class="field-error">{{ errors.text }}</p>
          </div>
        </template>

        <div class="modal-actions form-span-full">
          <button type="button" class="btn btn-outline" @click="closeForm">انصراف</button>
          <button type="submit" class="btn">{{ isEditMode ? 'ذخیره' : 'ارسال' }}</button>
        </div>
      </form>
    </FormHost>

    <PagedListPanel
      v-show="!showForm || isMobile"
      :loading="loading"
      :skeleton-columns="7"
      :show-pagination="showPagination"
      :page="page"
      :total-pages="totalPages"
      :total-count="totalCount"
      :range-start="rangeStart"
      :range-end="rangeEnd"
      :has-prev="hasPrev"
      :has-next="hasNext"
      @prev="goPrev"
      @next="goNext"
    >
      <table class="mobile-table message-table">
        <thead>
          <tr>
            <th class="col-fit">زمان</th>
            <th class="col-fit">پیام‌رسان</th>
            <th class="col-fit">نوع</th>
            <th class="col-fit">وضعیت</th>
            <th class="col-fit">مقصد</th>
            <th class="col-text">متن</th>
            <th v-if="auth.hasAnyPermission('messages.update', 'messages.delete', 'audit.view')" class="col-fit"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in items" :key="item.id">
            <td data-label="زمان" class="col-fit"><DateDisplay :value="item.sentAt || item.audit?.createdAt" show-time /></td>
            <td data-label="پیام‌رسان" class="col-fit">{{ messengerLabel(item.messengerKind) }}</td>
            <td data-label="نوع" class="col-fit">{{ messageTypeLabel(item.messageType) }}</td>
            <td data-label="وضعیت" class="col-fit status-cell">
              <span class="status-label">{{ baleMessageStatusLabel(item.status) }}</span>
              <button
                v-if="item.errorMessage"
                type="button"
                class="status-error-trigger"
                aria-label="نمایش خطا"
                :aria-expanded="openErrorId === item.id"
                @click.stop="toggleError(item, $event)"
              >
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                  <circle cx="12" cy="12" r="9" />
                  <path d="M12 8v5M12 16h.01" stroke-linecap="round" />
                </svg>
              </button>
            </td>
            <td data-label="مقصد" class="col-fit">
              <PersonCell v-if="item.personSummary" :person="item.personSummary" />
              <template v-else-if="item.messageChannelName">{{ item.messageChannelName }}</template>
              <template v-else-if="item.personGroupName">گروه: {{ item.personGroupName }}</template>
              <template v-else>{{ item.chatId || '—' }}</template>
              <p v-if="item.broadcastBatchId" class="field-hint">دسته ارسال</p>
            </td>
            <td data-label="متن" class="message-text-cell">
              <span>{{ item.text || item.caption || item.linkLabel || '—' }}</span>
              <p v-if="item.targetMobile" class="field-hint">موبایل: {{ item.targetMobile }}</p>
              <template v-if="item.attachmentPaths?.length">
                <a
                  v-for="(path, index) in item.attachmentPaths"
                  :key="path"
                  :href="documentUrl(path)"
                  target="_blank"
                  rel="noopener"
                  class="attachment-link"
                >
                  {{ attachmentLinkLabel(item.messageType, index, item.attachmentPaths.length) }}
                </a>
              </template>
              <a v-else-if="item.photoPath" :href="documentUrl(item.photoPath)" target="_blank" rel="noopener">
                {{ isBaleMessageType(item.messageType, 3) ? 'فایل' : 'تصویر' }}
              </a>
            </td>
            <td
              v-if="auth.hasAnyPermission('messages.update', 'messages.delete', 'audit.view')"
              class="col-fit"
            >
              <RowActions
                :show-edit="auth.hasPermission('messages.update') && item.canEdit"
                :show-audit="auth.hasPermission('audit.view')"
                :audit="item.audit"
                :extras="messageExtras(item)"
                @edit="startEdit(item)"
                @extra="onMessageExtra(item, $event)"
              />
            </td>
          </tr>
        </tbody>
      </table>
      <div v-if="!items.length" class="empty-state">پیامی ارسال نشده</div>
    </PagedListPanel>

    <Teleport to="body">
      <div
        v-if="errorFlyout"
        class="message-error-flyout"
        role="tooltip"
        :style="{
          top: `${errorFlyout.top}px`,
          right: `${errorFlyout.right}px`,
          maxWidth: `${errorFlyout.maxWidth}px`
        }"
        @click.stop
      >
        {{ errorFlyout.message }}
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.message-compose-text :deep(textarea) {
  min-height: 9rem;
  padding-block: 0.85rem;
  padding-inline-end: 1rem;
  line-height: 1.7;
  resize: vertical;
}

.attach-tokens {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
  margin: 0.65rem 0 0;
  padding: 0;
  list-style: none;
}

.attach-token {
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
  max-width: 100%;
  padding-block: 0.22rem;
  padding-inline-start: 0.55rem;
  padding-inline-end: 0.35rem;
  border-radius: 999px;
  border: 1px solid color-mix(in srgb, var(--primary) 32%, var(--border));
  background: color-mix(in srgb, var(--primary) 14%, var(--surface));
  color: var(--primary);
  font-size: 0.78rem;
  font-weight: 600;
  line-height: 1.3;
}

.attach-token-name {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 14rem;
}

.attach-token-remove {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 1.15rem;
  height: 1.15rem;
  padding: 0;
  border: none;
  border-radius: 999px;
  background: color-mix(in srgb, var(--primary) 18%, transparent);
  color: inherit;
  font-size: 0.95rem;
  line-height: 1;
  cursor: pointer;
}

.attach-token-remove:hover {
  background: color-mix(in srgb, var(--primary) 28%, transparent);
}

.page-actions {
  margin-bottom: 0.75rem;
}

.status-cell {
  white-space: nowrap;
}

.status-label {
  display: inline;
}

.status-error-trigger {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 1.35rem;
  height: 1.35rem;
  margin-inline-start: 0.25rem;
  padding: 0;
  border: none;
  border-radius: 999px;
  background: var(--danger-soft);
  color: var(--danger);
  vertical-align: middle;
  cursor: pointer;
}

.status-error-trigger svg {
  width: 0.85rem;
  height: 0.85rem;
}

.message-error-flyout {
  position: fixed;
  z-index: 3200;
  min-width: 12rem;
  max-width: min(20rem, calc(100vw - 1.5rem));
  padding: 0.55rem 0.7rem;
  border-radius: 8px;
  border: 1px solid color-mix(in srgb, var(--danger) 35%, var(--border));
  background: var(--surface);
  color: var(--danger);
  font-size: 0.78rem;
  line-height: 1.5;
  box-shadow: 0 6px 20px rgba(0, 0, 0, 0.14);
  white-space: normal;
  word-break: break-word;
}

.message-text-cell {
  white-space: pre-wrap;
  word-break: break-word;
}

.message-text-cell .attachment-link {
  display: inline-block;
  margin-inline-end: 0.5rem;
}

.attach-kind-row {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

@media (min-width: 769px) {
  .message-table .col-fit {
    width: 1%;
    white-space: nowrap;
    vertical-align: top;
  }

  .message-table .col-text,
  .message-table .message-text-cell {
    width: 100%;
    white-space: pre-wrap;
    word-break: break-word;
    vertical-align: top;
  }
}
</style>
