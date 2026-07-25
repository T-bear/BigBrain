export type MediaSearchMode = 'libraries' | 'external'

export function MediaSearchModeSelector({
  mode,
  onChange,
}: {
  mode: MediaSearchMode
  onChange: (mode: MediaSearchMode) => void
}) {
  return <div className="media-search-modes" role="group" aria-label="Search source">
    <button type="button" aria-pressed={mode === 'libraries'} onClick={() => onChange('libraries')}>My libraries</button>
    <button type="button" aria-pressed={mode === 'external'} onClick={() => onChange('external')}>External catalog</button>
  </div>
}
