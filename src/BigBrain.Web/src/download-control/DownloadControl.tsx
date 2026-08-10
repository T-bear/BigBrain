import { useCallback, useEffect, useRef, useState } from 'react'
import { ApiError, getDownloads, operateDownload, operateDownloadsBatch, previewDownloadRemoval, removeDownload } from '../api'
import type { DownloadOperation, DownloadRemovalPreview, DownloadStatus, DownloadSummary } from '../types'

const labels: Record<DownloadStatus, string> = {
  active: 'Aktiv', queued: 'Köad', paused: 'Pausad', error: 'Fel', completed: 'Klar', unknown: 'Okänd',
}
const filters: Array<{ value: 'all' | DownloadStatus; label: string }> = [
  { value: 'all', label: 'Alla' }, { value: 'error', label: 'Fel' }, { value: 'active', label: 'Aktiva' },
  { value: 'queued', label: 'Köade' }, { value: 'paused', label: 'Pausade' }, { value: 'completed', label: 'Klara' },
]
const groups: Array<{ id: string; label: string; statuses: DownloadStatus[] }> = [
  { id: 'problems', label: 'Fel och problem', statuses: ['error', 'unknown'] },
  { id: 'active', label: 'Aktiva', statuses: ['active'] },
  { id: 'waiting', label: 'Köade och pausade', statuses: ['queued', 'paused'] },
  { id: 'completed', label: 'Klara', statuses: ['completed'] },
]

function bytes(value: number, suffix = '') {
  const units = ['B', 'KiB', 'MiB', 'GiB', 'TiB']
  let amount = Math.max(0, value); let unit = 0
  while (amount >= 1024 && unit < units.length - 1) { amount /= 1024; unit += 1 }
  return `${amount.toFixed(unit ? 1 : 0)} ${units[unit]}${suffix}`
}

function safeError(error: unknown) {
  if (!(error instanceof ApiError)) return 'Åtgärden kunde inte slutföras.'
  switch (error.code) {
    case 'downloadNotFound': return 'Nedladdningen finns inte längre.'
    case 'downloadIdentityChanged': return 'Nedladdningen ändrades. Uppdatera listan och försök igen.'
    case 'downloadRemovalConflict': return 'En borttagning pågår redan.'
    case 'destructiveRemovalNotAllowed': case 'sharedPathRisk': return 'Data kan inte raderas säkert. Du kan fortfarande avbryta och bevara filerna.'
    case 'confirmationExpired': return 'Bekräftelsen har löpt ut. Förhandsgranska igen.'
    case 'providerTimeout': return 'qBittorrent svarade inte i tid.'
    case 'providerAuthenticationFailure': return 'qBittorrent-autentiseringen misslyckades.'
    default: return 'qBittorrent-åtgärden kunde inte slutföras.'
  }
}

export function DownloadControl() {
  const [downloads, setDownloads] = useState<DownloadSummary[]>([])
  const [filter, setFilter] = useState<'all' | DownloadStatus>('all')
  const [selected, setSelected] = useState<DownloadSummary | null>(null)
  const [preview, setPreview] = useState<DownloadRemovalPreview | null>(null)
  const [deleteData, setDeleteData] = useState(false)
  const [understood, setUnderstood] = useState(false)
  const [busy, setBusy] = useState(false)
  const [message, setMessage] = useState('')
  const [checked, setChecked] = useState<Set<string>>(new Set())
  const [completedExpanded, setCompletedExpanded] = useState(false)
  const controller = useRef<AbortController | null>(null)
  const busyRef = useRef(false)
  const dialogRef = useRef<HTMLDivElement | null>(null)

  const load = useCallback(async () => {
    controller.current?.abort(); const next = new AbortController(); controller.current = next
    try { const response = await getDownloads(next.signal); setDownloads(Array.isArray(response.downloads) ? response.downloads : []); setMessage('') }
    catch (error) { if (!(error instanceof Error) || error.name !== 'AbortError') setMessage(safeError(error)) }
  }, [])

  useEffect(() => { void load(); return () => controller.current?.abort() }, [load])
  useEffect(() => {
    if (!selected) return
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && !busy) { setSelected(null); setPreview(null) }
      if (event.key !== 'Tab' || !dialogRef.current) return
      const focusable = [...dialogRef.current.querySelectorAll<HTMLElement>('button:not(:disabled),input:not(:disabled)')]
      if (!focusable.length) return
      const first = focusable[0]; const last = focusable[focusable.length - 1]
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus() }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus() }
    }
    document.addEventListener('keydown', onKey); dialogRef.current?.querySelector<HTMLElement>('button')?.focus()
    return () => document.removeEventListener('keydown', onKey)
  }, [selected, busy])

  const prepare = async (destructive: boolean) => {
    if (!selected || busyRef.current) return
    busyRef.current = true; setBusy(true); setMessage('')
    try { setPreview(await previewDownloadRemoval(selected.id, destructive)); setDeleteData(destructive); setUnderstood(false) }
    catch (error) { setMessage(safeError(error)) }
    finally { busyRef.current = false; setBusy(false) }
  }
  const confirm = async () => {
    if (!selected || !preview || busyRef.current || (deleteData && !understood)) return
    busyRef.current = true; setBusy(true); setMessage('')
    try {
      await removeDownload(selected.id, preview.confirmationToken, deleteData)
      setDownloads(items => items.filter(item => item.id !== selected.id))
      setSelected(null); setPreview(null); setMessage(deleteData ? 'Nedladdningen och dess data togs bort.' : 'Nedladdningen avbröts. Filerna bevarades.')
    } catch (error) { setMessage(safeError(error)) }
    finally { busyRef.current = false; setBusy(false) }
  }
  const visible = filter === 'all' ? downloads : downloads.filter(item => item.status === filter)
  const visibleGroups = groups
    .map(group => ({ ...group, items: visible.filter(item => group.statuses.includes(item.status)) }))
    .filter(group => group.items.length > 0)
  const applyOperation = async (item: DownloadSummary, operation: DownloadOperation) => {
    if (busyRef.current) return
    busyRef.current = true; setBusy(true); setMessage('')
    try { await operateDownload(item.id, operation); await load(); setMessage(operation === 'pause' ? 'Nedladdningen pausades.' : operation === 'resume' ? 'Nedladdningen återupptogs.' : 'Ett nytt försök skickades.') }
    catch (error) { setMessage(safeError(error)) }
    finally { busyRef.current = false; setBusy(false) }
  }
  const applyBatch = async (operation: DownloadOperation) => {
    if (busyRef.current || checked.size === 0) return
    busyRef.current = true; setBusy(true); setMessage('')
    try {
      const result = await operateDownloadsBatch([...checked], operation)
      const succeeded = result.results.filter(item => item.status === 'succeeded' || item.status === 'alreadyInDesiredState').length
      const failed = result.results.length - succeeded
      await load(); setChecked(new Set()); setMessage(`${succeeded} av ${result.results.length} behandlades.${failed ? ` ${failed} kunde inte behandlas.` : ''}`)
    } catch (error) { setMessage(safeError(error)) }
    finally { busyRef.current = false; setBusy(false) }
  }

  const downloadItem = (item: DownloadSummary) => <li key={item.id}>
    <label className="download-select"><input type="checkbox" checked={checked.has(item.id)} disabled={busy} onChange={event => setChecked(current => { const next = new Set(current); event.target.checked ? next.add(item.id) : next.delete(item.id); return next })} aria-label={`Markera ${item.name}`} /><span className="sr-only">Markera {item.name}</span></label>
    <div><strong>{item.name}</strong><span>{labels[item.status]} · {item.category}{item.ownership === 'sonarr' || item.ownership === 'radarr' ? ` · Hanteras av ${item.ownership === 'sonarr' ? 'Sonarr' : 'Radarr'}` : ''}</span></div>
    <progress aria-label={`Framsteg för ${item.name}`} max="100" value={item.progressPercent} />
    <div className="download-metrics"><span>{item.progressPercent.toFixed(1)} %</span><span>{bytes(item.sizeBytes)}</span><span>↓ {bytes(item.downloadSpeedBytesPerSecond, '/s')}</span><span>↑ {bytes(item.uploadSpeedBytesPerSecond, '/s')}</span>{item.queuePosition !== null && <span>Kö {item.queuePosition}</span>}</div>
    <div className="download-row-actions">{item.capabilities.canPause && <button type="button" disabled={busy} onClick={() => void applyOperation(item, 'pause')}>Pausa {item.name}</button>}{item.capabilities.canResume && <button type="button" disabled={busy} onClick={() => void applyOperation(item, 'resume')}>Återuppta {item.name}</button>}{item.capabilities.canRetry && <button type="button" disabled={busy} onClick={() => void applyOperation(item, 'retry')}>Försök igen {item.name}</button>}<button type="button" className="secondary-button" disabled={busy} onClick={() => { setSelected(item); setPreview(null); setDeleteData(false); setMessage('') }}>Hantera {item.name}</button></div>
    <details className="download-diagnosis"><summary>Varför laddar den inte ner?</summary><strong>{item.diagnosis.explanation}</strong><ul>{item.diagnosis.verifiedObservations.map(observation => <li key={observation}>{observation}</li>)}</ul></details>
  </li>

  return <section className="download-control" aria-labelledby="downloads-heading">
    <header><div><p className="eyebrow">Hantera det som laddas ner just nu</p><h3 id="downloads-heading">Nedladdningskö</h3><p className="download-intro">Pausa, återuppta, felsök eller hantera själva nedladdningen. Mediets väg till biblioteket visas i Medieflöde.</p></div><button className="secondary-button" type="button" disabled={busy} onClick={() => void load()}>Uppdatera nedladdningar</button></header>
    <div className="download-filters" aria-label="Filtrera nedladdningar">{filters.map(item => <button type="button" aria-pressed={filter === item.value} key={item.value} onClick={() => setFilter(item.value)}>{item.label}</button>)}</div>
    <div className="download-selection"><button type="button" className="secondary-button" disabled={visible.length === 0 || busy} onClick={() => setChecked(current => { const next = new Set(current); visible.forEach(item => next.add(item.id)); return next })}>Markera alla i aktuell vy</button><button type="button" className="secondary-button" disabled={checked.size === 0 || busy} onClick={() => setChecked(new Set())}>Avmarkera alla</button><span aria-live="polite">{checked.size} markerade</span></div>
    {checked.size > 0 && <div className="download-batch" aria-label="Åtgärder för markerade nedladdningar"><button type="button" disabled={busy} onClick={() => void applyBatch('pause')}>Pausa markerade</button><button type="button" disabled={busy} onClick={() => void applyBatch('resume')}>Återuppta markerade</button><button type="button" disabled={busy} onClick={() => void applyBatch('retry')}>Försök igen markerade</button></div>}
    <p aria-live="polite" className="download-message">{message}</p>
    {visible.length === 0 ? <p>Inga nedladdningar i det här filtret.</p> : <div className="download-groups">{visibleGroups.map(group => {
      const collapsible = group.id === 'completed' && filter === 'all'
      const expanded = !collapsible || completedExpanded
      const contentId = `download-group-${group.id}`
      return <section className={`download-group download-group--${group.id}`} aria-labelledby={`${contentId}-heading`} key={group.id}>
        <header><h4 id={`${contentId}-heading`}>{group.label} <span>{group.items.length}</span></h4>{collapsible && <button aria-controls={contentId} aria-expanded={expanded} className="secondary-button download-group__toggle" type="button" onClick={() => setCompletedExpanded(value => !value)}>{expanded ? 'Dölj klara' : `Visa ${group.items.length} klara`}</button>}</header>
        {expanded && <ul className="download-list" id={contentId}>{group.items.map(downloadItem)}</ul>}
      </section>
    })}</div>}
    {selected && <div className="download-dialog" role="dialog" aria-modal="true" aria-labelledby="download-dialog-title" ref={dialogRef}>
      <button type="button" className="download-dialog__close" disabled={busy} onClick={() => { setSelected(null); setPreview(null) }} aria-label="Stäng dialog">×</button>
      <h3 id="download-dialog-title">{preview ? (deleteData ? 'Avbryt och radera nedladdade data?' : 'Avbryt nedladdning?') : selected.name}</h3>
      {!preview ? <>
        <p>{labels[selected.status]} · {selected.progressPercent.toFixed(1)} % · {bytes(selected.downloadedBytes)} nedladdat</p>
        {selected.warnings.map(warning => <p className="notice notice--warning" key={warning}>{warning}</p>)}
        <button type="button" disabled={busy} onClick={() => void prepare(false)}>Avbryt nedladdning</button>
        <button type="button" className="danger-button" disabled={busy || !selected.destructiveRemovalAllowed} onClick={() => void prepare(true)}>Avbryt och radera data</button>
      </> : <>
        <p>{deleteData ? 'Åtgärden kan inte ångras från BigBrain. qBittorrent instrueras att radera torrentens data. Annan media får inte påverkas.' : 'Torrentjobbet tas bort från qBittorrent. Redan nedladdade filer bevaras.'}</p>
        {preview.warnings.map(warning => <p className="notice notice--warning" key={warning}>{warning}</p>)}
        {deleteData && <label className="download-understand"><input type="checkbox" checked={understood} onChange={event => setUnderstood(event.target.checked)} /> Jag förstår att nedladdade data raderas.</label>}
        <div className="download-dialog__actions"><button type="button" className="secondary-button" disabled={busy} onClick={() => setPreview(null)}>Tillbaka</button><button type="button" className={deleteData ? 'danger-button' : ''} disabled={busy || (deleteData && !understood)} onClick={() => void confirm()}>{busy ? 'Arbetar…' : 'Bekräfta'}</button></div>
      </>}
    </div>}
  </section>
}
