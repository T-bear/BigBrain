import type { DockerInventory, ModuleDefinition, SystemOverview, SystemRecoverySnapshot } from '../types'
import { BBButton, BBSurface, DockerContainerList, MetricCard, ModuleCard, ProgressMetric, StatusBadge } from '../components'
import { DownloadControl } from '../download-control/DownloadControl'
import { MealPlanner } from '../meal-planner/MealPlanner'
import { MediaDashboard } from '../MediaDashboard'
import { MediaJobs } from '../media-jobs/MediaJobs'
import { MediaSearch } from '../media-search/MediaSearch'
import { ShoppingList } from '../shopping-list/ShoppingList'
import { SmartShuffle } from '../smart-shuffle/SmartShuffle'
import { CalendarWidget } from '../calendar/Calendar'
import { FinanceObservation } from '../finance/FinanceObservation'
import { SystemRecovery } from '../system-recovery/SystemRecovery'
import { ApplicationWidgetRegistry, DashboardRegistry } from './widgetFramework'
import { AppIcon } from '../AppIcon'
import { useWidgets } from './widgetFramework'
import { ThemeControl } from '../ThemeControl'
import { AudiobookSettings } from '../audiobooks/AudiobookSettings'
import { HomeOverview } from './HomeOverview'

export interface AppWidgetData {
  docker: DockerInventory | null
  dockerError: boolean
  moduleError: boolean
  modules: ModuleDefinition[]
  recovery: SystemRecoverySnapshot | null
  recoveryError: boolean
  system: SystemOverview | null
  systemError: boolean
}

function formatBytes(value: number | null) {
  if (value === null) return 'Unavailable'
  const units = ['B', 'KiB', 'MiB', 'GiB', 'TiB']
  let size = value
  let unit = 0
  while (size >= 1024 && unit < units.length - 1) { size /= 1024; unit += 1 }
  return `${size.toFixed(unit < 2 ? 0 : 1)} ${units[unit]}`
}

function formatUptime(seconds: number | null) {
  if (seconds === null) return 'Unavailable'
  const days = Math.floor(seconds / 86_400)
  const hours = Math.floor((seconds % 86_400) / 3_600)
  const minutes = Math.floor((seconds % 3_600) / 60)
  return `${days ? `${days} ${days === 1 ? 'dag' : 'dagar'} ` : ''}${hours} ${hours === 1 ? 'timme' : 'timmar'} ${minutes} ${minutes === 1 ? 'minut' : 'minuter'}`
}

function PlannedWidget({ text }: { text: string }) {
  return <div className="widget-placeholder"><p>{text}</p><span>Förberedd för en kommande modul</span></div>
}

function AIOverview() {
  const planned = [
    ['AI-chatt', 'Samtal med en framtida deklarerad assistent.'],
    ['Agenter', 'Överblick över framtida auktoriserade agenter.'],
    ['Röstassistent', 'Röststyrning är ännu inte tillgänglig.'],
    ['Förslag och automationer', 'Inga automationer utförs utan deklarerad capability och godkännande.'],
  ]
  return <section aria-labelledby="ai-current-title" className="ai-overview"><header><p className="eyebrow">Nuvarande läge</p><h2 id="ai-current-title">AI-funktioner är planerade</h2><p>BigBrain visar inga kontroller förrän en verklig, auktoriserad capability finns.</p></header><ul>{planned.map(([title, detail]) => <li key={title}><strong>{title}</strong><span>{detail}</span></li>)}</ul></section>
}

function SystemWidget({ data }: { data: AppWidgetData }) {
  const systemStatus = data.system?.status ?? (data.systemError ? 'Error' : 'Loading')
  return <div className="system-widget">
    <StatusBadge status={systemStatus} />
    {!data.system && !data.systemError && <p aria-live="polite">Loading system metrics…</p>}
    {data.systemError && <p className="notice notice--error" role="alert">System metrics could not be refreshed.{data.system ? ' Showing the latest successful update.' : ''}</p>}
    {data.system?.status.toLowerCase() === 'unavailable' && <ModuleCard title="Host metrics not connected" status="Unavailable"><p>{data.system.warnings[0] ?? 'Host metrics are unavailable.'}</p></ModuleCard>}
    {data.system && <><div className="metric-grid">
      <ProgressMetric detail={`${data.system.cpu.logicalProcessorCount} logical processors`} label="CPU usage" value={data.system.cpu.usagePercent} />
      <ProgressMetric detail={`${formatBytes(data.system.memory.usedBytes)} of ${formatBytes(data.system.memory.totalBytes)}`} label="RAM usage" value={data.system.memory.usagePercent} />
      {data.system.disks.map(disk => <ProgressMetric detail={`${formatBytes(disk.usedBytes)} used of ${formatBytes(disk.totalBytes)} · ${formatBytes(disk.availableBytes)} free`} key={disk.filesystemId} label={disk.displayName} value={disk.usagePercent} />)}
      <MetricCard label="System uptime" value={formatUptime(data.system.uptimeSeconds)} />
      <MetricCard label="Hostname" value={data.system.hostname} />
      <MetricCard label="Temperature" value={data.system.temperatureCelsius === null ? 'Unavailable' : `${data.system.temperatureCelsius.toFixed(1)} °C`} />
    </div><p className="last-updated">Last updated <time dateTime={data.system.collectedAtUtc}>{new Date(data.system.collectedAtUtc).toLocaleTimeString()}</time></p></>}
  </div>
}

function DockerWidget({ data }: { data: AppWidgetData }) {
  const status = data.dockerError ? 'Error' : data.docker?.availability.available ? 'Available' : data.docker ? 'Unavailable' : 'Loading'
  return <div className="docker-widget"><StatusBadge status={status} />
    {data.dockerError ? <p className="notice notice--error" role="alert">Docker inventory could not be loaded.</p>
      : !data.docker ? <p aria-live="polite">Loading Docker inventory…</p>
        : !data.docker.availability.available ? <ModuleCard title="Integration not connected" status="Unavailable"><p>{data.docker.availability.reason}</p></ModuleCard>
          : <><MetricCard label="Containers" value={String(data.docker.containers.length)} /><DockerContainerList containers={data.docker.containers} /></>}
  </div>
}

export const dashboardRegistry = new DashboardRegistry([
  { id: 'home', title: 'Hem', description: 'Det viktigaste just nu', icon: 'home' },
  { id: 'family', title: 'Familj', description: 'Mat, inköp och kalender', icon: 'family' },
  { id: 'media', title: 'Media', description: 'Sök, spela och hantera media', icon: 'media' },
  { id: 'finance', title: 'Finance', description: 'Marknaden och forskning', icon: 'finance' },
  { id: 'more', title: 'Mer', description: 'Inställningar och verktyg', icon: 'more' },
  { id: 'ai', title: 'BigBrain AI', description: 'Assistenter och automation', icon: 'ai' },
  { id: 'admin', title: 'Admin', description: 'System, tjänster och drift', icon: 'admin' },
])

function MoreNavigation() {
  const { setActiveView } = useWidgets()
  return <div className="more-hub"><div className="module-launcher"><BBButton onClick={() => setActiveView('ai')} type="button" variant="contextual"><AppIcon name="ai" /><span><strong>BigBrain AI</strong><small>Befintliga och planerade AI-funktioner</small></span><AppIcon name="chevron" /></BBButton><BBButton onClick={() => setActiveView('admin')} type="button" variant="contextual"><AppIcon name="admin" /><span><strong>Admin</strong><small>System, recovery och integrationer</small></span><AppIcon name="chevron" /></BBButton></div><BBSurface aria-labelledby="theme-heading" className="settings-surface" as="section"><div><AppIcon name="settings" /><h3 id="theme-heading">Utseende</h3></div><ThemeControl /><AudiobookSettings /></BBSurface></div>
}

export function createAppWidgetRegistry(data: AppWidgetData) {
  return new ApplicationWidgetRegistry([
    { id: 'home-launcher', title: 'Just nu', description: 'Det viktigaste i BigBrain.', icon: '⌂', category: 'Översikt', defaultView: 'home', defaultSize: 'full', minimumSize: 'medium', supportedViews: ['home'], permissions: [], render: () => <HomeOverview recovery={data.recovery} /> },
    { id: 'meal-plan', title: 'Matlista', description: 'Planera familjens måltider.', icon: '◫', category: 'Familj', defaultView: 'family', defaultSize: 'large', minimumSize: 'medium', supportedViews: ['family'], permissions: [], render: () => <MealPlanner expanded onToggle={() => undefined} presentation="family" status={data.modules.find(module => module.id === 'meal-planner')?.status ?? (data.moduleError ? 'Unavailable' : 'Loading')} /> },
    { id: 'shopping-list', title: 'Inköpslista', description: 'Familjens aktiva inköpslista.', icon: '✓', category: 'Familj', defaultView: 'family', defaultSize: 'large', minimumSize: 'medium', supportedViews: ['family'], permissions: [], render: () => <ShoppingList expanded onToggle={() => undefined} presentation="family" status={data.modules.find(module => module.id === 'shopping-list')?.status ?? (data.moduleError ? 'Unavailable' : 'Loading')} /> },
    { id: 'calendar', title: 'Kalender', description: 'Veckans arbetsschema och säker Heroma-import.', icon: '▦', category: 'Familj', defaultView: 'family', defaultSize: 'large', minimumSize: 'medium', supportedViews: ['family'], permissions: ['calendar.events.read'], render: () => <CalendarWidget /> },
    { id: 'reminders', title: 'Påminnelser', description: 'Visa familjens viktigaste påminnelser.', icon: '◉', category: 'Familj', defaultView: 'family', defaultSize: 'medium', minimumSize: 'small', supportedViews: ['family'], permissions: [], render: () => <PlannedWidget text="Visa familjens viktigaste påminnelser." /> },
    { id: 'media-search', title: 'Mediesökning', description: 'Hitta filmer och serier.', icon: '⌕', category: 'Media', defaultView: 'media', defaultSize: 'full', minimumSize: 'medium', supportedViews: ['media'], permissions: [], render: () => <MediaSearch /> },
    { id: 'downloads', title: 'Nedladdningskö', description: 'Hantera aktiva nedladdningar.', icon: '⇣', category: 'Media', defaultView: 'media', defaultSize: 'large', minimumSize: 'medium', supportedViews: ['media'], permissions: [], render: () => <DownloadControl /> },
    { id: 'smart-shuffle', title: 'Smart Shuffle', description: 'Starta en rättvis serieblandning på TV:n.', icon: '⤨', category: 'Media', defaultView: 'media', defaultSize: 'large', minimumSize: 'medium', supportedViews: ['media'], permissions: [], render: () => <SmartShuffle /> },
    { id: 'media-jobs', title: 'Medieflöde', description: 'Följ film och serier från sökning till bibliotek.', icon: '↧', category: 'Media', defaultView: 'media', defaultSize: 'large', minimumSize: 'medium', supportedViews: ['media'], permissions: [], render: () => <MediaJobs showHeading={false} /> },
    { id: 'jellyfin-overview', title: 'Tekniska integrationer', description: 'Jellyfin, Sonarr, Radarr och Prowlarr.', icon: '▶', category: 'Avancerat', defaultView: 'media', defaultSize: 'full', minimumSize: 'large', supportedViews: ['media', 'admin'], permissions: [], render: () => <MediaDashboard administrationOnly administrationOpen={false} /> },
    { id: 'finance-observation', title: 'Finance observation', description: 'Read-only watchlist, historik och entitlementstatus.', icon: '◇', category: 'Finance', defaultView: 'finance', defaultSize: 'full', minimumSize: 'large', supportedViews: ['finance'], permissions: ['finance.research.read'], render: () => <FinanceObservation /> },
    { id: 'ai-overview', title: 'AI i BigBrain', description: 'Nuvarande och planerade capability-gränser.', icon: '✦', category: 'AI', defaultView: 'ai', defaultSize: 'full', minimumSize: 'medium', supportedViews: ['ai'], permissions: [], render: () => <AIOverview /> },
    { id: 'settings', title: 'Inställningar', description: 'Tema och sekundära destinationer.', icon: '⚙', category: 'Inställningar', defaultView: 'more', defaultSize: 'full', minimumSize: 'medium', supportedViews: ['more'], permissions: [], render: () => <MoreNavigation /> },
    { id: 'server-status', title: 'Serverstatus', description: 'CPU, minne, lagring och uptime.', icon: '▤', category: 'Administration', defaultView: 'admin', defaultSize: 'full', minimumSize: 'large', supportedViews: ['admin'], permissions: [], render: () => <SystemWidget data={data} /> },
    { id: 'system-recovery', title: 'Start och återställning', description: 'Boot, clean shutdown, storage och recovery.', icon: '↻', category: 'Administration', defaultView: 'admin', defaultSize: 'full', minimumSize: 'large', supportedViews: ['admin'], permissions: [], render: () => <SystemRecovery recovery={data.recovery} error={data.recoveryError} /> },
    { id: 'containers', title: 'Containers', description: 'Read-only Docker-inventering.', icon: '⬡', category: 'Administration', defaultView: 'admin', defaultSize: 'large', minimumSize: 'medium', supportedViews: ['admin'], permissions: [], render: () => <DockerWidget data={data} /> },
    { id: 'integrations', title: 'Mediaintegrationer', description: 'Teknisk status för mediatjänster.', icon: '⌁', category: 'Administration', defaultView: 'admin', defaultSize: 'full', minimumSize: 'large', supportedViews: ['admin'], permissions: [], render: () => <MediaDashboard administrationOnly /> },
    { id: 'updates', title: 'Uppdateringar', description: 'Tillgängliga system- och integrationsuppdateringar.', icon: '↑', category: 'Administration', defaultView: 'admin', defaultSize: 'medium', minimumSize: 'small', supportedViews: ['admin'], permissions: [], render: () => <PlannedWidget text="Samlad uppdateringshantering är ännu inte implementerad." /> },
  ])
}
