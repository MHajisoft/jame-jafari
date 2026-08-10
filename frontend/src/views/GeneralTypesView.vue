<script setup>
import { computed, ref, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import api from '../api/client'
import { useAuthStore } from '../stores/auth'
import { useFormValidation } from '../composables/useFormValidation'
import { useIsMobile } from '../composables/useMediaQuery'
import AppCheckbox from '../components/AppCheckbox.vue'
import ClearableInput from '../components/ClearableInput.vue'
import FormHost from '../components/FormHost.vue'
import RowActions from '../components/RowActions.vue'

const CATEGORIES = [
  { key: 'Unit', label: 'واحدها', singular: 'واحد' },
  { key: 'NamePrefix', label: 'پیشوند نام', singular: 'پیشوند' }
]

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()
const isMobile = useIsMobile()
const { error, errors, validate, trySubmit, clearErrors, clearFieldError } = useFormValidation()

const category = ref(resolveInitialCategory())
const items = ref([])
const showForm = ref(false)
const editing = ref(null)
const form = ref({ name: '', code: '', sortOrder: 0, isActive: true })

const canCreate = computed(() => auth.hasPermission('generaltypes.create'))
const canUpdate = computed(() => auth.hasPermission('generaltypes.update'))
const canDelete = computed(() => auth.hasPermission('generaltypes.delete'))

const currentMeta = computed(() =>
  CATEGORIES.find((c) => c.key === category.value) || CATEGORIES[0]
)

const rules = {
  name: [{ type: 'required', msg: 'نام الزامی است' }]
}

function resolveInitialCategory() {
  const q = String(route.query.category || '')
  if (CATEGORIES.some((c) => c.key === q)) return q
  return 'Unit'
}

async function load() {
  const { data } = await api.get('/general-types', {
    params: { category: category.value, includeInactive: true }
  })
  items.value = data
}

async function submit() {
  if (!validate(rules, form.value)) return
  const payload = {
    name: form.value.name,
    code: form.value.code || null,
    category: category.value,
    sortOrder: Number(form.value.sortOrder) || 0,
    isActive: form.value.isActive
  }
  const ok = await trySubmit(async () => {
    if (editing.value) {
      await api.put(`/general-types/${editing.value}`, {
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
  closeForm()
  await load()
}

function openCreate() {
  editing.value = null
  form.value = {
    name: '',
    code: '',
    sortOrder: items.value.length + 1,
    isActive: true
  }
  clearErrors()
  showForm.value = true
}

function openEdit(item) {
  editing.value = item.id
  form.value = {
    name: item.name,
    code: item.code || '',
    sortOrder: item.sortOrder ?? 0,
    isActive: item.isActive
  }
  clearErrors()
  showForm.value = true
}

function closeForm() {
  showForm.value = false
}

async function remove(id) {
  if (!confirm(`حذف این ${currentMeta.value.singular}؟`)) return
  await api.delete(`/general-types/${id}`)
  await load()
}

function switchCategory(next) {
  if (category.value === next) return
  category.value = next
  showForm.value = false
  clearErrors()
  router.replace({ query: { ...route.query, category: next } })
}

watch(category, () => {
  load()
})

onMounted(load)
</script>

<template>
  <div class="general-types-page">
    <div class="page-header" :class="{ 'form-mode': showForm && !isMobile }">
      <h1 class="page-title">
        <template v-if="showForm && !isMobile">
          {{ editing ? `ویرایش ${currentMeta.singular}` : `${currentMeta.singular} جدید` }}
        </template>
        <template v-else>انواع عمومی</template>
      </h1>
      <button
        v-if="canCreate && (!showForm || isMobile)"
        class="btn btn-fab-mobile"
        @click="openCreate"
      >
        <span aria-hidden="true">+</span>
        <span class="btn-fab-label">{{ currentMeta.singular }} جدید</span>
      </button>
    </div>

    <div v-show="!showForm || isMobile" class="page-tabs-wrap">
      <div class="page-tabs">
        <button
          v-for="cat in CATEGORIES"
          :key="cat.key"
          type="button"
          class="page-tab"
          :class="{ active: category === cat.key }"
          @click="switchCategory(cat.key)"
        >
          {{ cat.label }}
        </button>
      </div>
    </div>

    <FormHost
      :show="showForm"
      :title="isMobile ? (editing ? `ویرایش ${currentMeta.singular}` : `${currentMeta.singular} جدید`) : ''"
      @close="closeForm"
    >
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
        <div class="grid-2">
          <div class="form-group">
            <label>کد <span class="optional">(اختیاری)</span></label>
            <ClearableInput v-model="form.code" />
          </div>
          <div class="form-group">
            <label>ترتیب</label>
            <ClearableInput v-model="form.sortOrder" inputmode="numeric" />
          </div>
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
            <th>کد</th>
            <th>ترتیب</th>
            <th>وضعیت</th>
            <th v-if="canUpdate || canDelete"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in items" :key="item.id">
            <td data-label="نام"><strong>{{ item.name }}</strong></td>
            <td data-label="کد">{{ item.code || '—' }}</td>
            <td data-label="ترتیب">{{ item.sortOrder }}</td>
            <td data-label="وضعیت">
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
          <tr v-if="!items.length">
            <td :colspan="(canUpdate || canDelete) ? 5 : 4" class="text-muted" style="text-align:center">
              هنوز موردی تعریف نشده است
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

@media (max-width: 768px) {
  .general-types-page {
    padding-top: 3.55rem;
  }
  .general-types-page .page-header {
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
