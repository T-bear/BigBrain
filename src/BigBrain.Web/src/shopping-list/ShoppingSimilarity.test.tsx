import '@testing-library/jest-dom/vitest'
import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, expect, test, vi } from 'vitest'
import { ShoppingList } from './ShoppingList'

const items = [
  { id:'1', name:'Lördagsgodis', normalizedName:'LÖRDAGSGODIS', quantity:1, purchased:false, createdAtUtc:'2026-08-07T00:00:00Z', updatedAtUtc:'2026-08-07T00:00:00Z', sortOrdinal:1 },
]

afterEach(() => cleanup())

test('similar product suggests the existing item and requires explicit add anyway', async () => {
  const fetchMock = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input)
    if (url.endsWith('/items') && init?.method === 'GET') return new Response(JSON.stringify({ items, sessionId:null }))
    if (url.endsWith('/frequent') || url.includes('/suggestions')) return new Response(JSON.stringify([]))
    return new Response(JSON.stringify(items[0]))
  })
  vi.stubGlobal('fetch', fetchMock)
  render(<ShoppingList expanded onToggle={() => undefined} status="Available" />)
  await screen.findAllByText('1 kvar')
  fireEvent.click(screen.getByRole('button', { name:'Öppna handlingsläge' }))
  const input = screen.getByRole('combobox')
  fireEvent.change(input, { target:{ value:'lordags godis' } })
  fireEvent.submit(input.closest('form')!)
  expect(await screen.findByRole('alertdialog', { name:'Liknande vara finns redan: Lördagsgodis' })).toBeInTheDocument()
  expect(fetchMock.mock.calls.filter(([url, options]) => String(url).endsWith('/items') && (options as RequestInit)?.method === 'POST')).toHaveLength(0)
  fireEvent.click(screen.getByRole('button', { name:'Lägg till ändå' }))
  await waitFor(() => expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining('/items'), expect.objectContaining({ method:'POST', body:expect.stringContaining('"addAnyway":true') })))
})
