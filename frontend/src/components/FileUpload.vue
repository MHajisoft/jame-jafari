<script setup>
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useMobileAttachSheet } from '../composables/useMobileAttachSheet'
import { documentKind, documentUrl, isImageFile } from '../utils/format'
import DocumentPreview from './DocumentPreview.vue'

const props = defineProps({
  modelValue: { type: File, default: null },
  /** Existing server upload path */
  path: { type: String, default: '' },
  accept: { type: String, default: 'image/*,application/pdf' }
})

const emit = defineEmits(['update:modelValue', 'update:path'])

const blobUrl = ref('')
const previewOpen = ref(false)

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
  onFiles(files) {
    emit('update:modelValue', files[0])
  }
})

const hasAttachment = computed(() => !!props.modelValue || !!props.path)

const previewSrc = computed(() => {
  if (blobUrl.value) return blobUrl.value
  if (props.path && !props.modelValue) return documentUrl(props.path)
  return ''
})

const previewKind = computed(() => {
  if (props.modelValue) return documentKind(props.modelValue.name, props.modelValue.type)
  return documentKind(props.path)
})

const showImageThumb = computed(() => previewKind.value === 'image' && !!previewSrc.value)

function revokeBlob() {
  if (blobUrl.value) {
    URL.revokeObjectURL(blobUrl.value)
    blobUrl.value = ''
  }
}

watch(
  () => props.modelValue,
  (file) => {
    revokeBlob()
    if (!file) return
    if (isImageFile(file) || file.type === 'application/pdf') {
      blobUrl.value = URL.createObjectURL(file)
    }
  },
  { immediate: true }
)

function openAttach() {
  openSheetOrFile()
}

function openPreview() {
  if (previewSrc.value) previewOpen.value = true
}

function clear() {
  sheetOpen.value = false
  previewOpen.value = false
  if (props.modelValue) {
    emit('update:modelValue', null)
    return
  }
  if (props.path) emit('update:path', '')
}

onBeforeUnmount(() => {
  revokeBlob()
})
</script>

<template>
  <div class="file-upload">
    <div v-if="hasAttachment" class="preview">
      <button type="button" class="preview-open" aria-label="مشاهده پیوست" @click="openPreview">
        <img
          v-if="showImageThumb"
          :src="previewSrc"
          alt="پیش‌نمایش پیوست"
          class="preview-thumb"
        />
        <span v-else class="preview-icon" :class="`preview-icon--${previewKind}`" aria-hidden="true">
          <svg v-if="previewKind === 'pdf'" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" />
            <polyline points="14 2 14 8 20 8" />
            <line x1="16" y1="13" x2="8" y2="13" />
            <line x1="16" y1="17" x2="8" y2="17" />
          </svg>
          <svg v-else viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M21.44 11.05l-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48" />
          </svg>
        </span>
        <span class="preview-hint">مشاهده</span>
      </button>
      <div class="preview-actions">
        <button type="button" class="btn btn-sm btn-outline" @click="openAttach">جایگزینی</button>
        <button type="button" class="btn btn-sm btn-danger" @click="clear">حذف</button>
      </div>
    </div>
    <button v-else type="button" class="attach-btn" @click="openAttach">
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
        <path d="M21.44 11.05l-9.19 9.19a6 6 0 0 1-8.49-8.49l9.19-9.19a4 4 0 0 1 5.66 5.66l-9.2 9.19a2 2 0 0 1-2.83-2.83l8.49-8.48" />
      </svg>
      <span>{{ isMobile ? 'افزودن پیوست' : 'آپلود فایل' }}</span>
    </button>

    <DocumentPreview
      v-model:show="previewOpen"
      :src="previewSrc"
      :kind="previewKind"
    />

    <input ref="fileInput" type="file" :accept="accept" hidden @change="onFileChange" />
    <input ref="cameraInput" type="file" accept="image/*" :capture="captureAttr" hidden @change="onFileChange" />

    <Teleport to="body">
      <div v-if="sheetOpen && isMobile" class="attach-overlay" @click.self="closeSheet">
        <div class="attach-sheet">
          <div class="sheet-handle" />
          <p class="sheet-title">انتخاب منبع</p>
          <button type="button" class="sheet-option" @click="openCamera">
            <span class="option-icon camera">
              <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M23 19a2 2 0 0 1-2 2H3a2 2 0 0 1-2-2V8a2 2 0 0 1 2-2h4l2-3h6l2 3h4a2 2 0 0 1 2 2z" />
                <circle cx="12" cy="13" r="4" />
              </svg>
            </span>
            <span class="option-text">
              <strong>دوربین</strong>
              <small>عکس‌برداری از فاکتور یا رسید</small>
            </span>
          </button>
          <button type="button" class="sheet-option" @click="openGallery">
            <span class="option-icon gallery">
              <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <rect x="3" y="3" width="18" height="18" rx="2" />
                <circle cx="8.5" cy="8.5" r="1.5" />
                <polyline points="21 15 16 10 5 21" />
              </svg>
            </span>
            <span class="option-text">
              <strong>گالری / فایل</strong>
              <small>انتخاب از تصاویر یا فایل‌های دستگاه</small>
            </span>
          </button>
          <button type="button" class="sheet-cancel" @click="closeSheet">انصراف</button>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.file-upload { margin-top: 0.5rem; }
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
.attach-btn:active { opacity: 0.85; }

.preview-open {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.45rem;
  width: 100%;
  padding: 0.75rem;
  margin-bottom: 0.65rem;
  border: 1px solid var(--border);
  border-radius: 12px;
  background: color-mix(in srgb, var(--primary) 5%, var(--bg));
  cursor: pointer;
}

.preview-open:focus-visible {
  outline: 2px solid var(--primary);
  outline-offset: 2px;
}

.preview-thumb {
  max-width: 100%;
  max-height: 160px;
  object-fit: contain;
  border-radius: 8px;
}

.preview-icon {
  display: grid;
  place-items: center;
  width: 3.5rem;
  height: 3.5rem;
  border-radius: 12px;
  background: color-mix(in srgb, var(--primary) 12%, var(--surface));
  color: var(--primary);
}

.preview-icon svg {
  width: 1.65rem;
  height: 1.65rem;
}

.preview-hint {
  font-size: 0.85rem;
  font-weight: 600;
  color: var(--primary);
}

.preview-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
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
  animation: sheet-up 0.25s ease-out;
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
  font-size: 1rem;
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
}
.sheet-option:last-of-type { border-bottom: none; }
.option-icon {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 44px;
  height: 44px;
  border-radius: 12px;
  flex-shrink: 0;
}
.option-icon.camera {
  background: color-mix(in srgb, var(--primary) 14%, transparent);
  color: var(--primary);
}
.option-icon.gallery {
  background: color-mix(in srgb, var(--text-muted) 14%, transparent);
  color: var(--text);
}
.option-text {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}
.option-text strong { font-size: 0.95rem; }
.option-text small {
  font-size: 0.8rem;
  color: var(--text-muted);
}
.sheet-cancel {
  width: 100%;
  min-height: 44px;
  margin-top: 0.5rem;
  border: none;
  border-radius: 999px;
  background: var(--bg);
  color: var(--text);
  font-weight: 600;
  font-size: 0.95rem;
  cursor: pointer;
}

@keyframes sheet-up {
  from { transform: translateY(100%); }
  to { transform: translateY(0); }
}
</style>
