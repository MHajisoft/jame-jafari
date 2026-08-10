<script setup>
import { computed, ref, onMounted, watch } from 'vue'
import api from '../api/client'
import { useAuthStore } from '../stores/auth'
import { useFormValidation } from '../composables/useFormValidation'
import { useIsMobile } from '../composables/useMediaQuery'
import AppSelect from '../components/AppSelect.vue'
import AppCheckbox from '../components/AppCheckbox.vue'
import ClearableInput from '../components/ClearableInput.vue'
import FormHost from '../components/FormHost.vue'

const auth = useAuthStore()
const isMobile = useIsMobile()
const { error, errors, validate, trySubmit, clearErrors, clearFieldError } = useFormValidation()

const tab = ref('cost-types') // cost-types | units
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

const showUnitForm = ref(false)
const editingUnit = ref(null)
const unitForm = ref({ name: '', code: '', sortOrder: 0, isActive: true })

const canManage = computed(() => auth.hasPermission('costtypes.manage'))

function costTypeRules() {
  const r = {
    name: [{ type: 'required', msg: 'نام الزامی است' }]
  }
  if (form.value.isIngredient) {
    r.unitId = [{ type: 'required', msg: 'انتخاب واحد الزامی است' }]
  }
  return r
}

const unitRules = {
  name: [{ type: 'required', msg: 'نام واحد الزامی است' }]
}

async function load() {
  const [c, u] = await Promise.all([
    api.get('/cost-types', { params: { activeOnly: false } }),
    api.get('/general-types', { params: { category: 'Unit', includeInactive: true } })
  ])
  items.value = c.data
  units.value = u.data
}

async function submitCostType() {
  if (!validate(costTypeRules(), form.value)) return
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
  })
  if (!ok) return
  closeCostTypeForm()
  await load()
}

function openCreateCostType() {
  editing.value = null
  form.value = { name: '', description: '', isIngredient: false, unitId: '', isActive: true }
  clearErrors()
  showForm.value = true
}

function openEditCostType(item) {
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

function closeCostTypeForm() {
  showForm.value = false
}

async function removeCostType(id) {
  if (!confirm('حذف این نوع هزینه؟')) return
  await api.delete(`/cost-types/${id}`)
  await load()
}

async function submitUnit() {
  if (!validate(unitRules, unitForm.value)) return
  const payload = {
    name: unitForm.value.name,
    code: unitForm.value.code || null,
    category: 'Unit',
    sortOrder: Number(unitForm.value.sortOrder) || 0,
    isActive: unitForm.value.isActive
  }
  const ok = await trySubmit(async () => {
    if (editingUnit.value) {
      await api.put(`/general-types/${editingUnit.value}`, {
        name: payload.name,
        code: payload.code,
        sortOrder: payload.sortOrder,
        isActive: payload.isActive
      })
    } else {
      await api.post('/general-types', payload)
    }
  })
  if (!ok) return
  closeUnitForm()
  await load()
}

function openCreateUnit() {
  editingUnit.value = null
  unitForm.value = { name: '', code: '', sortOrder: units.value.length + 1, isActive: true }
  clearErrors()
  showUnitForm.value = true
}

function openEditUnit(item) {
  editingUnit.value = item.id
  unitForm.value = {
    name: item.name,
    code: item.code || '',
    sortOrder: item.sortOrder ?? 0,
    isActive: item.isActive
  }
  clearErrors()
  showUnitForm.value = true
}

function closeUnitForm() {
  showUnitForm.value = false
}

async function removeUnit(id) {
  if (!confirm('حذف این واحد؟')) return
  await api.delete(`/general-types/${id}`)
  await load()
}

function switchTab(next) {
  if (tab.value === next) return
  tab.value = next
  showForm.value = false
  showUnitForm.value = false
  clearErrors()
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
  <div class="cost-types-page">
    <div class="page-header" :class="{ 'form-mode': (showForm || showUnitForm) && !isMobile }">
      <h1 class="page-title">
        <template v-if="showForm && !isMobile">{{ editing ? 'ویرایش نوع هزینه' : 'نوع هزینه جدید' }}</template>
        <template v-else-if="showUnitForm && !isMobile">{{ editingUnit ? 'ویرایش واحد' : 'واحد جدید' }}</template>
        <template v-else>انواع هزینه</template>
      </h1>
      <button
        v-if="canManage && tab === 'cost-types' && (!showForm || isMobile)"
        class="btn btn-fab-mobile"
        @click="openCreateCostType"
      >
        <span aria-hidden="true">+</span>
        <span class="btn-fab-label">نوع جدید</span>
      </button>
      <button
        v-if="canManage && tab === 'units' && (!showUnitForm || isMobile)"
        class="btn btn-fab-mobile"
        @click="openCreateUnit"
      >
        <span aria-hidden="true">+</span>
        <span class="btn-fab-label">واحد جدید</span>
      </button>
    </div>

    <div v-show="(!showForm && !showUnitForm) || isMobile" class="page-tabs-wrap">
      <div class="page-tabs">
        <button
          type="button"
          class="page-tab"
          :class="{ active: tab === 'cost-types' }"
          @click="switchTab('cost-types')"
        >
          انواع هزینه
        </button>
        <button
          type="button"
          class="page-tab"
          :class="{ active: tab === 'units' }"
          @click="switchTab('units')"
        >
          واحدها
        </button>
      </div>
    </div>

    <FormHost
      v-if="tab === 'cost-types'"
      :show="showForm"
      :title="isMobile ? (editing ? 'ویرایش' : 'نوع هزینه جدید') : ''"
      @close="closeCostTypeForm"
    >
      <div v-if="error" class="form-error">{{ error }}</div>
      <form @submit.prevent="submitCostType">
        <div class="form-group">
          <label>نام *</label>
          <ClearableInput
            v-model="form.name"
            :invalid="!!errors.name"
            @input="clearFieldError('name')"
          />
          <div v-if="errors.name" class="field-error">{{ errors.name }}</div>
        </div>
        <div class="form-group">
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
            v-if="canManage"
            type="button"
            class="link-btn"
            @click="switchTab('units')"
          >
            مدیریت واحدها
          </button>
        </div>
        <div class="form-group">
          <AppCheckbox v-model="form.isActive" label="فعال" />
        </div>
        <div class="modal-actions">
          <button type="button" class="btn btn-outline" @click="closeCostTypeForm">انصراف</button>
          <button type="submit" class="btn">ذخیره</button>
        </div>
      </form>
    </FormHost>

    <FormHost
      v-if="tab === 'units'"
      :show="showUnitForm"
      :title="isMobile ? (editingUnit ? 'ویرایش واحد' : 'واحد جدید') : ''"
      @close="closeUnitForm"
    >
      <div v-if="error" class="form-error">{{ error }}</div>
      <form @submit.prevent="submitUnit">
        <div class="form-group">
          <label>نام *</label>
          <ClearableInput
            v-model="unitForm.name"
            :invalid="!!errors.name"
            @input="clearFieldError('name')"
          />
          <div v-if="errors.name" class="field-error">{{ errors.name }}</div>
        </div>
        <div class="grid-2">
          <div class="form-group">
            <label>کد <span class="optional">(اختیاری)</span></label>
            <ClearableInput v-model="unitForm.code" />
          </div>
          <div class="form-group">
            <label>ترتیب</label>
            <ClearableInput v-model="unitForm.sortOrder" inputmode="numeric" />
          </div>
        </div>
        <div class="form-group">
          <AppCheckbox v-model="unitForm.isActive" label="فعال" />
        </div>
        <div class="modal-actions">
          <button type="button" class="btn btn-outline" @click="closeUnitForm">انصراف</button>
          <button type="submit" class="btn">ذخیره</button>
        </div>
      </form>
    </FormHost>

    <div v-show="tab === 'cost-types' && (!showForm || isMobile)" class="card list-panel">
      <table class="mobile-table">
        <thead>
          <tr>
            <th>نام</th>
            <th>مواد اولیه</th>
            <th>واحد</th>
            <th>وضعیت</th>
            <th v-if="canManage"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in items" :key="item.id">
            <td data-label="نام"><strong>{{ item.name }}</strong></td>
            <td data-label="مواد اولیه">{{ item.isIngredient ? '✓' : '—' }}</td>
            <td data-label="واحد">{{ item.unitName || '—' }}</td>
            <td data-label="وضعیت">
              <span :class="item.isActive ? 'badge badge-success' : 'badge badge-danger'">
                {{ item.isActive ? 'فعال' : 'غیرفعال' }}
              </span>
            </td>
            <td v-if="canManage">
              <div class="table-actions">
                <button class="btn btn-sm btn-outline" @click="openEditCostType(item)">ویرایش</button>
                <button class="btn btn-sm btn-danger" @click="removeCostType(item.id)">حذف</button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-show="tab === 'units' && (!showUnitForm || isMobile)" class="card list-panel">
      <table class="mobile-table">
        <thead>
          <tr>
            <th>نام</th>
            <th>کد</th>
            <th>ترتیب</th>
            <th>وضعیت</th>
            <th v-if="canManage"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in units" :key="item.id">
            <td data-label="نام"><strong>{{ item.name }}</strong></td>
            <td data-label="کد">{{ item.code || '—' }}</td>
            <td data-label="ترتیب">{{ item.sortOrder }}</td>
            <td data-label="وضعیت">
              <span :class="item.isActive ? 'badge badge-success' : 'badge badge-danger'">
                {{ item.isActive ? 'فعال' : 'غیرفعال' }}
              </span>
            </td>
            <td v-if="canManage">
              <div class="table-actions">
                <button class="btn btn-sm btn-outline" @click="openEditUnit(item)">ویرایش</button>
                <button class="btn btn-sm btn-danger" @click="removeUnit(item.id)">حذف</button>
              </div>
            </td>
          </tr>
          <tr v-if="!units.length">
            <td :colspan="canManage ? 5 : 4" class="text-muted" style="text-align:center">
              هنوز واحدی تعریف نشده است
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<style scoped>
.page-tabs-wrap {
  margin-bottom: 1rem;
}
.page-tabs {
  display: inline-flex;
  gap: 0.35rem;
  padding: 0.3rem;
  background: var(--surface);
  border: 1px solid var(--border);
  border-radius: 12px;
}
.page-tab {
  border: none;
  background: transparent;
  color: var(--text-muted);
  padding: 0.45rem 0.95rem;
  border-radius: 9px;
  cursor: pointer;
  font: inherit;
  font-weight: 600;
  font-size: 0.9rem;
}
.page-tab.active {
  background: color-mix(in srgb, var(--primary) 16%, transparent);
  color: var(--primary);
}
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

@media (max-width: 768px) {
  .cost-types-page {
    /* space for fixed tabs under the top bar */
    padding-top: 3.55rem;
  }
  .cost-types-page .page-header {
    margin-bottom: 0;
    min-height: 0;
  }
  .page-tabs-wrap {
    position: fixed;
    top: calc(56px + env(safe-area-inset-top, 0));
    left: 0;
    right: 0;
    z-index: 190;
    margin: 0;
    padding: 0.55rem 1rem 0.65rem;
    background: var(--bg);
    border-bottom: 1px solid var(--border);
  }
  .page-tabs {
    display: flex;
    width: 100%;
    margin: 0;
  }
  .page-tab {
    flex: 1;
    text-align: center;
    min-height: 40px;
  }
}
</style>
