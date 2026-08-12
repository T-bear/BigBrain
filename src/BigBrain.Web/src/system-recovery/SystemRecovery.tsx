import type { SystemRecoverySnapshot } from '../types'
import { MetricCard, ModuleCard, StatusBadge } from '../components'

const label: Record<string, string> = {
  starting: 'STARTAR', recovering: 'ÅTERSTÄLLER', healthy: 'HEALTHY', degraded: 'DEGRADED',
  quiescing: 'QUIESCING', stopping: 'STOPPING', recoveryRequired: 'RECOVERY REQUIRED',
}

export function SystemRecovery({ recovery, error }: { recovery: SystemRecoverySnapshot | null; error: boolean }) {
  const state = recovery?.overall ?? (error ? 'unavailable' : 'recovering')
  return <div className="system-widget system-recovery">
    <div className="section-actions"><StatusBadge status={label[state] ?? state} /></div>
    {!recovery && !error && <p aria-live="polite">BigBrain återställer tjänster efter omstart…</p>}
    {error && <p className="notice notice--error" role="alert">Recovery-status kunde inte läsas.</p>}
    {recovery && <>
      {recovery.overall === 'recovering' && <p className="notice">BigBrain återställer tjänster efter omstart.</p>}
      <div className="metric-grid">
        <MetricCard label="Föregående avstängning" value={recovery.previousShutdown.toUpperCase()} />
        <MetricCard label="Klocka" value={recovery.clockSynchronized ? 'Synkroniserad' : 'Väntar'} />
        <MetricCard label="Ledigt utrymme" value={recovery.availableBytes === null ? 'Okänt' : `${(recovery.availableBytes / 1024 ** 3).toFixed(1)} GiB`} />
        <MetricCard label="Avbrutna jobb" value={String(recovery.interruptedJobs)} />
      </div>
      <details><summary>Komponenter och återställning</summary><div className="module-grid">
        {recovery.components.map(component => <ModuleCard key={component.id} title={component.id} status={component.state}><p>{component.summary}</p></ModuleCard>)}
      </div></details>
      <p className="last-updated">Boot <code>{recovery.bootId.slice(0, 8)}</code> · Finance {recovery.operatingMode}</p>
    </>}
  </div>
}
