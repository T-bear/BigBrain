import { useTheme } from './ThemeProvider'
import type { ThemeId } from './theme'

const choices: { id: ThemeId; label: string; description: string }[] = [
  { id: 'obsidian-gold', label: 'Obsidian Gold', description: 'Mörkt och varmt' },
  { id: 'arctic-wind', label: 'Arctic Wind', description: 'Ljust, kallt och luftigt' },
  { id: 'forest-night', label: 'Forest Night', description: 'Djupt grönt och lugnt' },
]

export function ThemeControl() {
  const { theme, setTheme, error } = useTheme()

  return <fieldset className="theme-control">
    <legend>Tema</legend>
    <div className="theme-picker">{choices.map(choice => <label className="theme-option" key={choice.id}>
      <input checked={theme === choice.id} name="bigbrain-theme" onChange={() => void setTheme(choice.id)} type="radio" value={choice.id} />
      <span aria-hidden="true" className={`theme-option__preview theme-option__preview--${choice.id}`} />
      <span><strong>{choice.label}</strong><small>{choice.description}</small></span>
      <span aria-hidden="true" className="theme-option__check">✓</span>
    </label>)}</div>
    <span aria-live="polite" className="theme-control__error">{error}</span>
  </fieldset>
}
