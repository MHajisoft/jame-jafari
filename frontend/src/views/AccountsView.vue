<script setup>
import { computed, onMounted } from 'vue'
import api from '../api/client'
import { ApiPaths } from '../api/paths'
import { useAuthStore } from '../stores/auth'
import { useDialogStore } from '../stores/dialog'
import { useLookupsStore } from '../stores/lookups'
import { useFormValidation } from '../composables/useFormValidation'
import { useEntityForm } from '../composables/useEntityForm'
import { useAsyncList } from '../composables/useAsyncList'
import { useIsMobile } from '../composables/useMediaQuery'
import ClearableInput from '../components/ClearableInput.vue'
import FormHost from '../components/FormHost.vue'
import AppCheckbox from '../components/AppCheckbox.vue'
import RowActions from '../components/RowActions.vue'
import PageHeader from '../components/PageHeader.vue'
import AppSkeleton from '../components/AppSkeleton.vue'

const auth = useAuthStore()
const dialog = useDialogStore()
const lookups = useLookupsStore()
const isMobile = useIsMobile()
const { error, errors, validate, trySubmit, clearErrors, clearFieldError } = useFormValidation()

const { showForm, editing, form, openCreate, openEdit, closeForm } = useEntityForm(
  () => ({ name: '', description: '', isActive: true }),
  { onReset: clearErrors }
)

const { items, loading, load } = useAsyncList(async () => {
  return lookups.getAccounts({ activeOnly: false, force: true, admin: true })
})

const rules = {
  name: [{ type: 'required', msg: 'نام حساب الزامی است' }]
}

const pageTitle = computed(() => {
  if (showForm.value && !isMobile.value) {
    return editing.value ? 'ویرایش حساب' : 'حساب جدید'
  }
  return 'حساب‌های مالی'
})

async function submit() {
  if (!validate(rules, form.value)) return
  const ok = await trySubmit(async () => {
    if (editing.value) {
      await api.put(ApiPaths.account(editing.value), form.value)
    } else {
      await api.post(ApiPaths.accounts, form.value)
    }
  }, { successMessage: editing.value ? 'حساب با موفقیت ویرایش شد' : 'حساب با موفقیت ایجاد شد' })
  if (!ok) return
  lookups.invalidateAccounts()
  closeForm()
  await load()
}

function startEdit(item) {
  openEdit(item.id, {
    name: item.name,
    description: item.description || '',
    isActive: item.isActive
  })
}

async function remove(id) {
  if (!(await dialog.confirmDelete('این حساب'))) return
  const ok = await trySubmit(async () => {
    await api.delete(ApiPaths.account(id))
  }, { successMessage: 'حساب حذف شد' })
  if (!ok) return
  lookups.invalidateAccounts()
  await load()
}

onMounted(() => load().catch(() => {}))
</script>

<template>
  <div>
    <PageHeader
      :title="pageTitle"
      :form-mode="showForm && !isMobile"
      :show-create="auth.hasPermission('accounts.create') && (!showForm || isMobile)"
      create-label="حساب جدید"
      @create="openCreate"
    />

    <FormHost :show="showForm" :title="isMobile ? (editing ? 'ویرایش حساب' : 'حساب جدید') : ''" @close="closeForm">
      <div v-if="error" class="form-error">{{ error }}</div>
      <form @submit.prevent="submit">
        <div class="form-group">
          <label>نام *</label>
          <ClearableInput
            v-model="form.name"
            :invalid="!!errors.name"
            @input="clearFieldError('name')"
          />
          <div v-if="errors.name" class="field-error">{{ errors.name }}</div>
        </div>
        <div class="form-group form-span-full">
          <label>توضیحات</label>
          <ClearableInput v-model="form.description" type="textarea" :rows="2" />
        </div>
        <div class="form-group">
          <AppCheckbox v-model="form.isActive" label="فعال" />
        </div>
        <div class="modal-actions">
          <button type="button" class="btn btn-outline" @click="closeForm">انصراف</button>
          <button type="submit" class="btn">ذخیره</button>
        </div>
      </form>
    </FormHost>

    <div v-show="!showForm || isMobile" class="card list-panel" :aria-busy="loading">
      <AppSkeleton v-if="loading" :columns="4" />
      <template v-else>
        <table class="mobile-table">
        <thead>
          <tr>
            <th>نام</th>
            <th>توضیحات</th>
            <th>وضعیت</th>
            <th v-if="auth.hasAnyPermission('accounts.update', 'accounts.delete')"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in items" :key="item.id">
            <td data-label="نام"><strong>{{ item.name }}</strong></td>
            <td data-label="توضیحات">{{ item.description }}</td>
            <td data-label="وضعیت">
              <span :class="item.isActive ? 'badge badge-success' : 'badge badge-danger'">
                {{ item.isActive ? 'فعال' : 'غیرفعال' }}
              </span>
            </td>
            <td v-if="auth.hasAnyPermission('accounts.update', 'accounts.delete')">
              <RowActions
                :show-edit="auth.hasPermission('accounts.update')"
                :show-delete="auth.hasPermission('accounts.delete')"
                @edit="startEdit(item)"
                @delete="remove(item.id)"
              />
            </td>
          </tr>
        </tbody>
      </table>
      </template>
    </div>
  </div>
</template>
