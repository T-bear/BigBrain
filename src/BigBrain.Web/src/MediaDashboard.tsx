import { useCallback, useEffect, useRef, useState } from 'react'
import { getMediaOverview } from './api'
import { ModuleCard, StatusBadge } from './components'
import type { MediaOverview, MediaQueueItem } from './types'

function formatEta(seconds: number | null) {
  if (seconds === null) return 'ETA unavailable'
  const hours = Math.floor(seconds / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  return hours > 0 ? `${hours}h ${minutes}m remaining` : `${minutes}m remaining`
}

function formatRate(bytesPerSecond: number) {
  if (bytesPerSecond < 1024) return `${bytesPerSecond} B/s`
  if (bytesPerSecond < 1024 ** 2) return `${(bytesPerSecond / 1024).toFixed(1)} KiB/s`
  return `${(bytesPerSecond / 1024 ** 2).toFixed(1)} MiB/s`
}

function QueueList({ title, count, items }: { title: string; count: number; items: MediaQueueItem[] }) {
  return (
    <article className="card media-list">
      <h3>{title} <span className="muted">({count})</span></h3>
      {items.length === 0 ? <p className="muted">Queue is empty.</p> : (
        <ul>
          {items.map((item, index) => (
            <li key={`${item.title}:${index}`}>
              <span>{item.title}</span>
              <span>{item.status}{item.progressPercent === null ? '' : ` · ${item.progressPercent.toFixed(1)}%`}</span>
            </li>
          ))}
        </ul>
      )}
    </article>
  )
}

export function MediaDashboard() {
  const [overview, setOverview] = useState<MediaOverview | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)
  const controllerRef = useRef<AbortController | null>(null)

  const refresh = useCallback(async () => {
    controllerRef.current?.abort()
    const controller = new AbortController()
    controllerRef.current = controller
    setLoading(true)
    try {
      setOverview(await getMediaOverview(controller.signal))
      setError(false)
    } catch (requestError) {
      if (requestError instanceof Error && requestError.name !== 'AbortError') setError(true)
    } finally {
      if (!controller.signal.aborted) setLoading(false)
    }
  }, [])

  useEffect(() => {
    void refresh()
    return () => controllerRef.current?.abort()
  }, [refresh])

  const warnings = overview
    ? [...overview.sonarr.healthWarnings, ...overview.radarr.healthWarnings, ...overview.prowlarr.healthWarnings]
    : []

  return (
    <section id="media" aria-labelledby="media-heading" className="media-section">
      <div className="section-heading">
        <div>
          <p className="eyebrow">Media module</p>
          <h2 id="media-heading">Media overview</h2>
        </div>
        <div className="section-actions">
          <StatusBadge status={overview?.status ?? (error ? 'error' : 'loading')} />
          <button type="button" className="secondary-button" onClick={() => void refresh()} disabled={loading}>
            Refresh
          </button>
        </div>
      </div>

      {loading && !overview && <p aria-live="polite">Loading media services…</p>}
      {error && (
        <p role="alert" className="notice notice--error">
          Media overview could not be loaded.{overview ? ' Showing the latest successful update.' : ''}
        </p>
      )}
      {overview && (
        <>
          {overview.status === 'degraded' && (
            <p className="notice">Some media services are unavailable. Available data is still shown.</p>
          )}
          <div className="service-grid">
            {overview.services.map((service) => (
              <ModuleCard key={service.serviceName} title={service.serviceName} status={service.status}>
                <p>{service.version ? `Version ${service.version}` : service.sanitizedMessage ?? 'Connected'}</p>
                {service.responseTimeMs !== null && <p className="muted">{service.responseTimeMs} ms</p>}
              </ModuleCard>
            ))}
          </div>

          <div className="media-summary">
            <article className="card media-list">
              <h3>Active downloads</h3>
              <p className="muted">
                {overview.qBittorrent.activeCount} active · {formatRate(overview.qBittorrent.downloadSpeedBytesPerSecond)} down · {formatRate(overview.qBittorrent.uploadSpeedBytesPerSecond)} up
              </p>
              {overview.qBittorrent.torrents.length === 0 ? <p className="muted">No torrents to show.</p> : (
                <ul>
                  {overview.qBittorrent.torrents.map((torrent, index) => (
                    <li key={`${torrent.name}:${index}`}>
                      <span>{torrent.name}</span>
                      <progress aria-label={`${torrent.name} progress`} max="100" value={torrent.progressPercent} />
                      <span>{torrent.progressPercent.toFixed(1)}% · {torrent.state} · {formatEta(torrent.etaSeconds)}</span>
                    </li>
                  ))}
                </ul>
              )}
            </article>
            <QueueList title="Sonarr queue" count={overview.sonarr.queueCount} items={overview.sonarr.queue} />
            <QueueList title="Radarr queue" count={overview.radarr.queueCount} items={overview.radarr.queue} />
          </div>

          {warnings.length > 0 && (
            <article className="card media-list">
              <h3>Health warnings</h3>
              <ul>{warnings.map((warning, index) => <li key={`${warning.source}:${index}`}>{warning.source}: {warning.message}</li>)}</ul>
            </article>
          )}
          <p className="last-updated">
            Last updated <time dateTime={overview.collectedAtUtc}>{new Date(overview.collectedAtUtc).toLocaleTimeString()}</time>
          </p>
        </>
      )}
    </section>
  )
}
