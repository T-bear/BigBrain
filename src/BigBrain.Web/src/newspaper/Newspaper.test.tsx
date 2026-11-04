import { cleanup, render, screen } from '@testing-library/react'
import { afterEach, expect, test } from 'vitest'
import { Newspaper } from './Newspaper'

afterEach(() => {
  cleanup()
})

test('shows the unchanged original designmaster', () => {
  const { container } = render(<Newspaper />)
  const image = container.querySelector('img')!
  expect(image.getAttribute('src')).toBe('/newspaper/Morgonbladet_designmaster_A4.png')
  expect(image.getAttribute('width')).toBe('2480')
  expect(image.getAttribute('height')).toBe('3508')
})

test('opens a static PDF instead of printing HTML', () => {
  const { container } = render(<Newspaper />)
  const link = screen.getByRole('link', { name: 'Öppna A4-PDF för utskrift' })
  expect(link).toHaveAttribute('href', '/newspaper/Morgonbladet_designmaster_A4.pdf')
  expect(link).toHaveAttribute('target', '_blank')
  expect(link).toHaveAttribute('rel', 'noopener noreferrer')
  expect(container.querySelector('#bigbrain-newspaper-print')).toBeNull()
  expect(screen.queryByRole('button', { name: /Skriv ut A4-sidan/ })).toBeNull()
})
