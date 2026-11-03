import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { SchoolLunch } from './SchoolLunch'
import * as api from '../api'
vi.mock('../api', () => ({ getSchoolMeals: vi.fn() }))
afterEach(() => {
  cleanup()
  vi.resetAllMocks()
  vi.useRealTimers()
})

describe('SchoolLunch', () => {
  const liveMenu = (id: string, seconds: number): api.SchoolMealWeek => ({
    schoolId: id,
    schoolName: id,
    sourceName: 'Testkälla',
    sourceUrl: 'https://example.org',
    year: 2026,
    week: 43,
    status: 'published',
    dateBasis: 'sourceGuid',
    fetchedAtUtc: new Date().toISOString(),
    isFixture: false,
    validUntilUtc: new Date(Date.now() + seconds * 1000).toISOString(),
    days: [{ date: '2026-10-19', status: 'published', dishes: [id + ' lunch'] }],
  })

  it('checks a weekly snapshot after 24 hours, not on every return', async () => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date('2026-10-19T10:00:00Z'))

    vi.mocked(api.getSchoolMeals).mockImplementation(async id => ({
      ...liveMenu(id, 7 * 24 * 60 * 60),
      lastCheckedAtUtc: new Date().toISOString(),
    }))

    render(<SchoolLunch allowFixture={false} />)
    await act(async () => {})

    expect(api.getSchoolMeals).toHaveBeenCalledTimes(2)

    fireEvent.blur(window)
    vi.setSystemTime(new Date('2026-10-19T16:00:00Z'))
    fireEvent.focus(window)
    await act(async () => {})

    expect(api.getSchoolMeals).toHaveBeenCalledTimes(2)

    fireEvent.blur(window)
    vi.setSystemTime(new Date('2026-10-20T10:01:00Z'))
    fireEvent.focus(window)
    await act(async () => {})

    expect(api.getSchoolMeals).toHaveBeenCalledTimes(4)
    expect(screen.getByText('rosenfeldt lunch')).toBeInTheDocument()
  })

  it.each(['focus', 'visibilitychange'])(
    'expires locally and refreshes only the expired school once on %s',
    async event => {
      vi.useFakeTimers()
      vi.setSystemTime(new Date('2026-10-19T10:00:00Z'))
      vi.mocked(api.getSchoolMeals).mockImplementation(async id => liveMenu(id, id === 'rosenfeldt' ? 60 : 3600))
      const view = render(<SchoolLunch allowFixture={false} />)
      await act(async () => {})
      fireEvent.focus(window)
      fireEvent(document, new Event('visibilitychange'))
      expect(api.getSchoolMeals).toHaveBeenCalledTimes(2)
      await act(async () => {
        vi.advanceTimersByTime(60000)
      })
      expect(screen.queryByText('rosenfeldt lunch')).not.toBeInTheDocument()
      expect(screen.getByText('musikugglan lunch')).toBeInTheDocument()
      expect(api.getSchoolMeals).toHaveBeenCalledTimes(2) // Timer never fetches.
      fireEvent.focus(window)
      fireEvent(document, new Event('visibilitychange'))
      expect(api.getSchoolMeals).toHaveBeenCalledTimes(2) // No absence, so not a new return.
      let resolve: (value: api.SchoolMealWeek) => void = () => {}
      vi.mocked(api.getSchoolMeals).mockImplementation(
        () =>
          new Promise(done => {
            resolve = done
          }),
      )
      const resume = () => {
        fireEvent.blur(window)
        if (event === 'focus') fireEvent.focus(window)
        else fireEvent(document, new Event('visibilitychange'))
      }
      resume()
      fireEvent.focus(window)
      fireEvent(document, new Event('visibilitychange'))
      expect(api.getSchoolMeals).toHaveBeenCalledTimes(3)
      expect(vi.mocked(api.getSchoolMeals).mock.calls[2][0]).toBe('rosenfeldt')
      await act(async () => {
        resolve(liveMenu('rosenfeldt', 3600))
      })
      expect(screen.getByText('rosenfeldt lunch')).toBeInTheDocument()
      resume()
      expect(api.getSchoolMeals).toHaveBeenCalledTimes(3)
      // A new displayed deadline permits a new check; a late response is ignored after unmount.
      vi.setSystemTime(new Date(Date.now() + 3600000))
      resume()
      expect(api.getSchoolMeals).toHaveBeenCalledTimes(5)
      const signal = vi.mocked(api.getSchoolMeals).mock.calls[4][4]!
      view.unmount()
      expect(signal.aborted).toBe(true)
      await act(async () => {
        resolve(liveMenu('musikugglan', 3600))
      })
      expect(screen.queryByText('musikugglan lunch')).not.toBeInTheDocument()
    },
  )

  it.each(['blocked', 'error', 'expired'])('retries a %s response only after another real return', async outcome => {
    vi.useFakeTimers()
    vi.mocked(api.getSchoolMeals).mockImplementation(async id => liveMenu(id, 60))
    render(<SchoolLunch allowFixture={false} />)
    await act(async () => {})
    vi.setSystemTime(new Date(Date.now() + 60000)) // Simulate a suspended timer.
    vi.mocked(api.getSchoolMeals).mockImplementation(async id => {
      if (outcome === 'error') throw new Error('offline')
      return outcome === 'expired' ? liveMenu(id, -1) : { ...liveMenu(id, 60), status: 'unavailable', days: [] }
    })
    fireEvent.blur(window)
    await act(async () => {
      fireEvent.focus(window)
    })
    expect(screen.queryByText('rosenfeldt lunch')).not.toBeInTheDocument()
    expect(screen.queryByText('musikugglan lunch')).not.toBeInTheDocument()
    for (let i = 0; i < 3; i++) {
      fireEvent.focus(window)
      fireEvent(document, new Event('visibilitychange'))
    }
    expect(api.getSchoolMeals).toHaveBeenCalledTimes(4)
    vi.mocked(api.getSchoolMeals).mockImplementation(async id => liveMenu(id, 3600))
    fireEvent.blur(window)
    await act(async () => {
      fireEvent.focus(window)
    })
    expect(api.getSchoolMeals).toHaveBeenCalledTimes(6)
    expect(screen.getByText('rosenfeldt lunch')).toBeInTheDocument()
    expect(screen.getByText('musikugglan lunch')).toBeInTheDocument()
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  it.each(['focus-first', 'visible-first'])(
    'retries on the separate 12:01:10 return after a floor-blocked 12:00:35 check (%s)',
    async order => {
      vi.useFakeTimers()
      vi.setSystemTime(new Date('2026-11-03T12:00:00Z'))
      const visibility = vi.spyOn(document, 'visibilityState', 'get').mockReturnValue('visible')
      const leave = () => {
        fireEvent.blur(window)
        visibility.mockReturnValue('hidden')
        fireEvent(document, new Event('visibilitychange'))
      }
      const resume = async () => {
        visibility.mockReturnValue('visible')
        // Resolve even the first event's promise before the paired event arrives.
        await act(async () => {
          if (order === 'focus-first') fireEvent.focus(window)
          else fireEvent(document, new Event('visibilitychange'))
        })
        await act(async () => {
          if (order === 'focus-first') fireEvent(document, new Event('visibilitychange'))
          else fireEvent.focus(window)
        })
      }
      try {
        vi.mocked(api.getSchoolMeals).mockImplementation(async id => liveMenu(id, id === 'rosenfeldt' ? 30 : 3600))
        render(<SchoolLunch allowFixture={false} />)
        await act(async () => {})
        expect(screen.getByText('rosenfeldt lunch')).toBeInTheDocument()
        await act(async () => {
          vi.advanceTimersByTime(30000)
        })
        expect(screen.queryByText('rosenfeldt lunch')).not.toBeInTheDocument()
        expect(api.getSchoolMeals).toHaveBeenCalledTimes(2)
        leave()
        vi.setSystemTime(new Date('2026-11-03T12:00:35Z'))
        vi.mocked(api.getSchoolMeals).mockImplementation(async id => ({
          ...liveMenu(id, 0),
          status: 'unavailable',
          days: [],
        }))
        await resume()
        expect(api.getSchoolMeals).toHaveBeenCalledTimes(3)
        expect(vi.mocked(api.getSchoolMeals).mock.calls[2][0]).toBe('rosenfeldt')
        expect(screen.queryByText('rosenfeldt lunch')).not.toBeInTheDocument()
        for (let i = 0; i < 3; i++) {
          fireEvent.focus(window)
          fireEvent(document, new Event('visibilitychange'))
        }
        await act(async () => {
          vi.advanceTimersByTime(30000)
        })
        expect(api.getSchoolMeals).toHaveBeenCalledTimes(3) // No retry at the server's floor or on duplicate events.
        leave()
        vi.setSystemTime(new Date('2026-11-03T12:01:10Z'))
        vi.mocked(api.getSchoolMeals).mockImplementation(async id => liveMenu(id, 3600))
        await resume()
        expect(api.getSchoolMeals).toHaveBeenCalledTimes(4)
        expect(screen.getByText('rosenfeldt lunch')).toBeInTheDocument()
        expect(screen.getByText('musikugglan lunch')).toBeInTheDocument()
        leave()
        await resume()
        expect(api.getSchoolMeals).toHaveBeenCalledTimes(4) // Both menus are still fresh.
      } finally {
        visibility.mockRestore()
      }
    },
  )

  it('does not overlap checks or queue a retry when another return occurs while pending', async () => {
    vi.useFakeTimers()
    vi.mocked(api.getSchoolMeals).mockImplementation(async id => liveMenu(id, id === 'rosenfeldt' ? 30 : 3600))
    const view = render(<SchoolLunch allowFixture={false} />)
    await act(async () => {})
    await act(async () => {
      vi.advanceTimersByTime(30000)
    })
    let resolve: (menu: api.SchoolMealWeek) => void = () => {}
    vi.mocked(api.getSchoolMeals).mockImplementation(
      () =>
        new Promise(done => {
          resolve = done
        }),
    )
    fireEvent.blur(window)
    fireEvent.focus(window)
    expect(api.getSchoolMeals).toHaveBeenCalledTimes(3)
    fireEvent.blur(window)
    fireEvent.focus(window)
    expect(api.getSchoolMeals).toHaveBeenCalledTimes(3)
    await act(async () => {
      resolve({ ...liveMenu('rosenfeldt', 0), status: 'unavailable', days: [] })
    })
    fireEvent.focus(window)
    fireEvent(document, new Event('visibilitychange'))
    expect(api.getSchoolMeals).toHaveBeenCalledTimes(3)
    fireEvent.blur(window)
    fireEvent.focus(window)
    expect(api.getSchoolMeals).toHaveBeenCalledTimes(4)
    const signal = vi.mocked(api.getSchoolMeals).mock.calls[3][4]!
    view.unmount()
    expect(signal.aborted).toBe(true)
    await act(async () => {
      resolve(liveMenu('rosenfeldt', 3600))
    })
    fireEvent.blur(window)
    fireEvent.focus(window)
    expect(api.getSchoolMeals).toHaveBeenCalledTimes(4)
    expect(screen.queryByText('rosenfeldt lunch')).not.toBeInTheDocument()
  })

  it('does not fetch while hidden, and refreshes on becoming visible', async () => {
    vi.useFakeTimers()
    vi.mocked(api.getSchoolMeals).mockImplementation(async id => liveMenu(id, 60))
    render(<SchoolLunch allowFixture={false} />)
    await act(async () => {})
    const visibility = vi.spyOn(document, 'visibilityState', 'get')
    try {
      visibility.mockReturnValue('hidden')
      vi.setSystemTime(new Date(Date.now() + 60000))
      fireEvent(document, new Event('visibilitychange'))
      fireEvent.focus(window)
      expect(api.getSchoolMeals).toHaveBeenCalledTimes(2)
      visibility.mockReturnValue('visible')
      await act(async () => {
        fireEvent(document, new Event('visibilitychange'))
      })
      expect(api.getSchoolMeals).toHaveBeenCalledTimes(4)
    } finally {
      visibility.mockRestore()
    }
  })

  it.each([undefined, null, '', 'not-a-date', '2000-01-01T00:00:00Z'])(
    'hides live dishes with invalid or expired deadline %s',
    async validUntilUtc => {
      vi.mocked(api.getSchoolMeals).mockResolvedValue({
        schoolId: 'rosenfeldt',
        schoolName: 'Rosenfeldtskolan',
        sourceName: 'Testkälla',
        sourceUrl: 'https://example.org',
        year: 2026,
        week: 43,
        status: 'published',
        dateBasis: 'sourceGuid',
        fetchedAtUtc: null,
        isFixture: false,
        validUntilUtc,
        days: [{ date: '2026-10-19', status: 'published', dishes: ['Expired lunch'] }],
      })
      render(<SchoolLunch allowFixture={false} />)
      expect(await screen.findAllByText('Kunde inte uppdatera matsedeln just nu. Försök igen senare.')).toHaveLength(2)
      expect(screen.queryByText('Expired lunch')).not.toBeInTheDocument()
      expect(api.getSchoolMeals).toHaveBeenCalledTimes(2)
    },
  )

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
    fireEvent.blur(window)
    fireEvent.focus(window)
    fireEvent(document, new Event('visibilitychange'))
    expect(api.getSchoolMeals).toHaveBeenCalledTimes(4)
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
  it('hides stale data without a deadline when the other school fails', async () => {
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
    await waitFor(() => expect(api.getSchoolMeals).toHaveBeenCalledTimes(2))
    expect(screen.queryByText('Pasta med spenat och ostsås')).not.toBeInTheDocument()
    expect(screen.queryByText(/Inaktuell meny/)).not.toBeInTheDocument()
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
    const validUntilUtc = new Date(Date.now() + 3600000).toISOString()
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
      validUntilUtc,
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
