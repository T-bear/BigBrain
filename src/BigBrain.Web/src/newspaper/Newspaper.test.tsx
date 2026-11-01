import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { afterEach, expect, test, vi } from 'vitest'
import { budgetPages, demoArticles, Newspaper } from './Newspaper'

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

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

test('only the first preview sheet uses the newspaper front-page hierarchy', () => {
  const { container } = render(<Newspaper />)
  const sheets = container.querySelectorAll('.newspaper-preview .newspaper-sheet')
  expect(sheets).toHaveLength(2)
  expect(sheets[0].classList.contains('newspaper-sheet--front')).toBe(true)
  expect(sheets[0].querySelector('.newspaper-edition-line')).not.toBeNull()
  expect(sheets[0].querySelector('.newspaper-story--lead')?.textContent).toContain('Ett rum av levande pil')
  expect(sheets[0].textContent).toContain('HUVUDARTIKEL · DEMO')
  expect(sheets[1].classList.contains('newspaper-sheet--front')).toBe(false)
  expect(sheets[1].querySelector('.newspaper-story--lead')).toBeNull()
})

test('front-page illustrations and contents use only selected pages', async () => {
  const { container } = render(<Newspaper />)
  const front = container.querySelector('.newspaper-preview .newspaper-sheet--front')

  expect(front?.querySelectorAll('.newspaper-art img')).toHaveLength(2)
  expect(front?.querySelector('.newspaper-front-index')).not.toBeNull()
  expect(front?.textContent).toContain('I detta nummer')
  expect(front?.textContent).toContain('Intresse för astronomi')

  fireEvent.change(within(container).getByLabelText('Maximalt antal sidor'), {
    target: { value: '1' },
  })

  await waitFor(() => {
    const sheets = container.querySelectorAll('.newspaper-preview .newspaper-sheet')
    expect(sheets).toHaveLength(1)
    expect(sheets[0].textContent).not.toContain('Intresse för astronomi')
  })

  fireEvent.change(within(container).getByLabelText('Maximalt antal sidor'), {
    target: { value: '3' },
  })

  await waitFor(() => {
    const preview = container.querySelector('.newspaper-preview')
    expect(preview?.querySelectorAll('.newspaper-sheet')).toHaveLength(3)
    expect(preview?.querySelector('.newspaper-front-index')?.textContent).toContain('Böcker och berättelser')
    expect(preview?.querySelectorAll('.newspaper-art img')).toHaveLength(2)
  })
})

test('expanded demo articles stay within the page budget', () => {
  const articles = budgetPages(demoArticles, 3).flat()

  expect(articles).toHaveLength(6)
  expect(articles.every(article => article.source === 'Demomaterial')).toBe(true)
  expect(articles.every(article => article.body.trim().split(/\s+/).length <= 85)).toBe(true)
  expect(articles[0].body.trim().split(/\s+/).length).toBeGreaterThan(50)
  expect(articles[2].body.trim().split(/\s+/).length).toBeGreaterThan(50)
})

test('editorial ribbon belongs only to the first sheet', async () => {
  const { container } = render(<Newspaper />)
  const sheets = container.querySelectorAll('.newspaper-preview .newspaper-sheet')

  expect(sheets).toHaveLength(2)
  expect(sheets[0].querySelectorAll('.newspaper-front-ribbon > div')).toHaveLength(3)
  expect(sheets[0].textContent).toContain('Gröna rum')
  expect(sheets[0].textContent).toContain('Spelminnen')
  expect(sheets[1].querySelector('.newspaper-front-ribbon')).toBeNull()

  fireEvent.change(within(container).getByLabelText('Maximalt antal sidor'), { target: { value: '3' } })

  await waitFor(() => {
    const all = container.querySelectorAll('.newspaper-preview .newspaper-sheet')
    expect(all).toHaveLength(3)
    expect(container.querySelectorAll('.newspaper-preview .newspaper-front-ribbon')).toHaveLength(1)
  })
})

test('print layout uses a fixed A4 frame with an anchored front-page footer', () => {
  const { container } = render(<Newspaper />)
  const css = container.querySelector('style')?.textContent ?? ''

  const frame = css.match(/\.newspaper-print-pages > \.newspaper-sheet--front\s*\{([^}]*)\}/)?.[1]

  const footer = css.match(/\.newspaper-print-pages \.newspaper-sheet--front \.newspaper-footer\s*\{([^}]*)\}/)?.[1]

  expect(frame).toContain('height:260mm')
  expect(footer).toContain('position:absolute')
  expect(footer).toContain('bottom:2mm')
  expect(footer).toContain('margin-top:0')

  expect(document.querySelectorAll('#bigbrain-newspaper-print .newspaper-sheet')).toHaveLength(2)
})

test('botanical illustrations appear in preview and print', () => {
  const { container } = render(<Newspaper />)

  const front = container.querySelector('.newspaper-preview .newspaper-sheet--front')
  const printed = document.querySelector('#bigbrain-newspaper-print .newspaper-sheet--front')

  expect(front?.querySelector('.newspaper-mast-mark svg')).not.toBeNull()
  expect(printed?.querySelector('.newspaper-mast-mark svg')).not.toBeNull()

  const willow = front?.querySelector('.newspaper-art--willow img')
  expect(willow).not.toBeNull()
  expect(willow?.getAttribute('src')).toBe('/newspaper/willow-dome.jpg')

  expect(front?.querySelectorAll('.newspaper-art img')).toHaveLength(2)
  expect(printed?.querySelectorAll('.newspaper-art img')).toHaveLength(2)
})

test('rasterbilder finns i förhandsvisning och utskrift', () => {
  const { container } = render(<Newspaper />)

  const preview = container.querySelector('.newspaper-preview .newspaper-sheet--front')
  const printed = document.querySelector('#bigbrain-newspaper-print .newspaper-sheet--front')

  for (const sheet of [preview, printed]) {
    const images = sheet?.querySelectorAll('.newspaper-art img')

    expect(images).toHaveLength(2)
    expect(images?.[0].getAttribute('src')).toBe('/newspaper/willow-dome.jpg')
    expect(images?.[1].getAttribute('src')).toBe('/newspaper/retro-games.jpg')

    expect(sheet?.textContent).toContain('CC BY-SA 2.0')
    expect(sheet?.textContent).toContain('CC BY 2.0')
  }
})

test('editorial first page contains an actual rail and lower feature', async () => {
  const { container } = render(<Newspaper />)

  const front = container.querySelector('.newspaper-preview .newspaper-sheet--front')
  const printFront = document.querySelector('#bigbrain-newspaper-print .newspaper-sheet--front')

  for (const sheet of [front, printFront]) {
    expect(sheet?.querySelector('.newspaper-front-rail')).not.toBeNull()
    expect(sheet?.querySelector('.newspaper-front-feature')).not.toBeNull()
    expect(sheet?.textContent).toContain('Intresse för astronomi')
    expect(sheet?.textContent).toContain('Teknik i vardagen')
    expect(sheet?.querySelectorAll('.newspaper-art img')).toHaveLength(2)
  }

  fireEvent.change(within(container).getByLabelText('Maximalt antal sidor'), { target: { value: '1' } })

  await waitFor(() => {
    const one = container.querySelector('.newspaper-preview .newspaper-sheet--front')
    expect(one?.textContent).not.toContain('Fortsätter på sida 2')
    expect(one?.textContent).not.toContain('Intresse för astronomi')
    expect(container.querySelectorAll('.newspaper-preview .newspaper-sheet')).toHaveLength(1)
  })
})

test('editorial print grid preserves the A4 frame and footer', () => {
  const { container } = render(<Newspaper />)
  const css = container.querySelector('style')?.textContent ?? ''

  expect(css).toContain('"lead lead rail"')
  expect(css).toContain('"retro feature rail"')
  expect(css).toContain('height:260mm')
  expect(css).toContain('bottom:2mm')
  expect(css).toContain('print-color-adjust:exact')
})
