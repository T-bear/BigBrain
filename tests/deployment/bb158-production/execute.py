#!/usr/bin/python3
"""Single BB-158 operator procedure. No downloads, activation or data rollback."""
import fcntl
import hashlib
import json
import os
from pathlib import Path
import sqlite3
import signal
import subprocess
import sys
import tarfile
import time
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parent
PLAN = ROOT / 'plan.json'
STATE = ROOT / 'state.json'
MAX_ARCHIVE = 32 * 1024 * 1024


def save(path, value):
    tmp = path.with_name(path.name + '.' + uuid.uuid4().hex)
    with open(tmp, 'x', encoding='utf8') as out:
        os.chmod(tmp, 0o600)
        json.dump(value, out, sort_keys=True)
        out.flush()
        os.fsync(out.fileno())
    os.replace(tmp, path)
    fd = os.open(path.parent, os.O_RDONLY | os.O_DIRECTORY)
    try:
        os.fsync(fd)
    finally:
        os.close(fd)


def fingerprint(i):
    obj = {k: sorted(i[k], key=lambda v: v['Destination']) if k == 'Mounts' else i[k]
           for k in ('Id', 'Image', 'Config', 'HostConfig', 'Mounts')}
    return hashlib.sha256(json.dumps(obj, sort_keys=True).encode()).hexdigest()


def command(args, timeout=60):
    r = subprocess.run(args, capture_output=True, timeout=timeout)
    if r.returncode:
        raise RuntimeError('CommandFailed')
    return r.stdout


def inspect(cid):
    return json.loads(command(['docker', 'inspect', cid]))[0]


def read_api(path):
    with urllib.request.urlopen('http://127.0.0.1:18080' + path, timeout=15) as r:
        return json.loads(r.read(2 * 1024 * 1024))


def activity():
    # Existing daemon continues fetching metadata; no torrent mutation is performed.
    jobs = read_api('/api/v1/modules/media/jobs?limit=50')
    for j in jobs['jobs']:
        assert j['provider'] == 'qBittorrent' and j['status'] == 'downloading'
        # QBittorrentClient.Positive maps zero to null in the display-only jobs DTO.
        # Null is not proof of inactivity: require numeric zero from the full queue below.
        assert j['downloadSpeedBytesPerSecond'] in (None, 0), 'JobTransferActive'
    downloads = read_api('/api/v1/modules/media/downloads')['downloads']
    active = [d for d in downloads if d['status'] != 'completed']
    assert not jobs['jobs'] or active, 'MissingQueueActivityEvidence'
    for d in active:
        assert d['diagnosis']['code'] == 'waitingForMetadata', 'QueueActivityUnsafe'
        assert type(d['downloadedBytes']) is int and d['downloadedBytes'] == 0, 'QueuePayloadPresentOrUnknown'
        assert type(d['downloadSpeedBytesPerSecond']) is int and d['downloadSpeedBytesPerSecond'] == 0, 'QueueSpeedActiveOrUnknown'


def wait_healthy(cid, seconds=100):
    end = time.monotonic() + seconds
    while time.monotonic() < end:
        i = inspect(cid)
        assert i['State']['Running'], 'ContainerNotRunning'
        health = i['State'].get('Health', {}).get('Status')
        if health == 'healthy':
            return i
        assert health != 'unhealthy', 'ContainerUnhealthy'
        time.sleep(1)
    raise TimeoutError('HealthDeadline')


def backup(cid, role):
    # Docker cp reads a cleanly stopped service; never mounts or reads Finance storage.
    source = '/config/.' if role == 'librarr' else '/data/.'
    target = ROOT / (role + '-capture.tar')
    assert not target.exists()
    total = 0
    def capture_timeout(signum, frame):
        raise TimeoutError('CaptureDeadline')
    previous = signal.signal(signal.SIGALRM, capture_timeout)
    signal.alarm(60)
    proc = subprocess.Popen(['docker', 'cp', cid + ':' + source, '-'],
                            stdout=subprocess.PIPE, stderr=subprocess.DEVNULL)
    try:
        with open(target, 'xb') as dst:
            os.chmod(target, 0o600)
            with tarfile.open(fileobj=proc.stdout, mode='r|') as incoming, \
                    tarfile.open(fileobj=dst, mode='w|') as outgoing:
                for item in incoming:
                    parts = Path(item.name).parts
                    assert not Path(item.name).is_absolute() and '..' not in parts
                    if item.isdir():
                        continue
                    assert item.isfile(), 'NonRegularCaptureEntry'
                    if role == 'api' and Path(item.name).name not in (
                            'audiobook-acquisition.db', 'audiobook-acquisition.db-wal',
                            'audiobook-acquisition.db-shm', 'audiobook-acquisition.db-journal'):
                        continue
                    total += item.size
                    assert total <= MAX_ARCHIVE, 'CaptureSizeExceeded'
                    outgoing.addfile(item, incoming.extractfile(item))
            dst.flush()
            os.fsync(dst.fileno())
        assert proc.wait(timeout=10) == 0
    finally:
        signal.alarm(0)
        signal.signal(signal.SIGALRM, previous)
        if proc.poll() is None:
            proc.kill()
        proc.wait(timeout=5)
        proc.stdout.close()
    assert total > 0
    save(ROOT / (role + '-capture.json'),
         {'sha256': hashlib.sha256(target.read_bytes()).hexdigest(), 'bytes': total})


def verify_backup(role):
    target = ROOT / (role + '-capture.tar')
    info = json.loads((ROOT / (role + '-capture.json')).read_text())
    assert hashlib.sha256(target.read_bytes()).hexdigest() == info['sha256']
    dest = ROOT / (role + '-verify')
    dest.mkdir(mode=0o700)
    with tarfile.open(target) as archive:
        archive.extractall(dest, filter='data')
    databases = list(dest.rglob('*.db'))
    expected = 'librarr.db' if role == 'librarr' else 'audiobook-acquisition.db'
    db = next(p for p in databases if p.name == expected)
    with sqlite3.connect(db.as_uri() + '?mode=ro', uri=True) as connection:
        assert connection.execute('PRAGMA integrity_check').fetchall() == [('ok',)]
        assert connection.execute('PRAGMA foreign_key_check').fetchall() == []
    save(ROOT / (role + '-backup-verified.json'), {'integrity': 'PASS', 'sha256': info['sha256']})


def same_service(old, new, candidate):
    assert new['Image'] == candidate
    assert dict(v.split('=', 1) for v in old['Config']['Env']) == dict(v.split('=', 1) for v in new['Config']['Env']), 'EnvironmentDrift'
    for key in ('Binds', 'Mounts', 'PortBindings', 'RestartPolicy', 'Privileged', 'CapAdd',
                'CapDrop', 'SecurityOpt', 'NetworkMode', 'Memory', 'MemorySwap', 'NanoCpus', 'PidsLimit'):
        assert old['HostConfig'].get(key) == new['HostConfig'].get(key), 'HostConfigDrift'
    def mounts(x):
        return sorted((m['Type'], m['Source'], m['Destination'], m['RW']) for m in x['Mounts'])
    assert mounts(old) == mounts(new), 'MountDrift'


def execute(plan, invocation, plan_hash):
    def record(role, phase, **extra):
        save(STATE, {'run': plan['run'], 'invocation': invocation, 'planHash': plan_hash,
                     'role': role, 'phase': phase, **extra})
    assert not STATE.exists(), 'AlreadyAttempted'
    assert os.statvfs(ROOT).f_bavail * os.statvfs(ROOT).f_frsize >= 20 * 1024**3
    for item in plan['originals']:
        i = inspect(item['Id'])
        assert fingerprint(i) == fingerprint(item) and i['State']['Running']
        assert i['State']['StartedAt'] == item['State']['StartedAt']
    for path, expected in plan['inputHashes'].items():
        assert hashlib.sha256(Path(path).read_bytes()).hexdigest() == expected
    activity()
    originals = {i['Config']['Labels'].get('com.docker.compose.service'): i for i in plan['originals']}
    for role in ('librarr', 'api', 'web'):
        old = originals[role]
        assert fingerprint(inspect(old['Id'])) == fingerprint(old)
        assert json.loads(command(['docker', 'image', 'inspect', plan['candidates'][role]]))[0]['Id'] == plan['candidates'][role]
        if role != 'web':
            activity()
            record(role, 'stopping-old')
            command(['docker', 'stop', '--time', str(old['Config']['StopTimeout']), old['Id']], timeout=55)
            stopped = inspect(old['Id'])
            assert not stopped['State']['Running'] and stopped['State']['ExitCode'] == 0 and not stopped['State']['OOMKilled']
            record(role, 'stopped-clean')
            backup(old['Id'], role)
            command(['docker', 'start', old['Id']])
            record(role, 'old-running')
            wait_healthy(old['Id'])
            verify_backup(role)
        record(role, 'replacement-possible')
        command(plan['composeCommand'] + ['up', '-d', '--no-deps', '--no-build', '--pull', 'never',
                                         '--wait', '--wait-timeout', '100', role], timeout=150)
        cid = command(plan['composeCommand'] + ['ps', '-q', role]).decode().strip()
        new = wait_healthy(cid)
        same_service(old, new, plan['candidates'][role])
        if role == 'librarr':
            env = dict(v.split('=', 1) for v in new['Config']['Env'])
            ip = next(n['IPAddress'] for n in new['NetworkSettings']['Networks'].values() if n['IPAddress'])
            request = urllib.request.Request('http://' + ip + ':5050/api/v1/direct-audio-capabilities',
                                             headers={'X-Api-Key': env['API_KEY']})
            with urllib.request.urlopen(request, timeout=10) as response:
                cap = json.load(response)
            assert cap['contract'] == 'bb158-direct-audio-v2' and cap['maximumBytes'] == 67108864
            assert set(cap['formats']) == {'mp3', 'm4a'}
        # Persist only this successful service image in the existing runtime override.
        override = Path(plan['runtimeOverride'])
        pins = json.loads(override.read_text())
        assert pins['services'][role]['image'] == old['Image']
        pins['services'][role]['image'] = plan['candidates'][role]
        save(override, pins)
        record(role, 'replacement-verified', newContainer=cid)
        save(ROOT / (role + '-deployed.json'), {'image': new['Image'], 'container': cid, 'health': 'healthy'})
    for old in plan['originals']:
        if old['Config']['Labels'].get('com.docker.compose.service') not in ('librarr', 'api', 'web'):
            new = inspect(old['Id'])
            assert fingerprint(old) == fingerprint(new) and new['State']['StartedAt'] == old['State']['StartedAt']
    record('web', 'complete')
    save(ROOT / 'complete.json', {'status': 'DEPLOYED_IMPORT_DISABLED', 'run': plan['run']})


def recover(plan, invocation, plan_hash):
    if not STATE.exists():
        save(ROOT / 'recovery.json', {'outcome': 'NO_STOP_RECORDED'})
        return
    state = json.loads(STATE.read_text())
    assert state['run'] == plan['run'] and state['invocation'] == invocation and state['planHash'] == plan_hash
    if state['phase'] == 'complete':
        save(ROOT / 'recovery.json', {'outcome': 'COMPLETE_NO_ACTION'})
        return
    assert state['phase'] in ('stopping-old', 'stopped-clean', 'old-running'), 'ReplacementRequiresContainment'
    old = next(i for i in plan['originals'] if i['Config']['Labels'].get('com.docker.compose.service') == state['role'])
    info = inspect(old['Id'])
    assert fingerprint(info) == fingerprint(old)
    if state['phase'] == 'stopping-old' and info['State']['Running']:
        command(['docker', 'wait', old['Id']], timeout=45)
        info = inspect(old['Id'])
    if not info['State']['Running']:
        assert info['State']['ExitCode'] == 0 and not info['State']['OOMKilled']
        command(['docker', 'start', old['Id']], timeout=30)
        assert inspect(old['Id'])['State']['Running']
    save(ROOT / 'recovery.json', {'outcome': 'EXACT_OLD_RUNNING', 'role': state['role']})


def main():
    assert os.getuid() == 1000 and not sys.flags.optimize
    assert ROOT.stat().st_mode & 0o077 == 0 and not ROOT.is_symlink()
    expected = json.loads((ROOT / 'package.json').read_text())
    assert hashlib.sha256(Path(__file__).read_bytes()).hexdigest() == expected['execute.py']
    raw = PLAN.read_bytes(); plan_hash = hashlib.sha256(raw).hexdigest()
    assert plan_hash == expected['plan.json']
    plan = json.loads(raw)
    invocation = os.environ.get('INVOCATION_ID', '')
    assert len(invocation) == 32 and plan['unit'] in Path('/proc/self/cgroup').read_text()
    fd = os.open(ROOT / 'operation.lock', os.O_CREAT | os.O_RDWR | os.O_NOFOLLOW, 0o600)
    fcntl.flock(fd, fcntl.LOCK_EX | fcntl.LOCK_NB)
    try:
        if sys.argv[1:] == ['work']:
            execute(plan, invocation, plan_hash)
        elif sys.argv[1:] == ['recover']:
            try:
                recover(plan, invocation, plan_hash)
            except Exception:
                save(ROOT / 'recovery.json', {'outcome': 'BLOCKED_NO_RESTORE'})
                raise
        else:
            raise ValueError('UnsupportedAction')
    finally:
        os.close(fd)


if __name__ == '__main__':
    try:
        if sys.flags.optimize:
            raise RuntimeError('OptimizedPythonNotAllowed')
        main()
    except Exception as error:
        print(type(error).__name__, file=sys.stderr)
        sys.exit(1)
