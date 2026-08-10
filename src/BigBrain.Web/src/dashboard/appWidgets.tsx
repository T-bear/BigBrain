import type { DockerInventory, ModuleDefinition, SystemOverview } from '../types'
import { DockerContainerList, MetricCard, ModuleCard, ProgressMetric, StatusBadge } from '../components'
import { DownloadControl } from '../download-control/DownloadControl'
import { MealPlanner } from '../meal-planner/MealPlanner'
import { MediaDashboard } from '../MediaDashboard'
import { MediaJobs } from '../media-jobs/MediaJobs'
import { MediaSearch } from '../media-search/MediaSearch'
import { ShoppingList } from '../shopping-list/ShoppingList'
import { SmartShuffle } from '../smart-shuffle/SmartShuffle'
import { CalendarWidget } from '../calendar/Calendar'
import { ApplicationWidgetRegistry, DashboardRegistry, type WidgetDefinition } from './widgetFramework'

export interface AppWidgetData {
  docker: DockerInventory | null
  dockerError: boolean
  moduleError: boolean
  modules: ModuleDefinition[]
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
  { id: 'home', title: 'Hem', description: 'Familjens dagliga översikt', icon: '⌂' },
  { id: 'media', title: 'Media', description: 'Sök, spela och hantera media', icon: '▶' },
  { id: 'ai', title: 'AI', description: 'Assistenter och automation', icon: '✦' },
  { id: 'admin', title: 'Admin', description: 'System, tjänster och drift', icon: '⚙' },
])

export function createAppWidgetRegistry(data: AppWidgetData) {
  const planned = (id: string, title: string, description: string, icon: string, defaultView: 'home' | 'ai'): WidgetDefinition => ({
    id, title, description, icon, category: defaultView === 'home' ? 'Familj' : 'AI', defaultView, defaultSize: 'medium', minimumSize: 'small', supportedViews: [defaultView], permissions: [], render: () => <PlannedWidget text={description} />,
  })
  return new ApplicationWidgetRegistry([
    { id: 'meal-plan', title: 'Matlista', description: 'Planera familjens måltider.', icon: '◫', category: 'Familj', defaultView: 'home', defaultSize: 'large', minimumSize: 'medium', supportedViews: ['home'], permissions: [], render: () => <MealPlanner expanded onToggle={() => undefined} status={data.modules.find(module => module.id === 'meal-planner')?.status ?? (data.moduleError ? 'Unavailable' : 'Loading')} /> },
    { id: 'shopping-list', title: 'Inköpslista', description: 'Familjens aktiva inköpslista.', icon: '✓', category: 'Familj', defaultView: 'home', defaultSize: 'large', minimumSize: 'medium', supportedViews: ['home'], permissions: [], render: () => <ShoppingList expanded onToggle={() => undefined} status={data.modules.find(module => module.id === 'shopping-list')?.status ?? (data.moduleError ? 'Unavailable' : 'Loading')} /> },
    { id: 'calendar', title: 'Kalender', description: 'Veckans arbetsschema och säker Heroma-import.', icon: '▦', category: 'Familj', defaultView: 'home', defaultSize: 'large', minimumSize: 'medium', supportedViews: ['home'], permissions: ['calendar.events.read'], render: () => <CalendarWidget /> },
    planned('reminders', 'Påminnelser', 'Visa familjens viktigaste påminnelser.', '◉', 'home'),
    { id: 'media-search', title: 'Mediesökning', description: 'Hitta filmer och serier.', icon: '⌕', category: 'Media', defaultView: 'media', defaultSize: 'full', minimumSize: 'medium', supportedViews: ['media'], permissions: [], render: () => <MediaSearch /> },
    { id: 'downloads', title: 'Nedladdningskö', description: 'Hantera aktiva nedladdningar.', icon: '⇣', category: 'Media', defaultView: 'media', defaultSize: 'large', minimumSize: 'medium', supportedViews: ['media'], permissions: [], render: () => <DownloadControl /> },
    { id: 'smart-shuffle', title: 'Smart Shuffle', description: 'Starta en rättvis serieblandning på TV:n.', icon: '⤨', category: 'Media', defaultView: 'media', defaultSize: 'large', minimumSize: 'medium', supportedViews: ['media'], permissions: [], render: () => <SmartShuffle /> },
    { id: 'media-jobs', title: 'Medieflöde', description: 'Följ film och serier från sökning till bibliotek.', icon: '↧', category: 'Media', defaultView: 'media', defaultSize: 'large', minimumSize: 'medium', supportedViews: ['media'], permissions: [], render: () => <MediaJobs showHeading={false} /> },
    { id: 'jellyfin-overview', title: 'Jellyfin och integrationer', description: 'Bibliotek, tjänster, köer och nyligen tillagt.', icon: '▶', category: 'Media', defaultView: 'media', defaultSize: 'full', minimumSize: 'large', supportedViews: ['media', 'admin'], permissions: [], render: () => <MediaDashboard administrationOnly /> },
    planned('ai-chat', 'AI-chatt', 'Samtala med BigBrains framtida AI-assistent.', '✦', 'ai'),
    planned('agents', 'Agenter', 'Överblick över framtida AI-agenter.', '◎', 'ai'),
    planned('voice-assistant', 'Röstassistent', 'Röststyr familjens BigBrain.', '◖', 'ai'),
    planned('ai-suggestions', 'AI-förslag', 'Granska förslag innan någon åtgärd utförs.', '◇', 'ai'),
    planned('automations', 'Automationer', 'Hantera framtida godkända automationer.', '↻', 'ai'),
    { id: 'server-status', title: 'Serverstatus', description: 'CPU, minne, lagring och uptime.', icon: '▤', category: 'Administration', defaultView: 'admin', defaultSize: 'full', minimumSize: 'large', supportedViews: ['admin'], permissions: [], render: () => <SystemWidget data={data} /> },
    { id: 'containers', title: 'Containers', description: 'Read-only Docker-inventering.', icon: '⬡', category: 'Administration', defaultView: 'admin', defaultSize: 'large', minimumSize: 'medium', supportedViews: ['admin'], permissions: [], render: () => <DockerWidget data={data} /> },
    { id: 'integrations', title: 'Mediaintegrationer', description: 'Teknisk status för mediatjänster.', icon: '⌁', category: 'Administration', defaultView: 'admin', defaultSize: 'full', minimumSize: 'large', supportedViews: ['admin'], permissions: [], render: () => <MediaDashboard administrationOnly /> },
    { id: 'updates', title: 'Uppdateringar', description: 'Tillgängliga system- och integrationsuppdateringar.', icon: '↑', category: 'Administration', defaultView: 'admin', defaultSize: 'medium', minimumSize: 'small', supportedViews: ['admin'], permissions: [], render: () => <PlannedWidget text="Samlad uppdateringshantering är ännu inte implementerad." /> },
  ])
}
