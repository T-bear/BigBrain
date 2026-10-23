import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { SchoolLunch } from './SchoolLunch'
import * as api from '../api'
vi.mock('../api', () => ({ getSchoolMeals: vi.fn() }))
afterEach(() => vi.clearAllMocks())

describe('SchoolLunch', () => {
  it('shows both schools offline without requesting anything', () => {
    render(<SchoolLunch allowFixture={false} />)
    expect(screen.getByRole('heading', { name: 'Skollunch' })).toBeInTheDocument()
    expect(screen.getByText('Rosenfeldtskolan')).toBeInTheDocument()
    expect(screen.getByText('Musikugglan')).toBeInTheDocument()
    expect(screen.queryByRole('button')).not.toBeInTheDocument()
    expect(api.getSchoolMeals).not.toHaveBeenCalled()
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
    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('Testmenyn kunde inte läsas'))
  })
})
