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
  const candidate=(editionId:string,narrator:string,language:string,languageLabel:string)=>({workId:'work',editionId,title:'Boken',author:'Författaren',narrator,language,languageLabel,edition:'Oavkortad',durationSeconds:100,publicationYear:2025,coverUrl:null,source:'fixture',availability:'available',languageConfidence:'verified'})
  fetch.mockResolvedValueOnce(json({library:[],discovery:[candidate('sv-a','Röst A','sv','Svenska'),candidate('en-b','Voice B','en','Engelska')],acquisition:provider}))
  render(<Audiobooks/>);await screen.findByText('Biblioteket är tomt');fireEvent.change(screen.getByLabelText('Hitta ljudbok'),{target:{value:'bok'}});fireEvent.click(screen.getByRole('button',{name:'Sök'}))
  expect(await screen.findByText('2 utgåvor')).toBeInTheDocument();expect(screen.getByText(/Röst A/)).toBeInTheDocument();expect(screen.getByText(/Voice B/)).toBeInTheDocument()
  expect(screen.getAllByRole('button',{name:'Lägg till'}).every(button=>(button as HTMLButtonElement).disabled)).toBe(true)
  fireEvent.click(screen.getAllByRole('button',{name:'Visa detaljer'})[0])
  expect(screen.getByRole('dialog')).toHaveTextContent('Röst A')
})

test('shows truthful Audiobookshelf not-configured state',async()=>{
  const fetch=vi.spyOn(globalThis,'fetch');initial(fetch,{state:'notConfigured',message:null,continueListening:null,library:[],recent:[],acquisition:provider})
  render(<Audiobooks/>);expect(await screen.findByText('Audiobookshelf väntar på konfigurering')).toBeInTheDocument()
})

test('polls real provider state for active jobs without inventing progress',async()=>{
  vi.spyOn(window,'setInterval').mockImplementation(handler=>{if(typeof handler==='function')void handler();return 1})
  const fetch=vi.spyOn(globalThis,'fetch')
  const active={id:'a'.repeat(32),providerJobId:'b'.repeat(40),candidate:{workId:'work',editionId:'edition',title:'Boken',author:'Författaren',narrator:null,language:'sv',languageLabel:'Svenska',edition:'Release',durationSeconds:null,publicationYear:null,coverUrl:null,source:'librarr',availability:'available',languageConfidence:'unknown'},status:'queued',createdAtUtc:'2026-08-23T10:00:00Z',updatedAtUtc:'2026-08-23T10:00:00Z',message:null}
  fetch.mockResolvedValueOnce(json(overview)).mockResolvedValueOnce(json({...provider,state:'configuredHealthy',provider:'librarr',canSearch:true,canRequest:true})).mockResolvedValueOnce(json({...jobs,items:[active],total:1})).mockResolvedValueOnce(json({...active,status:'downloading'}))
  render(<Audiobooks/>)
  await waitFor(()=>expect(screen.getByText('downloading')).toBeInTheDocument())
  expect(screen.queryByRole('progressbar')).not.toBeInTheDocument()
})
