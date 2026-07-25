import type { MediaSearchProviderResult } from '../types'
import { MediaSearchResultCard } from './MediaSearchResultCard'

export function MediaSearchProviderSection({ provider }: { provider: MediaSearchProviderResult }) {
  const headingId = `media-search-${provider.provider.toLowerCase()}`
  return <section className="media-search-provider" aria-labelledby={headingId}>
    <header>
      <h4 id={headingId}>{provider.provider}</h4>
      <span className={`provider-status provider-status--${provider.status}`}>{provider.status}</span>
    </header>
    {provider.error && <p className="notice notice--error" role="status">{provider.error}</p>}
    {provider.status === 'online' && provider.results.length === 0 && <p className="media-search-empty">No match in {provider.provider}.</p>}
    {provider.results.length > 0 && <div className="media-search-result-list">
      {provider.results.map(result => <MediaSearchResultCard result={result} key={`${provider.provider}:${result.sourceId}`} />)}
    </div>}
  </section>
}
