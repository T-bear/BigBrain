import { expect,test } from 'vitest'
import { nextEventLabel } from './HomeOverview'

test('next calendar event includes local date, title and time',()=>{
  const label=nextEventLabel({id:'1',date:'2026-08-28',startTime:'17:00:00',endTime:'18:00:00',eventType:'Other',visualClassification:'Other',title:'Träning',sourceLabel:'Familj',isAllDay:false,endsNextDay:false})
  expect(label).toContain('28 aug.')
  expect(label).toContain('Träning')
  expect(label).toContain('17:00:00–18:00:00')
})
