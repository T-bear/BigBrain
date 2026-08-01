import { useEffect, useRef, useState, type FormEvent, type KeyboardEvent } from 'react'
import { ApiError, confirmMediaRequest, createIdempotencyKey, getMediaAddOptions, previewMediaRequest } from '../api'
import type {
  MediaAddOptionsResponse,
  MediaLookupResult,
  MediaRequestConfirmResponse,
  MediaRequestPreviewResponse,
} from '../types'

type Phase = 'options' | 'review' | 'success'

function friendlyError(error: unknown) {
  if (error instanceof ApiError) {
    if (error.code === 'requestExpired') return 'This preview expired. Prepare the addition again.'
    if (error.code === 'alreadyRegistered') return 'This title is already registered.'
    if (error.code === 'providerUnavailable') return 'The provider is currently unavailable.'
    if (error.code === 'providerConfigurationInvalid') return 'The provider configuration was rejected.'
    if (error.code === 'providerRejectedRequest') return 'The provider rejected the request as invalid.'
    return error.message
  }
  return 'The media request could not be completed.'
}

export function MediaRequestDialog({
  result,
  returnFocus,
  onClose,
}: {
  result: MediaLookupResult
  returnFocus: HTMLButtonElement | null
  onClose: () => void
}) {
  const [phase, setPhase] = useState<Phase>('options')
  const [options, setOptions] = useState<MediaAddOptionsResponse | null>(null)
  const [preview, setPreview] = useState<MediaRequestPreviewResponse | null>(null)
  const [created, setCreated] = useState<MediaRequestConfirmResponse | null>(null)
  const [busy, setBusy] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const confirming = useRef(false)

  useEffect(() => {
    const previousOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    return () => { document.body.style.overflow = previousOverflow }
  }, [])

  useEffect(() => {
    const controller = new AbortController()
    void getMediaAddOptions(result.mediaType, controller.signal)
      .then(value => { setOptions(value); setBusy(false) })
      .catch(requestError => { setError(friendlyError(requestError)); setBusy(false) })
    return () => controller.abort()
  }, [result.mediaType])

  function close() {
    if (busy) return
    onClose()
    queueMicrotask(() => returnFocus?.focus())
  }

  function escape(event: KeyboardEvent<HTMLDialogElement>) {
    if (event.key === 'Escape' && phase !== 'success' && !busy) {
      event.preventDefault()
      close()
    }
  }

  async function previewRequest(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (busy || !options) return
    const data = new FormData(event.currentTarget)
    setBusy(true)
    setError(null)
    try {
      const response = await previewMediaRequest({
        provider: result.provider,
        mediaType: result.mediaType,
        foreignId: result.foreignId,
        rootFolderId: data.get('rootFolderId'),
        qualityProfileId: data.get('qualityProfileId'),
        monitor: data.get('monitor'),
        seriesType: data.get('seriesType') || null,
        searchAfterAdd: data.get('searchAfterAdd') === 'on',
      })
      setPreview(response)
      setPhase('review')
    } catch (requestError) {
      setError(friendlyError(requestError))
    } finally {
      setBusy(false)
    }
  }

  async function confirm(event: FormEvent) {
    event.preventDefault()
    if (busy || confirming.current || phase !== 'review' || !preview) return
    confirming.current = true
    setBusy(true)
    setError(null)
    try {
      const response = await confirmMediaRequest(preview.requestToken, createIdempotencyKey())
      setCreated(response)
      setPhase('success')
    } catch (requestError) {
      setError(friendlyError(requestError))
    } finally {
      confirming.current = false
      setBusy(false)
    }
  }

  return <dialog open aria-modal="true" aria-labelledby="media-request-title" className="media-request-dialog" onKeyDown={escape}>
    <div className="media-request-dialog-content">
      <header><div><p className="eyebrow">Lägg till</p><h3 id="media-request-title">{result.title}</h3></div>
        <button type="button" aria-label="Stäng" onClick={close} disabled={busy}>×</button>
      </header>
      <p>Granska först. Ingenting läggs till förrän du bekräftar nästa steg.</p>
      {busy && <p aria-live="polite">Laddar…</p>}
      {error && <p className="notice notice--error" role="alert">{error}</p>}
      {phase === 'options' && options && <form className="media-request-form" onSubmit={event => void previewRequest(event)}>
        <p>BigBrain använder rekommenderade inställningar. Du kan ändra dem om det behövs.</p>
        <details className="media-request-advanced"><summary>Avancerade inställningar</summary><div>
        <label>Rotmapp<select name="rootFolderId" defaultValue={options.defaultRootFolderId ?? ''} required>
          {options.rootFolders.map(option => <option value={option.id} key={option.id}>{option.displayName}</option>)}
        </select></label>
        <label>Kvalitetsprofil<select name="qualityProfileId" defaultValue={options.defaultQualityProfileId ?? ''} required>
          {options.qualityProfiles.map(option => <option value={option.id} key={option.id}>{option.displayName}</option>)}
        </select></label>
        <label>Bevakning<select name="monitor" defaultValue={options.defaultMonitoringOptionId} required>
          {options.monitoringOptions.map(option => <option value={option.id} key={option.id}>{option.displayName}</option>)}
        </select></label>
        {result.mediaType === 'series' && <label>Serietyp<select name="seriesType" defaultValue={options.defaultSeriesTypeId ?? ''} required>
          {options.seriesTypes.map(option => <option value={option.id} key={option.id}>{option.displayName}</option>)}
        </select></label>}
        <label className="media-request-checkbox"><input name="searchAfterAdd" type="checkbox" defaultChecked /> Börja söka efter filer direkt</label>
        </div></details>
        <button type="submit" disabled={busy}>Granska</button>
      </form>}
      {phase === 'review' && preview && <form className="media-request-review" onSubmit={event => void confirm(event)}>
        <h4>Det här händer när du bekräftar</h4>
        <p>Objektet läggs till i bibliotekshanteringen. {preview.summary.searchAfterAdd ? 'BigBrain börjar sedan söka efter tillgängliga filer. En eventuell nedladdning visas under Pågående och blir tillgänglig i Jellyfin efter lyckad nedladdning och import.' : 'Ingen filsökning startas automatiskt.'}</p>
        <p>Resultatet beror på vad som finns tillgängligt och kan därför inte garanteras.</p>
        <details className="media-request-technical"><summary>Tekniska detaljer</summary><dl>
          <div><dt>Provider</dt><dd>{preview.summary.provider}</dd></div>
          <div><dt>Root folder</dt><dd>{preview.summary.rootFolder}</dd></div>
          <div><dt>Quality</dt><dd>{preview.summary.qualityProfile}</dd></div>
          <div><dt>Monitoring</dt><dd>{preview.summary.monitoring}</dd></div>
          <div><dt>Search after add</dt><dd>{preview.summary.searchAfterAdd ? 'Yes' : 'No'}</dd></div>
        </dl></details>
        <button type="submit" disabled={busy}>
          {preview.summary.searchAfterAdd ? 'Bekräfta och börja söka' : 'Lägg till utan att söka'}
        </button>
      </form>}
      {phase === 'success' && created && <div className="media-request-success" role="status">
        <h4>Klart</h4><p>{created.title} har lagts till.</p>
        <button type="button" onClick={close}>Stäng</button>
      </div>}
    </div>
  </dialog>
}
