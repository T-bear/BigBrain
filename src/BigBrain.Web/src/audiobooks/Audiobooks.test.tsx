import { cleanup,fireEvent,render,screen,waitFor } from '@testing-library/react'
import { afterEach,expect,test,vi } from 'vitest'
import { Audiobooks } from './Audiobooks'
import { createAppWidgetRegistry } from '../dashboard/appWidgets'

const json=(value:unknown)=>new Response(JSON.stringify(value),{status:200,headers:{'Content-Type':'application/json'}})
const provider={state:'notConfigured',provider:'none',canSearch:false,canRequest:false,canCancel:false,message:'Ingen granskad anskaffningsleverantör är konfigurerad.'}
const jobs={items:[],offset:0,limit:25,total:0}
const overview={state:'configuredHealthy',message:null,continueListening:null,library:[],recent:[],acquisition:provider}
afterEach(()=>{cleanup();vi.restoreAllMocks();window.history.replaceState({},'','/')})
function collection(){window.history.replaceState({},'','/media/audiobooks')}

function initial(fetch:ReturnType<typeof vi.spyOn>,value:unknown=overview){
  fetch.mockResolvedValueOnce(json(value)).mockResolvedValueOnce(json(provider)).mockResolvedValueOnce(json(jobs))
}

test('is registered as a first-class Media view',()=>{const registry=createAppWidgetRegistry({modules:[],moduleError:false,system:null,systemError:false,docker:null,dockerError:false,recovery:null,recoveryError:false});expect(registry.getAll().some(widget=>widget.id==='audiobooks'&&widget.defaultView==='media')).toBe(true)})

test('renders provider-neutral search with Swedish preference and no fake progress',async()=>{
  collection()
  const fetch=vi.spyOn(globalThis,'fetch');initial(fetch)
  fetch.mockResolvedValueOnce(json({library:[],discovery:[],acquisition:provider}))
  render(<Audiobooks/>);expect(await screen.findByText('Biblioteket är tomt')).toBeInTheDocument()
  expect(screen.getByText('Automatisk hämtning är inte konfigurerad ännu.')).toBeInTheDocument()
  expect(screen.queryByRole('progressbar')).not.toBeInTheDocument()
  fireEvent.change(screen.getByLabelText('Hitta ljudbok'),{target:{value:'bok'}})
  expect(screen.getByPlaceholderText('Titel, författare, serie eller ISBN')).toBeInTheDocument()
  fireEvent.click(screen.getByRole('button',{name:'Sök'}))
  await waitFor(()=>expect(String(fetch.mock.calls[3][0])).toContain('language=sv'))
  expect(String(fetch.mock.calls[3][0])).not.toContain('author=')
})

test('shows resolved canonical metadata separately from audiobook releases',async()=>{
  collection()
  const fetch=vi.spyOn(globalThis,'fetch');initial(fetch)
  const metadata={query:{original:'The Wandering Inn',normalized:'The Wandering Inn',kind:'freeText'},state:'resolved',narratorSearchSupported:false,message:null,works:[{workId:'OL1W',editionIds:['OL1M'],canonicalTitle:'The Wandering Inn',alternateTitles:['Wandering Inn'],authors:['pirateaba'],series:'The Wandering Inn',seriesNumber:null,narrators:[],isbn10:null,isbn13:'9780306406157',asin:null,language:'en',publicationYear:2017,coverUrl:null,provider:'openLibrary'}]}
  fetch.mockResolvedValueOnce(json({library:[],metadata,discovery:[],acquisition:provider}))
  render(<Audiobooks/>);await screen.findByText('Biblioteket är tomt');fireEvent.change(screen.getByLabelText('Hitta ljudbok'),{target:{value:'The Wandering Inn'}});fireEvent.click(screen.getByRole('button',{name:'Sök'}))
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
  render(<Audiobooks/>);await screen.findByText('Biblioteket är tomt');fireEvent.change(screen.getByLabelText('Hitta ljudbok'),{target:{value:'bok'}});fireEvent.click(screen.getByRole('button',{name:'Sök'}))
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
  fetch.mockResolvedValueOnce(json({...overview,acquisition:unavailable})).mockResolvedValueOnce(json(unavailable)).mockResolvedValueOnce(json(jobs))
  render(<Audiobooks/>);expect(await screen.findByText('Biblioteket är tomt')).toBeInTheDocument()
  expect(screen.getByText('Det går inte att lägga till just nu.')).toBeInTheDocument()
  expect(screen.queryByText('Audiobookshelf väntar på konfigurering')).not.toBeInTheDocument()
})

test('does not mislabel a pending or failed provider-status request as not configured',async()=>{
  collection()
  const fetch=vi.spyOn(globalThis,'fetch')
  fetch.mockResolvedValueOnce(json(overview)).mockRejectedValueOnce(new TypeError('network')).mockResolvedValueOnce(json(jobs))
  render(<Audiobooks/>);expect(await screen.findByText('Biblioteket är tomt')).toBeInTheDocument()
  expect(screen.getByText('Det går inte att lägga till just nu.')).toBeInTheDocument()
  expect(screen.queryByText('Automatisk hämtning är inte konfigurerad ännu.')).not.toBeInTheDocument()
})

test('polls real provider state for active jobs without inventing progress',async()=>{
  collection()
  vi.spyOn(window,'setInterval').mockImplementation(handler=>{if(typeof handler==='function')void handler();return 1})
  const fetch=vi.spyOn(globalThis,'fetch')
  const active={id:'a'.repeat(32),providerJobId:'b'.repeat(40),candidate:{workId:'work',editionId:'edition',title:'Boken',author:'Författaren',narrator:null,language:'sv',languageLabel:'Svenska',edition:'Release',durationSeconds:null,publicationYear:null,coverUrl:null,source:'librarr',availability:'available',languageConfidence:'unknown'},status:'queued',createdAtUtc:'2026-08-23T10:00:00Z',updatedAtUtc:'2026-08-23T10:00:00Z',message:null}
  fetch.mockResolvedValueOnce(json(overview)).mockResolvedValueOnce(json({...provider,state:'configuredHealthy',provider:'librarr',canSearch:true,canRequest:true})).mockResolvedValueOnce(json({...jobs,items:[active],total:1})).mockResolvedValueOnce(json({...active,status:'downloading'}))
  render(<Audiobooks/>)
  await waitFor(()=>expect(screen.getByText('Hämtas')).toBeInTheDocument())
  expect(screen.queryByRole('progressbar')).not.toBeInTheDocument()
})

test('requires explicit edition confirmation before creating one acquisition job',async()=>{
  collection()
  const healthy={...provider,state:'configuredHealthy',provider:'librarr',canSearch:true,canRequest:true}
  const candidate={workId:'work',editionId:'edition',title:'En mycket lång ljudboksutgåva',author:'Författaren',narrator:'Röst',language:'sv',languageLabel:'Svenska',edition:'Oavkortad · 1.2 GB',durationSeconds:null,publicationYear:2025,coverUrl:null,source:'librarr',availability:'available',languageConfidence:'verified',provenance:'Prowlarr'}
  const fetch=vi.spyOn(globalThis,'fetch');fetch.mockResolvedValueOnce(json({...overview,acquisition:healthy})).mockResolvedValueOnce(json(healthy)).mockResolvedValueOnce(json(jobs))
  fetch.mockResolvedValueOnce(json({library:[],discovery:[candidate],acquisition:healthy}))
  fetch.mockResolvedValueOnce(new Response(JSON.stringify({id:'a'.repeat(32),providerJobId:'b'.repeat(40),candidate,status:'queued',createdAtUtc:'2026-08-24T10:00:00Z',updatedAtUtc:'2026-08-24T10:00:00Z',message:null}),{status:201,headers:{'Content-Type':'application/json'}}))
  render(<Audiobooks/>);await screen.findByText('Biblioteket är tomt');fireEvent.change(screen.getByLabelText('Hitta ljudbok'),{target:{value:'ljudbok'}});fireEvent.click(screen.getByRole('button',{name:'Sök'}));await screen.findByText('1 utgåvor')
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
  fetch.mockResolvedValueOnce(json(overview)).mockResolvedValueOnce(json({...provider,state:'configuredHealthy',provider:'librarr',canSearch:true,canRequest:true})).mockResolvedValueOnce(json({...jobs,items:[active],total:1})).mockResolvedValueOnce(json({...active,status:'completed',message:null})).mockResolvedValueOnce(json({...overview,library:[imported],recent:[imported]}))
  render(<Audiobooks/>);expect(await screen.findByText('Importerad bok')).toBeInTheDocument();expect(screen.getByText('Klar')).toBeInTheDocument()
})

test('keeps completed acquisition history collapsed while active work stays visible',async()=>{
  collection()
  const active={id:'a'.repeat(32),providerJobId:null,candidate:{workId:'w',editionId:'e',title:'Pågående bok',author:null,narrator:null,language:'und',languageLabel:'Språk okänt',edition:null,durationSeconds:null,publicationYear:null,coverUrl:null,source:'opaque',availability:'available',languageConfidence:'unknown'},status:'downloading',createdAtUtc:'2026-08-27T10:00:00Z',updatedAtUtc:'2026-08-27T10:00:00Z',message:null}
  const done={...active,id:'b'.repeat(32),candidate:{...active.candidate,editionId:'done',title:'Gammal bok'},status:'completed'}
  const fetch=vi.spyOn(globalThis,'fetch');fetch.mockResolvedValueOnce(json(overview)).mockResolvedValueOnce(json(provider)).mockResolvedValueOnce(json({...jobs,items:[active,done],total:42}))
  render(<Audiobooks/>);expect(await screen.findByText('Pågående bok')).toBeVisible()
  expect(screen.getByText('Historik (41)')).toBeInTheDocument();expect(screen.getByText('Gammal bok').closest('details')).not.toHaveAttribute('open')
})

test('keeps the Media overview compact and opens the bounded full collection on demand',async()=>{
  const item=(index:number)=>({id:`book-${index}`,title:`Ljudbok ${index}`,author:'Författare',series:null,narrator:null,language:'sv',languageLabel:'Svenska',durationSeconds:null,progressPercent:null,description:null,coverUrl:null,publishedYear:null,isAbridged:null,playbackUrl:null})
  const books=Array.from({length:12},(_,index)=>item(index+1))
  const fetch=vi.spyOn(globalThis,'fetch');initial(fetch,{...overview,library:books,recent:books.slice(0,6)})
  render(<Audiobooks/>);expect(await screen.findByRole('button',{name:'Öppna ljudboksbiblioteket, 12 ljudböcker'})).toBeInTheDocument()
  expect(screen.queryByText('Senast tillagda')).not.toBeInTheDocument()
  expect(screen.queryByText('Ljudbok 1')).not.toBeInTheDocument()
  expect(screen.queryByLabelText('Sök i biblioteket')).not.toBeInTheDocument()
  fireEvent.click(screen.getByRole('button',{name:'Öppna ljudboksbiblioteket, 12 ljudböcker'}))
  await waitFor(()=>expect(screen.getByRole('heading',{name:'Ljudböcker',level:1})).toBeInTheDocument())
  expect(screen.getByLabelText('Sök i biblioteket')).toBeInTheDocument()
})

test('uses progress-backed listening continuity and real collection/detail routes',async()=>{
  const listening={id:'book-1',title:'Påbörjad bok',author:'Författare',series:'Serien',narrator:'Uppläsaren',language:'sv',languageLabel:'Svenska',durationSeconds:1000,progressPercent:42,description:'Beskrivning',coverUrl:null,publishedYear:'2025',isAbridged:false,playbackUrl:'https://owner.example/item/book-1'}
  const fetch=vi.spyOn(globalThis,'fetch');initial(fetch,{...overview,continueListening:listening,library:[listening],recent:[listening]})
  render(<Audiobooks/>);expect(await screen.findByText('Fortsätt lyssna')).toBeInTheDocument()
  expect(screen.getByRole('progressbar',{name:'Lyssnat 42 procent'})).toBeInTheDocument()
  fireEvent.click(screen.getByRole('button',{name:'Öppna Påbörjad bok'}))
  expect(window.location.pathname).toBe('/media/audiobooks/book-1')
  expect(screen.getByRole('heading',{name:'Påbörjad bok',level:1})).toBeInTheDocument()
  expect(screen.getByRole('link',{name:'Spela ljudbok'})).toHaveAttribute('href','https://owner.example/item/book-1')
  const back=vi.spyOn(window.history,'back').mockImplementation(()=>undefined)
  fireEvent.click(screen.getByRole('button',{name:'‹ Ljudböcker'}))
  expect(back).toHaveBeenCalledOnce()
  window.history.replaceState({},'','/media/audiobooks')
  window.dispatchEvent(new PopStateEvent('popstate'))
  await waitFor(()=>expect(screen.getByRole('heading',{name:'Ljudböcker',level:1})).toBeInTheDocument())
})

test('loads a deep-linked audiobook detail through the bounded BigBrain API',async()=>{
  window.history.replaceState({},'','/media/audiobooks/deep-link')
  const item={id:'deep-link',title:'Direktlänkad bok',author:null,series:null,narrator:null,language:'und',languageLabel:'Språk okänt',durationSeconds:null,progressPercent:null,description:null,coverUrl:null,publishedYear:null,isAbridged:null,playbackUrl:null}
  const fetch=vi.spyOn(globalThis,'fetch');initial(fetch);fetch.mockResolvedValueOnce(json(item))
  render(<Audiobooks/>);expect(await screen.findByRole('heading',{name:'Direktlänkad bok'})).toBeInTheDocument()
  expect(String(fetch.mock.calls[3][0])).toContain('/api/v1/modules/media/audiobooks/deep-link')
})

test('restores collection query and sort after returning from detail',async()=>{
  collection()
  const item={id:'book-1',title:'En bok',author:'Författare',series:null,narrator:null,language:'sv',languageLabel:'Svenska',durationSeconds:null,progressPercent:null,description:null,coverUrl:null,publishedYear:null,isAbridged:null,playbackUrl:null}
  const fetch=vi.spyOn(globalThis,'fetch');initial(fetch,{...overview,library:[item],recent:[item]})
  render(<Audiobooks/>);await screen.findByText('En bok')
  fireEvent.change(screen.getByLabelText('Sök i biblioteket'),{target:{value:'min sökning'}})
  fireEvent.change(screen.getByLabelText('Sortera bibliotek'),{target:{value:'author'}})
  fireEvent.click(screen.getByRole('button',{name:'Visa ljudbok'}))
  window.history.replaceState({bbAudiobookOrigin:true},'','/media/audiobooks')
  window.dispatchEvent(new PopStateEvent('popstate'))
  await waitFor(()=>expect(screen.getByLabelText('Sök i biblioteket')).toHaveValue('min sökning'))
  expect(screen.getByLabelText('Sortera bibliotek')).toHaveValue('author')
})
