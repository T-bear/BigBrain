import { fireEvent, render, screen } from '@testing-library/react'
import { afterEach, expect, test, vi } from 'vitest'
import { budgetPages, demoArticles, Newspaper } from './Newspaper'

afterEach(() => vi.unstubAllGlobals())

test('content budget limits output to one through three pages', () => {
  expect(budgetPages(demoArticles, 1)).toHaveLength(1)
  expect(budgetPages(demoArticles, 2)).toHaveLength(2)
  expect(budgetPages(demoArticles, 3)).toHaveLength(3)
  expect(budgetPages(demoArticles, 100)).toHaveLength(3)
  expect(budgetPages(demoArticles, NaN)).toHaveLength(1)
})

test('long articles are limited before rendering', () => {
  const long = {
    ...demoArticles[0],
    title: 'X'.repeat(200),
    body: 'ord '.repeat(1000),
  }
  const pages = budgetPages(Array(20).fill(long), 1)
  expect(pages[0]).toHaveLength(2)
  expect(pages[0][0].title.length).toBeLessThanOrEqual(72)
  expect(pages[0][0].body.trim().split(/\s+/).length).toBeLessThanOrEqual(86)
})

test('print button uses the existing browser-print mechanism', () => {
  const print = vi.fn()
  vi.stubGlobal('print', print)
  render(<Newspaper />)
  fireEvent.click(screen.getByRole('button', { name: 'Skriv ut provtidning' }))
  expect(print).toHaveBeenCalledOnce()
})
