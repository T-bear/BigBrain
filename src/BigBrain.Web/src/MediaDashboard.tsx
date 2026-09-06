import { useCallback, useEffect, useRef, useState, type ReactNode } from 'react'
import { getMediaOverview } from './api'
import { StatusBadge } from './components'
import { DashboardSections } from './dashboard/DashboardSections'
import { mediaWidgetRegistry } from './dashboard/mediaWidgets'
import type { MediaOverview } from './types'
import { MediaSearch } from './media-search/MediaSearch'
import { MediaJobs } from './media-jobs/MediaJobs'
import { MediaServiceLinks } from './media-services/MediaServiceLinks'
import { CollapsibleModule } from './dashboard/CollapsibleModule'
import { useDashboardLayout, type DashboardExpandedState, type DashboardModuleId } from './dashboard/dashboardLayout'
import { SmartShuffle } from './smart-shuffle/SmartShuffle'
import { DownloadControl } from './download-control/DownloadControl'

const MEDIA_STATUS_POLL_MS = 45_000

const primarySections: readonly DashboardModuleId[] = ['media-health', 'insights', 'services']
const secondarySections: readonly DashboardModuleId[] = ['activity', 'details']

export function MediaDashboard({
  administrationOnly = false,
  administrationOpen = true,
  children,
  expanded,
  onToggle,
}: {
  administrationOnly?: boolean
  administrationOpen?: boolean
  children?: ReactNode
  expanded?: DashboardExpandedState
  onToggle?: (moduleId: DashboardModuleId) => void
}) {
  const [administrationExpanded, setAdministrationExpanded] = useState(administrationOnly && administrationOpen)
  const needsOverview = !administrationOnly || administrationExpanded
  const localLayout = useDashboardLayout()
  const layoutExpanded = expanded ?? localLayout.expanded
  const toggleModule = onToggle ?? localLayout.toggle
  const [overview, setOverview] = useState<MediaOverview | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)
  const controllerRef = useRef<AbortController | null>(null)

  const refresh = useCallback(async () => {
    if (controllerRef.current && !controllerRef.current.signal.aborted) return
    const controller = new AbortController()
    controllerRef.current = controller
    setLoading(true)
    try {
      const value = await getMediaOverview(controller.signal)
      if (!controller.signal.aborted) { setOverview(value); setError(false) }
    } catch (requestError) {
      if (!controller.signal.aborted && requestError instanceof Error && requestError.name !== 'AbortError') setError(true)
    } finally {
      if (!controller.signal.aborted) setLoading(false)
      if (controllerRef.current === controller) controllerRef.current = null
    }
  }, [])

  useEffect(() => {
    if (!needsOverview) return
    void refresh()
    const interval = window.setInterval(() => {
      if (document.visibilityState === 'visible') void refresh()
    }, MEDIA_STATUS_POLL_MS)
    const visible = () => {
      if (document.visibilityState === 'visible') void refresh()
    }
    document.addEventListener('visibilitychange', visible)
    return () => {
      window.clearInterval(interval)
      document.removeEventListener('visibilitychange', visible)
      controllerRef.current?.abort()
    }
  }, [refresh, needsOverview])

  return <section id={administrationOnly ? 'media-administration' : 'media'} aria-label={administrationOnly ? 'Mediaadministration' : undefined} aria-labelledby={administrationOnly ? undefined : 'media-heading'} className="media-section">
    {!administrationOnly && <div className="section-heading"><div><p className="eyebrow">Film och serier</p><h2 id="media-heading">Hitta något att titta på</h2></div>
    </div>}
    {needsOverview && loading && !overview && <p aria-live="polite">Laddar film och serier…</p>}
    {error && <p role="alert" className="notice notice--error">Film och serier kunde inte laddas.{overview ? ' Senaste tillgängliga uppdatering visas.' : ''}</p>}
    {overview && !administrationOnly && <>
      <div id="search" data-dashboard-module="media-search"><MediaSearch /></div>
      <SmartShuffle />
      <DownloadControl />
      <CollapsibleModule
        eyebrow="Följ film och serier från sökning till bibliotek"
        expanded={layoutExpanded['media-jobs']}
        moduleId="media-jobs"
        onToggle={() => toggleModule('media-jobs')}
        title="Medieflöde"
      >
        <div id="queue"><p className="media-flow-intro">Följ film och serier genom sökning, nedladdning och bearbetning tills de finns i biblioteket. En titel kan därför synas här samtidigt som själva nedladdningen visas i Nedladdningskö.</p><MediaJobs showHeading={false} /></div>
      </CollapsibleModule>
    </>}
    <details className="administration" id="administration" open={administrationExpanded} onToggle={event => setAdministrationExpanded(event.currentTarget.open)}>
      <summary><span><strong>Administration</strong><small>Systemstatus, tjänster och diagnostik</small></span></summary>
      <div className="administration__content">
        <div className="administration__heading"><div><p className="eyebrow">Teknisk översikt</p><h2>Administration</h2></div><div className="section-actions"><StatusBadge status={overview?.status ?? (error ? 'error' : 'loading')} /><button type="button" className="secondary-button" onClick={() => void refresh()} disabled={loading}>Uppdatera</button></div></div>
        {administrationExpanded && <MediaServiceLinks />}
        {overview && <DashboardSections data={overview} expanded={layoutExpanded} onToggle={toggleModule} state={overview.status} registry={mediaWidgetRegistry} sectionIds={primarySections} />}
        {children}
        {overview && <DashboardSections data={overview} expanded={layoutExpanded} onToggle={toggleModule} state={overview.status} registry={mediaWidgetRegistry} sectionIds={secondarySections} />}
        {overview && <p className="last-updated">Uppdaterad <time dateTime={overview.collectedAtUtc}>{new Date(overview.collectedAtUtc).toLocaleTimeString()}</time></p>}
      </div>
    </details>
  </section>
}
