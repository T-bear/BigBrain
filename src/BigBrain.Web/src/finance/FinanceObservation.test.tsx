import { render, screen, within } from '@testing-library/react'
import { describe, expect, test } from 'vitest'
import { FinanceObservation } from './FinanceObservation'
import type { FinanceFeatureSnapshot, FinanceObservationSnapshot } from '../types'
import { dashboardRegistry } from '../dashboard/appWidgets'

const empty: FinanceObservationSnapshot = {
  generatedAtUtc: '2026-08-11T10:00:00Z',
  safety: { mode: 'research', liveTradingEnabled: false, paperTradingEnabled: false, brokerConnected: false, ingestionAllowed: false, realProviderStorageAllowed: false },
  provider: { state: 'noneAuthorized', displayName: 'Ingen', entitlement: 'pendingWrittenConfirmation', entitlementGate: 'BB-071 / STATE B', reason: 'Skriftlig entitlement-bekräftelse saknas.' },
  latestMarketDataUpdateUtc: null, dataKind: 'none',
  watchlist: [{ instrumentId: 'US:XNAS:MSFT', symbol: 'MSFT', displayName: 'Microsoft', price: null, currency: null, dailyChangePercent: null, observedAtUtc: null, freshness: 'unavailable', session: 'unknown', quality: 'unknown', dataKind: 'none', history: [] }],
  historicalMemory: { observationCount: 0, activeRevisionId: null, parentRevisionId: null, coverageFrom: null, coverageTo: null, lastAcquiredAtUtc: null, gapCount: 0, correctionCount: 0, persistence: 'notConfigured', provider: 'none', product: 'none', policy: 'BB-071-pending', provenance: 'none' },
}
const featureFixture: FinanceFeatureSnapshot = {
  generatedAtUtc:'2026-08-11T18:00:00Z',operatingMode:'research',featureSetId:'core-daily-v1',instrumentId:'US:XNAS:MSFT',definitions:[],
  revision:{revisionId:'feature-revision-1',featureSetId:'core-daily-v1',featureSetFingerprint:'sha256:set',engineVersion:'daily-feature-engine-v1',sourceMarketRevisions:['market-revision-1'],coverageFrom:'2026-08-07',coverageTo:'2026-08-10',valueCount:42,availableCount:7,warmupCount:35,qualityIssueCount:0,checksum:'sha256:feature',createdAtUtc:'2026-08-11T18:00:00Z',buildElapsedMilliseconds:12,priceBasis:'raw close/OHLC',persistence:'durable'},
  latest:[
    {definitionId:'sma.20',name:'SMA 20',period:20,value:101.25,sessionDate:'2026-08-10',state:'available',quality:'good',knowledgeTimeUtc:'2026-08-11T18:00:00Z'},
    {definitionId:'rsi.14',name:'RSI 14',period:14,value:72.125,sessionDate:'2026-08-10',state:'available',quality:'good',knowledgeTimeUtc:'2026-08-11T18:00:00Z'},
  ],historyDefinitionId:'sma.20',history:[]
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

  test('renders real EOD memory and compact retention state without live or trading claims', () => {
    const fixture: FinanceObservationSnapshot = { ...empty,
      safety: { ...empty.safety, ingestionAllowed: true, realProviderStorageAllowed: true },
      provider: { state: 'authorized', displayName: 'EODHD Free', entitlement: 'authorized', entitlementGate: 'EODHD FREE PERSONAL RESEARCH', reason: 'EOD-only', evidenceClass: 'ownerAcceptedPersonalResearch' },
      dataKind: 'real', latestMarketDataUpdateUtc: '2026-08-11T18:00:00Z',
      watchlist: [{ ...empty.watchlist[0], price: 103, currency: 'USD', dailyChangePercent: 1.48, observedAtUtc: '2026-08-10T00:00:00Z', freshness: 'delayed', session: 'closed', quality: 'good', dataKind: 'real', history: [
        { observedAtUtc: '2026-08-07T00:00:00Z', value: 101.5, beginsAfterGap: false }, { observedAtUtc: '2026-08-10T00:00:00Z', value: 103, beginsAfterGap: false },
      ] }], historicalMemory: { ...empty.historicalMemory, observationCount: 2, activeRevisionId: 'eodhd-revision', coverageFrom: '2026-08-07', coverageTo: '2026-08-10', persistence: 'durable', provider: 'EODHD', product: 'Free' },
      retention: { state: 'active', entitlementEndsAtUtc: null, deletionDeadlineUtc: null, coveredObservationCount: 2, coveredRevisionCount: 1, coveredPayloadCount: 1, deletionScope: 'covered data', lastReceiptId: null, coveredFeatureValueCount:42, coveredFeatureRevisionCount:1 },
    }
    render(<FinanceObservation initialSnapshot={fixture} initialFeatures={featureFixture} />)
    expect(screen.getByText(/REAL EOD-MARKET DATA/)).toBeVisible()
    expect(screen.getByText('Ägargodkänd personlig research')).toBeVisible()
    expect(screen.getAllByText('Aktiv').length).toBeGreaterThan(0)
    expect(screen.getByText('2 observationer / 1 market-revisioner / 1 payloads / 42 feature-värden / 1 feature-revisioner')).toBeVisible()
    expect(screen.getAllByText('Indikatorer / Features').at(-1)).toBeVisible()
    expect(screen.getByText('SMA 20')).toBeVisible()
    expect(screen.getByText('101.250000')).toBeVisible()
    expect(screen.getByText('RSI 14')).toBeVisible()
    expect(screen.getAllByText(/inga köp- eller säljsignaler/i).at(-1)).toBeVisible()
    expect(screen.queryByText(/realtid/i)).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /köp|sälj|order|trade/i })).not.toBeInTheDocument()
  })
})
