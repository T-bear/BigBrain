import { render, screen } from '@testing-library/react'
import { beforeEach, expect, test, vi } from 'vitest'
import App from './App'

beforeEach(() => {
  vi.stubGlobal(
    'fetch',
    vi.fn()
      .mockResolvedValueOnce({
        ok: true,
        json: async () => [
          {
            id: 'system',
            name: 'System',
            description: 'Core system health.',
            route: '/',
            dashboardWidgets: [
              {
                id: 'system-health',
                title: 'System health',
                kind: 'health',
                dataEndpoint: '/api/v1/system/health',
              },
            ],
            capabilities: ['system.health.read'],
          },
        ],
      })
      .mockResolvedValueOnce({
        ok: true,
        json: async () => ({ status: 'Healthy', checkedAtUtc: new Date().toISOString() }),
      }),
  )
})

test('renders registered module navigation and widget', async () => {
  render(<App />)

  expect(await screen.findByRole('link', { name: 'System' })).toBeInTheDocument()
  expect(await screen.findByRole('heading', { name: 'System health' })).toBeInTheDocument()
  expect(await screen.findByText('Healthy')).toBeInTheDocument()
})

