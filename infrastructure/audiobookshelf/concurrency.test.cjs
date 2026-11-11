'use strict'
const scanner=require('/app/server/scanner/AudioFileScanner.js')
const assert=require('node:assert/strict')
let active=0, maximum=0, finished=0
scanner.scan=async function(){
  active++
  maximum=Math.max(active,maximum)
  await new Promise(r=>setTimeout(r,30))
  finished++
  active--
  return {ok:true}
}
;(async()=>{
  const files=Array.from({length:36},(_,i)=>({num:i}))
  const results=await scanner.executeMediaFileScans('book',{mediaMetadata:{}},files)
  console.log(JSON.stringify({files:files.length,completed:finished,results:results.length,maxConcurrent:maximum}))
  assert.equal(results.length,36)
  assert.equal(finished,36)
  assert.ok(maximum<=4,'ABSSCAN_MAX_CONCURRENCY_EXCEEDED')
  console.log('SCAN_BATCH_4_PASS')
})().catch(e=>{console.error(e.message);process.exitCode=1})
