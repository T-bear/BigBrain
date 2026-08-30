import { createContext, useContext, useMemo, useState, type ReactNode } from 'react'
import type { AppIconName } from '../AppIcon'

export const dashboardViewIds = ['home', 'family', 'media', 'finance', 'more', 'ai', 'admin'] as const
export type DashboardViewId = typeof dashboardViewIds[number]
export type WidgetSize = 'small' | 'medium' | 'large' | 'full'

export interface WidgetRenderContext {
  expanded: boolean
}

export interface WidgetDefinition {
  id: string
  title: string
  description: string
  icon: string
  category: string
  defaultView: DashboardViewId
  defaultSize: WidgetSize
  minimumSize: WidgetSize
  supportedViews: readonly DashboardViewId[]
  permissions: readonly string[]
  render: (context: WidgetRenderContext) => ReactNode
}

export interface DashboardDefinition {
  id: DashboardViewId
  title: string
  description: string
  icon: AppIconName
}

export class ApplicationWidgetRegistry {
  private readonly definitions: readonly WidgetDefinition[]

  constructor(definitions: readonly WidgetDefinition[]) {
    const ids = new Set<string>()
    definitions.forEach(definition => {
      if (ids.has(definition.id)) throw new Error(`Duplicate widget id: ${definition.id}`)
      if (!definition.supportedViews.includes(definition.defaultView)) {
        throw new Error(`Widget ${definition.id} does not support its default view`)
      }
      ids.add(definition.id)
    })
    this.definitions = [...definitions]
  }

  getAll(): readonly WidgetDefinition[] {
    return [...this.definitions]
  }

  getForView(view: DashboardViewId): readonly WidgetDefinition[] {
    return this.definitions.filter(widget => widget.supportedViews.includes(view))
  }

  get(id: string): WidgetDefinition | undefined {
    return this.definitions.find(widget => widget.id === id)
  }
}

export class DashboardRegistry {
  private readonly definitions: readonly DashboardDefinition[]

  constructor(definitions: readonly DashboardDefinition[]) {
    this.definitions = [...definitions]
  }

  getAll(): readonly DashboardDefinition[] {
    return [...this.definitions]
  }

  get(id: DashboardViewId): DashboardDefinition {
    const dashboard = this.definitions.find(definition => definition.id === id)
    if (!dashboard) throw new Error(`Unknown dashboard: ${id}`)
    return dashboard
  }
}

export const DASHBOARD_PREFERENCES_STORAGE_KEY = 'bigbrain.dashboard.preferences.v2'

export interface WidgetPreferences {
  order: string[]
  hidden: string[]
  collapsed: string[]
}

export interface DashboardPreferences {
  version: 2
  activeView: DashboardViewId
  views: Partial<Record<DashboardViewId, WidgetPreferences>>
}

function defaults(registry: ApplicationWidgetRegistry): DashboardPreferences {
  return {
    version: 2,
    activeView: 'home',
    views: Object.fromEntries(dashboardViewIds.map(view => [view, {
      order: registry.getForView(view).filter(widget => widget.defaultView === view).map(widget => widget.id),
      hidden: registry.getForView(view).filter(widget => widget.defaultView !== view).map(widget => widget.id),
      collapsed: [],
    }])) as unknown as Record<DashboardViewId, WidgetPreferences>,
  }
}

function validView(value: unknown): value is DashboardViewId {
  return typeof value === 'string' && dashboardViewIds.includes(value as DashboardViewId)
}

export function readDashboardPreferences(
  registry: ApplicationWidgetRegistry,
  storage: Pick<Storage, 'getItem'> = window.localStorage,
): DashboardPreferences {
  const fallback = defaults(registry)
  try {
    const parsed = JSON.parse(storage.getItem(DASHBOARD_PREFERENCES_STORAGE_KEY) ?? 'null') as Partial<DashboardPreferences> | null
    if (!parsed || parsed.version !== 2 || !validView(parsed.activeView) || typeof parsed.views !== 'object') return fallback

    const views = Object.fromEntries(dashboardViewIds.map(view => {
      const available = registry.getForView(view).map(widget => widget.id)
      const stored = parsed.views?.[view]
      const known = (values: unknown) => Array.isArray(values)
        ? values.filter((value): value is string => typeof value === 'string' && available.includes(value))
        : []
      const storedOrder = known(stored?.order)
      const newDefaults = available.filter(id => !storedOrder.includes(id) && registry.get(id)?.defaultView === view)
      const hidden = known(stored?.hidden)
      const nonDefault = available.filter(id => registry.get(id)?.defaultView !== view && !storedOrder.includes(id))
      return [view, {
        order: [...storedOrder, ...newDefaults],
        hidden: [...new Set([...hidden, ...nonDefault])],
        collapsed: known(stored?.collapsed),
      }]
    })) as unknown as Record<DashboardViewId, WidgetPreferences>

    return { version: 2, activeView: parsed.activeView, views }
  } catch {
    return fallback
  }
}

interface WidgetContextValue {
  activeView: DashboardViewId
  preferences: DashboardPreferences
  registry: ApplicationWidgetRegistry
  setActiveView: (view: DashboardViewId) => void
  setVisible: (view: DashboardViewId, widgetId: string, visible: boolean) => void
  toggleCollapsed: (view: DashboardViewId, widgetId: string) => void
  moveWidget: (view: DashboardViewId, widgetId: string, direction: -1 | 1) => void
  moveWidgetTo: (view: DashboardViewId, widgetId: string, targetId: string) => void
}

const WidgetContext = createContext<WidgetContextValue | null>(null)

export function WidgetProvider({ children, registry }: { children: ReactNode; registry: ApplicationWidgetRegistry }) {
  const [preferences, setPreferences] = useState(() => {
    const stored=readDashboardPreferences(registry)
    if (window.location.pathname.startsWith('/media/audiobooks')) return {...stored,activeView:'media' as const}
    if (window.location.pathname.startsWith('/admin/')) return {...stored,activeView:'admin' as const}
    return stored
  })

  const update = (mutate: (current: DashboardPreferences) => DashboardPreferences) => {
    setPreferences(current => {
      const next = mutate(current)
      try { window.localStorage.setItem(DASHBOARD_PREFERENCES_STORAGE_KEY, JSON.stringify(next)) } catch { /* Keep in-memory state. */ }
      return next
    })
  }

  const setActiveView = (activeView: DashboardViewId) => update(current => ({ ...current, activeView }))
  const updateView = (view: DashboardViewId, mutate: (current: WidgetPreferences) => WidgetPreferences) => update(current => ({
    ...current,
    views: { ...current.views, [view]: mutate(current.views[view] ?? { order: [], hidden: [], collapsed: [] }) },
  }))

  const value = useMemo<WidgetContextValue>(() => ({
    activeView: preferences.activeView,
    preferences,
    registry,
    setActiveView,
    setVisible: (view, widgetId, visible) => updateView(view, current => ({
      ...current,
      order: current.order.includes(widgetId) ? current.order : [...current.order, widgetId],
      hidden: visible ? current.hidden.filter(id => id !== widgetId) : [...new Set([...current.hidden, widgetId])],
    })),
    toggleCollapsed: (view, widgetId) => updateView(view, current => ({
      ...current,
      collapsed: current.collapsed.includes(widgetId) ? current.collapsed.filter(id => id !== widgetId) : [...current.collapsed, widgetId],
    })),
    moveWidget: (view, widgetId, direction) => updateView(view, current => {
      const order = [...current.order]
      const index = order.indexOf(widgetId)
      const target = index + direction
      if (index < 0 || target < 0 || target >= order.length) return current
      ;[order[index], order[target]] = [order[target], order[index]]
      return { ...current, order }
    }),
    moveWidgetTo: (view, widgetId, targetId) => updateView(view, current => {
      if (widgetId === targetId) return current
      const order = current.order.filter(id => id !== widgetId)
      const target = order.indexOf(targetId)
      if (target < 0) return current
      order.splice(target, 0, widgetId)
      return { ...current, order }
    }),
  // The callbacks intentionally close over the current update function; preferences is the state dependency.
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }), [preferences, registry])

  return <WidgetContext.Provider value={value}>{children}</WidgetContext.Provider>
}

export function useWidgets() {
  const context = useContext(WidgetContext)
  if (!context) throw new Error('useWidgets must be used inside WidgetProvider')
  return context
}
