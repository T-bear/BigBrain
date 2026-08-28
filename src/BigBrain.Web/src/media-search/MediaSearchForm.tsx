import type { FormEvent, KeyboardEvent } from 'react'
import { BBButton } from '../components'

export function MediaSearchForm({
  loading,
  query,
  showClear,
  onQueryChange,
  onClear,
  onSubmit,
}: {
  loading: boolean
  query: string
  showClear: boolean
  onQueryChange: (query: string) => void
  onClear: () => void
  onSubmit: () => void
}) {
  const canSubmit = query.trim().length >= 2 && !loading

  function submit(event: FormEvent) {
    event.preventDefault()
    if (canSubmit) onSubmit()
  }

  function submitOnEnter(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key !== 'Enter') return
    event.preventDefault()
    if (canSubmit) onSubmit()
  }

  return <form className="media-search-form" role="search" onSubmit={submit}>
    <label htmlFor="media-search-query">Titel</label>
    <div className="media-search-controls">
      <input className="bb-input"
        id="media-search-query"
        name="query"
        type="search"
        value={query}
        onChange={event => onQueryChange(event.target.value)}
        onKeyDown={submitOnEnter}
        placeholder="Sök efter en film eller serie"
        autoComplete="off"
      />
      {showClear && <button aria-label="Rensa sökning" className="media-search-clear-control" onClick={onClear} type="button">×</button>}
      <BBButton busy={loading} type="submit" disabled={!query.trim()||query.trim().length<2} variant="primary">Sök</BBButton>
    </div>
  </form>
}
