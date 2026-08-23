import { AppIcon } from '../AppIcon'
import type { WidgetDefinition } from '../dashboard/widgetFramework'
import type { ReactNode, RefObject } from 'react'

type FamilyExperienceProps = {
  widgets: WidgetDefinition[]
  onOpenSettings: () => void
  settingsOpen: boolean
  settings: ReactNode
  settingsButtonRef: RefObject<HTMLButtonElement | null>
}

const sectionNames: Record<string, { title: string; kicker: string }> = {
  'meal-plan': { title: 'Veckans matsedel', kicker: 'Måltider' },
  'shopping-list': { title: 'Inköpslista', kicker: 'Att handla' },
  calendar: { title: 'Kalender', kicker: 'Familjens vecka' },
  reminders: { title: 'Påminnelser', kicker: 'Framåt' },
}

export function FamilyExperience({ widgets, onOpenSettings, settingsOpen, settings, settingsButtonRef }: FamilyExperienceProps) {
  return <main className="main bb-page family-experience" id="family">
    <header className="family-header">
      <div><p>Familjeöversikt</p><h1>Familj</h1></div>
      <button aria-expanded={settingsOpen} aria-haspopup="dialog" aria-label="Dashboardinställningar" className="family-settings-trigger" onClick={onOpenSettings} ref={settingsButtonRef} type="button"><AppIcon name="settings" size={20} /></button>
      {settings}
    </header>
    <div className="family-context" role="status"><span aria-hidden="true">◇</span><div><strong>Veckan tillsammans</strong><small>Mat, inköp och kalender på samma plats</small></div></div>
    <div className="family-flow">
      {widgets.map(widget => {
        const copy = sectionNames[widget.id] ?? { title: widget.title, kicker: widget.category }
        return <section aria-labelledby={`family-${widget.id}-title`} className={`family-section family-section--${widget.id}`} data-family-section={widget.id} data-widget-id={widget.id} key={widget.id}>
          <header className="family-section__heading"><div><p>{copy.kicker}</p><h2 id={`family-${widget.id}-title`}>{copy.title}</h2></div><span aria-hidden="true">{widget.icon}</span></header>
          <div className="family-section__content">{widget.render({ expanded: true })}</div>
        </section>
      })}
    </div>
  </main>
}
