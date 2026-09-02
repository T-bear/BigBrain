import { describe, expect, test } from 'vitest'
import type { FinanceObservationSnapshot } from '../types'
import { FINANCE_SNAPSHOT_CACHE_KEY, readFinanceSnapshotCache, writeFinanceSnapshotCache } from './financeSnapshotCache'

const snapshot: FinanceObservationSnapshot = {
  generatedAtUtc: '2026-09-02T18:00:00Z',
  safety: { mode: 'research', liveTradingEnabled: false, paperTradingEnabled: false, brokerConnected: false, ingestionAllowed: false, realProviderStorageAllowed: false },
  provider: { state: 'authorized', displayName: 'EODHD Free', entitlement: 'authorized', entitlementGate: 'policy', reason: 'read-only' },
  latestMarketDataUpdateUtc: '2026-09-01T00:00:00Z', dataKind: 'real',
  watchlist: [{ instrumentId: 'US:XNAS:MSFT', symbol: 'MSFT', displayName: 'Microsoft', price: 100, currency: 'USD', dailyChangePercent: 1, observedAtUtc: '2026-09-01T00:00:00Z', freshness: 'delayed', session: 'closed', quality: 'good', dataKind: 'real', history: [] }],
  historicalMemory: { observationCount: 1, activeRevisionId: 'revision', parentRevisionId: null, coverageFrom: '2026-09-01', coverageTo: '2026-09-01', lastAcquiredAtUtc: '2026-09-02T00:00:00Z', gapCount: 0, correctionCount: 0, persistence: 'durable', provider: 'EODHD', product: 'Free', policy: 'policy', provenance: 'verified' },
}

describe('Finance last-known-good cache', () => {
  test('ignores malformed and incompatible cache entries', () => {
    localStorage.setItem(FINANCE_SNAPSHOT_CACHE_KEY, '{bad')
    expect(readFinanceSnapshotCache()).toBeNull()
    localStorage.setItem(FINANCE_SNAPSHOT_CACHE_KEY, JSON.stringify({ version: 999, fetchedAtUtc: '2026-09-02T18:00:00Z', sections: ['observation'], snapshot }))
    expect(readFinanceSnapshotCache()).toBeNull()
    localStorage.setItem(FINANCE_SNAPSHOT_CACHE_KEY, JSON.stringify({ version: 1, fetchedAtUtc: '2026-09-02T18:00:00Z', sections: ['observation'], snapshot: { ...snapshot, watchlist: [{ ...snapshot.watchlist[0], price: 'not-a-number' }] } }))
    expect(readFinanceSnapshotCache()).toBeNull()
  })

  test('persists only an explicit display-safe projection and never unexpected credential fields', () => {
    const unsafe = { ...snapshot, apiKey: 'must-not-persist', provider: { ...snapshot.provider, authorizationToken: 'must-not-persist' } } as FinanceObservationSnapshot
    expect(writeFinanceSnapshotCache(unsafe)).toBe(true)
    const raw = localStorage.getItem(FINANCE_SNAPSHOT_CACHE_KEY)!
    expect(raw).not.toContain('must-not-persist')
    expect(readFinanceSnapshotCache()?.snapshot).toEqual(snapshot)
  })

  test('rejects an unbounded watchlist instead of persisting an arbitrary response', () => {
    const oversized = { ...snapshot, watchlist: Array.from({ length: 17 }, (_, index) => ({ ...snapshot.watchlist[0], instrumentId: `instrument-${index}` })) }
    expect(writeFinanceSnapshotCache(oversized)).toBe(false)
    expect(localStorage.getItem(FINANCE_SNAPSHOT_CACHE_KEY)).toBeNull()
  })
})
