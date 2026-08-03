import { useCallback, useEffect, useRef, useState } from 'react'
import { createSmartShuffleSession, getSmartShuffleDevices, getSmartShuffleOptions, getSmartShuffleSession, skipSmartShuffle, stopSmartShuffle } from '../api'
import type { SmartShuffleDevice, SmartShuffleOptions, SmartShuffleSession } from '../types'

export function SmartShuffle() {
  const [options, setOptions] = useState<SmartShuffleOptions | null>(null)
  const [devices, setDevices] = useState<SmartShuffleDevice[]>([])
  const [selected, setSelected] = useState<string[]>([])
  const [deviceId, setDeviceId] = useState('')
  const [session, setSession] = useState<SmartShuffleSession | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const controller = useRef<AbortController | null>(null)
  const polling = useRef(false)

  const load = useCallback(async () => {
    controller.current?.abort()
    const next = new AbortController()
    controller.current = next
    try {
      const loaded = await getSmartShuffleOptions(next.signal)
      setOptions(loaded)
      if (loaded.enabled) {
        const available = await getSmartShuffleDevices(next.signal)
        setDevices(available)
        if (available.length === 1) setDeviceId(available[0].id)
      }
    } catch (requestError) {
      if (requestError instanceof Error && requestError.name !== 'AbortError') setError('Smart Shuffle kunde inte laddas.')
    }
  }, [])

  useEffect(() => {
    void load()
    return () => controller.current?.abort()
  }, [load])

  useEffect(() => {
    if (!session || session.status !== 'active') return
    const poll = async () => {
      if (document.visibilityState !== 'visible' || polling.current) return
      polling.current = true
      try { setSession(await getSmartShuffleSession(session.id)) }
      catch { setError('Shuffle-sessionens status kunde inte uppdateras.') }
      finally { polling.current = false }
    }
    const interval = window.setInterval(() => void poll(), 10_000)
    const visible = () => { if (document.visibilityState === 'visible') void poll() }
    document.addEventListener('visibilitychange', visible)
    return () => { window.clearInterval(interval); document.removeEventListener('visibilitychange', visible) }
  }, [session?.id, session?.status])

  const act = async (request: (signal: AbortSignal) => Promise<SmartShuffleSession>) => {
    if (busy) return
    setBusy(true); setError('')
    const requestController = new AbortController()
    controller.current = requestController
    try { setSession(await request(requestController.signal)) }
    catch { setError('Jellyfin kunde inte utföra Smart Shuffle-åtgärden.') }
    finally { if (!requestController.signal.aborted) setBusy(false) }
  }

  if (!options) return <section className="smart-shuffle" aria-live="polite"><h3>Smart Shuffle</h3><p>Laddar…</p></section>
  if (!options.enabled) return <section className="smart-shuffle"><h3>Smart Shuffle</h3><p>Smart Shuffle är inte aktiverat.</p></section>

  return <section className="smart-shuffle" aria-labelledby="smart-shuffle-heading">
    <h3 id="smart-shuffle-heading">Smart Shuffle</h3>
    {error && <p role="alert" className="notice notice--error">{error}</p>}
    {session ? <div aria-live="polite">
      <p><strong>Status:</strong> {session.status}</p>
      {session.nowPlaying && <p><strong>Spelas nu:</strong> {session.nowPlaying.seriesName} – S{session.nowPlaying.seasonNumber}E{session.nowPlaying.episodeNumber} {session.nowPlaying.title}</p>}
      <p><strong>TV:</strong> {session.deviceName}</p>
      <div className="smart-shuffle__actions">
        <button type="button" disabled={busy || session.status !== 'active'} onClick={() => void act(signal => skipSmartShuffle(session.id, signal))}>Hoppa till nästa</button>
        <button type="button" disabled={busy || session.status !== 'active'} onClick={() => void act(signal => stopSmartShuffle(session.id, signal))}>Stoppa Smart Shuffle</button>
      </div>
    </div> : <>
      <fieldset><legend>Välj minst två serier ({selected.length} valda)</legend>
        <div className="smart-shuffle__series">{options.series.map(series => <label key={series.id}>
          <input type="checkbox" checked={selected.includes(series.id)} disabled={!series.hasPlayableEpisode || busy}
            onChange={event => setSelected(current => event.target.checked ? [...current, series.id] : current.filter(id => id !== series.id))} />
          {series.name}{!series.hasPlayableEpisode ? ' – saknar osedda avsnitt' : ''}
        </label>)}</div>
      </fieldset>
      <label>TV-enhet<select value={deviceId} disabled={busy} onChange={event => setDeviceId(event.target.value)}>
        <option value="">Välj TV</option>{devices.map(device => <option key={device.id} value={device.id}>{device.displayName} ({device.clientType})</option>)}
      </select></label>
      {devices.length === 0 && <p>Ingen styrbar TV hittades. Öppna Jellyfin på TV:n och försök igen.</p>}
      <button type="button" disabled={busy || selected.length < 2 || !deviceId} onClick={() => void act(signal => createSmartShuffleSession(selected, deviceId, signal))}>
        {busy ? 'Startar…' : 'Starta på TV'}
      </button>
    </>}
  </section>
}
