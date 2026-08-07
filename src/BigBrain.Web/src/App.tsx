import { useEffect, useMemo, useRef, useState } from 'react'
import { getDockerContainers, getModules, getSystemOverview } from './api'
import { DashboardWorkspace } from './dashboard/DashboardWorkspace'
import { createAppWidgetRegistry, dashboardRegistry } from './dashboard/appWidgets'
import { WidgetProvider, useWidgets } from './dashboard/widgetFramework'
import { MobileNavigation } from './MobileNavigation'
import type { DockerInventory, ModuleDefinition, SystemOverview } from './types'
import { ThemeProvider } from './ThemeProvider'

const POLL_INTERVAL_MS = 5_000

function AppShell() {
  const { activeView, setActiveView } = useWidgets()

  return <div className="shell bb-page-shell">
    <aside className="sidebar">
      <button className="brand" onClick={() => setActiveView('home')} type="button">
        <img alt="" className="brand__mark" height="34" src="/icons/bigbrain-192.png" width="34" />
        <span>BigBrain</span>
      </button>
      <nav aria-label="Dashboardvyer" className="desktop-navigation">
        <p className="nav-label">Vyer</p>
        {dashboardRegistry.getAll().map(view => <button aria-current={activeView === view.id ? 'page' : undefined} className="nav-link" key={view.id} onClick={() => setActiveView(view.id)} type="button"><span aria-hidden="true">{view.icon}</span><span>{view.title}</span></button>)}
      </nav>
    </aside>
    <DashboardWorkspace dashboards={dashboardRegistry} />
    <MobileNavigation dashboards={dashboardRegistry} />
  </div>
}

function AppContent() {
  const [modules, setModules] = useState<ModuleDefinition[]>([])
  const [moduleError, setModuleError] = useState(false)
  const [system, setSystem] = useState<SystemOverview | null>(null)
  const [systemError, setSystemError] = useState(false)
  const [docker, setDocker] = useState<DockerInventory | null>(null)
  const [dockerError, setDockerError] = useState(false)
  const systemRequestActive = useRef(false)

  useEffect(() => {
    const controller = new AbortController()
    getModules(controller.signal).then(setModules).catch((error: unknown) => {
      if (error instanceof Error && error.name !== 'AbortError') setModuleError(true)
    })
    getDockerContainers(controller.signal).then(setDocker).catch((error: unknown) => {
      if (error instanceof Error && error.name !== 'AbortError') setDockerError(true)
    })
    const refreshSystem = async () => {
      if (systemRequestActive.current) return
      systemRequestActive.current = true
      try { setSystem(await getSystemOverview(controller.signal)); setSystemError(false) }
      catch (error) { if (error instanceof Error && error.name !== 'AbortError') setSystemError(true) }
      finally { systemRequestActive.current = false }
    }
    void refreshSystem()
    const interval = window.setInterval(() => void refreshSystem(), POLL_INTERVAL_MS)
    return () => { window.clearInterval(interval); controller.abort() }
  }, [])

  const registry = useMemo(() => createAppWidgetRegistry({ docker, dockerError, moduleError, modules, system, systemError }), [docker, dockerError, moduleError, modules, system, systemError])
  return <WidgetProvider registry={registry}><AppShell /></WidgetProvider>
}

export default function App() {
  return <ThemeProvider><AppContent /></ThemeProvider>
}
