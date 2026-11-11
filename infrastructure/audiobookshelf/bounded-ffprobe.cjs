#!/usr/local/bin/node
'use strict';

// ABS FFPROBE_PATH hook. Fixed binaries; no shell, URL fetcher or configurable CLI limits.
const { spawn } = require('node:child_process');
const limits = Object.freeze({ milliseconds: 60000, stdout: 8 * 1024 * 1024, stderr: 256 * 1024 });

function failure(category) {
  return { code: 1, stdout: Buffer.from(JSON.stringify({ error: { code: -1, string: `BB158 probe ${category}` } }) + '\n'), stderr: Buffer.alloc(0) };
}

// Options are an in-process test seam only; the executable never reads them from env/arguments.
function probe(args, options = {}) {
  const bound = { ...limits, ...options.limits };
  const executable = options.executable || '/usr/bin/ffprobe';
  return new Promise((resolve) => {
    let category;
    let out = [], outBytes = 0, errBytes = 0;
    let exited = false;
    // The independent timeout remains effective if this JS supervisor is killed abruptly.
    // The image's BusyBox timeout and init/reaping behavior are verified in isolated tests.
    const child = spawn('/usr/bin/timeout', ['-s', 'KILL', String(Math.ceil(bound.milliseconds / 1000)), executable, ...args], {
      detached: true, shell: false, stdio: ['inherit', 'pipe', 'pipe'],
      env: { PATH: '/usr/local/bin:/usr/bin:/bin', LANG: 'C.UTF-8' }
    });
    function killGroup() {
      if (!child.pid) return;
      try { process.kill(-child.pid, 'SIGKILL'); }
      catch (error) { if (error.code !== 'ESRCH') category = 'cleanup failed'; }
    }
    function stop(reason) {
      category ||= reason;
      out = [];
      killGroup();
    }
    const term = () => stop('cancelled');
    process.on('SIGTERM', term);
    process.on('SIGINT', term);
    const timer = setTimeout(() => stop('deadline exceeded'), bound.milliseconds);
    child.on('error', () => stop('unavailable'));
    child.stdout.on('error', () => stop('output failure'));
    child.stderr.on('error', () => stop('output failure'));
    child.stdout.on('data', (data) => {
      if (category) return;
      outBytes += data.length;
      if (outBytes > bound.stdout) stop('stdout limit exceeded');
      else out.push(data);
    });
    child.stderr.on('data', (data) => {
      if (category) return;
      errBytes += data.length;
      if (errBytes > bound.stderr) stop('stderr limit exceeded');
      // Count, but never retain/forward decoder diagnostics or private file names.
    });
    child.on('exit', () => { exited = true; killGroup(); });
    child.on('close', (code, signal) => {
      clearTimeout(timer);
      process.off('SIGTERM', term);
      process.off('SIGINT', term);
      if (category) return resolve(failure(category));
      if (!exited || code !== 0 || signal) return resolve(failure('failed'));
      // Only a fully completed successful probe is forwarded. Preserve both JSON and
      // ffprobe's normal text output (-version and fluent-ffmpeg's INI-style requests).
      resolve({ code: 0, stdout: Buffer.concat(out, outBytes), stderr: Buffer.alloc(0) });
    });
  });
}

// The per-child timeout does not bound writing a successful result to stdout:
// an abandoned or non-reading ABS caller can leave this Node process blocked
// indefinitely in pipe backpressure. Keep an independent hard lifecycle budget
// from invocation start through the last stdout byte, with no CLI override.
async function main(args = process.argv.slice(2), options = {}) {
  const requested = options.limits?.milliseconds;
  const duration = Number.isFinite(requested) && requested > 0
    ? Math.min(requested, limits.milliseconds) : limits.milliseconds;
  // Reserve a bounded cleanup/output slice INSIDE the total lifetime cap.
  // The ordinary CLI gets 59s native probing and up to 1s for child reaping
  // and flushing a sanitized failure response. The in-process test seam may
  // only shorten the total cap, never extend the production 60s ceiling.
  const grace = Math.min(1000, Math.floor(duration / 2));
  const nativeOptions = {
    ...options,
    limits: { ...options.limits, milliseconds: Math.max(1, duration - grace) }
  };
  const resultPromise = probe(args, nativeOptions).catch(() => failure('supervisor failed'));
  const watchdog = setTimeout(() => {
    // stdout cannot be trusted to drain. Forced nonzero exit is preferable
    // to unbounded task lifetime or falsely successful partial metadata.
    process.exit(1);
  }, duration);
  const result = await resultPromise;
  process.exitCode = result.code;
  process.stdout.on('error', () => { process.exitCode = 1; });
  const clearWatchdog = () => clearTimeout(watchdog);
  process.stdout.once('finish', clearWatchdog);
  process.stdout.once('close', clearWatchdog);
  process.stdout.end(result.stdout);
}
if (require.main === module) main().catch(() => {
  process.exitCode = 1;
  process.stdout.end(failure('supervisor failed').stdout);
});
module.exports = { probe, limits, main };
