<script setup>
import { ref } from 'vue'

const props = defineProps({
  modelValue: { type: File, default: null },
  accept: { type: String, default: 'image/*' }
})
const emit = defineEmits(['update:modelValue'])

const preview = ref(null)
const fileInput = ref(null)
const cameraInput = ref(null)

function handleFile(file) {
  if (!file) return
  emit('update:modelValue', file)
  preview.value = URL.createObjectURL(file)
}

function onFileChange(e) {
  handleFile(e.target.files[0])
}

function openGallery() {
  fileInput.value?.click()
}

function openCamera() {
  cameraInput.value?.click()
}

function clear() {
  emit('update:modelValue', null)
  preview.value = null
}
</script>

<template>
  <div class="file-upload">
    <div v-if="preview" class="preview">
      <img :src="preview" alt="preview" />
      <button type="button" class="btn btn-sm btn-danger" @click="clear">حذف</button>
    </div>
    <div v-else class="actions">
      <button type="button" class="btn btn-outline btn-sm" @click="openGallery">گالری</button>
      <button type="button" class="btn btn-outline btn-sm" @click="openCamera">دوربین</button>
      <label class="btn btn-outline btn-sm">
        آپلود
        <input type="file" :accept="accept" hidden @change="onFileChange" />
      </label>
    </div>
    <input ref="fileInput" type="file" :accept="accept" hidden @change="onFileChange" />
    <input ref="cameraInput" type="file" :accept="accept" capture="environment" hidden @change="onFileChange" />
  </div>
</template>

<style scoped>
.file-upload { margin-top: 0.5rem; }
.actions { display: flex; gap: 0.5rem; flex-wrap: wrap; }
.preview img {
  max-width: 200px;
  max-height: 150px;
  border-radius: 8px;
  display: block;
  margin-bottom: 0.5rem;
}
</style>
