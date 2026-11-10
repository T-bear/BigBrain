'use strict'
// Four concurrent native probes and cancelled supervisors. Run only in
// a networkless ABS image with synthetic data and PID128 / CPU2 / RAM1GiB.
const fs = require('node:fs')
const assert = require('node:assert/strict')
const { spawn } = require('node:child_process')
const { probe } = require('/review/bounded-ffprobe.cjs')
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms))
const pidEvents = () => Number(fs.readFileSync('/sys/fs/cgroup/pids.events', 'utf8').match(/max\s+(\d+)/)[1])

function probeRemnants() {
  const remnants = []
  for (const name of fs.readdirSync('/proc').filter((x) => /^\d+$/.test(x))) {
    try {
      const dir = '/proc/' + name
      const comm = fs.readFileSync(dir + '/comm', 'utf8').trim()
      const status = fs.readFileSync(dir + '/status', 'utf8')
      const state = (status.match(/^State:\s+(\S+)/m) || [])[1]
      const args = fs.readFileSync(dir + '/cmdline', 'utf8').replace(/\0/g, ' ')
      // Child processes, independent BusyBox watchdog and zombies are all
      // relevant. Node's lazily-created libuv worker threads are NOT leaks.
      if (comm === 'sleep' || comm === 'timeout' || state === 'Z'
          || args.includes('/lifecycle/cancel-driver')) {
        remnants.push({ pid: Number(name), comm, state })
      }
    } catch {
      // The process may have already exited and been reaped.
    }
  }
  return remnants
}

async function timeouts() {
  const hits = pidEvents(), start = Date.now()
  const results = await Promise.all(Array.from({ length: 4 }, () =>
    probe(['3'], { executable: '/bin/sleep', limits: { milliseconds: 850 } })))
  assert.ok(results.every((r) => r.code === 1 &&
    JSON.parse(r.stdout).error.string.includes('deadline exceeded')),
    'all four children must return sanitized deadline errors')
  assert.ok(Date.now() - start < 6000, 'probe timeout did not bound execution')
  // BusyBox timeout independently rounds this deadline up to 1s.
  await sleep(1400)
  assert.equal(pidEvents() - hits, 0, 'PID max reached in timeout batch')
  assert.deepEqual(probeRemnants(), [], 'native probes/watchdogs not reaped after timeout')
  console.log('FOUR_CONCURRENT_TIMEOUT_REAP_PASS')
}

async function cancellations() {
  const hits = pidEvents()
  const driver = [
    "const {probe}=require('/review/bounded-ffprobe.cjs')",
    "probe(['3'],{executable:'/bin/sleep',limits:{milliseconds:2000}}).then(r=>{process.stdout.write(r.stdout);process.exitCode=r.code})",
    "setTimeout(()=>process.kill(process.pid,'SIGTERM'),300)"
  ].join(';')
  const pending = Array.from({ length: 4 }, () => new Promise((resolve, reject) => {
    const child = spawn(process.execPath, ['-e', driver], { stdio: ['ignore', 'pipe', 'pipe'] })
    let out = '', err = ''
    child.stdout.on('data', (d) => { out += d.toString() })
    child.stderr.on('data', (d) => { err += d.toString() })
    child.on('error', reject)
    child.on('close', (code) => {
      try {
        assert.equal(code, 1)
        assert.match(out, /cancelled/)
        resolve()
      } catch (e) { reject(new Error(e.message + ' ' + err.slice(0, 200))) }
    })
  }))
  await Promise.all(pending)
  // A separate BusyBox watchdog session can briefly outlive SIGTERM of
  // the JS supervisor. It must terminate after its own 2s cap.
  await sleep(2200)
  assert.equal(pidEvents() - hits, 0, 'PID max reached during cancellation')
  assert.deepEqual(probeRemnants(), [], 'native process/watchdog/zombie remains after cancelled batch')
  console.log('FOUR_CONCURRENT_CANCEL_WATCHDOG_REAP_PASS')
}

;(async () => {
  assert.equal(fs.readFileSync('/sys/fs/cgroup/pids.max', 'utf8').trim(), '128')
  await timeouts()
  await cancellations()
  console.log('FOUR_PROBE_LIFECYCLE_PASS')
})().catch((e) => {
  console.error('FOUR_PROBE_LIFECYCLE_FAIL', e.message)
  process.exitCode = 1
})
