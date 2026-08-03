import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, expect, test, vi } from 'vitest'
import { SmartShuffle } from './SmartShuffle'

const options = { enabled: true, series: [
  { id: 'a', name: 'Serie A', hasPlayableEpisode: true },
  { id: 'b', name: 'Serie B', hasPlayableEpisode: true },
] }
const device = { id: 'opaque-device', displayName: 'Vardagsrums-TV', clientType: 'Tizen', available: true, isPlaying: false }
const session = { id: 'opaque-session', status: 'active', nowPlaying: { id: 'episode', seriesId: 'a', seriesName: 'Serie A', title: 'Pilot', seasonNumber: 1, episodeNumber: 1, playbackPositionTicks: null }, recentSeries: ['a'], remainingSeries: 2, deviceName: 'Vardagsrums-TV', startedAtUtc: '2026-08-03T20:00:00Z', errorCode: null }

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

test('requires two series and starts only from explicit button click', async () => {
  const fetch = vi.fn()
    .mockResolvedValueOnce({ ok: true, json: async () => options })
    .mockResolvedValueOnce({ ok: true, json: async () => [device] })
    .mockResolvedValueOnce({ ok: true, json: async () => session })
  vi.stubGlobal('fetch', fetch)
  render(<SmartShuffle />)

  const start = await screen.findByRole('button', { name: 'Starta på TV' })
  expect(start).toBeDisabled()
  fireEvent.click(screen.getByLabelText('Serie A'))
  expect(start).toBeDisabled()
  fireEvent.click(screen.getByLabelText('Serie B'))
  expect(start).toBeEnabled()
  fireEvent.click(start)

  expect(await screen.findByText(/Serie A – S1E1 Pilot/)).toBeInTheDocument()
  const request = fetch.mock.calls[2][1]
  expect(request.method).toBe('POST')
  expect(JSON.parse(request.body)).toEqual({ seriesIds: ['a', 'b'], deviceId: 'opaque-device' })
})

test('shows guidance when no controllable TV exists and aborts load on unmount', async () => {
  const fetch = vi.fn()
    .mockResolvedValueOnce({ ok: true, json: async () => options })
    .mockResolvedValueOnce({ ok: true, json: async () => [] })
  vi.stubGlobal('fetch', fetch)
  const view = render(<SmartShuffle />)
  expect(await screen.findByText(/Ingen styrbar TV hittades/)).toBeInTheDocument()
  const signal = fetch.mock.calls[0][1].signal as AbortSignal
  view.unmount()
  await waitFor(() => expect(signal.aborted).toBe(true))
})

test('active session can skip and stop automation through explicit actions', async () => {
  const skipped = { ...session, nowPlaying: { ...session.nowPlaying!, seriesId: 'b', seriesName: 'Serie B' }, recentSeries: ['a', 'b'] }
  const stopped = { ...skipped, status: 'stopped' as const }
  const fetch = vi.fn()
    .mockResolvedValueOnce({ ok: true, json: async () => options })
    .mockResolvedValueOnce({ ok: true, json: async () => [device] })
    .mockResolvedValueOnce({ ok: true, json: async () => session })
    .mockResolvedValueOnce({ ok: true, json: async () => skipped })
    .mockResolvedValueOnce({ ok: true, json: async () => stopped })
  vi.stubGlobal('fetch', fetch)
  render(<SmartShuffle />)

  fireEvent.click(await screen.findByLabelText('Serie A'))
  fireEvent.click(screen.getByLabelText('Serie B'))
  fireEvent.click(screen.getByRole('button', { name: 'Starta på TV' }))
  await screen.findByText(/Serie A – S1E1 Pilot/)

  fireEvent.click(screen.getByRole('button', { name: 'Hoppa till nästa' }))
  expect(await screen.findByText(/Serie B – S1E1 Pilot/)).toBeInTheDocument()
  expect(fetch.mock.calls[3][0]).toBe('/api/v1/modules/media/smart-shuffle/sessions/opaque-session/skip')

  fireEvent.click(screen.getByRole('button', { name: 'Stoppa Smart Shuffle' }))
  expect(await screen.findByText(/stopped/)).toBeInTheDocument()
  expect(fetch.mock.calls[4][0]).toBe('/api/v1/modules/media/smart-shuffle/sessions/opaque-session/stop')
  expect(screen.getByRole('button', { name: 'Hoppa till nästa' })).toBeDisabled()
})
