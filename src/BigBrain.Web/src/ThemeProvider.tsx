import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { applyTheme, normalizeTheme, resolveInitialTheme, THEME_STORAGE_KEY, type ThemeId } from './theme'

type ThemeContextValue = { theme: ThemeId; setTheme: (theme: ThemeId) => Promise<void>; error: string }
const ThemeContext = createContext<ThemeContextValue | null>(null)

async function readTheme(signal?: AbortSignal): Promise<{ theme: ThemeId; configured: boolean }> {
  const response = await fetch('/api/v1/settings/theme', { signal })
  if (!response.ok) throw new Error('Temainställningen kunde inte hämtas.')
  const value = await response.json() as { theme?: string; configured?: boolean }
  const theme = normalizeTheme(value.theme ?? null)
  if (!theme) throw new Error('Servern returnerade ett ogiltigt tema.')
  return { theme, configured: value.configured !== false }
}

async function writeTheme(theme: ThemeId): Promise<void> {
  const response = await fetch('/api/v1/settings/theme', { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ theme }) })
  if (!response.ok) throw new Error('Temainställningen kunde inte sparas.')
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [theme, setThemeState] = useState<ThemeId>(() => resolveInitialTheme())
  const [error, setError] = useState('')
  const themeRef = useRef(theme)

  const apply = useCallback((next: ThemeId) => {
    applyTheme(next)
    window.localStorage.setItem(THEME_STORAGE_KEY, next)
    themeRef.current = next
    setThemeState(next)
  }, [])

  const refresh = useCallback(async (signal?: AbortSignal) => {
    try {
      const remote = await readTheme(signal)
      if (remote.configured) apply(remote.theme)
      else { await writeTheme(themeRef.current); apply(themeRef.current) }
      setError('')
    }
    catch (reason) { if (!(reason instanceof DOMException && reason.name === 'AbortError')) setError(reason instanceof Error ? reason.message : 'Temainställningen kunde inte hämtas.') }
  }, [apply])

  useEffect(() => {
    applyTheme(theme)
    const controller = new AbortController()
    void refresh(controller.signal)
    const onFocus = () => void refresh()
    window.addEventListener('focus', onFocus)
    return () => { controller.abort(); window.removeEventListener('focus', onFocus) }
  }, []) // Servern är auktoritativ vid start och när klienten återfår fokus.

  const setTheme = useCallback(async (next: ThemeId) => {
    const previous = theme
    apply(next)
    try { await writeTheme(next); setError('') }
    catch (reason) { apply(previous); setError(reason instanceof Error ? reason.message : 'Temainställningen kunde inte sparas.') }
  }, [apply, theme])

  const value = useMemo(() => ({ theme, setTheme, error }), [error, setTheme, theme])
  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
}

export function useTheme() {
  const value = useContext(ThemeContext)
  if (!value) throw new Error('useTheme måste användas inom ThemeProvider.')
  return value
}
