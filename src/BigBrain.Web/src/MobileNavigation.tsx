import { useEffect, useState } from 'react'

const items = [
  { id: 'home', label: 'Hem', icon: '⌂' },
  { id: 'search', label: 'Sök', icon: '⌕' },
  { id: 'queue', label: 'Kö', icon: '↧' },
  { id: 'services', label: 'Tjänster', icon: '▦' },
]

export function MobileNavigation() {
  const [active, setActive] = useState(() => window.location.hash.slice(1) || 'home')

  useEffect(() => {
    const update = () => setActive(window.location.hash.slice(1) || 'home')
    window.addEventListener('hashchange', update)
    return () => window.removeEventListener('hashchange', update)
  }, [])

  return <nav className="mobile-navigation" aria-label="Snabbnavigation">
    {items.map(item => <a key={item.id} href={`#${item.id}`} aria-current={active === item.id ? 'page' : undefined}>
      <span aria-hidden="true">{item.icon}</span>{item.label}
    </a>)}
  </nav>
}
