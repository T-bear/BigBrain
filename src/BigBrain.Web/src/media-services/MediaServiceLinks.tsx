import { useEffect, useState } from 'react'
import { getMediaServiceLinks } from '../api'
import type { MediaServiceLink } from '../types'

export function MediaServiceLinks() {
  const [links, setLinks] = useState<MediaServiceLink[]>([])
  const [loaded, setLoaded] = useState(false)

  useEffect(() => {
    const controller = new AbortController()
    void getMediaServiceLinks(controller.signal)
      .then(result => setLinks(Array.isArray(result) ? result : []))
      .catch(() => setLinks([]))
      .finally(() => setLoaded(true))
    return () => controller.abort()
  }, [])

  const icons: Record<MediaServiceLink['id'], string> = {
    jellyfin: '▶',
    radarr: '◆',
    sonarr: '▥',
    prowlarr: '⌕',
    qbittorrent: '⇩',
  }
  return <section id="services" className="media-service-links card" aria-labelledby="service-links-heading">
    <div><p className="eyebrow">Snabbval</p><h3 id="service-links-heading">Mediatjänster</h3></div>
    {!loaded && <p className="muted" aria-live="polite">Laddar tjänstelänkar…</p>}
    {loaded && links.length === 0 && <p className="muted">Tjänstelänkarna kunde inte hämtas.</p>}
    {links.length > 0 && <div className="media-service-link-list">{links.map(link =>
      link.enabled && link.url
        ? <a key={link.id} href={link.url} target="_blank" rel="noreferrer">
            <span className="media-service-link-icon" aria-hidden="true">{icons[link.id]}</span>
            <span>Öppna <strong>{link.displayName}</strong></span>
            <span aria-hidden="true">↗</span>
          </a>
        : <span key={link.id} className="media-service-link-disabled" aria-disabled="true">
            <span className="media-service-link-icon" aria-hidden="true">{icons[link.id]}</span>
            <span><strong>{link.displayName}</strong><small>Inte konfigurerad</small></span>
          </span>)}</div>}
  </section>
}
