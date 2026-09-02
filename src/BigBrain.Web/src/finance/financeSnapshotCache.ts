import type { FinanceObservationSnapshot } from '../types'

export const FINANCE_SNAPSHOT_CACHE_KEY = 'bigbrain.finance.last-known-good.v1'
export const FINANCE_SNAPSHOT_CACHE_VERSION = 1
const MAX_CACHE_BYTES = 512_000
const MAX_INSTRUMENTS = 16
const MAX_HISTORY_POINTS = 400

export interface FinanceSnapshotCacheEntry {
  version: 1
  fetchedAtUtc: string
  sections: ['observation']
  snapshot: FinanceObservationSnapshot
}

function validDate(value: unknown): value is string {
  return typeof value === 'string' && !Number.isNaN(Date.parse(value))
}
const validNullableDate = (value: unknown) => value === null || validDate(value)
const validString = (value: unknown): value is string => typeof value === 'string'
const validNullableString = (value: unknown) => value === null || validString(value)
const validNumber = (value: unknown): value is number => typeof value === 'number' && Number.isFinite(value)
const validNullableNumber = (value: unknown) => value === null || validNumber(value)

export function readFinanceSnapshotCache(storage: Pick<Storage, 'getItem'> = window.localStorage): FinanceSnapshotCacheEntry | null {
  try {
    const raw = storage.getItem(FINANCE_SNAPSHOT_CACHE_KEY)
    if (!raw || raw.length > MAX_CACHE_BYTES) return null
    const entry = JSON.parse(raw) as Partial<FinanceSnapshotCacheEntry>
    if (entry.version !== FINANCE_SNAPSHOT_CACHE_VERSION || !validDate(entry.fetchedAtUtc) ||
      entry.sections?.length !== 1 || entry.sections[0] !== 'observation' || !isSnapshot(entry.snapshot)) return null
    return { version: 1, fetchedAtUtc: entry.fetchedAtUtc, sections: ['observation'], snapshot: displaySafeSnapshot(entry.snapshot) }
  } catch { return null }
}

export function writeFinanceSnapshotCache(snapshot: FinanceObservationSnapshot, fetchedAtUtc = new Date().toISOString(), storage: Pick<Storage, 'setItem'> = window.localStorage) {
  if (!isSnapshot(snapshot) || !validDate(fetchedAtUtc)) return false
  const entry: FinanceSnapshotCacheEntry = { version: FINANCE_SNAPSHOT_CACHE_VERSION, fetchedAtUtc, sections: ['observation'], snapshot: displaySafeSnapshot(snapshot) }
  const serialized = JSON.stringify(entry)
  if (serialized.length > MAX_CACHE_BYTES) return false
  try { storage.setItem(FINANCE_SNAPSHOT_CACHE_KEY, serialized); return true } catch { return false }
}

function displaySafeSnapshot(snapshot: FinanceObservationSnapshot): FinanceObservationSnapshot {
  return {
    generatedAtUtc: snapshot.generatedAtUtc,
    safety: {
      mode: snapshot.safety.mode,
      liveTradingEnabled: snapshot.safety.liveTradingEnabled,
      paperTradingEnabled: snapshot.safety.paperTradingEnabled,
      brokerConnected: snapshot.safety.brokerConnected,
      ingestionAllowed: snapshot.safety.ingestionAllowed,
      realProviderStorageAllowed: snapshot.safety.realProviderStorageAllowed,
    },
    provider: {
      state: snapshot.provider.state,
      displayName: snapshot.provider.displayName,
      entitlement: snapshot.provider.entitlement,
      entitlementGate: snapshot.provider.entitlementGate,
      reason: snapshot.provider.reason,
      ...(snapshot.provider.evidenceClass ? { evidenceClass: snapshot.provider.evidenceClass } : {}),
    },
    latestMarketDataUpdateUtc: snapshot.latestMarketDataUpdateUtc,
    dataKind: snapshot.dataKind,
    watchlist: snapshot.watchlist.map(item => ({
      instrumentId: item.instrumentId, symbol: item.symbol, displayName: item.displayName,
      price: item.price, currency: item.currency, dailyChangePercent: item.dailyChangePercent,
      observedAtUtc: item.observedAtUtc, freshness: item.freshness, session: item.session,
      quality: item.quality, dataKind: item.dataKind,
      history: item.history.map(point => ({ observedAtUtc: point.observedAtUtc, value: point.value, beginsAfterGap: point.beginsAfterGap })),
    })),
    historicalMemory: {
      observationCount: snapshot.historicalMemory.observationCount,
      activeRevisionId: snapshot.historicalMemory.activeRevisionId,
      parentRevisionId: snapshot.historicalMemory.parentRevisionId,
      coverageFrom: snapshot.historicalMemory.coverageFrom,
      coverageTo: snapshot.historicalMemory.coverageTo,
      lastAcquiredAtUtc: snapshot.historicalMemory.lastAcquiredAtUtc,
      gapCount: snapshot.historicalMemory.gapCount,
      correctionCount: snapshot.historicalMemory.correctionCount,
      persistence: snapshot.historicalMemory.persistence,
      provider: snapshot.historicalMemory.provider,
      product: snapshot.historicalMemory.product,
      policy: snapshot.historicalMemory.policy,
      provenance: snapshot.historicalMemory.provenance,
    },
    retention: snapshot.retention ? {
      state: snapshot.retention.state,
      entitlementEndsAtUtc: snapshot.retention.entitlementEndsAtUtc,
      deletionDeadlineUtc: snapshot.retention.deletionDeadlineUtc,
      coveredObservationCount: snapshot.retention.coveredObservationCount,
      coveredRevisionCount: snapshot.retention.coveredRevisionCount,
      coveredPayloadCount: snapshot.retention.coveredPayloadCount,
      deletionScope: snapshot.retention.deletionScope,
      lastReceiptId: snapshot.retention.lastReceiptId,
      coveredFeatureValueCount: snapshot.retention.coveredFeatureValueCount,
      coveredFeatureRevisionCount: snapshot.retention.coveredFeatureRevisionCount,
      coveredBacktestRunCount: snapshot.retention.coveredBacktestRunCount,
      coveredBacktestEventCount: snapshot.retention.coveredBacktestEventCount,
      coveredBacktestFillCount: snapshot.retention.coveredBacktestFillCount,
      coveredBacktestEquityPointCount: snapshot.retention.coveredBacktestEquityPointCount,
      coveredRobustnessEvaluationCount: snapshot.retention.coveredRobustnessEvaluationCount,
      coveredRobustnessWindowCount: snapshot.retention.coveredRobustnessWindowCount,
      coveredRobustnessParameterPointCount: snapshot.retention.coveredRobustnessParameterPointCount,
      coveredRobustnessCostPointCount: snapshot.retention.coveredRobustnessCostPointCount,
      coveredRobustnessRunReferenceCount: snapshot.retention.coveredRobustnessRunReferenceCount,
    } : snapshot.retention,
  }
}

function isSnapshot(value: unknown): value is FinanceObservationSnapshot {
  if (!value || typeof value !== 'object') return false
  const item = value as FinanceObservationSnapshot
  if (!validDate(item.generatedAtUtc) || !item.safety || item.safety.mode !== 'research' ||
    typeof item.safety.liveTradingEnabled !== 'boolean' || typeof item.safety.paperTradingEnabled !== 'boolean' ||
    typeof item.safety.brokerConnected !== 'boolean' || typeof item.safety.ingestionAllowed !== 'boolean' ||
    typeof item.safety.realProviderStorageAllowed !== 'boolean' || !item.provider ||
    !validString(item.provider.state) || !validString(item.provider.displayName) || !validString(item.provider.entitlement) ||
    !validString(item.provider.entitlementGate) || !validString(item.provider.reason) ||
    (item.provider.evidenceClass !== undefined && !validString(item.provider.evidenceClass)) ||
    !validNullableDate(item.latestMarketDataUpdateUtc) || !['none', 'syntheticFixture', 'real'].includes(item.dataKind) || !item.historicalMemory ||
    !Array.isArray(item.watchlist) || item.watchlist.length > MAX_INSTRUMENTS) return false
  if (!validNumber(item.historicalMemory.observationCount) || !validNullableString(item.historicalMemory.activeRevisionId) ||
    !validNullableString(item.historicalMemory.parentRevisionId) || !validNullableDate(item.historicalMemory.coverageFrom) ||
    !validNullableDate(item.historicalMemory.coverageTo) || !validNullableDate(item.historicalMemory.lastAcquiredAtUtc) ||
    !validNumber(item.historicalMemory.gapCount) || !validNumber(item.historicalMemory.correctionCount) ||
    !validString(item.historicalMemory.persistence) || !validString(item.historicalMemory.provider) ||
    !validString(item.historicalMemory.product) || !validString(item.historicalMemory.policy) || !validString(item.historicalMemory.provenance)) return false
  if (item.retention && (!validString(item.retention.state) || !validNullableDate(item.retention.entitlementEndsAtUtc) ||
    !validNullableDate(item.retention.deletionDeadlineUtc) || !validNumber(item.retention.coveredObservationCount) ||
    !validNumber(item.retention.coveredRevisionCount) || !validNumber(item.retention.coveredPayloadCount) ||
    !validString(item.retention.deletionScope) || !validNullableString(item.retention.lastReceiptId))) return false
  return item.watchlist.every(instrument => validString(instrument.instrumentId) && validString(instrument.symbol) && validString(instrument.displayName) &&
    validNullableNumber(instrument.price) && validNullableString(instrument.currency) && validNullableNumber(instrument.dailyChangePercent) &&
    validNullableDate(instrument.observedAtUtc) && validString(instrument.freshness) && validString(instrument.session) && validString(instrument.quality) && validString(instrument.dataKind) &&
    Array.isArray(instrument.history) && instrument.history.length <= MAX_HISTORY_POINTS &&
    instrument.history.every(point => validDate(point.observedAtUtc) && validNullableNumber(point.value) && typeof point.beginsAfterGap === 'boolean'))
}
