import type { DashboardRegistry } from './dashboard/widgetFramework'
import { useWidgets } from './dashboard/widgetFramework'
import { dashboardRegistry } from './dashboard/appWidgets'

export function MobileNavigation({ dashboards = dashboardRegistry }: { dashboards?: DashboardRegistry }) {
  const { activeView, setActiveView } = useWidgets()
  return <nav className="mobile-navigation" aria-label="Snabbnavigation">
    {dashboards.getAll().map(item => <button key={item.id} aria-current={activeView === item.id ? 'page' : undefined} onClick={() => setActiveView(item.id)} type="button"><span aria-hidden="true">{item.icon}</span>{item.title}</button>)}
  </nav>
}
