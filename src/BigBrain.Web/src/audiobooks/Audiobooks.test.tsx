import { cleanup,fireEvent,render,screen,waitFor } from '@testing-library/react'
import { afterEach,expect,test,vi } from 'vitest'
import { Audiobooks } from './Audiobooks'
import { createAppWidgetRegistry } from '../dashboard/appWidgets'

const json=(value:unknown)=>new Response(JSON.stringify(value),{status:200,headers:{'Content-Type':'application/json'}})
const provider={state:'notConfigured',provider:'none',canSearch:false,canRequest:false,canCancel:false,message:'Ingen granskad anskaffningsleverantör är konfigurerad.'}
const jobs={items:[],offset:0,limit:25,total:0}
const overview={state:'configuredHealthy',message:null,continueListening:null,library:[],recent:[],acquisition:provider}
afterEach(()=>{cleanup();vi.restoreAllMocks()})

function initial(fetch:ReturnType<typeof vi.spyOn>,value:unknown=overview){
  fetch.mockResolvedValueOnce(json(value)).mockResolvedValueOnce(json(provider)).mockResolvedValueOnce(json(jobs))
}

test('is registered as a first-class Media view',()=>{const registry=createAppWidgetRegistry({modules:[],moduleError:false,system:null,systemError:false,docker:null,dockerError:false,recovery:null,recoveryError:false});expect(registry.getAll().some(widget=>widget.id==='audiobooks'&&widget.defaultView==='media')).toBe(true)})

test('renders provider-neutral search with Swedish preference and no fake progress',async()=>{
  const fetch=vi.spyOn(globalThis,'fetch');initial(fetch)
  fetch.mockResolvedValueOnce(json({library:[],discovery:[],acquisition:provider}))
  render(<Audiobooks/>);expect(await screen.findByText('Biblioteket är tomt')).toBeInTheDocument()
  expect(screen.getByText('Automatisk hämtning är inte konfigurerad ännu.')).toBeInTheDocument()
  expect(screen.queryByRole('progressbar')).not.toBeInTheDocument()
  fireEvent.change(screen.getByLabelText('Hitta ljudbok'),{target:{value:'bok'}})
  fireEvent.change(screen.getByLabelText('Författare'),{target:{value:'Författare'}})
  fireEvent.click(screen.getByRole('button',{name:'Sök'}))
  await waitFor(()=>expect(String(fetch.mock.calls[3][0])).toContain('language=sv'))
  expect(String(fetch.mock.calls[3][0])).toContain('author=F%C3%B6rfattare')
})

test('keeps materially different discovery editions visible',async()=>{
  const fetch=vi.spyOn(globalThis,'fetch');initial(fetch)
  const candidate=(editionId:string,narrator:string,language:string,languageLabel:string)=>({workId:'work',editionId,title:'Boken',author:'Författaren',narrator,language,languageLabel,edition:'Oavkortad',durationSeconds:100,publicationYear:2025,coverUrl:null,source:'librarr',availability:'available',languageConfidence:'verified',provenance:'AudioBookBay'})
  fetch.mockResolvedValueOnce(json({library:[],discovery:[candidate('sv-a','Röst A','sv','Svenska'),candidate('en-b','Voice B','en','Engelska')],acquisition:provider}))
  render(<Audiobooks/>);await screen.findByText('Biblioteket är tomt');fireEvent.change(screen.getByLabelText('Hitta ljudbok'),{target:{value:'bok'}});fireEvent.click(screen.getByRole('button',{name:'Sök'}))
  expect(await screen.findByText('2 utgåvor')).toBeInTheDocument();expect(screen.getByText(/Röst A/)).toBeInTheDocument();expect(screen.getByText(/Voice B/)).toBeInTheDocument()
  expect(screen.queryByRole('button',{name:'Lägg till vald utgåva'})).not.toBeInTheDocument()
  fireEvent.click(screen.getAllByRole('button',{name:'Välj utgåva'})[0])
  expect(screen.getByRole('dialog')).toHaveTextContent('Röst A')
  expect(screen.getByRole('button',{name:'Lägg till vald utgåva'})).toBeDisabled()
})

test('shows truthful Audiobookshelf not-configured state',async()=>{
  const fetch=vi.spyOn(globalThis,'fetch');initial(fetch,{state:'notConfigured',message:null,continueListening:null,library:[],recent:[],acquisition:provider})
  render(<Audiobooks/>);expect(await screen.findByText('Audiobookshelf väntar på konfigurering')).toBeInTheDocument()
})

test('keeps library usable while acquisition provider is unavailable',async()=>{
  const unavailable={...provider,state:'configuredUnavailable',provider:'librarr',message:'Librarr kunde inte nås.'}
  const fetch=vi.spyOn(globalThis,'fetch')
  fetch.mockResolvedValueOnce(json({...overview,acquisition:unavailable})).mockResolvedValueOnce(json(unavailable)).mockResolvedValueOnce(json(jobs))
  render(<Audiobooks/>);expect(await screen.findByText('Biblioteket är tomt')).toBeInTheDocument()
  expect(screen.getByText('Automatisk hämtning är tillfälligt otillgänglig.')).toBeInTheDocument()
  expect(screen.queryByText('Audiobookshelf väntar på konfigurering')).not.toBeInTheDocument()
})

test('does not mislabel a pending or failed provider-status request as not configured',async()=>{
  const fetch=vi.spyOn(globalThis,'fetch')
  fetch.mockResolvedValueOnce(json(overview)).mockRejectedValueOnce(new TypeError('network')).mockResolvedValueOnce(json(jobs))
  render(<Audiobooks/>);expect(await screen.findByText('Biblioteket är tomt')).toBeInTheDocument()
  expect(screen.getByText('Automatisk hämtning är tillfälligt otillgänglig.')).toBeInTheDocument()
  expect(screen.queryByText('Automatisk hämtning är inte konfigurerad ännu.')).not.toBeInTheDocument()
})

test('polls real provider state for active jobs without inventing progress',async()=>{
  vi.spyOn(window,'setInterval').mockImplementation(handler=>{if(typeof handler==='function')void handler();return 1})
  const fetch=vi.spyOn(globalThis,'fetch')
  const active={id:'a'.repeat(32),providerJobId:'b'.repeat(40),candidate:{workId:'work',editionId:'edition',title:'Boken',author:'Författaren',narrator:null,language:'sv',languageLabel:'Svenska',edition:'Release',durationSeconds:null,publicationYear:null,coverUrl:null,source:'librarr',availability:'available',languageConfidence:'unknown'},status:'queued',createdAtUtc:'2026-08-23T10:00:00Z',updatedAtUtc:'2026-08-23T10:00:00Z',message:null}
  fetch.mockResolvedValueOnce(json(overview)).mockResolvedValueOnce(json({...provider,state:'configuredHealthy',provider:'librarr',canSearch:true,canRequest:true})).mockResolvedValueOnce(json({...jobs,items:[active],total:1})).mockResolvedValueOnce(json({...active,status:'downloading'}))
  render(<Audiobooks/>)
  await waitFor(()=>expect(screen.getByText('Hämtas')).toBeInTheDocument())
  expect(screen.queryByRole('progressbar')).not.toBeInTheDocument()
})

test('requires explicit edition confirmation before creating one acquisition job',async()=>{
  const healthy={...provider,state:'configuredHealthy',provider:'librarr',canSearch:true,canRequest:true}
  const candidate={workId:'work',editionId:'edition',title:'En mycket lång ljudboksutgåva',author:'Författaren',narrator:'Röst',language:'sv',languageLabel:'Svenska',edition:'Oavkortad · 1.2 GB',durationSeconds:null,publicationYear:2025,coverUrl:null,source:'librarr',availability:'available',languageConfidence:'verified',provenance:'Prowlarr'}
  const fetch=vi.spyOn(globalThis,'fetch');fetch.mockResolvedValueOnce(json({...overview,acquisition:healthy})).mockResolvedValueOnce(json(healthy)).mockResolvedValueOnce(json(jobs))
  fetch.mockResolvedValueOnce(json({library:[],discovery:[candidate],acquisition:healthy}))
  fetch.mockResolvedValueOnce(new Response(JSON.stringify({id:'a'.repeat(32),providerJobId:'b'.repeat(40),candidate,status:'queued',createdAtUtc:'2026-08-24T10:00:00Z',updatedAtUtc:'2026-08-24T10:00:00Z',message:null}),{status:201,headers:{'Content-Type':'application/json'}}))
  render(<Audiobooks/>);await screen.findByText('Biblioteket är tomt');fireEvent.change(screen.getByLabelText('Hitta ljudbok'),{target:{value:'ljudbok'}});fireEvent.click(screen.getByRole('button',{name:'Sök'}));await screen.findByText('1 utgåvor')
  expect(fetch.mock.calls.filter(([,init])=>(init as RequestInit|undefined)?.method==='POST')).toHaveLength(0)
  fireEvent.click(screen.getByRole('button',{name:'Välj utgåva'}));expect(screen.getByRole('dialog')).toHaveTextContent('Kontrollera utgåvan')
  fireEvent.click(screen.getByRole('button',{name:'Lägg till vald utgåva'}));await waitFor(()=>expect(fetch.mock.calls.filter(([,init])=>(init as RequestInit|undefined)?.method==='POST')).toHaveLength(1))
  expect(await screen.findByText('Köad')).toBeInTheDocument()
})

test('refreshes Audiobookshelf library after indexing is confirmed complete',async()=>{
  vi.spyOn(window,'setInterval').mockImplementation(handler=>{if(typeof handler==='function')void handler();return 1})
  const active={id:'a'.repeat(32),providerJobId:'b'.repeat(40),candidate:{workId:'work',editionId:'edition',title:'Hämtad release',author:'Författaren',narrator:null,language:'und',languageLabel:'Språk okänt',edition:'Release',durationSeconds:null,publicationYear:null,coverUrl:null,source:'librarr',availability:'available',languageConfidence:'unknown'},status:'indexing',createdAtUtc:'2026-08-24T10:00:00Z',updatedAtUtc:'2026-08-24T10:00:00Z',message:'Väntar på indexering.'}
  const imported={id:'abs-item',title:'Importerad bok',author:'Författaren',series:null,narrator:null,language:'und',languageLabel:'Språk okänt',durationSeconds:null,progressPercent:null,description:null,coverUrl:null,publishedYear:null,isAbridged:null,playbackUrl:'http://owner/item/abs-item'}
  const fetch=vi.spyOn(globalThis,'fetch')
  fetch.mockResolvedValueOnce(json(overview)).mockResolvedValueOnce(json({...provider,state:'configuredHealthy',provider:'librarr',canSearch:true,canRequest:true})).mockResolvedValueOnce(json({...jobs,items:[active],total:1})).mockResolvedValueOnce(json({...active,status:'completed',message:null})).mockResolvedValueOnce(json({...overview,library:[imported],recent:[imported]}))
  render(<Audiobooks/>);expect(await screen.findByText('Importerad bok')).toBeInTheDocument();expect(screen.getByText('Klar')).toBeInTheDocument()
})
