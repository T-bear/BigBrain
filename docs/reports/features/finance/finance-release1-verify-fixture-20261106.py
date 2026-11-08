# Synthetic test harness only; creates fixtures in a new temporary directory.
import ast
import contextlib
import datetime
import hashlib
import io
import json
import os
from pathlib import Path
import re
import shutil
import sqlite3
import sys
import tempfile

import types
SCRIPT = Path(sys.argv[1])
source = SCRIPT.read_text()
ROOT = Path(tempfile.mkdtemp(prefix='finance-r1-synthetic-'))
Path('/tmp').as_uri()
ns = {'__name__': 'review_fixture'}
exec(compile(source, str(SCRIPT), 'exec'), ns)
# Replace only environment gates and snapshot pins inside the test namespace.
# The review artifact on disk is unchanged and never run against Finance data.
ns['os'] = types.SimpleNamespace(**{k:getattr(os,k) for k in dir(os)})
ns['os'].getuid = ns['os'].geteuid = lambda: ns['DEDICATED_UID']
ns['os'].access = lambda p, mode: False
RUNS=ns['RUNS']; FEATURE=ns['FEATURE']; MARKET=ns['MARKET']; IDS=sorted(ns['UNIVERSE'])
active=False
calls=[]
def audit(event,args):
    if not active: return
    if event=='open':
        path,mode,flags=args
        if isinstance(path,(str,bytes,os.PathLike)):
            p=Path(os.fsdecode(path)).absolute()
            assert p.is_relative_to(ROOT), 'outside fixture read'
        if isinstance(flags,int):
            assert not flags & (os.O_WRONLY|os.O_RDWR|os.O_CREAT|os.O_TRUNC|os.O_APPEND), 'write open'
    assert not event.startswith(('subprocess.','socket.')), 'forbidden process or network'
    assert event not in ('os.mkdir','os.remove','os.rename','os.rmdir','os.symlink'), 'filesystem mutation'
    calls.append(event)
sys.addaudithook(audit)
def compact(x): return json.dumps(x,separators=(',',':'),ensure_ascii=True,default=float)
def sha(b): return 'sha256:'+hashlib.sha256(b).hexdigest()

def seed(folder):
    folder.mkdir()
    db=folder/ns['SNAPSHOT_NAME']
    c=sqlite3.connect(db)
    c.executescript('''
    CREATE TABLE backtest_runs(run_id TEXT PRIMARY KEY,checksum TEXT,strategy_id TEXT,strategy_version TEXT,cost_model TEXT,feature_revision_id TEXT,market_revisions_json TEXT,from_date TEXT,to_date TEXT,result_json TEXT,created_utc TEXT);
    CREATE TABLE backtest_events(run_id TEXT,sequence INTEGER,event_json TEXT,PRIMARY KEY(run_id,sequence));
    CREATE TABLE backtest_fills(run_id TEXT,fill_id TEXT,fill_json TEXT,PRIMARY KEY(run_id,fill_id));
    CREATE TABLE backtest_equity(run_id TEXT,session_date TEXT,point_json TEXT,PRIMARY KEY(run_id,session_date));
    CREATE TABLE feature_revisions(revision_id TEXT,source_revisions_json TEXT,created_utc TEXT,value_count INTEGER,available_count INTEGER,warmup_count INTEGER,quality_issue_count INTEGER,checksum TEXT);
    CREATE TABLE feature_values(revision_id TEXT,instrument_id TEXT,session_date TEXT,source_revision_id TEXT,source_from TEXT,source_to TEXT,state TEXT,quality TEXT);
    CREATE TABLE revisions(revision_id TEXT,checksum TEXT);
    CREATE TABLE observations(instrument_id TEXT,session_date TEXT,revision_id TEXT);
    ''')
    dates=[(datetime.date(2014,1,2)+datetime.timedelta(days=i)).isoformat() for i in range(747)]+['2016-12-19']
    c.execute('INSERT INTO feature_revisions VALUES(?,?,?,?,?,?,?,?)',(FEATURE,compact([MARKET]),'2026-08-15T00:00:00.1234567Z',78162,76567,1595,0,sha(b'feature')))
    c.execute('INSERT INTO feature_revisions VALUES(?,?,?,?,?,?,?,?)',('feature-fixture-new',compact([MARKET]),'2026-08-16T00:00:00Z',0,0,0,0,sha(b'new')))
    c.execute('INSERT INTO revisions VALUES(?,?)',(MARKET,sha(b'synthetic-source')))
    source_pairs=[(i,d) for n,i in enumerate(IDS) for d in (dates if n<3 else dates[:738]+dates[-1:])]
    c.executemany('INSERT INTO observations VALUES(?,?,?)',[(i,d,MARKET) for i,d in source_pairs])
    features=[(FEATURE,i,d,MARKET,d,d,'Warmup' if k<1595 else 'Available','Good')
              for k,(i,d) in enumerate(pair for pair in source_pairs for _ in range(21))]
    c.executemany('INSERT INTO feature_values VALUES(?,?,?,?,?,?,?,?)',features)
    for k,rid in enumerate(RUNS):
        cfg,_=ns['expected_configuration'](rid)
        events=[{'sequence':i+1,'instrumentId':IDS[i%5],'session':dates[min(i//7,747)],'featureRevisionId':FEATURE,'marketRevisionId':MARKET,'value':1.25,'detail':'not-for-output-SECRET'} for i in range(4665+(k<2))]
        fills=[{'fillId':f'fill-{i:016x}','instrumentId':IDS[i%5],'fillSession':dates[i//5],'intentSession':dates[i//5],'value':2.125} for i in range(195+(k<2))]
        equity=[{'session':d,'totalEquity':123.125} for d in dates]
        obj={'runId':rid,'checksum':'','configuration':cfg,'metrics':{'checksum':'nested-must-stay','value':0.125},'fills':fills,'events':events,'equityCurve':equity,'status':'RESEARCH','limitations':['synthetic only']}
        obj['checksum']=sha(compact(obj).encode())
        c.execute('INSERT INTO backtest_runs VALUES(?,?,?,?,?,?,?,?,?,?,?)',(rid,obj['checksum'],cfg['strategy']['id'],'v1',cfg['costModel']['id']+'-v1',FEATURE,compact([MARKET]),cfg['from'],cfg['to'],compact(obj),'2026-08-15T01:00:00Z'))
        c.executemany('INSERT INTO backtest_events VALUES(?,?,?)',[(rid,x['sequence'],compact(x)) for x in events])
        c.executemany('INSERT INTO backtest_fills VALUES(?,?,?)',[(rid,x['fillId'],compact(x)) for x in fills])
        c.executemany('INSERT INTO backtest_equity VALUES(?,?,?)',[(rid,x['session'],compact(x)) for x in equity])
    c.commit();c.close()
    bids=[]
    for i in range(2):
        data=compact({'fixture':i,'padding':'x'*2100000}).encode()
        digest=sha(data);bid='finance-backup-'+digest[7:23];bids.append(bid)
        (folder/(bid+'.data.json')).write_bytes(data)
        m={'backupId':bid,'createdAtUtc':'2026-08-15T00:00:00Z','schemaVersion':'fixture-v1','bigBrainVersion':'fixture','status':'Complete','sources':[], 'revisions':[{'revisionId':MARKET,'observationCount':3740}], 'featureRevisionIds':[FEATURE], 'backtestRunIds':list(RUNS), 'robustnessEvaluationIds':[], 'artifacts':[{'path':bid+'.data.json','bytes':len(data),'sha256':digest}],'contentFingerprint':digest}
        (folder/(bid+'.manifest.json')).write_text(compact(m))
    (folder/'bb090-closure-20260816').mkdir()
    (folder/'bb090-closure-20260816'/'metadata-only.txt').write_text('MUST-NOT-READ')
    return bids

def inventory(folder):
    return {str(p.relative_to(folder)):(p.stat().st_size,p.stat().st_mtime_ns,hashlib.sha256(p.read_bytes()).hexdigest()) for p in folder.rglob('*') if p.is_file() and not p.is_symlink()}

def verify(folder,bids):
    global active
    ns['BACKUPS']=tuple(bids)
    before=inventory(folder)
    db=folder/ns['SNAPSHOT_NAME']
    if db.is_file(): ns['SNAPSHOT_SHA256']=hashlib.sha256(db.read_bytes()).hexdigest()
    output=io.StringIO()
    active=True
    try:
        with contextlib.redirect_stdout(output):
            code=ns['main'](['--db',str(folder/ns['SNAPSHOT_NAME']),'--manifest-dir',str(folder)])
    finally: active=False
    assert before==inventory(folder),'fixture mutation'
    s=output.getvalue()
    assert len(s.splitlines())<=80
    assert 'SECRET' not in s and 'totalEquity' not in s and 'MUST-NOT-READ' not in s
    assert all(line.startswith(('PASS ','FAIL ','NOT CHECKED ','SAMMANFATTNING:')) for line in s.splitlines())
    return code,s

base=ROOT/'valid'
bids=seed(base)
code,out=verify(base,bids)
assert code==0,out
assert out.count(':result_checksum ')==6
assert 'PASS BB084_totals events=27992 fills=1172 sessions=748 instruments=5' in out
assert 'non_BB084=1 newer=1' in out
print('VALID',out.splitlines()[-1],'lines='+str(len(out.splitlines())))

cases=[
('feature_lineage',"UPDATE feature_values SET source_revision_id='wrong' WHERE rowid=1"),
('feature_count',"UPDATE feature_revisions SET value_count=1 WHERE revision_id='feature-3833eb92bb641e51'"),
('full_configuration',"UPDATE backtest_runs SET result_json=replace(result_json,'100000','100001') WHERE run_id=?"),
('event_lineage',"UPDATE backtest_runs SET result_json=replace(result_json,'\"marketRevisionId\":\"wiki-5713d7dccfa38f56\"','\"marketRevisionId\":\"wrong\"') WHERE run_id=?"),
('child_content',"UPDATE backtest_events SET event_json=replace(event_json,'1.25','9.25') WHERE run_id=? AND sequence=1"),
('missing_fill','DELETE FROM backtest_fills WHERE run_id=? AND fill_id=(SELECT min(fill_id) FROM backtest_fills WHERE run_id=?)'),
('header_mismatch',"UPDATE backtest_runs SET strategy_version='v99' WHERE run_id=?"),
('payload_checksum',"UPDATE backtest_runs SET result_json=replace(result_json,'synthetic only','modified') WHERE run_id=?"),
('invalid_json',"UPDATE backtest_runs SET result_json='{' WHERE run_id=?"),
('missing_table','DROP TABLE backtest_equity'),
]
for name,sql in cases:
    f=ROOT/name;shutil.copytree(base,f)
    c=sqlite3.connect(f/ns['SNAPSHOT_NAME']);c.execute(sql,(RUNS[0],)*sql.count('?'));c.commit();c.close()
    code,s=verify(f,bids);assert code==1,(name,s);print('DETECTED',name)
for name in ('event_order','duplicate_json_key','extra_equity','fill_order'):
    f=ROOT/name;shutil.copytree(base,f);c=sqlite3.connect(f/ns['SNAPSHOT_NAME'])
    raw=c.execute('SELECT result_json FROM backtest_runs WHERE run_id=?',(RUNS[0],)).fetchone()[0]
    obj=json.loads(raw)
    if name=='event_order':obj['events'][0],obj['events'][1]=obj['events'][1],obj['events'][0]
    if name=='fill_order':obj['fills'][0],obj['fills'][1]=obj['fills'][1],obj['fills'][0]
    if name=='extra_equity':c.execute('INSERT INTO backtest_equity VALUES(?,?,?)',(RUNS[0],'2017-01-01',compact({'session':'2017-01-01','totalEquity':0})))
    else:
        obj['checksum']='';obj['checksum']=sha(compact(obj).encode());raw=compact(obj)
        if name=='duplicate_json_key':raw=raw.replace('"status":"RESEARCH"','"status":"RESEARCH","status":"RESEARCH"')
        c.execute('UPDATE backtest_runs SET checksum=?,result_json=? WHERE run_id=?',(obj['checksum'],raw,RUNS[0]))
    c.commit();c.close();code,s=verify(f,bids);assert code==1,(name,s);print('DETECTED',name)
for suffix in ('-wal','-journal','-shm'):
    f=ROOT/('guard'+suffix);shutil.copytree(base,f);Path(str(f/ns['SNAPSHOT_NAME'])+suffix).touch()
    code,s=verify(f,bids);assert code==1 and len(s.splitlines())==2;print('REFUSED',suffix)
f=ROOT/'nonregular';shutil.copytree(base,f);(f/ns['SNAPSHOT_NAME']).unlink();(f/ns['SNAPSHOT_NAME']).mkdir()
code,s=verify(f,bids);assert code==1;print('REFUSED','nonregular')
f=ROOT/'symlink';shutil.copytree(base,f);(f/ns['SNAPSHOT_NAME']).unlink();(f/ns['SNAPSHOT_NAME']).symlink_to(base/ns['SNAPSHOT_NAME'])
code,s=verify(f,bids);assert code==1;print('REFUSED','symlink')
for name in ('manifest_hash','manifest_path','manifest_missing_list'):
    f=ROOT/name;shutil.copytree(base,f)
    if name=='manifest_hash':
        with (f/(bids[0]+'.data.json')).open('ab') as w:w.write(b'!')
    else:
        p=f/(bids[0]+'.manifest.json');m=json.loads(p.read_text())
        if name=='manifest_path':m['artifacts'][0]['path']='../unapproved.data.json'
        else:del m['backtestRunIds']
        p.write_text(compact(m))
    code,s=verify(f,bids);assert code==1,(name,s);print('DETECTED',name)
# Exercise gates with the unchanged production constants/control flow.
for name in ('wrong_identity', 'root_identity', 'writable_copy', 'wrong_hash'):
    old_uid, old_euid, old_access = ns['os'].getuid, ns['os'].geteuid, ns['os'].access
    ns['SNAPSHOT_SHA256'] = hashlib.sha256((base/ns['SNAPSHOT_NAME']).read_bytes()).hexdigest()
    if name == 'wrong_identity': ns['os'].getuid = ns['os'].geteuid = lambda: 999999
    if name == 'root_identity': ns['os'].getuid = ns['os'].geteuid = lambda: 0
    if name == 'writable_copy': ns['os'].access = lambda p, mode: True
    if name == 'wrong_hash': ns['SNAPSHOT_SHA256'] = '0'*64
    before = inventory(base)
    output=io.StringIO()
    active=True
    try:
        with contextlib.redirect_stdout(output):
            code=ns['main'](['--db',str(base/ns['SNAPSHOT_NAME']),'--manifest-dir',str(base)])
    finally:
        active=False
        ns['os'].getuid,ns['os'].geteuid,ns['os'].access=old_uid,old_euid,old_access
    assert code == 1 and 'connection_ro' not in output.getvalue()
    assert before == inventory(base)
    print('REFUSED',name)
terms=('INSERT','UPDATE','DELETE','ALTER','DROP','VACUUM','REINDEX','ATTACH','CREATE','subprocess','socket')
print('SELF_SCAN', {word:len(re.findall(r'\b'+word+r'\b',source,re.I)) for word in terms})
tree=ast.parse(source)
opens=[ast.unparse(n) for n in ast.walk(tree) if isinstance(n,ast.Call) and isinstance(n.func,ast.Attribute) and n.func.attr=='open']
assert all(x.endswith(".open('rb')") for x in opens),opens
print('READ_ONLY_OPENS',opens)
print('AUDIT',len(calls),'events; no write/process/network during verifier; all fixture files confined to temp')
print('SCRIPT_SHA256',hashlib.sha256(source.encode()).hexdigest())
