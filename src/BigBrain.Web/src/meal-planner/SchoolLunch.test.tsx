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
      week: 43,
      status: 'published',
      dateBasis: 'sourceGuid',
      fetchedAtUtc: null,
      isFixture: true,
      days: Array.from({ length: 5 }, (_, i) => ({
        date: `2026-10-${19 + i}`,
        status: i === 0 && id === 'rosenfeldt' ? 'published' : 'missing',
        dishes: i === 0 && id === 'rosenfeldt' ? ['Testtext', 'Testtext'] : [],
      })),
    }))
    const view = render(<SchoolLunch allowFixture />)
    fireEvent.click(screen.getByRole('button', { name: 'Visa testvecka 43, 2026' }))
    expect(await screen.findAllByText('Testtext')).toHaveLength(2)
    expect(screen.getByText(/TESTDATA/)).toBeInTheDocument()
    expect(api.getSchoolMeals).toHaveBeenCalledWith('rosenfeldt', 2026, 43, true, expect.any(AbortSignal))
    const signal = vi.mocked(api.getSchoolMeals).mock.calls[0][4]!
    view.unmount()
    expect(signal.aborted).toBe(true)
  })
  it('reports a failed local preview honestly', async () => {
    vi.mocked(api.getSchoolMeals).mockRejectedValue(new Error('offline'))
    render(<SchoolLunch allowFixture />)
    fireEvent.click(screen.getByRole('button', { name: 'Visa testvecka 43, 2026' }))
    await waitFor(() => expect(screen.getByRole('alert')).toHaveTextContent('Testmenyn kunde inte läsas'))
  })
})
