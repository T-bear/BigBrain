import { useEffect, useRef, useState } from 'react'
import { getDockerContainers, getModules, getSystemOverview } from './api'
import { DockerContainerList, MetricCard, ModuleCard, ProgressMetric, StatusBadge } from './components'
import { MediaDashboard } from './MediaDashboard'
import type { DockerInventory, ModuleDefinition, SystemOverview } from './types'
import { MobileNavigation } from './MobileNavigation'
import { CollapsibleModule } from './dashboard/CollapsibleModule'
import { useDashboardLayout } from './dashboard/dashboardLayout'

const POLL_INTERVAL_MS = 5_000

function formatBytes(value: number | null) {
  if (value === null) return 'Unavailable'
  const units = ['B', 'KiB', 'MiB', 'GiB', 'TiB']
  let size = value
  let unit = 0
  while (size >= 1024 && unit < units.length - 1) {
    size /= 1024
    unit += 1
  }
  return `${size.toFixed(unit < 2 ? 0 : 1)} ${units[unit]}`
}

function formatUptime(seconds: number | null) {
  if (seconds === null) return 'Unavailable'
  const days = Math.floor(seconds / 86_400)
  const hours = Math.floor((seconds % 86_400) / 3_600)
  const minutes = Math.floor((seconds % 3_600) / 60)
  const dayPart = days > 0 ? `${days} ${days === 1 ? 'dag' : 'dagar'} ` : ''
  return `${dayPart}${hours} ${hours === 1 ? 'timme' : 'timmar'} ${minutes} ${minutes === 1 ? 'minut' : 'minuter'}`
}

export default function App() {
  const [modules, setModules] = useState<ModuleDefinition[]>([])
  const [moduleError, setModuleError] = useState(false)
  const [system, setSystem] = useState<SystemOverview | null>(null)
  const [systemError, setSystemError] = useState(false)
  const [docker, setDocker] = useState<DockerInventory | null>(null)
  const [dockerError, setDockerError] = useState(false)
  const systemRequestActive = useRef(false)
  const { expanded, toggle } = useDashboardLayout()

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
      try {
        const overview = await getSystemOverview(controller.signal)
        setSystem(overview)
        setSystemError(false)
      } catch (error) {
        if (error instanceof Error && error.name !== 'AbortError') setSystemError(true)
      } finally {
        systemRequestActive.current = false
      }
    }

    void refreshSystem()
    const interval = window.setInterval(() => void refreshSystem(), POLL_INTERVAL_MS)

    return () => {
      window.clearInterval(interval)
      controller.abort()
    }
  }, [])

  const systemStatus = system?.status ?? (systemError ? 'Error' : 'Loading')
  const dockerStatus = dockerError ? 'Error' : docker?.availability.available ? 'Available' : docker ? 'Unavailable' : 'Loading'

  return (
    <div className="shell">
      <aside className="sidebar">
        <a className="brand" href="/" aria-label="BigBrain home">
          <span className="brand__mark">B</span>
          <span>BigBrain</span>
        </a>
        <nav aria-label="Modules">
          <p className="nav-label">Modules</p>
          {moduleError && <p role="alert" className="muted">Module registry unavailable.</p>}
          {modules.map((module) => (
            <a className="nav-link" href={module.route} key={module.id}>
              <span>{module.name}</span>
              <StatusBadge status={module.status} compact />
            </a>
          ))}
        </nav>
      </aside>

      <main className="main" id="home">
        <header className="page-header">
          <div>
            <p className="eyebrow">Control plane</p>
            <h1>Server overview</h1>
          </div>
          <span className="sprint-badge">Sprint 2</span>
        </header>

        <MediaDashboard expanded={expanded} onToggle={toggle}>
          <CollapsibleModule
            actions={<StatusBadge status={systemStatus} />}
            eyebrow="System module"
            expanded={expanded.system}
            moduleId="system"
            onToggle={() => toggle('system')}
            title="System status"
          >

            {!system && !systemError && <p aria-live="polite">Loading system metrics…</p>}
            {systemError && (
              <p role="alert" className="notice notice--error">
                System metrics could not be refreshed.{system ? ' Showing the latest successful update.' : ''}
              </p>
            )}
            {system?.status.toLowerCase() === 'unavailable' && (
              <ModuleCard title="Host metrics not connected" status="Unavailable">
                <p>{system.warnings[0] ?? 'Host metrics are unavailable.'}</p>
              </ModuleCard>
            )}
            {system && (
              <>
                <div className="metric-grid">
                  <ProgressMetric label="CPU usage" value={system.cpu.usagePercent} detail={`${system.cpu.logicalProcessorCount} logical processors`} />
                  <ProgressMetric label="RAM usage" value={system.memory.usagePercent} detail={`${formatBytes(system.memory.usedBytes)} of ${formatBytes(system.memory.totalBytes)}`} />
                  {system.disks.map((disk) => (
                    <ProgressMetric key={disk.filesystemId} label={disk.displayName} value={disk.usagePercent} detail={`${formatBytes(disk.usedBytes)} used of ${formatBytes(disk.totalBytes)} · ${formatBytes(disk.availableBytes)} free`} />
                  ))}
                  <MetricCard label="System uptime" value={formatUptime(system.uptimeSeconds)} />
                  <MetricCard label="Hostname" value={system.hostname} />
                  <MetricCard label="Temperature" value={system.temperatureCelsius === null ? 'Unavailable' : `${system.temperatureCelsius.toFixed(1)} °C`} />
                </div>
                <p className="last-updated">
                  Last updated <time dateTime={system.collectedAtUtc}>{new Date(system.collectedAtUtc).toLocaleTimeString()}</time>
                </p>
                {system.status.toLowerCase() !== 'unavailable' && system.warnings.length > 0 && (
                  <p className="muted">{system.warnings.join(' ')}</p>
                )}
              </>
            )}
          </CollapsibleModule>

          <CollapsibleModule
            actions={<StatusBadge status={dockerStatus} />}
            eyebrow="Docker module"
            expanded={expanded.docker}
            moduleId="docker"
            onToggle={() => toggle('docker')}
            title="Docker overview"
          >
            {dockerError ? (
              <p role="alert" className="notice notice--error">Docker inventory could not be loaded.</p>
            ) : !docker ? (
              <p aria-live="polite">Loading Docker inventory…</p>
            ) : !docker.availability.available ? (
              <ModuleCard title="Integration not connected" status="Unavailable">
                <p>{docker.availability.reason}</p>
              </ModuleCard>
            ) : (
              <>
                <MetricCard label="Containers" value={String(docker.containers.length)} />
                <DockerContainerList containers={docker.containers} />
              </>
            )}
          </CollapsibleModule>
        </MediaDashboard>
      </main>
      <MobileNavigation />
    </div>
  )
}
