import { useState } from 'react'

export const DASHBOARD_LAYOUT_STORAGE_KEY = 'bigbrain.dashboard.layout.v1'

export const dashboardModules = [
  { id: 'meal-planner', defaultExpanded: true, collapsible: true },
  { id: 'shopping-list', defaultExpanded: true, collapsible: true },
  { id: 'media-search', defaultExpanded: true, collapsible: false },
  { id: 'quick-actions', defaultExpanded: true, collapsible: false },
  { id: 'media-jobs', defaultExpanded: true, collapsible: true },
  { id: 'media-health', defaultExpanded: false, collapsible: true },
  { id: 'insights', defaultExpanded: true, collapsible: true },
  { id: 'services', defaultExpanded: true, collapsible: true },
  { id: 'system', defaultExpanded: false, collapsible: true },
  { id: 'docker', defaultExpanded: false, collapsible: true },
  { id: 'activity', defaultExpanded: false, collapsible: true },
  { id: 'details', defaultExpanded: false, collapsible: true },
] as const

export type DashboardModuleId = typeof dashboardModules[number]['id']
export type DashboardExpandedState = Record<DashboardModuleId, boolean>

const defaults = Object.fromEntries(
  dashboardModules.map(module => [module.id, module.defaultExpanded]),
) as DashboardExpandedState

interface StoredDashboardLayout {
  version: 1
  expanded: Partial<Record<DashboardModuleId, boolean>>
}

export function readDashboardLayout(storage: Pick<Storage, 'getItem'> = window.localStorage): DashboardExpandedState {
  try {
    const raw = storage.getItem(DASHBOARD_LAYOUT_STORAGE_KEY)
    if (!raw) return { ...defaults }
    const stored = JSON.parse(raw) as Partial<StoredDashboardLayout>
    if (stored.version !== 1 || !stored.expanded || typeof stored.expanded !== 'object') return { ...defaults }

    return dashboardModules.reduce((state, module) => {
      const storedValue = stored.expanded?.[module.id]
      state[module.id] = typeof storedValue === 'boolean' ? storedValue : module.defaultExpanded
      return state
    }, {} as DashboardExpandedState)
  } catch {
    return { ...defaults }
  }
}

function writeDashboardLayout(state: DashboardExpandedState, storage: Pick<Storage, 'setItem'> = window.localStorage) {
  try {
    storage.setItem(DASHBOARD_LAYOUT_STORAGE_KEY, JSON.stringify({ version: 1, expanded: state }))
  } catch {
    // Storage can be unavailable in privacy modes. The in-memory layout remains usable.
  }
}

export function useDashboardLayout() {
  const [expanded, setExpanded] = useState<DashboardExpandedState>(() => readDashboardLayout())

  const toggle = (moduleId: DashboardModuleId) => {
    setExpanded(current => {
      const next = { ...current, [moduleId]: !current[moduleId] }
      writeDashboardLayout(next)
      return next
    })
  }

  return { expanded, toggle }
}
