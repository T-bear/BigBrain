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

const MEDIA_STATUS_POLL_MS = 45_000

const primarySections: readonly DashboardModuleId[] = ['media-health', 'insights', 'services']
const secondarySections: readonly DashboardModuleId[] = ['activity', 'details']

export function MediaDashboard({
  children,
  expanded,
  onToggle,
}: {
  children?: ReactNode
  expanded?: DashboardExpandedState
  onToggle?: (moduleId: DashboardModuleId) => void
}) {
  const localLayout = useDashboardLayout()
  const layoutExpanded = expanded ?? localLayout.expanded
  const toggleModule = onToggle ?? localLayout.toggle
  const [overview, setOverview] = useState<MediaOverview | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)
  const controllerRef = useRef<AbortController | null>(null)

  const refresh = useCallback(async () => {
    controllerRef.current?.abort()
    const controller = new AbortController()
    controllerRef.current = controller
    setLoading(true)
    try {
      setOverview(await getMediaOverview(controller.signal))
      setError(false)
    } catch (requestError) {
      if (requestError instanceof Error && requestError.name !== 'AbortError') setError(true)
    } finally {
      if (!controller.signal.aborted) setLoading(false)
    }
  }, [])

  useEffect(() => {
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
  }, [refresh])

  return <section id="media" aria-labelledby="media-heading" className="media-section">
    <div className="section-heading"><div><p className="eyebrow">Film och serier</p><h2 id="media-heading">Hitta något att titta på</h2></div>
    </div>
    {loading && !overview && <p aria-live="polite">Laddar film och serier…</p>}
    {error && <p role="alert" className="notice notice--error">Film och serier kunde inte laddas.{overview ? ' Senaste tillgängliga uppdatering visas.' : ''}</p>}
    {overview && <>
      <div id="search" data-dashboard-module="media-search"><MediaSearch /></div>
      <SmartShuffle />
      <CollapsibleModule
        eyebrow="Pågående aktivitet"
        expanded={layoutExpanded['media-jobs']}
        moduleId="media-jobs"
        onToggle={() => toggleModule('media-jobs')}
        title="Pågående"
      >
        <div id="queue"><MediaJobs showHeading={false} /></div>
      </CollapsibleModule>
    </>}
    <details className="administration" id="administration">
      <summary><span><strong>Administration</strong><small>Systemstatus, tjänster och diagnostik</small></span></summary>
      <div className="administration__content">
        <div className="administration__heading"><div><p className="eyebrow">Teknisk översikt</p><h2>Administration</h2></div><div className="section-actions"><StatusBadge status={overview?.status ?? (error ? 'error' : 'loading')} /><button type="button" className="secondary-button" onClick={() => void refresh()} disabled={loading}>Uppdatera</button></div></div>
        <MediaServiceLinks />
        {overview && <DashboardSections data={overview} expanded={layoutExpanded} onToggle={toggleModule} state={overview.status} registry={mediaWidgetRegistry} sectionIds={primarySections} />}
        {children}
        {overview && <DashboardSections data={overview} expanded={layoutExpanded} onToggle={toggleModule} state={overview.status} registry={mediaWidgetRegistry} sectionIds={secondarySections} />}
        {overview && <p className="last-updated">Uppdaterad <time dateTime={overview.collectedAtUtc}>{new Date(overview.collectedAtUtc).toLocaleTimeString()}</time></p>}
      </div>
    </details>
  </section>
}
