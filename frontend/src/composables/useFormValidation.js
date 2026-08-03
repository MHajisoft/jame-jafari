import { reactive, toRefs } from 'vue'

function isBlank(v) {
  return v === undefined || v === null || (typeof v === 'string' && v.trim() === '')
}

const validators = {
  required: (v, _, msg) => (isBlank(v) ? (msg || 'این فیلد الزامی است') : null),
  minLength: (v, len, msg) => (typeof v === 'string' && v.trim().length < len ? (msg || `حداقل ${len} کاراکتر`) : null),
  maxLength: (v, len, msg) => (typeof v === 'string' && v.trim().length > len ? (msg || `حداکثر ${len} کاراکتر`) : null),
  min: (v, num, msg) => (typeof v === 'number' && v < num ? (msg || `حداقل ${num}`) : null),
  max: (v, num, msg) => (typeof v === 'number' && v > num ? (msg || `حداکثر ${num}`) : null),
  positiveNumber: (v, _, msg) => {
    if (isBlank(v)) return null
    const n = Number(v)
    return (isNaN(n) || n <= 0) ? (msg || 'مقدار باید بیشتر از صفر باشد') : null
  },
  email: (v, _, msg) => {
    if (isBlank(v)) return null
    return !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v) ? (msg || 'فرمت ایمیل نامعتبر است') : null
  }
}

/**
 * Composable for form validation + API error handling.
 *
 * @returns {{ error, errors, clearErrors, clearFieldError, validate, trySubmit }}
 */
export function useFormValidation() {
  const state = reactive({
    error: '',
    errors: {}
  })

  function clearErrors() {
    state.error = ''
    Object.keys(state.errors).forEach(k => delete state.errors[k])
  }

  function clearFieldError(field) {
    delete state.errors[field]
  }

  /**
   * Run validation rules against data.
   * @param {Object} rules - { fieldName: [{ validatorName, param, msg }] }
   * @param {Object} data - the reactive form ref data
   * @returns {boolean} true if valid
   */
  function validate(rules, data) {
    clearErrors()
    let valid = true

    for (const [field, fieldRules] of Object.entries(rules)) {
      for (const rule of fieldRules) {
        const v = data[field]
        // Dynamic rules (functions) for conditional validation
        if (typeof rule === 'function') {
          const err = rule(v, data)
          if (err) {
            state.errors[field] = err
            valid = false
            break
          }
          continue
        }
        const { type, param, msg } = rule
        const check = validators[type]
        if (check) {
          const err = check(v, param, msg)
          if (err) {
            state.errors[field] = err
            valid = false
            break
          }
        }
      }
    }

    return valid
  }

  /**
   * Wrap an async action in try/catch.
   * Parses ASP.NET Core ValidationProblemDetails into per-field errors.
   * @param {Function} fn - async function to execute
   * @returns success boolean
   */
  async function trySubmit(fn) {
    clearErrors()
    try {
      await fn()
      return true
    } catch (e) {
      const response = e.response
      if (response && response.data) {
        const data = response.data
        if (data.errors && typeof data.errors === 'object') {
          // ASP.NET ValidationProblemDetails: { errors: { "FieldName": ["msg1","msg2"] } }
          for (const [field, messages] of Object.entries(data.errors)) {
            // Convert PascalCase to camelCase for matching
            const key = field.charAt(0).toLowerCase() + field.slice(1)
            state.errors[key] = Array.isArray(messages) ? messages[0] : messages
          }
        } else if (data.detail || data.title) {
          state.error = data.detail || data.title
        } else if (data.message) {
          state.error = data.message
        }
      } else if (e.message) {
        state.error = e.message
      } else {
        state.error = 'خطایی رخ داد. لطفاً دوباره تلاش کنید.'
      }
      return false
    }
  }

  return { ...toRefs(state), clearErrors, clearFieldError, validate, trySubmit }
}
