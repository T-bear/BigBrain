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
