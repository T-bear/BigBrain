import { act, cleanup, fireEvent, render, screen, within } from '@testing-library/react'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import App from './App'
import { DASHBOARD_PREFERENCES_STORAGE_KEY } from './dashboard/widgetFramework'

const modules = [
  { id: 'media', name: 'Media', description: '', route: '/#media', status: 'NotConfigured', dashboardWidgets: [], capabilities: [] },
  { id: 'system', name: 'System', description: '', route: '/', status: 'Available', dashboardWidgets: [], capabilities: [] },
]
const overview = {
  hostname: 'bigbrain-host', operatingSystem: 'Linux', architecture: 'X64', uptimeSeconds: 310_920,
  cpu: { usagePercent: 23.5, logicalProcessorCount: 8 },
  memory: { totalBytes: 17_179_869_184, usedBytes: 8_589_934_592, availableBytes: 8_589_934_592, usagePercent: 50 },
  disks: [{ filesystemId: 'system', displayName: 'System Storage', totalBytes: 1_000_000_000_000, usedBytes: 400_000_000_000, availableBytes: 600_000_000_000, usagePercent: 40 }],
  temperatureCelsius: null, collectedAtUtc: '2026-07-23T10:00:00Z', status: 'Degraded', warnings: ['Temperature is unavailable.'],
}
const dockerUnavailable = { availability: { available: false, reason: 'Docker inventory requires Sentinel integration.' }, collectedAtUtc: '2026-07-23T10:00:00Z', containers: [] }
const recovery = { overall: 'healthy', bootId: '12345678-abcd', bootedAtUtc: '2026-08-12T10:00:00Z', previousShutdown: 'clean', recoveryCompleted: true, clockSynchronized: true, clockSource: 'systemd-timesync-marker', availableBytes: 100_000_000_000, lowDisk: false, lastCleanShutdownUtc: '2026-08-12T09:59:00Z', lastIntegrityCheckUtc: '2026-08-12T10:00:01Z', interruptedJobs: 0, operatingMode: 'RESEARCH', components: [{ id: 'finance-memory', state: 'healthy', critical: false, summary: 'Fast open/write check passed.', checkedAtUtc: '2026-08-12T10:00:01Z' }], recoveryActions: [], scheduledJobs: [] }

function response(body: unknown) { return { ok: true, json: async () => body } }
function successfulFetch() {
  return vi.fn((input: RequestInfo | URL) => {
    const url = String(input)
    if (url.endsWith('/api/v1/modules')) return Promise.resolve(response(modules))
    if (url.endsWith('/api/v1/system/overview')) return Promise.resolve(response(overview))
    if (url.endsWith('/api/v1/docker/containers')) return Promise.resolve(response(dockerUnavailable))
    if (url.endsWith('/api/v1/system/recovery')) return Promise.resolve(response(recovery))
    if (url.endsWith('/api/v1/modules/meal-planner/schedules')) return Promise.resolve(response([{ id: 'week', startDate: '2026-08-17', endDate: '2026-08-23', createdAtUtc: '', updatedAtUtc: '', title: null, generationVersion: 1, days: [{ date: new Date().toLocaleDateString('sv-SE'), mealType: 'dinner', dayOfWeek: 'Sunday', peopleCount: 4, mealId: 'meal', mealName: 'Pappas soppa', tagSummary: [], isManuallyReplaced: false }] }]))
    if (url.endsWith('/api/v1/modules/calendar/week')) return Promise.resolve(response({ from: '2026-08-17', to: '2026-08-23', events: [] }))
    if (url.endsWith('/api/v1/modules/shopping-list/items')) return Promise.resolve(response({ sessionId: 'active', items: [{ id: 'item', name: 'Mjölk', normalizedName: 'mjölk', quantity: 1, purchased: false, createdAtUtc: '', updatedAtUtc: '', sortOrdinal: 1 }] }))
    if (url.endsWith('/api/v1/modules/media')) return Promise.resolve(response({ healthSummary: 'Allt lugnt', insights: [], qBittorrent: { activeCount: 0 } }))
    if (url.endsWith('/api/v1/modules/finance/overview')) return Promise.resolve(response({ marketSummary: 'Marknaden är blandad', signals: [], prospective: { curve: [] } }))
    return Promise.reject(new Error('Unexpected URL'))
  })
}
function switchView(name: string) {
  const primary = screen.getByRole('navigation', { name: 'Primär navigation' })
  const direct = within(primary).queryByRole('button', { name: new RegExp(name) })
  if (direct) fireEvent.click(direct)
  else {
    fireEvent.click(within(primary).getByRole('button', { name: 'Mer' }))
    fireEvent.click(screen.getByRole('button', { name: new RegExp(name) }))
  }
}

beforeEach(() => { window.localStorage.clear(); vi.stubGlobal('fetch', successfulFetch()) })
afterEach(() => { cleanup(); vi.useRealTimers(); vi.unstubAllGlobals() })

test('starts on the calm Home launcher and keeps family tools in Family', async () => {
  const { container } = render(<App />)
  expect(screen.getByRole('heading', { level: 1, name: 'Hem' })).toBeInTheDocument()
  expect([...container.querySelectorAll('[data-widget-id]')].map(element => element.getAttribute('data-widget-id'))).toEqual(['home-launcher'])
  switchView('Familj')
  expect([...container.querySelectorAll('[data-widget-id]')].map(element => element.getAttribute('data-widget-id'))).toEqual(['meal-plan', 'shopping-list', 'calendar', 'reminders'])
  expect(container.querySelector('#family')).toHaveClass('family-experience')
  expect(container.querySelector('#family .dashboard-widget')).not.toBeInTheDocument()
  expect(screen.getByRole('heading', { level: 1, name: 'Familj' })).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Dashboardinställningar' })).toHaveAttribute('aria-haspopup', 'dialog')
  expect(await screen.findByRole('tab', { name: 'Matsedel' })).toHaveAttribute('aria-selected', 'true')
  expect(container.querySelector('[data-family-section="shopping-list"]')).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Öppna kalender' })).toBeInTheDocument()
  expect(screen.getByRole('navigation', { name: 'Snabbnavigation' })).toBeInTheDocument()
})

test('Home presents real glanceable data before contextual navigation', async () => {
  render(<App />)
  expect(await screen.findByText('Pappas soppa')).toBeInTheDocument()
  expect(screen.getByText('1 vara kvar att handla')).toBeInTheDocument()
  expect(screen.getByText('Allt lugnt')).toBeInTheDocument()
  expect(screen.getByText('Marknaden är blandad')).toBeInTheDocument()
  fireEvent.click(screen.getByRole('button', { name: /Familj.*1 vara kvar/ }))
  expect(screen.getByRole('heading', { level: 1, name: 'Familj' })).toBeInTheDocument()
})

test('switches dashboard without reload and remembers the active view', () => {
  render(<App />)
  switchView('Media')
  expect(screen.getByRole('heading', { level: 1, name: 'Media' })).toBeInTheDocument()
  expect(screen.getByRole('heading', { name: 'Mediesökning' })).toBeInTheDocument()
  expect(JSON.parse(window.localStorage.getItem(DASHBOARD_PREFERENCES_STORAGE_KEY) ?? '{}')).toMatchObject({ activeView: 'media', version: 2 })
})

test('Media keeps technical integrations progressively disclosed', () => {
  const { container } = render(<App />)
  switchView('Media')
  const administration = container.querySelector('[data-widget-id="jellyfin-overview"] details.administration')
  expect(administration).toBeInTheDocument()
  expect(administration).not.toHaveAttribute('open')
})

test('restores the last selected dashboard and falls back from invalid storage', () => {
  window.localStorage.setItem(DASHBOARD_PREFERENCES_STORAGE_KEY, JSON.stringify({ version: 2, activeView: 'ai', views: {} }))
  const { unmount } = render(<App />)
  expect(screen.getByRole('heading', { level: 1, name: 'BigBrain AI' })).toBeInTheDocument()
  unmount()
  window.localStorage.setItem(DASHBOARD_PREFERENCES_STORAGE_KEY, '{invalid')
  render(<App />)
  expect(screen.getByRole('heading', { level: 1, name: 'Hem' })).toBeInTheDocument()
})

test('widget library hides a widget without deleting data and persists visibility', () => {
  render(<App />)
  switchView('Familj')
  fireEvent.click(screen.getByRole('button', { name: 'Dashboardinställningar' }))
  fireEvent.click(screen.getByRole('button', { name: 'Öppna widgetbibliotek' }))
  const dialog = screen.getByRole('dialog', { name: 'Visa widgets' })
  fireEvent.click(within(dialog).getByRole('checkbox', { name: /Kalender/ }))
  fireEvent.click(within(dialog).getByRole('button', { name: 'Klar' }))
  expect(screen.queryByRole('heading', { name: 'Kalender' })).not.toBeInTheDocument()
  expect(JSON.parse(window.localStorage.getItem(DASHBOARD_PREFERENCES_STORAGE_KEY) ?? '{}').views.family.hidden).toContain('calendar')
})

test('edit mode reorders widgets and collapsed state is persisted', () => {
  const { container } = render(<App />)
  switchView('Familj')
  fireEvent.click(screen.getByRole('button', { name: 'Dashboardinställningar' }))
  fireEvent.click(screen.getByRole('button', { name: 'Aktivera redigeringsläge' }))
  fireEvent.click(screen.getByRole('button', { name: 'Flytta Inköpslista upp' }))
  expect([...container.querySelectorAll('[data-widget-id]')].map(element => element.getAttribute('data-widget-id')).slice(0, 2)).toEqual(['shopping-list', 'meal-plan'])
  const shoppingWidget = container.querySelector('[data-widget-id="shopping-list"]') as HTMLElement
  fireEvent.click(within(shoppingWidget).getAllByRole('button', { name: 'Minimera Inköpslista' })[0])
  const stored = JSON.parse(window.localStorage.getItem(DASHBOARD_PREFERENCES_STORAGE_KEY) ?? '{}')
  expect(stored.views.family.order.slice(0, 2)).toEqual(['shopping-list', 'meal-plan'])
  expect(stored.views.family.collapsed).toContain('shopping-list')
})

test('renders system values and safe Docker state only in Admin', async () => {
  render(<App />)
  switchView('Admin')
  expect(await screen.findByText('bigbrain-host')).toBeInTheDocument()
  expect(screen.getByRole('progressbar', { name: 'CPU usage' })).toHaveAttribute('value', '23.5')
  expect(screen.getByText('Docker inventory requires Sentinel integration.')).toBeInTheDocument()
  expect(screen.getByText('CLEAN')).toBeInTheDocument()
  expect(screen.getByText('Synkroniserad')).toBeInTheDocument()
})

test('dashboard settings groups theme, editing and widget library with keyboard dismissal', () => {
  render(<App />)
  const trigger = screen.getByRole('button', { name: 'Dashboardinställningar' })
  fireEvent.click(trigger)
  const settings = screen.getByRole('dialog', { name: 'Dashboardinställningar' })
  expect(within(settings).getByRole('group', { name: 'Tema' })).toBeInTheDocument()
  expect(within(settings).getByRole('button', { name: 'Aktivera redigeringsläge' })).toBeInTheDocument()
  expect(within(settings).getByRole('button', { name: 'Öppna widgetbibliotek' })).toBeInTheDocument()
  fireEvent.keyDown(document, { key: 'Escape' })
  expect(screen.queryByRole('dialog', { name: 'Dashboardinställningar' })).not.toBeInTheDocument()
})

test('shows loading and safe errors in Admin', async () => {
  vi.stubGlobal('fetch', vi.fn(() => Promise.reject(new Error('Network unavailable'))))
  render(<App />)
  switchView('Admin')
  expect(await screen.findByText('System metrics could not be refreshed.')).toBeInTheDocument()
  expect(screen.getByText('Docker inventory could not be loaded.')).toBeInTheDocument()
})

test('polls system overview without overlapping the dashboard navigation state', async () => {
  vi.useFakeTimers()
  const fetchMock = successfulFetch()
  vi.stubGlobal('fetch', fetchMock)
  render(<App />)
  switchView('Admin')
  await act(async () => { await Promise.resolve(); await Promise.resolve() })
  expect(screen.getByText('bigbrain-host')).toBeInTheDocument()
  await act(async () => { await vi.advanceTimersByTimeAsync(5_000) })
  expect(fetchMock.mock.calls.filter(([url]) => String(url).endsWith('/api/v1/system/overview'))).toHaveLength(2)
  expect(screen.getByRole('heading', { level: 1, name: 'Admin' })).toBeInTheDocument()
})
