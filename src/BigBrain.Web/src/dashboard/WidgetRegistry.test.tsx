import { render, screen } from '@testing-library/react'
import { expect, test } from 'vitest'
import { DashboardSections } from './DashboardSections'
import { WidgetRegistry } from './WidgetRegistry'

test('renders registered sections and widgets in declared order', () => {
  const registry = new WidgetRegistry<{ value: string }>(
    [
      { id: 'later', title: 'Later', section: 'widgets', order: 20, supportedStates: ['online'], component: ({ data }) => <span>{data.value} later</span> },
      { id: 'first', title: 'First', section: 'widgets', order: 10, supportedStates: ['online'], component: ({ data }) => <span>{data.value} first</span> },
      { id: 'hidden', title: 'Hidden', section: 'hero', order: 10, supportedStates: ['unavailable'], component: () => <span>hidden</span> },
    ],
    [
      { id: 'hero', order: 10, className: 'hero' },
      { id: 'widgets', order: 20, className: 'widgets', title: 'Registered widgets' },
    ])

  const { container } = render(<DashboardSections data={{ value: 'Widget' }} state="online" registry={registry} />)

  expect(screen.getByRole('heading', { name: 'Registered widgets' })).toBeInTheDocument()
  expect(screen.queryByText('hidden')).not.toBeInTheDocument()
  expect([...container.querySelectorAll('.widgets span')].map(element => element.textContent))
    .toEqual(['Widget first', 'Widget later'])
})
