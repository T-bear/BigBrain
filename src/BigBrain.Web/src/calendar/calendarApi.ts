import { ApiError } from '../api'

export type CalendarEventType = 'Work' | 'Education' | 'Collaboration' | 'Vacation' | 'Other'
export type CalendarVisual = 'Day' | 'Evening' | 'Education' | 'Collaboration' | 'Vacation' | 'Other' | 'Unknown'
export interface CalendarEvent { id:string; date:string; startTime:string|null; endTime:string|null; eventType:CalendarEventType; visualClassification:CalendarVisual; title:string; sourceLabel:string|null; isAllDay:boolean; endsNextDay:boolean }
export interface CalendarWeek { from:string; to:string; events:CalendarEvent[] }
export interface CalendarMonth { year:number; month:number; events:CalendarEvent[] }
export interface CalendarImport { importId:string; originalFileName:string; importedAt:string; year:number; month:number; importedEvents:number; skippedRows:number; warningCount:number; status:string }
export interface CalendarPreview { previewId:string; fileName:string; year:number; month:number; counts:{total:number;day:number;evening:number;education:number;collaboration:number;vacation:number;other:number}; skippedRows:number; warningCount:number; monthExists:boolean; exactDuplicate:boolean; existingEventCount:number; conflictCount:number; expiresAt:string }
export interface CalendarPreviewFile { fileName:string; preview:CalendarPreview|null; errorCode:string|null; message:string|null }

async function json<T>(response:Response):Promise<T> {
  if (!response.ok) { const problem = await response.json().catch(() => null) as {code?:string;detail?:string}|null; throw new ApiError(problem?.code ?? 'requestFailed', problem?.detail ?? 'Åtgärden kunde inte slutföras.') }
  return response.json() as Promise<T>
}
export const getCalendarWeek = (signal?:AbortSignal) => fetch('/api/v1/modules/calendar/week',{signal}).then(json<CalendarWeek>)
export const getCalendarMonth = (year:number,month:number,signal?:AbortSignal) => fetch(`/api/v1/modules/calendar/month?year=${year}&month=${month}`,{signal}).then(json<CalendarMonth>)
export const getCalendarImports = (signal?:AbortSignal) => fetch('/api/v1/modules/calendar/imports',{signal}).then(json<CalendarImport[]>)
export const previewCalendarFiles = (files:File[],signal?:AbortSignal) => { const body=new FormData(); files.forEach(file=>body.append('files',file)); return fetch('/api/v1/modules/calendar/import-preview',{method:'POST',body,signal}).then(json<{files:CalendarPreviewFile[]}>) }
export const confirmCalendarImport = (id:string,strategy:'add'|'replace'|'merge'|'cancel',signal?:AbortSignal) => fetch(`/api/v1/modules/calendar/imports/${encodeURIComponent(id)}/confirm`,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({strategy}),signal}).then(json<{status:string;importedEvents:number;skippedDuplicates:number;conflicts:number}>)
