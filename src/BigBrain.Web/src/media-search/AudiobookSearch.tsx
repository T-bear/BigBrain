import { useEffect, useRef, useState, type ReactNode } from 'react'
import {
  getAudiobookAcquisitionJob,
  getAudiobookAcquisitionJobs,
  getAudiobookLibrary,
  getAudiobookOverview,
  requestAudiobookAcquisition,
  searchAudiobooks,
} from '../api'
import { BBButton, BBEmptyState, BBMediaArtwork, BBSelect, BBSurface } from '../components'
import type {
  AudiobookAcquisitionCandidate,
  AudiobookAcquisitionJob,
  AudiobookItem,
  AudiobookSearchResponse,
} from '../types'
import { audiobookStatusLabel } from '../audiobooks/audiobookStatusLabel'
import { MediaSearchForm } from './MediaSearchForm'
import type { MediaSearchMode } from './MediaSearchModeSelector'

export function AudiobookLibraryResults({ items }: { items: AudiobookItem[] }) {
  return (
    <section aria-label="Finns i Audiobookshelf">
      <h3>Finns i Audiobookshelf</h3>
      {items.map(item => (
        <BBSurface className="audiobook-candidate" key={item.id}>
          <BBMediaArtwork alt={`Omslag till ${item.title}`} loading="lazy" src={item.coverUrl ?? undefined} />
          <div className="audiobook__copy">
            <h4>{item.title}</h4>
            {item.author && <p>{item.author}</p>}
            <p>
              {[item.series, item.narrator && `Uppläsare ${item.narrator}`, item.languageLabel, item.publishedYear]
                .filter(Boolean)
                .join(' · ')}
            </p>
            {item.playbackUrl ? (
              <a className="bb-button bb-button--secondary" href={item.playbackUrl} rel="noreferrer">
                Öppna i Audiobookshelf
              </a>
            ) : (
              <p>Uppspelning sker i Audiobookshelf. Ingen uppspelningslänk är konfigurerad.</p>
            )}
          </div>
        </BBSurface>
      ))}
    </section>
  )
}

// UI composition of the existing versioned catalog / metadata / acquisition contracts.
// Search cancellation never retries an acquisition mutation: a failed POST can have committed.
export function AudiobookSearch({
  mode,
  query,
  onQueryChange,
  controls,
}: {
  mode: MediaSearchMode
  query: string
  onQueryChange: (value: string) => void
  controls?: ReactNode
}) {
  const [language, setLanguage] = useState('sv')
  const [library, setLibrary] = useState<AudiobookItem[] | null>(null)
  const [search, setSearch] = useState<AudiobookSearchResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [notice, setNotice] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [selected, setSelected] = useState<AudiobookAcquisitionCandidate | null>(null)
  const [job, setJob] = useState<AudiobookAcquisitionJob | null>(null)
  const [requesting, setRequesting] = useState(false)
  const [history, setHistory] = useState<AudiobookAcquisitionJob[] | null>(null)
  const [historyLoading, setHistoryLoading] = useState(false)
  const historyController = useRef<AbortController | null>(null)
  const controller = useRef<AbortController | null>(null)
  const mounted = useRef(true)
  const mutation = useRef(false)
  const attempted = useRef(new Set<string>())
  const confirmRef = useRef<HTMLDivElement>(null)
  const trigger = useRef<HTMLButtonElement | null>(null)
  useEffect(() => {
    mounted.current = true
    return () => {
      mounted.current = false
      controller.current?.abort()
      historyController.current?.abort()
    }
  }, [])
  useEffect(() => {
    if (selected) confirmRef.current?.focus()
  }, [selected])

  function reset() {
    controller.current?.abort()
    setLibrary(null)
    setSearch(null)
    setSelected(null)
    setLoading(false)
    setNotice(null)
    setError(null)
  }
  async function submit() {
    if (query.trim().length < 2 || query.trim().length > 120) return
    reset()
    const current = new AbortController()
    controller.current = current
    setLoading(true)
    try {
      if (mode === 'libraries') {
        // The existing library page does not distinguish an unconfigured catalog from no matches.
        const overview = await getAudiobookOverview(current.signal)
        if (current.signal.aborted) return
        if (overview.state !== 'configuredHealthy') {
          setNotice(
            overview.state === 'notConfigured'
              ? 'Audiobookshelf är inte konfigurerat.'
              : 'Audiobookshelf är tillfälligt otillgängligt.',
          )
          return
        }
        const page = await getAudiobookLibrary(0, 50, query, '', current.signal)
        if (!current.signal.aborted) setLibrary(page.items)
      } else {
        const response = await searchAudiobooks(query, language, current.signal)
        if (!current.signal.aborted) {
          setSearch(response)
          setLibrary(response.library)
        }
      }
    } catch {
      if (!current.signal.aborted) setError('Ljudbokssökningen kunde inte genomföras. Försök igen.')
    } finally {
      if (!current.signal.aborted) setLoading(false)
    }
  }
  async function request(candidate: AudiobookAcquisitionCandidate) {
    const identity = `${candidate.source}:${candidate.editionId}`
    if (mutation.current || attempted.current.has(identity) || !search?.acquisition.canRequest) return
    mutation.current = true
    attempted.current.add(identity)
    setRequesting(true)
    setError(null)
    try {
      const result = await requestAudiobookAcquisition(candidate)
      if (mounted.current) {
        setJob(result)
        setSelected(null)
        trigger.current?.focus()
      }
    } catch {
      if (mounted.current)
        setError('Hämtningens utfall kunde inte bekräftas. Kontrollera befintliga hämtningar innan du försöker igen.')
    } finally {
      mutation.current = false
      if (mounted.current) setRequesting(false)
    }
  }
  async function readHistory() {
    historyController.current?.abort()
    const current = new AbortController()
    historyController.current = current
    setHistoryLoading(true)
    try {
      const page = await getAudiobookAcquisitionJobs(current.signal, 0, 25)
      if (!current.signal.aborted) setHistory(page.items)
    } catch {
      if (!current.signal.aborted) setError('Tidigare ljudbokshämtningar kunde inte läsas.')
    } finally {
      if (!current.signal.aborted) setHistoryLoading(false)
    }
  }
  async function refreshJob() {
    if (!job || mutation.current) return
    mutation.current = true
    setRequesting(true)
    try {
      const updated = await getAudiobookAcquisitionJob(job.id)
      if (mounted.current) setJob(updated)
    } catch {
      if (mounted.current) setError('Hämtningens status kunde inte läsas.')
    } finally {
      mutation.current = false
      if (mounted.current) setRequesting(false)
    }
  }
  return (
    <div className="media-search-results">
      <MediaSearchForm
        loading={loading}
        query={query}
        placeholder="Titel, författare, serie eller ISBN"
        showClear={Boolean(query || library || notice || error)}
        onQueryChange={onQueryChange}
        onClear={() => {
          reset()
          onQueryChange('')
        }}
        onSubmit={() => void submit()}
      />
      {controls}
      {mode === 'external' && (
        <BBSelect
          aria-label="Föredraget ljudboksspråk"
          value={language}
          onChange={event => {
            reset()
            setLanguage(event.target.value)
          }}
        >
          <option value="sv">Svenska</option>
          <option value="en">Engelska</option>
          <option value="all">Alla språk</option>
        </BBSelect>
      )}
      <p>
        {mode === 'libraries'
          ? 'Söker bland de 50 senast tillagda ljudböckerna i Audiobookshelf. Hela katalogen kan sökas i Audiobookshelf.'
          : 'Bokmetadata och anskaffningsbara utgåvor visas separat. Lokala träffar omfattar högst 25 senast tillagda böcker. Sökning på uppläsare stöds inte.'}
      </p>
      {notice && <BBEmptyState title={notice} />}
      {error && (
        <p role="alert" className="notice notice--error">
          {error}
        </p>
      )}
      {library !== null && (
        <>
          {library.length > 0 ? (
            <AudiobookLibraryResults items={library} />
          ) : (
            <BBEmptyState title="Inga lokala träffar i det sökta urvalet" />
          )}
        </>
      )}
      {search && (
        <>
          {search.metadata.works.length > 0 && (
            <section aria-label="Bokmetadata">
              <h3>Bokmetadata — inte bekräftad tillgänglighet</h3>
              {search.metadata.works.map(work => (
                <p key={work.workId}>
                  {work.canonicalTitle} · {work.authors.join(', ')}
                  {work.series && ` · ${work.series}`}
                </p>
              ))}
            </section>
          )}
          {search.metadata.state === 'unavailable' && <BBEmptyState title="Bokmetadata är tillfälligt otillgänglig" />}
          {!search.acquisition.canSearch && (
            <BBEmptyState
              title={
                search.acquisition.state === 'notConfigured'
                  ? 'Anskaffning är inte konfigurerad'
                  : 'Anskaffningssökning är inte tillgänglig'
              }
            />
          )}
          {!search.acquisition.canRequest && <p>Det går inte att begära hämtning just nu.</p>}
          {search.discovery.length === 0 && search.acquisition.canSearch && (
            <BBEmptyState title="Inga utgåvor hittades för anskaffning" />
          )}
          {search.discovery.map(candidate => (
            <BBSurface className="audiobook-candidate" key={`${candidate.source}:${candidate.editionId}`}>
              <BBMediaArtwork
                alt={`Omslag till ${candidate.title}`}
                loading="lazy"
                src={candidate.coverUrl ?? undefined}
              />
              <div className="audiobook__copy">
                <h3>{candidate.title}</h3>
                <p>{candidate.author}</p>
                <p>
                  {[
                    candidate.narrator && `Uppläsare ${candidate.narrator}`,
                    candidate.languageLabel,
                    candidate.edition,
                    candidate.publicationYear,
                    candidate.provenance ?? candidate.source,
                    candidate.availability,
                  ]
                    .filter(Boolean)
                    .join(' · ')}
                </p>
                {candidate.languageConfidence === 'probable' && <p>Språket är troligt, inte verifierat.</p>}
                <BBButton
                  disabled={
                    !search.acquisition.canRequest ||
                    requesting ||
                    attempted.current.has(`${candidate.source}:${candidate.editionId}`)
                  }
                  onClick={event => {
                    trigger.current = event.currentTarget
                    setSelected(candidate)
                  }}
                >
                  Välj utgåva
                </BBButton>
              </div>
            </BBSurface>
          ))}
        </>
      )}
      {selected && (
        <BBSurface>
          <div
            className="audiobook-confirmation"
            ref={confirmRef}
            tabIndex={-1}
            role="group"
            aria-label="Bekräfta ljudboksutgåva"
          >
            <h3>Lägg till {selected.title}?</h3>
            <p>
              {[selected.author, selected.narrator, selected.languageLabel, selected.edition, selected.source]
                .filter(Boolean)
                .join(' · ')}
            </p>
            <div className="bb-action-group">
              <BBButton
                disabled={requesting}
                onClick={() => {
                  setSelected(null)
                  trigger.current?.focus()
                }}
              >
                Avbryt
              </BBButton>
              <BBButton
                busy={requesting}
                disabled={attempted.current.has(`${selected.source}:${selected.editionId}`)}
                variant="primary"
                onClick={() => void request(selected)}
              >
                Begär hämtning
              </BBButton>
            </div>
          </div>
        </BBSurface>
      )}
      {mode === 'external' && (
        <section aria-label="Ljudbokshämtningar">
          <BBButton busy={historyLoading} onClick={() => void readHistory()}>
            Visa senaste ljudbokshämtningar
          </BBButton>
          {history !== null && (
            <>
              <p>Senaste högst 25 hämtningarna. Uppdateras när du trycker på knappen.</p>
              {history.length === 0 && <BBEmptyState title="Inga tidigare ljudbokshämtningar" />}
              {history.map(entry => (
                <p key={entry.id}>
                  {entry.candidate.title} · {audiobookStatusLabel(entry.status)}
                </p>
              ))}
            </>
          )}
        </section>
      )}
      {job && (
        <BBSurface aria-live="polite">
          <h3>Begärd hämtning: {job.candidate.title}</h3>
          <p>Status: {audiobookStatusLabel(job.status)}</p>
          <BBButton busy={requesting} onClick={() => void refreshJob()}>
            Uppdatera status
          </BBButton>
        </BBSurface>
      )}
    </div>
  )
}
