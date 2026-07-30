import { CollapsibleModule } from './CollapsibleModule'
import type { DashboardExpandedState, DashboardModuleId } from './dashboardLayout'
import type { WidgetRegistry } from './WidgetRegistry'

export function DashboardSections<TData>({
  data,
  expanded,
  onToggle,
  state,
  registry,
  sectionIds,
}: {
  data: TData
  expanded?: DashboardExpandedState
  onToggle?: (moduleId: DashboardModuleId) => void
  state: string
  registry: WidgetRegistry<TData>
  sectionIds?: readonly DashboardModuleId[]
}) {
  return registry.getSections().filter(section => !sectionIds || sectionIds.includes(section.id as DashboardModuleId)).map(section => {
    const widgets = registry.getWidgets(section.id, state)
    if (widgets.length === 0) return null
    const moduleId = section.id as DashboardModuleId

    return (
      <CollapsibleModule
        className="media-dashboard-module"
        eyebrow={section.label}
        expanded={expanded?.[moduleId] ?? section.defaultExpanded ?? false}
        headingLevel={3}
        key={section.id}
        moduleId={moduleId}
        onToggle={() => onToggle?.(moduleId)}
        title={section.title ?? section.id}
      >
        <div className={section.className} data-dashboard-section={section.id}>
          {widgets.map(widget => {
            const Component = widget.component
            return <Component data={data} key={widget.id} />
          })}
        </div>
      </CollapsibleModule>
    )
  })
}
