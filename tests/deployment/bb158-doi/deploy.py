#!/usr/bin/python3
"""Single-use API then Web DOI deployment using the existing BB-158 guard helpers."""
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
from execute import save, fingerprint, same_service, backup, verify_backup, wait_healthy, activity

ROOT = Path(__file__).resolve().parent
FLAG = 'Media__Librarr__DirectAudioImportEnabled'
ROLES = ('api', 'web')


def digest(p):
    return hashlib.sha256(p.read_bytes()).hexdigest()


def command(args, timeout=30):
    r = subprocess.run(args, capture_output=True, timeout=timeout, env={
        'PATH':'/usr/bin:/bin', 'HOME':os.environ['HOME'], 'LANG':'C',
        'COMPOSE_REMOVE_ORPHANS':'false', 'COMPOSE_IGNORE_ORPHANS':'true'})
    if r.returncode: raise RuntimeError('CommandFailed')
    return r.stdout


def docker(*args, timeout=30):
    return command(['/usr/bin/docker','--host','unix:///var/run/docker.sock',*args], timeout)


def inspect(cid):
    assert re.fullmatch('[0-9a-f]{64}',cid)
    return json.loads(docker('inspect',cid))[0]


def compose(p,*args,timeout=30):
    return docker('compose','--project-name',p['project'],'--project-directory',str(ROOT),
        '--env-file',str(ROOT/'empty.env'),'-f',str(ROOT/'release.compose.json'),*args,timeout=timeout)


def request(p,path,method='GET'):
    req=urllib.request.Request(p['webBase']+path,method=method)
    try:
        with urllib.request.urlopen(req,timeout=20) as r:return r.status,r.read(2*1024*1024)
    except urllib.error.HTTPError as e:return e.code,e.read(16384)


def imports(p):
    code,body=request(p,'/api/v1/modules/media/url-imports/')
    assert code==200
    rows=json.loads(body)
    assert rows==p['drafts'] and not any(r['status'] in ('downloading','importing','indexing','uncertain') for r in rows), 'ImportActivityOrDrift'
    return rows


def old(p,role):
    return p['targets'][role]


def unchanged(p,all_services=False):
    ids={old(p,r)['Id'] for r in ROLES}
    for previous in p['originals']:
        if not all_services and previous['Id'] in ids:continue
        current=inspect(previous['Id'])
        assert fingerprint(current)==fingerprint(previous), 'UnrelatedServiceDrift'
        assert current['State']['Running'] and current['State']['StartedAt']==previous['State']['StartedAt']
        if previous['State'].get('Health',{}).get('Status')=='healthy':assert current['State']['Health']['Status']=='healthy'


def verify_service(p,role,c):
    previous=old(p,role)
    same_service(previous,c,p['candidates'][role])
    for field in ('User','Cmd','Entrypoint','WorkingDir','Healthcheck','StopTimeout'):
        assert c['Config'].get(field)==previous['Config'].get(field), 'RuntimeConfigDrift'
    assert c['Config']['Labels']['com.docker.compose.service']==role
    assert c['Config']['Labels']['com.docker.compose.project']==p['project']
    assert c['Config']['Labels']['org.opencontainers.image.revision']==p['revision']
    assert set(c['NetworkSettings']['Networks'])==set(previous['NetworkSettings']['Networks'])
    assert c['State']['Running'] and c['State']['Health']['Status']=='healthy' and not c['State']['OOMKilled']
    if role=='api':assert dict(x.split('=',1) for x in c['Config']['Env'])[FLAG]=='true'


def current(p,role):
    return inspect(compose(p,'ps','--all','-q',role).decode().strip())


def load():
    assert not sys.flags.optimize and os.getuid()==1000
    assert ROOT.resolve()==ROOT and ROOT.stat().st_mode&0o077==0
    hashes=json.loads((ROOT/'package.json').read_text())
    assert set(hashes)=={'deploy.py','execute.py','plan.json','release.compose.json','empty.env'}
    for name,h in hashes.items():
        f=ROOT/name
        assert f.resolve()==f and f.stat().st_uid==1000 and f.stat().st_mode&0o077==0
        assert digest(f)==h, 'PackageChanged'
    p=json.loads((ROOT/'plan.json').read_text())
    assert re.fullmatch('[0-9a-f]{32}',p['run']) and p['unit']=='bb158-doi-'+p['run']+'.service'
    assert set(p['targets'])==set(ROLES) and set(p['candidates'])==set(ROLES)
    cfg=json.loads((ROOT/'release.compose.json').read_text())
    assert cfg==p['compose'] and set(cfg)=={'name','services','networks','volumes'}
    assert set(cfg['services'])==set(ROLES)
    for role in ROLES:
        svc=cfg['services'][role]
        assert svc['image']==p['candidates'][role] and 'build' not in svc and 'depends_on' not in svc
    assert cfg['services']['api']['environment'][FLAG]=='true'
    assert all(x.get('external') is True for x in [*cfg['volumes'].values(),*cfg['networks'].values()])
    assert (ROOT/'empty.env').read_bytes()==b''
    return p,hashes['plan.json']


def preflight(p):
    unchanged(p,True);imports(p);activity()
    for path,h in p['inputHashes'].items():
        f=Path(path);assert f.resolve()==f and digest(f)==h,'InputDrift'
    assert json.loads(compose(p,'config','--format','json'))==p['compose']
    for role in ROLES:
        image=json.loads(docker('image','inspect',p['candidates'][role]))[0]
        assert image['Id']==p['candidates'][role] and image['Config']['Labels']['org.opencontainers.image.revision']==p['revision']
    assert os.statvfs(ROOT).f_bavail*os.statvfs(ROOT).f_frsize>=20*1024**3


def verify_api(p,c):
    assert request(p,'/health')[0]==200
    imports(p)
    status,body=request(p,'/api/v1/modules/media/url-imports/'+uuid.uuid4().hex+'/start','POST')
    assert status==404 and json.loads(body)['code']=='draftNotFound', 'LiveCapabilityGateFailed'
    imports(p)
    identity=docker('exec',c['Id'],'dotnet','BigBrain.Finance.Hosting.dll','system-build-identity').decode().strip()
    assert identity=='revision='+p['revision']
    status,body=request(p,'/api/v1/modules/media/audiobooks/library?page=0&limit=1')
    assert status==200 and hashlib.sha256(body).hexdigest()==p['libraryResponseSha256'], 'LibraryDrift'


def verify_web(p):
    status,body=request(p,'/')
    assert status==200 and hashlib.sha256(body).hexdigest()==p['indexSha256'], 'WebIdentityMismatch'
    status,body=request(p,p['jsPath'])
    assert status==200 and hashlib.sha256(body).hexdigest()==p['jsSha256'], 'WebBundleMismatch'
    assert request(p,'/health')[0]==200
    imports(p)


def record(p,h,inv,role,phase):
    save(ROOT/'state.json',{'run':p['run'],'planHash':h,'invocation':inv,'role':role,'phase':phase})


def work(p,h,inv):
    assert not (ROOT/'state.json').exists() and not (ROOT/'complete.json').exists(), 'AlreadyAttempted'
    preflight(p)
    # Existing stopped-clean Media-only capture. No Finance file is captured or restored.
    record(p,h,inv,'api','stopping-old')
    docker('stop','--time',str(old(p,'api')['Config']['StopTimeout']),old(p,'api')['Id'],timeout=55)
    stopped=inspect(old(p,'api')['Id'])
    assert not stopped['State']['Running'] and stopped['State']['ExitCode']==0 and not stopped['State']['OOMKilled']
    record(p,h,inv,'api','stopped-clean')
    backup(old(p,'api')['Id'],'api')
    docker('start',old(p,'api')['Id']);wait_healthy(old(p,'api')['Id'])
    record(p,h,inv,'api','old-running');verify_backup('api')
    override=Path(p['runtimeOverride']); expected_hash=p['inputHashes'][str(override)]
    for role in ROLES:
        imports(p);activity();unchanged(p)
        before=inspect(old(p,role)['Id'])
        assert fingerprint(before)==fingerprint(old(p,role)) and before['State']['Running']
        assert digest(override)==expected_hash,'OverrideDrift'
        record(p,h,inv,role,'replacement-possible')
        compose(p,'up','-d','--no-deps','--no-build','--pull','never','--wait','--wait-timeout','100',role,timeout=150)
        c=current(p,role);verify_service(p,role,c)
        if role=='api':verify_api(p,c)
        else:verify_web(p)
        unchanged(p)
        assert digest(override)==expected_hash
        pins=json.loads(override.read_text());assert pins['services'][role]['image']==old(p,role)['Image']
        pins['services'][role]['image']=p['candidates'][role];save(override,pins);expected_hash=digest(override)
        save(ROOT/(role+'-deployed.json'),{'run':p['run'],'planHash':h,'invocation':inv,'image':c['Image'],'container':c['Id'],'health':'healthy'})
        record(p,h,inv,role,'verified')
    for role in ROLES:verify_service(p,role,current(p,role))
    unchanged(p);imports(p)
    save(ROOT/'complete.json',{'run':p['run'],'planHash':h,'invocation':inv,'status':'DOI_OPTIONAL_LIVE_READY','images':p['candidates'],'importEnabled':True,'realImportPerformed':False})


def recover(p,h,inv):
    unchanged(p)
    if not (ROOT/'state.json').exists():outcome='NO_STOP_RECORDED'
    else:
        state=json.loads((ROOT/'state.json').read_text())
        assert (state['run'],state['planHash'],state['invocation'])==(p['run'],h,inv)
        for role in ROLES:
            c=current(p,role)
            if c['Id']==old(p,role)['Id']:
                assert fingerprint(c)==fingerprint(old(p,role))
                if role=='api' and state['role']=='api' and state['phase']=='stopping-old' and c['State']['Running']:
                    docker('wait',c['Id'],timeout=45);c=inspect(c['Id'])
                if not c['State']['Running']:
                    assert state['role']==role and c['State']['Status']=='exited'
                    assert c['State']['ExitCode']==0 and not c['State']['OOMKilled']
                    docker('start',c['Id']);wait_healthy(c['Id'])
            else:verify_service(p,role,c)
        outcome='SERVICES_RETAINED_MANUAL_REVIEW'
        if (ROOT/'complete.json').exists():
            done=json.loads((ROOT/'complete.json').read_text())
            assert (done['run'],done['planHash'],done['invocation'])==(p['run'],h,inv)
            outcome='COMPLETE_NO_ACTION'
    save(ROOT/'recovery.json',{'run':p['run'],'outcome':outcome})


def main():
    p,h=load()
    if sys.argv[1:]==['check']:preflight(p);print('PASS: read-only preflight, no deployment');return
    inv=os.environ.get('INVOCATION_ID','');assert re.fullmatch('[0-9a-f]{32}',inv)
    assert any(s.split(':',2)[-1].endswith('/'+p['unit']) for s in Path('/proc/self/cgroup').read_text().splitlines())
    fd=os.open(ROOT/'operation.lock',os.O_CREAT|os.O_RDWR|os.O_NOFOLLOW,0o600);fcntl.flock(fd,fcntl.LOCK_EX|fcntl.LOCK_NB)
    try:
        if sys.argv[1:]==['work']:work(p,h,inv)
        elif sys.argv[1:]==['recover']:recover(p,h,inv)
        else:raise RuntimeError('UnsupportedAction')
    except Exception as e:
        save(ROOT/('recovery.json' if sys.argv[1:]==['recover'] else 'failure.json'),{'run':p['run'],'status':'BLOCKED_NO_RESTORE','category':type(e).__name__})
        raise
    finally:os.close(fd)


if __name__=='__main__':
    try:main()
    except Exception as e:print(type(e).__name__,file=sys.stderr);sys.exit(1)
