import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import { AppShell } from '../AppShell'
import { createAppWidgetRegistry } from '../dashboard/appWidgets'
import {
  WidgetProvider,
  readDashboardPreferences,
  DASHBOARD_PREFERENCES_STORAGE_KEY,
} from '../dashboard/widgetFramework'
import { mediaControlPage } from './mediaNavigation'

const registry = createAppWidgetRegistry({
  docker: null,
  dockerError: false,
  modules: [],
  moduleError: false,
  recovery: null,
  recoveryError: false,
  system: null,
  systemError: false,
})
const reads: Array<{ url: string; signal?: AbortSignal }> = []
function mount() {
  return render(
    <WidgetProvider registry={registry}>
      <AppShell />
    </WidgetProvider>,
  )
}
function go(path: string) {
  window.history.replaceState({}, '', path)
}
beforeEach(() => {
  localStorage.clear()
  reads.length = 0
  go('/media/control')
  vi.stubGlobal(
    'fetch',
    vi.fn((url: string, init?: RequestInit) => {
      reads.push({ url, signal: init?.signal ?? undefined })
      // Pending requests exercise cancellation without real providers or unrelated fixture data.
      return new Promise<Response>(() => {})
    }),
  )
})
afterEach(() => {
  cleanup()
  vi.useRealTimers()
  vi.unstubAllGlobals()
  localStorage.clear()
  go('/')
})

test('overview has exactly three ordered links and performs no reads or polling', async () => {
  vi.useFakeTimers()
  mount()
  const links = within(screen.getByRole('navigation', { name: 'Mediekontroll' })).getAllByRole('link')
  expect(links.map(a => a.querySelector('strong')?.textContent)).toEqual([
    'Nedladdningskö',
    'Medieflöde',
    'Tekniska integrationer',
  ])
  expect(links.map(a => a.getAttribute('href'))).toEqual([
    '/media/control/downloads',
    '/media/control/flow',
    '/media/control/integrations',
  ])
  await act(async () => {
    await vi.advanceTimersByTimeAsync(90_000)
  })
  expect(reads).toEqual([])
  expect(screen.getByRole('heading', { name: 'Mediekontroll' })).toHaveFocus()
})

test.each([
  ['Nedladdningskö', 'downloads', ['/api/v1/modules/media/downloads']],
  ['Medieflöde', 'flow', ['/api/v1/modules/media/jobs?limit=50']],
  ['Tekniska integrationer', 'integrations', ['/api/v1/modules/media', '/api/v1/modules/media/service-links']],
])('%s mounts only its function; leaving aborts requests and stops polling', async (name, path, expected) => {
  vi.useFakeTimers()
  mount()
  fireEvent.click(screen.getByRole('link', { name: new RegExp(name) }))
  expect(window.location.pathname).toBe(`/media/control/${path}`)
  expect(screen.getByRole('heading', { level: 1, name })).toHaveFocus()
  expect(reads.map(r => r.url).sort()).toEqual([...expected].sort())
  fireEvent.click(screen.getByRole('link', { name: '‹ Till Mediekontroll' }))
  expect(reads.every(r => r.signal?.aborted)).toBe(true)
  const count = reads.length
  await act(async () => {
    await vi.advanceTimersByTimeAsync(90_000)
  })
  expect(reads).toHaveLength(count)
  expect(screen.getByRole('heading', { name: 'Mediekontroll' })).toBeInTheDocument()
})

test('actual browser back/forward and remount preserve the selected function', async () => {
  let view = mount()
  fireEvent.click(screen.getByRole('link', { name: /Medieflöde/ }))
  await act(async () => {
    window.history.back()
  })
  await waitFor(() => expect(screen.getByRole('heading', { level: 1, name: 'Mediekontroll' })).toBeInTheDocument())
  await act(async () => {
    window.history.forward()
  })
  await waitFor(() => expect(screen.getByRole('heading', { level: 1, name: 'Medieflöde' })).toBeInTheDocument())
  view.unmount()
  view = mount()
  expect(screen.getByRole('heading', { level: 1, name: 'Medieflöde' })).toBeInTheDocument()
  view.unmount()
})

test.each(['/media/control/downloads', '/#downloads', '/media#queue', '/#administration', '/#jellyfin-overview'])(
  'direct bookmark %s overrides stored non-Media view',
  path => {
    go(path)
    localStorage.setItem(
      DASHBOARD_PREFERENCES_STORAGE_KEY,
      JSON.stringify({ version: 2, activeView: 'family', views: {} }),
    )
    mount()
    expect(screen.queryByRole('heading', { level: 1, name: 'Familj' })).not.toBeInTheDocument()
    expect(mediaControlPage()).not.toBeNull()
    expect(reads.length).toBeGreaterThan(0)
  },
)

test.each(['Primär navigation', 'Snabbnavigation'])('%s can leave the route and history returns to it', async name => {
  go('/media/control/flow')
  mount()
  fireEvent.click(within(screen.getByRole('navigation', { name })).getByRole('button', { name: 'Media' }))
  expect(window.location.pathname).toBe('/media')
  expect(screen.getByRole('button', { name: 'Film' })).toBeInTheDocument()
  expect(screen.queryByRole('heading', { level: 1, name: 'Medieflöde' })).not.toBeInTheDocument()
  await act(async () => {
    window.history.back()
  })
  await waitFor(() => expect(screen.getByRole('heading', { level: 1, name: 'Medieflöde' })).toBeInTheDocument())
})

test('legacy widget preferences consolidate once without changing unrelated choices', () => {
  const stored = {
    version: 2,
    activeView: 'media',
    views: {
      media: {
        order: ['media-search', 'downloads', 'smart-shuffle', 'media-jobs', 'jellyfin-overview'],
        hidden: ['downloads', 'media-jobs', 'jellyfin-overview'],
        collapsed: ['media-search', 'downloads'],
      },
      admin: { order: ['jellyfin-overview'], hidden: [], collapsed: [] },
    },
  }
  const result = readDashboardPreferences(registry, { getItem: () => JSON.stringify(stored) })
  expect(result.views.media).toEqual({
    order: ['media-search', 'media-control', 'smart-shuffle'],
    hidden: ['media-control'],
    collapsed: ['media-search'],
  })
  expect(result.views.admin?.order).toContain('media-control')
  expect(result.views.admin?.hidden).not.toContain('media-control')
  expect(readDashboardPreferences(registry, { getItem: () => JSON.stringify(result) })).toEqual(result)
  expect(registry.getForView('media').map(x => x.id)).toEqual(['media-search', 'media-control', 'smart-shuffle'])
})

test('resolved job polling runs only on its selected page', async () => {
  vi.useFakeTimers()
  const fetchMock = vi.fn(async () => ({ ok: true, json: async () => ({ jobs: [], providers: [] }) }))
  vi.stubGlobal('fetch', fetchMock)
  mount()
  fireEvent.click(screen.getByRole('link', { name: /Medieflöde/ }))
  await act(async () => {
    await vi.advanceTimersByTimeAsync(12_000)
  })
  expect(fetchMock).toHaveBeenCalledTimes(2)
  fireEvent.click(screen.getByRole('link', { name: '‹ Till Mediekontroll' }))
  await act(async () => {
    await vi.advanceTimersByTimeAsync(90_000)
  })
  expect(fetchMock).toHaveBeenCalledTimes(2)
})

test('download error stays on the selected page and overview remains usable', async () => {
  vi.stubGlobal(
    'fetch',
    vi.fn(async () => {
      throw new Error('fixture unavailable')
    }),
  )
  mount()
  fireEvent.click(screen.getByRole('link', { name: /Nedladdningskö/ }))
  expect(await screen.findByText('Åtgärden kunde inte slutföras.')).toBeInTheDocument()
  fireEvent.click(screen.getByRole('link', { name: '‹ Till Mediekontroll' }))
  expect(screen.queryByText('Åtgärden kunde inte slutföras.')).not.toBeInTheDocument()
  expect(within(screen.getByRole('navigation', { name: 'Mediekontroll' })).getAllByRole('link')).toHaveLength(3)
})
