import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { UXLab } from './UXLab'

describe('UX/UI Lab', () => {
  afterEach(() => { cleanup(); vi.restoreAllMocks() })
  it('renders stable identities, review vocabulary and shared production primitives', () => {
    render(<UXLab />)
    expect(screen.getByRole('heading', { name: 'UX/UI-labb' })).toBeInTheDocument()
    expect(screen.getAllByRole('heading', { name: 'Actions / Button' })).toHaveLength(3)
    expect(screen.getAllByText('EXPERIMENTELL').length).toBeGreaterThan(0)
    expect(screen.getByText('RATAD')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Primary' })).toHaveClass('bb-button--primary')
  })
  it('keeps the accessible timer candidate local', () => {
    const fetch = vi.spyOn(globalThis, 'fetch')
    render(<UXLab />)
    const trigger = screen.getByRole('button', { name: 'Öppna sovtimerkandidat' })
    expect(trigger).toHaveAttribute('aria-expanded', 'false')
    fireEvent.click(trigger)
    fireEvent.click(screen.getByRole('button', { name: '30 min' }))
    expect(screen.getByText('Aktiv: 30 min')).toBeInTheDocument()
    expect(fetch).not.toHaveBeenCalled()
  })
})
