import { useEffect, useMemo, useState } from 'react'
import { getDockerContainers, getModules, getSystemOverview, getSystemRecovery } from './api'
import { createAppWidgetRegistry } from './dashboard/appWidgets'
import { useWidgets, WidgetProvider } from './dashboard/widgetFramework'
import type { DockerInventory, ModuleDefinition, SystemOverview, SystemRecoverySnapshot } from './types'
import { ThemeProvider } from './ThemeProvider'
import { AppShell } from './AppShell'
import { AudiobookPlaybackProvider } from './audiobooks/AudiobookPlayback'

const POLL_INTERVAL_MS = 5_000

// Request lifetime follows the existing dashboard view, including restored/deep-linked views.
function ViewRequests({ onModules, onModuleError, onDocker, onDockerError, onRecovery, onRecoveryError, onSystem, onSystemError }: {
  onModules: (value: ModuleDefinition[]) => void; onModuleError: (value: boolean) => void
  onDocker: (value: DockerInventory) => void; onDockerError: (value: boolean) => void
  onRecovery: (value: SystemRecoverySnapshot) => void; onRecoveryError: (value: boolean) => void
  onSystem: (value: SystemOverview) => void; onSystemError: (value: boolean) => void
}) {
  const { activeView } = useWidgets()
  const needsRecovery = activeView === 'home' || activeView === 'admin'
  useEffect(() => {
    if (activeView !== 'family') return
    const controller = new AbortController()
    void getModules(controller.signal).then(value => { if (!controller.signal.aborted) { onModules(value); onModuleError(false) } })
      .catch(() => { if (!controller.signal.aborted) onModuleError(true) })
    return () => controller.abort()
  }, [activeView, onModules, onModuleError])
  useEffect(() => {
    if (!needsRecovery) return
    const controller = new AbortController()
    void getSystemRecovery(controller.signal).then(value => { if (!controller.signal.aborted) { onRecovery(value); onRecoveryError(false) } })
      .catch(() => { if (!controller.signal.aborted) onRecoveryError(true) })
    return () => controller.abort()
  }, [needsRecovery, onRecovery, onRecoveryError])
  useEffect(() => {
    if (activeView !== 'admin') return
    const controller = new AbortController()
    void getDockerContainers(controller.signal).then(value => { if (!controller.signal.aborted) { onDocker(value); onDockerError(false) } })
      .catch(() => { if (!controller.signal.aborted) onDockerError(true) })
    return () => controller.abort()
  }, [activeView, onDocker, onDockerError])
  useEffect(() => {
    if (activeView !== 'admin') return
    const controller = new AbortController()
    let requestActive = false
    const refresh = async () => {
      if (controller.signal.aborted || requestActive || document.visibilityState === 'hidden') return
      requestActive = true
      try {
        const value = await getSystemOverview(controller.signal)
        if (!controller.signal.aborted) { onSystem(value); onSystemError(false) }
      } catch { if (!controller.signal.aborted) onSystemError(true) }
      finally { requestActive = false }
    }
    void refresh()
    const interval = window.setInterval(() => void refresh(), POLL_INTERVAL_MS)
    const visible = () => { if (document.visibilityState === 'visible') void refresh() }
    document.addEventListener('visibilitychange', visible)
    return () => {
      controller.abort()
      window.clearInterval(interval)
      document.removeEventListener('visibilitychange', visible)
    }
  }, [activeView, onSystem, onSystemError])
  return null
}

function AppContent() {
  const [modules, setModules] = useState<ModuleDefinition[]>([])
  const [moduleError, setModuleError] = useState(false)
  const [system, setSystem] = useState<SystemOverview | null>(null)
  const [systemError, setSystemError] = useState(false)
  const [docker, setDocker] = useState<DockerInventory | null>(null)
  const [dockerError, setDockerError] = useState(false)
  const [recovery, setRecovery] = useState<SystemRecoverySnapshot | null>(null)
  const [recoveryError, setRecoveryError] = useState(false)

  const registry = useMemo(() => createAppWidgetRegistry({ docker, dockerError, moduleError, modules, recovery, recoveryError, system, systemError }), [docker, dockerError, moduleError, modules, recovery, recoveryError, system, systemError])
  return <WidgetProvider registry={registry}><ViewRequests onModules={setModules} onModuleError={setModuleError} onDocker={setDocker} onDockerError={setDockerError} onRecovery={setRecovery} onRecoveryError={setRecoveryError} onSystem={setSystem} onSystemError={setSystemError} /><AudiobookPlaybackProvider><AppShell /></AudiobookPlaybackProvider></WidgetProvider>
}

export default function App() {
  return <ThemeProvider><AppContent /></ThemeProvider>
}
