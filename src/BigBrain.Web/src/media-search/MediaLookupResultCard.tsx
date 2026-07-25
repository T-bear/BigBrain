import type { MediaLookupResult } from '../types'

export function MediaLookupResultCard({
  result,
  requestsEnabled,
  onPrepare,
}: {
  result: MediaLookupResult
  requestsEnabled: boolean
  onPrepare: (result: MediaLookupResult, trigger: HTMLButtonElement) => void
}) {
  return <article className="media-lookup-result-card">
    <div className="media-search-poster-placeholder" aria-hidden="true">No poster</div>
    <div className="media-search-result-copy">
      <h5 title={result.title}>{result.title}</h5>
      <p>{result.mediaType}{result.year !== null ? ` · ${result.year}` : ''}</p>
      {result.overview && <p className="media-lookup-overview">{result.overview}</p>}
      {result.alreadyRegistered
        ? <strong className="media-search-state">Already registered</strong>
        : requestsEnabled && <button
            type="button"
            className="secondary-button media-prepare-button"
            onClick={event => onPrepare(result, event.currentTarget)}
          >
            {result.mediaType === 'series' ? 'Add to Sonarr' : 'Add to Radarr'}
          </button>}
    </div>
  </article>
}
