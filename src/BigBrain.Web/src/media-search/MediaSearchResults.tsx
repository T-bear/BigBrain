import type { MediaSearchResponse } from '../types'
import { MediaSearchProviderSection } from './MediaSearchProviderSection'

export function MediaSearchResults({ response }: { response: MediaSearchResponse }) {
  return <div className="media-search-results" aria-live="polite">
    <div className="media-search-summary">
      <h3>Results for “{response.query}”</h3>
      <span>{response.status}</span>
    </div>
    {response.providers.map(provider =>
      <MediaSearchProviderSection provider={provider} key={provider.provider} />)}
  </div>
}
