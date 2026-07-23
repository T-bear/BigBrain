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

export interface MediaOverview {
  status: MediaServiceStatus['status']
  collectedAtUtc: string
  services: MediaServiceStatus[]
  qBittorrent: {
    service: MediaServiceStatus
    activeCount: number
    pausedCount: number
    completedCount: number
    downloadSpeedBytesPerSecond: number
    uploadSpeedBytesPerSecond: number
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
    healthWarnings: Array<{ source: string; message: string }>
  }
  radarr: {
    service: MediaServiceStatus
    queueCount: number
    queue: MediaQueueItem[]
    healthWarnings: Array<{ source: string; message: string }>
  }
  prowlarr: {
    service: MediaServiceStatus
    healthWarnings: Array<{ source: string; message: string }>
  }
  jellyfin: {
    service: MediaServiceStatus
    libraryCount: number
    movieCount: number
    seriesCount: number
    activeSessionCount: number
  }
}
