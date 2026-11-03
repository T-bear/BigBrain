#!/usr/bin/env python3
"""T2: bounded API+Web image replacement. Independent review required before use.

Build from accepted Git, prove isolated restartability, then replace API+Web.
No merge, schema operation, privilege grant or database restore.
Default is read-only preflight. See docs/policy/lanes.md for authority and limits.
"""
import argparse
import copy
from contextlib import closing
import fcntl
import hashlib
import importlib.util
import io
import json
import os
from pathlib import Path
import re
import signal
import stat
import shutil
import sqlite3
import subprocess
import sys
import tarfile
import time
import urllib.request
import uuid

ROOT = Path(__file__).resolve().parents[1]
ROLES = ('api', 'web')
PROJECT = 'bigbrain-sprint1'
MEMORY_FETCH = '283060d41f4494d2e5606f7c857fb82972e86094'
CHECKS = {'backend', 'frontend', 'documentation', 'secrets'}
SHA = r'[0-9a-f]{40}'
IMAGE = r'sha256:[0-9a-f]{64}'
SDK_VERSION = '10.0.302'
SDK_REPOSITORY = 'mcr.microsoft.com/dotnet/sdk'
RUNTIME_REPOSITORY = 'mcr.microsoft.com/dotnet/aspnet'
RUNTIME_KEYS = ('DOTNET_VERSION', 'ASPNET_VERSION')
BACKUP_IMAGE = 'python@sha256:b658b05ce9c4eb97ec6fc19424818a1f31993659c2b505a532f470e83807815d'
HELPER_HOME = Path('/tmp/bigbrain-standard-backup')
IDENTITY_FIELDS = ('device', 'inode', 'uid', 'gid', 'mode')
NGINX_SOURCE = 'https://github.com/nginx/docker-nginx-unprivileged'
REPOSITORIES = {'git@github.com:T-bear/BigBrain.git', 'https://github.com/T-bear/BigBrain.git',
                'https://github.com/T-bear/BigBrain'}
# Intentionally narrow initial lane. Expansion requires review, never a path-only safety claim.
APP_FILES = {
    'src/BigBrain.Api/MealPlanner/SchoolMealService.cs',
    'src/BigBrain.Api/MealPlanner/MealPlannerStore.SchoolMeals.cs',
    'src/BigBrain.Api/MealPlanner/SchoolMealSourceClient.cs',
    'src/BigBrain.Api/MealPlanner/SchoolMeals.cs',
    'src/BigBrain.Web/src/api.ts',
    'src/BigBrain.Web/src/meal-planner/SchoolLunch.tsx',
    'src/BigBrain.Web/src/meal-planner/SchoolLunch.test.tsx',
}
# These disjoint source trees are copied only by their respective Dockerfiles.
# Other inputs (including root build/config files) remain subject to the shared gate.
# Dockerfile changes themselves are NOT allowed by APP_FILES.
SOURCE_TREES = {
    'api': ('src/BigBrain.Api/', 'src/BigBrain.Modules/', 'src/BigBrain.Sentinel.Contracts/',
            'src/BigBrain.Brain/', 'src/BigBrain.Finance.Hosting/'),
    'web': ('src/BigBrain.Web/',),
}
CONFIG_FIELDS = ('User', 'Cmd', 'Entrypoint', 'WorkingDir', 'Healthcheck', 'StopTimeout', 'StopSignal')
IMAGE_FIELDS = ('User', 'Cmd', 'Entrypoint', 'WorkingDir', 'Env', 'Healthcheck',
                'StopSignal', 'Volumes', 'ExposedPorts', 'Shell', 'OnBuild')
spec = importlib.util.spec_from_file_location('standard_backup', ROOT / 'scripts/approved-ops/backup_support.py')
backup = importlib.util.module_from_spec(spec)
spec.loader.exec_module(backup)


class Blocked(Exception):
    pass


class Uncertain(Blocked):
    """An interrupted Docker mutation may still be running in the daemon."""


def need(value, reason):
    if not value:
        raise Blocked(reason)


def environment_file(value):
    path = Path(value)
    need(path.is_absolute(), 'ComposeEnvironmentMissing')
    backup.no_symlinks(path)
    try:
        info = path.lstat()
    except OSError:
        raise Blocked('ComposeEnvironmentMissing') from None
    if path == Path('/dev/null'):
        need(str(value) == '/dev/null' and stat.S_ISCHR(info.st_mode)
             and info.st_rdev == os.makedev(1, 3), 'ComposeEnvironmentInvalid')
    else:
        need(stat.S_ISREG(info.st_mode), 'ComposeEnvironmentInvalid')
    return path


def command(args, timeout=30, mutation=False, input_data=None):
    # Retain no raw stdout/stderr on failure: Compose/inspect may contain secrets.
    try:
        stream = {'stdin': subprocess.DEVNULL} if input_data is None else {'input': input_data}
        result = subprocess.run(args, cwd=ROOT, **stream,
                                capture_output=True, timeout=timeout, check=False)
    except (subprocess.TimeoutExpired, KeyboardInterrupt):
        raise Uncertain('CommandInterrupted') if mutation else Blocked('CommandInterrupted') from None
    need(result.returncode == 0, 'CommandFailed')
    return result.stdout


def docker(*args, timeout=30, mutation=False, input_data=None):
    # Fixed local daemon, never a caller-selected context/remote host.
    extra = {} if input_data is None else {'input_data': input_data}
    return command(['docker', '--host', 'unix:///var/run/docker.sock', *args], timeout, mutation, **extra)


def inspect(identifier):
    return json.loads(docker('inspect', identifier))[0]


def image_info(identifier):
    need(re.fullmatch(IMAGE, identifier), 'UnpinnedImage')
    value = json.loads(docker('image', 'inspect', identifier))[0]
    need(value['Id'] == identifier, 'ImageIdentityChanged')
    return value


def fingerprint(c):
    host = copy.deepcopy(c['HostConfig'])
    for key in ('Binds', 'CapAdd', 'CapDrop', 'SecurityOpt', 'GroupAdd'):
        if isinstance(host.get(key), list):
            host[key].sort()
    value = {'config': {k: c['Config'].get(k) for k in CONFIG_FIELDS},
             'env': sorted(c['Config']['Env']), 'host': host,
             'mounts': sorted((m['Type'], m.get('Name'), m['Source'], m['Destination'], m['RW']) for m in c['Mounts']),
             'networks': sorted(c['NetworkSettings']['Networks'])}
    return hashlib.sha256(json.dumps(value, sort_keys=True).encode()).hexdigest()


def runtime_versions(env):
    need(isinstance(env, list) and all(isinstance(x, str) and '=' in x for x in env),
         'RuntimeEnvironmentUnknown')
    pairs = [x.split('=', 1) for x in env]
    need(len({k for k, _ in pairs}) == len(pairs), 'RuntimeEnvironmentAmbiguous')
    values = dict(pairs)
    versions = {k: values.get(k, '') for k in RUNTIME_KEYS}
    need(all(re.fullmatch(r'(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)', v)
             for v in versions.values()) and len(set(versions.values())) == 1,
         'RuntimeVersionInvalid')
    return versions


def runtime_patch(prior, candidate):
    before, after = runtime_versions(prior), runtime_versions(candidate)
    old, new = (tuple(map(int, versions['DOTNET_VERSION'].split('.'))) for versions in (before, after))
    need(old[:2] == new[:2] and new >= old, 'RuntimePatchRequired')
    # Preserve every other entry and its ordering, just like the original image check.
    expected = [x.split('=', 1)[0] + '=' + after[x.split('=', 1)[0]]
                if x.split('=', 1)[0] in RUNTIME_KEYS else x for x in prior]
    need(expected == candidate, 'ImageRuntimeContractChanged')
    return before, after


def healthy(c):
    need(c['State']['Running'] and not c['State']['OOMKilled']
         and c['State'].get('Health', {}).get('Status') == 'healthy', 'NotHealthy')


def http(role, path):
    need((role, path) in {('api', '/health'), ('web', '/'), ('web', '/icons/bigbrain-32.png')}, 'HttpScope')
    class NoRedirect(urllib.request.HTTPRedirectHandler):
        def redirect_request(self, *args, **kwargs):
            raise Blocked('HttpRedirect')
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({}), NoRedirect())
    port = 18080 if role == 'api' else 13000
    with opener.open(f'http://127.0.0.1:{port}{path}', timeout=3) as response:
        need(response.status == 200, 'HttpStatus')
        value = response.read(2 * 1024 * 1024 + 1)
        need(len(value) <= 2 * 1024 * 1024, 'HttpSize')
        return value


def http_health(role):
    if role == 'api':
        http(role, '/health')
    else:
        need(re.search(br'<html[\s>]', http(role, '/'), re.I), 'WebHtml')
        need(http(role, '/icons/bigbrain-32.png').startswith(b'\x89PNG\r\n\x1a\n'), 'WebIcon')


def ci_green(revision):
    value = json.loads(command(['gh', 'api', f'repos/T-bear/BigBrain/commits/{revision}/check-runs?per_page=100']))
    need(value['total_count'] <= 100, 'CiPagination')
    for name in CHECKS:
        runs = [c for c in value['check_runs'] if c['name'] == name and c['app']['slug'] == 'github-actions']
        need(runs and all(c['status'] == 'completed' and c['conclusion'] == 'success' for c in runs), 'CiNotGreen')


def legacy_web_provenance(image):
    """Read only the existing local builder; no tag, supplied receipt or label fallback."""
    prior = image_info(image)
    need(prior['Config'].get('Labels', {}).get('org.opencontainers.image.source') == NGINX_SOURCE,
         'LegacyWebSourceUnknown')
    rows = [json.loads(line) for line in docker('buildx', 'history', 'ls', '--builder', 'default',
            '--no-trunc', '--format', 'json').splitlines()]
    matches = []
    for row in rows:
        if row.get('status') != 'Completed' or 'bigbrain.web' not in row.get('name', '').lower():
            continue
        need(re.fullmatch(r'default/default/[a-z0-9]{25}', row.get('ref', '')), 'BuildRecordLocationUnknown')
        ref = row['ref'].split('/')[-1]
        raw = docker('buildx', 'history', 'inspect', '--builder', 'default', '--format', 'json', ref)
        record = json.loads(raw)
        attachments = record.get('Attachments', [])
        images = [a for a in attachments if a.get('Digest') == image and a.get('Type') in
                  {'application/vnd.oci.image.index.v1+json', 'application/vnd.oci.image.manifest.v1+json'}]
        if not images:
            continue
        need(len(images) == 1 and record.get('Status') == 'completed'
             and record.get('Ref') == ref and record.get('Dockerfile') == 'src/BigBrain.Web/Dockerfile'
             and record.get('VCSRepository') in REPOSITORIES, 'BuildRecordMismatch')
        revision = record.get('VCSRevision', '')
        need(re.fullmatch(SHA, revision), 'BuildRevisionUnknown')
        attestations = [a for a in attachments if a.get('Type') == 'https://slsa.dev/provenance/v1']
        need(len(attestations) == 1 and re.fullmatch(IMAGE, attestations[0].get('Digest', '')), 'BuildProvenanceMissing')
        raw_proof = docker('buildx', 'history', 'inspect', 'attachment', '--builder', 'default',
                           ref, attestations[0]['Digest'])
        proof = json.loads(raw_proof)
        definition, details = proof.get('buildDefinition', {}), proof.get('runDetails', {})
        args = definition.get('externalParameters', {}).get('request', {}).get('root', {}).get('request', {}).get('args', {})
        metadata = details.get('metadata', {})
        vcs = metadata.get('buildkit_metadata', {}).get('vcs', {})
        need(definition.get('buildType') == 'https://github.com/moby/buildkit/blob/master/docs/attestations/slsa-definitions.md'
             and definition.get('externalParameters', {}).get('configSource', {}).get('path') == 'Dockerfile'
             and args.get('vcs:revision') == vcs.get('revision') == revision
             and args.get('vcs:source') == vcs.get('source') == record['VCSRepository']
             and metadata.get('buildkit_completeness', {}).get('request') is True,
             'BuildProvenanceMismatch')
        # Preserve raw builder evidence privately on execute, never print it.
        matches.append({'image': image, 'revision': revision, 'ref': ref,
                        'record': record, 'provenanceDigest': attestations[0]['Digest'], 'provenance': proof})
    need(len(matches) == 1, 'BuildProvenanceAmbiguousOrMissing')
    return matches[0]


def git_guard(revision, originals):
    need(re.fullmatch(SHA, revision), 'InvalidRevision')
    remote = command(['git', 'ls-remote', 'origin', 'refs/heads/main']).decode().split()
    need(remote and remote[0] == revision, 'NotCurrentRemoteMain')
    need(command(['git', 'rev-parse', 'HEAD']).decode().strip() == revision, 'CheckoutNotAcceptedMain')
    need(not command(['git', 'status', '--porcelain', '--untracked-files=no']).strip(), 'TrackedChanges')
    command(['git', 'merge-base', '--is-ancestor', MEMORY_FETCH, revision])
    need(set(originals) == set(ROLES), 'MissingRuntimeBaseline')
    evidence = {}
    for role, old in originals.items():
        base = old['Config']['Labels'].get('org.opencontainers.image.revision', '')
        need(re.fullmatch(SHA, base), 'OldRevisionUnknown')
        try:
            need(command(['git', 'cat-file', '-t', base]).strip() == b'commit', 'OldRevisionUnavailable')
        except Blocked:
            if role != 'web' or old['Config']['Labels'].get('org.opencontainers.image.source') != NGINX_SOURCE:
                raise Blocked('OldRevisionUnavailable') from None
            evidence[role] = legacy_web_provenance(old['Image'])
            base = evidence[role]['revision']
            need(command(['git', 'cat-file', '-t', base]).strip() == b'commit', 'OldRevisionUnavailable')
        try:
            command(['git', 'merge-base', '--is-ancestor', base, revision])
        except Blocked:
            raise Blocked('OldRevisionNotAccepted') from None
        paths = command(['git', 'diff', '--no-renames', '--name-only', '-z', base, revision]).decode().split('\0')
        for path in filter(None, paths):
            # API and Web may have been deployed from different accepted commits.
            # The other application's delta is checked against ITS own baseline below.
            other_role = 'web' if role == 'api' else 'api'
            if path.startswith(SOURCE_TREES[other_role]):
                continue
            need(path in APP_FILES or path.startswith(('docs/', 'tests/', 'scripts/'))
                 or path in {'AGENTS.md', '.github/workflows/ci.yml'}, 'OutsideStandardScope')
    ci_green(revision)
    return evidence


def timesync_identity(container, service):
    declared = [m for m in service.get('volumes', []) if m.get('target') == '/host-timesync']
    actual = [m for m in container['Mounts'] if m['Destination'] == '/host-timesync']
    need(len(declared) == len(actual) == 1, 'TimesyncMountMissing')
    mount, live = declared[0], actual[0]
    source = Path('/run/systemd/timesync')
    need(mount.get('type') == live.get('Type') == 'bind'
         and mount.get('source') == live.get('Source') == str(source)
         and mount.get('read_only') is True and live.get('RW') is False
         and live.get('Propagation') == 'rprivate'
         and mount.get('bind', {}).get('propagation', 'rprivate') == 'rprivate', 'TimesyncMountMismatch')
    backup.no_symlinks(source)
    before = source.stat()
    need(stat.S_ISDIR(before.st_mode), 'TimesyncNotDirectory')
    # stat runs as the existing container user; no writes, shell, root override or restart.
    fields = docker('exec', container['Id'], 'stat', '-c', '%d:%i:%f', '/host-timesync').decode().strip().split(':')
    need(len(fields) == 3 and all(re.fullmatch(r'[0-9a-f]+', x) for x in fields), 'TimesyncIdentityUnknown')
    after = source.stat()
    identity = (before.st_dev, before.st_ino)
    need(identity == (after.st_dev, after.st_ino) == (int(fields[0]), int(fields[1]))
         and stat.S_ISDIR(int(fields[2], 16)), 'TimesyncIdentityMismatch')
    return identity


def writable_layer(role, container, service):
    identity = None
    for row in docker('diff', container['Id']).decode().splitlines():
        if role == 'api' and row == 'A /host-timesync':
            identity = timesync_identity(container, service)
            continue
        need(re.fullmatch(r'[ACD] /.+', row) and any(row[2:] == p or row[2:].startswith(p + '/')
             for p in ('/tmp', '/var/cache/nginx', '/var/run', '/run')), 'RollbackImageHasRuntimeRepairs')
    return identity


def release_config(config, images):
    need(config.get('name') == PROJECT, 'WrongComposeProject')
    result = copy.deepcopy(config)
    result['services'] = {r: result['services'][r] for r in ROLES}
    for role, service in result['services'].items():
        need(not any(k in service for k in ('profiles', 'extends', 'env_file', 'label_file', 'configs', 'secrets', 'develop', 'volumes_from', 'links')), 'UnsupportedCompose')
        service.pop('build', None)
        service.pop('depends_on', None)
        service['image'] = images[role]
        service['pull_policy'] = 'never'
    # Retain only named volumes referenced by the projected API/Web services.
    # Every retained resource remains external: Compose must not create volumes.
    used = {mount['source']
            for service in result['services'].values()
            for mount in service.get('volumes', [])
            if mount.get('type') == 'volume'}
    need(used <= set(result.get('volumes', {})), 'VolumeDeclarationMissing')
    for kind in ('volumes', 'networks'):
        result[kind] = {key: {'name': entry['name'], 'external': True}
                        for key, entry in result.get(kind, {}).items()
                        if kind != 'volumes' or key in used}
    result.pop('configs', None)
    result.pop('secrets', None)
    return result


def schemas(roots):
    result = {}
    for target, names in backup.EXPECTED.items():
        for name in names:
            path = roots[target][0] / name
            with closing(sqlite3.connect(path.as_uri() + '?mode=ro', uri=True, timeout=3)) as db:
                db.execute('PRAGMA query_only=ON')
                result[target + '/' + name] = db.execute(
                    'SELECT type,name,tbl_name,sql FROM sqlite_schema ORDER BY type,name').fetchall()
    return result


def snapshot_matches(rendered, expected):
    # Compose's canonical output is already dollar-escaped for safe re-use.
    # Re-rendering may add empty IPAM metadata or omit empty top-level maps.
    for value in (rendered, expected):
        for network in value.get('networks', {}).values():
            if network.get('ipam') == {}:
                network.pop('ipam')
        for key in ('volumes', 'networks'):
            if value.get(key) == {}:
                value.pop(key)
    return rendered == expected


def save(path, value):
    # Exclusive artifacts: no existing file or recovery evidence is overwritten.
    fd = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL | os.O_NOFOLLOW, 0o600)
    with os.fdopen(fd, 'w', encoding='utf8') as stream:
        json.dump(value, stream, sort_keys=True, indent=2)
        stream.flush()
        os.fsync(stream.fileno())
    sync_dir(path.parent)


def sync_dir(path):
    fd = os.open(path, os.O_RDONLY | os.O_DIRECTORY)
    try:
        os.fsync(fd)
    finally:
        os.close(fd)


def record(folder, phase):
    with (folder / 'journal.jsonl').open('a', encoding='utf8') as stream:
        stream.write(json.dumps({'phase': phase, 'at': backup.utc()}) + '\n')
        stream.flush()
        os.fsync(stream.fileno())
    sync_dir(folder)


def no_pending_run(state):
    for folder in state.glob('run-*'):
        journal = folder / 'journal.jsonl'
        rows = journal.read_text().splitlines() if journal.exists() else []
        need(rows and json.loads(rows[-1])['phase'] in {'PASS', 'ROLLED_BACK', 'PRE_SWITCH_FAILED'}, 'PreviousRunNeedsReview')


def volume_sources(container):
    """Daemon metadata only; never traverse host production directories."""
    need(container['Config'].get('User') in {'1654', '1654:1654'}, 'BackupApiUser')
    selected = []
    for target in backup.EXPECTED:
        matches = [m for m in container['Mounts'] if m['Destination'] == target]
        need(len(matches) == 1 and matches[0]['Type'] == 'volume', 'BackupMountMismatch')
        mount = matches[0]
        name, source = mount.get('Name', ''), mount['Source']
        need(re.fullmatch(r'[a-zA-Z0-9][a-zA-Z0-9_.-]+', name)
             and source == str(Path(source)) and Path(source).is_absolute()
             and '..' not in Path(source).parts, 'BackupVolumeIdentity')
        info = json.loads(docker('volume', 'inspect', name))
        need(len(info) == 1 and info[0]['Name'] == name and info[0]['Mountpoint'] == source
             and info[0]['Driver'] == 'local' and not info[0].get('Options'), 'BackupVolumeIdentity')
        for other in container['Mounts']:
            if other is mount:
                continue
            path = Path(other['Source'])
            need(other.get('Name') != name and not Path(source).is_relative_to(path)
                 and not path.is_relative_to(source)
                 and not other['Destination'].startswith(target + '/'), 'BackupVolumeOverlap')
        selected.append({k: mount[k] for k in ('Type', 'Name', 'Source', 'Destination')})
    need(len(selected) == 5, 'BackupVolumeCoverage')
    return {'Mounts': selected}


def data_snapshot(roots):
    directories = {}
    for target, (path, _) in roots.items():
        backup.no_symlinks(path)
        s = path.stat()
        need(stat.S_ISDIR(s.st_mode) and s.st_uid == 1654 and s.st_gid == 0
             and stat.S_IMODE(s.st_mode) == 0o755, 'BackupDirectoryIdentity')
        directories[target] = [s.st_dev, s.st_ino, s.st_uid, s.st_gid, stat.S_IMODE(s.st_mode)]
    inventory, _ = backup.inventory(roots)
    need(all(v['uid'] == v['gid'] == 1654 and v['mode'] in (0o600, 0o644)
             for v in inventory.values()), 'BackupDatabasePermissions')
    schema = schemas(roots)  # mode=ro/query_only; SQLite may create WAL/SHM files.
    for target, (path, _) in roots.items():
        for name in backup.EXPECTED[target]:
            for suffix in ('-wal', '-shm'):
                sidecar = path / (name + suffix)
                try:
                    s = sidecar.lstat()
                except FileNotFoundError:
                    continue  # normal SQLite close/removal, not a required invariant
                need(stat.S_ISREG(s.st_mode) and s.st_nlink == 1 and s.st_uid == s.st_gid == 1654
                     and stat.S_IMODE(s.st_mode) in (0o600, 0o644), 'BackupSidecarPermissions')
    return json.loads(json.dumps({'directories': directories, 'inventory': inventory, 'schemas': schema}))


def same_data(before, after):
    need(before['directories'] == after['directories'], 'BackupDirectoryChanged')
    need(before['schemas'] == after['schemas'], 'SchemaChangedNoAutomaticRollback')
    old, new = before['inventory'], after['inventory']
    need(old.keys() == new.keys(), 'DatabaseCoverageChanged')
    need(all(old[key][field] == new[key][field] for key in old for field in IDENTITY_FIELDS),
         'DatabaseIdentityChanged')


def backup_helper(request):
    """Internal one-shot entrypoint, copied from accepted code; no new operator CLI."""
    need(os.getuid() == os.getgid() == 1654 and os.getgroups() in ([], [1654]), 'BackupHelperUser')
    need(sqlite3.sqlite_version == '3.53.4', 'BackupSQLiteVersion')
    os.umask(0o077)
    HELPER_HOME.mkdir(mode=0o700)  # exclusive; retained container layer, never tmpfs
    record(HELPER_HOME, 'STARTED')
    try:
        roots = backup.sources(request['sources'])
        before = data_snapshot(roots)
        if request['baseline'] is not None:
            same_data(request['baseline'], before)
        save(HELPER_HOME / 'baseline.json', before)
        result = {'data': before}
        if request['backup']:
            folder = backup.run_backup(roots, HELPER_HOME / 'backups', request['identity'],
                                       lambda: same_data(before, data_snapshot(roots)))
            result.update(backupId=folder.name, manifestSha256=backup.verify_set(folder))
        same_data(before, data_snapshot(roots))
        save(HELPER_HOME / 'result.json', result)
        record(HELPER_HOME, 'PASS')
    except BaseException:
        record(HELPER_HOME, 'MANUAL_REVIEW_REQUIRED')
        raise


def helper_archive(request):
    stream = io.BytesIO()
    with tarfile.open(fileobj=stream, mode='w') as bundle:
        files = {path: (ROOT / path).read_bytes() for path in
                 ('scripts/deploy-standard.py', 'scripts/approved-ops/backup_support.py')}
        files['request.json'] = json.dumps(request).encode()
        for path, body in files.items():
            info = tarfile.TarInfo('standard-code/' + path)
            info.mode, info.size = 0o444, len(body)
            bundle.addfile(info, io.BytesIO(body))
    return stream.getvalue()


def verify_private_copy(folder):
    """docker cp without -a makes a separate caller-owned copy; persist before use."""
    for path in [folder, *folder.rglob('*')]:
        s = path.lstat()
        need(s.st_uid == os.geteuid() and not path.is_symlink(), 'BackupCopyOwner')
        if stat.S_ISDIR(s.st_mode):
            need(stat.S_IMODE(s.st_mode) == 0o700, 'BackupCopyMode')
            sync_dir(path)
        else:
            need(stat.S_ISREG(s.st_mode) and s.st_nlink == 1 and stat.S_IMODE(s.st_mode) == 0o600,
                 'BackupCopyMode')
            with path.open('rb') as stream:
                os.fsync(stream.fileno())
    sync_dir(folder.parent)


class Deployment:
    def __init__(self, revision, images, compose_file, state):
        self.revision, self.images, self.source, self.state = revision, images, compose_file, state
        self.originals = {}
        self.changed = []
        self.folder = None
        self.source_files = [compose_file] if compose_file else []
        self.project_directory = compose_file.parent if compose_file else None
        self.probes = set()
        self.env_files = []
        self.reconstruct = False
        self.provenance = {}
        self.timesync = None
        self.data = None
        self.helper_incomplete = False
        self.runtime_build = None
        self.runtime_contract = None
        self.replacement_ids = {}

    def compose_model(self, config, *args):
        environment_file('/dev/null')
        return docker('compose', '--project-name', PROJECT, '--project-directory', str(self.project_directory),
                      '--env-file', '/dev/null', '-f', '-', *args, timeout=120,
                      input_data=json.dumps(config).encode())

    def compose(self, path, *args, mutation=False):
        files = self.source_files if path == self.source else [path]
        # Resolve ordinary YAML/overrides and .env ourselves; no prepared JSON required.
        if path != self.source:
            env_files = [Path('/dev/null')]
        elif self.env_files:
            env_files = self.env_files
        else:
            implicit = self.project_directory / '.env'
            env_files = [implicit if implicit.exists() or implicit.is_symlink() else Path('/dev/null')]
        env_args = [arg for env in env_files for arg in ('--env-file', str(environment_file(env)))]
        return docker('compose', '--project-name', PROJECT, '--project-directory', str(self.project_directory),
                      *env_args,
                      *[arg for source in files for arg in ('-f', str(source))],
                      *args, timeout=120, mutation=mutation)

    def discover_compose(self):
        if self.source is not None:
            return
        labels = [self.current(r)['Config']['Labels'] for r in ROLES]
        keys = ('com.docker.compose.project.config_files', 'com.docker.compose.project.working_dir')
        need(all(l.get(k) for l in labels for k in keys), 'ComposeProvenanceMissing')
        self.reconstruct = any(labels[0][k] != labels[1][k] for k in keys)
        if self.reconstruct:
            # Only the diagnosed legacy API + ordinary Web combination is supported.
            need(re.fullmatch(r'/var/lib/bigbrain-approved-ops/runs/[a-zA-Z0-9_-]+/release\.compose\.json', labels[0][keys[0]])
                 and Path(labels[1][keys[0]]).name == 'compose.yaml'
                 and ',' not in labels[1][keys[0]], 'ComposeProvenanceMissing')
        selected = labels[1] if self.reconstruct else labels[0]
        self.source_files = [Path(x) for x in selected[keys[0]].split(',')]
        self.source = self.source_files[0]
        env_key = 'com.docker.compose.project.environment_file'
        need(self.reconstruct or labels[0].get(env_key, '') == labels[1].get(env_key, ''), 'ComposeEnvironmentMismatch')
        self.env_files = [environment_file(x) for x in selected.get(env_key, '').split(',') if x]
        self.project_directory = Path(selected[keys[1]])
        need(self.project_directory.is_absolute(), 'ComposeDirectoryInvalid')
        backup.no_symlinks(self.project_directory)

    def configuration(self, originals):
        self.discover_compose()
        for path in self.source_files:
            need(path.is_absolute() and path.is_file(), 'ComposeMissing')
            backup.no_symlinks(path)
        config = json.loads(self.compose(self.source, 'config', '--format', 'json'))
        if self.reconstruct:
            web_hash = self.compose(self.source, 'config', '--hash', 'web').decode().split()
            need(web_hash == ['web', originals['web']['Config']['Labels'].get('com.docker.compose.config-hash')], 'ComposeRuntimeMismatch')
            api = config['services']['api']
            api['image'] = originals['api']['Image']
            api.setdefault('labels', {})['org.opencontainers.image.revision'] = originals['api']['Config']['Labels']['org.opencontainers.image.revision']
            env = originals['api']['Config']['Env']
            need(env and all(isinstance(x, str) and re.match(r'^[A-Za-z_][A-Za-z0-9_]*=', x) for x in env), 'RuntimeEnvironmentUnknown')
            pairs = [x.split('=', 1) for x in env]
            need(len({k for k, v in pairs}) == len(pairs), 'RuntimeEnvironmentAmbiguous')
            api['environment'] = {k: v.replace('$', '$$') for k, v in pairs}
            hashes = dict(line.split() for line in self.compose_model(config, 'config', '--hash', '*').decode().splitlines())
            rendered = json.loads(self.compose_model(config, 'config', '--format', 'json'))
            need(snapshot_matches(rendered, copy.deepcopy(config)), 'SnapshotChanged')
        else:
            hashes = dict(line.split() for line in self.compose(self.source, 'config', '--hash', '*').decode().splitlines())
        need(all(hashes.get(r) and hashes[r] == originals[r]['Config']['Labels'].get('com.docker.compose.config-hash') for r in ROLES), 'ComposeRuntimeMismatch')
        return config

    def current(self, role):
        ids = docker('ps', '-aq', '--no-trunc', '--filter', f'label=com.docker.compose.project={PROJECT}',
                     '--filter', f'label=com.docker.compose.service={role}').decode().split()
        need(len(ids) == 1, 'ServiceIdentityAmbiguous')
        return inspect(ids[0])

    def others(self):
        target_ids = {self.current(r)['Id'] for r in ROLES}
        result = {}
        for cid in docker('ps', '-aq', '--no-trunc').decode().split():
            if cid not in target_ids and cid not in self.probes:
                c = inspect(cid)
                result[cid] = (c['Image'], fingerprint(c), c['State']['Running'], c['State']['StartedAt'])
        return result

    def unchanged(self):
        need(all(not inspect(cid)['State']['Running'] for cid in self.probes), 'ProbeStillRunning')
        need(self.others() == self.other_snapshot, 'OtherServiceChanged')
        for role, old in self.originals.items():
            current = self.current(role)
            need(current['Id'] == old['Id'] and current['Image'] == old['Image']
                 and fingerprint(current) == fingerprint(old)
                 and current['State']['StartedAt'] == old['State']['StartedAt'], 'RuntimeChanged')
            healthy(current)
        if self.timesync is not None:
            need(timesync_identity(self.current('api'), self.config['services']['api']) == self.timesync, 'TimesyncIdentityMismatch')

    def preflight(self):
        originals = {r: self.current(r) for r in ROLES}
        self.config = self.configuration(originals)
        for role in ROLES:
            old = originals[role]
            healthy(old)
            prior = image_info(old['Image'])
            need(prior['Config'].get('Labels', {}).get('org.opencontainers.image.revision')
                 == old['Config']['Labels'].get('org.opencontainers.image.revision'), 'OldImageRevisionMismatch')
            need(all(old['Config'].get(k) == prior['Config'].get(k)
                     for k in ('User', 'Cmd', 'Entrypoint', 'WorkingDir')), 'RuntimeEntrypointOverride')
            # A healthy container repaired in its writable layer is not a safe rollback image.
            identity = writable_layer(role, old, self.config['services'][role])
            if identity is not None:
                self.timesync = identity
            for name in old['Config']['Env']:
                need('VerifiedInitialNotBeforeUtc=' not in name, 'LegacyBootstrapPresent')
            self.originals[role] = old
            http_health(role)
        self.provenance = git_guard(self.revision, self.originals)
        self.other_snapshot = self.others()
        self.rollback = release_config(self.config, {r: c['Image'] for r, c in self.originals.items()})
        self.release = copy.deepcopy(self.rollback)
        if self.images:
            self.bind_images()
        # Resolve existing resource IDs; no creation is permitted by the execution snapshot.
        for kind, noun in (('networks', 'network'), ('volumes', 'volume')):
            for value in self.release.get(kind, {}).values():
                docker(noun, 'inspect', value['name'])
        self.sources = volume_sources(self.originals['api'])
        self.unchanged()

    def helper_data(self, make_backup=False):
        need(self.folder is not None, 'HelperRequiresExecute')
        need(os.geteuid() == 1000, 'BackupOperatorUser')
        sources = volume_sources(self.current('api'))
        need(sources == self.sources, 'BackupVolumesChanged')
        image = json.loads(docker('image', 'inspect', BACKUP_IMAGE))[0]
        need(BACKUP_IMAGE in image.get('RepoDigests', []) and not image['Config'].get('Volumes')
             and re.fullmatch(IMAGE, image['Id']), 'BackupImageIdentity')
        request = {'sources': sources, 'baseline': self.data, 'backup': make_backup,
                   'identity': {'containerId': self.originals['api']['Id'], 'image': self.originals['api']['Image'],
                                'revision': self.originals['api']['Config']['Labels']['org.opencontainers.image.revision']}}
        name = 'bigbrain-standard-backup-' + uuid.uuid4().hex
        folder = self.folder / name
        folder.mkdir(mode=0o700)
        save(folder / 'intent.json', {'name': name, 'image': BACKUP_IMAGE, 'sources': sources})
        self.helper_incomplete = True
        record(self.folder, 'HELPER_STARTED')
        code = (
            "import importlib.util,json,signal,sys; from pathlib import Path; "
            "s=importlib.util.spec_from_file_location('standard','/standard-code/scripts/deploy-standard.py'); "
            "m=importlib.util.module_from_spec(s); s.loader.exec_module(m); "
            "signal.alarm(180); "
            "m.backup_helper(json.loads(Path('/standard-code/request.json').read_text()))"
        )
        args = ['create', '--name', name, '--pull', 'never', '--user', '1654:1654',
                '--network', 'none', '--cap-drop', 'ALL', '--security-opt', 'no-new-privileges',
                '--restart', 'no', '--memory', '512m', '--memory-swap', '512m', '--cpus', '1',
                '--pids-limit', '32', '--log-driver', 'none', '--entrypoint', 'python3']
        for mount in sources['Mounts']:
            args += ['--mount', f"type=volume,source={mount['Name']},target={mount['Source']},volume-nocopy"]
        args += [BACKUP_IMAGE, '-I', '-B', '-c', code]
        cid = docker(*args, mutation=True).decode().strip()
        need(re.fullmatch(r'[0-9a-f]{64}', cid), 'BackupHelperIdentity')
        self.probes.add(cid)
        save(folder / 'identity.json', {'containerId': cid, 'imageId': image['Id']})
        c = inspect(cid)
        host = c['HostConfig']
        need(c['Image'] == image['Id'] and c['Config']['User'] == '1654:1654'
             and c['Config']['Entrypoint'] == ['python3'] and c['Config']['Cmd'] == ['-I', '-B', '-c', code]
             and not c['State']['Running'] and host['NetworkMode'] == 'none'
             and not host.get('Privileged') and not host.get('Binds') and not host.get('Devices')
             and not host.get('GroupAdd') and not host.get('CapAdd') and not host.get('PortBindings')
             and host.get('CapDrop') == ['ALL'] and host.get('SecurityOpt') == ['no-new-privileges']
             and host['Memory'] == host['MemorySwap'] == 512 * 1024 * 1024
             and host['NanoCpus'] == 1000000000 and host['PidsLimit'] == 32
             and host['RestartPolicy']['Name'] == 'no' and not host.get('AutoRemove'), 'BackupHelperIsolation')
        actual = sorted((m['Type'], m.get('Name'), m['Source'], m['Destination'], m['RW']) for m in c['Mounts'])
        expected = sorted(('volume', m['Name'], m['Source'], m['Source'], True) for m in sources['Mounts'])
        need(actual == expected, 'BackupHelperMounts')
        docker('cp', '-', cid + ':/', input_data=helper_archive(request), mutation=True)
        # The container exits after this one operation. Timeout/crash is never retried.
        docker('start', '--attach', cid, timeout=210, mutation=True)
        stopped = inspect(cid)
        need(not stopped['State']['Running'] and not stopped['State']['OOMKilled']
             and stopped['State']['ExitCode'] == 0, 'BackupHelperFailed')
        output = folder / 'output'
        output.mkdir(mode=0o700)
        docker('cp', cid + ':' + str(HELPER_HOME) + '/.', str(output), timeout=120, mutation=True)
        verify_private_copy(output)
        result = json.loads((output / 'result.json').read_text())
        journal = [json.loads(row)['phase'] for row in (output / 'journal.jsonl').read_text().splitlines()]
        need(journal == ['STARTED', 'PASS'], 'BackupHelperJournal')
        if self.data is not None:
            same_data(self.data, result['data'])
        if make_backup:
            need(re.fullmatch(r'[0-9]{8}T[0-9]{6}Z-[0-9a-f]{32}', result['backupId']), 'BackupId')
            backup_folder = output / 'backups' / result['backupId']
            need(backup.verify_set(backup_folder) == result['manifestSha256'], 'BackupCopyMismatch')
            manifest = json.loads((backup_folder / 'manifest.json').read_text())
            need(manifest['runtime'] == request['identity'], 'BackupRuntimeMismatch')
            self.data = result['data']
        need(volume_sources(self.current('api')) == sources, 'BackupVolumesChanged')
        self.helper_incomplete = False
        record(self.folder, 'HELPER_VERIFIED')
        return backup_folder if make_backup else result['data']

    def backup_data(self):
        folder = self.helper_data(make_backup=True)
        self.unchanged()
        return folder

    def bind_images(self):
        contract = None
        for role, old in self.originals.items():
            prior, candidate = image_info(old['Image']), image_info(self.images[role])
            need(candidate['Config'].get('Labels', {}).get('org.opencontainers.image.revision') == self.revision, 'ImageRevisionMismatch')
            need(all(candidate['Config'].get(k) == prior['Config'].get(k) for k in IMAGE_FIELDS if k != 'Env'), 'ImageRuntimeContractChanged')
            if candidate['Config'].get('Env') != prior['Config'].get('Env'):
                need(role == 'api', 'ImageRuntimeContractChanged')
                before, after = runtime_patch(prior['Config'].get('Env'), candidate['Config'].get('Env'))
                proof = self.runtime_build
                need(proof is not None and proof['image'] == self.images[role]
                     and proof['revision'] == self.revision and proof['versions'] == after
                     and re.fullmatch(re.escape(RUNTIME_REPOSITORY) + r'@sha256:[0-9a-f]{64}', proof['base']),
                     'RuntimeBuildProofMissing')
                expected = copy.deepcopy(old)
                overrides = self.config['services'][role].get('environment', {})
                need(isinstance(overrides, dict), 'RuntimeEnvironmentUnknown')
                runtime_versions(old['Config']['Env'])
                for key in RUNTIME_KEYS:
                    if key in overrides:
                        need(isinstance(overrides[key], str) and
                             key + '=' + overrides[key].replace('$$', '$') in old['Config']['Env'],
                             'RuntimeEnvironmentOverrideChanged')
                    else:
                        need(key + '=' + before[key] in old['Config']['Env'], 'RuntimeEnvironmentUnknown')
                        expected['Config']['Env'][expected['Config']['Env'].index(key + '=' + before[key])] = key + '=' + after[key]
                runtime_versions(expected['Config']['Env'])
                contract = {'original': old['Image'], 'image': self.images[role],
                            'env': expected['Config']['Env']}
        self.runtime_contract = contract
        self.release = release_config(self.config, self.images)
        for service in self.release['services'].values():
            service.setdefault('labels', {})['org.opencontainers.image.revision'] = self.revision

    def build_images(self):
        # No workspace/untracked file enters the context. Keep archive + base bindings.
        need(git_guard(self.revision, self.originals) == self.provenance, 'BuildProvenanceChanged')
        archive = self.folder / 'source.tar'
        command(['git', 'archive', '--format=tar', '--output=' + str(archive), self.revision], timeout=120)
        source = self.folder / 'source'
        source.mkdir()
        with tarfile.open(archive) as bundle:
            need(all(m.isfile() or m.isdir() for m in bundle.getmembers()), 'UnsupportedBuildTree')
            bundle.extractall(source, filter='data')
        # Bounded workaround for the accepted 10.0.300/latestPatch contract.
        # Do not modify the archive, extracted sources or ordinary Dockerfiles.
        contract = json.loads((source / 'global.json').read_text())
        need(contract == {'sdk': {'version': '10.0.300', 'rollForward': 'latestPatch'}},
             'UnsupportedSdkContract')
        need(re.fullmatch(r'10\.0\.3[0-9]{2}', SDK_VERSION), 'IncompatibleBuildSdk')
        sdk_ref = SDK_REPOSITORY + ':' + SDK_VERSION
        bases = {}
        runtime = None
        self.runtime_build = None
        for role in ROLES:
            original = source / ('src/BigBrain.' + ('Api' if role == 'api' else 'Web') + '/Dockerfile')
            lines = original.read_text().splitlines()
            if role == 'api':
                sdk_from = 'FROM ' + SDK_REPOSITORY + ':10.0 AS build'
                global_copy = 'COPY Directory.Build.props global.json ./'
                need(lines.count(sdk_from) == lines.count(global_copy) == 1, 'UnsupportedSdkBuildDefinition')
                lines[lines.index(sdk_from)] = 'FROM ' + sdk_ref + ' AS build'
                # Resolve with the archived global.json in scope, not image metadata.
                # A missing/wrong SDK fails the build before any database helper runs.
                lines.insert(lines.index(global_copy) + 1,
                             'RUN test "$(dotnet --version)" = "' + SDK_VERSION + '"')
            for index, line in enumerate(lines):
                if not line.upper().startswith('FROM '):
                    continue
                fields = line.split()
                ref = fields[1]
                need(re.fullmatch(r'[a-z0-9./_-]+:[A-Za-z0-9._-]+', ref), 'UnsupportedBuildBase')
                if ref not in bases:
                    docker('pull', ref, timeout=600, mutation=True)
                    info = json.loads(docker('image', 'inspect', ref))[0]
                    repo = ref.rsplit(':', 1)[0]
                    matches = [x for x in info.get('RepoDigests', []) if x.startswith(repo + '@sha256:')]
                    need(len(matches) == 1 and re.fullmatch(r'.+@sha256:[0-9a-f]{64}', matches[0]), 'UnpinnedBuildBase')
                    bases[ref] = matches[0]
                    if role == 'api' and repo == RUNTIME_REPOSITORY:
                        runtime = {'base': matches[0], 'versions': runtime_versions(info['Config'].get('Env'))}
                fields[1] = bases[ref]
                lines[index] = ' '.join(fields)
            if role == 'api':
                need(runtime is not None and lines.count('FROM ' + runtime['base'] + ' AS runtime') == 1,
                     'UnsupportedRuntimeBuildDefinition')
                version = runtime['versions']['DOTNET_VERSION']
                # Final-stage content proof, after all COPY/RUN instructions, as the
                # image user. Numeric values are validated before shell interpolation.
                lines.append('RUN runtimes="$(dotnet --list-runtimes)" && test "$runtimes" = "$(printf \'%s\\n\' '
                             + "'Microsoft.AspNetCore.App " + version + " [/usr/share/dotnet/shared/Microsoft.AspNetCore.App]' "
                             + "'Microsoft.NETCore.App " + version + " [/usr/share/dotnet/shared/Microsoft.NETCore.App]')\"")
            pinned = self.folder / (role + '.Dockerfile')
            pinned.write_text('\n'.join(lines) + '\n')
            iid = self.folder / (role + '.iid')
            docker('build', '--pull=false', '--file', str(pinned), '--iidfile', str(iid),
                   '--tag', f'bigbrain-standard-{role}:{self.revision}-{self.folder.name}',
                   '--label', 'org.opencontainers.image.revision=' + self.revision,
                   '--build-arg', 'BIGBRAIN_REVISION=' + self.revision, str(source), timeout=1200, mutation=True)
            self.images[role] = iid.read_text().strip()
            image_info(self.images[role])
            if role == 'api':
                self.runtime_build = {**runtime, 'revision': self.revision, 'image': self.images[role],
                                      'dockerfileSha256': backup.digest(pinned),
                                      'verification': 'final-stage dotnet --list-runtimes exact content check'}
            self.unchanged()
        self.bind_images()
        save(self.folder / 'build.json', {'revision': self.revision,
             'tree': command(['git', 'rev-parse', self.revision + '^{tree}']).decode().strip(),
             'archiveSha256': backup.digest(archive), 'bases': bases, 'images': self.images,
             'runtime': self.runtime_build,
             'sdk': {'version': SDK_VERSION, 'image': bases[sdk_ref],
                     'globalJsonSha256': backup.digest(source / 'global.json'),
                     'verification': 'dotnet --version with archived global.json during API build'}})

    def prove_restartability(self, backup_folder):
        results = []
        for role in ROLES:
            for purpose, image in (('rollback', self.originals[role]['Image']), ('candidate', self.images[role])):
                self.probe_image(role, image, backup_folder, purpose)
                results.append({'role': role, 'purpose': purpose, 'image': image, 'starts': 2, 'status': 'PASS'})
                self.unchanged()
        save(self.folder / 'restartability.json', {'backupManifestSha256': backup.verify_set(backup_folder),
             'isolation': 'network-none; no production mounts or ports; disposable backup copies', 'results': results})

    def probe_image(self, role, image, backup_folder, purpose):
        # Actual immutable image, entrypoint and non-root user. Never use the old
        # container's writable layer, production mounts, provider network or Docker socket.
        old = self.originals[role]
        uid = old['Config'].get('User', '')
        need(re.fullmatch(r'[1-9][0-9]*(?::[1-9][0-9]*)?', uid), 'ProbeUserUnknown')
        uid_number = uid.split(':')[0]
        gid = uid.split(':')[-1]
        folder = self.folder / ('probe-' + role + '-' + purpose)
        folder.mkdir(mode=0o700)
        args = ['create', '--name', 'bigbrain-standard-' + uuid.uuid4().hex,
                '--label', 'bigbrain.standard.probe=' + self.folder.name,
                '--network', 'none', '--read-only', '--cap-drop', 'ALL',
                '--security-opt', 'no-new-privileges', '--restart', 'no',
                '--memory', '768m', '--cpus', '1', '--pids-limit', '256', '--user', uid,
                '--tmpfs', f'/tmp:rw,nosuid,nodev,uid={uid_number},gid={gid},mode=0700']
        env = dict(entry.split('=', 1) for entry in self.expected_environment(role, image))
        roots = {}
        if role == 'api':
            # Disable external work only in the probe; production snapshot is unchanged.
            for key in list(env):
                if key.lower().endswith('__enabled') or key.lower().endswith('__directaudioimportenabled'):
                    env[key] = 'false'
            for key in ('MealPlanner__SchoolMeals__Enabled', 'MealPlanner__SchoolMeals__RosenfeldtEnabled',
                        'MealPlanner__SchoolMeals__MusikugglanEnabled', 'Sentinel__Enabled',
                        'Finance__RecurringLocalReasoner__Enabled', 'Finance__ResearchScheduler__Enabled',
                        'Finance__ObservationRuntime__Enabled', 'Media__Requests__DirectAudioImportEnabled',
                        'Media__SmartShuffle__Enabled'):
                env[key] = 'false'
            env['Finance__ResearchOperations__MaintenancePaused'] = 'true'
            for target, names in backup.EXPECTED.items():
                copies = folder / target.lstrip('/')
                copies.mkdir()
                # Accessible to the image UID only through its bind mount. The host
                # parent remains 0700; original backups and production stay untouched.
                copies.chmod(0o777)
                for name in names:
                    shutil.copyfile(backup_folder / target.lstrip('/') / name, copies / name)
                    (copies / name).chmod(0o666)
                roots[target] = (copies, 'isolated-copy')
                args += ['--mount', f'type=bind,source={copies},target={target}']
            for target in ('/finance-data', '/app/data'):
                args += ['--tmpfs', f'{target}:rw,nosuid,nodev,uid={uid_number},gid={gid},mode=0700']
        else:
            # nginx resolves its upstream at startup, but no upstream request is made.
            args += ['--add-host', 'api:127.0.0.1', '--tmpfs', f'/var/cache/nginx:uid={uid_number},gid={gid}',
                     '--tmpfs', f'/run:uid={uid_number},gid={gid}']
        need(all('\n' not in k + v and '\r' not in k + v for k, v in env.items()), 'ProbeEnvironmentInvalid')
        env_file = folder / 'environment'
        env_file.write_text(''.join(k + '=' + v + '\n' for k, v in env.items()))
        env_file.chmod(0o600)
        args += ['--env-file', str(env_file), image]
        save(folder / 'intent.json', {'name': args[2], 'image': image})
        cid = docker(*args, mutation=True).decode().strip()
        need(re.fullmatch(r'[0-9a-f]{64}', cid), 'ProbeIdentityUnknown')
        self.probes.add(cid)
        save(folder / 'identity.json', {'containerId': cid, 'image': image})
        initial_schema = schemas(roots) if roots else None
        try:
            for cycle in range(2):
                docker('start', cid, mutation=True)
                deadline = time.monotonic() + 60
                while True:
                    c = inspect(cid)
                    need(c['Image'] == image and c['State']['Running'] and not c['State']['OOMKilled'], 'ProbeExited')
                    need(c['HostConfig']['NetworkMode'] == 'none' and not c['HostConfig'].get('PortBindings')
                         and c['Config']['User'] == uid, 'ProbeIsolationChanged')
                    try:
                        if role == 'api':
                            check = "exec 3<>/dev/tcp/127.0.0.1/8080; printf 'GET /health HTTP/1.1\\r\\nHost: localhost\\r\\nConnection: close\\r\\n\\r\\n' >&3; IFS= read -r line <&3; [[ $line == *' 200 '* ]]"
                            docker('exec', cid, '/bin/bash', '-ec', check)
                        else:
                            docker('exec', cid, 'wget', '--spider', '-q', 'http://127.0.0.1:8080/')
                        break
                    except Blocked:
                        need(time.monotonic() < deadline, 'ProbeHealthDeadline')
                        time.sleep(1)
                docker('stop', '--time', '-1', cid, timeout=90, mutation=True)
                stopped = inspect(cid)
                need(not stopped['State']['Running'] and stopped['State']['ExitCode'] == 0
                     and not stopped['State']['OOMKilled'], 'ProbeUncleanStop')
                if roots:
                    need(schemas(roots) == initial_schema, 'ProbeSchemaChanged')
                record(self.folder, 'PROBE_' + role.upper() + '_' + purpose.upper() + '_' + str(cycle + 1))
        finally:
            # Retain the uniquely owned stopped probe and its private evidence. No rm,
            # production stop or force-kill; uncertain cleanup blocks the release.
            if not isinstance(sys.exc_info()[1], (Uncertain, KeyboardInterrupt, SystemExit)):
                try:
                    c = inspect(cid)
                    if c['State']['Running']:
                        docker('stop', '--time', '-1', cid, timeout=90, mutation=True)
                    need(not inspect(cid)['State']['Running'], 'ProbeStillRunning')
                except BaseException:
                    raise Uncertain('ProbeCleanupUncertain') from None

    def data_identity(self):
        need(self.data is not None, 'BackupDataUnverified')
        same_data(self.data, self.helper_data())

    def expected_environment(self, role, image):
        contract = self.runtime_contract
        if role == 'api' and contract is not None and image != self.originals[role]['Image']:
            need(image == self.images[role] == contract['image']
                 and self.originals[role]['Image'] == contract['original'], 'RuntimeImageBindingChanged')
            return contract['env']
        return self.originals[role]['Config']['Env']

    def runtime_matches(self, role, container, image):
        expected = copy.deepcopy(self.originals[role])
        expected['Config']['Env'] = self.expected_environment(role, image)
        return container['Image'] == image and fingerprint(container) == fingerprint(expected)

    def wait_healthy(self, role, image):
        deadline = time.monotonic() + 100
        while time.monotonic() < deadline:
            c = self.current(role)
            need(self.runtime_matches(role, c, image), 'ReplacementConfigChanged')
            need(c['Id'] == self.replacement_ids.get(role, self.originals[role]['Id']), 'ReplacementIdentityChanged')
            need(c['State']['Running'] or c['State'].get('Restarting'), 'ReplacementExited')
            if role == 'api' and self.timesync is not None and c['State']['Running']:
                need(timesync_identity(c, self.config['services']['api']) == self.timesync, 'TimesyncIdentityMismatch')
            try:
                healthy(c)
                http_health(role)
                return
            except (Blocked, OSError):
                time.sleep(2)
        raise Blocked('HealthDeadline')

    def stop(self, role, recovery=False):
        c = self.current(role)
        original = c['Id'] == self.originals[role]['Id']
        image = self.originals[role]['Image'] if original else self.images[role]
        need((original or recovery and c['Id'] == self.replacement_ids.get(role))
             and self.runtime_matches(role, c, image), 'UncleanStop')
        if c['State']['Running'] or c['State'].get('Restarting'):
            docker('stop', '--time', '-1', c['Id'], timeout=90, mutation=True)
        stopped = inspect(c['Id'])
        need(stopped['Id'] == c['Id'] and self.runtime_matches(role, stopped, image), 'UncleanStop')
        # A failed candidate is not the original healthy service. Its known exit=1
        # must not prevent image rollback, but never waive the original's clean stop.
        failed_candidate = (recovery and stopped['Id'] != self.originals[role]['Id']
                            and stopped['Image'] == self.images[role]
                            and self.runtime_matches(role, stopped, self.images[role])
                            and stopped['State'].get('Status') == 'exited')
        need(not stopped['State']['Running'] and not stopped['State'].get('Restarting')
             and not stopped['State'].get('Dead') and not stopped['State']['OOMKilled']
             and (stopped['State']['ExitCode'] == 0 or failed_candidate and stopped['State']['ExitCode'] == 1), 'UncleanStop')

    def switch(self, role, recovery=False):
        self.stop(role, recovery=recovery)
        path = self.folder / ('rollback.json' if recovery else 'release.json')
        previous_id = self.current(role)['Id']
        try:
            self.compose(path, 'up', '-d', '--no-deps', '--no-build', '--pull', 'never', '--force-recreate', role, mutation=True)
        except Uncertain:
            raise
        except Blocked:
            self.identify_replacement(role, previous_id, recovery)
            raise
        self.identify_replacement(role, previous_id, recovery)
        self.wait_healthy(role, self.originals[role]['Image'] if recovery else self.images[role])

    def identify_replacement(self, role, previous_id, recovery):
        c = self.current(role)
        image = self.originals[role]['Image'] if recovery else self.images[role]
        need(c['Id'] != previous_id and self.runtime_matches(role, c, image), 'ReplacementConfigChanged')
        self.replacement_ids[role] = c['Id']

    def execute(self):
        no_pending_run(self.state)
        self.folder = self.state / ('run-' + uuid.uuid4().hex)
        self.folder.mkdir(mode=0o700)
        sync_dir(self.state)
        record(self.folder, 'PREPARING')
        try:
            save(self.folder / 'baseline.json', self.config)
            save(self.folder / 'provenance.json', self.provenance)
            record(self.folder, 'BUILDING')
            self.build_images()
            save(self.folder / 'release.json', self.release)
            save(self.folder / 'rollback.json', self.rollback)
            # Preserve canonical Compose escaping; verify the frozen execution model.
            for name, expected in (('release', self.release), ('rollback', self.rollback)):
                rendered = json.loads(self.compose(self.folder / (name + '.json'), 'config', '--format', 'json'))
                need(snapshot_matches(rendered, copy.deepcopy(expected)), 'SnapshotChanged')
            folder = self.backup_data()
            digest = backup.verify_set(folder)
            self.prove_restartability(folder)
            need(backup.verify_set(folder) == digest, 'BackupChangedDuringProbe')
            save(self.folder / 'receipt.json', {'revision': self.revision, 'images': self.images,
                 'previous': {r: c['Image'] for r, c in self.originals.items()},
                 'backupId': folder.name, 'backupPath': str(folder.relative_to(self.folder)), 'backupManifestSha256': digest})
            need(git_guard(self.revision, self.originals) == self.provenance, 'BuildProvenanceChanged')
            self.unchanged()
            self.data_identity()
            for role in ROLES:
                # Persist intent before every mutation; abrupt death blocks automatic retries.
                record(self.folder, 'SWITCHING_' + role.upper())
                self.changed.append(role)
                self.switch(role)
            self.data_identity()
            need(self.others() == self.other_snapshot, 'OtherServiceChanged')
            record(self.folder, 'PASS')
        except BaseException as error:
            if self.helper_incomplete:
                record(self.folder, 'MANUAL_REVIEW_REQUIRED')
            elif not self.changed:
                record(self.folder, 'MANUAL_REVIEW_REQUIRED' if isinstance(error, (Uncertain, KeyboardInterrupt, SystemExit)) else 'PRE_SWITCH_FAILED')
            elif isinstance(error, (Uncertain, KeyboardInterrupt, SystemExit)):
                record(self.folder, 'MANUAL_REVIEW_REQUIRED')
            else:
                try:
                    # No DB restore: preserve writes made after the backup.
                    self.data_identity()
                    need(self.others() == self.other_snapshot, 'OtherServiceChanged')
                    record(self.folder, 'ROLLING_BACK')
                    for role in self.changed:  # API before Web, also on recovery.
                        self.switch(role, recovery=True)
                    for role in ROLES:
                        self.wait_healthy(role, self.originals[role]['Image'])
                    self.data_identity()
                    need(self.others() == self.other_snapshot, 'OtherServiceChanged')
                    record(self.folder, 'ROLLED_BACK')
                except BaseException:
                    record(self.folder, 'MANUAL_REVIEW_REQUIRED')
                    raise Blocked('RollbackFailedManualReview') from None
            raise


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--revision', required=True)
    parser.add_argument('--compose', type=Path, help='Optional ordinary Compose file; default discovers active files from runtime labels')
    parser.add_argument('--execute', action='store_true', help='Separately authorized deployment; default is read-only preflight')
    args = parser.parse_args()
    os.umask(0o077)
    # Compose interpolation must not inherit alternative files, projects or orphan removal.
    for name in list(os.environ):
        if name.startswith(('COMPOSE_', 'DOCKER_')):
            del os.environ[name]
    state = Path.home() / '.local/state/bigbrain-standard'
    deployment = Deployment(args.revision, {}, args.compose, state)
    if not args.execute:
        deployment.preflight()
        print('PREFLIGHT BLOCKED: data inventory/schema/backup DEFERRED to separately authorized --execute; no mutations performed')
        raise SystemExit(2)
    for folder in (state, Path.home() / '.local/state/bigbrain-web-deploy'):
        backup.no_symlinks(folder)
        folder.mkdir(mode=0o700, parents=True, exist_ok=True)
        need(folder.stat().st_uid == os.geteuid() and folder.stat().st_mode & 0o777 == 0o700, 'UnsafeState')
    # Same lock as the existing Web-only script; no concurrent STANDARD replacement.
    lock = Path.home() / '.local/state/bigbrain-web-deploy/lock'
    fd = os.open(lock, os.O_CREAT | os.O_WRONLY | os.O_NOFOLLOW, 0o600)
    with os.fdopen(fd, 'w') as stream:
        fcntl.flock(stream, fcntl.LOCK_EX | fcntl.LOCK_NB)
        no_pending_run(state)
        deployment.preflight()
        deployment.execute()
    print('PASS: API+Web replaced; iPhone/PWA and school source verification PENDING')


if __name__ == '__main__':
    try:
        for sig in (signal.SIGHUP, signal.SIGTERM):
            signal.signal(sig, lambda *_: (_ for _ in ()).throw(KeyboardInterrupt()))
        main()
    except SystemExit:
        raise
    except BaseException:
        print('STANDARD BLOCKED: inspect private journal; no automatic retry or database restore', file=sys.stderr)
        sys.exit(1)
