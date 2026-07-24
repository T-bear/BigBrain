import { useCallback, useEffect, useRef, useState } from 'react'
import { getMediaOverview } from './api'
import { StatusBadge } from './components'
import { DashboardSections } from './dashboard/DashboardSections'
import { mediaWidgetRegistry } from './dashboard/mediaWidgets'
import type { MediaOverview } from './types'

export function MediaDashboard() {
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
    return () => controllerRef.current?.abort()
  }, [refresh])

  return <section id="media" aria-labelledby="media-heading" className="media-section">
    <div className="section-heading"><div><p className="eyebrow">Media intelligence</p><h2 id="media-heading">Your media ecosystem</h2></div>
      <div className="section-actions"><StatusBadge status={overview?.status ?? (error ? 'error' : 'loading')} /><button type="button" className="secondary-button" onClick={() => void refresh()} disabled={loading}>Refresh</button></div>
    </div>
    {loading && !overview && <p aria-live="polite">Loading media intelligence…</p>}
    {error && <p role="alert" className="notice notice--error">Media dashboard could not be loaded.{overview ? ' Showing the latest update.' : ''}</p>}
    {overview && <>
      <DashboardSections data={overview} state={overview.status} registry={mediaWidgetRegistry} />
      <p className="last-updated">Updated <time dateTime={overview.collectedAtUtc}>{new Date(overview.collectedAtUtc).toLocaleTimeString()}</time></p>
    </>}
  </section>
}
