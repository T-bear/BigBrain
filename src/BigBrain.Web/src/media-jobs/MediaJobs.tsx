import { useEffect, useRef, useState } from 'react'
import { getMediaJobs } from '../api'
import type { MediaJobsResponse } from '../types'
import { MediaJobCard } from './MediaJobCard'

type JobFilter = 'active' | 'importing' | 'available' | 'failed' | 'all'
const JOB_POLL_MS = 12_000
const filters: Array<{ id: JobFilter; label: string }> = [
  { id: 'active', label: 'Pågår' },
  { id: 'importing', label: 'Bearbetas' },
  { id: 'available', label: 'Klara' },
  { id: 'failed', label: 'Problem' },
  { id: 'all', label: 'Alla' },
]
const compactStatus: Record<string, string> = {
  queued: 'I kö', requested: 'Förbereds', searching: 'Söker', downloading: 'Laddar ner',
  stalled: 'Har stannat', importing: 'Bearbetas', unknown: 'Status väntar',
}
function compactTitle(title: string) {
  const normalized = title.match(/^(.*?)\s+((?:19|20)\d{2})\s+(?=(?:EXTRAS|2160p|1080p|720p|480p|UHD|BluRay|WEB[- .]?DL|WEBRip|HDTV)\b)/i)
  return normalized ? `${normalized[1].trim()} (${normalized[2]})` : title
}

export function MediaJobs({ showHeading = true }: { showHeading?: boolean }) {
  const [snapshot, setSnapshot] = useState<MediaJobsResponse | null>(null)
  const [failed, setFailed] = useState(false)
  const [filter, setFilter] = useState<JobFilter>('active')
  const [visibleCount, setVisibleCount] = useState(8)
  const [jobsExpanded, setJobsExpanded] = useState(false)
  const mounted = useRef(true)

  useEffect(() => {
    mounted.current = true
    const controller = new AbortController()
    let requestActive = false
    const refresh = async () => {
      if (requestActive || document.visibilityState !== 'visible') return
      requestActive = true
      try {
        const result = await getMediaJobs(controller.signal)
        if (mounted.current) {
          setSnapshot(result)
          setFailed(false)
        }
      } catch (error) {
        if (mounted.current && (!(error instanceof Error) || error.name !== 'AbortError')) setFailed(true)
      } finally {
        requestActive = false
      }
    }
    void refresh()
    const interval = window.setInterval(() => void refresh(), JOB_POLL_MS)
    const visible = () => {
      if (document.visibilityState === 'visible') void refresh()
    }
    document.addEventListener('visibilitychange', visible)
    return () => {
      mounted.current = false
      controller.abort()
      window.clearInterval(interval)
      document.removeEventListener('visibilitychange', visible)
    }
  }, [])

  const jobs = (Array.isArray(snapshot?.jobs) ? snapshot.jobs : []).filter(job => {
    if (filter === 'all') return true
    if (filter === 'active') return !['available', 'failed', 'completed'].includes(job.status)
    return job.status === filter
  })
  const visibleJobs = jobs.slice(0, visibleCount)
  const compactActive = filter === 'active' && jobs.length > 1
  const unavailableProviders = (Array.isArray(snapshot?.providers) ? snapshot.providers : [])
    .filter(provider => provider.status !== 'online')

  return <section className="media-jobs-section" aria-label={showHeading ? undefined : 'Poster i Medieflödet'} aria-labelledby={showHeading ? 'media-jobs-heading' : undefined}>
    {showHeading && <header className="media-jobs-heading">
      <div><p className="eyebrow">Live activity</p><h3 id="media-jobs-heading">Media Jobs</h3></div>
      <span aria-live="polite">{failed ? 'Uppdatering avbruten' : 'Uppdateras automatiskt'}</span>
    </header>}
    {!showHeading && <p className="media-jobs-update-status" aria-live="polite">{failed ? 'Uppdatering avbruten' : 'Uppdateras automatiskt'}</p>}
    {!snapshot && !failed && <p aria-live="polite">Loading media jobs…</p>}
    {failed && !snapshot && <p role="alert" className="notice notice--error">Media jobs could not be loaded.</p>}
    {failed && snapshot && <p role="status" className="notice notice--warning">Automatisk uppdatering är tillfälligt otillgänglig. Senaste status visas.</p>}
    {unavailableProviders.length > 0 && <p role="status" className="notice notice--warning">
      Vissa mediatjänster svarar inte just nu. Tillgängliga nedladdningar visas fortfarande.
    </p>}
    <div className="media-job-filters" aria-label="Filtrera pågående media">
      {filters.map(item => <button
        aria-pressed={filter === item.id}
        className={filter === item.id ? 'media-job-filter media-job-filter--active' : 'media-job-filter'}
        key={item.id}
        onClick={() => { setFilter(item.id); setVisibleCount(8); setJobsExpanded(false) }}
        type="button">{item.label}</button>)}
    </div>
    {snapshot && jobs.length === 0 && <p className="media-jobs-empty">Inget pågår i den här vyn.</p>}
    {compactActive && <section aria-labelledby="media-jobs-compact-heading" className="media-jobs-compact">
      <header><h4 id="media-jobs-compact-heading">{jobs.length} pågående nedladdningar</h4><button aria-controls="media-jobs-expanded" aria-expanded={jobsExpanded} className="secondary-button" onClick={() => setJobsExpanded(value => !value)} type="button">{jobsExpanded ? 'Dölj nedladdningar' : 'Visa nedladdningar'}</button></header>
      <ul>{visibleJobs.map(job => <li key={job.id}><span><strong title={job.title}>{compactTitle(job.title)}</strong><small>{compactStatus[job.status] ?? job.status}</small></span>{job.progressPercent !== null && <b>{Math.max(0, Math.min(100, job.progressPercent)).toFixed(0)}%</b>}</li>)}</ul>
    </section>}
    {jobs.length > 0 && (!compactActive || jobsExpanded) &&
      <div className="media-jobs-grid" id={compactActive ? 'media-jobs-expanded' : undefined}>{visibleJobs.map(job => <MediaJobCard job={job} key={job.id} />)}</div>}
    {visibleCount < jobs.length &&
      <button className="secondary-button media-jobs-more" type="button" onClick={() => setVisibleCount(count => count + 8)}>
        Visa fler
      </button>}
  </section>
}
