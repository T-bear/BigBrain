import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, expect, test, vi } from 'vitest'
import { DownloadControl } from './DownloadControl'

const item = {
  id: 'opaque-download-id', name: 'Safe Show', status: 'active', progressPercent: 40,
  sizeBytes: 1000, downloadedBytes: 400, downloadSpeedBytesPerSecond: 20,
  uploadSpeedBytesPerSecond: 2, queuePosition: 1, category: 'sonarr', ownership: 'sonarr',
  importStatus: 'notImported', destructiveRemovalAllowed: true,
  warnings: ['Det här jobbet verkar hanteras av Sonarr. BigBrain tar endast bort den aktuella posten från qBittorrent.'],
}
const response = (body: unknown, ok = true) => ({ ok, json: async () => body })

afterEach(() => { cleanup(); vi.unstubAllGlobals() })

test('lists safe downloads with Swedish filters and ARR warning', async () => {
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(response({ collectedAtUtc: '2026-08-04T10:00:00Z', downloads: [item] }))))
  render(<DownloadControl />)
  expect(await screen.findByText('Safe Show')).toBeInTheDocument()
  expect(screen.getByText(/Hanteras av Sonarr/)).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Aktiva' })).toHaveAttribute('aria-pressed', 'false')
  fireEvent.click(screen.getByRole('button', { name: 'Pausade' }))
  expect(screen.getByText('Inga nedladdningar i det här filtret.')).toBeInTheDocument()
})

test('normal removal preserves files, locks double click and removes the row after success', async () => {
  let removeResolve!: (value: unknown) => void
  const fetch = vi.fn()
    .mockResolvedValueOnce(response({ collectedAtUtc: '2026-08-04T10:00:00Z', downloads: [item] }))
    .mockResolvedValueOnce(response({ confirmationToken: 'token', expiresAtUtc: '2026-08-04T10:02:00Z', name: item.name, status: 'active', category: 'sonarr', ownership: 'sonarr', downloadedBytes: 400, filesWillBePreserved: true, destructiveRemovalAllowed: true, warnings: [] }))
    .mockImplementationOnce(() => new Promise(resolve => { removeResolve = resolve }))
  vi.stubGlobal('fetch', fetch)
  render(<DownloadControl />)
  fireEvent.click(await screen.findByRole('button', { name: 'Hantera Safe Show' }))
  fireEvent.click(screen.getByRole('button', { name: 'Avbryt nedladdning' }))
  expect(await screen.findByText('Torrentjobbet tas bort från qBittorrent. Redan nedladdade filer bevaras.')).toBeInTheDocument()
  const confirm = screen.getByRole('button', { name: 'Bekräfta' })
  await waitFor(() => expect(confirm).toBeEnabled())
  fireEvent.click(confirm)
  fireEvent.click(confirm)
  expect(fetch).toHaveBeenCalledTimes(3)
  expect(JSON.parse(String(fetch.mock.calls[2][1]?.body))).toEqual({ confirmationToken: 'token', deleteData: false })
  removeResolve(response({ status: 'removed', removed: true, dataPreserved: true, alreadyMissing: false, ownership: 'sonarr', errorCode: null }))
  expect(await screen.findByText('Nedladdningen avbröts. Filerna bevarades.')).toBeInTheDocument()
  expect(screen.queryByText('Safe Show')).not.toBeInTheDocument()
})

test('destructive action is separate, requires checkbox and Escape closes before mutation', async () => {
  const fetch = vi.fn()
    .mockResolvedValueOnce(response({ collectedAtUtc: '2026-08-04T10:00:00Z', downloads: [item] }))
    .mockResolvedValueOnce(response({ confirmationToken: 'destructive-token', expiresAtUtc: '2026-08-04T10:02:00Z', name: item.name, status: 'active', category: 'sonarr', ownership: 'sonarr', downloadedBytes: 400, filesWillBePreserved: false, destructiveRemovalAllowed: true, warnings: ['ARR-varning'] }))
    .mockResolvedValueOnce(response({ status: 'removed', removed: true, dataPreserved: false, alreadyMissing: false, ownership: 'sonarr', errorCode: null }))
  vi.stubGlobal('fetch', fetch)
  render(<DownloadControl />)
  fireEvent.click(await screen.findByRole('button', { name: 'Hantera Safe Show' }))
  fireEvent.click(screen.getByRole('button', { name: 'Avbryt och radera data' }))
  const confirm = await screen.findByRole('button', { name: 'Bekräfta' })
  expect(confirm).toBeDisabled()
  fireEvent.click(screen.getByRole('checkbox', { name: 'Jag förstår att nedladdade data raderas.' }))
  expect(confirm).toBeEnabled()
  fireEvent.keyDown(document, { key: 'Escape' })
  await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument())
  expect(fetch).toHaveBeenCalledTimes(2)
})

test('unsafe destructive removal stays disabled and raw upstream detail is never rendered', async () => {
  const blocked = { ...item, destructiveRemovalAllowed: false, warnings: ['Radering är blockerad.'] }
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(response({ collectedAtUtc: '2026-08-04T10:00:00Z', downloads: [blocked] }))))
  render(<DownloadControl />)
  fireEvent.click(await screen.findByRole('button', { name: 'Hantera Safe Show' }))
  expect(screen.getByRole('button', { name: 'Avbryt och radera data' })).toBeDisabled()
  expect(screen.queryByText(/SID=|raw upstream|[0-9a-f]{40}/i)).not.toBeInTheDocument()
})
