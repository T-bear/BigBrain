import { useEffect, useRef, useState } from 'react'
import {
  ApiError,
  confirmUrlImport,
  getUrlImportDrafts,
  previewUrlImport,
  type MediaUrlImportDraft,
  type MediaUrlImportInput,
} from '../api'
import { BBButton, BBEmptyState, BBInput, BBSelect, BBSurface } from '../components'

export function MediaUrlImport({ specimen = false }: { specimen?: boolean }) {
  const [input, setInput] = useState<MediaUrlImportInput>({ sourceUrl: '', fileName: '', doi: '', format: 'm4a' })
  const [draft, setDraft] = useState<MediaUrlImportDraft | null>(null)
  const [history, setHistory] = useState<MediaUrlImportDraft[]>([])
  const [agreed, setAgreed] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const request = useRef<AbortController | null>(null)
  const generation = useRef(0)
  const result = useRef<HTMLElement>(null)

  useEffect(() => {
    if (specimen) return
    const controller = new AbortController()
    void getUrlImportDrafts(controller.signal)
      .then(items => {
        if (!controller.signal.aborted)
          setHistory(current =>
            [...current, ...items.filter(item => !current.some(saved => saved.id === item.id))].slice(0, 200),
          )
      })
      .catch(() => {
        if (!controller.signal.aborted) setError('Sparade importavsikter kunde inte läsas. Försök igen senare.')
      })
    return () => {
      controller.abort()
      request.current?.abort()
      generation.current++
    }
  }, [specimen])

  function change(field: keyof MediaUrlImportInput, value: string) {
    request.current?.abort()
    generation.current++
    setInput(current => ({ ...current, [field]: value }))
    setDraft(null)
    setAgreed(false)
    setError('')
    setBusy(false)
  }

  async function act(confirm: boolean) {
    if (specimen || (confirm && (!draft || !agreed))) return
    request.current?.abort()
    const controller = new AbortController()
    request.current = controller
    const version = ++generation.current
    setBusy(true)
    setError('')
    try {
      const value = confirm
        ? await confirmUrlImport(draft!.id, controller.signal)
        : await previewUrlImport(input, controller.signal)
      if (version !== generation.current || controller.signal.aborted) return
      setDraft(value)
      setHistory(items => [value, ...items.filter(item => item.id !== value.id)].slice(0, 200))
      result.current?.focus()
    } catch (failure) {
      if (version === generation.current && !controller.signal.aborted)
        setError(
          failure instanceof ApiError
            ? failure.message
            : 'Uppgifterna kunde inte sparas. Ingen nedladdning har startats.',
        )
    } finally {
      if (version === generation.current) setBusy(false)
    }
  }

  return (
    <div className="url-import">
      <p role="note">
        Förbered en ljudimport. Nedladdning och biblioteksimport är spärrade tills en säker importväg är verifierad.
      </p>
      <form
        onSubmit={event => {
          event.preventDefault()
          void act(false)
        }}
      >
        <label>
          Källänk
          <BBInput
            type="url"
            required
            maxLength={2048}
            placeholder="https://källa.example/ljud.m4a"
            value={input.sourceUrl}
            onChange={e => change('sourceUrl', e.target.value)}
          />
        </label>
        <label>
          Filnamn
          <BBInput
            required
            maxLength={80}
            placeholder="Kapitel 1 – Introduktion"
            value={input.fileName}
            onChange={e => change('fileName', e.target.value)}
          />
        </label>
        <label>
          DOI
          <BBInput
            required
            maxLength={271}
            placeholder="10.1234/exempel"
            value={input.doi}
            onChange={e => change('doi', e.target.value)}
          />
        </label>
        <label>
          Ljudformat
          <BBSelect value={input.format} onChange={e => change('format', e.target.value)}>
            <option value="m4a">M4A</option>
            <option value="mp3">MP3</option>
          </BBSelect>
        </label>
        <p>
          Endast länkens och uppgifternas format kontrolleras. Ingen källa kontaktas. DOI-registrering, innehåll,
          rättigheter och faktisk filtyp är inte verifierade. YouTube, videosidor och konvertering stöds inte.
        </p>
        <BBButton type="submit" busy={busy} disabled={specimen}>
          Analysera uppgifter och förhandsgranska
        </BBButton>
      </form>
      {error && <p role="alert">{error}</p>}
      <section ref={result} tabIndex={-1} aria-label="Importförhandsgranskning" aria-live="polite">
        {draft && (
          <BBSurface>
            <h2>Granska importavsikten</h2>
            <dl>
              <dt>Källänk</dt>
              <dd>{draft.sourceUrl}</dd>
              <dt>Filnamn</dt>
              <dd>{draft.fileName}</dd>
              <dt>DOI</dt>
              <dd>{draft.doi}</dd>
              <dt>Ljudformat</dt>
              <dd>{draft.format.toUpperCase()}</dd>
              <dt>Föreslagen unik filidentitet (ingen fil skapad)</dt>
              <dd>{draft.proposedFileName}</dd>
            </dl>
            <p>{draft.message}</p>
            {draft.status === 'previewed' && (
              <>
                <label className="url-import-consent">
                  <input type="checkbox" checked={agreed} onChange={e => setAgreed(e.target.checked)} />
                  Jag har granskat samtliga uppgifter och har rätt att hämta och lagra innehållet.
                </label>
                <BBButton type="button" disabled={!agreed || specimen} busy={busy} onClick={() => void act(true)}>
                  Bekräfta uppgifter – ingen nedladdning
                </BBButton>
              </>
            )}
            {draft.status === 'blocked' && (
              <p role="status">Uppgifterna är bekräftade och sparade. Importen är blockerad, inte köad.</p>
            )}
            <BBButton type="button" disabled aria-describedby="url-import-blocker">
              Starta import
            </BBButton>
            <p id="url-import-blocker">
              Säker placering utan överskrivning och exakt Audiobookshelf-indexering måste verifieras först.
            </p>
          </BBSurface>
        )}
      </section>
      <section aria-label="Sparade importavsikter">
        <h2>Sparade importavsikter</h2>
        {history.length === 0 ? (
          <BBEmptyState title="Inga sparade importavsikter" />
        ) : (
          <ul>
            {history.map(item => (
              <li key={item.id}>
                <strong>{item.fileName}</strong> · DOI {item.doi} · {item.format.toUpperCase()}
                <br />
                {item.status === 'blocked' ? 'Bekräftad men blockerad' : 'Förhandsgranskad'} — ingen fil skapad.
                <br />
                <small>Import-ID: {item.id}</small>
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  )
}
