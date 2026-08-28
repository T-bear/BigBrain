import { afterEach, expect, test } from 'vitest'
import { focusRouteHeading } from './routeFocus'

afterEach(() => { document.body.replaceChildren() })

test('marks pointer route focus for visual suppression while retaining DOM focus', () => {
  window.dispatchEvent(new PointerEvent('pointerdown'))
  const heading=document.createElement('h1');heading.tabIndex=-1;document.body.append(heading)
  focusRouteHeading(heading)
  expect(document.activeElement).toBe(heading)
  expect(heading.dataset.bbRouteFocus).toBe('pointer')
})

test('keeps keyboard route focus visibly classified', () => {
  window.dispatchEvent(new KeyboardEvent('keydown',{key:'Tab'}))
  const heading=document.createElement('h1');heading.tabIndex=-1;document.body.append(heading)
  focusRouteHeading(heading)
  expect(document.activeElement).toBe(heading)
  expect(heading.dataset.bbRouteFocus).toBe('keyboard')
})
