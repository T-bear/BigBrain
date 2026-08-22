import { render, screen, within } from '@testing-library/react'
import { describe, expect, test } from 'vitest'
import { aggregateSignalRisk, FinanceObservation } from './FinanceObservation'
import type { FinanceAutonomousResearch, FinanceBackupInventory, FinanceFeatureSnapshot, FinanceObservationSnapshot, FinanceOverview, FinanceResearchSchedulerStatus, FinanceRiskEvaluation, FinanceRiskStatus, FinanceRobustnessCatalog, FinanceShadowCatalog } from '../types'
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
const schedulerFixture:FinanceResearchSchedulerStatus={currentUtc:'2026-08-23T01:00:00Z',enabled:true,schedulerVersion:'finance-research-scheduler-v1',nextDueUtc:'2026-08-23T02:00:00Z',lastOpportunity:{opportunityId:'finance-research-scheduler-v1:2026-08-22',researchDate:'2026-08-22',dueAtUtc:'2026-08-23T02:00:00Z',attemptedAtUtc:'2026-08-23T02:03:00Z',completedAtUtc:'2026-08-23T02:04:00Z',state:'Completed',researchRunId:'research-run-fixture',reason:'finance.research.scheduler.completed',nextEligibilityUtc:null},lastResearchRunId:'research-run-fixture',lastOutcome:'Completed',lastReason:'finance.research.scheduler.completed',researchCurrentlyRunning:false,operatingMode:'RESEARCH',budgetSek:0,executionAuthority:'NONE'}

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
    expect(screen.getAllByText('Robusthet / Out-of-sample').at(-1)).toBeVisible()
    expect(screen.getAllByText('DATA INSUFFICIENT').at(-1)).toBeVisible()
    expect(screen.getAllByText(/176 \/ 26 \/ 50 sessions/).at(-1)).toBeVisible()
    expect(screen.queryByRole('button',{name:/köp|sälj|order|trade/i})).not.toBeInTheDocument()
  })

  test('distinguishes backed-up historical memory from canonical and provider-restricted evidence',()=>{
    render(<FinanceObservation initialSnapshot={empty} initialBackups={backupFixture}/>)
    expect(screen.getAllByText('Dataskydd / Historiskt minne').at(-1)).toBeVisible();expect(screen.getByText('BACKED UP')).toBeVisible();expect(screen.getByText('finance-backup-fixture · 1 market-revisioner')).toBeVisible()
    expect(screen.getByText('NASDAQ-WIKI / WIKI/PRICES')).toBeVisible();expect(screen.getByText('EODHD / Free')).toBeVisible();expect(screen.getByText('SEPARAT FRÅN CANONICAL')).toBeVisible()
    expect(screen.queryByRole('button',{name:/backup|restore|radera|cleanup/i})).not.toBeInTheDocument()
  })
  test('separates prospective pending evidence from historical backtests and states sample limits',()=>{
    const shadow:FinanceShadowCatalog={generatedAtUtc:'2026-08-15T18:00:00Z',operatingMode:'RESEARCH',observationClass:'CURRENT EOD / PROSPECTIVE EOD',total:1,pending:1,evaluated:0,insufficient:0,missed:0,evidenceMaturity:'BOOTSTRAPPING',predictions:[{predictionId:'shadow-fixture',instrumentId:'US:XNAS:AAPL',symbol:'AAPL',sessionDate:'2026-08-14',provider:'EODHD',sourceRevisionId:'eodhd-fixture',observationKnowledgeUtc:'2026-08-15T17:00:00Z',knowledgeCutoffUtc:'2026-08-15T18:00:00Z',featureRevisionId:'feature-fixture',strategyId:'momentum',strategyVersion:'v1',parameterFingerprint:'sha256:fixture',signal:'TargetLong',horizon:'next-eligible-source-session-close-v1',createdAtUtc:'2026-08-15T18:00:00Z',state:'pending',operatingMode:'RESEARCH',reasonCodes:['momentum.positive']}]}
    render(<FinanceObservation initialSnapshot={empty} initialShadow={shadow}/>)
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
    expect(screen.getAllByText('Autonomous Research').length).toBeGreaterThan(1)
    expect(screen.getByText('1 experiment')).toBeVisible();expect(screen.getAllByText('1').length).toBeGreaterThan(1)
    expect(screen.getByText(/family-momentum-v1 · REJECTED/)).toBeVisible();expect(screen.getByText(/INTEGRITY FAIL/)).toBeVisible()
    expect(screen.getByText(/Research-only · 0 SEK · ingen execution authority/)).toBeVisible()
    expect(screen.queryByText(/vinnare|lönsam strategi|garanterad/i)).not.toBeInTheDocument()
    expect(screen.queryByRole('button',{name:/köp|sälj|order|trade/i})).not.toBeInTheDocument()
  })
  test('renders bounded scheduler status as research-only operations',()=>{
    render(<FinanceObservation initialSnapshot={empty} initialAutonomousResearch={autonomousFixture} initialResearchScheduler={schedulerFixture}/>)
    expect(screen.getByText('AKTIV')).toBeVisible();expect(screen.getByText('Completed')).toBeVisible();expect(screen.getByText('RESEARCH · 0 SEK · NONE')).toBeVisible()
    expect(screen.queryByText(/trading active/i)).not.toBeInTheDocument()
  })
})
