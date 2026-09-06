// Read-only BB-130 browser probe. Node 20: use --experimental-websocket.
// Start a dedicated Firefox profile with --headless --remote-debugging-port 9224.
// Never attach this probe to a personal browser profile. No response bodies are exported.
import { writeFile } from 'node:fs/promises'

const [baseUrl, output] = process.argv.slice(2)
if (!baseUrl || !output) throw new Error('Usage: measure-browser-loading.mjs <app URL> <output.json>')
const socket = new WebSocket('ws://127.0.0.1:9224/session')
await new Promise((resolve, reject) => { socket.onopen = resolve; socket.onerror = reject })
let sequence = 0
const pending = new Map()
socket.onmessage = ({ data }) => {
  const message = JSON.parse(data)
  if (!pending.has(message.id)) return
  const { resolve, reject } = pending.get(message.id)
  pending.delete(message.id)
  if (message.type === 'error') reject(new Error(message.error))
  else resolve(message.result)
}
function command(method, params = {}) {
  return new Promise((resolve, reject) => {
    const id = ++sequence
    pending.set(id, { resolve, reject })
    socket.send(JSON.stringify({ id, method, params }))
  })
}

// Endpoint vocabulary is intentionally restricted. Unknown segments and all queries
// are redacted; private identifiers, headers and bodies never reach output.
function preload(view) {
  if (view !== 'finance-cached') localStorage.clear()
  localStorage.setItem('bigbrain.dashboard.preferences.v2', JSON.stringify({ version: 2, activeView: view === 'finance-cached' ? 'finance' : view, views: {} }))
  const vocabulary = new Set('api v1 settings theme modules docker containers system overview recovery finance observation risk status evaluations autonomous features datasets backtests robustness backups shadow scorecard research scheduler governor operations media search providers provider-status service-links services jobs downloads queue smart-shuffle audiobooks acquisition availability series library playback family meal-planner schedules calendar week shopping-list items health recommendations'.split(' '))
  const endpoint = value => new URL(value, location.href).pathname.split('/').map(part => !part || vocabulary.has(part) ? part : ':redacted').join('/')
  const original = window.fetch.bind(window)
  const requests = []
  let inFlight = 0
  window.__loadingProbe = { view, requests, blockedWrites: 0, financeHeroMs: null }
  const observer = new MutationObserver(() => {
    if (document.querySelector('.finance-hero') && window.__loadingProbe.financeHeroMs === null) window.__loadingProbe.financeHeroMs = performance.now()
  })
  observer.observe(document, { childList: true, subtree: true })
  window.fetch = async (input, init) => {
    const url = typeof input === 'string' ? input : input.url ?? String(input)
    const method = (init?.method ?? input.method ?? 'GET').toUpperCase()
    if (method !== 'GET') {
      window.__loadingProbe.blockedWrites++
      throw new Error('Measurement blocks mutations')
    }
    const row = { endpoint: endpoint(url), startMs: performance.now(), inFlightAtStart: ++inFlight }
    requests.push(row)
    try {
      const response = await original(input, init)
      row.status = response.status
      return response
    } catch (error) {
      row.failed = true
      throw error
    } finally {
      row.headersEndMs = performance.now()
      row.headersDurationMs = row.headersEndMs - row.startMs
      inFlight--
    }
  }
  window.__finishLoadingProbe = () => {
    const resources = performance.getEntriesByType('resource').filter(entry => entry.initiatorType === 'fetch')
    return { ...window.__loadingProbe, sampledAtMs: performance.now(), requests: requests.map(row => {
      const resource = resources.find(entry => endpoint(entry.name) === row.endpoint && Math.abs(entry.startTime - row.startMs) < 10)
      return { ...row, endMs: resource?.responseEnd, durationMs: resource?.duration,
        decodedBodyBytes: resource?.decodedBodySize, transferBytes: resource?.transferSize,
        cache: resource ? resource.transferSize > 0 ? 'network' : 'cache-or-unavailable' : 'unavailable' }
    }) }
  }
}

const evidence = { measuredAtUtc: new Date().toISOString(), measurement: 'isolated Firefox; GET only; empty local storage per view; no response bodies; browser HTTP cache not cleared between tabs', views: [] }
try {
  await command('session.new', { capabilities: {} })
  for (const view of ['home', 'finance', 'finance-cached', 'media']) {
    const { context } = await command('browsingContext.create', { type: 'tab' })
    const { script } = await command('script.addPreloadScript', { functionDeclaration: `() => (${preload.toString()})(${JSON.stringify(view)})`, contexts: [context] })
    await command('browsingContext.navigate', { context, url: baseUrl, wait: 'complete' })
    await new Promise(resolve => setTimeout(resolve, 6500))
    // Include slow Finance reads, bounded to 45 seconds after navigation.
    for (let attempt = 0; attempt < 38; attempt++) {
      const state = await command('script.evaluate', { expression: 'window.__loadingProbe.requests.every(row => row.headersEndMs !== undefined)', target: { context }, awaitPromise: false })
      if (state.result?.value === true) break
      await new Promise(resolve => setTimeout(resolve, 1000))
    }
    const result = await command('script.evaluate', { expression: 'JSON.stringify(window.__finishLoadingProbe())', target: { context }, awaitPromise: false })
    if (result.type !== 'success') throw new Error('Probe did not initialize')
    evidence.views.push(JSON.parse(result.result.value))
    if (view === 'finance-cached' || view === 'media') {
      const selector = view === 'media' ? '#media-administration details.administration' : '.finance-research-details'
      const opened = await command('script.evaluate', { expression: `(() => { const details = document.querySelector(${JSON.stringify(selector)}); if (!details) return false; details.open = true; return true })()`, target: { context }, awaitPromise: false })
      if (opened.result?.value === true) {
        await new Promise(resolve => setTimeout(resolve, 6500))
        const detailsResult = await command('script.evaluate', { expression: 'JSON.stringify(window.__finishLoadingProbe())', target: { context }, awaitPromise: false })
        const detailsEvidence = JSON.parse(detailsResult.result.value)
        detailsEvidence.view += '-details-open'
        evidence.views.push(detailsEvidence)
      }
    }
    await command('script.removePreloadScript', { script })
    await command('browsingContext.close', { context })
  }
  await writeFile(output, JSON.stringify(evidence, null, 2) + '\n')
  console.log(JSON.stringify(evidence.views.map(({ view, requests, blockedWrites }) => ({ view, requests: requests.length, peakHeadersInFlight: Math.max(...requests.map(row => row.inFlightAtStart)), blockedWrites }))))
} finally {
  await command('session.end').catch(() => {})
  socket.close()
}
