import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { afterEach, expect, test, vi } from 'vitest'
import { Newspaper } from './Newspaper'

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

test('shows only the original master with one image in the print portal', () => {
  const { container, unmount } = render(<Newspaper />)
  const preview = container.querySelector('img')!
  const printed = document.querySelector('#bigbrain-newspaper-print img')!
  expect(preview.getAttribute('src')).toBe('/newspaper/Morgonbladet_designmaster_A4.png')
  expect(printed.getAttribute('src')).toBe(preview.getAttribute('src'))
  expect(document.querySelectorAll('#bigbrain-newspaper-print img')).toHaveLength(1)
  expect(screen.queryByRole('combobox')).toBeNull()
  unmount()
  expect(document.querySelector('#bigbrain-newspaper-print')).toBeNull()
})

test('waits for the print image before allowing browser printing', () => {
  const print = vi.fn()
  vi.stubGlobal('print', print)
  render(<Newspaper />)
  const button = screen.getByRole('button', { name: 'Skriv ut A4-sidan' })
  expect(button).toBeDisabled()
  fireEvent.click(button)
  expect(print).not.toHaveBeenCalled()
  fireEvent.load(document.querySelector('#bigbrain-newspaper-print img')!)
  expect(button).toBeEnabled()
  fireEvent.click(button)
  expect(print).toHaveBeenCalledOnce()
})

test('failed image loading prevents an empty print from the button', () => {
  const print = vi.fn()
  vi.stubGlobal('print', print)
  render(<Newspaper />)
  fireEvent.error(document.querySelector('#bigbrain-newspaper-print img')!)
  expect(screen.getByRole('alert')).toHaveTextContent('Designmastern kunde inte laddas')
  const button = screen.getByRole('button', { name: 'Skriv ut A4-sidan' })
  expect(button).toBeDisabled()
  fireEvent.click(button)
  expect(print).not.toHaveBeenCalled()
})
