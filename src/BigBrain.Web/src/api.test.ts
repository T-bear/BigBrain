import { afterEach, expect, test, vi } from 'vitest'
import { subscribeMediaJobs } from './api'

class FakeEventSource {
  static instances: FakeEventSource[] = []
  onerror: (() => void) | null = null
  readonly listeners = new Map<string, (event: MessageEvent<string>) => void>()
  closed = false

  constructor(public readonly url: string) {
    FakeEventSource.instances.push(this)
  }

  addEventListener(name: string, listener: EventListener) {
    this.listeners.set(name, listener as (event: MessageEvent<string>) => void)
  }

  close() {
    this.closed = true
  }
}

afterEach(() => {
  vi.useRealTimers()
  vi.unstubAllGlobals()
  FakeEventSource.instances = []
})

test('media jobs stream reconnects with bounded backoff and cleans up', () => {
  vi.useFakeTimers()
  vi.stubGlobal('EventSource', FakeEventSource)
  const onJobs = vi.fn()
  const onError = vi.fn()

  const unsubscribe = subscribeMediaJobs(onJobs, onError)
  const first = FakeEventSource.instances[0]
  first.onerror?.()

  expect(first.closed).toBe(true)
  expect(onError).toHaveBeenCalledTimes(1)
  vi.advanceTimersByTime(4_999)
  expect(FakeEventSource.instances).toHaveLength(1)
  vi.advanceTimersByTime(1)
  expect(FakeEventSource.instances).toHaveLength(2)

  const second = FakeEventSource.instances[1]
  second.onerror?.()
  vi.advanceTimersByTime(9_999)
  expect(FakeEventSource.instances).toHaveLength(2)
  vi.advanceTimersByTime(1)
  expect(FakeEventSource.instances).toHaveLength(3)

  unsubscribe()
  expect(FakeEventSource.instances[2].closed).toBe(true)
})
