import type { WidgetRegistry } from './WidgetRegistry'

export function DashboardSections<TData>({
  data,
  state,
  registry,
}: {
  data: TData
  state: string
  registry: WidgetRegistry<TData>
}) {
  return registry.getSections().map(section => {
    const widgets = registry.getWidgets(section.id, state)
    if (widgets.length === 0) return null

    return (
      <section className={section.className} data-dashboard-section={section.id} key={section.id}>
        {(section.label || section.title) && (
          <header className="dashboard-section-title">
            {section.label && <p className="eyebrow">{section.label}</p>}
            {section.title && <h3>{section.title}</h3>}
          </header>
        )}
        {widgets.map(widget => {
          const Component = widget.component
          return <Component data={data} key={widget.id} />
        })}
      </section>
    )
  })
}
