import type { MediaLookupResult } from '../types'
import { MediaPoster } from './MediaPoster'

const statusText = (result: MediaLookupResult) => {
  if (result.alreadyExists || result.alreadyRegistered)
    return result.monitored === false ? 'Tillagd, ej bevakad' : 'Redan tillagd'
  if (result.canRequest !== false) return 'Kan läggas till'
  return 'Inte tillgänglig'
}

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
    <MediaPoster title={result.title} url={result.posterUrl} />
    <div className="media-search-result-copy">
      <h5 title={result.title}>{result.title}</h5>
      <p>{result.mediaType === 'series' ? 'Serie' : 'Film'}{result.year !== null ? ` · ${result.year}` : ''}</p>
      {result.overview && <p className="media-lookup-overview">{result.overview}</p>}
      <div className="media-result-actions">
        <strong className="media-search-state">{statusText(result)}</strong>
        {!result.alreadyRegistered && requestsEnabled && result.canRequest !== false && <button
            type="button"
            className="secondary-button media-prepare-button"
            onClick={event => onPrepare(result, event.currentTarget)}
          >
            {result.mediaType === 'series' ? 'Lägg till serie' : 'Lägg till film'}
          </button>}
      </div>
    </div>
  </article>
}
