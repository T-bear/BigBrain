import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, expect, test, vi } from 'vitest'
import { MediaJobs } from './MediaJobs'
import type { MediaJob, MediaJobsResponse } from '../types'

const getMediaJobs = vi.fn()
const getMediaPlay = vi.fn()

vi.mock('../api', () => ({
  getMediaJobs: (...args: unknown[]) => getMediaJobs(...args),
  getMediaPlay: (...args: unknown[]) => getMediaPlay(...args),
}))

const importingJob: MediaJob = {
  id: '818c6e2a440b345bf8cd73c1',
  title: 'The Expanse',
  subtitle: 'Season 1',
  provider: 'Sonarr',
  mediaType: 'season',
  status: 'importing',
  progressPercent: 98,
  sizeBytes: 1_000,
  downloadSpeedBytesPerSecond: 0,
  uploadSpeedBytesPerSecond: 0,
  etaSeconds: 120,
  episodeCount: 2,
  completedEpisodeCount: 1,
  requestedAt: '2026-07-25T09:54:00Z',
  startedAt: '2026-07-25T09:55:00Z',
  updatedAt: '2026-07-25T10:00:00Z',
  availableAt: null,
  errorCode: null,
  userMessage: null,
  playItemId: null,
  canPlay: false,
  artwork: null,
  details: [{
    provider: 'Sonarr',
    status: 'importing',
    progressPercent: 98,
    subtitle: 'Episode 1',
    userMessage: null,
  }],
}

const importing: MediaJobsResponse = {
  collectedAtUtc: '2026-07-25T10:00:00Z',
  status: 'complete',
  providers: [
    { provider: 'Sonarr', status: 'online', userMessage: null },
    { provider: 'Radarr', status: 'online', userMessage: null },
    { provider: 'qBittorrent', status: 'online', userMessage: null },
    { provider: 'Jellyfin', status: 'online', userMessage: null },
  ],
  jobs: [importingJob],
}

beforeEach(() => {
  getMediaJobs.mockReset()
  getMediaPlay.mockReset()
  getMediaJobs.mockResolvedValue(importing)
  getMediaPlay.mockResolvedValue({
    jellyfinItemId: 'abc123',
    title: 'The Expanse',
    mediaType: 'series',
    artwork: null,
    playUrl: '/jellyfin/web/index.html#!/details?id=abc123',
    canPlay: true,
  })
})

afterEach(cleanup)

test('renders jobs, progress, episode aggregation and expandable provider details', async () => {
  const { container } = render(<MediaJobs />)

  expect(await screen.findByRole('heading', { name: 'The Expanse' })).toBeInTheDocument()
  expect(screen.getByText('Bearbetas', { selector: '.media-job__status' })).toBeInTheDocument()
  expect(screen.getByLabelText('98 percent')).toBeInTheDocument()
  expect(screen.getByText('1 av 2 avsnitt klara')).toBeInTheDocument()
  expect(screen.getByText('Cirka 2 min kvar')).toBeInTheDocument()
  expect(screen.getByText('Provider: Sonarr')).not.toBeVisible()
  fireEvent.click(screen.getByText('Visa tekniska detaljer'))
  expect(screen.getByText('Provider: Sonarr')).toBeVisible()
  expect(screen.getByText('Episode 1')).toBeInTheDocument()
  expect(container.querySelector('.media-jobs-grid')).toBeInTheDocument()
  expect(container.querySelector('.media-job__identity')).toBeInTheDocument()
})

test('summarizes multiple active jobs compactly and expands their full cards', async () => {
  getMediaJobs.mockResolvedValue({ ...importing, jobs: [
    importingJob,
    { ...importingJob, id: '918c6e2a440b345bf8cd73c2', title: 'Alien 1979 2160p UHD BluRay X265-GROUP', progressPercent: 42 },
  ] })
  render(<MediaJobs />)
  expect(await screen.findByRole('heading', { name: '2 pågående nedladdningar' })).toBeInTheDocument()
  expect(screen.getByText('The Expanse')).toBeInTheDocument()
  expect(screen.getByText('Alien (1979)')).toHaveAttribute('title', 'Alien 1979 2160p UHD BluRay X265-GROUP')
  expect(screen.getByText('42%')).toBeInTheDocument()
  expect(screen.queryAllByRole('article')).toHaveLength(0)
  const show = screen.getByRole('button', { name: 'Visa nedladdningar' })
  expect(show).toHaveAttribute('aria-expanded', 'false')
  fireEvent.click(show)
  expect(screen.getAllByRole('article')).toHaveLength(2)
  expect(screen.getAllByText('Visa tekniska detaljer')[0].closest('details')).not.toHaveAttribute('open')
  fireEvent.click(screen.getByRole('button', { name: 'Dölj nedladdningar' }))
  expect(screen.queryAllByRole('article')).toHaveLength(0)
})

test('polling transition to available resolves Jellyfin play metadata without reload', async () => {
  getMediaJobs
    .mockResolvedValueOnce(importing)
    .mockResolvedValue({
      ...importing,
      jobs: [{
        ...importingJob,
        mediaType: 'series',
        subtitle: null,
        status: 'available',
        progressPercent: 100,
        playItemId: 'abc123',
        canPlay: true,
      }],
    })
  render(<MediaJobs />)
  await screen.findByRole('heading', { name: 'The Expanse' })

  act(() => window.dispatchEvent(new Event('focus')))
  Object.defineProperty(document, 'visibilityState', { configurable: true, value: 'visible' })
  act(() => document.dispatchEvent(new Event('visibilitychange')))
  fireEvent.click(within(screen.getByLabelText('Filtrera pågående media')).getByRole('button', { name: 'Klara' }))

  expect(await screen.findByText('Klar', { selector: '.media-job__status' })).toBeInTheDocument()
  const play = await screen.findByRole('link', { name: /spela i jellyfin/i })
  expect(play).toHaveAttribute('href', '/jellyfin/web/index.html#!/details?id=abc123')
  expect(getMediaPlay).toHaveBeenCalledTimes(1)
})

test('Jellyfin degraded state retains jobs and never shows an unverified play action', async () => {
  getMediaJobs.mockResolvedValue({
    ...importing,
    status: 'degraded',
    providers: importing.providers.map(provider =>
      provider.provider === 'Jellyfin'
        ? { ...provider, status: 'unavailable' as const, userMessage: 'sanitized' }
        : provider),
  })
  render(<MediaJobs />)

  expect(await screen.findByText('Vissa mediatjänster svarar inte just nu. Tillgängliga nedladdningar visas fortfarande.')).toBeInTheDocument()
  expect(screen.getByRole('heading', { name: 'The Expanse' })).toBeInTheDocument()
  expect(screen.queryByRole('link', { name: /play in jellyfin/i })).not.toBeInTheDocument()
})

test('polling failure keeps the latest snapshot and shows degraded update state', async () => {
  getMediaJobs.mockResolvedValueOnce(importing).mockRejectedValueOnce(new Error('offline'))
  render(<MediaJobs />)
  await screen.findByRole('heading', { name: 'The Expanse' })

  Object.defineProperty(document, 'visibilityState', { configurable: true, value: 'visible' })
  act(() => document.dispatchEvent(new Event('visibilitychange')))

  expect(await screen.findByText('Automatisk uppdatering är tillfälligt otillgänglig. Senaste status visas.')).toBeInTheDocument()
  expect(screen.getByRole('heading', { name: 'The Expanse' })).toBeInTheDocument()
})

test('filters jobs and reveals only a bounded first page', async () => {
  const availableJobs = Array.from({ length: 9 }, (_, index): MediaJob => ({
    ...importingJob,
    id: `${index}`.padStart(24, '0'),
    title: `Available title ${index}`,
    mediaType: 'movie',
    subtitle: null,
    status: 'available',
    progressPercent: 100,
    canPlay: false,
  }))
  getMediaJobs.mockResolvedValue({ ...importing, jobs: availableJobs })
  render(<MediaJobs />)
  await screen.findByRole('button', { name: 'Klara' })

  fireEvent.click(within(screen.getByLabelText('Filtrera pågående media')).getByRole('button', { name: 'Klara' }))

  expect(screen.getAllByRole('article')).toHaveLength(8)
  fireEvent.click(screen.getByRole('button', { name: 'Visa fler' }))
  expect(screen.getAllByRole('article')).toHaveLength(9)
})

test('long titles remain in overflow-protected identity and errors are sanitized', async () => {
  getMediaJobs.mockResolvedValue({
    ...importing,
    jobs: [{
      ...importingJob,
      title: 'A very long title '.repeat(30),
      status: 'failed',
      errorCode: 'providerJobFailed',
      userMessage: 'This media job needs attention.',
    }],
  })
  const { container } = render(<MediaJobs />)
  fireEvent.click(within(await screen.findByLabelText('Filtrera pågående media')).getByRole('button', { name: 'Problem' }))

  expect(await screen.findByRole('alert')).toHaveTextContent('This media job needs attention.')
  const article = screen.getByRole('article')
  expect(within(article).getByRole('heading')).toBeInTheDocument()
  expect(container.querySelector('.media-job__identity')).toBeInTheDocument()
  await waitFor(() => expect(screen.queryByText(/api\/v3|exception|\/srv\/|https?:\/\//i)).not.toBeInTheDocument())
  expect(screen.queryByRole('button', { name: /delete|pause|resume|download|release|command/i })).not.toBeInTheDocument()
})
