import { expect, test } from 'vitest'

// Expected-red hosted PREDEPLOY transport proof. Never merge this fixture.
test('PREDEPLOY hosted pilot preserves a visible red functional result', () => {
  expect('intentional-red-hosted-pilot').toBe('not-a-passing-functional-test')
})
