#!/usr/bin/env python3
"""T2: bounded API+Web image replacement. Independent review required before use.

No merge, build, pull, schema operation, privilege grant or database restore.
Default is read-only preflight. See docs/policy/lanes.md for authority and limits.
"""
import argparse
import copy
from contextlib import closing
import fcntl
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import re
import signal
import sqlite3
import subprocess
import sys
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
# Intentionally narrow initial lane. Expansion requires review, never a path-only safety claim.
APP_FILES = {
    'src/BigBrain.Api/MealPlanner/SchoolMealService.cs',
    'src/BigBrain.Api/MealPlanner/MealPlannerStore.SchoolMeals.cs',
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


def command(args, timeout=30, mutation=False):
    # Retain no raw stdout/stderr on failure: Compose/inspect may contain secrets.
    try:
        result = subprocess.run(args, cwd=ROOT, stdin=subprocess.DEVNULL,
                                capture_output=True, timeout=timeout, check=False)
    except (subprocess.TimeoutExpired, KeyboardInterrupt):
        raise Uncertain('CommandInterrupted') if mutation else Blocked('CommandInterrupted') from None
    need(result.returncode == 0, 'CommandFailed')
    return result.stdout


def docker(*args, timeout=30, mutation=False):
    # Fixed local daemon, never a caller-selected context/remote host.
    return command(['docker', '--host', 'unix:///var/run/docker.sock', *args], timeout, mutation)


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


def git_guard(revision, originals):
    need(re.fullmatch(SHA, revision), 'InvalidRevision')
    remote = command(['git', 'ls-remote', 'origin', 'refs/heads/main']).decode().split()
    need(remote and remote[0] == revision, 'NotCurrentRemoteMain')
    need(command(['git', 'rev-parse', 'HEAD']).decode().strip() == revision, 'CheckoutNotAcceptedMain')
    need(not command(['git', 'status', '--porcelain', '--untracked-files=no']).strip(), 'TrackedChanges')
    command(['git', 'merge-base', '--is-ancestor', MEMORY_FETCH, revision])
    for old in originals.values():
        base = old['Config']['Labels'].get('org.opencontainers.image.revision', '')
        need(re.fullmatch(SHA, base), 'OldRevisionUnknown')
        command(['git', 'merge-base', '--is-ancestor', base, revision])
        paths = command(['git', 'diff', '--no-renames', '--name-only', '-z', base, revision]).decode().split('\0')
        for path in filter(None, paths):
            need(path in APP_FILES or path.startswith(('docs/', 'tests/', 'scripts/'))
                 or path in {'AGENTS.md', '.github/workflows/ci.yml'}, 'OutsideStandardScope')
    ci_green(revision)


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
    # Existing volumes/networks only. Never let Compose create a network or volume.
    for kind in ('volumes', 'networks'):
        result[kind] = {key: {'name': entry['name'], 'external': True}
                        for key, entry in result.get(kind, {}).items()}
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
    with path.open('x', encoding='utf8') as stream:
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


class Deployment:
    def __init__(self, revision, images, compose_file, state):
        self.revision, self.images, self.source, self.state = revision, images, compose_file, state
        self.originals = {}
        self.changed = []
        self.folder = None

    def compose(self, path, *args, mutation=False):
        return docker('compose', '--project-name', PROJECT, '--project-directory', str(self.source.parent),
                      '--env-file', '/dev/null', '-f', str(path),
                      *args, timeout=120, mutation=mutation)

    def current(self, role):
        ids = docker('ps', '-aq', '--no-trunc', '--filter', f'label=com.docker.compose.project={PROJECT}',
                     '--filter', f'label=com.docker.compose.service={role}').decode().split()
        need(len(ids) == 1, 'ServiceIdentityAmbiguous')
        return inspect(ids[0])

    def others(self):
        target_ids = {self.current(r)['Id'] for r in ROLES}
        result = {}
        for cid in docker('ps', '-aq', '--no-trunc').decode().split():
            if cid not in target_ids:
                c = inspect(cid)
                result[cid] = (c['Image'], fingerprint(c), c['State']['Running'], c['State']['StartedAt'])
        return result

    def unchanged(self):
        need(self.others() == self.other_snapshot, 'OtherServiceChanged')
        for role, old in self.originals.items():
            current = self.current(role)
            need(current['Id'] == old['Id'] and current['Image'] == old['Image']
                 and fingerprint(current) == fingerprint(old)
                 and current['State']['StartedAt'] == old['State']['StartedAt'], 'RuntimeChanged')
            healthy(current)

    def preflight(self):
        need(self.source.is_absolute() and self.source.is_file(), 'ComposeMissing')
        backup.no_symlinks(self.source)
        self.config = json.loads(self.compose(self.source, 'config', '--format', 'json'))
        hashes = dict(line.split() for line in self.compose(self.source, 'config', '--hash', '*').decode().splitlines())
        for role in ROLES:
            old = self.current(role)
            healthy(old)
            need(old['Config']['Labels'].get('com.docker.compose.config-hash') == hashes[role], 'ComposeRuntimeMismatch')
            prior, candidate = image_info(old['Image']), image_info(self.images[role])
            need(candidate['Config'].get('Labels', {}).get('org.opencontainers.image.revision') == self.revision, 'ImageRevisionMismatch')
            need(all(candidate['Config'].get(k) == prior['Config'].get(k) for k in IMAGE_FIELDS), 'ImageRuntimeContractChanged')
            # A healthy container repaired in its writable layer is not a safe rollback image.
            diff = docker('diff', old['Id']).decode().splitlines()
            need(not any(not (row[2:] == '/tmp' or row[2:].startswith(('/tmp/', '/var/cache/nginx', '/var/run/', '/run/')))
                         for row in diff), 'RollbackImageHasRuntimeRepairs')
            for name in old['Config']['Env']:
                need('VerifiedInitialNotBeforeUtc=' not in name, 'LegacyBootstrapPresent')
            self.originals[role] = old
            http_health(role)
        git_guard(self.revision, self.originals)
        self.other_snapshot = self.others()
        self.release = release_config(self.config, self.images)
        for service in self.release['services'].values():
            service.setdefault('labels', {})['org.opencontainers.image.revision'] = self.revision
        self.rollback = release_config(self.config, {r: c['Image'] for r, c in self.originals.items()})
        # Resolve existing resource IDs; no creation is permitted by the execution snapshot.
        for kind, noun in (('networks', 'network'), ('volumes', 'volume')):
            for value in self.release.get(kind, {}).values():
                docker(noun, 'inspect', value['name'])
        self.roots = backup.sources(self.originals['api'])
        self.inventory, _ = backup.inventory(self.roots)
        self.schemas = schemas(self.roots)
        self.unchanged()

    def data_identity(self):
        need(schemas(self.roots) == self.schemas, 'SchemaChangedNoAutomaticRollback')
        after, _ = backup.inventory(self.roots)
        need(after.keys() == self.inventory.keys(), 'DatabaseCoverageChanged')
        for key, old in self.inventory.items():
            need(all(after[key][k] == old[k] for k in ('device', 'inode', 'uid', 'gid', 'mode')), 'DatabaseIdentityChanged')

    def wait_healthy(self, role, image):
        deadline = time.monotonic() + 100
        while time.monotonic() < deadline:
            c = self.current(role)
            need(c['Image'] == image and fingerprint(c) == fingerprint(self.originals[role]), 'ReplacementConfigChanged')
            try:
                healthy(c)
                http_health(role)
                return
            except (Blocked, OSError):
                time.sleep(2)
        raise Blocked('HealthDeadline')

    def stop(self, role):
        c = self.current(role)
        if c['State']['Running']:
            docker('stop', '--time', '-1', c['Id'], timeout=90, mutation=True)
        stopped = inspect(c['Id'])
        need(not stopped['State']['Running'] and not stopped['State']['OOMKilled']
             and stopped['State']['ExitCode'] == 0, 'UncleanStop')

    def switch(self, role, recovery=False):
        self.stop(role)
        path = self.folder / ('rollback.json' if recovery else 'release.json')
        self.compose(path, 'up', '-d', '--no-deps', '--no-build', '--pull', 'never', '--force-recreate', role, mutation=True)
        self.wait_healthy(role, self.originals[role]['Image'] if recovery else self.images[role])

    def execute(self):
        no_pending_run(self.state)
        self.folder = self.state / ('run-' + uuid.uuid4().hex)
        self.folder.mkdir(mode=0o700)
        sync_dir(self.state)
        record(self.folder, 'PREPARING')
        try:
            save(self.folder / 'release.json', self.release)
            save(self.folder / 'rollback.json', self.rollback)
            # Preserve canonical Compose escaping; verify the frozen execution model.
            for name, expected in (('release', self.release), ('rollback', self.rollback)):
                rendered = json.loads(self.compose(self.folder / (name + '.json'), 'config', '--format', 'json'))
                need(snapshot_matches(rendered, copy.deepcopy(expected)), 'SnapshotChanged')
            identity = {'containerId': self.originals['api']['Id'], 'image': self.originals['api']['Image'],
                        'revision': self.originals['api']['Config']['Labels']['org.opencontainers.image.revision']}
            folder = backup.run_backup(self.roots, self.state / 'backups', identity, self.unchanged)
            digest = backup.verify_set(folder)
            save(self.folder / 'receipt.json', {'revision': self.revision, 'images': self.images,
                 'previous': {r: c['Image'] for r, c in self.originals.items()},
                 'backupId': folder.name, 'backupManifestSha256': digest})
            git_guard(self.revision, self.originals)
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
            if not self.changed:
                record(self.folder, 'PRE_SWITCH_FAILED')
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
    parser.add_argument('--api-image', required=True)
    parser.add_argument('--web-image', required=True)
    parser.add_argument('--compose', required=True, type=Path)
    parser.add_argument('--execute', action='store_true', help='Separately authorized deployment; default is read-only preflight')
    args = parser.parse_args()
    os.umask(0o077)
    # Compose interpolation must not inherit alternative files, projects or orphan removal.
    for name in list(os.environ):
        if name.startswith(('COMPOSE_', 'DOCKER_')):
            del os.environ[name]
    state = Path.home() / '.local/state/bigbrain-standard'
    deployment = Deployment(args.revision, {'api': args.api_image, 'web': args.web_image}, args.compose, state)
    if not args.execute:
        deployment.preflight()
        print('PREFLIGHT PASS; no backup, deployment or acceptance performed')
        return
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
