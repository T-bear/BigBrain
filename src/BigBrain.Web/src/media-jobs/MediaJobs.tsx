import { useEffect, useRef, useState } from 'react'
import { getMediaJobs, subscribeMediaJobs } from '../api'
import type { MediaJobsResponse } from '../types'
import { MediaJobCard } from './MediaJobCard'

type JobFilter = 'active' | 'importing' | 'available' | 'failed' | 'all'
const filters: Array<{ id: JobFilter; label: string }> = [
  { id: 'active', label: 'Active' },
  { id: 'importing', label: 'Importing' },
  { id: 'available', label: 'Available' },
  { id: 'failed', label: 'Failed' },
  { id: 'all', label: 'All' },
]

export function MediaJobs() {
  const [snapshot, setSnapshot] = useState<MediaJobsResponse | null>(null)
  const [failed, setFailed] = useState(false)
  const [filter, setFilter] = useState<JobFilter>('active')
  const [visibleCount, setVisibleCount] = useState(8)
  const mounted = useRef(true)

  useEffect(() => {
    mounted.current = true
    const controller = new AbortController()
    void getMediaJobs(controller.signal)
      .then(result => {
        if (mounted.current) {
          setSnapshot(result)
          setFailed(false)
        }
      })
      .catch(error => {
        if (mounted.current && (!(error instanceof Error) || error.name !== 'AbortError')) setFailed(true)
      })
    const unsubscribe = subscribeMediaJobs(
      result => {
        if (mounted.current) {
          setSnapshot(result)
          setFailed(false)
        }
      },
      () => { if (mounted.current) setFailed(true) },
    )
    return () => {
      mounted.current = false
      controller.abort()
      unsubscribe()
    }
  }, [])

  const jobs = (Array.isArray(snapshot?.jobs) ? snapshot.jobs : []).filter(job => {
    if (filter === 'all') return true
    if (filter === 'active') return !['available', 'failed', 'completed'].includes(job.status)
    return job.status === filter
  })
  const visibleJobs = jobs.slice(0, visibleCount)
  const unavailableProviders = (Array.isArray(snapshot?.providers) ? snapshot.providers : [])
    .filter(provider => provider.status !== 'online')

  return <section className="media-jobs-section" aria-labelledby="media-jobs-heading">
    <header className="media-jobs-heading">
      <div><p className="eyebrow">Live activity</p><h3 id="media-jobs-heading">Media Jobs</h3></div>
      <span aria-live="polite">{failed ? 'Updates interrupted' : 'Live updates'}</span>
    </header>
    {!snapshot && !failed && <p aria-live="polite">Loading media jobs…</p>}
    {failed && !snapshot && <p role="alert" className="notice notice--error">Media jobs could not be loaded.</p>}
    {failed && snapshot && <p role="status" className="notice notice--warning">Live updates are temporarily unavailable. Showing the latest status.</p>}
    {unavailableProviders.length > 0 && <p role="status" className="notice notice--warning">
      {unavailableProviders.some(provider => provider.provider === 'Jellyfin')
        ? 'Jellyfin is currently unavailable. Download and import status is still shown.'
        : 'Some media providers are temporarily unavailable. Available job data is still shown.'}
    </p>}
    <div className="media-job-filters" aria-label="Filter media jobs">
      {filters.map(item => <button
        aria-pressed={filter === item.id}
        className={filter === item.id ? 'media-job-filter media-job-filter--active' : 'media-job-filter'}
        key={item.id}
        onClick={() => { setFilter(item.id); setVisibleCount(8) }}
        type="button">{item.label}</button>)}
    </div>
    {snapshot && jobs.length === 0 && <p className="media-jobs-empty">No active or recently available media jobs.</p>}
    {jobs.length > 0 &&
      <div className="media-jobs-grid">{visibleJobs.map(job => <MediaJobCard job={job} key={job.id} />)}</div>}
    {visibleCount < jobs.length &&
      <button className="secondary-button media-jobs-more" type="button" onClick={() => setVisibleCount(count => count + 8)}>
        Show more
      </button>}
  </section>
}
