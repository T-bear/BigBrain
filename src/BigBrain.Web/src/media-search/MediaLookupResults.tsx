import type { MediaLookupResponse, MediaLookupResult } from '../types'
import { MediaLookupResultCard } from './MediaLookupResultCard'

export function MediaLookupResults({
  response,
  onPrepare,
}: {
  response: MediaLookupResponse
  onPrepare: (result: MediaLookupResult, trigger: HTMLButtonElement) => void
}) {
  return <div className="media-search-results" aria-live="polite">
    <div className="media-search-summary">
      <h3>External results for “{response.query}”</h3><span>{response.status}</span>
    </div>
    {!response.requestsEnabled && <p className="notice">Media requests are currently disabled.</p>}
    {response.providers.map(provider => <section className="media-search-provider" key={provider.provider}>
      <header><h4>{provider.provider}</h4><span className="provider-status">{provider.status}</span></header>
      {provider.error && <p className="notice notice--error" role="status">{provider.error}</p>}
      {provider.status === 'online' && provider.results.length === 0 && <p>No external matches.</p>}
      <div className="media-search-result-list">
        {provider.results.map(result => <MediaLookupResultCard
          key={`${provider.provider}:${result.foreignId}`}
          result={result}
          requestsEnabled={response.requestsEnabled}
          onPrepare={onPrepare}
        />)}
      </div>
    </section>)}
  </div>
}
