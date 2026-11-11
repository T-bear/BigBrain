'use strict'
const assert=require('node:assert/strict')
const crypto=require('node:crypto')
const fs=require('node:fs')
const path=require('node:path')
const host='http://127.0.0.1:13378'
const sleep=ms=>new Promise(r=>setTimeout(r,ms))
async function send(path,method='GET',body=null,token=null) {
 const headers={};if(body)headers['content-type']='application/json';if(token)headers.authorization='Bearer '+token
 const r=await fetch(host+path,{method,headers,body:body?JSON.stringify(body):undefined,signal:AbortSignal.timeout(9000)})
 let json=null;const raw=await r.text()
 try{json=JSON.parse(raw)}catch{}
 return {status:r.status,json}
}
;(async()=>{
 let status=null
 for(let i=0;i<35;i++){
  try{const r=await send('/status');if(r.status===200){status=r.json;break}}catch{}
  await sleep(500)
 }
 assert.ok(status,'ABS did not become ready')
 assert.equal(status.isInit,false,'synthetic instance must start empty')
 console.log('EMPTY_SYNTHETIC_ABS_READY')
 const password=crypto.randomBytes(24).toString('hex')
 const init=await send('/init','POST',{newRoot:{username:'bb158_synthetic_admin',password}})
 assert.equal(init.status,200,'root setup failed '+init.status)
 const login=await send('/login','POST',{username:'bb158_synthetic_admin',password})
 assert.equal(login.status,200,'login failed '+login.status)
 const token=login.json?.user?.accessToken||login.json?.user?.token
 assert.ok(token,'synthetic root token unavailable')
 const created=await send('/api/libraries','POST',{name:'Synthetic BB158',folders:[{fullPath:'/audiobooks/synthetic'}],mediaType:'book'},token)
 assert.equal(created.status,200,'library create failed '+created.status)
 const libraryId=created.json?.id
 assert.ok(libraryId,'library ID not returned')
 console.log('SYNTHETIC_LIBRARY_CREATED')
 const scanned=await send('/api/libraries/'+libraryId+'/scan?force=0','POST',null,token)
 assert.equal(scanned.status,200,'scan trigger failed '+scanned.status)
 console.log('SYNTHETIC_SCAN_TRIGGERED')
 let lastScan=null,done=false,observed=0,filesInItem=0,detailStatus=null
 for(let i=0;i<80;i++){
  await sleep(1000)
  const [items,lib]=await Promise.all([
    send('/api/libraries/'+libraryId+'/items?limit=100','GET',null,token),
    send('/api/libraries/'+libraryId,'GET',null,token)
  ])
  if(items.status!==200||lib.status!==200)continue
  const count=items.json?.total||items.json?.results?.length||0
  observed=Math.max(observed,count)
  lastScan=lib.json?.lastScan||lib.json?.library?.lastScan||null
  const itemId=items.json?.results?.[0]?.id
  if(itemId){
   const detail=await send('/api/items/'+encodeURIComponent(itemId),'GET',null,token)
   detailStatus=detail.status
   filesInItem=Math.max(filesInItem,detail.json?.media?.audioFiles?.length||0)
  }
  if(count>=1&&lastScan&&filesInItem===36){done=true;break}
 }
 console.log(JSON.stringify({indexedBooks:observed,scanHasLastScan:Boolean(lastScan),filesInItem,detailStatus,complete:done}))
 assert.ok(done,'scan did not confirm 36 indexed synthetic audio files')
 console.log('ISOLATED_REAL_ABS_SCAN_PASS')
 // From this point, NO explicit library-scan API call is permitted.
 // A single new book and MP3 must be detected by ABS's own watcher.
 const folder='/audiobooks/synthetic/Synthetic Second Author - Watcher Book'
 fs.mkdirSync(folder,{recursive:true})
 fs.copyFileSync('/audiobooks/synthetic/Synthetic Author - Synthetic 36 Chapter Book/part-01.mp3',
                 path.join(folder,'watcher-part-01.mp3'))
 console.log('WATCHER_SECOND_BOOK_ADDED')
 let watcherBooks=0,watcherFiles=0,watcherDetected=false
 for(let i=0;i<55;i++){
  await sleep(1000)
  const r=await send('/api/libraries/'+libraryId+'/items?limit=100','GET',null,token)
  if(r.status!==200)continue
  watcherBooks=r.json?.total||r.json?.results?.length||0
  if(watcherBooks<2)continue
  for(const item of r.json?.results||[]){
   const detail=await send('/api/items/'+encodeURIComponent(item.id),'GET',null,token)
   const count=detail.json?.media?.audioFiles?.length||0
   // Initial synthetic book has 36 files. The only new test book has one.
   // Do not depend on the metadata parser's optional author-name formatting.
   if(count===1){
    watcherFiles=count;watcherDetected=true;break
   }
  }
  if(watcherDetected)break
 }
 console.log(JSON.stringify({watcherDetected,watcherBooks,watcherFiles}))
 assert.ok(watcherDetected,'native ABS watcher did not index new book without admin library scan')
 console.log('NATIVE_WATCHER_NO_ADMIN_SCAN_PASS')
})().catch(e=>{console.error('ISOLATED_SCAN_FAILED:',e.message);process.exitCode=1})
