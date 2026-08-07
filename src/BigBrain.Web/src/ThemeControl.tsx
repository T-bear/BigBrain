import { useTheme } from './ThemeProvider'
import type { ThemeId } from './theme'

export function ThemeControl() {
  const { theme, setTheme, error } = useTheme()

  return <label className="bb-field theme-control" htmlFor="bigbrain-theme">
    <span>Tema</span>
    <select className="bb-select" id="bigbrain-theme" value={theme} onChange={event => void setTheme(event.target.value as ThemeId)}>
      <option value="bigbrain-dark">Mörkt</option>
      <option value="bigbrain-light">Ljust</option>
      <option value="bigbrain-obsidian-gold">Obsidian Gold</option>
    </select>
    <span aria-live="polite">{error}</span>
  </label>
}
