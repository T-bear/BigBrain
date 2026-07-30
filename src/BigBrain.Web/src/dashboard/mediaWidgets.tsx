import type { ReactNode } from 'react'
import { StatusBadge } from '../components'
import type { MediaInsight, MediaOverview, MediaQueueItem, MediaServiceStatus } from '../types'
import { ExpandableList } from './ExpandableList'
import { WidgetRegistry, type DashboardSectionRegistration, type WidgetRegistration } from './WidgetRegistry'

const allStates = ['online', 'degraded', 'unavailable', 'notConfigured'] as const
const icons: Record<string, string> = { Jellyfin: '▶', Sonarr: '◫', Radarr: '◆', Prowlarr: '⌁', qBittorrent: '⇣' }
const healthLabels: Record<string, string> = {
  excellent: 'Excellent',
  good: 'Good',
  actionRecommended: 'Action recommended',
  critical: 'Critical',
  notConfigured: 'Not configured',
}

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
    <header>
      <span className="service-icon" aria-hidden="true">{icons[service.serviceName] ?? '●'}</span>
      <div className="service-identity">
        <h3>{service.serviceName}</h3>
        <small title={service.sanitizedMessage ?? undefined}>
          {service.version ? `v${service.version}` : service.sanitizedMessage ?? 'Connected'}
          {service.responseTimeMs !== null && ` · ${service.responseTimeMs} ms`}
        </small>
      </div>
      <StatusBadge status={service.status} compact />
    </header>
    <div className="stat-grid">{children}</div>
  </article>
}

function Insight({ insight }: { insight: MediaInsight }) {
  return <article className={`insight insight--${insight.severity}`}>
    <span aria-hidden="true">●</span>
    <div><small>{insight.severity}</small><strong>{insight.title}</strong><p>{insight.message}</p></div>
  </article>
}

function ActivityCard({
  accessibleName,
  emptyMessage,
  items,
  totalCount,
  title,
}: {
  accessibleName: string
  emptyMessage: string
  items: MediaQueueItem[]
  totalCount: number
  title: string
}) {
  return <article className="card activity-widget">
    <header><h3>{title}</h3><span aria-label={`${totalCount} total`}>{totalCount}</span></header>
    <ExpandableList
      accessibleName={accessibleName}
      emptyMessage={emptyMessage}
      items={items}
      renderItem={(item, index) =>
        <li key={`${item.title}:${index}`}><div className="item-copy"><strong title={item.title}>{item.title}</strong><small>{item.status}</small></div><b>{item.progressPercent === null ? '—' : `${item.progressPercent.toFixed(0)}%`}</b></li>}
    />
  </article>
}

function isCompleted(torrent: MediaOverview['qBittorrent']['torrents'][number]) {
  return torrent.progressPercent >= 100
}

function isPaused(torrent: MediaOverview['qBittorrent']['torrents'][number]) {
  return !isCompleted(torrent) && /paused|stopped/i.test(torrent.state)
}

function isActive(torrent: MediaOverview['qBittorrent']['torrents'][number]) {
  return !isCompleted(torrent) && !isPaused(torrent) && /downloading|uploading|stalled|checking|queued/i.test(torrent.state)
}

function TorrentGroup({
  emptyMessage,
  label,
  torrents,
}: {
  emptyMessage: string
  label: string
  torrents: MediaOverview['qBittorrent']['torrents']
}) {
  return <div className="detail-group">
    <h4>{label} <span>{torrents.length}</span></h4>
    <ExpandableList
      accessibleName={`${label.toLowerCase()} torrents`}
      emptyMessage={emptyMessage}
      items={torrents}
      renderItem={(torrent, index) =>
        <li key={`${torrent.name}:${index}`}>
          <div className="item-copy"><strong title={torrent.name}>{torrent.name}</strong><small>{torrent.state} · {formatEta(torrent.etaSeconds)}</small></div>
          <b>{torrent.progressPercent.toFixed(0)}%</b>
        </li>}
    />
  </div>
}

const widgets: WidgetRegistration<MediaOverview>[] = [
  { id: 'media-health-summary', title: 'Media Health', section: 'media-health', order: 10, supportedStates: allStates, component: ({ data }) => {
    const notConfigured = data.healthStatusLevel === 'notConfigured'
    return <article className={`health-hero card health-hero--${data.healthStatusLevel}`}>
      <div className={`health-score ${notConfigured ? 'health-score--neutral' : ''}`} style={{ '--score': `${data.healthScore * 3.6}deg` } as React.CSSProperties}>
        <strong>{notConfigured ? '—' : data.healthScore}</strong><span>{notConfigured ? 'No score' : '/ 100'}</span>
      </div>
      <div className="health-copy">
        <p className="eyebrow">Media health</p>
        <div className="health-title"><h3>{healthLabels[data.healthStatusLevel] ?? data.healthStatusLevel}</h3><StatusBadge status={notConfigured ? 'notConfigured' : data.status} compact /></div>
        <p>{data.healthSummary}</p>
        <p className="health-time">Collected <time dateTime={data.collectedAtUtc}>{new Date(data.collectedAtUtc).toLocaleTimeString()}</time></p>
        <details className="score-help"><summary>How is this calculated?</summary><p>Availability, service warnings, indexer state, download activity and reported free space contribute to this rule-based score.</p></details>
      </div>
    </article>
  } },
  { id: 'media-insights', title: 'BigBrain Insights', section: 'insights', order: 10, supportedStates: allStates, component: ({ data }) =>
    <>{data.insights.length
      ? data.insights.map((insight, index) => <Insight insight={insight} key={`${insight.title}:${index}`} />)
      : <article className="insight insight--information insight--quiet"><span aria-hidden="true">●</span><div><strong>{data.status === 'notConfigured' ? 'Ready when you are' : 'Nothing needs attention'}</strong><p>{data.status === 'notConfigured' ? 'Configure a media service to begin collecting insights.' : 'No actionable media insights were found.'}</p></div></article>}</>
  },
  { id: 'jellyfin', title: 'Jellyfin', section: 'services', order: 10, supportedStates: allStates, component: ({ data }) =>
    <ServiceWidget service={data.jellyfin.service}><Stat label="Movies" value={data.jellyfin.movieCount} /><Stat label="Series" value={data.jellyfin.seriesCount} /><Stat label="Episodes" value={data.jellyfin.episodeCount} /><Stat label="Streams" value={data.jellyfin.activeStreamCount} /></ServiceWidget> },
  { id: 'sonarr', title: 'Sonarr', section: 'services', order: 20, supportedStates: allStates, component: ({ data }) =>
    <ServiceWidget service={data.sonarr.service}><Stat label="Series" value={data.sonarr.seriesCount} /><Stat label="Monitored" value={data.sonarr.monitoredSeriesCount} /><Stat label="Missing" value={data.sonarr.missingMonitoredEpisodes} /><Stat label="Queue" value={data.sonarr.queueCount} /></ServiceWidget> },
  { id: 'radarr', title: 'Radarr', section: 'services', order: 30, supportedStates: allStates, component: ({ data }) =>
    <ServiceWidget service={data.radarr.service}><Stat label="Movies" value={data.radarr.movieCount} /><Stat label="Missing" value={data.radarr.missingMovieCount} /><Stat label="Queue" value={data.radarr.queueCount} /><Stat label="Upgrades" value={data.radarr.qualityUpgradeCount} /></ServiceWidget> },
  { id: 'prowlarr', title: 'Prowlarr', section: 'services', order: 40, supportedStates: allStates, component: ({ data }) =>
    <ServiceWidget service={data.prowlarr.service}><Stat label="Indexers" value={data.prowlarr.indexerCount} /><Stat label="Online" value={`${data.prowlarr.onlineIndexerCount}/${data.prowlarr.enabledIndexerCount}`} /><Stat label="RSS" value={data.prowlarr.rssEnabledIndexerCount} /><Stat label="Failures" value={data.prowlarr.recentFailures.length} /></ServiceWidget> },
  { id: 'qbittorrent', title: 'qBittorrent', section: 'services', order: 50, supportedStates: allStates, component: ({ data }) =>
    <ServiceWidget service={data.qBittorrent.service}><Stat label="Active" value={data.qBittorrent.activeCount} /><Stat label="Paused" value={data.qBittorrent.pausedCount} /><Stat label="Download" value={formatBytes(data.qBittorrent.downloadSpeedBytesPerSecond, '/s')} /><Stat label="Free space" value={formatBytes(data.qBittorrent.freeSpaceBytes)} /></ServiceWidget> },
  { id: 'sonarr-queue', title: 'Sonarr queue', section: 'activity', order: 10, supportedStates: allStates, component: ({ data }) =>
    <ActivityCard accessibleName="Sonarr queue items" emptyMessage="Sonarr queue is clear." items={data.sonarr.queue} title="Sonarr queue" totalCount={data.sonarr.queueCount} /> },
  { id: 'radarr-queue', title: 'Radarr queue', section: 'activity', order: 20, supportedStates: allStates, component: ({ data }) =>
    <ActivityCard accessibleName="Radarr queue items" emptyMessage="Radarr queue is clear." items={data.radarr.queue} title="Radarr queue" totalCount={data.radarr.queueCount} /> },
  { id: 'recently-added', title: 'Recently added', section: 'activity', order: 30, supportedStates: allStates, component: ({ data }) =>
    <article className="card activity-widget"><header><h3>Recently added</h3><span aria-label={`${data.jellyfin.recentlyAdded.length} total`}>{data.jellyfin.recentlyAdded.length}</span></header>
      <ExpandableList accessibleName="recently added items" emptyMessage="No recently added media." items={data.jellyfin.recentlyAdded}
        renderItem={(item, index) => <li key={`${item.name}:${index}`}><div className="item-copy"><strong title={item.name}>{item.name}</strong><small>{item.mediaType}</small></div></li>} />
    </article> },
  { id: 'torrent-details', title: 'qBittorrent downloads', section: 'details', order: 10, supportedStates: allStates, component: ({ data }) => {
    const active = data.qBittorrent.torrents.filter(isActive)
    const paused = data.qBittorrent.torrents.filter(isPaused)
    const completed = data.qBittorrent.torrents.filter(isCompleted)
    return <article className="card detail-widget"><header><div><p className="eyebrow">qBittorrent</p><h3>Download details</h3></div><span>{data.qBittorrent.torrents.length} total</span></header>
      <TorrentGroup emptyMessage="No active downloads." label="Active" torrents={active} />
      <TorrentGroup emptyMessage="No paused or stopped downloads." label="Paused / stopped" torrents={paused} />
      <TorrentGroup emptyMessage="No completed downloads in the recent result." label="Completed" torrents={completed} />
    </article>
  } },
  { id: 'health-warnings', title: 'Health warnings', section: 'details', order: 20, supportedStates: allStates, component: ({ data }) => {
    const warnings = [...data.sonarr.healthWarnings, ...data.radarr.healthWarnings, ...data.prowlarr.healthWarnings]
    return <article className="card detail-widget"><header><div><p className="eyebrow">Services</p><h3>Health warnings</h3></div><span>{warnings.length} total</span></header>
      <ExpandableList accessibleName="health warnings" emptyMessage="No service health warnings." items={warnings}
        renderItem={(warning, index) => <li key={`${warning.source}:${index}`}><div className="item-copy"><strong>{warning.source}</strong><small title={warning.message}>{warning.message}</small></div></li>} />
    </article>
  } },
]

const sections: DashboardSectionRegistration[] = [
  { id: 'media-health', order: 10, className: 'dashboard-hero', label: 'Health summary', title: 'Media Health', defaultExpanded: false },
  { id: 'insights', order: 20, className: 'insights-grid', label: 'Prioriterade signaler', title: 'BigBrain Insights', defaultExpanded: true },
  { id: 'services', order: 30, className: 'widget-grid', label: 'Anslutna tjänster', title: 'Service Overview', defaultExpanded: true },
  { id: 'activity', order: 40, className: 'activity-grid', label: 'Senaste aktivitet', title: 'Activity', defaultExpanded: false },
  { id: 'details', order: 50, className: 'details-grid', label: 'Fördjupning', title: 'Detaljer · köer, nedladdningar och varningar', defaultExpanded: false },
]

export const mediaWidgetRegistry = new WidgetRegistry(widgets, sections)
