'use strict';
const { test, after } = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { spawn, spawnSync } = require('node:child_process');
const { once } = require('node:events');
const { probe, limits } = require('./bounded-ffprobe.cjs');
const tmp = fs.mkdtempSync(path.join(os.tmpdir(), 'bb158-probe-'));
after(() => fs.rmSync(tmp, { recursive: true, force: true }));
const wrapper = path.join(__dirname, 'bounded-ffprobe.cjs');
const fake = path.join(tmp, 'fake.cjs');
fs.writeFileSync(fake, `
const fs = require('node:fs');
const mode = process.argv[2];
if (process.argv[3]) fs.writeFileSync(process.argv[3], String(process.pid));
if (process.send) process.send('ready');
if(mode==='ok') process.stdout.write(JSON.stringify({streams:[],format:{tags:{title:process.argv[4]}}}));
else if(mode==='nonzero') { process.stdout.write('{"streams":[]}'); process.exitCode=7; }
else if(mode==='out') setInterval(()=>process.stdout.write(Buffer.alloc(16384, 120)),1);
else if(mode==='err') setInterval(()=>process.stderr.write(Buffer.alloc(16384, 120)),1);
else if(mode==='child') {
 const child=require('node:child_process').spawn(process.execPath,['-e', 'setInterval(()=>{},1000)'],{stdio:'inherit'});
 fs.writeFileSync(process.argv[3]+'.child', String(child.pid));
 setInterval(()=>{},1000);
} else setInterval(()=>{},1000);
`);
function opts(extra = {}) {
  return { executable: process.execPath, limits: { milliseconds: 500, stdout: 32768, stderr: 32768 }, ...extra };
}
function gone(pid) {
  assert.throws(() => process.kill(pid, 0), { code: 'ESRCH' });
}
async function runFile(file, args = []) {
  const p = spawn(file, args, { stdio: ['ignore', 'pipe', 'pipe'] });
  let stdout='', stderr='';
  p.stdout.on('data', d => stdout += d);
  p.stderr.on('data', d => stderr += d);
  const [code, signal] = await once(p, 'close');
  return { code, signal, stdout, stderr };
}
test('successful output and arguments are preserved; no shell evaluation', async () => {
  const title = '$(touch forbidden) ; & " spaces';
  const r = await probe([fake,'ok','',title],opts());
  assert.equal(r.code,0);
  assert.equal(JSON.parse(r.stdout).format.tags.title,title);
  assert.equal(r.stderr.length,0);
});
test('nonzero native exit never becomes partial successful JSON', async () => {
  const r=await probe([fake,'nonzero'],opts());
  assert.equal(r.code,1); assert.match(JSON.parse(r.stdout).error.string,/failed/);
});
for (const [mode, reason] of [['hang','deadline'],['out','stdout'],['err','stderr'],['child','deadline']]) {
  test(`${mode}: bounded rejection, native child reaped, no partial output`, { timeout: 5000 }, async () => {
    const pidfile=path.join(tmp,mode+'.pid');
    const start=Date.now();
    const r=await probe([fake,mode,pidfile],opts());
    assert.equal(r.code,1); assert.match(JSON.parse(r.stdout).error.string,new RegExp(reason));
    assert.ok(r.stdout.length<256);assert.equal(r.stderr.length,0);
    assert.ok(Date.now()-start<3000);
    gone(Number(fs.readFileSync(pidfile,'utf8')));
    if (mode==='child') {
      await new Promise(resolve=>setTimeout(resolve,100));
      gone(Number(fs.readFileSync(pidfile+'.child','utf8')));
    }
  });
}
for (const [mode, reason] of [['out','stdout'],['err','stderr']]) {
  test(`production byte ceiling: ${mode}`, {timeout:5000}, async()=>{
    const r=await probe([fake,mode],{executable:process.execPath});
    assert.equal(r.code,1);assert.match(JSON.parse(r.stdout).error.string,new RegExp(reason));
    assert.ok(r.stdout.length<256);assert.equal(r.stderr.length,0);
  });
}
test('missing native executable produces a controlled sanitized error',async()=>{
  const r=await probe([],opts({executable:'/missing-private-filename'}));
  assert.equal(r.code,1);assert.doesNotMatch(r.stdout.toString(),/missing-private-filename/);
});
test('SIGTERM cancels and reaps child before supervisor completion', {timeout:5000},async()=>{
  const driver=path.join(tmp,'cancel.cjs'), pidfile=path.join(tmp,'cancel.pid');
  fs.writeFileSync(driver, `const {probe}=require(${JSON.stringify(wrapper)});
probe([${JSON.stringify(fake)},'hang',${JSON.stringify(pidfile)}],{executable:process.execPath,limits:{milliseconds:2000}}).then(r=>{process.stdout.write(r.stdout);process.exitCode=r.code});
setTimeout(()=>process.kill(process.pid,'SIGTERM'),500);`);
  const r=await runFile(process.execPath,[driver]);assert.equal(r.code,1);
  assert.match(JSON.parse(r.stdout).error.string,/cancelled/);
  gone(Number(fs.readFileSync(pidfile,'utf8')));
});
test('independent watchdog bounds native life after abrupt JS-supervisor death', {timeout:6000},async()=>{
  const driver=path.join(tmp,'abrupt.cjs'), pidfile=path.join(tmp,'abrupt.pid');
  fs.writeFileSync(driver, `const {probe}=require(${JSON.stringify(wrapper)});
probe([${JSON.stringify(fake)},'hang',${JSON.stringify(pidfile)}],{executable:process.execPath,limits:{milliseconds:1000}});
setTimeout(()=>process.kill(process.pid,'SIGKILL'),500);`);
  const r=await runFile(process.execPath,[driver]);assert.equal(r.signal,'SIGKILL');
  // Single deadline assertion; no polling loop. Image's tini reaps adopted descendants.
  await new Promise(resolve=>setTimeout(resolve,1600));
  gone(Number(fs.readFileSync(pidfile,'utf8')));
});
test('actual production limits and cgroups, no capabilities, no-new-privileges',()=>{
  assert.deepEqual(limits,{milliseconds:60000,stdout:8388608,stderr:262144});
  assert.equal(process.env.SOURCE,'docker'); // ABS skips its downloader/version substitution in this image.
  assert.match(fs.readFileSync('/app/server/Server.js','utf8'),/if \(global.Source !== 'docker'\)/);
  assert.equal(fs.readFileSync('/sys/fs/cgroup/memory.max','utf8').trim(),'1073741824');
  assert.equal(fs.readFileSync('/sys/fs/cgroup/memory.swap.max','utf8').trim(),'0');
  assert.equal(fs.readFileSync('/sys/fs/cgroup/pids.max','utf8').trim(),'128');
  assert.equal(fs.readFileSync('/sys/fs/cgroup/cpu.max','utf8').trim(),'200000 100000');
  const status=fs.readFileSync('/proc/self/status','utf8');
  assert.match(status,/CapEff:\s+0000000000000000/); assert.match(status,/NoNewPrivs:\s+1/);
  assert.throws(()=>fs.writeFileSync(wrapper,'tamper'),e=>['EROFS','EACCES'].includes(e.code));
});
test('real MP3/M4A probe and ABS JSON + fluent-ffmpeg consumers remain compatible', {timeout:15000},async()=>{
  process.env.FFPROBE_PATH=wrapper;
  const prober=require('/app/server/utils/prober');
  const fluent=require('/app/server/libs/fluentFfmpeg');
  const version=await runFile(wrapper,['-version']); assert.equal(version.code,0);assert.match(version.stdout,/ffprobe version/);
  for(const [ext,codec] of [['mp3','libmp3lame'],['m4a','aac']]) {
    const file=path.join(tmp,'Synthetic chapter – space.'+ext);
    const made=spawnSync('/usr/bin/ffmpeg',['-hide_banner','-loglevel','error','-f','lavfi','-i','sine=frequency=440:duration=2','-c:a',codec,'-metadata','title=Synthetic chapter',file],{timeout:5000});
    assert.equal(made.status,0,made.stderr?.toString());
    const result=await prober.probe(file);
    assert.ok(!result.error); assert.ok(result.duration>0); assert.ok(result.audioStream);
    const raw=await prober.rawProbe(file);assert.equal(raw.format.tags.title,'Synthetic chapter');
    const parsed=await new Promise((resolve,reject)=>fluent.ffprobe(file,(e,d)=>e?reject(e):resolve(d)));
    assert.ok(parsed.streams.length>0);
  }
  const bad=await prober.probe(path.join(tmp,'absent-private.mp3'));
  assert.ok(bad.error);assert.doesNotMatch(JSON.stringify(bad),/absent-private/);
  assert.equal((await runFile(wrapper,['-version'])).code,0);
});
test('actual ABS caller receives bounded errors then still processes another request', {timeout:8000},async()=>{
  const raw=require('/app/server/libs/nodeFfprobe');
  const driver=path.join(tmp,'hook.cjs');
  fs.writeFileSync(driver,`#!/usr/local/bin/node
const {probe}=require(${JSON.stringify(wrapper)});
probe([${JSON.stringify(fake)},process.argv.at(-1)],{executable:process.execPath,limits:{milliseconds:500,stdout:32768,stderr:32768}}).then(r=>{process.exitCode=r.code;process.stdout.end(r.stdout)});`,{mode:0o755});
  raw.FFPROBE_PATH=driver;
  for(const mode of ['hang','out','err']) {
    const value=await raw(mode);assert.ok(value.error);assert.ok(JSON.stringify(value).length<256);
  }
  raw.FFPROBE_PATH=wrapper;
  const result=await require('/app/server/utils/prober').rawProbe(path.join(tmp,'Synthetic chapter – space.mp3'));
  assert.ok(!result.error);assert.ok(result.streams.length);
});
