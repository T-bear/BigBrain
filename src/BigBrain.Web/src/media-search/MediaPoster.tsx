import { useState } from 'react'

export function MediaPoster({ title, url }: { title: string; url: string | null }) {
  const [failedUrl, setFailedUrl] = useState<string | null>(null)
  const visibleUrl = url && failedUrl !== url ? url : null

  return visibleUrl
    ? <img
        src={visibleUrl}
        alt={`Poster för ${title}`}
        loading="lazy"
        onError={() => setFailedUrl(visibleUrl)}
      />
    : <div className="media-search-poster-placeholder" role="img" aria-label={`Poster saknas för ${title}`}>
        <span aria-hidden="true">▧</span>
        <small>Poster saknas</small>
      </div>
}
