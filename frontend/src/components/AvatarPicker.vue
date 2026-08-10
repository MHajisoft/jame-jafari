<script setup>
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useIsMobile } from '../composables/useMediaQuery'
import EntityAvatar from './EntityAvatar.vue'

const props = defineProps({
  /** Pending local file */
  modelValue: { type: File, default: null },
  /** Existing server upload path */
  path: { type: String, default: '' },
  name: { type: String, default: '' },
  disabled: { type: Boolean, default: false },
  label: { type: String, default: 'تصویر' }
})

const emit = defineEmits(['update:modelValue', 'update:path'])

const isMobile = useIsMobile()
const sheetOpen = ref(false)
const fileInput = ref(null)
const cameraInput = ref(null)
const localPreview = ref('')

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

const displayPath = computed(() => {
  if (localPreview.value) return localPreview.value
  return props.path || ''
})

const hasImage = computed(() => !!props.modelValue || !!props.path)

function openPicker() {
  if (props.disabled) return
  if (isMobile.value) sheetOpen.value = true
  else fileInput.value?.click()
}

function closeSheet() {
  sheetOpen.value = false
}

function openGallery() {
  fileInput.value?.click()
}

function openCamera() {
  cameraInput.value?.click()
}

function onFileChange(e) {
  const file = e.target.files?.[0]
  e.target.value = ''
  sheetOpen.value = false
  if (!file) return
  emit('update:modelValue', file)
}

function clear() {
  if (props.disabled) return
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
    <div class="avatar-picker-row">
      <button
        type="button"
        class="avatar-hit"
        :disabled="disabled"
        :aria-label="hasImage ? 'تغییر تصویر' : 'افزودن تصویر'"
        @click="openPicker"
      >
        <EntityAvatar :src="displayPath" :name="name" :size="72" />
        <span class="avatar-hit-hint">{{ hasImage ? 'تغییر' : 'افزودن' }}</span>
      </button>
      <div class="avatar-picker-actions">
        <button type="button" class="btn btn-sm" :disabled="disabled" @click="openPicker">
          {{ hasImage ? 'انتخاب تصویر' : 'افزودن تصویر' }}
        </button>
        <button
          v-if="hasImage"
          type="button"
          class="btn btn-sm btn-outline"
          :disabled="disabled"
          @click="clear"
        >
          حذف تصویر
        </button>
        <p class="text-muted hint">فرمت‌های تصویری؛ در موبایل می‌توانید از دوربین استفاده کنید.</p>
      </div>
    </div>

    <input ref="fileInput" type="file" accept="image/*" hidden @change="onFileChange" />
    <input ref="cameraInput" type="file" accept="image/*" capture="environment" hidden @change="onFileChange" />

    <Teleport to="body">
      <div v-if="sheetOpen && isMobile" class="attach-overlay" @click.self="closeSheet">
        <div class="attach-sheet">
          <div class="sheet-handle" />
          <p class="sheet-title">انتخاب تصویر</p>
          <button type="button" class="sheet-option" @click="openCamera">
            <span class="option-icon">دوربین</span>
            <span class="option-text">
              <strong>دوربین</strong>
              <small>گرفتن عکس جدید</small>
            </span>
          </button>
          <button type="button" class="sheet-option" @click="openGallery">
            <span class="option-icon">گالری</span>
            <span class="option-text">
              <strong>گالری</strong>
              <small>انتخاب از تصاویر دستگاه</small>
            </span>
          </button>
          <button type="button" class="sheet-cancel" @click="closeSheet">انصراف</button>
        </div>
      </div>
    </Teleport>
  </div>
</template>

<style scoped>
.avatar-picker { margin-bottom: 1rem; }
.avatar-picker-label {
  display: block;
  margin-bottom: 0.45rem;
  font-weight: 600;
  font-size: 0.9rem;
}
.avatar-picker-row {
  display: flex;
  align-items: center;
  gap: 1rem;
}
.avatar-hit {
  position: relative;
  border: none;
  background: transparent;
  padding: 0;
  cursor: pointer;
  border-radius: 50%;
}
.avatar-hit:disabled { opacity: 0.6; cursor: not-allowed; }
.avatar-hit-hint {
  position: absolute;
  inset-inline-end: -0.15rem;
  bottom: -0.1rem;
  background: var(--primary);
  color: var(--on-primary);
  font-size: 0.65rem;
  font-weight: 700;
  padding: 0.15rem 0.4rem;
  border-radius: 999px;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.12);
}
.avatar-picker-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.45rem;
  align-items: center;
  min-width: 0;
}
.hint {
  flex-basis: 100%;
  margin: 0;
  font-size: 0.78rem;
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
}
.option-icon {
  width: 2.4rem;
  height: 2.4rem;
  border-radius: 10px;
  display: grid;
  place-items: center;
  background: color-mix(in srgb, var(--primary) 14%, transparent);
  color: var(--primary);
  font-size: 0.72rem;
  font-weight: 700;
}
.option-text {
  display: flex;
  flex-direction: column;
  gap: 0.1rem;
}
.option-text small { color: var(--text-muted); }
.sheet-cancel {
  width: 100%;
  margin-top: 0.25rem;
  border: none;
  background: transparent;
  color: var(--text-muted);
  padding: 0.85rem;
  font: inherit;
  cursor: pointer;
}
</style>
