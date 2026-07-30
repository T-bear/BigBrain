import { cleanup, fireEvent, render, screen, within } from '@testing-library/react'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import { MediaDashboard } from './MediaDashboard'
import type { MediaOverview, MediaServiceStatus } from './types'
import { DASHBOARD_LAYOUT_STORAGE_KEY } from './dashboard/dashboardLayout'

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
  } as MediaServiceStatus
}

function overview(status = 'online', serviceStatuses: Record<string, string> = {}): MediaOverview {
  const services = ['Jellyfin', 'Sonarr', 'Radarr', 'Prowlarr', 'qBittorrent']
    .map((name) => service(name, serviceStatuses[name] ?? status))
  return {
    status,
    healthScore: status === 'online' ? 100 : 40,
    healthSummary: status === 'online' ? 'Everything looks great' : status === 'notConfigured' ? 'Configure media services to calculate health.' : 'Immediate attention is recommended',
    healthStatusLevel: status === 'online' ? 'excellent' : status === 'notConfigured' ? 'notConfigured' : 'critical',
    insights: status === 'notConfigured' ? [] : status === 'online'
      ? [{ severity: 'success', title: 'All services healthy', message: 'Everything is responding.' }]
      : [{ severity: 'critical', title: 'Services unavailable', message: 'One service cannot be reached.' }],
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
  } as MediaOverview
}

function response(body: unknown) {
  return { ok: true, json: async () => body }
}

beforeEach(() => {
  window.localStorage.setItem(DASHBOARD_LAYOUT_STORAGE_KEY, JSON.stringify({
    version: 1,
    expanded: {
      'media-jobs': true,
      'media-health': true,
      insights: true,
      services: true,
      activity: true,
      details: true,
    },
  }))
})

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
  const jellyfinCard = screen.getByRole('heading', { name: 'Jellyfin' }).closest('article')
  expect(jellyfinCard).not.toBeNull()
  expect(within(jellyfinCard as HTMLElement).getByText('online')).toBeInTheDocument()
  expect(within(jellyfinCard as HTMLElement).queryByText('unavailable')).not.toBeInTheDocument()
  expect(screen.getByText('New movie')).toBeInTheDocument()
  expect(screen.getByText('Sonarr queue is clear.')).toBeInTheDocument()
  expect(screen.getByText('Radarr queue is clear.')).toBeInTheDocument()
  expect(screen.queryByRole('button', { name: /\b(pause|resume|delete|add|play)\b/i })).not.toBeInTheDocument()
})

test('shows partial success with degraded and unavailable services', async () => {
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(response(overview('degraded', {
    Jellyfin: 'online',
    Sonarr: 'unavailable',
    Radarr: 'degraded',
  })))))
  render(<MediaDashboard />)

  expect(await screen.findByText('Critical')).toBeInTheDocument()
  expect(screen.getByText('Services unavailable')).toBeInTheDocument()
  expect(screen.getByRole('heading', { name: 'Jellyfin' })).toBeInTheDocument()
  expect(screen.getByTitle('Service is unavailable.')).toBeInTheDocument()
  expect(screen.getAllByTitle('Service is degraded.').length).toBeGreaterThan(0)
})

test('shows not configured services', async () => {
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(response(overview('notConfigured')))))
  render(<MediaDashboard />)

  expect(await screen.findAllByText('Service is notConfigured.')).toHaveLength(5)
  expect(screen.getByRole('heading', { name: 'Not configured' })).toBeInTheDocument()
  expect(screen.getByText('No score')).toBeInTheDocument()
  expect(screen.queryByText('Critical')).not.toBeInTheDocument()
  expect(screen.queryByText('All services healthy')).not.toBeInTheDocument()
})

test('keeps the dashboard usable when every service is offline', async () => {
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(response(overview('unavailable')))))
  render(<MediaDashboard />)

  expect(await screen.findAllByTitle('Service is unavailable.')).toHaveLength(5)
  expect(screen.getByRole('heading', { name: 'qBittorrent' })).toBeInTheDocument()
  expect(screen.getByText('Sonarr queue is clear.')).toBeInTheDocument()
  expect(screen.getByText('Radarr queue is clear.')).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Refresh' })).toBeEnabled()
})

test('shows total API failure', async () => {
  vi.stubGlobal('fetch', vi.fn(() => Promise.reject(new Error('network failure'))))
  render(<MediaDashboard />)

  expect(await screen.findByRole('alert')).toHaveTextContent('Media dashboard could not be loaded.')
})

test('limits long lists and supports accessible show all and collapse controls', async () => {
  const data = overview()
  data.sonarr.queueCount = 5
  data.sonarr.queue = Array.from({ length: 5 }, (_, index) => ({
    title: `Very long release name ${index} ${'x'.repeat(100)}`,
    status: 'downloading',
    progressPercent: index * 10,
  }))
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(response(data))))
  render(<MediaDashboard />)

  const queue = await screen.findByRole('heading', { name: 'Sonarr queue' })
  const card = queue.closest('article')
  expect(card).not.toBeNull()
  expect(within(card!).getAllByRole('listitem')).toHaveLength(3)
  const showAll = within(card!).getByRole('button', { name: 'Show all 5 Sonarr queue items' })
  expect(showAll).toHaveAttribute('aria-expanded', 'false')

  fireEvent.click(showAll)
  expect(within(card!).getAllByRole('listitem')).toHaveLength(5)
  const collapse = within(card!).getByRole('button', { name: 'Show fewer Sonarr queue items' })
  expect(collapse).toHaveAttribute('aria-expanded', 'true')
  fireEvent.click(collapse)
  expect(within(card!).getAllByRole('listitem')).toHaveLength(3)
  expect(within(card!).getAllByTitle(/Very long release name/)[0].closest('.item-copy')).not.toBeNull()
})

test('separates active, paused and completed torrents', async () => {
  const data = overview()
  data.qBittorrent.torrents = [
    { name: 'Downloading item', progressPercent: 20, state: 'downloading', category: null, etaSeconds: 60 },
    { name: 'Stopped item', progressPercent: 50, state: 'stoppedDL', category: null, etaSeconds: null },
    { name: 'Completed item', progressPercent: 100, state: 'stoppedUP', category: null, etaSeconds: null },
  ]
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(response(data))))
  render(<MediaDashboard />)

  const activeHeading = await screen.findByRole('heading', { name: /Active/ })
  const activeGroup = activeHeading.closest('.detail-group')
  expect(activeGroup).not.toBeNull()
  expect(within(activeGroup as HTMLElement).getByText('Downloading item')).toBeInTheDocument()
  expect(within(activeGroup as HTMLElement).queryByText('Completed item')).not.toBeInTheDocument()
  expect(screen.getByRole('heading', { name: /Paused \/ stopped/ })).toBeInTheDocument()
  expect(screen.getByRole('heading', { name: /Completed/ })).toBeInTheDocument()
})

test('renders registered production sections in information hierarchy order', async () => {
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(response(overview()))))
  const { container } = render(<MediaDashboard />)
  await screen.findByText('Everything looks great')

  expect([...container.querySelectorAll('[data-dashboard-section]')].map(element => element.getAttribute('data-dashboard-section')))
    .toEqual(['media-health', 'insights', 'services', 'activity', 'details'])
})

test('collapses and expands a module with accessible state', async () => {
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(response(overview()))))
  render(<MediaDashboard />)
  await screen.findByText('All services healthy')

  const collapse = screen.getByRole('button', { name: 'Minimera BigBrain Insights' })
  expect(collapse).toHaveAttribute('aria-expanded', 'true')
  expect(collapse).toHaveAttribute('aria-controls', 'insights-content')

  fireEvent.click(collapse)
  const expand = screen.getByRole('button', { name: 'Expandera BigBrain Insights' })
  expect(expand).toHaveAttribute('aria-expanded', 'false')
  expect(document.getElementById('insights-content')).toHaveAttribute('hidden')

  fireEvent.click(expand)
  expect(screen.getByRole('button', { name: 'Minimera BigBrain Insights' })).toHaveAttribute('aria-expanded', 'true')
  expect(document.getElementById('insights-content')).not.toHaveAttribute('hidden')
})

test('restores persisted module state after a new render', async () => {
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(response(overview()))))
  const firstRender = render(<MediaDashboard />)
  await screen.findByText('All services healthy')
  fireEvent.click(screen.getByRole('button', { name: 'Minimera BigBrain Insights' }))
  firstRender.unmount()

  render(<MediaDashboard />)
  expect(await screen.findByRole('button', { name: 'Expandera BigBrain Insights' }))
    .toHaveAttribute('aria-expanded', 'false')
})

test('falls back safely when persisted module state is invalid', async () => {
  window.localStorage.setItem(DASHBOARD_LAYOUT_STORAGE_KEY, '{invalid json')
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(response(overview()))))
  render(<MediaDashboard />)

  expect(await screen.findByRole('button', { name: 'Minimera Media Jobs' }))
    .toHaveAttribute('aria-expanded', 'true')
  expect(screen.getByRole('button', { name: 'Expandera Media Health' }))
    .toHaveAttribute('aria-expanded', 'false')
})
