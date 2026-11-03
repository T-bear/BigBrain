import { useEffect, useState } from 'react'
import { getSchoolMeals, type SchoolMealWeek } from '../api'
import { BBButton } from '../components'

const weekdays = ['Måndag', 'Tisdag', 'Onsdag', 'Torsdag', 'Fredag']
const schools = [
  { id: 'rosenfeldt', name: 'Rosenfeldtskolan', testWeek: 43 },
  { id: 'musikugglan', name: 'Musikugglan', testWeek: 44 },
]
const statusText: Record<string, string> = {
  offline: 'Extern hämtning är avstängd.',
  notPublished: 'Ingen meny tillgänglig för aktuell vecka.',
  missing: 'Meny saknas för dagen. Det betyder inte att skolan är stängd.',
  invalid: 'Kunde inte uppdatera matsedeln just nu. Försök igen senare.',
  unavailable: 'Kunde inte uppdatera matsedeln just nu. Försök igen senare.',
  cachePolicy: 'Kunde inte uppdatera matsedeln just nu. Försök igen senare.',
}
const checkedTime = (value: string) => new Date(value).toLocaleString('sv-SE', { timeZone: 'Europe/Stockholm' })
// API dates are calendar dates, not instants in the device's time zone.
const lunchDate = (value: string) =>
  new Date(`${value}T00:00:00Z`).toLocaleDateString('sv-SE', { day: 'numeric', month: 'long', timeZone: 'UTC' })

function expireMenu(menu: SchoolMealWeek): SchoolMealWeek {
  if (menu.isFixture || menu.status !== 'published') return menu
  const deadline = menu.validUntilUtc ? Date.parse(menu.validUntilUtc) : NaN
  if (Number.isFinite(deadline) && deadline > Date.now()) return menu
  return {
    ...menu,
    status: 'unavailable',
    sourceStatus: 'unavailable',
    days: [],
    isStale: false,
    fetchedAtUtc: null,
    sourceUpdatedAtUtc: null,
    validUntilUtc: null,
  }
}

export function SchoolLunch({
  allowFixture = import.meta.env.DEV,
  offlinePreview = false,
}: {
  allowFixture?: boolean
  offlinePreview?: boolean
}) {
  const [fixture, setFixture] = useState(false)
  const [menus, setMenus] = useState<Record<string, SchoolMealWeek>>({})
  const [failed, setFailed] = useState<string[]>([])
  useEffect(() => {
    if (offlinePreview) return
    const controller = new AbortController()
    // Retain expired displayed deadlines across failures; a later real return may retry.
    const refreshable = new Map<string, number>()
    const pending = new Set<string>()
    let away = document.visibilityState !== 'visible'
    setMenus({})
    setFailed([])
    const load = (school: (typeof schools)[number]) => {
      if (pending.has(school.id)) return
      pending.add(school.id)
      void getSchoolMeals(
        school.id,
        fixture ? 2026 : undefined,
        fixture ? school.testWeek : undefined,
        fixture,
        controller.signal,
      )
        .then(menu => {
          if (controller.signal.aborted) return
          const visible = expireMenu(menu)
          if (!visible.isFixture) {
            const weekEnd =
              visible.status === 'published' && visible.validUntilUtc ? Date.parse(visible.validUntilUtc) : Infinity
            const nextCheck = visible.lastCheckedAtUtc
              ? Math.min(Date.parse(visible.lastCheckedAtUtc) + 86400000, weekEnd)
              : weekEnd
            if (Number.isFinite(nextCheck)) refreshable.set(school.id, nextCheck)
          }
          setFailed(current => current.filter(id => id !== school.id))
          setMenus(current => ({ ...current, [school.id]: visible }))
        })
        .catch(() => {
          if (!controller.signal.aborted) setFailed(current => [...current, school.id])
        })
        .finally(() => pending.delete(school.id))
    }
    for (const school of schools) load(school)
    const resume = () => {
      if (fixture || document.visibilityState !== 'visible' || !away) return
      // Consume this return before requests settle, not the menu's future retry eligibility.
      away = false
      setMenus(current => Object.fromEntries(Object.entries(current).map(([id, menu]) => [id, expireMenu(menu)])))
      for (const school of schools) {
        const deadline = refreshable.get(school.id)
        if (deadline !== undefined && deadline <= Date.now()) {
          load(school)
        }
      }
    }
    const leave = () => {
      away = true
    }
    const visibilityChanged = () => {
      if (document.visibilityState !== 'visible') leave()
      else resume()
    }
    window.addEventListener('blur', leave)
    window.addEventListener('focus', resume)
    document.addEventListener('visibilitychange', visibilityChanged)
    return () => {
      controller.abort()
      window.removeEventListener('blur', leave)
      window.removeEventListener('focus', resume)
      document.removeEventListener('visibilitychange', visibilityChanged)
    }
  }, [fixture, offlinePreview])

  useEffect(() => {
    const deadlines = Object.values(menus)
      .filter(menu => !menu.isFixture && menu.status === 'published' && menu.validUntilUtc)
      .map(menu => Date.parse(menu.validUntilUtc!))
    if (deadlines.length === 0) return
    // Expire presentation locally; this must never initiate another API/provider request.
    const expire = () =>
      setMenus(current => Object.fromEntries(Object.entries(current).map(([id, menu]) => [id, expireMenu(menu)])))
    const timer = window.setTimeout(expire, Math.max(0, Math.min(...deadlines) - Date.now()))
    return () => {
      window.clearTimeout(timer)
    }
  }, [menus])

  return (
    <section className="family-section school-lunch" aria-labelledby="school-lunch-title">
      <header className="family-section__heading">
        <div>
          <p>Skolornas måltider</p>
          <h2 id="school-lunch-title">Skollunch</h2>
        </div>
      </header>
      <div className="family-section__content">
        <p role="status">
          {fixture ? 'TESTDATA – manuellt hämtade exempel, inte aktuell live-matsedel.' : 'Aktuell veckas skolluncher'}
        </p>
        {allowFixture && !offlinePreview && (
          <BBButton variant="secondary" aria-pressed={fixture} onClick={() => setFixture(!fixture)}>
            {fixture ? 'Stäng testvisning' : 'Visa testveckorna'}
          </BBButton>
        )}
        {schools.map(school => {
          const menu = menus[school.id]
          return (
            <div key={school.id}>
              <h3>
                {school.name}
                {menu && (
                  <>
                    {' '}
                    · vecka {menu.week}
                    {menu.dateBasis === 'htmlWeekRequestedYear' ? ' (källans årtal saknas)' : `, ${menu.year}`}
                  </>
                )}
              </h3>
              {failed.includes(school.id) && menu?.status !== 'published' ? (
                <p role="alert">
                  {fixture
                    ? 'Testmenyn kunde inte läsas. Kontrollera att API körs i utvecklingsläge.'
                    : 'Kunde inte uppdatera matsedeln just nu. Försök igen senare.'}
                </p>
              ) : !menu ? (
                <p role="status">{offlinePreview ? statusText.offline : 'Läser skolmeny…'}</p>
              ) : (
                <>
                  {failed.includes(school.id) && menu.status === 'published' && (
                    <p role="status">Kunde inte kontrollera matsedeln; visar senast hämtad information.</p>
                  )}
                  {menu.isFixture && <p>Historiskt offline-exempel från produktägarens manuellt hämtade fil.</p>}
                  {menu.isStale && (
                    <p role="status">
                      Matsedeln kan vara inaktuell – senaste kontrollen kunde inte bekräfta ny information.
                    </p>
                  )}
                  {menu.status !== 'published' && (
                    <p role="status">{statusText[menu.status] ?? 'Information saknas.'}</p>
                  )}
                  {menu.status === 'published' &&
                    weekdays.map((day, index) => (
                      <div className="school-lunch-day" key={day}>
                        <h4>
                          {day}
                          {menu.days[index] && (
                            <>
                              {' · '}
                              <time dateTime={menu.days[index].date}>{lunchDate(menu.days[index].date)}</time>
                            </>
                          )}
                        </h4>
                        {menu.days[index]?.status === 'published' ? (
                          <ul>
                            {menu.days[index].dishes.map((dish, order) => (
                              <li key={order}>{dish}</li>
                            ))}
                          </ul>
                        ) : (
                          <p>{statusText[menu.days[index]?.status] ?? 'Information saknas.'}</p>
                        )}
                      </div>
                    ))}
                  <p>
                    Källa: <a href={menu.sourceUrl}>{menu.sourceName}</a>.
                  </p>
                  {menu.dateBasis === 'sourceGuid' && <p>Datum från RSS-identitet.</p>}
                  {menu.dateBasis === 'htmlWeekRequestedYear' && (
                    <p>Visningsår {menu.year} är härlett; källan anger endast veckonummer.</p>
                  )}
                  {menu.lastCheckedAtUtc && <p>Senast kontrollerat: {checkedTime(menu.lastCheckedAtUtc)}.</p>}
                  {menu.fetchedAtUtc && (
                    <p>Menyn hämtad: {checkedTime(menu.fetchedAtUtc)}. Hämtningstid är inte källans publiceringstid.</p>
                  )}
                  {menu.sourceUpdatedAtUtc && (
                    <p>Källans angivna ändringstid: {checkedTime(menu.sourceUpdatedAtUtc)}.</p>
                  )}
                </>
              )}
            </div>
          )
        })}
      </div>
    </section>
  )
}
