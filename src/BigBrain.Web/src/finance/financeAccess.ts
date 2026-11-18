// An in-tab secret for an authenticated Finance boundary. Never write it to a URL,
// permanent localStorage, analytics or logs. Other BigBrain endpoints do not receive it.
const key = 'bigbrain.finance.access.v1'
let memoryToken = ''

export function getFinanceAccessToken(): string {
  if (memoryToken) return memoryToken
  try { memoryToken = sessionStorage.getItem(key) ?? '' } catch { /* storage disabled */ }
  return memoryToken
}

export function setFinanceAccessToken(value: string) {
  memoryToken = value
  try {
    if (value) sessionStorage.setItem(key, value)
    else sessionStorage.removeItem(key)
  } catch { /* in-memory only */ }
}

export function financeAccessHeaders(url: string): Record<string, string> {
  if (!url.startsWith('/api/v1/modules/finance')) return {}
  const token = getFinanceAccessToken()
  return token ? { Authorization: `Bearer ${token}` } : {}
}
