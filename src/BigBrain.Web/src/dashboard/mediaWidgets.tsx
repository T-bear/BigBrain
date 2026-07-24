import type { ReactNode } from 'react'
import { StatusBadge } from '../components'
import type { MediaInsight, MediaOverview, MediaQueueItem, MediaServiceStatus } from '../types'
import { WidgetRegistry, type DashboardSectionRegistration, type WidgetRegistration } from './WidgetRegistry'

const allStates = ['online', 'degraded', 'unavailable', 'notConfigured'] as const
const icons: Record<string, string> = { Jellyfin: '▶', Sonarr: '◫', Radarr: '◆', Prowlarr: '⌁', qBittorrent: '⇣' }

function formatBytes(value: number | null, suffix = '') {
  if (value === null) return 'Unavailable'
  const units = ['B', 'KiB', 'MiB', 'GiB', 'TiB']
  let size = value
  let unit = 0
  while (size >= 1024 && unit < units.length - 1) { size /= 1024; unit++ }
  return `${size.toFixed(unit === 0 ? 0 : 1)} ${units[unit]}${suffix}`
}

function formatEta(seconds: number | null) {
  if (seconds === null) return 'No ETA'
  const hours = Math.floor(seconds / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  return hours ? `${hours}h ${minutes}m` : `${minutes}m`
}

function Stat({ label, value }: { label: string; value: ReactNode }) {
  return <div className="media-stat"><span>{label}</span><strong>{value}</strong></div>
}

function ServiceWidget({ service, children }: { service: MediaServiceStatus; children: ReactNode }) {
  return <article className={`card service-widget service-widget--${service.status.toLowerCase()}`}>
    <header><span className="service-icon" aria-hidden="true">{icons[service.serviceName] ?? '●'}</span>
      <div><h3>{service.serviceName}</h3><small>{service.version ? `v${service.version}` : service.sanitizedMessage ?? 'Connected'}</small></div>
      <StatusBadge status={service.status} compact /></header>
    <div className="stat-grid">{children}</div>
  </article>
}

function Insight({ insight }: { insight: MediaInsight }) {
  return <article className={`insight insight--${insight.severity}`}><span aria-hidden="true">●</span><div><strong>{insight.title}</strong><p>{insight.message}</p></div></article>
}

function Queue({ title, count, items }: { title: string; count: number; items: MediaQueueItem[] }) {
  return <article className="card activity-widget"><header><h3>{title}</h3><span>{count}</span></header>
    {items.length === 0 ? <p className="muted">Queue is clear.</p> :
      <ul>{items.slice(0, 5).map((item, index) => <li key={`${item.title}:${index}`}><div><strong>{item.title}</strong><small>{item.status}</small></div><b>{item.progressPercent === null ? '—' : `${item.progressPercent.toFixed(0)}%`}</b></li>)}</ul>}
  </article>
}

const widgets: WidgetRegistration<MediaOverview>[] = [
  { id: 'media-health', title: 'Media Health', section: 'hero', order: 10, supportedStates: allStates, component: ({ data }) =>
    <article className="health-hero card"><div className="health-score" style={{ '--score': `${data.healthScore * 3.6}deg` } as React.CSSProperties}><strong>{data.healthScore}</strong><span>/ 100</span></div>
      <div><p className="eyebrow">Media health</p><h3>{data.healthSummary}</h3><p>One score across availability, warnings, downloads, indexers and storage.</p></div></article> },
  { id: 'media-insights', title: 'BigBrain Insights', section: 'insights', order: 10, supportedStates: allStates, component: ({ data }) =>
    <>{data.insights.length ? data.insights.map((insight, index) => <Insight insight={insight} key={`${insight.title}:${index}`} />) : <p className="muted">No insights available yet.</p>}</> },
  { id: 'jellyfin', title: 'Jellyfin', section: 'widgets', order: 10, supportedStates: allStates, component: ({ data }) =>
    <ServiceWidget service={data.jellyfin.service}><Stat label="Movies" value={data.jellyfin.movieCount} /><Stat label="Series" value={data.jellyfin.seriesCount} /><Stat label="Episodes" value={data.jellyfin.episodeCount} /><Stat label="Libraries" value={data.jellyfin.libraryCount} /><Stat label="Active users" value={data.jellyfin.activeUserCount} /><Stat label="Streams" value={data.jellyfin.activeStreamCount} /></ServiceWidget> },
  { id: 'sonarr', title: 'Sonarr', section: 'widgets', order: 20, supportedStates: allStates, component: ({ data }) =>
    <ServiceWidget service={data.sonarr.service}><Stat label="Series" value={data.sonarr.seriesCount} /><Stat label="Monitored" value={data.sonarr.monitoredSeriesCount} /><Stat label="Missing" value={data.sonarr.missingMonitoredEpisodes} /><Stat label="Queue" value={data.sonarr.queueCount} /><Stat label="Calendar" value={data.sonarr.calendar.length} /><Stat label="Health" value={data.sonarr.healthWarnings.length ? `${data.sonarr.healthWarnings.length} warning` : 'Clear'} /></ServiceWidget> },
  { id: 'radarr', title: 'Radarr', section: 'widgets', order: 30, supportedStates: allStates, component: ({ data }) =>
    <ServiceWidget service={data.radarr.service}><Stat label="Movies" value={data.radarr.movieCount} /><Stat label="Monitored" value={data.radarr.monitoredMovieCount} /><Stat label="Missing" value={data.radarr.missingMovieCount} /><Stat label="Queue" value={data.radarr.queueCount} /><Stat label="Upgrades" value={data.radarr.qualityUpgradeCount} /><Stat label="Health" value={data.radarr.healthWarnings.length ? `${data.radarr.healthWarnings.length} warning` : 'Clear'} /></ServiceWidget> },
  { id: 'prowlarr', title: 'Prowlarr', section: 'widgets', order: 40, supportedStates: allStates, component: ({ data }) =>
    <ServiceWidget service={data.prowlarr.service}><Stat label="Indexers" value={data.prowlarr.indexerCount} /><Stat label="Online" value={`${data.prowlarr.onlineIndexerCount}/${data.prowlarr.enabledIndexerCount}`} /><Stat label="RSS" value={data.prowlarr.rssEnabledIndexerCount} /><Stat label="Failures" value={data.prowlarr.recentFailures.length} /><Stat label="Health" value={data.prowlarr.healthWarnings.length ? `${data.prowlarr.healthWarnings.length} warning` : 'Clear'} /></ServiceWidget> },
  { id: 'qbittorrent', title: 'qBittorrent', section: 'widgets', order: 50, supportedStates: allStates, component: ({ data }) =>
    <ServiceWidget service={data.qBittorrent.service}><Stat label="Active" value={data.qBittorrent.activeCount} /><Stat label="Paused" value={data.qBittorrent.pausedCount} /><Stat label="Download" value={formatBytes(data.qBittorrent.downloadSpeedBytesPerSecond, '/s')} /><Stat label="Upload" value={formatBytes(data.qBittorrent.uploadSpeedBytesPerSecond, '/s')} /><Stat label="ETA" value={formatEta(data.qBittorrent.etaSeconds)} /><Stat label="Ratio" value={data.qBittorrent.averageRatio?.toFixed(2) ?? '—'} /><Stat label="Transferred" value={`↓${formatBytes(data.qBittorrent.totalDownloadedBytes)} ↑${formatBytes(data.qBittorrent.totalUploadedBytes)}`} /><Stat label="Free space" value={formatBytes(data.qBittorrent.freeSpaceBytes)} /></ServiceWidget> },
  { id: 'sonarr-queue', title: 'Sonarr queue', section: 'activity', order: 10, supportedStates: allStates, component: ({ data }) => <Queue title="Sonarr queue" count={data.sonarr.queueCount} items={data.sonarr.queue} /> },
  { id: 'radarr-queue', title: 'Radarr queue', section: 'activity', order: 20, supportedStates: allStates, component: ({ data }) => <Queue title="Radarr queue" count={data.radarr.queueCount} items={data.radarr.queue} /> },
  { id: 'recently-added', title: 'Recently added', section: 'activity', order: 30, supportedStates: allStates, component: ({ data }) =>
    <article className="card activity-widget"><header><h3>Recently added</h3><span>{data.jellyfin.recentlyAdded.length}</span></header><ul>{data.jellyfin.recentlyAdded.slice(0, 5).map((item, index) => <li key={`${item.name}:${index}`}><div><strong>{item.name}</strong><small>{item.mediaType}</small></div></li>)}</ul></article> },
]

const sections: DashboardSectionRegistration[] = [
  { id: 'hero', order: 10, className: 'dashboard-hero' },
  { id: 'insights', order: 20, className: 'insights-grid', label: 'BigBrain Insights', title: 'What deserves your attention' },
  { id: 'widgets', order: 30, className: 'widget-grid' },
  { id: 'activity', order: 40, className: 'activity-grid' },
]

export const mediaWidgetRegistry = new WidgetRegistry(widgets, sections)
