#!/usr/bin/python3
"""One owner-launched Web-only continuation. Never captures/restores application data."""
import fcntl
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import urllib.request

from execute import fingerprint, save, same_service

ROOT = Path(__file__).resolve().parent


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def command(args, timeout=30):
    # Do not inherit Compose profiles, orphan removal, Docker context or remote daemon settings.
    env = {'PATH': '/usr/bin:/bin', 'HOME': os.environ['HOME'], 'LANG': 'C',
           'COMPOSE_REMOVE_ORPHANS': 'false', 'COMPOSE_IGNORE_ORPHANS': 'true'}
    result = subprocess.run(args, capture_output=True, timeout=timeout, env=env)
    if result.returncode:
        raise RuntimeError('CommandFailed')
    return result.stdout


def docker(*args, timeout=30):
    return command(['/usr/bin/docker', '--host', 'unix:///var/run/docker.sock', *args], timeout)


def inspect(cid):
    assert re.fullmatch('[0-9a-f]{64}', cid), 'InvalidContainerIdentity'
    return json.loads(docker('inspect', cid))[0]


def compose(plan, *args, timeout=30):
    # No caller-controlled service or arbitrary command prefix in the private plan.
    return docker('compose', '--project-name', plan['project'], '--project-directory', str(ROOT),
                  '--env-file', str(ROOT / 'empty.env'), '-f', str(ROOT / 'web-compose.json'),
                  *args, timeout=timeout)


def old_web(plan):
    matches = [i for i in plan['originals'] if i['Config']['Labels'].get('com.docker.compose.service') == 'web'
               and i['Config']['Labels'].get('com.docker.compose.project') == plan['project']]
    assert len(matches) == 1
    return matches[0]


def unchanged(plan, include_web=False):
    web_id = old_web(plan)['Id']
    for old in plan['originals']:
        if include_web or old['Id'] != web_id:
            new = inspect(old['Id'])
            assert fingerprint(new) == fingerprint(old), 'ContainerDrift'
            assert new['State']['StartedAt'] == old['State']['StartedAt'] and new['State']['Running']
            if old['State'].get('Health', {}).get('Status') == 'healthy':
                assert new['State'].get('Health', {}).get('Status') == 'healthy', 'HealthDrift'


def bundle():
    assert not sys.flags.optimize and os.getuid() == 1000
    assert ROOT.resolve() == ROOT and ROOT.stat().st_mode & 0o077 == 0
    expected = json.loads((ROOT / 'package.json').read_text())
    assert set(expected) == {'web_only.py', 'execute.py', 'plan.json', 'web-compose.json', 'empty.env'}
    for name, value in expected.items():
        path = ROOT / name
        assert path.resolve() == path and path.stat().st_uid == 1000 and path.stat().st_mode & 0o077 == 0
        assert digest(path) == value, 'PackageChanged'
    plan = json.loads((ROOT / 'plan.json').read_text())
    assert re.fullmatch('[0-9a-f]{32}', plan['run'])
    assert plan['unit'] == 'bb158-web-' + plan['run'] + '.service'
    assert re.fullmatch('[a-z0-9][a-z0-9_-]+', plan['project'])
    assert len({i['Id'] for i in plan['originals']}) == len(plan['originals'])
    cfg = json.loads((ROOT / 'web-compose.json').read_text())
    assert set(cfg) == {'name', 'services', 'networks'} and cfg['name'] == plan['project']
    assert set(cfg['services']) == {'web'}, 'WebOnlyScopeRequired'
    web = cfg['services']['web']
    assert set(web) <= {'command', 'entrypoint', 'healthcheck', 'image', 'networks', 'ports', 'restart', 'stop_grace_period'}
    assert web['image'] == plan['candidate'] and not old_web(plan)['Mounts'], 'StatelessWebRequired'
    assert set(web['networks']) == set(cfg['networks'])
    for network in cfg['networks'].values():
        assert set(network) == {'name', 'external', 'ipam'} and network['external'] is True
        assert network['ipam'] == {}  # Compose normalizes an external network to empty IPAM.
    assert (ROOT / 'empty.env').read_bytes() == b''
    return plan, expected['plan.json']


def preflight(plan):
    unchanged(plan, include_web=True)
    for path, value in plan['inputHashes'].items():
        p = Path(path)
        assert p.resolve() == p and digest(p) == value, 'InputDrift'
    image = json.loads(docker('image', 'inspect', plan['candidate']))[0]
    assert image['Id'] == plan['candidate']
    assert image['Config']['Labels']['org.opencontainers.image.revision'] == plan['acceptedMain']
    actual = json.loads(compose(plan, 'config', '--format', 'json'))
    assert actual == json.loads((ROOT / 'web-compose.json').read_text()), 'ComposeDrift'
    old = old_web(plan)
    assert set(old['NetworkSettings']['Networks']) == {v['name'] for v in actual['networks'].values()}
    pins = json.loads(Path(plan['runtimeOverride']).read_text())
    assert pins['services']['web']['image'] == old['Image']
    assert os.statvfs(ROOT).f_bavail * os.statvfs(ROOT).f_frsize >= 20 * 1024**3


def record(plan, invocation, plan_hash, phase):
    save(ROOT / 'state.json', {'run': plan['run'], 'invocation': invocation, 'planHash': plan_hash, 'phase': phase})


def state_matches(value, plan, invocation, plan_hash):
    assert value['run'] == plan['run'] and value['invocation'] == invocation and value['planHash'] == plan_hash, 'StateIdentityMismatch'


def verify_web(plan, new, image):
    old = old_web(plan)
    same_service(old, new, image)
    for field in ('User', 'Entrypoint', 'Cmd', 'WorkingDir', 'Healthcheck', 'StopTimeout'):
        assert old['Config'].get(field) == new['Config'].get(field), 'WebConfigDrift'
    assert new['Config']['Labels'].get('com.docker.compose.project') == plan['project']
    assert new['Config']['Labels'].get('com.docker.compose.service') == 'web'
    assert set(new['NetworkSettings']['Networks']) == set(old['NetworkSettings']['Networks'])
    assert new['State']['Running'] and new['State']['Health']['Status'] == 'healthy'
    assert not new['State']['OOMKilled']


def current_web(plan):
    cid = compose(plan, 'ps', '--all', '-q', 'web').decode().strip()
    return inspect(cid)


def execute(plan, invocation, plan_hash):
    assert not (ROOT / 'state.json').exists() and not (ROOT / 'complete.json').exists(), 'AlreadyAttempted'
    preflight(plan)
    # Write-ahead marker before the ONLY production replacement command; no explicit stop.
    record(plan, invocation, plan_hash, 'web-replacement-possible')
    compose(plan, 'up', '-d', '--no-deps', '--no-build', '--pull', 'never', '--wait', '--wait-timeout', '100', 'web', timeout=150)
    new = current_web(plan)
    verify_web(plan, new, plan['candidate'])
    assert new['Id'] != old_web(plan)['Id']
    # Compare served index to the preverified candidate commitment, via its published port.
    port = new['NetworkSettings']['Ports']['8080/tcp'][0]['HostPort']
    with urllib.request.urlopen('http://127.0.0.1:' + port + '/', timeout=10) as response:
        assert response.status == 200
        content = response.read(1024 * 1024 + 1)
    assert len(content) <= 1024 * 1024 and hashlib.sha256(content).hexdigest() == plan['indexSha256'], 'ServedWebMismatch'
    unchanged(plan)
    override = Path(plan['runtimeOverride'])
    assert digest(override) == plan['inputHashes'][str(override)], 'OverrideDrift'
    pins = json.loads(override.read_text())
    assert pins['services']['web']['image'] == old_web(plan)['Image']
    pins['services']['web']['image'] = plan['candidate']
    save(override, pins)
    record(plan, invocation, plan_hash, 'complete')
    save(ROOT / 'complete.json', {'run': plan['run'], 'planHash': plan_hash, 'invocation': invocation,
        'status': 'WEB_DEPLOYED_IMPORT_UNCHANGED_DISABLED', 'container': new['Id'], 'image': new['Image']})


def recover(plan, invocation, plan_hash):
    unchanged(plan)
    state = ROOT / 'state.json'
    if not state.exists():
        outcome = 'NO_REPLACEMENT_RECORDED'
    else:
        value = json.loads(state.read_text()); state_matches(value, plan, invocation, plan_hash)
        assert value['phase'] in ('web-replacement-possible', 'complete')
        new = current_web(plan)
        old = old_web(plan)
        if new['Id'] == old['Id']:
            assert fingerprint(new) == fingerprint(old)
            if not new['State']['Running']:
                assert new['State']['ExitCode'] == 0 and not new['State']['OOMKilled']
                # The only recovery start: exact unchanged old stateless Web, never another service.
                docker('start', old['Id'])
            outcome = 'EXACT_OLD_WEB_RUNNING'
            assert inspect(old['Id'])['State']['Running']
        else:
            verify_web(plan, new, plan['candidate'])
            outcome = 'CANDIDATE_HEALTHY_RETAINED'
        if value['phase'] == 'complete':
            done = json.loads((ROOT / 'complete.json').read_text())
            state_matches(done, plan, invocation, plan_hash)
            outcome = 'COMPLETE_NO_ACTION'
    save(ROOT / 'recovery.json', {'run': plan['run'], 'invocation': invocation, 'planHash': plan_hash, 'outcome': outcome})


def unit_arguments(plan):
    return ['sudo', 'systemd-run', '--no-block', '--collect', '--unit=' + plan['unit'],
            '-p', 'Type=oneshot', '-p', 'User=1000', '-p', 'Group=1000', '-p', 'SupplementaryGroups=docker',
            '-p', 'UMask=0077', '-p', 'NoNewPrivileges=yes', '-p', 'CapabilityBoundingSet=',
            '-p', 'TimeoutStartSec=300s', '-p', 'TimeoutStopSec=90s', '-p', 'KillMode=control-group', '-p', 'Restart=no',
            '-p', 'ExecStopPost=/usr/bin/python3 -B ' + str(ROOT / 'web_only.py') + ' recover',
            '/usr/bin/python3', '-B', str(ROOT / 'web_only.py'), 'work']


def main():
    if sys.flags.optimize:
        raise RuntimeError('OptimizedPythonNotAllowed')
    plan, plan_hash = bundle()
    action = sys.argv[1:]
    if action == ['check']:
        preflight(plan); print('PASS: read-only Web preflight; no deployment'); return
    if action == ['launch']:
        assert not any((ROOT / f).exists() for f in ('queued.json', 'state.json', 'complete.json', 'recovery.json'))
        preflight(plan)
        subprocess.run(['sudo', '-v'], check=True, timeout=120)
        # Exclusive durable claim prevents double queueing, even after an ambiguous launch failure.
        with open(ROOT / 'queued.json', 'x') as out:
            os.fchmod(out.fileno(), 0o600)
            json.dump({'run': plan['run'], 'planHash': plan_hash, 'status': 'QUEUED_NOT_COMPLETED'}, out)
            out.flush(); os.fsync(out.fileno())
        fd = os.open(ROOT, os.O_DIRECTORY); os.fsync(fd); os.close(fd)
        subprocess.run(unit_arguments(plan), check=True, timeout=30)
        print('QUEUED: Web only. Read complete.json/recovery.json; do not retry.'); return
    assert action in (['work'], ['recover'])
    invocation = os.environ.get('INVOCATION_ID', '')
    assert re.fullmatch('[0-9a-f]{32}', invocation)
    assert any(line.split(':', 2)[-1].rstrip().endswith('/' + plan['unit']) for line in Path('/proc/self/cgroup').read_text().splitlines())
    queued = json.loads((ROOT / 'queued.json').read_text())
    assert queued['run'] == plan['run'] and queued['planHash'] == plan_hash
    fd = os.open(ROOT / 'operation.lock', os.O_CREAT | os.O_RDWR | os.O_NOFOLLOW, 0o600)
    fcntl.flock(fd, fcntl.LOCK_EX | fcntl.LOCK_NB)
    try:
        if action == ['work']:
            try:
                execute(plan, invocation, plan_hash)
            except Exception as error:
                save(ROOT / 'failure.json', {'run': plan['run'], 'invocation': invocation,
                    'planHash': plan_hash, 'status': 'FAILED', 'category': type(error).__name__})
                raise
        else:
            try:
                recover(plan, invocation, plan_hash)
            except Exception:
                save(ROOT / 'recovery.json', {'run': plan['run'], 'outcome': 'BLOCKED_NO_RESTORE'})
                raise
    finally:
        os.close(fd)


if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        print(type(error).__name__, file=sys.stderr)
        sys.exit(1)
