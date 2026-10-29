#!/usr/bin/python3.13 -IS
"""Reviewed API+Web worker interface. Preparation only until independently approved.
Derived from deploy-school-lunch-v3.py at 7971fa2; launch/prepare/rollback CLI removed.
Only the installed approval gateway may submit this immutable worker to systemd.
"""
import copy
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import re
import signal
import stat
import subprocess
import sys
import time
import urllib.request

CONTRACT = 'school-lunch-v3-systemd-v1'
ROLES = ('api', 'web')
PREFIX = 'school-lunch-v3-'
RUNS = Path('/var/lib/bigbrain-approved-ops/runs')
PACKAGE_FILES = ('runner.py', 'plan.json', 'release.compose.json', 'rollback.compose.json',
                 'backup.py', 'backup-runtime.json', 'verify-backup.py')
DBS = ('/data/meal-planner.db', '/data/audiobook-acquisition.db', '/shopping-data/shopping-list.db',
       '/calendar-data/calendar.db', '/settings-data/settings.db', '/lifecycle-data/lifecycle.db')
ENV = {'PATH': '/usr/bin', 'HOME': '/var/empty', 'LANG': 'C.UTF-8',
       'DOCKER_HOST': 'unix:///var/run/docker.sock',
       'DOCKER_CONFIG': '/etc/bigbrain-approved-ops/docker',
       'COMPOSE_REMOVE_ORPHANS': 'false', 'COMPOSE_IGNORE_ORPHANS': 'true'}
LIMIT = 4 * 1024 * 1024
COMPOSE = '/usr/libexec/docker/cli-plugins/docker-compose'
CONFIG_FIELDS = ('User', 'Cmd', 'Entrypoint', 'WorkingDir', 'Healthcheck', 'StopTimeout', 'StopSignal')
FLAGS = tuple('MealPlanner__SchoolMeals__' + k for k in ('Enabled', 'RosenfeldtEnabled', 'MusikugglanEnabled'))
BOOTSTRAP = {school: 'MealPlanner__SchoolMeals__' + name + 'VerifiedInitialNotBeforeUtc'
             for school, name in (('rosenfeldt', 'Rosenfeldt'), ('musikugglan', 'Musikugglan'))}

class Blocked(Exception):
    pass


def need(value, code):
    if not value:
        raise Blocked(code)


def command(args, timeout=20):
    # Output goes to bounded pipes; terminate the entire child group on timeout/overflow.
    import selectors
    with subprocess.Popen(args, env=ENV, cwd='/', stdin=subprocess.DEVNULL,
                          stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                          start_new_session=True) as child:
        output = bytearray()
        total = 0
        deadline = time.monotonic() + timeout
        try:
            with selectors.DefaultSelector() as poll:
                poll.register(child.stdout, selectors.EVENT_READ)
                poll.register(child.stderr, selectors.EVENT_READ)
                while poll.get_map():
                    need(time.monotonic() < deadline, 'CommandTimeout')
                    for key, _ in poll.select(min(0.1, max(0, deadline - time.monotonic()))):
                        block = os.read(key.fileobj.fileno(), 65536)
                        if not block:
                            poll.unregister(key.fileobj)
                            continue
                        total += len(block)
                        need(total <= LIMIT, 'CommandOutputLimit')
                        if key.fileobj is child.stdout:
                            output.extend(block)
                child.wait(timeout=max(0.01, deadline - time.monotonic()))
            need(child.returncode == 0, 'CommandFailed')
            return bytes(output)
        except BaseException:
            # A descendant may keep a pipe open after its parent has exited.
            try:
                os.killpg(child.pid, signal.SIGKILL)
            except ProcessLookupError:
                pass
            child.wait()
            raise


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


def service_config(c):
    return {'config': {k: c['Config'].get(k) for k in CONFIG_FIELDS},
            'env': dict(x.split('=', 1) for x in c['Config']['Env']),
            'host': host_config(c['HostConfig']),
            'mounts': sorted((m['Type'], m.get('Name'), m['Source'], m['Destination'], m['RW'], m.get('Driver')) for m in c['Mounts']),
            'networks': sorted(c['NetworkSettings']['Networks'])}


def fingerprint(c):
    return hashlib.sha256(json.dumps(service_config(c), sort_keys=True).encode()).hexdigest()


def healthy(c):
    need(c['State']['Running'] and c['State'].get('Health', {}).get('Status') == 'healthy'
         and not c['State']['OOMKilled'], 'NotHealthy')


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
    # Short normal drain only; no supplier cooldown or hours-long client pause.
    # Sockets conservatively include reads/keepalives; graceful stop protects late arrivals.
    for sample in range(3):
        rows = docker('exec', cid, '/bin/cat', '/proc/net/tcp', '/proc/net/tcp6').decode()
        need(inbound_connections(rows) == 0, 'ApiConnectionsPresent')
        if sample < 2:
            time.sleep(1)


def check_other(p):
    running = set(docker('ps', '-q', '--no-trunc').decode().split())
    targets = {inspect(p['targets'][r]['Name'])['Id'] for r in ROLES}
    need(running - targets == {c['id'] for c in p['others']}, 'OtherServiceSetChanged')
    for old in p['others']:
        c = inspect(old['id'])
        need(c['Image'] == old['image'] and fingerprint(c) == old['fingerprint']
             and c['State']['Running'] and c['State']['StartedAt'] == old['started'], 'OtherServiceChanged')


def compose(root, p, *args, timeout=30, recovery=False):
    return command([COMPOSE, '--project-name', p['project'], '--project-directory', str(root),
                  '--env-file', '/dev/null', '-f', str(root / ('rollback.compose.json' if recovery else 'release.compose.json')), *args], timeout=timeout)


def validate_release(p, cfg):
    need(set(cfg['services']) == set(ROLES), 'ServiceScope')
    need(all(x.get('external') is True for x in [*cfg.get('volumes', {}).values(), *cfg['networks'].values()]), 'NonExternalStorageOrNetwork')
    for role in ROLES:
        s = cfg['services'][role]
        need(s['image'] == p['candidates'][role] and not any(k in s for k in ('build', 'depends_on', 'profiles', 'extends', 'env_file', 'label_file', 'configs', 'secrets', 'develop')), 'ReleaseScope')
        need(s == p['expectedServices'][role], 'ServiceConfigChanged')


def preflight(root, p):
    source_gate(p)
    validate_release(p, json.loads((root / 'release.compose.json').read_text()))
    validate_rollback(root, p)
    check_other(p)
    for role in ROLES:
        old = p['targets'][role]
        c = inspect(old['Id'])
        healthy(c)
        need(c['Image'] == old['Image'] and fingerprint(c) == fingerprint(old)
             and c['State']['StartedAt'] == old['State']['StartedAt'], 'OriginalChanged')
        i = json.loads(docker('image', 'inspect', p['candidates'][role]))[0]
        need(i['Id'] == p['candidates'][role] and i['Config']['Labels']['org.opencontainers.image.revision'] == p['revision'], 'CandidateChanged')
    backup_plan = json.loads((root / 'backup-runtime.json').read_text())
    need(backup_plan['runtime']['containerId'] == p['targets']['api']['Id'] and backup_plan['runtime']['image'] == p['targets']['api']['Image'], 'BackupPlanDrift')
    need(set(backup_plan['runtime']['volumes']) == {str(Path(db).parent) for db in DBS}, 'BackupVolumeCoverage')
    selected = {m['Destination']: {k: m[k] for k in ('Type', 'Name', 'Source', 'Driver', 'RW')} for m in p['targets']['api']['Mounts'] if m['Destination'] in backup_plan['runtime']['volumes']}
    need(selected == backup_plan['runtime']['volumes'], 'BackupVolumesDrift')
    flags_preserved(p['targets']['api'], p['targets']['api'])
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
    need(isinstance(drafts, list) and len(drafts) < 200 and all(x['status'] in ('previewed', 'confirmed', 'completed', 'failed') for x in drafts), 'InFlightUrlImport')
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
    need(c['Config']['Labels']['org.opencontainers.image.revision'] == p['revision'], 'RevisionDrift')
    need(c['Config']['Labels']['com.docker.compose.service'] == role
         and c['Config']['Labels']['com.docker.compose.project'] == p['project'], 'ServiceIdentityDrift')
    if role == 'api':
        flags_preserved(p['targets']['api'], c)
        need(db_identity(c['Id']) == p['databases'], 'DatabaseIdentityDrift')


def recovery_identity(p, role):
    old = p['targets'][role]
    if role != 'api' or 'apiRecovery' not in p:
        return old['Image'], old['Config']['Labels']['org.opencontainers.image.revision']
    r = p['apiRecovery']
    need(isinstance(r, dict) and set(r) == {'image', 'revision', 'backupManifestSha256', 'evidenceSha256'}, 'ApiRecoveryScope')
    need(r['image'] == p['candidates']['api'] and r['revision'] == p['revision']
         and r['backupManifestSha256'] == p['backupManifestSha256']
         and isinstance(r['evidenceSha256'], str)
         and re.fullmatch('[0-9a-f]{64}', r['evidenceSha256']) is not None, 'ApiRecoveryScope')
    return r['image'], r['revision']


def validate_rollback(root, p):
    cfg = json.loads((root / 'rollback.compose.json').read_text())
    need(set(cfg['services']) == set(ROLES), 'RollbackScope')
    release = json.loads((root / 'release.compose.json').read_text())
    for role in ROLES:
        expected = copy.deepcopy(release['services'][role])
        image, revision = recovery_identity(p, role)
        expected['image'] = image
        expected['labels']['org.opencontainers.image.revision'] = revision
        expected['environment'] = recovery_environment(p, role)
        need(cfg['services'][role] == expected, 'RollbackConfigurationChanged')
        inspected = json.loads(docker('image', 'inspect', image))[0]
        need(inspected['Id'] == image, 'RollbackImageMissing')
        if role == 'api' and 'apiRecovery' in p:
            need(inspected['Config']['Labels'].get('org.opencontainers.image.revision') == revision,
                 'RecoveryRevisionDrift')
    need(cfg.get('volumes') == release.get('volumes') and cfg.get('networks') == release.get('networks'), 'RollbackMountsChanged')


def record(root, p, role, phase):
    save(root / 'state.json', {'run': p['run'], 'invocation': os.environ.get('INVOCATION_ID'), 'role': role, 'phase': phase})


def work(root, p):
    need(not (root / 'state.json').exists(), 'AlreadyAttempted')
    need((root / 'backup-verified.json').exists() and json.loads((root / 'backup-verified.json').read_text()) == {'status': 'PASS', 'manifestSha256': p['backupManifestSha256'], 'runtimeMatches': True}, 'BackupReceiptMissing')
    preflight(root, p)
    quiescent(p['targets']['api']['Id'])
    for role in ROLES:
        source_gate(p)
        old = p['targets'][role]
        check_other(p)
        current = inspect(old['Id'])
        healthy(current)
        need(current['Image'] == old['Image'] and fingerprint(current) == fingerprint(old)
             and current['State']['StartedAt'] == old['State']['StartedAt'], 'OriginalChanged')
        if role == 'api':
            quiescent(old['Id'])
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
    save(root / 'complete.json', {'run': p['run'], 'status': 'COMPLETED', 'revision': p['revision'],
                                 'images': p['candidates'], 'schoolSources': 'UNCHANGED', 'sourceVerification': 'NOT_RUN'})


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
        if s['phase'] not in ('stopping-old', 'old-stopped'):
            save(root / 'recovery.json', {'run': p['run'], 'status': 'MANUAL_REVIEW_NO_RESTORE'})
            return
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
            source_gate(p)
            check_other(p)
            env = service_config(old)['env']
            if s['role'] == 'api' and ('apiRecovery' in p or any(key in env for key in BOOTSTRAP.values())
                                     or any(env.get(key) != 'false' for key in FLAGS)):
                # Old code may not have durable admission. Never restart it with sources ON.
                # Reuse reviewed rollback Compose: sources OFF and, when bound, the approved fixed image.
                validate_rollback(root, p)
                record(root, p, 'api', 'recovery-replacement-possible')
                compose(root, p, 'up', '-d', '--no-deps', '--no-build', '--pull', 'never', 'api',
                        timeout=120, recovery=True)
                cid = compose(root, p, 'ps', '-q', 'api', recovery=True).decode().strip()
                restored = wait_healthy(cid)
                expected = copy.deepcopy(old)
                expected['Config']['Env'] = [key + '=' + value
                    for key, value in recovery_environment(p, 'api').items()]
                image, revision = recovery_identity(p, 'api')
                need(restored['Image'] == image and service_config(restored) == service_config(expected),
                     'RecoveryConfigDrift')
                need(restored['Config']['Labels'].get('org.opencontainers.image.revision') ==
                     revision, 'RecoveryRevisionDrift')
                need(db_identity(cid) == p['databases'], 'DatabaseIdentityDrift')
                endpoint('api', '/health')
                need(hashlib.sha256(endpoint('api', '/api/v1/modules/meal-planner/meals')).hexdigest() == p['mealsHash'],
                     'HouseholdChanged')
                check_other(p)
                outcome = 'APPROVED_API_RESTORED_SOURCES_OFF' if 'apiRecovery' in p else 'OLD_API_RESTORED_SOURCES_OFF'
            else:
                docker('start', old['Id'])
                wait_healthy(old['Id'])
                outcome = 'EXACT_OLD_RESTARTED'
        else:
            outcome = 'MANUAL_REVIEW_NO_RESTORE'
    save(root / 'recovery.json', {'run': p['run'], 'invocation': os.environ.get('INVOCATION_ID'), 'status': outcome})


def host_config(value):
    # Only known unordered Docker collections: do not sort command/healthcheck argv.
    result = copy.deepcopy(value)
    for key in ('Binds', 'CapAdd', 'CapDrop', 'SecurityOpt', 'GroupAdd'):
        if isinstance(result.get(key), list):
            result[key] = sorted(result[key])
    return result


def bootstrap_changes(p, now=None):
    """Validate facts sealed by the existing root-held, single-use package approval.

    Hashes bind evidence; they do not establish supplier consent or DB state by themselves.
    No caller arguments, new authority, SQL writes or HTTP requests are introduced.
    """
    old = service_config(p['targets']['api'])['env']
    keys = set(BOOTSTRAP.values())
    # .NET treats keys case-insensitively and normalizes __ to :. Reject aliases/duplicates.
    aliases = {key.replace('__', ':').lower() for key in keys}
    raw = p['targets']['api']['Config']['Env']
    matching = [entry.split('=', 1)[0] for entry in raw
                if entry.split('=', 1)[0].replace('__', ':').lower() in aliases]
    need(all(key in keys for key in matching) and len(matching) == len(set(matching)),
         'BootstrapAlias')
    present = keys & old.keys()
    if 'bootstrap' not in p:
        need(not present, 'BootstrapRemovalRequired')
        return {}
    b = p['bootstrap']
    need(isinstance(b, dict) and set(b) == {'action', 'run', 'containerId', 'image',
         'backupManifestSha256', 'evidenceSha256', 'schools'}, 'BootstrapScope')
    need(b['run'] == p['run'] and b['containerId'] == p['targets']['api']['Id']
         and b['image'] == p['targets']['api']['Image']
         and b['backupManifestSha256'] == p['backupManifestSha256']
         and isinstance(b['evidenceSha256'], str)
         and re.fullmatch('[0-9a-f]{64}', b['evidenceSha256']) is not None, 'BootstrapIdentity')
    schools = b['schools']
    need(isinstance(schools, dict) and bool(schools) and set(schools) <= BOOTSTRAP.keys(),
         'BootstrapSchools')
    if b['action'] == 'remove':
        # Evidence must establish durable initialization of every selected school.
        need({BOOTSTRAP[s] for s in schools} == present
             and all(value is None for value in schools.values()), 'BootstrapRemovalScope')
        return {BOOTSTRAP[s]: None for s in schools}
    need(b['action'] == 'initialize' and not present, 'BootstrapAction')
    changes = {}
    for school, consent in schools.items():
        need(isinstance(consent, dict) and set(consent) == {'basis', 'consentAtUtc', 'notBeforeUtc'}
             and consent['basis'] == 'owner-relayed-supplier-initialization-permission', 'BootstrapConsent')
        def utc(value):
            need(isinstance(value, str) and re.fullmatch(r'\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}Z', value),
                 'BootstrapUtc')
            try:
                return datetime.strptime(value, '%Y-%m-%dT%H:%M:%SZ').replace(tzinfo=timezone.utc)
            except ValueError:
                raise Blocked('BootstrapUtc') from None
        granted = utc(consent['consentAtUtc'])
        boundary = utc(consent['notBeforeUtc'])
        need(granted <= (now or datetime.now(timezone.utc)) and boundary >= granted, 'BootstrapTime')
        changes[BOOTSTRAP[school]] = consent['notBeforeUtc']
    return changes


def expected_config(p, role):
    result = service_config(p['targets'][role])
    if role == 'api':
        for key, value in bootstrap_changes(p).items():
            if value is None:
                result['env'].pop(key)
            else:
                result['env'][key] = value
    return result


def recovery_environment(p, role):
    env = service_config(p['targets'][role])['env']
    if role == 'api':
        env.update({key: 'false' for key in FLAGS})
        for key in BOOTSTRAP.values():
            env.pop(key, None)
    return env


def flags_preserved(before, after):
    old = dict(x.split('=', 1) for x in before['Config']['Env'])
    new = dict(x.split('=', 1) for x in after['Config']['Env'])
    need(all(old.get(k) in ('true', 'false') and new.get(k) == old[k] for k in FLAGS), 'SchoolFlagsDrift')
    need(not any('schoolmeals' in str(x).lower() for x in before['Config'].get('Cmd') or []), 'UnexpectedSchoolArgument')


def source_gate(p, now=None):
    """Administrator binds reviewed durable admission code to this exact API image/revision.

    This is not a caller-issued capability claim. The protected package approval covers
    the evidence hash. Unknown history stays blocked unless an explicit bootstrap is sealed.
    No assertion of elapsed Retry-After or paused household clients is accepted here.
    """
    bootstrap_changes(p, now)
    g = p.get('sourceGate', {})
    need(set(g) == {'containerId', 'mode', 'revision', 'apiImage', 'evidenceSha256'}, 'SourceGateMissing')
    need(g['mode'] == 'meal-planner-sqlite-v1' and g['containerId'] == p['targets']['api']['Id']
         and g['revision'] == p['revision'] and re.fullmatch('[0-9a-f]{40}', g['revision']) is not None
         and g['apiImage'] == p['candidates']['api']
         and re.fullmatch('[0-9a-f]{64}', g['evidenceSha256']) is not None, 'SourceGuardNotVerified')


def endpoint(role, path):
    # No school endpoint, fixture, authentication mutation, redirects or proxy inherited
    # from the caller. These fixed reads do not enter SchoolMealService.
    need(role in ROLES and path in ('/health', '/', '/api/v1/modules/meal-planner/meals',
                                    '/api/v1/modules/media/url-imports/'), 'HttpScope')
    class NoRedirect(urllib.request.HTTPRedirectHandler):
        def redirect_request(self, *args, **kwargs):
            raise Blocked('HttpRedirect')
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({}), NoRedirect())
    port = 18080 if role == 'api' else 13000
    with opener.open('http://127.0.0.1:' + str(port) + path, timeout=15) as r:
        need(r.status == 200, 'HttpCheckFailed')
        data = r.read(2 * 1024 * 1024 + 1)
        need(len(data) <= 2 * 1024 * 1024, 'HttpResponseTooLarge')
        return data


def db_identity(cid):
    rows = docker('exec', cid, '/usr/bin/stat', '-c', '%d:%i', *DBS).decode().splitlines()
    need(len(rows) == 6 and all(re.fullmatch(r'\d+:\d+', x) for x in rows), 'DatabaseIdentityUnavailable')
    return rows


def functional(role, p):
    endpoint(role, '/health')
    need(hashlib.sha256(endpoint(role, '/api/v1/modules/meal-planner/meals')).hexdigest() == p['mealsHash'], 'HouseholdChanged')
    if role == 'web':
        need(hashlib.sha256(endpoint(role, '/')).hexdigest() == p['webIndexHash'], 'WebContentChanged')


def protected(path, directory=False):
    info = path.lstat()
    kind = stat.S_ISDIR if directory else stat.S_ISREG
    need(kind(info.st_mode) and info.st_uid == 0 and info.st_gid == 0
         and not info.st_mode & 0o022 and (directory or info.st_nlink == 1), 'UnprotectedPath')


def load(root, expected_hash):
    need(root.is_absolute() and root.parent == RUNS and re.fullmatch(PREFIX + '[0-9a-f]{32}', root.name), 'UnsafePackage')
    for path in reversed((root, *root.parents)):
        protected(path, directory=True)
    need(not root.stat().st_mode & 0o077, 'NonPrivateRun')
    protected(root / 'package.json')
    need(re.fullmatch('[0-9a-f]{64}', expected_hash) and hash_file(root / 'package.json') == expected_hash, 'PackageChanged')
    hashes = json.loads((root / 'package.json').read_text())
    need(set(hashes) == set(PACKAGE_FILES), 'PackageCoverage')
    for name in PACKAGE_FILES:
        protected(root / name)
        need(hash_file(root / name) == hashes[name], 'InputChanged')
    p = json.loads((root / 'plan.json').read_text())
    need(p['contract'] == CONTRACT and root.name == PREFIX + p['run']
         and re.fullmatch('[0-9a-f]{40}', p['revision']) and re.fullmatch('[0-9a-f]{64}', p['backupManifestSha256'])
         and set(p['targets']) == set(ROLES) and set(p['candidates']) == set(ROLES), 'WrongPlan')
    return p


def main():
    need(os.geteuid() == 0 and len(sys.argv) == 4 and sys.argv[1] in ('work', 'recover'), 'InvalidInvocation')
    os.umask(0o077)
    invocation = os.environ.get('INVOCATION_ID', '')
    os.environ.clear()
    os.environ.update(ENV)
    os.environ['INVOCATION_ID'] = invocation
    os.chdir('/')
    root = Path(sys.argv[2])
    p = load(root, sys.argv[3])
    need(re.fullmatch('[0-9a-f]{32}', invocation)
         and any(line.endswith('/' + PREFIX + p['run'] + '.service')
                 for line in Path('/proc/self/cgroup').read_text().splitlines()), 'SystemManagerRequired')
    try:
        {'work': work, 'recover': recover}[sys.argv[1]](root, p)
    except BaseException as error:
        save(root / ('failure.json' if sys.argv[1] == 'work' else 'recovery.json'),
             {'run': p['run'], 'status': 'BLOCKED_NO_RESTORE',
              'category': str(error) if isinstance(error, Blocked) else type(error).__name__})
        raise


if __name__ == '__main__':
    try:
        main()
    except BaseException:
        print('{"status":"BLOCKED","reason":"worker check failed; administrator review required"}', file=sys.stderr)
        sys.exit(1)
