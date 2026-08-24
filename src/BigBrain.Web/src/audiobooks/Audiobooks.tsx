import { useEffect,useState,type FormEvent } from 'react'
import { getAudiobookAcquisitionJob,getAudiobookAcquisitionJobs,getAudiobookAcquisitionStatus,getAudiobookOverview,requestAudiobookAcquisition,searchAudiobooks } from '../api'
import { BBButton,BBEmptyState,BBInput,BBSelect,BBSurface } from '../components'
import type { AudiobookAcquisitionCandidate,AudiobookAcquisitionJob,AudiobookAcquisitionProviderStatus,AudiobookItem,AudiobookOverview } from '../types'

function Book({item,prominent=false,onOpen}:{item:AudiobookItem;prominent?:boolean;onOpen:(item:AudiobookItem)=>void}) {
  return <article className={`audiobook ${prominent?'audiobook--continue':''}`}>
    {item.coverUrl?<img alt="" loading="lazy" src={item.coverUrl}/>:<div aria-hidden="true" className="audiobook__cover-placeholder"/>}
    <div className="audiobook__copy"><h4>{item.title}</h4>{item.author&&<p>{item.author}</p>}
      <small>{[item.narrator?`Uppläsare ${item.narrator}`:null,item.languageLabel].filter(Boolean).join(' · ')}</small>
      {item.progressPercent!==null&&<><progress aria-label={`Lyssnat ${Math.round(item.progressPercent)} procent`} max="100" value={item.progressPercent}/><span className="audiobook__progress">{Math.round(item.progressPercent)} %</span></>}
      {prominent&&item.playbackUrl?<a className="bb-button bb-button--primary" href={item.playbackUrl} rel="noreferrer">Fortsätt i Audiobookshelf</a>:<BBButton onClick={()=>onOpen(item)} variant={prominent?'primary':'tertiary'}>{prominent?'Visa lyssning':'Visa ljudbok'}</BBButton>}
    </div>
  </article>
}

function Candidate({candidate,onView}:{candidate:AudiobookAcquisitionCandidate;onView:(candidate:AudiobookAcquisitionCandidate)=>void}) {
  const metadata=[candidate.narrator?`Uppläsare ${candidate.narrator}`:null,candidate.languageLabel,candidate.edition,candidate.publicationYear,candidate.provenance??candidate.source].filter(Boolean).join(' · ')
  return <BBSurface className="audiobook-candidate">
    {candidate.coverUrl?<img alt="" loading="lazy" src={candidate.coverUrl}/>:<div aria-hidden="true" className="audiobook__cover-placeholder"/>}
    <div className="audiobook__copy"><h4>{candidate.title}</h4>{candidate.author&&<p>{candidate.author}</p>}<small>{metadata}</small>
      <span className="audiobook-confidence">{candidate.languageConfidence==='verified'?'Verifierat språk':candidate.languageConfidence==='probable'?'Troligt språk':'Språk okänt'}</span>
      <div className="audiobook-candidate__actions"><BBButton onClick={()=>onView(candidate)} variant="secondary">Välj utgåva</BBButton></div>
    </div>
  </BBSurface>
}

function Activity({jobs}:{jobs:AudiobookAcquisitionJob[]}) {
  if(!jobs.length)return null
  return <section aria-labelledby="audiobook-activity-heading" className="audiobook-activity"><div className="audiobook-section-title"><h3 id="audiobook-activity-heading">Hämtningar</h3><span>{jobs.length}</span></div>
    <BBSurface>{jobs.map(job=><div className="audiobook-job" key={job.id}><div><strong>{job.candidate.title}</strong><small>{job.candidate.languageLabel} · {job.candidate.provenance??job.candidate.source}</small>{job.message&&<small>{job.message}</small>}</div><span>{statusLabel(job.status)}</span></div>)}</BBSurface>
  </section>
}

function statusLabel(status:string){return ({requested:'Begärd',searching:'Söker',candidateFound:'Träff hittad',awaitingSelection:'Väntar på val',queued:'Köad',downloading:'Hämtas',importing:'Importeras',indexing:'Indexeras',completed:'Klar',failed:'Misslyckades',cancelled:'Avbruten'} as Record<string,string>)[status]??'Okänt tillstånd'}

export function Audiobooks(){
  const[data,setData]=useState<AudiobookOverview|null>(null)
  const[provider,setProvider]=useState<AudiobookAcquisitionProviderStatus|null>(null)
  const[jobs,setJobs]=useState<AudiobookAcquisitionJob[]>([])
  const[selected,setSelected]=useState<AudiobookItem|null>(null)
  const[selectedCandidate,setSelectedCandidate]=useState<AudiobookAcquisitionCandidate|null>(null)
  const[query,setQuery]=useState('')
  const[author,setAuthor]=useState('')
  const[language,setLanguage]=useState('sv')
  const[localResults,setLocalResults]=useState<AudiobookItem[]|null>(null)
  const[discovery,setDiscovery]=useState<AudiobookAcquisitionCandidate[]|null>(null)
  const[notice,setNotice]=useState<string|null>(null)
  const[busy,setBusy]=useState(false)

  useEffect(()=>{const c=new AbortController()
    void getAudiobookOverview(c.signal).then(setData).catch(()=>setData({state:'configuredUnavailable',message:'Ljudböcker kunde inte laddas.',continueListening:null,library:[],recent:[],acquisition:{state:'configuredUnavailable',canSearch:false,canRequest:false,message:null}}))
    void getAudiobookAcquisitionStatus(c.signal).then(setProvider).catch(()=>setProvider({state:'configuredUnavailable',provider:'unknown',canSearch:false,canRequest:false,canCancel:false,message:'Anskaffningsleverantören kunde inte nås.'}))
    void getAudiobookAcquisitionJobs(c.signal).then(activity=>setJobs(activity.items)).catch(()=>setJobs([]))
    return()=>c.abort()},[])

  useEffect(()=>{
    const active=jobs.filter(job=>!['completed','failed','cancelled'].includes(job.status)).slice(0,10)
    if(!active.length)return
    const timer=window.setInterval(()=>{void Promise.all(active.map(job=>getAudiobookAcquisitionJob(job.id).catch(()=>job))).then(updated=>{
      const completed=updated.some(item=>item.status==='completed'&&jobs.find(job=>job.id===item.id)?.status!=='completed')
      setJobs(current=>current.map(job=>updated.find(item=>item.id===job.id)??job))
      if(completed)void getAudiobookOverview().then(setData).catch(()=>undefined)
    })},10000)
    return()=>window.clearInterval(timer)
  },[jobs])

  async function search(e:FormEvent){e.preventDefault();if(query.trim().length<2)return;setBusy(true);setNotice(null);try{const result=await searchAudiobooks(query,language,author);setLocalResults(result.library);setDiscovery(result.discovery);setProvider(result.acquisition)}catch{setNotice('Sökningen kunde inte genomföras. Försök igen.')}finally{setBusy(false)}}
  async function add(candidate:AudiobookAcquisitionCandidate){if(!provider?.canRequest){setNotice(provider?.message??'Automatisk hämtning är inte konfigurerad ännu.');return}setBusy(true);try{const job=await requestAudiobookAcquisition(candidate);setJobs(current=>[job,...current]);setSelectedCandidate(null);setNotice('Hämtningen har begärts.')}catch{setNotice('Hämtningen kunde inte begäras.')}finally{setBusy(false)}}

  return <section aria-labelledby="audiobooks-heading" className="audiobooks-section"><div className="section-heading"><div><p className="eyebrow">Lyssna</p><h2 id="audiobooks-heading">Ljudböcker</h2></div></div>
    {!data&&<p aria-live="polite">Laddar ljudböcker…</p>}
    {data?.state==='notConfigured'&&<BBEmptyState title="Audiobookshelf väntar på konfigurering" detail="Biblioteket blir tillgängligt här när serveranslutningen är klar."/>}
    {data?.state==='configuredUnavailable'&&<BBEmptyState title="Ljudböcker är tillfälligt otillgängliga" detail={data.message??undefined}/>}
    {data?.continueListening&&<BBSurface className="audiobook-feature"><p className="eyebrow">Fortsätt lyssna</p><Book item={data.continueListening} onOpen={setSelected} prominent/></BBSurface>}
    {data?.state==='configuredHealthy'&&<><div className="audiobook-section-title"><h3>Ditt bibliotek</h3><span>{data.library.length} visas</span></div>
      {data.library.length?<div className="audiobook-grid">{data.library.map(i=><Book item={i} key={i.id} onOpen={setSelected}/>)}</div>:<BBEmptyState title="Biblioteket är tomt" detail="Lägg till en ljudbok i Audiobookshelf så visas den här."/>}
      <form className="audiobook-search" onSubmit={search}><div className="audiobook-section-title"><label htmlFor="audiobook-query">Hitta ljudbok</label><span>{provider?.provider&&provider.provider!=='none'?provider.provider:'Bibliotekssökning'}</span></div>
        <div><BBInput id="audiobook-query" minLength={2} maxLength={120} onChange={e=>setQuery(e.target.value)} placeholder="Titel" value={query}/><BBInput aria-label="Författare" maxLength={120} onChange={e=>setAuthor(e.target.value)} placeholder="Författare (valfritt)" value={author}/><BBSelect aria-label="Föredraget språk" onChange={e=>setLanguage(e.target.value)} value={language}><option value="sv">Svenska</option><option value="en">Engelska</option><option value="all">Alla språk</option></BBSelect><BBButton busy={busy} type="submit" variant="secondary">Sök</BBButton></div>
      </form>
      {notice&&<p aria-live="polite" className="audiobook-notice">{notice}</p>}
      {localResults&&(localResults.length?<><h3>I biblioteket</h3><div className="audiobook-grid">{localResults.map(i=><Book item={i} key={i.id} onOpen={setSelected}/>)}</div></>:<BBEmptyState title="Ingen ljudbok hittades i biblioteket"/>)}
      {discovery&&discovery.length>0&&<><div className="audiobook-section-title"><h3>Kan läggas till</h3><span>{discovery.length} utgåvor</span></div><div className="audiobook-candidates">{discovery.map(i=><Candidate candidate={i} key={`${i.source}:${i.editionId}`} onView={setSelectedCandidate}/>)}</div></>}
      {provider&&!provider.canRequest&&<BBSurface className="audiobook-provider-note"><strong>{provider.state==='configuredUnavailable'?'Automatisk hämtning är tillfälligt otillgänglig.':'Automatisk hämtning är inte konfigurerad ännu.'}</strong><span>{provider.state==='configuredUnavailable'?'Ditt Audiobookshelf-bibliotek fungerar fortfarande. Försök igen när anskaffningsleverantören är tillgänglig.':'Du kan söka i ditt bibliotek. En granskad anskaffningsleverantör krävs för att lägga till nya utgåvor.'}</span></BBSurface>}
      <Activity jobs={jobs}/>
    </>}
    {selected&&<div aria-modal="true" className="bb-dialog-backdrop" onClick={()=>setSelected(null)} role="dialog"><BBSurface aria-labelledby="audiobook-detail-title" className="audiobook-detail" onClick={e=>e.stopPropagation()}><BBButton aria-label="Stäng" className="audiobook-detail__close" onClick={()=>setSelected(null)} variant="icon">×</BBButton><h2 id="audiobook-detail-title">{selected.title}</h2>{selected.author&&<p>{selected.author}</p>}{selected.narrator&&<p>Uppläsare: {selected.narrator}</p>}<p>{selected.languageLabel}{selected.publishedYear?` · ${selected.publishedYear}`:''}</p>{selected.description&&<p>{selected.description}</p>}{selected.playbackUrl&&<a className="bb-button bb-button--primary" href={selected.playbackUrl} rel="noreferrer">Öppna i Audiobookshelf</a>}</BBSurface></div>}
    {selectedCandidate&&<div aria-modal="true" className="bb-dialog-backdrop" onClick={()=>setSelectedCandidate(null)} role="dialog"><BBSurface aria-describedby="audiobook-candidate-confirmation" aria-labelledby="audiobook-candidate-title" className="audiobook-detail" onClick={e=>e.stopPropagation()}><BBButton aria-label="Stäng" className="audiobook-detail__close" onClick={()=>setSelectedCandidate(null)} variant="icon">×</BBButton><p className="eyebrow">Vald utgåva</p><h2 id="audiobook-candidate-title">{selectedCandidate.title}</h2>{selectedCandidate.author&&<p>{selectedCandidate.author}</p>}{selectedCandidate.narrator&&<p>Uppläsare: {selectedCandidate.narrator}</p>}<p>{selectedCandidate.languageLabel}{selectedCandidate.edition?` · ${selectedCandidate.edition}`:''}{selectedCandidate.publicationYear?` · ${selectedCandidate.publicationYear}`:''}</p><p>Källa: {selectedCandidate.provenance??selectedCandidate.source} · {selectedCandidate.languageConfidence==='verified'?'Verifierat språk':selectedCandidate.languageConfidence==='probable'?'Troligt språk':'Språk okänt'}</p><p id="audiobook-candidate-confirmation">Kontrollera utgåvan innan hämtningen startas.</p><BBButton busy={busy} disabled={!provider?.canRequest} onClick={()=>add(selectedCandidate)} variant="primary">Lägg till vald utgåva</BBButton></BBSurface></div>}
  </section>
}
