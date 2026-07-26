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
  ModuleDefinition,
  SystemOverview,
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

export const getModules = (signal?: AbortSignal) =>
  getJson<ModuleDefinition[]>('/api/v1/modules', signal)

export const getSystemOverview = (signal?: AbortSignal) =>
  getJson<SystemOverview>('/api/v1/system/overview', signal)

export const getDockerContainers = (signal?: AbortSignal) =>
  getJson<DockerInventory>('/api/v1/docker/containers', signal)

export const getMediaOverview = (signal?: AbortSignal) =>
  getJson<MediaOverview>('/api/v1/modules/media', signal)

export const getMediaServiceLinks = (signal?: AbortSignal) =>
  getJson<MediaServiceLink[]>('/api/v1/modules/media/service-links', signal)

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
