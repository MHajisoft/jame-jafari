<script setup>
import { ref, onMounted } from 'vue'
import api from '../api/client'
import { formatMoney, toInputDate } from '../utils/format'
import { todayGregorian } from '../utils/jalali'
import { useAuthStore } from '../stores/auth'
import DateDisplay from '../components/DateDisplay.vue'
import PersianDatePicker from '../components/PersianDatePicker.vue'

const auth = useAuthStore()
const items = ref([])
const ingredients = ref([])
const recommendations = ref([])
const cookDate = ref(todayGregorian())
const showModal = ref(false)
const form = ref({
  name: '', cookDate: toInputDate(new Date()), totalCount: '', description: '',
  ingredientRows: [{ costTypeId: '', units: '', price: '' }]
})

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
  const payload = {
    name: form.value.name,
    cookDate: new Date(form.value.cookDate).toISOString(),
    totalCount: +form.value.totalCount,
    description: form.value.description,
    ingredients: form.value.ingredientRows
      .filter(r => r.costTypeId)
      .map(r => ({
        costTypeId: +r.costTypeId,
        units: +r.units,
        price: +r.price
      }))
  }
  await api.post('/food', payload)
  showModal.value = false
  cookDate.value = form.value.cookDate
  await load()
}

function openCreate() {
  form.value = {
    name: '', cookDate: cookDate.value, totalCount: '', description: '',
    ingredientRows: [{ costTypeId: '', units: '', price: '' }]
  }
  showModal.value = true
}

onMounted(load)
</script>

<template>
  <div>
    <div class="page-header">
      <h1 class="page-title">تهیه غذا</h1>
      <div class="page-toolbar date-toolbar">
        <PersianDatePicker v-model="cookDate" variant="bar" placeholder="تاریخ پخت" @change="load" />
        <button v-if="auth.hasPermission('food.manage')" class="btn btn-fab-mobile" @click="openCreate">
          <span aria-hidden="true">+</span>
          <span class="btn-fab-label">غذای جدید</span>
        </button>
      </div>
    </div>

    <div v-for="food in items" :key="food.id" class="card" style="margin-bottom:1rem">
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
        <thead>
          <tr><th>ماده اولیه</th><th>مقدار</th><th>قیمت</th><th>قیمت پیشنهادی</th></tr>
        </thead>
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
    <div v-if="!items.length" class="card empty-state">غذایی برای این تاریخ ثبت نشده</div>

    <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
      <div class="modal" style="max-width:640px">
        <h2 class="modal-title">ثبت تهیه غذا</h2>
        <div class="grid-2">
          <div class="form-group">
            <label>نام غذا</label>
            <input v-model="form.name" class="form-control" required />
          </div>
          <div class="form-group">
            <label>تاریخ</label>
            <PersianDatePicker v-model="form.cookDate" />
          </div>
        </div>
        <div class="form-group">
          <label>تعداد پخته شده</label>
          <input v-model="form.totalCount" type="number" class="form-control" required />
        </div>

        <h4 style="margin:1rem 0 0.5rem">مواد اولیه</h4>
        <div v-for="(row, i) in form.ingredientRows" :key="i" class="grid-3" style="margin-bottom:0.5rem">
          <select v-model="row.costTypeId" class="form-control" @change="onIngredientSelect(row)">
            <option value="">ماده اولیه</option>
            <option v-for="ing in ingredients" :key="ing.id" :value="ing.id">{{ ing.name }}</option>
          </select>
          <input v-model="row.units" type="number" class="form-control" placeholder="مقدار" />
          <input v-model="row.price" type="number" class="form-control" placeholder="قیمت" />
        </div>
        <button class="btn btn-outline btn-sm" @click="addRow">+ ماده اولیه</button>

        <div class="form-group" style="margin-top:1rem">
          <label>توضیحات</label>
          <textarea v-model="form.description" class="form-control" rows="2"></textarea>
        </div>
        <div class="modal-actions">
          <button class="btn btn-outline" @click="showModal = false">انصراف</button>
          <button class="btn" @click="submit">ثبت</button>
        </div>
      </div>
    </div>
  </div>
</template>
