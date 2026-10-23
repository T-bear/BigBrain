import { useEffect, useState } from 'react'
import { getSchoolMeals, type SchoolMealWeek } from '../api'
import { BBButton } from '../components'

const weekdays = ['Måndag', 'Tisdag', 'Onsdag', 'Torsdag', 'Fredag']
const statusText: Record<string, string> = {
  offline: 'Extern hämtning är avstängd.',
  notPublished: 'Ingen meny tillgänglig för vald vecka.',
  missing: 'Meny saknas för dagen. Det betyder inte att skolan är stängd.',
  invalid: 'Menyn kunde inte tolkas säkert.',
}

// Static offline preview is tied to its actual source week, never to the current date.
export function SchoolLunch({ allowFixture = import.meta.env.DEV }: { allowFixture?: boolean }) {
  const [fixture, setFixture] = useState(false)
  const [menus, setMenus] = useState<SchoolMealWeek[]>([])
  const [failed, setFailed] = useState(false)
  useEffect(() => {
    if (!fixture) return
    const controller = new AbortController()
    setMenus([])
    setFailed(false)
    void Promise.all([
      getSchoolMeals('rosenfeldt', 2026, 43, true, controller.signal),
      getSchoolMeals('musikugglan', 2026, 44, true, controller.signal),
    ])
      .then(data => {
        if (!controller.signal.aborted) setMenus(data)
      })
      .catch(() => {
        if (!controller.signal.aborted) setFailed(true)
      })
    return () => controller.abort()
  }, [fixture])

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
          {fixture
            ? 'TESTDATA – manuellt hämtade exempel, inte aktuell live-matsedel.'
            : 'Offline – extern hämtning är avstängd.'}
        </p>
        {allowFixture && (
          <BBButton variant="secondary" aria-pressed={fixture} onClick={() => setFixture(!fixture)}>
            {fixture ? 'Stäng testvisning' : 'Visa testveckorna'}
          </BBButton>
        )}
        {!fixture && (
          <>
            <h3>Rosenfeldtskolan</h3>
            <p>
              {statusText.offline} Källa:{' '}
              <a href="https://www.skolmat.info/matsedlar?facilityId=C9643AEF-D05B-463E-84CB-76BEB47FF685">
                Skolmat.info
              </a>
            </p>
            <h3>Musikugglan</h3>
            <p>
              {statusText.offline} Källa: <a href="https://www.tantgron.nu/matsedel/">Tant Grön</a>
            </p>
          </>
        )}
        {fixture && !failed && menus.length === 0 && <p role="status">Läser lokal testdata…</p>}
        {fixture && failed && (
          <p role="alert">Testmenyn kunde inte läsas. Kontrollera att API körs i utvecklingsläge.</p>
        )}
        {fixture &&
          menus.map(menu => (
            <div key={menu.schoolId}>
              <h3>
                {menu.schoolName} · vecka {menu.week}
                {menu.dateBasis === 'sourceGuid' ? `, ${menu.year}` : ' (källans årtal saknas)'}
              </h3>
              <p>
                {menu.schoolId === 'rosenfeldt'
                  ? 'Fem vardagar från den manuellt hämtade RSS-filen, vecka 43 år 2026.'
                  : 'Fem luncher från den manuellt hämtade HTML-filen, vecka 44. Visningsår 2026 enligt ägarens observation, inte verifierat i HTML.'}
              </p>
              {weekdays.map((day, index) => (
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
                Källa: <a href={menu.sourceUrl}>{menu.sourceName}</a>.{' '}
                {menu.dateBasis === 'sourceGuid'
                  ? 'Datum från RSS-identitet.'
                  : 'Datum i vald visningsvecka är härledda; källan anger endast veckonummer.'}{' '}
                Ingen extern hämtning.
              </p>
            </div>
          ))}
      </div>
    </section>
  )
}
