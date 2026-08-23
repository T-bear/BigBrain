import { useEffect, useRef, useState } from 'react'
import { ThemeControl } from '../ThemeControl'
import type { DashboardRegistry, DashboardViewId, WidgetDefinition } from './widgetFramework'
import { useWidgets } from './widgetFramework'
import { FamilyExperience } from '../family/FamilyExperience'

function WidgetLibrary({ onClose, view }: { onClose: () => void; view: DashboardViewId }) {
  const { preferences, registry, setVisible } = useWidgets()
  const dialogRef = useRef<HTMLDivElement>(null)
  const widgets = registry.getForView(view)
  const hidden = preferences.views[view]?.hidden ?? []

  useEffect(() => {
    const dialog = dialogRef.current
    const focusable = dialog?.querySelectorAll<HTMLElement>('button, input')
    focusable?.[0]?.focus()
    const trap = (event: KeyboardEvent) => {
      if (event.key !== 'Tab' || !focusable?.length) return
      const first = focusable[0]
      const last = focusable[focusable.length - 1]
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus() }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus() }
    }
    dialog?.addEventListener('keydown', trap)
    return () => dialog?.removeEventListener('keydown', trap)
  }, [])

  return <div className="widget-library-backdrop" onMouseDown={event => { if (event.target === event.currentTarget) onClose() }}>
    <div aria-labelledby="widget-library-title" aria-modal="true" className="widget-library" onKeyDown={event => { if (event.key === 'Escape') onClose() }} ref={dialogRef} role="dialog">
      <header><div><p className="eyebrow">Widgetbibliotek</p><h2 id="widget-library-title">Visa widgets</h2></div><button aria-label="Stäng widgetbibliotek" className="secondary-button" onClick={onClose} type="button">×</button></header>
      <p className="muted">Ändringen sparas lokalt på den här enheten. Ingen widgetdata raderas.</p>
      <div className="widget-library__list">
        {widgets.map(widget => <label key={widget.id}>
          <input checked={!hidden.includes(widget.id)} onChange={event => setVisible(view, widget.id, event.target.checked)} type="checkbox" />
          <span aria-hidden="true">{widget.icon}</span><span><strong>{widget.title}</strong><small>{widget.description}</small></span>
        </label>)}
      </div>
      <button className="primary-button" onClick={onClose} type="button">Klar</button>
    </div>
  </div>
}

function WidgetFrame({ definition, editMode, index, total, view }: { definition: WidgetDefinition; editMode: boolean; index: number; total: number; view: DashboardViewId }) {
  const { moveWidget, moveWidgetTo, preferences, setVisible, toggleCollapsed } = useWidgets()
  const collapsed = preferences.views[view]?.collapsed.includes(definition.id) ?? false

  return <section
    aria-labelledby={`${definition.id}-widget-title`}
    className={`dashboard-widget dashboard-widget--${definition.defaultSize}${editMode ? ' dashboard-widget--editing' : ''}`}
    data-widget-id={definition.id}
    draggable={editMode}
    onDragOver={event => { if (editMode) event.preventDefault() }}
    onDrop={event => { if (editMode) moveWidgetTo(view, event.dataTransfer.getData('text/widget-id'), definition.id) }}
    onDragStart={event => event.dataTransfer.setData('text/widget-id', definition.id)}
  >
    <header className="dashboard-widget__header">
      <div className="dashboard-widget__identity"><span aria-hidden="true">{definition.icon}</span><div><p>{definition.category}</p><h2 id={`${definition.id}-widget-title`}>{definition.title}</h2></div></div>
      <div className="dashboard-widget__actions">
        {editMode && <>
          <button aria-label={`Flytta ${definition.title} upp`} disabled={index === 0} onClick={() => moveWidget(view, definition.id, -1)} type="button">↑</button>
          <button aria-label={`Flytta ${definition.title} ned`} disabled={index === total - 1} onClick={() => moveWidget(view, definition.id, 1)} type="button">↓</button>
          <button aria-label={`Dölj ${definition.title}`} onClick={() => setVisible(view, definition.id, false)} type="button">Dölj</button>
        </>}
        <button aria-controls={`${definition.id}-widget-content`} aria-expanded={!collapsed} aria-label={`${collapsed ? 'Expandera' : 'Minimera'} ${definition.title}`} onClick={() => toggleCollapsed(view, definition.id)} type="button">⌄</button>
      </div>
    </header>
    <div className="dashboard-widget__content" hidden={collapsed} id={`${definition.id}-widget-content`}>{definition.render({ expanded: !collapsed })}</div>
  </section>
}

export function DashboardWorkspace({ dashboards }: { dashboards: DashboardRegistry }) {
  const { activeView, preferences, registry } = useWidgets()
  const [editMode, setEditMode] = useState(false)
  const [libraryOpen, setLibraryOpen] = useState(false)
  const [settingsOpen, setSettingsOpen] = useState(false)
  const settingsButtonRef = useRef<HTMLButtonElement>(null)
  const settingsRef = useRef<HTMLDivElement>(null)
  const dashboard = dashboards.get(activeView)
  const viewPreferences = preferences.views[activeView] ?? { order: [], hidden: [], collapsed: [] }
  const available = registry.getForView(activeView)
  const ordered = [
    ...viewPreferences.order.map(id => registry.get(id)).filter((widget): widget is WidgetDefinition => Boolean(widget) && widget!.supportedViews.includes(activeView)),
    ...available.filter(widget => widget.defaultView === activeView && !viewPreferences.order.includes(widget.id)),
  ].filter(widget => !viewPreferences.hidden.includes(widget.id))

  useEffect(() => {
    if (!settingsOpen) return
    settingsRef.current?.querySelector<HTMLElement>('select, button')?.focus()
    const close = (event: KeyboardEvent | MouseEvent) => {
      if (event instanceof KeyboardEvent && event.key !== 'Escape') return
      if (event instanceof MouseEvent && (settingsRef.current?.contains(event.target as Node) || settingsButtonRef.current?.contains(event.target as Node))) return
      setSettingsOpen(false)
      window.setTimeout(() => settingsButtonRef.current?.focus(), 0)
    }
    document.addEventListener('keydown', close)
    document.addEventListener('mousedown', close)
    return () => { document.removeEventListener('keydown', close); document.removeEventListener('mousedown', close) }
  }, [settingsOpen])

  const settings = settingsOpen && <div aria-label="Familjeinställningar" className="dashboard-settings family-settings" ref={settingsRef} role="dialog">
    <ThemeControl />
    <button aria-pressed={editMode} className="secondary-button" onClick={() => setEditMode(current => !current)} type="button">{editMode ? 'Avsluta redigering' : 'Aktivera redigeringsläge'}</button>
    <button className="secondary-button" onClick={() => { setSettingsOpen(false); setLibraryOpen(true) }} type="button">Öppna widgetbibliotek</button>
  </div>

  if (activeView === 'family' && !editMode) return <>
    <FamilyExperience onOpenSettings={() => setSettingsOpen(current => !current)} settings={settings} settingsButtonRef={settingsButtonRef} settingsOpen={settingsOpen} widgets={ordered} />
    {libraryOpen && <WidgetLibrary onClose={() => { setLibraryOpen(false); window.setTimeout(() => settingsButtonRef.current?.focus(), 0) }} view={activeView} />}
  </>

  return <main className="main bb-page dashboard-workspace" id={activeView}>
    <header className="page-header dashboard-workspace__header">
      <div><p className="eyebrow">{dashboard.description}</p><h1>{dashboard.title}</h1></div>
      <div className="dashboard-workspace__actions">
        <button aria-expanded={settingsOpen} aria-haspopup="dialog" aria-label="Dashboardinställningar" className="secondary-button dashboard-settings__trigger" onClick={() => setSettingsOpen(current => !current)} ref={settingsButtonRef} type="button"><span aria-hidden="true">⚙</span><span>Dashboardinställningar</span></button>
        {settingsOpen && <div aria-label="Dashboardinställningar" className="dashboard-settings" ref={settingsRef} role="dialog">
          <ThemeControl />
          <button aria-pressed={editMode} className="secondary-button" onClick={() => setEditMode(current => !current)} type="button">{editMode ? 'Avsluta redigering' : 'Aktivera redigeringsläge'}</button>
          <button className="secondary-button" onClick={() => { setSettingsOpen(false); setLibraryOpen(true) }} type="button">Öppna widgetbibliotek</button>
        </div>}
      </div>
    </header>
    {editMode && <p aria-live="polite" className="notice">Redigeringsläge är aktivt. Dra widgets eller använd pilknapparna för att ändra ordning.</p>}
    <div className="dashboard-widget-grid">
      {ordered.map((widget, index) => <WidgetFrame definition={widget} editMode={editMode} index={index} key={widget.id} total={ordered.length} view={activeView} />)}
    </div>
    {ordered.length === 0 && <section className="empty-state"><h2>Inga widgets visas</h2><p>Öppna widgetbiblioteket för att lägga till widgets i den här vyn.</p><button className="primary-button" onClick={() => setLibraryOpen(true)} type="button">Öppna widgetbiblioteket</button></section>}
    {libraryOpen && <WidgetLibrary onClose={() => { setLibraryOpen(false); window.setTimeout(() => settingsButtonRef.current?.focus(), 0) }} view={activeView} />}
  </main>
}
