import { useEffect, useState } from 'react'
import { getFinanceFeatures, getFinanceObservation } from '../api'
import type { FinanceFeatureSnapshot, FinanceObservationSnapshot } from '../types'

const labels: Record<string, string> = {
  noneAuthorized: 'Ingen provider auktoriserad', candidate: 'Kandidat', authorized: 'Auktoriserad', unavailable: 'Saknas', unknown: 'Okänd',
  pendingWrittenConfirmation: 'Skriftlig bekräftelse väntar', denied: 'Nekad', expired: 'Utgången', current: 'Aktuell', delayed: 'Fördröjd', stale: 'Inaktuell',
  open: 'Öppen', closed: 'Stängd', preMarket: 'Förhandel', gap: 'Datagap', outage: 'Avbrott', good: 'God', warning: 'Varning', error: 'Fel',
  notConfigured: 'Inte konfigurerad', fixtureMemory: 'Fixture-minne', durable: 'Beständig',
  ownerAcceptedPersonalResearch: 'Ägargodkänd personlig research', active: 'Aktiv', deletionRequired: 'Radering krävs', expiredBlocked: 'Utgången / blockerad', deletionComplete: 'Radering klar',
  available: 'Tillgänglig', warmup: 'Warmup', gapAffected: 'Gap-påverkad', invalidInput: 'Ogiltig input',
}
const label = (value: string) => labels[value] ?? value
const formatTime = (value: string | null) => value ? new Date(value).toLocaleString('sv-SE') : 'Ingen uppdatering'

export function FinancePriceChart({ instrument }: { instrument: FinanceObservationSnapshot['watchlist'][number] | null }) {
  const points = instrument?.history.filter(point => point.value !== null) ?? []
  if (!instrument || points.length < 2) return <div className="finance-chart-empty" role="status"><strong>Ingen prishistorik</strong><span>Diagrammet visas när observationer med historik finns.</span></div>
  const values = points.map(point => point.value as number)
  const min = Math.min(...values), max = Math.max(...values), range = Math.max(max - min, 0.0001)
  const segments: string[] = []
  let current = ''
  points.forEach((point, index) => {
    const x = (index / (points.length - 1)) * 100
    const y = 38 - (((point.value as number) - min) / range) * 34
    if (point.beginsAfterGap && current) { segments.push(current); current = '' }
    current += `${current ? ' L' : 'M'} ${x.toFixed(2)} ${y.toFixed(2)}`
  })
  if (current) segments.push(current)
  return <figure className="finance-chart" aria-labelledby="finance-chart-caption"><svg aria-hidden="true" preserveAspectRatio="none" viewBox="0 0 100 42">{segments.map((path, index) => <path d={path} key={index} />)}</svg><figcaption id="finance-chart-caption">{instrument.symbol}: {points.length} punkter, lägst {min.toFixed(2)} och högst {max.toFixed(2)} {instrument.currency}. Datagap ritas inte som sammanhängande linjer.</figcaption></figure>
}

const visibleFeatures = ['sma.20','ema.20','rsi.14','atr.14','volatility.20','momentum.20','volume.ratio.20']

export function FinanceObservation({ initialSnapshot, initialFeatures }: { initialSnapshot?: FinanceObservationSnapshot; initialFeatures?: FinanceFeatureSnapshot }) {
  const [snapshot, setSnapshot] = useState<FinanceObservationSnapshot | null>(initialSnapshot ?? null)
  const [features, setFeatures] = useState<FinanceFeatureSnapshot | null>(initialFeatures ?? null)
  const [failed, setFailed] = useState(false)
  const [selected, setSelected] = useState<string | null>(initialSnapshot?.watchlist.find(item => item.price !== null)?.instrumentId ?? null)
  useEffect(() => {
    if (initialSnapshot) return
    const controller = new AbortController()
    getFinanceObservation(controller.signal).then(value => { setSnapshot(value); setSelected(value.watchlist.find(item => item.price !== null)?.instrumentId ?? null) }).catch(error => { if (error instanceof Error && error.name !== 'AbortError') setFailed(true) })
    return () => controller.abort()
  }, [initialSnapshot])
  useEffect(() => {
    if (!selected || (initialFeatures && selected === initialFeatures.instrumentId)) return
    const controller = new AbortController()
    getFinanceFeatures(selected, controller.signal).then(setFeatures).catch(error => { if (error instanceof Error && error.name !== 'AbortError') setFeatures(null) })
    return () => controller.abort()
  }, [selected, initialFeatures])
  if (failed) return <section className="finance-view"><div className="notice notice--error" role="alert"><strong>Finance är otillgängligt</strong><p>Read-only-status kunde inte hämtas. Ingen handel eller datainhämtning har startats.</p></div></section>
  if (!snapshot) return <section className="finance-view" aria-busy="true"><p aria-live="polite">Hämtar Finance-status…</p></section>
  const selectedInstrument = snapshot.watchlist.find(item => item.instrumentId === selected) ?? null
  const memory = snapshot.historicalMemory
  return <section className="finance-view" aria-labelledby="finance-title">
    <header className="finance-hero"><div><p className="finance-eyebrow">Marknadsobservation</p><h2 id="finance-title">Finance <span className="finance-research-badge">RESEARCH</span></h2><p className="finance-safety-copy">Ingen handel med riktiga pengar</p></div><dl className="finance-status-strip"><div><dt>Systemläge</dt><dd>RESEARCH</dd></div><div><dt>Provider</dt><dd>{label(snapshot.provider.state)}</dd></div><div><dt>Entitlement</dt><dd>{label(snapshot.provider.entitlement)}</dd></div><div><dt>Senast uppdaterad</dt><dd>{formatTime(snapshot.latestMarketDataUpdateUtc)}</dd></div></dl></header>
    {snapshot.dataKind === 'syntheticFixture' && <p className="finance-synthetic-banner" role="status">SYNTHETISK FIXTURE – inte realt eller live market data</p>}
    {snapshot.dataKind === 'real' && <p className="finance-real-banner" role="status">REAL EOD-MARKET DATA – senaste avslutade session, inte live</p>}
    <div className="finance-layout">
      <section className="finance-panel finance-panel--wide" aria-labelledby="watchlist-title"><header><div><h3 id="watchlist-title">Research-watchlist</h3><p>Konfigurerade instrument innebär inte auktoriserad ingestion.</p></div><span>{snapshot.watchlist.filter(item => item.price !== null).length} observationer</span></header><div className="finance-watchlist">{snapshot.watchlist.map(item => <button aria-pressed={selected === item.instrumentId} className="finance-instrument" key={item.instrumentId} onClick={() => setSelected(item.instrumentId)} type="button"><span><strong>{item.symbol}</strong><small>{item.displayName}</small><small>{item.instrumentId}</small></span><span className="finance-instrument__value"><strong>{item.price === null ? 'Ingen observation' : `${item.price.toFixed(2)} ${item.currency}`}</strong>{item.dailyChangePercent !== null && <small>{item.dailyChangePercent >= 0 ? '+' : ''}{item.dailyChangePercent.toFixed(2)} %</small>}<small>{label(item.freshness)} · {label(item.session)}</small>{item.dataKind === 'syntheticFixture' && <mark>Syntetisk</mark>}{['warning','gap','error'].includes(item.quality) && <mark className="finance-warning">Kvalitet: {label(item.quality)}</mark>}</span></button>)}</div></section>
      <section className="finance-panel finance-panel--wide" aria-labelledby="chart-title"><header><div><h3 id="chart-title">Prishistorik</h3><p>Provider-neutral observationsserie</p></div></header><FinancePriceChart instrument={selectedInstrument} /></section>
      <section className="finance-panel finance-panel--wide" aria-labelledby="features-title"><header><div><h3 id="features-title">Indikatorer / Features</h3><p>Mätvärden för research – inga köp- eller säljsignaler</p></div><span>{features?.featureSetId ?? 'core-daily-v1'}</span></header>
        {!features?.revision ? <p className="muted">Ingen feature-revision tillgänglig ännu.</p> : <><dl className="finance-details"><div><dt>Feature-revision</dt><dd>{features.revision.revisionId}</dd></div><div><dt>Market-revisioner</dt><dd>{features.revision.sourceMarketRevisions.length}</dd></div><div><dt>Prisbas</dt><dd>{features.revision.priceBasis}</dd></div><div><dt>Kvalitet / warmup</dt><dd>{features.revision.qualityIssueCount} / {features.revision.warmupCount}</dd></div></dl><div className="finance-feature-grid">{visibleFeatures.map(id=>{const value=features.latest.find(item=>item.definitionId===id);return <article className="finance-feature" key={id}><strong>{value?.name ?? id}</strong><span>{value?.value === null || value?.value === undefined ? label(value?.state ?? 'unavailable') : value.value.toFixed(6)}</span><small>{value?.sessionDate ?? 'Ingen'} · {label(value?.quality ?? 'unknown')}</small></article>})}</div></>}
      </section>
      <section className="finance-panel" aria-labelledby="memory-title"><header><div><h3 id="memory-title">Historiskt minne</h3><p>Immutable revisions- och kvalitetsöversikt</p></div></header><dl className="finance-details"><div><dt>Observationer</dt><dd>{memory.observationCount}</dd></div><div><dt>Aktiv revision</dt><dd>{memory.activeRevisionId ?? 'Ingen'}</dd></div><div><dt>Täckning</dt><dd>{memory.coverageFrom && memory.coverageTo ? `${memory.coverageFrom} – ${memory.coverageTo}` : 'Ingen'}</dd></div><div><dt>Senaste acquisition</dt><dd>{formatTime(memory.lastAcquiredAtUtc)}</dd></div><div><dt>Gap / korrigeringar</dt><dd>{memory.gapCount} / {memory.correctionCount}</dd></div><div><dt>Persistence</dt><dd>{label(memory.persistence)}</dd></div><div><dt>Policy / provenance</dt><dd>{memory.policy} / {memory.provenance}</dd></div></dl></section>
      <section className="finance-panel finance-entitlement" aria-labelledby="entitlement-title"><header><div><h3 id="entitlement-title">Provider och entitlement</h3><p>{snapshot.provider.entitlementGate}</p></div></header><dl className="finance-details"><div><dt>Provider</dt><dd>{snapshot.provider.displayName}</dd></div><div><dt>Evidensklass</dt><dd>{label(snapshot.provider.evidenceClass ?? 'unknown')}</dd></div><div><dt>Ingestion tillåten</dt><dd>{snapshot.safety.ingestionAllowed ? 'JA' : 'NEJ'}</dd></div><div><dt>Lagring av real providerdata</dt><dd>{snapshot.safety.realProviderStorageAllowed ? 'JA' : 'NEJ'}</dd></div><div><dt>Orsak</dt><dd>{snapshot.provider.reason}</dd></div></dl></section>
      {snapshot.retention && <section className="finance-panel" aria-labelledby="retention-title"><header><div><h3 id="retention-title">Retention</h3><p>EODHD-datas livscykel</p></div></header><dl className="finance-details"><div><dt>Status</dt><dd>{label(snapshot.retention.state)}</dd></div><div><dt>Raderingsfrist</dt><dd>{formatTime(snapshot.retention.deletionDeadlineUtc)}</dd></div><div><dt>Omfattning</dt><dd>{snapshot.retention.coveredObservationCount} observationer / {snapshot.retention.coveredRevisionCount} market-revisioner / {snapshot.retention.coveredPayloadCount} payloads / {snapshot.retention.coveredFeatureValueCount ?? 0} feature-värden / {snapshot.retention.coveredFeatureRevisionCount ?? 0} feature-revisioner</dd></div>{snapshot.retention.lastReceiptId && <div><dt>Kvitto</dt><dd>{snapshot.retention.lastReceiptId}</dd></div>}</dl></section>}
    </div>
  </section>
}
