import type { DashboardRegistry } from './dashboard/widgetFramework'
import { useWidgets } from './dashboard/widgetFramework'
import { dashboardRegistry } from './dashboard/appWidgets'
import { AppIcon } from './AppIcon'

const primary = new Set(['home', 'family', 'media', 'finance', 'more'])

export function MobileNavigation({ dashboards = dashboardRegistry }: { dashboards?: DashboardRegistry }) {
  const { activeView, setActiveView } = useWidgets()
  const selectView=(view:Parameters<typeof setActiveView>[0])=>{if(window.location.pathname.startsWith('/media/audiobooks')){window.history.pushState({},'','/');window.dispatchEvent(new Event('bb:navigation'))}setActiveView(view)}
  return <nav className="mobile-navigation" aria-label="Snabbnavigation">
    {dashboards.getAll().filter(item => primary.has(item.id)).map(item => <button key={item.id} aria-current={activeView === item.id ? 'page' : undefined} onClick={() => selectView(item.id)} type="button"><AppIcon name={item.icon} />{item.title}</button>)}
  </nav>
}
