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
    collectedAtUtc: checkedAtUtc,
    services,
    qBittorrent: {
      service: services[4],
      activeCount: 1,
      pausedCount: 0,
      completedCount: 2,
      downloadSpeedBytesPerSecond: 2_097_152,
      uploadSpeedBytesPerSecond: 1024,
      torrents: [{ name: 'Safe download', progressPercent: 42.5, state: 'downloading', category: 'tv', etaSeconds: 3600 }],
    },
    sonarr: { service: services[1], queueCount: 0, queue: [], healthWarnings: [] },
    radarr: { service: services[2], queueCount: 0, queue: [], healthWarnings: [] },
    prowlarr: { service: services[3], healthWarnings: [] },
    jellyfin: { service: services[0], libraryCount: 2, movieCount: 10, seriesCount: 5, activeSessionCount: 1 },
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

  expect(screen.getByText('Loading media services…')).toBeInTheDocument()
  resolveRequest?.(response(overview()))

  expect(await screen.findByText('Safe download')).toBeInTheDocument()
  expect(screen.getByText('42.5% · downloading · 1h 0m remaining')).toBeInTheDocument()
  expect(screen.getAllByText('Queue is empty.')).toHaveLength(2)
  expect(screen.queryByRole('button', { name: /pause|resume|delete|add|search/i })).not.toBeInTheDocument()
})

test('shows partial success with degraded and unavailable services', async () => {
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(response(overview('degraded', {
    Jellyfin: 'online',
    Sonarr: 'unavailable',
    Radarr: 'degraded',
  })))))
  render(<MediaDashboard />)

  expect(await screen.findByText('Some media services are unavailable. Available data is still shown.')).toBeInTheDocument()
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
  expect(screen.getByRole('heading', { name: 'Active downloads' })).toBeInTheDocument()
  expect(screen.getAllByText('Queue is empty.')).toHaveLength(2)
  expect(screen.getByRole('button', { name: 'Refresh' })).toBeEnabled()
})

test('shows total API failure', async () => {
  vi.stubGlobal('fetch', vi.fn(() => Promise.reject(new Error('network failure'))))
  render(<MediaDashboard />)

  expect(await screen.findByRole('alert')).toHaveTextContent('Media overview could not be loaded.')
})
