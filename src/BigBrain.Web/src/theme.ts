export const themes = ['obsidian-gold', 'arctic-wind', 'forest-night'] as const
export type ThemeId = (typeof themes)[number]
export const DEFAULT_THEME: ThemeId = 'obsidian-gold'
export const THEME_STORAGE_KEY = 'bigbrain-theme'

const legacyThemes: Record<string, ThemeId> = {
  'bigbrain-dark': 'obsidian-gold',
  'bigbrain-light': 'arctic-wind',
  'bigbrain-obsidian-gold': 'obsidian-gold',
}

export function isThemeId(value: string | null): value is ThemeId {
  return value !== null && themes.includes(value as ThemeId)
}

export function resolveInitialTheme(storage: Pick<Storage, 'getItem'> = window.localStorage): ThemeId {
  const stored = storage.getItem(THEME_STORAGE_KEY)
  if (isThemeId(stored)) return stored
  if (stored && legacyThemes[stored]) return legacyThemes[stored]
  if (stored !== null) return DEFAULT_THEME
  return DEFAULT_THEME
}

export function applyTheme(theme: ThemeId) {
  document.documentElement.dataset.theme = theme
  const colors: Record<ThemeId, string> = { 'obsidian-gold': '#080a0d', 'arctic-wind': '#071320', 'forest-night': '#06130e' }
  document.querySelector<HTMLMetaElement>('meta[name="theme-color"]')?.setAttribute('content', colors[theme])
}

export function normalizeTheme(value: string | null): ThemeId | null {
  if (isThemeId(value)) return value
  return value ? legacyThemes[value] ?? null : null
}
