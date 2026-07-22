import { useEffect, useState } from 'react'
import { getSystemHealth } from './api'
import type { DashboardWidgetDefinition, SystemHealth } from './types'

interface DashboardWidgetProps {
  widget: DashboardWidgetDefinition
}

function HealthWidget({ widget }: DashboardWidgetProps) {
  const [health, setHealth] = useState<SystemHealth | null>(null)
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    const controller = new AbortController()

    getSystemHealth(widget.dataEndpoint, controller.signal)
      .then(setHealth)
      .catch((error: unknown) => {
        if (error instanceof Error && error.name !== 'AbortError') {
          setFailed(true)
        }
      })

    return () => controller.abort()
  }, [widget.dataEndpoint])

  return (
    <article className="widget" aria-labelledby={`${widget.id}-title`}>
      <p className="eyebrow">System module</p>
      <h2 id={`${widget.id}-title`}>{widget.title}</h2>
      {failed ? (
        <p className="status status--error">Unavailable</p>
      ) : (
        <p className="status" aria-live="polite">
          <span className="status__dot" aria-hidden="true" />
          {health?.status ?? 'Checking…'}
        </p>
      )}
    </article>
  )
}

export function DashboardWidget({ widget }: DashboardWidgetProps) {
  if (widget.kind === 'health') {
    return <HealthWidget widget={widget} />
  }

  return null
}

