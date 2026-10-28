import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { SchoolLunch } from './SchoolLunch'
import * as api from '../api'
vi.mock('../api', () => ({ getSchoolMeals: vi.fn() }))
afterEach(() => {
  cleanup()
  vi.resetAllMocks()
})

describe('SchoolLunch', () => {
  it('reads current menus through the local API without exposing fixtures in production', async () => {
    vi.mocked(api.getSchoolMeals).mockResolvedValue({
      schoolId: 'rosenfeldt',
      schoolName: 'Rosenfeldtskolan',
      sourceName: 'Skolmat.info',
      sourceUrl: 'https://www.skolmat.info',
      year: 2026,
      week: 43,
      status: 'offline',
      dateBasis: 'unknown',
      fetchedAtUtc: null,
      isFixture: false,
      days: [],
    })
    render(<SchoolLunch allowFixture={false} />)
    expect(await screen.findAllByText('Extern hämtning är avstängd.')).toHaveLength(2)
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
    expect(api.getSchoolMeals).toHaveBeenCalledWith('rosenfeldt', undefined, undefined, false, expect.any(AbortSignal))
    expect(api.getSchoolMeals).toHaveBeenCalledWith('musikugglan', undefined, undefined, false, expect.any(AbortSignal))
  })
  it('loads only the explicit historical fixture and preserves identical alternatives', async () => {
    vi.mocked(api.getSchoolMeals).mockImplementation(async id => ({
      schoolId: id,
      schoolName: id === 'rosenfeldt' ? 'Rosenfeldtskolan' : 'Musikugglan',
      sourceName: 'Testkälla',
      sourceUrl: 'https://example.org',
      year: 2026,
      week: id === 'rosenfeldt' ? 43 : 44,
      status: 'published',
      dateBasis: id === 'rosenfeldt' ? 'sourceGuid' : 'htmlWeekRequestedYear',
      fetchedAtUtc: null,
      isFixture: true,
      days: Array.from({ length: 5 }, (_, i) => ({
        date: `2026-10-${(id === 'rosenfeldt' ? 19 : 26) + i}`,
        status: 'published',
        dishes:
          id === 'rosenfeldt'
            ? [
                ['Pasta med spenat och ostsås', 'Pasta serveras med ost & skinksås'],
                ['Grönsakragu serveras med kokt potatis', 'Krämigt örtbakad fisk serveras med kokt potatis'],
                [
                  'Korv serveras med potatismos, ketchup & senap',
                  'Stekt kycklingkorv serveras med potatismos, ketchup och senap',
                ],
                ['Gulaschsoppa serveras med bröd och ost', 'Gulaschsoppa serveras med bröd och ost'],
                ['Kockens val', 'Kockens val'],
              ][i]
            : [
                [
                  'Pasta med köttfärssås/Vegosås',
                  'Currygryta med kyckling samt ris/Vegogryta',
                  'Stekt falukorv med makaroner och ketchup/Vegokorv',
                  'Ugnspannkaka med baconströssel och stekt potatis,lingonsylt',
                  'Fläsknoisette och stekt kulpotatis samt kall sås /Vegobiff',
                ][i],
              ],
      })),
    }))
    const view = render(<SchoolLunch allowFixture />)
    fireEvent.click(screen.getByRole('button', { name: 'Visa testveckorna' }))
    expect(await screen.findAllByText('Kockens val')).toHaveLength(2)
    expect(screen.getByText('Pasta serveras med ost & skinksås')).toBeInTheDocument()
    expect(screen.getByText('Fläsknoisette och stekt kulpotatis samt kall sås /Vegobiff')).toBeInTheDocument()
    expect(screen.getByText(/TESTDATA/)).toBeInTheDocument()
    expect(api.getSchoolMeals).toHaveBeenCalledWith('rosenfeldt', 2026, 43, true, expect.any(AbortSignal))
    expect(api.getSchoolMeals).toHaveBeenCalledWith('musikugglan', 2026, 44, true, expect.any(AbortSignal))
    expect(screen.getByRole('heading', { name: 'Rosenfeldtskolan · vecka 43, 2026' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Musikugglan · vecka 44 (källans årtal saknas)' })).toBeInTheDocument()
    const signal = vi.mocked(api.getSchoolMeals).mock.calls[0][4]!
    view.unmount()
    expect(signal.aborted).toBe(true)
  })
  it('reports a failed local preview honestly', async () => {
    vi.mocked(api.getSchoolMeals).mockRejectedValue(new Error('offline'))
    render(<SchoolLunch allowFixture />)
    fireEvent.click(screen.getByRole('button', { name: 'Visa testveckorna' }))
    await waitFor(() => expect(screen.getAllByRole('alert')[0]).toHaveTextContent('Testmenyn kunde inte läsas'))
  })
  it('keeps the other school visible when one request fails and marks stale data', async () => {
    vi.mocked(api.getSchoolMeals).mockImplementation(async id => {
      if (id === 'musikugglan') throw new Error('unavailable')
      return {
        schoolId: id,
        schoolName: 'Rosenfeldtskolan',
        sourceName: 'Skolmat.info',
        sourceUrl: 'https://www.skolmat.info',
        year: 2026,
        week: 43,
        status: 'published',
        dateBasis: 'sourceGuid',
        fetchedAtUtc: '2026-10-19T10:00:00Z',
        lastCheckedAtUtc: '2026-10-19T16:00:00Z',
        isFixture: false,
        isStale: true,
        days: [{ date: '2026-10-19', status: 'published', dishes: ['Pasta med spenat och ostsås'] }],
      }
    })
    render(<SchoolLunch allowFixture={false} />)
    expect(await screen.findByText('Pasta med spenat och ostsås')).toBeInTheDocument()
    expect(screen.getByText(/Inaktuell meny/)).toBeInTheDocument()
    expect(screen.getByRole('alert')).toHaveTextContent('Kunde inte uppdatera matsedeln just nu. Försök igen senare.')
    expect(screen.queryByText(/TESTDATA/)).not.toBeInTheDocument()
  })
  it('clears previous dishes on mode change and ignores late responses', async () => {
    let resolveLate: (menu: api.SchoolMealWeek) => void = () => {}
    vi.mocked(api.getSchoolMeals).mockImplementation(
      () =>
        new Promise(resolve => {
          resolveLate = resolve
        }),
    )
    render(<SchoolLunch allowFixture />)
    const oldSignal = vi.mocked(api.getSchoolMeals).mock.calls[0][4]!
    fireEvent.click(screen.getByRole('button', { name: 'Visa testveckorna' }))
    expect(oldSignal.aborted).toBe(true)
    const fixtureReply = resolveLate
    fireEvent.click(screen.getByRole('button', { name: 'Stäng testvisning' }))
    fixtureReply({
      schoolId: 'musikugglan',
      schoolName: 'Musikugglan',
      sourceName: 'Tant Grön',
      sourceUrl: 'https://www.tantgron.nu',
      year: 2026,
      week: 44,
      status: 'published',
      dateBasis: 'htmlWeekRequestedYear',
      fetchedAtUtc: null,
      isFixture: true,
      days: [{ date: '2026-10-26', status: 'published', dishes: ['SEN TESTDATA'] }],
    })
    await waitFor(() => expect(screen.queryByText('SEN TESTDATA')).not.toBeInTheDocument())
  })
  it('shows calendar dates across ISO year boundaries and renders source text without executing markup', async () => {
    const unsafeText = '<img src=x onerror="alert(1)"><script>alert(1)</script>'
    vi.mocked(api.getSchoolMeals).mockImplementation(async id => ({
      schoolId: id,
      schoolName: id === 'rosenfeldt' ? 'Rosenfeldtskolan' : 'Musikugglan',
      sourceName: id === 'rosenfeldt' ? 'Skolmat.info' : 'Tant Grön',
      sourceUrl: id === 'rosenfeldt' ? 'https://www.skolmat.info' : 'https://www.tantgron.nu/matsedel/',
      year: 2026,
      week: 53,
      status: id === 'rosenfeldt' ? 'published' : 'offline',
      dateBasis: id === 'rosenfeldt' ? 'sourceGuid' : 'unknown',
      fetchedAtUtc: id === 'rosenfeldt' ? '2026-12-28T08:00:00Z' : null,
      sourceUpdatedAtUtc: id === 'rosenfeldt' ? '2026-12-27T12:00:00Z' : null,
      isFixture: false,
      days: [
        { date: '2026-12-28', status: 'published', dishes: [unsafeText, 'Pasta med spenat och ostsås'] },
        { date: '2026-12-29', status: 'missing', dishes: [] },
        { date: '2026-12-30', status: 'missing', dishes: [] },
        { date: '2026-12-31', status: 'missing', dishes: [] },
        { date: '2027-01-01', status: 'published', dishes: ['Kockens val', 'Kockens val'] },
      ],
    }))
    const { container } = render(<SchoolLunch allowFixture={false} />)
    expect(await screen.findByText(unsafeText)).toBeInTheDocument()
    expect(container.querySelector('img, script')).toBeNull()
    expect(screen.getByRole('heading', { name: 'Måndag · 28 december' })).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: 'Fredag · 1 januari' })).toBeInTheDocument()
    expect(container.querySelector('time[datetime="2027-01-01"]')).toHaveTextContent('1 januari')
    expect(screen.getByRole('heading', { name: 'Rosenfeldtskolan · vecka 53, 2026' })).toBeInTheDocument()
    expect(screen.getByText(/^Källans angivna ändringstid:/)).toHaveTextContent('2026-12-27')
    expect(screen.getByText(/^Menyn hämtad:/)).toHaveTextContent('2026-12-28')
    expect(screen.getAllByText('Meny saknas för dagen. Det betyder inte att skolan är stängd.')).toHaveLength(3)
    expect(screen.getAllByRole('listitem').map(item => item.textContent)).toEqual([
      unsafeText,
      'Pasta med spenat och ostsås',
      'Kockens val',
      'Kockens val',
    ])
    expect(screen.getByText('Extern hämtning är avstängd.')).toBeInTheDocument()
    expect(screen.queryByText(/TESTDATA/)).not.toBeInTheDocument()
  })
})
