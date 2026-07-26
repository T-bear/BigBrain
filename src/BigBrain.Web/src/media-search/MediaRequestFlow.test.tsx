import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, expect, test, vi } from 'vitest'
import { MediaSearch } from './MediaSearch'

const lookup = {
  query: 'The Expanse',
  mediaType: 'all',
  lookedUpAtUtc: '2026-07-25T10:00:00Z',
  status: 'complete',
  requestsEnabled: true,
  providers: [
    {
      provider: 'Sonarr',
      status: 'online',
      error: null,
      results: [{
        provider: 'Sonarr',
        foreignId: '280619',
        title: 'The Expanse',
        originalTitle: null,
        year: 2015,
        overview: 'Humanity has colonized the solar system.',
        network: 'Syfy',
        runtimeMinutes: 45,
        status: 'ended',
        mediaType: 'series',
        lookupState: 'external',
        imageAvailable: true,
        alreadyRegistered: false,
        existingSourceId: null,
      }],
    },
    {
      provider: 'Radarr',
      status: 'online',
      error: null,
      results: [{
        provider: 'Radarr',
        foreignId: '99',
        title: 'Existing Movie',
        originalTitle: null,
        year: 2020,
        overview: null,
        network: null,
        runtimeMinutes: 100,
        status: 'released',
        mediaType: 'movie',
        lookupState: 'alreadyRegistered',
        imageAvailable: false,
        alreadyRegistered: true,
        existingSourceId: '8',
      }],
    },
  ],
}

const options = {
  provider: 'Sonarr',
  mediaType: 'series',
  requestsEnabled: true,
  rootFolders: [{ id: 'opaque-root', displayName: 'TV Library', freeSpaceBytes: 1000 }],
  qualityProfiles: [{ id: 'opaque-quality', displayName: 'HD 1080p', freeSpaceBytes: null }],
  monitoringOptions: [{ id: 'all', displayName: 'all', freeSpaceBytes: null }],
  seriesTypes: [{ id: 'standard', displayName: 'standard', freeSpaceBytes: null }],
  defaultRootFolderId: 'opaque-root',
  defaultQualityProfileId: 'opaque-quality',
  defaultMonitoringOptionId: 'all',
  defaultSeriesTypeId: 'standard',
  defaultSearchAfterAdd: false,
}

function ok(body: unknown) {
  return Promise.resolve({ ok: true, json: async () => body })
}

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

async function openExternalResult(fetch: ReturnType<typeof vi.fn>) {
  vi.stubGlobal('fetch', fetch)
  render(<MediaSearch />)
  fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'The Expanse' } })
  fireEvent.click(screen.getByRole('button', { name: 'Sök' }))
  return screen.findByRole('button', { name: 'Lägg till serie' })
}

test('switches to grouped external lookup and blocks already registered results', async () => {
  const fetch = vi.fn(() => ok(lookup))
  await openExternalResult(fetch)

  expect(screen.getByRole('heading', { name: 'Sonarr' })).toBeInTheDocument()
  expect(screen.getByRole('heading', { name: 'Radarr' })).toBeInTheDocument()
  expect(screen.getByText('Redan tillagd')).toBeInTheDocument()
  expect(screen.queryByRole('button', { name: 'Lägg till film' })).not.toBeInTheDocument()
})

test('requires options and preview review before one explicit confirm', async () => {
  let confirmResolve: ((value: unknown) => void) | undefined
  const fetch = vi.fn((url: string, init?: RequestInit) => {
    if (url.includes('/lookup')) return ok(lookup)
    if (url.includes('/add-options/')) return ok(options)
    if (url.includes('/preview')) return ok({
      requestToken: 'opaque-token',
      expiresAtUtc: '2026-07-25T10:05:00Z',
      status: 'previewReady',
      summary: {
        title: 'The Expanse', year: 2015, provider: 'Sonarr', mediaType: 'series',
        rootFolder: 'TV Library', qualityProfile: 'HD 1080p', monitoring: 'All',
        seriesType: 'Standard', searchAfterAdd: false,
      },
    })
    if (url.includes('/confirm')) {
      expect(init?.method).toBe('POST')
      return new Promise(resolve => { confirmResolve = resolve })
    }
    throw new Error(`Unexpected request: ${url}`)
  })
  const add = await openExternalResult(fetch)
  fireEvent.click(add)

  const dialog = await screen.findByRole('dialog')
  await waitFor(() => expect(within(dialog).getByLabelText('Root folder')).toHaveFocus())
  expect(within(dialog).getByText('TV Library')).toBeInTheDocument()
  expect(within(dialog).queryByText('/srv/')).not.toBeInTheDocument()
  expect(fetch).not.toHaveBeenCalledWith(expect.stringContaining('/confirm'), expect.anything())

  fireEvent.click(within(dialog).getByRole('button', { name: 'Review addition' }))
  expect(await within(dialog).findByRole('heading', { name: 'Review before adding' })).toBeInTheDocument()
  expect(within(dialog).getByText('HD 1080p')).toBeInTheDocument()
  const confirm = within(dialog).getByRole('button', { name: 'Add series to Sonarr' })
  fireEvent.click(confirm)
  fireEvent.click(confirm)
  await waitFor(() => expect(confirm).toBeDisabled())
  expect(fetch.mock.calls.filter(([url]) => String(url).includes('/confirm'))).toHaveLength(1)

  confirmResolve?.({ ok: true, json: async () => ({
    status: 'created', provider: 'Sonarr', mediaType: 'series', sourceId: '9', title: 'The Expanse',
  }) })
  expect(await within(dialog).findByText('The Expanse was added to Sonarr.')).toBeInTheDocument()
})

test('Escape closes before review and returns focus while raw errors stay hidden', async () => {
  const fetch = vi.fn((url: string) => {
    if (url.includes('/lookup')) return ok(lookup)
    if (url.includes('/add-options/')) return Promise.reject(new Error('raw upstream /srv/private'))
    throw new Error('unexpected')
  })
  const add = await openExternalResult(fetch)
  fireEvent.click(add)
  const dialog = await screen.findByRole('dialog')

  expect(await within(dialog).findByRole('alert')).toHaveTextContent('The media request could not be completed.')
  expect(within(dialog).queryByText(/raw upstream|\/srv\/private/)).not.toBeInTheDocument()
  fireEvent.keyDown(dialog, { key: 'Escape' })
  await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
  expect(add).toHaveFocus()
  expect(screen.queryByRole('button', { name: /delete|rename|move|pause|resume/i })).not.toBeInTheDocument()
})
