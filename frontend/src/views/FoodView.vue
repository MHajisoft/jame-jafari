<script setup>
import { computed, ref, onMounted } from 'vue'
import api from '../api/client'
import { ApiPaths } from '../api/paths'
import { formatMoney, toInputDate } from '../utils/format'
import { todayGregorian } from '../utils/jalali'
import { useAuthStore } from '../stores/auth'
import { useLookupsStore } from '../stores/lookups'
import { useFormValidation } from '../composables/useFormValidation'
import { useEntityForm } from '../composables/useEntityForm'
import { useIsMobile } from '../composables/useMediaQuery'
import DateDisplay from '../components/DateDisplay.vue'
import PersianDatePicker from '../components/PersianDatePicker.vue'
import CurrencyInput from '../components/CurrencyInput.vue'
import AppSelect from '../components/AppSelect.vue'
import ClearableInput from '../components/ClearableInput.vue'
import FormHost from '../components/FormHost.vue'
import RowActions from '../components/RowActions.vue'

const auth = useAuthStore()
const lookups = useLookupsStore()
const isMobile = useIsMobile()
const { error, errors, validate, trySubmit, clearErrors, clearFieldError } = useFormValidation()
const items = ref([])
const ingredients = ref([])
const recommendations = ref([])
const loading = ref(false)
const formLookupsReady = ref(false)
const cookDate = ref(todayGregorian())

function blankForm() {
  return {
    name: '',
    cookDate: cookDate.value,
    totalCount: '',
    description: '',
    ingredientRows: [{ costTypeId: '', units: '', price: '' }]
  }
}

const { showForm, editing, form, openCreate, openEdit, closeForm } = useEntityForm(blankForm, {
  onReset: clearErrors
})

const pageTitle = computed(() => {
  if (showForm.value && !isMobile.value) {
    return editing.value ? 'ویرایش تهیه غذا' : 'ثبت تهیه غذا'
  }
  return 'تهیه غذا'
})

const formTitle = computed(() => (editing.value ? 'ویرایش تهیه غذا' : 'ثبت تهیه غذا'))

function getRules() {
  return {
    name: [{ type: 'required', msg: 'نام غذا الزامی است' }],
    cookDate: [{ type: 'required', msg: 'تاریخ پخت الزامی است' }],
    totalCount: [{ type: 'positiveNumber', msg: 'تعداد باید حداقل ۱ باشد' }],
    ingredientRows: [
      (val) => {
        if (!val?.length) return 'حداقل یک ماده اولیه الزامی است'
        const valid = val.some(r => r.costTypeId && r.units && r.price && +r.units > 0 && +r.price > 0)
        if (!valid) return 'هر ماده اولیه باید نوع، مقدار و قیمت معتبر داشته باشد'
        return null
      }
    ]
  }
}

const recommendationMap = computed(() => {
  const map = new Map()
  for (const rec of recommendations.value) map.set(rec.costTypeId, rec)
  return map
})

const ingredientMap = computed(() => {
  const map = new Map()
  for (const ing of ingredients.value) map.set(ing.id, ing)
  return map
})

function ingredientUnit(costTypeId) {
  const id = +costTypeId
  if (!id) return null
  return ingredientMap.value.get(id)?.unitName
    || recommendationMap.value.get(id)?.unitName
    || null
}

function recommendedUnitPrice(costTypeId) {
  const rec = recommendationMap.value.get(+costTypeId)
  if (!rec?.recommendedPrice || +rec.recommendedPrice <= 0) return null
  return rec.recommendedPrice
}

function recommendedTotal(row) {
  const unitPrice = recommendedUnitPrice(row.costTypeId)
  if (!unitPrice || !row.units || +row.units <= 0) return null
  return unitPrice * +row.units
}

async function load() {
  loading.value = true
  formLookupsReady.value = false
  try {
    const [f, rec] = await Promise.all([
      api.get(ApiPaths.food, { params: { date: new Date(cookDate.value).toISOString() } }),
      api.get(ApiPaths.foodRecommendations)
    ])
    items.value = f.data
    recommendations.value = rec.data
    try {
      ingredients.value = await lookups.getCostTypes({ isIngredient: true, activeOnly: true })
      formLookupsReady.value = true
    } catch {
      ingredients.value = []
    }
  } finally {
    loading.value = false
  }
}

function addRow() {
  form.value.ingredientRows.push({ costTypeId: '', units: '', price: '' })
}

function removeRow(index) {
  if (form.value.ingredientRows.length <= 1) return
  form.value.ingredientRows.splice(index, 1)
}

function onIngredientChange(row) {
  row.units = ''
  row.price = ''
}

function handleOpenCreate() {
  openCreate()
  form.value.cookDate = cookDate.value
}

function startEdit(food) {
  openEdit(food.id, {
    name: food.name,
    cookDate: toInputDate(food.cookDate),
    totalCount: String(food.totalCount),
    description: food.description || '',
    ingredientRows: food.ingredients.length
      ? food.ingredients.map(ing => ({
          costTypeId: String(ing.costTypeId),
          units: String(ing.units),
          price: ing.price
        }))
      : [{ costTypeId: '', units: '', price: '' }]
  })
}

async function submit() {
  if (!validate(getRules(), form.value)) return
  const payload = {
    name: form.value.name,
    cookDate: new Date(form.value.cookDate).toISOString(),
    totalCount: +form.value.totalCount,
    description: form.value.description || null,
    ingredients: form.value.ingredientRows
      .filter(r => r.costTypeId)
      .map(r => ({
        costTypeId: +r.costTypeId,
        units: +r.units,
        price: +r.price
      }))
  }
  const ok = await trySubmit(async () => {
    if (editing.value) {
      await api.put(ApiPaths.foodItem(editing.value), payload)
    } else {
      await api.post(ApiPaths.food, payload)
    }
  }, {
    successMessage: editing.value ? 'غذا ویرایش شد' : 'غذا با موفقیت ثبت شد'
  })
  if (!ok) return
  closeForm()
  cookDate.value = form.value.cookDate
  await load()
}

onMounted(load)
</script>

<template>
  <div class="food-page">
    <div class="page-header" :class="{ 'form-mode': showForm && !isMobile }">
      <h1 class="page-title">{{ pageTitle }}</h1>
      <div v-if="!showForm || isMobile" class="page-toolbar food-toolbar">
        <PersianDatePicker
          v-model="cookDate"
          variant="bar"
          placeholder="تاریخ پخت"
          required
          @change="load"
        />
        <button
          v-if="auth.hasPermission('food.create') && formLookupsReady"
          type="button"
          class="btn btn-fab-mobile"
          @click="handleOpenCreate"
        >
          <span aria-hidden="true">+</span>
          <span class="btn-fab-label">غذای جدید</span>
        </button>
      </div>
    </div>

    <p
      v-if="auth.hasPermission('food.create') && !loading && !formLookupsReady"
      class="form-error list-lookup-hint"
    >
      بارگذاری لیست مواد اولیه ممکن نشد؛ ثبت غذای جدید غیرفعال است.
    </p>

    <FormHost :show="showForm" :title="isMobile ? formTitle : ''" @close="closeForm">
      <div v-if="error" class="form-error">{{ error }}</div>
      <form class="form-layout-adaptive" @submit.prevent="submit">
        <div class="form-group">
          <label>نام غذا *</label>
          <ClearableInput
            v-model="form.name"
            :invalid="!!errors.name"
            @input="clearFieldError('name')"
          />
          <div v-if="errors.name" class="field-error">{{ errors.name }}</div>
        </div>
        <div class="form-group">
          <label>تاریخ</label>
          <PersianDatePicker v-model="form.cookDate" required />
        </div>
        <div class="form-group">
          <label>تعداد پخته شده *</label>
          <ClearableInput
            v-model="form.totalCount"
            type="number"
            :min="1"
            inputmode="numeric"
            placeholder="مثلاً ۵۰"
            :invalid="!!errors.totalCount"
            @input="clearFieldError('totalCount')"
          />
          <div v-if="errors.totalCount" class="field-error">{{ errors.totalCount }}</div>
        </div>

        <div class="form-span-full ingredients-editor">
          <div class="ingredients-head">
            <h4 class="ingredients-title">مواد اولیه</h4>
            <button type="button" class="btn btn-outline btn-sm" @click="addRow">+ ماده اولیه</button>
          </div>
          <div v-if="errors.ingredientRows" class="field-error">{{ errors.ingredientRows }}</div>

          <div class="ingredient-row ingredient-row-head hide-mobile" aria-hidden="true">
            <span>ماده اولیه</span>
            <span>مقدار</span>
            <span>قیمت هر واحد</span>
            <span />
          </div>

          <div
            v-for="(row, i) in form.ingredientRows"
            :key="i"
            class="ingredient-row"
          >
            <div class="ingredient-cell">
              <label v-if="isMobile" class="ingredient-mobile-label">ماده اولیه</label>
              <AppSelect
                v-model="row.costTypeId"
                :options="ingredients"
                option-value="id"
                option-label="name"
                placeholder="انتخاب ماده"
                @change="onIngredientChange(row)"
              />
            </div>

            <div class="ingredient-cell">
              <label v-if="isMobile" class="ingredient-mobile-label">
                مقدار
                <span v-if="ingredientUnit(row.costTypeId)" class="text-muted">({{ ingredientUnit(row.costTypeId) }})</span>
              </label>
              <div class="amount-with-unit">
                <ClearableInput
                  v-model="row.units"
                  type="number"
                  step="any"
                  inputmode="decimal"
                  placeholder="مقدار"
                />
                <span v-if="ingredientUnit(row.costTypeId)" class="unit-badge hide-mobile">
                  {{ ingredientUnit(row.costTypeId) }}
                </span>
              </div>
            </div>

            <div class="ingredient-cell">
              <label v-if="isMobile" class="ingredient-mobile-label">قیمت هر واحد</label>
              <CurrencyInput v-model="row.price" placeholder="قیمت" />
              <p v-if="recommendedUnitPrice(row.costTypeId)" class="field-hint">
                پیشنهادی: {{ formatMoney(recommendedUnitPrice(row.costTypeId)) }}
                <template v-if="ingredientUnit(row.costTypeId)"> / {{ ingredientUnit(row.costTypeId) }}</template>
                <template v-if="recommendedTotal(row)">
                  · جمع ≈ {{ formatMoney(recommendedTotal(row)) }}
                </template>
              </p>
            </div>

            <div class="ingredient-cell ingredient-actions">
              <button
                v-if="form.ingredientRows.length > 1"
                type="button"
                class="btn btn-outline btn-sm ingredient-remove"
                aria-label="حذف ماده"
                @click="removeRow(i)"
              >
                حذف
              </button>
            </div>
          </div>
        </div>

        <div class="form-group form-span-full">
          <label>توضیحات</label>
          <ClearableInput v-model="form.description" type="textarea" :rows="2" />
        </div>
        <div class="modal-actions">
          <button type="button" class="btn btn-outline" @click="closeForm">انصراف</button>
          <button type="submit" class="btn">{{ editing ? 'ذخیره' : 'ثبت' }}</button>
        </div>
      </form>
    </FormHost>

    <div v-show="!showForm || isMobile" class="food-list">
      <article v-for="food in items" :key="food.id" class="card food-card">
        <header class="food-card-header">
          <div class="food-card-title">
            <h3>{{ food.name }}</h3>
            <DateDisplay class="text-muted" :value="food.cookDate" />
          </div>
          <dl class="food-card-stats">
            <div>
              <dt>تعداد</dt>
              <dd>{{ food.totalCount }}</dd>
            </div>
            <div>
              <dt>هزینه هر واحد</dt>
              <dd>{{ formatMoney(food.costPerUnit) }}</dd>
            </div>
            <div>
              <dt>هزینه کل</dt>
              <dd class="text-danger">{{ formatMoney(food.totalCost) }}</dd>
            </div>
          </dl>
          <RowActions
            v-if="auth.hasAnyPermission('food.update', 'audit.view')"
            :show-edit="auth.hasPermission('food.update')"
            :show-audit="auth.hasPermission('audit.view')"
            :audit="food.audit"
            @edit="startEdit(food)"
          />
        </header>

        <div class="food-ingredients-wrap">
          <table class="food-ingredients-table">
            <thead>
              <tr>
                <th>ماده اولیه</th>
                <th>مقدار</th>
                <th>قیمت واحد</th>
                <th>قیمت پیشنهادی</th>
                <th>جمع ردیف</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="ing in food.ingredients" :key="ing.id">
                <td data-label="ماده اولیه">
                  <span class="ingredient-name">{{ ing.costTypeName }}</span>
                  <span v-if="ing.unitName" class="unit-tag">{{ ing.unitName }}</span>
                </td>
                <td data-label="مقدار">{{ ing.units }}</td>
                <td data-label="قیمت واحد">{{ formatMoney(ing.price) }}</td>
                <td data-label="قیمت پیشنهادی" class="text-muted">
                  {{ ing.recommendedPrice ? formatMoney(ing.recommendedPrice) : '—' }}
                </td>
                <td data-label="جمع ردیف">{{ formatMoney(ing.units * ing.price) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </article>

      <div v-if="!items.length" class="card empty-state">غذایی برای این تاریخ ثبت نشده</div>
    </div>
  </div>
</template>

<style scoped>
.food-page .food-toolbar {
  flex: 1;
  justify-content: flex-end;
  min-width: 0;
}
.food-page .food-toolbar :deep(.persian-date-picker) {
  flex: 1;
  min-width: min(100%, 280px);
  max-width: 320px;
}

.ingredients-editor {
  margin-top: 0.25rem;
}
.ingredients-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  margin-bottom: 0.75rem;
}
.ingredients-title {
  margin: 0;
  font-size: 1rem;
  font-weight: 700;
}
.ingredient-row {
  display: grid;
  grid-template-columns: minmax(0, 2fr) minmax(0, 1fr) minmax(0, 1.35fr) auto;
  gap: 0.65rem 0.85rem;
  align-items: start;
  margin-bottom: 0.75rem;
  padding-bottom: 0.75rem;
  border-bottom: 1px solid color-mix(in srgb, var(--border) 70%, transparent);
}
.ingredient-row:last-child {
  border-bottom: none;
  margin-bottom: 0;
  padding-bottom: 0;
}
.ingredient-row-head {
  margin-bottom: 0.35rem;
  padding-bottom: 0;
  border-bottom: none;
  font-size: 0.78rem;
  font-weight: 700;
  color: var(--text-muted);
}
.ingredient-cell {
  min-width: 0;
}
.ingredient-mobile-label {
  display: block;
  margin-bottom: 0.35rem;
  font-size: 0.82rem;
  font-weight: 600;
}
.amount-with-unit {
  display: flex;
  align-items: center;
  gap: 0.45rem;
  min-width: 0;
}
.amount-with-unit :deep(.clearable-input) {
  flex: 1;
  min-width: 0;
}
.unit-badge {
  flex-shrink: 0;
  font-size: 0.78rem;
  font-weight: 600;
  color: var(--text-muted);
  padding: 0.25rem 0.5rem;
  border-radius: 999px;
  background: var(--bg-elevated);
  border: 1px solid var(--border);
}
.field-hint {
  margin: 0.35rem 0 0;
  font-size: 0.76rem;
  color: var(--text-muted);
  line-height: 1.45;
}
.ingredient-actions {
  display: flex;
  align-items: flex-start;
  justify-content: flex-end;
  padding-top: 0.1rem;
}
.ingredient-remove {
  white-space: nowrap;
}

.food-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
.food-card {
  padding: 0;
  overflow: hidden;
}
.food-card-header {
  display: grid;
  grid-template-columns: minmax(0, 1fr) auto auto;
  gap: 0.85rem 1.25rem;
  align-items: start;
  padding: 1rem 1.15rem;
  border-bottom: 1px solid var(--border);
  background: color-mix(in srgb, var(--bg) 45%, var(--surface));
}
.food-card-title h3 {
  margin: 0 0 0.2rem;
  font-size: 1.05rem;
}
.food-card-stats {
  display: grid;
  grid-template-columns: repeat(3, auto);
  gap: 0.65rem 1.1rem;
  margin: 0;
}
.food-card-stats div {
  display: flex;
  flex-direction: column;
  gap: 0.1rem;
  text-align: left;
}
.food-card-stats dt {
  margin: 0;
  font-size: 0.72rem;
  color: var(--text-muted);
  font-weight: 500;
}
.food-card-stats dd {
  margin: 0;
  font-weight: 700;
  font-size: 0.92rem;
}
.food-ingredients-wrap {
  overflow-x: auto;
}
.food-ingredients-table {
  width: 100%;
  border-collapse: collapse;
  table-layout: fixed;
}
.food-ingredients-table th,
.food-ingredients-table td {
  padding: 0.75rem 1rem;
  text-align: right;
  border-bottom: 1px solid var(--border);
  vertical-align: middle;
}
.food-ingredients-table th {
  font-size: 0.78rem;
  color: var(--text-muted);
  background: var(--bg-elevated);
  font-weight: 700;
}
.food-ingredients-table tbody tr:last-child td {
  border-bottom: none;
}
.food-ingredients-table th:nth-child(1),
.food-ingredients-table td:nth-child(1) { width: 32%; }
.food-ingredients-table th:nth-child(2),
.food-ingredients-table td:nth-child(2) { width: 14%; }
.food-ingredients-table th:nth-child(3),
.food-ingredients-table td:nth-child(3) { width: 18%; }
.food-ingredients-table th:nth-child(4),
.food-ingredients-table td:nth-child(4) { width: 18%; }
.food-ingredients-table th:nth-child(5),
.food-ingredients-table td:nth-child(5) { width: 18%; }
.ingredient-name {
  display: inline;
  font-weight: 600;
}
.unit-tag {
  display: inline-block;
  margin-inline-start: 0.45rem;
  font-size: 0.72rem;
  font-weight: 600;
  color: var(--text-muted);
  padding: 0.1rem 0.4rem;
  border-radius: 999px;
  background: var(--bg);
  border: 1px solid var(--border);
  vertical-align: middle;
}

@media (max-width: 768px) {
  .food-page .page-header {
    flex-direction: column;
    align-items: stretch;
  }
  .food-page .food-toolbar {
    flex-direction: column;
    align-items: stretch;
  }
  .food-page .food-toolbar :deep(.persian-date-picker) {
    max-width: none;
  }

  .ingredient-row {
    grid-template-columns: 1fr;
    gap: 0.55rem;
    padding: 0.85rem;
    border: 1px solid var(--border);
    border-radius: 12px;
    background: var(--bg-elevated);
  }
  .ingredient-actions {
    justify-content: flex-start;
  }

  .food-card-header {
    grid-template-columns: 1fr;
    gap: 0.75rem;
  }
  .food-card-stats {
    grid-template-columns: repeat(3, minmax(0, 1fr));
    width: 100%;
  }
  .food-card-stats div {
    text-align: right;
  }

  .food-ingredients-table thead {
    display: none;
  }
  .food-ingredients-table tbody tr {
    display: block;
    padding: 0.85rem 1rem;
    border-bottom: 1px solid var(--border);
  }
  .food-ingredients-table tbody tr:last-child {
    border-bottom: none;
  }
  .food-ingredients-table td {
    display: flex;
    justify-content: space-between;
    align-items: baseline;
    gap: 1rem;
    padding: 0.35rem 0;
    border: none;
    text-align: left;
  }
  .food-ingredients-table td::before {
    content: attr(data-label);
    font-size: 0.78rem;
    color: var(--text-muted);
    font-weight: 600;
    text-align: right;
    flex: 1;
  }
  .food-ingredients-table td[data-label="ماده اولیه"] {
    flex-direction: column;
    align-items: flex-start;
    gap: 0.25rem;
    padding-bottom: 0.55rem;
    margin-bottom: 0.25rem;
    border-bottom: 1px solid color-mix(in srgb, var(--border) 80%, transparent);
  }
  .food-ingredients-table td[data-label="ماده اولیه"]::before {
    display: none;
  }
}
</style>
