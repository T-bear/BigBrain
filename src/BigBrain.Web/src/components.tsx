import { forwardRef, type ButtonHTMLAttributes, type HTMLAttributes, type InputHTMLAttributes, type PropsWithChildren, type SelectHTMLAttributes } from 'react'
import type { DockerContainer } from './types'

export function StatusBadge({ status, compact = false }: { status: string; compact?: boolean }) {
  const normalized = status.toLowerCase()
  return <span className={`bb-badge status-badge status-badge--${normalized} ${compact ? 'status-badge--compact' : ''}`}>{status}</span>
}

export type BBButtonVariant = 'primary' | 'secondary' | 'tertiary' | 'danger' | 'icon' | 'contextual'

export const BBButton = forwardRef<HTMLButtonElement, PropsWithChildren<ButtonHTMLAttributes<HTMLButtonElement> & { variant?: BBButtonVariant; busy?: boolean }>>(function BBButton({ variant = 'secondary', className = '', busy = false, children, disabled, ...props }, ref) {
  return <button aria-busy={busy || undefined} className={`bb-button bb-button--${variant} ${className}`.trim()} disabled={disabled || busy} ref={ref} {...props}>{busy ? <><span aria-hidden="true" className="bb-spinner" />Vänta…</> : children}</button>
})

export function BBInput({ className = '', ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return <input className={`bb-input ${className}`.trim()} {...props} />
}

export function BBSelect({ className = '', children, ...props }: PropsWithChildren<SelectHTMLAttributes<HTMLSelectElement>>) {
  return <select className={`bb-select ${className}`.trim()} {...props}>{children}</select>
}

export function BBSurface({ as = 'section', className = '', children, ...props }: PropsWithChildren<HTMLAttributes<HTMLElement> & { as?: 'section' | 'article' | 'div' }>) {
  const Component = as
  return <Component className={`bb-surface ${className}`.trim()} {...props}>{children}</Component>
}

export function BBEmptyState({ title, detail, children }: PropsWithChildren<{ title: string; detail?: string }>) {
  return <div className="bb-empty-state" role="status"><strong>{title}</strong>{detail && <span>{detail}</span>}{children}</div>
}

export function MetricCard({ label, value }: { label: string; value: string }) {
  return (
    <article className="bb-card card metric-card">
      <h3>{label}</h3>
      <p className="metric-value">{value}</p>
    </article>
  )
}

export function ProgressMetric({ label, value, detail }: { label: string; value: number | null; detail: string }) {
  const safeValue = value === null ? 0 : Math.min(100, Math.max(0, value))
  return (
    <article className="bb-card card metric-card">
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
    <article className="bb-card card module-card">
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
        <li className="bb-card bb-list-row card" key={container.id}>
          <strong>{container.name}</strong>
          <span>{container.image}</span>
          <StatusBadge status={container.state} />
        </li>
      ))}
    </ul>
  )
}
