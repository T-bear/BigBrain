import { useEffect, useState } from 'react'
import { getModules } from './api'
import { DashboardWidget } from './DashboardWidget'
import type { ModuleDefinition } from './types'

export default function App() {
  const [modules, setModules] = useState<ModuleDefinition[]>([])
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    const controller = new AbortController()

    getModules(controller.signal)
      .then(setModules)
      .catch((error: unknown) => {
        if (error instanceof Error && error.name !== 'AbortError') {
          setFailed(true)
        }
      })

    return () => controller.abort()
  }, [])

  return (
    <div className="shell">
      <aside className="sidebar">
        <a className="brand" href="/" aria-label="BigBrain home">
          <span className="brand__mark">B</span>
          <span>BigBrain</span>
        </a>
        <nav aria-label="Modules">
          <p className="nav-label">Modules</p>
          {modules.map((module) => (
            <a className="nav-link" href={module.route} key={module.id}>
              {module.name}
            </a>
          ))}
        </nav>
      </aside>

      <main className="main">
        <header className="page-header">
          <div>
            <p className="eyebrow">Control plane</p>
            <h1>Dashboard</h1>
          </div>
          <span className="sprint-badge">Sprint 1</span>
        </header>

        <section aria-label="Dashboard widgets" className="widget-grid">
          {failed ? (
            <p role="alert">The module registry is unavailable.</p>
          ) : modules.length === 0 ? (
            <p>Loading modules…</p>
          ) : (
            modules.flatMap((module) =>
              module.dashboardWidgets.map((widget) => (
                <DashboardWidget key={`${module.id}:${widget.id}`} widget={widget} />
              )),
            )
          )}
        </section>
      </main>
    </div>
  )
}

