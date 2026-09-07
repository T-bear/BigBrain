import { useEffect, useRef, useState } from 'react'
import { getFinanceBacktest, getFinanceBacktests } from '../api'
import type { FinanceBacktestCatalog, FinanceBacktestResult } from '../types'

type ResultRead = {
  runId: string
  status: 'loading' | 'ready' | 'failed'
  result: FinanceBacktestResult | null
}

export function useFinanceBacktestDetails(detailsOpen: boolean) {
  const [backtests, setBacktests] = useState<FinanceBacktestCatalog | null>(null)
  const [selectedRun, setSelectedRun] = useState<string | null>(null)
  const [read, setRead] = useState<ResultRead | null>(null)
  const [retryVersion, setRetryVersion] = useState(0)
  const requestRef = useRef<AbortController | null>(null)

  useEffect(() => {
    if (!detailsOpen) return
    const controller = new AbortController()
    getFinanceBacktests(controller.signal).then(value => {
      if (controller.signal.aborted) return
      setBacktests(value)
      setSelectedRun(value.runs[0]?.runId ?? null)
    }).catch(() => {
      if (!controller.signal.aborted) setBacktests(null)
    })
    return () => controller.abort()
  }, [detailsOpen])

  useEffect(() => {
    if (!selectedRun) return
    const controller = new AbortController()
    requestRef.current = controller
    setRead({ runId: selectedRun, status: 'loading', result: null })
    getFinanceBacktest(selectedRun, controller.signal).then(value => {
      if (controller.signal.aborted) return
      if (value.runId !== selectedRun) throw new Error('Backtest result identity mismatch')
      setRead({ runId: selectedRun, status: 'ready', result: value })
    }).catch(() => {
      if (!controller.signal.aborted) setRead({ runId: selectedRun, status: 'failed', result: null })
    })
    return () => controller.abort()
  }, [selectedRun, retryVersion])

  const selectRun = (runId: string) => {
    if (runId === selectedRun) return
    // Invalidate immediately, including completions before the next effect cleanup.
    requestRef.current?.abort()
    setRead(null)
    setSelectedRun(runId)
  }
  const retry = () => {
    if (!selectedRun || (read?.runId === selectedRun && read.status === 'loading')) return
    requestRef.current?.abort()
    setRead({ runId: selectedRun, status: 'loading', result: null })
    setRetryVersion(value => value + 1)
  }
  const current = read?.runId === selectedRun ? read : null
  return {
    backtests, selectedRun, setSelectedRun: selectRun,
    backtestResult: current?.status === 'ready' ? current.result : null,
    backtestLoading: Boolean(selectedRun) && (!current || current.status === 'loading'),
    backtestFailed: current?.status === 'failed', retryBacktest: retry,
  }
}
