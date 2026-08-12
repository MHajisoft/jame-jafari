export const PASSWORD_MIN_LENGTH = 6
export const PASSWORD_MAX_LENGTH = 100

export function validatePassword(password, { allowEmpty = false } = {}) {
  if (!password) return allowEmpty ? null : 'رمز عبور الزامی است'
  if (password.length < PASSWORD_MIN_LENGTH) return 'رمز عبور حداقل ۶ کاراکتر'
  if (password.length > PASSWORD_MAX_LENGTH) return 'رمز عبور حداکثر ۱۰۰ کاراکتر'
  if (!/[a-zA-Z\u0600-\u06FF]/.test(password)) return 'رمز عبور باید حداقل یک حرف داشته باشد'
  if (!/\d/.test(password)) return 'رمز عبور باید حداقل یک عدد داشته باشد'
  if (!/[^a-zA-Z\u0600-\u06FF\d]/.test(password)) return 'رمز عبور باید حداقل یک نماد داشته باشد'
  return null
}

export function passwordFieldRules({ required = true } = {}) {
  const rules = []
  if (required) rules.push({ type: 'required', msg: 'رمز عبور الزامی است' })
  rules.push({ type: 'passwordStrength', param: !required })
  return rules
}
