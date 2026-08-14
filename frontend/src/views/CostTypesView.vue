<script setup>
import { computed, ref, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import api from '../api/client'
import { useAuthStore } from '../stores/auth'
import { useDialogStore } from '../stores/dialog'
import { useLookupsStore } from '../stores/lookups'
import { useFormValidation } from '../composables/useFormValidation'
import { useIsMobile } from '../composables/useMediaQuery'
import AppSelect from '../components/AppSelect.vue'
import AppCheckbox from '../components/AppCheckbox.vue'
import ClearableInput from '../components/ClearableInput.vue'
import FormHost from '../components/FormHost.vue'
import RowActions from '../components/RowActions.vue'

const auth = useAuthStore()
const dialog = useDialogStore()
const lookups = useLookupsStore()
const router = useRouter()
const isMobile = useIsMobile()
const { error, errors, validate, trySubmit, clearErrors, clearFieldError } = useFormValidation()

const items = ref([])
const units = ref([])
const unitOptions = computed(() => {
  const active = units.value.filter((u) => u.isActive)
  const selectedId = form.value.unitId
  if (!selectedId) return active
  const selected = units.value.find((u) => String(u.id) === String(selectedId))
  if (selected && !selected.isActive && !active.some((u) => u.id === selected.id)) {
    return [...active, selected]
  }
  return active
})

const showForm = ref(false)
const editing = ref(null)
const form = ref({ name: '', description: '', isIngredient: false, unitId: '', isActive: true })
const canCreate = computed(() => auth.hasPermission('costtypes.create'))
const canUpdate = computed(() => auth.hasPermission('costtypes.update'))
const canDelete = computed(() => auth.hasPermission('costtypes.delete'))
const canManageUnits = computed(() =>
  auth.hasAnyPermission('generaltypes.view', 'generaltypes.create', 'generaltypes.update', 'generaltypes.delete')
)

function rules() {
  const r = {
    name: [{ type: 'required', msg: 'نام الزامی است' }]
  }
  if (form.value.isIngredient) {
    r.unitId = [{ type: 'required', msg: 'انتخاب واحد الزامی است' }]
  }
  return r
}

async function load() {
  const [c, u] = await Promise.all([
    lookups.getCostTypes({ activeOnly: false, force: true, admin: true }),
    lookups.getGeneralTypes('Unit', { includeInactive: true, admin: true })
  ])
  items.value = c
  units.value = u
}

async function submit() {
  if (!validate(rules(), form.value)) return
  const payload = {
    ...form.value,
    unitId: form.value.isIngredient && form.value.unitId ? +form.value.unitId : null
  }
  const ok = await trySubmit(async () => {
    if (editing.value) {
      await api.put(`/cost-types/${editing.value}`, payload)
    } else {
      await api.post('/cost-types', payload)
    }
  }, { successMessage: editing.value ? 'نوع هزینه ویرایش شد' : 'نوع هزینه ایجاد شد' })
  if (!ok) return
  lookups.invalidateCostTypes()
  closeForm()
  await load()
}

function openCreate() {
  editing.value = null
  form.value = { name: '', description: '', isIngredient: false, unitId: '', isActive: true }
  clearErrors()
  showForm.value = true
}

function openEdit(item) {
  editing.value = item.id
  form.value = {
    name: item.name,
    description: item.description || '',
    isIngredient: item.isIngredient,
    unitId: item.unitId || '',
    isActive: item.isActive
  }
  clearErrors()
  showForm.value = true
}

function closeForm() {
  showForm.value = false
}

async function remove(id) {
  if (!(await dialog.confirmDelete('این نوع هزینه'))) return
  const ok = await trySubmit(async () => {
    await api.delete(`/cost-types/${id}`)
  }, { successMessage: 'نوع هزینه حذف شد' })
  if (!ok) return
  lookups.invalidateCostTypes()
  await load()
}

watch(() => form.value.isIngredient, (v) => {
  if (!v) {
    form.value.unitId = ''
    clearFieldError('unitId')
  }
})

onMounted(load)
</script>

<template>
  <div>
    <div class="page-header" :class="{ 'form-mode': showForm && !isMobile }">
      <h1 class="page-title">{{ showForm && !isMobile ? (editing ? 'ویرایش' : 'نوع هزینه جدید') : 'انواع هزینه' }}</h1>
      <button
        v-if="canCreate && (!showForm || isMobile)"
        class="btn btn-fab-mobile"
        @click="openCreate"
      >
        <span aria-hidden="true">+</span>
        <span class="btn-fab-label">نوع جدید</span>
      </button>
    </div>

    <FormHost :show="showForm" :title="isMobile ? (editing ? 'ویرایش' : 'نوع هزینه جدید') : ''" @close="closeForm">
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
          <AppCheckbox v-model="form.isIngredient" label="مواد اولیه (برای تهیه غذا)" />
        </div>
        <div v-if="form.isIngredient" class="form-group">
          <label>واحد *</label>
          <AppSelect
            v-model="form.unitId"
            :options="unitOptions"
            option-value="id"
            option-label="name"
            placeholder="انتخاب کنید"
            :invalid="!!errors.unitId"
            @change="clearFieldError('unitId')"
          />
          <div v-if="errors.unitId" class="field-error">{{ errors.unitId }}</div>
          <button
            v-if="canManageUnits"
            type="button"
            class="link-btn"
            @click="router.push({ path: '/general-types', query: { category: 'Unit' } })"
          >
            مدیریت واحدها
          </button>
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

    <div v-show="!showForm || isMobile" class="card list-panel">
      <table class="mobile-table">
        <thead>
          <tr>
            <th>نام</th>
            <th class="hide-mobile">مواد اولیه</th>
            <th class="hide-mobile">واحد</th>
            <th class="hide-mobile">وضعیت</th>
            <th v-if="canUpdate || canDelete"></th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="item in items"
            :key="item.id"
            :class="item.isActive ? 'type-row-active' : 'type-row-inactive'"
          >
            <td data-label="نام">
              <span class="cost-type-title">
                <strong>{{ item.name }}</strong>
                <span
                  v-if="item.isIngredient && item.unitName"
                  class="badge unit-badge"
                >{{ item.unitName }}</span>
              </span>
            </td>
            <td data-label="مواد اولیه" class="hide-mobile">{{ item.isIngredient ? '✓' : '—' }}</td>
            <td data-label="واحد" class="hide-mobile">{{ item.unitName || '—' }}</td>
            <td data-label="وضعیت" class="hide-mobile">
              <span :class="item.isActive ? 'badge badge-success' : 'badge badge-danger'">
                {{ item.isActive ? 'فعال' : 'غیرفعال' }}
              </span>
            </td>
            <td v-if="canUpdate || canDelete">
              <RowActions
                :show-edit="canUpdate"
                :show-delete="canDelete"
                @edit="openEdit(item)"
                @delete="remove(item.id)"
              />
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.link-btn {
  margin-top: 0.45rem;
  border: none;
  background: none;
  color: var(--primary);
  font: inherit;
  font-size: 0.82rem;
  font-weight: 600;
  cursor: pointer;
  padding: 0;
}
.link-btn:hover { text-decoration: underline; }
.cost-type-title {
  display: inline-flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 0.4rem;
  min-width: 0;
}
.unit-badge {
  display: none;
}

@media (max-width: 768px) {
  .unit-badge {
    display: inline-flex;
    flex-shrink: 0;
    font-size: 0.68rem;
    padding: 0.12rem 0.45rem;
    background: color-mix(in srgb, var(--primary) 14%, transparent);
    color: var(--primary);
    border: 1px solid color-mix(in srgb, var(--primary) 28%, transparent);
  }
  .mobile-table tbody tr.type-row-active {
    box-shadow:
      inset -4px 0 0 var(--success),
      0 1px 2px rgba(0, 0, 0, 0.04);
  }
  .mobile-table tbody tr.type-row-inactive {
    box-shadow:
      inset -4px 0 0 var(--danger),
      0 1px 2px rgba(0, 0, 0, 0.04);
  }
}
</style>
