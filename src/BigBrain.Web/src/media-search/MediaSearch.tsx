import { useEffect, useRef, useState } from 'react'
import { searchMedia } from '../api'
import type { MediaSearchResponse } from '../types'
import { MediaSearchForm } from './MediaSearchForm'
import { MediaSearchResults } from './MediaSearchResults'

export function MediaSearch() {
  const [query, setQuery] = useState('')
  const [result, setResult] = useState<MediaSearchResponse | null>(null)
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
      setResult(await searchMedia(query, controller.signal))
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
      <p>Check your existing Jellyfin, Sonarr and Radarr libraries.</p>
    </div>
    <MediaSearchForm
      loading={loading}
      query={query}
      onQueryChange={setQuery}
      onSubmit={() => void submit()}
    />
    {loading && <p className="media-search-loading" aria-live="polite">Searching media services…</p>}
    {failed && <p className="notice notice--error" role="alert">Media search could not be completed. Try again.</p>}
    {!loading && result && <MediaSearchResults response={result} />}
  </section>
}
