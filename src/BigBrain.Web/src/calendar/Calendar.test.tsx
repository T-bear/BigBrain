import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { CalendarWidget,calendarDayState } from './Calendar'

const event = { id:'event-1',date:'2026-08-03',startTime:'07:00:00',endTime:'15:00:00',eventType:'Work',visualClassification:'Day',title:'Dagpass',sourceLabel:'Arbete',isAllDay:false,endsNextDay:false }
const ok=(body:unknown)=>Promise.resolve(new Response(JSON.stringify(body),{status:200,headers:{'Content-Type':'application/json'}}))

describe('CalendarWidget',()=>{
  beforeEach(()=>{vi.stubGlobal('fetch',vi.fn((input:RequestInfo|URL)=>{const url=String(input);if(url.includes('/week'))return ok({from:'2026-08-03',to:'2026-08-09',events:[event]});if(url.includes('/month'))return ok({year:2026,month:8,events:[event]});if(url.endsWith('/imports'))return ok([]);return ok({files:[]})}))})
  afterEach(()=>{cleanup();vi.unstubAllGlobals();document.body.className=''})

  it('classifies past, today and future using local calendar dates',()=>{
    const localToday=new Date(2026,7,27,23,55)
    expect(calendarDayState('2026-08-26',localToday)).toBe('past')
    expect(calendarDayState('2026-08-27',localToday)).toBe('today')
    expect(calendarDayState('2026-08-28',localToday)).toBe('future')
  })

  it('shows the current week with accessible symbol and time',async()=>{
    render(<CalendarWidget/>)
    expect(await screen.findByText(/07:00–15:00/)).toBeInTheDocument()
    expect(screen.getByText('Dagpass:',{selector:'.sr-only'})).toBeInTheDocument()
    expect(document.querySelector('.calendar-event__marker--day')).toBeInTheDocument()
    expect(screen.getByRole('button',{name:'Öppna kalender'})).toBeEnabled()
  })

  it('shows safe empty and error states',async()=>{
    vi.mocked(fetch).mockImplementationOnce(()=>ok({from:'2026-08-03',to:'2026-08-09',events:[]}))
    const {unmount}=render(<CalendarWidget/>);expect(await screen.findByText('Inga schemaposter denna vecka.')).toBeInTheDocument();unmount()
    vi.mocked(fetch).mockImplementationOnce(()=>Promise.resolve(new Response(JSON.stringify({code:'calendarImportInvalidStructure',detail:'private detail ignored'}),{status:400,headers:{'Content-Type':'application/json'}})))
    render(<CalendarWidget/>);expect(await screen.findByRole('alert')).toHaveTextContent('Filen verkar inte vara ett Heroma-schema.')
  })

  it('opens the month, renders time directly, navigates and restores focus on Escape',async()=>{
    render(<CalendarWidget/>);const open=await screen.findByRole('button',{name:'Öppna kalender'});fireEvent.click(open)
    expect(await screen.findByRole('dialog',{name:/augusti 2026/i})).toBeInTheDocument()
    expect(screen.getAllByText(/07:00–15:00/).length).toBeGreaterThan(0)
    fireEvent.click(screen.getByRole('button',{name:'Nästa månad'}));await waitFor(()=>expect(vi.mocked(fetch).mock.calls.some(([url])=>String(url).includes('month=9'))).toBe(true))
    fireEvent.keyDown(document,{key:'Escape'});expect(screen.queryByRole('dialog',{name:/kalender/i})).not.toBeInTheDocument();await waitFor(()=>expect(open).toHaveFocus())
  })

  it('supports multiple files and displays per-file preview actions',async()=>{
    vi.mocked(fetch).mockImplementation((input:RequestInfo|URL,init?:RequestInit)=>{const url=String(input);if(url.includes('/week'))return ok({from:'2026-08-03',to:'2026-08-09',events:[event]});if(url.includes('/month'))return ok({year:2026,month:8,events:[event]});if(url.endsWith('/imports'))return ok([]);if(url.includes('import-preview'))return ok({files:[{fileName:'synthetic-a.xlsx',errorCode:null,message:null,preview:{previewId:'opaque',fileName:'synthetic-a.xlsx',year:2026,month:8,counts:{total:5,day:1,evening:1,education:1,collaboration:1,vacation:1,other:0},skippedRows:0,warningCount:0,monthExists:true,exactDuplicate:false,existingEventCount:4,conflictCount:0,expiresAt:'2026-08-05T12:00:00Z'}}]});if(init?.method==='POST')return ok({status:'completed',importedEvents:5,skippedDuplicates:0,conflicts:0});return ok([])})
    render(<CalendarWidget/>);fireEvent.click(await screen.findByRole('button',{name:'Öppna kalender'}));fireEvent.click(await screen.findByRole('button',{name:'Importera Heroma-schema'}))
    const input=screen.getByLabelText(/Välj en eller flera/);const files=[new File(['a'],'synthetic-a.xlsx'),new File(['b'],'synthetic-b.xlsx')];fireEvent.change(input,{target:{files}});expect((input as HTMLInputElement).multiple).toBe(true)
    fireEvent.click(screen.getByRole('button',{name:'Förhandsgranska'}));expect(await screen.findByText('synthetic-a.xlsx')).toBeInTheDocument();expect(screen.getByRole('button',{name:/Ersätt månad/})).toBeEnabled();expect(screen.getByRole('button',{name:'Slå ihop'})).toBeEnabled();expect(screen.getByRole('button',{name:'Avbryt'})).toBeEnabled()
  })

  it('keeps all August days and long import history in one ordered content flow',async()=>{
    const longName='synthetic-very-long-calendar-export-filename-that-must-wrap-without-overflow-august-2026.xlsx'
    vi.mocked(fetch).mockImplementation((input:RequestInfo|URL)=>{const url=String(input);if(url.includes('/week'))return ok({from:'2026-08-03',to:'2026-08-09',events:[event]});if(url.includes('/month'))return ok({year:2026,month:8,events:[event]});if(url.endsWith('/imports'))return ok([{importId:'import-1',originalFileName:longName,importedAt:'2026-08-05T12:00:00Z',year:2026,month:8,importedEvents:19,skippedRows:0,warningCount:0,status:'completed'}]);return ok({files:[]})})
    render(<CalendarWidget/>);fireEvent.click(await screen.findByRole('button',{name:'Öppna kalender'}));await screen.findByRole('dialog',{name:/augusti 2026/i})
    expect(await screen.findByText(longName)).toBeInTheDocument()
    const flow=screen.getByTestId('calendar-content-flow'),month=screen.getByTestId('calendar-month-list'),history=screen.getByTestId('calendar-import-history')
    expect(month.children).toHaveLength(31)
    expect(month.parentElement).toBe(flow);expect(history.parentElement).toBe(flow)
    expect(month.compareDocumentPosition(history)&Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy()
    expect(history.closest('.calendar-month-list__day')).toBeNull()
  })
})
