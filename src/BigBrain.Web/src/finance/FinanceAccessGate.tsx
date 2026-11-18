import { useEffect, useState, type FormEvent, type ReactNode } from 'react'
import { getFinanceAccessToken, setFinanceAccessToken } from './financeAccess'

export function FinanceAccessGate({ children }: { children: ReactNode }) {
  const [unlocked, setUnlocked] = useState(false)
  const [checking, setChecking] = useState(Boolean(getFinanceAccessToken()))
  const [code, setCode] = useState('')
  const [error, setError] = useState('')

  useEffect(() => {
    const token = getFinanceAccessToken()
    if (!token) return
    let active = true
    void fetch('/api/v1/modules/finance/risk/status', {
      headers: { Authorization: `Bearer ${token}` },
      cache: 'no-store',
    }).then(response => {
      if (!active) return
      if (response.ok) setUnlocked(true)
      else { setFinanceAccessToken(''); setError('Ange Finance-åtkomstkoden på nytt.') }
    }).catch(() => {
      if (active) setError('Det går inte att kontrollera Finance just nu.')
    }).finally(() => { if (active) setChecking(false) })
    return () => { active = false }
  }, [])

  async function unlock(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (checking || code.length < 48) { setError('Ange hela åtkomstkoden.'); return }
    setChecking(true)
    setError('')
    try {
      const response = await fetch('/api/v1/modules/finance/risk/status', {
        headers: { Authorization: `Bearer ${code}` },
        cache: 'no-store',
      })
      if (!response.ok) { setError('Åtkomstkoden godkändes inte.'); return }
      setFinanceAccessToken(code)
      setCode('')
      setUnlocked(true)
    } catch {
      setError('Det gick inte att kontakta Finance.')
    } finally {
      setChecking(false)
    }
  }

  if (unlocked)
    return <>{children}<button type="button" onClick={() => { setFinanceAccessToken(''); setUnlocked(false) }}>
      Lås Finance
    </button></>

  return (
    <section className="finance-view" aria-labelledby="finance-access-title">
      <h2 id="finance-access-title">Finance är låst</h2>
      <p>Ange Finance-åtkomstkoden för att öppna dina uppgifter. Övriga delar av BigBrain fungerar som vanligt.</p>
      <form onSubmit={event => void unlock(event)}>
        <label htmlFor="finance-access-code">Finance-åtkomstkod</label>
        <input id="finance-access-code" type="password" value={code}
          autoComplete="off" required minLength={48}
          onChange={event => setCode(event.target.value)} />
        <button type="submit" disabled={checking}>Lås upp Finance</button>
      </form>
      {checking && <p role="status">Kontrollerar åtkomst…</p>}
      {error && <p role="alert">{error}</p>}
    </section>
  )
}
