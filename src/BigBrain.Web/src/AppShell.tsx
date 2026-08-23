import { AppIcon } from './AppIcon'
import { dashboardRegistry } from './dashboard/appWidgets'
import { DashboardWorkspace } from './dashboard/DashboardWorkspace'
import { useWidgets } from './dashboard/widgetFramework'
import { MobileNavigation } from './MobileNavigation'
import { BBButton } from './components'

const primaryDestinations = new Set(['home', 'family', 'media', 'finance', 'more'])

export function AppShell() {
  const { activeView, setActiveView } = useWidgets()

  return <div className="shell bb-page-shell">
    <aside className="sidebar">
      <BBButton className="brand" onClick={() => setActiveView('home')} type="button" variant="tertiary">
        <img alt="" className="brand__mark" height="34" src="/icons/bigbrain-192.png" width="34" />
        <span>BigBrain</span>
      </BBButton>
      <nav aria-label="Primär navigation" className="desktop-navigation">
        <p className="nav-label">Vyer</p>
        {dashboardRegistry.getAll().filter(view => primaryDestinations.has(view.id)).map(view => <BBButton aria-current={activeView === view.id ? 'page' : undefined} className="nav-link" key={view.id} onClick={() => setActiveView(view.id)} type="button" variant="tertiary"><AppIcon name={view.icon} /><span>{view.title}</span></BBButton>)}
      </nav>
    </aside>
    <DashboardWorkspace dashboards={dashboardRegistry} />
    <MobileNavigation dashboards={dashboardRegistry} />
  </div>
}
