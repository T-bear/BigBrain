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

function response(body: unknown) { return { ok: true, json: async () => body } }
function successfulFetch() {
  return vi.fn((input: RequestInfo | URL) => {
    const url = String(input)
    if (url.endsWith('/api/v1/modules')) return Promise.resolve(response(modules))
    if (url.endsWith('/api/v1/system/overview')) return Promise.resolve(response(overview))
    if (url.endsWith('/api/v1/docker/containers')) return Promise.resolve(response(dockerUnavailable))
    return Promise.reject(new Error('Unexpected URL'))
  })
}
function switchView(name: string) {
  fireEvent.click(within(screen.getByRole('navigation', { name: 'Dashboardvyer' })).getByRole('button', { name: new RegExp(name) }))
}

beforeEach(() => { window.localStorage.clear(); vi.stubGlobal('fetch', successfulFetch()) })
afterEach(() => { cleanup(); vi.useRealTimers(); vi.unstubAllGlobals() })

test('starts on Home with the family-priority widgets in registry order', () => {
  const { container } = render(<App />)
  expect(screen.getByRole('heading', { level: 1, name: 'Hem' })).toBeInTheDocument()
  expect([...container.querySelectorAll('[data-widget-id]')].map(element => element.getAttribute('data-widget-id'))).toEqual(['meal-plan', 'shopping-list', 'calendar', 'reminders'])
  expect(screen.getByRole('navigation', { name: 'Snabbnavigation' })).toBeInTheDocument()
})

test('switches dashboard without reload and remembers the active view', () => {
  render(<App />)
  switchView('Media')
  expect(screen.getByRole('heading', { level: 1, name: 'Media' })).toBeInTheDocument()
  expect(screen.getByRole('heading', { name: 'Mediesökning' })).toBeInTheDocument()
  expect(JSON.parse(window.localStorage.getItem(DASHBOARD_PREFERENCES_STORAGE_KEY) ?? '{}')).toMatchObject({ activeView: 'media', version: 2 })
})

test('restores the last selected dashboard and falls back from invalid storage', () => {
  window.localStorage.setItem(DASHBOARD_PREFERENCES_STORAGE_KEY, JSON.stringify({ version: 2, activeView: 'ai', views: {} }))
  const { unmount } = render(<App />)
  expect(screen.getByRole('heading', { level: 1, name: 'AI' })).toBeInTheDocument()
  unmount()
  window.localStorage.setItem(DASHBOARD_PREFERENCES_STORAGE_KEY, '{invalid')
  render(<App />)
  expect(screen.getByRole('heading', { level: 1, name: 'Hem' })).toBeInTheDocument()
})

test('widget library hides a widget without deleting data and persists visibility', () => {
  render(<App />)
  fireEvent.click(screen.getByRole('button', { name: 'Widgetbibliotek' }))
  const dialog = screen.getByRole('dialog', { name: 'Visa widgets' })
  fireEvent.click(within(dialog).getByRole('checkbox', { name: /Kalender/ }))
  fireEvent.click(within(dialog).getByRole('button', { name: 'Klar' }))
  expect(screen.queryByRole('heading', { name: 'Kalender' })).not.toBeInTheDocument()
  expect(JSON.parse(window.localStorage.getItem(DASHBOARD_PREFERENCES_STORAGE_KEY) ?? '{}').views.home.hidden).toContain('calendar')
})

test('edit mode reorders widgets and collapsed state is persisted', () => {
  const { container } = render(<App />)
  fireEvent.click(screen.getByRole('button', { name: 'Redigera' }))
  fireEvent.click(screen.getByRole('button', { name: 'Flytta Inköpslista upp' }))
  expect([...container.querySelectorAll('[data-widget-id]')].map(element => element.getAttribute('data-widget-id')).slice(0, 2)).toEqual(['shopping-list', 'meal-plan'])
  const shoppingWidget = container.querySelector('[data-widget-id="shopping-list"]') as HTMLElement
  fireEvent.click(within(shoppingWidget).getAllByRole('button', { name: 'Minimera Inköpslista' })[0])
  const stored = JSON.parse(window.localStorage.getItem(DASHBOARD_PREFERENCES_STORAGE_KEY) ?? '{}')
  expect(stored.views.home.order.slice(0, 2)).toEqual(['shopping-list', 'meal-plan'])
  expect(stored.views.home.collapsed).toContain('shopping-list')
})

test('renders system values and safe Docker state only in Admin', async () => {
  render(<App />)
  switchView('Admin')
  expect(await screen.findByText('bigbrain-host')).toBeInTheDocument()
  expect(screen.getByRole('progressbar', { name: 'CPU usage' })).toHaveAttribute('value', '23.5')
  expect(screen.getByText('Docker inventory requires Sentinel integration.')).toBeInTheDocument()
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
