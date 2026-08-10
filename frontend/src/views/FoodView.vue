<script setup>
import { ref, onMounted } from 'vue'
import api from '../api/client'
import { formatMoney, toInputDate } from '../utils/format'
import { todayGregorian } from '../utils/jalali'
import { useAuthStore } from '../stores/auth'
import { useFormValidation } from '../composables/useFormValidation'
import { useIsMobile } from '../composables/useMediaQuery'
import DateDisplay from '../components/DateDisplay.vue'
import PersianDatePicker from '../components/PersianDatePicker.vue'
import CurrencyInput from '../components/CurrencyInput.vue'
import AppSelect from '../components/AppSelect.vue'
import ClearableInput from '../components/ClearableInput.vue'
import FormHost from '../components/FormHost.vue'

const auth = useAuthStore()
const isMobile = useIsMobile()
const { error, errors, validate, trySubmit, clearErrors, clearFieldError } = useFormValidation()
const items = ref([])
const ingredients = ref([])
const recommendations = ref([])
const cookDate = ref(todayGregorian())
const showForm = ref(false)
const form = ref({
  name: '', cookDate: toInputDate(new Date()), totalCount: '', description: '',
  ingredientRows: [{ costTypeId: '', units: '', price: '' }]
})

function getRules() {
  return {
    name: [{ type: 'required', msg: 'نام غذا الزامی است' }],
    totalCount: [{ type: 'positiveNumber', msg: 'تعداد باید حداقل ۱ باشد' }],
    ingredientRows: [
      (val) => {
        if (!val || val.length === 0) return 'حداقل یک ماده اولیه الزامی است'
        const valid = val.some(r => r.costTypeId && r.units && r.price && +r.units > 0 && +r.price > 0)
        if (!valid) return 'هر ماده اولیه باید نوع، مقدار و قیمت معتبر داشته باشد'
        return null
      }
    ]
  }
}

async function load() {
  const [f, ing, rec] = await Promise.all([
    api.get('/food', { params: { date: new Date(cookDate.value).toISOString() } }),
    api.get('/cost-types', { params: { isIngredient: true } }),
    api.get('/food/recommendations')
  ])
  items.value = f.data
  ingredients.value = ing.data
  recommendations.value = rec.data
}

function addRow() {
  form.value.ingredientRows.push({ costTypeId: '', units: '', price: '' })
}

function onIngredientSelect(row) {
  const rec = recommendations.value.find(r => r.costTypeId === +row.costTypeId)
  if (rec && !row.price) row.price = rec.recommendedPrice
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
    await api.post('/food', payload)
  }, { successMessage: 'غذا با موفقیت ثبت شد' })
  if (!ok) return
  closeForm()
  cookDate.value = form.value.cookDate
  await load()
}

function openCreate() {
  form.value = {
    name: '', cookDate: cookDate.value, totalCount: '', description: '',
    ingredientRows: [{ costTypeId: '', units: '', price: '' }]
  }
  clearErrors()
  showForm.value = true
}

function closeForm() {
  showForm.value = false
}

onMounted(load)
</script>

<template>
  <div>
    <div class="page-header" :class="{ 'form-mode': showForm && !isMobile }">
      <h1 class="page-title">{{ showForm && !isMobile ? 'ثبت تهیه غذا' : 'تهیه غذا' }}</h1>
      <div v-if="!showForm || isMobile" class="page-toolbar date-toolbar">
        <PersianDatePicker v-model="cookDate" variant="bar" placeholder="تاریخ پخت" @change="load" />
        <button
          v-if="auth.hasPermission('food.create')"
          type="button"
          class="btn btn-fab-mobile"
          @click="openCreate"
        >
          <span aria-hidden="true">+</span>
          <span class="btn-fab-label">غذای جدید</span>
        </button>
      </div>
    </div>

    <FormHost :show="showForm" :title="isMobile ? 'ثبت تهیه غذا' : ''" @close="closeForm">
      <div v-if="error" class="form-error">{{ error }}</div>
      <form @submit.prevent="submit">
        <div class="grid-2">
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
            <PersianDatePicker v-model="form.cookDate" />
          </div>
        </div>
        <div class="form-group">
          <label>تعداد پخته شده *</label>
          <ClearableInput
            v-model="form.totalCount"
            type="number"
            :min="1"
            :invalid="!!errors.totalCount"
            @input="clearFieldError('totalCount')"
          />
          <div v-if="errors.totalCount" class="field-error">{{ errors.totalCount }}</div>
        </div>

        <h4 style="margin:1rem 0 0.5rem">مواد اولیه</h4>
        <div v-if="errors.ingredientRows" class="field-error" style="margin-bottom:0.5rem">{{ errors.ingredientRows }}</div>
        <div v-for="(row, i) in form.ingredientRows" :key="i" class="grid-3" style="margin-bottom:0.5rem">
          <AppSelect
            v-model="row.costTypeId"
            :options="ingredients"
            option-value="id"
            option-label="name"
            placeholder="ماده اولیه"
            @change="onIngredientSelect(row)"
          />
          <ClearableInput v-model="row.units" type="number" :min="0" step="any" placeholder="مقدار" />
          <CurrencyInput v-model="row.price" placeholder="قیمت" />
        </div>
        <button type="button" class="btn btn-outline btn-sm" @click="addRow">+ ماده اولیه</button>

        <div class="form-group" style="margin-top:1rem">
          <label>توضیحات</label>
          <ClearableInput v-model="form.description" type="textarea" :rows="2" />
        </div>
        <div class="modal-actions">
          <button type="button" class="btn btn-outline" @click="closeForm">انصراف</button>
          <button type="submit" class="btn">ثبت</button>
        </div>
      </form>
    </FormHost>

    <div v-show="!showForm || isMobile">
      <div v-for="food in items" :key="food.id" class="card list-panel" style="margin-bottom:1rem">
        <div style="display:flex;justify-content:space-between;margin-bottom:1rem">
          <div>
            <h3>{{ food.name }}</h3>
            <span class="text-muted"><DateDisplay :value="food.cookDate" /></span>
          </div>
          <div style="text-align:left">
            <div>تعداد: <strong>{{ food.totalCount }}</strong></div>
            <div>هزینه هر واحد: <strong>{{ formatMoney(food.costPerUnit) }}</strong></div>
            <div>هزینه کل: <strong class="text-danger">{{ formatMoney(food.totalCost) }}</strong></div>
          </div>
        </div>
        <table class="mobile-table">
          <thead><tr><th>ماده اولیه</th><th>مقدار</th><th>قیمت</th><th>قیمت پیشنهادی</th></tr></thead>
          <tbody>
            <tr v-for="ing in food.ingredients" :key="ing.id">
              <td data-label="ماده اولیه">{{ ing.costTypeName }} ({{ ing.unitName }})</td>
              <td data-label="مقدار">{{ ing.units }}</td>
              <td data-label="قیمت">{{ formatMoney(ing.price) }}</td>
              <td class="text-muted" data-label="قیمت پیشنهادی">{{ ing.recommendedPrice ? formatMoney(ing.recommendedPrice) : '—' }}</td>
            </tr>
          </tbody>
        </table>
      </div>
      <div v-if="!items.length" class="card list-panel empty-state">غذایی برای این تاریخ ثبت نشده</div>
    </div>
  </div>
</template>
