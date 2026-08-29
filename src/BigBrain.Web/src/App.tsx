import { useEffect, useMemo, useRef, useState } from 'react'
import { getDockerContainers, getModules, getSystemOverview, getSystemRecovery } from './api'
import { createAppWidgetRegistry } from './dashboard/appWidgets'
import { WidgetProvider } from './dashboard/widgetFramework'
import type { DockerInventory, ModuleDefinition, SystemOverview, SystemRecoverySnapshot } from './types'
import { ThemeProvider } from './ThemeProvider'
import { AppShell } from './AppShell'
import { AudiobookPlaybackProvider } from './audiobooks/AudiobookPlayback'

const POLL_INTERVAL_MS = 5_000

function AppContent() {
  const [modules, setModules] = useState<ModuleDefinition[]>([])
  const [moduleError, setModuleError] = useState(false)
  const [system, setSystem] = useState<SystemOverview | null>(null)
  const [systemError, setSystemError] = useState(false)
  const [docker, setDocker] = useState<DockerInventory | null>(null)
  const [dockerError, setDockerError] = useState(false)
  const [recovery, setRecovery] = useState<SystemRecoverySnapshot | null>(null)
  const [recoveryError, setRecoveryError] = useState(false)
  const systemRequestActive = useRef(false)

  useEffect(() => {
    const controller = new AbortController()
    getModules(controller.signal).then(setModules).catch((error: unknown) => {
      if (error instanceof Error && error.name !== 'AbortError') setModuleError(true)
    })
    getDockerContainers(controller.signal).then(setDocker).catch((error: unknown) => {
      if (error instanceof Error && error.name !== 'AbortError') setDockerError(true)
    })
    getSystemRecovery(controller.signal).then(setRecovery).catch((error: unknown) => {
      if (error instanceof Error && error.name !== 'AbortError') setRecoveryError(true)
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

  const registry = useMemo(() => createAppWidgetRegistry({ docker, dockerError, moduleError, modules, recovery, recoveryError, system, systemError }), [docker, dockerError, moduleError, modules, recovery, recoveryError, system, systemError])
  return <WidgetProvider registry={registry}><AudiobookPlaybackProvider><AppShell /></AudiobookPlaybackProvider></WidgetProvider>
}

export default function App() {
  return <ThemeProvider><AppContent /></ThemeProvider>
}
