/** Normalize messenger channel/group chat target. */
export function normalizeMessengerChatTarget(value, messengerKind = 1) {
  const s = String(value ?? '').trim()
  if (!s) return ''

  if (Number(messengerKind) === 2) {
    return s
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

  if (/^-?\d+$/.test(s)) {
    const n = Number(s)
    return Number.isFinite(n) && n !== 0
  }

  const username = s.startsWith('@') ? s.slice(1) : s
  return /^[a-zA-Z][\w]{3,}$/.test(username)
}
