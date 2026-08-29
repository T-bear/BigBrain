import type {
  DockerInventory,
  MediaAddOptionsResponse,
  MediaLookupResponse,
  MediaJobsResponse,
  MediaOverview,
  MediaPlayResponse,
  MediaRequestConfirmResponse,
  MediaRequestPreviewResponse,
  MediaSearchResponse,
  MediaServiceLink,
  MealPlannerDay,
  MealPlannerMeal,
  MealPlannerSchedule,
  MealPlannerSeedResult,
  MealPlannerTag,
  ModuleDefinition,
  SystemOverview,
  SystemRecoverySnapshot,
  SmartShuffleDevice,
  SmartShuffleOptions,
  SmartShuffleSession,
  DownloadsResponse,
  DownloadSummary,
  DownloadRemovalPreview,
  DownloadRemovalResult,
  DownloadOperation,
  DownloadOperationResult,
  DownloadBatchResult,
  FinanceObservationSnapshot,
  FinanceDatasetCatalog,
  FinanceBackupInventory,
  FinanceFeatureSnapshot,
  FinanceBacktestCatalog,
  FinanceBacktestResult,
  FinanceRobustnessCatalog,
  FinanceRobustnessEvaluation,
  FinanceShadowCatalog,
  FinanceOverview,
  FinanceRiskStatus,
  FinanceRiskEvaluation,
  FinanceAutonomousResearch,
  FinanceResearchSchedulerStatus,
  FinanceResearchResourceDecision,
  FinanceResearchOperationsStatus,
  AudiobookOverview,
  AudiobookSearchResponse,
} from './types'

export class ApiError extends Error {
  public constructor(public readonly code: string, message: string) {
    super(message)
  }
}

async function getJson<T>(url: string, signal?: AbortSignal): Promise<T> {
  const response = await fetch(url, { signal })

  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { code?: string; detail?: string } | null
    throw new ApiError(problem?.code ?? 'requestFailed', problem?.detail ?? 'The request could not be completed.')
  }

  return response.json() as Promise<T>
}

async function sendJson<T>(url: string, method: 'POST' | 'PUT', body: unknown): Promise<T> {
  const response = await fetch(url, {
    method,
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { code?: string; detail?: string } | null
    throw new ApiError(problem?.code ?? 'requestFailed', problem?.detail ?? 'The request could not be completed.')
  }
  return response.json() as Promise<T>
}

async function deleteRequest(url: string): Promise<void> {
  const response = await fetch(url, { method: 'DELETE' })
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { code?: string; detail?: string } | null
    throw new ApiError(problem?.code ?? 'requestFailed', problem?.detail ?? 'The request could not be completed.')
  }
}

export const getModules = (signal?: AbortSignal) =>
  getJson<ModuleDefinition[]>('/api/v1/modules', signal)

export const getSystemOverview = (signal?: AbortSignal) =>
  getJson<SystemOverview>('/api/v1/system/overview', signal)

export const getDockerContainers = (signal?: AbortSignal) =>
  getJson<DockerInventory>('/api/v1/docker/containers', signal)

export const getFinanceObservation = (signal?: AbortSignal) =>
  getJson<FinanceObservationSnapshot>('/api/v1/modules/finance/observation', signal)
export const getFinanceDatasets = (signal?: AbortSignal) => getJson<FinanceDatasetCatalog>('/api/v1/modules/finance/datasets',signal)
export const getFinanceBackups = (signal?: AbortSignal) => getJson<FinanceBackupInventory>('/api/v1/modules/finance/backups',signal)
export const getFinanceShadow = (signal?: AbortSignal) => getJson<FinanceShadowCatalog>('/api/v1/modules/finance/shadow/scorecard',signal)
export const getFinanceOverview = (signal?: AbortSignal) => getJson<FinanceOverview>('/api/v1/modules/finance/overview',signal)
export const getFinanceAutonomousResearch = (signal?:AbortSignal)=>getJson<FinanceAutonomousResearch>('/api/v1/modules/finance/research/autonomous',signal)
export const getFinanceResearchSchedulerStatus = (signal?:AbortSignal)=>getJson<FinanceResearchSchedulerStatus>('/api/v1/modules/finance/research/scheduler/status',signal)
export const getFinanceResearchGovernorStatus = (signal?:AbortSignal)=>getJson<FinanceResearchResourceDecision>('/api/v1/modules/finance/research/governor/status',signal)
export const getFinanceResearchOperationsStatus = (signal?:AbortSignal)=>getJson<FinanceResearchOperationsStatus>('/api/v1/modules/finance/research/operations/status',signal)
export const getFinanceRiskStatus = (signal?: AbortSignal) => getJson<FinanceRiskStatus>('/api/v1/modules/finance/risk/status',signal)
export const getFinanceRiskEvaluations = (signal?: AbortSignal) => getJson<FinanceRiskEvaluation[]>('/api/v1/modules/finance/risk/evaluations?limit=50',signal)

export const getSystemRecovery = (signal?: AbortSignal) => getJson<SystemRecoverySnapshot>('/api/v1/system/recovery', signal)

export const getFinanceFeatures = (instrumentId: string, signal?: AbortSignal) =>
  getJson<FinanceFeatureSnapshot>(`/api/v1/modules/finance/features?instrumentId=${encodeURIComponent(instrumentId)}&featureId=sma.20&limit=260`, signal)
export const getFinanceBacktests = (signal?: AbortSignal) => getJson<FinanceBacktestCatalog>('/api/v1/modules/finance/backtests', signal)
export const getFinanceBacktest = (runId:string, signal?:AbortSignal) => getJson<FinanceBacktestResult>(`/api/v1/modules/finance/backtests/${encodeURIComponent(runId)}`,signal)
export const getFinanceRobustness = (signal?:AbortSignal)=>getJson<FinanceRobustnessCatalog>('/api/v1/modules/finance/robustness',signal)
export const getFinanceRobustnessEvaluation = (id:string,signal?:AbortSignal)=>getJson<FinanceRobustnessEvaluation>(`/api/v1/modules/finance/robustness/${encodeURIComponent(id)}`,signal)

export const getMediaOverview = (signal?: AbortSignal) =>
  getJson<MediaOverview>('/api/v1/modules/media', signal)

export const getMediaServiceLinks = (signal?: AbortSignal) =>
  getJson<MediaServiceLink[]>('/api/v1/modules/media/service-links', signal)

export const getAudiobookOverview = (signal?: AbortSignal) => getJson<AudiobookOverview>('/api/v1/modules/media/audiobooks/overview',signal)
export const getAudiobookLibrary = (page=0,limit=24,query='',language='',signal?:AbortSignal) => getJson<import('./types').AudiobookLibraryPage>(`/api/v1/modules/media/audiobooks/library?page=${page}&limit=${limit}&query=${encodeURIComponent(query.trim())}&language=${encodeURIComponent(language)}`,signal)
export const getAudiobook = (id:string,signal?:AbortSignal) => getJson<import('./types').AudiobookItem>(`/api/v1/modules/media/audiobooks/${encodeURIComponent(id)}`,signal)
export const getAudiobookPlaybackAvailability = (signal?:AbortSignal) => getJson<import('./types').AudiobookPlaybackAvailability>('/api/v1/modules/media/audiobooks/playback/availability',signal)
export const startAudiobookPlayback = (id:string,signal?:AbortSignal) => postJson<import('./types').AudiobookPlaybackSession>(`/api/v1/modules/media/audiobooks/${encodeURIComponent(id)}/playback`,{},signal)
export const syncAudiobookPlayback = async (id:string,currentTime:number,duration:number,timeListened:number,close=false,signal?:AbortSignal) => {
  const response=await fetch(`/api/v1/modules/media/audiobooks/playback/sessions/${encodeURIComponent(id)}/${close?'close':'sync'}`,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({currentTime,duration,timeListened}),signal,keepalive:close})
  if(!response.ok) throw new Error('Playback sync failed')
}
export const searchAudiobooks = (query:string,language:string,signal?:AbortSignal) => getJson<AudiobookSearchResponse>(`/api/v1/modules/media/audiobooks/search?query=${encodeURIComponent(query.trim())}&language=${encodeURIComponent(language)}`,signal)
export const getAudiobookAcquisitionStatus = (signal?:AbortSignal) => getJson<import('./types').AudiobookAcquisitionProviderStatus>('/api/v1/modules/media/audiobooks/acquisition/provider-status',signal)
export const getAudiobookAcquisitionJobs = (signal?:AbortSignal,offset=0,limit=25) => getJson<import('./types').AudiobookAcquisitionJobPage>(`/api/v1/modules/media/audiobooks/acquisition/jobs?offset=${offset}&limit=${limit}`,signal)
export const getAudiobookAcquisitionJob = (id:string,signal?:AbortSignal) => getJson<import('./types').AudiobookAcquisitionJob>(`/api/v1/modules/media/audiobooks/acquisition/jobs/${encodeURIComponent(id)}`,signal)
export const requestAudiobookAcquisition = (candidate:import('./types').AudiobookAcquisitionCandidate) => sendJson<import('./types').AudiobookAcquisitionJob>('/api/v1/modules/media/audiobooks/acquisition/jobs','POST',candidate)
export const getAudiobookLanguages = (signal?:AbortSignal) => getJson<{preferredLanguage:string;fallbackLanguage:string}>('/api/v1/settings/audiobooks/languages',signal)
export const setAudiobookLanguages = (preferredLanguage:string,fallbackLanguage:string) => sendJson<{preferredLanguage:string;fallbackLanguage:string}>('/api/v1/settings/audiobooks/languages','PUT',{preferredLanguage,fallbackLanguage})

const mealPlannerBase = '/api/v1/modules/meal-planner'
export const getMealPlannerMeals = (tagIds: string[] = []) =>
  getJson<MealPlannerMeal[]>(`${mealPlannerBase}/meals${tagIds.length ? `?tags=${encodeURIComponent(tagIds.join(','))}` : ''}`)
export const createMealPlannerMeal = (name: string, tagIds: string[]) =>
  sendJson<MealPlannerMeal>(`${mealPlannerBase}/meals`, 'POST', { name, tagIds })
export const updateMealPlannerMeal = (id: string, name: string, tagIds: string[]) =>
  sendJson<MealPlannerMeal>(`${mealPlannerBase}/meals/${encodeURIComponent(id)}`, 'PUT', { name, tagIds })
export const deleteMealPlannerMeal = (id: string) => deleteRequest(`${mealPlannerBase}/meals/${encodeURIComponent(id)}`)
export const seedMealPlannerExamples = () =>
  sendJson<MealPlannerSeedResult>(`${mealPlannerBase}/meals/seed-examples`, 'POST', {})
export const getMealPlannerTags = () => getJson<MealPlannerTag[]>(`${mealPlannerBase}/tags`)
export const createMealPlannerTag = (name: string, category: MealPlannerTag['category']) =>
  sendJson<MealPlannerTag>(`${mealPlannerBase}/tags`, 'POST', { name, category })
export const deleteMealPlannerTag = (id: string) => deleteRequest(`${mealPlannerBase}/tags/${encodeURIComponent(id)}`)
export const getMealPlannerSchedules = () => getJson<MealPlannerSchedule[]>(`${mealPlannerBase}/schedules`)
export const generateMealPlannerSchedule = (startDate: string, weekCount: number, title: string) =>
  sendJson<MealPlannerSchedule>(`${mealPlannerBase}/schedules/generate`, 'POST', { startDate, weekCount, title: title || null, seed: 0 })
export const replaceMealPlannerDay = (scheduleId: string, date: string, mealType: MealPlannerDay['mealType']) =>
  sendJson<MealPlannerSchedule>(`${mealPlannerBase}/schedules/${encodeURIComponent(scheduleId)}/days/${date}/${mealType}/replace`, 'PUT', { seed: 0 })
export const setMealPlannerDay = (scheduleId: string, date: string, mealType: MealPlannerDay['mealType'], mealId: string) =>
  sendJson<MealPlannerSchedule>(`${mealPlannerBase}/schedules/${encodeURIComponent(scheduleId)}/days/${date}/${mealType}/meal`, 'PUT', { mealId })
export const deleteMealPlannerSchedule = (id: string) => deleteRequest(`${mealPlannerBase}/schedules/${encodeURIComponent(id)}`)

export const getMediaJobs = (signal?: AbortSignal) =>
  getJson<MediaJobsResponse>('/api/v1/modules/media/jobs?limit=50', signal)

export const getMediaJob = (id: string, signal?: AbortSignal) =>
  getJson<MediaJobsResponse['jobs'][number]>(
    `/api/v1/modules/media/jobs/${encodeURIComponent(id)}`,
    signal)

export const getMediaPlay = (itemId: string, signal?: AbortSignal) =>
  getJson<MediaPlayResponse>(`/api/v1/modules/media/play/${encodeURIComponent(itemId)}`, signal)

export function subscribeMediaJobs(
  onJobs: (jobs: MediaJobsResponse) => void,
  onError: () => void,
) {
  if (typeof EventSource === 'undefined') return () => undefined
  let source: EventSource | null = null
  let retryTimer: ReturnType<typeof setTimeout> | null = null
  let retryDelayMs = 5_000
  let stopped = false

  function connect() {
    if (stopped) return
    source = new EventSource('/api/v1/modules/media/jobs/events')
    source.addEventListener('jobs', event => {
      try {
        onJobs(JSON.parse((event as MessageEvent<string>).data) as MediaJobsResponse)
        retryDelayMs = 5_000
      } catch {
        onError()
      }
    })
    source.onerror = () => {
      source?.close()
      source = null
      onError()
      if (!stopped && retryTimer === null) {
        retryTimer = setTimeout(() => {
          retryTimer = null
          connect()
        }, retryDelayMs)
        retryDelayMs = Math.min(retryDelayMs * 2, 30_000)
      }
    }
  }

  connect()
  return () => {
    stopped = true
    source?.close()
    if (retryTimer !== null) clearTimeout(retryTimer)
  }
}

export const searchMedia = (query: string, signal?: AbortSignal) =>
  getJson<MediaSearchResponse>(`/api/v1/modules/media/search?query=${encodeURIComponent(query.trim())}`, signal)

export const lookupMedia = (query: string, mediaType = 'all', signal?: AbortSignal) =>
  getJson<MediaLookupResponse>(
    `/api/v1/modules/media/lookup?query=${encodeURIComponent(query.trim())}&mediaType=${encodeURIComponent(mediaType)}`,
    signal)

export function mediaErrorMessage(code: string | null | undefined) {
  switch (code) {
    case 'timeout': return 'Tjänsten svarade inte i tid. Försök igen.'
    case 'authenticationFailure':
    case 'providerConfigurationInvalid': return 'Tjänstens autentisering misslyckades.'
    case 'providerUnavailable': return 'Tjänsten är inte tillgänglig just nu.'
    case 'validationError': return 'Kontrollera de angivna uppgifterna.'
    case 'rootFolderUnavailable':
    case 'invalidRootFolder': return 'Den valda rotmappen är inte längre tillgänglig.'
    case 'alreadyExists':
    case 'alreadyRegistered': return 'Titeln är redan tillagd.'
    default: return 'Åtgärden kunde inte slutföras. Försök igen.'
  }
}

export const getMediaAddOptions = (mediaType: 'series' | 'movie', signal?: AbortSignal) =>
  getJson<MediaAddOptionsResponse>(`/api/v1/modules/media/add-options/${mediaType}`, signal)

async function postJson<T>(url: string, body: unknown, signal?: AbortSignal): Promise<T> {
  const response = await fetch(url, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
    signal,
  })
  if (!response.ok) {
    const problem = await response.json().catch(() => null) as { code?: string; detail?: string } | null
    throw new ApiError(problem?.code ?? 'requestFailed', problem?.detail ?? 'The request could not be completed.')
  }
  return response.json() as Promise<T>
}

export const previewMediaRequest = (body: unknown, signal?: AbortSignal) =>
  postJson<MediaRequestPreviewResponse>('/api/v1/modules/media/requests/preview', body, signal)

export function createIdempotencyKey() {
  if (typeof crypto.randomUUID === 'function') return crypto.randomUUID()
  const bytes = crypto.getRandomValues(new Uint8Array(16))
  bytes[6] = (bytes[6] & 0x0f) | 0x40
  bytes[8] = (bytes[8] & 0x3f) | 0x80
  const hex = Array.from(bytes, byte => byte.toString(16).padStart(2, '0')).join('')
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`
}

export const confirmMediaRequest = (requestToken: string, idempotencyKey: string, signal?: AbortSignal) =>
  postJson<MediaRequestConfirmResponse>(
    '/api/v1/modules/media/requests/confirm',
    { requestToken, idempotencyKey },
    signal)

const smartShuffleBase = '/api/v1/modules/media/smart-shuffle'
export const getSmartShuffleOptions = (signal?: AbortSignal) => getJson<SmartShuffleOptions>(`${smartShuffleBase}/options`, signal)
export const getSmartShuffleDevices = (signal?: AbortSignal) => getJson<SmartShuffleDevice[]>(`${smartShuffleBase}/devices`, signal)
export const getSmartShuffleSession = (id: string, signal?: AbortSignal) => getJson<SmartShuffleSession>(`${smartShuffleBase}/sessions/${encodeURIComponent(id)}`, signal)
export const createSmartShuffleSession = (seriesIds: string[], deviceId: string, signal?: AbortSignal) =>
  postJson<SmartShuffleSession>(`${smartShuffleBase}/sessions`, { seriesIds, deviceId }, signal)
export const skipSmartShuffle = (id: string, signal?: AbortSignal) =>
  postJson<SmartShuffleSession>(`${smartShuffleBase}/sessions/${encodeURIComponent(id)}/skip`, {}, signal)
export const stopSmartShuffle = (id: string, signal?: AbortSignal) =>
  postJson<SmartShuffleSession>(`${smartShuffleBase}/sessions/${encodeURIComponent(id)}/stop`, {}, signal)

const downloadsBase = '/api/v1/modules/media/downloads'
export const getDownloads = (signal?: AbortSignal) => getJson<DownloadsResponse>(downloadsBase, signal)
export const getDownload = (id: string, signal?: AbortSignal) =>
  getJson<DownloadSummary>(`${downloadsBase}/${encodeURIComponent(id)}`, signal)
export const previewDownloadRemoval = (id: string, deleteData: boolean, signal?: AbortSignal) =>
  postJson<DownloadRemovalPreview>(`${downloadsBase}/${encodeURIComponent(id)}/remove-preview`, { deleteData }, signal)
export const removeDownload = (id: string, confirmationToken: string, deleteData: boolean, signal?: AbortSignal) =>
  postJson<DownloadRemovalResult>(`${downloadsBase}/${encodeURIComponent(id)}/remove`, { confirmationToken, deleteData }, signal)
export const operateDownload = (id: string, operation: DownloadOperation, signal?: AbortSignal) =>
  postJson<DownloadOperationResult>(`${downloadsBase}/${encodeURIComponent(id)}/actions/${operation}`, {}, signal)
export const operateDownloadsBatch = (ids: string[], operation: DownloadOperation, signal?: AbortSignal) =>
  postJson<DownloadBatchResult>(`${downloadsBase}/actions/batch`, { ids, operation }, signal)
