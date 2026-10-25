#!/usr/bin/python3
"""Bounded SchoolLunch API/Web operator procedure; authorized source activation; no data restore."""
import copy
import fcntl
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import time
import urllib.request

REV = 'ff17bf46c7882ff3d046b07a037409ca59de3523'
OLD_REV = '485aeda2ec5819ffc7f433b51ac867c55157c19f'
OLD = {'api': 'sha256:b0bb52d525770660d059dd15f97a11d24eeafb667da14360637507b7e1c509aa',
       'web': 'sha256:a77aa1c05a9dc63a402ca9639689a971d26dc3a9a804587ae1f930ffc434416f'}
BACKUP = '/var/backups/bigbrain-school-lunch/20261025T182326Z-9479fced116f4ab58ba97219372faaa1/manifest.json'
BACKUP_SHA = '09ccfdd17da297d307f8a76b0af69a2dfafaf16c77fcf56ce1eab167f8aec209'
FLAGS = {'MealPlanner__SchoolMeals__' + k: 'true' for k in ('Enabled', 'RosenfeldtEnabled', 'MusikugglanEnabled')}
PACKAGE_FILES = ('runner.py', 'plan.json', 'release.compose.json', 'rollback.compose.json', 'backup.py', 'backup-runtime.json', 'verify-backup.py')
ROLES = ('api', 'web')
DBS = ('/data/meal-planner.db', '/data/audiobook-acquisition.db', '/shopping-data/shopping-list.db',
       '/calendar-data/calendar.db', '/settings-data/settings.db', '/lifecycle-data/lifecycle.db')
ENV = {'PATH': '/usr/bin:/bin', 'DOCKER_HOST': 'unix:///var/run/docker.sock',
       'COMPOSE_REMOVE_ORPHANS': 'false', 'COMPOSE_IGNORE_ORPHANS': 'true'}
CONFIG_FIELDS = ('User', 'Cmd', 'Entrypoint', 'WorkingDir', 'Healthcheck', 'StopTimeout', 'StopSignal')


class Blocked(Exception):
    pass


def need(value, code):
    if not value:
        raise Blocked(code)


def command(args, timeout=30, env=None):
    r = subprocess.run(args, capture_output=True, timeout=timeout, env=env or ENV)
    need(r.returncode == 0, 'CommandFailed')
    return r.stdout


def docker(*args, timeout=30):
    return command(['/usr/bin/docker', '--host', 'unix:///var/run/docker.sock', *args], timeout)


def inspect(cid):
    return json.loads(docker('inspect', cid))[0]


def hash_file(p):
    with Path(p).open('rb') as f:
        return hashlib.file_digest(f, 'sha256').hexdigest()


def save(path, value):
    temp = path.with_suffix('.pending')
    with temp.open('w') as f:
        json.dump(value, f, sort_keys=True, indent=2)
        f.flush()
        os.fsync(f.fileno())
    os.replace(temp, path)
    fd = os.open(path.parent, os.O_RDONLY | os.O_DIRECTORY)
    try:
        os.fsync(fd)
    finally:
        os.close(fd)


def normalized(value):
    if isinstance(value, dict):
        return {k: normalized(v) for k, v in value.items()}
    if isinstance(value, list):
        return sorted((normalized(v) for v in value), key=lambda v: json.dumps(v, sort_keys=True))
    return value


def service_config(c):
    return {'config': {k: c['Config'].get(k) for k in CONFIG_FIELDS},
            'env': dict(x.split('=', 1) for x in c['Config']['Env']),
            'host': normalized(c['HostConfig']),
            'mounts': sorted((m['Type'], m.get('Name'), m['Source'], m['Destination'], m['RW'], m.get('Driver')) for m in c['Mounts']),
            'networks': sorted(c['NetworkSettings']['Networks'])}


def fingerprint(c):
    return hashlib.sha256(json.dumps(service_config(c), sort_keys=True).encode()).hexdigest()


def healthy(c):
    need(c['State']['Running'] and c['State'].get('Health', {}).get('Status') == 'healthy'
         and not c['State']['OOMKilled'], 'NotHealthy')


def flags_off(c):
    for item in c['Config']['Env']:
        key, value = item.split('=', 1)
        key = key.replace('__', ':').lower()
        if key.startswith('mealplanner:schoolmeals:'):
            need(value.lower() == 'false', 'SchoolSourceEnabled')
    need(not any('schoolmeals' in str(x).lower() for x in c['Config'].get('Cmd') or []), 'UnexpectedSchoolArgument')


def expected_config(p, role):
    result = service_config(p['targets'][role])
    if role == 'api':
        result['env'].update(FLAGS)
    return result


def flags_on(c):
    env = dict(x.split('=', 1) for x in c['Config']['Env'])
    need(all(env.get(k) == v for k, v in FLAGS.items()), 'SchoolFlagsNotEnabled')
    need(not any('schoolmeals' in str(x).lower() for x in c['Config'].get('Cmd') or []), 'UnexpectedSchoolArgument')


def endpoint(role, path):
    port = 18080 if role == 'api' else 13000
    with urllib.request.urlopen('http://127.0.0.1:' + str(port) + path, timeout=15) as r:
        need(r.status == 200, 'HttpCheckFailed')
        data = r.read(2 * 1024 * 1024 + 1)
        need(len(data) <= 2 * 1024 * 1024, 'HttpResponseTooLarge')
        return data


def db_identity(cid):
    # Ordinary container-user stat only; never SQL or file contents, never Finance paths.
    rows = docker('exec', cid, '/usr/bin/stat', '-c', '%d:%i', *DBS).decode().splitlines()
    need(len(rows) == 6 and all(re.fullmatch(r'\d+:\d+', x) for x in rows), 'DatabaseIdentityUnavailable')
    return rows


def inbound_connections(text):
    count = 0
    for row in text.splitlines():
        cells = row.split()
        if not cells or cells[0] == 'sl':
            continue
        need(len(cells) >= 4 and ':' in cells[1], 'TcpInspectionInvalid')
        if cells[1].rsplit(':', 1)[1].upper() == '1F90' and cells[3] not in ('0A', '06', '07'):
            count += 1
    return count


def quiescent(cid):
    # Owner must pause users/integrations; sockets conservatively include reads/keepalives.
    for sample in range(3):
        rows = docker('exec', cid, '/bin/cat', '/proc/net/tcp', '/proc/net/tcp6').decode()
        need(inbound_connections(rows) == 0, 'ApiConnectionsPresent')
        if sample < 2:
            time.sleep(1)


def check_other(p):
    for old in p['others']:
        c = inspect(old['id'])
        need(c['Image'] == old['image'] and fingerprint(c) == old['fingerprint']
             and c['State']['Running'] and c['State']['StartedAt'] == old['started'], 'OtherServiceChanged')


def compose(root, p, *args, timeout=30):
    return docker('compose', '--project-name', p['project'], '--project-directory', str(root),
                  '--env-file', '/dev/null', '-f', str(root / 'release.compose.json'), *args, timeout=timeout)


def validate_release(p, cfg):
    need(set(cfg['services']) == set(ROLES), 'ServiceScope')
    need(all(x.get('external') is True for x in [*cfg.get('volumes', {}).values(), *cfg['networks'].values()]), 'NonExternalStorageOrNetwork')
    for role in ROLES:
        s = cfg['services'][role]
        need(s['image'] == p['candidates'][role] and not any(k in s for k in ('build', 'depends_on', 'profiles', 'extends')), 'ReleaseScope')
        need(s == p['expectedServices'][role], 'ServiceConfigChanged')


def preflight(root, p):
    validate_release(p, json.loads((root / 'release.compose.json').read_text()))
    validate_rollback(root, p)
    check_other(p)
    for role in ROLES:
        old = p['targets'][role]
        c = inspect(old['Id'])
        healthy(c)
        need(c['Image'] == OLD[role] and fingerprint(c) == fingerprint(old)
             and c['State']['StartedAt'] == old['State']['StartedAt'], 'OriginalChanged')
        i = json.loads(docker('image', 'inspect', p['candidates'][role]))[0]
        need(i['Id'] == p['candidates'][role] and i['Config']['Labels']['org.opencontainers.image.revision'] == REV, 'CandidateChanged')
    backup_plan = json.loads((root / 'backup-runtime.json').read_text())
    need(backup_plan['runtime']['containerId'] == p['targets']['api']['Id'] and backup_plan['runtime']['image'] == OLD['api'], 'BackupPlanDrift')
    selected = {m['Destination']: {k: m[k] for k in ('Type', 'Name', 'Source', 'Driver', 'RW')} for m in p['targets']['api']['Mounts'] if m['Destination'] in backup_plan['runtime']['volumes']}
    need(selected == backup_plan['runtime']['volumes'], 'BackupVolumesDrift')
    flags_off(p['targets']['api'])
    need(db_identity(p['targets']['api']['Id']) == p['databases'], 'DatabaseIdentityDrift')
    for role in ROLES:
        service = json.loads((root / 'release.compose.json').read_text())['services'][role]
        need(service['environment'] == expected_config(p, role)['env'], 'ReleaseEnvironmentChanged')
    rendered = json.loads(compose(root, p, 'config', '--format', 'json'))
    need(set(rendered['services']) == set(ROLES), 'RenderedServiceScope')
    for role in ROLES:
        need(rendered['services'][role]['environment'] == expected_config(p, role)['env'],
             'ComposeEnvironmentChanged')
    need(hashlib.sha256(endpoint('api', '/api/v1/modules/meal-planner/meals')).hexdigest() == p['mealsHash'], 'HouseholdChanged')
    drafts = json.loads(endpoint('api', '/api/v1/modules/media/url-imports/'))
    need(isinstance(drafts, list) and all(x['status'] in ('previewed', 'confirmed', 'completed', 'failed') for x in drafts), 'InFlightUrlImport')
    for filename, expected in p['originalConfigHashes'].items():
        need(hash_file(filename) == expected, 'PrivateConfigChanged')


def wait_healthy(cid):
    deadline = time.monotonic() + 100
    while True:
        c = inspect(cid)
        if c['State'].get('Health', {}).get('Status') == 'healthy':
            healthy(c)
            return c
        need(c['State']['Running'] and time.monotonic() < deadline, 'HealthDeadline')
        time.sleep(2)


def verify_service(p, role, c):
    healthy(c)
    need(c['Image'] == p['candidates'][role] and service_config(c) == expected_config(p, role), 'CandidateConfigDrift')
    need(c['Config']['Labels']['org.opencontainers.image.revision'] == REV, 'RevisionDrift')
    need(c['Config']['Labels']['com.docker.compose.service'] == role
         and c['Config']['Labels']['com.docker.compose.project'] == p['project'], 'ServiceIdentityDrift')
    if role == 'api':
        flags_on(c)
        need(db_identity(c['Id']) == p['databases'], 'DatabaseIdentityDrift')


def functional(role, p):
    endpoint(role, '/health')
    need(hashlib.sha256(endpoint(role, '/api/v1/modules/meal-planner/meals')).hexdigest() == p['mealsHash'], 'HouseholdChanged')
    for school in ('rosenfeldt', 'musikugglan'):
        result = json.loads(endpoint(role, '/api/v1/modules/meal-planner/school-meals?schoolId=' + school))
        need(result['schoolId'] == school and result['status'] in ('published', 'missing', 'unavailable', 'invalid', 'notPublished', 'cachePolicy'), 'UnexpectedSchoolResponse')
        need(result.get('isFixture') is not True, 'FixtureInProduction')
        # Source errors stay visible; preserve sanitized operational status without menu contents.
        p.setdefault('sourceResults', {})[role + ':' + school] = {k: result.get(k) for k in ('status', 'sourceStatus', 'year', 'week', 'dateBasis', 'isStale', 'fetchedAtUtc', 'lastCheckedAtUtc', 'sourceUpdatedAtUtc')}
    if role == 'web':
        need(hashlib.sha256(endpoint('web', '/')).hexdigest() == p['webIndexHash'], 'WebContentChanged')


def validate_rollback(root, p):
    cfg = json.loads((root / 'rollback.compose.json').read_text())
    need(set(cfg['services']) == set(ROLES), 'RollbackScope')
    release = json.loads((root / 'release.compose.json').read_text())
    for role in ROLES:
        expected = copy.deepcopy(release['services'][role])
        expected['image'] = OLD[role]
        expected['labels']['org.opencontainers.image.revision'] = OLD_REV
        expected['environment'] = service_config(p['targets'][role])['env']
        need(cfg['services'][role] == expected, 'RollbackConfigurationChanged')
        need(json.loads(docker('image', 'inspect', OLD[role]))[0]['Id'] == OLD[role], 'RollbackImageMissing')
    need(cfg.get('volumes') == release.get('volumes') and cfg.get('networks') == release.get('networks'), 'RollbackMountsChanged')


def record(root, p, role, phase):
    save(root / 'state.json', {'run': p['run'], 'invocation': os.environ.get('INVOCATION_ID'), 'role': role, 'phase': phase})


def work(root, p):
    need(not (root / 'state.json').exists(), 'AlreadyAttempted')
    need((root / 'backup-verified.json').exists() and json.loads((root / 'backup-verified.json').read_text()) == {'status': 'PASS', 'manifestSha256': BACKUP_SHA, 'runtimeMatches': True}, 'BackupReceiptMissing')
    preflight(root, p)
    quiescent(p['targets']['api']['Id'])
    for role in ROLES:
        old = p['targets'][role]
        check_other(p)
        record(root, p, role, 'stopping-old')
        # Infinite Docker grace: no forced SIGKILL of an unknown transaction.
        docker('stop', '--time', '-1', old['Id'], timeout=90)
        c = inspect(old['Id'])
        need(not c['State']['Running'] and c['State']['ExitCode'] == 0 and not c['State']['OOMKilled'], 'UncleanStop')
        record(root, p, role, 'old-stopped')
        record(root, p, role, 'replacement-possible')
        compose(root, p, 'up', '-d', '--no-deps', '--no-build', '--pull', 'never', role, timeout=120)
        cid = compose(root, p, 'ps', '-q', role).decode().strip()
        c = wait_healthy(cid)
        verify_service(p, role, c)
        functional(role, p)
        save(root / (role + '-deployed.json'), {'run': p['run'], 'container': cid, 'image': c['Image'], 'health': 'healthy'})
        record(root, p, role, 'verified')
    check_other(p)
    save(root / 'complete.json', {'run': p['run'], 'status': 'COMPLETED', 'revision': REV,
                                 'images': p['candidates'], 'schoolSources': 'ON', 'sourceResults': p.get('sourceResults', {})})


def recovery_action(phase, old_matches, running, clean_exit):
    if phase not in ('stopping-old', 'old-stopped') or not old_matches:
        return 'MANUAL_REVIEW_NO_RESTORE'
    if running:
        return 'WAIT_FOR_STOP'
    return 'RESTART_EXACT_OLD' if clean_exit else 'MANUAL_REVIEW_NO_RESTORE'


def recover(root, p):
    if (root / 'complete.json').exists():
        check_other(p)
        for role in ROLES:
            verify_service(p, role, inspect(compose(root, p, 'ps', '-q', role).decode().strip()))
        outcome = 'COMPLETE_NO_ACTION'
    elif not (root / 'state.json').exists():
        outcome = 'NO_STOP_RECORDED'
    else:
        s = json.loads((root / 'state.json').read_text())
        need(s['run'] == p['run'] and s.get('invocation') == os.environ.get('INVOCATION_ID') and s['role'] in ROLES, 'StaleRun')
        old = p['targets'][s['role']]
        c = inspect(old['Id'])
        matches = c['Image'] == old['Image'] and fingerprint(c) == fingerprint(old)
        if (root / 'recovery.json').exists():
            prior = json.loads((root / 'recovery.json').read_text())
            if prior.get('run') == p['run'] and prior.get('invocation') == s['invocation'] and prior.get('status') == 'EXACT_OLD_RESTARTED':
                need(matches and s['phase'] in ('stopping-old', 'old-stopped'), 'RecoveredIdentityChanged')
                healthy(c)
                check_other(p)
                return
        action = recovery_action(s['phase'], matches, c['State']['Running'], c['State']['ExitCode'] == 0 and not c['State']['OOMKilled'])
        if action == 'WAIT_FOR_STOP':
            docker('wait', old['Id'], timeout=60)
            c = inspect(old['Id'])
            matches = c['Image'] == old['Image'] and fingerprint(c) == fingerprint(old)
        action = recovery_action(s['phase'], matches, c['State']['Running'], c['State']['ExitCode'] == 0 and not c['State']['OOMKilled'])
        if action == 'RESTART_EXACT_OLD':
            check_other(p)
            docker('start', old['Id'])
            wait_healthy(old['Id'])
            outcome = 'EXACT_OLD_RESTARTED'
        else:
            outcome = 'MANUAL_REVIEW_NO_RESTORE'
    save(root / 'recovery.json', {'run': p['run'], 'invocation': os.environ.get('INVOCATION_ID'), 'status': outcome})


def verify_old(p, role, c):
    healthy(c)
    need(c['Image'] == OLD[role] and service_config(c) == service_config(p['targets'][role]), 'RollbackRuntimeDrift')
    need(c['Config']['Labels']['org.opencontainers.image.revision'] == OLD_REV, 'RollbackRevisionDrift')
    if role == 'api':
        flags_off(c)
        need(db_identity(c['Id']) == p['databases'], 'DatabaseIdentityDrift')


def rollback_preflight(root, p):
    validate_rollback(root, p)
    check_other(p)
    current = {}
    for role in ROLES:
        c = inspect(compose(root, p, 'ps', '-q', role).decode().strip())
        (verify_old if c['Image'] == OLD[role] else verify_service)(p, role, c)
        current[role] = c
    need(hashlib.sha256(endpoint('api', '/api/v1/modules/meal-planner/meals')).hexdigest() == p['mealsHash'], 'HouseholdChanged')
    drafts = json.loads(endpoint('api', '/api/v1/modules/media/url-imports/'))
    need(isinstance(drafts, list) and all(x['status'] in ('previewed', 'confirmed', 'completed', 'failed') for x in drafts), 'InFlightUrlImport')
    quiescent(current['api']['Id'])
    return current


def rollback_work(root, p):
    # Explicit operator action only. Unknown/unhealthy/data-drift states require manual review.
    need(not (root / 'rollback-state.json').exists(), 'RollbackAlreadyAttempted')
    current = rollback_preflight(root, p)
    for role in ROLES:
        check_other(p)
        c = inspect(current[role]['Id'])
        need(c['Image'] == current[role]['Image'] and fingerprint(c) == fingerprint(current[role]), 'RollbackTargetChanged')
        state = {'run': p['run'], 'invocation': os.environ.get('INVOCATION_ID'), 'role': role,
                 'container': c['Id'], 'image': c['Image'], 'fingerprint': fingerprint(c), 'phase': 'stopping-old'}
        save(root / 'rollback-state.json', state)
        docker('stop', '--time', '-1', c['Id'], timeout=90)
        stopped = inspect(c['Id'])
        need(not stopped['State']['Running'] and stopped['State']['ExitCode'] == 0 and not stopped['State']['OOMKilled'], 'UncleanStop')
        state['phase'] = 'replacement-possible'
        save(root / 'rollback-state.json', state)
        docker('compose', '--project-name', p['project'], '--project-directory', str(root), '--env-file', '/dev/null',
               '-f', str(root / 'rollback.compose.json'), 'up', '-d', '--no-deps', '--no-build', '--pull', 'never', role, timeout=120)
        c = wait_healthy(compose(root, p, 'ps', '-q', role).decode().strip())
        verify_old(p, role, c)
        endpoint(role, '/health')
        need(hashlib.sha256(endpoint(role, '/api/v1/modules/meal-planner/meals')).hexdigest() == p['mealsHash'], 'HouseholdChanged')
        for school in ('rosenfeldt', 'musikugglan'):
            need(json.loads(endpoint(role, '/api/v1/modules/meal-planner/school-meals?schoolId=' + school))['status'] == 'offline', 'SchoolNotOffline')
        if role == 'web':
            need(hashlib.sha256(endpoint(role, '/')).hexdigest() == p['oldWebIndexHash'], 'OldWebContentChanged')
        state['phase'] = 'verified'
        save(root / 'rollback-state.json', state)
    check_other(p)
    save(root / 'rollback-complete.json', {'run': p['run'], 'status': 'ROLLED_BACK', 'revision': OLD_REV, 'images': OLD, 'schoolSources': 'OFF'})


def rollback_recover(root, p):
    outcome = 'NO_STOP_RECORDED'
    if (root / 'rollback-complete.json').exists():
        check_other(p)
        for role in ROLES:
            verify_old(p, role, inspect(compose(root, p, 'ps', '-q', role).decode().strip()))
        outcome = 'COMPLETE_NO_ACTION'
    elif (root / 'rollback-state.json').exists():
        s = json.loads((root / 'rollback-state.json').read_text())
        need(s['run'] == p['run'] and s['invocation'] == os.environ.get('INVOCATION_ID'), 'StaleRun')
        outcome = 'MANUAL_REVIEW_NO_RESTORE'
        if s['phase'] == 'stopping-old':
            c = inspect(s['container'])
            need(c['Image'] == s['image'] and fingerprint(c) == s['fingerprint'], 'RollbackTargetChanged')
            if c['State']['Running']:
                docker('wait', c['Id'], timeout=60)
                c = inspect(c['Id'])
            if not c['State']['Running'] and c['State']['ExitCode'] == 0 and not c['State']['OOMKilled']:
                check_other(p)
                docker('start', c['Id'])
                wait_healthy(c['Id'])
                outcome = 'EXACT_OLD_RESTARTED'
    save(root / 'rollback-recovery.json', {'run': p['run'], 'status': outcome})


def load(root, expected_hash):
    need(root.is_absolute() and root.resolve() == root and root.stat().st_uid == os.getuid()
         and root.stat().st_mode & 0o077 == 0, 'UnsafePackage')
    need(hash_file(root / 'package.json') == expected_hash, 'PackageChanged')
    hashes = json.loads((root / 'package.json').read_text())
    need(set(hashes) == set(PACKAGE_FILES), 'PackageCoverage')
    for name, h in hashes.items():
        need(name in PACKAGE_FILES and hash_file(root / name) == h, 'InputChanged')
    p = json.loads((root / 'plan.json').read_text())
    need(p['revision'] == REV and re.fullmatch('[0-9a-f]{32}', p['run']), 'WrongPlan')
    return p


def prepare(root):
    need(command(['git', 'rev-parse', 'origin/main']).decode().strip() == REV, 'MainChanged')
    need((root / 'build-exit').read_text().strip() == '0', 'BuildNotComplete')
    need(not (root / 'plan.json').exists(), 'PackageAlreadyPrepared')
    need(not (root / 'queued.json').exists(), 'RunAlreadyQueued')
    ids = docker('ps', '-q').decode().split()
    containers = [inspect(i) for i in ids]
    original_containers = json.loads((root / 'before-containers.json').read_text())
    need({c['Id'] for c in containers} == {c['Id'] for c in original_containers}, 'RuntimeSetChanged')
    by_id = {c['Id']: c for c in containers}
    for old in original_containers:
        current = by_id[old['Id']]
        need(current['Image'] == old['Image'] and fingerprint(current) == fingerprint(old) and current['State']['StartedAt'] == old['State']['StartedAt'], 'RuntimeChangedSinceBuild')
    p = {'run': root.name.removeprefix('school-lunch-v3-'), 'revision': REV, 'targets': {}, 'others': [],
         'candidates': {}, 'originalConfigHashes': {}, 'expectedServices': {}}
    cfg = {'services': {}, 'networks': {}, 'volumes': {}}
    for role in ROLES:
        c = next(x for x in containers if x['Name'] == '/bigbrain-sprint1-' + role + '-1')
        healthy(c)
        need(c['Image'] == OLD[role] and c['Config']['Labels']['org.opencontainers.image.revision'] == OLD_REV, 'OldRevisionChanged')
        p['targets'][role] = c
        p['project'] = c['Config']['Labels']['com.docker.compose.project']
        filename = c['Config']['Labels']['com.docker.compose.project.config_files']
        original = json.loads(Path(filename).read_text())
        p['originalConfigHashes'][filename] = hash_file(filename)
        need(set(original['services']) == set(ROLES), 'OriginalServiceScope')
        candidate = (root / (role + '.iid')).read_text().strip()
        i = json.loads(docker('image', 'inspect', candidate))[0]
        need(i['Config']['Labels']['org.opencontainers.image.revision'] == REV, 'BuildRevisionMissing')
        p['candidates'][role] = candidate
        service = copy.deepcopy(original['services'][role])
        service['image'] = candidate
        service.setdefault('labels', {})['org.opencontainers.image.revision'] = REV
        # Preserve the actual environment, including existing image-derived defaults.
        service['environment'] = dict(x.split('=', 1) for x in c['Config']['Env'])
        if role == 'api':
            service['environment'].update(FLAGS)
        for field in ('User', 'Entrypoint', 'Cmd', 'WorkingDir'):
            key = {'User': 'user', 'Entrypoint': 'entrypoint', 'Cmd': 'command', 'WorkingDir': 'working_dir'}[field]
            effective = service.get(key) if service.get(key) is not None else i['Config'].get(field)
            need(effective == c['Config'].get(field), 'CandidateRuntimeDefaultsChanged')
        cfg['services'][role] = service
        p['expectedServices'][role] = copy.deepcopy(service)
        for group in ('networks', 'volumes'):
            for name, value in original.get(group, {}).items():
                need(name not in cfg[group] or cfg[group][name] == value, 'ComposeConflict')
                cfg[group][name] = value
    for c in containers:
        if c['Id'] not in [x['Id'] for x in p['targets'].values()]:
            p['others'].append({'id': c['Id'], 'image': c['Image'], 'fingerprint': fingerprint(c), 'started': c['State']['StartedAt']})
    cfg['name'] = p['project']
    p['databases'] = db_identity(p['targets']['api']['Id'])
    p['oldWebIndexHash'] = hashlib.sha256(endpoint('web', '/')).hexdigest()
    p['mealsHash'] = hashlib.sha256(endpoint('api', '/api/v1/modules/meal-planner/meals')).hexdigest()
    # Read static Web artifacts from a never-started isolated container; no volumes/networks.
    cid = docker('create', '--network', 'none', '--entrypoint', '/bin/true', p['candidates']['web']).decode().strip()
    try:
        import io, tarfile
        with tarfile.open(fileobj=io.BytesIO(docker('cp', cid + ':/usr/share/nginx/html/index.html', '-'))) as t:
            content = t.extractfile(next(x for x in t if x.isfile())).read()
        p['webIndexHash'] = hashlib.sha256(content).hexdigest()
    finally:
        docker('rm', cid)
    validate_release(p, cfg)
    need(json.loads((root / 'release.compose.json').read_text()) == cfg or {k: v for k, v in cfg.items() if k != 'name'} == json.loads((root / 'release.compose.json').read_text()), 'PreparedComposeChanged')
    save(root / 'release.compose.json', cfg)
    save(root / 'plan.json', p)
    (root / 'runner.py').write_bytes(Path(__file__).read_bytes())
    save(root / 'package.json', {name: hash_file(root / name) for name in PACKAGE_FILES})
    preflight(root, p)
    validate_rollback(root, p)
    print('PREPARED: ' + str(root))
    print('Package SHA-256: ' + hash_file(root / 'package.json'))
    print(json.dumps(p['candidates'], sort_keys=True))


def launch(root):
    need(len(sys.argv) == 5 and sys.argv[4] == '--users-paused', 'OwnerPauseRequired')
    rollback = sys.argv[1] == 'launch-rollback'
    h = sys.argv[3]
    need(re.fullmatch('[0-9a-f]{64}', h), 'ReviewedPackageHashRequired')
    p = load(root, h)
    queued = 'rollback-queued.json' if rollback else 'queued.json'
    need(not (root / queued).exists(), 'RunAlreadyQueued')
    if rollback:
        need((root / 'state.json').exists(), 'NoDeploymentToRollback')
    need(command(['git', 'ls-remote', 'origin', 'refs/heads/main'], env=os.environ.copy()).decode().split()[0] == REV, 'RemoteMainChanged')
    if rollback:
        rollback_preflight(root, p)
    else:
        preflight(root, p)
        quiescent(p['targets']['api']['Id'])
    # Local interactive auth only: password is never read or logged by this script.
    need(subprocess.run(['/usr/bin/sudo', '-v']).returncode == 0, 'OperatorAuthenticationFailed')
    receipt = json.loads(command(['/usr/bin/sudo', '-n', '/usr/bin/python3', '-I', str(root / 'verify-backup.py'), *(['--retained-only'] if rollback else [])], timeout=180))
    need(receipt == {'status': 'PASS', 'manifestSha256': BACKUP_SHA, 'runtimeMatches': not rollback}, 'BackupVerificationFailed')
    save(root / ('rollback-backup-verified.json' if rollback else 'backup-verified.json'), receipt)
    # Authentication/backup may take time: recheck identities and activity immediately before queue.
    if rollback:
        rollback_preflight(root, p)
    else:
        preflight(root, p)
        quiescent(p['targets']['api']['Id'])
    unit = 'school-lunch-v3-' + p['run'] + ('-rollback' if rollback else '')
    runner = str(root / 'runner.py')
    need(' ' not in runner and '%' not in runner, 'UnsafeUnitPath')
    save(root / queued, {'run': p['run'], 'status': 'QUEUED', 'packageHash': h, 'unit': unit + '.service'})
    args = ['/usr/bin/sudo', '-n', '/usr/bin/systemd-run', '--unit=' + unit,
            '--uid=' + str(os.getuid()), '--gid=' + str(os.getgid()), '--service-type=oneshot',
            '--property=TimeoutStartSec=480', '--property=TimeoutStopSec=180', '--property=KillMode=control-group',
            '--property=Restart=no', '--property=UMask=0077',
            '--property=ExecStopPost=/usr/bin/python3 -I ' + runner + (' rollback-recover ' if rollback else ' recover ') + str(root) + ' ' + h,
            '/usr/bin/python3', '-I', runner, 'rollback-work' if rollback else 'work', str(root), h]
    try:
        command(args)
    except BaseException:
        save(root / ('rollback-failure.json' if rollback else 'failure.json'), {'run': p['run'], 'status': 'LAUNCH_FAILED_CHECK_UNIT', 'unit': unit + '.service'})
        raise
    print('QUEUED: ' + unit + '.service; not completion. Results: ' + str(root))


def main():
    os.umask(0o077)
    action = sys.argv[1]
    root = Path(sys.argv[2]).resolve()
    need(root.name.startswith('school-lunch-v3-'), 'WrongPackageLocation')
    if action == 'prepare':
        prepare(root)
        return
    if action in ('launch', 'launch-rollback'):
        launch(root)
        return
    need(action in ('work', 'recover', 'rollback-work', 'rollback-recover'), 'UnknownAction')
    p = load(root, sys.argv[3])
    need(re.fullmatch('[0-9a-f]{32}', os.environ.get('INVOCATION_ID', ''))
         and any(line.endswith('/school-lunch-v3-' + p['run'] + ('-rollback' if action.startswith('rollback-') else '') + '.service') for line in Path('/proc/self/cgroup').read_text().splitlines()), 'SystemManagerRequired')
    fd = os.open(root.parent / '.school-lunch-v2-deployment.lock', os.O_CREAT | os.O_RDWR | os.O_NOFOLLOW, 0o600)
    try:
        fcntl.flock(fd, fcntl.LOCK_EX | fcntl.LOCK_NB)
        {'work': work, 'recover': recover, 'rollback-work': rollback_work, 'rollback-recover': rollback_recover}[action](root, p)
    except BaseException as error:
        save(root / (('rollback-' if action.startswith('rollback-') else '') + ('failure.json' if action.endswith('work') else 'recovery.json')),
             {'run': p['run'], 'status': 'BLOCKED_NO_RESTORE', 'category': str(error) if isinstance(error, Blocked) else type(error).__name__})
        raise
    finally:
        os.close(fd)


if __name__ == '__main__':
    try:
        main()
    except BaseException as error:
        print('BLOCKED: ' + (str(error) if isinstance(error, Blocked) else type(error).__name__), file=sys.stderr)
        sys.exit(1)
