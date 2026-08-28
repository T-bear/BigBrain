import { useEffect,useState,type FormEvent } from 'react'
import { getAudiobookAcquisitionJob,getAudiobookAcquisitionJobs,getAudiobookAcquisitionStatus,getAudiobookLibrary,getAudiobookOverview,requestAudiobookAcquisition,searchAudiobooks } from '../api'
import { BBButton,BBEmptyState,BBInput,BBLoadingIndicator,BBMediaArtwork,BBSelect,BBSurface } from '../components'
import type { AudiobookAcquisitionCandidate,AudiobookAcquisitionJob,AudiobookAcquisitionProviderStatus,AudiobookItem,AudiobookMetadataResolution,AudiobookOverview } from '../types'

function Book({item,prominent=false,onOpen}:{item:AudiobookItem;prominent?:boolean;onOpen:(item:AudiobookItem)=>void}) {
  return <article className={`audiobook ${prominent?'audiobook--continue':''}`}>
    <BBMediaArtwork alt={`Omslag till ${item.title}`} loading="lazy" src={item.coverUrl??undefined}/>
    <div className="audiobook__copy"><h4>{item.title}</h4>{item.author&&<p>{item.author}</p>}
      <small>{[item.narrator?`Uppläsare ${item.narrator}`:null,item.languageLabel].filter(Boolean).join(' · ')}</small>
      {item.progressPercent!==null&&<><progress aria-label={`Lyssnat ${Math.round(item.progressPercent)} procent`} max="100" value={item.progressPercent}/><span className="audiobook__progress">{Math.round(item.progressPercent)} %</span></>}
      <BBButton onClick={()=>onOpen(item)} variant={prominent?'primary':'tertiary'}>{prominent?'Fortsätt lyssna':'Visa ljudbok'}</BBButton>
    </div>
  </article>
}

function ShelfBook({item,onOpen}:{item:AudiobookItem;onOpen:(item:AudiobookItem)=>void}) {
  return <button className="audiobook-shelf-book" onClick={()=>onOpen(item)} type="button"><BBMediaArtwork alt={`Omslag till ${item.title}`} loading="lazy" src={item.coverUrl??undefined}/><span><strong>{item.title}</strong>{item.author&&<small>{item.author}</small>}</span></button>
}

function Candidate({candidate,onView}:{candidate:AudiobookAcquisitionCandidate;onView:(candidate:AudiobookAcquisitionCandidate)=>void}) {
  const metadata=[candidate.narrator?`Uppläsare ${candidate.narrator}`:null,candidate.languageLabel,candidate.edition,candidate.publicationYear,candidate.provenance??candidate.source].filter(Boolean).join(' · ')
  return <BBSurface className="audiobook-candidate">
    <BBMediaArtwork alt={`Omslag till ${candidate.title}`} loading="lazy" src={candidate.coverUrl??undefined}/>
    <div className="audiobook__copy"><h4>{candidate.title}</h4>{candidate.author&&<p>{candidate.author}</p>}<small>{metadata}</small>
      <span className="audiobook-confidence">{candidate.languageConfidence==='verified'?'Verifierat språk':candidate.languageConfidence==='probable'?'Troligt språk':'Språk okänt'}</span>
      <div className="audiobook-candidate__actions"><BBButton onClick={()=>onView(candidate)} variant="secondary">Välj utgåva</BBButton></div>
    </div>
  </BBSurface>
}

function JobRows({jobs}:{jobs:AudiobookAcquisitionJob[]}){return <>{jobs.map(job=><div className="audiobook-job" key={job.id}><div><strong>{job.candidate.title}</strong><small>{job.candidate.languageLabel}</small>{job.message&&<small>{job.message}</small>}</div><span>{statusLabel(job.status)}</span></div>)}</>}
function Activity({jobs,total,onMore}:{jobs:AudiobookAcquisitionJob[];total:number;onMore:()=>void}) {
  if(!jobs.length)return null
  const active=jobs.filter(job=>!['completed','failed','cancelled'].includes(job.status)),attention=jobs.filter(job=>job.status==='failed'),history=jobs.filter(job=>['completed','cancelled'].includes(job.status))
  return <section aria-labelledby="audiobook-activity-heading" className="audiobook-activity"><div className="audiobook-section-title"><h3 id="audiobook-activity-heading">Hämtningar</h3><span>{active.length} pågår</span></div>
    {active.length>0&&<BBSurface className="audiobook-job-list"><JobRows jobs={active}/></BBSurface>}
    {attention.length>0&&<BBSurface className="audiobook-job-list audiobook-job-list--attention"><strong>Kräver åtgärd</strong><JobRows jobs={attention}/></BBSurface>}
    {(history.length>0||total>jobs.length)&&<details className="audiobook-history"><summary>Historik ({Math.max(history.length,total-active.length-attention.length)})</summary><BBSurface className="audiobook-job-list"><JobRows jobs={history}/>{total>jobs.length&&<BBButton onClick={onMore} variant="tertiary">Visa fler</BBButton>}</BBSurface></details>}
  </section>
}

function statusLabel(status:string){return ({requested:'Begärd',searching:'Söker',candidateFound:'Träff hittad',awaitingSelection:'Väntar på val',queued:'Köad',downloading:'Hämtas',importing:'Importeras',indexing:'Indexeras',completed:'Klar',failed:'Misslyckades',cancelled:'Avbruten'} as Record<string,string>)[status]??'Okänt tillstånd'}

export function Audiobooks(){
  const[data,setData]=useState<AudiobookOverview|null>(null)
  const[provider,setProvider]=useState<AudiobookAcquisitionProviderStatus|null>(null)
  const[jobs,setJobs]=useState<AudiobookAcquisitionJob[]>([])
  const[jobTotal,setJobTotal]=useState(0)
  const[library,setLibrary]=useState<AudiobookItem[]>([])
  const[libraryTotal,setLibraryTotal]=useState(0)
  const[libraryQuery,setLibraryQuery]=useState('')
  const[librarySort,setLibrarySort]=useState('recent')
  const[libraryBusy,setLibraryBusy]=useState(false)
  const[collectionOpen,setCollectionOpen]=useState(false)
  const[selected,setSelected]=useState<AudiobookItem|null>(null)
  const[selectedCandidate,setSelectedCandidate]=useState<AudiobookAcquisitionCandidate|null>(null)
  const[query,setQuery]=useState('')
  const[language,setLanguage]=useState('sv')
  const[localResults,setLocalResults]=useState<AudiobookItem[]|null>(null)
  const[metadata,setMetadata]=useState<AudiobookMetadataResolution|null>(null)
  const[discovery,setDiscovery]=useState<AudiobookAcquisitionCandidate[]|null>(null)
  const[notice,setNotice]=useState<string|null>(null)
  const[busy,setBusy]=useState(false)

  useEffect(()=>{const c=new AbortController()
    void getAudiobookOverview(c.signal).then(value=>{setData(value);setLibrary(value.library);setLibraryTotal(value.library.length)}).catch(()=>setData({state:'configuredUnavailable',message:'Ljudböcker kunde inte laddas.',continueListening:null,library:[],recent:[],acquisition:{state:'configuredUnavailable',canSearch:false,canRequest:false,message:null}}))
    void getAudiobookAcquisitionStatus(c.signal).then(setProvider).catch(()=>setProvider({state:'configuredUnavailable',provider:'unknown',canSearch:false,canRequest:false,canCancel:false,message:'Anskaffningsleverantören kunde inte nås.'}))
    void getAudiobookAcquisitionJobs(c.signal).then(activity=>{setJobs(activity.items);setJobTotal(activity.total)}).catch(()=>setJobs([]))
    return()=>c.abort()},[])

  useEffect(()=>{
    const active=jobs.filter(job=>!['completed','failed','cancelled'].includes(job.status)).slice(0,10)
    if(!active.length)return
    const timer=window.setInterval(()=>{void Promise.all(active.map(job=>getAudiobookAcquisitionJob(job.id).catch(()=>job))).then(updated=>{
      const completed=updated.some(item=>item.status==='completed'&&jobs.find(job=>job.id===item.id)?.status!=='completed')
      setJobs(current=>current.map(job=>updated.find(item=>item.id===job.id)??job))
      if(completed)void getAudiobookOverview().then(value=>{setData(value);setLibrary(value.library);setLibraryTotal(value.library.length)}).catch(()=>undefined)
    })},10000)
    return()=>window.clearInterval(timer)
  },[jobs])

  async function search(e:FormEvent){e.preventDefault();if(query.trim().length<2)return;setBusy(true);setNotice(null);try{const result=await searchAudiobooks(query,language);setLocalResults(result.library);setMetadata(result.metadata);setDiscovery(result.discovery);setProvider(result.acquisition)}catch{setNotice('Sökningen kunde inte genomföras. Försök igen.')}finally{setBusy(false)}}
  async function add(candidate:AudiobookAcquisitionCandidate){if(!provider?.canRequest){setNotice(provider?.message??'Automatisk hämtning är inte konfigurerad ännu.');return}setBusy(true);try{const job=await requestAudiobookAcquisition(candidate);setJobs(current=>[job,...current]);setSelectedCandidate(null);setNotice('Hämtningen har begärts.')}catch{setNotice('Hämtningen kunde inte begäras.')}finally{setBusy(false)}}
  async function findLibrary(e:FormEvent){e.preventDefault();setLibraryBusy(true);try{const page=await getAudiobookLibrary(0,24,libraryQuery,'');setLibrary(page.items);setLibraryTotal(page.total)}catch{setNotice('Biblioteket kunde inte filtreras. Försök igen.')}finally{setLibraryBusy(false)}}
  async function moreLibrary(){setLibraryBusy(true);try{const page=await getAudiobookLibrary(Math.floor(library.length/24),24,libraryQuery,'');setLibrary(current=>[...current,...page.items.filter(item=>!current.some(old=>old.id===item.id))]);setLibraryTotal(page.total)}finally{setLibraryBusy(false)}}
  async function moreJobs(){const page=await getAudiobookAcquisitionJobs(undefined,jobs.length,25);setJobs(current=>[...current,...page.items]);setJobTotal(page.total)}
  const visibleLibrary=[...library].sort((a,b)=>librarySort==='title'?a.title.localeCompare(b.title,'sv'):librarySort==='author'?(a.author??'').localeCompare(b.author??'','sv'):0)

  return <section aria-labelledby="audiobooks-heading" className="audiobooks-section"><div className="section-heading"><div><p className="eyebrow">Lyssna</p><h2 id="audiobooks-heading">Ljudböcker</h2></div></div>
    {!data&&<div className="bb-loading-state"><BBLoadingIndicator label="Laddar ljudböcker"/></div>}
    {data?.state==='notConfigured'&&<BBEmptyState title="Audiobookshelf väntar på konfigurering" detail="Biblioteket blir tillgängligt här när serveranslutningen är klar."/>}
    {data?.state==='configuredUnavailable'&&<BBEmptyState title="Ljudböcker är tillfälligt otillgängliga" detail={data.message??undefined}/>}
    {data?.continueListening&&<BBSurface className="audiobook-feature"><p className="eyebrow">Fortsätt lyssna</p><Book item={data.continueListening} onOpen={setSelected} prominent/></BBSurface>}
    {data?.state==='configuredHealthy'&&<><div className="audiobook-section-title"><h3>Ditt bibliotek</h3><span>{libraryTotal} ljudböcker</span></div>
      {!collectionOpen&&(data.recent.length||library.length)?<BBSurface className="audiobook-library-overview"><div className="audiobook-section-title"><h4>Senast tillagda</h4><BBButton onClick={()=>setCollectionOpen(true)} variant="tertiary">Visa alla</BBButton></div><div className="audiobook-shelf">{(data.recent.length?data.recent:library).slice(0,4).map(i=><ShelfBook item={i} key={i.id} onOpen={setSelected}/>)}</div></BBSurface>:null}
      {collectionOpen&&<section aria-labelledby="all-audiobooks-heading" className="audiobook-collection"><div className="audiobook-section-title"><h3 id="all-audiobooks-heading">Alla ljudböcker</h3><BBButton onClick={()=>setCollectionOpen(false)} variant="tertiary">Till översikten</BBButton></div><form className="audiobook-library-tools" onSubmit={findLibrary}><BBInput aria-label="Sök i biblioteket" onChange={e=>setLibraryQuery(e.target.value)} placeholder="Sök titel, författare eller serie" value={libraryQuery}/><BBSelect aria-label="Sortera bibliotek" onChange={e=>setLibrarySort(e.target.value)} value={librarySort}><option value="recent">Senast tillagda</option><option value="title">Titel</option><option value="author">Författare</option></BBSelect><BBButton busy={libraryBusy} type="submit">Filtrera</BBButton></form>{visibleLibrary.length?<><div className="audiobook-grid">{visibleLibrary.map(i=><Book item={i} key={i.id} onOpen={setSelected}/>)}</div>{library.length<libraryTotal&&<BBButton busy={libraryBusy} onClick={moreLibrary} variant="tertiary">Visa fler ljudböcker</BBButton>}</>:<BBEmptyState title="Inga ljudböcker matchar" detail="Ändra sökningen eller gå tillbaka till översikten."/>}</section>}
      {!collectionOpen&&!library.length&&<BBEmptyState title="Biblioteket är tomt" detail="Dina ljudböcker visas här när de har lagts till."/>}
      <form className="audiobook-search" onSubmit={search}><div className="audiobook-section-title"><label htmlFor="audiobook-query">Hitta ljudbok</label></div>
        <div><BBInput id="audiobook-query" minLength={2} maxLength={120} onChange={e=>setQuery(e.target.value)} placeholder="Titel, författare, serie eller ISBN" value={query}/><BBSelect aria-label="Föredraget språk" onChange={e=>setLanguage(e.target.value)} value={language}><option value="sv">Svenska</option><option value="en">Engelska</option><option value="all">Alla språk</option></BBSelect><BBButton busy={busy} type="submit" variant="secondary">Sök</BBButton></div>
      </form>
      {notice&&<p aria-live="polite" className="audiobook-notice">{notice}</p>}
      {metadata&&metadata.works.length>0&&<section aria-labelledby="audiobook-metadata-heading"><div className="audiobook-section-title"><h3 id="audiobook-metadata-heading">Bokträff</h3><span>Open Library</span></div><BBSurface className="audiobook-metadata">{metadata.works.slice(0,3).map(work=><article key={work.workId}>{work.coverUrl&&<img alt="" loading="lazy" src={work.coverUrl}/>}<div><strong>{work.canonicalTitle}</strong>{work.authors.length>0&&<p>{work.authors.join(', ')}</p>}<small>{[work.series?'Serie: '+work.series:null,work.publicationYear,work.isbn13?'ISBN '+work.isbn13:work.isbn10?'ISBN '+work.isbn10:null].filter(Boolean).join(' · ')}</small></div></article>)}</BBSurface></section>}
      {metadata?.state==='unavailable'&&<p className="audiobook-notice">{metadata.message}</p>}
      {localResults&&(localResults.length?<><h3>I biblioteket</h3><div className="audiobook-grid">{localResults.map(i=><Book item={i} key={i.id} onOpen={setSelected}/>)}</div></>:<BBEmptyState title="Ingen ljudbok hittades i biblioteket"/>)}
      {discovery&&discovery.length>0&&<><div className="audiobook-section-title"><h3>Kan läggas till</h3><span>{discovery.length} utgåvor</span></div><div className="audiobook-candidates">{discovery.map(i=><Candidate candidate={i} key={`${i.source}:${i.editionId}`} onView={setSelectedCandidate}/>)}</div></>}
      {provider&&!provider.canRequest&&<BBSurface className="audiobook-provider-note"><strong>{provider.state==='configuredUnavailable'?'Det går inte att lägga till just nu.':'Automatisk hämtning är inte konfigurerad ännu.'}</strong><span>{provider.state==='configuredUnavailable'?'Försök igen om en stund. Ditt bibliotek fungerar fortfarande.':'Du kan fortfarande söka och använda biblioteket.'}</span></BBSurface>}
      <Activity jobs={jobs} onMore={moreJobs} total={jobTotal}/>
    </>}
    {selected&&<div aria-modal="true" className="bb-dialog-backdrop" onClick={()=>setSelected(null)} role="dialog"><BBSurface aria-labelledby="audiobook-detail-title" className="audiobook-detail" onClick={e=>e.stopPropagation()}><BBButton aria-label="Stäng" className="audiobook-detail__close" onClick={()=>setSelected(null)} variant="icon">×</BBButton><BBMediaArtwork alt={`Omslag till ${selected.title}`} loading="lazy" src={selected.coverUrl??undefined}/><h2 id="audiobook-detail-title">{selected.title}</h2>{selected.author&&<p>{selected.author}</p>}{selected.series&&<p>Serie: {selected.series}</p>}{selected.narrator&&<p>Uppläsare: {selected.narrator}</p>}<p>{selected.languageLabel}{selected.publishedYear?` · ${selected.publishedYear}`:''}</p>{selected.progressPercent!==null&&<progress aria-label={`Lyssnat ${Math.round(selected.progressPercent)} procent`} max="100" value={selected.progressPercent}/>} {selected.description&&<p>{selected.description}</p>}{selected.playbackUrl&&<a className="bb-button bb-button--primary" href={selected.playbackUrl} rel="noreferrer">Spela ljudbok</a>}</BBSurface></div>}
    {selectedCandidate&&<div aria-modal="true" className="bb-dialog-backdrop" onClick={()=>setSelectedCandidate(null)} role="dialog"><BBSurface aria-describedby="audiobook-candidate-confirmation" aria-labelledby="audiobook-candidate-title" className="audiobook-detail" onClick={e=>e.stopPropagation()}><BBButton aria-label="Stäng" className="audiobook-detail__close" onClick={()=>setSelectedCandidate(null)} variant="icon">×</BBButton><p className="eyebrow">Vald utgåva</p><h2 id="audiobook-candidate-title">{selectedCandidate.title}</h2>{selectedCandidate.author&&<p>{selectedCandidate.author}</p>}{selectedCandidate.narrator&&<p>Uppläsare: {selectedCandidate.narrator}</p>}<p>{selectedCandidate.languageLabel}{selectedCandidate.edition?` · ${selectedCandidate.edition}`:''}{selectedCandidate.publicationYear?` · ${selectedCandidate.publicationYear}`:''}</p><p id="audiobook-candidate-confirmation">Lägg till den här utgåvan?</p><details><summary>Detaljer</summary><p>{selectedCandidate.provenance??selectedCandidate.source} · {selectedCandidate.languageConfidence==='verified'?'Verifierat språk':selectedCandidate.languageConfidence==='probable'?'Troligt språk':'Språk okänt'}</p></details><div className="bb-action-group"><BBButton onClick={()=>setSelectedCandidate(null)} variant="tertiary">Avbryt</BBButton><BBButton busy={busy} disabled={!provider?.canRequest} onClick={()=>add(selectedCandidate)} variant="primary">Lägg till</BBButton></div></BBSurface></div>}
  </section>
}
