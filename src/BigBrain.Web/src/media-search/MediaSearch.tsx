import { useEffect, useRef, useState } from 'react'
import { ApiError, lookupMedia, mediaErrorMessage, searchMedia } from '../api'
import type { MediaLookupResponse, MediaLookupResult, MediaSearchResponse } from '../types'
import { MediaSearchForm } from './MediaSearchForm'
import { MediaSearchResults } from './MediaSearchResults'
import { MediaSearchModeSelector, type MediaSearchMode } from './MediaSearchModeSelector'
import { MediaLookupResults } from './MediaLookupResults'
import { MediaRequestDialog } from './MediaRequestDialog'
import { MediaTypeSelector, type MediaTypeSelection } from './MediaTypeSelector'
import { AudiobookSearch } from './AudiobookSearch'
import { AppIcon } from '../AppIcon'

export function MediaSearch({ initialType = 'movie' }: { initialType?: MediaTypeSelection }) {
  const [query, setQuery] = useState('')
  const [result, setResult] = useState<MediaSearchResponse | null>(null)
  const [lookupResult, setLookupResult] = useState<MediaLookupResponse | null>(null)
  const [mode, setMode] = useState<MediaSearchMode>('external')
  const [mediaType, setMediaType] = useState<MediaTypeSelection>(initialType)
  const [requestTarget, setRequestTarget] = useState<MediaLookupResult | null>(null)
  const [returnFocus, setReturnFocus] = useState<HTMLButtonElement | null>(null)
  const [loading, setLoading] = useState(false)
  const [errorCode, setErrorCode] = useState<string | null>(null)
  const [expandedProviders, setExpandedProviders] = useState<string[]>([])
  const [showFab, setShowFab] = useState(false)
  const [fabOpen, setFabOpen] = useState(false)
  const controllerRef = useRef<AbortController | null>(null)
  const searchFormRef = useRef<HTMLDivElement | null>(null)
  const fabRef = useRef<HTMLDivElement | null>(null)
  const fabButtonRef = useRef<HTMLButtonElement | null>(null)

  useEffect(() => () => controllerRef.current?.abort(), [])
  const hasResults = Boolean(
    result?.providers.some(provider => provider.results.length > 0) ||
    lookupResult?.providers.some(provider => provider.results.length > 0),
  )
  useEffect(() => {
    if (!hasResults) {
      setShowFab(false)
      setFabOpen(false)
      return
    }
    const update = () => setShowFab((searchFormRef.current?.getBoundingClientRect().bottom ?? 100) < 72)
    update()
    window.addEventListener('scroll', update, { passive: true })
    return () => window.removeEventListener('scroll', update)
  }, [hasResults])
  useEffect(() => {
    if (!fabOpen) return
    const closeOutside = (event: PointerEvent) => {
      if (!fabRef.current?.contains(event.target as Node)) setFabOpen(false)
    }
    window.requestAnimationFrame(() => fabRef.current?.querySelector<HTMLButtonElement>('[role="menuitem"]')?.focus())
    const closeEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setFabOpen(false)
        fabButtonRef.current?.focus()
      }
    }
    document.addEventListener('pointerdown', closeOutside)
    document.addEventListener('keydown', closeEscape)
    return () => {
      document.removeEventListener('pointerdown', closeOutside)
      document.removeEventListener('keydown', closeEscape)
    }
  }, [fabOpen])

  async function submit() {
    controllerRef.current?.abort()
    const controller = new AbortController()
    controllerRef.current = controller
    setResult(null)
    setLookupResult(null)
    setLoading(true)
    setErrorCode(null)
    setExpandedProviders([])
    try {
      if (mode === 'libraries') {
        const response = await searchMedia(query, controller.signal)
        if (!controller.signal.aborted)
          setResult({
            ...response,
            providers: response.providers.map(provider => ({
              ...provider,
              results: provider.results.filter(item =>
                mediaType === 'movie'
                  ? item.mediaType === 'movie'
                  : ['series', 'season', 'episode'].includes(item.mediaType),
              ),
            })),
          })
      } else {
        if (mediaType === 'audiobook') return
        const response = await lookupMedia(query, mediaType, controller.signal)
        if (!controller.signal.aborted) setLookupResult(response)
      }
    } catch (error) {
      if (!controller.signal.aborted && (!(error instanceof Error) || error.name !== 'AbortError'))
        setErrorCode(error instanceof ApiError ? error.code : 'unknownError')
    } finally {
      if (!controller.signal.aborted) setLoading(false)
    }
  }

  function resetResults() {
    controllerRef.current?.abort()
    setRequestTarget(null)
    setResult(null)
    setLookupResult(null)
    setErrorCode(null)
    setLoading(false)
    setExpandedProviders([])
    setFabOpen(false)
  }

  function clearSearch() {
    resetResults()
    setQuery('')
  }

  const hiddenLookupCount =
    lookupResult?.providers.reduce(
      (count, provider) =>
        count + (expandedProviders.includes(provider.provider) ? 0 : Math.max(0, provider.results.length - 1)),
      0,
    ) ?? 0
  const hasExpandedResults = expandedProviders.length > 0
  const focusSearch = () => {
    setFabOpen(false)
    searchFormRef.current?.scrollIntoView?.({ behavior: 'smooth', block: 'center' })
    window.setTimeout(() => document.getElementById('media-search-query')?.focus(), 0)
  }

  const controls = (
    <>
      <MediaSearchModeSelector
        mode={mode}
        onChange={next => {
          resetResults()
          setMode(next)
          queueMicrotask(() =>
            document.querySelector<HTMLButtonElement>(`.media-search-modes button[aria-pressed="true"]`)?.focus(),
          )
        }}
      />
      <MediaTypeSelector
        value={mediaType}
        onChange={next => {
          resetResults()
          setMediaType(next)
          queueMicrotask(() =>
            document.querySelector<HTMLButtonElement>(`.media-type-selector button[aria-pressed="true"]`)?.focus(),
          )
        }}
      />
    </>
  )

  return (
    <section className="media-search" aria-labelledby="media-search-heading">
      <div className="media-search-intro">
        <p className="eyebrow">Mediasökning</p>
        <h3 id="media-search-heading">Hitta film, serier och ljudböcker</h3>
        <p>Sök efter något nytt eller kontrollera det som redan finns.</p>
      </div>
      <div className="media-search-form-anchor" ref={searchFormRef}>
        {mediaType === 'audiobook' && (
          <AudiobookSearch key={mode} mode={mode} query={query} onQueryChange={setQuery} controls={controls} />
        )}
        {mediaType !== 'audiobook' && (
          <MediaSearchForm
            loading={loading}
            query={query}
            showClear={query.length > 0 || hasResults || Boolean(errorCode)}
            onQueryChange={setQuery}
            onClear={clearSearch}
            onSubmit={() => void submit()}
          />
        )}
      </div>
      {mediaType !== 'audiobook' && controls}
      {errorCode && (
        <p className="notice notice--error" role="alert">
          {mediaErrorMessage(errorCode)}
        </p>
      )}
      {!loading && mode === 'libraries' && result && <MediaSearchResults response={result} />}
      {!loading && mode === 'external' && lookupResult && (
        <MediaLookupResults
          response={lookupResult}
          expandedProviders={expandedProviders}
          onExpandedProvidersChange={setExpandedProviders}
          onPrepare={(target, trigger) => {
            setRequestTarget(target)
            setReturnFocus(trigger)
          }}
        />
      )}
      {showFab && (
        <div className="media-search-fab" ref={fabRef}>
          <div
            aria-label="Sökåtgärder"
            className="media-search-fab__menu"
            hidden={!fabOpen}
            id="media-search-fab-menu"
            role="menu"
          >
            <button onClick={focusSearch} role="menuitem" type="button">
              Till sökfältet
            </button>
            <button
              onClick={() => {
                clearSearch()
                window.setTimeout(() => document.getElementById('media-search-query')?.focus(), 0)
              }}
              role="menuitem"
              type="button"
            >
              Rensa sökning
            </button>
            {lookupResult && hasExpandedResults && (
              <button
                onClick={() => {
                  setExpandedProviders([])
                  setFabOpen(false)
                }}
                role="menuitem"
                type="button"
              >
                Visa färre
              </button>
            )}
            {lookupResult && !hasExpandedResults && hiddenLookupCount > 0 && (
              <button
                onClick={() => {
                  setExpandedProviders(
                    lookupResult.providers
                      .filter(provider => provider.results.length > 1)
                      .map(provider => provider.provider),
                  )
                  setFabOpen(false)
                }}
                role="menuitem"
                type="button"
              >
                Visa {hiddenLookupCount} fler träffar
              </button>
            )}
          </div>
          <button
            aria-controls="media-search-fab-menu"
            aria-expanded={fabOpen}
            aria-label="Sökåtgärder"
            className="media-search-fab__button"
            onClick={() => setFabOpen(open => !open)}
            ref={fabButtonRef}
            type="button"
          >
            <AppIcon name="search" />
            <span>Åtgärder</span>
          </button>
        </div>
      )}
      {requestTarget && (
        <MediaRequestDialog result={requestTarget} returnFocus={returnFocus} onClose={() => setRequestTarget(null)} />
      )}
    </section>
  )
}
