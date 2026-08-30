/** Normalize Bale channel/group chat target: numeric ID or @username. */
export function normalizeMessengerChatTarget(value) {
  const s = String(value ?? '').trim()
  if (!s) return ''

  if (/^-?\d+$/.test(s)) return s

  const username = s.startsWith('@') ? s.slice(1) : s
  return `@${username}`
}

export function isValidMessengerChatTarget(value) {
  const s = String(value ?? '').trim()
  if (!s) return false

  if (/^-?\d+$/.test(s)) {
    const n = Number(s)
    return Number.isFinite(n) && n !== 0
  }

  const username = s.startsWith('@') ? s.slice(1) : s
  return /^[a-zA-Z][\w]{3,}$/.test(username)
}
