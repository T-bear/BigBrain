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
    vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response(JSON.stringify({ theme: 'obsidian-gold', configured: true }), { status: 200, headers: { 'Content-Type': 'application/json' } })))
  })

  const renderControl = () => render(<ThemeProvider><ThemeControl /></ThemeProvider>)

  it('uses and applies the default theme without stored state', () => {
    expect(resolveInitialTheme()).toBe(DEFAULT_THEME)
    applyTheme(resolveInitialTheme())
    expect(document.documentElement.dataset.theme).toBe('obsidian-gold')
  })

  it('switches without reload and persists the Swedish-labelled selection', () => {
    renderControl()
    fireEvent.click(screen.getByRole('radio', { name: /Arctic Wind/ }))
    expect(document.documentElement.dataset.theme).toBe('arctic-wind')
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('arctic-wind')
    expect(screen.getByRole('radio', { name: /Arctic Wind/ })).toBeChecked()
  })

  it('falls back when storage contains an invalid theme', () => {
    vi.stubGlobal('matchMedia', vi.fn().mockReturnValue({ matches: true }))
    localStorage.setItem(THEME_STORAGE_KEY, 'unsafe-theme')
    expect(resolveInitialTheme()).toBe(DEFAULT_THEME)
  })

  it('uses the stored theme on a new render', () => {
    localStorage.setItem(THEME_STORAGE_KEY, 'arctic-wind')
    renderControl()
    expect(screen.getByRole('radio', { name: /Arctic Wind/ })).toBeChecked()
    expect(document.documentElement.dataset.theme).toBe('arctic-wind')
  })

  it('registers, selects and restores Obsidian Gold', () => {
    expect(themes).toContain('obsidian-gold')
    const { unmount } = renderControl()
    fireEvent.click(screen.getByRole('radio', { name: /Forest Night/ }))
    expect(document.documentElement.dataset.theme).toBe('forest-night')
    expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe('forest-night')

    unmount()
    renderControl()
    expect(screen.getByRole('radio', { name: /Forest Night/ })).toBeChecked()
    expect(document.documentElement.dataset.theme).toBe('forest-night')
  })

  it('keeps every registered theme unique', () => {
    expect(new Set(themes).size).toBe(themes.length)
  })

  it('seeds an unconfigured shared setting from the existing browser theme', async () => {
    localStorage.setItem(THEME_STORAGE_KEY, 'arctic-wind')
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(JSON.stringify({ theme: 'obsidian-gold', configured: false }), { status: 200, headers: { 'Content-Type': 'application/json' } }))
      .mockResolvedValueOnce(new Response(JSON.stringify({ theme: 'arctic-wind', configured: true }), { status: 200, headers: { 'Content-Type': 'application/json' } }))
    vi.stubGlobal('fetch', fetchMock)
    renderControl()
    await waitFor(() => expect(fetchMock).toHaveBeenCalledWith('/api/v1/settings/theme', expect.objectContaining({ method: 'PUT' })))
    expect(document.documentElement.dataset.theme).toBe('arctic-wind')
  })
})
