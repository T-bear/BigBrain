import { useState } from 'react'
import { applyTheme, resolveInitialTheme, THEME_STORAGE_KEY, type ThemeId } from './theme'

export function ThemeControl() {
  const [theme, setTheme] = useState<ThemeId>(() => {
    const initial = resolveInitialTheme()
    applyTheme(initial)
    return initial
  })

  const changeTheme = (next: ThemeId) => {
    applyTheme(next)
    window.localStorage.setItem(THEME_STORAGE_KEY, next)
    setTheme(next)
  }

  return <label className="bb-field theme-control" htmlFor="bigbrain-theme">
    <span>Tema</span>
    <select className="bb-select" id="bigbrain-theme" value={theme} onChange={event => changeTheme(event.target.value as ThemeId)}>
      <option value="bigbrain-dark">Mörkt</option>
      <option value="bigbrain-light">Ljust</option>
    </select>
  </label>
}
