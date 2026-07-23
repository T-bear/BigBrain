import type { PropsWithChildren } from 'react'
import type { DockerContainer } from './types'

export function StatusBadge({ status, compact = false }: { status: string; compact?: boolean }) {
  const normalized = status.toLowerCase()
  return <span className={`status-badge status-badge--${normalized} ${compact ? 'status-badge--compact' : ''}`}>{status}</span>
}

export function MetricCard({ label, value }: { label: string; value: string }) {
  return (
    <article className="card metric-card">
      <h3>{label}</h3>
      <p className="metric-value">{value}</p>
    </article>
  )
}

export function ProgressMetric({ label, value, detail }: { label: string; value: number | null; detail: string }) {
  const safeValue = value === null ? 0 : Math.min(100, Math.max(0, value))
  return (
    <article className="card metric-card">
      <div className="metric-header">
        <h3>{label}</h3>
        <strong>{value === null ? 'Unavailable' : `${safeValue.toFixed(1)}%`}</strong>
      </div>
      <progress aria-label={label} max="100" value={safeValue} />
      <p className="metric-detail">{detail}</p>
    </article>
  )
}

export function ModuleCard({ title, status, children }: PropsWithChildren<{ title: string; status: string }>) {
  return (
    <article className="card module-card">
      <div className="metric-header">
        <h3>{title}</h3>
        <StatusBadge status={status} />
      </div>
      {children}
    </article>
  )
}

export function DockerContainerList({ containers }: { containers: DockerContainer[] }) {
  if (containers.length === 0) return <p>No containers reported.</p>
  return (
    <ul className="container-list">
      {containers.map((container) => (
        <li className="card" key={container.id}>
          <strong>{container.name}</strong>
          <span>{container.image}</span>
          <StatusBadge status={container.state} />
        </li>
      ))}
    </ul>
  )
}
