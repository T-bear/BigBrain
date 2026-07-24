import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, expect, test, vi } from 'vitest'
import { MediaDashboard } from './MediaDashboard'

const checkedAtUtc = '2026-07-23T10:00:00Z'

function service(serviceName: string, status = 'online') {
  return {
    serviceName,
    status,
    version: status === 'online' ? '1.0' : null,
    responseTimeMs: status === 'notConfigured' ? null : 12,
    checkedAtUtc,
    sanitizedMessage: status === 'online' ? null : `Service is ${status}.`,
    isConfigured: status !== 'notConfigured',
  }
}

function overview(status = 'online', serviceStatuses: Record<string, string> = {}) {
  const services = ['Jellyfin', 'Sonarr', 'Radarr', 'Prowlarr', 'qBittorrent']
    .map((name) => service(name, serviceStatuses[name] ?? status))
  return {
    status,
    healthScore: status === 'online' ? 100 : 40,
    healthSummary: status === 'online' ? 'Everything looks great' : 'Action recommended',
    healthStatusLevel: status === 'online' ? 'healthy' : 'critical',
    insights: [{ severity: 'success', title: 'All services healthy', message: 'Everything is responding.' }],
    collectedAtUtc: checkedAtUtc,
    services,
    qBittorrent: {
      service: services[4],
      activeCount: 1,
      pausedCount: 0,
      completedCount: 2,
      downloadSpeedBytesPerSecond: 2_097_152,
      uploadSpeedBytesPerSecond: 1024,
      etaSeconds: 3600,
      averageRatio: 1.25,
      totalDownloadedBytes: 4_194_304,
      totalUploadedBytes: 2_097_152,
      freeSpaceBytes: 107_374_182_400,
      torrents: [{ name: 'Safe download', progressPercent: 42.5, state: 'downloading', category: 'tv', etaSeconds: 3600 }],
    },
    sonarr: { service: services[1], seriesCount: 20, monitoredSeriesCount: 18, missingMonitoredEpisodes: 3, queueCount: 0, queue: [], calendar: [], recentHistory: [], healthWarnings: [] },
    radarr: { service: services[2], movieCount: 50, monitoredMovieCount: 45, missingMovieCount: 2, qualityUpgradeCount: 4, queueCount: 0, queue: [], recentHistory: [], healthWarnings: [] },
    prowlarr: { service: services[3], indexerCount: 5, enabledIndexerCount: 4, onlineIndexerCount: 4, rssEnabledIndexerCount: 3, indexerStatuses: [], recentFailures: [], healthWarnings: [] },
    jellyfin: { service: services[0], libraryCount: 2, movieCount: 10, seriesCount: 5, episodeCount: 80, activeUserCount: 1, activeStreamCount: 1, recentlyAdded: [{ name: 'New movie', mediaType: 'Movie', dateCreatedUtc: checkedAtUtc }] },
  }
}

function response(body: unknown) {
  return { ok: true, json: async () => body }
}

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

test('shows loading and then online activity without write controls', async () => {
  let resolveRequest: ((value: unknown) => void) | undefined
  vi.stubGlobal('fetch', vi.fn(() => new Promise((resolve) => { resolveRequest = resolve })))
  render(<MediaDashboard />)

  expect(screen.getByText('Loading media intelligence…')).toBeInTheDocument()
  resolveRequest?.(response(overview()))

  expect(await screen.findByText('All services healthy')).toBeInTheDocument()
  expect(screen.getByText('New movie')).toBeInTheDocument()
  expect(screen.getAllByText('Queue is clear.')).toHaveLength(2)
  expect(screen.queryByRole('button', { name: /pause|resume|delete|add|search/i })).not.toBeInTheDocument()
})

test('shows partial success with degraded and unavailable services', async () => {
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(response(overview('degraded', {
    Jellyfin: 'online',
    Sonarr: 'unavailable',
    Radarr: 'degraded',
  })))))
  render(<MediaDashboard />)

  expect(await screen.findByText('Action recommended')).toBeInTheDocument()
  expect(screen.getByRole('heading', { name: 'Jellyfin' })).toBeInTheDocument()
  expect(screen.getByText('Service is unavailable.')).toBeInTheDocument()
  expect(screen.getAllByText('Service is degraded.').length).toBeGreaterThan(0)
})

test('shows not configured services', async () => {
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(response(overview('notConfigured')))))
  render(<MediaDashboard />)

  expect(await screen.findAllByText('Service is notConfigured.')).toHaveLength(5)
})

test('keeps the dashboard usable when every service is offline', async () => {
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(response(overview('unavailable')))))
  render(<MediaDashboard />)

  expect(await screen.findAllByText('Service is unavailable.')).toHaveLength(5)
  expect(screen.getByRole('heading', { name: 'qBittorrent' })).toBeInTheDocument()
  expect(screen.getAllByText('Queue is clear.')).toHaveLength(2)
  expect(screen.getByRole('button', { name: 'Refresh' })).toBeEnabled()
})

test('shows total API failure', async () => {
  vi.stubGlobal('fetch', vi.fn(() => Promise.reject(new Error('network failure'))))
  render(<MediaDashboard />)

  expect(await screen.findByRole('alert')).toHaveTextContent('Media dashboard could not be loaded.')
})
