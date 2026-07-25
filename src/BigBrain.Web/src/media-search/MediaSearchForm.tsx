import type { FormEvent, KeyboardEvent } from 'react'

export function MediaSearchForm({
  loading,
  query,
  onQueryChange,
  onSubmit,
}: {
  loading: boolean
  query: string
  onQueryChange: (query: string) => void
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
    <label htmlFor="media-search-query">Search title</label>
    <div className="media-search-controls">
      <input
        id="media-search-query"
        name="query"
        type="search"
        value={query}
        onChange={event => onQueryChange(event.target.value)}
        onKeyDown={submitOnEnter}
        placeholder="Try Family Guy"
        autoComplete="off"
      />
      <button type="submit" disabled={!canSubmit}>{loading ? 'Searching…' : 'Search'}</button>
    </div>
  </form>
}
