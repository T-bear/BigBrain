import { createHash } from 'node:crypto'
// Local, isolated rendering of the real Newspaper component. No API or production access.
// Requires existing Web npm dependencies, Chromium, and Poppler's pdfinfo.
// CHROMIUM_BIN=/path/to/chromium node --experimental-websocket tests/newspaper/verify-print.mjs
import { spawn, execFileSync } from 'node:child_process'
import { mkdir, writeFile, unlink, readFile } from 'node:fs/promises'
import { resolve } from 'node:path'
import { once } from 'node:events'
import assert from 'node:assert/strict'
import { createServer } from '../../src/BigBrain.Web/node_modules/vite/dist/node/index.js'

const web = resolve('src/BigBrain.Web')
const output = resolve('data/newspaper-checkpoint1')
const harness = resolve(web, '.newspaper-print-check.html')
const browserPath = process.env.CHROMIUM_BIN
assert.ok(browserPath, 'Set CHROMIUM_BIN to an existing Chromium executable')
let server, browser, socket
try {
  const original = await readFile(resolve(web, 'public/newspaper/Morgonbladet_designmaster_A4.png'))
  assert.equal(
    createHash('sha256').update(original).digest('hex'),
    '9c3bb9676c337dc8bf6d97c93befd1fc18e03ef1200c5c089a119f45d9ad4733',
  )
  assert.deepEqual([original.readUInt32BE(16), original.readUInt32BE(20)], [2480, 3508])
  await mkdir(output, { recursive: true })
  await writeFile(
    harness,
    `<!doctype html><html><head><meta name="viewport" content="width=device-width, initial-scale=1"></head><body><div id="root"></div><script type="module">
import React from 'react';
import {createRoot} from 'react-dom/client';
import {Newspaper} from '/src/newspaper/Newspaper.tsx';
import '/src/styles.css';
createRoot(document.getElementById('root')).render(React.createElement(Newspaper));
</script></body></html>`,
    { flag: 'wx' },
  )
  server = await createServer({ root: web, server: { host: '127.0.0.1', port: 0, open: false } })
  await server.listen()
  const port = server.httpServer.address().port
  browser = spawn(browserPath, ['--headless', '--disable-gpu', '--remote-debugging-port=0', 'about:blank'], {
    stdio: ['ignore', 'ignore', 'pipe'],
  })
  const endpoint = await new Promise((resolveEndpoint, reject) => {
    let text = ''
    const timer = setTimeout(() => reject(new Error('Browser startup timeout')), 15000)
    browser.on('error', reject)
    browser.stderr.on('data', chunk => {
      text += chunk
      const match = text.match(/DevTools listening on (ws:\/\/[^\s]+)/)
      if (match) {
        clearTimeout(timer)
        resolveEndpoint(match[1])
      }
    })
  })
  const targets = await (await fetch(`http://${new URL(endpoint).host}/json/list`)).json()
  socket = new WebSocket(targets.find(target => target.type === 'page').webSocketDebuggerUrl)
  await once(socket, 'open')
  let id = 0
  const pending = new Map()
  socket.addEventListener('message', event => {
    const message = JSON.parse(event.data)
    const request = pending.get(message.id)
    if (!request) return
    pending.delete(message.id)
    message.error ? request.reject(new Error(JSON.stringify(message.error))) : request.resolve(message.result)
  })
  const call = (method, params = {}) =>
    new Promise((resolveCall, reject) => {
      const callId = ++id
      pending.set(callId, { resolve: resolveCall, reject })
      socket.send(JSON.stringify({ id: callId, method, params }))
    })
  const evaluate = async expression => {
    const result = await call('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true })
    assert.ok(!result.exceptionDetails, JSON.stringify(result.exceptionDetails))
    return result.result.value
  }
  await call('Page.enable')
  await call('Page.navigate', { url: `http://127.0.0.1:${port}/.newspaper-print-check.html` })
  await evaluate(`new Promise((resolve, reject) => {
    const end = Date.now() + 15000;
    const check = () => {
      const button = document.querySelector('button');
      if (button && !button.disabled) resolve(true);
      else if (Date.now() > end) reject(new Error('Image not ready'));
      else setTimeout(check, 50);
    }; check();
  })`)
  const evidence = { browser: await call('Browser.getVersion'), cases: [] }
  for (const [name, width, height, mobile] of [
    ['desktop', 1280, 900, false],
    ['mobile', 390, 844, true],
  ]) {
    await call('Emulation.setDeviceMetricsOverride', { width, height, deviceScaleFactor: 1, mobile })
    await call('Emulation.setEmulatedMedia', { media: 'screen' })
    const preview = await evaluate(`(() => {
      const img = document.querySelector('.newspaper-master-preview');
      const r = img.getBoundingClientRect();
      return {width:r.width, height:r.height, naturalWidth:img.naturalWidth, naturalHeight:img.naturalHeight, viewport:innerWidth};
    })()`)
    assert.equal(preview.naturalWidth, 2480)
    assert.equal(preview.naturalHeight, 3508)
    assert.ok(Math.abs(preview.width / preview.height - 2480 / 3508) < 0.001)
    assert.ok(preview.width <= preview.viewport)
    await call('Emulation.setEmulatedMedia', { media: 'print' })
    const printed = await evaluate(`(() => {
      const img = document.querySelector('#bigbrain-newspaper-print img');
      const r = img.getBoundingClientRect();
      return {width:r.width, height:r.height, fit:getComputedStyle(img).objectFit, rootDisplay:getComputedStyle(document.getElementById('root')).display};
    })()`)
    assert.equal(printed.fit, 'contain')
    assert.equal(printed.rootDisplay, 'none')
    assert.ok(Math.abs(printed.width - (210 * 96) / 25.4) < 0.1)
    assert.ok(Math.abs(printed.height - (297 * 96) / 25.4) < 0.1)
    const pdf = await call('Page.printToPDF', {
      preferCSSPageSize: true,
      printBackground: false,
      displayHeaderFooter: false,
      marginTop: 0,
      marginBottom: 0,
      marginLeft: 0,
      marginRight: 0,
    })
    const path = resolve(output, `${name}.pdf`)
    await writeFile(path, Buffer.from(pdf.data, 'base64'))
    const info = execFileSync('pdfinfo', [path], { encoding: 'utf8' })
    assert.match(info, /Pages:\s+1\b/)
    const size = info.match(/Page size:\s+([\d.]+) x ([\d.]+)/)
    assert.ok(size && Math.abs(Number(size[1]) - 595.28) < 1 && Math.abs(Number(size[2]) - 841.89) < 1)
    evidence.cases.push({ name, preview, printed, pages: 1, points: size.slice(1) })
  }
  await writeFile(resolve(output, 'evidence.json'), JSON.stringify(evidence, null, 2))
  console.log(JSON.stringify(evidence, null, 2))
} finally {
  socket?.close()
  browser?.kill('SIGTERM')
  await server?.close()
  await unlink(harness).catch(() => {})
}
