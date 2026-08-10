import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, expect, test, vi } from 'vitest'
import { DownloadControl } from './DownloadControl'

const item = {
  id: 'opaque-download-id', name: 'Safe Show', status: 'active', progressPercent: 40,
  sizeBytes: 1000, downloadedBytes: 400, downloadSpeedBytesPerSecond: 20,
  uploadSpeedBytesPerSecond: 2, queuePosition: 1, category: 'sonarr', ownership: 'sonarr',
  importStatus: 'notImported', destructiveRemovalAllowed: true,
  warnings: ['Det här jobbet verkar hanteras av Sonarr. BigBrain tar endast bort den aktuella posten från qBittorrent.'],
  capabilities: { canPause: true, canResume: false, canRetry: false, canRemove: true },
  diagnosis: { code: 'insufficientData', severity: 'info', explanation: 'BigBrain kan inte avgöra orsaken med tillgänglig information.', verifiedObservations: ['Ingen säker diagnostisk orsak kunde verifieras.'], availableSafeActions: ['pause', 'remove'] },
}
const response = (body: unknown, ok = true) => ({ ok, json: async () => body })
const download = (id: string, status: typeof item.status, capabilities = item.capabilities) => ({
  ...item, id, name: id, status, capabilities,
  progressPercent: status === 'completed' ? 100 : item.progressPercent,
})

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

test('prioritizes problems and active jobs while keeping a large completed history one keyboard action away', async () => {
  const manyCompleted = Array.from({ length: 30 }, (_, index) => download(`Klar ${index + 1}`, 'completed', { canPause: false, canResume: false, canRetry: false, canRemove: true }))
  const downloads = [
    ...manyCompleted,
    download('Aktiv nu', 'active'),
    download('Problem nu', 'error', { canPause: false, canResume: true, canRetry: true, canRemove: true }),
    download('Pausad nu', 'paused', { canPause: false, canResume: true, canRetry: true, canRemove: true }),
  ]
  vi.stubGlobal('fetch', vi.fn(() => Promise.resolve(response({ collectedAtUtc: '2026-08-10T10:00:00Z', downloads }))))
  const { container } = render(<DownloadControl />)

  expect(await screen.findByText('Problem nu')).toBeInTheDocument()
  expect(screen.getByText('Aktiv nu')).toBeInTheDocument()
  expect(screen.queryByText('Klar 1')).not.toBeInTheDocument()
  const headings = [...container.querySelectorAll('.download-group h4')].map(node => node.textContent)
  expect(headings).toEqual(['Fel och problem 1', 'Aktiva 1', 'Köade och pausade 1', 'Klara 30'])

  const toggle = screen.getByRole('button', { name: 'Visa 30 klara' })
  expect(toggle).toHaveAttribute('aria-expanded', 'false')
  toggle.focus()
  fireEvent.keyDown(toggle, { key: 'Enter' })
  fireEvent.click(toggle)
  expect(toggle).toHaveAttribute('aria-expanded', 'true')
  expect(toggle).toHaveFocus()
  expect(screen.getByText('Klar 30')).toBeInTheDocument()
  expect(container.querySelector('.download-control')).toHaveClass('download-control')
  expect(container.querySelector('.download-groups')).toBeInTheDocument()
})

test('filters, filtered selection, batch toolbar, diagnostics and row operations survive grouped presentation', async () => {
  const downloads = [
    download('Aktiv A', 'active'),
    download('Aktiv B', 'active'),
    download('Problem C', 'error', { canPause: false, canResume: true, canRetry: true, canRemove: true }),
    download('Klar D', 'completed', { canPause: false, canResume: false, canRetry: false, canRemove: true }),
  ]
  const fetch = vi.fn(() => Promise.resolve(response({ collectedAtUtc: '2026-08-10T10:00:00Z', downloads })))
  vi.stubGlobal('fetch', fetch)
  render(<DownloadControl />)
  await screen.findByText('Aktiv A')

  fireEvent.click(screen.getByRole('button', { name: 'Aktiva' }))
  expect(screen.queryByText('Problem C')).not.toBeInTheDocument()
  fireEvent.click(screen.getByRole('button', { name: 'Markera alla i aktuell vy' }))
  expect(screen.getByText('2 markerade')).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Pausa markerade' })).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Återuppta markerade' })).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Försök igen markerade' })).toBeInTheDocument()
  expect(screen.getAllByText('Varför laddar den inte ner?')).toHaveLength(2)
  expect(screen.getByRole('button', { name: 'Pausa Aktiv A' })).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Hantera Aktiv A' })).toBeInTheDocument()

  fireEvent.click(screen.getByRole('button', { name: 'Fel' }))
  expect(screen.getByRole('button', { name: 'Återuppta Problem C' })).toBeInTheDocument()
  expect(screen.getByRole('button', { name: 'Försök igen Problem C' })).toBeInTheDocument()
  expect(screen.getByText('2 markerade')).toBeInTheDocument()
  fireEvent.click(screen.getByRole('button', { name: 'Avmarkera alla' }))
  expect(screen.getByText('0 markerade')).toBeInTheDocument()
})
