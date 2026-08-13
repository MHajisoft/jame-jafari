<script setup>
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import api from '../api/client'
import { useMobileAttachSheet } from '../composables/useMobileAttachSheet'
import { useDialogStore } from '../stores/dialog'
import { useToastStore } from '../stores/toast'
import { documentKind, documentUrl, isImageFile } from '../utils/format'
import DocumentPreview from './DocumentPreview.vue'

const props = defineProps({
  existing: { type: Array, default: () => [] },
  pending: { type: Array, default: () => [] },
  transactionId: { type: [Number, String], default: null },
  deleteAttachmentPath: { type: Function, default: null },
  accept: { type: String, default: 'image/*,application/pdf' },
  disabled: { type: Boolean, default: false },
  /** Show existing files and open preview */
  canView: { type: Boolean, default: true },
  /** Show attach / add controls */
  canAdd: { type: Boolean, default: true },
  /** Show delete on saved attachments */
  canDelete: { type: Boolean, default: true }
})

const emit = defineEmits(['update:pending', 'update:existing'])

const dialog = useDialogStore()
const toast = useToastStore()

const previewOpen = ref(false)
const previewSrc = ref('')
const previewKind = ref('file')
const pendingUrls = ref([])

const {
  isMobile,
  sheetOpen,
  fileInput,
  cameraInput,
  captureAttr,
  closeSheet,
  onFileChange,
  openSheetOrFile,
  openGallery,
  openCamera
} = useMobileAttachSheet({
  cameraFacing: 'environment',
  enabled: () => props.canAdd && !props.disabled,
  onFiles(files) {
    addFiles(files)
  }
})

function revokePendingUrls() {
  for (const url of pendingUrls.value) URL.revokeObjectURL(url)
  pendingUrls.value = []
}

watch(
  () => props.pending,
  (files) => {
    revokePendingUrls()
    pendingUrls.value = files.map((file) => {
      if (isImageFile(file) || file.type === 'application/pdf') {
        return URL.createObjectURL(file)
      }
      return ''
    })
  },
  { immediate: true, deep: true }
)

const hasAny = computed(() =>
  (props.canView && props.existing.length > 0) || props.pending.length > 0
)

function openPreview(src, kind) {
  previewSrc.value = src
  previewKind.value = kind
  previewOpen.value = true
}

function openExisting(att) {
  if (!props.canView) return
  openPreview(documentUrl(att.path), documentKind(att.path))
}

function openPending(file, index) {
  const src = pendingUrls.value[index]
  if (!src) return
  openPreview(src, documentKind(file.name, file.type))
}

function addFiles(fileList) {
  if (!fileList?.length || props.disabled || !props.canAdd) return
  emit('update:pending', [...props.pending, ...Array.from(fileList)])
}

function openAttach() {
  if (props.disabled || !props.canAdd) return
  openSheetOrFile()
}

function removePending(index) {
  const next = [...props.pending]
  next.splice(index, 1)
  emit('update:pending', next)
}

async function removeExisting(att) {
  if (props.disabled || !props.canDelete) return
  if (!(await dialog.confirmDelete('این پیوست'))) return

  if (props.transactionId && props.deleteAttachmentPath) {
    try {
      await api.delete(props.deleteAttachmentPath(props.transactionId, att.id))
      toast.success('پیوست حذف شد')
    } catch {
      toast.error('حذف پیوست ناموفق بود')
      return
    }
  }

  emit('update:existing', props.existing.filter(a => a.id !== att.id))
}

onBeforeUnmount(() => {
  revokePendingUrls()
})
</script>

<template>
  <div class="tx-attachments">
    <div v-if="hasAny" class="tx-attachments-grid">
      <article
        v-for="att in (canView ? existing : [])"
        :key="`saved-${att.id}`"
        class="tx-attach-card"
      >
        <button type="button" class="tx-attach-preview" @click="openExisting(att)">
          <img
            v-if="documentKind(att.path) === 'image'"
            :src="documentUrl(att.path)"
            alt="پیوست"
            class="tx-attach-thumb"
          />
          <span v-else class="tx-attach-icon" :class="`tx-attach-icon--${documentKind(att.path)}`" aria-hidden="true">
            <svg v-if="documentKind(att.path) === 'pdf'" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
              <polyline points="14 2 14 8 20 8" />
            </svg>
            <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M21.44 11.05l-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48" />
            </svg>
          </span>
          <span class="tx-attach-hint">مشاهده</span>
        </button>
        <button
          v-if="!disabled && canDelete"
          type="button"
          class="tx-attach-remove"
          title="حذف پیوست"
          aria-label="حذف پیوست"
          @click.stop="removeExisting(att)"
        >
          ×
        </button>
      </article>

      <article
        v-for="(file, index) in pending"
        :key="`pending-${index}-${file.name}-${file.size}`"
        class="tx-attach-card tx-attach-card--pending"
      >
        <button type="button" class="tx-attach-preview" @click="openPending(file, index)">
          <img
            v-if="isImageFile(file) && pendingUrls[index]"
            :src="pendingUrls[index]"
            alt="پیوست جدید"
            class="tx-attach-thumb"
          />
          <span v-else class="tx-attach-icon" :class="`tx-attach-icon--${documentKind(file.name, file.type)}`" aria-hidden="true">
            <svg v-if="documentKind(file.name, file.type) === 'pdf'" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
              <polyline points="14 2 14 8 20 8" />
            </svg>
            <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M21.44 11.05l-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48" />
            </svg>
          </span>
          <span class="tx-attach-hint">جدید</span>
        </button>
        <button
          type="button"
          class="tx-attach-remove"
          title="حذف پیوست"
          aria-label="حذف پیوست"
          @click.stop="removePending(index)"
        >
          ×
        </button>
      </article>
    </div>

    <button
      v-if="!disabled && canAdd"
      type="button"
      class="attach-btn"
      @click="openAttach"
    >
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
        <path d="M21.44 11.05l-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48" />
      </svg>
      <span>{{ isMobile ? 'افزودن پیوست' : 'افزودن فایل' }}</span>
    </button>

    <DocumentPreview v-model:show="previewOpen" :src="previewSrc" :kind="previewKind" />

    <input ref="fileInput" type="file" :accept="accept" multiple hidden @change="onFileChange" />
    <input ref="cameraInput" type="file" accept="image/*" :capture="captureAttr" hidden @change="onFileChange" />

    <Teleport to="body">
      <div v-if="sheetOpen && isMobile" class="attach-overlay" @click.self="closeSheet">
        <div class="attach-sheet">
          <div class="sheet-handle" />
          <p class="sheet-title">انتخاب منبع</p>
          <button type="button" class="sheet-option" @click="openCamera">
            <span class="option-icon camera" aria-hidden="true">📷</span>
            <span class="option-text">
              <strong>دوربین</strong>
              <small>عکس فاکتور یا رسید</small>
            </span>
          </button>
          <button type="button" class="sheet-option" @click="openGallery">
            <span class="option-icon gallery" aria-hidden="true">🖼️</span>
            <span class="option-text">
              <strong>گالری / فایل</strong>
              <small>انتخاب چند فایل</small>
            </span>
          </button>
          <button type="button" class="sheet-cancel" @click="closeSheet">انصراف</button>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.tx-attachments {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  margin-top: 0.5rem;
}

.tx-attachments-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(108px, 1fr));
  gap: 0.65rem;
}

.tx-attach-card {
  position: relative;
  border: 1px solid var(--border);
  border-radius: 12px;
  background: var(--surface);
  overflow: hidden;
}

.tx-attach-card--pending {
  border-style: dashed;
}

.tx-attach-preview {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.35rem;
  width: 100%;
  min-height: 108px;
  padding: 0.65rem 0.45rem;
  border: none;
  background: transparent;
  cursor: pointer;
}

.tx-attach-thumb {
  width: 100%;
  max-height: 72px;
  object-fit: cover;
  border-radius: 8px;
}

.tx-attach-icon {
  display: grid;
  place-items: center;
  width: 3rem;
  height: 3rem;
  border-radius: 10px;
  background: color-mix(in srgb, var(--primary) 10%, var(--surface));
  color: var(--primary);
}

.tx-attach-icon svg {
  width: 1.5rem;
  height: 1.5rem;
}

.tx-attach-hint {
  font-size: 0.78rem;
  font-weight: 600;
  color: var(--primary);
}

.tx-attach-remove {
  position: absolute;
  top: 0.35rem;
  inset-inline-start: 0.35rem;
  z-index: 2;
  width: 1.75rem;
  height: 1.75rem;
  border: 2px solid #fff;
  border-radius: 999px;
  background: var(--danger);
  color: #fff;
  font-size: 1.15rem;
  font-weight: 700;
  line-height: 1;
  cursor: pointer;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.25);
}

.attach-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  width: 100%;
  min-height: 44px;
  padding: 0.65rem 1rem;
  border: 1px dashed var(--border);
  border-radius: 12px;
  background: var(--bg);
  color: var(--text);
  font-size: 0.95rem;
  font-weight: 600;
  cursor: pointer;
}

.attach-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.45);
  z-index: 3000;
  display: flex;
  align-items: flex-end;
  justify-content: center;
}

.attach-sheet {
  width: 100%;
  max-width: 420px;
  background: var(--surface);
  border-radius: 20px 20px 0 0;
  padding: 0.75rem 1rem calc(1rem + env(safe-area-inset-bottom, 0));
}

.sheet-handle {
  width: 36px;
  height: 4px;
  border-radius: 999px;
  background: var(--border);
  margin: 0 auto 0.75rem;
}

.sheet-title {
  text-align: center;
  font-weight: 700;
  margin: 0 0 0.75rem;
}

.sheet-option {
  display: flex;
  align-items: center;
  gap: 0.85rem;
  width: 100%;
  padding: 0.85rem 0.5rem;
  border: none;
  border-bottom: 1px solid var(--border);
  background: transparent;
  color: var(--text);
  text-align: right;
  cursor: pointer;
  min-height: 56px;
}
.sheet-option:last-of-type {
  border-bottom: none;
}
.option-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 44px;
  height: 44px;
  border-radius: 12px;
  flex-shrink: 0;
  font-size: 1.25rem;
}
.option-icon.camera {
  background: color-mix(in srgb, var(--primary) 14%, transparent);
}
.option-icon.gallery {
  background: color-mix(in srgb, var(--text-muted) 14%, transparent);
}
.option-text {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
  flex: 1;
  min-width: 0;
  text-align: start;
}
.option-text strong {
  font-size: 0.95rem;
  line-height: 1.3;
}
.option-text small {
  font-size: 0.8rem;
  color: var(--text-muted);
  line-height: 1.35;
}

.sheet-cancel {
  width: 100%;
  min-height: 44px;
  margin-top: 0.5rem;
  border: none;
  border-radius: 999px;
  background: var(--bg);
  font-weight: 600;
  cursor: pointer;
}
</style>
