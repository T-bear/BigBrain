import { useEffect, useRef } from 'react'
import { DownloadControl } from '../download-control/DownloadControl'
import { MediaJobs } from '../media-jobs/MediaJobs'
import { MediaDashboard } from '../MediaDashboard'
import { useWidgets } from '../dashboard/widgetFramework'
import { AppIcon } from '../AppIcon'
import { followMediaLink, mediaControlPath, type MediaControlPage } from './mediaNavigation'

const choices = [
  { id: 'downloads', title: 'Nedladdningskö', detail: 'Hantera aktiva nedladdningar.' },
  { id: 'flow', title: 'Medieflöde', detail: 'Följ film och serier från sökning till bibliotek.' },
  { id: 'integrations', title: 'Tekniska integrationer', detail: 'Status och diagnostik för mediatjänsterna.' },
] as const

export function MediaControlEntry() {
  const { activeView } = useWidgets()
  return (
    <a
      className="bb-surface media-control-card"
      href={mediaControlPath}
      onClick={event => {
        window.history.replaceState({ ...window.history.state, bigbrainView: activeView }, '')
        followMediaLink(event)
      }}
    >
      <span>
        <strong>Öppna Mediekontroll</strong>
        <small>Nedladdningar, medieflöde och integrationer</small>
      </span>
      <AppIcon name="chevron" />
    </a>
  )
}

export function MediaControlOverview({ preview = false }: { preview?: boolean }) {
  return (
    <nav aria-label="Mediekontroll" className="media-control-grid">
      {choices.map(choice => (
        <a
          className="bb-surface media-control-card"
          href={`${mediaControlPath}/${choice.id}`}
          key={choice.id}
          onClick={preview ? event => event.preventDefault() : followMediaLink}
        >
          <span>
            <strong>{choice.title}</strong>
            <small>{choice.detail}</small>
          </span>
          <AppIcon name="chevron" />
        </a>
      ))}
    </nav>
  )
}

export function MediaControl({ page }: { page: MediaControlPage }) {
  const title = useRef<HTMLHeadingElement>(null)
  useEffect(() => {
    title.current?.focus()
  }, [page])
  return (
    <section className="media-control">
      <a
        className="bb-button bb-button--tertiary"
        href={page === 'overview' ? '/media' : mediaControlPath}
        onClick={followMediaLink}
      >
        {page === 'overview' ? '‹ Till Media' : '‹ Till Mediekontroll'}
      </a>
      <header className="page-header">
        <div>
          <p className="eyebrow">{page === 'overview' ? 'Media' : 'Mediekontroll'}</p>
          <h1 ref={title} tabIndex={-1}>
            {page === 'overview' ? 'Mediekontroll' : choices.find(choice => choice.id === page)?.title}
          </h1>
        </div>
      </header>
      {page === 'overview' && <MediaControlOverview />}
      {page === 'downloads' && <DownloadControl />}
      {page === 'flow' && <MediaJobs showHeading={false} />}
      {page === 'integrations' && <MediaDashboard administrationOnly />}
    </section>
  )
}
