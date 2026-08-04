export const themes = ['bigbrain-dark', 'bigbrain-light', 'bigbrain-obsidian-gold'] as const
export type ThemeId = (typeof themes)[number]
export const DEFAULT_THEME: ThemeId = 'bigbrain-dark'
export const THEME_STORAGE_KEY = 'bigbrain-theme'

export function isThemeId(value: string | null): value is ThemeId {
  return value !== null && themes.includes(value as ThemeId)
}

export function resolveInitialTheme(storage: Pick<Storage, 'getItem'> = window.localStorage): ThemeId {
  const stored = storage.getItem(THEME_STORAGE_KEY)
  if (isThemeId(stored)) return stored
  if (stored !== null) return DEFAULT_THEME
  return window.matchMedia?.('(prefers-color-scheme: light)').matches ? 'bigbrain-light' : DEFAULT_THEME
}

export function applyTheme(theme: ThemeId) {
  document.documentElement.dataset.theme = theme
}
