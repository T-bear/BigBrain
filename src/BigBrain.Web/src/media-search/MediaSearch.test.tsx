import { cleanup, fireEvent, render, screen, within } from '@testing-library/react'
import { afterEach, expect, test, vi } from 'vitest'
import { MediaSearch } from './MediaSearch'
import type { MediaSearchResponse } from '../types'

const result: MediaSearchResponse = {
  query: 'Family Guy',
  searchedAtUtc: '2026-07-25T10:00:00Z',
  status: 'partial',
  providers: [
    {
      provider: 'Jellyfin',
      status: 'online',
      error: null,
      results: [{
        sourceId: 'jf-1',
        title: `Family Guy ${'x'.repeat(150)}`,
        year: 1999,
        mediaType: 'series',
        state: 'available',
        posterUrl: null,
        metadata: { seasonCount: 23, episodeCount: null, episodeFileCount: null, hasFile: null, availableInLibrary: true, imageAvailable: true },
      }],
    },
    { provider: 'Sonarr', status: 'unavailable', error: 'The provider could not be reached.', results: [] },
    { provider: 'Radarr', status: 'online', error: null, results: [] },
  ],
}

function response(body: unknown) {
  return { ok: true, json: async () => body }
}

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

test('renders accessible search controls and prevents short queries', () => {
  const fetch = vi.fn()
  vi.stubGlobal('fetch', fetch)
  const { container } = render(<MediaSearch />)
  fireEvent.click(screen.getByRole('button', { name: 'Mina bibliotek' }))

  const input = screen.getByRole('searchbox', { name: 'Titel' })
  const button = screen.getByRole('button', { name: 'Sök' })
  expect(button).toBeDisabled()
  fireEvent.change(input, { target: { value: 'a' } })
  fireEvent.submit(container.querySelector('form')!)
  expect(fetch).not.toHaveBeenCalled()
  expect(button).toBeDisabled()
})

test('Enter starts search and shows loading state', async () => {
  let resolveRequest: ((value: unknown) => void) | undefined
  const fetch = vi.fn(() => new Promise(resolve => { resolveRequest = resolve }))
  vi.stubGlobal('fetch', fetch)
  render(<MediaSearch />)
  fireEvent.click(screen.getByRole('button', { name: 'Mina bibliotek' }))

  fireEvent.change(screen.getByRole('searchbox', { name: 'Titel' }), { target: { value: 'Family Guy' } })
  fireEvent.keyDown(screen.getByRole('searchbox'), { key: 'Enter', code: 'Enter' })

  expect(screen.getByRole('button', { name: 'Sök pågår' })).toHaveAttribute('aria-busy', 'true')
  expect(screen.getByRole('button', { name: 'Sök pågår' })).toBeDisabled()
  resolveRequest?.(response(result))
  expect(await screen.findByText('Results for “Family Guy”')).toBeInTheDocument()
})

test('groups results, empty state and partial provider failure without actions', async () => {
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(response(result))))
  const { container } = render(<MediaSearch />)
  fireEvent.click(screen.getByRole('button', { name: 'Mina bibliotek' }))

  fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'Family Guy' } })
  fireEvent.click(screen.getByRole('button', { name: 'Sök' }))

  const jellyfin = (await screen.findByRole('heading', { name: 'Jellyfin' })).closest('section')
  const sonarr = screen.getByRole('heading', { name: 'Sonarr' }).closest('section')
  const radarr = screen.getByRole('heading', { name: 'Radarr' }).closest('section')
  expect(within(jellyfin!).getByText(/Family Guy/)).toBeInTheDocument()
  expect(within(sonarr!).getByText('The provider could not be reached.')).toBeInTheDocument()
  expect(within(radarr!).getByText('No match in Radarr.')).toBeInTheDocument()
  expect(within(jellyfin!).getByText(/Family Guy/)).toHaveAttribute('title')
  expect(container.querySelector('.media-search-result-list')).toBeInTheDocument()
  expect(container.querySelector('.media-search-controls')).toBeInTheDocument()
  expect(screen.queryByRole('button', { name: /add|play|delete|refresh/i })).not.toBeInTheDocument()
})

test('shows request failure', async () => {
  vi.stubGlobal('fetch', vi.fn(() => Promise.reject(new Error('raw provider error'))))
  render(<MediaSearch />)
  fireEvent.click(screen.getByRole('button', { name: 'Mina bibliotek' }))

  fireEvent.change(screen.getByRole('searchbox'), { target: { value: 'Family Guy' } })
  fireEvent.click(screen.getByRole('button', { name: 'Sök' }))

  expect(await screen.findByRole('alert')).toHaveTextContent('Åtgärden kunde inte slutföras. Försök igen.')
  expect(screen.queryByText('raw provider error')).not.toBeInTheDocument()
})
