import { useEffect, useMemo, useRef, useState } from 'react'
import { ApiError } from '../api'
import { confirmCalendarImport, getCalendarImports, getCalendarMonth, getCalendarWeek, previewCalendarFiles, type CalendarEvent, type CalendarImport, type CalendarPreviewFile } from './calendarApi'

const sv = new Intl.DateTimeFormat('sv-SE',{weekday:'short',day:'numeric',month:'short'})
const monthName = new Intl.DateTimeFormat('sv-SE',{month:'long',year:'numeric'})
const symbols:Record<string,{symbol:string;label:string}> = {
  Day:{symbol:'☀️',label:'Dagpass'}, Evening:{symbol:'🌙',label:'Kvällspass'}, Education:{symbol:'📚',label:'Utbildning'}, Collaboration:{symbol:'🚗',label:'Samverkan'}, Vacation:{symbol:'🏖️',label:'Semester'}, Other:{symbol:'•',label:'Annan schemapost'}, Unknown:{symbol:'•',label:'Okänd schemapost'},
}
const errorText=(error:unknown)=> error instanceof ApiError ? ({calendarImportDuplicate:'Filen har redan importerats.',calendarImportConflict:'Två olika pass hittades för samma datum.',calendarImportExpiredPreview:'Importförhandsgranskningen har gått ut. Försök igen.',calendarImportTooLarge:'Filen är för stor.',calendarImportInvalidStructure:'Filen verkar inte vara ett Heroma-schema.',calendarImportNoEventsFound:'Inga schemaposter kunde hittas.'}[error.code] ?? error.message) : 'Kalendern kunde inte laddas.'
const dateValue=(date:string)=>new Date(`${date}T12:00:00`)
const eventTime=(event:CalendarEvent)=>event.startTime&&event.endTime?`${event.startTime.slice(0,5)}–${event.endTime.slice(0,5)}${event.endsNextDay?' (+1)':''}`:''

function EventLine({event,compact=false}:{event:CalendarEvent;compact?:boolean}) { const visual=symbols[event.visualClassification]??symbols.Unknown; return <div className={`calendar-event ${compact?'calendar-event--compact':''}`}><span role="img" aria-label={visual.label}>{visual.symbol}</span><span>{event.title}{eventTime(event)&&<> · <time>{eventTime(event)}</time></>}</span></div> }

export function CalendarWidget() {
  const [events,setEvents]=useState<CalendarEvent[]|null>(null),[error,setError]=useState(''),[open,setOpen]=useState(false)
  const trigger=useRef<HTMLButtonElement>(null)
  useEffect(()=>{const controller=new AbortController();getCalendarWeek(controller.signal).then(data=>setEvents(data.events)).catch(error=>{if(!controller.signal.aborted)setError(errorText(error))});return()=>controller.abort()},[])
  return <section className="calendar-widget" aria-labelledby="calendar-widget-title"><header><div><h3 id="calendar-widget-title">Kalender</h3><p>Denna vecka</p></div><button ref={trigger} className="secondary-button" onClick={()=>setOpen(true)} aria-haspopup="dialog">Öppna kalender</button></header>
    {events===null&&!error&&<p aria-live="polite">Laddar kalender…</p>}{error&&<p role="alert" className="notice notice--error">{error}</p>}{events?.length===0&&<p className="muted">Inga schemaposter denna vecka.</p>}
    <ol className="calendar-week">{events?.map(event=><li key={event.id}><time dateTime={event.date}>{sv.format(dateValue(event.date))}</time><EventLine event={event} compact/></li>)}</ol>
    {open&&<CalendarDetails onClose={()=>{setOpen(false);requestAnimationFrame(()=>trigger.current?.focus())}}/>}
  </section>
}

function CalendarDetails({onClose}:{onClose:()=>void}) {
  const now=new Date(),[cursor,setCursor]=useState(()=>new Date(now.getFullYear(),now.getMonth(),1)),[events,setEvents]=useState<CalendarEvent[]>([]),[imports,setImports]=useState<CalendarImport[]>([]),[loading,setLoading]=useState(true),[error,setError]=useState(''),[importOpen,setImportOpen]=useState(false)
  const close=useRef<HTMLButtonElement>(null)
  const refresh=()=>{const controller=new AbortController();setLoading(true);Promise.all([getCalendarMonth(cursor.getFullYear(),cursor.getMonth()+1,controller.signal),getCalendarImports(controller.signal)]).then(([month,history])=>{setEvents(month.events);setImports(history);setError('')}).catch(value=>setError(errorText(value))).finally(()=>setLoading(false));return controller}
  useEffect(()=>{const c=refresh();return()=>c.abort()},[cursor.getFullYear(),cursor.getMonth()]) // eslint-disable-line react-hooks/exhaustive-deps
  useEffect(()=>{close.current?.focus();const key=(e:KeyboardEvent)=>{if(e.key==='Escape')onClose()};document.addEventListener('keydown',key);document.body.classList.add('calendar-open');return()=>{document.removeEventListener('keydown',key);document.body.classList.remove('calendar-open')}},[onClose])
  const byDate=useMemo(()=>events.reduce<Record<string,CalendarEvent[]>>((result,event)=>{(result[event.date]??=[]).push(event);return result},{}),[events]);const first=new Date(cursor.getFullYear(),cursor.getMonth(),1);const offset=(first.getDay()+6)%7;const days=Array.from({length:offset+new Date(cursor.getFullYear(),cursor.getMonth()+1,0).getDate()},(_,i)=>i<offset?null:i-offset+1)
  return <div className="calendar-dialog" role="dialog" aria-modal="true" aria-labelledby="calendar-dialog-title"><header><div><p className="eyebrow">Kalender</p><h2 id="calendar-dialog-title">{monthName.format(cursor)}</h2></div><button ref={close} className="calendar-close" onClick={onClose} aria-label="Stäng kalender">×</button></header>
    <div className="calendar-toolbar"><button className="secondary-button" onClick={()=>setCursor(new Date(cursor.getFullYear(),cursor.getMonth()-1,1))} aria-label="Föregående månad">←</button><button className="primary-button" onClick={()=>setImportOpen(true)}>Importera Heroma-schema</button><button className="secondary-button" onClick={()=>setCursor(new Date(cursor.getFullYear(),cursor.getMonth()+1,1))} aria-label="Nästa månad">→</button></div>
    <div className="calendar-dialog__content" data-testid="calendar-content-flow">
      {loading&&<p aria-live="polite">Laddar månad…</p>}{error&&<p role="alert" className="notice notice--error">{error}</p>}
      <div className="calendar-grid" aria-label={monthName.format(cursor)}><div className="calendar-grid__weekdays">{['Mån','Tis','Ons','Tor','Fre','Lör','Sön'].map(day=><span key={day}>{day}</span>)}</div><div className="calendar-grid__days">{days.map((day,index)=>day===null?<div aria-hidden="true" key={`empty-${index}`}/>:<div className="calendar-day" key={day}><strong>{day}</strong>{(byDate[`${cursor.getFullYear()}-${String(cursor.getMonth()+1).padStart(2,'0')}-${String(day).padStart(2,'0')}`]??[]).map(event=><EventLine key={event.id} event={event}/>)}</div>)}</div></div>
      <div className="calendar-month-list" data-testid="calendar-month-list">{Array.from({length:new Date(cursor.getFullYear(),cursor.getMonth()+1,0).getDate()},(_,i)=>i+1).map(day=>{const key=`${cursor.getFullYear()}-${String(cursor.getMonth()+1).padStart(2,'0')}-${String(day).padStart(2,'0')}`;return <div className="calendar-month-list__day" key={key}><time dateTime={key}>{sv.format(dateValue(key))}</time><div>{(byDate[key]??[]).length?(byDate[key]??[]).map(event=><EventLine key={event.id} event={event}/>):<span className="muted">Ledig</span>}</div></div>})}</div>
      <section className="calendar-import-history" data-testid="calendar-import-history"><h3>Importhistorik</h3>{imports.length===0?<p className="muted">Inga importer ännu.</p>:<ul>{imports.map(item=><li key={item.importId}><strong>{item.originalFileName}</strong><span>{item.year}-{String(item.month).padStart(2,'0')} · {item.importedEvents} poster · {item.warningCount} varningar</span></li>)}</ul>}</section>
    </div>
    {importOpen&&<ImportDialog onClose={()=>setImportOpen(false)} onImported={()=>{setImportOpen(false);refresh()}}/>}
  </div>
}

function ImportDialog({onClose,onImported}:{onClose:()=>void;onImported:()=>void}) {
  const [files,setFiles]=useState<File[]>([]),[previews,setPreviews]=useState<CalendarPreviewFile[]>([]),[busy,setBusy]=useState(false),[message,setMessage]=useState('');const controller=useRef<AbortController|null>(null)
  useEffect(()=>()=>controller.current?.abort(),[])
  const preview=async()=>{setBusy(true);setMessage('');controller.current=new AbortController();try{const result=await previewCalendarFiles(files,controller.current.signal);setPreviews(result.files);setMessage('Förhandsgranskning klar. Kontrollera varje fil före import.')}catch(error){setMessage(errorText(error))}finally{setBusy(false)}}
  const confirm=async(item:CalendarPreviewFile,strategy:'add'|'replace'|'merge'|'cancel')=>{if(!item.preview)return;setBusy(true);try{await confirmCalendarImport(item.preview.previewId,strategy);setPreviews(current=>current.filter(value=>value!==item));setMessage(strategy==='cancel'?'Importen avbröts.':'Import slutförd.');if(strategy!=='cancel')onImported()}catch(error){setMessage(errorText(error))}finally{setBusy(false)}}
  return <div className="calendar-import-dialog" role="dialog" aria-modal="true" aria-labelledby="calendar-import-title"><h3 id="calendar-import-title">Importera Heroma-schema</h3><label>Välj en eller flera .xlsx-filer<input type="file" accept=".xlsx,application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" multiple onChange={event=>setFiles(Array.from(event.target.files??[]))}/></label><button className="primary-button" disabled={busy||files.length===0} onClick={preview}>{busy?'Arbetar…':'Förhandsgranska'}</button><p aria-live="polite">{message}</p>
    <ul className="calendar-previews">{previews.map(item=><li key={item.fileName}><strong>{item.fileName}</strong>{item.preview?<><span>{monthName.format(new Date(item.preview.year,item.preview.month-1,1))} · {item.preview.counts.total} poster</span><span>☀️ {item.preview.counts.day} · 🌙 {item.preview.counts.evening} · 📚 {item.preview.counts.education} · 🚗 {item.preview.counts.collaboration} · 🏖️ {item.preview.counts.vacation}</span>{item.preview.warningCount>0&&<span>⚠ {item.preview.warningCount} varningar</span>}{item.preview.exactDuplicate?<span>Redan importerad</span>:<div className="calendar-preview-actions">{item.preview.monthExists?<><button disabled={busy} onClick={()=>confirm(item,'replace')}>Ersätt månad ({item.preview.existingEventCount} → {item.preview.counts.total})</button><button disabled={busy||item.preview.conflictCount>0} onClick={()=>confirm(item,'merge')}>Slå ihop</button></>:<button disabled={busy} onClick={()=>confirm(item,'add')}>Importera</button>}<button className="secondary-button" disabled={busy} onClick={()=>confirm(item,'cancel')}>Avbryt</button></div>}</>:<span role="alert">⚠ {item.message}</span>}</li>)}</ul><button className="secondary-button" disabled={busy} onClick={onClose}>Stäng</button></div>
}
