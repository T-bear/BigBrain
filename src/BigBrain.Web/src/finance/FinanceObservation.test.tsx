import { render, screen, within } from '@testing-library/react'
import { describe, expect, test } from 'vitest'
import { FinanceObservation } from './FinanceObservation'
import type { FinanceObservationSnapshot } from '../types'
import { dashboardRegistry } from '../dashboard/appWidgets'

const empty: FinanceObservationSnapshot = {
  generatedAtUtc: '2026-08-11T10:00:00Z',
  safety: { mode: 'research', liveTradingEnabled: false, paperTradingEnabled: false, brokerConnected: false, ingestionAllowed: false, realProviderStorageAllowed: false },
  provider: { state: 'noneAuthorized', displayName: 'Ingen', entitlement: 'pendingWrittenConfirmation', entitlementGate: 'BB-071 / STATE B', reason: 'Skriftlig entitlement-bekräftelse saknas.' },
  latestMarketDataUpdateUtc: null, dataKind: 'none',
  watchlist: [{ instrumentId: 'US:XNAS:MSFT', symbol: 'MSFT', displayName: 'Microsoft', price: null, currency: null, dailyChangePercent: null, observedAtUtc: null, freshness: 'unavailable', session: 'unknown', quality: 'unknown', dataKind: 'none', history: [] }],
  historicalMemory: { observationCount: 0, activeRevisionId: null, parentRevisionId: null, coverageFrom: null, coverageTo: null, lastAcquiredAtUtc: null, gapCount: 0, correctionCount: 0, persistence: 'notConfigured', provider: 'none', product: 'none', policy: 'BB-071-pending', provenance: 'none' },
}

describe('Finance read-only observation UI', () => {
  test('registers Finance as a navigable dashboard view', () => {
    expect(dashboardRegistry.get('finance')).toMatchObject({ title: 'Finance' })
  })
  test('renders unmistakable fail-closed research and empty states without trading controls', () => {
    render(<FinanceObservation initialSnapshot={empty} />)
    expect(screen.getAllByText('RESEARCH').length).toBeGreaterThan(0)
    expect(screen.getByText('Ingen handel med riktiga pengar')).toBeVisible()
    expect(screen.getByText('Ingen provider auktoriserad')).toBeVisible()
    expect(screen.getByText('Skriftlig bekräftelse väntar')).toBeVisible()
    expect(screen.getByText('Ingen observation')).toBeVisible()
    expect(screen.getByText('Ingen prishistorik')).toBeVisible()
    expect(screen.getByText('Historiskt minne')).toBeVisible()
    expect(screen.getByRole('button', { name: /MSFT/ })).toBeEnabled()
    expect(screen.queryByRole('button', { name: /köp|sälj|order|trade/i })).not.toBeInTheDocument()
  })

  test('labels synthetic stale and gap evidence and keeps chart gap segments separate', () => {
    const fixture: FinanceObservationSnapshot = { ...empty, dataKind: 'syntheticFixture', latestMarketDataUpdateUtc: '2026-08-11T10:00:00Z', watchlist: [{ ...empty.watchlist[0], price: 101, currency: 'USD', freshness: 'stale', session: 'gap', quality: 'warning', dataKind: 'syntheticFixture', history: [
      { observedAtUtc: '2026-08-11T09:00:00Z', value: 100, beginsAfterGap: false }, { observedAtUtc: '2026-08-11T09:15:00Z', value: 101, beginsAfterGap: false }, { observedAtUtc: '2026-08-11T10:00:00Z', value: 99, beginsAfterGap: true }, { observedAtUtc: '2026-08-11T10:15:00Z', value: 101, beginsAfterGap: false },
    ] }], historicalMemory: { ...empty.historicalMemory, observationCount: 4, activeRevisionId: 'fixture-revision-1', gapCount: 1, persistence: 'fixtureMemory' } }
    const { container } = render(<FinanceObservation initialSnapshot={fixture} />)
    expect(screen.getByText(/SYNTHETISK FIXTURE/)).toBeVisible()
    expect(screen.getByText('Inaktuell · Datagap')).toBeVisible()
    expect(screen.getByText('Kvalitet: Varning')).toBeVisible()
    expect(screen.getByText('fixture-revision-1')).toBeVisible()
    expect(container.querySelectorAll('.finance-chart path')).toHaveLength(2)
    expect(within(container).getByText(/Datagap ritas inte/)).toBeVisible()
  })
})
