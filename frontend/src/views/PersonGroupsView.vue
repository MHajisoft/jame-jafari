<script setup>
import { computed, onMounted, ref } from 'vue'
import api from '../api/client'
import { ApiPaths } from '../api/paths'
import { useAuthStore } from '../stores/auth'
import { useDialogStore } from '../stores/dialog'
import { useFormValidation } from '../composables/useFormValidation'
import { useEntityForm } from '../composables/useEntityForm'
import { usePagedList } from '../composables/usePagedList'
import { useIsMobile } from '../composables/useMediaQuery'
import AppCheckbox from '../components/AppCheckbox.vue'
import ClearableInput from '../components/ClearableInput.vue'
import FormHost from '../components/FormHost.vue'
import PersonSelect from '../components/PersonSelect.vue'
import RowActions from '../components/RowActions.vue'
import PageHeader from '../components/PageHeader.vue'
import PagedListPanel from '../components/PagedListPanel.vue'

const auth = useAuthStore()
const dialog = useDialogStore()
const isMobile = useIsMobile()
const { error, errors, validate, trySubmit, clearErrors, clearFieldError } = useFormValidation()

const pickerPersonId = ref('')
const memberLabels = ref({})

const { showForm, editing, form, openCreate, openEdit, closeForm } = useEntityForm(
  () => ({ name: '', description: '', isActive: true, personIds: [] }),
  {
    onReset: () => {
      clearErrors()
      pickerPersonId.value = ''
      memberLabels.value = {}
    }
  }
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
  const { data } = await api.get(ApiPaths.personGroups, {
    params: { activeOnly: false, page, pageSize }
  })
  return data
})

const rules = {
  name: [{ type: 'required', msg: 'نام گروه الزامی است' }]
}

const pageTitle = computed(() => {
  if (showForm.value && !isMobile.value) {
    return editing.value ? 'ویرایش گروه' : 'گروه جدید'
  }
  return 'گروه‌های اشخاص'
})

async function addMember() {
  const id = Number(pickerPersonId.value)
  if (!id || form.value.personIds.includes(id)) {
    pickerPersonId.value = ''
    return
  }
  try {
    const { data } = await api.get(ApiPaths.person(id))
    memberLabels.value[id] = [data.firstName, data.lastName].filter(Boolean).join(' ')
  } catch {
    memberLabels.value[id] = `شخص #${id}`
  }
  form.value.personIds.push(id)
  pickerPersonId.value = ''
}

function removeMember(id) {
  form.value.personIds = form.value.personIds.filter((x) => x !== id)
  delete memberLabels.value[id]
}

async function submit() {
  if (!validate(rules, form.value)) return
  const payload = {
    name: form.value.name,
    description: form.value.description || null,
    isActive: form.value.isActive,
    personIds: form.value.personIds
  }
  const ok = await trySubmit(async () => {
    if (editing.value) {
      await api.put(ApiPaths.personGroup(editing.value), payload)
    } else {
      await api.post(ApiPaths.personGroups, payload)
    }
  }, { successMessage: editing.value ? 'گروه ویرایش شد' : 'گروه ایجاد شد' })
  if (!ok) return
  closeForm()
  await reload()
}

function startEdit(item) {
  openEdit(item.id, {
    name: item.name,
    description: item.description || '',
    isActive: item.isActive,
    personIds: [...(item.personIds || [])]
  })
}

async function remove(id) {
  if (!(await dialog.confirmDelete('این گروه'))) return
  const ok = await trySubmit(async () => {
    await api.delete(ApiPaths.personGroup(id))
  }, { successMessage: 'گروه حذف شد' })
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
      :show-create="auth.hasPermission('persongroups.create') && (!showForm || isMobile)"
      create-label="گروه جدید"
      @create="openCreate"
    />

    <FormHost :show="showForm" :title="isMobile ? (editing ? 'ویرایش گروه' : 'گروه جدید') : ''" @close="closeForm">
      <div v-if="error" class="form-error">{{ error }}</div>
      <form class="form-layout-adaptive" @submit.prevent="submit">
        <div class="form-group">
          <label>نام گروه *</label>
          <ClearableInput v-model="form.name" :invalid="!!errors.name" @input="clearFieldError('name')" />
          <div v-if="errors.name" class="field-error">{{ errors.name }}</div>
        </div>
        <div class="form-group form-span-full">
          <label>توضیحات</label>
          <ClearableInput v-model="form.description" type="textarea" :rows="2" />
        </div>
        <div class="form-group form-span-full">
          <label>افزودن عضو</label>
          <div class="member-picker">
            <PersonSelect v-model="pickerPersonId" />
            <button type="button" class="btn btn-outline btn-sm" @click="addMember">افزودن</button>
          </div>
          <ul v-if="form.personIds.length" class="member-list">
            <li v-for="id in form.personIds" :key="id">
              <span>{{ memberLabels[id] || `شخص #${id}` }}</span>
              <button type="button" class="btn btn-outline btn-sm" @click="removeMember(id)">حذف</button>
            </li>
          </ul>
          <p v-else class="field-hint">هنوز عضوی اضافه نشده</p>
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
      :skeleton-columns="4"
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
            <th>اعضا</th>
            <th>وضعیت</th>
            <th v-if="auth.hasAnyPermission('persongroups.update', 'persongroups.delete', 'audit.view')"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in items" :key="item.id">
            <td data-label="نام">
              {{ item.name }}
              <p v-if="item.description" class="field-hint">{{ item.description }}</p>
            </td>
            <td data-label="اعضا">{{ item.memberCount }}</td>
            <td data-label="وضعیت">{{ item.isActive ? 'فعال' : 'غیرفعال' }}</td>
            <td v-if="auth.hasAnyPermission('persongroups.update', 'persongroups.delete', 'audit.view')">
              <RowActions
                :show-edit="auth.hasPermission('persongroups.update')"
                :show-delete="auth.hasPermission('persongroups.delete')"
                :show-audit="auth.hasPermission('audit.view')"
                :audit="item.audit"
                @edit="startEdit(item)"
                @delete="remove(item.id)"
              />
            </td>
          </tr>
        </tbody>
      </table>
      <div v-if="!items.length" class="empty-state">گروهی ثبت نشده</div>
    </PagedListPanel>
  </div>
</template>

<style scoped>
.member-picker {
  display: flex;
  gap: 0.5rem;
  align-items: flex-start;
}

.member-picker :deep(.person-select) {
  flex: 1;
}

.member-list {
  list-style: none;
  padding: 0;
  margin: 0.75rem 0 0;
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
}

.member-list li {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 0.5rem;
}
</style>
