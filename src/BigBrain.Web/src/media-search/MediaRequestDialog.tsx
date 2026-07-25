import { useEffect, useRef, useState, type FormEvent, type KeyboardEvent } from 'react'
import { ApiError, confirmMediaRequest, getMediaAddOptions, previewMediaRequest } from '../api'
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
  const firstControl = useRef<HTMLSelectElement>(null)
  const confirming = useRef(false)

  useEffect(() => {
    const controller = new AbortController()
    void getMediaAddOptions(result.mediaType, controller.signal)
      .then(value => { setOptions(value); setBusy(false) })
      .catch(requestError => { setError(friendlyError(requestError)); setBusy(false) })
    return () => controller.abort()
  }, [result.mediaType])

  useEffect(() => {
    if (options && phase === 'options' && !busy) firstControl.current?.focus()
  }, [options, phase, busy])

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
      const response = await confirmMediaRequest(preview.requestToken, crypto.randomUUID())
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
      <header><div><p className="eyebrow">Controlled media request</p><h3 id="media-request-title">{result.title}</h3></div>
        <button type="button" aria-label="Close media request" onClick={close} disabled={busy}>×</button>
      </header>
      <p>BigBrain will only send this request to {result.provider} after you review and confirm it.</p>
      {busy && <p aria-live="polite">Loading request step…</p>}
      {error && <p className="notice notice--error" role="alert">{error}</p>}
      {phase === 'options' && options && <form className="media-request-form" onSubmit={event => void previewRequest(event)}>
        <label>Root folder<select ref={firstControl} name="rootFolderId" defaultValue={options.defaultRootFolderId ?? ''} required>
          {options.rootFolders.map(option => <option value={option.id} key={option.id}>{option.displayName}</option>)}
        </select></label>
        <label>Quality profile<select name="qualityProfileId" defaultValue={options.defaultQualityProfileId ?? ''} required>
          {options.qualityProfiles.map(option => <option value={option.id} key={option.id}>{option.displayName}</option>)}
        </select></label>
        <label>Monitoring<select name="monitor" defaultValue={options.defaultMonitoringOptionId} required>
          {options.monitoringOptions.map(option => <option value={option.id} key={option.id}>{option.displayName}</option>)}
        </select></label>
        {result.mediaType === 'series' && <label>Series type<select name="seriesType" defaultValue={options.defaultSeriesTypeId ?? ''} required>
          {options.seriesTypes.map(option => <option value={option.id} key={option.id}>{option.displayName}</option>)}
        </select></label>}
        <label className="media-request-checkbox"><input name="searchAfterAdd" type="checkbox" defaultChecked={options.defaultSearchAfterAdd} /> Search after adding</label>
        <button type="submit" disabled={busy}>Review addition</button>
      </form>}
      {phase === 'review' && preview && <form className="media-request-review" onSubmit={event => void confirm(event)}>
        <h4>Review before adding</h4>
        <dl>
          <div><dt>Provider</dt><dd>{preview.summary.provider}</dd></div>
          <div><dt>Root folder</dt><dd>{preview.summary.rootFolder}</dd></div>
          <div><dt>Quality</dt><dd>{preview.summary.qualityProfile}</dd></div>
          <div><dt>Monitoring</dt><dd>{preview.summary.monitoring}</dd></div>
          <div><dt>Search after add</dt><dd>{preview.summary.searchAfterAdd ? 'Yes' : 'No'}</dd></div>
        </dl>
        <button type="submit" disabled={busy}>
          {result.mediaType === 'series' ? 'Add series to Sonarr' : 'Add movie to Radarr'}
        </button>
      </form>}
      {phase === 'success' && created && <div className="media-request-success" role="status">
        <h4>Request completed</h4><p>{created.title} was added to {created.provider}.</p>
        <button type="button" onClick={close}>Close</button>
      </div>}
    </div>
  </dialog>
}
