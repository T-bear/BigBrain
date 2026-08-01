import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, expect, test, vi } from 'vitest'
import { MobileNavigation } from '../MobileNavigation'
import { MediaPoster } from './MediaPoster'
import { MediaSearch } from './MediaSearch'
import { MediaServiceLinks } from '../media-services/MediaServiceLinks'
import { MediaLookupResultCard } from './MediaLookupResultCard'

afterEach(() => {
  cleanup()
  vi.restoreAllMocks()
  vi.unstubAllGlobals()
  window.location.hash = ''
})

const emptyLookup = {
  query: 'Alien',
  mediaType: 'movie',
  lookedUpAtUtc: '2026-07-26T10:00:00Z',
  status: 'complete',
  requestsEnabled: true,
  providers: [],
}

test('switching Film and Serie sends the selected media type', async () => {
  const fetch = vi.fn((_url: string) => Promise.resolve({ ok: true, json: async () => emptyLookup }))
  vi.stubGlobal('fetch', fetch)
  render(<MediaSearch />)
  fireEvent.click(screen.getByRole('button', { name: 'Film' }))
  fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'Alien' } })
  fireEvent.click(screen.getByRole('button', { name: 'Sök' }))
  await screen.findByText('Resultat för “Alien”')
  expect(String(fetch.mock.calls[0][0])).toContain('mediaType=movie')

  fireEvent.click(screen.getByRole('button', { name: 'Serie' }))
  fireEvent.click(screen.getByRole('button', { name: 'Sök' }))
  await screen.findByText('Resultat för “Alien”')
  expect(String(fetch.mock.calls[1][0])).toContain('mediaType=series')
})

test('puts search first, ranks the best result and keeps additional results collapsed', async () => {
  const result = (title: string, foreignId: string) => ({
    provider: 'Radarr', foreignId, title, originalTitle: null, year: 2026, overview: null,
    network: null, runtimeMinutes: 90, status: 'released', mediaType: 'movie', lookupState: 'external',
    imageAvailable: false, alreadyRegistered: false, existingSourceId: null,
  })
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve({ ok: true, json: async () => ({
    ...emptyLookup,
    query: 'Alien',
    providers: [{ provider: 'Radarr', status: 'online', error: null, results: [
      result('Alien: Romulus', '2'), result('Alien', '1'), result('Another title', '3'),
    ] }],
  }) })))
  const { container } = render(<MediaSearch />)
  const search = screen.getByRole('search')
  const type = screen.getByRole('group', { name: 'Jag söker' })
  expect(search.compareDocumentPosition(type) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
  fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'Alien' } })
  fireEvent.click(screen.getByRole('button', { name: 'Sök' }))
  expect(await screen.findByRole('heading', { name: 'Alien' })).toBeInTheDocument()
  expect(screen.queryByRole('heading', { name: 'Alien: Romulus' })).not.toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Visa 2 fler träffar' })).toHaveAttribute('aria-expanded', 'false')
  fireEvent.click(screen.getByRole('button', { name: 'Visa 2 fler träffar' }))
  expect(screen.getByRole('heading', { name: 'Alien: Romulus' })).toBeInTheDocument()
  fireEvent.click(screen.getByRole('button', { name: 'Visa färre' }))
  expect(screen.queryByRole('heading', { name: 'Alien: Romulus' })).not.toBeInTheDocument()
  fireEvent.click(screen.getByRole('button', { name: 'Rensa sökning' }))
  expect(screen.getByRole('searchbox')).toHaveValue('')
  expect(container.querySelector('.media-search-results')).toBeNull()
})

test('shows contextual search actions only after results have scrolled away', async () => {
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve({ ok: true, json: async () => ({
    ...emptyLookup,
    providers: [{ provider: 'Radarr', status: 'online', error: null, results: [
      { provider: 'Radarr', foreignId: '1', title: 'Alien', originalTitle: null, year: 1979, overview: null, network: null, runtimeMinutes: 117, status: 'released', mediaType: 'movie', lookupState: 'external', imageAvailable: false, alreadyRegistered: false, existingSourceId: null },
      { provider: 'Radarr', foreignId: '2', title: 'Aliens', originalTitle: null, year: 1986, overview: null, network: null, runtimeMinutes: 137, status: 'released', mediaType: 'movie', lookupState: 'external', imageAvailable: false, alreadyRegistered: false, existingSourceId: null },
    ] }],
  }) })))
  const { container } = render(<MediaSearch />)
  const formAnchor = container.querySelector('.media-search-form-anchor')!
  const formRect = vi.spyOn(formAnchor, 'getBoundingClientRect').mockReturnValue({ bottom: 200 } as DOMRect)
  fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'Alien' } })
  fireEvent.click(screen.getByRole('button', { name: 'Sök' }))
  await screen.findByRole('heading', { name: 'Alien' })
  expect(screen.queryByRole('button', { name: 'Sökåtgärder' })).not.toBeInTheDocument()
  formRect.mockReturnValue({ bottom: 0 } as DOMRect)
  fireEvent.scroll(window)
  const fab = await screen.findByRole('button', { name: 'Sökåtgärder' })
  fireEvent.click(fab)
  expect(fab).toHaveAttribute('aria-expanded', 'true')
  expect(screen.getByRole('menuitem', { name: 'Visa 1 fler träffar' })).toBeVisible()
  fireEvent.click(screen.getByRole('menuitem', { name: 'Visa 1 fler träffar' }))
  expect(screen.getByRole('heading', { name: 'Aliens' })).toBeInTheDocument()
  fireEvent.click(fab)
  expect(screen.getByRole('menuitem', { name: 'Visa färre' })).toBeVisible()
  fireEvent.click(screen.getByRole('menuitem', { name: 'Till sökfältet' }))
  await waitFor(() => expect(screen.getByRole('searchbox')).toHaveFocus())
  fireEvent.scroll(window)
  fireEvent.click(await screen.findByRole('button', { name: 'Sökåtgärder' }))
  fireEvent.click(screen.getByRole('menuitem', { name: 'Rensa sökning' }))
  expect(screen.getByRole('searchbox')).toHaveValue('')
  expect(container.querySelector('.media-search-results')).toBeNull()
})

test('does not show contextual search actions for an empty result', async () => {
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve({ ok: true, json: async () => emptyLookup })))
  render(<MediaSearch />)
  fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'Saknas' } })
  fireEvent.click(screen.getByRole('button', { name: 'Sök' }))
  await screen.findByText(/Resultat för/)
  fireEvent.scroll(window)
  expect(screen.queryByRole('button', { name: 'Sökåtgärder' })).not.toBeInTheDocument()
})

test('poster falls back without leaving a broken image', () => {
  render(<MediaPoster title="Alien" url="https://images.example.test/alien.jpg" />)
  fireEvent.error(screen.getByRole('img', { name: 'Poster för Alien' }))
  expect(screen.getByRole('img', { name: 'Poster saknas för Alien' })).toBeInTheDocument()
  expect(screen.queryByRole('img', { name: 'Poster för Alien' })).not.toBeInTheDocument()
})

test('valid poster is lazy loaded and missing poster uses placeholder', () => {
  const { rerender } = render(
    <MediaPoster title="Alien" url="/api/v1/modules/media/posters/signed-token" />)
  expect(screen.getByRole('img', { name: 'Poster för Alien' })).toHaveAttribute('loading', 'lazy')
  expect(screen.getByRole('img', { name: 'Poster för Alien' })).toHaveAttribute(
    'src',
    '/api/v1/modules/media/posters/signed-token')

  rerender(<MediaPoster title="Alien" url={null} />)
  expect(screen.getByRole('img', { name: 'Poster saknas för Alien' })).toBeInTheDocument()
})

test('mobile navigation has stable destinations and marks the active view', () => {
  window.location.hash = '#queue'
  render(<MobileNavigation />)
  expect(screen.getByRole('link', { name: /Hem/ })).toHaveAttribute('href', '#home')
  expect(screen.getByRole('link', { name: /Sök/ })).toHaveAttribute('href', '#search')
  expect(screen.getByRole('link', { name: /Pågår/ })).toHaveAttribute('href', '#queue')
  expect(screen.getByRole('link', { name: /Pågår/ })).toHaveAttribute('aria-current', 'page')
  expect(screen.getByRole('link', { name: /Admin/ })).toHaveAttribute('href', '#administration')
})

test('provider timeout is shown with a Swedish safe message', async () => {
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve({
    ok: false,
    json: async () => ({ code: 'timeout', detail: 'raw upstream detail' }),
  })))
  render(<MediaSearch />)
  fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'Alien' } })
  fireEvent.click(screen.getByRole('button', { name: 'Sök' }))
  expect(await screen.findByRole('alert')).toHaveTextContent('Tjänsten svarade inte i tid. Försök igen.')
  expect(screen.queryByText('raw upstream detail')).not.toBeInTheDocument()
})

test('service shortcuts show configured links and clear disabled states', async () => {
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve({
    ok: true,
    json: async () => [
      { id: 'jellyfin', displayName: 'Jellyfin', url: 'https://media.example.test', enabled: true },
      { id: 'radarr', displayName: 'Radarr', url: '', enabled: false },
      { id: 'sonarr', displayName: 'Sonarr', url: '', enabled: false },
      { id: 'prowlarr', displayName: 'Prowlarr', url: '', enabled: false },
      { id: 'qbittorrent', displayName: 'qBittorrent', url: '', enabled: false },
    ],
  })))
  render(<MediaServiceLinks />)

  const jellyfin = await screen.findByRole('link', { name: /Öppna Jellyfin/ })
  expect(jellyfin).toHaveAttribute('href', 'https://media.example.test')
  expect(jellyfin).toHaveAttribute('target', '_blank')
  expect(screen.getAllByText('Inte konfigurerad')).toHaveLength(4)
  expect(screen.getByText('qBittorrent')).toBeInTheDocument()
})

test('lookup status is placed above the request action with a dedicated gap', () => {
  const { container } = render(<MediaLookupResultCard
    result={{
      provider: 'Radarr',
      mediaType: 'movie',
      foreignId: '348',
      title: 'Alien',
      originalTitle: 'Alien',
      year: 1979,
      overview: 'I rymden kan ingen höra dig skrika.',
      network: null,
      runtimeMinutes: 117,
      status: 'released',
      lookupState: 'external',
      imageAvailable: false,
      posterUrl: null,
      alreadyExists: false,
      alreadyRegistered: false,
      existingSourceId: null,
      providerId: '348',
      monitored: false,
      canRequest: true,
      requestState: 'available',
      errorCode: null,
      errorMessage: null,
    }}
    requestsEnabled
    onPrepare={vi.fn()}
  />)

  const actions = container.querySelector('.media-result-actions')
  expect(actions).not.toBeNull()
  expect(actions?.children[0]).toHaveTextContent('Kan läggas till')
  expect(actions?.children[1]).toHaveTextContent('Lägg till film')
})
