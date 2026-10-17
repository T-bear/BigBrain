import type { MouseEvent } from 'react'

export type MediaControlPage = 'overview' | 'downloads' | 'flow' | 'integrations' | 'import'
export const mediaControlPath = '/media/control'

// Existing anchors remain bookmarks into the selected function, not hidden widgets.
export function mediaControlPage(
  pathname = window.location.pathname,
  hash = window.location.hash,
): MediaControlPage | null {
  const path = pathname.replace(/\/$/, '')
  if (path === mediaControlPath) return 'overview'
  for (const page of ['downloads', 'flow', 'integrations', 'import'] as const)
    if (path === `${mediaControlPath}/${page}`) return page
  if (path === '' || path === '/media') {
    if (['#downloads', '#download-control'].includes(hash)) return 'downloads'
    if (['#queue', '#media-jobs'].includes(hash)) return 'flow'
    if (['#administration', '#media-administration', '#jellyfin-overview'].includes(hash)) return 'integrations'
  }
  return null
}

export function isMediaLocation() {
  return (
    window.location.pathname === '/media' ||
    window.location.pathname.startsWith('/media/') ||
    mediaControlPage() !== null
  )
}

// Use the shell's existing history event; preserve real links and modified-click behavior.
export function followMediaLink(event: MouseEvent<HTMLAnchorElement>) {
  if (event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return
  event.preventDefault()
  window.history.pushState({ ...window.history.state, bigbrainView: 'media' }, '', event.currentTarget.href)
  window.dispatchEvent(new Event('bb:navigation'))
}

export const legacyMediaWidgets = ['downloads', 'media-jobs', 'jellyfin-overview']

export function mediaControlPreferences(
  view: string,
  stored?: { order: string[]; hidden: string[]; collapsed: string[] },
) {
  if (!stored || !['media', 'admin'].includes(view) || stored.order?.includes('media-control')) return stored
  const old = view === 'media' ? legacyMediaWidgets : ['jellyfin-overview']
  const convert = (values: unknown, all: boolean) => {
    if (!Array.isArray(values)) return []
    const include = all ? old.every(id => values.includes(id)) : old.some(id => values.includes(id))
    return [...new Set(values.flatMap(id => (old.includes(id) ? (include ? ['media-control'] : []) : [id])))]
  }
  return {
    order: convert(stored.order, false),
    hidden: convert(stored.hidden, true),
    collapsed: convert(stored.collapsed, true),
  }
}
