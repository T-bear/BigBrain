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
    <div className="section-heading"><div><p className="eyebrow">Media intelligence</p><h2 id="media-heading">Your media ecosystem</h2></div>
      <div className="section-actions"><StatusBadge status={overview?.status ?? (error ? 'error' : 'loading')} /><button type="button" className="secondary-button" onClick={() => void refresh()} disabled={loading}>Refresh</button></div>
    </div>
    {loading && !overview && <p aria-live="polite">Loading media intelligence…</p>}
    {error && <p role="alert" className="notice notice--error">Media dashboard could not be loaded.{overview ? ' Showing the latest update.' : ''}</p>}
    {overview && <>
      <div id="search" data-dashboard-module="media-search"><MediaSearch /></div>
      <MediaServiceLinks />
      <CollapsibleModule
        eyebrow="Pågående aktivitet"
        expanded={layoutExpanded['media-jobs']}
        moduleId="media-jobs"
        onToggle={() => toggleModule('media-jobs')}
        title="Media Jobs"
      >
        <div id="queue"><MediaJobs showHeading={false} /></div>
      </CollapsibleModule>
      <DashboardSections data={overview} expanded={layoutExpanded} onToggle={toggleModule} state={overview.status} registry={mediaWidgetRegistry} sectionIds={primarySections} />
    </>}
    {children}
    {overview && <>
      <DashboardSections data={overview} expanded={layoutExpanded} onToggle={toggleModule} state={overview.status} registry={mediaWidgetRegistry} sectionIds={secondarySections} />
      <p className="last-updated">Updated <time dateTime={overview.collectedAtUtc}>{new Date(overview.collectedAtUtc).toLocaleTimeString()}</time></p>
    </>}
  </section>
}
