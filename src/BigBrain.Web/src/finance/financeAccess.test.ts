import { afterEach, expect, test } from 'vitest'
import { financeAccessHeaders, getFinanceAccessToken, setFinanceAccessToken } from './financeAccess'

afterEach(() => setFinanceAccessToken(''))

test('does not send Finance token to Media, modules or arbitrary URLs', () => {
  setFinanceAccessToken('a'.repeat(64))
  expect(financeAccessHeaders('/api/v1/modules/finance/risk/status')).toEqual({
    Authorization: `Bearer ${'a'.repeat(64)}`,
  })
  expect(financeAccessHeaders('/api/v1/modules/media')).toEqual({})
  expect(financeAccessHeaders('/api/v1/modules')).toEqual({})
  expect(financeAccessHeaders('https://example.invalid/api/v1/modules/finance')).toEqual({})
})

test('locking Finance removes the credential from subsequent requests', () => {
  setFinanceAccessToken('b'.repeat(64))
  expect(getFinanceAccessToken()).toHaveLength(64)
  setFinanceAccessToken('')
  expect(getFinanceAccessToken()).toBe('')
  expect(financeAccessHeaders('/api/v1/modules/finance/overview')).toEqual({})
})
