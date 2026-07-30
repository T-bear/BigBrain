import { act, cleanup, fireEvent, render, screen } from '@testing-library/react'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import App from './App'
import { DASHBOARD_LAYOUT_STORAGE_KEY } from './dashboard/dashboardLayout'

const modules = [
  { id: 'docker', name: 'Docker', description: '', route: '/#docker', status: 'Unavailable', dashboardWidgets: [], capabilities: [] },
  { id: 'media', name: 'Media', description: '', route: '/#media', status: 'NotConfigured', dashboardWidgets: [], capabilities: [] },
  { id: 'system', name: 'System', description: '', route: '/', status: 'Available', dashboardWidgets: [], capabilities: [] },
]
const overview = {
  hostname: 'bigbrain-host',
  operatingSystem: 'Linux',
  architecture: 'X64',
  uptimeSeconds: 310_920,
  cpu: { usagePercent: 23.5, logicalProcessorCount: 8 },
  memory: { totalBytes: 17_179_869_184, usedBytes: 8_589_934_592, availableBytes: 8_589_934_592, usagePercent: 50 },
  disks: [
    { filesystemId: 'system', displayName: 'System Storage', totalBytes: 1_000_000_000_000, usedBytes: 400_000_000_000, availableBytes: 600_000_000_000, usagePercent: 40 },
    { filesystemId: 'media', displayName: 'Media Storage', totalBytes: 2_000_000_000_000, usedBytes: 500_000_000_000, availableBytes: 1_500_000_000_000, usagePercent: 25 },
  ],
  temperatureCelsius: null,
  collectedAtUtc: '2026-07-23T10:00:00Z',
  status: 'Degraded',
  warnings: ['Temperature is unavailable.'],
}
const dockerUnavailable = {
  availability: { available: false, reason: 'Docker inventory requires Sentinel integration.' },
  collectedAtUtc: '2026-07-23T10:00:00Z',
  containers: [],
}
const systemUnavailable = {
  hostname: 'Unavailable',
  operatingSystem: 'Unavailable',
  architecture: 'Unavailable',
  uptimeSeconds: null,
  cpu: { usagePercent: null, logicalProcessorCount: 0 },
  memory: { totalBytes: null, usedBytes: null, availableBytes: null, usagePercent: null },
  disks: [],
  temperatureCelsius: null,
  collectedAtUtc: '2026-07-23T10:00:00Z',
  status: 'Unavailable',
  warnings: ['Host metrics require Sentinel integration.'],
}
const mediaServices = ['Jellyfin', 'Sonarr', 'Radarr', 'Prowlarr', 'qBittorrent'].map((serviceName) => ({
  serviceName,
  status: 'notConfigured',
  version: null,
  responseTimeMs: null,
  checkedAtUtc: '2026-07-23T10:00:00Z',
  sanitizedMessage: 'Service credentials are not configured.',
  isConfigured: false,
}))
const mediaNotConfigured = {
  status: 'notConfigured',
  healthScore: 0,
  healthSummary: 'Configure media services to calculate health.',
  healthStatusLevel: 'notConfigured',
  insights: [],
  collectedAtUtc: '2026-07-23T10:00:00Z',
  services: mediaServices,
  qBittorrent: { service: mediaServices[4], activeCount: 0, pausedCount: 0, completedCount: 0, downloadSpeedBytesPerSecond: 0, uploadSpeedBytesPerSecond: 0, etaSeconds: null, averageRatio: null, totalDownloadedBytes: 0, totalUploadedBytes: 0, freeSpaceBytes: null, torrents: [] },
  sonarr: { service: mediaServices[1], seriesCount: 0, monitoredSeriesCount: 0, missingMonitoredEpisodes: 0, queueCount: 0, queue: [], calendar: [], recentHistory: [], healthWarnings: [] },
  radarr: { service: mediaServices[2], movieCount: 0, monitoredMovieCount: 0, missingMovieCount: 0, qualityUpgradeCount: 0, queueCount: 0, queue: [], recentHistory: [], healthWarnings: [] },
  prowlarr: { service: mediaServices[3], indexerCount: 0, enabledIndexerCount: 0, onlineIndexerCount: 0, rssEnabledIndexerCount: 0, indexerStatuses: [], recentFailures: [], healthWarnings: [] },
  jellyfin: { service: mediaServices[0], libraryCount: 0, movieCount: 0, seriesCount: 0, episodeCount: 0, activeUserCount: 0, activeStreamCount: 0, recentlyAdded: [] },
}

function response(body: unknown) {
  return { ok: true, json: async () => body }
}

function successfulFetch() {
  return vi.fn((input: RequestInfo | URL) => {
    const url = String(input)
    if (url.endsWith('/api/v1/modules')) return Promise.resolve(response(modules))
    if (url.endsWith('/api/v1/system/overview')) return Promise.resolve(response(overview))
    if (url.endsWith('/api/v1/docker/containers')) return Promise.resolve(response(dockerUnavailable))
    if (url.endsWith('/api/v1/modules/media')) return Promise.resolve(response(mediaNotConfigured))
    return Promise.reject(new Error('Unexpected URL'))
  })
}

beforeEach(() => {
  window.localStorage.setItem(DASHBOARD_LAYOUT_STORAGE_KEY, JSON.stringify({
    version: 1,
    expanded: { system: true, docker: true },
  }))
  vi.stubGlobal('fetch', successfulFetch())
})
afterEach(() => {
  cleanup()
  vi.useRealTimers()
  vi.unstubAllGlobals()
})

test('renders system values returned by the API', async () => {
  render(<App />)

  expect(await screen.findByText('bigbrain-host')).toBeInTheDocument()
  expect(screen.getByText('23.5%')).toBeInTheDocument()
  expect(screen.getByText('8 logical processors')).toBeInTheDocument()
  expect(screen.getByRole('progressbar', { name: 'CPU usage' })).toHaveAttribute('value', '23.5')
  expect(screen.getByText('50.0%')).toBeInTheDocument()
  expect(screen.getByText('8.0 GiB of 16.0 GiB')).toBeInTheDocument()
  expect(screen.getByRole('progressbar', { name: 'RAM usage' })).toHaveAttribute('value', '50')
  expect(screen.getByText('System Storage')).toBeInTheDocument()
  expect(screen.getByText('Media Storage')).toBeInTheDocument()
  expect(screen.getByText('372.5 GiB used of 931.3 GiB · 558.8 GiB free')).toBeInTheDocument()
  expect(screen.getByText('465.7 GiB used of 1.8 TiB · 1.4 TiB free')).toBeInTheDocument()
  expect(screen.getByRole('progressbar', { name: 'System Storage' })).toHaveAttribute('value', '40')
  expect(screen.getByRole('progressbar', { name: 'Media Storage' })).toHaveAttribute('value', '25')
  expect(screen.getByText('3 dagar 14 timmar 22 minuter')).toBeInTheDocument()
})

test('shows loading state while requests are pending', () => {
  vi.stubGlobal('fetch', vi.fn(() => new Promise(() => undefined)))
  render(<App />)

  expect(screen.getByText('Loading system metrics…')).toBeInTheDocument()
  expect(screen.getByText('Loading Docker inventory…')).toBeInTheDocument()
})

test('shows friendly errors when APIs fail', async () => {
  vi.stubGlobal('fetch', vi.fn(() => Promise.reject(new Error('Network unavailable'))))
  render(<App />)

  expect(await screen.findByText('System metrics could not be refreshed.')).toBeInTheDocument()
  expect(await screen.findByText('Docker inventory could not be loaded.')).toBeInTheDocument()
})

test('shows Docker unavailable state from provider response', async () => {
  render(<App />)

  expect(await screen.findByRole('heading', { name: 'Integration not connected' })).toBeInTheDocument()
  expect(screen.getByText('Docker inventory requires Sentinel integration.')).toBeInTheDocument()
})

test('shows System unavailable state from provider response', async () => {
  const fetchMock = successfulFetch()
  fetchMock.mockImplementation((input: RequestInfo | URL) => {
    const url = String(input)
    if (url.endsWith('/api/v1/modules')) return Promise.resolve(response(modules))
    if (url.endsWith('/api/v1/system/overview')) return Promise.resolve(response(systemUnavailable))
    if (url.endsWith('/api/v1/docker/containers')) return Promise.resolve(response(dockerUnavailable))
    if (url.endsWith('/api/v1/modules/media')) return Promise.resolve(response(mediaNotConfigured))
    return Promise.reject(new Error('Unexpected URL'))
  })
  vi.stubGlobal('fetch', fetchMock)

  render(<App />)

  expect(await screen.findByRole('heading', { name: 'Host metrics not connected' })).toBeInTheDocument()
  expect(screen.getByText('Host metrics require Sentinel integration.')).toBeInTheDocument()
  expect(screen.getAllByText('Unavailable', { selector: '.metric-value' }).length).toBeGreaterThan(0)
})

test('polls system overview without discarding the latest successful data', async () => {
  vi.useFakeTimers()
  const fetchMock = successfulFetch()
  vi.stubGlobal('fetch', fetchMock)
  render(<App />)

  await act(async () => { await Promise.resolve(); await Promise.resolve() })
  expect(screen.getByText('bigbrain-host')).toBeInTheDocument()

  fetchMock.mockImplementation((input: RequestInfo | URL) =>
    String(input).endsWith('/api/v1/system/overview')
      ? Promise.reject(new Error('Temporary failure'))
      : Promise.resolve(response(dockerUnavailable)),
  )
  await act(async () => { await vi.advanceTimersByTimeAsync(5_000) })

  const systemCalls = fetchMock.mock.calls.filter(([url]) => String(url).endsWith('/api/v1/system/overview'))
  expect(systemCalls).toHaveLength(2)
  expect(screen.getByText(/Showing the latest successful update/)).toBeInTheDocument()
  expect(screen.getByText('bigbrain-host')).toBeInTheDocument()
})

test('prioritizes search and quick actions before system and Docker modules', async () => {
  const { container } = render(<App />)
  await screen.findByRole('heading', { name: 'Hitta film och serier' })

  expect([...container.querySelectorAll('[data-dashboard-module]')].map(element => element.getAttribute('data-dashboard-module')))
    .toEqual([
      'media-search',
      'quick-actions',
      'media-jobs',
      'media-health',
      'insights',
      'services',
      'system',
      'docker',
      'activity',
      'details',
    ])
})

test('uses collapsed defaults for low-priority infrastructure modules', async () => {
  window.localStorage.clear()
  render(<App />)
  await screen.findByRole('heading', { name: 'System status' })

  const systemToggle = screen.getByRole('button', { name: 'Expandera System status' })
  const dockerToggle = screen.getByRole('button', { name: 'Expandera Docker overview' })
  expect(systemToggle).toHaveAttribute('aria-expanded', 'false')
  expect(systemToggle).toHaveAttribute('aria-controls', 'system-content')
  expect(dockerToggle).toHaveAttribute('aria-expanded', 'false')

  fireEvent.click(systemToggle)
  expect(screen.getByRole('button', { name: 'Minimera System status' })).toHaveAttribute('aria-expanded', 'true')
  expect(screen.getByRole('progressbar', { name: 'CPU usage' })).toBeInTheDocument()
})
