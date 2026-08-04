import { expect, test } from 'vitest'
import { ApplicationWidgetRegistry, readDashboardPreferences, type WidgetDefinition } from './widgetFramework'

function widget(overrides: Partial<WidgetDefinition> = {}): WidgetDefinition {
  return {
    id: 'test-widget', title: 'Test', description: 'Test widget', icon: 'T', category: 'Test',
    defaultView: 'home', defaultSize: 'medium', minimumSize: 'small', supportedViews: ['home'], permissions: [], render: () => null,
    ...overrides,
  }
}

test('registry exposes complete metadata and filters supported views', () => {
  const registry = new ApplicationWidgetRegistry([widget(), widget({ id: 'admin-widget', defaultView: 'admin', supportedViews: ['admin'] })])
  expect(registry.getForView('home').map(entry => entry.id)).toEqual(['test-widget'])
  expect(registry.get('test-widget')).toMatchObject({ defaultSize: 'medium', minimumSize: 'small', permissions: [] })
})

test('registry rejects duplicate ids and invalid default views', () => {
  expect(() => new ApplicationWidgetRegistry([widget(), widget()])).toThrow(/Duplicate widget id/)
  expect(() => new ApplicationWidgetRegistry([widget({ supportedViews: ['media'] })])).toThrow(/does not support its default view/)
})

test('preferences safely fall back and automatically include newly registered defaults', () => {
  const registry = new ApplicationWidgetRegistry([widget(), widget({ id: 'new-widget' })])
  const storage = { getItem: () => JSON.stringify({ version: 2, activeView: 'home', views: { home: { order: ['test-widget'], hidden: [], collapsed: [] } } }) }
  expect(readDashboardPreferences(registry, storage).views.home?.order).toEqual(['test-widget', 'new-widget'])
  expect(readDashboardPreferences(registry, { getItem: () => '{invalid' }).activeView).toBe('home')
})
