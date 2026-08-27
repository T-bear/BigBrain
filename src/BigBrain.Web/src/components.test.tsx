import { cleanup, fireEvent, render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it } from 'vitest'
import { BBButton, BBEmptyState, BBInput, BBMediaArtwork, BBSelect, BBSurface } from './components'

describe('BigBrain semantic controls', () => {
  afterEach(cleanup)

  it('maps button intent and preserves native button semantics', () => {
    render(<BBButton variant="primary">Spara</BBButton>)
    const button = screen.getByRole('button', { name: 'Spara' })
    expect(button).toHaveClass('bb-button', 'bb-button--primary')
    expect(button).toBeEnabled()
  })

  it('exposes loading state and prevents duplicate activation', () => {
    render(<BBButton busy>Skicka</BBButton>)
    const button = screen.getByRole('button', { name: 'Skicka pågår' })
    expect(button).toBeDisabled()
    expect(button).toHaveAttribute('aria-busy', 'true')
    expect(screen.getByRole('status')).toHaveTextContent('Skicka pågår')
  })

  it('keeps native labels and values for material inputs and selects', () => {
    render(<><label htmlFor="query">Sök</label><BBInput id="query" /><label htmlFor="theme">Tema</label><BBSelect id="theme" defaultValue="forest"><option value="forest">Forest Night</option></BBSelect></>)
    expect(screen.getByLabelText('Sök')).toHaveClass('bb-input')
    expect(screen.getByLabelText('Tema')).toHaveClass('bb-select')
    expect(screen.getByLabelText('Tema')).toHaveValue('forest')
  })

  it('replaces missing or broken artwork with the shared branded placeholder', () => {
    const {rerender}=render(<BBMediaArtwork alt="Omslag till Test" />)
    expect(screen.getByRole('img',{name:'Omslag till Test'})).toHaveClass('bb-media-placeholder')
    rerender(<BBMediaArtwork alt="Omslag till Test" src="/broken.jpg" />)
    fireEvent.error(screen.getByRole('img',{name:'Omslag till Test'}))
    expect(screen.getByRole('img',{name:'Omslag till Test'})).toHaveClass('bb-media-placeholder')
  })

  it('provides restrained surface and empty-state anatomy', () => {
    render(<BBSurface aria-label="Grupp"><BBEmptyState title="Inget här" detail="Försök senare." /></BBSurface>)
    expect(screen.getByLabelText('Grupp')).toHaveClass('bb-surface')
    expect(screen.getByRole('status')).toHaveTextContent('Inget härFörsök senare.')
  })
})
