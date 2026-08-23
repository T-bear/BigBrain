import { useEffect, useState } from 'react'
import { getFinanceOverview, getMealPlannerSchedules, getMediaOverview } from '../api'
import { AppIcon } from '../AppIcon'
import { getCalendarWeek, type CalendarEvent } from '../calendar/calendarApi'
import { BBButton } from '../components'
import { getShoppingList } from '../shopping-list/shoppingListApi'
import type { FinanceOverview, MealPlannerDay, MediaOverview, SystemRecoverySnapshot } from '../types'
import { useWidgets } from './widgetFramework'

type HomeSnapshot = {
  todayMeals: MealPlannerDay[]
  nextEvent: CalendarEvent | null
  shoppingRemaining: number | null
  media: MediaOverview | null
  finance: FinanceOverview | null
}

const localDate = () => new Date().toLocaleDateString('sv-SE', { year: 'numeric', month: '2-digit', day: '2-digit' })
const eventTime = (event: CalendarEvent) => event.isAllDay ? 'Hela dagen' : [event.startTime, event.endTime].filter(Boolean).join('–')

export function HomeOverview({ recovery }: { recovery: SystemRecoverySnapshot | null }) {
  const { setActiveView } = useWidgets()
  const [snapshot, setSnapshot] = useState<HomeSnapshot>({ todayMeals: [], nextEvent: null, shoppingRemaining: null, media: null, finance: null })

  useEffect(() => {
    let current = true
    const update = (change: Partial<HomeSnapshot>) => { if (current) setSnapshot(previous => ({ ...previous, ...change })) }
    const today = localDate()
    void getMealPlannerSchedules().then(schedules => update({ todayMeals: schedules.flatMap(schedule => schedule.days).filter(day => day.date === today) })).catch(() => undefined)
    void getCalendarWeek().then(calendar => update({ nextEvent: calendar.events.filter(event => event.date >= today).sort((a, b) => `${a.date}${a.startTime ?? ''}`.localeCompare(`${b.date}${b.startTime ?? ''}`))[0] ?? null })).catch(() => undefined)
    void getShoppingList().then(shopping => update({ shoppingRemaining: shopping.items.filter(item => !item.purchased).length })).catch(() => undefined)
    void getMediaOverview().then(media => update({ media })).catch(() => undefined)
    void getFinanceOverview().then(finance => update({ finance })).catch(() => undefined)
    return () => { current = false }
  }, [])

  const mediaActive = snapshot.media?.qBittorrent.activeCount ?? 0
  const mediaWarnings = snapshot.media?.insights.filter(item => item.severity === 'warning' || item.severity === 'critical') ?? []
  const needsAttention = recovery && recovery.overall !== 'healthy' ? `Systemstatus: ${recovery.overall}` : mediaWarnings[0]?.title

  return <div className="home-overview">
    <section aria-labelledby="home-today-title" className="home-today">
      <header><p className="eyebrow">I dag</p><h2 id="home-today-title">Just nu</h2></header>
      <div className="home-today__grid">
        <div><span>Måltid</span><strong>{snapshot.todayMeals.length ? snapshot.todayMeals.map(meal => meal.mealName).join(' · ') : 'Ingen måltid planerad'}</strong></div>
        <div><span>Nästa i kalendern</span><strong>{snapshot.nextEvent ? `${snapshot.nextEvent.title} · ${eventTime(snapshot.nextEvent)}` : 'Inget kommande i veckan'}</strong></div>
      </div>
    </section>
    <div className="home-glances">
      <BBButton onClick={() => setActiveView('family')} type="button" variant="contextual"><AppIcon name="family" size={24} /><span><small>Familj</small><strong>{snapshot.shoppingRemaining === null ? 'Inköpslistan är inte tillgänglig' : snapshot.shoppingRemaining ? `${snapshot.shoppingRemaining} ${snapshot.shoppingRemaining === 1 ? 'vara' : 'varor'} kvar att handla` : 'Inköpslistan är klar'}</strong></span><AppIcon name="chevron" /></BBButton>
      <BBButton onClick={() => setActiveView('media')} type="button" variant="contextual"><AppIcon name="media" size={24} /><span><small>Media</small><strong>{snapshot.media ? mediaActive ? `${mediaActive} aktiva nedladdningar` : snapshot.media.healthSummary : 'Mediastatus är inte tillgänglig'}</strong></span><AppIcon name="chevron" /></BBButton>
      <BBButton onClick={() => setActiveView('finance')} type="button" variant="contextual"><AppIcon name="finance" size={24} /><span><small>Finance · RESEARCH</small><strong>{snapshot.finance ? snapshot.finance.signals.length ? `${snapshot.finance.signals.length} aktuella researchsignaler` : snapshot.finance.marketSummary : 'Researchstatus är inte tillgänglig'}</strong></span><AppIcon name="chevron" /></BBButton>
    </div>
    {needsAttention && <section aria-labelledby="home-attention-title" className="home-attention"><AppIcon name="admin" /><div><p className="eyebrow">Behöver uppmärksamhet</p><h2 id="home-attention-title">{needsAttention}</h2></div></section>}
  </div>
}
