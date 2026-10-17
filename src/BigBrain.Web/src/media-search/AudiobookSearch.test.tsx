import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, expect, test, vi } from 'vitest'
import { MediaSearch } from './MediaSearch'
import type { AudiobookItem, AudiobookAcquisitionCandidate, AudiobookSearchResponse } from '../types'

const item: AudiobookItem = {
  id: 'catalog-book',
  title: 'Biblioteksboken',
  author: 'Författare',
  narrator: 'Uppläsare',
  language: 'sv',
  languageLabel: 'Svenska',
  series: null,
  durationSeconds: 500,
  progressPercent: 42,
  description: null,
  coverUrl: null,
  publishedYear: '2020',
  isAbridged: false,
  playbackUrl: 'https://books.example.test/item/catalog-book',
}
const candidate: AudiobookAcquisitionCandidate = {
  workId: 'work',
  editionId: 'opaque-edition',
  title: 'Ny utgåva',
  author: 'Författare',
  narrator: 'Annan uppläsare',
  language: 'en',
  languageLabel: 'Engelska',
  edition: 'Oavkortad',
  durationSeconds: null,
  publicationYear: 2021,
  coverUrl: null,
  source: 'Librarr',
  availability: 'available',
  languageConfidence: 'probable',
}
const results: AudiobookSearchResponse = {
  library: [item],
  discovery: [candidate],
  metadata: {
    query: { original: 'Boken', normalized: 'boken', kind: 'freeText' },
    state: 'notFound',
    works: [],
    narratorSearchSupported: false,
    message: null,
  },
  acquisition: {
    state: 'configuredHealthy',
    provider: 'Librarr',
    canSearch: true,
    canRequest: true,
    canCancel: false,
    message: null,
  },
}
const ok = (value: unknown) => ({ ok: true, json: async () => value })
function start() {
  render(<MediaSearch />)
  expect(screen.getByRole('button', { name: 'Film' })).toHaveAttribute('aria-pressed', 'true')
  expect(screen.queryByRole('button', { name: 'Båda' })).toBeNull()
  fireEvent.click(screen.getByRole('button', { name: 'Ljudbok' }))
  fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'Boken' } })
}
afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
  vi.restoreAllMocks()
})

test('discovery uses existing API, preserves opaque edition and requires explicit confirmation', async () => {
  const fetch = vi.fn((url: string, init?: RequestInit) => {
    if (url.includes('/search?')) return Promise.resolve(ok(results))
    expect(url).toBe('/api/v1/modules/media/audiobooks/acquisition/jobs')
    expect(init?.method).toBe('POST')
    expect(JSON.parse(String(init?.body))).toEqual(candidate)
    return Promise.resolve(ok({ id: 'job', candidate, status: 'requested' }))
  })
  vi.stubGlobal('fetch', fetch)
  start()
  fireEvent.click(screen.getByRole('button', { name: 'Sök' }))
  expect(await screen.findByRole('link', { name: 'Öppna i Audiobookshelf' })).toHaveAttribute('href', item.playbackUrl)
  expect(fetch.mock.calls[0][0]).toContain('/audiobooks/search?query=Boken&language=sv')
  expect(screen.getByText(/Språket är troligt/)).toBeInTheDocument()
  fireEvent.click(screen.getByRole('button', { name: 'Välj utgåva' }))
  expect(fetch).toHaveBeenCalledTimes(1)
  expect(screen.getByRole('group', { name: 'Bekräfta ljudboksutgåva' })).toHaveFocus()
  const confirm = screen.getByRole('button', { name: 'Begär hämtning' })
  fireEvent.click(confirm)
  fireEvent.click(confirm)
  expect(await screen.findByText('Status: Begärd')).toBeInTheDocument()
  expect(fetch).toHaveBeenCalledTimes(2)
  expect(document.querySelector('audio')).toBeNull()
  expect(screen.getByRole('button', { name: 'Välj utgåva' })).toBeDisabled()
})

test('library uses only catalog reads and discloses the bounded search without changing progress', async () => {
  const fetch = vi.fn((url: string, init?: RequestInit) => {
    expect(init?.method ?? 'GET').toBe('GET')
    if (url.endsWith('/overview')) return Promise.resolve(ok({ state: 'configuredHealthy' }))
    expect(url).toContain('/audiobooks/library?page=0&limit=50&query=Boken&language=')
    return Promise.resolve(ok({ items: [item], total: 1, page: 0, pageSize: 50 }))
  })
  vi.stubGlobal('fetch', fetch)
  start()
  fireEvent.click(screen.getByRole('button', { name: 'Mina bibliotek' }))
  fireEvent.click(screen.getByRole('button', { name: 'Sök' }))
  expect(await screen.findByText(item.title)).toBeInTheDocument()
  expect(screen.getByText(/50 senast tillagda/)).toBeInTheDocument()
  expect(fetch).toHaveBeenCalledTimes(2)
})

test.each(['notConfigured', 'configuredUnavailable'])(
  'library reports %s honestly without an empty-library claim',
  async state => {
    const fetch = vi.fn(() => Promise.resolve(ok({ state })))
    vi.stubGlobal('fetch', fetch)
    start()
    fireEvent.click(screen.getByRole('button', { name: 'Mina bibliotek' }))
    fireEvent.click(screen.getByRole('button', { name: 'Sök' }))
    expect(
      await screen.findByText(
        state === 'notConfigured'
          ? 'Audiobookshelf är inte konfigurerat.'
          : 'Audiobookshelf är tillfälligt otillgängligt.',
      ),
    ).toBeInTheDocument()
    expect(screen.queryByText(/Inga lokala träffar/)).toBeNull()
    expect(fetch).toHaveBeenCalledTimes(1)
  },
)

test.each(['Film', 'Mina bibliotek', 'Rensa sökning'])(
  'aborts and rejects a late successful response after %s',
  async action => {
    let resolve!: (value: unknown) => void
    let signal: AbortSignal | undefined
    vi.stubGlobal(
      'fetch',
      vi.fn((_url: string, init?: RequestInit) => {
        signal = init?.signal as AbortSignal
        return new Promise(done => {
          resolve = done
        })
      }),
    )
    start()
    fireEvent.click(screen.getByRole('button', { name: 'Sök' }))
    fireEvent.click(screen.getByRole('button', { name: action }))
    expect(signal?.aborted).toBe(true)
    await act(async () => resolve(ok(results)))
    expect(screen.queryByText(item.title)).toBeNull()
    expect(screen.queryByText(candidate.title)).toBeNull()
    expect(screen.queryByRole('button', { name: 'Sök pågår' })).toBeNull()
  },
)

test('failed acquisition is not retried and its arbitrary error text is hidden', async () => {
  const fetch = vi.fn((url: string) =>
    url.includes('/search?') ? Promise.resolve(ok(results)) : Promise.reject(new Error('private upstream body')),
  )
  vi.stubGlobal('fetch', fetch)
  start()
  fireEvent.click(screen.getByRole('button', { name: 'Sök' }))
  fireEvent.click(await screen.findByRole('button', { name: 'Välj utgåva' }))
  fireEvent.click(screen.getByRole('button', { name: 'Begär hämtning' }))
  expect(await screen.findByRole('alert')).toHaveTextContent('utfall kunde inte bekräftas')
  expect(screen.queryByText('private upstream body')).toBeNull()
  expect(screen.getByRole('button', { name: 'Begär hämtning' })).toBeDisabled()
  expect(fetch).toHaveBeenCalledTimes(2)
})

test('unconfigured discovery, empty results and missing playback link have truthful states', async () => {
  vi.stubGlobal(
    'fetch',
    vi.fn(() =>
      Promise.resolve(
        ok({
          ...results,
          library: [{ ...item, playbackUrl: null }],
          discovery: [],
          acquisition: { ...results.acquisition, state: 'notConfigured', canSearch: false, canRequest: false },
        }),
      ),
    ),
  )
  start()
  fireEvent.click(screen.getByRole('button', { name: 'Sök' }))
  expect(await screen.findByText('Anskaffning är inte konfigurerad')).toBeInTheDocument()
  expect(screen.getByText(/Ingen uppspelningslänk/)).toBeInTheDocument()
  expect(screen.queryByRole('link')).toBeNull()
  expect(screen.queryByRole('button', { name: 'Välj utgåva' })).toBeNull()
})

test('movie search rejects late failure after switching to audiobook', async () => {
  let reject!: (value: unknown) => void
  vi.stubGlobal(
    'fetch',
    vi.fn(
      () =>
        new Promise((_resolve, fail) => {
          reject = fail
        }),
    ),
  )
  render(<MediaSearch />)
  fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'Movie' } })
  fireEvent.click(screen.getByRole('button', { name: 'Sök' }))
  fireEvent.click(screen.getByRole('button', { name: 'Ljudbok' }))
  await act(async () => reject(new Error('old failure')))
  await waitFor(() => expect(screen.queryByRole('alert')).toBeNull())
})

test('type and mode switches clear completed results and retain keyboard focus', async () => {
  vi.stubGlobal(
    'fetch',
    vi.fn(() => Promise.resolve(ok(results))),
  )
  start()
  fireEvent.click(screen.getByRole('button', { name: 'Sök' }))
  await screen.findByText(item.title)
  fireEvent.click(screen.getByRole('button', { name: 'Mina bibliotek' }))
  expect(screen.queryByText(item.title)).toBeNull()
  await waitFor(() => expect(screen.getByRole('button', { name: 'Mina bibliotek' })).toHaveFocus())
  fireEvent.click(screen.getByRole('button', { name: 'Serie' }))
  await waitFor(() => expect(screen.getByRole('button', { name: 'Serie' })).toHaveFocus())
  expect(screen.queryByText(candidate.title)).toBeNull()
})

test('empty discovery and transport failures are explicit; changing language clears old results', async () => {
  const fetch = vi
    .fn()
    .mockResolvedValueOnce(ok({ ...results, library: [], discovery: [] }))
    .mockRejectedValueOnce(new Error('private'))
  vi.stubGlobal('fetch', fetch)
  start()
  fireEvent.click(screen.getByRole('button', { name: 'Sök' }))
  await screen.findByText('Inga utgåvor hittades för anskaffning')
  fireEvent.change(screen.getByRole('combobox', { name: 'Föredraget ljudboksspråk' }), { target: { value: 'en' } })
  expect(screen.queryByText('Inga utgåvor hittades för anskaffning')).toBeNull()
  fireEvent.click(screen.getByRole('button', { name: 'Sök' }))
  expect(await screen.findByRole('alert')).toHaveTextContent('Ljudbokssökningen kunde inte genomföras')
  expect(fetch.mock.calls[1][0]).toContain('language=en')
})

test('existing acquisition history is readable on demand without creating jobs or playback sessions', async () => {
  const fetch = vi.fn((_url: string, init?: RequestInit) => {
    expect(init?.method ?? 'GET').toBe('GET')
    return Promise.resolve(ok({ items: [{ id: 'previous-job', candidate, status: 'indexing' }], total: 1 }))
  })
  vi.stubGlobal('fetch', fetch)
  start()
  expect(fetch).not.toHaveBeenCalled()
  fireEvent.click(screen.getByRole('button', { name: 'Visa senaste ljudbokshämtningar' }))
  expect(await screen.findByText('Ny utgåva · Indexeras')).toBeInTheDocument()
  expect(fetch.mock.calls[0][0]).toBe('/api/v1/modules/media/audiobooks/acquisition/jobs?offset=0&limit=25')
})
