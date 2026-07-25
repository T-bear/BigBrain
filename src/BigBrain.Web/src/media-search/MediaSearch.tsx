import { useEffect, useRef, useState } from 'react'
import { lookupMedia, searchMedia } from '../api'
import type { MediaLookupResponse, MediaLookupResult, MediaSearchResponse } from '../types'
import { MediaSearchForm } from './MediaSearchForm'
import { MediaSearchResults } from './MediaSearchResults'
import { MediaSearchModeSelector, type MediaSearchMode } from './MediaSearchModeSelector'
import { MediaLookupResults } from './MediaLookupResults'
import { MediaRequestDialog } from './MediaRequestDialog'

export function MediaSearch() {
  const [query, setQuery] = useState('')
  const [result, setResult] = useState<MediaSearchResponse | null>(null)
  const [lookupResult, setLookupResult] = useState<MediaLookupResponse | null>(null)
  const [mode, setMode] = useState<MediaSearchMode>('libraries')
  const [requestTarget, setRequestTarget] = useState<MediaLookupResult | null>(null)
  const [returnFocus, setReturnFocus] = useState<HTMLButtonElement | null>(null)
  const [loading, setLoading] = useState(false)
  const [failed, setFailed] = useState(false)
  const controllerRef = useRef<AbortController | null>(null)

  useEffect(() => () => controllerRef.current?.abort(), [])

  async function submit() {
    controllerRef.current?.abort()
    const controller = new AbortController()
    controllerRef.current = controller
    setLoading(true)
    setFailed(false)
    try {
      if (mode === 'libraries') {
        setResult(await searchMedia(query, controller.signal))
      } else {
        setLookupResult(await lookupMedia(query, 'all', controller.signal))
      }
    } catch (error) {
      if (!(error instanceof Error) || error.name !== 'AbortError') setFailed(true)
    } finally {
      if (!controller.signal.aborted) setLoading(false)
    }
  }

  return <section className="media-search card" aria-labelledby="media-search-heading">
    <div className="media-search-intro">
      <p className="eyebrow">Unified media search</p>
      <h3 id="media-search-heading">Find a title across your stack</h3>
      <p>Check your libraries or search the external Sonarr and Radarr catalogs.</p>
    </div>
    <MediaSearchModeSelector mode={mode} onChange={next => { setMode(next); setFailed(false) }} />
    <MediaSearchForm
      loading={loading}
      query={query}
      onQueryChange={setQuery}
      onSubmit={() => void submit()}
    />
    {loading && <p className="media-search-loading" aria-live="polite">Searching media services…</p>}
    {failed && <p className="notice notice--error" role="alert">Media search could not be completed. Try again.</p>}
    {!loading && mode === 'libraries' && result && <MediaSearchResults response={result} />}
    {!loading && mode === 'external' && lookupResult && <MediaLookupResults
      response={lookupResult}
      onPrepare={(target, trigger) => { setRequestTarget(target); setReturnFocus(trigger) }}
    />}
    {requestTarget && <MediaRequestDialog
      result={requestTarget}
      returnFocus={returnFocus}
      onClose={() => setRequestTarget(null)}
    />}
  </section>
}
