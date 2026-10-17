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
  fireEvent.change(screen.getByLabelText('DOI'), { target: { value: draft.doi } })
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
  fireEvent.change(screen.getByLabelText('DOI'), { target: { value: '10.1234/changed' } })
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
