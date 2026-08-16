export interface DashboardWidgetDefinition {
  id: string
  title: string
  kind: string
  dataEndpoint: string
}

export interface ModuleDefinition {
  id: string
  name: string
  description: string
  route: string
  status: string
  dashboardWidgets: DashboardWidgetDefinition[]
  capabilities: string[]
}

export interface SystemOverview {
  hostname: string
  operatingSystem: string
  architecture: string
  uptimeSeconds: number | null
  cpu: { usagePercent: number | null; logicalProcessorCount: number }
  memory: { totalBytes: number | null; usedBytes: number | null; availableBytes: number | null; usagePercent: number | null }
  disks: Array<{ filesystemId: string; displayName: string; totalBytes: number | null; usedBytes: number | null; availableBytes: number | null; usagePercent: number | null }>
  temperatureCelsius: number | null
  collectedAtUtc: string
  status: string
  warnings: string[]
}

export interface SystemRecoverySnapshot {
  overall: 'starting' | 'recovering' | 'healthy' | 'degraded' | 'quiescing' | 'stopping' | 'recoveryRequired'
  bootId: string; bootedAtUtc: string; previousShutdown: 'unknown' | 'clean' | 'unclean'; recoveryCompleted: boolean
  clockSynchronized: boolean; clockSource: string; availableBytes: number | null; lowDisk: boolean
  lastCleanShutdownUtc: string | null; lastIntegrityCheckUtc: string | null; interruptedJobs: number; operatingMode: 'RESEARCH'
  components: Array<{ id: string; state: 'healthy' | 'degraded' | 'recovering' | 'unavailable'; critical: boolean; summary: string; checkedAtUtc: string }>
  recoveryActions: Array<{ code: string; outcome: string; atUtc: string }>
  scheduledJobs: Array<{ job: string; policy: string; reason: string }>
}

export interface DockerContainer {
  id: string
  name: string
  image: string
  state: string
  status: string
  health: string | null
  createdAtUtc: string | null
  startedAtUtc: string | null
  ports: Array<{ privatePort: number; publicPort: number | null; protocol: string }>
  cpuUsagePercent: number | null
  memoryUsageBytes: number | null
  memoryLimitBytes: number | null
  memoryUsagePercent: number | null
}

export interface DockerInventory {
  availability: { available: boolean; reason: string }
  collectedAtUtc: string
  containers: DockerContainer[]
}

export type FinanceDataKind = 'none' | 'syntheticFixture' | 'real'
export type FinanceFreshness = 'unknown' | 'current' | 'delayed' | 'stale' | 'unavailable'
export type FinanceSession = 'unknown' | 'preMarket' | 'open' | 'closed' | 'gap' | 'outage'
export type FinanceQuality = 'unknown' | 'good' | 'warning' | 'gap' | 'error'

export interface FinanceObservationSnapshot {
  generatedAtUtc: string
  safety: { mode: 'unknown' | 'research'; liveTradingEnabled: boolean; paperTradingEnabled: boolean; brokerConnected: boolean; ingestionAllowed: boolean; realProviderStorageAllowed: boolean }
  provider: { state: 'unknown' | 'noneAuthorized' | 'candidate' | 'authorized' | 'unavailable'; displayName: string; entitlement: 'unknown' | 'pendingWrittenConfirmation' | 'authorized' | 'denied' | 'expired'; entitlementGate: string; reason: string; evidenceClass?: string }
  latestMarketDataUpdateUtc: string | null
  dataKind: FinanceDataKind
  watchlist: Array<{ instrumentId: string; symbol: string; displayName: string; price: number | null; currency: string | null; dailyChangePercent: number | null; observedAtUtc: string | null; freshness: FinanceFreshness; session: FinanceSession; quality: FinanceQuality; dataKind: FinanceDataKind; history: Array<{ observedAtUtc: string; value: number | null; beginsAfterGap: boolean }> }>
  historicalMemory: { observationCount: number; activeRevisionId: string | null; parentRevisionId: string | null; coverageFrom: string | null; coverageTo: string | null; lastAcquiredAtUtc: string | null; gapCount: number; correctionCount: number; persistence: 'unknown' | 'notConfigured' | 'fixtureMemory' | 'durable'; provider: string; product: string; policy: string; provenance: string }
  retention?: { state: 'unknown' | 'active' | 'deletionRequired' | 'expiredBlocked' | 'deletionComplete'; entitlementEndsAtUtc: string | null; deletionDeadlineUtc: string | null; coveredObservationCount: number; coveredRevisionCount: number; coveredPayloadCount: number; deletionScope: string; lastReceiptId: string | null; coveredFeatureValueCount?: number; coveredFeatureRevisionCount?: number; coveredBacktestRunCount?:number; coveredBacktestEventCount?:number; coveredBacktestFillCount?:number; coveredBacktestEquityPointCount?:number; coveredRobustnessEvaluationCount?:number; coveredRobustnessWindowCount?:number; coveredRobustnessParameterPointCount?:number; coveredRobustnessCostPointCount?:number; coveredRobustnessRunReferenceCount?:number } | null
}

export type FinanceFeatureState = 'unknown' | 'available' | 'warmup' | 'unavailable'
export type FinanceFeatureQuality = 'unknown' | 'good' | 'gapAffected' | 'invalidInput'
export interface FinanceFeatureSnapshot {
  generatedAtUtc: string
  operatingMode: 'research'
  featureSetId: string
  instrumentId: string
  definitions: Array<{ id: string; name: string; version: string; kind: string; period: number; requiredInputs: string[]; requiredLookback: number; warmupBehavior: string; outputType: string; missingDataBehavior: string; gapBehavior: string; calculationMethod: string; priceBasis: string; fingerprint: string }>
  revision: null | { revisionId: string; featureSetId: string; featureSetFingerprint: string; engineVersion: string; sourceMarketRevisions: string[]; coverageFrom: string | null; coverageTo: string | null; valueCount: number; availableCount: number; warmupCount: number; qualityIssueCount: number; checksum: string; createdAtUtc: string; buildElapsedMilliseconds: number; priceBasis: string; persistence: string }
  latest: Array<{ definitionId: string; name: string; period: number; value: number | null; sessionDate: string | null; state: FinanceFeatureState; quality: FinanceFeatureQuality; knowledgeTimeUtc: string | null }>
  historyDefinitionId: string
  history: Array<{ sessionDate: string; value: number | null; state: FinanceFeatureState; quality: FinanceFeatureQuality; knowledgeTimeUtc: string }>
}

export interface FinanceBacktestRunSummary {
  runId:string; checksum:string; strategyId:string; strategyVersion:string; parameters:Record<string,number>; costModel:string; from:string; to:string;
  initialEquity:number; finalEquity:number; grossReturn:number; netReturn:number; maxDrawdown:number; trades:number; costImpact:number;
  benchmarkReturn:number|null; excessReturn:number|null; marketRevisionIds:string[]; featureRevisionId:string; simulationModel:string; sizingPolicy:string; status:string; limitations:string[]
}
export interface FinanceBacktestCatalog { generatedAtUtc:string; operatingMode:string; strategies:Array<{id:string;version:string;name:string;defaultParameters:Record<string,number>}>; runs:FinanceBacktestRunSummary[] }
export interface FinanceBacktestResult { runId:string; checksum:string; equityCurve:Array<{session:string;cash:number;holdingsValue:number;totalEquity:number;drawdown:number}>; fills:Array<unknown>; events:Array<unknown>; metrics:Record<string,number|null> }
export interface FinanceRobustnessSummary { evaluationId:string;checksum:string;planId:string;planVersion:string;strategyId:string;strategyVersion:string;verdict:string;score:number;evidenceLabel:string;trainSessions:number;testSessions:number;embargoSessions:number;walkForwardWindows:number;parameterVariants:number;costVariants:number;featureRevisionId:string;marketRevisionIds:string[];limitations:string[] }
export interface FinanceRobustnessCatalog {generatedAtUtc:string;operatingMode:string;plans:Array<unknown>;evaluations:FinanceRobustnessSummary[]}
export interface FinanceDatasetCatalog { generatedAtUtc:string; operatingMode:'RESEARCH'; datasets:Array<{candidateId:string;source:string;sourceUrl:string;hostingPlatform:string;status:string;licenseClass:string;provenanceResult:string;artifactSha256:string;artifactBytes:number;coverageFrom:string|null;coverageTo:string|null;observationCount:number;instrumentCount:number;priceBasis:string;survivorshipBias:string;validationResult:string;promotionDecision:string;canonicalRevisionId:string|null;promotedObservationCount:number;promotedSymbols:string[];limitations:string[];cleanupEligible:boolean;cleanupState:string;manifestRetained:boolean}> }
export interface FinanceBackupInventory { generatedAtUtc:string;operatingMode:'RESEARCH';backups:Array<{backupId:string;createdAtUtc:string;schemaVersion:string;bigBrainVersion:string;status:string;sources:Array<{provider:string;product:string;rightsClass:string;retentionClass:string;deletionRequirement:string;deletionDeadlineUtc:string|null;backupEligibility:string;restoreEligible:boolean;reason:string}>;revisions:Array<{revisionId:string;provider:string;product:string;policy:string;checksum:string;observationCount:number;coverageFrom:string|null;coverageTo:string|null}>;featureRevisionIds:string[];backtestRunIds:string[];robustnessEvaluationIds:string[];artifacts:Array<{path:string;bytes:number;sha256:string}>;contentFingerprint:string}>;sourcePolicies:Array<{provider:string;product:string;rightsClass:string;retentionClass:string;deletionRequirement:string;deletionDeadlineUtc:string|null;backupEligibility:string;restoreEligible:boolean;reason:string}> }
export interface FinanceShadowCatalog { generatedAtUtc:string;operatingMode:'RESEARCH';observationClass:string;predictions:Array<{predictionId:string;instrumentId:string;symbol:string;sessionDate:string;provider:string;sourceRevisionId:string;observationKnowledgeUtc:string;knowledgeCutoffUtc:string;featureRevisionId:string;strategyId:string;strategyVersion:string;parameterFingerprint:string;signal:string;horizon:string;createdAtUtc:string;state:'pending'|'evaluated'|'insufficientData'|'missedProspectiveWindow'|'invalidated';operatingMode:'RESEARCH';reasonCodes:string[]}>;total:number;pending:number;evaluated:number;insufficient:number;missed:number;evidenceMaturity:string }
export interface FinanceOverview { generatedAtUtc:string;mode:'RESEARCH';provider:string;observationClass:string;latestSession:string|null;freshness:string;tracked:number;up:number;down:number;unchanged:number;marketSummary:string;signals:Array<{instrumentId:string;symbol:string;name:string;state:'POSITIVE'|'NEUTRAL'|'NEGATIVE'|'INSUFFICIENT';sessionChangePercent:number|null;positiveStrategies:number;neutralStrategies:number;negativeStrategies:number;strategyCount:number;agreement:string;freshness:string}>;prospective:{valid:number;pending:number;evaluated:number;invalidated:number;correct:number;incorrect:number;directionalAccuracy:number|null;meanRealizedReturn:number|null;evidenceMaturity:string;curve:Array<{session:string;cumulativeReturn:number}>};cadence:{enabled:boolean;provider:string;observationClass:string;health:string;lastProviderCheckUtc:string|null;lastSuccessfulAcquisitionUtc:string|null;latestCanonicalSession:string|null;lastPredictionUtc:string|null;lastOutcomeUtc:string|null;pending:number;evaluated:number;invalidated:number;clockIntegrity:boolean;nextAction:string;pollingPolicy:string;operatingMode:'RESEARCH'};disclaimer:string;evidenceSeparation:string }
export interface FinanceRiskStatus { policyVersion:string;operatingMode:'RESEARCH';engineHealth:string;safetyState:string;activeHalt:boolean;haltScope:string;haltReason:string|null;haltedAtUtc:string|null;evaluationCount:number;lastEvaluationUtc:string|null;executionAuthority:string }
export interface FinanceRiskEvaluation { evaluationId:string;policyVersion:string;proposalId:string;instrumentId:string;strategyId:string;strategyVersion:string;parameterFingerprint:string;shadowPredictionId:string|null;sourceRevisionId:string;featureRevisionId:string;knowledgeCutoffUtc:string;evaluatedAtUtc:string;operatingMode:'RESEARCH';direction:string;researchCapital:number;requestedExposure:number;allowedExposure:number;riskAdjustedExposure:number;verdict:'allow'|'reduce'|'deny'|'halt'|'insufficientData';reasonCodes:string[];rules:Array<{ruleId:string;state:'pass'|'fail'|'notEvaluable';reasonCode:string;explanation:string;evidence:string}>;evidenceLineage:string }
export interface FinanceRobustnessEvaluation {evaluationId:string;checksum:string;verdict:string;verdictReasons:string[];trainSessions:number;testSessions:number;primarySplit:{train:Record<string,number|null>;test:Record<string,number|null>;netReturnDegradation:number;drawdownDegradation:number;sharpeDegradation:number|null;benchmarkRelativeDegradation:number|null};parameterSensitivity:{variantsEvaluated:number;medianNetReturn:number;minimumNetReturn:number;maximumNetReturn:number;returnStandardDeviation:number;medianDrawdown:number;worstDrawdown:number;percentBeatingBenchmark:number;percentPositive:number;verdict:string;points:Array<{parameters:Record<string,number>;testNetReturn:number}>};costSensitivity:{points:Array<{costModel:string;netReturn:number;degradation:number;costBurdenOfGrossPnl:number;trades:number;averageHoldingSessions:number}>;estimatedBreakEvenSlippageBps:number|null;rankingStable:boolean};walkForwardWindows:Array<{id:string;trainFrom:string;trainTo:string;testFrom:string;testTo:string}>;walkForwardPositivePercent:number;score:{total:number;label:string;components:Array<{id:string;weight:number;score:number;reason:string}>};limitations:string[]}

export interface MediaServiceStatus {
  serviceName: string
  status: 'online' | 'degraded' | 'unavailable' | 'notConfigured'
  version: string | null
  responseTimeMs: number | null
  checkedAtUtc: string
  sanitizedMessage: string | null
  isConfigured: boolean
}

export interface MediaQueueItem {
  title: string
  status: string
  progressPercent: number | null
}

export interface MediaInsight {
  severity: 'success' | 'information' | 'warning' | 'critical'
  title: string
  message: string
}

export interface MediaOverview {
  status: MediaServiceStatus['status']
  healthScore: number
  healthSummary: string
  healthStatusLevel: string
  collectedAtUtc: string
  insights: MediaInsight[]
  services: MediaServiceStatus[]
  qBittorrent: {
    service: MediaServiceStatus
    activeCount: number
    pausedCount: number
    completedCount: number
    downloadSpeedBytesPerSecond: number
    uploadSpeedBytesPerSecond: number
    etaSeconds: number | null
    averageRatio: number | null
    totalDownloadedBytes: number
    totalUploadedBytes: number
    freeSpaceBytes: number | null
    torrents: Array<{
      name: string
      progressPercent: number
      state: string
      category: string | null
      etaSeconds: number | null
    }>
  }
  sonarr: {
    service: MediaServiceStatus
    queueCount: number
    queue: MediaQueueItem[]
    seriesCount: number
    monitoredSeriesCount: number
    missingMonitoredEpisodes: number
    calendar: Array<{ title: string; airDateUtc: string | null }>
    recentHistory: Array<{ title: string; eventType: string; dateUtc: string | null }>
    healthWarnings: Array<{ source: string; message: string }>
  }
  radarr: {
    service: MediaServiceStatus
    queueCount: number
    queue: MediaQueueItem[]
    movieCount: number
    monitoredMovieCount: number
    missingMovieCount: number
    qualityUpgradeCount: number
    recentHistory: Array<{ title: string; eventType: string; dateUtc: string | null }>
    healthWarnings: Array<{ source: string; message: string }>
  }
  prowlarr: {
    service: MediaServiceStatus
    indexerCount: number
    enabledIndexerCount: number
    onlineIndexerCount: number
    rssEnabledIndexerCount: number
    indexerStatuses: string[]
    recentFailures: Array<{ title: string; eventType: string; dateUtc: string | null }>
    healthWarnings: Array<{ source: string; message: string }>
  }
  jellyfin: {
    service: MediaServiceStatus
    libraryCount: number
    movieCount: number
    seriesCount: number
    episodeCount: number
    activeUserCount: number
    activeStreamCount: number
    recentlyAdded: Array<{ name: string; mediaType: string; dateCreatedUtc: string | null }>
  }
}

export type MediaSearchProviderStatus = 'online' | 'degraded' | 'unavailable' | 'notConfigured'

export interface MealPlannerTag {
  id: string
  name: string
  category: 'portion' | 'occasion' | 'mealType' | 'custom'
  createdAtUtc: string
  isProtected: boolean
}

export interface MealPlannerMeal {
  id: string
  name: string
  tagIds: string[]
  createdAtUtc: string
  updatedAtUtc: string
}

export interface MealPlannerDay {
  date: string
  mealType: 'lunch' | 'dinner'
  dayOfWeek: string
  peopleCount: number
  mealId: string
  mealName: string
  tagSummary: string[]
  isManuallyReplaced: boolean
}

export interface MealPlannerSchedule {
  id: string
  startDate: string
  endDate: string
  createdAtUtc: string
  updatedAtUtc: string
  days: MealPlannerDay[]
  title: string | null
  generationVersion: number
}

export interface MealPlannerSeedResult {
  createdCount: number
  ignoredCount: number
}

export interface MediaSearchResult {
  sourceId: string
  title: string
  year: number | null
  mediaType: 'movie' | 'series' | 'season' | 'episode' | 'unknown'
  state: 'available' | 'monitored' | 'unmonitored' | 'missing' | 'unknown'
  posterUrl: string | null
  metadata: {
    seasonCount: number | null
    episodeCount: number | null
    episodeFileCount: number | null
    hasFile: boolean | null
    availableInLibrary: boolean | null
    imageAvailable: boolean | null
  }
}

export interface MediaSearchProviderResult {
  provider: string
  status: MediaSearchProviderStatus
  error: string | null
  results: MediaSearchResult[]
}

export interface MediaSearchResponse {
  query: string
  searchedAtUtc: string
  status: 'complete' | 'partial' | 'unavailable'
  providers: MediaSearchProviderResult[]
}

export type MediaJobStatus =
  'requested' | 'searching' | 'queued' | 'downloading' | 'stalled' |
  'completed' | 'importing' | 'available' | 'failed' | 'unknown'

export interface MediaJob {
  id: string
  mediaType: 'series' | 'movie' | 'season' | 'episode' | 'unknown'
  title: string
  subtitle: string | null
  provider: 'Sonarr' | 'Radarr' | 'qBittorrent'
  status: MediaJobStatus
  progressPercent: number | null
  sizeBytes: number | null
  downloadSpeedBytesPerSecond: number | null
  uploadSpeedBytesPerSecond: number | null
  etaSeconds: number | null
  episodeCount: number | null
  completedEpisodeCount: number | null
  requestedAt: string | null
  startedAt: string | null
  updatedAt: string
  availableAt: string | null
  errorCode: string | null
  userMessage: string | null
  playItemId: string | null
  canPlay: boolean
  artwork: string | null
  details: Array<{
    provider: string
    status: MediaJobStatus
    progressPercent: number | null
    subtitle: string | null
    userMessage: string | null
  }>
}

export interface MediaJobsResponse {
  collectedAtUtc: string
  status: 'complete' | 'degraded'
  providers: Array<{
    provider: string
    status: MediaSearchProviderStatus
    userMessage: string | null
  }>
  jobs: MediaJob[]
}

export interface MediaPlayResponse {
  jellyfinItemId: string
  title: string
  mediaType: 'series' | 'movie'
  artwork: string | null
  playUrl: string
  canPlay: boolean
}

export interface MediaLookupResult {
  provider: 'Sonarr' | 'Radarr'
  foreignId: string
  title: string
  originalTitle: string | null
  year: number | null
  overview: string | null
  network: string | null
  runtimeMinutes: number | null
  status: string | null
  mediaType: 'series' | 'movie'
  lookupState: 'external' | 'alreadyRegistered' | 'unavailable' | 'unknown'
  imageAvailable: boolean
  alreadyRegistered: boolean
  alreadyExists: boolean
  existingSourceId: string | null
  providerId: string
  posterUrl: string | null
  monitored: boolean | null
  canRequest: boolean
  requestState: string | null
  errorCode: string | null
  errorMessage: string | null
}

export interface MediaLookupResponse {
  query: string
  mediaType: 'series' | 'movie' | 'all'
  lookedUpAtUtc: string
  status: 'complete' | 'partial' | 'unavailable'
  requestsEnabled: boolean
  providers: Array<{
    provider: string
    status: MediaSearchProviderStatus
    error: string | null
    errorCode: string | null
    results: MediaLookupResult[]
  }>
}

export interface MediaServiceLink {
  id: 'jellyfin' | 'radarr' | 'sonarr' | 'prowlarr' | 'qbittorrent'
  displayName: string
  url: string
  enabled: boolean
}

export interface MediaAddOption {
  id: string
  displayName: string
  freeSpaceBytes: number | null
}

export interface MediaAddOptionsResponse {
  provider: string
  mediaType: 'series' | 'movie'
  requestsEnabled: boolean
  rootFolders: MediaAddOption[]
  qualityProfiles: MediaAddOption[]
  monitoringOptions: MediaAddOption[]
  seriesTypes: MediaAddOption[]
  defaultRootFolderId: string | null
  defaultQualityProfileId: string | null
  defaultMonitoringOptionId: string
  defaultSeriesTypeId: string | null
  defaultSearchAfterAdd: boolean
}

export interface MediaRequestPreviewResponse {
  requestToken: string
  expiresAtUtc: string
  status: 'previewReady'
  summary: {
    title: string
    year: number | null
    provider: string
    mediaType: 'series' | 'movie'
    rootFolder: string
    qualityProfile: string
    monitoring: string
    seriesType: string | null
    searchAfterAdd: boolean
  }
}

export interface MediaRequestConfirmResponse {
  status: 'created' | 'alreadyExists'
  provider: string
  mediaType: 'series' | 'movie'
  sourceId: string
  title: string
}

export interface SmartShuffleSeries { id: string; name: string; hasPlayableEpisode: boolean }
export interface SmartShuffleDevice { id: string; displayName: string; clientType: string; available: boolean; isPlaying: boolean }
export interface SmartShuffleEpisode { id: string; seriesId: string; seriesName: string; title: string; seasonNumber: number; episodeNumber: number; playbackPositionTicks: number | null }
export interface SmartShuffleOptions { enabled: boolean; series: SmartShuffleSeries[] }
export interface SmartShuffleSession {
  id: string
  status: 'starting' | 'awaitingPlaybackConfirmation' | 'active' | 'failed' | 'stopped' | 'completed'
  nowPlaying: SmartShuffleEpisode | null
  recentSeries: string[]
  remainingSeries: number
  deviceName: string
  startedAtUtc: string
  errorCode: string | null
}

export type DownloadStatus = 'active' | 'queued' | 'paused' | 'error' | 'completed' | 'unknown'
export type DownloadOperation = 'pause' | 'resume' | 'retry'
export interface DownloadCapabilities { canPause: boolean; canResume: boolean; canRetry: boolean; canRemove: boolean }
export interface DownloadDiagnosis { code: string; severity: 'info' | 'warning' | 'error'; explanation: string; verifiedObservations: string[]; availableSafeActions: string[] }
export interface DownloadSummary {
  id: string
  name: string
  status: DownloadStatus
  progressPercent: number
  sizeBytes: number
  downloadedBytes: number
  downloadSpeedBytesPerSecond: number
  uploadSpeedBytesPerSecond: number
  queuePosition: number | null
  category: string
  ownership: 'sonarr' | 'radarr' | 'manual' | 'unknown'
  importStatus: 'notImported' | 'unknown'
  destructiveRemovalAllowed: boolean
  warnings: string[]
  capabilities: DownloadCapabilities
  diagnosis: DownloadDiagnosis
}
export interface DownloadsResponse { collectedAtUtc: string; downloads: DownloadSummary[] }
export interface DownloadRemovalPreview {
  confirmationToken: string
  expiresAtUtc: string
  name: string
  status: DownloadStatus
  category: string
  ownership: DownloadSummary['ownership']
  downloadedBytes: number
  filesWillBePreserved: boolean
  destructiveRemovalAllowed: boolean
  warnings: string[]
}
export interface DownloadRemovalResult {
  status: 'removed' | 'alreadyMissing'
  removed: boolean
  dataPreserved: boolean
  alreadyMissing: boolean
  ownership: DownloadSummary['ownership']
  errorCode: string | null
}
export interface DownloadOperationResult { id: string; operation: DownloadOperation; status: 'succeeded' | 'alreadyInDesiredState'; download: DownloadSummary | null }
export type DownloadBatchStatus = 'succeeded' | 'alreadyInDesiredState' | 'notFound' | 'identityChanged' | 'operationNotAllowed' | 'providerUnavailable' | 'providerTimeout' | 'rejected'
export interface DownloadBatchResult { operation: DownloadOperation; partial: true; results: Array<{ id: string; status: DownloadBatchStatus; download: DownloadSummary | null }> }
