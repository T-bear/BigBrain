import { act,cleanup,fireEvent,render,screen,waitFor } from '@testing-library/react'
import { afterEach,expect,test,vi } from 'vitest'
import { Audiobooks } from './Audiobooks'
import { AudiobookPlaybackProvider } from './AudiobookPlayback'
import { createAppWidgetRegistry } from '../dashboard/appWidgets'

const json=(value:unknown)=>new Response(JSON.stringify(value),{status:200,headers:{'Content-Type':'application/json'}})
const provider={state:'notConfigured',provider:'none',canSearch:false,canRequest:false,canCancel:false,message:'Ingen granskad anskaffningsleverantör är konfigurerad.'}
const jobs={items:[],offset:0,limit:25,total:0}
const overview={state:'configuredHealthy',message:null,continueListening:null,library:[],recent:[],acquisition:provider}
const playbackHealthy={state:'configuredHealthy',message:null,separateIdentity:true,hasProgress:true}
afterEach(()=>{cleanup();vi.restoreAllMocks();window.history.replaceState({},'','/');window.localStorage.clear();Object.defineProperty(window,'scrollY',{configurable:true,value:0})})
function collection(){window.history.replaceState({},'','/media/audiobooks')}

function initial(fetch:ReturnType<typeof vi.spyOn>,value:unknown=overview,availability:unknown=playbackHealthy){
  fetch.mockResolvedValueOnce(json(value)).mockResolvedValueOnce(json(availability)).mockResolvedValueOnce(json(provider)).mockResolvedValueOnce(json(jobs))
  if(window.location.pathname==='/media/audiobooks'){
    const items=(value as typeof overview).library??[]
    fetch.mockResolvedValueOnce(json({items,page:0,pageSize:24,total:items.length,hasMore:false}))
  }
}

test('is registered as a first-class Media view',()=>{const registry=createAppWidgetRegistry({modules:[],moduleError:false,system:null,systemError:false,docker:null,dockerError:false,recovery:null,recoveryError:false});expect(registry.getAll().some(widget=>widget.id==='audiobooks'&&widget.defaultView==='media')).toBe(true)})

test('renders provider-neutral search with Swedish preference and no fake progress',async()=>{
  collection()
  const fetch=vi.spyOn(globalThis,'fetch');initial(fetch)
  fetch.mockResolvedValueOnce(json({library:[],discovery:[],acquisition:provider}))
  render(<Audiobooks/>);expect(await screen.findByText('Biblioteket är tomt')).toBeInTheDocument()
  expect(screen.getByText('Automatisk hämtning är inte konfigurerad ännu.')).toBeInTheDocument()
  expect(screen.queryByRole('progressbar')).not.toBeInTheDocument()
  fireEvent.change(screen.getByLabelText('Sök efter en ny ljudbok'),{target:{value:'bok'}})
  expect(screen.getByPlaceholderText('Titel, författare, serie eller ISBN')).toBeInTheDocument()
  fireEvent.click(screen.getByRole('button',{name:'Sök'}))
  await waitFor(()=>expect(String(fetch.mock.calls[5][0])).toContain('language=sv'))
  expect(String(fetch.mock.calls[5][0])).not.toContain('author=')
})

test('shows resolved canonical metadata separately from audiobook releases',async()=>{
  collection()
  const fetch=vi.spyOn(globalThis,'fetch');initial(fetch)
  const metadata={query:{original:'The Wandering Inn',normalized:'The Wandering Inn',kind:'freeText'},state:'resolved',narratorSearchSupported:false,message:null,works:[{workId:'OL1W',editionIds:['OL1M'],canonicalTitle:'The Wandering Inn',alternateTitles:['Wandering Inn'],authors:['pirateaba'],series:'The Wandering Inn',seriesNumber:null,narrators:[],isbn10:null,isbn13:'9780306406157',asin:null,language:'en',publicationYear:2017,coverUrl:null,provider:'openLibrary'}]}
  fetch.mockResolvedValueOnce(json({library:[],metadata,discovery:[],acquisition:provider}))
  render(<Audiobooks/>);await screen.findByText('Biblioteket är tomt');fireEvent.change(screen.getByLabelText('Sök efter en ny ljudbok'),{target:{value:'The Wandering Inn'}});fireEvent.click(screen.getByRole('button',{name:'Sök'}))
  expect(await screen.findByText('Bokträff')).toBeInTheDocument()
  expect(screen.getByText('pirateaba')).toBeInTheDocument()
  expect(screen.getByText(/Serie: The Wandering Inn/)).toBeInTheDocument()
  expect(screen.queryByText(/metadata-provider|Librarr/)).not.toBeInTheDocument()
})

test('keeps materially different discovery editions visible',async()=>{
  collection()
  const fetch=vi.spyOn(globalThis,'fetch');initial(fetch)
  const candidate=(editionId:string,narrator:string,language:string,languageLabel:string)=>({workId:'work',editionId,title:'Boken',author:'Författaren',narrator,language,languageLabel,edition:'Oavkortad',durationSeconds:100,publicationYear:2025,coverUrl:null,source:'librarr',availability:'available',languageConfidence:'verified',provenance:'AudioBookBay'})
  fetch.mockResolvedValueOnce(json({library:[],discovery:[candidate('sv-a','Röst A','sv','Svenska'),candidate('en-b','Voice B','en','Engelska')],acquisition:provider}))
  render(<Audiobooks/>);await screen.findByText('Biblioteket är tomt');fireEvent.change(screen.getByLabelText('Sök efter en ny ljudbok'),{target:{value:'bok'}});fireEvent.click(screen.getByRole('button',{name:'Sök'}))
  expect(await screen.findByText('2 utgåvor')).toBeInTheDocument();expect(screen.getByText(/Röst A/)).toBeInTheDocument();expect(screen.getByText(/Voice B/)).toBeInTheDocument()
  expect(screen.queryByRole('button',{name:'Lägg till'})).not.toBeInTheDocument()
  fireEvent.click(screen.getAllByRole('button',{name:'Välj utgåva'})[0])
  expect(screen.getByRole('dialog')).toHaveTextContent('Boken')
  expect(screen.getByRole('button',{name:'Lägg till'})).toBeDisabled()
})

test('shows truthful Audiobookshelf not-configured state',async()=>{
  const fetch=vi.spyOn(globalThis,'fetch');initial(fetch,{state:'notConfigured',message:null,continueListening:null,library:[],recent:[],acquisition:provider})
  render(<Audiobooks/>);expect(await screen.findByText('Audiobookshelf väntar på konfigurering')).toBeInTheDocument()
})

test('keeps library usable while acquisition provider is unavailable',async()=>{
  collection()
  const unavailable={...provider,state:'configuredUnavailable',provider:'librarr',message:'Librarr kunde inte nås.'}
  const fetch=vi.spyOn(globalThis,'fetch')
  fetch.mockResolvedValueOnce(json({...overview,acquisition:unavailable})).mockResolvedValueOnce(json(playbackHealthy)).mockResolvedValueOnce(json(unavailable)).mockResolvedValueOnce(json(jobs))
  render(<Audiobooks/>);expect(await screen.findByText('Biblioteket är tomt')).toBeInTheDocument()
  expect(await screen.findByText('Det går inte att lägga till just nu.')).toBeInTheDocument()
  expect(screen.queryByText('Audiobookshelf väntar på konfigurering')).not.toBeInTheDocument()
})

test('does not mislabel a pending or failed provider-status request as not configured',async()=>{
  collection()
  const fetch=vi.spyOn(globalThis,'fetch')
  fetch.mockResolvedValueOnce(json(overview)).mockResolvedValueOnce(json(playbackHealthy)).mockRejectedValueOnce(new TypeError('network')).mockResolvedValueOnce(json(jobs))
  render(<Audiobooks/>);expect(await screen.findByText('Biblioteket är tomt')).toBeInTheDocument()
  expect(await screen.findByText('Det går inte att lägga till just nu.')).toBeInTheDocument()
  expect(screen.queryByText('Automatisk hämtning är inte konfigurerad ännu.')).not.toBeInTheDocument()
})

test('polls real provider state for active jobs without inventing progress',async()=>{
  collection()
  vi.spyOn(window,'setInterval').mockImplementation(handler=>{if(typeof handler==='function')void handler();return 1})
  const fetch=vi.spyOn(globalThis,'fetch')
  const active={id:'a'.repeat(32),providerJobId:'b'.repeat(40),candidate:{workId:'work',editionId:'edition',title:'Boken',author:'Författaren',narrator:null,language:'sv',languageLabel:'Svenska',edition:'Release',durationSeconds:null,publicationYear:null,coverUrl:null,source:'librarr',availability:'available',languageConfidence:'unknown'},status:'queued',createdAtUtc:'2026-08-23T10:00:00Z',updatedAtUtc:'2026-08-23T10:00:00Z',message:null}
  fetch.mockResolvedValueOnce(json(overview)).mockResolvedValueOnce(json(playbackHealthy)).mockResolvedValueOnce(json({...provider,state:'configuredHealthy',provider:'librarr',canSearch:true,canRequest:true})).mockResolvedValueOnce(json({...jobs,items:[active],total:1})).mockResolvedValueOnce(json({items:[],page:0,pageSize:24,total:0,hasMore:false})).mockResolvedValueOnce(json({...active,status:'downloading'}))
  render(<Audiobooks/>)
  await waitFor(()=>expect(screen.getByText('Hämtas')).toBeInTheDocument())
  expect(screen.queryByRole('progressbar')).not.toBeInTheDocument()
})

test('requires explicit edition confirmation before creating one acquisition job',async()=>{
  collection()
  const healthy={...provider,state:'configuredHealthy',provider:'librarr',canSearch:true,canRequest:true}
  const candidate={workId:'work',editionId:'edition',title:'En mycket lång ljudboksutgåva',author:'Författaren',narrator:'Röst',language:'sv',languageLabel:'Svenska',edition:'Oavkortad · 1.2 GB',durationSeconds:null,publicationYear:2025,coverUrl:null,source:'librarr',availability:'available',languageConfidence:'verified',provenance:'Prowlarr'}
  const fetch=vi.spyOn(globalThis,'fetch');fetch.mockResolvedValueOnce(json({...overview,acquisition:healthy})).mockResolvedValueOnce(json(playbackHealthy)).mockResolvedValueOnce(json(healthy)).mockResolvedValueOnce(json(jobs)).mockResolvedValueOnce(json({items:[],page:0,pageSize:24,total:0,hasMore:false}))
  fetch.mockResolvedValueOnce(json({library:[],discovery:[candidate],acquisition:healthy}))
  fetch.mockResolvedValueOnce(new Response(JSON.stringify({id:'a'.repeat(32),providerJobId:'b'.repeat(40),candidate,status:'queued',createdAtUtc:'2026-08-24T10:00:00Z',updatedAtUtc:'2026-08-24T10:00:00Z',message:null}),{status:201,headers:{'Content-Type':'application/json'}}))
  render(<Audiobooks/>);await screen.findByText('Biblioteket är tomt');fireEvent.change(screen.getByLabelText('Sök efter en ny ljudbok'),{target:{value:'ljudbok'}});fireEvent.click(screen.getByRole('button',{name:'Sök'}));await screen.findByText('1 utgåvor')
  expect(fetch.mock.calls.filter(([,init])=>(init as RequestInit|undefined)?.method==='POST')).toHaveLength(0)
  fireEvent.click(screen.getByRole('button',{name:'Välj utgåva'}));expect(screen.getByRole('dialog')).toHaveTextContent('Lägg till den här utgåvan?')
  fireEvent.click(screen.getByRole('button',{name:'Lägg till'}));await waitFor(()=>expect(fetch.mock.calls.filter(([,init])=>(init as RequestInit|undefined)?.method==='POST')).toHaveLength(1))
  expect(await screen.findByText('Köad')).toBeInTheDocument()
})

test('refreshes Audiobookshelf library after indexing is confirmed complete',async()=>{
  collection()
  vi.spyOn(window,'setInterval').mockImplementation(handler=>{if(typeof handler==='function')void handler();return 1})
  const active={id:'a'.repeat(32),providerJobId:'b'.repeat(40),candidate:{workId:'work',editionId:'edition',title:'Hämtad release',author:'Författaren',narrator:null,language:'und',languageLabel:'Språk okänt',edition:'Release',durationSeconds:null,publicationYear:null,coverUrl:null,source:'librarr',availability:'available',languageConfidence:'unknown'},status:'indexing',createdAtUtc:'2026-08-24T10:00:00Z',updatedAtUtc:'2026-08-24T10:00:00Z',message:'Väntar på indexering.'}
  const imported={id:'abs-item',title:'Importerad bok',author:'Författaren',series:null,narrator:null,language:'und',languageLabel:'Språk okänt',durationSeconds:null,progressPercent:null,description:null,coverUrl:null,publishedYear:null,isAbridged:null,playbackUrl:'http://owner/item/abs-item'}
  const fetch=vi.spyOn(globalThis,'fetch')
  fetch.mockResolvedValueOnce(json(overview)).mockResolvedValueOnce(json(playbackHealthy)).mockResolvedValueOnce(json({...provider,state:'configuredHealthy',provider:'librarr',canSearch:true,canRequest:true})).mockResolvedValueOnce(json({...jobs,items:[active],total:1})).mockResolvedValueOnce(json({items:[],page:0,pageSize:24,total:0,hasMore:false})).mockResolvedValueOnce(json({...active,status:'completed',message:null})).mockResolvedValueOnce(json({...overview,library:[imported],recent:[imported]}))
  render(<Audiobooks/>);expect(await screen.findByText('Importerad bok')).toBeInTheDocument();expect(screen.getByText('Klar')).toBeInTheDocument()
})

test('keeps completed acquisition history collapsed while active work stays visible',async()=>{
  collection()
  const active={id:'a'.repeat(32),providerJobId:null,candidate:{workId:'w',editionId:'e',title:'Pågående bok',author:null,narrator:null,language:'und',languageLabel:'Språk okänt',edition:null,durationSeconds:null,publicationYear:null,coverUrl:null,source:'opaque',availability:'available',languageConfidence:'unknown'},status:'downloading',createdAtUtc:'2026-08-27T10:00:00Z',updatedAtUtc:'2026-08-27T10:00:00Z',message:null}
  const done={...active,id:'b'.repeat(32),candidate:{...active.candidate,editionId:'done',title:'Gammal bok'},status:'completed'}
  const fetch=vi.spyOn(globalThis,'fetch');fetch.mockResolvedValueOnce(json(overview)).mockResolvedValueOnce(json(playbackHealthy)).mockResolvedValueOnce(json(provider)).mockResolvedValueOnce(json({...jobs,items:[active,done],total:42}))
  render(<Audiobooks/>);expect(await screen.findByText('Pågående bok')).toBeVisible()
  expect(screen.getByText('Historik (41)')).toBeInTheDocument();expect(screen.getByText('Gammal bok').closest('details')).not.toHaveAttribute('open')
})

test('dismisses failed attention locally without mutating audit or provider state',async()=>{
  collection()
  const failed={id:'f'.repeat(32),providerJobId:null,candidate:{workId:'w',editionId:'e',title:'Gammalt misslyckat jobb',author:null,narrator:null,language:'und',languageLabel:'Språk okänt',edition:null,durationSeconds:null,publicationYear:null,coverUrl:null,source:'opaque',availability:'available',languageConfidence:'unknown'},status:'failed',createdAtUtc:'2026-08-27T10:00:00Z',updatedAtUtc:'2026-08-27T10:00:00Z',message:'Jobbet finns inte längre hos leverantören.'}
  const fetch=vi.spyOn(globalThis,'fetch');fetch.mockResolvedValueOnce(json(overview)).mockResolvedValueOnce(json(playbackHealthy)).mockResolvedValueOnce(json(provider)).mockResolvedValueOnce(json({...jobs,items:[failed],total:1})).mockResolvedValueOnce(json({items:[],page:0,pageSize:24,total:0,hasMore:false}))
  render(<Audiobooks/>);await screen.findByText('Gammalt misslyckat jobb')
  fireEvent.click(screen.getByRole('button',{name:'Dölj Gammalt misslyckat jobb från åtgärdslistan'}))
  expect(screen.queryByText('Gammalt misslyckat jobb')).not.toBeInTheDocument()
  expect(JSON.parse(window.localStorage.getItem('bigbrain.audiobooks.hidden-attention.v1')??'[]')).toEqual([failed.id])
  expect(fetch.mock.calls.filter(([,init])=>(init as RequestInit|undefined)?.method&&((init as RequestInit).method!=='GET'))).toHaveLength(0)
})

test('keeps the Media overview compact and opens the bounded full collection on demand',async()=>{
  const item=(index:number)=>({id:`book-${index}`,title:`Ljudbok ${index}`,author:'Författare',series:null,narrator:null,language:'sv',languageLabel:'Svenska',durationSeconds:null,progressPercent:null,description:null,coverUrl:null,publishedYear:null,isAbridged:null,playbackUrl:null})
  const books=Array.from({length:12},(_,index)=>item(index+1))
  const fetch=vi.spyOn(globalThis,'fetch');initial(fetch,{...overview,library:books,recent:books.slice(0,6)})
  render(<Audiobooks/>);const libraryLink=await screen.findByRole('link',{name:'Öppna ljudboksbiblioteket'})
  expect(libraryLink).toHaveAttribute('href','/media/audiobooks');expect(libraryLink).not.toHaveClass('bb-button')
  expect(screen.queryByText('Senast tillagda')).not.toBeInTheDocument()
  expect(screen.queryByText('Ljudbok 1')).not.toBeInTheDocument()
  expect(screen.queryByLabelText('Sök i biblioteket')).not.toBeInTheDocument()
  fetch.mockResolvedValueOnce(json({items:books,page:0,pageSize:24,total:books.length,hasMore:false}))
  fireEvent.click(screen.getByRole('link',{name:'Öppna ljudboksbiblioteket'}))
  await waitFor(()=>expect(screen.getByRole('heading',{name:'Ljudböcker',level:1})).toBeInTheDocument())
  expect(screen.getByLabelText('Sök i ditt bibliotek')).toBeInTheDocument()
})

test('uses progress-backed listening continuity and real collection/detail routes',async()=>{
  const listening={id:'book-1',title:'Påbörjad bok',author:'Författare',series:'Serien',narrator:'Uppläsaren',language:'sv',languageLabel:'Svenska',durationSeconds:1000,progressPercent:42,description:'Beskrivning',coverUrl:null,publishedYear:'2025',isAbridged:false,playbackUrl:'https://owner.example/item/book-1'}
  const fetch=vi.spyOn(globalThis,'fetch');initial(fetch,{...overview,continueListening:listening,library:[listening],recent:[listening]})
  render(<Audiobooks/>);expect(await screen.findByText('Fortsätt lyssna')).toBeInTheDocument()
  expect(screen.getByRole('progressbar',{name:'Lyssnat 42 procent'})).toBeInTheDocument()
  expect(screen.getByText('7:00 / 16:40')).toBeInTheDocument()
  expect(screen.getByRole('button',{name:'Spela Påbörjad bok'})).toHaveAttribute('aria-pressed','false')
  const timer=screen.getByRole('button',{name:'Sovtimer för Påbörjad bok'});expect(timer).toBeEnabled()
  fireEvent.click(timer);expect(screen.getByText('Starta uppspelningen för att använda sovtimern.')).toBeVisible();expect(screen.getByRole('button',{name:'Starta uppspelningen'})).toBeVisible()
  fireEvent.click(screen.getByRole('button',{name:'Öppna Påbörjad bok'}))
  expect(window.location.pathname).toBe('/media/audiobooks/book-1')
  expect(screen.getByRole('heading',{name:'Påbörjad bok',level:1})).toBeInTheDocument()
  expect(screen.getByText('Av Författare')).toBeInTheDocument()
  expect(screen.queryByText('Ljudbok')).not.toBeInTheDocument()
  expect(screen.queryByText(/reservväg/i)).not.toBeInTheDocument()
  expect(screen.queryByRole('link',{name:'Öppna i Audiobookshelf'})).not.toBeInTheDocument()
  const back=vi.spyOn(window.history,'back').mockImplementation(()=>undefined)
  fireEvent.click(screen.getByRole('button',{name:'‹ Ljudböcker'}))
  expect(back).toHaveBeenCalledOnce()
  window.history.replaceState({},'','/media/audiobooks')
  window.dispatchEvent(new PopStateEvent('popstate'))
  await waitFor(()=>expect(screen.getByRole('heading',{name:'Ljudböcker',level:1})).toBeInTheDocument())
})

test('starts native playback directly while the non-control area retains detail navigation',async()=>{
  const now=Date.now();vi.spyOn(Date,'now').mockReturnValue(now);const intervals=vi.spyOn(window,'setInterval')
  const listening={id:'book-1',title:'Påbörjad bok',author:'Författare',series:null,narrator:null,language:'sv',languageLabel:'Svenska',durationSeconds:1000,progressPercent:42,description:null,coverUrl:null,publishedYear:null,isAbridged:null,playbackUrl:'https://owner.example/item/book-1'}
  const fetch=vi.spyOn(globalThis,'fetch');initial(fetch,{...overview,continueListening:listening,library:[listening],recent:[listening]})
  fetch.mockResolvedValueOnce(json({id:'session',itemId:'book-1',currentTime:420,duration:1000,tracks:[{index:0,startOffset:0,duration:1000,title:null,mimeType:'audio/mpeg',streamUrl:'/api/v1/modules/media/audiobooks/playback/sessions/session/tracks/0'}],expiresAtUtc:'2026-08-29T20:00:00Z'}))
  const mediaPlay=vi.spyOn(HTMLMediaElement.prototype,'play').mockImplementation(async function(this:HTMLMediaElement){this.dispatchEvent(new Event('play'))})
  vi.spyOn(HTMLMediaElement.prototype,'load').mockImplementation(()=>undefined)
  render(<AudiobookPlaybackProvider><Audiobooks/></AudiobookPlaybackProvider>)
  const navigation=await screen.findByRole('button',{name:'Öppna Påbörjad bok'});const play=screen.getByRole('button',{name:'Spela Påbörjad bok'});expect(play).toHaveAttribute('aria-pressed','false')
  const continueTimer=screen.getByRole('button',{name:'Sovtimer för Påbörjad bok'});fireEvent.click(continueTimer);expect(screen.getByText('Starta uppspelningen för att använda sovtimern.')).toBeVisible()
  fireEvent.click(screen.getByRole('button',{name:'Starta uppspelningen'}))
  await waitFor(()=>expect(mediaPlay).toHaveBeenCalled())
  expect(screen.queryByRole('region',{name:'Spelar Påbörjad bok'})).not.toBeInTheDocument()
  expect(fetch.mock.calls.some(([url,init])=>String(url).endsWith('/book-1/playback')&&(init as RequestInit).method==='POST')).toBe(true)
  expect(window.location.pathname).toBe('/')
  expect(continueTimer).toHaveAttribute('aria-expanded','false');fireEvent.click(continueTimer);expect(screen.getByRole('dialog',{name:'Sovtimeralternativ'})).toBeVisible()
  fireEvent.click(screen.getByRole('button',{name:'15 min'}));expect(screen.getByText(/15 min kvar/)).toBeInTheDocument()
  fireEvent.click(navigation);expect(window.location.pathname).toBe('/media/audiobooks/book-1')
  expect(screen.getByRole('region',{name:'Spelar Påbörjad bok'})).toBeInTheDocument()
  expect(screen.getByText(/15 min kvar/)).toBeInTheDocument()
  const detailTimer=screen.getByRole('button',{name:'Sovtimer'});fireEvent.click(detailTimer)
  fireEvent.click(screen.getByRole('button',{name:'Sluttid…'}));fireEvent.change(screen.getByLabelText('Välj lokal sluttid'),{target:{value:'22:30'}});expect(screen.getByText(/Stannar 22:30/)).toBeInTheDocument()
  fireEvent.click(detailTimer);fireEvent.click(screen.getByRole('button',{name:'Stäng av'}));expect(screen.queryByText(/min kvar/)).not.toBeInTheDocument()
  fireEvent.click(detailTimer);fireEvent.click(screen.getByRole('button',{name:'15 min'}))
  const pause=vi.spyOn(HTMLMediaElement.prototype,'pause').mockImplementation(function(this:HTMLMediaElement){this.dispatchEvent(new Event('pause'))})
  vi.mocked(Date.now).mockReturnValue(now+16*60_000)
  const expiration=[...intervals.mock.calls].reverse().find(call=>call[1]===1000)?.[0]
  expect(expiration).toBeTypeOf('function');act(()=>{if(typeof expiration==='function')expiration()})
  expect(pause).toHaveBeenCalled();expect(screen.queryByText(/min kvar/)).not.toBeInTheDocument()
})

test('shows a truthful fallback instead of a native primary action when playback is unavailable',async()=>{
  window.history.replaceState({},'','/media/audiobooks/unplayable')
  const item={id:'unplayable',title:'Otillgänglig bok',author:null,series:null,narrator:null,language:'und',languageLabel:'Språk okänt',durationSeconds:null,progressPercent:null,description:null,coverUrl:null,publishedYear:null,isAbridged:null,playbackUrl:'https://owner.example/item/unplayable'}
  const unavailable={state:'configuredUnavailable',message:'Playback-identiteten kunde inte verifieras.',separateIdentity:false,hasProgress:false}
  const fetch=vi.spyOn(globalThis,'fetch');initial(fetch,overview,unavailable);fetch.mockResolvedValueOnce(json(item))
  render(<Audiobooks/>);expect(await screen.findByText('BigBrains spelare är inte tillgänglig för den här ljudboken.')).toBeInTheDocument()
  expect(screen.queryByRole('button',{name:'Spela ljudboken'})).not.toBeInTheDocument()
  expect(screen.getByRole('link',{name:'Öppna i Audiobookshelf'})).toHaveClass('bb-button--primary')
  expect(screen.queryByText(/reservväg/i)).not.toBeInTheDocument()
})

test('presents semantic detail metadata without raw filler',async()=>{
  window.history.replaceState({},'','/media/audiobooks/ghostsong')
  const item={id:'ghostsong',title:'Ghostsong (Unabridged)',author:'Pirateaba',series:'Singer of Terandria Series #3',narrator:'Andrea Parsneau',language:'en',languageLabel:'Engelska',durationSeconds:1000,progressPercent:null,description:'Användbar beskrivning.',coverUrl:null,publishedYear:'2025',isAbridged:false,playbackUrl:'https://owner.example/item/ghostsong'}
  const fetch=vi.spyOn(globalThis,'fetch');initial(fetch);fetch.mockResolvedValueOnce(json(item))
  render(<Audiobooks/>);expect(await screen.findByText('Av Pirateaba')).toBeInTheDocument()
  expect(screen.getByText('Serie: Singer of Terandria Series #3')).toBeInTheDocument()
  expect(screen.getByText('Uppläsare: Andrea Parsneau')).toBeInTheDocument()
  expect(screen.getByRole('heading',{name:'Beskrivning'})).toBeInTheDocument()
  expect(screen.queryByText('Ljudbok')).not.toBeInTheDocument()
})

test('loads a deep-linked audiobook detail through the bounded BigBrain API',async()=>{
  window.history.replaceState({},'','/media/audiobooks/deep-link')
  const item={id:'deep-link',title:'Direktlänkad bok',author:null,series:null,narrator:null,language:'und',languageLabel:'Språk okänt',durationSeconds:null,progressPercent:null,description:null,coverUrl:null,publishedYear:null,isAbridged:null,playbackUrl:null}
  const fetch=vi.spyOn(globalThis,'fetch');initial(fetch);fetch.mockResolvedValueOnce(json(item))
  render(<Audiobooks/>);expect(await screen.findByRole('heading',{name:'Direktlänkad bok'})).toBeInTheDocument()
  expect(String(fetch.mock.calls[4][0])).toContain('/api/v1/modules/media/audiobooks/deep-link')
  expect(screen.queryByText('Språk okänt')).not.toBeInTheDocument()
  expect(screen.getByRole('img',{name:'Omslag till Direktlänkad bok'})).toHaveClass('audiobook-detail-page__artwork')
})

test('starts a forward detail route at top and restores collection scroll on back',async()=>{
  collection()
  const item={id:'scroll-book',title:'Scrollbok',author:null,series:null,narrator:null,language:'und',languageLabel:'Språk okänt',durationSeconds:null,progressPercent:null,description:null,coverUrl:null,publishedYear:null,isAbridged:null,playbackUrl:null}
  const fetch=vi.spyOn(globalThis,'fetch');initial(fetch,{...overview,library:[item],recent:[item]})
  Object.defineProperty(window,'scrollY',{configurable:true,value:720})
  vi.spyOn(window,'requestAnimationFrame').mockImplementation(callback=>{callback(0);return 1})
  const scrollTo=vi.spyOn(window,'scrollTo').mockImplementation(()=>undefined)
  render(<Audiobooks/>);fireEvent.click(await screen.findByRole('button',{name:'Öppna Scrollbok'}))
  expect(scrollTo).toHaveBeenCalledWith({top:0,behavior:'auto'})
  window.history.replaceState({bbAudiobookOrigin:true,scrollY:720},'','/media/audiobooks')
  window.dispatchEvent(new PopStateEvent('popstate'))
  expect(scrollTo).toHaveBeenLastCalledWith({top:720})
})

test('restores collection query and sort after returning from detail',async()=>{
  collection()
  const item={id:'book-1',title:'En bok',author:'Författare',series:null,narrator:null,language:'sv',languageLabel:'Svenska',durationSeconds:null,progressPercent:null,description:null,coverUrl:null,publishedYear:null,isAbridged:null,playbackUrl:null}
  const fetch=vi.spyOn(globalThis,'fetch');initial(fetch,{...overview,library:[item],recent:[item]})
  render(<Audiobooks/>);await screen.findByText('En bok')
  fireEvent.change(screen.getByLabelText('Sök i ditt bibliotek'),{target:{value:'min sökning'}})
  fireEvent.change(screen.getByLabelText('Sortera bibliotek'),{target:{value:'author'}})
  fireEvent.click(screen.getByRole('button',{name:'Öppna En bok'}))
  window.history.replaceState({bbAudiobookOrigin:true},'','/media/audiobooks')
  window.dispatchEvent(new PopStateEvent('popstate'))
  await waitFor(()=>expect(screen.getByLabelText('Sök i ditt bibliotek')).toHaveValue('min sökning'))
  expect(screen.getByLabelText('Sortera bibliotek')).toHaveValue('author')
})

test('uses the whole semantic book row for detail navigation',async()=>{
  collection()
  const item={id:'whole-row',title:'Tryckbar bok',author:'Författare',series:null,narrator:null,language:'sv',languageLabel:'Svenska',durationSeconds:null,progressPercent:null,description:null,coverUrl:null,publishedYear:null,isAbridged:null,playbackUrl:null}
  const fetch=vi.spyOn(globalThis,'fetch');initial(fetch,{...overview,library:[item],recent:[item]})
  render(<Audiobooks/>);const row=await screen.findByRole('button',{name:'Öppna Tryckbar bok'})
  expect(row).toHaveClass('audiobook-book-row');expect(screen.queryByText('Visa ljudbok')).not.toBeInTheDocument()
  fireEvent.click(row);expect(window.location.pathname).toBe('/media/audiobooks/whole-row')
})

test('keeps new-book discovery distinct from local-library filtering',async()=>{
  collection()
  const fetch=vi.spyOn(globalThis,'fetch');initial(fetch)
  render(<Audiobooks/>);await screen.findByText('Biblioteket är tomt')
  expect(screen.getByLabelText('Sök efter en ny ljudbok')).toBeInTheDocument()
  expect(screen.getByLabelText('Sök i ditt bibliotek')).toBeInTheDocument()
  expect(screen.getByRole('button',{name:'Sök'})).toBeInTheDocument()
  expect(await screen.findByRole('button',{name:'Filtrera'})).toBeInTheDocument()
  expect(screen.queryByText('Lägg till')).not.toBeInTheDocument()
  expect(screen.queryByText('Din samling')).not.toBeInTheDocument()
})

test('orders discovery before library before downloads',async()=>{
  collection()
  const active={id:'a'.repeat(32),providerJobId:null,candidate:{workId:'w',editionId:'e',title:'Pågående',author:null,narrator:null,language:'und',languageLabel:'Språk okänt',edition:null,durationSeconds:null,publicationYear:null,coverUrl:null,source:'opaque',availability:'available',languageConfidence:'unknown'},status:'downloading',createdAtUtc:'2026-08-27T10:00:00Z',updatedAtUtc:'2026-08-27T10:00:00Z',message:null}
  const fetch=vi.spyOn(globalThis,'fetch');fetch.mockResolvedValueOnce(json(overview)).mockResolvedValueOnce(json(playbackHealthy)).mockResolvedValueOnce(json(provider)).mockResolvedValueOnce(json({...jobs,items:[active],total:1})).mockResolvedValueOnce(json({items:[],page:0,pageSize:24,total:0,hasMore:false}))
  render(<Audiobooks/>);await screen.findByText('Biblioteket är tomt')
  const discovery=screen.getByRole('heading',{name:'Hitta ljudbok'});const library=screen.getByRole('heading',{name:'Bibliotek'});const downloads=await screen.findByRole('heading',{name:'Hämtningar'})
  expect(discovery.compareDocumentPosition(library)&Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
  expect(library.compareDocumentPosition(downloads)&Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
})

test('shows a dock-safe scroll-to-top utility and respects reduced motion',async()=>{
  collection()
  Object.defineProperty(window,'scrollY',{configurable:true,value:700})
  Object.defineProperty(window,'matchMedia',{configurable:true,value:vi.fn().mockReturnValue({matches:true,addEventListener:vi.fn(),removeEventListener:vi.fn()})})
  const scrollTo=vi.spyOn(window,'scrollTo').mockImplementation(()=>undefined)
  const fetch=vi.spyOn(globalThis,'fetch');initial(fetch)
  render(<Audiobooks/>);window.dispatchEvent(new Event('scroll'))
  const button=await screen.findByRole('button',{name:'Till början av ljudboksbiblioteket'})
  fireEvent.click(button)
  expect(scrollTo).toHaveBeenCalledWith({top:0,behavior:'auto'})
})
