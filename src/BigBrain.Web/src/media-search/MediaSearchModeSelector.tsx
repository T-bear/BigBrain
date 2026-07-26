export type MediaSearchMode = 'libraries' | 'external'

export function MediaSearchModeSelector({
  mode,
  onChange,
}: {
  mode: MediaSearchMode
  onChange: (mode: MediaSearchMode) => void
}) {
  return <div className="media-search-modes" role="group" aria-label="Sökkälla">
    <button type="button" aria-pressed={mode === 'external'} onClick={() => onChange('external')}>Hitta nytt</button>
    <button type="button" aria-pressed={mode === 'libraries'} onClick={() => onChange('libraries')}>Mina bibliotek</button>
  </div>
}
