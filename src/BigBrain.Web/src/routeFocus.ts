let modality: 'keyboard' | 'pointer' = 'keyboard'
let listening = false

function listenForModality() {
  if (listening || typeof window === 'undefined') return
  listening = true
  window.addEventListener('keydown', event => {
    if (event.key === 'Tab' || event.key.startsWith('Arrow')) modality = 'keyboard'
  }, true)
  window.addEventListener('pointerdown', () => { modality = 'pointer' }, true)
}

export function focusRouteHeading(heading: HTMLElement | null) {
  if (!heading) return
  listenForModality()
  heading.dataset.bbRouteFocus = modality
  heading.focus({ preventScroll: true })
}

listenForModality()
