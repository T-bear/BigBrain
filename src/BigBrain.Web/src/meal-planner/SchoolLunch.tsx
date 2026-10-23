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
  invalid: 'Menyn kunde inte tolkas säkert.',
  unavailable: 'Källan är tillfälligt otillgänglig.',
  cachePolicy: 'Källans cachevillkor behöver granskas innan menyn kan visas.',
}
const checkedTime = (value: string) => new Date(value).toLocaleString('sv-SE', { timeZone: 'Europe/Stockholm' })

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
    setMenus({})
    setFailed([])
    for (const school of schools) {
      void getSchoolMeals(
        school.id,
        fixture ? 2026 : undefined,
        fixture ? school.testWeek : undefined,
        fixture,
        controller.signal,
      )
        .then(menu => {
          if (!controller.signal.aborted) setMenus(current => ({ ...current, [school.id]: menu }))
        })
        .catch(() => {
          if (!controller.signal.aborted) setFailed(current => [...current, school.id])
        })
    }
    return () => controller.abort()
  }, [fixture, offlinePreview])

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
              {failed.includes(school.id) ? (
                <p role="alert">
                  {fixture
                    ? 'Testmenyn kunde inte läsas. Kontrollera att API körs i utvecklingsläge.'
                    : 'Skolmenyn kunde inte läsas. Försök igen när du öppnar Family View.'}
                </p>
              ) : !menu ? (
                <p role="status">{offlinePreview ? statusText.offline : 'Läser skolmeny…'}</p>
              ) : (
                <>
                  {menu.isFixture && <p>Historiskt offline-exempel från produktägarens manuellt hämtade fil.</p>}
                  {menu.isStale && <p role="status">Inaktuell meny – senaste kontrollen misslyckades.</p>}
                  {menu.status !== 'published' && (
                    <p role="status">{statusText[menu.status] ?? 'Information saknas.'}</p>
                  )}
                  {menu.status === 'published' &&
                    weekdays.map((day, index) => (
                      <div className="school-lunch-day" key={day}>
                        <h4>{day}</h4>
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
                </>
              )}
            </div>
          )
        })}
      </div>
    </section>
  )
}
