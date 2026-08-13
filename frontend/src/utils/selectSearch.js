/** Shared select/search text helpers (align with person lookup normalize). */

export function normalizeSearchText(value) {
  let s = String(value ?? '').trim().toLowerCase()
  s = s.replace(/\u064A/g, '\u06CC').replace(/\u0643/g, '\u06A9')
  while (s.includes('  ')) s = s.replace('  ', ' ')
  return s
}

export function tokenizeSearch(query) {
  return normalizeSearchText(query)
    .split(/\s+/)
    .filter(Boolean)
}

/** ALL tokens must appear in haystack (PersonSelect-style). Empty query → match all. */
export function matchesAllTokens(haystack, query) {
  const tokens = tokenizeSearch(query)
  if (!tokens.length) return true
  const blob = normalizeSearchText(haystack)
  return tokens.every((t) => blob.includes(t))
}
