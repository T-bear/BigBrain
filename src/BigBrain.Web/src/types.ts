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
  dashboardWidgets: DashboardWidgetDefinition[]
  capabilities: string[]
}

export interface SystemHealth {
  status: string
  checkedAtUtc: string
}

