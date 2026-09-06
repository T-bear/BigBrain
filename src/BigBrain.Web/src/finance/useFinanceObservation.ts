import { useCallback, useEffect, useRef, useState } from 'react'
import { getFinanceObservation } from '../api'
import type { FinanceObservationSnapshot } from '../types'
import { readFinanceSnapshotCache, writeFinanceSnapshotCache } from './financeSnapshotCache'

export function useFinanceObservation(initialSnapshot?: FinanceObservationSnapshot) {
  const [cachedEntry] = useState(() => initialSnapshot ? null : readFinanceSnapshotCache())
  const [snapshot, setSnapshot] = useState<FinanceObservationSnapshot | null>(initialSnapshot ?? cachedEntry?.snapshot ?? null)
  const [failed, setFailed] = useState(false)
  const [refreshing,setRefreshing]=useState(false)
  const [stale,setStale]=useState(Boolean(cachedEntry))
  const [lastFetchedAt,setLastFetchedAt]=useState<string|null>(cachedEntry?.fetchedAtUtc??null)
  const [selected, setSelected] = useState<string | null>((initialSnapshot??cachedEntry?.snapshot)?.watchlist.find(item => item.price !== null)?.instrumentId ?? null)
  const requestRef=useRef<Promise<void>|null>(null)
  const controllerRef=useRef<AbortController|null>(null)
  const refresh=useCallback(()=>{
    if(initialSnapshot)return Promise.resolve()
    if(requestRef.current)return requestRef.current
    const controller=new AbortController();controllerRef.current=controller;setRefreshing(true)
    const request=getFinanceObservation(controller.signal).then(value=>{
      const fetchedAt=new Date().toISOString();setSnapshot(value);setSelected(current=>value.watchlist.some(item=>item.instrumentId===current)?current:value.watchlist.find(item=>item.price!==null)?.instrumentId??null)
      writeFinanceSnapshotCache(value,fetchedAt);setLastFetchedAt(fetchedAt);setFailed(false);setStale(false)
    }).catch(error=>{if(!(error instanceof Error)||error.name!=='AbortError'){setFailed(true);setStale(true)}}).finally(()=>{if(!controller.signal.aborted)setRefreshing(false);if(requestRef.current===request)requestRef.current=null})
    requestRef.current=request;return request
  },[initialSnapshot])
  useEffect(()=>{
    if(initialSnapshot)return
    void refresh()
    const visible=()=>{if(document.visibilityState==='visible')void refresh()}
    const online=()=>void refresh()
    document.addEventListener('visibilitychange',visible);window.addEventListener('online',online)
    return()=>{document.removeEventListener('visibilitychange',visible);window.removeEventListener('online',online);controllerRef.current?.abort();requestRef.current=null}
  },[initialSnapshot,refresh])
  return { snapshot, failed, refreshing, stale, lastFetchedAt, selected, setSelected, refresh }
}
