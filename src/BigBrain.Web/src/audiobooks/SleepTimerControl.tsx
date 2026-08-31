import { useEffect, useId, useRef, useState } from 'react'
import { AppIcon } from '../AppIcon'
import { BBButton, BBInput } from '../components'

export type SleepTimerValue = 15 | 30 | 45 | 60
type Props = { active?: boolean; available?: boolean; compact?: boolean; label?: string; onActivate?: () => void; onCancel: () => void; onClock: (clock: string) => void; onMinutes: (minutes: SleepTimerValue) => void }

export function SleepTimerControl({ active = false, available = true, compact = false, label = 'Sovtimer', onActivate, onCancel, onClock, onMinutes }: Props) {
  const [open, setOpen] = useState(false)
  const [custom, setCustom] = useState(false)
  const menuId = useId()
  const triggerRef = useRef<HTMLButtonElement>(null)
  const panelRef = useRef<HTMLDivElement>(null)
  const closeRef = useRef<HTMLButtonElement>(null)
  useEffect(() => {
    if (!open) return
    const close = (event: KeyboardEvent | MouseEvent) => {
      if (event instanceof KeyboardEvent && event.key !== 'Escape') return
      if (event instanceof MouseEvent && (panelRef.current?.contains(event.target as Node) || triggerRef.current?.contains(event.target as Node))) return
      setOpen(false)
      if (event instanceof KeyboardEvent) window.setTimeout(() => triggerRef.current?.focus(), 0)
    }
    document.addEventListener('keydown', close); document.addEventListener('mousedown', close)
    return () => { document.removeEventListener('keydown', close); document.removeEventListener('mousedown', close) }
  }, [open])
  useEffect(() => {
    if (open) window.setTimeout(() => closeRef.current?.focus(), 0)
  }, [open])
  const cancel = () => { onCancel(); setCustom(false); setOpen(false) }
  const choose = (minutes: SleepTimerValue) => { onMinutes(minutes); setCustom(false); setOpen(false) }
  return <div className={`audiobook-sleep-timer${compact ? ' audiobook-sleep-timer--compact' : ''}${active ? ' audiobook-sleep-timer--active' : ''}`}>
    <BBButton aria-controls={menuId} aria-expanded={open} aria-haspopup="dialog" aria-label={label} aria-pressed={active} className="audiobook-sleep-timer__toggle" onClick={() => setOpen(value => !value)} ref={triggerRef} type="button" variant="tertiary"><AppIcon name="moon" size={23} />{!compact && <span>Sovtimer</span>}</BBButton>
    {open && <div aria-label="Sovtimeralternativ" className="audiobook-sleep-timer__panel" id={menuId} ref={panelRef} role="dialog">
      <header><strong>Sovtimer</strong><BBButton aria-label="Stäng sovtimer" onClick={() => { setOpen(false); triggerRef.current?.focus() }} ref={closeRef} type="button" variant="icon">×</BBButton></header>
      {available ? <>{!custom && <div className="audiobook-sleep-timer__choices">{([15, 30, 45, 60] as SleepTimerValue[]).map(minutes => <BBButton key={minutes} onClick={() => choose(minutes)} variant="contextual">{minutes} min</BBButton>)}<BBButton onClick={() => setCustom(true)} variant="contextual">Sluttid…</BBButton>{active && <BBButton onClick={cancel} variant="tertiary">Stäng av</BBButton>}</div>}{custom && <div className="audiobook-sleep-timer__custom"><label>Sluttid<BBInput aria-label="Välj lokal sluttid" onChange={event => { if (event.target.value) { onClock(event.target.value); setOpen(false) } }} type="time" /></label><BBButton onClick={() => setCustom(false)} variant="tertiary">Tillbaka</BBButton>{active && <BBButton onClick={cancel} variant="tertiary">Stäng av</BBButton>}</div>}</> : <><p className="audiobook-sleep-timer__hint">Starta uppspelningen för att använda sovtimern.</p>{onActivate && <BBButton onClick={() => { onActivate(); setOpen(false) }} variant="secondary">Starta uppspelningen</BBButton>}</>}
    </div>}
  </div>
}
