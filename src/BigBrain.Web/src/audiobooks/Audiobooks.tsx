import { useEffect, useRef, useState, type FormEvent } from 'react'
import { getAudiobook, getAudiobookAcquisitionJob, getAudiobookAcquisitionJobs, getAudiobookAcquisitionStatus, getAudiobookLibrary, getAudiobookOverview, requestAudiobookAcquisition, searchAudiobooks } from '../api'
import { BBButton, BBEmptyState, BBInput, BBLoadingIndicator, BBMediaArtwork, BBSelect, BBSurface } from '../components'
import type { AudiobookAcquisitionCandidate, AudiobookAcquisitionJob, AudiobookAcquisitionProviderStatus, AudiobookItem, AudiobookMetadataResolution, AudiobookOverview } from '../types'

type Route = { kind: 'overview' } | { kind: 'collection' } | { kind: 'detail'; id: string }
const collectionPath = '/media/audiobooks'
function readRoute(): Route {
  const detail = window.location.pathname.match(/^\/media\/audiobooks\/([A-Za-z0-9_-]{1,128})\/?$/)
  if (detail) return { kind: 'detail', id: detail[1] }
  return window.location.pathname.replace(/\/$/, '') === collectionPath ? { kind: 'collection' } : { kind: 'overview' }
}
function notifyRoute() { window.dispatchEvent(new Event('bb:navigation')) }
function statusLabel(status: string) { return ({ requested: 'Begärd', searching: 'Söker', candidateFound: 'Träff hittad', awaitingSelection: 'Väntar på val', queued: 'Köad', downloading: 'Hämtas', importing: 'Importeras', indexing: 'Indexeras', completed: 'Klar', failed: 'Misslyckades', cancelled: 'Avbruten' } as Record<string, string>)[status] ?? 'Okänt tillstånd' }

function Book({ item, onOpen }: { item: AudiobookItem; onOpen: (item: AudiobookItem) => void }) {
  return <article className="audiobook"><BBMediaArtwork alt={`Omslag till ${item.title}`} loading="lazy" src={item.coverUrl ?? undefined} /><div className="audiobook__copy"><h3>{item.title}</h3>{item.author && <p>{item.author}</p>}<small>{[item.narrator ? `Uppläsare ${item.narrator}` : null, item.languageLabel].filter(Boolean).join(' · ')}</small>{item.progressPercent !== null && <><progress aria-label={`Lyssnat ${Math.round(item.progressPercent)} procent`} max="100" value={item.progressPercent} /><span className="audiobook__progress">{Math.round(item.progressPercent)} %</span></>}<BBButton onClick={() => onOpen(item)} variant="tertiary">Visa ljudbok</BBButton></div></article>
}

function CompactBook({ item, onOpen }: { item: AudiobookItem; onOpen: (item: AudiobookItem) => void }) {
  return <button aria-label={`Öppna ${item.title}`} className="audiobook-compact-book" onClick={() => onOpen(item)} type="button"><BBMediaArtwork alt="" loading="lazy" src={item.coverUrl ?? undefined} /><span><strong>{item.title}</strong>{item.author && <small>{item.author}</small>}{item.progressPercent !== null && <progress aria-label={`Lyssnat ${Math.round(item.progressPercent)} procent`} max="100" value={item.progressPercent} />}</span></button>
}

function Candidate({ candidate, onView }: { candidate: AudiobookAcquisitionCandidate; onView: (candidate: AudiobookAcquisitionCandidate) => void }) {
  const metadata = [candidate.narrator ? `Uppläsare ${candidate.narrator}` : null, candidate.languageLabel, candidate.edition, candidate.publicationYear, candidate.provenance ?? candidate.source].filter(Boolean).join(' · ')
  return <BBSurface className="audiobook-candidate"><BBMediaArtwork alt={`Omslag till ${candidate.title}`} loading="lazy" src={candidate.coverUrl ?? undefined} /><div className="audiobook__copy"><h3>{candidate.title}</h3>{candidate.author && <p>{candidate.author}</p>}<small>{metadata}</small><span className="audiobook-confidence">{candidate.languageConfidence === 'verified' ? 'Verifierat språk' : candidate.languageConfidence === 'probable' ? 'Troligt språk' : 'Språk okänt'}</span><BBButton onClick={() => onView(candidate)} variant="secondary">Välj utgåva</BBButton></div></BBSurface>
}

function Activity({ jobs, total, onMore }: { jobs: AudiobookAcquisitionJob[]; total: number; onMore: () => void }) {
  if (!jobs.length) return null
  const active = jobs.filter(job => !['completed', 'failed', 'cancelled'].includes(job.status))
  const attention = jobs.filter(job => job.status === 'failed')
  const history = jobs.filter(job => ['completed', 'cancelled'].includes(job.status))
  return <section className="audiobook-activity"><h2>Hämtningar</h2>{active.map(job => <div className="audiobook-job" key={job.id}><strong>{job.candidate.title}</strong><span>{statusLabel(job.status)}</span></div>)}{attention.length > 0 && <BBSurface><h3>Kräver åtgärd</h3>{attention.map(job => <div className="audiobook-job" key={job.id}><strong>{job.candidate.title}</strong><span>{statusLabel(job.status)}</span></div>)}</BBSurface>}{history.length > 0 && <details><summary>Historik ({Math.max(history.length, total - active.length - attention.length)})</summary>{history.map(job => <div className="audiobook-job" key={job.id}><strong>{job.candidate.title}</strong><span>{statusLabel(job.status)}</span></div>)}{total > jobs.length && <BBButton onClick={onMore} variant="tertiary">Visa fler</BBButton>}</details>}</section>
}

export function Audiobooks() {
  const [data, setData] = useState<AudiobookOverview | null>(null)
  const [provider, setProvider] = useState<AudiobookAcquisitionProviderStatus | null>(null)
  const [jobs, setJobs] = useState<AudiobookAcquisitionJob[]>([])
  const [jobTotal, setJobTotal] = useState(0)
  const [library, setLibrary] = useState<AudiobookItem[]>([])
  const [libraryTotal, setLibraryTotal] = useState(0)
  const [libraryQuery, setLibraryQuery] = useState('')
  const [librarySort, setLibrarySort] = useState('recent')
  const [libraryBusy, setLibraryBusy] = useState(false)
  const [route, setRoute] = useState<Route>(readRoute)
  const [selected, setSelected] = useState<AudiobookItem | null>(null)
  const [detailBusy, setDetailBusy] = useState(false)
  const [direction, setDirection] = useState<'forward' | 'back'>('forward')
  const [selectedCandidate, setSelectedCandidate] = useState<AudiobookAcquisitionCandidate | null>(null)
  const [query, setQuery] = useState('')
  const [language, setLanguage] = useState('sv')
  const [localResults, setLocalResults] = useState<AudiobookItem[] | null>(null)
  const [metadata, setMetadata] = useState<AudiobookMetadataResolution | null>(null)
  const [discovery, setDiscovery] = useState<AudiobookAcquisitionCandidate[] | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const headingRef = useRef<HTMLHeadingElement>(null)

  const navigate = (path: string, next: Route, item?: AudiobookItem) => {
    window.history.replaceState({ ...window.history.state, bbAudiobookOrigin: true, scrollY: window.scrollY }, '')
    window.history.pushState({ bbAudiobook: true }, '', path)
    setDirection('forward'); setRoute(next); if (item) setSelected(item); notifyRoute()
  }
  const goBack = (fallback: string, next: Route) => {
    setDirection('back')
    if (window.history.state?.bbAudiobook || window.history.state?.bbAudiobookOrigin) window.history.back()
    else { window.history.replaceState({}, '', fallback); setRoute(next); notifyRoute() }
  }
  const openDetail = (item: AudiobookItem) => navigate(`${collectionPath}/${encodeURIComponent(item.id)}`, { kind: 'detail', id: item.id }, item)

  useEffect(() => {
    const pop = () => { setDirection('back'); setRoute(readRoute()); notifyRoute(); window.requestAnimationFrame(() => window.scrollTo({ top: Number(window.history.state?.scrollY ?? 0) })) }
    window.addEventListener('popstate', pop); return () => window.removeEventListener('popstate', pop)
  }, [])
  useEffect(() => { headingRef.current?.focus() }, [route])
  useEffect(() => {
    const controller = new AbortController()
    void getAudiobookOverview(controller.signal).then(value => { setData(value); setLibrary(value.library); setLibraryTotal(value.library.length) }).catch(() => setData({ state: 'configuredUnavailable', message: 'Ljudböcker kunde inte laddas.', continueListening: null, library: [], recent: [], acquisition: { state: 'configuredUnavailable', canSearch: false, canRequest: false, message: null } }))
    void getAudiobookAcquisitionStatus(controller.signal).then(setProvider).catch(() => setProvider({ state: 'configuredUnavailable', provider: 'unknown', canSearch: false, canRequest: false, canCancel: false, message: 'Anskaffningsleverantören kunde inte nås.' }))
    void getAudiobookAcquisitionJobs(controller.signal).then(value => { setJobs(value.items); setJobTotal(value.total) }).catch(() => setJobs([]))
    return () => controller.abort()
  }, [])
  useEffect(() => {
    if (route.kind !== 'detail' || selected?.id === route.id) return
    const cached = library.find(item => item.id === route.id)
    if (cached) { setSelected(cached); return }
    const controller = new AbortController(); setDetailBusy(true)
    void getAudiobook(route.id, controller.signal).then(setSelected).catch(() => setNotice('Ljudboken kunde inte öppnas.')).finally(() => setDetailBusy(false))
    return () => controller.abort()
  }, [route, selected, library])
  useEffect(() => {
    const active = jobs.filter(job => !['completed', 'failed', 'cancelled'].includes(job.status)).slice(0, 10)
    if (!active.length) return
    const timer = window.setInterval(() => { void Promise.all(active.map(job => getAudiobookAcquisitionJob(job.id).catch(() => job))).then(updated => { const completed = updated.some(item => item.status === 'completed' && jobs.find(job => job.id === item.id)?.status !== 'completed'); setJobs(current => current.map(job => updated.find(item => item.id === job.id) ?? job)); if (completed) void getAudiobookOverview().then(value => { setData(value); setLibrary(value.library); setLibraryTotal(value.library.length) }).catch(() => undefined) }) }, 10000)
    return () => window.clearInterval(timer)
  }, [jobs])

  async function search(event: FormEvent) { event.preventDefault(); if (query.trim().length < 2) return; setBusy(true); setNotice(null); try { const result = await searchAudiobooks(query, language); setLocalResults(result.library); setMetadata(result.metadata); setDiscovery(result.discovery); setProvider(result.acquisition) } catch { setNotice('Sökningen kunde inte genomföras. Försök igen.') } finally { setBusy(false) } }
  async function add(candidate: AudiobookAcquisitionCandidate) { if (!provider?.canRequest) return; setBusy(true); try { const job = await requestAudiobookAcquisition(candidate); setJobs(current => [job, ...current]); setSelectedCandidate(null); setNotice('Hämtningen har begärts.') } catch { setNotice('Hämtningen kunde inte begäras.') } finally { setBusy(false) } }
  async function findLibrary(event: FormEvent) { event.preventDefault(); setLibraryBusy(true); try { const page = await getAudiobookLibrary(0, 24, libraryQuery, ''); setLibrary(page.items); setLibraryTotal(page.total) } catch { setNotice('Biblioteket kunde inte filtreras.') } finally { setLibraryBusy(false) } }
  async function moreLibrary() { setLibraryBusy(true); try { const page = await getAudiobookLibrary(Math.floor(library.length / 24), 24, libraryQuery, ''); setLibrary(current => [...current, ...page.items.filter(item => !current.some(old => old.id === item.id))]); setLibraryTotal(page.total) } finally { setLibraryBusy(false) } }
  async function moreJobs() { const page = await getAudiobookAcquisitionJobs(undefined, jobs.length, 25); setJobs(current => [...current, ...page.items]); setJobTotal(page.total) }
  const visibleLibrary = [...library].sort((a, b) => librarySort === 'title' ? a.title.localeCompare(b.title, 'sv') : librarySort === 'author' ? (a.author ?? '').localeCompare(b.author ?? '', 'sv') : 0)

  if (route.kind === 'overview') return <section aria-labelledby="audiobooks-heading" className="audiobooks-section audiobook-overview"><div className="audiobook-overview__heading"><div><p className="eyebrow">Lyssna</p><h2 id="audiobooks-heading">Ljudböcker</h2></div><BBButton aria-label={`Öppna ljudboksbiblioteket, ${libraryTotal} ljudböcker`} className="audiobook-collection-link" onClick={() => navigate(collectionPath, { kind: 'collection' })} variant="tertiary"><span>{libraryTotal} ljudböcker</span><span aria-hidden="true">›</span></BBButton></div>{!data && <BBLoadingIndicator label="Laddar ljudböcker" />}{data?.state === 'notConfigured' && <BBEmptyState title="Audiobookshelf väntar på konfigurering" />}{data?.state === 'configuredUnavailable' && <BBEmptyState title="Ljudböcker är tillfälligt otillgängliga" detail={data.message ?? undefined} />}{data?.continueListening ? <BBSurface className="audiobook-continue-strip"><h3>Fortsätt lyssna</h3><CompactBook item={data.continueListening} onOpen={openDetail} /></BBSurface> : data?.state === 'configuredHealthy' && <p className="audiobook-overview__quiet">Ingen påbörjad ljudbok just nu.</p>}</section>

  if (route.kind === 'detail') return <section className={`audiobooks-section audiobook-route-view audiobook-route-view--${direction}`}><BBButton className="audiobook-route-back" onClick={() => goBack(collectionPath, { kind: 'collection' })} variant="tertiary">‹ Ljudböcker</BBButton>{detailBusy && <BBLoadingIndicator label="Öppnar ljudbok" />}{selected && <article className="audiobook-detail-page"><BBMediaArtwork alt={`Omslag till ${selected.title}`} loading="eager" src={selected.coverUrl ?? undefined} /><div><p className="eyebrow">Ljudbok</p><h1 ref={headingRef} tabIndex={-1}>{selected.title}</h1>{selected.author && <p>{selected.author}</p>}{selected.series && <p>Serie: {selected.series}</p>}{selected.narrator && <p>Uppläsare: {selected.narrator}</p>}<p>{selected.languageLabel}{selected.publishedYear ? ` · ${selected.publishedYear}` : ''}</p>{selected.progressPercent !== null && <progress aria-label={`Lyssnat ${Math.round(selected.progressPercent)} procent`} max="100" value={selected.progressPercent} />}{selected.description && <p>{selected.description}</p>}{selected.playbackUrl && <a className="bb-button bb-button--primary" href={selected.playbackUrl} rel="noreferrer">Spela ljudbok</a>}</div></article>}{!selected && !detailBusy && notice && <BBEmptyState title={notice} />}</section>

  return <section aria-labelledby="all-audiobooks-heading" className={`audiobooks-section audiobook-route-view audiobook-route-view--${direction}`}><header className="audiobook-route-header"><BBButton className="audiobook-route-back" onClick={() => goBack('/', { kind: 'overview' })} variant="tertiary">‹ Media</BBButton><div><p className="eyebrow">Bibliotek</p><h1 id="all-audiobooks-heading" ref={headingRef} tabIndex={-1}>Ljudböcker</h1><span>{libraryTotal} ljudböcker</span></div></header>{data?.continueListening && <BBSurface className="audiobook-continue-strip"><h2>Fortsätt lyssna</h2><CompactBook item={data.continueListening} onOpen={openDetail} /></BBSurface>}<form className="audiobook-library-tools" onSubmit={findLibrary}><BBInput aria-label="Sök i biblioteket" onChange={event => setLibraryQuery(event.target.value)} placeholder="Sök titel, författare eller serie" value={libraryQuery} /><BBSelect aria-label="Sortera bibliotek" onChange={event => setLibrarySort(event.target.value)} value={librarySort}><option value="recent">Senast tillagda</option><option value="title">Titel</option><option value="author">Författare</option></BBSelect><BBButton busy={libraryBusy} type="submit">Filtrera</BBButton></form>{visibleLibrary.length ? <><div className="audiobook-grid">{visibleLibrary.map(item => <Book item={item} key={item.id} onOpen={openDetail} />)}</div>{library.length < libraryTotal && <BBButton busy={libraryBusy} onClick={moreLibrary} variant="tertiary">Visa fler ljudböcker</BBButton>}</> : <BBEmptyState title="Biblioteket är tomt" />}
    <form className="audiobook-search" onSubmit={search}><label htmlFor="audiobook-query">Hitta ljudbok</label><div><BBInput id="audiobook-query" minLength={2} maxLength={120} onChange={event => setQuery(event.target.value)} placeholder="Titel, författare, serie eller ISBN" value={query} /><BBSelect aria-label="Föredraget språk" onChange={event => setLanguage(event.target.value)} value={language}><option value="sv">Svenska</option><option value="en">Engelska</option><option value="all">Alla språk</option></BBSelect><BBButton busy={busy} type="submit" variant="secondary">Sök</BBButton></div></form>{notice && <p aria-live="polite" className="audiobook-notice">{notice}</p>}{metadata?.works.length ? <section aria-labelledby="audiobook-metadata-heading"><h2 id="audiobook-metadata-heading">Bokträff</h2><BBSurface className="audiobook-metadata">{metadata.works.slice(0, 3).map(work => <article key={work.workId}><div><strong>{work.canonicalTitle}</strong>{work.authors.length > 0 && <p>{work.authors.join(', ')}</p>}{work.series && <small>Serie: {work.series}</small>}</div></article>)}</BBSurface></section> : null}{localResults?.length ? <><h2>I biblioteket</h2><div className="audiobook-grid">{localResults.map(item => <Book item={item} key={item.id} onOpen={openDetail} />)}</div></> : null}{discovery?.length ? <><div className="audiobook-section-title"><h2>Kan läggas till</h2><span>{discovery.length} utgåvor</span></div><div className="audiobook-candidates">{discovery.map(item => <Candidate candidate={item} key={`${item.source}:${item.editionId}`} onView={setSelectedCandidate} />)}</div></> : null}{provider && !provider.canRequest && <BBSurface className="audiobook-provider-note"><strong>{provider.state === 'configuredUnavailable' ? 'Det går inte att lägga till just nu.' : 'Automatisk hämtning är inte konfigurerad ännu.'}</strong></BBSurface>}<Activity jobs={jobs} onMore={moreJobs} total={jobTotal} />{selectedCandidate && <div aria-modal="true" className="bb-dialog-backdrop" role="dialog"><BBSurface className="audiobook-detail"><h2>{selectedCandidate.title}</h2><p>Lägg till den här utgåvan?</p><div className="bb-action-group"><BBButton onClick={() => setSelectedCandidate(null)} variant="tertiary">Avbryt</BBButton><BBButton busy={busy} disabled={!provider?.canRequest} onClick={() => add(selectedCandidate)} variant="primary">Lägg till</BBButton></div></BBSurface></div>}</section>
}
