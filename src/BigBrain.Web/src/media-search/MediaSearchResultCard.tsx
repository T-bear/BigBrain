import type { MediaSearchResult } from '../types'
import { MediaPoster } from './MediaPoster'

function metadataLines(result: MediaSearchResult) {
  const lines: string[] = []
  if (result.metadata.seasonCount !== null) lines.push(`${result.metadata.seasonCount} seasons`)
  if (result.metadata.episodeCount !== null) lines.push(`${result.metadata.episodeCount} episodes`)
  if (result.metadata.episodeFileCount !== null) lines.push(`${result.metadata.episodeFileCount} episode files`)
  if (result.metadata.hasFile !== null) lines.push(result.metadata.hasFile ? 'File available' : 'File missing')
  return lines
}

export function MediaSearchResultCard({ result }: { result: MediaSearchResult }) {
  const details = metadataLines(result)
  return <article className="media-search-result-card">
    <MediaPoster title={result.title} url={result.posterUrl} />
    <div className="media-search-result-copy">
      <h5 title={result.title}>{result.title}</h5>
      <p>{result.mediaType}{result.year !== null ? ` · ${result.year}` : ''}</p>
      <strong className={`media-search-state media-search-state--${result.state}`}>{result.state}</strong>
      {details.length > 0 && <ul>{details.map(detail => <li key={detail}>{detail}</li>)}</ul>}
    </div>
  </article>
}
