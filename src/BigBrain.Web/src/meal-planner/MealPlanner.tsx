import { useEffect, useMemo, useState } from 'react'
import {
  createMealPlannerMeal, createMealPlannerTag, deleteMealPlannerMeal, deleteMealPlannerSchedule,
  deleteMealPlannerTag, generateMealPlannerSchedule, getMealPlannerMeals, getMealPlannerSchedules,
  getMealPlannerTags, replaceMealPlannerDay, seedMealPlannerExamples, setMealPlannerDay, updateMealPlannerMeal,
} from '../api'
import { CollapsibleModule } from '../dashboard/CollapsibleModule'
import type { MealPlannerDay, MealPlannerMeal, MealPlannerSchedule, MealPlannerTag } from '../types'

const dayNames: Record<string, string> = {
  Monday: 'Måndag', Tuesday: 'Tisdag', Wednesday: 'Onsdag', Thursday: 'Torsdag',
  Friday: 'Fredag', Saturday: 'Lördag', Sunday: 'Söndag',
}
const tabs = [
  { id: 'schedule', label: 'Matsedel' },
  { id: 'meals', label: 'Maträtter' },
  { id: 'generate', label: 'Generera' },
  { id: 'saved', label: 'Sparade' },
] as const
type MealPlannerTab = typeof tabs[number]['id']

function formatDate(value: string) {
  return new Intl.DateTimeFormat('sv-SE', { day: 'numeric', month: 'short' }).format(new Date(`${value}T12:00:00`))
}

function weekLabel(schedule: MealPlannerSchedule, weekIndex: number) {
  const dates = scheduleDates(schedule).slice(weekIndex * 7, weekIndex * 7 + 7)
  return dates.length ? `${formatDate(dates[0])}–${formatDate(dates[dates.length - 1])}` : ''
}

function scheduleDates(schedule: MealPlannerSchedule) {
  return [...new Set(schedule.days.map(day => day.date))].sort()
}

function weekMeals(schedule: MealPlannerSchedule, weekIndex: number) {
  const dates = new Set(scheduleDates(schedule).slice(weekIndex * 7, weekIndex * 7 + 7))
  return schedule.days.filter(day => dates.has(day.date))
}

function localToday() {
  const value = new Date()
  return `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, '0')}-${String(value.getDate()).padStart(2, '0')}`
}

function findSummaryDay(schedules: MealPlannerSchedule[], today: string) {
  const exactSchedule = schedules.find(schedule => schedule.days.some(day => day.date === today))
  if (exactSchedule) {
    const meals = exactSchedule.days.filter(day => day.date === today)
    return { schedule: exactSchedule, meals, next: null, isToday: true }
  }
  const dated = schedules.flatMap(schedule => scheduleDates(schedule).map(date => ({ schedule, date })))
  const selected = dated.filter(item => item.date > today).sort((a, b) => a.date.localeCompare(b.date))[0]
    ?? dated.sort((a, b) => b.date.localeCompare(a.date))[0]
  return selected ? { schedule: selected.schedule, meals: selected.schedule.days.filter(day => day.date === selected.date), next: null, isToday: false } : null
}

export function MealPlanner({
  expanded = true,
  onToggle = () => undefined,
  status = 'Available',
  today = localToday(),
  presentation = 'dashboard',
}: {
  expanded?: boolean
  onToggle?: () => void
  status?: string
  today?: string
  presentation?: 'dashboard' | 'family'
}) {
  const [meals, setMeals] = useState<MealPlannerMeal[]>([])
  const [tags, setTags] = useState<MealPlannerTag[]>([])
  const [schedules, setSchedules] = useState<MealPlannerSchedule[]>([])
  const [activeScheduleId, setActiveScheduleId] = useState('')
  const [activeWeek, setActiveWeek] = useState(0)
  const [activeTab, setActiveTab] = useState<MealPlannerTab>('schedule')
  const [replaceKey, setReplaceKey] = useState<string | null>(null)
  const [manualMode, setManualMode] = useState(false)
  const [manualMealId, setManualMealId] = useState('')
  const [busyDay, setBusyDay] = useState<string | null>(null)
  const [selectedPrintIds, setSelectedPrintIds] = useState<string[]>([])
  const [mealName, setMealName] = useState('')
  const [mealTagIds, setMealTagIds] = useState<string[]>([])
  const [editingMealId, setEditingMealId] = useState<string | null>(null)
  const [showMealForm, setShowMealForm] = useState(false)
  const [mealSearch, setMealSearch] = useState('')
  const [filterTagIds, setFilterTagIds] = useState<string[]>([])
  const [showFilters, setShowFilters] = useState(false)
  const [tagName, setTagName] = useState('')
  const [tagCategory, setTagCategory] = useState<MealPlannerTag['category']>('custom')
  const [startDate, setStartDate] = useState('2026-08-03')
  const [weekCount, setWeekCount] = useState(1)
  const [title, setTitle] = useState('')
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [loading, setLoading] = useState(true)

  const reload = async () => {
    const [nextMeals, nextTags, nextSchedules] = await Promise.all([
      getMealPlannerMeals(), getMealPlannerTags(), getMealPlannerSchedules(),
    ])
    setMeals(nextMeals); setTags(nextTags); setSchedules(nextSchedules)
    if (!activeScheduleId || !nextSchedules.some(item => item.id === activeScheduleId)) {
      const preferred = findSummaryDay(nextSchedules, today)
      setActiveScheduleId(preferred?.schedule.id ?? '')
      setActiveWeek(preferred ? Math.floor(scheduleDates(preferred.schedule).indexOf(preferred.meals[0].date) / 7) : 0)
    }
  }

  useEffect(() => {
    reload().catch((caught: unknown) => setError(caught instanceof Error ? caught.message : 'Matlista kunde inte laddas.')).finally(() => setLoading(false))
  }, [])

  const activeSchedule = schedules.find(schedule => schedule.id === activeScheduleId) ?? null
  const weekTotal = activeSchedule ? Math.ceil(scheduleDates(activeSchedule).length / 7) : 0
  const activeDays = activeSchedule ? weekMeals(activeSchedule, activeWeek) : []
  const activeDates = activeSchedule ? scheduleDates(activeSchedule).slice(activeWeek * 7, activeWeek * 7 + 7) : []
  const normalizedSearch = mealSearch.trim().toLocaleLowerCase('sv-SE')
  const visibleMeals = meals.filter(meal =>
    meal.name.toLocaleLowerCase('sv-SE').includes(normalizedSearch)
    && filterTagIds.every(tagId => meal.tagIds.includes(tagId)))
  const tagNames = useMemo(() => new Map(tags.map(tag => [tag.id, tag.name])), [tags])
  const groupedTags = useMemo(() => [
    { category: 'mealType', label: 'Måltid' },
    { category: 'portion', label: 'Antal personer' },
    { category: 'occasion', label: 'Tillfälle' },
    { category: 'custom', label: 'Övrigt' },
  ].map(group => ({ ...group, tags: tags.filter(tag => tag.category === group.category) })).filter(group => group.tags.length), [tags])
  const summary = findSummaryDay(schedules, today)

  const run = async (action: () => Promise<void>) => {
    try { setError(''); setNotice(''); await action() }
    catch (caught) { setError(caught instanceof Error ? caught.message : 'Åtgärden misslyckades.') }
  }

  const resetMealForm = () => {
    setMealName(''); setMealTagIds([]); setEditingMealId(null); setShowMealForm(false)
  }
  const saveMeal = () => run(async () => {
    if (editingMealId) await updateMealPlannerMeal(editingMealId, mealName, mealTagIds)
    else await createMealPlannerMeal(mealName, mealTagIds)
    resetMealForm(); await reload()
  })
  const updateSchedule = (schedule: MealPlannerSchedule) => {
    setSchedules(current => current.map(item => item.id === schedule.id ? schedule : item))
  }
  const selectTab = (tab: MealPlannerTab, focus = false) => {
    setActiveTab(tab)
    if (focus) window.requestAnimationFrame(() => document.getElementById(`meal-planner-tab-${tab}`)?.focus())
  }
  const handleTabKey = (event: React.KeyboardEvent<HTMLButtonElement>, index: number) => {
    let nextIndex: number | null = null
    if (event.key === 'ArrowRight') nextIndex = (index + 1) % tabs.length
    if (event.key === 'ArrowLeft') nextIndex = (index - 1 + tabs.length) % tabs.length
    if (event.key === 'Home') nextIndex = 0
    if (event.key === 'End') nextIndex = tabs.length - 1
    if (nextIndex === null) return
    event.preventDefault(); selectTab(tabs[nextIndex].id, true)
  }
  const openReplace = (day: MealPlannerDay) => {
    setReplaceKey(`${day.date}:${day.mealType}`); setManualMode(false); setManualMealId(day.mealId); setError(''); setNotice('')
  }
  const replaceAutomatically = (day: MealPlannerDay) => run(async () => {
    if (!activeSchedule) return
    const key = `${day.date}:${day.mealType}`; setBusyDay(key)
    try {
      updateSchedule(await replaceMealPlannerDay(activeSchedule.id, day.date, day.mealType))
      setReplaceKey(null); setNotice(`${dayNames[day.dayOfWeek]} ${day.mealType === 'lunch' ? 'lunch' : 'middag'} uppdaterades.`)
    } finally { setBusyDay(null) }
  })
  const replaceManually = (day: MealPlannerDay) => run(async () => {
    if (!activeSchedule) return
    const key = `${day.date}:${day.mealType}`; setBusyDay(key)
    try {
      updateSchedule(await setMealPlannerDay(activeSchedule.id, day.date, day.mealType, manualMealId))
      setReplaceKey(null); setNotice(`${dayNames[day.dayOfWeek]} ${day.mealType === 'lunch' ? 'lunch' : 'middag'} uppdaterades.`)
    } finally { setBusyDay(null) }
  })

  const summaryDinner = summary?.meals.find(day => day.mealType === 'dinner')
  const summaryLunch = summary?.meals.find(day => day.mealType === 'lunch')
  const collapsedSummary = loading ? <p aria-live="polite">Laddar dagens middag…</p> : summary && summaryDinner ? (
    <div className="meal-planner__today-summary">
      <p>{summary.isToday ? `I dag, ${dayNames[summaryDinner.dayOfWeek].toLocaleLowerCase('sv-SE')}` : `Närmaste matsedel, ${dayNames[summaryDinner.dayOfWeek].toLocaleLowerCase('sv-SE')}`} · {formatDate(summaryDinner.date)} · {summaryDinner.peopleCount} personer</p>
      {summaryLunch && <strong>Lunch: {summaryLunch.mealName}</strong>}
      <strong>Middag: {summaryDinner.mealName}</strong>
    </div>
  ) : <p className="meal-planner__empty-summary">Ingen matsedel skapad</p>

  return (
    <CollapsibleModule
      actions={status.toLowerCase() === 'available' ? undefined : <span className={`status-badge status-badge--${status.toLowerCase()}`}>{status}</span>}
      className="meal-planner"
      collapsedSummary={collapsedSummary}
      eyebrow="Familjemodul"
      expanded={expanded}
      moduleId="meal-planner"
      onToggle={onToggle}
      title="Matlista"
      variant={presentation === 'family' ? 'family' : 'dashboard'}
    >
      {error && <p className="notice notice--error" role="alert">{error}</p>}
      {notice && <p className="notice notice--success" role="status">{notice}</p>}
      {loading ? <p aria-live="polite">Laddar Matlista…</p> : <>
        <div aria-label="Matlista arbetsläge" className="meal-planner__tabs" role="tablist">
          {tabs.map((tab, index) => <button
            aria-controls={`meal-planner-panel-${tab.id}`}
            aria-selected={activeTab === tab.id}
            className="meal-planner__tab"
            id={`meal-planner-tab-${tab.id}`}
            key={tab.id}
            onClick={() => selectTab(tab.id)}
            onKeyDown={event => handleTabKey(event, index)}
            role="tab"
            tabIndex={activeTab === tab.id ? 0 : -1}
            type="button"
          >{tab.label}</button>)}
        </div>

        <section aria-labelledby="meal-planner-tab-schedule" hidden={activeTab !== 'schedule'} id="meal-planner-panel-schedule" role="tabpanel">
          <div className="meal-planner__week card">
            {activeSchedule ? <>
              <div className="meal-planner__week-heading">
                <button aria-label="Föregående vecka" className="meal-planner__week-button" disabled={activeWeek === 0} onClick={() => setActiveWeek(value => value - 1)} type="button">‹</button>
                <div><p className="eyebrow">{activeSchedule.title || 'Sparad matsedel'}</p><h3>Vecka {activeWeek + 1} · {weekLabel(activeSchedule, activeWeek)}</h3></div>
                <button aria-label="Nästa vecka" className="meal-planner__week-button" disabled={activeWeek >= weekTotal - 1} onClick={() => setActiveWeek(value => value + 1)} type="button">›</button>
              </div>
              <ol className="meal-planner__days">
                {activeDates.map(date => {
                  const dayMeals = activeDays.filter(day => day.date === date)
                  const first = dayMeals[0]
                  return <li className={`meal-planner__day${date === today ? ' meal-planner__day--today' : ''}`} key={date}>
                    <div className="meal-planner__day-copy">
                      <div><strong>{dayNames[first.dayOfWeek]}</strong>{date === today && <small className="meal-planner__today-label">Idag</small>}</div>
                      <span>{formatDate(date)} · {first.peopleCount} personer</span>
                    </div>
                    <div className="meal-planner__day-meals">{dayMeals.map(day => {
                      const key = `${day.date}:${day.mealType}`
                      return <div className="meal-planner__meal-entry" key={key}>
                        <div className="meal-planner__day-meal"><small>{day.mealType === 'lunch' ? 'Lunch' : 'Middag'}</small><strong>{day.mealName}</strong>{day.isManuallyReplaced && <small>Utbytt</small>}</div>
                        <button aria-expanded={replaceKey === key} aria-label={`Byt ${day.mealType === 'lunch' ? 'lunch' : 'middag'} ${dayNames[day.dayOfWeek]}`} className="meal-planner__replace-button secondary-button" onClick={() => replaceKey === key ? setReplaceKey(null) : openReplace(day)} type="button">Byt</button>
                        {replaceKey === key && <div className="meal-planner__replace-panel">
                          <button disabled={busyDay === key} onClick={() => void replaceAutomatically(day)} type="button">{busyDay === key ? 'Byter…' : 'Föreslå en annan'}</button>
                          {!manualMode && <button className="secondary-button" onClick={() => setManualMode(true)} type="button">Välj maträtt manuellt</button>}
                          {manualMode && <div className="meal-planner__manual-choice">
                            <label>Välj maträtt<select value={manualMealId} onChange={event => setManualMealId(event.target.value)}>{meals.map(meal => <option key={meal.id} value={meal.id}>{meal.name}</option>)}</select></label>
                            <button disabled={busyDay === key || !manualMealId} onClick={() => void replaceManually(day)} type="button">Använd vald</button>
                          </div>}
                          <button className="secondary-button" onClick={() => { setReplaceKey(null); setManualMode(false) }} type="button">Avbryt</button>
                        </div>}
                      </div>
                    })}</div>
                  </li>
                })}
              </ol>
            </> : <div className="meal-planner__empty"><strong>Ingen matsedel skapad</strong><p>Gå till Generera när du vill skapa familjens första matsedel.</p></div>}
          </div>
        </section>

        <section aria-labelledby="meal-planner-tab-meals" hidden={activeTab !== 'meals'} id="meal-planner-panel-meals" role="tabpanel">
          <div className="meal-planner__workspace card">
            <header className="meal-planner__workspace-header"><div><p className="eyebrow">Bibliotek</p><h3>Maträtter</h3></div><button aria-label="Lägg till maträtt" className="meal-planner__add-button secondary-button" onClick={() => { resetMealForm(); setShowMealForm(true) }} type="button">+ Lägg till</button></header>
            {showMealForm && <form className="meal-planner__edit-form" onSubmit={event => { event.preventDefault(); void saveMeal() }}>
              <label>Namn<input required value={mealName} onChange={event => setMealName(event.target.value)} /></label>
              <fieldset><legend>Taggar</legend>{tags.map(tag => <label key={tag.id}><input checked={mealTagIds.includes(tag.id)} onChange={() => setMealTagIds(ids => ids.includes(tag.id) ? ids.filter(id => id !== tag.id) : [...ids, tag.id])} type="checkbox" />{tag.name}</label>)}</fieldset>
              <div className="meal-planner__form-actions"><button type="submit">{editingMealId ? 'Spara ändring' : 'Lägg till maträtt'}</button><button className="secondary-button" onClick={resetMealForm} type="button">Avbryt</button></div>
            </form>}
            <div className="meal-planner__library-tools">
              <label>Sök maträtt<input placeholder="Sök på namn" type="search" value={mealSearch} onChange={event => setMealSearch(event.target.value)} /></label>
              <button aria-controls="meal-planner-filter-panel" aria-expanded={showFilters} className="meal-planner__filter-button secondary-button" onClick={() => setShowFilters(true)} type="button">Filter{filterTagIds.length ? ` (${filterTagIds.length})` : ''}</button>
              {showFilters && <div aria-labelledby="meal-planner-filter-title" className="meal-planner__filter-panel" id="meal-planner-filter-panel" onKeyDown={event => { if (event.key === 'Escape') setShowFilters(false) }} role="dialog">
                <header><h4 id="meal-planner-filter-title">Filtrera maträtter</h4><button aria-label="Stäng filter" className="secondary-button" onClick={() => setShowFilters(false)} type="button">Stäng</button></header>
                <div className="meal-planner__filter-groups">{groupedTags.map(group => <fieldset key={group.category}><legend>{group.label}</legend>{group.tags.map(tag => <label key={tag.id}><input checked={filterTagIds.includes(tag.id)} onChange={() => setFilterTagIds(ids => ids.includes(tag.id) ? ids.filter(id => id !== tag.id) : [...ids, tag.id])} type="checkbox" />{tag.name}</label>)}</fieldset>)}</div>
                <button className="secondary-button" disabled={filterTagIds.length === 0} onClick={() => setFilterTagIds([])} type="button">Rensa filter</button>
              </div>}
            </div>
            <p className="meal-planner__match-count" aria-live="polite">{visibleMeals.length} av {meals.length} maträtter</p>
            {meals.length === 0 && <div className="meal-planner__empty"><strong>Inga maträtter ännu</strong><p>Lägg till en egen rätt eller fyll biblioteket med säker testdata.</p><button onClick={() => { if (window.confirm('Lägg in 24 exempelrätter? Befintliga maträtter ändras inte.')) void run(async () => { const result = await seedMealPlannerExamples(); await reload(); setNotice(`${result.createdCount} exempelrätter lades till.`) }) }} type="button">Lägg in exempelrätter</button></div>}
            {meals.length > 0 && visibleMeals.length === 0 && <p className="meal-planner__empty">Inga maträtter matchar sökning och filter.</p>}
            <ul className="meal-planner__meals">{visibleMeals.map(meal => <li key={meal.id}><div><strong>{meal.name}</strong><small>{meal.tagIds.map(id => tagNames.get(id)).filter(Boolean).join(', ') || 'Utan taggar'}</small></div><details className="meal-planner__row-menu"><summary aria-label={`Åtgärder för ${meal.name}`}>•••</summary><div><button className="secondary-button" onClick={() => { setEditingMealId(meal.id); setMealName(meal.name); setMealTagIds([...meal.tagIds]); setShowMealForm(true) }} type="button">Redigera</button><button className="secondary-button" onClick={() => { if (window.confirm(`Ta bort ${meal.name}?`)) void run(async () => { await deleteMealPlannerMeal(meal.id); await reload() }) }} type="button">Ta bort</button></div></details></li>)}</ul>
            <details className="meal-planner__tag-manager"><summary>Hantera taggar</summary>
              <div><p><strong>Standardtaggar</strong> är skyddade. Egna taggar kan tas bort.</p>
                <form className="meal-planner__tag-form" onSubmit={event => { event.preventDefault(); void run(async () => { await createMealPlannerTag(tagName, tagCategory); setTagName(''); await reload() }) }}>
                  <label>Namn<input required value={tagName} onChange={event => setTagName(event.target.value)} /></label>
                  <label>Kategori<select value={tagCategory} onChange={event => setTagCategory(event.target.value as MealPlannerTag['category'])}><option value="portion">Portion</option><option value="occasion">Tillfälle</option><option value="mealType">Måltidstyp</option><option value="custom">Egen</option></select></label>
                  <button type="submit">Skapa tagg</button>
                </form>
                <ul className="meal-planner__tags">{tags.map(tag => <li key={tag.id}><span><strong>{tag.name}</strong><small>{tag.isProtected ? 'Standardtagg' : 'Egen tagg'} · {tag.category}</small></span>{!tag.isProtected && <button className="secondary-button" onClick={() => { if (window.confirm(`Ta bort taggen ${tag.name}?`)) void run(async () => { await deleteMealPlannerTag(tag.id); await reload() }) }} type="button">Ta bort</button>}</li>)}</ul>
              </div>
            </details>
          </div>
        </section>

        <section aria-labelledby="meal-planner-tab-generate" hidden={activeTab !== 'generate'} id="meal-planner-panel-generate" role="tabpanel">
          <div className="meal-planner__workspace card"><p className="eyebrow">Ny matsedel</p><h3>Generera matsedel</h3>
            <form className="meal-planner__generate-form" onSubmit={event => { event.preventDefault(); void run(async () => { const created = await generateMealPlannerSchedule(startDate, weekCount, title); await reload(); setActiveScheduleId(created.id); setActiveWeek(0); setActiveTab('schedule'); setNotice('Matsedeln skapades och sparades.') }) }}>
              <label>Startdatum<input type="date" value={startDate} onChange={event => setStartDate(event.target.value)} /></label>
              <label>Antal veckor<input max="12" min="1" type="number" value={weekCount} onChange={event => setWeekCount(Number(event.target.value))} /></label>
              <label>Titel (valfri)<input value={title} onChange={event => setTitle(event.target.value)} /></label>
              <button type="submit">Generera matsedel</button>
            </form>
          </div>
        </section>

        <section aria-labelledby="meal-planner-tab-saved" hidden={activeTab !== 'saved'} id="meal-planner-panel-saved" role="tabpanel">
          <div className="meal-planner__workspace card"><p className="eyebrow">Arkiv</p><h3>Sparade matsedlar</h3>
            {schedules.length === 0 ? <p className="meal-planner__empty">Inga sparade matsedlar.</p> : <ul className="meal-planner__saved-list">{schedules.map(schedule => <li key={schedule.id}><div><strong>{schedule.title || 'Matsedel'}</strong><small>{formatDate(schedule.startDate)}–{formatDate(schedule.endDate)} · {Math.ceil(scheduleDates(schedule).length / 7)} veckor · {schedule.days.length} måltider</small></div><button className="secondary-button" onClick={() => { setActiveScheduleId(schedule.id); setActiveWeek(0); setActiveTab('schedule') }} type="button">Öppna</button><label><input checked={selectedPrintIds.includes(schedule.id)} onChange={() => setSelectedPrintIds(ids => ids.includes(schedule.id) ? ids.filter(id => id !== schedule.id) : [...ids, schedule.id])} type="checkbox" /> Skriv ut</label><details className="meal-planner__row-menu"><summary aria-label={`Åtgärder för ${schedule.title || 'matsedel'}`}>•••</summary><div><button className="secondary-button" onClick={() => { if (window.confirm('Ta bort den sparade matsedeln?')) void run(async () => { await deleteMealPlannerSchedule(schedule.id); await reload() }) }} type="button">Ta bort</button></div></details></li>)}</ul>}
            <button disabled={selectedPrintIds.length === 0} onClick={() => window.print()} type="button">Skriv ut valda</button>
          </div>
        </section>

        <div aria-hidden="true" className="meal-planner-print">
          {schedules.filter(schedule => selectedPrintIds.includes(schedule.id)).map(schedule => <section key={schedule.id}>{Array.from({ length: Math.ceil(scheduleDates(schedule).length / 7) }, (_, weekIndex) => {
            const printDays = weekMeals(schedule, weekIndex)
            return <article className="meal-planner-print__week" key={printDays[0]?.date}><h1>{schedule.title || 'Matlista'}</h1><h2>Vecka {weekIndex + 1}: {weekLabel(schedule, weekIndex)}</h2><table><thead><tr><th>Dag</th><th>Datum</th><th>Måltid</th><th>Maträtt</th><th>Personer</th></tr></thead><tbody>{printDays.map(day => <tr key={`${day.date}:${day.mealType}`}><td>{dayNames[day.dayOfWeek]}</td><td>{formatDate(day.date)}</td><td>{day.mealType === 'lunch' ? 'Lunch' : 'Middag'}</td><td>{day.mealName}</td><td>{day.peopleCount}</td></tr>)}</tbody></table></article>
          })}</section>)}
        </div>
      </>}
    </CollapsibleModule>
  )
}
