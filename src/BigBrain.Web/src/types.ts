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
  disks: Array<{ mountPoint: string; totalBytes: number | null; usedBytes: number | null; availableBytes: number | null; usagePercent: number | null }>
  temperatureCelsius: number | null
  collectedAtUtc: string
  status: string
  warnings: string[]
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
  existingSourceId: string | null
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
    results: MediaLookupResult[]
  }>
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
