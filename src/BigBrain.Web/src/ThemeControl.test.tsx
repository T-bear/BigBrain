import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ThemeControl } from './ThemeControl'
import { ThemeProvider } from './ThemeProvider'
import { applyTheme, DEFAULT_THEME, resolveInitialTheme, THEME_STORAGE_KEY, themes } from './theme'

describe('theme contract', () => {
  afterEach(cleanup)
  beforeEach(() => {
    localStorage.clear()
    delete document.documentElement.dataset.theme
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: false }))
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ theme: 'bigbrain-dark' }), { status: 200, headers: { 'Content-Type': 'application/json' } })))
  })

  const renderControl = () => render(<ThemeProvider><ThemeControl /></ThemeProvider>)

  it('uses and applies the default theme without stored state', () => {
    expect(resolveInitialTheme()).toBe(DEFAULT_THEME)
    applyTheme(resolveInitialTheme())
    expect(document.documentElement.dataset.theme).toBe('bigbrain-dark')
  })

  it('switches without reload and persists the Swedish-labelled selection', () => {
    renderControl()
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
    renderControl()
    expect(screen.getByLabelText('Tema')).toHaveValue('bigbrain-light')
    expect(document.documentElement.dataset.theme).toBe('bigbrain-light')
  })

  it('registers, selects and restores Obsidian Gold', () => {
    expect(themes).toContain('bigbrain-obsidian-gold')
    const { unmount } = renderControl()
    const control = screen.getByLabelText('Tema')

    expect(screen.getByRole('option', { name: 'Obsidian Gold' })).toBeInTheDocument()
    fireEvent.change(control, { target: { value: 'bigbrain-obsidian-gold' } })
    expect(document.documentElement.dataset.theme).toBe('bigbrain-obsidian-gold')
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('bigbrain-obsidian-gold')

    unmount()
    renderControl()
    expect(screen.getByLabelText('Tema')).toHaveValue('bigbrain-obsidian-gold')
    expect(document.documentElement.dataset.theme).toBe('bigbrain-obsidian-gold')
  })

  it('keeps every registered theme unique', () => {
    expect(new Set(themes).size).toBe(themes.length)
  })

  it('seeds an unconfigured shared setting from the existing browser theme', async () => {
    localStorage.setItem(THEME_STORAGE_KEY, 'bigbrain-light')
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(JSON.stringify({ theme: 'bigbrain-dark', configured: false }), { status: 200, headers: { 'Content-Type': 'application/json' } }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ theme: 'bigbrain-light', configured: true }), { status: 200, headers: { 'Content-Type': 'application/json' } }))
    vi.stubGlobal('fetch', fetchMock)
    renderControl()
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/v1/settings/theme', expect.objectContaining({ method: 'PUT' })))
    expect(document.documentElement.dataset.theme).toBe('bigbrain-light')
  })
})
