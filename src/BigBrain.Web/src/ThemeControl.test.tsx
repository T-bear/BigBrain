import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ThemeControl } from './ThemeControl'
import { applyTheme, DEFAULT_THEME, resolveInitialTheme, THEME_STORAGE_KEY } from './theme'

describe('theme contract', () => {
  afterEach(cleanup)
  beforeEach(() => {
    localStorage.clear()
    delete document.documentElement.dataset.theme
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: false }))
  })

  it('uses and applies the default theme without stored state', () => {
    expect(resolveInitialTheme()).toBe(DEFAULT_THEME)
    applyTheme(resolveInitialTheme())
    expect(document.documentElement.dataset.theme).toBe('bigbrain-dark')
  })

  it('switches without reload and persists the Swedish-labelled selection', () => {
    render(<ThemeControl />)
    const control = screen.getByLabelText('Tema')
    fireEvent.change(control, { target: { value: 'bigbrain-light' } })
    expect(document.documentElement.dataset.theme).toBe('bigbrain-light')
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('bigbrain-light')
    expect(screen.getByRole('option', { name: 'Ljust' })).toBeInTheDocument()
  })

  it('falls back when storage contains an invalid theme', () => {
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: true }))
    localStorage.setItem(THEME_STORAGE_KEY, 'unsafe-theme')
    expect(resolveInitialTheme()).toBe(DEFAULT_THEME)
  })

  it('uses the stored theme on a new render', () => {
    localStorage.setItem(THEME_STORAGE_KEY, 'bigbrain-light')
    render(<ThemeControl />)
    expect(screen.getByLabelText('Tema')).toHaveValue('bigbrain-light')
    expect(document.documentElement.dataset.theme).toBe('bigbrain-light')
  })
})
