import type { ReactNode } from 'react'
export function CollapsibleModule({
  actions,
  children,
  className,
  collapsedSummary,
  eyebrow,
  expanded,
  headingLevel = 2,
  moduleId,
  onToggle,
  title,
}: {
  actions?: ReactNode
  children: ReactNode
  className?: string
  collapsedSummary?: ReactNode
  eyebrow?: string
  expanded: boolean
  headingLevel?: 2 | 3
  moduleId: string
  onToggle: () => void
  title: string
}) {
  const contentId = `${moduleId}-content`
  const headingId = `${moduleId}-heading`
  const Heading = headingLevel === 2 ? 'h2' : 'h3'

  return (
    <section
      aria-labelledby={headingId}
      className={`dashboard-module${className ? ` ${className}` : ''}`}
      data-dashboard-module={moduleId}
      id={moduleId}
    >
      <header className="dashboard-module__header">
        <div>
          {eyebrow && <p className="eyebrow">{eyebrow}</p>}
          <Heading id={headingId}>{title}</Heading>
        </div>
        <div className="dashboard-module__actions">
          {actions}
          <button
            aria-controls={contentId}
            aria-expanded={expanded}
            aria-label={`${expanded ? 'Minimera' : 'Expandera'} ${title}`}
            className="dashboard-module__toggle"
            onClick={onToggle}
            type="button"
          >
            <span aria-hidden="true" className="dashboard-module__chevron">⌄</span>
          </button>
        </div>
      </header>
      {!expanded && collapsedSummary && <div className="dashboard-module__summary">{collapsedSummary}</div>}
      <div className="dashboard-module__content" hidden={!expanded} id={contentId}>
        {children}
      </div>
    </section>
  )
}
