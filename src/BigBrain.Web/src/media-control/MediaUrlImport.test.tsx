import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, expect, test, vi } from 'vitest'
import { MediaUrlImport } from './MediaUrlImport'
import { mediaControlPage } from './mediaNavigation'

const draft = {
  id: 'abc',
  sourceUrl: 'https://audio.example/chapter.m4a',
  fileName: 'Kapitel 1',
  doi: '10.1234/example',
  format: 'm4a',
  proposedFileName: 'Kapitel 1 -- abc.m4a',
  status: 'previewed',
  canStart: false,
  message: 'Ingen fil hämtas.',
  blockerCode: 'safeImportUnavailable',
}
afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})
function fill() {
  fireEvent.change(screen.getByLabelText('Källänk'), { target: { value: draft.sourceUrl } })
  fireEvent.change(screen.getByLabelText('Filnamn'), { target: { value: draft.fileName } })
  fireEvent.change(screen.getByLabelText('DOI (valfritt)'), { target: { value: draft.doi } })
}
function response(value: unknown) {
  return new Response(JSON.stringify(value), { status: 200, headers: { 'Content-Type': 'application/json' } })
}

test('explicit preview and consent persist only an intent; import remains disabled', async () => {
  const fetch = vi.fn(async (url: string) =>
    response(url.endsWith('/preview') ? draft : url.endsWith('/confirm') ? { ...draft, status: 'blocked' } : []),
  )
  vi.stubGlobal('fetch', fetch)
  render(<MediaUrlImport />)
  await waitFor(() => expect(fetch).toHaveBeenCalledTimes(1))
  fill()
  fireEvent.change(screen.getByLabelText('Ljudformat'), { target: { value: 'm4a' } })
  expect(fetch).toHaveBeenCalledTimes(1)
  fireEvent.click(screen.getByRole('button', { name: 'Analysera uppgifter och förhandsgranska' }))
  const confirm = await screen.findByRole('button', { name: 'Bekräfta uppgifter – ingen nedladdning' })
  expect(confirm).toBeDisabled()
  expect(screen.getByRole('button', { name: 'Starta import' })).toBeDisabled()
  fireEvent.click(screen.getByRole('checkbox'))
  fireEvent.click(confirm)
  await screen.findByText('Uppgifterna är bekräftade och sparade. Importen är blockerad, inte köad.')
  expect(fetch.mock.calls.map(call => call[0])).toEqual([
    '/api/v1/modules/media/url-imports/',
    '/api/v1/modules/media/url-imports/preview',
    '/api/v1/modules/media/url-imports/abc/confirm',
  ])
  expect(screen.getByRole('button', { name: 'Starta import' })).toBeDisabled()
})

test('changed input cancels and discards stale preview; navigation cancels reads', async () => {
  let resolvePreview!: (value: Response) => void
  const signals: AbortSignal[] = []
  vi.stubGlobal(
    'fetch',
    vi.fn((url: string, init: RequestInit) => {
      signals.push(init.signal as AbortSignal)
      return url.endsWith('/preview')
        ? new Promise<Response>(resolve => {
            resolvePreview = resolve
          })
        : Promise.resolve(response([]))
    }),
  )
  const view = render(<MediaUrlImport />)
  fill()
  fireEvent.click(screen.getByRole('button', { name: 'Analysera uppgifter och förhandsgranska' }))
  fireEvent.change(screen.getByLabelText('DOI (valfritt)'), { target: { value: '10.1234/changed' } })
  expect(signals[1].aborted).toBe(true)
  await act(async () => resolvePreview(response(draft)))
  expect(screen.queryByText('Granska importavsikten')).not.toBeInTheDocument()
  view.unmount()
  expect(signals.every(signal => signal.aborted)).toBe(true)
})

test('history distinguishes multiple files sharing DOI and shows failures honestly', async () => {
  vi.stubGlobal(
    'fetch',
    vi.fn(async (url: string) =>
      url.endsWith('/preview')
        ? new Response(JSON.stringify({ code: 'unsupportedSource', detail: 'YouTube stöds inte.' }), { status: 400 })
        : response([draft, { ...draft, id: 'def', fileName: 'Kapitel 2', status: 'blocked' }]),
    ),
  )
  render(<MediaUrlImport />)
  await screen.findByText('Kapitel 2')
  expect(screen.getAllByText(/DOI 10.1234\/example/)).toHaveLength(2)
  fill()
  fireEvent.click(screen.getByRole('button', { name: 'Analysera uppgifter och förhandsgranska' }))
  expect(await screen.findByRole('alert')).toHaveTextContent('YouTube stöds inte.')
  expect(screen.queryByRole('button', { name: 'Starta import' })).not.toBeInTheDocument()
})

test('UX Lab specimen makes no requests; direct import route is preserved', () => {
  const fetch = vi.fn()
  vi.stubGlobal('fetch', fetch)
  render(<MediaUrlImport specimen />)
  expect(screen.getByRole('button', { name: 'Analysera uppgifter och förhandsgranska' })).toBeDisabled()
  expect(fetch).not.toHaveBeenCalled()
  expect(mediaControlPage('/media/control/import', '')).toBe('import')
})

test.each([
  ['mp3', '10.1234/example'],
  ['m4a', '10.1234/example'],
  ['mp3', null],
  ['m4a', null],
])('%s with DOI %s preserves preview, consent, start, status and history', async (format, doi) => {
  const mp3 = { ...draft, format, doi, sourceUrl: `https://audio.example/file.${format}` }
  const completed = {
    ...mp3,
    status: 'completed',
    canStart: false,
    itemId: 'indexed-item',
    audiobookshelfUrl: 'https://abs.example/item/indexed-item',
    message: 'Verifierad i Audiobookshelf.',
  }
  let history: unknown[] = []
  const fetch = vi.fn(async (url: string, _init?: RequestInit) =>
    response(
      url.endsWith('/preview')
        ? mp3
        : url.endsWith('/confirm')
          ? { ...mp3, status: 'confirmed', canStart: true }
          : url.endsWith('/start')
            ? { ...mp3, status: 'indexing', canStart: false, message: 'Väntar på indexering.' }
            : url.endsWith('/inspect')
              ? completed
              : history,
    ),
  )
  vi.stubGlobal('fetch', fetch)
  const view = render(<MediaUrlImport />)
  fill()
  const doiField = screen.getByLabelText('DOI (valfritt)')
  expect(doiField).not.toBeRequired()
  fireEvent.change(doiField, { target: { value: doi ?? '' } })
  fireEvent.change(screen.getByLabelText('Källänk'), { target: { value: mp3.sourceUrl } })
  fireEvent.change(screen.getByLabelText('Ljudformat'), { target: { value: format } })
  fireEvent.click(screen.getByRole('button', { name: 'Analysera uppgifter och förhandsgranska' }))
  await screen.findByRole('checkbox')
  const previewCall = fetch.mock.calls.find(([url]) => url.endsWith('/preview'))!
  expect(JSON.parse(previewCall[1]!.body as string)).toEqual({
    sourceUrl: mp3.sourceUrl,
    fileName: draft.fileName,
    doi,
    format,
  })
  if (doi === null) expect(screen.getAllByText(/Ingen DOI angiven/)).toHaveLength(2)
  fireEvent.click(screen.getByRole('checkbox'))
  fireEvent.click(screen.getByRole('button', { name: 'Bekräfta uppgifter – ingen nedladdning' }))
  await waitFor(() => expect(screen.getByRole('button', { name: 'Starta import' })).toBeEnabled())
  expect(fetch.mock.calls.some(([url]) => url.endsWith('/start'))).toBe(false)
  fireEvent.click(screen.getByRole('button', { name: 'Starta import' }))
  const inspect = await screen.findByRole('button', { name: 'Kontrollera importstatus för Kapitel 1' })
  expect(screen.queryByText(/Audiobookshelf-objekt:/)).not.toBeInTheDocument()
  fireEvent.click(inspect)
  await screen.findByText('Audiobookshelf-objekt: indexed-item')
  expect(screen.getByRole('link', { name: 'Öppna Kapitel 1 i Audiobookshelf' })).toHaveAttribute(
    'href',
    'https://abs.example/item/indexed-item',
  )
  expect(fetch.mock.calls.filter(([url]) => url.endsWith('/start'))).toHaveLength(1)
  history = [completed]
  view.unmount()
  render(<MediaUrlImport />)
  await screen.findByText('Audiobookshelf-objekt: indexed-item')
  if (doi === null) expect(screen.getByText(/Ingen DOI angiven/)).toBeInTheDocument()
  else expect(screen.getByText(/DOI 10.1234\/example/)).toBeInTheDocument()
  expect(fetch.mock.calls.filter(([url]) => url.endsWith('/start'))).toHaveLength(1)
})
