import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, test, vi } from 'vitest'
import { MealPlanner } from './MealPlanner'
import type { MealPlannerTag } from '../types'

const initialTags: MealPlannerTag[] = [
  { id: 'portion-3-4', name: '3–4 personer', category: 'portion', createdAtUtc: '2026-08-01T00:00:00Z', isProtected: true },
  { id: 'easy', name: 'Lättlagat', category: 'occasion', createdAtUtc: '2026-08-01T00:00:00Z', isProtected: true },
  { id: 'meal-type-lunch', name: 'Lunch', category: 'mealType', createdAtUtc: '2026-08-01T00:00:00Z', isProtected: true },
  { id: 'custom', name: 'Vegetariskt', category: 'custom', createdAtUtc: '2026-08-01T00:00:00Z', isProtected: false },
]
const initialMeals = [
  { id: 'pasta', name: 'Pasta pesto', tagIds: ['easy', 'custom'], createdAtUtc: '2026-08-01T00:00:00Z', updatedAtUtc: '2026-08-01T00:00:00Z' },
  { id: 'soup', name: 'Soppa', tagIds: [], createdAtUtc: '2026-08-01T00:00:00Z', updatedAtUtc: '2026-08-01T00:00:00Z' },
  { id: 'pancakes', name: 'Pannkakor', tagIds: ['easy'], createdAtUtc: '2026-08-01T00:00:00Z', updatedAtUtc: '2026-08-01T00:00:00Z' },
]
const dates = Array.from({ length: 14 }, (_, index) => ({
  date: `2026-08-${String(index + 3).padStart(2, '0')}`,
  dayOfWeek: ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'][index % 7],
  peopleCount: index < 2 ? 4 : index < 9 ? 6 : 3,
}))
const days = dates.flatMap((date, index) => (date.dayOfWeek === 'Saturday' || date.dayOfWeek === 'Sunday' ? ['lunch', 'dinner'] : ['dinner']).map(mealType => ({
  ...date, mealType: mealType as 'lunch' | 'dinner',
  mealId: mealType === 'lunch' ? 'pancakes' : index % 2 ? 'soup' : 'pasta',
  mealName: mealType === 'lunch' ? 'Pannkakor' : index % 2 ? 'Soppa' : 'Pasta pesto', tagSummary: [], isManuallyReplaced: false,
})))
const baseSchedule = { id: 'schedule', startDate: '2026-08-03', endDate: '2026-08-16', createdAtUtc: '2026-08-01T00:00:00Z', updatedAtUtc: '2026-08-01T00:00:00Z', days, title: 'Familjens veckor', generationVersion: 2 }

let tags = [...initialTags]
let meals = [...initialMeals]
let schedules = [baseSchedule]
function response(body: unknown, status = 200) { return { ok: status < 400, status, json: async () => body } }

beforeEach(() => {
  tags = [...initialTags]; meals = [...initialMeals]; schedules = [baseSchedule]
  vi.stubGlobal('fetch', vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input); const method = init?.method ?? 'GET'
    if (url.endsWith('/tags') && method === 'GET') return response(tags)
    if (url.endsWith('/meals') && method === 'GET') return response(meals)
    if (url.endsWith('/schedules') && method === 'GET') return response(schedules)
    if (url.endsWith('/meals/seed-examples') && method === 'POST') {
      const examples = [
        { id: 'tacos', name: 'Tacos', tagIds: ['easy'], createdAtUtc: '2026-08-01T00:00:00Z', updatedAtUtc: '2026-08-01T00:00:00Z' },
        { id: 'pizza', name: 'Hemmagjord pizza', tagIds: [], createdAtUtc: '2026-08-01T00:00:00Z', updatedAtUtc: '2026-08-01T00:00:00Z' },
      ]
      meals = [...meals, ...examples]; return response({ createdCount: examples.length, ignoredCount: 0 })
    }
    if (url.endsWith('/meals') && method === 'POST') {
      const body = JSON.parse(String(init?.body)) as { name: string; tagIds: string[] }
      const created = { id: 'new', ...body, createdAtUtc: '2026-08-01T00:00:00Z', updatedAtUtc: '2026-08-01T00:00:00Z' }
      meals = [...meals, created]; return response(created, 201)
    }
    if (url.includes('/schedules/') && method === 'PUT' && url.endsWith('/meal')) {
      const body = JSON.parse(String(init?.body)) as { mealId: string }
      const chosen = meals.find(meal => meal.id === body.mealId)!
      const [date, mealType] = url.split('/days/')[1].split('/')
      const updated = { ...schedules[0], days: schedules[0].days.map(day => day.date === date && day.mealType === mealType ? { ...day, mealId: chosen.id, mealName: chosen.name, isManuallyReplaced: true } : day) }
      schedules = [updated]; return response(updated)
    }
    if (url.includes('/meals/') && method === 'PUT') {
      const body = JSON.parse(String(init?.body)) as { name: string; tagIds: string[] }
      meals = meals.map(meal => url.endsWith(meal.id) ? { ...meal, ...body } : meal); return response(meals.find(meal => url.endsWith(meal.id)))
    }
    if (url.endsWith('/tags') && method === 'POST') {
      const body = JSON.parse(String(init?.body)) as { name: string; category: 'portion' | 'occasion' | 'mealType' | 'custom' }
      const created = { id: 'new-tag', ...body, createdAtUtc: '2026-08-01T00:00:00Z', isProtected: false }
      tags = [...tags, created]; return response(created, 201)
    }
    if (url.includes('/tags/') && method === 'DELETE') { tags = tags.filter(tag => !url.endsWith(tag.id)); return response(null, 204) }
    if (url.includes('/meals/') && method === 'DELETE') { meals = meals.filter(meal => !url.endsWith(meal.id)); return response(null, 204) }
    if (url.endsWith('/schedules/generate') && method === 'POST') {
      const generated = { ...baseSchedule, id: 'generated', title: 'Ny matsedel' }
      schedules = [generated, ...schedules]; return response(generated, 201)
    }
    if (url.includes('/replace') && method === 'PUT') {
      const [date, mealType] = url.split('/days/')[1].split('/')
      const updated = { ...schedules[0], days: schedules[0].days.map(day => day.date === date && day.mealType === mealType ? { ...day, mealId: 'soup', mealName: 'Soppa', isManuallyReplaced: true } : day) }
      schedules = [updated]; return response(updated)
    }
    if (url.includes('/schedules/') && method === 'DELETE') { schedules = schedules.filter(schedule => !url.endsWith(schedule.id)); return response(null, 204) }
    return response({ detail: 'Unexpected request' }, 500)
  }))
  vi.stubGlobal('print', vi.fn())
  vi.stubGlobal('confirm', vi.fn(() => true))
})

afterEach(() => { cleanup(); vi.unstubAllGlobals() })

async function openTab(name: string) {
  fireEvent.click(await screen.findByRole('tab', { name }))
}

describe('Matlista UX', () => {
  test('collapsed summary shows todays meal, people count and next meal', async () => {
    render(<MealPlanner expanded={false} today="2026-08-03" />)
    expect(await screen.findByText(/I dag, måndag.*4 personer/)).toBeInTheDocument()
    const summary = document.querySelector('.dashboard-module__summary')!
    expect(within(summary as HTMLElement).getByText('Middag: Pasta pesto')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Expandera Matlista' })).toHaveAttribute('aria-expanded', 'false')
  })

  test('collapsed summary has an explicit empty state', async () => {
    schedules = []
    render(<MealPlanner expanded={false} />)
    await screen.findByRole('button', { name: 'Expandera Matlista' })
    expect(document.querySelector('.dashboard-module__summary')).toHaveTextContent('Ingen matsedel skapad')
  })

  test('collapsed weekend summary shows lunch and dinner with the same people count', async () => {
    render(<MealPlanner expanded={false} today="2026-08-08" />)
    const summary = document.querySelector('.dashboard-module__summary')!
    await waitFor(() => expect(summary).toHaveTextContent('6 personer'))
    expect(summary).toHaveTextContent('Lunch: Pannkakor')
    expect(summary).toHaveTextContent('Middag: Soppa')
  })

  test('schedule is the default accessible tab and only the selected workspace is visible', async () => {
    render(<MealPlanner today="2026-08-03" />)
    const scheduleTab = await screen.findByRole('tab', { name: 'Matsedel' })
    expect(scheduleTab).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByRole('tabpanel', { name: 'Matsedel' })).toBeVisible()
    fireEvent.keyDown(scheduleTab, { key: 'ArrowRight' })
    expect(screen.getByRole('tab', { name: 'Maträtter' })).toHaveAttribute('aria-selected', 'true')
    expect(screen.getByRole('tabpanel', { name: 'Maträtter' })).toBeVisible()
    expect(screen.queryByRole('heading', { name: /Vecka 1/ })).not.toBeInTheDocument()
    await openTab('Generera'); expect(screen.getByRole('heading', { name: 'Generera matsedel' })).toBeVisible()
    await openTab('Sparade'); expect(screen.getByRole('heading', { name: 'Sparade matsedlar' })).toBeVisible()
  })

  test('week view shows seven compact rows, today and bounded navigation without permanent select', async () => {
    render(<MealPlanner today="2026-08-03" />)
    expect(await screen.findByRole('heading', { name: /Vecka 1/ })).toBeInTheDocument()
    expect(document.querySelectorAll('.meal-planner__day')).toHaveLength(7)
    expect(document.querySelector('.meal-planner__day--today')).toHaveTextContent('Idag')
    expect(screen.getByRole('button', { name: 'Föregående vecka' })).toBeDisabled()
    expect(screen.queryByLabelText(/Välj maträtt för/)).not.toBeInTheDocument()
    const saturday = document.querySelectorAll('.meal-planner__day')[5]
    expect(saturday).toHaveTextContent('Lunch')
    expect(saturday).toHaveTextContent('Middag')
    expect(within(saturday as HTMLElement).getByRole('button', { name: 'Byt lunch Lördag' })).toBeInTheDocument()
    expect(within(saturday as HTMLElement).getByRole('button', { name: 'Byt middag Lördag' })).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Nästa vecka' }))
    expect(screen.getByRole('heading', { name: /Vecka 2/ })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Nästa vecka' })).toBeDisabled()
  })

  test('inline replacement supports automatic, manual and cancel while only changing one day', async () => {
    render(<MealPlanner today="2026-08-03" />)
    await screen.findByRole('heading', { name: /Vecka 1/ })
    fireEvent.click(screen.getAllByRole('button', { name: /Byt middag/ })[0])
    expect(screen.getByRole('button', { name: 'Föreslå en annan' })).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Avbryt' }))
    expect(screen.queryByRole('button', { name: 'Föreslå en annan' })).not.toBeInTheDocument()

    fireEvent.click(screen.getAllByRole('button', { name: /Byt middag/ })[0])
    fireEvent.click(screen.getByRole('button', { name: 'Föreslå en annan' }))
    expect(await screen.findByRole('status')).toHaveTextContent('Måndag middag uppdaterades')
    expect(document.querySelectorAll('.meal-planner__day')[0]).toHaveTextContent('Soppa')
    expect(document.querySelectorAll('.meal-planner__day')[1]).toHaveTextContent('Soppa')

    fireEvent.click(screen.getAllByRole('button', { name: /Byt middag/ })[1])
    fireEvent.click(screen.getByRole('button', { name: 'Välj maträtt manuellt' }))
    fireEvent.change(screen.getByLabelText('Välj maträtt'), { target: { value: 'pancakes' } })
    fireEvent.click(screen.getByRole('button', { name: 'Använd vald' }))
    await waitFor(() => expect(document.querySelectorAll('.meal-planner__day')[1]).toHaveTextContent('Pannkakor'))
    expect(document.querySelectorAll('.meal-planner__day')[0]).toHaveTextContent('Soppa')
  })

  test('meal library combines text search and tags, reports matches and keeps actions compact', async () => {
    render(<MealPlanner />); await openTab('Maträtter')
    const library = screen.getByRole('tabpanel', { name: 'Maträtter' })
    expect(screen.getByText('3 av 3 maträtter')).toBeInTheDocument()
    fireEvent.change(screen.getByLabelText('Sök maträtt'), { target: { value: 'pasta' } })
    expect(screen.getByText('1 av 3 maträtter')).toBeInTheDocument()
    expect(within(library).queryByText('Soppa')).not.toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Filter' }))
    const filter = screen.getByRole('dialog', { name: 'Filtrera maträtter' })
    fireEvent.click(within(filter).getByLabelText('Vegetariskt'))
    expect(screen.getByText('1 av 3 maträtter')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Filter (1)' })).toHaveAttribute('aria-expanded', 'true')
    fireEvent.change(screen.getByLabelText('Sök maträtt'), { target: { value: 'pannkakor' } })
    expect(screen.getByText('0 av 3 maträtter')).toBeInTheDocument()
    expect(screen.getByText('Inga maträtter matchar sökning och filter.')).toBeInTheDocument()
    fireEvent.click(within(filter).getByRole('button', { name: 'Rensa filter' }))
    expect(screen.getByRole('button', { name: 'Filter' })).toBeInTheDocument()
  })

  test('meal editing and confirmed deletion are opened from the row action menu', async () => {
    render(<MealPlanner />); await openTab('Maträtter')
    const library = screen.getByRole('tabpanel', { name: 'Maträtter' })
    const soupRow = within(library).getByText('Soppa').closest('li')!
    fireEvent.click(within(soupRow).getByLabelText('Åtgärder för Soppa'))
    fireEvent.click(within(soupRow).getByRole('button', { name: 'Redigera' }))
    const editForm = document.querySelector('.meal-planner__edit-form')!
    expect(within(editForm as HTMLElement).getByLabelText('Namn')).toHaveValue('Soppa')
    fireEvent.change(within(editForm as HTMLElement).getByLabelText('Namn'), { target: { value: 'Tomatsoppa' } })
    fireEvent.click(within(editForm as HTMLElement).getByRole('button', { name: 'Spara ändring' }))
    expect(await screen.findByText('Tomatsoppa')).toBeInTheDocument()
    const tomatoRow = screen.getByText('Tomatsoppa').closest('li')!
    fireEvent.click(within(tomatoRow).getByLabelText('Åtgärder för Tomatsoppa'))
    fireEvent.click(within(tomatoRow).getByRole('button', { name: 'Ta bort' }))
    expect(window.confirm).toHaveBeenCalled()
    await waitFor(() => expect(screen.queryByText('Tomatsoppa')).not.toBeInTheDocument())
  })

  test('empty library can seed examples only after confirmation', async () => {
    meals = []
    render(<MealPlanner />); await openTab('Maträtter')
    fireEvent.click(screen.getByRole('button', { name: 'Lägg in exempelrätter' }))
    expect(window.confirm).toHaveBeenCalledWith(expect.stringContaining('24 exempelrätter'))
    expect(await screen.findByText('2 exempelrätter lades till.')).toBeInTheDocument()
    expect(screen.getByText('Tacos')).toBeInTheDocument()
  })

  test('tag management starts closed and supports custom tag deletion with confirmation', async () => {
    render(<MealPlanner />); await openTab('Maträtter')
    const manager = screen.getByText('Hantera taggar').closest('details')!
    expect(manager).not.toHaveAttribute('open')
    fireEvent.click(screen.getByText('Hantera taggar'))
    expect(within(manager).getByText(/Standardtaggar/)).toBeInTheDocument()
    const customRow = within(manager).getByText('Vegetariskt').closest('li')!
    fireEvent.click(within(customRow).getByRole('button', { name: 'Ta bort' }))
    expect(window.confirm).toHaveBeenCalled()
  })

  test('successful generation activates the new schedule and returns to schedule tab', async () => {
    render(<MealPlanner />); await openTab('Generera')
    fireEvent.change(screen.getByLabelText('Antal veckor'), { target: { value: '2' } })
    fireEvent.click(screen.getByRole('button', { name: 'Generera matsedel' }))
    expect(await screen.findByText('Matsedeln skapades och sparades.')).toBeInTheDocument()
    expect(screen.getByRole('tab', { name: 'Matsedel' })).toHaveAttribute('aria-selected', 'true')
    expect(within(screen.getByRole('tabpanel', { name: 'Matsedel' })).getByText('Ny matsedel')).toBeInTheDocument()
    expect(vi.mocked(fetch).mock.calls.some(([, init]) => String(init?.body).includes('"weekCount":2'))).toBe(true)
  })

  test('saved schedules are compact, open in schedule view, print and require delete confirmation', async () => {
    render(<MealPlanner />); await openTab('Sparade')
    expect(screen.getByText(/3 aug.*16 aug.*2 veckor/)).toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Öppna' }))
    expect(screen.getByRole('tab', { name: 'Matsedel' })).toHaveAttribute('aria-selected', 'true')
    await openTab('Sparade')
    fireEvent.click(screen.getByRole('checkbox', { name: 'Skriv ut' }))
    fireEvent.click(screen.getByRole('button', { name: 'Skriv ut valda' }))
    expect(window.print).toHaveBeenCalledOnce()
    expect(document.querySelectorAll('.meal-planner-print__week')).toHaveLength(2)
    expect(document.querySelectorAll('.meal-planner-print__week')[0].querySelectorAll('tbody tr')).toHaveLength(9)
    expect(document.querySelector('.meal-planner-print')).toHaveTextContent('Lunch')
    fireEvent.click(screen.getByLabelText('Åtgärder för Familjens veckor'))
    fireEvent.click(screen.getByRole('button', { name: 'Ta bort' }))
    expect(window.confirm).toHaveBeenCalled()
  })
})
