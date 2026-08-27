import { forwardRef, useEffect, useState, type ButtonHTMLAttributes, type HTMLAttributes, type ImgHTMLAttributes, type InputHTMLAttributes, type PropsWithChildren, type SelectHTMLAttributes } from 'react'
import type { DockerContainer } from './types'

export function StatusBadge({ status, compact = false }: { status: string; compact?: boolean }) {
  const normalized = status.toLowerCase()
  return <span className={`bb-badge status-badge status-badge--${normalized} ${compact ? 'status-badge--compact' : ''}`}>{status}</span>
}

export type BBButtonVariant = 'primary' | 'secondary' | 'tertiary' | 'danger' | 'icon' | 'contextual'

export const BBButton = forwardRef<HTMLButtonElement, PropsWithChildren<ButtonHTMLAttributes<HTMLButtonElement> & { variant?: BBButtonVariant; busy?: boolean }>>(function BBButton({ variant = 'secondary', className = '', busy = false, children, disabled, ...props }, ref) {
  const busyLabel=typeof children==='string'?`${children} pågår`:'Åtgärden pågår'
  return <button {...props} aria-busy={busy || undefined} aria-label={busy?busyLabel:props['aria-label']} className={`bb-button bb-button--${variant} ${className}`.trim()} disabled={disabled || busy} ref={ref}><span className={busy?'bb-button__content bb-button__content--busy':'bb-button__content'}>{children}</span>{busy&&<BBLoadingIndicator label={busyLabel} compact/>}</button>
})

export function BBLoadingIndicator({ label = 'Laddar', compact = false }: { label?: string; compact?: boolean }) {
  return <span aria-live="polite" className={`bb-loading-indicator ${compact?'bb-loading-indicator--compact':''}`} role="status"><span aria-hidden="true" className="bb-loading-indicator__dots"><i/><i/><i/></span><span className="bb-sr-only">{label}</span></span>
}

export function BBMediaArtwork({ src, alt, className = '', ...props }: ImgHTMLAttributes<HTMLImageElement>) {
  const [failed,setFailed]=useState(!src)
  useEffect(()=>setFailed(!src),[src])
  if(failed)return <div aria-label={alt||'Omslag saknas'} className={`bb-media-placeholder ${className}`.trim()} role="img"><span aria-hidden="true" className="bb-media-placeholder__mark">B</span></div>
  return <img {...props} alt={alt??''} className={className} onError={()=>setFailed(true)} src={src}/>
}

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
