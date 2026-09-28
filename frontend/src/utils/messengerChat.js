/** Normalize messenger channel/group chat target. */
export function normalizeMessengerChatTarget(value, messengerKind = 1) {
  const s = String(value ?? '').trim()
  if (!s) return ''

  if (Number(messengerKind) === 2) {
    return s
  }

  if (Number(messengerKind) === 4) {
    return normalizeWhatsAppPhone(s)
  }

  if (/^-?\d+$/.test(s)) return s

  const username = s.startsWith('@') ? s.slice(1) : s
  return `@${username}`
}

export function isValidMessengerChatTarget(value, messengerKind = 1) {
  const s = String(value ?? '').trim()
  if (!s) return false

  if (Number(messengerKind) === 2) {
    return /^[A-Za-z0-9_-]{4,100}$/.test(s)
  }

  if (Number(messengerKind) === 4) {
    return normalizeWhatsAppPhone(s).length >= 10
  }

  if (/^-?\d+$/.test(s)) {
    const n = Number(s)
    return Number.isFinite(n) && n !== 0
  }

  const username = s.startsWith('@') ? s.slice(1) : s
  return /^[a-zA-Z][\w]{3,}$/.test(username)
}

/** Digits-only wa_id (Iran +98), matching PhoneNormalizeHelper.ToWhatsAppId. */
function normalizeWhatsAppPhone(value) {
  let digits = String(value ?? '').replace(/[^\d]/g, '')
  if (digits.startsWith('0098')) digits = digits.slice(4)
  else if (digits.startsWith('98') && digits.length >= 12) digits = digits.slice(2)
  if (digits.startsWith('0') && digits.length === 11) digits = digits.slice(1)
  if (digits.length === 10 && digits.startsWith('9')) return `98${digits}`
  if (digits.startsWith('98')) return digits
  return digits.length ? `98${digits}` : ''
}
