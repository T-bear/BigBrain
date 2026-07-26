import { useEffect, useRef, useState } from 'react'
import { ApiError, lookupMedia, mediaErrorMessage, searchMedia } from '../api'
import type { MediaLookupResponse, MediaLookupResult, MediaSearchResponse } from '../types'
import { MediaSearchForm } from './MediaSearchForm'
import { MediaSearchResults } from './MediaSearchResults'
import { MediaSearchModeSelector, type MediaSearchMode } from './MediaSearchModeSelector'
import { MediaLookupResults } from './MediaLookupResults'
import { MediaRequestDialog } from './MediaRequestDialog'
import { MediaTypeSelector, type MediaTypeSelection } from './MediaTypeSelector'

export function MediaSearch() {
  const [query, setQuery] = useState('')
  const [result, setResult] = useState<MediaSearchResponse | null>(null)
  const [lookupResult, setLookupResult] = useState<MediaLookupResponse | null>(null)
  const [mode, setMode] = useState<MediaSearchMode>('external')
  const [mediaType, setMediaType] = useState<MediaTypeSelection>('all')
  const [requestTarget, setRequestTarget] = useState<MediaLookupResult | null>(null)
  const [returnFocus, setReturnFocus] = useState<HTMLButtonElement | null>(null)
  const [loading, setLoading] = useState(false)
  const [errorCode, setErrorCode] = useState<string | null>(null)
  const controllerRef = useRef<AbortController | null>(null)

  useEffect(() => () => controllerRef.current?.abort(), [])

  async function submit() {
    controllerRef.current?.abort()
    const controller = new AbortController()
    controllerRef.current = controller
    setLoading(true)
    setErrorCode(null)
    try {
      if (mode === 'libraries') {
        setResult(await searchMedia(query, controller.signal))
      } else {
        setLookupResult(await lookupMedia(query, mediaType, controller.signal))
      }
    } catch (error) {
      if (!(error instanceof Error) || error.name !== 'AbortError')
        setErrorCode(error instanceof ApiError ? error.code : 'unknownError')
    } finally {
      if (!controller.signal.aborted) setLoading(false)
    }
  }

  return <section className="media-search card" aria-labelledby="media-search-heading">
    <div className="media-search-intro">
      <p className="eyebrow">Mediasökning</p>
      <h3 id="media-search-heading">Hitta film och serier</h3>
      <p>Sök i Sonarr och Radarr eller kontrollera dina befintliga bibliotek.</p>
    </div>
    <MediaSearchModeSelector mode={mode} onChange={next => { setMode(next); setErrorCode(null) }} />
    {mode === 'external' && <MediaTypeSelector value={mediaType} onChange={setMediaType} />}
    <MediaSearchForm
      loading={loading}
      query={query}
      onQueryChange={setQuery}
      onSubmit={() => void submit()}
    />
    {loading && <p className="media-search-loading" aria-live="polite">Searching media services…</p>}
    {errorCode && <p className="notice notice--error" role="alert">{mediaErrorMessage(errorCode)}</p>}
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
