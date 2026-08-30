<script setup>
import { computed, onMounted } from 'vue'
import api from '../api/client'
import { ApiPaths } from '../api/paths'
import { useAuthStore } from '../stores/auth'
import { useDialogStore } from '../stores/dialog'
import { useFormValidation } from '../composables/useFormValidation'
import { useEntityForm } from '../composables/useEntityForm'
import { usePagedList } from '../composables/usePagedList'
import { useIsMobile } from '../composables/useMediaQuery'
import { enumValue, messengerKindLabel, messengerKinds } from '../utils/format'
import { normalizeMessengerChatTarget } from '../utils/messengerChat'
import AppSelect from '../components/AppSelect.vue'
import AppCheckbox from '../components/AppCheckbox.vue'
import ClearableInput from '../components/ClearableInput.vue'
import FormHost from '../components/FormHost.vue'
import RowActions from '../components/RowActions.vue'
import PageHeader from '../components/PageHeader.vue'
import PagedListPanel from '../components/PagedListPanel.vue'

const auth = useAuthStore()
const dialog = useDialogStore()
const isMobile = useIsMobile()
const { error, errors, validate, trySubmit, clearErrors, clearFieldError } = useFormValidation()

const messengerOptions = messengerKinds.map((m) => ({ value: m.value, label: m.label }))

const { showForm, editing, form, openCreate, openEdit, closeForm } = useEntityForm(
  () => ({ name: '', messengerKind: 1, externalChatId: '', isActive: true }),
  { onReset: clearErrors }
)

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
  const { data } = await api.get(ApiPaths.messageChannels, {
    params: { activeOnly: false, page, pageSize }
  })
  return data
})

const rules = {
  name: [{ type: 'required', msg: 'نام کانال الزامی است' }],
  externalChatId: [{ type: 'messengerChatTarget', msg: 'شناسه عددی یا نام کاربری @ وارد کنید' }]
}

const pageTitle = computed(() => {
  if (showForm.value && !isMobile.value) {
    return editing.value ? 'ویرایش کانال' : 'کانال جدید'
  }
  return 'کانال‌های پیام'
})

function messengerLabel(value) {
  return messengerKindLabel(value) || '—'
}

async function submit() {
  if (!validate(rules, form.value)) return
  const payload = {
    name: form.value.name,
    messengerKind: enumValue(messengerKinds, form.value.messengerKind, 1),
    externalChatId: normalizeMessengerChatTarget(form.value.externalChatId),
    isActive: form.value.isActive
  }
  const ok = await trySubmit(async () => {
    if (editing.value) {
      await api.put(ApiPaths.messageChannel(editing.value), payload)
    } else {
      await api.post(ApiPaths.messageChannels, payload)
    }
  }, { successMessage: editing.value ? 'کانال ویرایش شد' : 'کانال ایجاد شد' })
  if (!ok) return
  closeForm()
  await reload()
}

function startEdit(item) {
  openEdit(item.id, {
    name: item.name,
    messengerKind: enumValue(messengerKinds, item.messengerKind, 1),
    externalChatId: String(item.externalChatId),
    isActive: item.isActive
  })
}

async function remove(id) {
  if (!(await dialog.confirmDelete('این کانال'))) return
  const ok = await trySubmit(async () => {
    await api.delete(ApiPaths.messageChannel(id))
  }, { successMessage: 'کانال حذف شد' })
  if (!ok) return
  await reload()
}

onMounted(() => load().catch(() => {}))
</script>

<template>
  <div>
    <PageHeader
      :title="pageTitle"
      :form-mode="showForm && !isMobile"
      :show-create="auth.hasPermission('messagechannels.create') && (!showForm || isMobile)"
      create-label="کانال جدید"
      @create="openCreate"
    />

    <FormHost :show="showForm" :title="isMobile ? (editing ? 'ویرایش کانال' : 'کانال جدید') : ''" @close="closeForm">
      <div v-if="error" class="form-error">{{ error }}</div>
      <form class="form-layout-adaptive" @submit.prevent="submit">
        <div class="form-group">
          <label>نام کانال *</label>
          <ClearableInput v-model="form.name" :invalid="!!errors.name" @input="clearFieldError('name')" />
          <div v-if="errors.name" class="field-error">{{ errors.name }}</div>
        </div>
        <div class="form-group">
          <label>پیام‌رسان</label>
          <AppSelect v-model="form.messengerKind" :options="messengerOptions" />
        </div>
        <div class="form-group">
          <label>شناسه گفتگو / کانال *</label>
          <ClearableInput
            v-model="form.externalChatId"
            dir="ltr"
            placeholder="مثلاً @channelname یا -1001234567890"
            :invalid="!!errors.externalChatId"
            @input="clearFieldError('externalChatId')"
          />
          <div v-if="errors.externalChatId" class="field-error">{{ errors.externalChatId }}</div>
          <p class="field-hint">شناسه عددی یا نام کاربری کانال/گروه</p>
        </div>
        <div class="form-group">
          <AppCheckbox v-model="form.isActive" label="فعال" />
        </div>
        <div class="modal-actions form-span-full">
          <button type="button" class="btn btn-outline" @click="closeForm">انصراف</button>
          <button type="submit" class="btn">ذخیره</button>
        </div>
      </form>
    </FormHost>

    <PagedListPanel
      v-show="!showForm || isMobile"
      :loading="loading"
      :skeleton-columns="5"
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
      <table class="mobile-table">
        <thead>
          <tr>
            <th>نام</th>
            <th>پیام‌رسان</th>
            <th>شناسه گفتگو</th>
            <th>وضعیت</th>
            <th v-if="auth.hasAnyPermission('messagechannels.update', 'messagechannels.delete', 'audit.view')"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in items" :key="item.id">
            <td data-label="نام">{{ item.name }}</td>
            <td data-label="پیام‌رسان">{{ messengerLabel(item.messengerKind) }}</td>
            <td data-label="شناسه گفتگو" dir="ltr">{{ item.externalChatId }}</td>
            <td data-label="وضعیت">{{ item.isActive ? 'فعال' : 'غیرفعال' }}</td>
            <td v-if="auth.hasAnyPermission('messagechannels.update', 'messagechannels.delete', 'audit.view')">
              <RowActions
                :show-edit="auth.hasPermission('messagechannels.update')"
                :show-delete="auth.hasPermission('messagechannels.delete')"
                :show-audit="auth.hasPermission('audit.view')"
                :audit="item.audit"
                @edit="startEdit(item)"
                @delete="remove(item.id)"
              />
            </td>
          </tr>
        </tbody>
      </table>
      <div v-if="!items.length" class="empty-state">کانالی ثبت نشده</div>
    </PagedListPanel>
  </div>
</template>
