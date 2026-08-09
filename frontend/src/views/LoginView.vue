<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import ClearableInput from '../components/ClearableInput.vue'

const router = useRouter()
const auth = useAuthStore()
const username = ref('admin')
const password = ref('admin123')
const error = ref('')
const fieldErrors = ref({})
const loading = ref(false)

function clearField(field) {
  const next = { ...fieldErrors.value }
  delete next[field]
  fieldErrors.value = next
}

async function submit() {
  error.value = ''
  fieldErrors.value = {}

  if (!username.value.trim()) fieldErrors.value.username = 'نام کاربری الزامی است'
  if (!password.value) fieldErrors.value.password = 'رمز عبور الزامی است'
  if (Object.keys(fieldErrors.value).length) return

  loading.value = true
  try {
    await auth.login(username.value, password.value)
    router.push('/')
  } catch {
    error.value = 'نام کاربری یا رمز عبور اشتباه است'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="login-page">
    <div class="login-card card">
      <h1>موسسه جامعه جعفری</h1>
      <p class="subtitle">سامانه مدیریت مالی</p>
      <form @submit.prevent="submit">
        <div class="form-group">
          <label>نام کاربری</label>
          <ClearableInput
            v-model="username"
            :invalid="!!fieldErrors.username"
            autocomplete="username"
            @input="clearField('username')"
          />
          <div v-if="fieldErrors.username" class="field-error">{{ fieldErrors.username }}</div>
        </div>
        <div class="form-group">
          <label>رمز عبور</label>
          <ClearableInput
            v-model="password"
            type="password"
            :invalid="!!fieldErrors.password"
            autocomplete="current-password"
            @input="clearField('password')"
          />
          <div v-if="fieldErrors.password" class="field-error">{{ fieldErrors.password }}</div>
        </div>
        <p v-if="error" class="error">{{ error }}</p>
        <button type="submit" class="btn" :disabled="loading" style="width:100%;margin-top:1rem">
          {{ loading ? 'در حال ورود...' : 'ورود' }}
        </button>
      </form>
    </div>
  </div>
</template>

<style scoped>
.login-page {
  min-height: 100vh;
  min-height: 100dvh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, var(--primary), var(--sidebar));
  padding: 1rem;
  padding-top: calc(1rem + env(safe-area-inset-top, 0));
  padding-bottom: calc(1rem + env(safe-area-inset-bottom, 0));
}
.login-card {
  width: 100%;
  max-width: 400px;
  text-align: center;
}
.login-card h1 { color: var(--primary); margin-bottom: 0.25rem; }
.subtitle { color: var(--text-muted); margin-bottom: 1.5rem; }
.error { color: var(--danger); margin-top: 0.5rem; font-size: 0.9rem; }
.form-group { text-align: right; }
</style>
