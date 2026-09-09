import { act, cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import { StrictMode } from 'react'
import { afterEach, describe, expect, test, vi } from 'vitest'
import * as api from '../api'
import { aggregateSignalRisk, FinanceObservation } from './FinanceObservation'
import { FINANCE_SNAPSHOT_CACHE_KEY, writeFinanceSnapshotCache } from './financeSnapshotCache'
import type { FinanceAutonomousResearch, FinanceBackupInventory, FinanceBacktestCatalog, FinanceBacktestResult, FinanceFeatureSnapshot, FinanceObservationSnapshot, FinanceOverview, FinanceResearchOperationsStatus, FinanceResearchResourceDecision, FinanceResearchSchedulerStatus, FinanceRiskEvaluation, FinanceRiskStatus, FinanceRobustnessCatalog, FinanceRobustnessEvaluation, FinanceShadowCatalog } from '../types'
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
const backupFixture:FinanceBackupInventory={generatedAtUtc:'2026-08-15T18:00:00Z',operatingMode:'RESEARCH',backups:[{backupId:'finance-backup-fixture',createdAtUtc:'2026-08-15T18:00:00Z',schemaVersion:'finance-provider-backup-v1',bigBrainVersion:'test',status:'Complete',sources:[{provider:'NASDAQ-WIKI',product:'WIKI/PRICES',rightsClass:'PublicDomain',retentionClass:'Indefinite',deletionRequirement:'None',deletionDeadlineUtc:null,backupEligibility:'Eligible',restoreEligible:true,reason:'verified'}],revisions:[{revisionId:'wiki-fixture',provider:'NASDAQ-WIKI',product:'WIKI/PRICES',policy:'dataset-promotion-v1',checksum:'sha256:fixture',observationCount:50,coverageFrom:'2016-01-01',coverageTo:'2016-02-19'}],featureRevisionIds:['feature-fixture'],backtestRunIds:[],robustnessEvaluationIds:[],artifacts:[{path:'fixture.data.json',bytes:100,sha256:'sha256:fixture'}],contentFingerprint:'sha256:fixture'}],sourcePolicies:[{provider:'NASDAQ-WIKI',product:'WIKI/PRICES',rightsClass:'PublicDomain',retentionClass:'Indefinite',deletionRequirement:'None',deletionDeadlineUtc:null,backupEligibility:'Eligible',restoreEligible:true,reason:'verified'},{provider:'EODHD',product:'Free',rightsClass:'OwnerAcceptedPersonalResearch',retentionClass:'SubscriptionOnly',deletionRequirement:'DeleteAtSubscriptionEnd',deletionDeadlineUtc:null,backupEligibility:'Restricted',restoreEligible:true,reason:'provider-specific'}]}
const overviewFixture:FinanceOverview={generatedAtUtc:'2026-08-15T19:00:00Z',mode:'RESEARCH',provider:'EODHD',observationClass:'CURRENT EOD / PROSPECTIVE EOD',latestSession:'2026-08-14',freshness:'CURRENT EOD',tracked:2,up:1,down:1,unchanged:0,marketSummary:'1 av 2 bevakade instrument steg under senaste tillgängliga marknadssessionen; 1 föll och 0 var oförändrade.',signals:[{instrumentId:'US:XNAS:AAPL',symbol:'AAPL',name:'Apple',state:'POSITIVE',sessionChangePercent:1.2,positiveStrategies:2,neutralStrategies:0,negativeStrategies:1,strategyCount:3,agreement:'2/3 strategies agree; positive 2, neutral 0, negative 1',freshness:'Delayed'},{instrumentId:'US:XNAS:MSFT',symbol:'MSFT',name:'Microsoft',state:'NEUTRAL',sessionChangePercent:-.2,positiveStrategies:1,neutralStrategies:1,negativeStrategies:1,strategyCount:3,agreement:'1/3 strategies agree; positive 1, neutral 1, negative 1',freshness:'Delayed'}],prospective:{valid:24,pending:24,evaluated:0,invalidated:24,correct:0,incorrect:0,directionalAccuracy:null,meanRealizedReturn:null,evidenceMaturity:'BOOTSTRAPPING',curve:[]},cadence:{enabled:true,provider:'EODHD',observationClass:'CURRENT EOD / PROSPECTIVE EOD',health:'Healthy',lastProviderCheckUtc:'2026-08-15T19:00:00Z',lastSuccessfulAcquisitionUtc:null,latestCanonicalSession:'2026-08-14',lastPredictionUtc:'2026-08-15T18:00:00Z',lastOutcomeUtc:null,pending:24,evaluated:0,invalidated:24,clockIntegrity:true,nextAction:'Waiting for next weekday EOD provider window',pollingPolicy:'internal check every 30 minutes',operatingMode:'RESEARCH'},disclaimer:'Research results — no money is traded. Signals are not recommendations.',evidenceSeparation:'Prospective evidence records prior decisions; historical backtests remain separate and are not included.'}
const riskStatusFixture:FinanceRiskStatus={policyVersion:'research-eod-v1',operatingMode:'RESEARCH',engineHealth:'Healthy',safetyState:'READY',activeHalt:false,haltScope:'SYSTEM',haltReason:null,haltedAtUtc:null,evaluationCount:1,lastEvaluationUtc:'2026-08-16T10:00:00Z',executionAuthority:'NONE — research evidence only; no orders'}
const riskEvaluationFixture:FinanceRiskEvaluation={evaluationId:'risk-fixture',policyVersion:'research-eod-v1',proposalId:'proposal-fixture',instrumentId:'US:XNAS:AAPL',strategyId:'momentum',strategyVersion:'v1',parameterFingerprint:'sha256:fixture',shadowPredictionId:'shadow-fixture',sourceRevisionId:'source-fixture',featureRevisionId:'feature-fixture',knowledgeCutoffUtc:'2026-08-16T09:59:00Z',evaluatedAtUtc:'2026-08-16T10:00:00Z',operatingMode:'RESEARCH',direction:'TargetLong',researchCapital:100000,requestedExposure:4000,allowedExposure:4000,riskAdjustedExposure:4000,verdict:'allow',reasonCodes:[],rules:[],evidenceLineage:'source=source-fixture'}
const autonomousFixture:FinanceAutonomousResearch={generatedAtUtc:'2026-08-22T10:00:00Z',operatingMode:'RESEARCH',budgetSek:0,engineVersion:'autonomous-research-v1',featureLibraryVersion:'finance-research-signals-v1',totalExperiments:1,rejectedCount:1,inconclusiveCount:0,notEvaluableCount:0,promisingCount:0,challengerCount:0,status:'CONTINUE_RESEARCH',executionAuthority:'NONE',features:[],hypotheses:[],latestRun:{runId:'research-run-fixture',state:'completed',experimentCount:1,rejectedCount:1,inconclusiveCount:0,notEvaluableCount:0,promisingCount:0,challengerCount:0,failureReason:null,recoveryStatus:'NONE',experiments:[{experimentId:'experiment-fixture',familyId:'family-momentum-v1',familyAttemptCount:3,attemptCount:3,runId:'research-run-fixture',runIds:['research-run-fixture'],verdict:'rejected',rejectionReason:'integrity.out-of-sample.failed',outOfSampleNetReturn:-.02,costModel:'hypothetical-conservative-v1',featureRevisionId:'feature-fixture',marketRevisionIds:['market-fixture'],knowledgeCutoffUtc:'2026-08-21T22:00:00Z',complexity:{score:6},integrity:{state:'fail',checks:[{id:'out-of-sample',state:'fail',evidence:'net=-0.02'},{id:'dsr',state:'notEvaluable',evidence:'inputs unavailable'}]}}]}}
const schedulerFixture:FinanceResearchSchedulerStatus={currentUtc:'2026-08-23T01:00:00Z',enabled:true,schedulerVersion:'finance-research-scheduler-v1',nextDueUtc:'2026-08-23T02:00:00Z',lastOpportunity:{opportunityId:'finance-research-scheduler-v1:2026-08-22',researchDate:'2026-08-22',dueAtUtc:'2026-08-23T02:00:00Z',attemptedAtUtc:'2026-08-23T02:03:00Z',completedAtUtc:'2026-08-23T02:04:00Z',state:'Completed',researchRunId:'research-run-fixture',reason:'finance.research.scheduler.completed',nextEligibilityUtc:null},lastResearchRunId:'research-run-fixture',lastOutcome:'Completed',lastReason:'finance.research.scheduler.completed',researchCurrentlyRunning:false,operatingMode:'RESEARCH',budgetSek:0,executionAuthority:'NONE',historicalEvidenceAvailable:true,currentSessionRequired:true,requiredResearchDate:'2026-08-22',currentSessionReadiness:'COMPLETE',featureLineageReadiness:'READY',dataReady:true,readinessReason:'finance.research.scheduler.ready',currentInstrumentCount:8,expectedInstrumentCount:8}
const governorFixture:FinanceResearchResourceDecision={decision:'defer',evaluatedAtUtc:'2026-08-23T01:00:00Z',governorVersion:'finance-research-resource-governor-v1',reasonCodes:['finance.research.scheduler.resource.memory'],evidence:{cpuUsagePercent:32,memoryUsagePercent:88,availableMemoryBytes:536870912,minimumAvailableDiskBytes:107374182400,availableDiskCount:1,temperatureCelsius:null,temperatureSupported:false,metricsStatus:'Healthy',collectedAtUtc:'2026-08-23T01:00:00Z'},operatingMode:'RESEARCH',budgetSek:0,executionAuthority:'NONE'}
const operationsFixture:FinanceResearchOperationsStatus={operationsVersion:'finance-research-operations-v1',evaluatedAtUtc:'2026-08-23T03:00:00Z',state:'attentionRequired',requiresAttention:true,currentActivity:'OPERATIONAL_FAILURE_STREAK',schedulerEnabled:true,maintenancePaused:false,lastSchedulerEvaluationUtc:'2026-08-23T03:00:00Z',lastSuccessfulResearchUtc:'2026-08-22T02:04:00Z',lastOperationalFailureUtc:'2026-08-23T03:00:00Z',consecutiveOperationalFailures:3,lastFailureReason:'finance.research.scheduler.unexpected.SqliteException',lastSuccessfulEvidenceRefreshUtc:'2026-08-22T22:10:00Z',historicalEvidenceAvailable:true,currentSessionRequired:true,requiredResearchDate:'2026-08-22',dataReadiness:'COMPLETE',featureLineageReadiness:'READY',resourceDecision:'DEFER',activeResearchRunId:null,operatingMode:'RESEARCH',budgetSek:0,executionAuthority:'NONE'}

const backtestCatalog: FinanceBacktestCatalog = {
  generatedAtUtc: '2026-09-06T12:00:00Z', operatingMode: 'RESEARCH', strategies: [],
  runs: ['first', 'second', 'third'].map(id => ({
    runId: id, checksum: `checksum-${id}`, strategyId: `strategy-${id}`, strategyVersion: 'v1',
    parameters: {}, costModel: 'fixture-cost', from: '2020-01-01', to: '2021-01-01',
    initialEquity: 100, finalEquity: 101, grossReturn: .01, netReturn: .01,
    maxDrawdown: 0, trades: 1, costImpact: 0, benchmarkReturn: null, excessReturn: null,
    marketRevisionIds: ['fixture-market'], featureRevisionId: 'fixture-feature',
    simulationModel: 'fixture', sizingPolicy: 'fixture', status: 'complete', limitations: [],
  })),
}
const backtestResultFixture: FinanceBacktestResult = {
  runId: 'first', checksum: 'checksum-first', fills: [], events: [], metrics: {},
  equityCurve: [0, 1].map(day => ({ session: `2020-01-0${day + 1}`, cash: 100,
    holdingsValue: day, totalEquity: 100 + day, drawdown: 0 })),
}
const setResearchOpen = (open: boolean) => {
  const details = screen.getAllByText('Detaljer & forskning').at(-1)!.closest('details')!
  details.open = open
  fireEvent(details, new Event('toggle'))
}

const openResearchDetails = () => fireEvent.click(screen.getAllByText('Detaljer & forskning').at(-1)!)

afterEach(() => {
  cleanup()
  vi.useRealTimers()
  vi.restoreAllMocks()
})

describe('Finance read-only observation UI', () => {

  test('nine detail reads abort on close, restart on reopen, and ignore ordinary observation refresh', async () => {
    const snapshot = { ...empty, watchlist: [{ ...empty.watchlist[0], price: 100 }] }
    vi.spyOn(api, 'getFinanceObservation').mockResolvedValue(snapshot)
    const features = vi.spyOn(api, 'getFinanceFeatures').mockImplementation(() => new Promise(() => {}))
    const details = [vi.spyOn(api, 'getFinanceBacktests'), vi.spyOn(api, 'getFinanceRobustness'),
      vi.spyOn(api, 'getFinanceDatasets'), vi.spyOn(api, 'getFinanceBackups'), vi.spyOn(api, 'getFinanceShadow'),
      vi.spyOn(api, 'getFinanceResearchSchedulerStatus'), vi.spyOn(api, 'getFinanceResearchGovernorStatus'),
      vi.spyOn(api, 'getFinanceResearchOperationsStatus')]
    details.forEach(read => read.mockImplementation(() => new Promise<never>(() => {})))
    const view = render(<FinanceObservation />)
    await screen.findByText('Ingen handel med riktiga pengar')
    details.forEach(read => expect(read).not.toHaveBeenCalled())
    expect(features).not.toHaveBeenCalled()
    setResearchOpen(true)
    details.forEach(read => expect(read).toHaveBeenCalledTimes(1))
    expect(features).toHaveBeenCalledTimes(1)
    await act(async () => { fireEvent(window, new Event('online')) })
    details.forEach(read => expect(read).toHaveBeenCalledTimes(1))
    expect(features).toHaveBeenCalledTimes(1)
    setResearchOpen(false)
    details.forEach(read => expect(read.mock.calls[0][0]?.aborted).toBe(true))
    expect(features.mock.calls[0][1]?.aborted).toBe(true)
    setResearchOpen(true)
    details.forEach(read => expect(read).toHaveBeenCalledTimes(2))
    expect(features).toHaveBeenCalledTimes(2)
    view.unmount()
    details.forEach(read => expect(read.mock.calls[1][0]?.aborted).toBe(true))
    expect(features.mock.calls[1][1]?.aborted).toBe(true)
  })

  test('backtest catalog selects first result; close does not abort selected-result read and reopen does not refetch same ID', async () => {
    const catalog = vi.spyOn(api, 'getFinanceBacktests').mockResolvedValue(backtestCatalog)
    let finish: (value: FinanceBacktestResult) => void = () => {}
    const result = vi.spyOn(api, 'getFinanceBacktest').mockImplementation(() => new Promise(resolve => { finish = resolve }))
    const view = render(<FinanceObservation initialSnapshot={empty} />)
    expect(result).not.toHaveBeenCalled()
    setResearchOpen(true)
    await waitFor(() => expect(result).toHaveBeenCalledTimes(1))
    expect(result.mock.calls[0][0]).toBe('first')
    setResearchOpen(false)
    expect(catalog.mock.calls[0][0]?.aborted).toBe(true)
    expect(result.mock.calls[0][1]?.aborted).toBe(false)
    await act(async () => { finish(backtestResultFixture) })
    setResearchOpen(true)
    await waitFor(() => expect(catalog).toHaveBeenCalledTimes(2))
    expect(result).toHaveBeenCalledTimes(1)
    expect(screen.getByLabelText('Equity curve och drawdown')).toBeVisible()
    view.unmount()
    expect(result.mock.calls[0][1]?.aborted).toBe(true)
  })

  test('rapid A to B to C ignores stale B completion and shows only C result', async () => {
    vi.spyOn(api, 'getFinanceBacktests').mockResolvedValue(backtestCatalog)
    const pending: Array<{ id: string; resolve: (value: FinanceBacktestResult) => void }> = []
    const result = vi.spyOn(api, 'getFinanceBacktest').mockImplementation(id => new Promise(resolve => { pending.push({ id, resolve }) }))
    render(<FinanceObservation initialSnapshot={empty} />)
    setResearchOpen(true)
    await waitFor(() => expect(result).toHaveBeenCalledTimes(1))
    fireEvent.click(screen.getByRole('button', { name: /strategy-second/ }))
    fireEvent.click(screen.getByRole('button', { name: /strategy-third/ }))
    await waitFor(() => expect(result).toHaveBeenCalledTimes(3))
    await act(async () => { pending[1].resolve({ ...backtestResultFixture, runId: 'second', checksum: 'checksum-second' }) })
    expect(screen.getByText('strategy-third / v1')).toBeVisible()
    expect(screen.queryByLabelText('Equity curve och drawdown')).not.toBeInTheDocument()
    await act(async () => { pending[2].resolve({ ...backtestResultFixture, runId: 'third', checksum: 'checksum-third' }) })
    expect(screen.getByLabelText('Equity curve och drawdown')).toBeVisible()
    expect(screen.getByText('third / checksum-third')).toBeVisible()
  })

  test('backtest selection never pairs new summary with prior curve while pending or failed', async () => {
    vi.spyOn(api, 'getFinanceBacktests').mockResolvedValue(backtestCatalog)
    let rejectSecond: (reason: Error) => void = () => {}
    const result = vi.spyOn(api, 'getFinanceBacktest').mockResolvedValueOnce(backtestResultFixture)
      .mockImplementation(() => new Promise((_resolve, reject) => { rejectSecond = reject }))
    render(<FinanceObservation initialSnapshot={empty} />)
    setResearchOpen(true)
    await screen.findByLabelText('Equity curve och drawdown')
    fireEvent.click(screen.getByRole('button', { name: /strategy-second/ }))
    expect(result.mock.calls[1][0]).toBe('second')
    expect(result.mock.calls[0][1]?.aborted).toBe(true)
    expect(screen.getByText('second / checksum-second')).toBeVisible()
    // Regression: selected B must never retain A's visualization.
    expect(screen.queryByLabelText('Equity curve och drawdown')).not.toBeInTheDocument()
    await act(async () => { rejectSecond(new Error('unavailable')) })
    expect(screen.queryByLabelText('Equity curve och drawdown')).not.toBeInTheDocument()
    expect(screen.getByText('second / checksum-second')).toBeVisible()
    expect(screen.getByText('Ingen handel med riktiga pengar')).toBeVisible()
  })

  test('cached observation starts secondary reads while refresh is pending and aborts them on exit', async () => {
    writeFinanceSnapshotCache(empty, '2026-09-02T18:04:00Z')
    const observation = vi.spyOn(api, 'getFinanceObservation').mockImplementation(() => new Promise(() => {}))
    const secondary = [
      vi.spyOn(api, 'getFinanceOverview').mockImplementation(() => new Promise(() => {})),
      vi.spyOn(api, 'getFinanceRiskStatus').mockImplementation(() => new Promise(() => {})),
      vi.spyOn(api, 'getFinanceRiskEvaluations').mockImplementation(() => new Promise(() => {})),
      vi.spyOn(api, 'getFinanceAutonomousResearch').mockImplementation(() => new Promise(() => {})),
    ]
    const view = render(<FinanceObservation />)
    expect(screen.getByText('Ingen handel med riktiga pengar')).toBeVisible()
    expect(screen.getByText('Riskstatus kunde inte läsas. Inget riskgodkännande antas.')).toBeVisible()
    secondary.forEach(request => expect(request).toHaveBeenCalledTimes(1))
    expect(observation.mock.calls[0][0]?.aborted).toBe(false)
    view.unmount()
    expect(observation.mock.calls[0][0]?.aborted).toBe(true)
    secondary.forEach(request => expect(request.mock.calls[0][0]?.aborted).toBe(true))
    fireEvent(window, new Event('online'))
    fireEvent(document, new Event('visibilitychange'))
    expect(observation).toHaveBeenCalledTimes(1)
  })

  test('hidden visibility does not refresh; online still recovers while hidden without overlap or polling', async () => {
    vi.useFakeTimers()
    const visibility = vi.spyOn(document, 'visibilityState', 'get').mockReturnValue('visible')
    let finish: (value: FinanceObservationSnapshot) => void = () => {}
    const observation = vi.spyOn(api, 'getFinanceObservation').mockResolvedValueOnce(empty)
      .mockImplementation(() => new Promise(resolve => { finish = resolve }))
    render(<FinanceObservation />)
    await act(async () => {})
    visibility.mockReturnValue('hidden')
    fireEvent(document, new Event('visibilitychange'))
    await act(async () => { await vi.advanceTimersByTimeAsync(60_000) })
    expect(observation).toHaveBeenCalledTimes(1)
    fireEvent(window, new Event('online'))
    expect(observation).toHaveBeenCalledTimes(2)
    visibility.mockReturnValue('visible')
    fireEvent(document, new Event('visibilitychange'))
    fireEvent(window, new Event('online'))
    expect(observation).toHaveBeenCalledTimes(2)
    // An ordinary refresh of fresh content has no stale banner or new initial loader.
    expect(screen.queryByText('Visar senast hämtade data')).not.toBeInTheDocument()
    expect(screen.queryByText('Hämtar Finance-status')).not.toBeInTheDocument()
    await act(async () => { finish(empty) })
  })

  test('StrictMode abort cleanup cannot clear the replacement in-flight refresh or overwrite its cache', async () => {
    const requests: Array<{ signal?: AbortSignal; finish: (value: FinanceObservationSnapshot) => void }> = []
    const observation = vi.spyOn(api, 'getFinanceObservation').mockImplementation(signal => new Promise((resolve, reject) => {
      requests.push({ signal, finish: resolve })
      // Exercise the existing same-realm Error/name guard (jsdom DOMException is a different realm).
      signal?.addEventListener('abort', () => reject(Object.assign(new Error('aborted'), { name: 'AbortError' })))
    }))
    const view = render(<StrictMode><FinanceObservation /></StrictMode>)
    expect(requests).toHaveLength(2)
    expect(requests[0].signal?.aborted).toBe(true)
    expect(requests[1].signal?.aborted).toBe(false)
    await act(async () => {})
    fireEvent(window, new Event('online'))
    expect(observation).toHaveBeenCalledTimes(2)
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    await act(async () => { requests[1].finish(empty) })
    const cache = localStorage.getItem(FINANCE_SNAPSHOT_CACHE_KEY)
    fireEvent(window, new Event('online'))
    expect(requests).toHaveLength(3)
    view.unmount()
    expect(requests[2].signal?.aborted).toBe(true)
    await act(async () => { requests[2].finish({ ...empty, generatedAtUtc: '2026-09-06T10:00:00Z' }) })
    expect(localStorage.getItem(FINANCE_SNAPSHOT_CACHE_KEY)).toBe(cache)
    fireEvent(window, new Event('online'))
    expect(observation).toHaveBeenCalledTimes(3)
  })

  test('an explicit initial snapshot bypasses cache acquisition and observation recovery listeners', () => {
    writeFinanceSnapshotCache(empty, '2026-09-02T18:04:00Z')
    const observation = vi.spyOn(api, 'getFinanceObservation')
    render(<FinanceObservation initialSnapshot={empty} />)
    fireEvent(window, new Event('online'))
    fireEvent(document, new Event('visibilitychange'))
    expect(observation).not.toHaveBeenCalled()
    expect(screen.queryByText('Visar senast hämtade data')).not.toBeInTheDocument()
    expect(screen.getByText('Ingen handel med riktiga pengar')).toBeVisible()
  })

  test('failed cache persistence does not turn a successful observation into an unavailable read', async () => {
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => { throw new Error('quota') })
    vi.spyOn(api, 'getFinanceObservation').mockResolvedValue(empty)
    render(<FinanceObservation />)
    expect(await screen.findByText('Ingen handel med riktiga pengar')).toBeVisible()
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
    expect(screen.queryByText('Visar senast hämtade data')).not.toBeInTheDocument()
    expect(localStorage.getItem(FINANCE_SNAPSHOT_CACHE_KEY)).toBeNull()
  })

  test('first-load retry keeps the failure message and only a disabled accessible button loader until success', async () => {
    let finish: (value: FinanceObservationSnapshot) => void = () => {}
    vi.spyOn(api, 'getFinanceObservation').mockRejectedValueOnce('offline')
      .mockImplementationOnce(() => new Promise(resolve => { finish = resolve }))
    render(<FinanceObservation />)
    await screen.findByText('Finance är otillgängligt')
    fireEvent.click(screen.getByRole('button', { name: 'Försök igen' }))
    expect(screen.getByRole('alert')).toHaveTextContent('Ingen handel eller datainhämtning har startats.')
    expect(screen.getByRole('button', { name: 'Försök igen pågår' })).toBeDisabled()
    expect(screen.getByRole('status')).toHaveAttribute('aria-live', 'polite')
    expect(screen.queryByText('Hämtar Finance-status')).not.toBeInTheDocument()
    await act(async () => { finish(empty) })
    expect(screen.queryByRole('alert')).not.toBeInTheDocument()
  })

  test('refresh preserves a user-selected instrument and falls back only when it leaves the observation', async () => {
    const first = { ...empty.watchlist[0], price: 100 }
    const second = { ...first, instrumentId: 'fixture-second', symbol: 'SECOND', displayName: 'Second fixture' }
    const initial = { ...empty, watchlist: [first, second] }
    vi.spyOn(api, 'getFinanceObservation').mockResolvedValueOnce(initial)
      .mockResolvedValueOnce({ ...initial, watchlist: [second, first] })
      .mockResolvedValueOnce({ ...initial, watchlist: [first] })
    const features = vi.spyOn(api, 'getFinanceFeatures').mockRejectedValue(new Error('unavailable'))
    render(<FinanceObservation />)
    await screen.findByText('Ingen handel med riktiga pengar')
    openResearchDetails()
    await waitFor(() => expect(features).toHaveBeenCalledTimes(1))
    fireEvent.click(screen.getByRole('button', { name: /SECOND/ }))
    await waitFor(() => expect(features).toHaveBeenCalledTimes(2))
    await act(async () => { fireEvent(window, new Event('online')) })
    expect(screen.getByRole('button', { name: /SECOND/ })).toHaveAttribute('aria-pressed', 'true')
    expect(features).toHaveBeenCalledTimes(2)
    await act(async () => { fireEvent(window, new Event('online')) })
    expect(screen.getByRole('button', { name: /MSFT/ })).toHaveAttribute('aria-pressed', 'true')
    expect(features).toHaveBeenCalledTimes(3)
    expect(features.mock.calls.map(([instrument]) => instrument)).toEqual([first.instrumentId, second.instrumentId, first.instrumentId])
  })
  test('uses the shared loading primitive for the initial Finance read', () => {
    vi.spyOn(api, 'getFinanceObservation').mockImplementation(() => new Promise(() => undefined))
    const { container } = render(<FinanceObservation />)
    expect(container.querySelector('.bb-loading-indicator')).toBeInTheDocument()
    expect(screen.getByRole('status')).toHaveTextContent('Hämtar Finance-status')
    expect(container.querySelector('.finance-view')).toHaveAttribute('aria-busy', 'true')
  })

  test('persists a successful first read as bounded last-known-good display state', async () => {
    const observation = vi.spyOn(api, 'getFinanceObservation').mockResolvedValueOnce(empty)
    render(<FinanceObservation />)
    expect(await screen.findByText('Ingen handel med riktiga pengar')).toBeVisible()
    expect(JSON.parse(localStorage.getItem(FINANCE_SNAPSHOT_CACHE_KEY)!).version).toBe(1)
    expect(observation).toHaveBeenCalledTimes(1)
  })

  test('renders compatible cached Finance immediately and keeps it on refresh failure', async () => {
    writeFinanceSnapshotCache(empty, '2026-09-02T18:04:00Z')
    const observation = vi.spyOn(api, 'getFinanceObservation').mockRejectedValue(new Error('offline'))
    render(<FinanceObservation />)
    expect(screen.getAllByText('Ingen handel med riktiga pengar')[0]).toBeVisible()
    expect(screen.getByText('Visar senast hämtade data')).toBeVisible()
    await screen.findByText('Uppdatering misslyckades')
    expect(screen.queryByText('Finance är otillgängligt')).not.toBeInTheDocument()
    fireEvent.click(screen.getByRole('button', { name: 'Försök igen' }))
    await waitFor(() => expect(observation).toHaveBeenCalledTimes(2))
    expect(screen.getAllByText('Ingen handel med riktiga pengar')[0]).toBeVisible()
    expect(screen.getByText('Uppdatering misslyckades')).toBeVisible()
  })

  test('manual retry recovers in place without clearing cached content', async () => {
    writeFinanceSnapshotCache(empty, '2026-09-02T18:04:00Z')
    const observation = vi.spyOn(api, 'getFinanceObservation').mockRejectedValueOnce(new Error('offline')).mockResolvedValueOnce({ ...empty, generatedAtUtc: '2026-09-02T18:06:00Z' })
    render(<FinanceObservation />)
    await screen.findByText('Uppdatering misslyckades')
    fireEvent.click(screen.getByRole('button', { name: 'Försök igen' }))
    expect(screen.getAllByText('Ingen handel med riktiga pengar')[0]).toBeVisible()
    await waitFor(() => expect(screen.queryByText('Visar senast hämtade data')).not.toBeInTheDocument())
    expect(observation).toHaveBeenCalledTimes(2)
  })

  test('first-load failure remains honest and can be retried without navigation', async () => {
    const observation = vi.spyOn(api, 'getFinanceObservation').mockRejectedValueOnce(new Error('offline')).mockResolvedValueOnce(empty)
    render(<FinanceObservation />)
    expect(await screen.findByText('Finance är otillgängligt')).toBeVisible()
    fireEvent.click(screen.getByRole('button', { name: 'Försök igen' }))
    expect(await screen.findByText('Ingen handel med riktiga pengar')).toBeVisible()
    expect(observation).toHaveBeenCalledTimes(2)
  })

  test('does not overlap refreshes and revalidates once when connectivity returns', async () => {
    writeFinanceSnapshotCache(empty, '2026-09-02T18:04:00Z')
    let finish: ((value: FinanceObservationSnapshot) => void) | undefined
    const observation = vi.spyOn(api, 'getFinanceObservation').mockImplementation(() => new Promise(resolve => { finish = resolve }))
    render(<FinanceObservation />)
    fireEvent(window, new Event('online'))
    fireEvent(window, new Event('online'))
    expect(observation).toHaveBeenCalledTimes(1)
    expect(screen.queryByRole('button', { name: /Försök igen/ })).not.toBeInTheDocument()
    expect(screen.getAllByRole('status').filter(status => status.classList.contains('bb-loading-indicator'))).toHaveLength(1)
    finish!(empty)
    await waitFor(() => expect(screen.queryByText('Visar senast hämtade data')).not.toBeInTheDocument())
  })

  test('uses only the retry button loader during a manual retry', async () => {
    writeFinanceSnapshotCache(empty, '2026-09-02T18:04:00Z')
    let finish: ((value: FinanceObservationSnapshot) => void) | undefined
    const observation = vi.spyOn(api, 'getFinanceObservation').mockRejectedValueOnce(new Error('offline')).mockImplementationOnce(() => new Promise(resolve => { finish = resolve }))
    render(<FinanceObservation />)
    await screen.findByText('Uppdatering misslyckades')
    fireEvent.click(screen.getByRole('button', { name: 'Försök igen' }))
    const retry = screen.getByRole('button', { name: 'Försök igen pågår' })
    expect(retry).toHaveAttribute('aria-busy', 'true')
    expect(screen.getAllByRole('status').filter(status => status.classList.contains('bb-loading-indicator'))).toHaveLength(1)
    expect(screen.getAllByText('Ingen handel med riktiga pengar')[0]).toBeVisible()
    finish!(empty)
    await waitFor(() => expect(screen.queryByText('Visar senast hämtade data')).not.toBeInTheDocument())
    expect(observation).toHaveBeenCalledTimes(2)
  })

  test('keeps stale label and fetched time as wrapping-safe elements without a literal separator', async () => {
    writeFinanceSnapshotCache(empty, '2026-09-02T18:04:00Z')
    vi.spyOn(api, 'getFinanceObservation').mockRejectedValue(new Error('offline'))
    const { container } = render(<FinanceObservation />)
    await screen.findByText('Uppdatering misslyckades')
    const freshness = container.querySelector('.finance-stale-notice__freshness')!
    expect(freshness.querySelector('strong')).toHaveTextContent('Visar senast hämtade data')
    expect(freshness.querySelector('time')).toHaveAttribute('dateTime', '2026-09-02T18:04:00Z')
    expect(freshness.querySelector('time')?.textContent).toMatch(/^\d{2}:\d{2}$/)
    expect(freshness.textContent).not.toContain('·')
  })

  test('revalidates once when the visible page returns to the foreground', async () => {
    writeFinanceSnapshotCache(empty, '2026-09-02T18:04:00Z')
    const observation = vi.spyOn(api, 'getFinanceObservation').mockResolvedValue(empty)
    render(<FinanceObservation />)
    await waitFor(() => expect(observation).toHaveBeenCalledTimes(1))
    fireEvent(document, new Event('visibilitychange'))
    await waitFor(() => expect(observation).toHaveBeenCalledTimes(2))
  })

  test('defers technical detail requests until the details section opens', async () => {
    const features = vi.spyOn(api, 'getFinanceFeatures').mockRejectedValue(new Error('unavailable'))
    const backtests = vi.spyOn(api, 'getFinanceBacktests').mockRejectedValue(new Error('unavailable'))
    const otherDetails = [
      vi.spyOn(api, 'getFinanceRobustness'), vi.spyOn(api, 'getFinanceDatasets'),
      vi.spyOn(api, 'getFinanceBackups'), vi.spyOn(api, 'getFinanceShadow'),
      vi.spyOn(api, 'getFinanceResearchSchedulerStatus'), vi.spyOn(api, 'getFinanceResearchGovernorStatus'),
      vi.spyOn(api, 'getFinanceResearchOperationsStatus'),
    ]
    otherDetails.forEach(request => request.mockRejectedValue(new Error('unavailable')))
    render(<FinanceObservation initialSnapshot={{ ...empty, watchlist: [{ ...empty.watchlist[0], price: 100 }] }} />)
    expect(features).not.toHaveBeenCalled()
    expect(backtests).not.toHaveBeenCalled()
    otherDetails.forEach(request => expect(request).not.toHaveBeenCalled())
    openResearchDetails()
    await waitFor(() => expect(features).toHaveBeenCalledTimes(1))
    expect(backtests).toHaveBeenCalledTimes(1)
    otherDetails.forEach(request => expect(request).toHaveBeenCalledTimes(1))
    expect(screen.getByText('Ingen handel med riktiga pengar')).toBeVisible()
  })

  test('cold observation renders before secondary reads and refresh does not restart them', async () => {
    let resolveObservation: (value: FinanceObservationSnapshot) => void = () => {}
    vi.spyOn(api, 'getFinanceObservation').mockImplementation(() => new Promise(resolve => { resolveObservation = resolve }))
    const secondary = [
      vi.spyOn(api, 'getFinanceOverview').mockRejectedValue(new Error('unavailable')),
      vi.spyOn(api, 'getFinanceRiskStatus').mockRejectedValue(new Error('unavailable')),
      vi.spyOn(api, 'getFinanceRiskEvaluations').mockRejectedValue(new Error('unavailable')),
      vi.spyOn(api, 'getFinanceAutonomousResearch').mockRejectedValue(new Error('unavailable')),
    ]
    render(<FinanceObservation />)
    secondary.forEach(request => expect(request).not.toHaveBeenCalled())
    await act(async () => { resolveObservation(empty) })
    expect(screen.getByText('Ingen handel med riktiga pengar')).toBeVisible()
    secondary.forEach(request => expect(request).toHaveBeenCalledTimes(1))
    fireEvent(window, new Event('online'))
    await act(async () => { resolveObservation({ ...empty }) })
    secondary.forEach(request => expect(request).toHaveBeenCalledTimes(1))
  })

  test('aborted navigation is not rendered as a backend failure', () => {
    const observation = vi.spyOn(api, 'getFinanceObservation').mockImplementation(signal => new Promise((_, reject) => signal?.addEventListener('abort', () => reject(new DOMException('aborted', 'AbortError')))))
    const view = render(<FinanceObservation />)
    view.unmount()
    expect(observation).toHaveBeenCalledTimes(1)
    expect(screen.queryByText('Finance är otillgängligt')).not.toBeInTheDocument()
  })

  test('registers Finance as a navigable dashboard view', () => {
    expect(dashboardRegistry.get('finance')).toMatchObject({ title: 'Finance' })
  })
  test('renders unmistakable fail-closed research and empty states without trading controls', () => {
    render(<FinanceObservation initialSnapshot={empty} />)
    openResearchDetails()
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
    openResearchDetails()
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
    openResearchDetails()
    expect(screen.getByText(/REAL EOD-MARKET DATA/)).toBeVisible()
    expect(screen.getByText('Ägargodkänd personlig research')).toBeVisible()
    expect(screen.getAllByText('Aktiv').length).toBeGreaterThan(0)
    expect(screen.getByText(/2 observationer \/ 1 market-revisioner \/ 1 payloads \/ 42 feature-värden \/ 1 feature-revisioner/)).toBeVisible()
    expect(screen.getAllByText('Indikatorer / Features').at(-1)).toBeVisible()
    expect(screen.getByText('SMA 20')).toBeVisible()
    expect(screen.getByText('101.250000')).toBeVisible()
    expect(screen.getByText('RSI 14')).toBeVisible()
    expect(screen.getAllByText(/inga köp- eller säljsignaler/i).at(-1)).toBeVisible()
    expect(screen.queryByText(/realtid/i)).not.toBeInTheDocument()
    expect(screen.queryByRole('button', { name: /köp|sälj|order|trade/i })).not.toBeInTheDocument()
  })

  test('shows neutral insufficient out-of-sample evidence without optimization or trading controls', () => {
    const robustness: FinanceRobustnessCatalog={generatedAtUtc:'2026-08-12T08:00:00Z',operatingMode:'RESEARCH',plans:[],evaluations:[{evaluationId:'evaluation-1',checksum:'sha256:evidence',planId:'chronological-oos-walk-forward',planVersion:'v1',strategyId:'momentum',strategyVersion:'v1',verdict:'insufficientData',score:58.09,evidenceLabel:'mixedEvidence',trainSessions:176,testSessions:26,embargoSessions:50,walkForwardWindows:3,parameterVariants:3,costVariants:5,featureRevisionId:'feature-1',marketRevisionIds:['market-1'],limitations:['Engineering evidence only.']}]}
    render(<FinanceObservation initialSnapshot={empty} initialRobustness={robustness}/>)
    openResearchDetails()
    expect(screen.getAllByText('Robusthet / Out-of-sample').at(-1)).toBeVisible()
    expect(screen.getAllByText('DATA INSUFFICIENT').at(-1)).toBeVisible()
    expect(screen.getAllByText(/176 \/ 26 \/ 50 sessions/).at(-1)).toBeVisible()
    expect(screen.queryByRole('button',{name:/köp|sälj|order|trade/i})).not.toBeInTheDocument()
  })

  test('distinguishes backed-up historical memory from canonical and provider-restricted evidence',()=>{
    render(<FinanceObservation initialSnapshot={empty} initialBackups={backupFixture}/>)
    openResearchDetails()
    expect(screen.getAllByText('Dataskydd / Historiskt minne').at(-1)).toBeVisible();expect(screen.getByText('BACKED UP')).toBeVisible();expect(screen.getByText('finance-backup-fixture · 1 market-revisioner')).toBeVisible()
    expect(screen.getByText('NASDAQ-WIKI / WIKI/PRICES')).toBeVisible();expect(screen.getByText('EODHD / Free')).toBeVisible();expect(screen.getByText('SEPARAT FRÅN CANONICAL')).toBeVisible()
    expect(screen.queryByRole('button',{name:/backup|restore|radera|cleanup/i})).not.toBeInTheDocument()
  })
  test('separates prospective pending evidence from historical backtests and states sample limits',()=>{
    const shadow:FinanceShadowCatalog={generatedAtUtc:'2026-08-15T18:00:00Z',operatingMode:'RESEARCH',observationClass:'CURRENT EOD / PROSPECTIVE EOD',total:1,pending:1,evaluated:0,insufficient:0,missed:0,evidenceMaturity:'BOOTSTRAPPING',predictions:[{predictionId:'shadow-fixture',instrumentId:'US:XNAS:AAPL',symbol:'AAPL',sessionDate:'2026-08-14',provider:'EODHD',sourceRevisionId:'eodhd-fixture',observationKnowledgeUtc:'2026-08-15T17:00:00Z',knowledgeCutoffUtc:'2026-08-15T18:00:00Z',featureRevisionId:'feature-fixture',strategyId:'momentum',strategyVersion:'v1',parameterFingerprint:'sha256:fixture',signal:'TargetLong',horizon:'next-eligible-source-session-close-v1',createdAtUtc:'2026-08-15T18:00:00Z',state:'pending',operatingMode:'RESEARCH',reasonCodes:['momentum.positive']}]}
    render(<FinanceObservation initialSnapshot={empty} initialShadow={shadow}/>)
    openResearchDetails()
    expect(screen.getAllByText('Shadow research').at(-1)).toBeVisible();expect(screen.getByText('TargetLong · PENDING')).toBeVisible()
    expect(screen.getByText(/samplet bevisar pipelineintegritet, inte strategikvalitet/i)).toBeVisible()
    expect(screen.getAllByText('Backtests / Strategiforskning').at(-1)).toBeVisible();expect(screen.queryByRole('button',{name:/köp|sälj|order|trade/i})).not.toBeInTheDocument()
  })
  test('renders human overview from backend truth without fake index, portfolio, order or real-time claims',()=>{
    render(<FinanceObservation initialSnapshot={empty} initialOverview={overviewFixture} initialRiskStatus={riskStatusFixture} initialRiskEvaluations={[riskEvaluationFixture]}/>)
    expect(screen.getAllByText('Bevakade marknaden').at(-1)).toBeVisible();expect(screen.getByText(/1 av 2 bevakade instrument steg/)).toBeVisible()
    expect(screen.getByText('▲ POSITIV')).toBeVisible();expect(screen.getByText('● NEUTRAL')).toBeVisible();expect(screen.getAllByText('24').length).toBeGreaterThanOrEqual(2)
    expect(screen.getByText(/Ingen resultatgraf ännu/)).toBeVisible();expect(screen.getByText('Forskningsresultat – inga pengar handlas')).toBeVisible()
    expect(screen.getAllByText('Detaljer & forskning').at(-1)).toBeVisible();expect(screen.queryByText(/Nasdaq ↑|S&P 500|portföljvärde|faktisk P\/L|realtid/i)).not.toBeInTheDocument()
    expect(screen.queryByRole('button',{name:/köp|sälj|order|trade/i})).not.toBeInTheDocument()
    expect(screen.getAllByText('Riskbedömning saknas').length).toBe(2);expect(screen.getAllByText('research-eod-v1').length).toBeGreaterThan(0)
    expect(screen.getByText(/Godkänd betyder endast att hypotetisk research passerar policyn/)).toBeVisible()
  })
  test('matches risk only through exact shadow prediction lineage',()=>{
    const exact:FinanceOverview={...overviewFixture,signals:overviewFixture.signals.map((signal,index)=>({...signal,predictionIds:index===0?['shadow-fixture']:[]}))}
    render(<FinanceObservation initialSnapshot={empty} initialOverview={exact} initialRiskStatus={riskStatusFixture} initialRiskEvaluations={[riskEvaluationFixture]}/>)
    expect(screen.getByText('Risk: Godkänd')).toBeVisible();expect(screen.getAllByText('Riskbedömning saknas').length).toBeGreaterThan(0)
  })
  test('aggregates multiple exact risk evaluations conservatively without arbitrary first-match selection',()=>{
    const allow={...riskEvaluationFixture,evaluationId:'risk-allow',shadowPredictionId:'shadow-a',verdict:'allow' as const,evaluatedAtUtc:'2026-08-15T18:00:00Z'}
    const deny={...riskEvaluationFixture,evaluationId:'risk-deny',shadowPredictionId:'shadow-b',verdict:'deny' as const,evaluatedAtUtc:'2026-08-15T18:01:00Z'}
    expect(aggregateSignalRisk(['shadow-a','shadow-b'],[allow,deny])).toBe('Risk: Blockerad (blandad)')
    expect(aggregateSignalRisk(['shadow-a','shadow-missing'],[allow])).toBe('Risk: Godkänd (blandad)')
    expect(aggregateSignalRisk(['shadow-missing'],[allow])).toBe('Riskbedömning saknas')
  })
  test('renders autonomous research evidence conservatively with progressive details and no profitability claim',()=>{
    render(<FinanceObservation initialSnapshot={empty} initialAutonomousResearch={autonomousFixture}/>)
    openResearchDetails()
    expect(screen.getAllByText('Autonomous Research').length).toBeGreaterThan(1)
    expect(screen.getByText('1 experiment')).toBeVisible();expect(screen.getAllByText('1').length).toBeGreaterThan(1)
    expect(screen.getByText(/family-momentum-v1 · REJECTED/)).toBeVisible();expect(screen.getByText(/INTEGRITY FAIL/)).toBeVisible()
    expect(screen.getByText(/Research-only · 0 SEK · ingen execution authority/)).toBeVisible()
    expect(screen.queryByText(/vinnare|lönsam strategi|garanterad/i)).not.toBeInTheDocument()
    expect(screen.queryByRole('button',{name:/köp|sälj|order|trade/i})).not.toBeInTheDocument()
  })
  test('renders bounded scheduler status as research-only operations',()=>{
    render(<FinanceObservation initialSnapshot={empty} initialAutonomousResearch={autonomousFixture} initialResearchScheduler={schedulerFixture} initialResearchGovernor={governorFixture} initialResearchOperations={operationsFixture}/>)
    openResearchDetails()
    expect(screen.getByText('AKTIV')).toBeVisible();expect(screen.getByText('Completed')).toBeVisible();expect(screen.getByText('RESEARCH · 0 SEK · NONE')).toBeVisible()
    expect(screen.getByText('PAUSAD — SYSTEMBELASTNING')).toBeVisible();expect(screen.getByText('finance.research.scheduler.resource.memory')).toBeVisible()
    expect(screen.getByText('BEHÖVER UPPMÄRKSAMHET')).toBeVisible();expect(screen.getAllByText('3').length).toBeGreaterThan(0)
    expect(screen.queryByText(/trading active/i)).not.toBeInTheDocument()
  })
})


// BB-130C E1: BLOCKER HANDOFF — NOT MERGEABLE if the identity assertions fail.
// Synthetic API promises deliberately permit late completion after abort to test
// UI stale-response protection independently of transport cancellation.
describe('BB-130C E1 robustness selected-result identity', () => {
  const catalog: FinanceRobustnessCatalog = {
    generatedAtUtc: '2026-09-08T12:00:00Z', operatingMode: 'RESEARCH', plans: [],
    evaluations: ['A', 'B'].map(id => ({
      evaluationId: `evaluation-${id}`, checksum: `checksum-${id}`,
      strategyId: `robustness-${id}`, strategyVersion: 'v1', planId: `plan-${id}`, planVersion: 'v1',
      verdict: 'insufficientData', score: id === 'A' ? 11 : 22, evidenceLabel: `evidence-${id}`,
      trainSessions: 100, testSessions: 20, embargoSessions: 50, walkForwardWindows: 1,
      parameterVariants: 1, costVariants: 1, featureRevisionId: `feature-${id}`,
      marketRevisionIds: [`market-${id}`], limitations: [`limitations-${id}`],
    })),
  }
  const result = (id: 'A' | 'B'): FinanceRobustnessEvaluation => ({
    evaluationId: `evaluation-${id}`, checksum: `checksum-${id}`, verdict: 'insufficientData',
    verdictReasons: [], trainSessions: 100, testSessions: 20,
    primarySplit: { train: { netReturn: id === 'A' ? .11 : .22 }, test: { netReturn: .01 },
      netReturnDegradation: 0, drawdownDegradation: 0, sharpeDegradation: null, benchmarkRelativeDegradation: null },
    parameterSensitivity: { variantsEvaluated: 1, medianNetReturn: .01, minimumNetReturn: .01,
      maximumNetReturn: .01, returnStandardDeviation: 0, medianDrawdown: 0, worstDrawdown: 0,
      percentBeatingBenchmark: 0, percentPositive: 100, verdict: `parameters-${id}`, points: [] },
    costSensitivity: { points: [{ costModel: `cost-${id}`, netReturn: .01, degradation: 0,
      costBurdenOfGrossPnl: 0, trades: 1, averageHoldingSessions: 1 }],
      estimatedBreakEvenSlippageBps: null, rankingStable: true },
    walkForwardWindows: [], walkForwardPositivePercent: id === 'A' ? 11 : 22,
    score: { total: id === 'A' ? 11 : 22, label: `evidence-${id}`, components: [] }, limitations: [],
  })
  const deferred = () => {
    let resolve!: (value: FinanceRobustnessEvaluation) => void
    let reject!: (reason: Error) => void
    const promise = new Promise<FinanceRobustnessEvaluation>((yes, no) => { resolve = yes; reject = no })
    return { promise, resolve, reject }
  }
  const setup = async () => {
    // All unrelated IO fails locally; no network, provider or production data.
    vi.spyOn(globalThis, 'fetch').mockRejectedValue(new Error('synthetic unrelated read unavailable'))
    const catalogRead = vi.spyOn(api, 'getFinanceRobustness').mockResolvedValue(catalog)
    const a = deferred(), b = deferred()
    const detailRead = vi.spyOn(api, 'getFinanceRobustnessEvaluation').mockImplementation(id => {
      if (id === 'evaluation-A') return a.promise
      if (id === 'evaluation-B') return b.promise
      throw new Error('Unexpected synthetic identity')
    })
    const view = render(<FinanceObservation initialSnapshot={empty} />)
    expect(catalogRead).not.toHaveBeenCalled()
    expect(detailRead).not.toHaveBeenCalled()
    await act(async () => { setResearchOpen(true) })
    expect(detailRead).toHaveBeenCalledWith('evaluation-A', expect.any(AbortSignal))
    const panel = within(screen.getByRole('region', { name: 'Robusthet / Out-of-sample' }))
    const select = (id: 'A' | 'B') => fireEvent.click(panel.getByRole('button', { name: `robustness-${id}insufficientData` }))
    const coherent = (id: 'A' | 'B') => {
      const other = id === 'A' ? 'B' : 'A'
      expect(panel.getByText(`evaluation-${id} / checksum-${id}`)).toBeVisible()
      expect(panel.getByText(`parameters-${id}`)).toBeVisible()
      expect(panel.getByText(`cost-${id}`)).toBeVisible()
      expect(panel.getByText(`${id === 'A' ? '11' : '22'}.00 % → 1.00 %`)).toBeVisible()
      expect(panel.getByText(`${id === 'A' ? '11' : '22'}.0 % positiva benchmark-relative testfönster`)).toBeVisible()
      expect(panel.queryByText(`parameters-${other}`)).not.toBeInTheDocument()
    }
    return { ...view, a, b, detailRead, panel, select, coherent }
  }

  test('control: A and then successful B render their own evidence; unmount aborts B', async () => {
    const ui = await setup()
    await act(async () => { ui.a.resolve(result('A')) })
    ui.coherent('A')
    ui.select('B')
    expect(ui.detailRead.mock.calls[0][1]?.aborted).toBe(true)
    await act(async () => { ui.b.resolve(result('B')) })
    ui.coherent('B')
    ui.unmount()
    expect(ui.detailRead.mock.calls[1][1]?.aborted).toBe(true)
  })

  test('BLOCKER: selecting B pending must not display A evidence, including close/reopen', async () => {
    const ui = await setup()
    await act(async () => { ui.a.resolve(result('A')) })
    ui.coherent('A')
    ui.select('B')
    expect(ui.panel.getByText('evaluation-B / checksum-B')).toBeVisible()
    expect.soft(ui.panel.queryByText('parameters-A')).not.toBeInTheDocument()
    expect.soft(ui.panel.queryByText('cost-A')).not.toBeInTheDocument()
    await act(async () => { setResearchOpen(false) })
    expect(ui.detailRead.mock.calls[1][1]?.aborted).toBe(false)
    await act(async () => { setResearchOpen(true) })
    // Catalog reopens and selects A; B's pending request is now aborted.
    expect(ui.detailRead.mock.calls[1][1]?.aborted).toBe(true)
    ui.select('B')
    expect(ui.panel.getByText('evaluation-B / checksum-B')).toBeVisible()
    expect.soft(ui.panel.queryByText('parameters-A')).not.toBeInTheDocument()
    await act(async () => { ui.b.resolve(result('B')) })
    ui.coherent('B')
  })

  test('BLOCKER: late aborted A must not replace already resolved selected B', async () => {
    const ui = await setup()
    ui.select('B')
    expect(ui.detailRead.mock.calls[0][1]?.aborted).toBe(true)
    await act(async () => { ui.b.resolve(result('B')) })
    ui.coherent('B')
    await act(async () => { ui.a.resolve(result('A')) })
    expect(ui.panel.getByText('evaluation-B / checksum-B')).toBeVisible()
    expect.soft(ui.panel.queryByText('parameters-A')).not.toBeInTheDocument()
    expect.soft(ui.panel.queryByText('parameters-B')).toBeVisible()
  })

  test('control: B failure clears detail, no retry control; reselect retries the current identity', async () => {
    const ui = await setup()
    await act(async () => { ui.a.resolve(result('A')) })
    ui.select('B')
    await act(async () => { ui.b.reject(new Error('synthetic B unavailable')) })
    expect(ui.panel.getByText('evaluation-B / checksum-B')).toBeVisible()
    expect(ui.panel.queryByText('parameters-A')).not.toBeInTheDocument()
    expect(ui.panel.queryByRole('alert')).not.toBeInTheDocument()
    expect(ui.panel.queryByRole('button', { name: /Försök igen/ })).not.toBeInTheDocument()
    ui.select('B')
    expect(ui.detailRead).toHaveBeenCalledTimes(2)
    ui.select('A')
    await act(async () => {})
    ui.coherent('A')
    ui.detailRead.mockImplementationOnce(async () => result('B'))
    ui.select('B')
    await act(async () => {})
    expect(ui.detailRead).toHaveBeenLastCalledWith('evaluation-B', expect.any(AbortSignal))
    ui.coherent('B')
  })
})
