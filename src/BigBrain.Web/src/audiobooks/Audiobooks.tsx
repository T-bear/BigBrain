import { useEffect, useRef, useState, type FormEvent } from 'react'
import { getAudiobook, getAudiobookAcquisitionJob, getAudiobookAcquisitionJobs, getAudiobookAcquisitionStatus, getAudiobookLibrary, getAudiobookOverview, getAudiobookPlaybackAvailability, requestAudiobookAcquisition, searchAudiobooks } from '../api'
import { BBButton, BBEmptyState, BBInput, BBLoadingIndicator, BBMediaArtwork, BBSelect, BBSurface } from '../components'
import type { AudiobookAcquisitionCandidate, AudiobookAcquisitionJob, AudiobookAcquisitionProviderStatus, AudiobookItem, AudiobookMetadataResolution, AudiobookOverview, AudiobookPlaybackAvailability } from '../types'
import { focusRouteHeading } from '../routeFocus'
import { useAudiobookPlayback } from './AudiobookPlayback'

type Route = { kind: 'overview' } | { kind: 'collection' } | { kind: 'detail'; id: string }
const collectionPath = '/media/audiobooks'
const hiddenHistoryKey = 'bigbrain.audiobooks.hidden-history.v1'
const hiddenAttentionKey = 'bigbrain.audiobooks.hidden-attention.v1'
const terminalStatuses = ['completed', 'failed', 'cancelled']

function readRoute(): Route {
  const detail = window.location.pathname.match(/^\/media\/audiobooks\/([A-Za-z0-9_-]{1,128})\/?$/)
  if (detail) return { kind: 'detail', id: detail[1] }
  return window.location.pathname.replace(/\/$/, '') === collectionPath ? { kind: 'collection' } : { kind: 'overview' }
}
function notifyRoute() { window.dispatchEvent(new Event('bb:navigation')) }
function statusLabel(status: string) { return ({ requested: 'Begärd', searching: 'Söker', candidateFound: 'Träff hittad', awaitingSelection: 'Väntar på val', queued: 'Köad', downloading: 'Hämtas', importing: 'Importeras', indexing: 'Indexeras', completed: 'Klar', failed: 'Misslyckades', cancelled: 'Avbruten' } as Record<string, string>)[status] ?? 'Okänt tillstånd' }
function knownLanguage(item: Pick<AudiobookItem, 'language' | 'languageLabel'>) { return item.language !== 'und' && item.languageLabel !== 'Språk okänt' ? item.languageLabel : null }
function formatTime(seconds:number){const safe=Math.max(0,Math.floor(seconds||0));const hours=Math.floor(safe/3600);const minutes=Math.floor((safe%3600)/60);const secs=safe%60;return hours?`${hours}:${String(minutes).padStart(2,'0')}:${String(secs).padStart(2,'0')}`:`${minutes}:${String(secs).padStart(2,'0')}`}

function Book({ item, onOpen }: { item: AudiobookItem; onOpen: (item: AudiobookItem) => void }) {
  const details = [item.narrator ? `Uppläsare ${item.narrator}` : null, knownLanguage(item)].filter(Boolean).join(' · ')
  return <button aria-label={`Öppna ${item.title}`} className="audiobook audiobook-book-row" onClick={() => onOpen(item)} type="button"><BBMediaArtwork alt="" loading="lazy" src={item.coverUrl ?? undefined} /><span className="audiobook__copy"><h3>{item.title}</h3>{item.author && <span>{item.author}</span>}{details && <small>{details}</small>}{item.progressPercent !== null && <><progress aria-label={`Lyssnat ${Math.round(item.progressPercent)} procent`} max="100" value={item.progressPercent} /><span className="audiobook__progress">{Math.round(item.progressPercent)} %</span></>}</span><span aria-hidden="true" className="audiobook-book-row__chevron">›</span></button>
}

function CompactBook({ item, onOpen, onPlayback, playing, playable }: { item: AudiobookItem; onOpen: (item: AudiobookItem) => void; onPlayback: (item:AudiobookItem)=>void; playing:boolean; playable:boolean }) {
  const current=item.durationSeconds!==null&&item.progressPercent!==null?item.durationSeconds*item.progressPercent/100:null
  return <div className="audiobook-compact-book"><button aria-label={`Öppna ${item.title}`} className="audiobook-compact-book__identity" onClick={() => onOpen(item)} type="button"><BBMediaArtwork alt="" loading="lazy" src={item.coverUrl ?? undefined} /><span><strong>{item.title}</strong>{item.author && <small>{item.author}</small>}{item.progressPercent !== null && <progress aria-label={`Lyssnat ${Math.round(item.progressPercent)} procent`} max="100" value={item.progressPercent} />}{current!==null&&item.durationSeconds!==null&&<small className="audiobook-compact-book__time">{formatTime(current)} / {formatTime(item.durationSeconds)}</small>}</span></button>{playable?<BBButton aria-label={playing?`Pausa ${item.title}`:`Spela ${item.title}`} aria-pressed={playing} className="audiobook-compact-book__play" onClick={()=>onPlayback(item)} variant="primary">{playing?'❚❚':'▶'}</BBButton>:<span aria-hidden="true" className="audiobook-compact-book__chevron">›</span>}</div>
}

function Candidate({ candidate, onView }: { candidate: AudiobookAcquisitionCandidate; onView: (candidate: AudiobookAcquisitionCandidate) => void }) {
  const details = [candidate.narrator ? `Uppläsare ${candidate.narrator}` : null, candidate.languageLabel, candidate.edition, candidate.publicationYear, candidate.provenance ?? candidate.source].filter(Boolean).join(' · ')
  return <BBSurface className="audiobook-candidate"><BBMediaArtwork alt={`Omslag till ${candidate.title}`} loading="lazy" src={candidate.coverUrl ?? undefined} /><div className="audiobook__copy"><h3>{candidate.title}</h3>{candidate.author && <p>{candidate.author}</p>}<small>{details}</small>{candidate.languageConfidence === 'probable' && <span className="audiobook-confidence">Troligt {candidate.languageLabel.toLocaleLowerCase('sv')}</span>}<BBButton onClick={() => onView(candidate)} variant="secondary">Välj utgåva</BBButton></div></BBSurface>
}

function readHidden(key = hiddenHistoryKey) {
  try {
    const value = JSON.parse(window.localStorage.getItem(key) ?? '[]')
    return Array.isArray(value) ? value.filter(item => typeof item === 'string').slice(-200) : []
  } catch { return [] }
}

function Activity({ jobs, total, onMore }: { jobs: AudiobookAcquisitionJob[]; total: number; onMore: () => void }) {
  const [hiddenHistory, setHiddenHistory] = useState<string[]>(() => readHidden())
  const [hiddenAttention, setHiddenAttention] = useState<string[]>(() => readHidden(hiddenAttentionKey))
  if (!jobs.length) return null
  const active = jobs.filter(job => !terminalStatuses.includes(job.status))
  const allAttention = jobs.filter(job => job.status === 'failed')
  const attention = allAttention.filter(job => !hiddenAttention.includes(job.id))
  const history = jobs.filter(job => ['completed', 'cancelled'].includes(job.status) && !hiddenHistory.includes(job.id))
  const historyTotal = Math.max(jobs.filter(job => ['completed', 'cancelled'].includes(job.status)).length, total - active.length - allAttention.length)
  const hideHistory = () => {
    const next = [...new Set([...hiddenHistory, ...history.map(job => job.id)])].slice(-200)
    window.localStorage.setItem(hiddenHistoryKey, JSON.stringify(next)); setHiddenHistory(next)
  }
  const dismissAttention = (id: string) => {
    const next = [...new Set([...hiddenAttention, id])].slice(-200)
    window.localStorage.setItem(hiddenAttentionKey, JSON.stringify(next)); setHiddenAttention(next)
  }
  return <section aria-labelledby="audiobook-downloads-heading" className="audiobook-activity"><h2 id="audiobook-downloads-heading">Hämtningar</h2>{active.length > 0 && <section aria-labelledby="active-downloads-heading" className="audiobook-job-group"><h3 id="active-downloads-heading">Aktiva</h3>{active.map(job => <div className="audiobook-job" key={job.id}><strong>{job.candidate.title}</strong><span>{statusLabel(job.status)}</span></div>)}</section>}{attention.length > 0 && <BBSurface className="audiobook-job-group audiobook-job-list--attention"><h3>Kräver åtgärd</h3>{attention.map(job => <div className="audiobook-job" key={job.id}><strong>{job.candidate.title}</strong><span>{statusLabel(job.status)}</span>{job.message && <small>{job.message}</small>}<BBButton aria-label={`Dölj ${job.candidate.title} från åtgärdslistan`} onClick={() => dismissAttention(job.id)} variant="tertiary">Dölj</BBButton></div>)}</BBSurface>}{historyTotal > 0 && <details className="audiobook-history"><summary>Historik ({historyTotal})</summary>{history.map(job => <div className="audiobook-job" key={job.id}><strong>{job.candidate.title}</strong><span>{statusLabel(job.status)}</span></div>)}{history.length === 0 && <p>Visad historik är dold på den här enheten.</p>}<div className="bb-action-group">{total > jobs.length && <BBButton onClick={onMore} variant="tertiary">Visa fler</BBButton>}{history.length > 0 && <BBButton onClick={hideHistory} variant="tertiary">Dölj visad historik</BBButton>}</div><small>Endast presentationen rensas. BigBrains audit- och jobbdata bevaras.</small></details>}</section>
}

function Discovery({ busy, discovery, language, metadata, notice, provider, query, onLanguage, onQuery, onSearch, onView }: { busy: boolean; discovery: AudiobookAcquisitionCandidate[] | null; language: string; metadata: AudiobookMetadataResolution | null; notice: string | null; provider: AudiobookAcquisitionProviderStatus | null; query: string; onLanguage: (value: string) => void; onQuery: (value: string) => void; onSearch: (event: FormEvent) => void; onView: (candidate: AudiobookAcquisitionCandidate) => void }) {
  return <section aria-labelledby="new-audiobook-heading" className="audiobook-discovery"><h2 id="new-audiobook-heading">Hitta ljudbok</h2><form className="audiobook-search" onSubmit={onSearch}><div><BBInput aria-label="Sök efter en ny ljudbok" id="audiobook-query" minLength={2} maxLength={120} onChange={event => onQuery(event.target.value)} placeholder="Titel, författare, serie eller ISBN" value={query} /><BBSelect aria-label="Föredraget språk" onChange={event => onLanguage(event.target.value)} value={language}><option value="sv">Svenska</option><option value="en">Engelska</option><option value="all">Alla språk</option></BBSelect><BBButton busy={busy} type="submit" variant="secondary">Sök</BBButton></div></form>{notice && <p aria-live="polite" className="audiobook-notice">{notice}</p>}{metadata?.works.length ? <section aria-labelledby="audiobook-metadata-heading"><h3 id="audiobook-metadata-heading">Bokträff</h3><BBSurface className="audiobook-metadata">{metadata.works.slice(0, 3).map(work => <article key={work.workId}><div><strong>{work.canonicalTitle}</strong>{work.authors.length > 0 && <p>{work.authors.join(', ')}</p>}{work.series && <small>Serie: {work.series}</small>}</div></article>)}</BBSurface></section> : null}{discovery?.length ? <><div className="audiobook-section-title"><h3>Kan läggas till</h3><span>{discovery.length} utgåvor</span></div><div className="audiobook-candidates">{discovery.map(item => <Candidate candidate={item} key={`${item.source}:${item.editionId}`} onView={onView} />)}</div></> : null}{provider && !provider.canRequest && <BBSurface className="audiobook-provider-note"><strong>{provider.state === 'configuredUnavailable' ? 'Det går inte att lägga till just nu.' : 'Automatisk hämtning är inte konfigurerad ännu.'}</strong></BBSurface>}</section>
}

export function Audiobooks() {
  const playback = useAudiobookPlayback()
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
  const [showScrollTop, setShowScrollTop] = useState(false)
  const [playbackAvailability,setPlaybackAvailability]=useState<AudiobookPlaybackAvailability|null>(null)
  const headingRef = useRef<HTMLHeadingElement>(null)
  const collectionLoaded = useRef(false)

  const navigate = (path: string, next: Route, item?: AudiobookItem) => { window.history.replaceState({ ...window.history.state, bbAudiobookOrigin: true, scrollY: window.scrollY }, ''); window.history.pushState({ bbAudiobook: true, scrollY: 0 }, '', path); setDirection('forward'); setRoute(next); if (item) setSelected(item); notifyRoute(); window.requestAnimationFrame(() => window.scrollTo({ top: 0, behavior: 'auto' })) }
  const goBack = (fallback: string, next: Route) => { setDirection('back'); if (window.history.state?.bbAudiobook || window.history.state?.bbAudiobookOrigin) window.history.back(); else { window.history.replaceState({}, '', fallback); setRoute(next); notifyRoute() } }
  const openDetail = (item: AudiobookItem) => navigate(`${collectionPath}/${encodeURIComponent(item.id)}`, { kind: 'detail', id: item.id }, item)

  useEffect(() => { const pop = () => { setDirection('back'); setRoute(readRoute()); notifyRoute(); window.requestAnimationFrame(() => window.scrollTo({ top: Number(window.history.state?.scrollY ?? 0) })) }; window.addEventListener('popstate', pop); return () => window.removeEventListener('popstate', pop) }, [])
  useEffect(() => { focusRouteHeading(headingRef.current) }, [route, selected])
  useEffect(() => {
    const controller = new AbortController()
    void getAudiobookOverview(controller.signal).then(value => { setData(value); setLibrary(value.library); setLibraryTotal(value.library.length) }).catch(() => setData({ state: 'configuredUnavailable', message: 'Ljudböcker kunde inte laddas.', continueListening: null, library: [], recent: [], acquisition: { state: 'configuredUnavailable', canSearch: false, canRequest: false, message: null } }))
    void getAudiobookPlaybackAvailability(controller.signal).then(setPlaybackAvailability).catch(()=>setPlaybackAvailability({state:'configuredUnavailable',message:'BigBrains spelare kunde inte verifieras.',separateIdentity:false,hasProgress:false}))
    void getAudiobookAcquisitionStatus(controller.signal).then(setProvider).catch(() => setProvider({ state: 'configuredUnavailable', provider: 'unknown', canSearch: false, canRequest: false, canCancel: false, message: 'Anskaffningsleverantören kunde inte nås.' }))
    void getAudiobookAcquisitionJobs(controller.signal).then(value => { setJobs(value.items); setJobTotal(value.total) }).catch(() => setJobs([]))
    return () => controller.abort()
  }, [])
  useEffect(() => {
    if (route.kind !== 'collection' || collectionLoaded.current) return
    collectionLoaded.current = true
    const controller = new AbortController(); setLibraryBusy(true)
    void getAudiobookLibrary(0, 24, '', '', controller.signal).then(page => { setLibrary(page.items); setLibraryTotal(page.total) }).catch(() => setNotice('Biblioteket kunde inte laddas.')).finally(() => setLibraryBusy(false))
    return () => controller.abort()
  }, [route])
  useEffect(() => {
    if (route.kind !== 'detail' || selected?.id === route.id) return
    const cached = library.find(item => item.id === route.id)
    if (cached) { setSelected(cached); return }
    const controller = new AbortController(); setDetailBusy(true)
    void getAudiobook(route.id, controller.signal).then(setSelected).catch(() => setNotice('Ljudboken kunde inte öppnas.')).finally(() => setDetailBusy(false))
    return () => controller.abort()
  }, [route, selected, library])
  useEffect(() => {
    const active = jobs.filter(job => !terminalStatuses.includes(job.status)).slice(0, 10)
    if (!active.length) return
    const timer = window.setInterval(() => { void Promise.all(active.map(job => getAudiobookAcquisitionJob(job.id).catch(() => job))).then(updated => { const completed = updated.some(item => item.status === 'completed' && jobs.find(job => job.id === item.id)?.status !== 'completed'); setJobs(current => current.map(job => updated.find(item => item.id === job.id) ?? job)); if (completed) void getAudiobookOverview().then(value => { setData(value); setLibrary(value.library); setLibraryTotal(value.library.length) }).catch(() => undefined) }) }, 10000)
    return () => window.clearInterval(timer)
  }, [jobs])
  useEffect(() => {
    if (route.kind !== 'collection') { setShowScrollTop(false); return }
    const update = () => setShowScrollTop(window.scrollY > 600)
    update(); window.addEventListener('scroll', update, { passive: true }); return () => window.removeEventListener('scroll', update)
  }, [route])

  async function search(event: FormEvent) { event.preventDefault(); if (query.trim().length < 2) return; setBusy(true); setNotice(null); try { const result = await searchAudiobooks(query, language); setLocalResults(result.library); setMetadata(result.metadata); setDiscovery(result.discovery); setProvider(result.acquisition) } catch { setNotice('Sökningen kunde inte genomföras. Försök igen.') } finally { setBusy(false) } }
  async function add(candidate: AudiobookAcquisitionCandidate) { if (!provider?.canRequest) return; setBusy(true); try { const job = await requestAudiobookAcquisition(candidate); setJobs(current => [job, ...current]); setSelectedCandidate(null); setNotice('Hämtningen har begärts.') } catch { setNotice('Hämtningen kunde inte begäras.') } finally { setBusy(false) } }
  async function findLibrary(event: FormEvent) { event.preventDefault(); setLibraryBusy(true); try { const page = await getAudiobookLibrary(0, 24, libraryQuery, ''); setLibrary(page.items); setLibraryTotal(page.total) } catch { setNotice('Biblioteket kunde inte filtreras.') } finally { setLibraryBusy(false) } }
  async function moreLibrary() { setLibraryBusy(true); try { const page = await getAudiobookLibrary(Math.floor(library.length / 24), 24, libraryQuery, ''); setLibrary(current => [...current, ...page.items.filter(item => !current.some(old => old.id === item.id))]); setLibraryTotal(page.total) } finally { setLibraryBusy(false) } }
  async function moreJobs() { const page = await getAudiobookAcquisitionJobs(undefined, jobs.length, 25); setJobs(current => [...current, ...page.items]); setJobTotal(page.total) }
  const scrollToTop = () => window.scrollTo({ top: 0, behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth' })
  const visibleLibrary = [...library].sort((a, b) => librarySort === 'title' ? a.title.localeCompare(b.title, 'sv') : librarySort === 'author' ? (a.author ?? '').localeCompare(b.author ?? '', 'sv') : 0)
  const playable=playbackAvailability?.state==='configuredHealthy'
  const startPlayback=async(item:AudiobookItem)=>{setNotice(null);try{if(playback.item?.id===item.id&&playback.session)await playback.toggle();else await playback.start(item)}catch{setNotice('BigBrains spelare kunde inte starta den här ljudboken. Använd reservvägen till Audiobookshelf.') }}

  if (route.kind === 'overview') return <section aria-labelledby="audiobooks-heading" className="audiobooks-section audiobook-overview"><header><span className="eyebrow">Lyssna</span><h2 className="audiobook-overview__title" id="audiobooks-heading">Ljudböcker</h2></header>{!data && <BBLoadingIndicator label="Laddar ljudböcker" />}{data?.state === 'notConfigured' && <BBEmptyState title="Audiobookshelf väntar på konfigurering" />}{data?.state === 'configuredUnavailable' && <BBEmptyState title="Ljudböcker är tillfälligt otillgängliga" detail={data.message ?? undefined} />}{data?.continueListening && <BBSurface className="audiobook-continue-strip"><h3>Fortsätt lyssna</h3><CompactBook item={data.continueListening} onOpen={openDetail} onPlayback={item=>void startPlayback(item)} playable={playable} playing={playback.item?.id===data.continueListening.id&&playback.playing}/></BBSurface>}{notice&&<p aria-live="polite" className="audiobook-notice">{notice}</p>}<a aria-label="Öppna ljudboksbiblioteket" className="audiobook-overview__heading audiobook-collection-link" href={collectionPath} onClick={event => { event.preventDefault(); navigate(collectionPath, { kind: 'collection' }) }}><span>Bibliotek</span><span aria-hidden="true">›</span></a></section>

  if (route.kind === 'detail') return <section className={`audiobooks-section audiobook-route-view audiobook-route-view--${direction}`}><BBButton className="audiobook-route-back" onClick={() => goBack(collectionPath, { kind: 'collection' })} variant="tertiary">‹ Ljudböcker</BBButton>{detailBusy && <BBLoadingIndicator label="Öppnar ljudbok" />}{selected && <article className="audiobook-detail-page"><div className="audiobook-detail-page__hero"><BBMediaArtwork alt={`Omslag till ${selected.title}`} className="audiobook-detail-page__artwork" loading="eager" src={selected.coverUrl ?? undefined} /><div className="audiobook-detail-page__summary"><p className="eyebrow">Ljudbok</p><h1 ref={headingRef} tabIndex={-1}>{selected.title}</h1>{selected.author && <p className="audiobook-detail-page__author">{selected.author}</p>}<div className="audiobook-detail-page__metadata">{selected.series && <span>Serie: {selected.series}</span>}{selected.narrator && <span>Uppläsare: {selected.narrator}</span>}{knownLanguage(selected) && <span>{knownLanguage(selected)}</span>}{selected.publishedYear && <span>{selected.publishedYear}</span>}</div></div></div><div className="audiobook-detail-page__body">{playable&&<BBButton aria-label={playback.item?.id===selected.id&&playback.playing?'Pausa ljudboken':'Spela ljudboken'} aria-pressed={playback.item?.id===selected.id&&playback.playing} onClick={()=>void startPlayback(selected)} variant="primary">{playback.item?.id===selected.id&&playback.playing?'Pausa':selected.progressPercent&&selected.progressPercent>0?'Fortsätt lyssna':'Spela'}</BBButton>}{playbackAvailability&&!playable&&<BBSurface className="audiobook-playback-unavailable"><strong>BigBrains spelare är inte tillgänglig för den här ljudboken.</strong><span>{playbackAvailability.message??'Playback kunde inte verifieras.'}</span></BBSurface>}{notice&&<p aria-live="polite" className="audiobook-notice">{notice}</p>}{selected.progressPercent !== null && <progress aria-label={`Lyssnat ${Math.round(selected.progressPercent)} procent`} max="100" value={selected.progressPercent} />}{selected.description && <p>{selected.description}</p>}{selected.playbackUrl && <a className={`bb-button bb-button--${playable?'tertiary':'primary'}`} href={selected.playbackUrl} rel="noreferrer">Öppna i Audiobookshelf (reservväg)</a>}</div></article>}{!selected && !detailBusy && notice && <BBEmptyState title={notice} />}</section>

  return <section aria-labelledby="all-audiobooks-heading" className={`audiobooks-section audiobook-route-view audiobook-route-view--${direction}`}>
    <header className="audiobook-route-header"><BBButton className="audiobook-route-back" onClick={() => goBack('/', { kind: 'overview' })} variant="tertiary">‹ Media</BBButton><div><h1 id="all-audiobooks-heading" ref={headingRef} tabIndex={-1}>Ljudböcker</h1><small>{libraryTotal} i biblioteket</small></div></header>
    {data?.continueListening && <BBSurface className="audiobook-continue-strip"><h2>Fortsätt lyssna</h2><CompactBook item={data.continueListening} onOpen={openDetail} onPlayback={item=>void startPlayback(item)} playable={playable} playing={playback.item?.id===data.continueListening.id&&playback.playing}/></BBSurface>}
    <Discovery busy={busy} discovery={discovery} language={language} metadata={metadata} notice={notice} provider={provider} query={query} onLanguage={setLanguage} onQuery={setQuery} onSearch={search} onView={setSelectedCandidate} />
    {localResults?.length ? <section aria-labelledby="local-search-heading"><h2 id="local-search-heading">Finns i ditt bibliotek</h2><div className="audiobook-grid">{localResults.map(item => <Book item={item} key={item.id} onOpen={openDetail} />)}</div></section> : null}
    <section aria-labelledby="owned-audiobooks-heading" className="audiobook-owned-library"><div className="audiobook-section-title"><h2 id="owned-audiobooks-heading">Bibliotek</h2><small>{libraryTotal} ljudböcker</small></div><form className="audiobook-library-tools" onSubmit={findLibrary}><BBInput aria-label="Sök i ditt bibliotek" onChange={event => setLibraryQuery(event.target.value)} placeholder="Titel, författare eller serie" value={libraryQuery} /><BBSelect aria-label="Sortera bibliotek" onChange={event => setLibrarySort(event.target.value)} value={librarySort}><option value="recent">Senast tillagda</option><option value="title">Titel</option><option value="author">Författare</option></BBSelect><BBButton busy={libraryBusy} type="submit">Filtrera</BBButton></form>{visibleLibrary.length ? <><div className="audiobook-grid">{visibleLibrary.map(item => <Book item={item} key={item.id} onOpen={openDetail} />)}</div>{library.length < libraryTotal && <BBButton busy={libraryBusy} onClick={moreLibrary} variant="tertiary">Visa fler ljudböcker</BBButton>}</> : <BBEmptyState title="Biblioteket är tomt" />}</section>
    <Activity jobs={jobs} onMore={moreJobs} total={jobTotal} />
    {showScrollTop && <BBButton aria-label="Till början av ljudboksbiblioteket" className="audiobook-scroll-top" onClick={scrollToTop} variant="secondary">↑</BBButton>}
    {selectedCandidate && <div aria-labelledby="audiobook-confirm-title" aria-modal="true" className="bb-dialog-backdrop" role="dialog"><BBSurface className="audiobook-detail"><h2 id="audiobook-confirm-title">{selectedCandidate.title}</h2><p>Lägg till den här utgåvan?</p><div className="bb-action-group"><BBButton onClick={() => setSelectedCandidate(null)} variant="tertiary">Avbryt</BBButton><BBButton busy={busy} disabled={!provider?.canRequest} onClick={() => add(selectedCandidate)} variant="primary">Lägg till</BBButton></div></BBSurface></div>}
  </section>
}
