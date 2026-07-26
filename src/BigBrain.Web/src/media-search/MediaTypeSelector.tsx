export type MediaTypeSelection = 'movie' | 'series' | 'all'

const choices: Array<{ value: MediaTypeSelection; label: string }> = [
  { value: 'movie', label: 'Film' },
  { value: 'series', label: 'Serie' },
  { value: 'all', label: 'Båda' },
]

export function MediaTypeSelector({ value, onChange }: {
  value: MediaTypeSelection
  onChange: (value: MediaTypeSelection) => void
}) {
  return <fieldset className="media-type-selector">
    <legend>Mediatyp</legend>
    <div>{choices.map(choice => <button
      type="button"
      key={choice.value}
      aria-pressed={value === choice.value}
      onClick={() => onChange(choice.value)}
    >{choice.label}</button>)}</div>
  </fieldset>
}
