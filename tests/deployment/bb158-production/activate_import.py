#!/usr/bin/python3
"""Bounded owner-local API configuration activation; no audio import or data rollback."""
import fcntl
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import urllib.error
import urllib.request
import uuid

from execute import fingerprint, save, same_service

ROOT = Path(__file__).resolve().parent
FLAG = 'Media__Librarr__DirectAudioImportEnabled'


def command(args, timeout=30):
    r = subprocess.run(args, capture_output=True, timeout=timeout,
        env={'PATH': '/usr/bin:/bin', 'HOME': os.environ['HOME'], 'LANG': 'C',
             'COMPOSE_REMOVE_ORPHANS': 'false', 'COMPOSE_IGNORE_ORPHANS': 'true'})
    if r.returncode: raise RuntimeError('CommandFailed')
    return r.stdout


def inspect(cid):
    assert re.fullmatch('[0-9a-f]{64}', cid)
    return json.loads(command(['/usr/bin/docker', '--host', 'unix:///var/run/docker.sock', 'inspect', cid]))[0]


def compose(p, *args, timeout=30):
    return command(['/usr/bin/docker', '--host', 'unix:///var/run/docker.sock', 'compose',
        '--project-name', p['project'], '--project-directory', str(ROOT), '--env-file', str(ROOT/'empty.env'),
        '-f', str(ROOT/'api.compose.json'), *args], timeout)


def request(p, path, method='GET'):
    req = urllib.request.Request(p['webBase'] + path, method=method)
    try:
        with urllib.request.urlopen(req, timeout=20) as r: return r.status, r.read(2*1024*1024)
    except urllib.error.HTTPError as e: return e.code, e.read(16384)


def drafts(p):
    status, body = request(p, '/api/v1/modules/media/url-imports/')
    assert status == 200
    rows = json.loads(body)
    assert not any(v['status'] in ('downloading','importing','uncertain') for v in rows), 'UnresolvedImport'
    return rows


def unchanged(p, all_services=False):
    for old in p['originals']:
        if not all_services and old['Id'] == p['api']['Id']: continue
        current = inspect(old['Id'])
        assert fingerprint(current) == fingerprint(old)
        assert current['State']['Running'] and current['State']['StartedAt'] == old['State']['StartedAt']
        if old['State'].get('Health',{}).get('Status') == 'healthy':
            assert current['State'].get('Health',{}).get('Status') == 'healthy'


def enabled_matches(p, current):
    expected = json.loads(json.dumps(p['api']))
    env = dict(x.split('=',1) for x in expected['Config']['Env']); env[FLAG] = 'true'
    expected['Config']['Env'] = [k+'='+v for k,v in env.items()]
    same_service(expected, current, p['api']['Image'])
    for k in ('User','Cmd','Entrypoint','WorkingDir','Healthcheck','StopTimeout'):
        assert current['Config'].get(k) == expected['Config'].get(k)
    assert current['Config']['Labels']['com.docker.compose.service']=='api'
    assert current['Config']['Labels']['com.docker.compose.project']==p['project']
    assert set(current['NetworkSettings']['Networks'])==set(expected['NetworkSettings']['Networks'])
    assert current['State']['Running'] and current['State']['Health']['Status']=='healthy'


def load():
    if sys.flags.optimize: raise RuntimeError('OptimizedPythonNotAllowed')
    assert os.getuid()==1000 and ROOT.stat().st_mode&0o077==0
    hashes=json.loads((ROOT/'package.json').read_text())
    assert set(hashes)=={'activate_import.py','execute.py','plan.json','api.compose.json','empty.env'}
    for name,h in hashes.items():
        f=ROOT/name; assert f.resolve()==f and f.stat().st_mode&0o077==0
        assert hashlib.sha256(f.read_bytes()).hexdigest()==h
    p=json.loads((ROOT/'plan.json').read_text())
    assert re.fullmatch('[0-9a-f]{32}',p['run']) and p['unit']=='bb158-activate-'+p['run']+'.service'
    cfg=json.loads((ROOT/'api.compose.json').read_text())
    assert cfg==p['compose'] and set(cfg)=={'name','services','networks','volumes'}
    assert set(cfg['services'])=={'api'} and 'build' not in cfg['services']['api'] and 'depends_on' not in cfg['services']['api']
    assert cfg['services']['api']['image']==p['api']['Image']
    assert cfg['services']['api']['environment'][FLAG]=='true'
    assert all(v.get('external') is True for v in [*cfg['networks'].values(),*cfg['volumes'].values()])
    return p,hashes['plan.json']


def preflight(p):
    unchanged(p, True)
    assert drafts(p)==p['drafts'], 'ImportHistoryChanged'
    for path,h in p['inputHashes'].items():
        f=Path(path); assert f.resolve()==f and hashlib.sha256(f.read_bytes()).hexdigest()==h
    assert json.loads(compose(p,'config','--format','json'))==p['compose']
    assert os.statvfs(ROOT).f_bavail*os.statvfs(ROOT).f_frsize>=20*1024**3


def work(p,h,inv):
    assert not (ROOT/'state.json').exists()
    preflight(p)
    save(ROOT/'state.json',{'run':p['run'],'planHash':h,'invocation':inv,'phase':'api-replacement-possible'})
    compose(p,'up','-d','--no-deps','--no-build','--pull','never','--wait','--wait-timeout','100','api',timeout=150)
    cid=compose(p,'ps','-q','api').decode().strip(); current=inspect(cid);enabled_matches(p,current)
    assert request(p,'/health')[0]==200
    before=drafts(p);assert before==p['drafts']
    probe=uuid.uuid4().hex; assert all(v['id']!=probe for v in before)
    status,body=request(p,'/api/v1/modules/media/url-imports/'+probe+'/start','POST')
    # Absent intent reaches the store only AFTER the live opt-in and authenticated capability gate.
    assert status==404 and json.loads(body)['code']=='draftNotFound', 'ActivationContractFailed'
    assert drafts(p)==before
    status,body=request(p,'/api/v1/modules/media/audiobooks/library?page=0&limit=1')
    assert status==200 and json.loads(body)['total']==p['libraryCount']
    unchanged(p)
    override=Path(p['runtimeOverride']);assert hashlib.sha256(override.read_bytes()).hexdigest()==p['inputHashes'][str(override)]
    pins=json.loads(override.read_text());pins['services']['api'].setdefault('environment',{})[FLAG]='true';save(override,pins)
    save(ROOT/'complete.json',{'run':p['run'],'planHash':h,'invocation':inv,'status':'LIVE_READY',
        'apiContainer':cid,'image':current['Image'],'probe':'404_draftNotFound_after_capability','realImportPerformed':False})


def recover(p,h,inv):
    unchanged(p)
    if not (ROOT/'state.json').exists(): outcome='NO_REPLACEMENT_RECORDED'
    else:
        state=json.loads((ROOT/'state.json').read_text());assert (state['run'],state['planHash'],state['invocation'])==(p['run'],h,inv)
        cid=compose(p,'ps','--all','-q','api').decode().strip();current=inspect(cid)
        if cid==p['api']['Id']:
            assert fingerprint(current)==fingerprint(p['api'])
            if not current['State']['Running']:
                assert current['State']['ExitCode']==0 and not current['State']['OOMKilled']
                command(['/usr/bin/docker','--host','unix:///var/run/docker.sock','start',cid])
            outcome='EXACT_OLD_API_RETAINED_IMPORT_DISABLED'
        else:
            enabled_matches(p,current)
            outcome='API_ENABLED_RETAINED_MANUAL_REVIEW'
        if (ROOT/'complete.json').exists():
            done=json.loads((ROOT/'complete.json').read_text());assert (done['run'],done['planHash'],done['invocation'])==(p['run'],h,inv)
            outcome='COMPLETE_NO_ACTION'
    save(ROOT/'recovery.json',{'run':p['run'],'outcome':outcome})


def main():
    p,h=load()
    if sys.argv[1:]==['check']: preflight(p);print('PASS: no activation');return
    inv=os.environ.get('INVOCATION_ID','');assert re.fullmatch('[0-9a-f]{32}',inv)
    assert any(s.split(':',2)[-1].endswith('/'+p['unit']) for s in Path('/proc/self/cgroup').read_text().splitlines())
    fd=os.open(ROOT/'operation.lock',os.O_CREAT|os.O_RDWR|os.O_NOFOLLOW,0o600)
    fcntl.flock(fd,fcntl.LOCK_EX|fcntl.LOCK_NB)
    try:
        if sys.argv[1:]==['work']:work(p,h,inv)
        elif sys.argv[1:]==['recover']:recover(p,h,inv)
        else:raise RuntimeError('UnsupportedAction')
    except Exception as e:
        save(ROOT/('recovery.json' if sys.argv[1:]==['recover'] else 'failure.json'),
             {'run':p['run'],'status':'BLOCKED_NO_RESTORE','category':type(e).__name__})
        raise
    finally:os.close(fd)


if __name__=='__main__':
    try:main()
    except Exception as e:print(type(e).__name__,file=sys.stderr);sys.exit(1)
