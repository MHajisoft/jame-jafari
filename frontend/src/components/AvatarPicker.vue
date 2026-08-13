<script setup>
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useMobileAttachSheet } from '../composables/useMobileAttachSheet'
import EntityAvatar from './EntityAvatar.vue'
import AvatarPreview from './AvatarPreview.vue'

const props = defineProps({
  modelValue: { type: File, default: null },
  path: { type: String, default: '' },
  name: { type: String, default: '' },
  disabled: { type: Boolean, default: false },
  label: { type: String, default: 'تصویر' },
  size: { type: Number, default: null },
  showHint: { type: Boolean, default: true },
  /** Front camera for portraits; environment for documents */
  cameraFacing: { type: String, default: 'user' }
})

const emit = defineEmits(['update:modelValue', 'update:path'])

const previewOpen = ref(false)
const localPreview = ref('')

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
  cameraFacing: () => props.cameraFacing,
  onFiles(files) {
    const file = files[0]
    if (!file || props.disabled) return
    if (props.path) emit('update:path', '')
    emit('update:modelValue', file)
  }
})

watch(
  () => props.modelValue,
  (file) => {
    if (localPreview.value) URL.revokeObjectURL(localPreview.value)
    localPreview.value = file ? URL.createObjectURL(file) : ''
  },
  { immediate: true }
)

onBeforeUnmount(() => {
  if (localPreview.value) URL.revokeObjectURL(localPreview.value)
})

const displayPath = computed(() => localPreview.value || props.path || '')
const hasImage = computed(() => !!props.modelValue || !!props.path)

const avatarSize = computed(() => props.size ?? (isMobile.value ? 96 : 132))

const previewUrl = computed(() => {
  const path = displayPath.value
  if (!path) return ''
  if (path.startsWith('http://') || path.startsWith('https://') || path.startsWith('/') || path.startsWith('blob:')) {
    return path
  }
  return `/uploads/${path}`
})

function openPreview() {
  if (!hasImage.value || props.disabled) return
  previewOpen.value = true
}

function openPreviewFromSheet() {
  closeSheet()
  openPreview()
}

function openPicker() {
  if (props.disabled) return
  openSheetOrFile()
}

function clear() {
  if (props.disabled) return
  sheetOpen.value = false
  if (props.modelValue) {
    emit('update:modelValue', null)
    return
  }
  if (props.path) emit('update:path', '')
}
</script>

<template>
  <div class="avatar-picker">
    <label v-if="label" class="avatar-picker-label">{{ label }}</label>

    <div class="avatar-stage">
      <button
        type="button"
        class="avatar-hit"
        :disabled="disabled"
        :aria-label="hasImage ? 'تغییر تصویر' : 'افزودن تصویر'"
        @click="openPicker"
      >
        <EntityAvatar :src="displayPath" :name="name" :size="avatarSize" />
        <span class="avatar-badge" aria-hidden="true">
          <svg viewBox="0 0 24 24" width="15" height="15" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M4 8h3l2-2h6l2 2h3v11H4z" />
            <circle cx="12" cy="13" r="3.2" />
          </svg>
        </span>
      </button>

      <p v-if="showHint" class="avatar-hint text-muted">
        برای {{ hasImage ? 'تغییر' : 'افزودن' }} تصویر، روی عکس بزنید
      </p>

      <div v-if="hasImage" class="avatar-actions">
        <button
          type="button"
          class="btn btn-outline btn-sm"
          :disabled="disabled"
          @click="openPreview"
        >
          مشاهده اندازه واقعی
        </button>
        <button
          type="button"
          class="btn btn-outline btn-sm avatar-remove-btn"
          :disabled="disabled"
          @click="clear"
        >
          حذف تصویر
        </button>
      </div>
    </div>

    <input ref="fileInput" type="file" accept="image/*" hidden @change="onFileChange" />
    <input
      ref="cameraInput"
      type="file"
      accept="image/*"
      :capture="captureAttr"
      hidden
      @change="onFileChange"
    />

    <Teleport to="body">
      <div v-if="sheetOpen && isMobile" class="attach-overlay" @click.self="closeSheet">
        <div class="attach-sheet" role="dialog" aria-modal="true" aria-label="انتخاب تصویر">
          <div class="sheet-handle" />
          <p class="sheet-title">انتخاب تصویر</p>

          <button v-if="hasImage" type="button" class="sheet-option" @click="openPreviewFromSheet">
            <span class="option-icon" aria-hidden="true">
              <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round">
                <circle cx="10.5" cy="10.5" r="6.5" />
                <path d="M21 21l-5.5-5.5" />
              </svg>
            </span>
            <span class="option-text">
              <strong>مشاهده اندازه واقعی</strong>
              <small>برای تشخیص چهره</small>
            </span>
          </button>

          <button type="button" class="sheet-option" @click="openCamera">
            <span class="option-icon" aria-hidden="true">
              <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M4 8h3l2-2h6l2 2h3v11H4z" />
                <circle cx="12" cy="13" r="3.2" />
              </svg>
            </span>
            <span class="option-text">
              <strong>دوربین</strong>
              <small>گرفتن عکس جدید</small>
            </span>
          </button>

          <button type="button" class="sheet-option" @click="openGallery">
            <span class="option-icon" aria-hidden="true">
              <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <rect x="3" y="4" width="18" height="16" rx="2" />
                <circle cx="9" cy="10" r="1.8" />
                <path d="m21 16-5-5-8 8" />
              </svg>
            </span>
            <span class="option-text">
              <strong>گالری</strong>
              <small>انتخاب از تصاویر دستگاه</small>
            </span>
          </button>

          <button
            v-if="hasImage"
            type="button"
            class="sheet-option danger"
            @click="clear"
          >
            <span class="option-icon danger" aria-hidden="true">
              <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M3 6h18" />
                <path d="M8 6V4h8v2" />
                <path d="M19 6l-1 14H6L5 6" />
              </svg>
            </span>
            <span class="option-text">
              <strong>حذف تصویر</strong>
              <small>بازگشت به حروف اول نام</small>
            </span>
          </button>

          <button type="button" class="sheet-cancel" @click="closeSheet">انصراف</button>
        </div>
      </div>
    </Teleport>

    <AvatarPreview
      v-model:show="previewOpen"
      :src="previewUrl"
      :title="name"
    />
  </div>
</template>

<style scoped>
.avatar-picker { margin-bottom: 1rem; }
.avatar-picker-label {
  display: block;
  margin-bottom: 0.55rem;
  font-weight: 600;
  font-size: 0.9rem;
}

.avatar-stage {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  gap: 0.55rem;
}

.avatar-hit {
  position: relative;
  border: none;
  background: transparent;
  padding: 0;
  cursor: pointer;
  border-radius: 50%;
  -webkit-tap-highlight-color: transparent;
}
.avatar-hit:disabled { opacity: 0.6; cursor: not-allowed; }
.avatar-hit:focus-visible {
  outline: 2px solid var(--primary);
  outline-offset: 3px;
}

.avatar-badge {
  position: absolute;
  inset-inline-end: 0;
  bottom: 0;
  width: 30px;
  height: 30px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  background: var(--primary);
  color: var(--on-primary);
  border-radius: 50%;
  border: 2px solid var(--surface);
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.16);
}
.avatar-badge svg { display: block; }

.avatar-hint {
  margin: 0;
  font-size: 0.82rem;
  line-height: 1.45;
  max-width: 16rem;
}

.avatar-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  width: 100%;
  max-width: 20rem;
}

.avatar-actions .btn {
  min-height: 44px;
  flex: 1 1 auto;
}

.avatar-remove-btn {
  color: var(--danger);
  border-color: color-mix(in srgb, var(--danger) 32%, var(--border));
}
.avatar-remove-btn:hover {
  background: color-mix(in srgb, var(--danger) 8%, var(--surface));
  border-color: color-mix(in srgb, var(--danger) 45%, var(--border));
}

.attach-overlay {
  position: fixed;
  inset: 0;
  z-index: 3000;
  background: var(--overlay, rgba(0, 0, 0, 0.45));
  display: flex;
  align-items: flex-end;
}
.attach-sheet {
  width: 100%;
  background: var(--surface);
  border-radius: 18px 18px 0 0;
  padding: 0.75rem 1rem calc(1rem + env(safe-area-inset-bottom, 0));
}
.sheet-handle {
  width: 42px;
  height: 4px;
  border-radius: 999px;
  background: var(--border);
  margin: 0.25rem auto 0.85rem;
}
.sheet-title {
  text-align: center;
  font-weight: 700;
  margin: 0 0 0.75rem;
}
.sheet-option {
  width: 100%;
  display: flex;
  align-items: center;
  gap: 0.75rem;
  border: 1px solid var(--border);
  background: var(--bg);
  border-radius: 12px;
  padding: 0.85rem 0.9rem;
  margin-bottom: 0.55rem;
  cursor: pointer;
  color: var(--text);
  text-align: right;
  min-height: 56px;
}
.sheet-option.danger {
  border-color: color-mix(in srgb, var(--danger) 28%, var(--border));
  background: color-mix(in srgb, var(--danger) 6%, var(--surface));
}
.option-icon {
  width: 2.4rem;
  height: 2.4rem;
  border-radius: 10px;
  display: grid;
  place-items: center;
  background: color-mix(in srgb, var(--primary) 14%, transparent);
  color: var(--primary);
  flex-shrink: 0;
}
.option-icon.danger {
  background: color-mix(in srgb, var(--danger) 12%, transparent);
  color: var(--danger);
}
.option-text {
  display: flex;
  flex-direction: column;
  gap: 0.1rem;
}
.option-text small { color: var(--text-muted); }
.sheet-option.danger .option-text strong { color: var(--danger); }
.sheet-cancel {
  width: 100%;
  margin-top: 0.25rem;
  border: none;
  background: transparent;
  color: var(--text-muted);
  padding: 0.85rem;
  font: inherit;
  cursor: pointer;
  min-height: 48px;
}
</style>
