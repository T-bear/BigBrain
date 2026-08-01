import { useEffect, useState } from 'react'
import { getMediaPlay } from '../api'
import type { MediaJob, MediaPlayResponse } from '../types'

const labels: Record<MediaJob['status'], string> = {
  queued: 'I kö',
  requested: 'Förbereds',
  searching: 'Söker',
  downloading: 'Laddar ner',
  stalled: 'Har stannat',
  completed: 'Väntar på import',
  importing: 'Bearbetas',
  available: 'Klar',
  failed: 'Problem',
  unknown: 'Status väntar',
}
const mediaTypeLabels: Record<MediaJob['mediaType'], string> = { movie: 'Film', series: 'Serie', season: 'Säsong', episode: 'Avsnitt', unknown: 'Media' }

function eta(seconds: number | null) {
  if (seconds === null) return null
  const hours = Math.floor(seconds / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  return hours > 0 ? `${hours}h ${minutes}m` : `${minutes} min`
}

export function MediaJobCard({ job }: { job: MediaJob }) {
  const [play, setPlay] = useState<MediaPlayResponse | null>(null)

  useEffect(() => {
    if (!job.canPlay || job.status !== 'available' || !job.playItemId) {
      setPlay(null)
      return
    }
    const controller = new AbortController()
    void getMediaPlay(job.playItemId, controller.signal)
      .then(setPlay)
      .catch(() => setPlay(null))
    return () => controller.abort()
  }, [job.canPlay, job.playItemId, job.status])

  const progress = job.progressPercent === null ? null : Math.max(0, Math.min(100, job.progressPercent))
  return <article className={`media-job media-job--${job.status}`}>
    <header>
      <div className="media-job__identity">
        <span className="media-job__status">{labels[job.status]}</span>
        <h4 title={job.title}>{job.title}</h4>
        <small>{mediaTypeLabels[job.mediaType]}</small>
      </div>
      {progress !== null && <strong aria-label={`${progress.toFixed(0)} percent`}>{progress.toFixed(0)}%</strong>}
    </header>
    {progress !== null && job.status !== 'available' &&
      <progress max="100" value={progress} aria-label={`${job.title} progress`}>{progress}%</progress>}
    <div className="media-job__metrics">
      {job.episodeCount !== null && <span>{job.completedEpisodeCount ?? 0} av {job.episodeCount} avsnitt klara</span>}
      {eta(job.etaSeconds) && <span>Cirka {eta(job.etaSeconds)} kvar</span>}
    </div>
    {job.userMessage && <p role={job.status === 'failed' ? 'alert' : 'status'} className="media-job__error">{job.userMessage}</p>}
    <details className="media-job__details">
      <summary>Visa tekniska detaljer</summary>
      <div className="media-job__technical-summary"><span>Provider: {job.provider}</span>{job.subtitle && <span>Fullständig titel: {job.subtitle}</span>}<span>Intern status: {job.status}</span></div>
      {job.details.length > 0 && <ul>{job.details.map((detail, index) =>
        <li key={`${detail.provider}:${detail.subtitle ?? index}`}>
          <strong>{detail.provider}</strong>
          <span>{labels[detail.status]}{detail.progressPercent === null ? '' : ` · ${detail.progressPercent.toFixed(0)}%`}</span>
          {detail.subtitle && <small title={detail.subtitle}>{detail.subtitle}</small>}
        </li>)}</ul>}
    </details>
    {play?.canPlay && <a className="primary-button media-job__play" href={play.playUrl}>▶ Spela i Jellyfin</a>}
  </article>
}
