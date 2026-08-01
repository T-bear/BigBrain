import { useEffect, useState } from 'react'
import type { MediaLookupResponse, MediaLookupResult } from '../types'
import { MediaLookupResultCard } from './MediaLookupResultCard'
import { mediaErrorMessage } from '../api'

export function MediaLookupResults({
  response,
  onPrepare,
}: {
  response: MediaLookupResponse
  onPrepare: (result: MediaLookupResult, trigger: HTMLButtonElement) => void
}) {
  const [expandedProviders, setExpandedProviders] = useState<string[]>([])
  useEffect(() => setExpandedProviders([]), [response.query, response.lookedUpAtUtc])
  const normalizedQuery = response.query.trim().toLocaleLowerCase('sv-SE')
  const ranked = (provider: MediaLookupResponse['providers'][number]) => provider.results
    .map((result, index) => ({ result, index, rank: result.title.trim().toLocaleLowerCase('sv-SE') === normalizedQuery ? 0 : result.title.toLocaleLowerCase('sv-SE').includes(normalizedQuery) ? 1 : 2 }))
    .sort((left, right) => left.rank - right.rank || left.index - right.index)
    .map(item => item.result)
  const providerTitle = (provider: string) => provider === 'Sonarr' ? 'Serier' : provider === 'Radarr' ? 'Filmer' : provider
  return <div className="media-search-results" aria-live="polite">
    <div className="media-search-summary">
      <h3>Resultat för “{response.query}”</h3>
      <span>{response.status === 'complete' ? 'Klar' : response.status === 'partial' ? 'Delvis klar' : 'Ej tillgänglig'}</span>
    </div>
    {!response.requestsEnabled && <p className="notice">Media requests are currently disabled.</p>}
    {response.providers.map(provider => {
      const results = ranked(provider)
      const expanded = expandedProviders.includes(provider.provider)
      return <section className="media-search-provider" key={provider.provider}>
      <header><h4>{providerTitle(provider.provider)}</h4>{provider.status !== 'online' && <span className="provider-status">{provider.status}</span>}</header>
      {provider.error && <p className="notice notice--error" role="status">
        {mediaErrorMessage(provider.errorCode)}
      </p>}
      {provider.status === 'online' && provider.results.length === 0 && <p>No external matches.</p>}
      <div className="media-search-result-list">
        {(expanded ? results : results.slice(0, 1)).map(result => <MediaLookupResultCard
          key={`${provider.provider}:${result.foreignId}`}
          result={result}
          requestsEnabled={response.requestsEnabled}
          onPrepare={onPrepare}
        />)}
      </div>
      {results.length > 1 && <button className="secondary-button media-results-toggle" aria-expanded={expanded} type="button" onClick={() => setExpandedProviders(current => expanded ? current.filter(value => value !== provider.provider) : [...current, provider.provider])}>{expanded ? 'Visa färre' : 'Visa fler resultat'}</button>}
    </section>})}
  </div>
}
